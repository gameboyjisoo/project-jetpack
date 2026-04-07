# Track A: Feel — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Refactor the monolithic PlayerController into focused, readable components with centralized tuning, implement the deterministic air movement axiom, and build a runtime tuning panel for iterative feel work.

**Architecture:** Extract behaviors from PlayerController.cs into single-responsibility scripts that all read from a shared PlayerTuning ScriptableObject. Add a priority-based GravityHandler that enforces gravity=0 during all propelled states. Build an IMGUI debug panel for live parameter tuning during play.

**Tech Stack:** Unity 6 (6000.0.34f1), C#, Unity New Input System, Unity Test Framework (EditMode tests for pure logic)

**Branch:** All work on `track-a/<task-description>` branches. Only touches `Assets/Scripts/Player/`, `Assets/Scripts/UI/`, `Assets/Tests/`.

**Conventions (MUST preserve):**
- `rb.linearVelocity` (Unity 6 API, not `rb.velocity`)
- Input reading in `Update`, all physics in `FixedUpdate`
- Manual edge detection (`IsPressed()` + prev frame), not `WasPressedThisFrame()`
- Input actions loaded via `Resources.Load<InputActionAsset>("PlayerInput")`

**Core Design Axiom:** Gravity = 0 during ALL jetpack directions and during secondary boost recoil window. No exceptions.

---

## File Map

| File | Action | Responsibility |
|---|---|---|
| `Assets/Scripts/Player/PlayerTuning.cs` | Create | ScriptableObject — ALL tuning values |
| `Assets/Scripts/Player/PlayerCollision.cs` | Create | Ground check, wall check, ceiling check |
| `Assets/Scripts/Player/PlayerStateMachine.cs` | Create | State enum + transitions |
| `Assets/Scripts/Player/GravityHandler.cs` | Create | Gravity math + priority override system |
| `Assets/Scripts/Player/GroundMovement.cs` | Create | Horizontal accel/decel |
| `Assets/Scripts/Player/JumpBehavior.cs` | Create | Variable jump, coyote, buffer, apex |
| `Assets/Scripts/Player/JetpackBehavior.cs` | Create | Boost activation, direction, fuel |
| `Assets/Scripts/Player/MomentumHandler.cs` | Create | Wavedash, momentum conservation |
| `Assets/Scripts/Player/PlayerController.cs` | Rewrite | Slim orchestrator |
| `Assets/Scripts/Player/JetpackGas.cs` | Modify | Read from PlayerTuning, fuel extensions |
| `Assets/Scripts/Player/SecondaryBooster.cs` | Modify | Gravity-free recoil, then mode host |
| `Assets/Scripts/Player/Boosters/IBoosterMode.cs` | Create | Interface for swappable modes |
| `Assets/Scripts/Player/Boosters/RecoilBooster.cs` | Create | Default mode (current behavior) |
| `Assets/Scripts/Player/Boosters/GunBooster.cs` | Create | Aimed projectile mode |
| `Assets/Scripts/Player/PlayerAnimator.cs` | Modify | Read from new API |
| `Assets/Scripts/Player/JetpackParticles.cs` | Modify | Read from new API |
| `Assets/Scripts/Player/JetpackAudioFeedback.cs` | Modify | Read from new API |
| `Assets/Scripts/UI/TuningPanel.cs` | Create | IMGUI debug overlay |
| `Assets/Tests/EditMode/TestGravityHandler.cs` | Create | Unit tests |
| `Assets/Tests/EditMode/TestPlayerStateMachine.cs` | Create | Unit tests |
| `docs/feel-dictionary.md` | Create | Feel term → parameter mapping |

---

### Task 1: Set Up Test Infrastructure

**Files:**
- Create: `Assets/Tests/EditMode/Tests.asmdef`
- Create: `Assets/Tests/EditMode/TestPlaceholder.cs`

This project has no test setup. Unity Test Framework needs an Assembly Definition to discover tests.

- [ ] **Step 1: Create EditMode test assembly definition**

Create `Assets/Tests/EditMode/Tests.asmdef`:

```json
{
    "name": "Tests",
    "rootNamespace": "",
    "references": [],
    "includePlatforms": [
        "Editor"
    ],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": true,
    "precompiledReferences": [
        "nunit.framework.dll"
    ],
    "autoReferenced": false,
    "defineConstraints": [
        "UNITY_INCLUDE_TESTS"
    ],
    "versionDefines": [],
    "noEngineReferences": false
}
```

- [ ] **Step 2: Create placeholder test to verify setup**

Create `Assets/Tests/EditMode/TestPlaceholder.cs`:

```csharp
using NUnit.Framework;

public class TestPlaceholder
{
    [Test]
    public void TestFrameworkWorks()
    {
        Assert.AreEqual(1, 1);
    }
}
```

- [ ] **Step 3: Verify in Unity**

Open Unity → Window → General → Test Runner → EditMode tab. Run all tests.
Expected: 1 test passes (TestFrameworkWorks).

- [ ] **Step 4: Commit**

```bash
git checkout -b track-a/test-infrastructure
git add Assets/Tests/
git commit -m "feat: set up Unity Test Framework for EditMode tests"
```

---

### Task 2: Create PlayerTuning ScriptableObject

**Files:**
- Create: `Assets/Scripts/Player/PlayerTuning.cs`
- Create: `Assets/Resources/DefaultPlayerTuning.asset` (via Unity Editor)

The single source of truth for ALL movement parameters. Every behavior script reads from this instead of having its own SerializeFields.

- [ ] **Step 1: Create PlayerTuning.cs**

Create `Assets/Scripts/Player/PlayerTuning.cs`:

```csharp
using UnityEngine;

[CreateAssetMenu(fileName = "PlayerTuning", menuName = "Project Jetpack/Player Tuning")]
public class PlayerTuning : ScriptableObject
{
    [Header("Ground Movement")]
    public float moveSpeed = 10f;
    public float groundAcceleration = 120f;
    public float groundDeceleration = 120f;
    public float airMult = 0.65f;

    [Header("Jump — Celeste")]
    public float jumpForce = 18f;
    public float varJumpTime = 0.2f;
    public float jumpHBoost = 2.5f;
    public float coyoteTime = 0.1f;
    public float jumpBufferTime = 0.1f;

    [Header("Jetpack — Booster 2.0")]
    public float boostSpeed = 19f;
    public float gasConsumptionRate = 100f;
    public float wallNudgeSpeed = 2f;
    public float maxGas = 100f;
    public float fuelDrainMultiplier = 1f;

    [Header("Gravity")]
    public float fallGravityMultiplier = 2.0f;
    public float apexGravityMultiplier = 0.5f;
    public float apexThreshold = 4f;
    public float maxFallSpeed = 30f;

    [Header("Collision")]
    public Vector2 groundCheckSize = new Vector2(0.8f, 0.05f);
    public Vector2 groundCheckOffset = new Vector2(0f, -0.5f);
    public float wallCheckDistance = 0.6f;

    [Header("Secondary Booster")]
    public int maxAmmo = 3;
    public float recoilForce = 12f;
    public float boosterCooldown = 0.15f;
    public float recoilGravityFreeWindow = 0.1f;

    [Header("Momentum")]
    public float wavedashSpeedBonus = 8f;
    public float momentumDecayRate = 5f;
    public float maxBoostedGroundSpeed = 18f;
    public float maxWavedashSpeed = 25f;
    public float momentumConservationRatio = 0.5f;
    public float wavedashGroundProximity = 1.5f;
}
```

- [ ] **Step 2: Create the default asset in Unity**

In Unity Editor: Right-click in `Assets/Resources/` → Create → Project Jetpack → Player Tuning. Name it `DefaultPlayerTuning`. Values are already set to current defaults from the code.

- [ ] **Step 3: Verify asset loads at runtime**

Temporarily add to any existing script's Awake:
```csharp
var tuning = Resources.Load<PlayerTuning>("DefaultPlayerTuning");
Debug.Log($"PlayerTuning loaded: {tuning != null}, moveSpeed={tuning?.moveSpeed}");
```

Play → check Console. Expected: `PlayerTuning loaded: True, moveSpeed=10`.
Remove the temporary code after verifying.

- [ ] **Step 4: Commit**

```bash
git add Assets/Scripts/Player/PlayerTuning.cs Assets/Resources/DefaultPlayerTuning.asset
git commit -m "feat: create PlayerTuning ScriptableObject with all movement parameters"
```

---

### Task 3: Create PlayerCollision

**Files:**
- Create: `Assets/Scripts/Player/PlayerCollision.cs`

Extracts ground check and wall check logic from PlayerController. Pure physics queries, no state management.

- [ ] **Step 1: Create PlayerCollision.cs**

Create `Assets/Scripts/Player/PlayerCollision.cs`:

```csharp
using UnityEngine;

/// <summary>
/// Handles all collision queries (ground, wall, ceiling).
/// Reads check dimensions from PlayerTuning.
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public class PlayerCollision : MonoBehaviour
{
    private PlayerTuning tuning;
    private LayerMask groundLayer;

    public bool IsGrounded { get; private set; }
    public bool WasGrounded { get; private set; }
    public bool JustLanded => IsGrounded && !WasGrounded;
    public bool JustLeftGround => !IsGrounded && WasGrounded;

    public void Initialize(PlayerTuning tuning, LayerMask groundLayer)
    {
        this.tuning = tuning;
        this.groundLayer = groundLayer;

        if (this.groundLayer.value == 0)
            this.groundLayer = 1 << 8;
    }

    /// <summary>
    /// Call once per FixedUpdate, before any movement logic.
    /// </summary>
    public void UpdateGroundCheck()
    {
        WasGrounded = IsGrounded;
        Vector2 checkPos = (Vector2)transform.position + tuning.groundCheckOffset;
        IsGrounded = Physics2D.OverlapBox(checkPos, tuning.groundCheckSize, 0f, groundLayer);
    }

    public bool IsTouchingWall(float directionX)
    {
        if (Mathf.Abs(directionX) < 0.5f) return false;
        Vector2 origin = (Vector2)transform.position;
        RaycastHit2D hit = Physics2D.Raycast(
            origin,
            new Vector2(directionX, 0f),
            tuning.wallCheckDistance,
            groundLayer
        );
        return hit.collider != null;
    }

    private void OnDrawGizmosSelected()
    {
        if (tuning == null) return;
        Gizmos.color = Color.green;
        Vector2 checkPos = (Vector2)transform.position + tuning.groundCheckOffset;
        Gizmos.DrawWireCube(checkPos, tuning.groundCheckSize);
    }
}
```

- [ ] **Step 2: Verify it compiles**

Save the file, switch to Unity. Check Console for compilation errors.
Expected: No errors (the script doesn't depend on anything not yet created).

- [ ] **Step 3: Commit**

```bash
git add Assets/Scripts/Player/PlayerCollision.cs
git commit -m "feat: extract PlayerCollision from PlayerController"
```

---

### Task 4: Create PlayerStateMachine

**Files:**
- Create: `Assets/Scripts/Player/PlayerStateMachine.cs`
- Create: `Assets/Tests/EditMode/TestPlayerStateMachine.cs`

Simple state enum with transition rules. Pure logic — no MonoBehaviour dependency for the core logic.

- [ ] **Step 1: Write the test**

Create `Assets/Tests/EditMode/TestPlayerStateMachine.cs`:

```csharp
using NUnit.Framework;

public class TestPlayerStateMachine
{
    [Test]
    public void StartsGrounded()
    {
        var sm = new PlayerStateLogic();
        Assert.AreEqual(PlayerState.Grounded, sm.Current);
    }

    [Test]
    public void GroundedToAirborne_WhenNotGrounded()
    {
        var sm = new PlayerStateLogic();
        sm.Update(isGrounded: false, isJetpacking: false, isBoosting: false);
        Assert.AreEqual(PlayerState.Airborne, sm.Current);
    }

    [Test]
    public void AirborneToJetpacking()
    {
        var sm = new PlayerStateLogic();
        sm.Update(isGrounded: false, isJetpacking: false, isBoosting: false);
        sm.Update(isGrounded: false, isJetpacking: true, isBoosting: false);
        Assert.AreEqual(PlayerState.Jetpacking, sm.Current);
    }

    [Test]
    public void JetpackingToGrounded_OnLanding()
    {
        var sm = new PlayerStateLogic();
        sm.Update(isGrounded: false, isJetpacking: true, isBoosting: false);
        sm.Update(isGrounded: true, isJetpacking: false, isBoosting: false);
        Assert.AreEqual(PlayerState.Grounded, sm.Current);
    }

    [Test]
    public void AirborneToBoosting()
    {
        var sm = new PlayerStateLogic();
        sm.Update(isGrounded: false, isJetpacking: false, isBoosting: false);
        sm.Update(isGrounded: false, isJetpacking: false, isBoosting: true);
        Assert.AreEqual(PlayerState.Boosting, sm.Current);
    }

    [Test]
    public void PreviousStateTracked()
    {
        var sm = new PlayerStateLogic();
        sm.Update(isGrounded: false, isJetpacking: false, isBoosting: false);
        Assert.AreEqual(PlayerState.Grounded, sm.Previous);
        Assert.AreEqual(PlayerState.Airborne, sm.Current);
    }
}
```

- [ ] **Step 2: Run test — verify it fails**

Unity Test Runner → EditMode → Run All.
Expected: FAIL — `PlayerState` and `PlayerStateLogic` not defined.

- [ ] **Step 3: Implement PlayerStateMachine.cs**

Create `Assets/Scripts/Player/PlayerStateMachine.cs`:

```csharp
using UnityEngine;

public enum PlayerState
{
    Grounded,
    Airborne,
    Jetpacking,
    Boosting
}

/// <summary>
/// Pure logic state machine — no MonoBehaviour dependency.
/// Testable independently.
/// </summary>
public class PlayerStateLogic
{
    public PlayerState Current { get; private set; } = PlayerState.Grounded;
    public PlayerState Previous { get; private set; } = PlayerState.Grounded;

    public bool JustChanged => Current != Previous;

    public void Update(bool isGrounded, bool isJetpacking, bool isBoosting)
    {
        Previous = Current;

        if (isGrounded && !isJetpacking)
        {
            Current = PlayerState.Grounded;
        }
        else if (isJetpacking)
        {
            Current = PlayerState.Jetpacking;
        }
        else if (isBoosting)
        {
            Current = PlayerState.Boosting;
        }
        else
        {
            Current = PlayerState.Airborne;
        }
    }
}

/// <summary>
/// MonoBehaviour wrapper for the state machine.
/// Provides Current/Previous state to other components.
/// </summary>
public class PlayerStateMachine : MonoBehaviour
{
    private PlayerStateLogic logic = new PlayerStateLogic();

    public PlayerState Current => logic.Current;
    public PlayerState Previous => logic.Previous;
    public bool JustChanged => logic.JustChanged;

    public void UpdateState(bool isGrounded, bool isJetpacking, bool isBoosting)
    {
        logic.Update(isGrounded, isJetpacking, isBoosting);
    }
}
```

- [ ] **Step 4: Run tests — verify they pass**

Unity Test Runner → EditMode → Run All.
Expected: All 6 tests PASS.

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/Player/PlayerStateMachine.cs Assets/Tests/EditMode/TestPlayerStateMachine.cs
git commit -m "feat: create PlayerStateMachine with pure-logic state tracking"
```

---

### Task 5: Create GravityHandler

**Files:**
- Create: `Assets/Scripts/Player/GravityHandler.cs`
- Create: `Assets/Tests/EditMode/TestGravityHandler.cs`

Priority-based override system. Enforces the gravity axiom: gravity=0 during propelled states.

- [ ] **Step 1: Write the test**

Create `Assets/Tests/EditMode/TestGravityHandler.cs`:

```csharp
using NUnit.Framework;

public class TestGravityHandler
{
    private GravityLogic gravity;

    [SetUp]
    public void SetUp()
    {
        gravity = new GravityLogic(
            fallMultiplier: 2f,
            apexMultiplier: 0.5f,
            apexThreshold: 4f,
            maxFallSpeed: 30f
        );
    }

    [Test]
    public void DefaultGravity_Falling_ReturnsFallMultiplier()
    {
        float scale = gravity.CalculateGravityScale(
            velocityY: -5f, holdingJump: false, didJump: false
        );
        Assert.AreEqual(2f, scale);
    }

    [Test]
    public void DefaultGravity_Apex_ReturnsApexMultiplier()
    {
        float scale = gravity.CalculateGravityScale(
            velocityY: 2f, holdingJump: true, didJump: true
        );
        Assert.AreEqual(0.5f, scale);
    }

    [Test]
    public void DefaultGravity_Rising_ReturnsNormal()
    {
        float scale = gravity.CalculateGravityScale(
            velocityY: 10f, holdingJump: true, didJump: true
        );
        Assert.AreEqual(1f, scale);
    }

    [Test]
    public void Override_HighPriority_WinsOverDefault()
    {
        gravity.SetOverride(0f, priority: 100);
        float scale = gravity.CalculateGravityScale(
            velocityY: -5f, holdingJump: false, didJump: false
        );
        Assert.AreEqual(0f, scale);
    }

    [Test]
    public void Override_HighestPriorityWins()
    {
        gravity.SetOverride(0.5f, priority: 50);
        gravity.SetOverride(0f, priority: 100);
        float scale = gravity.CalculateGravityScale(
            velocityY: -5f, holdingJump: false, didJump: false
        );
        Assert.AreEqual(0f, scale);
    }

    [Test]
    public void ClearOverride_FallsBackToDefault()
    {
        gravity.SetOverride(0f, priority: 100);
        gravity.ClearOverride(priority: 100);
        float scale = gravity.CalculateGravityScale(
            velocityY: -5f, holdingJump: false, didJump: false
        );
        Assert.AreEqual(2f, scale);
    }

    [Test]
    public void ClearOverride_FallsBackToLowerPriority()
    {
        gravity.SetOverride(0.5f, priority: 50);
        gravity.SetOverride(0f, priority: 100);
        gravity.ClearOverride(priority: 100);
        float scale = gravity.CalculateGravityScale(
            velocityY: -5f, holdingJump: false, didJump: false
        );
        Assert.AreEqual(0.5f, scale);
    }

    [Test]
    public void ClampFallSpeed_ClampsToMax()
    {
        float clamped = gravity.ClampFallSpeed(-50f);
        Assert.AreEqual(-30f, clamped);
    }

    [Test]
    public void ClampFallSpeed_DoesNotAffectRising()
    {
        float clamped = gravity.ClampFallSpeed(50f);
        Assert.AreEqual(50f, clamped);
    }
}
```

- [ ] **Step 2: Run tests — verify they fail**

Unity Test Runner → EditMode → Run All.
Expected: FAIL — `GravityLogic` not defined.

- [ ] **Step 3: Implement GravityHandler.cs**

Create `Assets/Scripts/Player/GravityHandler.cs`:

```csharp
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Pure logic for gravity calculations. Testable independently.
/// </summary>
public class GravityLogic
{
    private readonly float fallMultiplier;
    private readonly float apexMultiplier;
    private readonly float apexThreshold;
    private readonly float maxFallSpeed;

    // Priority → multiplier. Sorted by priority descending.
    private readonly SortedList<int, float> overrides = new SortedList<int, float>();

    public GravityLogic(float fallMultiplier, float apexMultiplier, float apexThreshold, float maxFallSpeed)
    {
        this.fallMultiplier = fallMultiplier;
        this.apexMultiplier = apexMultiplier;
        this.apexThreshold = apexThreshold;
        this.maxFallSpeed = maxFallSpeed;
    }

    public void SetOverride(float multiplier, int priority)
    {
        overrides[priority] = multiplier;
    }

    public void ClearOverride(int priority)
    {
        overrides.Remove(priority);
    }

    public bool HasOverride => overrides.Count > 0;

    /// <summary>
    /// Returns the gravity scale to apply this frame.
    /// If overrides are active, highest priority wins.
    /// Otherwise, Celeste-style gravity modifiers apply.
    /// </summary>
    public float CalculateGravityScale(float velocityY, bool holdingJump, bool didJump)
    {
        if (overrides.Count > 0)
        {
            // Highest priority = last key in SortedList (ascending order)
            return overrides.Values[overrides.Count - 1];
        }

        // Celeste-style gravity modifiers
        if (velocityY < 0)
        {
            return fallMultiplier;
        }
        else if (didJump && holdingJump && Mathf.Abs(velocityY) < apexThreshold)
        {
            return apexMultiplier;
        }
        else
        {
            return 1f;
        }
    }

    public float ClampFallSpeed(float velocityY)
    {
        if (velocityY < -maxFallSpeed)
            return -maxFallSpeed;
        return velocityY;
    }
}

/// <summary>
/// MonoBehaviour wrapper. Applies gravity scale to Rigidbody2D.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class GravityHandler : MonoBehaviour
{
    private Rigidbody2D rb;
    private GravityLogic logic;
    private PlayerTuning tuning;

    public void Initialize(PlayerTuning tuning)
    {
        this.tuning = tuning;
        rb = GetComponent<Rigidbody2D>();
        logic = new GravityLogic(
            tuning.fallGravityMultiplier,
            tuning.apexGravityMultiplier,
            tuning.apexThreshold,
            tuning.maxFallSpeed
        );
    }

    public void SetOverride(float multiplier, int priority)
    {
        logic.SetOverride(multiplier, priority);
    }

    public void ClearOverride(int priority)
    {
        logic.ClearOverride(priority);
    }

    /// <summary>
    /// Call in FixedUpdate after all movement logic.
    /// </summary>
    public void ApplyGravity(bool holdingJump, bool didJump)
    {
        float scale = logic.CalculateGravityScale(rb.linearVelocity.y, holdingJump, didJump);
        rb.gravityScale = scale;

        float clampedY = logic.ClampFallSpeed(rb.linearVelocity.y);
        if (clampedY != rb.linearVelocity.y)
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, clampedY);
    }
}
```

- [ ] **Step 4: Run tests — verify they pass**

Unity Test Runner → EditMode → Run All.
Expected: All 9 GravityHandler tests PASS plus previous tests.

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/Player/GravityHandler.cs Assets/Tests/EditMode/TestGravityHandler.cs
git commit -m "feat: create GravityHandler with priority-based override system"
```

---

### Task 6: Create GroundMovement

**Files:**
- Create: `Assets/Scripts/Player/GroundMovement.cs`

Extracts horizontal movement from PlayerController. Celeste-style MoveTowards with air control multiplier.

- [ ] **Step 1: Create GroundMovement.cs**

Create `Assets/Scripts/Player/GroundMovement.cs`:

```csharp
using UnityEngine;

/// <summary>
/// Horizontal ground/air movement. Celeste-style MoveTowards for crisp feel.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class GroundMovement : MonoBehaviour
{
    private Rigidbody2D rb;
    private PlayerTuning tuning;
    private bool facingRight = true;

    public bool FacingRight => facingRight;

    public void Initialize(PlayerTuning tuning)
    {
        this.tuning = tuning;
        rb = GetComponent<Rigidbody2D>();
    }

    /// <summary>
    /// Call in FixedUpdate. Skipped when jetpacking (caller's responsibility).
    /// </summary>
    public void ApplyMovement(float inputX, bool isGrounded)
    {
        float targetSpeed = inputX * tuning.moveSpeed;
        float currentSpeed = rb.linearVelocity.x;

        float mult = isGrounded ? 1f : tuning.airMult;
        float rate = Mathf.Abs(inputX) > 0.01f
            ? tuning.groundAcceleration
            : tuning.groundDeceleration;
        rate *= mult;

        float newSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, rate * Time.fixedDeltaTime);
        rb.linearVelocity = new Vector2(newSpeed, rb.linearVelocity.y);

        // Sprite flipping
        if (inputX > 0.01f && !facingRight) Flip();
        else if (inputX < -0.01f && facingRight) Flip();
    }

    private void Flip()
    {
        facingRight = !facingRight;
        Vector3 scale = transform.localScale;
        scale.x *= -1;
        transform.localScale = scale;
    }
}
```

- [ ] **Step 2: Verify compilation in Unity**

Expected: No errors.

- [ ] **Step 3: Commit**

```bash
git add Assets/Scripts/Player/GroundMovement.cs
git commit -m "feat: extract GroundMovement from PlayerController"
```

---

### Task 7: Create JumpBehavior

**Files:**
- Create: `Assets/Scripts/Player/JumpBehavior.cs`

Celeste-style variable jump with coyote time, buffer, apex float, and horizontal boost.

- [ ] **Step 1: Create JumpBehavior.cs**

Create `Assets/Scripts/Player/JumpBehavior.cs`:

```csharp
using UnityEngine;

/// <summary>
/// Celeste-style jump: variable height, coyote time, jump buffer, apex float, horizontal boost.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class JumpBehavior : MonoBehaviour
{
    private Rigidbody2D rb;
    private PlayerTuning tuning;

    // Timers
    private float coyoteTimer;
    private float jumpBufferTimer;
    private float varJumpTimer;
    private float varJumpSpeed;

    // State
    public bool DidJump { get; private set; }

    public void Initialize(PlayerTuning tuning)
    {
        this.tuning = tuning;
        rb = GetComponent<Rigidbody2D>();
    }

    /// <summary>
    /// Call at the start of FixedUpdate to update timers.
    /// </summary>
    public void UpdateTimers(bool isGrounded, bool wasGrounded, bool justLeftGround)
    {
        coyoteTimer -= Time.fixedDeltaTime;
        varJumpTimer -= Time.fixedDeltaTime;
        jumpBufferTimer -= Time.fixedDeltaTime;

        // Reset on landing
        if (isGrounded && !wasGrounded)
        {
            DidJump = false;
            varJumpTimer = 0f;
        }

        // Coyote time: start counting when leaving ground while falling
        if (justLeftGround && rb.linearVelocity.y <= 0)
            coyoteTimer = tuning.coyoteTime;
    }

    /// <summary>
    /// Buffer a jump request (called from Update via input edge detection).
    /// </summary>
    public void RequestJump()
    {
        jumpBufferTimer = tuning.jumpBufferTime;
    }

    /// <summary>
    /// Signal that jump was released (for variable jump height).
    /// </summary>
    public void ReleaseJump()
    {
        if (varJumpTimer > 0f)
            varJumpTimer = 0f;
    }

    /// <summary>
    /// Attempt to execute a jump. Call in FixedUpdate.
    /// Returns true if a jump was executed.
    /// </summary>
    public bool TryJump(bool isGrounded, float inputX, bool isJetpacking)
    {
        if (isJetpacking) return false;

        bool canJump = isGrounded || coyoteTimer > 0f;
        if (jumpBufferTimer > 0f && canJump)
        {
            float newVx = rb.linearVelocity.x + inputX * tuning.jumpHBoost;
            rb.linearVelocity = new Vector2(newVx, tuning.jumpForce);

            jumpBufferTimer = 0f;
            coyoteTimer = 0f;
            DidJump = true;
            varJumpTimer = tuning.varJumpTime;
            varJumpSpeed = tuning.jumpForce;
            return true;
        }
        return false;
    }

    /// <summary>
    /// Maintain upward velocity while holding jump within the variable jump window.
    /// Call in FixedUpdate after gravity.
    /// </summary>
    public void ApplyVarJump(bool holdingJump)
    {
        if (varJumpTimer > 0f && holdingJump && rb.linearVelocity.y >= 0f)
        {
            rb.linearVelocity = new Vector2(
                rb.linearVelocity.x,
                Mathf.Max(rb.linearVelocity.y, varJumpSpeed)
            );
        }
    }

    /// <summary>
    /// Reset jump state when jetpack activates.
    /// </summary>
    public void CancelForJetpack()
    {
        DidJump = false;
        varJumpTimer = 0f;
    }
}
```

- [ ] **Step 2: Verify compilation**

Expected: No errors.

- [ ] **Step 3: Commit**

```bash
git add Assets/Scripts/Player/JumpBehavior.cs
git commit -m "feat: extract JumpBehavior with Celeste-style variable jump"
```

---

### Task 8: Create JetpackBehavior

**Files:**
- Create: `Assets/Scripts/Player/JetpackBehavior.cs`

Booster 2.0-style jetpack with the **gravity axiom**: gravity = 0 during ALL boost directions.

- [ ] **Step 1: Create JetpackBehavior.cs**

Create `Assets/Scripts/Player/JetpackBehavior.cs`:

```csharp
using UnityEngine;
using System;

/// <summary>
/// Booster 2.0-style jetpack. 4 cardinal directions, fuel-based.
/// GRAVITY AXIOM: gravity = 0 during ALL boost directions.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class JetpackBehavior : MonoBehaviour
{
    private Rigidbody2D rb;
    private PlayerTuning tuning;
    private JetpackGas gas;
    private GravityHandler gravityHandler;
    private PlayerCollision collision;

    // Boost state
    private Vector2 jetpackDirection = Vector2.up;
    private int boostMode; // 0=off, 1=horizontal, 2=up, 3=down

    // Gravity override priority for jetpack (high)
    private const int JETPACK_GRAVITY_PRIORITY = 100;

    public bool IsJetpacking { get; private set; }
    public bool WasJetpacking { get; private set; }
    public int BoostMode => boostMode;
    public Vector2 JetpackDirection => jetpackDirection;

    /// <summary>
    /// Fired when boost ends. Listeners can check BoostMode before it resets.
    /// </summary>
    public event Action OnBoostEnded;

    public void Initialize(PlayerTuning tuning, JetpackGas gas,
        GravityHandler gravityHandler, PlayerCollision collision)
    {
        this.tuning = tuning;
        this.gas = gas;
        this.gravityHandler = gravityHandler;
        this.collision = collision;
        rb = GetComponent<Rigidbody2D>();
    }

    /// <summary>
    /// Update the most-recently-pressed cardinal direction.
    /// Call from Update with edge-detected input.
    /// </summary>
    public void UpdateDirection(Vector2 moveInput, Vector2 prevMoveInput)
    {
        bool rightNew = moveInput.x > 0.5f && prevMoveInput.x <= 0.5f;
        bool leftNew = moveInput.x < -0.5f && prevMoveInput.x >= -0.5f;
        bool upNew = moveInput.y > 0.5f && prevMoveInput.y <= 0.5f;
        bool downNew = moveInput.y < -0.5f && prevMoveInput.y >= -0.5f;

        if (rightNew) jetpackDirection = Vector2.right;
        else if (leftNew) jetpackDirection = Vector2.left;
        else if (upNew) jetpackDirection = Vector2.up;
        else if (downNew) jetpackDirection = Vector2.down;
    }

    /// <summary>
    /// Process jetpack logic. Call in FixedUpdate.
    /// </summary>
    public void ProcessJetpack(bool justPressed, bool held, bool isGrounded)
    {
        WasJetpacking = IsJetpacking;

        // Reset on landing
        if (isGrounded && IsJetpacking)
        {
            EndBoost();
            return;
        }

        // Recharge on landing
        if (isGrounded)
        {
            boostMode = 0;
            return;
        }

        // Activation: first press while airborne with gas
        if (justPressed && !isGrounded && !IsJetpacking && gas != null && gas.HasGas)
        {
            ActivateBoost();
        }

        if (!IsJetpacking) return;

        // End: released button
        if (!held)
        {
            EndBoost();
            return;
        }

        // Consume gas
        gas.ConsumeGas(tuning.gasConsumptionRate * tuning.fuelDrainMultiplier * Time.fixedDeltaTime);

        // Gas empty
        if (!gas.HasGas)
        {
            EndBoost();
            return;
        }

        // Wall nudge during horizontal boost
        if (boostMode == 1 && collision.IsTouchingWall(jetpackDirection.x))
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, tuning.wallNudgeSpeed);
    }

    private void ActivateBoost()
    {
        IsJetpacking = true;

        // GRAVITY AXIOM: gravity = 0 during ALL boost directions
        gravityHandler.SetOverride(0f, JETPACK_GRAVITY_PRIORITY);

        // Booster 2.0: instant max velocity in chosen direction, zero perpendicular axis
        if (jetpackDirection == Vector2.up)
        {
            boostMode = 2;
            rb.linearVelocity = new Vector2(0f, tuning.boostSpeed);
        }
        else if (jetpackDirection == Vector2.down)
        {
            boostMode = 3;
            rb.linearVelocity = new Vector2(0f, -tuning.boostSpeed);
        }
        else
        {
            boostMode = 1;
            rb.linearVelocity = new Vector2(jetpackDirection.x * tuning.boostSpeed, 0f);
        }
    }

    private void EndBoost()
    {
        // Booster 2.0: mode-specific velocity halving
        Vector2 vel = rb.linearVelocity;
        if (boostMode == 1)       // horizontal: halve X only
            vel.x *= 0.5f;
        else if (boostMode == 2)  // upward: halve Y only
            vel.y *= 0.5f;
        // boostMode 3 (down): no halving — matches Cave Story

        rb.linearVelocity = vel;

        // Clear gravity override
        gravityHandler.ClearOverride(JETPACK_GRAVITY_PRIORITY);

        OnBoostEnded?.Invoke();
        IsJetpacking = false;
        boostMode = 0;
    }

    /// <summary>
    /// Force-end boost (e.g., on landing). Called by orchestrator.
    /// </summary>
    public void ForceEnd()
    {
        if (IsJetpacking)
            EndBoost();
    }
}
```

- [ ] **Step 2: Verify compilation**

Expected: No errors.

- [ ] **Step 3: Verify gravity axiom in code**

Confirm: `ActivateBoost()` calls `gravityHandler.SetOverride(0f, JETPACK_GRAVITY_PRIORITY)` — this applies to ALL directions (up, down, horizontal). No per-direction branching for gravity. `EndBoost()` calls `gravityHandler.ClearOverride(JETPACK_GRAVITY_PRIORITY)`.

- [ ] **Step 4: Commit**

```bash
git add Assets/Scripts/Player/JetpackBehavior.cs
git commit -m "feat: create JetpackBehavior with gravity axiom (gravity=0 all directions)"
```

---

### Task 9: Rewrite PlayerController as Orchestrator

**Files:**
- Rewrite: `Assets/Scripts/Player/PlayerController.cs`

The monolithic PlayerController becomes a slim orchestrator: reads input, delegates to behaviors, exposes public API.

- [ ] **Step 1: Back up current PlayerController**

```bash
cp Assets/Scripts/Player/PlayerController.cs Assets/Scripts/Player/PlayerController.cs.bak
```

- [ ] **Step 2: Rewrite PlayerController.cs**

Replace the entire contents of `Assets/Scripts/Player/PlayerController.cs`:

```csharp
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Slim orchestrator: reads input, delegates to behavior components.
/// All tuning values come from PlayerTuning ScriptableObject.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(PlayerCollision))]
[RequireComponent(typeof(PlayerStateMachine))]
[RequireComponent(typeof(GravityHandler))]
[RequireComponent(typeof(GroundMovement))]
[RequireComponent(typeof(JumpBehavior))]
[RequireComponent(typeof(JetpackBehavior))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] private PlayerTuning tuning;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private InputActionAsset inputActions;

    // Components
    private Rigidbody2D rb;
    private PlayerCollision collision;
    private PlayerStateMachine stateMachine;
    private GravityHandler gravityHandler;
    private GroundMovement groundMovement;
    private JumpBehavior jumpBehavior;
    private JetpackBehavior jetpackBehavior;
    private JetpackGas jetpackGas;

    // Input actions
    private InputAction moveAction;
    private InputAction jumpAction;
    private InputAction jetpackAction;

    // Input state (read in Update, consumed in FixedUpdate)
    private Vector2 moveInput;
    private Vector2 prevMoveInput;
    private bool jetpackHeld;
    private bool prevJetpackHeld;
    private bool jetpackJustPressed;
    private bool jumpHeld;
    private bool prevJumpHeld;

    // Public API (read by Track B systems, animators, particles, audio)
    public bool IsGrounded => collision.IsGrounded;
    public bool IsJetpacking => jetpackBehavior.IsJetpacking;
    public bool FacingRight => groundMovement.FacingRight;
    public Vector2 Velocity => rb.linearVelocity;
    public PlayerState CurrentState => stateMachine.Current;
    public int BoostMode => jetpackBehavior.BoostMode;
    public bool DidJump => jumpBehavior.DidJump;
    public bool JumpHeld => jumpHeld;
    public float FuelPercent => jetpackGas != null ? jetpackGas.GasPercent : 1f;

    private void Awake()
    {
        // Load tuning
        if (tuning == null)
            tuning = Resources.Load<PlayerTuning>("DefaultPlayerTuning");

        // Get components
        rb = GetComponent<Rigidbody2D>();
        collision = GetComponent<PlayerCollision>();
        stateMachine = GetComponent<PlayerStateMachine>();
        gravityHandler = GetComponent<GravityHandler>();
        groundMovement = GetComponent<GroundMovement>();
        jumpBehavior = GetComponent<JumpBehavior>();
        jetpackBehavior = GetComponent<JetpackBehavior>();
        jetpackGas = GetComponent<JetpackGas>();

        // Configure rigidbody
        rb.gravityScale = 1f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        // Initialize components with shared tuning
        collision.Initialize(tuning, groundLayer);
        gravityHandler.Initialize(tuning);
        groundMovement.Initialize(tuning);
        jumpBehavior.Initialize(tuning);
        jetpackBehavior.Initialize(tuning, jetpackGas, gravityHandler, collision);

        // Input setup
        if (inputActions == null)
            inputActions = Resources.Load<InputActionAsset>("PlayerInput");

        if (inputActions != null)
        {
            var gameplay = inputActions.FindActionMap("Gameplay");
            moveAction = gameplay.FindAction("Move");
            jumpAction = gameplay.FindAction("Jump");
            jetpackAction = gameplay.FindAction("Jetpack");
        }
    }

    private void OnEnable() => inputActions?.Enable();
    private void OnDisable() => inputActions?.Disable();

    // --- UPDATE: Input only ---
    private void Update()
    {
        ReadMoveInput();
        ReadJumpInput();
        ReadJetpackInput();
    }

    private void ReadMoveInput()
    {
        if (moveAction == null) return;
        prevMoveInput = moveInput;
        moveInput = moveAction.ReadValue<Vector2>();
        jetpackBehavior.UpdateDirection(moveInput, prevMoveInput);
    }

    private void ReadJumpInput()
    {
        if (jumpAction == null) return;
        prevJumpHeld = jumpHeld;
        jumpHeld = jumpAction.IsPressed();

        if (jumpHeld && !prevJumpHeld)
            jumpBehavior.RequestJump();
        if (!jumpHeld && prevJumpHeld)
            jumpBehavior.ReleaseJump();
    }

    private void ReadJetpackInput()
    {
        if (jetpackAction == null) return;
        prevJetpackHeld = jetpackHeld;
        jetpackHeld = jetpackAction.IsPressed();
        if (jetpackHeld && !prevJetpackHeld)
            jetpackJustPressed = true;
    }

    // --- FIXED UPDATE: Physics ---
    private void FixedUpdate()
    {
        // 1. Collision
        collision.UpdateGroundCheck();

        // 2. Landing events
        if (collision.JustLanded)
        {
            jetpackGas?.Recharge();
            jetpackBehavior.ForceEnd();
        }

        // 3. Timers
        jumpBehavior.UpdateTimers(
            collision.IsGrounded,
            collision.WasGrounded,
            collision.JustLeftGround
        );

        // 4. Jump
        bool jumped = jumpBehavior.TryJump(
            collision.IsGrounded, moveInput.x, jetpackBehavior.IsJetpacking
        );
        if (jumped)
            jetpackBehavior.ForceEnd();

        // 5. Jetpack
        bool jPressed = jetpackJustPressed;
        jetpackJustPressed = false;
        jetpackBehavior.ProcessJetpack(jPressed, jetpackHeld, collision.IsGrounded);
        if (jetpackBehavior.IsJetpacking && !jetpackBehavior.WasJetpacking)
            jumpBehavior.CancelForJetpack();

        // 6. Horizontal movement (skipped during jetpack)
        if (!jetpackBehavior.IsJetpacking)
            groundMovement.ApplyMovement(moveInput.x, collision.IsGrounded);

        // 7. Gravity + fall clamp
        gravityHandler.ApplyGravity(jumpHeld, jumpBehavior.DidJump);

        // 8. Variable jump sustain
        jumpBehavior.ApplyVarJump(jumpHeld);

        // 9. State machine
        stateMachine.UpdateState(
            collision.IsGrounded,
            jetpackBehavior.IsJetpacking,
            false // isBoosting — wired up when SecondaryBooster is refactored
        );
    }
}
```

- [ ] **Step 3: Add required components to player GameObject in Unity**

In Unity Editor, select the Player GameObject. Add components:
- PlayerCollision
- PlayerStateMachine
- GravityHandler
- GroundMovement
- JumpBehavior
- JetpackBehavior

Assign `DefaultPlayerTuning` to the PlayerController's `tuning` field.
Set `groundLayer` to "Ground" (layer 8).

- [ ] **Step 4: Playtest**

Press Play. Verify:
- Ground movement feels identical to before (same speed, same snappiness)
- Jump works (variable height, coyote time, buffer, apex float)
- Jetpack activates in all 4 directions with gravity = 0 (no sinking during horizontal boost!)
- Fuel depletes and recharges on landing
- Wall nudge works during horizontal boost
- Velocity halving on boost release works per mode

- [ ] **Step 5: Delete backup**

```bash
rm Assets/Scripts/Player/PlayerController.cs.bak
```

- [ ] **Step 6: Commit**

```bash
git add Assets/Scripts/Player/PlayerController.cs
git commit -m "refactor: rewrite PlayerController as slim orchestrator delegating to behaviors"
```

---

### Task 10: Update JetpackGas to Use PlayerTuning

**Files:**
- Modify: `Assets/Scripts/Player/JetpackGas.cs`

Remove the hardcoded `maxGas` field. Read from PlayerTuning instead.

- [ ] **Step 1: Modify JetpackGas.cs**

Replace the contents of `Assets/Scripts/Player/JetpackGas.cs`:

```csharp
using UnityEngine;
using System;

public class JetpackGas : MonoBehaviour
{
    private PlayerTuning tuning;
    private float currentGas;

    public float CurrentGas => currentGas;
    public float MaxGas => tuning != null ? tuning.maxGas : 100f;
    public float GasPercent => currentGas / MaxGas;
    public bool HasGas => currentGas > 0f;

    public event Action<float> OnGasChanged;
    public event Action OnGasEmpty;
    public event Action OnGasRecharged;

    public void Initialize(PlayerTuning tuning)
    {
        this.tuning = tuning;
        currentGas = MaxGas;
    }

    private void Awake()
    {
        // Fallback if Initialize not called (backward compat during transition)
        if (tuning == null)
        {
            tuning = Resources.Load<PlayerTuning>("DefaultPlayerTuning");
            currentGas = MaxGas;
        }
    }

    public void ConsumeGas(float amount)
    {
        float previous = currentGas;
        currentGas = Mathf.Max(0f, currentGas - amount);
        OnGasChanged?.Invoke(GasPercent);

        if (previous > 0f && currentGas <= 0f)
            OnGasEmpty?.Invoke();
    }

    public void Recharge()
    {
        if (currentGas < MaxGas)
        {
            currentGas = MaxGas;
            OnGasChanged?.Invoke(GasPercent);
            OnGasRecharged?.Invoke();
        }
    }

    /// <summary>
    /// Mid-air pickup recharge. Can be called by event bus subscribers.
    /// </summary>
    public void RechargeFromPickup()
    {
        Recharge();
    }

    /// <summary>
    /// Apply a chapter-level fuel override (e.g., maxFuel: 40 for harder chapters).
    /// </summary>
    public void ApplyFuelOverride(float newMaxGas)
    {
        float ratio = currentGas / MaxGas;
        // tuning.maxGas is the baseline; override is applied per chapter
        currentGas = Mathf.Min(currentGas, newMaxGas);
        OnGasChanged?.Invoke(currentGas / newMaxGas);
    }
}
```

- [ ] **Step 2: Update PlayerController.Awake to initialize JetpackGas**

In `PlayerController.cs`, add after the other Initialize calls:

```csharp
jetpackGas?.Initialize(tuning);
```

(Add this line right after `jetpackBehavior.Initialize(...)`)

- [ ] **Step 3: Playtest fuel system**

Play → use jetpack → verify fuel drains and recharges on landing.
Expected: Same behavior as before.

- [ ] **Step 4: Commit**

```bash
git add Assets/Scripts/Player/JetpackGas.cs Assets/Scripts/Player/PlayerController.cs
git commit -m "refactor: JetpackGas reads from PlayerTuning instead of hardcoded maxGas"
```

---

### Task 11: Update SecondaryBooster with Gravity-Free Recoil

**Files:**
- Modify: `Assets/Scripts/Player/SecondaryBooster.cs`

Add gravity=0 window during recoil. Read values from PlayerTuning.

- [ ] **Step 1: Rewrite SecondaryBooster.cs**

Replace the contents of `Assets/Scripts/Player/SecondaryBooster.cs`:

```csharp
using UnityEngine;
using UnityEngine.InputSystem;
using System;

/// <summary>
/// Secondary Booster with gravity-free recoil window.
/// Will later become a mode host (Task 15-17).
/// </summary>
[RequireComponent(typeof(PlayerController))]
public class SecondaryBooster : MonoBehaviour
{
    [Header("Projectile (optional)")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private float projectileSpeed = 20f;
    [SerializeField] private float projectileLifetime = 0.5f;

    [Header("Input")]
    [SerializeField] private InputActionAsset inputActions;

    private PlayerTuning tuning;
    private int currentAmmo;
    private float cooldownTimer;
    private float gravityFreeTimer;
    private Rigidbody2D rb;
    private PlayerController player;
    private GravityHandler gravityHandler;
    private Vector2 aimDirection = Vector2.right;

    private InputAction moveAction;
    private InputAction fireAction;
    private bool fireHeld;
    private bool prevFireHeld;

    // Gravity override priority for booster recoil (same level as jetpack)
    private const int BOOSTER_GRAVITY_PRIORITY = 100;

    public int CurrentAmmo => currentAmmo;
    public int MaxAmmo => tuning != null ? tuning.maxAmmo : 3;
    public bool IsBoosting => gravityFreeTimer > 0f;

    public event Action<int> OnAmmoChanged;
    public event Action<Vector2> OnRecoilApplied;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        player = GetComponent<PlayerController>();
        gravityHandler = GetComponent<GravityHandler>();

        tuning = Resources.Load<PlayerTuning>("DefaultPlayerTuning");
        currentAmmo = MaxAmmo;

        if (inputActions == null)
            inputActions = Resources.Load<InputActionAsset>("PlayerInput");

        if (inputActions != null)
        {
            var gameplay = inputActions.FindActionMap("Gameplay");
            moveAction = gameplay.FindAction("Move");
            fireAction = gameplay.FindAction("Fire");
        }
    }

    private void OnEnable() => inputActions?.Enable();
    private void OnDisable() => inputActions?.Disable();

    private void Update()
    {
        cooldownTimer -= Time.deltaTime;

        // Gravity-free window countdown
        if (gravityFreeTimer > 0f)
        {
            gravityFreeTimer -= Time.deltaTime;
            if (gravityFreeTimer <= 0f)
                gravityHandler?.ClearOverride(BOOSTER_GRAVITY_PRIORITY);
        }

        // Recharge ammo on ground
        if (player.IsGrounded && currentAmmo < MaxAmmo)
        {
            currentAmmo = MaxAmmo;
            OnAmmoChanged?.Invoke(currentAmmo);
        }

        // Aim direction from input
        if (moveAction != null)
        {
            Vector2 input = moveAction.ReadValue<Vector2>();
            if (input.sqrMagnitude > 0.01f)
            {
                if (Mathf.Abs(input.x) > Mathf.Abs(input.y))
                    aimDirection = new Vector2(Mathf.Sign(input.x), 0f);
                else
                    aimDirection = new Vector2(0f, Mathf.Sign(input.y));
            }
        }

        // Fire edge detection
        if (fireAction != null)
        {
            prevFireHeld = fireHeld;
            fireHeld = fireAction.IsPressed();

            if (fireHeld && !prevFireHeld && cooldownTimer <= 0f && currentAmmo > 0)
                Fire();
        }
    }

    private void Fire()
    {
        currentAmmo--;
        cooldownTimer = tuning.boosterCooldown;
        OnAmmoChanged?.Invoke(currentAmmo);

        // Apply recoil
        Vector2 recoilDir = -aimDirection;
        Vector2 recoilVector = recoilDir * tuning.recoilForce;
        rb.linearVelocity = new Vector2(
            rb.linearVelocity.x + recoilVector.x,
            rb.linearVelocity.y + recoilVector.y
        );

        // GRAVITY AXIOM: gravity = 0 during recoil window
        gravityFreeTimer = tuning.recoilGravityFreeWindow;
        gravityHandler?.SetOverride(0f, BOOSTER_GRAVITY_PRIORITY);

        // Notify listeners (MomentumHandler uses this)
        OnRecoilApplied?.Invoke(recoilVector);

        // Spawn projectile
        if (projectilePrefab != null)
        {
            var proj = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
            var projRb = proj.GetComponent<Rigidbody2D>();
            if (projRb != null)
                projRb.linearVelocity = aimDirection * projectileSpeed;
            Destroy(proj, projectileLifetime);
        }
    }
}
```

- [ ] **Step 2: Wire IsBoosting into PlayerController's state machine**

In `PlayerController.cs`:

1. Add a cached field at the top with the other component fields:
```csharp
private SecondaryBooster secondaryBooster;
```

2. In Awake, after other GetComponent calls:
```csharp
secondaryBooster = GetComponent<SecondaryBooster>();
```

3. Update the state machine call in FixedUpdate:
```csharp
// 9. State machine
stateMachine.UpdateState(
    collision.IsGrounded,
    jetpackBehavior.IsJetpacking,
    secondaryBooster != null && secondaryBooster.IsBoosting
);
```

- [ ] **Step 3: Playtest**

Play → fire secondary booster mid-air → verify the recoil sends you on a clean vector without gravity pulling you down during the brief window.
Expected: Noticeably cleaner, more predictable recoil trajectories.

- [ ] **Step 4: Commit**

```bash
git add Assets/Scripts/Player/SecondaryBooster.cs Assets/Scripts/Player/PlayerController.cs
git commit -m "feat: secondary booster gravity-free recoil window + OnRecoilApplied event"
```

---

### Task 12: Update Feedback Scripts for New API

**Files:**
- Modify: `Assets/Scripts/Player/PlayerAnimator.cs`
- Modify: `Assets/Scripts/Player/JetpackParticles.cs`
- Modify: `Assets/Scripts/Player/JetpackAudioFeedback.cs`

These scripts read from `PlayerController` properties which still exist with the same names. Only minimal changes needed — verify they compile and work with the refactored controller.

- [ ] **Step 1: Verify PlayerAnimator.cs compiles**

Read `PlayerAnimator.cs`. It accesses `player.Velocity`, `player.IsGrounded`, `player.IsJetpacking` — all still exposed on the refactored PlayerController. No changes needed.

- [ ] **Step 2: Verify JetpackParticles.cs compiles**

Read `JetpackParticles.cs`. It accesses `player.IsJetpacking` and `jetpackGas.GasPercent` — both still available. No changes needed.

- [ ] **Step 3: Verify JetpackAudioFeedback.cs compiles**

Read `JetpackAudioFeedback.cs`. Same API surface. No changes needed.

- [ ] **Step 4: Playtest all feedback**

Play → run, jump, jetpack. Verify:
- Run animation triggers when moving
- Jetpack exhaust particles appear and color-shift with fuel
- Audio bursts fire and pitch-shift with fuel
- No console errors

- [ ] **Step 5: Commit (if any changes were needed)**

```bash
git add Assets/Scripts/Player/
git commit -m "verify: feedback scripts compatible with refactored PlayerController"
```

---

### Task 13: Build Runtime Tuning Panel

**Files:**
- Create: `Assets/Scripts/UI/TuningPanel.cs`

IMGUI debug overlay toggled with F1. Sliders for every PlayerTuning value, grouped by category.

- [ ] **Step 1: Create TuningPanel.cs**

Create `Assets/Scripts/UI/TuningPanel.cs`:

```csharp
using UnityEngine;

/// <summary>
/// Debug tuning panel. Toggle with F1.
/// Modifies PlayerTuning ScriptableObject values at runtime.
/// Changes persist in the asset during the editor session.
/// </summary>
public class TuningPanel : MonoBehaviour
{
    [SerializeField] private PlayerTuning tuning;
    [SerializeField] private KeyCode toggleKey = KeyCode.F1;

    private bool visible;
    private Vector2 scrollPos;

    // Category foldouts
    private bool showGround = true;
    private bool showJump = true;
    private bool showJetpack = true;
    private bool showGravity = true;
    private bool showBooster = true;
    private bool showMomentum = false;
    private bool showCollision = false;

    private void Awake()
    {
        if (tuning == null)
            tuning = Resources.Load<PlayerTuning>("DefaultPlayerTuning");
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
            visible = !visible;
    }

    private void OnGUI()
    {
        if (!visible || tuning == null) return;

        float panelWidth = 360f;
        float panelHeight = Screen.height - 40f;
        Rect panelRect = new Rect(Screen.width - panelWidth - 10, 20, panelWidth, panelHeight);

        GUI.Box(panelRect, "");
        GUILayout.BeginArea(panelRect);
        scrollPos = GUILayout.BeginScrollView(scrollPos);

        GUILayout.Label("<b>TUNING PANEL (F1)</b>", new GUIStyle(GUI.skin.label) { richText = true, fontSize = 14 });
        GUILayout.Space(5);

        // Ground Movement
        showGround = GUILayout.Toggle(showGround, "<b>Ground Movement</b>", new GUIStyle(GUI.skin.toggle) { richText = true });
        if (showGround)
        {
            tuning.moveSpeed = LabeledSlider("Move Speed", tuning.moveSpeed, 1f, 30f);
            tuning.groundAcceleration = LabeledSlider("Ground Accel", tuning.groundAcceleration, 10f, 300f);
            tuning.groundDeceleration = LabeledSlider("Ground Decel", tuning.groundDeceleration, 10f, 300f);
            tuning.airMult = LabeledSlider("Air Mult", tuning.airMult, 0f, 1f);
        }

        // Jump
        showJump = GUILayout.Toggle(showJump, "<b>Jump — Celeste</b>", new GUIStyle(GUI.skin.toggle) { richText = true });
        if (showJump)
        {
            tuning.jumpForce = LabeledSlider("Jump Force", tuning.jumpForce, 5f, 35f);
            tuning.varJumpTime = LabeledSlider("Var Jump Time", tuning.varJumpTime, 0f, 0.5f);
            tuning.jumpHBoost = LabeledSlider("Jump H Boost", tuning.jumpHBoost, 0f, 10f);
            tuning.coyoteTime = LabeledSlider("Coyote Time", tuning.coyoteTime, 0f, 0.3f);
            tuning.jumpBufferTime = LabeledSlider("Jump Buffer", tuning.jumpBufferTime, 0f, 0.3f);
        }

        // Jetpack
        showJetpack = GUILayout.Toggle(showJetpack, "<b>Jetpack — Booster 2.0</b>", new GUIStyle(GUI.skin.toggle) { richText = true });
        if (showJetpack)
        {
            tuning.boostSpeed = LabeledSlider("Boost Speed", tuning.boostSpeed, 5f, 40f);
            tuning.gasConsumptionRate = LabeledSlider("Gas Drain Rate", tuning.gasConsumptionRate, 10f, 300f);
            tuning.maxGas = LabeledSlider("Max Gas", tuning.maxGas, 20f, 200f);
            tuning.fuelDrainMultiplier = LabeledSlider("Drain Multiplier", tuning.fuelDrainMultiplier, 0.1f, 3f);
            tuning.wallNudgeSpeed = LabeledSlider("Wall Nudge", tuning.wallNudgeSpeed, 0f, 10f);
        }

        // Gravity
        showGravity = GUILayout.Toggle(showGravity, "<b>Gravity</b>", new GUIStyle(GUI.skin.toggle) { richText = true });
        if (showGravity)
        {
            tuning.fallGravityMultiplier = LabeledSlider("Fall Gravity", tuning.fallGravityMultiplier, 0.5f, 5f);
            tuning.apexGravityMultiplier = LabeledSlider("Apex Gravity", tuning.apexGravityMultiplier, 0f, 1f);
            tuning.apexThreshold = LabeledSlider("Apex Threshold", tuning.apexThreshold, 0.5f, 10f);
            tuning.maxFallSpeed = LabeledSlider("Max Fall Speed", tuning.maxFallSpeed, 10f, 60f);
        }

        // Secondary Booster
        showBooster = GUILayout.Toggle(showBooster, "<b>Secondary Booster</b>", new GUIStyle(GUI.skin.toggle) { richText = true });
        if (showBooster)
        {
            tuning.recoilForce = LabeledSlider("Recoil Force", tuning.recoilForce, 1f, 30f);
            tuning.boosterCooldown = LabeledSlider("Cooldown", tuning.boosterCooldown, 0f, 0.5f);
            tuning.recoilGravityFreeWindow = LabeledSlider("Gravity-Free Window", tuning.recoilGravityFreeWindow, 0f, 0.3f);
        }

        // Momentum (collapsed by default — not yet implemented)
        showMomentum = GUILayout.Toggle(showMomentum, "<b>Momentum</b>", new GUIStyle(GUI.skin.toggle) { richText = true });
        if (showMomentum)
        {
            tuning.wavedashSpeedBonus = LabeledSlider("Wavedash Bonus", tuning.wavedashSpeedBonus, 1f, 20f);
            tuning.momentumDecayRate = LabeledSlider("Momentum Decay", tuning.momentumDecayRate, 0f, 20f);
            tuning.maxBoostedGroundSpeed = LabeledSlider("Max Boosted Speed", tuning.maxBoostedGroundSpeed, 10f, 30f);
            tuning.maxWavedashSpeed = LabeledSlider("Max Wavedash Speed", tuning.maxWavedashSpeed, 15f, 40f);
            tuning.momentumConservationRatio = LabeledSlider("Momentum Conservation", tuning.momentumConservationRatio, 0f, 1f);
            tuning.wavedashGroundProximity = LabeledSlider("Wavedash Proximity", tuning.wavedashGroundProximity, 0.5f, 5f);
        }

        // Collision (collapsed by default)
        showCollision = GUILayout.Toggle(showCollision, "<b>Collision</b>", new GUIStyle(GUI.skin.toggle) { richText = true });
        if (showCollision)
        {
            tuning.wallCheckDistance = LabeledSlider("Wall Check Dist", tuning.wallCheckDistance, 0.1f, 2f);
        }

        GUILayout.Space(10);
        GUILayout.Label($"State: {FindAnyObjectByType<PlayerStateMachine>()?.Current}");

        GUILayout.EndScrollView();
        GUILayout.EndArea();
    }

    private float LabeledSlider(string label, float value, float min, float max)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label($"  {label}: {value:F2}", GUILayout.Width(200));
        float newValue = GUILayout.HorizontalSlider(value, min, max, GUILayout.Width(140));
        GUILayout.EndHorizontal();
        return newValue;
    }
}
```

- [ ] **Step 2: Attach to scene**

In Unity: Create an empty GameObject named "DebugTools". Add the `TuningPanel` component. Assign `DefaultPlayerTuning` to the `tuning` field.

- [ ] **Step 3: Playtest**

Play → press F1 → tuning panel appears on the right side. Drag sliders → values change live. Close with F1.
Expected: Movement parameters change in real time as you drag sliders.

- [ ] **Step 4: Commit**

```bash
git add Assets/Scripts/UI/TuningPanel.cs
git commit -m "feat: runtime tuning panel (F1 toggle) for live parameter adjustment"
```

---

### Task 14: Write Feel Dictionary

**Files:**
- Create: `docs/feel-dictionary.md`

Reference document mapping game-feel vocabulary to specific parameter adjustments.

- [ ] **Step 1: Create docs/feel-dictionary.md**

```markdown
# Feel Dictionary

Maps plain-language descriptions to specific PlayerTuning parameters.
Used by Claude Code during feel iteration sessions.

## Ground Movement

| Feels like... | Adjust | Direction |
|---|---|---|
| "sluggish" / "slow to start" | groundAcceleration | Increase (try 150-200) |
| "slippery" / "ice skating" | groundDeceleration | Increase (try 150-200) |
| "too snappy" / "twitchy" | groundAcceleration, groundDeceleration | Decrease slightly (try 80-100) |
| "slow" | moveSpeed | Increase (try 12-15) |
| "can't control in air" | airMult | Increase (try 0.75-0.85) |
| "too much air control" | airMult | Decrease (try 0.4-0.55) |

## Jump

| Feels like... | Adjust | Direction |
|---|---|---|
| "floaty" / "moon gravity" | jumpForce | Decrease; OR fallGravityMultiplier | Increase |
| "heavy" / "can't get height" | jumpForce | Increase (try 20-22) |
| "can't control jump height" | varJumpTime | Increase (try 0.25-0.3) |
| "jump height too binary" | varJumpTime | Decrease (try 0.12-0.15) |
| "missing ledges by a pixel" | coyoteTime | Increase (try 0.12-0.15) |
| "jump eats my input" | jumpBufferTime | Increase (try 0.12-0.15) |
| "not enough horizontal reach" | jumpHBoost | Increase (try 3-4) |
| "jump launches me sideways" | jumpHBoost | Decrease (try 1-2) |
| "hangs too long at peak" | apexGravityMultiplier | Increase toward 1.0 |
| "no hang time at peak" | apexGravityMultiplier | Decrease (try 0.3-0.4); OR apexThreshold | Increase |

## Jetpack

| Feels like... | Adjust | Direction |
|---|---|---|
| "jetpack is too fast" | boostSpeed | Decrease (try 15-17) |
| "jetpack is too slow" | boostSpeed | Increase (try 21-25) |
| "fuel runs out too fast" | gasConsumptionRate | Decrease (try 70-80); OR maxGas | Increase |
| "fuel lasts forever" | gasConsumptionRate | Increase (try 120-150) |
| "can't climb walls" | wallNudgeSpeed | Increase (try 3-5) |

## Fall

| Feels like... | Adjust | Direction |
|---|---|---|
| "falls like a rock" | fallGravityMultiplier | Decrease (try 1.5) |
| "falls too slowly" | fallGravityMultiplier | Increase (try 2.5-3.0) |
| "terminal velocity too fast" | maxFallSpeed | Decrease (try 20-25) |

## Secondary Booster

| Feels like... | Adjust | Direction |
|---|---|---|
| "recoil too weak" | recoilForce | Increase (try 15-18) |
| "recoil too violent" | recoilForce | Decrease (try 8-10) |
| "recoil drops me" | recoilGravityFreeWindow | Increase (try 0.12-0.2) |
| "recoil floats too long" | recoilGravityFreeWindow | Decrease (try 0.05-0.08) |
```

- [ ] **Step 2: Commit**

```bash
git add docs/feel-dictionary.md
git commit -m "docs: add feel dictionary mapping game-feel terms to tuning parameters"
```

---

### Task 15: Create IBoosterMode Interface + RecoilBooster

**Files:**
- Create: `Assets/Scripts/Player/Boosters/IBoosterMode.cs`
- Create: `Assets/Scripts/Player/Boosters/RecoilBooster.cs`

- [ ] **Step 1: Create the Boosters directory**

```bash
mkdir -p Assets/Scripts/Player/Boosters
```

- [ ] **Step 2: Create IBoosterMode.cs**

Create `Assets/Scripts/Player/Boosters/IBoosterMode.cs`:

```csharp
using UnityEngine;

/// <summary>
/// Interface for swappable secondary booster modes.
/// Each mode defines its own firing behavior, recoil, and ammo rules.
/// </summary>
public interface IBoosterMode
{
    /// <summary>
    /// Fire the booster. Returns the recoil vector applied (zero if no recoil).
    /// </summary>
    Vector2 Fire(Vector2 aimDirection, Vector2 playerPosition);

    /// <summary>
    /// Whether this mode produces recoil (affects momentum system).
    /// </summary>
    bool HasRecoil { get; }

    /// <summary>
    /// Current ammo / max ammo.
    /// </summary>
    int CurrentAmmo { get; }
    int MaxAmmo { get; }

    /// <summary>
    /// Whether the booster can fire right now.
    /// </summary>
    bool CanFire { get; }

    /// <summary>
    /// Called each frame to update cooldowns, etc.
    /// </summary>
    void Tick(float deltaTime);

    /// <summary>
    /// Refill ammo (called on ground contact).
    /// </summary>
    void Recharge();
}
```

- [ ] **Step 3: Create RecoilBooster.cs**

Create `Assets/Scripts/Player/Boosters/RecoilBooster.cs`:

```csharp
using UnityEngine;

/// <summary>
/// Default booster mode: fires a projectile and applies recoil.
/// Matches the current SecondaryBooster behavior.
/// </summary>
public class RecoilBooster : IBoosterMode
{
    private readonly PlayerTuning tuning;
    private readonly GameObject projectilePrefab;
    private readonly float projectileSpeed;
    private readonly float projectileLifetime;

    private int currentAmmo;
    private float cooldownTimer;

    public bool HasRecoil => true;
    public int CurrentAmmo => currentAmmo;
    public int MaxAmmo => tuning.maxAmmo;
    public bool CanFire => cooldownTimer <= 0f && currentAmmo > 0;

    public RecoilBooster(PlayerTuning tuning, GameObject projectilePrefab,
        float projectileSpeed = 20f, float projectileLifetime = 0.5f)
    {
        this.tuning = tuning;
        this.projectilePrefab = projectilePrefab;
        this.projectileSpeed = projectileSpeed;
        this.projectileLifetime = projectileLifetime;
        this.currentAmmo = tuning.maxAmmo;
    }

    public Vector2 Fire(Vector2 aimDirection, Vector2 playerPosition)
    {
        currentAmmo--;
        cooldownTimer = tuning.boosterCooldown;

        // Spawn projectile
        if (projectilePrefab != null)
        {
            var proj = Object.Instantiate(projectilePrefab, playerPosition, Quaternion.identity);
            var projRb = proj.GetComponent<Rigidbody2D>();
            if (projRb != null)
                projRb.linearVelocity = aimDirection * projectileSpeed;
            Object.Destroy(proj, projectileLifetime);
        }

        // Return recoil vector (opposite of aim)
        return -aimDirection * tuning.recoilForce;
    }

    public void Tick(float deltaTime)
    {
        cooldownTimer -= deltaTime;
    }

    public void Recharge()
    {
        currentAmmo = tuning.maxAmmo;
    }
}
```

- [ ] **Step 4: Verify compilation**

Expected: No errors.

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/Player/Boosters/
git commit -m "feat: create IBoosterMode interface and RecoilBooster (default mode)"
```

---

### Task 16: Create GunBooster

**Files:**
- Create: `Assets/Scripts/Player/Boosters/GunBooster.cs`

Aimed projectile mode for hitting SwitchTargets. Reduced recoil compared to RecoilBooster.

- [ ] **Step 1: Create GunBooster.cs**

Create `Assets/Scripts/Player/Boosters/GunBooster.cs`:

```csharp
using UnityEngine;

/// <summary>
/// Gun mode: fires aimed projectiles that hit SwitchTargets.
/// Reduced recoil compared to RecoilBooster — this is a precision tool.
/// </summary>
public class GunBooster : IBoosterMode
{
    private readonly PlayerTuning tuning;
    private readonly GameObject projectilePrefab;
    private readonly float projectileSpeed;
    private readonly float projectileLifetime;
    private readonly float recoilRatio;

    private int currentAmmo;
    private float cooldownTimer;

    public bool HasRecoil => recoilRatio > 0f;
    public int CurrentAmmo => currentAmmo;
    public int MaxAmmo => tuning.maxAmmo;
    public bool CanFire => cooldownTimer <= 0f && currentAmmo > 0;

    /// <param name="recoilRatio">0 = no recoil, 1 = same as RecoilBooster. Default 0.3 for light pushback.</param>
    public GunBooster(PlayerTuning tuning, GameObject projectilePrefab,
        float projectileSpeed = 25f, float projectileLifetime = 1f, float recoilRatio = 0.3f)
    {
        this.tuning = tuning;
        this.projectilePrefab = projectilePrefab;
        this.projectileSpeed = projectileSpeed;
        this.projectileLifetime = projectileLifetime;
        this.recoilRatio = recoilRatio;
        this.currentAmmo = tuning.maxAmmo;
    }

    public Vector2 Fire(Vector2 aimDirection, Vector2 playerPosition)
    {
        currentAmmo--;
        cooldownTimer = tuning.boosterCooldown;

        // Spawn projectile
        if (projectilePrefab != null)
        {
            var proj = Object.Instantiate(projectilePrefab, playerPosition, Quaternion.identity);
            var projRb = proj.GetComponent<Rigidbody2D>();
            if (projRb != null)
                projRb.linearVelocity = aimDirection * projectileSpeed;
            Object.Destroy(proj, projectileLifetime);
        }

        // Reduced recoil
        return -aimDirection * tuning.recoilForce * recoilRatio;
    }

    public void Tick(float deltaTime)
    {
        cooldownTimer -= deltaTime;
    }

    public void Recharge()
    {
        currentAmmo = tuning.maxAmmo;
    }
}
```

- [ ] **Step 2: Commit**

```bash
git add Assets/Scripts/Player/Boosters/GunBooster.cs
git commit -m "feat: create GunBooster mode (aimed projectile with reduced recoil)"
```

---

### Task 17: Refactor SecondaryBooster as Mode Host

**Files:**
- Modify: `Assets/Scripts/Player/SecondaryBooster.cs`

SecondaryBooster becomes a thin host that delegates to the active `IBoosterMode`.

- [ ] **Step 1: Rewrite SecondaryBooster.cs as mode host**

Replace the contents of `Assets/Scripts/Player/SecondaryBooster.cs`:

```csharp
using UnityEngine;
using UnityEngine.InputSystem;
using System;

/// <summary>
/// Host for swappable booster modes. Delegates firing to active IBoosterMode.
/// Handles input, gravity-free recoil window, and mode swapping.
/// </summary>
[RequireComponent(typeof(PlayerController))]
public class SecondaryBooster : MonoBehaviour
{
    [Header("Projectile (optional)")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private float projectileSpeed = 20f;
    [SerializeField] private float projectileLifetime = 0.5f;

    [Header("Input")]
    [SerializeField] private InputActionAsset inputActions;

    private PlayerTuning tuning;
    private Rigidbody2D rb;
    private PlayerController player;
    private GravityHandler gravityHandler;

    private IBoosterMode activeMode;
    private IBoosterMode previousMode; // for temporary swaps
    private bool isTemporarySwap;

    private Vector2 aimDirection = Vector2.right;
    private float gravityFreeTimer;

    private InputAction moveAction;
    private InputAction fireAction;
    private bool fireHeld;
    private bool prevFireHeld;

    private const int BOOSTER_GRAVITY_PRIORITY = 100;

    // Public API
    public int CurrentAmmo => activeMode?.CurrentAmmo ?? 0;
    public int MaxAmmo => activeMode?.MaxAmmo ?? 0;
    public bool IsBoosting => gravityFreeTimer > 0f;
    public bool HasRecoil => activeMode?.HasRecoil ?? false;

    public event Action<int> OnAmmoChanged;
    public event Action<Vector2> OnRecoilApplied;
    public event Action OnModeChanged;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        player = GetComponent<PlayerController>();
        gravityHandler = GetComponent<GravityHandler>();

        tuning = Resources.Load<PlayerTuning>("DefaultPlayerTuning");

        // Default mode
        activeMode = new RecoilBooster(tuning, projectilePrefab, projectileSpeed, projectileLifetime);

        if (inputActions == null)
            inputActions = Resources.Load<InputActionAsset>("PlayerInput");

        if (inputActions != null)
        {
            var gameplay = inputActions.FindActionMap("Gameplay");
            moveAction = gameplay.FindAction("Move");
            fireAction = gameplay.FindAction("Fire");
        }
    }

    private void OnEnable() => inputActions?.Enable();
    private void OnDisable() => inputActions?.Disable();

    private void Update()
    {
        activeMode?.Tick(Time.deltaTime);

        // Gravity-free window
        if (gravityFreeTimer > 0f)
        {
            gravityFreeTimer -= Time.deltaTime;
            if (gravityFreeTimer <= 0f)
                gravityHandler?.ClearOverride(BOOSTER_GRAVITY_PRIORITY);
        }

        // Recharge on ground
        if (player.IsGrounded)
            activeMode?.Recharge();

        // Aim direction
        if (moveAction != null)
        {
            Vector2 input = moveAction.ReadValue<Vector2>();
            if (input.sqrMagnitude > 0.01f)
            {
                if (Mathf.Abs(input.x) > Mathf.Abs(input.y))
                    aimDirection = new Vector2(Mathf.Sign(input.x), 0f);
                else
                    aimDirection = new Vector2(0f, Mathf.Sign(input.y));
            }
        }

        // Fire
        if (fireAction != null)
        {
            prevFireHeld = fireHeld;
            fireHeld = fireAction.IsPressed();

            if (fireHeld && !prevFireHeld && activeMode != null && activeMode.CanFire)
                Fire();
        }
    }

    private void Fire()
    {
        Vector2 recoilVector = activeMode.Fire(aimDirection, transform.position);
        OnAmmoChanged?.Invoke(activeMode.CurrentAmmo);

        // Apply recoil to player
        if (recoilVector.sqrMagnitude > 0.01f)
        {
            rb.linearVelocity = new Vector2(
                rb.linearVelocity.x + recoilVector.x,
                rb.linearVelocity.y + recoilVector.y
            );

            // Gravity-free window
            gravityFreeTimer = tuning.recoilGravityFreeWindow;
            gravityHandler?.SetOverride(0f, BOOSTER_GRAVITY_PRIORITY);

            OnRecoilApplied?.Invoke(recoilVector);
        }
    }

    /// <summary>
    /// Swap to a new booster mode.
    /// If temporary, call RevertMode() to go back.
    /// </summary>
    public void SetMode(IBoosterMode newMode, bool temporary = false)
    {
        if (temporary)
        {
            previousMode = activeMode;
            isTemporarySwap = true;
        }
        else
        {
            previousMode = null;
            isTemporarySwap = false;
        }

        activeMode = newMode;
        OnModeChanged?.Invoke();
        OnAmmoChanged?.Invoke(activeMode.CurrentAmmo);
    }

    /// <summary>
    /// Revert to the previous mode (after a temporary swap).
    /// </summary>
    public void RevertMode()
    {
        if (isTemporarySwap && previousMode != null)
        {
            activeMode = previousMode;
            previousMode = null;
            isTemporarySwap = false;
            OnModeChanged?.Invoke();
            OnAmmoChanged?.Invoke(activeMode.CurrentAmmo);
        }
    }
}
```

- [ ] **Step 2: Playtest**

Play → fire secondary booster → verify recoil works identically to before (RecoilBooster is the default mode). Gravity-free window should still apply.

- [ ] **Step 3: Commit**

```bash
git add Assets/Scripts/Player/SecondaryBooster.cs
git commit -m "refactor: SecondaryBooster as mode host with SetMode/RevertMode support"
```

---

### Task 18: Create MomentumHandler

**Files:**
- Create: `Assets/Scripts/Player/MomentumHandler.cs`

Wavedash and momentum conservation. Listens to `OnRecoilApplied` from SecondaryBooster.

- [ ] **Step 1: Create MomentumHandler.cs**

Create `Assets/Scripts/Player/MomentumHandler.cs`:

```csharp
using UnityEngine;

/// <summary>
/// Handles wavedash (recoil → ground speed conversion) and momentum conservation
/// (air velocity → ground speed on landing).
/// Listens to OnRecoilApplied — decoupled from specific booster modes.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(PlayerController))]
public class MomentumHandler : MonoBehaviour
{
    private Rigidbody2D rb;
    private PlayerTuning tuning;
    private PlayerCollision collision;
    private SecondaryBooster booster;

    private float currentBoostedSpeed;
    private bool wasGrounded;

    public float CurrentBoostedSpeed => currentBoostedSpeed;

    public void Initialize(PlayerTuning tuning, PlayerCollision collision)
    {
        this.tuning = tuning;
        this.collision = collision;
        rb = GetComponent<Rigidbody2D>();
        booster = GetComponent<SecondaryBooster>();

        if (booster != null)
            booster.OnRecoilApplied += HandleRecoil;
    }

    private void OnDestroy()
    {
        if (booster != null)
            booster.OnRecoilApplied -= HandleRecoil;
    }

    private void FixedUpdate()
    {
        if (tuning == null) return;

        // Momentum conservation: convert air velocity to ground speed on landing
        if (collision.IsGrounded && !wasGrounded)
        {
            float airSpeedX = Mathf.Abs(rb.linearVelocity.x);
            if (airSpeedX > tuning.moveSpeed)
            {
                float bonus = (airSpeedX - tuning.moveSpeed) * tuning.momentumConservationRatio;
                currentBoostedSpeed = Mathf.Min(
                    tuning.moveSpeed + bonus,
                    tuning.maxBoostedGroundSpeed
                );
            }
        }

        wasGrounded = collision.IsGrounded;

        // Decay boosted speed back to normal
        if (collision.IsGrounded && currentBoostedSpeed > tuning.moveSpeed)
        {
            currentBoostedSpeed = Mathf.MoveTowards(
                currentBoostedSpeed,
                tuning.moveSpeed,
                tuning.momentumDecayRate * Time.fixedDeltaTime
            );
        }
        else if (collision.IsGrounded)
        {
            currentBoostedSpeed = 0f;
        }
    }

    private void HandleRecoil(Vector2 recoilVector)
    {
        // Wavedash: diagonal-downward recoil near ground → horizontal speed bonus
        bool nearGround = IsNearGround();
        bool diagonalDown = recoilVector.y < -0.1f && Mathf.Abs(recoilVector.x) > 0.1f;

        if (nearGround && diagonalDown)
        {
            // Convert vertical recoil component into horizontal speed
            float horizontalBonus = Mathf.Abs(recoilVector.y) * 0.7f + Mathf.Abs(recoilVector.x);
            float direction = Mathf.Sign(recoilVector.x);

            float newSpeed = rb.linearVelocity.x + direction * horizontalBonus;
            newSpeed = Mathf.Clamp(newSpeed, -tuning.maxWavedashSpeed, tuning.maxWavedashSpeed);

            rb.linearVelocity = new Vector2(newSpeed, rb.linearVelocity.y);
            currentBoostedSpeed = Mathf.Min(Mathf.Abs(newSpeed), tuning.maxWavedashSpeed);
        }
    }

    private bool IsNearGround()
    {
        if (collision.IsGrounded) return true;

        // Check slightly below player for "near ground" detection
        Vector2 checkPos = (Vector2)transform.position + tuning.groundCheckOffset;
        checkPos.y -= tuning.wavedashGroundProximity;
        return Physics2D.OverlapBox(
            checkPos,
            tuning.groundCheckSize,
            0f,
            1 << 8 // Ground layer
        );
    }
}
```

- [ ] **Step 2: Wire into PlayerController**

In `PlayerController.cs`, add the MomentumHandler component requirement and initialization:

Add to the class attributes:
```csharp
[RequireComponent(typeof(MomentumHandler))]
```

Add field:
```csharp
private MomentumHandler momentumHandler;
```

In Awake, after other GetComponent calls:
```csharp
momentumHandler = GetComponent<MomentumHandler>();
```

After other Initialize calls:
```csharp
momentumHandler?.Initialize(tuning, collision);
```

- [ ] **Step 3: Playtest wavedash**

Play → jump → aim diagonally downward → fire secondary booster near the ground.
Expected: Player gets a horizontal speed boost. Chain multiple for increasing speed.

- [ ] **Step 4: Commit**

```bash
git add Assets/Scripts/Player/MomentumHandler.cs Assets/Scripts/Player/PlayerController.cs
git commit -m "feat: create MomentumHandler with wavedash and momentum conservation"
```

---

### Task 19: Merge Track A to Main

- [ ] **Step 1: Run all tests**

Unity Test Runner → EditMode → Run All.
Expected: All tests pass.

- [ ] **Step 2: Full playtest checklist**

- [ ] Ground movement: run, stop, direction change — snappy
- [ ] Jump: variable height, coyote time, buffer, apex float
- [ ] Jetpack: all 4 directions, gravity = 0 during all
- [ ] Fuel: drains, recharges on landing, particles + audio shift
- [ ] Secondary booster: recoil, gravity-free window, ammo recharge
- [ ] Wavedash: diagonal-down recoil near ground → speed boost
- [ ] Momentum: fast air → landing preserves some speed
- [ ] Tuning panel: F1 toggle, sliders work, values change live
- [ ] Wall nudge: horizontal jetpack into wall → climb
- [ ] No console errors

- [ ] **Step 3: Merge**

```bash
git checkout main
git merge track-a/refactor --no-ff -m "merge: Track A refactor — behaviors, tuning, gravity axiom, momentum"
git push
```

- [ ] **Step 4: Delete backup branch**

```bash
git branch -d track-a/refactor
```
