using UnityEngine;

/// <summary>
/// Simple projectile: moves forward, destroys on Ground layer contact or after lifetime.
/// Spawned by SecondaryBooster in Gun mode.
/// </summary>
public class Projectile : MonoBehaviour
{
    private float lifetime;
    private float timer;
    private bool initialized;

    /// <summary>
    /// Called by SecondaryBooster immediately after AddComponent.
    /// Velocity is set externally on the Rigidbody2D.
    /// </summary>
    public void Init(float lifetime)
    {
        this.lifetime = lifetime;
        initialized = true;
    }

    private void Update()
    {
        if (!initialized) return;

        timer += Time.deltaTime;
        if (timer >= lifetime)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Destroy on contact with Ground layer
        if (other.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            Destroy(gameObject);
        }
    }
}
