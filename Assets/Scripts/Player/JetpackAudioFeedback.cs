using UnityEngine;

/// <summary>
/// Provides audio feedback for jetpack fuel state.
/// Plays a short SFX clip repeatedly with gaps — gaps widen and pitch distorts as fuel drains.
/// </summary>
public class JetpackAudioFeedback : MonoBehaviour
{
    [Header("Burst SFX (e.g. SE_14_2E)")]
    [SerializeField] private AudioSource engineSource;
    [SerializeField] private AudioClip burstClip;
    [SerializeField] private float volume = 0.6f;

    [Header("Repeat Timing")]
    [SerializeField] private float fullFuelInterval = 0.08f;   // near-consecutive at full gas
    [SerializeField] private float emptyFuelInterval = 0.3f;   // long gaps when nearly empty

    [Header("Pitch Distortion")]
    [SerializeField] private float fullPitch = 1.0f;
    [SerializeField] private float emptyPitch = 0.55f;
    [SerializeField] private float pitchJitterAt = 0.3f;       // below 30% gas, add random jitter
    [SerializeField] private float maxPitchJitter = 0.2f;      // +/- range of random jitter

    [Header("Empty Click")]
    [SerializeField] private AudioSource clickSource;
    [SerializeField] private AudioClip emptyClickClip;

    private PlayerController player;
    private JetpackGas jetpackGas;
    private bool wasJetpacking;
    private float burstTimer;
    private bool playedEmptyClick;

    private void Awake()
    {
        player = GetComponentInParent<PlayerController>();
        jetpackGas = GetComponentInParent<JetpackGas>();

        if (engineSource != null)
        {
            engineSource.playOnAwake = false;
            engineSource.loop = false;
        }
    }

    private void OnEnable()
    {
        if (jetpackGas != null)
            jetpackGas.OnGasEmpty += HandleGasEmpty;
    }

    private void OnDisable()
    {
        if (jetpackGas != null)
            jetpackGas.OnGasEmpty -= HandleGasEmpty;
    }

    private void Update()
    {
        if (player == null) return;

        bool jetting = player.IsJetpacking;

        if (jetting && !wasJetpacking)
        {
            // Play immediately on activation
            burstTimer = 999f;
            playedEmptyClick = false;
        }
        else if (!jetting && wasJetpacking)
        {
            burstTimer = 0f;
        }

        if (jetting)
            UpdateBursts();

        wasJetpacking = jetting;
    }

    private void UpdateBursts()
    {
        if (engineSource == null || burstClip == null || jetpackGas == null) return;

        float percent = jetpackGas.GasPercent;

        // Interval widens as fuel drains
        float interval = Mathf.Lerp(emptyFuelInterval, fullFuelInterval, percent);

        burstTimer += Time.deltaTime;
        if (burstTimer >= interval)
        {
            burstTimer = 0f;

            // Base pitch drops with fuel
            float pitch = Mathf.Lerp(emptyPitch, fullPitch, percent);

            // Add random jitter at low fuel for "struggling engine" feel
            if (percent < pitchJitterAt)
            {
                float jitterStrength = 1f - (percent / pitchJitterAt);
                pitch += Random.Range(-maxPitchJitter, maxPitchJitter) * jitterStrength;
            }

            engineSource.pitch = pitch;
            engineSource.PlayOneShot(burstClip, volume);
        }
    }

    private void HandleGasEmpty()
    {
        if (playedEmptyClick) return;
        playedEmptyClick = true;

        if (clickSource != null && emptyClickClip != null)
            clickSource.PlayOneShot(emptyClickClip);
    }
}
