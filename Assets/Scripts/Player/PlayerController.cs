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
    [SerializeField] private Vector2 groundCheckSize = new Vector2(0.8f, 0.05f);
    [SerializeField] private Vector2 groundCheckOffset = new Vector2(0f, -0.5f);
    [SerializeField] private LayerMask groundLayer;

    [Header("Input")]
    [SerializeField] private InputActionAsset inputActions;

    // Components
    private Rigidbody2D rb;
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

    public bool IsGrounded => isGrounded;
    public bool IsJetpacking => jetpack.IsJetpacking;
    public bool FacingRight => movement.FacingRight;
    public Vector2 Velocity => rb.linearVelocity;
    public PlayerGravity Gravity => gravity;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
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
        Vector2 checkPos = (Vector2)transform.position + groundCheckOffset;
        isGrounded = Physics2D.OverlapBox(checkPos, groundCheckSize, 0f, groundLayer);

        if (isGrounded && !wasGrounded)
        {
            jetpackGas?.Recharge();
            jetpack.OnLand();
            jump.OnLand();
            gravity.ResetSuppression();
        }

        if (wasGrounded && !isGrounded && rb.linearVelocity.y <= 0)
            jump.StartCoyoteTime();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Vector2 checkPos = (Vector2)transform.position + groundCheckOffset;
        Gizmos.DrawWireCube(checkPos, groundCheckSize);
    }
}
