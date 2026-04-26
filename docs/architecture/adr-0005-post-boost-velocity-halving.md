# ADR-0005: Post-Boost Velocity Halving

## Status
Accepted

## Date
2026-04-14

## Last Verified
2026-04-19

## Decision Makers
- Project lead (pz_ma)

## Summary
When the jetpack deactivates (key release or fuel empty), velocity is halved in a mode-specific way that matches Cave Story exactly: horizontal boost halves X only, upward boost halves Y only, downward boost applies no halving.

> **Update 2026-04-21**: Wall nudge has been removed from the implementation. The wall contact behavior (setting Y velocity to `wallNudgeSpeed` upward during horizontal boost) documented below is no longer active. All other velocity-halving rules remain unchanged.

## Engine Compatibility

| Field | Value |
|-------|-------|
| **Engine** | Unity 6 (6000.0.34f1) |
| **Domain** | Physics |
| **Knowledge Risk** | MEDIUM — Unity 6 is post-LLM-cutoff |
| **References Consulted** | Unity Physics2D documentation, Rigidbody2D.linearVelocity API |
| **Post-Cutoff APIs Used** | `Rigidbody2D.linearVelocity` (renamed from `velocity` in Unity 6) |
| **Verification Required** | Confirm that modifying a single axis of linearVelocity mid-flight produces correct behavior; confirm wall collision detection fires reliably during horizontal boost |

## ADR Dependencies

| Field | Value |
|-------|-------|
| **Depends On** | ADR-0003 (gravity axiom — halved velocity must interact correctly with restored gravity), ADR-0004 (boostMode must be set on activation for halving to know which rule to apply) |
| **Enables** | None — this is a terminal behavior in the boost lifecycle |
| **Blocks** | Jetpack feel cannot be finalized until halving rules are confirmed |
| **Ordering Note** | Must be implemented after ADR-0004; cannot be tested in isolation since it depends on boostMode |

## Context

### Problem Statement
When a boost ends (player releases the key or fuel runs out), the player's velocity must transition from boost-speed to a post-boost state. A naive approach (just let gravity take over at full boost velocity) feels wrong — the player retains too much momentum. Cave Story solves this with mode-specific velocity halving that creates distinct post-boost arcs for each direction. We need to replicate this exactly.

### Constraints
- Must match Cave Story's Booster 2.0 post-boost behavior for each of the 3 boost modes
- Must work with the one-shot velocity model from ADR-0004 (no per-frame velocity during boost)
- Halving must be applied in a single frame on boost termination
- ~~Wall nudge during horizontal boost must feel natural and not fight gravity~~ (REMOVED 2026-04-21 — conflicted with gravity=0 axiom)

### Requirements
- Horizontal boost (mode 1): halve X velocity, preserve Y
- Upward boost (mode 2): halve Y velocity, preserve X
- Downward boost (mode 3): no halving — full velocity maintained
- ~~Wall nudge: when horizontally boosting into a wall, set Y velocity to wallNudgeSpeed (2) upward~~ (REMOVED 2026-04-21)
- Must trigger on both key release and fuel depletion

## Decision

On boost termination (key release or fuel empty), apply velocity modification based on the recorded boostMode (set by ADR-0004):

1. **boostMode 1 (horizontal)**: Halve X velocity. `linearVelocity.x *= 0.5f`. Y velocity is untouched — gravity (ADR-0003) resumes pulling the player down from whatever Y velocity they have.
2. **boostMode 2 (up)**: Halve Y velocity. `linearVelocity.y *= 0.5f`. X velocity is untouched.
3. **boostMode 3 (down)**: No halving. Full downward velocity is maintained. This makes downward boost the most committed and punishing direction — you slam into whatever is below you.
4. ~~**Wall nudge**~~: REMOVED (2026-04-21). Was: during horizontal boost, wall contact set Y velocity to wallNudgeSpeed (2) upward. Removed because gravity=0 during boost (ADR-0003) caused this upward velocity to persist indefinitely, creating infinite wall climbing.
5. **boostMode reset**: After halving is applied, set boostMode to 0 (off).

### Architecture Diagram

```
[Boost Termination Trigger]
   (key release OR fuel == 0)
            |
            v
   [Read boostMode]
            |
     +------+------+------+
     |      |      |      |
   mode=1 mode=2 mode=3 mode=0
   (horiz) (up)  (down) (already off)
     |      |      |      |
     v      v      v      v
  vx*=0.5 vy*=0.5  nop   nop
     |      |      |      |
     +------+------+------+
            |
            v
   [boostMode = 0]
            |
            v
   [Gravity resumes (ADR-0003)]


[Wall Nudge — REMOVED 2026-04-21]
(Diagram preserved for historical context — this behavior is no longer implemented)

   [OnCollision with Ground layer wall]
            |
            v
   [boostMode == 1?] --no--> ignore
            |
           yes
            |
            v
   [vy = wallNudgeSpeed (2)]  <-- REMOVED: conflicted with gravity=0 axiom
```

### Key Interfaces

```csharp
// PlayerController (partial)
private float wallNudgeSpeed = 2f;

void DeactivateBoost()
{
    switch (boostMode)
    {
        case 1: // horizontal
            rb.linearVelocity = new Vector2(
                rb.linearVelocity.x * 0.5f,
                rb.linearVelocity.y);
            break;
        case 2: // up
            rb.linearVelocity = new Vector2(
                rb.linearVelocity.x,
                rb.linearVelocity.y * 0.5f);
            break;
        case 3: // down — no halving
            break;
    }
    boostMode = 0;
    isBoosting = false;
}

// Wall nudge code REMOVED (2026-04-21) — conflicted with gravity=0 axiom.
// Was: OnCollisionStay2D set vy = wallNudgeSpeed during horizontal boost.
```

## Alternatives Considered

### Alternative 1: Uniform Halving on All Axes
- **Description**: On boost end, halve both X and Y velocity regardless of boost direction
- **Pros**: Simpler implementation — one rule for all cases; easier to reason about
- **Cons**: Loses the directional nuance that makes Cave Story's post-boost feel distinctive; downward boost loses its "slam" quality; horizontal boost loses its clean arc
- **Rejection Reason**: The mode-specific halving is what creates the distinct post-boost trajectories for each direction. Uniform halving homogenizes the feel and departs from Cave Story.

### Alternative 2: No Halving / Instant Stop
- **Description**: On boost end, either preserve full velocity or set it to zero
- **Pros**: Full velocity preservation is trivially simple; instant stop is easy to predict
- **Cons**: Full velocity feels uncontrolled — player rockets away at boostSpeed after releasing. Instant stop feels abrupt and unnatural, like hitting an invisible wall
- **Rejection Reason**: Both extremes feel wrong. Halving is the sweet spot that Cave Story found — enough momentum to arc gracefully, not so much that you overshoot.

### Alternative 3: Gradual Deceleration Curve
- **Description**: Instead of instant halving, apply a deceleration over multiple frames (e.g., lerp velocity down over 0.2 seconds)
- **Pros**: Smoother transition from boost to freefall; more "realistic" physics feel
- **Cons**: Introduces frame-dependent behavior; makes post-boost trajectory less predictable; feels floaty compared to Cave Story's crisp transitions; harder to design precise platforming around
- **Rejection Reason**: The instant halving is a deliberate design choice. The crisp transition from boost-speed to half-speed is part of what makes the movement feel precise and game-like rather than simulation-like.

## Consequences

### Positive
- Each boost direction produces a distinct, predictable post-boost arc that players can learn and master
- Downward boost being the "no halving" direction makes it the most committed choice, adding risk/reward depth
- ~~Wall nudge prevents frustrating wall-sticking during horizontal boost~~ (REMOVED 2026-04-21)
- Matches Cave Story exactly — players who know the source material get the behavior they expect

### Negative
- Three separate halving rules add implementation complexity compared to a uniform approach
- ~~Wall nudge introduces an additional collision check during horizontal boost~~ (REMOVED 2026-04-21)
- The asymmetry (down = no halving) may be unintuitive for new players who expect consistent behavior

### Risks
- **Risk**: Halving velocity on the same frame as boost termination could interact poorly with Unity's physics step timing
  - **Mitigation**: Apply halving in FixedUpdate to ensure it aligns with the physics step; test frame-by-frame to verify the velocity write takes effect before the next physics solve
- ~~**Risk**: Wall nudge wallNudgeSpeed (2) may feel too weak or too strong depending on gravity settings~~ (REMOVED 2026-04-21 — wall nudge no longer exists)
  - ~~**Mitigation**: Expose wallNudgeSpeed as a tunable constant; test alongside gravity (ADR-0003) values~~
- **Risk**: Edge case where fuel depletes and key release happen on the same frame could apply halving twice
  - **Mitigation**: Guard with boostMode check — if boostMode is already 0, skip halving

## GDD Requirements Addressed

| GDD System | Requirement | How This ADR Addresses It |
|------------|-------------|--------------------------|
| jetpack-system.md | Horizontal boost halves X velocity on release | DeactivateBoost case 1: `vx *= 0.5` |
| jetpack-system.md | Upward boost halves Y velocity on release | DeactivateBoost case 2: `vy *= 0.5` |
| jetpack-system.md | Downward boost applies no halving | DeactivateBoost case 3: no-op |
| ~~jetpack-system.md~~ | ~~Wall nudge during horizontal boost~~ | ~~OnCollisionStay2D sets `vy = wallNudgeSpeed (2)` on wall contact during mode 1~~ REMOVED 2026-04-21 |

## Performance Implications
- **CPU**: Negligible — halving is a single multiplication on one frame
- **Memory**: Negligible — boostMode (int) already allocated by ADR-0004
- **Load Time**: No impact
- **Network**: N/A — single-player game

## Migration Plan
If a previous boost deactivation implementation exists, replace it with the mode-specific switch in DeactivateBoost. Remove any uniform halving or deceleration curves from prior implementations. (Wall nudge migration is no longer applicable — feature removed 2026-04-21.)

## Validation Criteria
- Horizontal boost followed by release: measured X velocity is exactly half of boostSpeed (9.5); Y velocity unchanged from pre-release value
- Upward boost followed by release: measured Y velocity is exactly half of boostSpeed (9.5); X velocity unchanged
- Downward boost followed by release: both X and Y velocity unchanged from pre-release values
- ~~Wall nudge: during horizontal boost, contacting a wall produces Y velocity of exactly 2 (upward)~~ — removed 2026-04-21; wall nudge is no longer implemented
- Fuel depletion triggers the same halving as key release for the active boost mode
- Double-halving cannot occur (verify boostMode resets to 0 after first deactivation)

## Related Decisions
- ADR-0003: Gravity axiom — gravity resumes after halving, shaping the post-boost arc
- ADR-0004: Booster activation pattern — sets the boostMode that this ADR reads
- ADR-0006: Diegetic fuel feedback — fuel-empty event triggers this ADR's deactivation
- ADR-0007: Physics layer allocation — wall detection uses Ground layer (8)
- GDD: `design/gdd/jetpack-system.md`
