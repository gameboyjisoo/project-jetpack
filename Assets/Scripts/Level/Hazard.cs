using UnityEngine;

/// <summary>
/// Kills the player on contact and respawns them at the room's spawn point.
/// </summary>
public class Hazard : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.TryGetComponent<PlayerRespawn>(out var respawn))
        {
            if (!respawn.IsDead && !respawn.IsInvincible)
                respawn.Die();
        }
    }
}
