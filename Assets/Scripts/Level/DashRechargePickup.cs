using UnityEngine;

/// <summary>
/// Mid-air pickup that recharges the player's dash ammo.
/// Disappears on use, reappears when player touches ground.
/// </summary>
public class DashRechargePickup : MonoBehaviour
{
    [SerializeField] private float bobAmplitude = 0.15f;
    [SerializeField] private float bobSpeed = 2f;

    private SpriteRenderer spriteRenderer;
    private Collider2D pickupCollider;
    private Vector3 startPos;
    private bool consumed;
    private PlayerController trackedPlayer;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        pickupCollider = GetComponent<Collider2D>();
        startPos = transform.position;
    }

    private void Update()
    {
        if (!consumed)
        {
            float yOffset = Mathf.Sin(Time.time * bobSpeed) * bobAmplitude;
            transform.position = startPos + Vector3.up * yOffset;
        }
        else if (trackedPlayer != null && trackedPlayer.IsGrounded)
        {
            Respawn();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (consumed) return;

        if (other.TryGetComponent<SecondaryBooster>(out var booster))
        {
            booster.Recharge();
            GameEventBus.Publish(new DashPickupCollected());
            trackedPlayer = other.GetComponent<PlayerController>();
            Consume();
        }
    }

    private void Consume()
    {
        consumed = true;
        spriteRenderer.enabled = false;
        pickupCollider.enabled = false;
    }

    private void Respawn()
    {
        consumed = false;
        spriteRenderer.enabled = true;
        pickupCollider.enabled = true;
        transform.position = startPos;
        trackedPlayer = null;
    }
}
