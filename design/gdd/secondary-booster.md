# Secondary Booster

> **Status**: Implemented
> **Layer**: Core
> **Priority**: MVP
> **Source File**: `Assets/Scripts/Player/SecondaryBooster.cs`
> **Created**: 2026-04-19
> **Last Updated**: 2026-04-19

---

## 1. Overview

The Secondary Booster is a short-range, high-speed 8-directional dash that complements the primary jetpack. Where the jetpack provides sustained directional thrust for traversal, the secondary booster delivers a brief, explosive burst of movement for dodging, repositioning, and precision gap-closing. It uses a limited ammo system (3 charges, recharges on ground) with a short cooldown between uses, creating a resource-management layer on top of the core movement. The system is designed to host swappable behavior modes in the future, with the current dash as the default mode.

---

## 2. Player Fantasy

The secondary booster should feel like a **rocket dash** -- instantaneous, punchy, and committed. When the player fires it, they should feel a sharp kick of speed that cuts through the air in a straight line, leaving an exhaust trail behind. The 8-direction aiming gives precision without the complexity of free-aim. The ammo system creates a "spend wisely" tension: three dashes is generous enough for creative routing but scarce enough that burning all three mid-air without a landing opportunity feels risky.

The intended loop is: **assess gap, pick direction, commit to the dash, land to recharge.** Skilled players chain dashes with jetpack boosts and jumps to maintain momentum and reach otherwise inaccessible areas. The dash is faster but shorter than the jetpack -- it is the scalpel to the jetpack's broad stroke.

---

## 3. Detailed Rules

### Activation

1. Player presses the Fire input (X key / gamepad button west).
2. Input uses manual edge detection (track `IsPressed()` between frames), not `WasPressedThisFrame()`.
3. Activation requires ALL of the following:
   - Fire input is newly pressed this frame (rising edge).
   - `currentAmmo > 0`.
   - `cooldownTimer <= 0`.
   - Not currently boosting (`boostTimer <= 0`).

### Direction Resolution

1. Read the Move input vector (WASD / left stick / dpad).
2. Quantize to 8 directions: each axis is snapped to -1, 0, or +1 using a 0.3 dead zone threshold, then the resulting vector is normalized.
3. If no directional input is held (input magnitude < 0.01), default to the player's current facing direction (left or right).
4. The resolved direction is stored as `aimDirection` and updated every frame, not just on fire.

### Dash Execution

1. On activation:
   - Decrement `currentAmmo` by 1. Fire `OnAmmoChanged` event.
   - Set `cooldownTimer = cooldown` (0.15s).
   - Compute `boostVelocity = aimDirection * boostSpeed`.
   - Set `boostTimer = boostDuration` (0.2s).
   - Immediately set `rb.linearVelocity = boostVelocity`.
   - Call `gravity.SuppressGravity(boostDuration)` to zero gravity for the full dash.
2. Every FixedUpdate while `boostTimer > 0`:
   - Override `rb.linearVelocity = boostVelocity` (velocity is locked, immune to external forces).
   - Refresh gravity suppression (`gravity.SuppressGravity(fixedDeltaTime * 2)` as a safety margin).
   - Decrement `boostTimer` by `fixedDeltaTime`.
3. On completion (`boostTimer` reaches 0):
   - Set `rb.linearVelocity = Vector2.zero`. The player stops dead.
   - Gravity suppression naturally expires.

### PlayerMovement Interaction

- PlayerMovement is disabled during the secondary boost to prevent input conflicts. The dash velocity fully overrides horizontal movement.
- After the dash completes, PlayerMovement resumes. Since velocity is zeroed, the player starts from a standstill unless other systems (jump, jetpack) apply velocity.

### Ammo and Recharge

1. Maximum ammo: `maxAmmo` (3).
2. Ammo decrements by 1 per dash.
3. Ammo fully recharges to `maxAmmo` when the player is grounded (`player.IsGrounded == true`).
4. Recharge is checked every Update frame -- ammo refills on the first grounded frame after being depleted.
5. `OnAmmoChanged` event fires on both spend and recharge.

### Cooldown

1. A `cooldownTimer` prevents rapid consecutive dashes.
2. Set to `cooldown` (0.15s) on each activation.
3. Counts down every Update frame.
4. Activation is blocked while `cooldownTimer > 0`.

### Exhaust Projectile

1. If `projectilePrefab` is assigned in the Inspector, a projectile is spawned on activation.
2. Projectile spawns at the player's position.
3. Projectile velocity: `-aimDirection * projectileSpeed` (opposite direction to dash -- rocket exhaust).
4. Projectile is destroyed after `projectileLifetime` (0.5s).
5. Projectile is purely visual/feedback by default. Collision behavior depends on the prefab's components.

---

## 4. Formulas

### Dash Distance

```
dashDistance = boostSpeed * boostDuration
            = 40 * 0.2
            = 8 units (8 tiles at 16 PPU)
```

### Diagonal Dash Distance (per axis)

```
diagonalAxisDistance = (boostSpeed / sqrt(2)) * boostDuration
                    = (40 / 1.414) * 0.2
                    = 28.28 * 0.2
                    = 5.66 units per axis
```

### Diagonal Total Distance

```
diagonalTotalDistance = boostSpeed * boostDuration
                     = 8 units (same magnitude, just split across axes)
```

### Effective Speed Comparison

| System | Speed | Duration | Distance |
|--------|-------|----------|----------|
| Secondary Booster (straight) | 40 u/s | 0.2s | 8 units |
| Secondary Booster (diagonal) | 40 u/s | 0.2s | 8 units (5.66 per axis) |
| Primary Jetpack | 19 u/s | ~1.0s (fuel) | ~19 units |
| Ground run | 10 u/s | continuous | unlimited |

### Cooldown Window

```
minTimeBetweenDashes = boostDuration + cooldown
                     = 0.2 + 0.15
                     = 0.35s
```

### Maximum Burst Travel (3 ammo, no recharge)

```
maxBurstDistance = dashDistance * maxAmmo
                = 8 * 3
                = 24 units (if all dashes in same direction)

maxBurstTime = (boostDuration * 3) + (cooldown * 2)
             = 0.6 + 0.3
             = 0.9s (to spend all 3 charges)
```

### Projectile Range

```
projectileRange = projectileSpeed * projectileLifetime
                = 20 * 0.5
                = 10 units
```

---

## 5. Edge Cases

| Situation | Expected Behavior | Rationale |
|-----------|-------------------|-----------|
| Fire pressed with no directional input | Dash in player's facing direction (left or right) | Natural fallback; avoids "nothing happens" confusion |
| Fire pressed with only vertical input | Dash straight up or straight down | 8-direction system includes cardinal verticals |
| Fire pressed during primary jetpack boost | Activation still checked (not currently blocked) -- secondary boost overrides velocity | Secondary is higher priority burst; may need explicit mutual exclusion in future |
| Fire pressed while grounded | Dash activates normally; ammo recharges next grounded frame after spending | Ground dashes are a valid technique |
| All 3 ammo spent mid-air | No more dashes until landing | Creates tension and route-planning pressure |
| Dash into a wall | Velocity is locked for full duration; player presses against wall, then stops when boost ends | No wall nudge (unlike primary jetpack); may feel stiff -- candidate for future polish |
| Dash off a ledge then back onto ground | Ammo recharges on the grounded frame, even mid-dash-sequence | Enables ground-skimming recharge tech |
| Cooldown overlaps with boost duration | Cooldown ticks during boost; if cooldown < boostDuration, next dash available immediately after current ends | Current values: 0.15 < 0.2, so cooldown finishes before boost does |
| Fire pressed during secondary boost | Blocked by `IsBoosting` check | Prevents interrupting an active dash |
| Player lands for a single frame then goes airborne | Ammo recharges (checked every Update frame) | Rewards precise landings; enables recharge-hop tech |
| No projectile prefab assigned | Projectile spawn silently skipped (null check) | Prefab is optional; dash works without visual exhaust |

---

## 6. Dependencies

| Dependency | Type | Interface | Notes |
|------------|------|-----------|-------|
| **PlayerController** | Component (same GO) | `player.IsGrounded`, `player.FacingRight` | Ground state and facing direction |
| **PlayerGravity** | Component (same GO) | `gravity.SuppressGravity(float duration)` | Zeroes gravity during dash |
| **PlayerMovement** | Component (same GO) | Disabled during boost | Prevents horizontal input from interfering with locked velocity |
| **Rigidbody2D** | Component (same GO) | `rb.linearVelocity` (Unity 6 API) | Velocity read/write for dash execution |
| **Input System** | Unity Package | `InputActionAsset` loaded from `Resources/PlayerInput` | Fire and Move actions from Gameplay action map |
| **Physics2D Settings** | Project | Project gravity = -20 | Gravity suppression matters because base gravity is strong |

---

## 7. Tuning Knobs

| Parameter | Field Name | Default | Range | Effect of Increase | Effect of Decrease |
|-----------|------------|---------|-------|--------------------|--------------------|
| Dash speed | `boostSpeed` | 40 | 20--80 | Longer dash distance, more explosive feel | Shorter, more controlled dash |
| Dash duration | `boostDuration` | 0.2s | 0.05--0.5s | Longer dash (distance scales linearly), more committed | Snappier, more responsive |
| Max ammo | `maxAmmo` | 3 | 1--6 | More air options, less punishing | Tighter resource management, higher stakes |
| Cooldown | `cooldown` | 0.15s | 0--0.5s | Slower follow-up dashes, more deliberate pacing | Near-instant chaining, more frantic |
| Projectile speed | `projectileSpeed` | 20 | 5--50 | Exhaust flies farther back faster | Exhaust lingers near player |
| Projectile lifetime | `projectileLifetime` | 0.5s | 0.1--2.0s | Exhaust persists longer, longer visual trail | Brief flash, subtle feedback |
| Dead zone threshold | (hardcoded) | 0.3 | 0.1--0.5 | Requires more deliberate stick deflection for diagonals | More sensitive, easier to trigger diagonals |

### Tuning Relationships

- **Dash distance** = `boostSpeed * boostDuration`. Changing either affects total travel. Prefer adjusting `boostSpeed` for feel and `boostDuration` for commitment.
- **Dash-to-jetpack ratio**: At current values, one dash (8 units) covers ~42% of a full jetpack tank (19 units). If secondary feels too dominant, reduce speed or ammo rather than buffing jetpack duration.
- **Cooldown vs duration**: Currently cooldown (0.15s) is shorter than duration (0.2s), so the cooldown expires before the dash ends. This means consecutive dashes have zero dead time. If consecutive dashes should have a gap, set cooldown > boostDuration.

---

## 8. Acceptance Criteria

### Functional

- [ ] Pressing Fire with ammo > 0 and cooldown expired launches the player at `boostSpeed` in the aimed 8-direction.
- [ ] With no directional input, dash fires in the player's facing direction.
- [ ] Velocity is locked for exactly `boostDuration` seconds (not affected by gravity, collisions, or input).
- [ ] Velocity is zeroed to (0, 0) when the dash ends.
- [ ] Gravity is fully suppressed during the dash.
- [ ] Ammo decrements by 1 per dash.
- [ ] Ammo fully recharges to `maxAmmo` on ground contact.
- [ ] Cooldown prevents activation for `cooldown` seconds after each dash.
- [ ] Cannot activate a new dash while an existing dash is active.
- [ ] PlayerMovement does not interfere with dash velocity during boost.
- [ ] Exhaust projectile spawns in the opposite direction of the dash (if prefab assigned).
- [ ] Projectile travels at `projectileSpeed` and self-destructs after `projectileLifetime`.
- [ ] No projectile spawns if prefab is not assigned (no errors).
- [ ] `OnAmmoChanged` event fires on both ammo spend and recharge.

### Feel

- [ ] Dash feels instantaneous -- no ramp-up, no easing.
- [ ] Dash feels faster and more explosive than jetpack boost (40 vs 19 speed).
- [ ] Dead stop at end of dash feels deliberate, not jarring (velocity zero is a design choice, not a bug).
- [ ] 8-direction aiming feels natural with both keyboard and gamepad.
- [ ] Diagonal dashes cover the same total distance as cardinal dashes (normalized direction).

### Integration

- [ ] Works alongside jump system (can dash after jumping).
- [ ] Works alongside jetpack system (can dash during or after jetpack use).
- [ ] Ground detection correctly triggers ammo recharge.
- [ ] Input edge detection prevents accidental double-fires from held button.
- [ ] Component initializes correctly via `GetComponent` references in Awake.

---

## Planned Extensions

### Swappable Mode System (Priority: Alpha)

SecondaryBooster will become a host component delegating to `IBoosterMode` implementations. The current dash behavior becomes the default `DashMode`. A `GunBooster` mode fires aimed projectiles at switch targets instead of dashing. Each mode defines its own movement rules, ammo behavior, and feedback.

Mode swapping can occur at:
- **Chapter level** via `ChapterConfig` (default mode for entire chapter).
- **Room level** on room entry.
- **Mid-room** via `BoosterSwapZone` gimmick or pickup (permanent or temporary).

### Wavedash / Momentum Tech (Priority: Full Vision)

A diagonal-downward dash performed near the ground converts into horizontal ground speed instead of stopping dead. Landing from a jetpack boost at a steep angle preserves a portion of momentum. This enables Celeste/Melee-style speed tech for skilled players without affecting normal play.

Implementation considerations:
- Detect "near ground" via a short raycast during diagonal-down dash.
- Convert remaining dash velocity into horizontal `rb.linearVelocity.x` on ground contact.
- Preserve factor is a tuning knob (e.g., 0.7 = 70% of dash speed becomes ground speed).
- Must not interfere with normal ground movement for players who do not use the tech.
