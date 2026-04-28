using UnityEngine;

/// <summary>
/// A ceiling hazard that detects the player below it and slams down fast.
/// The player must dash to clear the detection zone before the slam completes.
///
/// State machine: Idle -> Warning -> Slamming -> Holding -> Retracting -> Cooldown -> Idle
///
/// Layer conventions:
///   Layer 8 (Ground)  — this GameObject (solid, player cannot pass through)
///   Layer 9 (Player)  — detection target
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class Crusher : MonoBehaviour
{
    private enum CrusherState { Idle, Warning, Slamming, Holding, Retracting, Cooldown }

    [Header("Movement")]
    [SerializeField] private float slamDistance = 6f;
    [SerializeField] private float slamTime = 0.1f;
    [SerializeField] private float holdTime = 0.5f;
    [SerializeField] private float retractTime = 1.0f;
    [SerializeField] private float warningTime = 0.3f;
    [SerializeField] private float cooldownTime = 0.3f;

    [Header("Detection")]
    [SerializeField] private float detectionRange = 8f;
    [SerializeField] private LayerMask playerLayer = 1 << 9;

    [Header("Visual")]
    [SerializeField] private Color idleColor = new Color(0.6f, 0.2f, 0.2f, 1f);
    [SerializeField] private Color warningColor = new Color(1f, 0.3f, 0.3f, 1f);
    [SerializeField] private float warningFlashRate = 10f;

    private BoxCollider2D box;
    private SpriteRenderer sr;
    private CrusherState state = CrusherState.Idle;
    private float stateTimer;
    private float startY;
    private float targetY;

    private void Awake()
    {
        box = GetComponent<BoxCollider2D>();
        sr = GetComponent<SpriteRenderer>();
        sr.drawMode = SpriteDrawMode.Tiled;
        sr.size = box.size;
        startY = transform.position.y;
        targetY = startY - slamDistance;
    }

    private void Update()
    {
        stateTimer += Time.deltaTime;

        switch (state)
        {
            case CrusherState.Idle:       TickIdle(); break;
            case CrusherState.Warning:    TickWarning(); break;
            case CrusherState.Slamming:   TickSlamming(); break;
            case CrusherState.Holding:    TickHolding(); break;
            case CrusherState.Retracting: TickRetracting(); break;
            case CrusherState.Cooldown:   TickCooldown(); break;
        }
    }

    private void TickIdle()
    {
        sr.color = idleColor;
        if (IsPlayerInDetectionZone())
            EnterState(CrusherState.Warning);
    }

    private void TickWarning()
    {
        float flash = Mathf.Sin(stateTimer * warningFlashRate * Mathf.PI * 2f);
        sr.color = flash > 0f ? warningColor : idleColor;

        if (stateTimer >= warningTime)
            EnterState(CrusherState.Slamming);
    }

    private void TickSlamming()
    {
        float t = Mathf.Clamp01(stateTimer / slamTime);
        SetPositionY(Mathf.Lerp(startY, targetY, t));
        CheckCrush();

        if (stateTimer >= slamTime)
        {
            SetPositionY(targetY);
            GameEventBus.Publish(new GimmickActivated { GimmickId = gameObject.name });
            EnterState(CrusherState.Holding);
        }
    }

    private void TickHolding()
    {
        sr.color = idleColor;
        CheckCrush();

        if (stateTimer >= holdTime)
            EnterState(CrusherState.Retracting);
    }

    private void TickRetracting()
    {
        float t = Mathf.Clamp01(stateTimer / retractTime);
        SetPositionY(Mathf.Lerp(targetY, startY, t));

        if (stateTimer >= retractTime)
        {
            SetPositionY(startY);
            GameEventBus.Publish(new GimmickDeactivated { GimmickId = gameObject.name });
            EnterState(CrusherState.Cooldown);
        }
    }

    private void TickCooldown()
    {
        sr.color = idleColor;
        if (stateTimer >= cooldownTime)
            EnterState(CrusherState.Idle);
    }

    private void EnterState(CrusherState next)
    {
        state = next;
        stateTimer = 0f;
    }

    private void SetPositionY(float y)
    {
        var pos = transform.position;
        pos.y = y;
        transform.position = pos;
    }

    private bool IsPlayerInDetectionZone()
    {
        Vector2 crusherBottom = (Vector2)transform.position
            + new Vector2(box.offset.x, box.offset.y - box.size.y * 0.5f);
        Vector2 zoneCenter = crusherBottom + Vector2.down * (detectionRange * 0.5f);
        Vector2 zoneSize = new Vector2(box.size.x, detectionRange);
        return Physics2D.OverlapBox(zoneCenter, zoneSize, 0f, playerLayer) != null;
    }

    private void CheckCrush()
    {
        Vector2 bottomCenter = (Vector2)transform.position
            + new Vector2(box.offset.x, box.offset.y - box.size.y * 0.5f);
        Collider2D hit = Physics2D.OverlapBox(bottomCenter, new Vector2(box.size.x, 0.2f), 0f, playerLayer);
        if (hit == null) return;
        if (!hit.TryGetComponent<PlayerRespawn>(out var respawn)) return;
        if (!respawn.IsDead && !respawn.IsInvincible)
            respawn.Die();
    }

    private void OnDrawGizmos()
    {
        if (!TryGetComponent<BoxCollider2D>(out var b)) return;

        Vector2 crusherBottom = (Vector2)transform.position
            + new Vector2(b.offset.x, b.offset.y - b.size.y * 0.5f);

        // Detection zone
        Gizmos.color = new Color(1f, 0.9f, 0f, 0.4f);
        Vector2 zoneCenter = crusherBottom + Vector2.down * (detectionRange * 0.5f);
        Gizmos.DrawWireCube(zoneCenter, new Vector3(b.size.x, detectionRange, 0f));

        // Slam path
        float editY = Application.isPlaying ? startY : transform.position.y;
        Vector3 slamEnd = new Vector3(transform.position.x, editY - slamDistance, 0f);
        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.6f);
        Gizmos.DrawLine(transform.position, slamEnd);
        Gizmos.DrawWireCube(slamEnd + new Vector3(b.offset.x, b.offset.y, 0f), new Vector3(b.size.x, b.size.y, 0f));
    }
}
