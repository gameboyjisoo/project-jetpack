using UnityEngine;

/// <summary>
/// Defines a room boundary. Each room is a screen-sized area (Celeste-style).
/// </summary>
public class Room : MonoBehaviour
{
    [SerializeField] private Vector2 roomSize = new Vector2(30f, 17f);
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private string roomId;

    public Vector2 RoomSize => roomSize;
    public Vector2 RoomCenter => (Vector2)transform.position;
    public Transform SpawnPoint => spawnPoint;
    public string RoomId => roomId;

    public void Init(string id, Vector2 size, Transform spawn)
    {
        roomId = id;
        roomSize = size;
        spawnPoint = spawn;
    }

    public Bounds GetBounds()
    {
        return new Bounds(RoomCenter, new Vector3(roomSize.x, roomSize.y, 1f));
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0f, 1f, 1f, 0.3f);
        Gizmos.DrawWireCube(transform.position, new Vector3(roomSize.x, roomSize.y, 0f));

        Gizmos.color = new Color(0f, 1f, 1f, 0.1f);
        Gizmos.DrawCube(transform.position, new Vector3(roomSize.x, roomSize.y, 0f));
    }
}
