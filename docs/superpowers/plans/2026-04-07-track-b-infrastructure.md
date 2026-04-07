# Track B: Infrastructure — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build the level infrastructure, event bus, gimmick framework, and chapter configuration system — independent from Track A's player feel work.

**Architecture:** A central event bus decouples gimmicks from the systems they affect. Gimmicks implement a shared interface and interact with the player through read-only PlayerAPI or event publishing. Rooms are Celeste-style fixed screens with snap camera transitions. Chapter configs define per-chapter rules as ScriptableObject presets.

**Tech Stack:** Unity 6 (6000.0.34f1), C#, Unity Test Framework (EditMode tests for event bus)

**Branch:** All work on `track-b/<task-description>` branches. Only touches `Assets/Scripts/Core/`, `Assets/Scripts/Level/`, `Assets/Scripts/Camera/`, `Assets/Scripts/Gimmicks/`, `Assets/Tests/`.

**Conventions:**
- `rb.linearVelocity` (Unity 6 API)
- Never modify files in `Assets/Scripts/Player/` or `Assets/Scripts/UI/` — those belong to Track A
- Player interaction only through `PlayerAPI` (read-only) or `GameEventBus` (events)

---

## File Map

| File | Action | Responsibility |
|---|---|---|
| `Assets/Scripts/Core/GameEvent.cs` | Create | Event type definitions |
| `Assets/Scripts/Core/GameEventBus.cs` | Create | Central pub/sub system |
| `Assets/Scripts/Core/BoosterModeType.cs` | Create | Shared enum for booster modes |
| `Assets/Scripts/Core/PlayerAPI.cs` | Create | Read-only player state facade |
| `Assets/Scripts/Camera/RoomCamera.cs` | Rewrite | Room-snapping camera |
| `Assets/Scripts/Level/Room.cs` | Modify | Add spawn point, hazard support |
| `Assets/Scripts/Level/RoomManager.cs` | Modify | Camera integration, respawn |
| `Assets/Scripts/Level/DeathHandler.cs` | Create | Hazard collision → respawn |
| `Assets/Scripts/Level/ChapterConfig.cs` | Create | Per-chapter rules ScriptableObject |
| `Assets/Scripts/Level/ChapterLoader.cs` | Create | Applies ChapterConfig on load |
| `Assets/Scripts/Gimmicks/IGimmick.cs` | Create | Gimmick interface |
| `Assets/Scripts/Gimmicks/GimmickZone.cs` | Create | Trigger volume base |
| `Assets/Scripts/Gimmicks/WindTurbine.cs` | Create | Interval force gimmick |
| `Assets/Scripts/Gimmicks/GravitySwitch.cs` | Create | Gravity override gimmick |
| `Assets/Scripts/Gimmicks/ClosingPlatform.cs` | Create | Timed open/close platform |
| `Assets/Scripts/Gimmicks/FuelPickup.cs` | Create | Mid-air fuel + ammo refill |
| `Assets/Scripts/Gimmicks/SwitchTarget.cs` | Create | Shootable trigger target |
| `Assets/Scripts/Gimmicks/BoosterSwapZone.cs` | Create | Mode swap trigger zone |
| `Assets/Scripts/Camera/ScreenEffects.cs` | Create | Visual event subscriber |
| `Assets/Tests/EditMode/TestGameEventBus.cs` | Create | Unit tests |

---

### Task 1: Create Event Bus

**Files:**
- Create: `Assets/Scripts/Core/GameEvent.cs`
- Create: `Assets/Scripts/Core/GameEventBus.cs`
- Create: `Assets/Tests/EditMode/TestGameEventBus.cs`

The event bus is the backbone of Track B — gimmicks publish events, player/camera/UI systems subscribe.

- [ ] **Step 1: Write the test**

Create `Assets/Tests/EditMode/TestGameEventBus.cs`:

```csharp
using NUnit.Framework;

public class TestGameEventBus
{
    [SetUp]
    public void SetUp()
    {
        GameEventBus.Clear();
    }

    [Test]
    public void Subscribe_And_Publish_Fires_Handler()
    {
        bool fired = false;
        GameEventBus.Subscribe(GameEvent.FuelPickupCollected, () => fired = true);
        GameEventBus.Publish(GameEvent.FuelPickupCollected);
        Assert.IsTrue(fired);
    }

    [Test]
    public void Publish_Without_Subscribers_DoesNotThrow()
    {
        Assert.DoesNotThrow(() => GameEventBus.Publish(GameEvent.FuelPickupCollected));
    }

    [Test]
    public void Unsubscribe_Stops_Receiving()
    {
        int count = 0;
        System.Action handler = () => count++;
        GameEventBus.Subscribe(GameEvent.FuelPickupCollected, handler);
        GameEventBus.Publish(GameEvent.FuelPickupCollected);
        GameEventBus.Unsubscribe(GameEvent.FuelPickupCollected, handler);
        GameEventBus.Publish(GameEvent.FuelPickupCollected);
        Assert.AreEqual(1, count);
    }

    [Test]
    public void Multiple_Subscribers_All_Fire()
    {
        int count = 0;
        GameEventBus.Subscribe(GameEvent.FuelPickupCollected, () => count++);
        GameEventBus.Subscribe(GameEvent.FuelPickupCollected, () => count++);
        GameEventBus.Publish(GameEvent.FuelPickupCollected);
        Assert.AreEqual(2, count);
    }

    [Test]
    public void Different_Events_Are_Independent()
    {
        bool fuelFired = false;
        bool blindFired = false;
        GameEventBus.Subscribe(GameEvent.FuelPickupCollected, () => fuelFired = true);
        GameEventBus.Subscribe(GameEvent.BlindStart, () => blindFired = true);
        GameEventBus.Publish(GameEvent.FuelPickupCollected);
        Assert.IsTrue(fuelFired);
        Assert.IsFalse(blindFired);
    }

    [Test]
    public void PublishWithData_Delivers_Payload()
    {
        float received = 0f;
        GameEventBus.Subscribe<float>(GameEvent.BoosterModeChanged, (val) => received = val);
        GameEventBus.Publish(GameEvent.BoosterModeChanged, 2f);
        Assert.AreEqual(2f, received);
    }
}
```

- [ ] **Step 2: Run test — verify it fails**

Unity Test Runner → EditMode → Run All.
Expected: FAIL — `GameEvent`, `GameEventBus` not defined.

- [ ] **Step 3: Create GameEvent.cs**

Create `Assets/Scripts/Core/GameEvent.cs`:

```csharp
/// <summary>
/// All game events. Gimmicks and systems publish/subscribe through GameEventBus.
/// Add new events here as needed — no other changes required.
/// </summary>
public enum GameEvent
{
    // Pickups
    FuelPickupCollected,

    // Booster
    BoosterModeChanged,

    // Visual effects
    BlindStart,
    BlindEnd,
    ScreenShake,

    // Level
    SwitchActivated,
    RoomChanged,
    PlayerDied,
    PlayerRespawned,

    // Chapter
    ChapterLoaded
}
```

- [ ] **Step 4: Create GameEventBus.cs**

Create `Assets/Scripts/Core/GameEventBus.cs`:

```csharp
using System;
using System.Collections.Generic;

/// <summary>
/// Static event bus for decoupled pub/sub communication.
/// Supports parameterless events and single-parameter typed events.
/// </summary>
public static class GameEventBus
{
    private static readonly Dictionary<GameEvent, List<Action>> handlers = new();
    private static readonly Dictionary<GameEvent, List<Delegate>> typedHandlers = new();

    public static void Subscribe(GameEvent evt, Action handler)
    {
        if (!handlers.ContainsKey(evt))
            handlers[evt] = new List<Action>();
        handlers[evt].Add(handler);
    }

    public static void Subscribe<T>(GameEvent evt, Action<T> handler)
    {
        if (!typedHandlers.ContainsKey(evt))
            typedHandlers[evt] = new List<Delegate>();
        typedHandlers[evt].Add(handler);
    }

    public static void Unsubscribe(GameEvent evt, Action handler)
    {
        if (handlers.ContainsKey(evt))
            handlers[evt].Remove(handler);
    }

    public static void Unsubscribe<T>(GameEvent evt, Action<T> handler)
    {
        if (typedHandlers.ContainsKey(evt))
            typedHandlers[evt].Remove(handler);
    }

    public static void Publish(GameEvent evt)
    {
        if (handlers.ContainsKey(evt))
        {
            // Iterate copy to allow subscribe/unsubscribe during publish
            var list = new List<Action>(handlers[evt]);
            foreach (var handler in list)
                handler?.Invoke();
        }
    }

    public static void Publish<T>(GameEvent evt, T data)
    {
        // Fire parameterless handlers too
        Publish(evt);

        if (typedHandlers.ContainsKey(evt))
        {
            var list = new List<Delegate>(typedHandlers[evt]);
            foreach (var handler in list)
                (handler as Action<T>)?.Invoke(data);
        }
    }

    /// <summary>
    /// Clear all subscriptions. Used in tests and scene transitions.
    /// </summary>
    public static void Clear()
    {
        handlers.Clear();
        typedHandlers.Clear();
    }
}
```

- [ ] **Step 5: Run tests — verify they pass**

Unity Test Runner → EditMode → Run All.
Expected: All 6 GameEventBus tests PASS.

- [ ] **Step 6: Commit**

```bash
git checkout -b track-b/event-bus
git add Assets/Scripts/Core/ Assets/Tests/EditMode/TestGameEventBus.cs
git commit -m "feat: create GameEventBus with pub/sub for decoupled gimmick communication"
```

---

### Task 2: Create BoosterModeType and PlayerAPI

**Files:**
- Create: `Assets/Scripts/Core/BoosterModeType.cs`
- Create: `Assets/Scripts/Core/PlayerAPI.cs`

Shared types that both tracks reference. PlayerAPI is read-only — Track B reads player state without touching Player/ scripts.

- [ ] **Step 1: Create BoosterModeType.cs**

Create `Assets/Scripts/Core/BoosterModeType.cs`:

```csharp
/// <summary>
/// Shared enum for booster mode types.
/// Used by ChapterConfig (Track B) and SecondaryBooster (Track A).
/// </summary>
public enum BoosterModeType
{
    Recoil,
    Gun
}
```

- [ ] **Step 2: Create PlayerAPI.cs**

Create `Assets/Scripts/Core/PlayerAPI.cs`:

```csharp
using UnityEngine;

/// <summary>
/// Read-only facade for player state. Used by Track B systems (gimmicks, camera, level).
/// Finds the PlayerController automatically — Track B scripts never reference Player/ directly.
/// </summary>
public class PlayerAPI : MonoBehaviour
{
    private Component playerController;
    private Rigidbody2D playerRb;
    private Transform playerTransform;

    // Cached reflection — avoids direct type dependency on PlayerController
    // (so Track B doesn't need to reference Player/ assembly)
    private System.Reflection.PropertyInfo propIsGrounded;
    private System.Reflection.PropertyInfo propIsJetpacking;
    private System.Reflection.PropertyInfo propCurrentState;
    private System.Reflection.PropertyInfo propFuelPercent;
    private System.Reflection.PropertyInfo propBoostMode;

    public bool IsGrounded => (bool)(propIsGrounded?.GetValue(playerController) ?? false);
    public bool IsJetpacking => (bool)(propIsJetpacking?.GetValue(playerController) ?? false);
    public Vector2 Velocity => playerRb != null ? playerRb.linearVelocity : Vector2.zero;
    public Vector2 Position => playerTransform != null ? (Vector2)playerTransform.position : Vector2.zero;
    public float FuelPercent => (float)(propFuelPercent?.GetValue(playerController) ?? 1f);
    public bool IsValid => playerController != null;

    public static PlayerAPI Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
        FindPlayer();
    }

    private void FindPlayer()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        playerTransform = player.transform;
        playerRb = player.GetComponent<Rigidbody2D>();

        // Find PlayerController by name to avoid assembly dependency
        playerController = player.GetComponent("PlayerController");
        if (playerController != null)
        {
            var type = playerController.GetType();
            propIsGrounded = type.GetProperty("IsGrounded");
            propIsJetpacking = type.GetProperty("IsJetpacking");
            propCurrentState = type.GetProperty("CurrentState");
            propFuelPercent = type.GetProperty("FuelPercent");
            propBoostMode = type.GetProperty("BoostMode");
        }
    }
}
```

- [ ] **Step 3: Add PlayerAPI to the scene**

In Unity: Create or use an existing manager GameObject. Add `PlayerAPI` component.

- [ ] **Step 4: Verify it reads player state**

Temporarily add to PlayerAPI.Update:
```csharp
private void Update()
{
    if (playerController != null)
        Debug.Log($"PlayerAPI: grounded={IsGrounded}, jetpacking={IsJetpacking}");
}
```

Play → move around. Expected: Console shows correct state. Remove debug code after.

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/Core/BoosterModeType.cs Assets/Scripts/Core/PlayerAPI.cs
git commit -m "feat: create BoosterModeType enum and PlayerAPI read-only facade"
```

---

### Task 3: Room-Snapping Camera

**Files:**
- Rewrite: `Assets/Scripts/Camera/RoomCamera.cs`

Replace smooth-follow with Celeste-style room-snapping: camera locks to active room bounds, quick lerp on transitions.

- [ ] **Step 1: Rewrite RoomCamera.cs**

Replace the contents of `Assets/Scripts/Camera/RoomCamera.cs`:

```csharp
using UnityEngine;

/// <summary>
/// Celeste-style room-snapping camera.
/// Locks to active room center. Quick lerp on room transitions.
/// Falls back to smooth follow if no rooms exist (test level compatibility).
/// </summary>
public class RoomCamera : MonoBehaviour
{
    [SerializeField] private float transitionSpeed = 10f;
    [SerializeField] private float followSpeed = 8f;
    [SerializeField] private float zOffset = -10f;

    private Camera cam;
    private Transform target;
    private Room currentRoom;
    private Vector3 targetPosition;
    private bool inTransition;

    private void Start()
    {
        cam = GetComponent<Camera>();
        if (cam != null && cam.orthographic)
            cam.orthographicSize = 5.625f; // Half of 11.25 room height

        if (target == null)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                target = player.transform;
        }

        // Subscribe to room changes
        if (RoomManager.Instance != null)
            RoomManager.Instance.OnRoomChanged += HandleRoomChanged;
    }

    private void OnDestroy()
    {
        if (RoomManager.Instance != null)
            RoomManager.Instance.OnRoomChanged -= HandleRoomChanged;
    }

    private void HandleRoomChanged(Room oldRoom, Room newRoom)
    {
        currentRoom = newRoom;
        inTransition = true;
    }

    private void LateUpdate()
    {
        if (target == null) return;

        if (currentRoom != null)
        {
            // Room-snap mode: camera targets room center
            targetPosition = new Vector3(
                currentRoom.RoomCenter.x,
                currentRoom.RoomCenter.y,
                zOffset
            );

            float speed = inTransition ? transitionSpeed : 100f;
            transform.position = Vector3.Lerp(transform.position, targetPosition, speed * Time.deltaTime);

            // End transition when close enough
            if (inTransition && Vector3.Distance(transform.position, targetPosition) < 0.01f)
            {
                transform.position = targetPosition;
                inTransition = false;
            }
        }
        else
        {
            // Fallback: smooth follow (for test level with no rooms)
            Vector3 followPos = new Vector3(target.position.x, target.position.y, zOffset);
            transform.position = Vector3.Lerp(transform.position, followPos, followSpeed * Time.deltaTime);
        }
    }
}
```

- [ ] **Step 2: Playtest in test level**

Play in the test level (no rooms defined). Camera should fall back to smooth follow.
Expected: Same behavior as before.

- [ ] **Step 3: Commit**

```bash
git add Assets/Scripts/Camera/RoomCamera.cs
git commit -m "feat: room-snapping camera with smooth transitions and follow-mode fallback"
```

---

### Task 4: Death and Respawn System

**Files:**
- Create: `Assets/Scripts/Level/DeathHandler.cs`
- Modify: `Assets/Scripts/Level/Room.cs`

Celeste-style instant respawn. Hazard layer (10) triggers death.

- [ ] **Step 1: Verify Room.cs has spawn point**

Read `Room.cs`. It already has `[SerializeField] private Transform spawnPoint;` and `public Transform SpawnPoint => spawnPoint;`. No changes needed.

- [ ] **Step 2: Create DeathHandler.cs**

Create `Assets/Scripts/Level/DeathHandler.cs`:

```csharp
using UnityEngine;

/// <summary>
/// Handles player death and respawn. Celeste-style instant reset.
/// Triggered by Hazard layer (10) contact.
/// </summary>
public class DeathHandler : MonoBehaviour
{
    [SerializeField] private LayerMask hazardLayer = 1 << 10;

    private Transform playerTransform;
    private Rigidbody2D playerRb;

    private void Start()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
            playerRb = player.GetComponent<Rigidbody2D>();
        }
    }

    /// <summary>
    /// Call this when the player contacts a hazard.
    /// Attach a trigger collider with Hazard layer to hazard objects,
    /// and add this check to the player's collision callbacks.
    /// </summary>
    public void Die()
    {
        GameEventBus.Publish(GameEvent.PlayerDied);
        Respawn();
    }

    private void Respawn()
    {
        Room currentRoom = null;
        if (RoomManager.Instance != null)
            currentRoom = RoomManager.Instance.CurrentRoom;

        if (currentRoom != null && currentRoom.SpawnPoint != null)
        {
            playerTransform.position = currentRoom.SpawnPoint.position;
        }
        else
        {
            // Fallback: reset to origin
            playerTransform.position = Vector3.zero;
        }

        // Zero velocity for clean respawn
        if (playerRb != null)
            playerRb.linearVelocity = Vector2.zero;

        GameEventBus.Publish(GameEvent.PlayerRespawned);
    }
}
```

- [ ] **Step 3: Create hazard collision detection on the player**

Create `Assets/Scripts/Level/HazardDetector.cs`:

```csharp
using UnityEngine;

/// <summary>
/// Attach to the Player GameObject.
/// Detects contact with Hazard layer and triggers death.
/// </summary>
public class HazardDetector : MonoBehaviour
{
    private DeathHandler deathHandler;

    private void Start()
    {
        deathHandler = FindAnyObjectByType<DeathHandler>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.layer == 10) // Hazard layer
            deathHandler?.Die();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.layer == 10)
            deathHandler?.Die();
    }
}
```

- [ ] **Step 4: Update RoomManager to expose CurrentRoom**

In `RoomManager.cs`, ensure `CurrentRoom` is publicly accessible. The existing script likely tracks this already — verify and add if missing:

```csharp
public Room CurrentRoom { get; private set; }
```

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/Level/DeathHandler.cs Assets/Scripts/Level/HazardDetector.cs Assets/Scripts/Level/RoomManager.cs
git commit -m "feat: death/respawn system with hazard detection and instant room respawn"
```

---

### Task 5: Gimmick Framework

**Files:**
- Create: `Assets/Scripts/Gimmicks/IGimmick.cs`
- Create: `Assets/Scripts/Gimmicks/GimmickZone.cs`

Base framework that all gimmicks build on.

- [ ] **Step 1: Create IGimmick.cs**

Create `Assets/Scripts/Gimmicks/IGimmick.cs`:

```csharp
/// <summary>
/// Interface for environmental gimmicks.
/// Gimmicks never modify player code directly — they use
/// physics forces, GameEventBus, or PlayerAPI.
/// </summary>
public interface IGimmick
{
    /// <summary>Called when the gimmick becomes active (player enters zone, chapter loads, etc.)</summary>
    void Activate();

    /// <summary>Called when the gimmick deactivates (player leaves zone, chapter unloads, etc.)</summary>
    void Deactivate();

    /// <summary>Called each frame while active.</summary>
    void Tick(float deltaTime);
}
```

- [ ] **Step 2: Create GimmickZone.cs**

Create `Assets/Scripts/Gimmicks/GimmickZone.cs`:

```csharp
using UnityEngine;

/// <summary>
/// Trigger volume that activates a gimmick while the player is inside.
/// Attach to a GameObject with a Collider2D set to isTrigger.
/// Subclass or use with GetComponent&lt;IGimmick&gt;() to apply specific effects.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class GimmickZone : MonoBehaviour
{
    private IGimmick gimmick;
    private bool playerInside;

    protected virtual void Awake()
    {
        gimmick = GetComponent<IGimmick>() as IGimmick;

        var col = GetComponent<Collider2D>();
        if (!col.isTrigger)
            col.isTrigger = true;
    }

    private void Update()
    {
        if (playerInside && gimmick != null)
            gimmick.Tick(Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInside = true;
            gimmick?.Activate();
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInside = false;
            gimmick?.Deactivate();
        }
    }
}
```

- [ ] **Step 3: Commit**

```bash
git add Assets/Scripts/Gimmicks/
git commit -m "feat: create IGimmick interface and GimmickZone trigger volume"
```

---

### Task 6: Chapter Configuration

**Files:**
- Create: `Assets/Scripts/Level/ChapterConfig.cs`
- Create: `Assets/Scripts/Level/ChapterLoader.cs`

- [ ] **Step 1: Create ChapterConfig.cs**

Create `Assets/Scripts/Level/ChapterConfig.cs`:

```csharp
using UnityEngine;

/// <summary>
/// Per-chapter rules. Rooms and gimmicks can override these defaults.
/// </summary>
[CreateAssetMenu(fileName = "ChapterConfig", menuName = "Project Jetpack/Chapter Config")]
public class ChapterConfig : ScriptableObject
{
    [Header("Identity")]
    public string chapterName;
    public int chapterIndex;

    [Header("Booster")]
    public BoosterModeType defaultBoosterMode = BoosterModeType.Recoil;

    [Header("Fuel")]
    public float maxFuelOverride = -1f;  // -1 = use PlayerTuning default
    public float fuelDrainMultiplier = 1f;

    [Header("Audio")]
    public AudioClip musicTrack;
}
```

- [ ] **Step 2: Create ChapterLoader.cs**

Create `Assets/Scripts/Level/ChapterLoader.cs`:

```csharp
using UnityEngine;

/// <summary>
/// Applies a ChapterConfig when a chapter loads.
/// Publishes events so Track A systems (fuel, booster) can react.
/// </summary>
public class ChapterLoader : MonoBehaviour
{
    [SerializeField] private ChapterConfig config;

    public ChapterConfig CurrentConfig => config;

    public static ChapterLoader Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (config != null)
            ApplyConfig(config);
    }

    public void ApplyConfig(ChapterConfig newConfig)
    {
        config = newConfig;

        // Publish booster mode change
        GameEventBus.Publish(GameEvent.BoosterModeChanged, (float)config.defaultBoosterMode);

        // Publish chapter loaded (fuel system listens for override values)
        GameEventBus.Publish(GameEvent.ChapterLoaded);
    }
}
```

- [ ] **Step 3: Commit**

```bash
git add Assets/Scripts/Level/ChapterConfig.cs Assets/Scripts/Level/ChapterLoader.cs
git commit -m "feat: create ChapterConfig ScriptableObject and ChapterLoader"
```

---

### Task 7: Wind Turbine Gimmick

**Files:**
- Create: `Assets/Scripts/Gimmicks/WindTurbine.cs`

Applies a force vector to the player on an interval (on/off cycle).

- [ ] **Step 1: Create WindTurbine.cs**

Create `Assets/Scripts/Gimmicks/WindTurbine.cs`:

```csharp
using UnityEngine;

/// <summary>
/// Wind force gimmick. Applies directional force on a configurable on/off cycle.
/// Attach alongside GimmickZone on a trigger collider.
/// </summary>
public class WindTurbine : MonoBehaviour, IGimmick
{
    [Header("Wind")]
    [SerializeField] private Vector2 windDirection = Vector2.right;
    [SerializeField] private float windForce = 15f;

    [Header("Timing")]
    [SerializeField] private float onDuration = 2f;
    [SerializeField] private float offDuration = 1.5f;

    private float cycleTimer;
    private bool windActive = true;
    private Rigidbody2D playerRb;

    public bool IsWindActive => windActive;

    public void Activate()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            playerRb = player.GetComponent<Rigidbody2D>();
    }

    public void Deactivate()
    {
        playerRb = null;
    }

    public void Tick(float deltaTime)
    {
        // Cycle timer
        cycleTimer += deltaTime;
        float cycleLength = windActive ? onDuration : offDuration;
        if (cycleTimer >= cycleLength)
        {
            cycleTimer = 0f;
            windActive = !windActive;
        }

        // Apply force when wind is on and player is in zone
        if (windActive && playerRb != null)
        {
            playerRb.linearVelocity += windDirection.normalized * windForce * deltaTime;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(transform.position, windDirection.normalized * 3f);
    }
}
```

- [ ] **Step 2: Commit**

```bash
git add Assets/Scripts/Gimmicks/WindTurbine.cs
git commit -m "feat: create WindTurbine gimmick with on/off cycle"
```

---

### Task 8: Gravity Switch Gimmick

**Files:**
- Create: `Assets/Scripts/Gimmicks/GravitySwitch.cs`

Publishes gravity override events through the GameEventBus. GravityHandler (Track A) subscribes and applies the override. This keeps Track B from directly calling Track A methods.

**Note:** Track A must add a subscriber in GravityHandler that listens for `GameEvent.GravityOverrideStart` / `GameEvent.GravityOverrideEnd`. Add these events to `GameEvent.cs` (Task 1).

- [ ] **Step 1: Add gravity override events to GameEvent.cs**

In `Assets/Scripts/Core/GameEvent.cs`, add:
```csharp
    // Gravity
    GravityOverrideStart,  // Payload: float multiplier
    GravityOverrideEnd,
```

- [ ] **Step 2: Create GravitySwitch.cs**

Create `Assets/Scripts/Gimmicks/GravitySwitch.cs`:

```csharp
using UnityEngine;

/// <summary>
/// Overrides gravity while the player is in the zone.
/// Publishes events via GameEventBus — GravityHandler (Track A) subscribes.
/// Does NOT directly call Track A code.
/// </summary>
public class GravitySwitch : MonoBehaviour, IGimmick
{
    [SerializeField] private float gravityMultiplier = -1f; // -1 = reverse gravity

    public void Activate()
    {
        GameEventBus.Publish(GameEvent.GravityOverrideStart, gravityMultiplier);
    }

    public void Deactivate()
    {
        GameEventBus.Publish(GameEvent.GravityOverrideEnd);
    }

    public void Tick(float deltaTime)
    {
        // Gravity override is continuous while in zone — no per-frame work needed
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        var col = GetComponent<Collider2D>();
        if (col != null)
            Gizmos.DrawWireCube(transform.position, col.bounds.size);
    }
}
```

- [ ] **Step 2: Commit**

```bash
git add Assets/Scripts/Gimmicks/GravitySwitch.cs
git commit -m "feat: create GravitySwitch gimmick with priority-based gravity override"
```

---

### Task 9: Closing Platform Gimmick

**Files:**
- Create: `Assets/Scripts/Gimmicks/ClosingPlatform.cs`

Platforms that open/close on a timer. Requires burst-speed passage when open. Note: ClosingPlatform does NOT implement IGimmick because it runs its own cycle independently (not player-triggered via GimmickZone). It's always active in the room. This is intentional — not all gimmicks need the enter/exit pattern.

- [ ] **Step 1: Create ClosingPlatform.cs**

Create `Assets/Scripts/Gimmicks/ClosingPlatform.cs`:

```csharp
using UnityEngine;

/// <summary>
/// Platform that opens and closes on a timer.
/// When closed, it's solid. When open, the player can pass through.
/// Requires burst-speed timing (jetpack or secondary boost) to pass.
/// </summary>
public class ClosingPlatform : MonoBehaviour
{
    [Header("Timing")]
    [SerializeField] private float openDuration = 0.8f;
    [SerializeField] private float closedDuration = 2f;
    [SerializeField] private float startDelay = 0f;

    [Header("Visual")]
    [SerializeField] private Color openColor = new Color(1f, 1f, 1f, 0.3f);
    [SerializeField] private Color closedColor = new Color(1f, 1f, 1f, 1f);

    private Collider2D col;
    private SpriteRenderer spriteRenderer;
    private float timer;
    private bool isOpen;

    private void Awake()
    {
        col = GetComponent<Collider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        timer = -startDelay;
    }

    private void Update()
    {
        timer += Time.deltaTime;
        float cycleLength = isOpen ? openDuration : closedDuration;

        if (timer >= cycleLength)
        {
            timer = 0f;
            isOpen = !isOpen;

            if (col != null)
                col.enabled = !isOpen;

            if (spriteRenderer != null)
                spriteRenderer.color = isOpen ? openColor : closedColor;
        }
    }
}
```

- [ ] **Step 2: Commit**

```bash
git add Assets/Scripts/Gimmicks/ClosingPlatform.cs
git commit -m "feat: create ClosingPlatform gimmick with timed open/close cycle"
```

---

### Task 10: Fuel Pickup

**Files:**
- Create: `Assets/Scripts/Gimmicks/FuelPickup.cs`

Mid-air collectible that publishes refill event through GameEventBus.

- [ ] **Step 1: Create FuelPickup.cs**

Create `Assets/Scripts/Gimmicks/FuelPickup.cs`:

```csharp
using UnityEngine;

/// <summary>
/// Mid-air fuel pickup. Publishes FuelPickupCollected event.
/// JetpackGas and SecondaryBooster (Track A) subscribe and handle their own refill.
/// Respawns after a delay or on room re-entry.
/// </summary>
public class FuelPickup : MonoBehaviour
{
    [SerializeField] private float respawnDelay = 3f;

    private SpriteRenderer spriteRenderer;
    private Collider2D col;
    private bool collected;
    private float respawnTimer;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
    }

    private void Update()
    {
        if (!collected) return;

        respawnTimer -= Time.deltaTime;
        if (respawnTimer <= 0f)
            Respawn();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (collected) return;
        if (!other.CompareTag("Player")) return;

        collected = true;
        respawnTimer = respawnDelay;

        // Hide pickup
        if (spriteRenderer != null) spriteRenderer.enabled = false;
        if (col != null) col.enabled = false;

        // Publish event — Track A systems subscribe and refill
        GameEventBus.Publish(GameEvent.FuelPickupCollected);
    }

    private void Respawn()
    {
        collected = false;
        if (spriteRenderer != null) spriteRenderer.enabled = true;
        if (col != null) col.enabled = true;
    }
}
```

- [ ] **Step 2: Commit**

```bash
git add Assets/Scripts/Gimmicks/FuelPickup.cs
git commit -m "feat: create FuelPickup gimmick publishing refill event via GameEventBus"
```

---

### Task 11: Switch Target

**Files:**
- Create: `Assets/Scripts/Gimmicks/SwitchTarget.cs`

Shootable target for GunBooster mode. Triggers level events when hit.

- [ ] **Step 1: Create SwitchTarget.cs**

Create `Assets/Scripts/Gimmicks/SwitchTarget.cs`:

```csharp
using UnityEngine;
using System;

/// <summary>
/// Shootable target. Hit by projectiles to trigger level events.
/// Used with GunBooster mode.
/// </summary>
public class SwitchTarget : MonoBehaviour
{
    [SerializeField] private bool oneShot = true;
    [SerializeField] private Color activeColor = Color.red;
    [SerializeField] private Color hitColor = Color.green;

    private SpriteRenderer spriteRenderer;
    private bool activated;

    public bool IsActivated => activated;
    public event Action OnActivated;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
            spriteRenderer.color = activeColor;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (activated && oneShot) return;

        // Check if it's a projectile (tagged "SecondaryBooster" or on a projectile layer)
        if (other.CompareTag("SecondaryBooster") || other.gameObject.layer == 11)
        {
            Activate();
            Destroy(other.gameObject); // Consume the projectile
        }
    }

    private void Activate()
    {
        activated = true;
        if (spriteRenderer != null)
            spriteRenderer.color = hitColor;

        OnActivated?.Invoke();
        GameEventBus.Publish(GameEvent.SwitchActivated);
    }

    public void Reset()
    {
        activated = false;
        if (spriteRenderer != null)
            spriteRenderer.color = activeColor;
    }
}
```

- [ ] **Step 2: Commit**

```bash
git add Assets/Scripts/Gimmicks/SwitchTarget.cs
git commit -m "feat: create SwitchTarget for projectile-activated level triggers"
```

---

### Task 12: Booster Swap Zone

**Files:**
- Create: `Assets/Scripts/Gimmicks/BoosterSwapZone.cs`

Trigger zone that changes the active booster mode. Supports temporary (revert on exit) and permanent swaps.

- [ ] **Step 1: Create BoosterSwapZone.cs**

Create `Assets/Scripts/Gimmicks/BoosterSwapZone.cs`:

```csharp
using UnityEngine;

/// <summary>
/// Trigger zone that changes the player's active booster mode.
/// Supports temporary swaps (revert on exit) and permanent swaps.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class BoosterSwapZone : MonoBehaviour
{
    [SerializeField] private BoosterModeType targetMode = BoosterModeType.Gun;
    [SerializeField] private bool temporary = true;

    private void Awake()
    {
        var col = GetComponent<Collider2D>();
        if (!col.isTrigger)
            col.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        // Publish mode change event with the target mode
        // SecondaryBooster (Track A) subscribes and handles the swap
        GameEventBus.Publish(GameEvent.BoosterModeChanged, (float)targetMode);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (!temporary) return;

        // Revert: publish the chapter default mode
        var chapter = ChapterLoader.Instance;
        if (chapter != null && chapter.CurrentConfig != null)
        {
            GameEventBus.Publish(GameEvent.BoosterModeChanged,
                (float)chapter.CurrentConfig.defaultBoosterMode);
        }
        else
        {
            // Fallback to Recoil
            GameEventBus.Publish(GameEvent.BoosterModeChanged, (float)BoosterModeType.Recoil);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = targetMode == BoosterModeType.Gun
            ? new Color(1f, 0.5f, 0f, 0.3f)
            : new Color(0f, 0.5f, 1f, 0.3f);
        var col = GetComponent<Collider2D>();
        if (col != null)
            Gizmos.DrawCube(transform.position, col.bounds.size);
    }
}
```

- [ ] **Step 2: Commit**

```bash
git add Assets/Scripts/Gimmicks/BoosterSwapZone.cs
git commit -m "feat: create BoosterSwapZone for mid-room booster mode changes"
```

---

### Task 13: Screen Effects

**Files:**
- Create: `Assets/Scripts/Camera/ScreenEffects.cs`

Subscribes to visual events from GameEventBus. Handles blindness, screen shake, etc.

- [ ] **Step 1: Create ScreenEffects.cs**

Create `Assets/Scripts/Camera/ScreenEffects.cs`:

```csharp
using UnityEngine;

/// <summary>
/// Subscribes to visual events from GameEventBus.
/// Handles screen effects like blindness, shake, color grading.
/// </summary>
public class ScreenEffects : MonoBehaviour
{
    [Header("Screen Shake")]
    [SerializeField] private float shakeIntensity = 0.3f;
    [SerializeField] private float shakeDuration = 0.2f;

    [Header("Blind Effect")]
    [SerializeField] private Color blindColor = Color.black;
    [SerializeField] private float blindFadeSpeed = 3f;

    private float shakeTimer;
    private Vector3 originalCamPos;
    private bool isBlind;
    private float blindAlpha;
    private Texture2D blindTexture;

    private void Start()
    {
        GameEventBus.Subscribe(GameEvent.ScreenShake, OnScreenShake);
        GameEventBus.Subscribe(GameEvent.BlindStart, OnBlindStart);
        GameEventBus.Subscribe(GameEvent.BlindEnd, OnBlindEnd);
        GameEventBus.Subscribe(GameEvent.PlayerDied, OnPlayerDied);

        // Create 1x1 texture for blind overlay
        blindTexture = new Texture2D(1, 1);
        blindTexture.SetPixel(0, 0, Color.white);
        blindTexture.Apply();
    }

    private void OnDestroy()
    {
        GameEventBus.Unsubscribe(GameEvent.ScreenShake, OnScreenShake);
        GameEventBus.Unsubscribe(GameEvent.BlindStart, OnBlindStart);
        GameEventBus.Unsubscribe(GameEvent.BlindEnd, OnBlindEnd);
        GameEventBus.Unsubscribe(GameEvent.PlayerDied, OnPlayerDied);
    }

    private void LateUpdate()
    {
        // Screen shake
        if (shakeTimer > 0f)
        {
            shakeTimer -= Time.deltaTime;
            float intensity = shakeIntensity * (shakeTimer / shakeDuration);
            transform.position += (Vector3)Random.insideUnitCircle * intensity;
        }

        // Blind alpha
        float targetAlpha = isBlind ? 1f : 0f;
        blindAlpha = Mathf.MoveTowards(blindAlpha, targetAlpha, blindFadeSpeed * Time.deltaTime);
    }

    private void OnGUI()
    {
        if (blindAlpha > 0.01f)
        {
            GUI.color = new Color(blindColor.r, blindColor.g, blindColor.b, blindAlpha);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), blindTexture);
            GUI.color = Color.white;
        }
    }

    private void OnScreenShake() => shakeTimer = shakeDuration;
    private void OnBlindStart() => isBlind = true;
    private void OnBlindEnd() => isBlind = false;
    private void OnPlayerDied() => shakeTimer = shakeDuration * 0.5f;
}
```

- [ ] **Step 2: Attach ScreenEffects to the camera**

In Unity: Add `ScreenEffects` component to the Main Camera GameObject.

- [ ] **Step 3: Commit**

```bash
git add Assets/Scripts/Camera/ScreenEffects.cs
git commit -m "feat: create ScreenEffects subscribing to visual events (shake, blind)"
```

---

### Task 14: Merge Track B to Main

- [ ] **Step 1: Run all tests**

Unity Test Runner → EditMode → Run All.
Expected: All tests pass (GameEventBus tests + any previous tests).

- [ ] **Step 2: Verify no Player/ or UI/ files were modified**

```bash
git diff main --name-only | grep -E "^Assets/Scripts/(Player|UI)/"
```
Expected: No output (Track B never touched Track A files).

- [ ] **Step 3: Merge**

```bash
git checkout main
git merge track-b/infrastructure --no-ff -m "merge: Track B infrastructure — event bus, gimmicks, room system, chapter config"
git push
```
