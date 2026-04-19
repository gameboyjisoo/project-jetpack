# Jetpack System

> **Status**: Implemented
> **Layer**: Core
> **Priority**: MVP
> **Source Files**: `PlayerJetpack.cs`, `JetpackGas.cs`
> **Created**: 2026-04-19
> **Last Updated**: 2026-04-19

---

## 1. Overview

The jetpack is the game's signature mechanic — a press-to-activate, 4-directional booster modeled after Cave Story's Booster 2.0. The player presses a button while airborne to launch in one of four cardinal directions at a fixed speed. Fuel lasts roughly one second and recharges on landing. Gravity is completely suppressed during boost, making trajectories fully deterministic. The system creates a movement vocabulary where the player chains short directional bursts to navigate rooms, with fuel scarcity providing constant tension.

---

## 2. Player Fantasy

The player should feel like they are piloting a volatile, jury-rigged jetpack — each burst is a committed decision with a clear arc and a definite end. There is no hovering, no gentle floating, no gradual course correction. You pick a direction, you fire, and you ride the momentum until the fuel runs out or you let go. The fantasy is **precise, mechanical flight** — not freedom of movement, but mastery over limited movement. Every room becomes a puzzle of "which bursts, in which order, with how much fuel remaining?" When the engine sputters out mid-air and gravity reclaims you, the tension of the fuel running dry should feel physical.

---

## 3. Detailed Rules

### 3.1 Activation

- The jetpack button must be **newly pressed** while the player is airborne. This is edge-detected (not hold-to-start). Pressing jetpack while grounded does nothing.
- The player must have fuel remaining (`JetpackGas.HasGas == true`).
- The player must not already be jetpacking.
- Jump and jetpack are **mutually exclusive** — activating the jetpack clears any active jump state.

### 3.2 Direction Selection

- Direction is determined by the **most recently pressed** directional input at the moment of activation.
- Only 4 cardinal directions are valid: up, down, left, right. No diagonals.
- Direction input uses **edge detection per frame** — the direction updates only when a new directional key transitions from not-pressed to pressed, not while held.
- If no new direction has been pressed since the last activation, the most recent direction persists.

### 3.3 Velocity Application

- On activation, velocity is set **once** to `boostSpeed` in the chosen cardinal direction.
- The perpendicular axis is **zeroed** on activation:
  - Horizontal boost: `velocity = (direction.x * boostSpeed, 0)`
  - Upward boost: `velocity = (0, boostSpeed)`
  - Downward boost: `velocity = (0, -boostSpeed)`
- Velocity is **not reapplied each frame**. The initial impulse is the only force. The player coasts at that speed until the boost ends.

### 3.4 Gravity Suppression (Design Axiom)

**Gravity = 0 during all jetpack directions.** This is a core design axiom, not a tuning choice. While the jetpack is active, no gravitational force acts on the player. The trajectory is perfectly linear in the boost direction. The player must be able to predict exactly where they will end up. No invisible gravity math during propelled states.

### 3.5 Fuel Consumption

- Fuel drains at `gasConsumptionRate` units per second (default: 100/sec).
- Maximum fuel is 100 units, giving approximately **1 second** of boost time.
- When fuel reaches 0, the boost ends immediately (triggers EndBoost).
- Fuel recharges fully and instantly on landing.
- Fuel parameters are intended to be configurable per chapter via `ChapterConfig` (not yet implemented).

### 3.6 Boost Termination

A boost ends when any of these occur:
1. The player **releases** the jetpack button.
2. Fuel reaches **zero**.
3. The player **lands** on the ground.

### 3.7 Mode-Specific Halving on End

When the boost ends (via release or fuel empty), velocity is halved according to the boost mode. This matches Cave Story's Booster 2.0 behavior exactly:

| Boost Mode | boostMode Value | Halving Behavior |
|------------|-----------------|------------------|
| Horizontal (left/right) | 1 | Halve X velocity only. Y unchanged. |
| Upward | 2 | Halve Y velocity only. X unchanged. |
| Downward | 3 | **No halving.** Velocity unchanged. |

Rationale: Downward boost already feeds into gravity's direction, so halving would feel sluggish. Horizontal and upward halving creates a satisfying deceleration that signals "boost ended" without an abrupt stop.

### 3.8 Post-Boost Snap-Back

When the player has non-jump upward velocity after a boost ends (i.e., the upward boost was halved but still has positive Y velocity), **fast fall gravity (2x multiplier)** applies immediately. This prevents floaty post-boost arcs and makes the end of an upward boost feel decisive — you rise, you stop, you fall.

### 3.9 Wall Nudge

- During a **horizontal boost only** (boostMode == 1), if the player contacts a wall in the boost direction, a small upward velocity (`wallNudgeSpeed`) is applied.
- This allows the player to "climb" walls by repeatedly boosting horizontally into them — the nudge bumps them up slightly each frame of contact.
- Wall detection uses a raycast from the player's center in the horizontal boost direction, with length `wallCheckDistance`.
- This matches Booster 2.0's `ym = -0x100` behavior on wall contact.

### 3.10 Boost Mode Tracking

The system tracks the current boost type via `boostMode`:

| Value | Meaning |
|-------|---------|
| 0 | Off (not boosting) |
| 1 | Horizontal (left or right) |
| 2 | Up |
| 3 | Down |

### 3.11 Landing Reset

On landing (ground contact detected by PlayerController):
- `isJetpacking` set to false.
- `boostMode` set to 0.
- `JetpackGas.Recharge()` called (full refuel).

---

## 4. Formulas

### Boost Velocity

```
V_boost = boostSpeed * direction_unit_vector

where direction_unit_vector in {(1,0), (-1,0), (0,1), (0,-1)}
```

Perpendicular axis is always zeroed on activation:
```
if horizontal:  V = (sign(dir.x) * boostSpeed, 0)
if up:           V = (0, boostSpeed)
if down:         V = (0, -boostSpeed)
```

### Fuel Drain

```
gas_remaining = gas_current - (gasConsumptionRate * dt)
gas_remaining = max(0, gas_remaining)

boost_duration_max = maxGas / gasConsumptionRate
                   = 100 / 100
                   = 1.0 second
```

### Boost Distance (Theoretical Maximum)

```
distance_max = boostSpeed * boost_duration_max
             = 19 * 1.0
             = 19 units (= 19 tiles at 16 PPU)
```

With halving on release at time t:
```
distance = boostSpeed * t + (boostSpeed * 0.5) * t_coast

where t_coast depends on gravity recapture (post-boost gravity behavior)
```

### Halving on End

```
if boostMode == 1 (horizontal):
    V.x = V.x * 0.5
    V.y = V.y  (unchanged)

if boostMode == 2 (up):
    V.x = V.x  (unchanged)
    V.y = V.y * 0.5

if boostMode == 3 (down):
    V = V  (no change)
```

### Wall Nudge

```
if boostMode == 1 AND wall_detected:
    V.y = wallNudgeSpeed

wall_detected = Physics2D.Raycast(
    origin: player.position,
    direction: (jetpackDirection.x, 0),
    distance: wallCheckDistance,
    layerMask: groundLayer
).collider != null
```

### Post-Boost Snap-Back Gravity

```
if !isJetpacking AND V.y > 0 AND !isVarJumping:
    gravityScale = fallGravityMultiplier  (2.0x)
```

---

## 5. Edge Cases

| Situation | Expected Behavior |
|-----------|-------------------|
| Press jetpack while grounded | Nothing happens. Jetpack requires airborne state. |
| Press jetpack with 0 fuel | Nothing happens. Activation requires `HasGas == true`. |
| Press jetpack while already jetpacking | Nothing happens. Must not be currently jetpacking. |
| Hold jetpack button, then go airborne | Nothing happens. Button must be newly pressed (edge-detected) while airborne. |
| No directional input at activation | Uses the most recently stored direction from previous edge-detected input. |
| Diagonal input (e.g., up+right held) | Uses whichever cardinal direction was most recently edge-detected. Not both. |
| Land during active boost | Boost ends immediately. Fuel recharges. No halving applied (landing overrides). |
| Hit ceiling during upward boost | Physics collision stops upward movement. Boost continues draining fuel until release or empty. Wall nudge does not apply (only horizontal mode). |
| Hit floor during downward boost | Triggers landing. Boost ends. Fuel recharges. |
| Fuel runs out mid-boost | Boost ends with mode-specific halving. Post-boost gravity applies. |
| Release and re-press jetpack mid-air | First boost ends (with halving). Second press activates a new boost if fuel remains. |
| Wall nudge with no wall | No effect. Raycast finds nothing. Normal horizontal boost continues. |
| Wall nudge at wall corner/edge | Raycast may miss depending on player position relative to wall geometry. wallCheckDistance (0.6) provides some tolerance. |
| Jump pressed during jetpack | Ignored. Jump and jetpack are mutually exclusive. Jetpack activation clears jump state. |
| Jetpack pressed during variable jump hold | Activates jetpack, ends jump. Jump state cleared. |
| Secondary booster during jetpack | Handled by SecondaryBooster's own activation rules (separate system). |
| Fast fall after upward boost halving | If V.y > 0 after halving and player is not in variable jump, fast fall gravity (2x) kicks in immediately. No floating. |
| Downward boost into spikes/hazard | Hazard system handles collision independently. No halving on death. |

---

## 6. Dependencies

| System | Dependency Type | Description |
|--------|----------------|-------------|
| **JetpackGas** (Fuel System) | Hard | Provides fuel state (`HasGas`, `ConsumeGas`, `Recharge`). Jetpack cannot function without it. |
| **PlayerGravity** | Hard | Must suppress gravity (scale = 0) while `isJetpacking == true`. Must apply fast fall (2x) for post-boost snap-back. |
| **PlayerController** | Hard | Orchestrates `Tick()` call order in FixedUpdate. Provides ground state, input state, and edge-detection flags. |
| **Input System** | Hard | Provides jetpack button press state and directional input for direction selection. |
| **Physics2D** | Hard | Provides Rigidbody2D for velocity manipulation and Raycast for wall detection. |
| **PlayerJump** | Soft | Jump and jetpack are mutually exclusive. Jetpack activation clears jump timers. |
| **PlayerMovement** | Soft | Horizontal movement is effectively overridden during boost (velocity set directly). |
| **JetpackParticles** | Soft (presentation) | Reads `IsJetpacking` and fuel state for visual feedback. Not required for function. |
| **JetpackAudioFeedback** | Soft (presentation) | Reads fuel events for audio feedback. Not required for function. |
| **ChapterConfig** (planned) | Soft (future) | Will provide per-chapter fuel parameters (max gas, consumption rate). |

---

## 7. Tuning Knobs

| Parameter | Default Value | Range / Units | Location | Effect |
|-----------|--------------|---------------|----------|--------|
| `boostSpeed` | 19 | units/sec | PlayerJetpack | Speed of boost in any direction. ~1.9x moveSpeed. Higher = faster traversal, harder to control in tight spaces. |
| `gasConsumptionRate` | 100 | units/sec | PlayerJetpack | Fuel drain speed. At 100 max gas, this gives ~1 second of boost. Lower = longer boosts, more forgiving. |
| `maxGas` | 100 | units | JetpackGas | Total fuel capacity. Combined with consumption rate determines boost duration. |
| `wallNudgeSpeed` | 2 | units/sec | PlayerJetpack | Upward velocity applied when horizontal boost contacts wall. Higher = faster wall climbing. |
| `wallCheckDistance` | 0.6 | units | PlayerJetpack | Raycast length for wall detection during horizontal boost. Must be slightly larger than half the player's width to reliably detect walls. |
| `groundLayer` | Layer 8 | LayerMask | PlayerJetpack | Which layers count as walls for wall nudge raycast. |

### Tuning Relationships

- **boostSpeed / moveSpeed ratio**: Currently 1.9x. This ratio determines how much faster jetpack traversal is vs. ground movement. Cave Story's Booster 2.0 uses a similar ratio relative to its terminal velocity.
- **maxGas / gasConsumptionRate**: This ratio is the boost duration in seconds. Currently 1.0s. Changing either value independently changes the duration.
- **boostSpeed * (maxGas / gasConsumptionRate)**: Maximum boost distance in units. Currently 19 tiles. This is the fundamental level design constraint — gaps and rooms must be designed around this distance.

### Per-Chapter Tuning (Planned)

Future chapters may adjust these parameters via `ChapterConfig`:
- Shorter fuel duration for harder chapters.
- Mid-air fuel pickups (calls `JetpackGas.RechargeFromPickup()`) to create longer traversal puzzles.
- Modified wall nudge speed for chapters with vertical wall-climbing challenges.

---

## 8. Acceptance Criteria

### Activation
- [ ] Pressing jetpack button while airborne with fuel activates boost in the most recently edge-detected cardinal direction.
- [ ] Pressing jetpack button while grounded does nothing.
- [ ] Holding jetpack button before going airborne does not activate boost. Button must be newly pressed while airborne.
- [ ] Pressing jetpack with zero fuel does nothing.
- [ ] Pressing jetpack while already boosting does nothing.

### Velocity
- [ ] On activation, velocity is set to exactly `boostSpeed` in the chosen direction.
- [ ] Perpendicular axis is zeroed on activation (horizontal boost zeroes Y, vertical boost zeroes X).
- [ ] Velocity is set once on activation, not continuously applied each frame.

### Gravity Suppression
- [ ] During active boost in any direction, gravity is zero. Player moves in a perfectly straight line.
- [ ] No downward drift during horizontal boost. No lateral drift during vertical boost.

### Fuel
- [ ] Fuel drains at `gasConsumptionRate` per second during active boost.
- [ ] Boost ends when fuel reaches zero.
- [ ] Fuel recharges fully and instantly on landing.
- [ ] `OnGasChanged` event fires each frame during drain. `OnGasEmpty` fires when fuel hits zero. `OnGasRecharged` fires on landing refuel.

### Halving
- [ ] Horizontal boost (mode 1): X velocity halved on end. Y unchanged.
- [ ] Upward boost (mode 2): Y velocity halved on end. X unchanged.
- [ ] Downward boost (mode 3): No halving. Velocity unchanged on end.
- [ ] Halving applies on both manual release and fuel-empty termination.

### Post-Boost Snap-Back
- [ ] After upward boost ends and player has positive Y velocity (from halving), fast fall gravity (2x) applies immediately. No floaty arc.

### Wall Nudge
- [ ] During horizontal boost, contacting a wall applies `wallNudgeSpeed` upward velocity.
- [ ] Wall nudge does not apply during upward or downward boost.
- [ ] Repeated horizontal boosts into a wall allow the player to climb the wall incrementally.

### Mutual Exclusion
- [ ] Activating jetpack clears any active jump state (variable jump timer, etc.).
- [ ] Jump input is ignored while jetpack is active.

### Direction
- [ ] Direction uses edge detection: only updates when a directional key transitions from not-pressed to pressed.
- [ ] Holding a direction does not continuously update the stored direction.
- [ ] Only cardinal directions are valid. Simultaneous inputs resolve to the most recently pressed.

### Integration
- [ ] Boost state is readable by presentation systems (`IsJetpacking`, `BoostMode` properties).
- [ ] Landing resets all jetpack state (isJetpacking, boostMode) and triggers fuel recharge.

---

## Feel Reference

**Cave Story's Booster 2.0** is the primary reference. Key qualities to match:
- **Committed bursts**: Each activation is a decision you ride out. No micro-adjustments mid-boost.
- **Fuel tension**: One second feels generous until you're two-thirds across a gap and the engine sputters. The countdown is felt, not seen.
- **Wall climbing rhythm**: Horizontal boost into wall, nudge up, fall slightly, boost again. A rhythmic, earned vertical traversal.
- **Directional vocabulary**: Players develop an internal language of "right-boost, up-boost, right-boost" sequences to navigate complex rooms.
- **Post-boost commitment**: When the boost ends, you're at the mercy of gravity. The halving softens the transition but doesn't save you from a bad trajectory.
