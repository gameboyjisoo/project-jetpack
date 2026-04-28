using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerGravity : MonoBehaviour
{
    [Header("Gravity")]
    [SerializeField] private float fallGravityMultiplier = 2.0f;
    [SerializeField] private float apexGravityMultiplier = 0.5f;
    [SerializeField] private float apexThreshold = 2.5f;
    [SerializeField] private float maxFallSpeed = 20f;

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

        // upSpeed: positive = rising, negative = falling (same sign as old vy)
        float upSpeed = GravityState.GetUpSpeed(rb.linearVelocity);

        if (upSpeed < 0)
        {
            rb.gravityScale = fallGravityMultiplier;
        }
        else if (didJump && jumpHeld && Mathf.Abs(upSpeed) < apexThreshold)
        {
            rb.gravityScale = apexGravityMultiplier;
        }
        else if (!didJump && upSpeed > 0)
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
        float upSpeed = GravityState.GetUpSpeed(rb.linearVelocity);
        if (upSpeed < -maxFallSpeed)
        {
            // Decompose, clamp fall component, recompose
            float moveSpeed = GravityState.GetMoveSpeed(rb.linearVelocity);
            rb.linearVelocity = GravityState.ComposeVelocity(moveSpeed, -maxFallSpeed);
        }
    }
}
