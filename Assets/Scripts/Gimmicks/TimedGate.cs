using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A gate that opens after a timed countdown when the player enters a detection zone.
/// Adjacent TimedGate blocks auto-group into one logical gate via flood-fill at Start.
/// One block per group becomes the "leader" — owns the detection zone, audio, and countdown.
/// All blocks open/close together. Resets on PlayerDied.
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class TimedGate : MonoBehaviour
{
    [Header("Timing")]
    [SerializeField] private float countdownTime = 3f;
    [SerializeField] private int buzzCount = 3;

    [Header("Detection")]
    [SerializeField] private float detectionRadius = 10f;

    [Header("Audio")]
    [SerializeField] private AudioClip buzzClip;

    [Header("Visual")]
    [SerializeField] private Color closedColor = new Color(0.6f, 0.4f, 0.8f, 1f);
    [SerializeField] private Color openColor = new Color(0.6f, 0.4f, 0.8f, 0.2f);

    private BoxCollider2D gateCollider;
    private SpriteRenderer sr;
    private bool isOpen;
    private bool isCountingDown;

    // Group management
    private TimedGate leader;
    private List<TimedGate> group;
    private AudioSource audioSource;
    private Coroutine countdownRoutine;
    private static readonly List<TimedGate> allGates = new();
    private bool grouped;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ClearStaticState()
    {
        allGates.Clear();
    }

    private void Awake()
    {
        gateCollider = GetComponent<BoxCollider2D>();
        sr = GetComponent<SpriteRenderer>();
        allGates.Add(this);
    }

    private void Start()
    {
        SetVisual(false);

        // Auto-group adjacent gates via flood-fill
        if (!grouped)
            FormGroup();

        // Leader sets up detection and audio
        if (leader == this)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;
        }

        GameEventBus.Subscribe<PlayerDied>(OnPlayerDied);
    }

    private void OnDestroy()
    {
        allGates.Remove(this);
        GameEventBus.Unsubscribe<PlayerDied>(OnPlayerDied);
    }

    private void Update()
    {
        if (leader != this) return;
        if (isOpen || isCountingDown) return;

        // Check if player is within detection radius of ANY block in the group
        var player = GameObject.FindWithTag("Player");
        if (player == null) return;

        Vector2 playerPos = player.transform.position;
        for (int i = 0; i < group.Count; i++)
        {
            float dist = Vector2.Distance(group[i].transform.position, playerPos);
            if (dist <= detectionRadius)
            {
                countdownRoutine = StartCoroutine(CountdownCoroutine());
                return;
            }
        }
    }

    private IEnumerator CountdownCoroutine()
    {
        isCountingDown = true;
        float interval = countdownTime / buzzCount;

        for (int i = 0; i < buzzCount; i++)
        {
            if (buzzClip != null && audioSource != null)
                audioSource.PlayOneShot(buzzClip);

            GameEventBus.Publish(new GimmickActivated { GimmickId = gameObject.name + "_buzz" });
            yield return new WaitForSeconds(interval);
        }

        OpenGroup();
    }

    private void OpenGroup()
    {
        for (int i = 0; i < group.Count; i++)
            group[i].Open();

        GameEventBus.Publish(new GimmickActivated { GimmickId = gameObject.name });
    }

    private void ResetGroup()
    {
        if (leader != this) return;

        if (countdownRoutine != null)
        {
            StopCoroutine(countdownRoutine);
            countdownRoutine = null;
        }
        isCountingDown = false;

        for (int i = 0; i < group.Count; i++)
            group[i].Close();
    }

    private void Open()
    {
        isOpen = true;
        gateCollider.enabled = false;
        SetVisual(true);
    }

    private void Close()
    {
        isOpen = false;
        isCountingDown = false;
        gateCollider.enabled = true;
        SetVisual(false);
    }

    private void SetVisual(bool open)
    {
        sr.color = open ? openColor : closedColor;
    }

    private void OnPlayerDied(PlayerDied evt)
    {
        ResetGroup();
    }

    // --- Auto-grouping via flood-fill ---

    private void FormGroup()
    {
        group = new List<TimedGate>();
        FloodFill(this, group);
        leader = group[0];

        for (int i = 0; i < group.Count; i++)
        {
            group[i].grouped = true;
            group[i].leader = leader;
            group[i].group = group;
        }
    }

    private static void FloodFill(TimedGate start, List<TimedGate> result)
    {
        var visited = new HashSet<TimedGate>();
        var queue = new Queue<TimedGate>();
        queue.Enqueue(start);
        visited.Add(start);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            result.Add(current);

            // Check 4 cardinal neighbors at 1-unit distance (1 tile = 1 unit at 16 PPU)
            Vector2 pos = current.transform.position;
            for (int i = 0; i < 4; i++)
            {
                Vector2 neighborPos = i switch
                {
                    0 => pos + Vector2.right,
                    1 => pos + Vector2.left,
                    2 => pos + Vector2.up,
                    _ => pos + Vector2.down
                };

                TimedGate neighbor = FindGateAt(neighborPos);
                if (neighbor != null && !visited.Contains(neighbor))
                {
                    visited.Add(neighbor);
                    queue.Enqueue(neighbor);
                }
            }
        }
    }

    private static TimedGate FindGateAt(Vector2 pos)
    {
        for (int i = 0; i < allGates.Count; i++)
        {
            // 0.1 tolerance for floating point
            if (Vector2.Distance(allGates[i].transform.position, pos) < 0.1f)
                return allGates[i];
        }
        return null;
    }

    private void OnDrawGizmos()
    {
        // Detection radius (only meaningful on leader, but show on all in editor)
        Gizmos.color = new Color(0.6f, 0.4f, 0.8f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        // Gate block outline
        var box = GetComponent<BoxCollider2D>();
        if (box != null)
        {
            Gizmos.color = Application.isPlaying && isOpen
                ? new Color(0.6f, 0.4f, 0.8f, 0.2f)
                : new Color(0.6f, 0.4f, 0.8f, 0.8f);
            Gizmos.DrawWireCube(transform.position + (Vector3)box.offset, box.size);
        }
    }
}
