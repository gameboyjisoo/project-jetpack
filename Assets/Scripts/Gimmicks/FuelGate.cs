using UnityEngine;

/// <summary>
/// A barrier that opens when the player's fuel matches the required tier.
/// Color-matches the exhaust gradient: cyan (High), orange (Mid), red (Low).
/// Uses the Event Bus to listen for fuel changes — no direct player reference needed.
/// </summary>
[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class FuelGate : MonoBehaviour
{
    [Header("Gate Settings")]
    [SerializeField] private FuelTier requiredTier = FuelTier.Low;
    [SerializeField] private bool invertRequirement = false;

    [Header("Visual")]
    [SerializeField] private Color highColor = new Color(0f, 0.9f, 1f, 1f);   // Cyan
    [SerializeField] private Color midColor = new Color(1f, 0.6f, 0f, 1f);    // Orange
    [SerializeField] private Color lowColor = new Color(1f, 0.15f, 0.15f, 1f); // Red
    [SerializeField] private Color openColor = new Color(1f, 1f, 1f, 0.2f);   // Faded when open

    [Header("Behavior")]
    [SerializeField] private float checkRadius = 20f;
    [SerializeField] private bool stayOpenOnceTriggered = false;

    private Collider2D col;
    private SpriteRenderer sr;
    private bool isOpen;
    private bool permanentlyOpen;
    private JetpackGas playerGas;

    /// <summary>
    /// Runtime initialization for code-spawned gates.
    /// </summary>
    public void Init(FuelTier tier, bool invert = false, bool stayOpen = false)
    {
        requiredTier = tier;
        invertRequirement = invert;
        stayOpenOnceTriggered = stayOpen;
    }

    private void Start()
    {
        col = GetComponent<Collider2D>();
        sr = GetComponent<SpriteRenderer>();
        SetGateVisual(false);

        var player = GameObject.FindWithTag("Player");
        if (player != null)
            playerGas = player.GetComponent<JetpackGas>();
    }

    private void Update()
    {
        if (permanentlyOpen) return;
        if (playerGas == null) return;

        // Only check when player is within range
        float dist = Vector2.Distance(transform.position, playerGas.transform.position);
        if (dist > checkRadius) return;

        bool tierMatches = playerGas.CurrentTier == requiredTier;
        if (invertRequirement) tierMatches = !tierMatches;

        if (tierMatches && !isOpen)
            Open();
        else if (!tierMatches && isOpen)
            Close();
    }

    private void Open()
    {
        isOpen = true;
        col.enabled = false;
        SetGateVisual(true);
        GameEventBus.Publish(new GimmickActivated { GimmickId = gameObject.name });

        if (stayOpenOnceTriggered)
            permanentlyOpen = true;
    }

    private void Close()
    {
        isOpen = false;
        col.enabled = true;
        SetGateVisual(false);
        GameEventBus.Publish(new GimmickDeactivated { GimmickId = gameObject.name });
    }

    private void SetGateVisual(bool open)
    {
        if (open)
        {
            sr.color = openColor;
        }
        else
        {
            sr.color = requiredTier switch
            {
                FuelTier.High => highColor,
                FuelTier.Mid => midColor,
                FuelTier.Low => lowColor,
                _ => highColor
            };
        }
    }

    private void OnDrawGizmos()
    {
        // Show gate color and check radius in editor
        Gizmos.color = requiredTier switch
        {
            FuelTier.High => Color.cyan,
            FuelTier.Mid => new Color(1f, 0.6f, 0f),
            FuelTier.Low => Color.red,
            _ => Color.white
        };
        Gizmos.DrawWireSphere(transform.position, checkRadius);
    }
}
