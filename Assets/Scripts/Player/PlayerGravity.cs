using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerGravity : MonoBehaviour
{
    [Header("Gravity")]
    [SerializeField] private float fallGravityMultiplier = 2.0f;
    [SerializeField] private float apexGravityMultiplier = 0.5f;
    [SerializeField] private float apexThreshold = 4f;
    [SerializeField] private float maxFallSpeed = 30f;

    private Rigidbody2D rb;
    private float suppressTimer;

    public void Init(Rigidbody2D rigidbody) => rb = rigidbody;

    public void SuppressGravity(float duration)
    {
        suppressTimer = duration;
    }

    public void ResetSuppression()
    {
        suppressTimer = 0f;
    }

    public void Tick(bool isJetpacking, bool didJump, bool jumpHeld)
    {
        suppressTimer -= Time.fixedDeltaTime;

        if (isJetpacking || suppressTimer > 0f)
        {
            rb.gravityScale = 0f;
            return;
        }

        float vy = rb.linearVelocity.y;

        if (vy < 0)
        {
            rb.gravityScale = fallGravityMultiplier;
        }
        else if (didJump && jumpHeld && Mathf.Abs(vy) < apexThreshold)
        {
            rb.gravityScale = apexGravityMultiplier;
        }
        else if (!didJump && vy > 0)
        {
            rb.gravityScale = fallGravityMultiplier;
        }
        else
        {
            rb.gravityScale = 1f;
        }
    }

    public void ClampFallSpeed()
    {
        if (rb.linearVelocity.y < -maxFallSpeed)
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, -maxFallSpeed);
    }
}
