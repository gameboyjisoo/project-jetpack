using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Secondary Booster: fires a projectile and applies recoil to the player.
/// Limited ammo, recharges on ground. Used for precise movement (vs jetpack's fast movement).
/// Inspired by Cave Story's weapon recoil mechanic.
/// </summary>
[RequireComponent(typeof(PlayerController))]
public class SecondaryBooster : MonoBehaviour
{
    [Header("Booster")]
    [SerializeField] private int maxAmmo = 3;
    [SerializeField] private float recoilForce = 12f;
    [SerializeField] private float cooldown = 0.15f;

    [Header("Projectile (optional)")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private float projectileSpeed = 20f;
    [SerializeField] private float projectileLifetime = 0.5f;

    [Header("Input")]
    [SerializeField] private InputActionAsset inputActions;

    private int currentAmmo;
    private float cooldownTimer;
    private Rigidbody2D rb;
    private PlayerController player;
    private Vector2 aimDirection = Vector2.right;

    private InputAction moveAction;
    private InputAction fireAction;
    private bool fireHeld;
    private bool prevFireHeld;

    public int CurrentAmmo => currentAmmo;
    public int MaxAmmo => maxAmmo;
    public event System.Action<int> OnAmmoChanged;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        player = GetComponent<PlayerController>();
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
        cooldownTimer -= Time.deltaTime;

        // Recharge ammo on ground
        if (player.IsGrounded && currentAmmo < maxAmmo)
        {
            currentAmmo = maxAmmo;
            OnAmmoChanged?.Invoke(currentAmmo);
        }

        // Read aim direction from move input
        if (moveAction != null)
        {
            Vector2 input = moveAction.ReadValue<Vector2>();
            if (input.sqrMagnitude > 0.01f)
            {
                if (Mathf.Abs(input.x) > Mathf.Abs(input.y))
                    aimDirection = new Vector2(Mathf.Sign(input.x), 0f);
                else
                    aimDirection = new Vector2(0f, Mathf.Sign(input.y));
            }
        }

        // Manual edge detection for fire
        if (fireAction != null)
        {
            prevFireHeld = fireHeld;
            fireHeld = fireAction.IsPressed();

            if (fireHeld && !prevFireHeld && cooldownTimer <= 0f && currentAmmo > 0)
                Fire();
        }
    }

    private void Fire()
    {
        currentAmmo--;
        cooldownTimer = cooldown;
        OnAmmoChanged?.Invoke(currentAmmo);

        // Apply recoil in opposite direction of aim
        Vector2 recoilDir = -aimDirection;
        rb.linearVelocity = new Vector2(
            rb.linearVelocity.x + recoilDir.x * recoilForce,
            rb.linearVelocity.y + recoilDir.y * recoilForce
        );

        // Spawn projectile if prefab assigned
        if (projectilePrefab != null)
        {
            var proj = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
            var projRb = proj.GetComponent<Rigidbody2D>();
            if (projRb != null)
            {
                projRb.linearVelocity = aimDirection * projectileSpeed;
            }
            Destroy(proj, projectileLifetime);
        }
    }
}
