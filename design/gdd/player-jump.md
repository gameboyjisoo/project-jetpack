# Player Jump System

> **System**: Player Jump
> **Layer**: Core
> **Priority**: MVP
> **Status**: Implemented
> **Source Files**: `PlayerJump.cs`, `PlayerGravity.cs` (gravity subsystem)
> **Created**: 2026-04-19
> **Last Updated**: 2026-04-19

---

## 1. Summary

The jump system gives the player a Celeste-style variable-height jump with coyote time, input buffering, horizontal boost, and half-gravity apex float. It is the primary vertical movement tool on the ground and works in tandem with the gravity system to produce a tight, responsive, and forgiving platformer feel.

---

## 2. Overview

Jumping is the player's foundational aerial action. Every room in the game assumes the player can jump, and most rooms require precise jump height control to navigate. The system is modeled directly on Celeste's jump implementation: a fixed initial impulse whose effective height is extended by holding the jump button (variable jump timer), with forgiveness mechanics (coyote time, jump buffer) that make the game feel fair without making it easy. Gravity behavior during and after a jump is handled by the companion gravity system, which applies fast-fall, apex float, and gravity suppression rules.

---

## 3. Player Fantasy

The player should feel like they have **precise, trustworthy control** over their jump arc. Short taps produce short hops; held jumps reach full height. Near the apex, time seems to stretch slightly, giving the player a moment to aim their next move. The game never eats an input or punishes a slightly-late button press. The jump feels **crisp on takeoff, floaty at the peak, and snappy on descent** -- the signature Celeste rhythm.

---

## 4. Detailed Design

### 4.1 Core Rules

1. **Jump initiation**: The player jumps when a buffered jump input exists AND the player is grounded (or within coyote time) AND the player is NOT currently jetpacking.
2. **Immediate velocity set**: On jump frame, vertical velocity is set to `jumpForce` (not added -- set). Horizontal velocity receives a directional boost of `jumpHBoost` in the current input direction, added to existing horizontal velocity.
3. **Variable jump hold**: A `varJumpTimer` starts at `varJumpTime` (0.2s). While the timer is positive, the jump button is held, and vertical velocity is non-negative, vertical velocity is maintained at no less than `varJumpSpeed` (the initial `jumpForce`). This prevents gravity from decelerating the player during the hold window.
4. **Variable jump release**: If the player releases the jump button while `varJumpTimer > 0`, the timer is immediately zeroed. Gravity then decelerates the player naturally -- there is NO instant velocity cut. This produces smooth variable-height arcs.
5. **Coyote time**: When the player walks off a ledge (leaves ground without jumping), a `coyoteTimer` is set to `coyoteTime` (0.1s). The player can still initiate a jump during this window as if they were grounded.
6. **Jump buffer**: When the player presses jump while airborne, a `jumpBufferTimer` is set to `jumpBufferTime` (0.1s). If the player lands before the buffer expires, the jump fires automatically on the landing frame.
7. **Mutual exclusion with jetpack**: Jump cannot be initiated while `isJetpacking` is true. The jetpack system owns the player's velocity during boost. Conversely, the jetpack cannot activate while the player is grounded (jetpack is airborne-only), so the two systems naturally partition ground vs. air.
8. **Landing reset**: On landing, `didJump` is cleared and `varJumpTimer` is zeroed, ensuring stale jump state does not carry across ground touches.

### 4.2 States and Transitions

```
                        ┌──────────────────────────────────┐
                        │           GROUNDED               │
                        │  (coyoteTimer resets on leave)   │
                        └──────┬──────────────┬────────────┘
                               │              │
                     jump pressed        walked off edge
                               │              │
                               ▼              ▼
                        ┌──────────────┐  ┌───────────────┐
                        │  JUMP RISING │  │  COYOTE WINDOW│
                        │  varJump     │  │  (0.1s grace) │
                        │  active      │  └──────┬────────┘
                        └──────┬───────┘         │
                               │           jump pressed
                     timer expires or      (same as grounded)
                     button released             │
                               │                 ▼
                               ▼          ┌──────────────┐
                        ┌──────────────┐  │  JUMP RISING │
                        │   APEX       │  │  (from coyote)│
                        │ |vy| < 2.5, │  └──────────────┘
                        │ jump held    │
                        │ gravity 0.5x │
                        └──────┬───────┘
                               │
                        vy becomes negative
                               │
                               ▼
                        ┌──────────────┐
                        │   FALLING    │
                        │ gravity 2.0x │
                        │ capped at    │
                        │ maxFallSpeed │
                        └──────┬───────┘
                               │
                          touches ground
                               │
                               ▼
                        ┌──────────────┐
                        │   LANDED     │
                        │ didJump=false│
                        │ varJump=0    │
                        │ (consumes    │
                        │  jump buffer)│
                        └──────────────┘
```

### 4.3 Interactions with Other Systems

| System | Interaction |
|--------|------------|
| **PlayerGravity** | Gravity system reads `didJump` and `jumpHeld` to determine apex float eligibility. During variable jump hold, gravity still applies but `ApplyVarJump()` overrides the deceleration by maintaining `vy >= varJumpSpeed`. After release, gravity alone controls the arc. |
| **PlayerJetpack** | Mutually exclusive. `TryJump()` checks `!isJetpacking`. Jetpack checks `!isGrounded`. If jetpack is active, jump input is ignored. |
| **PlayerMovement** | Jump adds `jumpHBoost` to horizontal velocity on the jump frame. After that, `PlayerMovement.Tick()` handles horizontal control with air multiplier (0.65x). |
| **PlayerController** | Orchestrator calls jump methods in fixed pipeline order. Reads input in `Update()`, passes to jump system in `FixedUpdate()`. |
| **SecondaryBooster** | Independent system. Can be used during a jump arc (after variable jump window). Does not interact with jump state directly. |
| **PlayerAnimator** | Reads `IsGrounded` and `VelocityY` to drive jump/fall/land animations. Does not affect jump logic. |

---

## 5. Formulas

### Jump Initiation

```
canJump = isGrounded OR (coyoteTimer > 0)
doJump  = (jumpBufferTimer > 0) AND canJump AND (NOT isJetpacking)

if doJump:
    vy = jumpForce                                  // set, not add
    vx = vx + (moveInput.x * jumpHBoost)           // additive horizontal boost
    varJumpTimer = varJumpTime
    varJumpSpeed = jumpForce
    coyoteTimer  = 0                                // consume coyote
    jumpBufferTimer = 0                             // consume buffer
    didJump = true
```

### Variable Jump Maintenance (per FixedUpdate)

```
if (varJumpTimer > 0) AND jumpHeld AND (vy >= 0):
    vy = max(vy, varJumpSpeed)                      // floor, not set -- preserves upward boosts
```

### Variable Jump Release

```
if jumpReleased AND (varJumpTimer > 0):
    varJumpTimer = 0                                // gravity takes over naturally
    // NOTE: vy is NOT modified -- no instant velocity cut
```

### Timer Decay (per FixedUpdate)

```
coyoteTimer    -= fixedDeltaTime
varJumpTimer   -= fixedDeltaTime
jumpBufferTimer -= fixedDeltaTime                   // also set to jumpBufferTime on press
```

### Gravity Scaling (handled by PlayerGravity, included for completeness)

```
if isJetpacking OR suppressTimer > 0:
    gravityScale = 0                                // design axiom: no gravity during propulsion

else if vy < 0:
    gravityScale = fallGravityMultiplier (2.0)      // fast fall

else if didJump AND jumpHeld AND |vy| < apexThreshold:
    gravityScale = apexGravityMultiplier (0.5)      // apex float

else if (NOT didJump) AND vy > 0:
    gravityScale = fallGravityMultiplier (2.0)      // post-boost snap-back

else:
    gravityScale = 1.0                              // normal rising
```

### Terminal Velocity

```
if vy < -maxFallSpeed:
    vy = -maxFallSpeed
```

### Approximate Jump Height (analytical)

Full-hold jump height (simplified, assuming constant gravity during rise after varJump expires):

```
Phase 1 (varJump hold): h1 = jumpForce * varJumpTime
                            = 8 * 0.2 = 1.6 units

Phase 2 (normal gravity deceleration from jumpForce):
    Using v^2 = u^2 - 2*g*h, where g = |Physics2D.gravity.y| * gravityScale
    Project gravity = 20 (Physics2D setting), scale 1.0:
    h2 = jumpForce^2 / (2 * 20) = 64 / 40 = 1.6 units

Total ≈ h1 + h2 ≈ 3.2 units (full hold)
```

Tap jump height (release immediately, no varJump hold):

```
varJumpTimer = 0 on release, gravity decelerates from jumpForce at 1.0x:
h_tap = jumpForce^2 / (2 * 20) = 64 / 40 = 1.6 units
```

Note: Actual heights depend on Unity's physics step integration and whether apex float triggers. These are approximations for tuning reference. Project gravity = -20 (set in Physics2D settings) — significantly stronger than Unity's default -9.81. Use the runtime tuning panel for precise measurement.

---

## 6. Edge Cases

| Situation | Expected Behavior | Rationale |
|-----------|-------------------|-----------|
| Jump pressed 1 frame before landing | Jump buffer stores input; jump fires on landing frame | Forgiveness -- prevents "eaten" inputs |
| Walk off ledge, press jump within 0.1s | Coyote jump fires as if grounded | Forgiveness -- the player thought they were on the ledge |
| Walk off ledge, coyote expires, then land | Landing resets all timers; coyote state is irrelevant | Clean state on every ground touch |
| Hold jump for entire ascent | Variable jump maintains speed for 0.2s, then gravity decelerates normally; apex float extends hang time | Maximum height jump |
| Release jump mid-varJump window | varJumpTimer zeroed, but vy is NOT cut. Gravity decelerates smoothly | No jarring velocity snap -- Celeste behavior |
| Press jump while jetpacking | Jump is rejected (`!isJetpacking` check) | Mutual exclusion rule |
| Land on one-way platform from below | Ground check must not trigger while rising through platform | Platform effector handles this via Physics2D |
| Jetpack ends mid-air, player has jump buffer | Buffer may be stale (timer expired). No coyote time from jetpack end -- only ground-to-air transitions grant coyote | Coyote time is a ledge-forgiveness mechanic, not a general airborne grace period |
| Jump at zero horizontal input | `jumpHBoost * 0 = 0`, no horizontal velocity change | Neutral jump is valid |
| Jump into ceiling immediately | `vy` becomes 0 or negative on collision. `varJumpTimer` still counts down but `ApplyVarJump` condition (`vy >= 0`) fails, so velocity is NOT forced back up | Player does not stick to ceilings |
| Multiple jump presses while airborne (no double jump) | Only one buffer is stored (latest press refreshes the timer). No jump fires until grounded/coyote | Single jump only -- no air jumps |
| Frame-perfect coyote + buffer overlap | Both timers are positive. Jump fires, both are zeroed. Clean single jump | No double-consumption |

---

## 7. Dependencies

| Dependency | Type | Description |
|------------|------|-------------|
| `Rigidbody2D` | Unity Component | Velocity read/write via `rb.linearVelocity` (Unity 6 API) |
| `PlayerController` | Orchestrator | Calls `Init()`, `UpdateTimers()`, `HandleJumpRelease()`, `TryJump()`, `ApplyVarJump()` in pipeline order. Provides `isGrounded`, `isJetpacking`, `jumpHeld`, `jumpReleased`, `moveInput` |
| `PlayerGravity` | Sibling Component | Applies gravity scaling based on `didJump` and `jumpHeld`. Handles apex float, fast fall, terminal velocity |
| `PlayerMovement` | Sibling Component | Applies horizontal movement with air control multiplier after jump changes vx |
| Input System | Unity Package | Jump action read via `IsPressed()` with manual edge detection in `PlayerController.Update()` |
| Physics2D | Unity Built-in | Ground detection (raycast/overlap), collision resolution, gravity integration |

---

## 8. Tuning Knobs

| Parameter | Field | Default | Unit | Effect of Increase | Effect of Decrease |
|-----------|-------|---------|------|--------------------|--------------------|
| Jump Force | `jumpForce` | 8 | units/sec | Higher jumps, faster rise | Lower jumps, slower rise |
| Variable Jump Time | `varJumpTime` | 0.2 (12 frames) | seconds | Wider hold window, higher max jump, more height control | Tighter window, less height difference between tap and hold |
| Horizontal Boost | `jumpHBoost` | 2.4 | units/sec | More horizontal momentum on jump, longer running jumps | Less directional influence on jump |
| Coyote Time | `coyoteTime` | 0.1 (6 frames) | seconds | More forgiving ledge jumps | Stricter timing required |
| Jump Buffer | `jumpBufferTime` | 0.1 (6 frames) | seconds | More forgiving early presses | Stricter pre-land timing |
| Fall Gravity Mult | `fallGravityMultiplier` | 2.0 | multiplier | Faster descent, snappier feel | Floatier descent |
| Apex Gravity Mult | `apexGravityMultiplier` | 0.5 | multiplier | Floatier apex, more air control time | Snappier apex transition |
| Apex Threshold | `apexThreshold` | 2.5 | units/sec | Wider apex window (more of the arc feels floaty) | Narrower apex (brief float) |
| Max Fall Speed | `maxFallSpeed` | 20 | units/sec | Faster terminal velocity | Slower terminal velocity, floatier |

All values are `[SerializeField]` and editable in the Unity Inspector at runtime.

---

## 9. Visual / Audio

### Visual Feedback

| Event | Visual Cue | Implementation |
|-------|-----------|----------------|
| Jump takeoff | Squash-stretch on player sprite (compress then stretch upward) | PlayerAnimator trigger |
| Landing | Squash on ground contact, dust particles at feet | PlayerAnimator trigger + particle system |
| Apex float | Subtle sprite stretch (tall and thin) | Driven by `apexGravityMultiplier` state in animator |
| Coyote jump (off-ledge) | Same as normal jump -- player should NOT notice the forgiveness | Intentionally invisible |

### Audio Feedback

| Event | Sound | Notes |
|-------|-------|-------|
| Jump | Short, crisp jump SFX | Plays on `TryJump()` returning true |
| Landing | Soft thud, proportional to fall speed | Plays in `OnLand()`, pitch/volume scaled by impact velocity |
| Apex float | No dedicated sound | Silence reinforces the "moment of calm" |

---

## 10. Game Feel

### The Celeste Jump Rhythm

The jump arc has three distinct phases the player should **feel** even if they cannot articulate them:

1. **Takeoff** (0-0.2s): Fast, committed upward motion. The player is rewarded for holding the button -- height increases noticeably. Releasing early produces a satisfying short hop that feels intentional, not punished.

2. **Apex** (variable): Time stretches. Half-gravity gives the player a moment to assess, aim, and decide: drift left, drift right, activate jetpack, or let gravity take over. This is the decision point of every jump.

3. **Descent** (fast): 2x gravity snaps the player back down. The descent is faster than the rise, which creates the asymmetric arc that makes platforming feel "tight." Terminal velocity prevents the descent from becoming uncontrollable.

### Forgiveness Philosophy

Coyote time and jump buffer are invisible safety nets. The player should feel like the game "just works" -- they pressed jump and they jumped, even if technically they were 3 frames late off the ledge or 4 frames early before landing. These mechanics are never surfaced to the player. If the player notices them, the values are too large.

### Horizontal Boost

The `jumpHBoost` adds a subtle but important directional commitment on takeoff. Running right and jumping carries more rightward momentum than jumping then pressing right. This rewards intentional movement and creates a natural distinction between "running jump" and "standing jump" without separate animations or states.

---

## 11. UI Requirements

No direct UI elements. The jump system is entirely communicated through character animation, audio, and game feel. There is no jump counter, stamina bar, or cooldown indicator.

If a tutorial is added later, jump controls would be communicated through environmental prompts (signs, room layout that teaches mechanics) rather than HUD overlays -- consistent with the project's diegetic feedback philosophy.

---

## 12. Cross-References

| Related System | Document | Relationship |
|----------------|----------|-------------|
| Player Movement | `design/gdd/player-movement.md` | Horizontal velocity, air control multiplier, sprite flipping |
| Jetpack System | `design/gdd/jetpack-system.md` | Mutual exclusion, jetpack is the aerial counterpart to jump |
| Secondary Booster | `design/gdd/secondary-booster.md` | Can be used during jump arc, independent system |
| Fuel Feedback | `design/gdd/fuel-feedback.md` | No direct interaction, but shares the diegetic feedback philosophy |
| Gravity System | (inline in this doc + `PlayerGravity.cs`) | Apex float, fast fall, gravity suppression during propelled states |
| Systems Index | `design/gdd/systems-index.md` | Master list of all systems and their status |

---

## 13. Acceptance Criteria

### Functional

- [ ] Pressing jump while grounded sets vertical velocity to `jumpForce` (8)
- [ ] Holding jump extends the ascent for up to `varJumpTime` (0.2s) by maintaining `vy >= jumpForce`
- [ ] Releasing jump early zeros the varJump timer but does NOT cut velocity instantly
- [ ] Pressing jump within `coyoteTime` (0.1s) of leaving ground produces a valid jump
- [ ] Pressing jump up to `jumpBufferTime` (0.1s) before landing triggers a jump on the landing frame
- [ ] Jump is rejected while `isJetpacking` is true
- [ ] Horizontal velocity receives `moveInput.x * jumpHBoost` (2.4) additive boost on jump frame
- [ ] Gravity scale is 0.5x when `didJump && jumpHeld && |vy| < apexThreshold` (2.5) (apex float)
- [ ] Gravity scale is 2.0x when `vy < 0` (fast fall)
- [ ] Fall speed is clamped at `maxFallSpeed` (20 units/sec)
- [ ] Landing resets `didJump` and `varJumpTimer`
- [ ] No double jump -- only one jump per ground contact (+ coyote window)

### Feel

- [ ] Full-hold jump is noticeably higher than a tap jump
- [ ] Apex feels floaty -- player has a perceptible hang time to make decisions
- [ ] Descent is faster than ascent (asymmetric arc)
- [ ] Coyote time and jump buffer feel invisible -- the player does not notice forgiveness, they just feel the game is responsive
- [ ] Running jump carries more horizontal distance than standing jump
- [ ] Hitting a ceiling does not cause the player to hover or stick

---

## 14. Open Questions

| # | Question | Impact | Status |
|---|----------|--------|--------|
| 1 | Should coyote time apply after jetpack ends (not just ground-to-air)? | Could make jetpack-to-jump transitions smoother, but may make the jetpack feel less committed | **Decided: No** -- coyote is ledge-forgiveness only |
| 2 | Should `jumpHBoost` scale with current horizontal speed (e.g., larger boost at low speed, smaller at high speed)? | Could prevent excessive horizontal velocity stacking | Open -- test during vertical slice |
| 3 | Will wall jump be added later? | Would need to grant coyote time on wall-leave, modify horizontal boost to push away from wall | Open -- not in current scope |
| 4 | Should apex float activate on non-jump upward velocity (e.g., after upward jetpack ends)? | Currently only activates with `didJump` flag. Jetpack-to-apex-float could feel good | **Decided: No** -- post-boost snap-back uses fast fall (2x) intentionally |
| 5 | Exact jump heights need empirical measurement in-engine | Analytical estimates are approximate due to discrete physics integration | To do -- measure with runtime tuning panel |
