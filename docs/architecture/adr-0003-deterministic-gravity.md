# ADR-0003: Deterministic Gravity During Propulsion

| Field        | Value                          |
|--------------|--------------------------------|
| **Status**   | Accepted                       |
| **Date**     | 2026-04-14                     |
| **Deciders** | Project Jetpack maintainers    |
| **Engine**   | Unity 6 (6000.0.34f1), Physics2D |

## Context

Project Jetpack is a 2D pixel platformer combining Cave Story-style jetpack mechanics with Celeste-style stage design. A core player experience goal is that the player must be able to predict exactly where they will end up when they activate any propulsion ability. If gravity silently modifies velocity during propelled states, the resulting trajectory becomes opaque to the player and undermines the precision-platforming promise of the game.

The project uses `Physics2D` with a project gravity of **-20**.

### GDD Requirements

| Requirement Source         | Relevant Aspect                |
|----------------------------|--------------------------------|
| `jetpack-system.md`        | Deterministic trajectory       |
| `player-jump.md`           | Apex gravity                   |
| `secondary-booster.md`     | Gravity suppression            |

## Decision

**Gravity = 0 during ALL propelled states.** This is a design axiom -- the "deterministic air movement axiom" -- not merely an implementation detail. It is a foundational rule that downstream systems depend on.

### Rules

1. **Jetpack activation**: Gravity is suppressed entirely. Velocity is set once in the chosen direction at the moment of activation; no gravitational acceleration is applied for the duration of the boost.

2. **Secondary boost**: Gravity is suppressed for the full boost duration (0.2s). Velocity is locked for that window.

3. **Post-boost snap-back**: When a non-jump upward velocity exists after an upward boost ends, fast-fall gravity (2x base) applies immediately. There is no floating; the player snaps into a downward trajectory.

4. **Apex gravity**: A reduced gravity multiplier (0.5x) applies **only** when all of the following are true:
   - The player is holding jump.
   - `|vy| < apexThreshold` (4 units/sec).
   - The player is **not** in any propelled state.

5. **Unpropelled airborne state**: Standard gravity and fall modifiers apply only when the player is airborne and no propulsion system is active.

### Constants

| Parameter                  | Value  |
|----------------------------|--------|
| Project gravity            | -20    |
| `fallGravityMultiplier`    | 2.0    |
| `apexGravityMultiplier`    | 0.5    |
| `apexThreshold`            | 2.5 units/sec |
| `maxFallSpeed`             | 20     |
| Secondary boost duration   | 0.15s  |

### State-Gravity Matrix

| Player State                        | Gravity Applied         |
|-------------------------------------|-------------------------|
| Grounded                            | Normal (-20)            |
| Airborne, unpropelled, falling      | Fall gravity (2.0x)     |
| Airborne, unpropelled, apex float   | Apex gravity (0.5x)     |
| Airborne, unpropelled, rising       | Normal (-20)            |
| Jetpack boost active                | **0** (suppressed)      |
| Secondary boost active              | **0** (suppressed)      |
| Post-upward-boost (non-jump vy > 0) | Fall gravity (2.0x) immediately |

## Alternatives Considered

### 1. Apply reduced gravity during boost

Apply a fraction of gravity (e.g., 0.3x) while the jetpack is active so the player still drifts downward slightly during horizontal boosts.

- **Pros**: More physically realistic feel.
- **Cons**: Trajectory becomes unpredictable to the player. The amount of drift depends on boost duration and direction, making mental math impractical. Undermines the precision-platforming design goal.
- **Rejected**: Unpredictable.

### 2. Apply full gravity during boost and compensate with higher thrust

Keep normal gravity active and increase thrust force to overpower it, which is the approach used in many commercial games.

- **Pros**: Standard approach; familiar physics model.
- **Cons**: The player cannot predict the exact trajectory because the net movement is the result of two competing forces. Tuning becomes harder because changing gravity constants affects all boost arcs.
- **Rejected**: Player cannot predict trajectory.

### 3. Use fixed trajectory curves instead of physics

Replace physics-driven movement during boosts with predefined animation curves / spline paths.

- **Pros**: Perfectly deterministic; designer-authored arcs.
- **Cons**: Requires a significant rewrite of the movement system. Loses the emergent feel of physics-based movement. Harder to combine with wall interactions and other dynamic responses.
- **Rejected**: Would require rewrite; loses physics-based emergent behavior.

## Consequences

### Positive

- Player can visually predict landing position before activating a boost.
- Movement tuning is isolated: boost parameters and gravity parameters do not interact during propelled states, reducing coupling.
- Simplifies state reasoning in code -- each state has exactly one gravity rule.

### Negative

- Non-physical feel: horizontal boosts have zero vertical drift, which may feel "floaty" or "locked" to some players. Mitigated by post-boost snap-back applying fast-fall immediately.
- Every new propulsion mechanic must explicitly suppress gravity, or it will violate the axiom. This is a design constraint that must be communicated to contributors.

## Dependencies

| Direction    | ADR / System               | Relationship |
|--------------|-----------------------------|-------------|
| Depends on   | None                        | --          |
| Enables      | ADR-0004                    | Booster activation logic depends on the gravity axiom to reason about player state. |
| Enables      | ADR-0005                    | Velocity halving depends on knowing the player is in a deterministic (gravity-suppressed) state. |
