using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerJump : MonoBehaviour
{
    [Header("Jump — Celeste")]
    [SerializeField] private float jumpForce = 8f;
    [SerializeField] private float varJumpTime = 0.2f;
    [SerializeField] private float jumpHBoost = 2.4f;
    [SerializeField] private float coyoteTime = 0.1f;
    [SerializeField] private float jumpBufferTime = 0.1f;

    private Rigidbody2D rb;

    private float coyoteTimer;
    private float jumpBufferTimer;
    private float varJumpTimer;
    private float varJumpSpeed;
    private bool didJump;

    public bool DidJump => didJump;

    public void Init(Rigidbody2D rigidbody) => rb = rigidbody;

    public void StartCoyoteTime()
    {
        coyoteTimer = coyoteTime;
    }

    public void OnLand()
    {
        didJump = false;
        varJumpTimer = 0f;
    }

    public void UpdateTimers(bool jumpRequested)
    {
        coyoteTimer -= Time.fixedDeltaTime;
        varJumpTimer -= Time.fixedDeltaTime;

        if (jumpRequested)
            jumpBufferTimer = jumpBufferTime;
        jumpBufferTimer -= Time.fixedDeltaTime;
    }

    public void HandleJumpRelease(bool jumpReleased)
    {
        if (jumpReleased && varJumpTimer > 0f)
            varJumpTimer = 0f;
    }

    public bool TryJump(Vector2 moveInput, bool isGrounded, bool isJetpacking)
    {
        bool canJump = isGrounded || coyoteTimer > 0f;
        if (jumpBufferTimer > 0f && canJump && !isJetpacking)
        {
            float newVx = rb.linearVelocity.x + moveInput.x * jumpHBoost;
            rb.linearVelocity = new Vector2(newVx, jumpForce);

            jumpBufferTimer = 0f;
            coyoteTimer = 0f;
            didJump = true;
            varJumpTimer = varJumpTime;
            varJumpSpeed = jumpForce;
            return true;
        }
        return false;
    }

    public void ApplyVarJump(bool jumpHeld)
    {
        if (varJumpTimer > 0f && jumpHeld && rb.linearVelocity.y >= 0f)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x,
                Mathf.Max(rb.linearVelocity.y, varJumpSpeed));
        }
    }
}
