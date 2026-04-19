using UnityEngine;

/// <summary>
/// Celeste-style room-snapping camera.
/// When a Room is active, the camera locks to the room's center.
/// Transitions between rooms use a smooth lerp over transitionDuration seconds.
/// Falls back to follow-player mode when no rooms exist in the scene.
/// </summary>
public class RoomCamera : MonoBehaviour
{
    [Header("Follow Mode (fallback when no rooms)")]
    [SerializeField] private float followSpeed = 8f;

    [Header("Room Snap")]
    [SerializeField] private float transitionDuration = 0.3f;
    [SerializeField] private float zOffset = -10f;

    private Transform target;
    private Camera cam;

    // Room-snap state
    private Room activeRoom;
    private bool isTransitioning;
    private Vector3 transitionStart;
    private Vector3 transitionEnd;
    private float transitionTimer;

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

    /// <summary>
    /// Instantly snap camera to a room with no transition (used for initial placement).
    /// </summary>
    public void SetRoom(Room room)
    {
        activeRoom = room;
        isTransitioning = false;

        if (room != null)
        {
            Vector3 roomPos = new Vector3(room.RoomCenter.x, room.RoomCenter.y, zOffset);
            transform.position = roomPos;
        }
    }

    /// <summary>
    /// Smoothly transition to a new room over transitionDuration seconds.
    /// </summary>
    public void TransitionToRoom(Room room)
    {
        if (room == null) return;

        activeRoom = room;
        transitionStart = transform.position;
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
        {
            UpdateTransition();
            return;
        }

        if (activeRoom != null)
        {
            // Locked to room center — no movement needed
            return;
        }

        // Fallback: smooth follow (no rooms in scene)
        UpdateFollow();
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

    private void UpdateFollow()
    {
        if (target == null) return;

        Vector3 targetPos = new Vector3(target.position.x, target.position.y, zOffset);
        transform.position = Vector3.Lerp(transform.position, targetPos, followSpeed * Time.deltaTime);
    }
}
