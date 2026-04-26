# ADR-0004: Booster 2.0 Activation Pattern

## Status
Accepted (Updated 2026-04-26: boostSpeed tuned from 19 to 11, wall nudge removed. Activation pattern unchanged.)

## Date
2026-04-14

## Last Verified
2026-04-26

## Decision Makers
- Project lead (pz_ma)

## Summary
Jetpack uses press-to-activate (not hold), 4 cardinal directions only. Direction is determined by the most recently pressed direction key (edge-detected). Velocity is set ONCE on activation to boostSpeed (11) in the chosen direction, with the perpendicular axis zeroed. This matches Cave Story's Booster 2.0 exactly.

## Engine Compatibility

| Field | Value |
|-------|-------|
| **Engine** | Unity 6 (6000.0.34f1) |
| **Domain** | Physics |
| **Knowledge Risk** | MEDIUM — Unity 6 is post-LLM-cutoff |
| **References Consulted** | Unity Physics2D documentation, Rigidbody2D.linearVelocity API |
| **Post-Cutoff APIs Used** | `Rigidbody2D.linearVelocity` (renamed from `velocity` in Unity 6) |
| **Verification Required** | Confirm that setting linearVelocity directly in a single frame produces the expected deterministic movement; confirm no Physics2D interpolation smoothing interferes with the one-shot velocity set |

## ADR Dependencies

| Field | Value |
|-------|-------|
| **Depends On** | ADR-0003 (gravity axiom — boost must override gravity's per-frame pull) |
| **Enables** | ADR-0005 (post-boost velocity halving requires knowing which boost mode was active) |
| **Blocks** | Jetpack epic stories cannot be implemented until this activation model is finalized |
| **Ordering Note** | ADR-0003 must be Accepted first since boost activation temporarily suspends normal gravity behavior |

## Context

### Problem Statement
The jetpack is the core mechanic of Project Jetpack. We need to define exactly how boosting activates — the input pattern, directional model, and how velocity is applied. This is the single most important feel decision in the game and must match Cave Story's Booster 2.0 precisely, as that is the design anchor.

### Constraints
- Must feel identical to Cave Story's Booster 2.0 for players familiar with that game
- Must work with Unity's Physics2D system without fighting the physics engine
- Must support exactly 4 cardinal directions (no diagonals)
- Input detection must be edge-based (key-down event), not continuous polling
- Velocity application happens once, not per-frame, to preserve committed movement feel

### Requirements
- Must support up, down, left, right boost directions
- Must use the most recently pressed direction key to resolve direction
- Must set velocity to boostSpeed (11) in the chosen axis and zero the perpendicular axis
- Must integrate with the fuel system (ADR-0006) for duration limiting
- Must integrate with post-boost velocity halving (ADR-0005) on release/fuel-empty

## Decision

The jetpack activates on a single key press (not hold). On the frame the boost key is pressed:

1. **Direction resolution**: Read the most recently pressed cardinal direction key using edge detection (Input.GetKeyDown or equivalent). If no direction key is held, default to the direction the player is facing (horizontal).
2. **Velocity set**: Set Rigidbody2D.linearVelocity to boostSpeed (11) in the resolved direction. Zero the perpendicular axis. This is a one-shot assignment, not an additive force.
3. **Boost mode**: Record the current boost mode (1=horizontal, 2=up, 3=down) for use by ADR-0005's velocity halving on release.
4. **Fuel consumption**: Begin draining fuel at the configured rate. Boost continues until fuel is empty or the player releases the boost key.
5. **During boost**: No additional velocity is applied per-frame. The player travels at the set velocity until gravity (ADR-0003), collision, or boost termination modifies it.

### Architecture Diagram

```
[Input: Boost Key Down]
        |
        v
[Edge-Detect Direction Key] --> resolvedDir (up/down/left/right)
        |
        v
[Set linearVelocity]
   resolvedDir * boostSpeed (11)
   perpendicular axis = 0
        |
        v
[Record boostMode] --> 1=horiz, 2=up, 3=down
        |
        v
[Begin Fuel Drain]
        |
        v
[Boost Active — no per-frame velocity writes]
        |
   (fuel empty OR key release)
        |
        v
[ADR-0005: Mode-Specific Velocity Halving]
```

### Key Interfaces

```csharp
// PlayerController (partial)
private float boostSpeed = 11f;
private int boostMode; // 0=off, 1=horizontal, 2=up, 3=down

void ActivateBoost(Vector2 direction)
{
    rb.linearVelocity = direction * boostSpeed;
    boostMode = ResolveBoostMode(direction);
    isBoosting = true;
}

int ResolveBoostMode(Vector2 dir)
{
    if (dir.y > 0) return 2;  // up
    if (dir.y < 0) return 3;  // down
    return 1;                  // horizontal
}
```

## Alternatives Considered

### Alternative 1: Hold-to-Boost with Continuous Thrust
- **Description**: Player holds the boost key and thrust is applied every frame as a force or velocity addition, similar to a traditional jetpack
- **Pros**: More intuitive for players unfamiliar with Cave Story; allows mid-boost steering; smoother feel
- **Cons**: Less precise; players can course-correct mid-air, removing the commitment that makes Cave Story's movement rewarding; harder to balance level design around unpredictable trajectories
- **Rejection Reason**: Violates the core design goal of matching Cave Story's Booster 2.0. The committed, one-shot nature of the boost is what makes it satisfying to master.

### Alternative 2: 8-Directional Boost
- **Description**: Allow diagonal boost directions (up-left, up-right, down-left, down-right) in addition to the 4 cardinal directions
- **Pros**: More movement options; feels more flexible; diagonal shortcuts through levels
- **Cons**: Dramatically increases level design complexity; diagonal movement at boostSpeed covers more ground per frame; harder for players to predict exact trajectories; departs from Cave Story's model
- **Rejection Reason**: 4-directional constraint is a deliberate design choice that makes boost decisions cleaner and level design more tractable. Cave Story uses 4 directions for good reason.

### Alternative 3: Per-Frame Velocity Application
- **Description**: Instead of setting velocity once, apply the boost velocity every physics frame while boosting is active
- **Pros**: Smoother movement; boost overrides any external forces consistently; simpler mental model for the physics
- **Cons**: Fights against gravity and collision responses; creates unrealistic feeling of unstoppable movement; not how Cave Story's Booster 2.0 works; makes post-boost halving (ADR-0005) less meaningful
- **Rejection Reason**: The one-shot velocity set is what creates the "committed trajectory" feel. Per-frame application would make the player feel like a rocket rather than a projectile, losing the Cave Story DNA.

## Consequences

### Positive
- Exactly matches Cave Story's Booster 2.0 feel — players familiar with the source material will immediately understand the system
- Creates a "committed movement" mechanic where boost direction is a meaningful, weighty decision
- Simple implementation: one velocity write on activation, no per-frame boost logic
- Clean integration with post-boost velocity halving (ADR-0005)
- Level design can rely on predictable boost trajectories for precise platforming challenges

### Negative
- Steeper learning curve for players unfamiliar with Cave Story — press-to-activate is unusual
- No mid-boost course correction means mistimed boosts are fully punishing
- Edge-detected direction input requires careful handling of simultaneous key presses

### Risks
- **Risk**: Unity's Physics2D interpolation or collision resolution could modify the set velocity mid-boost in unexpected ways
  - **Mitigation**: Test with various collision scenarios; if needed, re-assert velocity on collision exit during active boost
- **Risk**: Input buffering or frame-timing issues could cause the wrong direction to be read on activation
  - **Mitigation**: Use a direction key priority stack that always reflects the most recently pressed key, updated on both key-down and key-up events
- **Risk**: boostSpeed value of 11 (tuned down from 19 during movement feel pass 2026-04-19)
  - **Mitigation**: Expose boostSpeed as a configurable constant; validate against level geometry during playtesting

## GDD Requirements Addressed

| GDD System | Requirement | How This ADR Addresses It |
|------------|-------------|--------------------------|
| jetpack-system.md | Booster 2.0 activation must match Cave Story's press-to-activate model | Defines exact input pattern: edge-detected key press, not hold |
| jetpack-system.md | 4 cardinal directions only | Restricts direction resolution to up/down/left/right |
| jetpack-system.md | Velocity set once to boostSpeed on activation | One-shot linearVelocity assignment of 11 in chosen direction |
| jetpack-system.md | Perpendicular axis zeroed on activation | Explicitly zeros the axis perpendicular to boost direction |

## Performance Implications
- **CPU**: Negligible — boost activation is a single velocity write on one frame; no per-frame boost computation during active boost
- **Memory**: Negligible — stores boostMode (int) and isBoosting (bool)
- **Load Time**: No impact
- **Network**: N/A — single-player game

## Migration Plan
If a previous boost implementation exists using hold-to-boost or per-frame application, replace the boost activation method with the one-shot velocity set pattern. Update the input handler to use edge detection for direction resolution. Ensure boostMode is recorded on activation for ADR-0005 integration.

## Validation Criteria
- Activating boost in each of the 4 cardinal directions sets linearVelocity to exactly (boostSpeed, 0), (-boostSpeed, 0), (0, boostSpeed), or (0, -boostSpeed)
- Perpendicular axis reads 0.0 immediately after activation
- No additional velocity writes occur during active boost (verify with debug logging)
- Direction resolves to the most recently pressed key when multiple direction keys are held
- Boost activates on key-down frame only — holding the key does not re-trigger
- A Cave Story player can pick up the controls and confirm the feel matches Booster 2.0

## Related Decisions
- ADR-0003: Gravity axiom — defines the baseline physics that boost overrides
- ADR-0005: Post-boost velocity halving — depends on boostMode set by this ADR
- ADR-0006: Diegetic fuel feedback — visual/audio feedback during boost
- ADR-0007: Physics layer allocation — defines collision layers boost interacts with
- GDD: `design/gdd/jetpack-system.md`
