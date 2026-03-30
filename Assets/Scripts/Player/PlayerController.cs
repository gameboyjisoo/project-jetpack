using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Ground Movement")]
    [SerializeField] private float moveSpeed = 10f;
    [SerializeField] private float groundAcceleration = 120f;
    [SerializeField] private float groundDeceleration = 120f;

    [Header("Jump")]
    [SerializeField] private float jumpForce = 18f;
    [SerializeField] private float jumpCutMultiplier = 0.3f;
    [SerializeField] private float coyoteTime = 0.08f;
    [SerializeField] private float jumpBufferTime = 0.1f;

    [Header("Jetpack")]
    [SerializeField] private float jetpackThrust = 11f;
    [SerializeField] private float gasConsumptionRate = 100f;

    [Header("Gravity")]
    [SerializeField] private float fallGravityMultiplier = 2.0f;
    [SerializeField] private float apexGravityMultiplier = 0.4f;
    [SerializeField] private float apexThreshold = 3f;
    [SerializeField] private float maxFallSpeed = 30f;

    [Header("Ground Check")]
    [SerializeField] private Vector2 groundCheckSize = new Vector2(0.8f, 0.05f);
    [SerializeField] private Vector2 groundCheckOffset = new Vector2(0f, -0.5f);
    [SerializeField] private LayerMask groundLayer;

    [Header("Input")]
    [SerializeField] private InputActionAsset inputActions;

    // Components
    private Rigidbody2D rb;
    private JetpackGas jetpackGas;

    // Input actions
    private InputAction moveAction;
    private InputAction jumpAction;
    private InputAction jetpackAction;

    // Input state (read in Update, consumed in FixedUpdate)
    private Vector2 moveInput;
    private Vector2 prevMoveInput;
    private bool jetpackHeld;
    private bool jumpHeld;
    private bool prevJumpHeld;
    private bool jumpRequested;
    private bool jumpReleased;

    // Movement state
    private bool isGrounded;
    private bool wasGrounded;
    private float coyoteTimer;
    private float jumpBufferTimer;
    private bool isJetpacking;
    private bool wasJetpacking;
    private bool didJump; // tracks if we're in a jump arc (for apex gravity)
    private bool facingRight = true;
    private Vector2 jetpackDirection = Vector2.up;

    public bool IsGrounded => isGrounded;
    public bool IsJetpacking => isJetpacking;
    public bool FacingRight => facingRight;
    public Vector2 Velocity => rb.linearVelocity;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        jetpackGas = GetComponent<JetpackGas>();

        rb.gravityScale = 1f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        if (groundLayer.value == 0)
            groundLayer = 1 << 8;

        if (inputActions == null)
            inputActions = Resources.Load<InputActionAsset>("PlayerInput");

        if (inputActions != null)
        {
            var gameplay = inputActions.FindActionMap("Gameplay");
            moveAction = gameplay.FindAction("Move");
            jumpAction = gameplay.FindAction("Jump");
            jetpackAction = gameplay.FindAction("Jetpack");
        }
    }

    private void OnEnable()
    {
        inputActions?.Enable();
    }

    private void OnDisable()
    {
        inputActions?.Disable();
    }

    // Update only reads input — no physics here
    private void Update()
    {
        if (moveAction != null)
        {
            prevMoveInput = moveInput;
            moveInput = moveAction.ReadValue<Vector2>();
            UpdateJetpackDirection();
        }
        if (jetpackAction != null)
            jetpackHeld = jetpackAction.IsPressed();

        // Manual edge detection for jump — more reliable than WasPressedThisFrame
        if (jumpAction != null)
        {
            prevJumpHeld = jumpHeld;
            jumpHeld = jumpAction.IsPressed();

            if (jumpHeld && !prevJumpHeld)
                jumpRequested = true;
            if (!jumpHeld && prevJumpHeld)
                jumpReleased = true;
        }
    }

    // All physics happens here in a clear order
    private void FixedUpdate()
    {
        CheckGround();
        UpdateTimers();

        wasJetpacking = isJetpacking;

        HandleJump();
        ApplyJetpack();
        HandleJetpackEnd();
        ApplyHorizontalMovement();
        ApplyGravityModifiers();
        ClampFallSpeed();
    }

    private void CheckGround()
    {
        wasGrounded = isGrounded;
        Vector2 checkPos = (Vector2)transform.position + groundCheckOffset;
        isGrounded = Physics2D.OverlapBox(checkPos, groundCheckSize, 0f, groundLayer);

        if (isGrounded && !wasGrounded)
        {
            jetpackGas?.Recharge();
            isJetpacking = false;
            didJump = false;
        }

        if (wasGrounded && !isGrounded && rb.linearVelocity.y <= 0)
            coyoteTimer = coyoteTime;
    }

    private void UpdateTimers()
    {
        coyoteTimer -= Time.fixedDeltaTime;

        if (jumpRequested)
        {
            jumpBufferTimer = jumpBufferTime;
            jumpRequested = false;
        }
        jumpBufferTimer -= Time.fixedDeltaTime;
    }

    private void HandleJump()
    {
        // Variable jump height: cut upward velocity on release
        if (jumpReleased)
        {
            if (rb.linearVelocity.y > 0 && !isJetpacking)
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * jumpCutMultiplier);
            jumpReleased = false;
        }

        bool canJump = isGrounded || coyoteTimer > 0f;
        if (jumpBufferTimer > 0f && canJump)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            jumpBufferTimer = 0f;
            coyoteTimer = 0f;
            didJump = true;
        }
    }

    private void UpdateJetpackDirection()
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

    private void ApplyJetpack()
    {
        isJetpacking = false;

        if (!jetpackHeld || isGrounded || jetpackGas == null)
            return;

        if (jetpackGas.CurrentGas <= 0f)
            return;

        rb.gravityScale = 0f;
        rb.linearVelocity = jetpackDirection * jetpackThrust;

        jetpackGas.ConsumeGas(gasConsumptionRate * Time.fixedDeltaTime);
        isJetpacking = true;

        // Gas just ran out this frame — same drift rules as releasing the button
        if (jetpackGas.CurrentGas <= 0f)
        {
            Vector2 vel = rb.linearVelocity;
            vel.x *= 0.5f;
            vel.y = vel.y > 0 ? vel.y * 0.5f : 0f;
            rb.linearVelocity = vel;
            isJetpacking = false;
            didJump = false;
        }
    }

    private void HandleJetpackEnd()
    {
        if (!wasJetpacking || isJetpacking || isGrounded)
            return;

        // Booster 2.0 style: halve horizontal velocity for drift,
        // but for vertical, only halve upward — let downward fall naturally
        Vector2 vel = rb.linearVelocity;
        vel.x *= 0.5f;
        vel.y = vel.y > 0 ? vel.y * 0.5f : 0f;
        rb.linearVelocity = vel;

        didJump = false;
    }

    private void ApplyHorizontalMovement()
    {
        if (isJetpacking) return;

        float targetSpeed = moveInput.x * moveSpeed;
        float currentSpeed = rb.linearVelocity.x;
        float speedDiff = targetSpeed - currentSpeed;

        // Use higher rate when accelerating vs decelerating, less control in air
        float accel, decel;
        if (isGrounded)
        {
            accel = groundAcceleration;
            decel = groundDeceleration;
        }
        else
        {
            accel = groundAcceleration * 0.8f;
            decel = groundDeceleration * 0.5f;
        }

        float rate = Mathf.Abs(targetSpeed) > 0.01f ? accel : decel;

        // MoveTowards for crisp, predictable movement instead of force-based mush
        float newSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, rate * Time.fixedDeltaTime);
        rb.linearVelocity = new Vector2(newSpeed, rb.linearVelocity.y);

        if (moveInput.x > 0.01f && !facingRight) Flip();
        else if (moveInput.x < -0.01f && facingRight) Flip();
    }

    private void ApplyGravityModifiers()
    {
        if (isJetpacking) return;

        float vy = rb.linearVelocity.y;

        if (vy < 0)
        {
            // Fast fall
            rb.gravityScale = fallGravityMultiplier;
        }
        else if (didJump && Mathf.Abs(vy) < apexThreshold)
        {
            // Apex hang — only during jumps, not after jetpack
            rb.gravityScale = apexGravityMultiplier;
        }
        else
        {
            rb.gravityScale = 1f;
        }
    }

    private void ClampFallSpeed()
    {
        if (rb.linearVelocity.y < -maxFallSpeed)
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, -maxFallSpeed);
    }

    private void Flip()
    {
        facingRight = !facingRight;
        Vector3 scale = transform.localScale;
        scale.x *= -1;
        transform.localScale = scale;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Vector2 checkPos = (Vector2)transform.position + groundCheckOffset;
        Gizmos.DrawWireCube(checkPos, groundCheckSize);
    }
}
