using UnityEngine;

/// <summary>
/// A platform that cycles between solid (closed) and passable (open) on a timer.
/// Forces the player to time their approach — hover with jetpack, then dash through.
/// Warning flash before opening tells the player "get ready."
/// </summary>
[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class ClosingPlatform : MonoBehaviour
{
    [Header("Timing")]
    [SerializeField] private float closedDuration = 2f;
    [SerializeField] private float openDuration = 1f;
    [SerializeField] private float warningTime = 0.5f;
    [SerializeField] private float cycleOffset = 0f;

    [Header("Visual")]
    [SerializeField] private Color closedColor = new Color(0.85f, 0.55f, 0.1f, 1f);
    [SerializeField] private Color warningColor = new Color(1f, 0.2f, 0.2f, 1f);
    [SerializeField] private Color openColor = new Color(0.85f, 0.55f, 0.1f, 0.15f);
    [SerializeField] private float warningFlashRate = 8f;

    private Collider2D col;
    private SpriteRenderer sr;
    private float timer;
    private bool isOpen;

    private float TotalCycle => closedDuration + openDuration;

    private void Awake()
    {
        col = GetComponent<Collider2D>();
        sr = GetComponent<SpriteRenderer>();

        // Offset lets you stagger multiple platforms so they don't all sync
        timer = cycleOffset % TotalCycle;
        isOpen = timer >= closedDuration;
        col.enabled = !isOpen;
    }

    private void Update()
    {
        timer += Time.deltaTime;
        if (timer >= TotalCycle)
            timer -= TotalCycle;

        bool shouldBeOpen = timer >= closedDuration;

        if (shouldBeOpen && !isOpen)
            Open();
        else if (!shouldBeOpen && isOpen)
            Close();

        UpdateVisual();
    }

    private void Open()
    {
        isOpen = true;
        col.enabled = false;
        GameEventBus.Publish(new GimmickActivated { GimmickId = gameObject.name });
    }

    private void Close()
    {
        isOpen = false;
        col.enabled = true;
        GameEventBus.Publish(new GimmickDeactivated { GimmickId = gameObject.name });
    }

    private void UpdateVisual()
    {
        if (isOpen)
        {
            sr.color = openColor;
            return;
        }

        // Flash warning before the platform opens
        float timeUntilOpen = closedDuration - timer;
        if (timeUntilOpen <= warningTime && warningTime > 0f)
        {
            float flash = Mathf.Sin(timeUntilOpen * warningFlashRate * Mathf.PI * 2f);
            sr.color = flash > 0f ? closedColor : warningColor;
        }
        else
        {
            sr.color = closedColor;
        }
    }

    private void OnDrawGizmos()
    {
        bool open = Application.isPlaying ? isOpen : false;
        Gizmos.color = open ? new Color(1f, 0.6f, 0f, 0.2f) : new Color(1f, 0.6f, 0f, 0.8f);

        if (TryGetComponent<BoxCollider2D>(out var box))
            Gizmos.DrawWireCube(transform.position + (Vector3)box.offset, box.size);
        else
            Gizmos.DrawWireCube(transform.position, Vector3.one);
    }
}
