using UnityEngine;

/// <summary>
/// Controls jetpack exhaust particle effects based on player state.
/// </summary>
public class JetpackParticles : MonoBehaviour
{
    [SerializeField] private ParticleSystem exhaustParticles;

    private PlayerController player;

    private void Awake()
    {
        player = GetComponentInParent<PlayerController>();
    }

    private void Update()
    {
        if (exhaustParticles == null || player == null) return;

        if (player.IsJetpacking && !exhaustParticles.isPlaying)
            exhaustParticles.Play();
        else if (!player.IsJetpacking && exhaustParticles.isPlaying)
            exhaustParticles.Stop();
    }
}
