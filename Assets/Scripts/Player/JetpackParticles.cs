using UnityEngine;

/// <summary>
/// Controls jetpack exhaust particle effects based on player state.
/// Exhaust color shifts from cyan (full) to red (empty) and sputters at low fuel.
/// </summary>
public class JetpackParticles : MonoBehaviour
{
    [SerializeField] private ParticleSystem exhaustParticles;

    [Header("Fuel Color Feedback")]
    [SerializeField] private Color fullColor = new Color(0.4f, 0.9f, 1f);    // bright cyan
    [SerializeField] private Color midColor = new Color(1f, 0.6f, 0.1f);     // orange
    [SerializeField] private Color emptyColor = new Color(1f, 0.15f, 0.1f);  // red

    [Header("Sputter Settings")]
    [SerializeField] private float sputterThreshold = 0.2f;  // below 20% gas
    [SerializeField] private float sputterInterval = 0.06f;   // toggle on/off rapidly

    private PlayerController player;
    private JetpackGas jetpackGas;
    private ParticleSystem.MainModule mainModule;
    private ParticleSystem.EmissionModule emissionModule;
    private float baseEmissionRate;
    private float sputterTimer;
    private bool sputterOn = true;

    private void Awake()
    {
        player = GetComponentInParent<PlayerController>();
        jetpackGas = GetComponentInParent<JetpackGas>();

        if (exhaustParticles != null)
        {
            mainModule = exhaustParticles.main;
            emissionModule = exhaustParticles.emission;
            baseEmissionRate = emissionModule.rateOverTime.constant;
        }
    }

    private void Update()
    {
        if (exhaustParticles == null || player == null) return;

        if (player.IsJetpacking)
        {
            if (!exhaustParticles.isPlaying)
                exhaustParticles.Play();

            UpdateExhaustColor();
            UpdateSputter();
        }
        else if (exhaustParticles.isPlaying)
        {
            exhaustParticles.Stop();
            sputterOn = true;
            sputterTimer = 0f;
        }
    }

    private void UpdateExhaustColor()
    {
        if (jetpackGas == null) return;

        float percent = jetpackGas.GasPercent;

        // Two-stage gradient: full→mid (100%-50%), mid→empty (50%-0%)
        Color targetColor;
        if (percent > 0.5f)
        {
            float t = (percent - 0.5f) / 0.5f; // 1 at full, 0 at mid
            targetColor = Color.Lerp(midColor, fullColor, t);
        }
        else
        {
            float t = percent / 0.5f; // 1 at mid, 0 at empty
            targetColor = Color.Lerp(emptyColor, midColor, t);
        }

        mainModule.startColor = targetColor;
    }

    private void UpdateSputter()
    {
        if (jetpackGas == null) return;

        if (jetpackGas.GasPercent <= sputterThreshold)
        {
            sputterTimer += Time.deltaTime;
            if (sputterTimer >= sputterInterval)
            {
                sputterTimer = 0f;
                sputterOn = !sputterOn;
            }

            // Flicker emission between full and zero to create sputter
            var rate = emissionModule.rateOverTime;
            rate.constant = sputterOn ? baseEmissionRate : 0f;
            emissionModule.rateOverTime = rate;
        }
        else
        {
            // Normal emission
            var rate = emissionModule.rateOverTime;
            rate.constant = baseEmissionRate;
            emissionModule.rateOverTime = rate;
            sputterOn = true;
            sputterTimer = 0f;
        }
    }
}
