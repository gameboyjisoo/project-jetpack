# Project Jetpack -- Master Architecture Document

> **Version**: 1.0
> **Date**: 2026-04-19
> **Engine**: Unity 6 (6000.0.34f1)
> **Language**: C#
> **Status**: Living document -- updated as ADRs are added or revised

---

## Table of Contents

1. [Overview](#overview)
2. [System Architecture Diagram](#system-architecture-diagram)
3. [Component Model](#component-model)
4. [Data Flow](#data-flow)
5. [Key Design Decisions (ADR Summary)](#key-design-decisions)
6. [Layer Architecture](#layer-architecture)
7. [Conventions & Constraints](#conventions--constraints)
8. [Development Tracks](#development-tracks)
9. [Open Questions](#open-questions)

---

## Overview

Project Jetpack is a 2D pixel art platformer combining **Cave Story's Booster 2.0 jetpack** with **Celeste-style room-based stages**. The core formula:

> **4-directional jetpack + chapter gimmicks**

The player traverses fixed-screen rooms using a toolkit of movement mechanics: snappy ground movement, a variable-height jump, a 4-cardinal-direction jetpack with committed trajectories, and a secondary booster with recoil-based propulsion. Each chapter introduces a new environmental gimmick that interacts with these mechanics.

### Design Pillars

- **Deterministic movement**: The player must be able to predict exactly where they will end up. Gravity is suppressed during all propelled states so trajectories are transparent.
- **Committed actions**: Boosting is a one-shot velocity assignment, not a continuous thrust. Direction is a meaningful, weighty decision.
- **Diegetic feedback**: Fuel state is communicated through particle color and audio rhythm on the character itself -- never a HUD bar. "If the player needs to look away from the character, the feedback has failed."
- **Incremental complexity**: Chapter 1 teaches the base kit; each subsequent chapter layers one gimmick on top.

### Tech Stack

| Component | Technology |
|-----------|-----------|
| Engine | Unity 6 (6000.0.34f1) |
| Language | C# |
| Physics | Physics2D, Rigidbody2D (continuous detection, interpolation) |
| Input | Unity New Input System (InputActionAsset, manual edge detection) |
| Art pipeline | 16 PPU pixel art, Point filtering, no compression |
| Project gravity | -20 (Physics2D global setting) |

---

## System Architecture Diagram

```
+========================================================================================+
|                              PROJECT JETPACK -- SYSTEM MAP                              |
+========================================================================================+

  PRESENTATION LAYER
  +------------------+   +------------------------+   +--------------------+
  | PlayerAnimator   |   | JetpackParticles       |   | JetpackAudioFback  |
  | (sprite, anim)   |   | (cyan>orange>red)      |   | (burst SFX, pitch) |
  +--------+---------+   +----------+-------------+   +---------+----------+
           |                        |                            |
           +------------+-----------+----------------------------+
                        |
                  reads state from
                        |
  CORE LAYER            v
  +=====================================================================+
  |                      PlayerController (Orchestrator)                 |
  |  Awake: Init() all components     FixedUpdate: 9-step pipeline      |
  |                                                                     |
  |  1. CheckGround        4. TryJump         7. Gravity.Tick           |
  |  2. UpdateTimers        5. Jetpack.Tick    8. ApplyVarJump          |
  |  3. HandleJumpRelease   6. Movement.Tick   9. ClampFallSpeed        |
  +====+========+========+========+========+========+===================+
       |        |        |        |        |        |
       v        v        v        v        v        v
  +--------+ +------+ +------+ +--------+ +------+ +----------------+
  | Player | |Player| |Player| | Player | |Player| | Secondary      |
  | Move-  | | Jump | | Jet- | | Gravity| | Col- | | Booster        |
  | ment   | |      | | pack | |        | |lision| | (IBoosterMode) |
  +--------+ +------+ +------+ +--------+ +------+ +----------------+
       |        |        |        |        |              |
       +--------+--------+--------+--------+--------------+
                         |
                  shared Rigidbody2D
                  shared PlayerTuning (SO)
                         |
  FOUNDATION LAYER       v
  +-----------+   +-----------+   +-----------+   +------------------+
  | Input     |   | Physics2D |   | Fuel      |   | Physics Layers   |
  | System    |   | (grav=-20)|   | (Jetpack  |   | Ground=8         |
  | (manual   |   |           |   |  Gas)     |   | Player=9         |
  |  edge det)|   |           |   |           |   | Hazard=10        |
  +-----------+   +-----------+   +-----------+   | Collectible=11   |
                                                  | RoomBoundary=12  |
                                                  +------------------+

  FEATURE LAYER (Track B -- communicates via GameEventBus)
  +----------+  +----------+  +---------+  +-----------+  +-------------+
  | Room     |  | Room     |  | Death   |  | Chapter   |  | Gimmick     |
  | Camera   |  | Manager  |  | Handler |  | Config    |  | Framework   |
  | (snap)   |  |          |  |         |  | (SO)      |  | (IGimmick)  |
  +----------+  +----------+  +---------+  +-----------+  +------+------+
                                                                 |
                              +----------------------------------+
                              |
       +----------+----------+----------+----------+-----------+----------+
       |          |          |          |          |           |          |
  +--------+ +--------+ +--------+ +--------+ +--------+ +--------+ +--------+
  | Wind   | |Gravity | |Closing | | Fuel   | |Switch  | |Booster | |Screen  |
  |Turbine | |Switch  | |Platform| |Pickup  | |Target  | |SwapZone| |Effects |
  +--------+ +--------+ +--------+ +--------+ +--------+ +--------+ +--------+

  COMMUNICATION BUS
  +=========================================================================+
  |                          GameEventBus (static pub/sub)                   |
  |  Events: FuelPickupCollected, BoosterModeChanged, BlindStart/End,       |
  |          ScreenShake, SwitchActivated, RoomChanged, PlayerDied,         |
  |          PlayerRespawned, ChapterLoaded, GravityOverride*               |
  +=========================================================================+
```

---

## Component Model

### Player GameObject Composition

All player behavior lives on a single GameObject. Components follow the single-responsibility principle ([ADR-0001](adr-0001-component-player-architecture.md)).

| Component | File | Responsibility |
|-----------|------|----------------|
| **PlayerController** | `PlayerController.cs` | Orchestrator: input reading, ground check, FixedUpdate pipeline dispatch. Single public API for external systems. |
| **PlayerMovement** | `PlayerMovement.cs` | Ground movement: Celeste-style `MoveTowards` accel/decel, air control multiplier (0.65), sprite flip |
| **PlayerJump** | `PlayerJump.cs` | Jump: coyote time (0.1s), jump buffer (0.1s), variable-height hold (0.2s), horizontal boost (2.5), apex gravity |
| **PlayerJetpack** | `PlayerJetpack.cs` | Jetpack: Booster 2.0 activation (4-cardinal, press-to-activate), fuel drain, wall nudge, end-boost velocity halving |
| **PlayerGravity** | `PlayerGravity.cs` | Gravity: dynamic scaling, fast fall (2x), apex float (0.5x), gravity suppression during propelled states |
| **SecondaryBooster** | `SecondaryBooster.cs` | Secondary boost: 8-direction dash, 3 ammo w/ ground recharge, cooldown, recoil, optional projectile. Delegates to `IBoosterMode`. |
| **JetpackGas** | `JetpackGas.cs` | Fuel system: 100 gas, 100/sec drain, recharge on landing, events for empty/recharge |
| **JetpackParticles** | `JetpackParticles.cs` | Visual feedback: exhaust color gradient (cyan > orange > red), sputter below 20% |
| **JetpackAudioFeedback** | `JetpackAudioFeedback.cs` | Audio feedback: burst SFX interval (0.08s-0.3s), pitch (1.0-0.55), jitter below 30%, dry-fire on empty |
| **PlayerAnimator** | `PlayerAnimator.cs` | Animation: drives Animator parameters from player state. Gracefully skips if no controller assigned. |

### Initialization

Components receive dependencies via `Init()` called from `PlayerController.Awake()`:

```
PlayerController.Awake()
  |-- movement.Init(rb)
  |-- jump.Init(rb)
  |-- jetpack.Init(rb, jetpackGas, groundLayer)
  |-- gravity.Init(rb)
```

All `GetComponent<T>()` calls happen once in `Awake()` and are cached. Zero per-frame `GetComponent` usage. `[RequireComponent]` enforces dependencies at edit time.

### External Access

External systems (camera, animation, UI, gimmicks) access player state through `PlayerController` public properties only:

```csharp
public bool   IsGrounded   => isGrounded;
public bool   IsJetpacking => jetpack.IsJetpacking;
public bool   FacingRight  => movement.FacingRight;
public Vector2 Velocity    => rb.linearVelocity;
```

Track B systems use `PlayerAPI` (read-only facade) or `GameEventBus` events -- never direct references to Player/ scripts.

---

## Data Flow

### Frame-Level Pipeline

```
UPDATE (every frame)
  |
  |-- Read move input (Vector2)
  |-- Edge-detect jump press/release    }  Manual: IsPressed() + wasPressed
  |-- Edge-detect jetpack press/release }  NOT WasPressedThisFrame()
  |-- Set flags for FixedUpdate
  |
FIXED UPDATE (physics tick)
  |
  |-- 1. CheckGround()          OverlapBox on layer 8
  |-- 2. UpdateTimers()          Coyote, jump buffer, var jump
  |-- 3. HandleJumpRelease()     Cancel var jump on button release
  |-- 4. TryJump()              Consume buffer if grounded/coyote
  |-- 5. Jetpack.Tick()         Activate/sustain/end boost, drain fuel
  |-- 6. Movement.Tick()         Horizontal accel/decel (skipped during boost)
  |-- 7. Gravity.Tick()         Set gravityScale based on state
  |-- 8. ApplyVarJump()          Sustain jump velocity while held
  |-- 9. ClampFallSpeed()        Hard cap on downward velocity
```

### Input Flow

```
InputActionAsset (Resources/PlayerInput.inputactions)
        |
        | Resources.Load<InputActionAsset>()
        |
        v
  Each script loads independently
  Enable() in OnEnable, Disable() in OnDisable
        |
        | action.IsPressed() every Update()
        | compare to wasPressed for edge detection
        |
        v
  Flags set in Update() --> consumed in FixedUpdate()
```

### Fuel Feedback Flow

```
JetpackGas.currentGas
        |
        | fuelNormalized (0.0 - 1.0)
        |
        +---> JetpackParticles
        |       Color: cyan (1.0) > orange (0.5) > red (0.0)
        |       Sputter: emission on/off @ 0.06s below 20%
        |
        +---> JetpackAudioFeedback
                Burst SFX interval: 0.08s (full) > 0.3s (empty)
                Pitch: 1.0 (full) > 0.55 (empty)
                Jitter: +/-0.2 below 30%
                Dry-fire click at 0%
```

### Boost Lifecycle

```
[Airborne + Fuel > 0 + Boost Key Down]
        |
        v
  Resolve direction (most recent cardinal key)
        |
        v
  Set linearVelocity = direction * 19
  Zero perpendicular axis
  Record boostMode (1=horiz, 2=up, 3=down)
  Suppress gravity (set to 0)    <-- ADR-0003 axiom
  Begin fuel drain
        |
        v
  [BOOSTING -- no per-frame velocity writes]
        |
  (key release OR fuel == 0 OR landing)
        |
        v
  Mode-specific velocity halving:
    mode 1 (horiz): vx *= 0.5
    mode 2 (up):    vy *= 0.5
    mode 3 (down):  no halving
  Restore gravity
  boostMode = 0
```

### Cross-Track Communication

```
Track A (Player/, UI/)                Track B (Level/, Camera/, Gimmicks/, Core/)
        |                                       |
        |                                       |
        +-----> PlayerController.Properties <---+ (via PlayerAPI read-only facade)
        |           (IsGrounded, Velocity, etc.)
        |                                       |
        |       GameEventBus (static pub/sub)   |
        |<====================================>|
        |  FuelPickupCollected --> Recharge gas  |
        |  BoosterModeChanged --> Swap mode      |
        |  PlayerDied --> Reset state            |
        |  GravityOverride --> Priority system   |
        +---------------------------------------+
```

---

## Key Design Decisions

Seven Architecture Decision Records define the project's technical foundation. They form a dependency chain:

```
ADR-0007 (Layers)  -->  ADR-0001 (Components)  -->  ADR-0002 (Input)
                              |
                              v
                        ADR-0003 (Gravity Axiom)
                              |
                              v
                        ADR-0004 (Boost Activation)
                              |
                              v
                        ADR-0005 (Velocity Halving)

ADR-0006 (Fuel Feedback)  -- independent, reads fuel state
```

### ADR-0001: Component-Based Player Architecture

**Decision**: Split monolithic PlayerController into 6 focused components on a single GameObject. PlayerController becomes an orchestrator dispatching a 9-step FixedUpdate pipeline.

**Why**: The monolithic class exceeded comfortable size when the secondary booster was added. Tuning friction, merge-hostile diffs, impossible-to-isolate testing.

**Key consequence**: Pipeline order is critical and must be maintained. Steps are numbered in comments. Adding a new mechanic means adding one `Tick()` call at the correct position.

> Full ADR: [adr-0001-component-player-architecture.md](adr-0001-component-player-architecture.md)

### ADR-0002: Input Timing Strategy

**Decision**: Read input in `Update()`, apply physics in `FixedUpdate()`. Use manual edge detection (`IsPressed()` + `wasPressed`), NOT `WasPressedThisFrame()`. Load InputActionAsset via `Resources.Load` -- no PlayerInput component.

**Why**: `WasPressedThisFrame()` silently drops presses depending on Input System update mode. For a precision platformer where a missed jump input means death, this is unacceptable.

**Key consequence**: Every script that detects button presses needs a `wasPressed` bool. Small boilerplate cost for zero dropped inputs.

> Full ADR: [adr-0002-input-timing-strategy.md](adr-0002-input-timing-strategy.md)

### ADR-0003: Deterministic Gravity During Propulsion

**Decision**: **Gravity = 0 during ALL propelled states.** This is a design axiom, not an implementation detail. Jetpack, secondary boost -- all suppress gravity entirely.

**Why**: If gravity modifies velocity during propelled states, the trajectory becomes opaque. The player must predict exactly where they will end up.

**State-gravity matrix**:

| Player State | Gravity |
|---|---|
| Grounded | Normal (-20) |
| Airborne, falling | 2.0x |
| Airborne, apex (holding jump, low vy) | 0.5x |
| Airborne, rising | Normal |
| **Jetpack active** | **0 (suppressed)** |
| **Secondary boost active** | **0 (suppressed)** |
| Post-upward-boost (non-jump vy > 0) | 2.0x immediately |

> Full ADR: [adr-0003-deterministic-gravity.md](adr-0003-deterministic-gravity.md)

### ADR-0004: Booster 2.0 Activation Pattern

**Decision**: Press-to-activate (not hold). 4 cardinal directions. Direction resolved from most recently pressed key (edge-detected). Velocity set ONCE to 19 units/sec in chosen direction, perpendicular axis zeroed. No per-frame velocity writes during boost.

**Why**: Matches Cave Story's Booster 2.0 exactly. The one-shot velocity creates a "committed trajectory" feel -- boost direction is a meaningful decision. Per-frame application would lose the Cave Story DNA.

> Full ADR: [adr-0004-booster-activation-pattern.md](adr-0004-booster-activation-pattern.md)

### ADR-0005: Post-Boost Velocity Halving

**Decision**: On boost termination, apply mode-specific halving: horizontal -> halve X, upward -> halve Y, downward -> no halving. Wall contact during horizontal boost applies wallNudgeSpeed (2) upward.

**Why**: Cave Story's distinct post-boost arcs per direction are what make the movement satisfying to master. Uniform halving homogenizes the feel. No halving on downward makes it the most committed direction.

> Full ADR: [adr-0005-post-boost-velocity-halving.md](adr-0005-post-boost-velocity-halving.md)

### ADR-0006: Diegetic Fuel Feedback

**Decision**: No HUD bar. Fuel state communicated through particle color (cyan > orange > red), emission sputter (below 20%), audio burst interval (0.08s-0.3s), pitch decay (1.0-0.55), pitch jitter (below 30%), and dry-fire click on empty.

**Why**: The moments when fuel information is most critical are the moments when looking away from the character is most dangerous. All feedback is anchored to the player's world-space position.

**Legacy note**: `GasMeterUI.cs` exists but is intentionally unwired. May return as an accessibility option.

> Full ADR: [adr-0006-diegetic-fuel-feedback.md](adr-0006-diegetic-fuel-feedback.md)

### ADR-0007: Physics Layer Allocation

**Decision**: Fixed layer allocation -- Ground=8, Player=9, Hazard=10, Collectible=11, RoomBoundary=12. Sorting layers: Default > Background > Tilemap > Player > Foreground > UI. Code fallback in `Awake()` forces groundLayer to layer 8 if serialized value reads as 0.

**Why**: Layer-based collision filtering happens at the physics engine level (native C++), making it faster than any script-based filtering. The serialization bug workaround ensures ground detection works regardless.

**Collision matrix** (only Player interacts with all others):

| Pair | Interaction |
|------|------------|
| Player-Ground | Physical collision |
| Player-Hazard | Trigger (kills player) |
| Player-Collectible | Trigger (pickup) |
| Player-RoomBoundary | Trigger (room transition) |
| All other pairs | No interaction |

> Full ADR: [adr-0007-physics-layer-allocation.md](adr-0007-physics-layer-allocation.md)

---

## Layer Architecture

The project's 18 systems are organized into four architectural layers. Each layer has specific rules defined in the [Control Manifest](control-manifest.md).

### Foundation Layer

Systems that everything else depends on. Changes here ripple everywhere.

| System | Owner | Key Rule |
|--------|-------|----------|
| **Input System** | ADR-0002 | Manual edge detection only. `Update()` for reads, `FixedUpdate()` for physics. |
| **Physics2D** | ADR-0007 | Gravity = -20. Layers 8-12 allocated. Static rigidbodies for platforms. |
| **Fuel System** (JetpackGas) | ADR-0006 | Normalized 0.0-1.0 output. Events for empty/recharge. |

### Core Layer

The player's movement mechanics. These define how the game feels.

| System | Owner | Key Rule |
|--------|-------|----------|
| **Gravity** | ADR-0003 | Gravity = 0 during ALL propelled states. No exceptions. |
| **Movement** | ADR-0001 | Celeste-style `MoveTowards`. Disabled during boost. |
| **Jump** | ADR-0001 | Variable height, coyote time, buffer, apex float. Mutually exclusive with jetpack. |
| **Jetpack** | ADR-0004 | Press-to-activate, 4 cardinal, velocity set once (19), perpendicular zeroed. |
| **Secondary Booster** | ADR-0001 | 8-direction dash, 3 ammo, recoil. Delegates to `IBoosterMode`. |
| **Event Bus** | Planned | Static pub/sub (`GameEventBus`). Decouples gimmicks from player. |

### Feature Layer

Level infrastructure and environmental mechanics. Communicates with Core via events only.

| System | Status | Notes |
|--------|--------|-------|
| **Camera** | Follow mode active, room-snap planned | Celeste-style one-screen-per-room |
| **Room System** | Implemented, inactive | `Room.cs`, `RoomManager.cs` |
| **Respawn** | Planned | Hazard layer (10) triggers instant respawn to room spawn point |
| **Hazards** | Planned | Trigger colliders on layer 10 |
| **Gimmick Framework** | Planned | `IGimmick` interface, `GimmickZone` trigger volumes |
| **Chapter Config** | Planned | Per-chapter rules as ScriptableObject (booster mode, fuel limits) |
| **Booster Mode Swapping** | Planned | Chapter-level, room-level, or mid-room via `BoosterSwapZone` |

### Presentation Layer

Visual and audio feedback systems. Read-only consumers of player state.

| System | Status | Notes |
|--------|--------|-------|
| **Animation** | Placeholder | `PlayerAnimator.cs` drives params; no controller connected yet |
| **Fuel Feedback** | Implemented | Particles + audio. ADR-0006. |
| **Momentum/Wavedash** | Planned | Diagonal-down recoil near ground -> horizontal speed |
| **Runtime Tuning Panel** | Planned | IMGUI debug overlay (F1 toggle) for live parameter adjustment |

---

## Conventions & Constraints

### Mandatory (enforced across all code)

| Rule | Rationale |
|------|-----------|
| `rb.linearVelocity` (never `rb.velocity`) | `velocity` is deprecated in Unity 6 |
| Input in `Update()`, physics in `FixedUpdate()` | Standard Unity pattern; FixedUpdate misses input between ticks |
| Manual edge detection (`IsPressed()` + `wasPressed`) | `WasPressedThisFrame()` drops inputs (ADR-0002) |
| `Resources.Load<InputActionAsset>("PlayerInput")` | No PlayerInput component; any script loads independently |
| Enable/Disable actions in `OnEnable()`/`OnDisable()` | Prevents input leaks |
| `[SerializeField] private` over `public` for Inspector fields | Encapsulation |
| No `Find()`, `FindObjectOfType()` in production code | Performance and coupling |
| No `SendMessage()` | Use direct references or events |
| No velocity writes in `Update()` | All physics in FixedUpdate |

### Art & Physics Constants

| Parameter | Value | Notes |
|-----------|-------|-------|
| Pixels per unit | 16 | 1 tile = 1 Unity unit |
| Sprite filtering | Point | No bilinear interpolation on pixel art |
| Sprite compression | None | Lossless pixel data |
| Project gravity | -20 | Snappier than default -9.81 |
| Ground/platform RB type | Static | Dynamic would fall |

### Tuning Values (current baseline)

| Parameter | Value | Source |
|-----------|-------|--------|
| moveSpeed | 10 | Celeste-level |
| groundAccel / groundDecel | 120 / 120 | Near-instant |
| airMult | 0.65 | Celeste's single air control multiplier |
| jumpForce | 18 | Tall, expressive jumps |
| varJumpTime | 0.2s | 12 frames at 60fps |
| jumpHBoost | 2.5 | Horizontal boost on jump |
| coyoteTime | 0.1s | Celeste's JumpGraceTime |
| jumpBufferTime | 0.1s | |
| boostSpeed | 19 | ~1.9x moveSpeed (Cave Story ratio) |
| gasConsumptionRate | 100 | ~1 second of boost |
| wallNudgeSpeed | 2 | Upward push during horizontal wall contact |
| fallGravityMultiplier | 2.0 | Fast fall |
| apexGravityMultiplier | 0.5 | Half gravity at jump peak |
| apexThreshold | 4.0 units/sec | Velocity below which apex kicks in |
| maxFallSpeed | 30 | |

These values will migrate to a `PlayerTuning` ScriptableObject (centralized, runtime-tunable).

---

## Development Tracks

Development proceeds on two parallel tracks with strict file ownership to prevent merge conflicts.

### Track A -- Feel

**Owns**: `Assets/Scripts/Player/`, `Assets/Scripts/UI/`, `Assets/Tests/`
**Branch pattern**: `track-a/<task-description>`

Focus: Refactor PlayerController into focused components, implement gravity axiom, build runtime tuning panel, nail movement feel, add momentum/wavedash system, booster mode framework.

Key deliverables:
- `PlayerTuning.cs` -- ScriptableObject centralizing ALL tuning values
- `GravityHandler.cs` -- Priority-based gravity override system
- `PlayerStateMachine.cs` -- Pure-logic state tracking
- `PlayerCollision.cs` -- Extracted ground/wall checks
- `MomentumHandler.cs` -- Wavedash and momentum conservation
- `TuningPanel.cs` -- IMGUI debug overlay
- `IBoosterMode` / `RecoilBooster` / `GunBooster` -- Swappable secondary booster modes

### Track B -- Infrastructure

**Owns**: `Assets/Scripts/Level/`, `Assets/Scripts/Camera/`, `Assets/Scripts/Gimmicks/`, `Assets/Scripts/Core/`
**Branch pattern**: `track-b/<task-description>`

Focus: Event bus, room-snapping camera, gimmick framework, chapter configuration, death/respawn.

Key deliverables:
- `GameEventBus.cs` -- Central pub/sub system
- `PlayerAPI.cs` -- Read-only player state facade for Track B
- `RoomCamera.cs` -- Celeste-style room-snap with follow-mode fallback
- `DeathHandler.cs` -- Hazard collision -> instant respawn
- `ChapterConfig.cs` -- Per-chapter rules ScriptableObject
- `IGimmick` / `GimmickZone` -- Gimmick framework
- Gimmicks: WindTurbine, GravitySwitch, ClosingPlatform, FuelPickup, SwitchTarget, BoosterSwapZone

### Cross-Track Contract

Track B **never** directly modifies player velocity or state. All player-affecting actions go through:
1. **GameEventBus** -- publish events that Track A systems subscribe to
2. **Physics forces** -- e.g., wind turbine applies force via Rigidbody2D (physics-mediated, not direct state mutation)
3. **PlayerAPI** -- read-only facade for querying player state

---

## Open Questions

### Architecture

- **Event bus scope**: Should `GameEventBus` be static (current plan) or instance-based? Static is simpler but harder to test and leaks across scenes. Needs resolution before Track B Task 1.
- **PlayerTuning hot-reload**: The ScriptableObject approach modifies the asset at runtime in the editor. Should runtime tuning use a cloned instance to avoid persisting debug values? Needs resolution before Track A Task 2.
- **SecondaryBooster pipeline integration**: SecondaryBooster currently runs its own `FixedUpdate` independently of the orchestrator pipeline (acknowledged in ADR-0001 as tech debt). When should this be folded into the main pipeline?

### Design

- **Momentum preservation ratio**: What percentage of air velocity should convert to ground speed on landing? `momentumConservationRatio` is set to 0.5 but untested.
- **Wavedash skill ceiling**: How fast should chained wavedashes let the player go? `maxWavedashSpeed = 25` is a guess. Needs playtesting.
- **Fuel pickup respawn**: Should mid-air fuel pickups respawn on a timer (current plan: 3s) or only on room re-entry?
- **Accessibility**: When should `GasMeterUI.cs` be reactivated as an accessibility toggle? What other accessibility options are needed?

### Systems Not Yet Designed

- Animation state machine and sprite sheet (blocked on original art -- Phase 5)
- Audio system beyond jetpack feedback (music, SFX library)
- Save/load system
- Title screen and menus
- Speedrun timer (Phase 4)
- B-side hard mode variant generation (Phase 6)

---

## Appendix: File Map

```
Assets/Scripts/
  Player/
    PlayerController.cs      -- Orchestrator
    PlayerMovement.cs         -- Ground movement
    PlayerJump.cs             -- Jump system
    PlayerJetpack.cs          -- Jetpack system
    PlayerGravity.cs          -- Gravity handling
    SecondaryBooster.cs       -- Secondary boost (mode host)
    JetpackGas.cs             -- Fuel system
    PlayerAnimator.cs         -- Animation driver
    JetpackParticles.cs       -- Fuel VFX
    JetpackAudioFeedback.cs   -- Fuel SFX
    Boosters/
      IBoosterMode.cs         -- Mode interface (planned)
      RecoilBooster.cs        -- Default mode (planned)
      GunBooster.cs           -- Aimed projectile mode (planned)
    PlayerTuning.cs           -- Tuning ScriptableObject (planned)
    PlayerCollision.cs        -- Ground/wall checks (planned)
    PlayerStateMachine.cs     -- State tracking (planned)
    GravityHandler.cs         -- Priority gravity overrides (planned)
    MomentumHandler.cs        -- Wavedash/momentum (planned)
  UI/
    GasMeterUI.cs             -- Legacy HUD bar (intentionally unwired)
    TuningPanel.cs            -- Debug overlay (planned)
  Core/
    GameEventBus.cs           -- Pub/sub system (planned)
    GameEvent.cs              -- Event enum (planned)
    PlayerAPI.cs              -- Read-only facade (planned)
    BoosterModeType.cs        -- Shared enum (planned)
  Camera/
    RoomCamera.cs             -- Room-snap camera
    ScreenEffects.cs          -- Visual event subscriber (planned)
  Level/
    Room.cs                   -- Room definition
    RoomManager.cs            -- Room transitions
    DeathHandler.cs           -- Hazard -> respawn (planned)
    ChapterConfig.cs          -- Per-chapter rules (planned)
    ChapterLoader.cs          -- Applies chapter config (planned)
  Gimmicks/
    IGimmick.cs               -- Gimmick interface (planned)
    GimmickZone.cs            -- Trigger volume base (planned)
    WindTurbine.cs            -- Interval force (planned)
    GravitySwitch.cs          -- Gravity override (planned)
    ClosingPlatform.cs        -- Timed open/close (planned)
    FuelPickup.cs             -- Mid-air refill (planned)
    SwitchTarget.cs           -- Shootable trigger (planned)
    BoosterSwapZone.cs        -- Mode swap zone (planned)

docs/architecture/
    architecture.md           -- This document
    adr-0001-*.md through adr-0007-*.md
    architecture-traceability.md
    control-manifest.md
```

---

## Appendix: ADR Dependency Chain

```
ADR-0007 Physics Layers (foundation, no deps)
    |
    +--> ADR-0001 Component Architecture (uses Ground layer for ground check)
    |        |
    |        +--> ADR-0002 Input Timing (enables per-component input loading)
    |
    +--> ADR-0003 Deterministic Gravity (ground detection depends on layers)
             |
             +--> ADR-0004 Booster Activation (boost overrides gravity)
                      |
                      +--> ADR-0005 Velocity Halving (reads boostMode from 0004)

ADR-0006 Diegetic Fuel Feedback (independent, reads fuel state)
```

All ADRs are **Accepted** as of 2026-04-19. No conflicts detected. See [architecture-traceability.md](architecture-traceability.md) for full requirement-to-ADR mapping.
