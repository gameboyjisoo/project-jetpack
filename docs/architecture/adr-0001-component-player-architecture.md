# ADR-0001: Component-Based Player Architecture

## Status

**Accepted**

## Date

2026-04-14

## Engine Compatibility

- **Engine**: Unity 6 (6000.0.34f1)
- **Language**: C#
- **Pattern**: MonoBehaviour composition (multiple components on a single GameObject)
- **Physics**: Rigidbody2D with continuous collision detection and interpolation
- **Input**: Unity Input System (InputActionAsset, action maps)

## ADR Dependencies

None. This is the foundational architecture decision for the player system. All future player-related ADRs depend on this one.

---

## Context

Project Jetpack is a 2D pixel art platformer combining Cave Story's Booster 2.0 jetpack with Celeste-style room-based stage design. The player character has five distinct movement subsystems -- ground movement, jumping, jetpacking, gravity handling, and a secondary booster dash -- each with its own tuning parameters, edge cases, and interaction rules.

The original implementation placed all of this logic inside a single monolithic `PlayerController` class. As the movement feel matured, this caused several problems:

1. **Tuning friction**: Changing jetpack wall-nudge speed required scrolling past jump buffer logic and gravity scaling. Designers (including the solo developer) could not quickly locate the parameter they needed.
2. **Merge-hostile**: Any change to any movement subsystem touched the same file, making version control diffs noisy and hard to review.
3. **Testing difficulty**: Testing coyote time in isolation was impossible because it was tangled with jetpack activation guards and gravity state.
4. **Growing complexity**: The secondary booster (8-direction dash with ammo, cooldown, and optional projectile) added yet another mode to an already-large file, pushing it past comfortable comprehension limits.
5. **Execution order sensitivity**: The player's FixedUpdate pipeline has a strict order dependency (ground check must happen before jump, jetpack must tick before gravity). In a monolithic class this order was implicit and easy to accidentally break during refactors.

The team needed an architecture that:
- Keeps each subsystem independently tunable in the Unity Inspector
- Makes the FixedUpdate execution order explicit and centrally managed
- Allows adding new movement mechanics (e.g., wavedash, booster mode swapping) without modifying existing components
- Remains simple enough for a solo developer to maintain without framework overhead

---

## Decision

Split the monolithic PlayerController into **6 focused single-responsibility components**, all attached to the same GameObject. The refactored PlayerController acts as an **orchestrator** that reads input, performs the ground check, and calls each component's `Tick()` method in a strict deterministic order during FixedUpdate.

### Component Breakdown

| Component | File | Responsibility |
|---|---|---|
| `PlayerController` | `PlayerController.cs` | Orchestrator: input reading, ground check, FixedUpdate pipeline dispatch |
| `PlayerMovement` | `PlayerMovement.cs` | Ground movement: Celeste-style `MoveTowards` accel/decel, air control multiplier, sprite flip |
| `PlayerJump` | `PlayerJump.cs` | Jump system: coyote time, jump buffer, variable-height jump hold, horizontal jump boost |
| `PlayerJetpack` | `PlayerJetpack.cs` | Jetpack system: Booster 2.0 activation (4-direction lock-on), fuel drain, wall nudge, end-boost velocity halving |
| `PlayerGravity` | `PlayerGravity.cs` | Gravity handling: dynamic gravity scaling, fast fall, apex float, gravity suppression timer |
| `SecondaryBooster` | `SecondaryBooster.cs` | Secondary boost: 8-direction fixed-distance dash, ammo with ground recharge, cooldown, optional reverse projectile |

### FixedUpdate Pipeline Order

The orchestrator (`PlayerController.FixedUpdate`) calls subsystems in this exact order:

```
1. CheckGround()           -- Ground overlap box, triggers OnLand events, starts coyote time
2. UpdateTimers()          -- Decrements coyote timer, jump buffer timer, variable jump timer
3. HandleJumpRelease()     -- Cancels variable jump hold when button released
4. TryJump()              -- Consumes jump buffer if grounded/coyote, applies force + h-boost
5. Jetpack.Tick()         -- Activates/sustains/ends boost, drains fuel, wall nudge
6. Movement.Tick()        -- Horizontal accel/decel (skipped during jetpack or secondary boost)
7. Gravity.Tick()         -- Sets gravityScale based on state (falling, apex, suppressed)
8. ApplyVarJump()         -- Sustains jump velocity while button held and timer active
9. ClampFallSpeed()       -- Hard cap on downward velocity
```

This ordering is critical. For example:
- Ground check (step 1) must run before TryJump (step 4) so coyote time is accurate.
- Jetpack.Tick (step 5) must run before Movement.Tick (step 6) so movement can early-return when jetpacking.
- Gravity.Tick (step 7) must run before ClampFallSpeed (step 9) so the gravity multiplier applies before the speed cap.

### Initialization Pattern

Each component receives its dependencies via an `Init()` method called in `PlayerController.Awake()`:

```csharp
movement.Init(rb);
jump.Init(rb);
jetpack.Init(rb, jetpackGas, groundLayer);
gravity.Init(rb);
```

Components are retrieved via `GetComponent<T>()`. Unity's `[RequireComponent]` attribute enforces that required components are always present on the GameObject.

### External Access Pattern

External systems (camera, animation, UI) access player state through properties on `PlayerController`:

```csharp
public bool IsGrounded => isGrounded;
public bool IsJetpacking => jetpack.IsJetpacking;
public bool FacingRight => movement.FacingRight;
public Vector2 Velocity => rb.linearVelocity;
public PlayerGravity Gravity => gravity;
```

This keeps `PlayerController` as the single public interface while internal components remain implementation details.

---

## Alternatives Considered

### Alternative 1: Keep Monolithic PlayerController

**Description**: Leave all movement logic in a single `PlayerController.cs` class.

**Pros**:
- Simplest to understand for a single developer -- everything is in one place
- No inter-component communication overhead
- No initialization ceremony

**Cons**:
- File grows unbounded as mechanics are added (wavedash, booster mode swapping, etc.)
- Tuning any subsystem requires navigating the entire file
- Impossible to test subsystems in isolation
- Merge conflicts on every change

**Rejection reason**: Already proven painful during initial development. The file exceeded comfortable size when the secondary booster was added.

### Alternative 2: Inheritance Hierarchy (PlayerBase with Subclasses)

**Description**: Create a `PlayerBase` class with protected methods, then subclasses like `GroundPlayer`, `JetpackPlayer`, `DashPlayer`.

**Pros**:
- Familiar OOP pattern
- Shared state accessible via `protected` fields

**Cons**:
- Unity MonoBehaviours do not support multiple inheritance; a GameObject can only have one script of each type active at a time
- The player needs all movement modes simultaneously, not one-at-a-time
- Deep inheritance trees are notoriously fragile ("fragile base class" problem)
- Violates composition-over-inheritance principle that Unity's component model is built around

**Rejection reason**: Fundamental mismatch with Unity's component architecture and the game's requirement that all movement systems coexist on a single character.

### Alternative 3: ScriptableObject-Based State Machine

**Description**: Define player states (Grounded, Airborne, Jetpacking, Dashing) as ScriptableObjects with `Enter/Tick/Exit` methods. A state machine driver selects the active state.

**Pros**:
- Clean state transitions with explicit enter/exit logic
- States are assets that can be swapped or configured in the Inspector
- Well-suited for characters with mutually exclusive modes

**Cons**:
- Project Jetpack's movement systems are **not** mutually exclusive in all cases (gravity runs during jumping, movement runs during falling, etc.)
- Adds framework complexity (state machine driver, transition rules, state asset management)
- Overkill for a solo-developer project at the current scale
- Harder to debug: state transitions are implicit rather than a readable top-down pipeline

**Rejection reason**: The player's subsystems overlap rather than forming discrete exclusive states. A flat pipeline with explicit ordering is simpler and more transparent for this project's needs.

---

## Consequences

### Positive

- **Independent tuning**: Each component exposes its own `[SerializeField]` parameters in the Inspector. Tweaking jetpack boost speed does not require touching jump code.
- **Explicit execution order**: The FixedUpdate pipeline in `PlayerController` reads top-to-bottom. Any developer can see exactly what runs when.
- **Open for extension**: New mechanics (wavedash, wall climb) can be added as new components without modifying existing ones. The orchestrator adds one `Tick()` call at the appropriate pipeline position.
- **Testable in isolation**: Each component's `Tick()` method can be called directly in unit tests with a mock Rigidbody2D.
- **Inspector-friendly**: Each component appears as a separate foldout in the Unity Inspector, making playtesting parameter sweeps faster.
- **Small diffs**: Changes to the jump system only touch `PlayerJump.cs`, producing clean version control history.

### Negative

- **Indirection cost**: Understanding the full player behavior requires reading 6 files instead of 1. New contributors must learn the pipeline order.
- **Init ceremony**: Components need explicit `Init()` calls rather than finding their own dependencies. Forgetting an `Init()` call produces null reference errors at runtime.
- **Coupling through orchestrator**: `PlayerController` still knows about all components and their calling conventions. It is a coordination point, not fully decoupled.
- **Shared Rigidbody2D**: Multiple components write to the same `rb.linearVelocity`. Ordering bugs (e.g., gravity overwriting jetpack velocity) are possible if the pipeline order is changed carelessly.

---

## Risks

| Risk | Likelihood | Impact | Mitigation |
|------|-----------|--------|------------|
| Pipeline order accidentally changed during refactor | Medium | High (broken movement feel) | Pipeline steps are numbered in comments; integration tests verify expected velocities after known input sequences |
| New component writes to velocity at wrong pipeline stage | Medium | Medium (subtle physics bugs) | Document pipeline contract; code review checklist for new components |
| `Init()` call forgotten for new component | Low | High (NullReferenceException on play) | `[RequireComponent]` catches missing components at edit time; null checks in `Init()` log errors early |
| Secondary Booster runs its own FixedUpdate independently | Low | Medium (order mismatch with main pipeline) | Acknowledged tech debt; future refactor should move SecondaryBooster.FixedUpdate logic into the orchestrator pipeline |

---

## Performance

- **Component count**: 6 MonoBehaviours + Rigidbody2D + BoxCollider2D on one GameObject. Unity handles this with negligible overhead for a single player character.
- **GetComponent calls**: All `GetComponent<T>()` calls happen once in `Awake()` and are cached. Zero per-frame `GetComponent` usage.
- **Physics**: Single `Physics2D.OverlapBox` per FixedUpdate for ground check. One `Physics2D.Raycast` per FixedUpdate during jetpack wall check. Both are lightweight for a single-player 2D game.
- **Allocations**: No per-frame heap allocations in any component's `Tick()` method. All state is stored in value-type fields.
- **Scaling concern**: None at current scope. This architecture is for a single player character, not hundreds of NPCs. If enemy AI needed similar decomposition, a data-oriented approach (ECS or struct-based) would be more appropriate.

---

## Migration Plan

Completed 2026-04-14. The migration followed these steps:

1. **Extract PlayerMovement**: Moved ground accel/decel and sprite flip logic out of monolithic PlayerController into `PlayerMovement.cs`. Verified movement feel was identical.
2. **Extract PlayerJump**: Moved coyote time, jump buffer, variable jump hold, and horizontal boost into `PlayerJump.cs`. Verified all jump edge cases (buffered jump, coyote jump, short hop, full hop).
3. **Extract PlayerJetpack**: Moved Booster 2.0 activation, fuel drain, direction lock-on, wall nudge, and end-boost halving into `PlayerJetpack.cs`. Verified all 4 boost directions and wall nudge behavior.
4. **Extract PlayerGravity**: Moved gravity scaling, fast fall, apex float, and suppression timer into `PlayerGravity.cs`. Verified apex hang time and fall speed cap.
5. **Refactor PlayerController**: Reduced to orchestrator role -- input reading, ground check, and FixedUpdate pipeline dispatch.
6. **Verify SecondaryBooster integration**: Confirmed SecondaryBooster communicates via `PlayerController` properties and `PlayerGravity.SuppressGravity()`.
7. **Playtest**: Full playtest of all movement mechanics to confirm no regressions.

---

## Validation

- [ ] Each component compiles and runs independently when `Init()` is called with valid references
- [ ] FixedUpdate pipeline order matches the documented 9-step sequence
- [ ] Ground check correctly triggers `OnLand()` on all relevant components (jetpack, jump, gravity, fuel)
- [ ] Coyote time activates only when walking off a ledge (not after jumping)
- [ ] Jump buffer works across the FixedUpdate boundary (input in Update, consumption in FixedUpdate)
- [ ] Jetpack suppresses gravity and overrides movement during boost
- [ ] Secondary booster suppresses gravity for its duration and blocks horizontal movement
- [ ] End-boost velocity halving applies to the correct axis (x for horizontal, y for upward)
- [ ] No `GetComponent` calls occur outside of `Awake()`
- [ ] All `[SerializeField]` parameters are visible and correctly labeled in the Inspector

---

## GDD Requirements Addressed

This architecture was designed to support the gameplay systems documented in the following design docs (listed in `design/gdd/systems-index.md`):

| Design Doc | System | How This ADR Supports It |
|---|---|---|
| `design/gdd/player-movement.md` | Player Movement (System #1) | `PlayerMovement.cs` encapsulates Celeste-style MoveTowards accel/decel with configurable ground speed, acceleration, deceleration, and air control multiplier |
| `design/gdd/player-jump.md` | Player Jump (System #2) | `PlayerJump.cs` encapsulates coyote time, jump buffer, variable-height hold, and horizontal boost -- all independently tunable |
| `design/gdd/jetpack-system.md` | Jetpack System (System #3) | `PlayerJetpack.cs` encapsulates Booster 2.0 directional lock-on, fuel integration via `JetpackGas`, wall nudge, and end-boost velocity halving |
| `design/gdd/secondary-booster.md` | Secondary Booster (System #4) | `SecondaryBooster.cs` encapsulates 8-direction dash, ammo/cooldown, gravity suppression, and optional reverse projectile |

The component architecture ensures that as these design docs evolve, each system can be tuned and iterated on independently without risk of regressions in unrelated systems.

---

## References

- Source files: `Assets/Scripts/Player/PlayerController.cs`, `PlayerMovement.cs`, `PlayerJump.cs`, `PlayerJetpack.cs`, `PlayerGravity.cs`, `SecondaryBooster.cs`
- Systems index: `design/gdd/systems-index.md`
- Unity 6 MonoBehaviour documentation: https://docs.unity3d.com/6000.0/Documentation/ScriptReference/MonoBehaviour.html
