using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(PlayerMovement))]
[RequireComponent(typeof(PlayerJump))]
[RequireComponent(typeof(PlayerJetpack))]
[RequireComponent(typeof(PlayerGravity))]
public class PlayerController : MonoBehaviour
{
    [Header("Ground Check")]
    [SerializeField] private Vector2 groundCheckSize = new Vector2(0.5f, 0.05f);
    [SerializeField] private Vector2 groundCheckOffset = new Vector2(0f, -0.5f);
    [SerializeField] private LayerMask groundLayer;

    [Header("Corner Correction")]
    [SerializeField] private float cornerCorrectionMax = 0.4f;
    [SerializeField] private int cornerCorrectionSteps = 4;

    [Header("Input")]
    [SerializeField] private InputActionAsset inputActions;

    // Components
    private Rigidbody2D rb;
    private BoxCollider2D col;
    private PlayerMovement movement;
    private PlayerJump jump;
    private PlayerJetpack jetpack;
    private PlayerGravity gravity;
    private SecondaryBooster secondaryBooster;
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

    // Ground state
    private bool isGrounded;
    private bool wasGrounded;
    private float cornerCorrectionCooldown;

    // Exposed for Inspector debugging — watch this during play mode
    public bool IsGrounded => isGrounded;
    [Header("Debug (read-only)")]
    [SerializeField] private bool debugIsGrounded;
    public bool IsJetpacking => jetpack.IsJetpacking;
    public bool FacingRight => movement.FacingRight;
    public Vector2 Velocity => rb.linearVelocity;
    public PlayerGravity Gravity => gravity;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<BoxCollider2D>();

        // Zero-friction material so the player slides off walls instead of sticking
        var frictionlessMat = new PhysicsMaterial2D("PlayerNoFriction") { friction = 0f, bounciness = 0f };
        col.sharedMaterial = frictionlessMat;
        rb.sharedMaterial = frictionlessMat;
        movement = GetComponent<PlayerMovement>();
        jump = GetComponent<PlayerJump>();
        jetpack = GetComponent<PlayerJetpack>();
        gravity = GetComponent<PlayerGravity>();
        secondaryBooster = GetComponent<SecondaryBooster>();
        jetpackGas = GetComponent<JetpackGas>();

        rb.gravityScale = 1f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        if (groundLayer.value == 0)
            groundLayer = 1 << 8;

        // Init components with shared references
        movement.Init(rb);
        jump.Init(rb);
        jetpack.Init(rb, jetpackGas, groundLayer);
        gravity.Init(rb);

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

    private void Update()
    {
        if (moveAction != null)
        {
            prevMoveInput = moveInput;
            moveInput = moveAction.ReadValue<Vector2>();
            jetpack.UpdateDirection(moveInput, prevMoveInput);
        }
        if (jetpackAction != null)
        {
            prevJetpackHeld = jetpackHeld;
            jetpackHeld = jetpackAction.IsPressed();
            if (jetpackHeld && !prevJetpackHeld)
                jetpackJustPressed = true;
        }

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

    private void FixedUpdate()
    {
        CheckGround();

        if (rb.linearVelocity.y > 0f)
            CornerCorrect();

        jump.UpdateTimers(jumpRequested);
        jumpRequested = false;

        jump.HandleJumpRelease(jumpReleased);
        jumpReleased = false;

        if (jump.TryJump(moveInput, isGrounded, jetpack.IsJetpacking))
        {
            // Jump clears jetpack state
        }

        bool justPressed = jetpackJustPressed;
        jetpackJustPressed = false;
        bool jetpackActivated = jetpack.Tick(justPressed, jetpackHeld, isGrounded);
        if (jetpackActivated)
            jump.OnLand(); // Clear jump state when jetpack activates

        bool isSecondaryBoosting = secondaryBooster != null && secondaryBooster.IsBoosting;
        movement.Tick(moveInput, isGrounded, jetpack.IsJetpacking, isSecondaryBoosting);

        gravity.Tick(jetpack.IsJetpacking, jump.DidJump, jumpHeld);
        jump.ApplyVarJump(jumpHeld);
        gravity.ClampFallSpeed();
    }

    private void CheckGround()
    {
        wasGrounded = isGrounded;

        // Hardened ground check: always enforce layer 8 if mask is invalid.
        // Prevents editor Inspector from overwriting the runtime value when Player is selected.
        if (groundLayer.value == 0)
            groundLayer = 1 << 8;

        // Use rb.position (physics authoritative) instead of transform.position (interpolated)
        // Two downward raycasts from feet edges — immune to wall false positives
        Vector2 feetCenter = rb.position + groundCheckOffset;
        float halfWidth = groundCheckSize.x * 0.5f;
        float rayLength = groundCheckSize.y;

        RaycastHit2D hitLeft = Physics2D.Raycast(
            new Vector2(feetCenter.x - halfWidth, feetCenter.y), Vector2.down, rayLength, groundLayer);
        RaycastHit2D hitRight = Physics2D.Raycast(
            new Vector2(feetCenter.x + halfWidth, feetCenter.y), Vector2.down, rayLength, groundLayer);

        isGrounded = (hitLeft.collider != null && hitLeft.normal.y > 0.7f)
                  || (hitRight.collider != null && hitRight.normal.y > 0.7f);
        debugIsGrounded = isGrounded;

        if (isGrounded && !wasGrounded)
        {
            jetpackGas?.Recharge();
            jetpack.OnLand();
            jump.OnLand();
            gravity.ResetSuppression();
            cornerCorrectionCooldown = 0f;
        }

        if (wasGrounded && !isGrounded && rb.linearVelocity.y <= 0)
            jump.StartCoyoteTime();
    }

    /// <summary>
    /// Celeste-style corner correction: when rising and the head clips a platform
    /// corner, nudge the player horizontally to clear it. Only triggers when one
    /// side of the head is blocked and the other is open (a genuine corner clip).
    /// Fires once per cooldown to prevent repeated nudging.
    /// </summary>
    private void CornerCorrect()
    {
        cornerCorrectionCooldown -= Time.fixedDeltaTime;
        if (cornerCorrectionCooldown > 0f) return;

        Vector3 pos = transform.position;
        Vector2 colOffset = col.offset;
        Vector2 colSize = col.size;
        float scaleX = Mathf.Abs(transform.localScale.x);
        float scaleY = Mathf.Abs(transform.localScale.y);
        float halfW = colSize.x * 0.5f * scaleX;
        float topY = pos.y + colOffset.y * scaleY + colSize.y * 0.5f * scaleY;
        float checkDist = Mathf.Abs(rb.linearVelocity.y) * Time.fixedDeltaTime + 0.05f;

        // Cast from left edge and right edge of the collider, upward
        Vector2 leftEdge = new Vector2(pos.x - halfW, topY);
        Vector2 rightEdge = new Vector2(pos.x + halfW, topY);

        bool hitLeft = Physics2D.Raycast(leftEdge, Vector2.up, checkDist, groundLayer);
        bool hitRight = Physics2D.Raycast(rightEdge, Vector2.up, checkDist, groundLayer);

        // Only correct if exactly ONE side is blocked (genuine corner clip)
        // If both sides are blocked it's a ceiling, not a corner — let the bonk happen
        if (hitLeft == hitRight) return;

        // Find the smallest nudge that clears the corner
        float stepSize = cornerCorrectionMax / cornerCorrectionSteps;
        float nudgeDir = hitLeft ? 1f : -1f; // Nudge away from the blocked side

        for (int i = 1; i <= cornerCorrectionSteps; i++)
        {
            float offset = stepSize * i * nudgeDir;
            Vector2 nudgedPos = new Vector2(pos.x + offset, pos.y);

            // Check head is clear at nudged position
            Vector2 nudgedHead = new Vector2(pos.x + offset, topY);
            if (Physics2D.Raycast(nudgedHead, Vector2.up, checkDist, groundLayer))
                continue; // Still blocked here

            // Check body won't overlap walls at nudged position
            if (Physics2D.OverlapBox(nudgedPos + colOffset, colSize * 0.9f, 0f, groundLayer))
                continue; // Body would be inside geometry

            transform.position = new Vector3(pos.x + offset, pos.y, pos.z);
            cornerCorrectionCooldown = 0.15f; // Don't correct again for 0.15s
            return;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Vector2 checkPos = (Vector2)transform.position + groundCheckOffset;
        Gizmos.DrawWireCube(checkPos, groundCheckSize);
    }
}
