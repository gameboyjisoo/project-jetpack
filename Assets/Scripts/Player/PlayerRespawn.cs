using UnityEngine;
using System.Collections;

[RequireComponent(typeof(PlayerController))]
[RequireComponent(typeof(SpriteRenderer))]
public class PlayerRespawn : MonoBehaviour
{
    [SerializeField] private float respawnDelay = 0.3f;
    [SerializeField] private float invincibilityDuration = 0.1f;

    private Vector3 currentSpawnPoint;
    private Rigidbody2D rb;
    private PlayerController controller;
    private SpriteRenderer spriteRenderer;
    private bool isDead;
    private bool isInvincible;

    public bool IsDead => isDead;
    public bool IsInvincible => isInvincible;
    public event System.Action OnDeath;
    public event System.Action OnRespawn;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        controller = GetComponent<PlayerController>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        currentSpawnPoint = transform.position;
    }

    private void Start()
    {
        if (RoomManager.Instance != null)
        {
            RoomManager.Instance.OnRoomChanged += HandleRoomChanged;
        }
    }

    private void OnDestroy()
    {
        if (RoomManager.Instance != null)
        {
            RoomManager.Instance.OnRoomChanged -= HandleRoomChanged;
        }
    }

    private void HandleRoomChanged(Room oldRoom, Room newRoom)
    {
        if (newRoom.SpawnPoint != null)
            currentSpawnPoint = newRoom.SpawnPoint.position;
    }

    public void SetSpawnPoint(Vector3 position)
    {
        currentSpawnPoint = position;
    }

    public void Die()
    {
        if (isDead) return;
        isDead = true;
        OnDeath?.Invoke();
        GameEventBus.Publish(new PlayerDied { Position = transform.position });
        StartCoroutine(RespawnCoroutine());
    }

    private IEnumerator RespawnCoroutine()
    {
        rb.linearVelocity = Vector2.zero;
        rb.simulated = false;
        controller.enabled = false;
        spriteRenderer.enabled = false;

        yield return new WaitForSeconds(respawnDelay);

        transform.position = currentSpawnPoint;
        rb.linearVelocity = Vector2.zero;
        rb.simulated = true;
        controller.enabled = true;
        spriteRenderer.enabled = true;
        isDead = false;

        var gas = GetComponent<JetpackGas>();
        if (gas != null)
            gas.Recharge();

        OnRespawn?.Invoke();

        StartCoroutine(InvincibilityFlash());
    }

    private IEnumerator InvincibilityFlash()
    {
        isInvincible = true;
        float elapsed = 0f;

        while (elapsed < invincibilityDuration)
        {
            spriteRenderer.enabled = !spriteRenderer.enabled;
            yield return new WaitForSeconds(0.08f);
            elapsed += 0.08f;
        }

        spriteRenderer.enabled = true;
        isInvincible = false;
    }
}
