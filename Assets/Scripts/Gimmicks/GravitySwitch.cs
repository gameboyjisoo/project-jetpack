using UnityEngine;

/// <summary>
/// Trigger zone that changes gravity direction when the player enters.
/// Place 4 variants (Down/Up/Left/Right) with arrow sprites pointing
/// in the direction gravity will pull.
/// </summary>
[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class GravitySwitch : MonoBehaviour
{
    [Header("Direction")]
    [SerializeField] private GravityDir targetDirection = GravityDir.Down;

    private void Awake()
    {
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other) => TrySwitch(other);
    private void OnTriggerStay2D(Collider2D other) => TrySwitch(other);

    private void TrySwitch(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (GravityState.Current == targetDirection) return;

        // Decompose velocity in OLD gravity axes before switching
        var rb = other.attachedRigidbody;
        float moveSpeed = 0f;
        float upSpeed = 0f;
        if (rb != null)
        {
            moveSpeed = GravityState.GetMoveSpeed(rb.linearVelocity);
            upSpeed = GravityState.GetUpSpeed(rb.linearVelocity);
        }

        GravityState.Set(targetDirection);

        // Recompose velocity in NEW gravity axes — no leftover momentum in the old direction
        if (rb != null)
            rb.linearVelocity = GravityState.ComposeVelocity(moveSpeed, upSpeed);

        GameEventBus.Publish(new GravitySwitched
        {
            NewDirection = targetDirection,
            Position = transform.position
        });
        GameEventBus.Publish(new GimmickActivated { GimmickId = gameObject.name });
    }

    private void OnDrawGizmos()
    {
        // Arrow showing gravity direction
        Vector2 dir = targetDirection switch
        {
            GravityDir.Down  => Vector2.down,
            GravityDir.Up    => Vector2.up,
            GravityDir.Left  => Vector2.left,
            GravityDir.Right => Vector2.right,
            _ => Vector2.down
        };

        Gizmos.color = Color.yellow;
        Vector3 pos = transform.position;
        Gizmos.DrawLine(pos, pos + (Vector3)(dir * 2f));
        // Arrowhead
        Vector2 perp = new Vector2(-dir.y, dir.x) * 0.4f;
        Vector3 tip = pos + (Vector3)(dir * 2f);
        Gizmos.DrawLine(tip, tip - (Vector3)(dir * 0.5f) + (Vector3)perp);
        Gizmos.DrawLine(tip, tip - (Vector3)(dir * 0.5f) - (Vector3)perp);
    }
}
