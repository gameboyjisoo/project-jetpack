using UnityEngine;

/// <summary>
/// Camera that follows the player smoothly.
/// Will be replaced with room-snapping camera once levels are designed.
/// </summary>
public class RoomCamera : MonoBehaviour
{
    [SerializeField] private float followSpeed = 8f;
    [SerializeField] private float zOffset = -10f;
    [SerializeField] private Transform target;

    private void Start()
    {
        if (target == null)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                target = player.transform;
        }

        var cam = GetComponent<Camera>();
        if (cam != null && cam.orthographic)
            cam.orthographicSize = 8.5f;
    }

    private void LateUpdate()
    {
        if (target == null) return;

        Vector3 targetPos = new Vector3(target.position.x, target.position.y, zOffset);
        transform.position = Vector3.Lerp(transform.position, targetPos, followSpeed * Time.deltaTime);
    }
}
