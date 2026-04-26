# Project Jetpack — Granular Development Plan Design

**Date:** 2026-04-07
**Status:** Approved (Largely executed as of 2026-04-26. Track A refactor complete, Track B room system + event bus + camera complete. See CLAUDE.md for current state.)
**Approach:** Parallel Tracks (Track A: Feel, Track B: Infrastructure)

---

## 1. Overview

A granular development plan for Project Jetpack, a 2D pixel art platformer in Unity 6 combining Celeste-style ground movement with Cave Story Booster 2.0-style jetpack air traversal. The plan is structured as two independent workstreams that can run in parallel Claude Code sessions, with **Track A (Feel) taking hard priority** since all level design and gimmick work depends on finalized movement feel.

### Core Design Axiom

**Deterministic air movement.** When the jetpack or secondary boost is active, gravity = 0. The player must know exactly where they'll end up. Gravity only applies during unpropelled airborne states (with Celeste-style apex float and fast fall modifiers).

---

## 2. Track A — Feel (Priority)

Track A owns: `Assets/Scripts/Player/`, `Assets/Scripts/UI/`

### A1. PlayerController Refactor

Break the monolithic `PlayerController.cs` into focused, readable components:

| File | Responsibility |
|---|---|
| `PlayerController.cs` | Slim orchestrator — reads input, delegates to behaviors |
| `PlayerStateMachine.cs` | State tracking: Grounded, Airborne, Jetpacking, Boosting |
| `GroundMovement.cs` | Horizontal accel/decel, direction changes |
| `JumpBehavior.cs` | Variable jump, coyote time, buffer, apex float |
| `JetpackBehavior.cs` | Boost activation, directional logic, fuel integration |
| `GravityHandler.cs` | Gravity modifiers with override system (see §2.1.1) |
| `MomentumHandler.cs` | Wavedash, momentum conservation, speed tiers |
| `PlayerTuning.cs` | ScriptableObject — single source of truth for ALL tuning values |
| `PlayerCollision.cs` | Ground check, wall check, ceiling check |
| `PlayerAnimator.cs` | (exists) Drives Animator from player state |
| `JetpackGas.cs` | (exists, refactored) Reads fuel params from PlayerTuning |
| `JetpackParticles.cs` | (exists) Exhaust color shift & sputter |
| `JetpackAudioFeedback.cs` | (exists) Engine pitch decay & sputter SFX |
| `SecondaryBooster.cs` | (exists, refactored) Host that delegates to active IBoosterMode |

**Key principle:** Every behavior reads values from `PlayerTuning` (ScriptableObject). No hardcoded inspector fields on individual scripts. This enables runtime tuning, presets, and single-file value changes.

**Preserved conventions (from CLAUDE.md):** All refactored code must maintain:
- `rb.linearVelocity` (Unity 6 API, not deprecated `rb.velocity`)
- Input reading in `Update`, all physics in `FixedUpdate`
- Manual edge detection for button presses (`IsPressed()` + previous frame tracking)
- Input actions loaded from `Assets/Resources/PlayerInput.inputactions` via `Resources.Load`

**CLAUDE.md update required:** The current CLAUDE.md documents gravity as active during horizontal/downward boost. This spec intentionally overrides that — gravity = 0 during ALL jetpack directions (see Core Design Axiom §1). CLAUDE.md must be updated when Track A work begins so future Claude Code sessions follow the new behavior.

#### 2.1.1 GravityHandler Override System

`GravityHandler` owns all gravity math but exposes a priority-based override system:

```
GravityHandler.SetOverride(float multiplier, int priority)
GravityHandler.ClearOverride(int priority)
```

- `JetpackBehavior` requests gravity=0 (high priority) when jetpack is active in any direction
- `SecondaryBooster` requests gravity=0 (high priority) for a brief tunable window during recoil
- `GravitySwitch` gimmick requests gravity override (medium priority)
- Highest priority wins when multiple overrides are active
- When no overrides are active, standard Celeste-style gravity applies (apex float 0.5×, fast fall 2.0×, terminal velocity 30)

### A2. Runtime Tuning Panel

A debug overlay toggled with F1 that exposes every `PlayerTuning` value as a live slider:

- **Grouped by category:** Ground, Jump, Jetpack, Gravity, Booster, Momentum
- **Live adjustment:** Drag sliders while playing, see numeric values
- **Snapshot:** Save current values as a named preset
- **Reset:** Restore any saved preset instantly
- **Implementation:** Unity IMGUI or UI Toolkit, in `Assets/Scripts/UI/TuningPanel.cs`

### A3. Feel Iteration Protocol

Structured workflow for Claude Code sessions focused on feel:

1. User playtests and describes what feels wrong in plain language
2. Claude Code references a **feel dictionary** (`docs/feel-dictionary.md`) mapping terms to parameter directions:
   - "floaty" → reduce jump force, increase fall gravity multiplier, reduce apex gravity window
   - "snappy" → increase ground accel/decel, reduce coyote time
   - "heavy" → increase fall gravity multiplier, increase fast fall speed
   - "sluggish" → increase ground accel, reduce air control multiplier
   - "slippery" → reduce ground decel
   - (expanded over time as vocabulary develops)
3. Changes applied to `PlayerTuning` — user tests with tuning panel
4. When values feel right, user snapshots the preset and Claude Code commits

### A4. Momentum / Wavedash System

New file: `MomentumHandler.cs`

- **Wavedash:** Secondary booster fired diagonally downward near ground converts recoil momentum into horizontal ground speed
- **Momentum conservation:** Landing from jetpack boost at an angle carries a portion of air velocity into ground speed
- **Speed cap tiers:** Normal (10), boosted ground (15-18), wavedash chain (20+) with gradual decay
- **Decoupled from booster modes:** Listens to generic `OnRecoilApplied` event, not SecondaryBooster directly. Works with any `IBoosterMode` that produces recoil; does nothing when active mode has no recoil.

### A5. Secondary Booster Mode System

Refactor `SecondaryBooster.cs` into a host that delegates to swappable modes:

```
Assets/Scripts/Player/Boosters/
  IBoosterMode.cs       — Interface: Fire(), GetRecoilVector(), GetAmmoInfo()
  RecoilBooster.cs      — Default mode: recoil projectile (current behavior)
  GunBooster.cs         — Aimed projectile, hits SwitchTargets, different recoil profile
```

Each mode declares:
- Whether it produces recoil and how much
- Its own ammo rules (count, recharge behavior)
- Visual/audio feedback hooks

Mode swapping triggered by `BoosterModeType` enum (defined in `Core/`). Swaps can be triggered at any granularity:
- **Chapter-level:** `ChapterConfig` sets the default mode for the chapter
- **Room-level:** A room can override the chapter default on entry (e.g., gun rooms in an otherwise recoil chapter)
- **Mid-room:** A `BoosterSwapZone` gimmick or pickup can change the mode on the fly (e.g., pick up a gun, lose it after the puzzle)
- **Revert behavior:** Each swap can specify whether it's permanent (until next swap) or temporary (reverts when leaving a zone or after N uses)

### A6. Fuel System Extensions

Extend `JetpackGas.cs` (all values read from `PlayerTuning`):

- **Configurable max fuel per chapter** — difficulty lever via `ChapterConfig`
- **Mid-air fuel pickups** — triggered via GameEventBus (`FuelPickupCollected`), JetpackGas subscribes
- **Dual recharge** — pickup events also refill secondary booster ammo
- **Fuel drain rate modifiers** — environmental multiplier (e.g., wind zones accelerate drain)

---

## 3. Track B — Infrastructure

Track B owns: `Assets/Scripts/Level/`, `Assets/Scripts/Camera/`, `Assets/Scripts/Gimmicks/`, `Assets/Scripts/Core/`

### B1. Room & Transition System

Replace current scaffolding with a proper Celeste-style room system:

- **Room definition:** Fixed-size screen (default 20×11.25 units). Rooms placed as GameObjects in Unity Editor. Each room has: ID, bounds, spawn point.
- **Screen-snap camera:** Replace smooth-follow `RoomCamera.cs` with room-locking camera. Quick lerp (~0.2s) on room transitions.
- **Respawn system:** Death resets to current room's spawn point. Celeste-style instant respawn, no loading screen. Death is triggered by contact with Hazard layer (Layer 10, already defined in project). Death handling logic (kill player → respawn at room spawn point) is a small script in Level/.
- **MovementTestLevel.cs disposition:** Deleted (2026-04-21). Was removed when real rooms were built via Tilemaps + Coplay MCP.

### B2. Gimmick Framework

Environmental effects that chapters can mix and match:

```
Assets/Scripts/Gimmicks/
  IGimmick.cs           — Interface: Activate(), Deactivate(), Tick()
  GimmickZone.cs        — Trigger volume, applies gimmick while player is inside
  WindTurbine.cs        — Force vector on interval (on/off cycle, tunable timing)
  GravitySwitch.cs      — Requests gravity override via GravityHandler
  ClosingPlatform.cs    — Platforms open/close on timer, require burst-speed passage
  FuelPickup.cs         — Mid-air collectible, publishes event for fuel + ammo refill
  SwitchTarget.cs       — Shootable target for GunBooster, triggers level events
  BoosterSwapZone.cs    — Trigger zone that changes active booster mode (temporary or permanent)
```

**Gimmick interaction rules:**
- Physics gimmicks (wind, moving platforms) apply forces directly via Rigidbody2D from trigger collisions
- Non-physics effects (visual, audio, pickups, mode swaps) publish events through GameEventBus
- Gimmicks **never modify player movement code** — they operate through the player's public API or events

### B3. Chapter Configuration

```
Assets/Scripts/Level/ChapterConfig.cs — ScriptableObject
```

Per-chapter rules:
- **Default** booster mode (`BoosterModeType` enum from Core/) — rooms and gimmicks can override this
- Max fuel override + fuel drain rate
- Available gimmick palette
- Music track reference

Applied on chapter load as baseline. Individual rooms and gimmick zones can override any chapter-level setting. "Shortened jetpack with mid-air recharges" = `ChapterConfig` with `maxFuel: 40` + `FuelPickup` gimmicks placed in rooms. "Gun puzzle mid-chapter" = a `BoosterSwapZone` placed in specific rooms that swaps to GunBooster on entry and reverts on exit.

### B4. Event Bus

```
Assets/Scripts/Core/
  GameEvent.cs          — Event type definitions
  GameEventBus.cs       — Central pub/sub: Publish(event), Subscribe(event, handler)
```

Decouples gimmicks from the systems they affect:
- `FuelPickup` publishes `FuelPickupCollected` → JetpackGas subscribes and refills
- `BlindZone` publishes `BlindStart/BlindEnd` → ScreenEffects subscribes and applies visual
- `ChapterConfig` publishes `BoosterModeChanged` → SecondaryBooster subscribes and swaps mode

Any future gimmick that affects any system has a clean path without modifying the framework.

### B5. Presentation Layer

```
Assets/Scripts/Camera/ScreenEffects.cs
```

Subscribes to visual events from GameEventBus:
- Blindness/fog effects
- Screen shake
- Color grading shifts
- Any future camera/screen effects triggered by gimmicks

---

## 4. Shared Interface

```
Assets/Scripts/Core/
  PlayerAPI.cs          — Read-only player state for Track B consumption
  BoosterModeType.cs    — Enum shared between ChapterConfig and SecondaryBooster
```

`PlayerAPI` exposes: `IsGrounded`, `IsJetpacking`, `IsBoosting`, `Velocity`, `ActiveBoosterMode`, `FuelPercent`, `CurrentState`.

Track B reads PlayerAPI. Track B never writes to player systems directly — all player-affecting actions go through GameEventBus.

---

## 5. Multi-Session Protocol

### File Ownership

| Track | Owns | Never touches |
|---|---|---|
| Track A (Feel) | `Player/`, `UI/` | `Level/`, `Camera/`, `Gimmicks/` |
| Track B (Infrastructure) | `Level/`, `Camera/`, `Gimmicks/`, `Core/` | `Player/`, `UI/` |

**Exception:** Track A may add event subscriptions in Player/ scripts that listen to Core/ events. Track B may read (never write) PlayerAPI from Core/.

### Branch Protocol

- Track A sessions commit to `track-a/<description>` branches
- Track B sessions commit to `track-b/<description>` branches
- User merges to `main` after reviewing both
- If a merge conflict arises in Core/ files, Track A's version takes priority (feel is king)

### Dependency Rule

Track B systems depend on PlayerController's **public API** (state queries, events), never on internal tuning values. If a Track B feature requires a movement feel decision (e.g., "how fast should wind push the player?"), it gets parked with a TODO until Track A resolves it.

---

## 6. Final File Structure

```
Assets/Scripts/
  Core/
    GameEvent.cs
    GameEventBus.cs
    PlayerAPI.cs
    BoosterModeType.cs
  Camera/
    RoomCamera.cs
    ScreenEffects.cs
  Gimmicks/
    IGimmick.cs
    GimmickZone.cs
    WindTurbine.cs
    GravitySwitch.cs
    ClosingPlatform.cs
    FuelPickup.cs
    SwitchTarget.cs
    BoosterSwapZone.cs
  Level/
    Room.cs
    RoomManager.cs
    ChapterConfig.cs
  Player/
    PlayerController.cs
    PlayerStateMachine.cs
    GroundMovement.cs
    JumpBehavior.cs
    JetpackBehavior.cs
    GravityHandler.cs
    MomentumHandler.cs
    PlayerTuning.cs
    PlayerCollision.cs
    PlayerAnimator.cs
    JetpackGas.cs
    JetpackParticles.cs
    JetpackAudioFeedback.cs
    SecondaryBooster.cs
    Boosters/
      IBoosterMode.cs
      RecoilBooster.cs
      GunBooster.cs
  UI/
    TuningPanel.cs
    GasMeterUI.cs
```

---

## 7. Phase Summary

| Phase | Track | Description | Depends on |
|---|---|---|---|
| A1 | A | PlayerController refactor | — |
| A2 | A | Runtime tuning panel | A1 |
| A3 | A | Feel dictionary + iteration protocol | A1 |
| A4 | A | Momentum / wavedash system | A1, A2 |
| A5 | A | Booster mode system | A1 |
| A6 | A | Fuel system extensions | A1 |
| B1 | B | Room & transition system | — |
| B2 | B | Gimmick framework | B4 |
| B3 | B | Chapter configuration | B2 |
| B4 | B | Event bus (Core/) | — |
| B5 | B | Screen effects / presentation | B4 |

Track A and Track B have **no cross-dependencies** until integration. A1 and B1/B4 can begin simultaneously.
