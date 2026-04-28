using UnityEngine;

/// <summary>
/// Plays placeholder SFX for player actions via Event Bus subscriptions.
/// Pure event-driven — no direct player references needed.
///
/// Recommended Cave Story SFX assignments:
///   Jump:  SE_03_0F.wav (Cave Story jump)
///   Land:  ID17_snd_thud.wav (thud)
///   Dash:  SE_10_1D.wav (teleport/whoosh)
///   Death: SE_23_05.wav (hurt)
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class PlayerSFX : MonoBehaviour
{
    [Header("Clips")]
    [SerializeField] private AudioClip jumpClip;
    [SerializeField] private AudioClip landClip;
    [SerializeField] private AudioClip dashClip;
    [SerializeField] private AudioClip deathClip;

    [Header("Volume")]
    [SerializeField] private float jumpVolume = 0.6f;
    [SerializeField] private float landVolume = 0.7f;
    [SerializeField] private float dashVolume = 0.6f;
    [SerializeField] private float deathVolume = 1f;

    private AudioSource audioSource;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = false;
    }

    private void OnEnable()
    {
        GameEventBus.Subscribe<PlayerJumped>(OnJumped);
        GameEventBus.Subscribe<PlayerLanded>(OnLanded);
        GameEventBus.Subscribe<PlayerDashed>(OnDashed);
        GameEventBus.Subscribe<PlayerDied>(OnDied);
    }

    private void OnDisable()
    {
        GameEventBus.Unsubscribe<PlayerJumped>(OnJumped);
        GameEventBus.Unsubscribe<PlayerLanded>(OnLanded);
        GameEventBus.Unsubscribe<PlayerDashed>(OnDashed);
        GameEventBus.Unsubscribe<PlayerDied>(OnDied);
    }

    private void OnJumped(PlayerJumped e)
    {
        if (jumpClip == null) return;
        audioSource.pitch = Random.Range(0.97f, 1.03f);
        audioSource.PlayOneShot(jumpClip, jumpVolume);
        audioSource.pitch = 1f;
    }

    private void OnLanded(PlayerLanded e)
    {
        if (landClip == null) return;

        // Heavier falls = lower pitch, louder volume
        float t = Mathf.Clamp01(e.FallSpeed / 15f);
        audioSource.pitch = Mathf.Lerp(1.1f, 0.85f, t);
        float vol = Mathf.Lerp(0.3f, landVolume, t);
        audioSource.PlayOneShot(landClip, vol);
        audioSource.pitch = 1f;
    }

    private void OnDashed(PlayerDashed e)
    {
        if (dashClip == null) return;
        audioSource.pitch = Random.Range(0.95f, 1.05f);
        audioSource.PlayOneShot(dashClip, dashVolume);
        audioSource.pitch = 1f;
    }

    private void OnDied(PlayerDied e)
    {
        if (deathClip == null) return;
        audioSource.pitch = 1f;
        audioSource.PlayOneShot(deathClip, deathVolume);
    }
}
