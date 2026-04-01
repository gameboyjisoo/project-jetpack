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
    [SerializeField] private float airMult = 0.65f;

    [Header("Jump — Celeste")]
    [SerializeField] private float jumpForce = 18f;
    [SerializeField] private float varJumpTime = 0.2f;
    [SerializeField] private float jumpHBoost = 2.5f;
    [SerializeField] private float coyoteTime = 0.1f;
    [SerializeField] private float jumpBufferTime = 0.1f;

    [Header("Jetpack — Booster 2.0")]
    [SerializeField] private float boostSpeed = 19f;
    [SerializeField] private float gasConsumptionRate = 100f;
    [SerializeField] private float wallNudgeSpeed = 2f;

    [Header("Gravity")]
    [SerializeField] private float fallGravityMultiplier = 2.0f;
    [SerializeField] private float apexGravityMultiplier = 0.5f;
    [SerializeField] private float apexThreshold = 4f;
    [SerializeField] private float maxFallSpeed = 30f;

    [Header("Ground Check")]
    [SerializeField] private Vector2 groundCheckSize = new Vector2(0.8f, 0.05f);
    [SerializeField] private Vector2 groundCheckOffset = new Vector2(0f, -0.5f);
    [SerializeField] private LayerMask groundLayer;

    [Header("Wall Check")]
    [SerializeField] private float wallCheckDistance = 0.6f;

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
    private bool prevJetpackHeld;
    private bool jetpackJustPressed;
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
    private bool didJump;
    private float varJumpTimer;
    private float varJumpSpeed;
    private bool facingRight = true;
    private Vector2 jetpackDirection = Vector2.up;
    private int boostMode; // 0=off, 1=horizontal, 2=up, 3=down

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
        {
            prevJetpackHeld = jetpackHeld;
            jetpackHeld = jetpackAction.IsPressed();
            if (jetpackHeld && !prevJetpackHeld)
                jetpackJustPressed = true;
        }

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
        ApplyHorizontalMovement();
        ApplyGravityModifiers();
        ApplyVarJump();
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
            boostMode = 0;
            didJump = false;
            varJumpTimer = 0f;
        }

        if (wasGrounded && !isGrounded && rb.linearVelocity.y <= 0)
            coyoteTimer = coyoteTime;
    }

    private void UpdateTimers()
    {
        coyoteTimer -= Time.fixedDeltaTime;
        varJumpTimer -= Time.fixedDeltaTime;

        if (jumpRequested)
        {
            jumpBufferTimer = jumpBufferTime;
            jumpRequested = false;
        }
        jumpBufferTimer -= Time.fixedDeltaTime;
    }

    private void HandleJump()
    {
        // Celeste variable jump: releasing jump stops maintaining upward speed
        // (gravity takes over naturally — no instant velocity cut)
        if (jumpReleased)
        {
            if (varJumpTimer > 0f)
                varJumpTimer = 0f;
            jumpReleased = false;
        }

        bool canJump = isGrounded || coyoteTimer > 0f;
        if (jumpBufferTimer > 0f && canJump && !isJetpacking)
        {
            // Celeste: horizontal boost in input direction on jump
            float newVx = rb.linearVelocity.x + moveInput.x * jumpHBoost;
            rb.linearVelocity = new Vector2(newVx, jumpForce);

            jumpBufferTimer = 0f;
            coyoteTimer = 0f;
            didJump = true;
            varJumpTimer = varJumpTime;
            varJumpSpeed = jumpForce;
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
        // Consume edge-detected press
        bool justPressed = jetpackJustPressed;
        jetpackJustPressed = false;

        // Booster 2.0 activation: first press while airborne with gas
        if (justPressed && !isGrounded && !isJetpacking
            && jetpackGas != null && jetpackGas.HasGas)
        {
            ActivateBoost();
        }

        if (!isJetpacking) return;

        // End: released button
        if (!jetpackHeld)
        {
            EndBoost();
            return;
        }

        // Consume gas
        jetpackGas.ConsumeGas(gasConsumptionRate * Time.fixedDeltaTime);

        // Gas empty
        if (!jetpackGas.HasGas)
        {
            EndBoost();
            return;
        }

        // Booster 2.0: wall nudge during horizontal boost (climb walls)
        if (boostMode == 1 && IsTouchingWall())
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, wallNudgeSpeed);
    }

    private void ActivateBoost()
    {
        isJetpacking = true;
        didJump = false;
        varJumpTimer = 0f;

        // Booster 2.0: instant max velocity in chosen direction, zero perpendicular axis
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
        // Booster 2.0: mode-specific velocity halving
        Vector2 vel = rb.linearVelocity;
        if (boostMode == 1)       // horizontal: halve X only
            vel.x *= 0.5f;
        else if (boostMode == 2)  // upward: halve Y only
            vel.y *= 0.5f;
        // boostMode 3 (down): no halving — matches Cave Story

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

    private void ApplyHorizontalMovement()
    {
        if (isJetpacking) return;

        float targetSpeed = moveInput.x * moveSpeed;
        float currentSpeed = rb.linearVelocity.x;

        // Celeste: single air control multiplier (0.65)
        float mult = isGrounded ? 1f : airMult;
        float rate = Mathf.Abs(moveInput.x) > 0.01f ? groundAcceleration : groundDeceleration;
        rate *= mult;

        // MoveTowards for crisp, predictable movement instead of force-based mush
        float newSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, rate * Time.fixedDeltaTime);
        rb.linearVelocity = new Vector2(newSpeed, rb.linearVelocity.y);

        if (moveInput.x > 0.01f && !facingRight) Flip();
        else if (moveInput.x < -0.01f && facingRight) Flip();
    }

    private void ApplyGravityModifiers()
    {
        if (isJetpacking)
        {
            // Booster 2.0: upward thrust cancels gravity; horizontal/downward keep gravity active
            rb.gravityScale = (boostMode == 2) ? 0f : 1f;
            return;
        }

        float vy = rb.linearVelocity.y;

        if (vy < 0)
        {
            // Fast fall
            rb.gravityScale = fallGravityMultiplier;
        }
        else if (didJump && jumpHeld && Mathf.Abs(vy) < apexThreshold)
        {
            // Celeste apex: half gravity when near peak AND holding jump
            rb.gravityScale = apexGravityMultiplier;
        }
        else
        {
            rb.gravityScale = 1f;
        }
    }

    // Celeste variable jump: maintain upward velocity while holding jump within the window.
    // After the window expires or jump is released, gravity decelerates naturally.
    private void ApplyVarJump()
    {
        if (varJumpTimer > 0f && jumpHeld && rb.linearVelocity.y >= 0f)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x,
                Mathf.Max(rb.linearVelocity.y, varJumpSpeed));
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
