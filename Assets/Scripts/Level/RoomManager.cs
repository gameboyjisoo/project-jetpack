using UnityEngine;

/// <summary>
/// Manages room transitions when the player crosses room boundaries.
/// Celeste-style: camera snaps to the new room, player appears at the entry edge.
/// </summary>
public class RoomManager : MonoBehaviour
{
    public static RoomManager Instance { get; private set; }

    [SerializeField] private Room[] rooms;
    [SerializeField] private float transitionDuration = 0.3f;

    private Room currentRoom;
    private Transform playerTransform;

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

        var player = FindFirstObjectByType<PlayerController>();
        if (player != null)
        {
            playerTransform = player.transform;
            currentRoom = GetRoomAtPosition(playerTransform.position);
        }
    }

    private void Update()
    {
        if (playerTransform == null || currentRoom == null) return;

        Room newRoom = GetRoomAtPosition(playerTransform.position);
        if (newRoom != null && newRoom != currentRoom)
        {
            TransitionToRoom(newRoom);
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

    private void TransitionToRoom(Room newRoom)
    {
        Room oldRoom = currentRoom;
        currentRoom = newRoom;
        OnRoomChanged?.Invoke(oldRoom, newRoom);
    }
}
