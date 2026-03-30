using UnityEngine;

/// <summary>
/// Changes gravity direction within this zone.
/// Stage gimmick: specific rooms can flip or rotate gravity.
/// Only affects the player.
/// </summary>
public class GravityZone : MonoBehaviour
{
    [SerializeField] private Vector2 gravityDirection = new Vector2(0f, 1f); // Inverted gravity
    [SerializeField] private float gravityStrength = 20f;

    private static readonly Vector2 DefaultGravity = new Vector2(0f, -20f);

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.TryGetComponent<PlayerController>(out _))
        {
            Physics2D.gravity = gravityDirection.normalized * gravityStrength;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.TryGetComponent<PlayerController>(out _))
        {
            Physics2D.gravity = DefaultGravity;
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.2f);
        var col = GetComponent<BoxCollider2D>();
        if (col != null)
        {
            Gizmos.DrawCube(transform.position + (Vector3)col.offset, col.size);
            Gizmos.color = Color.red;
            Gizmos.DrawRay(transform.position, gravityDirection.normalized * 2f);
        }
    }
}
