using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Secondary Booster: short fixed-distance dash in 8 directions.
/// Limited ammo, recharges on ground. Faster than jetpack but brief.
/// </summary>
[RequireComponent(typeof(PlayerController))]
public class SecondaryBooster : MonoBehaviour
{
    [Header("Booster")]
    [SerializeField] private int maxAmmo = 1;
    [SerializeField] private float boostSpeed = 32f;
    [SerializeField] private float boostDuration = 0.15f;
    [SerializeField] private float cooldown = 0.15f;
    [SerializeField] private float freezeFrameDuration = 0.05f;
    [SerializeField] private float endDecayTime = 0.06f;
    [SerializeField] [Range(0f, 0.5f)] private float momentumRetain = 0.25f;
    [SerializeField] private float dashBufferTime = 0.1f;

    [Header("Projectile (optional)")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private float projectileSpeed = 20f;
    [SerializeField] private float projectileLifetime = 0.5f;

    [Header("Input")]
    [SerializeField] private InputActionAsset inputActions;

    private int currentAmmo;
    private float cooldownTimer;
    private float boostTimer;
    private Vector2 boostVelocity;
    private Rigidbody2D rb;
    private PlayerController player;
    private PlayerGravity gravity;
    private Vector2 aimDirection = Vector2.right;

    private InputAction moveAction;
    private InputAction fireAction;
    private bool fireHeld;
    private bool prevFireHeld;
    private bool freezeActive;
    private float freezeTimer;
    private float decayTimer;
    private Vector2 decayStartVelocity;
    private float dashBufferTimer;

    public int CurrentAmmo => currentAmmo;
    public int MaxAmmo => maxAmmo;
    public bool IsBoosting => boostTimer > 0f || decayTimer > 0f;
    public event System.Action<int> OnAmmoChanged;

    /// <summary>
    /// Recharge dash ammo from external source (mid-air pickup, gimmick, etc.).
    /// </summary>
    public void Recharge(int amount = 1)
    {
        currentAmmo = Mathf.Min(currentAmmo + amount, maxAmmo);
        OnAmmoChanged?.Invoke(currentAmmo);
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        player = GetComponent<PlayerController>();
        gravity = GetComponent<PlayerGravity>();
        currentAmmo = maxAmmo;

        if (inputActions == null)
            inputActions = Resources.Load<InputActionAsset>("PlayerInput");

        if (inputActions != null)
        {
            var gameplay = inputActions.FindActionMap("Gameplay");
            moveAction = gameplay.FindAction("Move");
            fireAction = gameplay.FindAction("Fire");
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
        // Handle freeze frame (uses unscaled time so it works while timeScale=0)
        if (freezeActive)
        {
            freezeTimer -= Time.unscaledDeltaTime;
            if (freezeTimer <= 0f)
            {
                Time.timeScale = 1f;
                freezeActive = false;
            }
            return; // Skip all input during freeze
        }

        cooldownTimer -= Time.deltaTime;

        // Recharge ammo on ground
        if (player.IsGrounded && currentAmmo < maxAmmo)
        {
            currentAmmo = maxAmmo;
            OnAmmoChanged?.Invoke(currentAmmo);
        }

        // Read aim direction from move input (8 directions); default to facing direction when idle
        if (moveAction != null)
        {
            Vector2 input = moveAction.ReadValue<Vector2>();
            if (input.sqrMagnitude > 0.01f)
            {
                float x = Mathf.Abs(input.x) > 0.3f ? Mathf.Sign(input.x) : 0f;
                float y = Mathf.Abs(input.y) > 0.3f ? Mathf.Sign(input.y) : 0f;
                if (x != 0f || y != 0f)
                    aimDirection = new Vector2(x, y).normalized;
            }
            else
            {
                aimDirection = player.FacingRight ? Vector2.right : Vector2.left;
            }
        }

        // Manual edge detection for fire with input buffer
        if (fireAction != null)
        {
            prevFireHeld = fireHeld;
            fireHeld = fireAction.IsPressed();

            if (fireHeld && !prevFireHeld)
                dashBufferTimer = dashBufferTime;

            dashBufferTimer -= Time.deltaTime;

            if (dashBufferTimer > 0f && cooldownTimer <= 0f && currentAmmo > 0 && !IsBoosting)
            {
                dashBufferTimer = 0f;
                Fire();
            }
        }
    }

    private void FixedUpdate()
    {
        if (boostTimer > 0f)
        {
            boostTimer -= Time.fixedDeltaTime;
            rb.linearVelocity = boostVelocity;
            gravity.SuppressGravity(Time.fixedDeltaTime * 2f);

            if (boostTimer <= 0f)
            {
                // Start decay instead of hard stop
                decayTimer = endDecayTime;
                decayStartVelocity = boostVelocity;
            }
        }
        else if (decayTimer > 0f)
        {
            decayTimer -= Time.fixedDeltaTime;
            float t = Mathf.Clamp01(decayTimer / endDecayTime);
            rb.linearVelocity = decayStartVelocity * (t * t); // Quadratic ease-out
            gravity.SuppressGravity(Time.fixedDeltaTime * 2f);

            if (decayTimer <= 0f)
            {
                // Only retain horizontal momentum — vertical dashes hand off to gravity cleanly
                rb.linearVelocity = new Vector2(
                    decayStartVelocity.x * momentumRetain,
                    0f);
            }
        }
    }

    private void Fire()
    {
        currentAmmo--;
        cooldownTimer = cooldown;
        OnAmmoChanged?.Invoke(currentAmmo);

        boostVelocity = aimDirection * boostSpeed;
        boostTimer = boostDuration;
        rb.linearVelocity = boostVelocity;
        gravity.SuppressGravity(boostDuration);

        GameEventBus.Publish(new SecondaryUsed {
            Direction = aimDirection,
            ModeName = "dash"
        });
        GameEventBus.Publish(new PlayerDashed {
            Direction = aimDirection,
            Speed = boostSpeed
        });

        // Celeste-style freeze frame: brief pause on dash activation
        if (freezeFrameDuration > 0f)
        {
            Time.timeScale = 0f;
            freezeActive = true;
            freezeTimer = freezeFrameDuration;
        }

        if (projectilePrefab != null)
        {
            var proj = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
            var projRb = proj.GetComponent<Rigidbody2D>();
            if (projRb != null)
            {
                projRb.linearVelocity = -aimDirection * projectileSpeed;
            }
            Destroy(proj, projectileLifetime);
        }
    }
}
