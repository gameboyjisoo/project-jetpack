# GDD: Player Movement

> **Status**: Implemented
> **Layer**: Core
> **Priority**: MVP
> **Component**: `PlayerMovement.cs`
> **Dependencies**: Input System (Unity New Input System), Physics2D (Rigidbody2D)
> **Created**: 2026-04-19
> **Last Updated**: 2026-04-19

---

## 1. Summary

The Player Movement system handles horizontal ground and air movement using Celeste-style instant acceleration and deceleration via `MoveTowards`. It provides the crisp, responsive feel that anchors all other movement systems (jump, jetpack, secondary booster). This is a Core/MVP system with no gameplay dependencies beyond Unity's built-in Input System and Physics2D.

---

## 2. Overview

Player Movement governs the player's horizontal velocity in response to directional input. It runs every `FixedUpdate` as step 6 in the PlayerController pipeline, after jump and jetpack logic have resolved. The system intentionally uses `MoveTowards` instead of forces or lerps, producing the near-instant speed changes that define Celeste's ground feel. Air control is reduced to 65% of ground responsiveness via a single multiplier.

The system also owns sprite facing direction, flipping `transform.localScale.x` when the player changes direction. Movement is suppressed entirely during jetpack and secondary booster states to prevent input conflicts.

---

## 3. Player Fantasy

The player feels completely in control at all times. When they press a direction, the character responds instantly — no ice-skating, no wind-up animations eating input. Stopping is equally sharp: release the stick and the character plants. In the air, the player can still steer meaningfully but feels the difference — there is a slight commitment to each jump's trajectory without losing the ability to course-correct. The movement never fights the player.

---

## 4. Detailed Design

### 4.1 Core Rules

1. **Velocity-based, not force-based.** Horizontal velocity is set directly via `rb.linearVelocity` (Unity 6 API). No `AddForce`, no physics drag.
2. **MoveTowards interpolation.** Each physics tick, current X velocity moves toward the target speed at a fixed rate. With acceleration/deceleration at 120 and a target speed of 6, the player reaches full speed in ~0.05 seconds (effectively instant at 50Hz FixedUpdate).
3. **Air control multiplier.** When airborne, both acceleration and deceleration rates are multiplied by `airMult` (0.65). This is a single multiplier applied to whichever rate is active, matching Celeste's `AirMult` approach.
4. **Y velocity is never touched.** The system only writes to `rb.linearVelocity.x`. Vertical velocity is owned by PlayerJump, PlayerJetpack, and PlayerGravity.
5. **Sprite flip via localScale.** When input direction changes, `transform.localScale.x` is multiplied by -1. No SpriteRenderer.flipX — this ensures child objects (particles, etc.) flip with the player.
6. **Suppressed during propelled states.** If the player is jetpacking or secondary boosting, `Tick()` returns immediately. Those systems own velocity during their active states.

### 4.2 States and Transitions

The movement system itself is stateless — it applies the same logic every tick, with behavior varying based on two external flags:

| Condition | Acceleration Rate | Deceleration Rate | Notes |
|---|---|---|---|
| Grounded, input held | 120 | — | Near-instant ramp to full speed |
| Grounded, no input | — | 120 | Near-instant stop |
| Airborne, input held | 78 (120 x 0.65) | — | Reduced air steering |
| Airborne, no input | — | 78 (120 x 0.65) | Slower air deceleration (preserves trajectory) |
| Jetpacking | Suppressed | Suppressed | Jetpack owns velocity |
| Secondary boosting | Suppressed | Suppressed | Secondary booster owns velocity |

### 4.3 Interactions

| System | Interaction |
|---|---|
| **PlayerJump** | Jump may add horizontal boost (`jumpHBoost`) to velocity. Movement resumes applying accel/decel on the next tick. |
| **PlayerJetpack** | Movement is fully suppressed during jetpack. When jetpack ends, horizontal velocity may be halved (for horizontal boost mode). Movement then resumes from whatever velocity remains. |
| **SecondaryBooster** | Movement is fully suppressed during dash. Velocity is zeroed when dash completes. Movement resumes from zero. |
| **PlayerGravity** | No direct interaction. Gravity only affects Y axis; movement only affects X axis. |
| **PlayerAnimator** | Reads `moveInput.x` magnitude and `isGrounded` to drive run animation. Does not write to movement. |

---

## 5. Formulas

### 5.1 Core Movement Formula

```
targetSpeed = moveInput.x * moveSpeed
rate = (|moveInput.x| > 0.01) ? groundAcceleration : groundDeceleration
rate *= isGrounded ? 1.0 : airMult
newSpeed = MoveTowards(currentSpeed, targetSpeed, rate * fixedDeltaTime)
rb.linearVelocity.x = newSpeed
```

### 5.2 Variable Table

| Variable | Type | Default | Description |
|---|---|---|---|
| `moveSpeed` | float | 6 | Maximum horizontal speed in units/sec |
| `groundAcceleration` | float | 120 | Rate of speed increase toward target (units/sec^2) |
| `groundDeceleration` | float | 120 | Rate of speed decrease toward zero (units/sec^2) |
| `airMult` | float | 0.65 | Multiplier applied to accel/decel when airborne |
| `moveInput.x` | float | [-1, 1] | Horizontal input from player (read in Update) |
| `isGrounded` | bool | — | Ground check result from PlayerController |
| `isJetpacking` | bool | — | Active jetpack state from PlayerJetpack |
| `isSecondaryBoosting` | bool | — | Active dash state from SecondaryBooster |
| `facingRight` | bool | true | Current sprite facing direction |

### 5.3 Derived Values

| Derived Value | Formula | Result |
|---|---|---|
| Time to full speed (ground) | moveSpeed / groundAcceleration | 0.05s (~2.5 frames at 50Hz) |
| Time to full stop (ground) | moveSpeed / groundDeceleration | 0.05s (~2.5 frames at 50Hz) |
| Time to full speed (air) | moveSpeed / (groundAcceleration * airMult) | 0.077s (~3.8 frames at 50Hz) |
| Time to full stop (air) | moveSpeed / (groundDeceleration * airMult) | 0.077s (~3.8 frames at 50Hz) |
| Effective air acceleration | groundAcceleration * airMult | 78 units/sec^2 |
| Effective air deceleration | groundDeceleration * airMult | 78 units/sec^2 |

---

## 6. Edge Cases

| Edge Case | Expected Behavior | Rationale |
|---|---|---|
| Input direction reverses mid-air | Decel to 0 then accel in new direction, both at air rate | Air reversal should feel committed but not locked |
| Jetpack ends with high X velocity | Movement resumes from that velocity, decelerates normally | Preserves momentum from jetpack; player can steer out of it |
| Secondary boost ends (velocity zeroed) | Movement resumes from zero | Dash is a position tool, not a momentum tool |
| Input magnitude near zero (< 0.01) | Treated as no input; deceleration applies | Dead zone prevents drift from analog stick noise |
| Flip called while stationary | Scale flips, no velocity change | Sprite facing updates even at zero speed |
| Both jetpack and secondary boost active | First check (jetpack) returns early; secondary check never reached | Should not occur in practice; jetpack and secondary are mutually exclusive |
| Landing while moving at air speed | Ground accel/decel applies next tick; speed adjusts near-instantly | No jarring speed change since ground rates are high |
| FixedUpdate runs multiple times per frame | Each tick applies rate * fixedDeltaTime independently | MoveTowards is frame-rate independent by design |

---

## 7. Dependencies

| Dependency | Type | Direction | Description |
|---|---|---|---|
| **Rigidbody2D** | Unity Component | Required | Velocity target; must exist on same GameObject |
| **PlayerController** | Script | Upstream | Calls `Tick()` in FixedUpdate, provides input and state flags |
| **Input System** | Unity Package | Indirect | Input is read by PlayerController and passed as parameters |
| **PlayerJetpack** | Script | Peer | Provides `isJetpacking` flag; suppresses movement |
| **SecondaryBooster** | Script | Peer | Provides `isSecondaryBoosting` flag; suppresses movement |
| **PlayerAnimator** | Script | Downstream | Reads facing direction and velocity for animation |

---

## 8. Tuning Knobs

| Parameter | Current Value | Safe Range | Effect of Increasing | Effect of Decreasing |
|---|---|---|---|---|
| `moveSpeed` | 6 | 4 - 10 | Faster traversal, harder precision platforming | Slower, more deliberate, easier precision |
| `groundAcceleration` | 120 | 40 - 200 | Snappier response, more "digital" feel | Smoother ramp-up, more "analog" / floaty feel |
| `groundDeceleration` | 120 | 40 - 200 | Sharper stops, more precise platforming | Slidier stops, ice-skating feel |
| `airMult` | 0.65 | 0.3 - 1.0 | More air control, easier mid-air correction | Less air control, more jump commitment, harder |

### Tuning Notes

- **Accel and decel are intentionally equal.** Celeste uses the same value for both. Asymmetric values (fast accel, slow decel) create a slidey feel that conflicts with precision platforming.
- **airMult below 0.5** makes air reversal feel sluggish and frustrating. Above 0.8, air and ground feel indistinguishable, removing the skill element of committing to a jump trajectory.
- **moveSpeed is tightly coupled to level design.** Changing it affects how far the player travels during a 1-second jetpack burst (currently 6 units ground vs 11 units jetpack). Adjust level geometry if moveSpeed changes significantly.

---

## 9. Visual/Audio Requirements

| Requirement | Type | Status | Notes |
|---|---|---|---|
| Run animation | Visual | Not Started | Sprite animation driven by PlayerAnimator when `IsRunning` is true and `IsGrounded` is true |
| Idle animation | Visual | Not Started | Plays when grounded with no horizontal input |
| Direction flip | Visual | Implemented | `transform.localScale.x *= -1`; affects all child objects |
| Footstep SFX | Audio | Not Started | Should play per-frame of run animation, not per-tick |
| Skid SFX | Audio | Not Started | Optional: play when decelerating from near-max speed to zero quickly |
| Dust particles on direction change | Visual | Not Started | Optional: small puff when reversing direction at speed |

---

## 10. Game Feel

### 10.1 Feel Reference

**Celeste (ground movement)**. The gold standard for responsive 2D platformer movement. Key characteristics:
- Pressing a direction and reaching full speed feels instantaneous.
- Releasing input stops the character within 1-2 pixels of travel.
- Air control is meaningful but noticeably reduced — the player can correct but not completely reverse a jump's trajectory for free.
- Movement never feels like it is "fighting" the player's intent.

### 10.2 Input Responsiveness

| Metric | Target | Notes |
|---|---|---|
| Input-to-movement latency | < 1 FixedUpdate tick (0.02s) | Input read in Update, applied next FixedUpdate |
| Time to max speed (ground) | < 0.1s | Must feel instant; no visible ramp |
| Time to full stop (ground) | < 0.1s | Must feel instant; no visible slide |
| Direction reversal (ground) | < 0.10s | Decel to zero + accel to full in opposite direction (2 × 6/120) |
| Direction reversal (air) | < 0.16s | Same sequence at 65% rate (2 × 6/78) |

### 10.3 Weight and Responsiveness Profile

```
Responsiveness: ████████████████████░  (Very High)
Weight:         ███░░░░░░░░░░░░░░░░░  (Very Light)
Commitment:     ████░░░░░░░░░░░░░░░░  (Low — ground)
                ████████░░░░░░░░░░░░  (Medium — air)
```

The player character should feel nimble and instantly responsive on the ground, with a subtle sense of commitment only appearing in the air. The character has almost no "weight" to horizontal movement — this is by design. Weight and commitment come from the vertical axis (gravity, jump arc, jetpack fuel), not the horizontal axis.

### 10.4 Feel Acceptance Criteria

- [ ] Pressing left/right at standstill reaches full speed with no perceptible delay
- [ ] Releasing input at full speed stops the character with no perceptible slide
- [ ] Reversing direction at full speed on the ground feels instant (no U-turn arc)
- [ ] Air strafing is noticeably slower than ground movement but still responsive
- [ ] Landing while holding a direction does not produce a speed bump or stutter
- [ ] Sprite flips immediately on input change, even while stationary
- [ ] Movement feels identical at 50Hz, 60Hz, and 144Hz (frame-rate independent)

---

## 11. UI Requirements

None. Player Movement has no UI elements. Speed, facing direction, and movement state are communicated entirely through character animation and position.

---

## 12. Cross-References

| Document | Relationship |
|---|---|
| [Player Jump](player-jump.md) | Jump adds horizontal boost (`jumpHBoost`) in input direction; movement resumes after jump launch |
| [Jetpack System](jetpack-system.md) | Suppresses movement during boost; horizontal boost halves X velocity on end |
| [Secondary Booster](secondary-booster.md) | Suppresses movement during dash; zeroes velocity on completion |
| [Fuel Feedback](fuel-feedback.md) | No direct relationship; listed for completeness |
| [Momentum/Wavedash](momentum-wavedash.md) | Planned: will inject horizontal speed from diagonal-downward dash landing |

---

## 13. Acceptance Criteria

### Functional
- [x] Horizontal velocity reaches `moveSpeed` when input is held on ground
- [x] Horizontal velocity returns to 0 when input is released on ground
- [x] Acceleration uses `MoveTowards`, not `AddForce` or `Lerp`
- [x] Air control applies `airMult` (0.65) to both acceleration and deceleration
- [x] Sprite flips via `transform.localScale.x *= -1` on direction change
- [x] Movement is suppressed during jetpack state
- [x] Movement is suppressed during secondary booster state
- [x] Y velocity is never modified by this system
- [x] Input is read in `Update`, physics applied in `FixedUpdate`
- [x] Uses `rb.linearVelocity` (Unity 6 API), not deprecated `rb.velocity`

### Integration
- [x] `Init()` receives Rigidbody2D from PlayerController
- [x] `Tick()` is called at position 6 in the FixedUpdate pipeline
- [x] `FacingRight` property is readable by external systems (PlayerAnimator, etc.)
- [x] `MoveSpeed` property is readable by external systems

### Non-Functional
- [x] Frame-rate independent (uses `Time.fixedDeltaTime`)
- [x] No allocations per frame (no `new`, no boxing, no string operations)
- [x] Dead zone threshold (0.01) prevents analog stick drift

---

## 14. Open Questions

| # | Question | Impact | Status |
|---|---|---|---|
| 1 | Should ground accel/decel be asymmetric (faster accel, slightly slower decel) for a subtle "planted" feel on stop? | Game feel | Open — current symmetric values match Celeste; test asymmetric if stops feel too abrupt |
| 2 | Should sprite flip have a 1-frame delay or animation to prevent flickering during rapid input changes? | Visual polish | Open — not an issue with current placeholder sprites; revisit with final art |
| 3 | Should `moveSpeed` be exposed to the planned `PlayerTuning` ScriptableObject? | Architecture | Open — yes, when PlayerTuning is implemented (all tuning values will migrate) |
| 4 | Does the momentum/wavedash system need to bypass `moveSpeed` cap to allow super-speed? | Gameplay design | Open — depends on wavedash design; likely needs a separate uncapped speed path |
| 5 | Should direction reversal at high speed (e.g., post-jetpack) produce a skid visual/audio cue? | Game feel / juice | Open — low priority until final art and SFX pass |
