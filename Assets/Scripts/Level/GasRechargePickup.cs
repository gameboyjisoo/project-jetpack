using UnityEngine;

/// <summary>
/// Mid-air pickup that recharges jetpack gas (like Celeste's Dash Crystal).
/// Disappears on use, reappears when player touches ground or re-enters the room.
/// </summary>
public class GasRechargePickup : MonoBehaviour
{
    [SerializeField] private float bobAmplitude = 0.15f;
    [SerializeField] private float bobSpeed = 2f;

    private SpriteRenderer spriteRenderer;
    private Collider2D pickupCollider;
    private Vector3 startPos;
    private bool consumed;
    private JetpackGas subscribedGas;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        pickupCollider = GetComponent<Collider2D>();
        startPos = transform.position;
    }

    private void OnDestroy()
    {
        if (subscribedGas != null)
            subscribedGas.OnGasRecharged -= Respawn;
    }

    private void Update()
    {
        if (!consumed)
        {
            float yOffset = Mathf.Sin(Time.time * bobSpeed) * bobAmplitude;
            transform.position = startPos + Vector3.up * yOffset;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (consumed) return;

        if (other.TryGetComponent<JetpackGas>(out var gas))
        {
            gas.RechargeFromPickup();
            Consume(gas);
        }
    }

    private void Consume(JetpackGas gas)
    {
        consumed = true;
        spriteRenderer.enabled = false;
        pickupCollider.enabled = false;

        subscribedGas = gas;
        subscribedGas.OnGasRecharged += Respawn;
    }

    public void Respawn()
    {
        consumed = false;
        spriteRenderer.enabled = true;
        pickupCollider.enabled = true;
        transform.position = startPos;

        if (subscribedGas != null)
        {
            subscribedGas.OnGasRecharged -= Respawn;
            subscribedGas = null;
        }
    }
}
