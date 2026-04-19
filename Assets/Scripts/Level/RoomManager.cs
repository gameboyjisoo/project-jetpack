using UnityEngine;

/// <summary>
/// Manages room transitions when the player crosses room boundaries.
/// Celeste-style: camera snaps to the new room. During transitions player
/// input is never locked — the camera simply lerps to the new position.
/// Publishes RoomTransitionStarted, RoomEntered, and RoomTransitionCompleted
/// events via GameEventBus.
/// </summary>
public class RoomManager : MonoBehaviour
{
    public static RoomManager Instance { get; private set; }

    [SerializeField] private Room[] rooms;

    private Room currentRoom;
    private Transform playerTransform;
    private RoomCamera roomCamera;
    private bool waitingForTransition;

    public Room CurrentRoom => currentRoom;

    public event System.Action<Room, Room> OnRoomChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        if (rooms == null || rooms.Length == 0)
            rooms = FindObjectsByType<Room>(FindObjectsSortMode.None);

        roomCamera = FindFirstObjectByType<RoomCamera>();

        var player = FindFirstObjectByType<PlayerController>();
        if (player != null)
        {
            playerTransform = player.transform;
            currentRoom = GetRoomAtPosition(playerTransform.position);

            // If player starts inside a room, snap camera immediately (no lerp)
            if (currentRoom != null && roomCamera != null)
            {
                roomCamera.SetRoom(currentRoom);
                GameEventBus.Publish(new RoomEntered
                {
                    SpawnPoint = currentRoom.SpawnPoint != null
                        ? (Vector2)currentRoom.SpawnPoint.position
                        : currentRoom.RoomCenter,
                    RoomId = currentRoom.RoomId
                });
            }
        }
    }

    private void Update()
    {
        if (playerTransform == null) return;

        // While a transition is animating, check if it completed
        if (waitingForTransition && roomCamera != null && !roomCamera.IsTransitioning)
        {
            waitingForTransition = false;
            GameEventBus.Publish(new RoomTransitionCompleted
            {
                RoomId = currentRoom != null ? currentRoom.RoomId : ""
            });
        }

        // No rooms in scene — camera stays in follow mode
        if (rooms == null || rooms.Length == 0) return;
        if (currentRoom == null)
        {
            // Player might have just entered the first room
            Room firstRoom = GetRoomAtPosition(playerTransform.position);
            if (firstRoom != null)
                EnterRoom(firstRoom, null);
            return;
        }

        Room newRoom = GetRoomAtPosition(playerTransform.position);
        if (newRoom != null && newRoom != currentRoom)
        {
            EnterRoom(newRoom, currentRoom);
        }
    }

    public Room GetRoomAtPosition(Vector2 position)
    {
        foreach (var room in rooms)
        {
            if (room.GetBounds().Contains(new Vector3(position.x, position.y, room.transform.position.z)))
                return room;
        }
        return null;
    }

    private void EnterRoom(Room newRoom, Room oldRoom)
    {
        string fromId = oldRoom != null ? oldRoom.RoomId : "";
        string toId = newRoom.RoomId;

        // Publish transition start
        GameEventBus.Publish(new RoomTransitionStarted
        {
            FromRoomId = fromId,
            ToRoomId = toId
        });

        Room previousRoom = currentRoom;
        currentRoom = newRoom;

        // Tell the camera to lerp to the new room
        if (roomCamera != null)
        {
            if (oldRoom == null)
                roomCamera.SetRoom(newRoom); // First room — snap instantly
            else
                roomCamera.TransitionToRoom(newRoom); // Smooth transition
        }

        waitingForTransition = roomCamera != null && roomCamera.IsTransitioning;

        // Publish room entered
        GameEventBus.Publish(new RoomEntered
        {
            SpawnPoint = newRoom.SpawnPoint != null
                ? (Vector2)newRoom.SpawnPoint.position
                : newRoom.RoomCenter,
            RoomId = toId
        });

        // Legacy event for anything using the direct delegate
        OnRoomChanged?.Invoke(previousRoom, newRoom);

        // If no transition was needed (instant snap), fire completed immediately
        if (!waitingForTransition)
        {
            GameEventBus.Publish(new RoomTransitionCompleted
            {
                RoomId = toId
            });
        }
    }
}
