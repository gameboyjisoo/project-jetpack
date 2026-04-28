using UnityEngine;

/// <summary>
/// Celeste-style room-snapping camera.
/// When a Room is active, the camera locks to the room's center.
/// Transitions between rooms use a smooth lerp over transitionDuration seconds.
/// Falls back to follow-player mode when no rooms exist in the scene.
///
/// </summary>
public class RoomCamera : MonoBehaviour
{
    [Header("Follow Mode (fallback when no rooms)")]
    [SerializeField] private float followSpeed = 8f;

    [Header("Room Snap")]
    [SerializeField] private float transitionDuration = 0.3f;
    [SerializeField] private float zOffset = -10f;

    [Header("Screen Shake (Accessibility)")]
    [SerializeField] private bool enableScreenShake = true;

    private Transform target;
    private Camera cam;

    // Room-snap state
    private Room activeRoom;
    private bool isTransitioning;
    private Vector3 transitionStart;
    private Vector3 transitionEnd;
    private float transitionTimer;

    // Screen shake state
    private float shakeTimer;
    private float shakeMagnitude;
    private float shakeDuration;

    /// <summary>True while the camera is lerping between rooms.</summary>
    public bool IsTransitioning => isTransitioning;

    private void Start()
    {
        cam = GetComponent<Camera>();

        if (target == null)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                target = player.transform;
        }

        if (cam != null && cam.orthographic)
            cam.orthographicSize = 8.5f;
    }

    private void OnEnable()
    {
        GameEventBus.Subscribe<PlayerDied>(OnPlayerDied);
    }

    private void OnDisable()
    {
        GameEventBus.Unsubscribe<PlayerDied>(OnPlayerDied);
    }

    private void OnPlayerDied(PlayerDied evt)
    {
        Shake(0.2f, 0.12f);
    }

    /// <summary>
    /// Trigger a screen shake. Respects the enableScreenShake toggle.
    /// </summary>
    public void Shake(float duration, float magnitude)
    {
        if (!enableScreenShake) return;
        shakeTimer = duration;
        shakeDuration = duration;
        shakeMagnitude = magnitude;
    }

    /// <summary>
    /// Instantly snap camera to a room with no transition (used for initial placement).
    /// </summary>
    public void SetRoom(Room room)
    {
        activeRoom = room;
        isTransitioning = false;

        if (room != null && target != null)
        {
            // Snap to player position clamped within room bounds
            transform.position = ClampToRoom(target.position, room);
        }
        else if (room != null)
        {
            transform.position = new Vector3(room.RoomCenter.x, room.RoomCenter.y, zOffset);
        }
    }

    private Vector3 ClampToRoom(Vector3 pos, Room room)
    {
        float camHalfH = cam != null ? cam.orthographicSize : 8.5f;
        float camHalfW = camHalfH * (cam != null ? cam.aspect : 16f / 9f);

        Vector2 rc = room.RoomCenter;
        Vector2 rs = room.RoomSize;
        float rHalfW = rs.x * 0.5f;
        float rHalfH = rs.y * 0.5f;

        float minX = rc.x - rHalfW + camHalfW;
        float maxX = rc.x + rHalfW - camHalfW;
        float minY = rc.y - rHalfH + camHalfH;
        float maxY = rc.y + rHalfH - camHalfH;

        float x = (minX < maxX) ? Mathf.Clamp(pos.x, minX, maxX) : rc.x;
        float y = (minY < maxY) ? Mathf.Clamp(pos.y, minY, maxY) : rc.y;

        return new Vector3(x, y, zOffset);
    }

    /// <summary>
    /// Smoothly transition to a new room over transitionDuration seconds.
    /// Targets the player's position clamped to the new room, not the room center,
    /// so large-room transitions feel correct.
    /// </summary>
    public void TransitionToRoom(Room room)
    {
        if (room == null) return;

        activeRoom = room;
        transitionStart = transform.position;

        // Target where the player actually is inside the new room, not its center
        if (target != null)
            transitionEnd = ClampToRoom(target.position, room);
        else
            transitionEnd = new Vector3(room.RoomCenter.x, room.RoomCenter.y, zOffset);

        transitionTimer = 0f;
        isTransitioning = true;
    }

    /// <summary>
    /// Clear the active room, returning the camera to follow mode.
    /// </summary>
    public void ClearRoom()
    {
        activeRoom = null;
        isTransitioning = false;
    }

    private void LateUpdate()
    {
        if (isTransitioning)
            UpdateTransition();
        else if (activeRoom != null)
            UpdateRoomFollow();
        else
            UpdateFollow();

        ApplyShake();
    }

    private void UpdateTransition()
    {
        transitionTimer += Time.deltaTime;
        float t = Mathf.Clamp01(transitionTimer / transitionDuration);

        // Smooth-step for a nicer ease-in/ease-out
        float smooth = t * t * (3f - 2f * t);

        transform.position = Vector3.Lerp(transitionStart, transitionEnd, smooth);

        if (t >= 1f)
        {
            isTransitioning = false;
            transform.position = transitionEnd;
        }
    }

    /// <summary>
    /// Follow the player within the active room, clamping so the camera
    /// never shows outside the room bounds. For single-screen rooms this
    /// degrades to center-lock (same as before).
    /// </summary>
    private void UpdateRoomFollow()
    {
        if (target == null) return;

        float camHalfH = cam.orthographicSize;
        float camHalfW = camHalfH * cam.aspect;

        Vector2 roomCenter = activeRoom.RoomCenter;
        Vector2 roomSize = activeRoom.RoomSize;
        float roomHalfW = roomSize.x * 0.5f;
        float roomHalfH = roomSize.y * 0.5f;

        // Follow player smoothly
        Vector3 desired = new Vector3(target.position.x, target.position.y, zOffset);
        Vector3 pos = Vector3.Lerp(transform.position, desired, followSpeed * Time.deltaTime);

        // Clamp to room bounds so the camera never shows outside the room
        float minX = roomCenter.x - roomHalfW + camHalfW;
        float maxX = roomCenter.x + roomHalfW - camHalfW;
        float minY = roomCenter.y - roomHalfH + camHalfH;
        float maxY = roomCenter.y + roomHalfH - camHalfH;

        // If room is smaller than viewport on an axis, center on that axis
        pos.x = (minX < maxX) ? Mathf.Clamp(pos.x, minX, maxX) : roomCenter.x;
        pos.y = (minY < maxY) ? Mathf.Clamp(pos.y, minY, maxY) : roomCenter.y;

        transform.position = pos;
    }

    private void UpdateFollow()
    {
        if (target == null) return;

        Vector3 targetPos = new Vector3(target.position.x, target.position.y, zOffset);
        transform.position = Vector3.Lerp(transform.position, targetPos, followSpeed * Time.deltaTime);
    }

    private void ApplyShake()
    {
        if (shakeTimer <= 0f) return;

        float decay = shakeTimer / shakeDuration;
        Vector2 offset = Random.insideUnitCircle * (shakeMagnitude * decay);
        transform.position += new Vector3(offset.x, offset.y, 0f);

        shakeTimer -= Time.deltaTime;
    }
}
