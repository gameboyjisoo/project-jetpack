using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerJetpack : MonoBehaviour
{
    [Header("Jetpack — Booster 2.0")]
    [SerializeField] private float boostSpeed = 11f;
    [SerializeField] private float gasConsumptionRate = 100f;
    [SerializeField] private float wallNudgeSpeed = 2f;

    [Header("Wall Check")]
    [SerializeField] private float wallCheckDistance = 0.6f;
    [SerializeField] private LayerMask groundLayer;

    private Rigidbody2D rb;
    private JetpackGas jetpackGas;

    private bool isJetpacking;
    private int boostMode; // 0=off, 1=horizontal, 2=up, 3=down
    private Vector2 jetpackDirection = Vector2.up;
    private bool wallNudgeUsed;

    public bool IsJetpacking => isJetpacking;
    public int BoostMode => boostMode;

    public void Init(Rigidbody2D rigidbody, JetpackGas gas, LayerMask ground)
    {
        rb = rigidbody;
        jetpackGas = gas;
        groundLayer = ground;
    }

    public void UpdateDirection(Vector2 moveInput, Vector2 prevMoveInput)
    {
        bool rightNew = moveInput.x > 0.5f && prevMoveInput.x <= 0.5f;
        bool leftNew = moveInput.x < -0.5f && prevMoveInput.x >= -0.5f;
        bool upNew = moveInput.y > 0.5f && prevMoveInput.y <= 0.5f;
        bool downNew = moveInput.y < -0.5f && prevMoveInput.y >= -0.5f;

        if (rightNew) jetpackDirection = Vector2.right;
        else if (leftNew) jetpackDirection = Vector2.left;
        else if (upNew) jetpackDirection = Vector2.up;
        else if (downNew) jetpackDirection = Vector2.down;
    }

    public void OnLand()
    {
        isJetpacking = false;
        boostMode = 0;
        wallNudgeUsed = false;
    }

    /// <summary>
    /// Returns true if jetpack state changed (for clearing jump state).
    /// </summary>
    public bool Tick(bool justPressed, bool held, bool isGrounded)
    {
        bool activated = false;

        if (justPressed && !isGrounded && !isJetpacking
            && jetpackGas != null && jetpackGas.HasGas)
        {
            ActivateBoost();
            wallNudgeUsed = false;
            activated = true;
        }

        if (!isJetpacking) return activated;

        if (!held)
        {
            EndBoost();
            return activated;
        }

        jetpackGas.ConsumeGas(gasConsumptionRate * Time.fixedDeltaTime);

        if (!jetpackGas.HasGas)
        {
            EndBoost();
            return activated;
        }

        // Wall nudge: one-time upward bump when hitting a wall during horizontal boost
        if (boostMode == 1 && !wallNudgeUsed && IsTouchingWall())
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, wallNudgeSpeed);
            wallNudgeUsed = true;
        }

        return activated;
    }

    private void ActivateBoost()
    {
        isJetpacking = true;

        if (jetpackDirection == Vector2.up)
        {
            boostMode = 2;
            rb.linearVelocity = new Vector2(0f, boostSpeed);
        }
        else if (jetpackDirection == Vector2.down)
        {
            boostMode = 3;
            rb.linearVelocity = new Vector2(0f, -boostSpeed);
        }
        else
        {
            boostMode = 1;
            rb.linearVelocity = new Vector2(jetpackDirection.x * boostSpeed, 0f);
        }
    }

    private void EndBoost()
    {
        Vector2 vel = rb.linearVelocity;
        if (boostMode == 1)
            vel.x *= 0.5f;
        else if (boostMode == 2)
            vel.y *= 0.5f;

        GameEventBus.Publish(new PlayerJetpackDeactivated { BoostMode = boostMode });
        rb.linearVelocity = vel;
        isJetpacking = false;
        boostMode = 0;
    }

    private bool IsTouchingWall()
    {
        if (Mathf.Abs(jetpackDirection.x) < 0.5f) return false;
        Vector2 origin = (Vector2)transform.position;
        RaycastHit2D hit = Physics2D.Raycast(origin, new Vector2(jetpackDirection.x, 0f),
            wallCheckDistance, groundLayer);
        return hit.collider != null;
    }
}
