using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerJetpack : MonoBehaviour
{
    [Header("Jetpack — Booster 2.0")]
    [SerializeField] private float boostSpeed = 11f;
    [SerializeField] private float gasConsumptionRate = 100f;

    [Header("Ground Layer")]
    [SerializeField] private LayerMask groundLayer;

    private Rigidbody2D rb;
    private JetpackGas jetpackGas;

    private bool isJetpacking;
    private int boostMode; // 0=off, 1=horizontal, 2=up, 3=down
    private Vector2 jetpackDirection = Vector2.up;

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
    }

    /// <summary>
    /// Returns true if jetpack state changed (for clearing jump state).
    /// </summary>
    public bool Tick(bool justPressed, bool held, bool isGrounded, bool isDashing = false)
    {
        bool activated = false;

        if (justPressed && !isGrounded && !isJetpacking && !isDashing
            && jetpackGas != null && jetpackGas.HasGas)
        {
            ActivateBoost();
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
}
