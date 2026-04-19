using UnityEngine;
using UnityEngine.InputSystem;

public enum SecondaryMode { Dash, Gun }

/// <summary>
/// Secondary Booster: swappable mode — Dash (limited ammo, repositioning)
/// or Gun (free projectile, no movement effect).
/// </summary>
[RequireComponent(typeof(PlayerController))]
public class SecondaryBooster : MonoBehaviour
{
    [Header("Mode")]
    [SerializeField] private SecondaryMode currentMode = SecondaryMode.Dash;

    [Header("Dash")]
    [SerializeField] private int maxAmmo = 1;
    [SerializeField] private float boostSpeed = 32f;
    [SerializeField] private float boostDuration = 0.15f;
    [SerializeField] private float cooldown = 0.15f;
    [SerializeField] private float freezeFrameDuration = 0.05f;
    [SerializeField] private float endDecayTime = 0.06f;
    [SerializeField] [Range(0f, 0.5f)] private float momentumRetain = 0.25f;
    [SerializeField] private float dashBufferTime = 0.1f;
    [SerializeField] private float wavedashSpeedMultiplier = 1.2f;
    [SerializeField] private float wavedashKeepTime = 0.12f;

    [Header("Gun")]
    [SerializeField] private float gunCooldown = 0.2f;
    [SerializeField] private float projectileSpeed = 20f;
    [SerializeField] private float projectileLifetime = 1.0f;
    [SerializeField] private Sprite projectileSprite;

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
    private bool wasAirborneAtDashStart;
    private float wavedashTimer;
    private float wavedashSpeed;

    public SecondaryMode CurrentMode => currentMode;
    public int CurrentAmmo => currentAmmo;
    public int MaxAmmo => maxAmmo;
    public bool IsBoosting => boostTimer > 0f || decayTimer > 0f;
    public bool IsWavedashing => wavedashTimer > 0f;
    public event System.Action<int> OnAmmoChanged;

    /// <summary>
    /// Recharge dash ammo from external source (mid-air pickup, gimmick, etc.).
    /// </summary>
    public void Recharge(int amount = 1)
    {
        currentAmmo = Mathf.Min(currentAmmo + amount, maxAmmo);
        OnAmmoChanged?.Invoke(currentAmmo);
    }

    /// <summary>
    /// Swap secondary mode (called by gimmicks, pickups, etc.).
    /// </summary>
    public void SwapMode(SecondaryMode mode)
    {
        if (mode == currentMode) return;

        string oldName = currentMode.ToString().ToLower();
        currentMode = mode;
        string newName = currentMode.ToString().ToLower();

        GameEventBus.Publish(new SecondaryModeChanged {
            OldMode = oldName,
            NewMode = newName
        });
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

        // Recharge ammo on ground (dash mode only)
        if (currentMode == SecondaryMode.Dash && player.IsGrounded && currentAmmo < maxAmmo)
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

            bool canFire = cooldownTimer <= 0f;
            if (currentMode == SecondaryMode.Dash)
                canFire = canFire && currentAmmo > 0 && !IsBoosting;

            if (dashBufferTimer > 0f && canFire)
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

            // Wavedash: diagonal-down dash started in air + hit ground = convert to horizontal speed
            if (wasAirborneAtDashStart && boostVelocity.y < -0.1f && Mathf.Abs(boostVelocity.x) > 0.1f && player.IsGrounded)
            {
                wavedashSpeed = boostSpeed * wavedashSpeedMultiplier;
                rb.linearVelocity = new Vector2(Mathf.Sign(boostVelocity.x) * wavedashSpeed, 0f);
                wavedashTimer = wavedashKeepTime;
                boostTimer = 0f;
                decayTimer = 0f;
                return;
            }

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

        // Wavedash speed maintenance — hold speed for a brief window so the player
        // can jump out of it before ground deceleration kills the momentum
        if (wavedashTimer > 0f)
        {
            wavedashTimer -= Time.fixedDeltaTime;
            float dir = Mathf.Sign(rb.linearVelocity.x);
            rb.linearVelocity = new Vector2(dir * wavedashSpeed, rb.linearVelocity.y);
        }
    }

    private void Fire()
    {
        if (currentMode == SecondaryMode.Dash)
            FireDash();
        else
            FireGun();
    }

    private void FireDash()
    {
        wasAirborneAtDashStart = !player.IsGrounded;
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
    }

    private void FireGun()
    {
        cooldownTimer = gunCooldown;

        // No ammo cost, no movement effect, no freeze frame
        GameEventBus.Publish(new SecondaryUsed {
            Direction = aimDirection,
            ModeName = "gun"
        });

        SpawnProjectile(aimDirection);
    }

    private void SpawnProjectile(Vector2 direction)
    {
        var go = new GameObject("Projectile");
        go.transform.position = (Vector2)transform.position + direction * 0.5f;
        go.transform.localScale = new Vector3(0.3f, 0.3f, 1f);

        // Sprite
        var sr = go.AddComponent<SpriteRenderer>();
        if (projectileSprite != null)
            sr.sprite = projectileSprite;
        else
            sr.sprite = CreateFallbackSprite();
        sr.color = new Color(1f, 0.95f, 0.6f); // warm yellow-white
        sr.sortingOrder = 10;

        // Physics
        var projRb = go.AddComponent<Rigidbody2D>();
        projRb.gravityScale = 0f;
        projRb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        projRb.linearVelocity = direction * projectileSpeed;

        // Collider (trigger for ground detection)
        var col = go.AddComponent<CircleCollider2D>();
        col.radius = 0.15f;
        col.isTrigger = true;

        // Projectile behaviour
        var proj = go.AddComponent<Projectile>();
        proj.Init(projectileLifetime);
    }

    private static Sprite fallbackSprite;

    private static Sprite CreateFallbackSprite()
    {
        if (fallbackSprite != null) return fallbackSprite;
        var tex = new Texture2D(4, 4);
        var pixels = new Color[16];
        for (int i = 0; i < 16; i++) pixels[i] = Color.white;
        tex.SetPixels(pixels);
        tex.Apply();
        tex.filterMode = FilterMode.Point;
        fallbackSprite = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4);
        return fallbackSprite;
    }
}
