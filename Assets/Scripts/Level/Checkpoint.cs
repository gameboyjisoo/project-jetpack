using UnityEngine;

/// <summary>
/// Trigger zone that updates the player's respawn point when entered.
/// Place within a room to create mid-room save points.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class Checkpoint : MonoBehaviour
{
    [SerializeField] private bool activateOnce = true;

    private bool activated;
    private SpriteRenderer sr;

    private static readonly Color InactiveColor = new Color(0.4f, 1f, 0.4f, 0.8f);
    private static readonly Color ActiveColor = new Color(1f, 1f, 0.3f, 0.8f);

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;

        if (sr != null)
            sr.color = InactiveColor;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (activated && activateOnce) return;

        if (other.TryGetComponent<PlayerRespawn>(out var respawn))
        {
            respawn.SetSpawnPoint(transform.position);
            activated = true;

            if (sr != null)
                sr.color = ActiveColor;

            GameEventBus.Publish(new CheckpointReached { Position = transform.position });
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = activated ? Color.yellow : Color.green;
        Gizmos.DrawWireCube(transform.position, new Vector3(1f, 1f, 0f));
    }
}
