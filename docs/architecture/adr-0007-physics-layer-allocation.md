# ADR-0007: Physics Layer Allocation

## Status
Accepted

## Date
2026-04-14

## Last Verified
2026-04-19

## Decision Makers
- Project lead (pz_ma)

## Summary
Fixed physics layer allocation for collision filtering. Layers 8-12 are assigned to game object categories (Ground, Player, Hazard, Collectible, RoomBoundary). Sorting layers define render order as Default > Background > Tilemap > Player > Foreground > UI. A code fallback in PlayerController.Awake() forces groundLayer to layer 8 if it reads as 0, mitigating a known Unity serialization bug with LayerMask in scene files.

## Engine Compatibility

| Field | Value |
|-------|-------|
| **Engine** | Unity 6 (6000.0.34f1) |
| **Domain** | Physics / Collision |
| **Knowledge Risk** | LOW — layer-based collision is a stable Unity feature unchanged across versions |
| **References Consulted** | Unity Physics2D Layer Collision Matrix documentation, LayerMask API |
| **Post-Cutoff APIs Used** | None — layer system is unchanged in Unity 6 |
| **Verification Required** | Confirm LayerMask serialization bug persists in 6000.0.34f1; verify groundLayer fallback triggers correctly when scene value reads as 0 |

## ADR Dependencies

| Field | Value |
|-------|-------|
| **Depends On** | None (foundational) |
| **Enables** | ADR-0001 (player architecture — PlayerController uses Ground layer for ground check), ADR-0003 (gravity — ground detection depends on Ground layer raycast), ADR-0004 (booster activation — wall detection uses layer filtering), ADR-0005 (post-boost halving — depends on ground state which depends on Ground layer) |
| **Blocks** | All hazard and collectible systems require layer assignments before implementation |
| **Ordering Note** | This is a foundational ADR with no dependencies. All physics-interacting systems depend on these layer assignments being stable. |

## Context

### Problem Statement
Unity Physics2D uses a layer-based collision matrix to determine which objects interact. Without a consistent, documented layer allocation, collision filtering becomes ad-hoc — developers assign layers arbitrarily, leading to matrix conflicts, missed collisions, and debugging nightmares. Project Jetpack needs clear layer assignments for ground detection (player movement), hazard contact (kill triggers), collectible pickups (gas recharge), and room transitions (stage progression).

### Constraints
- Unity reserves layers 0-7 for built-in purposes; custom layers must use 8-31
- Layer names are project-global and shared across all scenes
- The Physics2D collision matrix is a global setting — layer interactions affect every scene
- LayerMask fields in MonoBehaviours have a known serialization bug where scene-file values can revert to 0 on reimport
- Sorting layers are independent of physics layers but must also be allocated to avoid render-order conflicts

### Requirements
- Ground check raycast in PlayerController must reliably detect only ground surfaces
- Hazard objects must trigger player death on contact without affecting other object types
- Collectible objects must trigger pickup logic on player contact only
- Room boundary triggers must detect player entry for room transitions
- Render order must place the player above tilemap terrain but below foreground decoration and UI

## Decision

### Physics Layer Allocation

| Layer | Name | Purpose | Tags Used |
|-------|------|---------|-----------|
| 0-7 | (Unity reserved) | Built-in layers — do not modify | — |
| 8 | Ground | Terrain and solid platforms. Used by PlayerController ground check raycast. | — |
| 9 | Player | The player character. Used for collision matrix filtering against hazards, collectibles, and room boundaries. | Player |
| 10 | Hazard | Objects that kill the player on trigger contact (spikes, projectiles, environmental dangers). | Hazard |
| 11 | Collectible | Gas recharge pickups and future item pickups. Trigger-based interaction with Player layer only. | GasRecharge |
| 12 | RoomBoundary | Invisible trigger zones that initiate room transitions when the player enters. | RoomTransition |

### Collision Matrix (relevant pairs)

| Layer A | Layer B | Collides? | Notes |
|---------|---------|-----------|-------|
| Player (9) | Ground (8) | Yes | Physical collision — player stands on ground |
| Player (9) | Hazard (10) | Yes (trigger) | Trigger only — kills player on contact |
| Player (9) | Collectible (11) | Yes (trigger) | Trigger only — pickup on contact |
| Player (9) | RoomBoundary (12) | Yes (trigger) | Trigger only — room transition on entry |
| Ground (8) | Hazard (10) | No | Hazards do not interact with terrain |
| Ground (8) | Collectible (11) | No | Collectibles do not interact with terrain |
| Ground (8) | RoomBoundary (12) | No | Room boundaries do not interact with terrain |
| Hazard (10) | Collectible (11) | No | No interaction between hazards and collectibles |
| Hazard (10) | RoomBoundary (12) | No | No interaction between hazards and room boundaries |
| Collectible (11) | RoomBoundary (12) | No | No interaction between collectibles and room boundaries |

### Tags

| Tag | Applied To | Purpose |
|-----|-----------|---------|
| Player | Player GameObject | Identity tag for collision callbacks |
| GasRecharge | Gas recharge pickups | Distinguishes gas pickups from future collectible types |
| RoomTransition | Room boundary triggers | Identifies room transition zones in trigger callbacks |
| Hazard | Hazard objects | Identifies hazard objects in trigger callbacks |
| SecondaryBooster | Secondary booster pickup | Reserved for future booster upgrade system |

### Sorting Layers (Render Order)

| Order | Sorting Layer | Purpose |
|-------|--------------|---------|
| 0 | Default | Unity default — fallback for unassigned objects |
| 1 | Background | Parallax backgrounds, sky, distant scenery |
| 2 | Tilemap | Terrain tiles, ground, walls, platforms |
| 3 | Player | Player sprite, player effects |
| 4 | Foreground | Foreground decoration that renders in front of the player |
| 5 | UI | HUD elements, menus, overlays |

### Known Issue: LayerMask Serialization Bug

Unity has a known serialization bug where LayerMask fields in MonoBehaviours lose their scene-file values on reimport, reverting to 0 (Nothing). This affects the `groundLayer` field in PlayerController, which must be set to layer 8 (Ground) for ground check raycasts.

**Mitigation — Code Fallback in PlayerController.Awake():**

```csharp
// PlayerController (partial)
[SerializeField] private LayerMask groundLayer;

void Awake()
{
    // Fallback: Unity serialization bug can reset LayerMask to 0 on reimport
    if (groundLayer == 0)
    {
        groundLayer = 1 << 8; // Layer 8 = Ground
    }
}
```

This fallback ensures ground detection works even if the scene file loses the LayerMask value. The check runs every Awake() call with negligible cost.

### Architecture Diagram

```
[Physics2D Collision Matrix]
        |
        v
Layer 8: Ground --------+
Layer 9: Player --------+----> Collision pairs defined in matrix
Layer 10: Hazard -------+     (only Player interacts with all others)
Layer 11: Collectible --+
Layer 12: RoomBoundary -+

[PlayerController.Awake()]
        |
        v
groundLayer == 0?
   |           |
  YES          NO
   |           |
   v           v
Force to    Use serialized
1 << 8      value as-is

[Sorting Layer Stack (back to front)]
Background -> Tilemap -> Player -> Foreground -> UI
```

### Key Interfaces

```csharp
// PlayerController (partial) — ground check using layer 8
[SerializeField] private LayerMask groundLayer;

void Awake()
{
    if (groundLayer == 0)
    {
        groundLayer = 1 << 8; // Layer 8 = Ground
    }
}

bool IsGrounded()
{
    return Physics2D.Raycast(
        transform.position,
        Vector2.down,
        groundCheckDistance,
        groundLayer
    );
}

// Hazard trigger detection
void OnTriggerEnter2D(Collider2D other)
{
    if (other.gameObject.layer == 10) // Hazard layer
    {
        Die();
    }
}

// Collectible trigger detection
void OnTriggerEnter2D(Collider2D other)
{
    if (other.CompareTag("GasRecharge"))
    {
        RechargeGas();
    }
}

// Room transition trigger detection
void OnTriggerEnter2D(Collider2D other)
{
    if (other.CompareTag("RoomTransition"))
    {
        TransitionRoom(other.GetComponent<RoomBoundary>());
    }
}
```

## Alternatives Considered

### Alternative 1: Tag-Based Collision Filtering
- **Description**: Use tags instead of layers for collision filtering. Check `other.CompareTag()` in OnCollisionEnter2D/OnTriggerEnter2D callbacks without separating objects into distinct physics layers.
- **Pros**: Simpler initial setup — no collision matrix configuration needed; tags are familiar to most Unity developers; unlimited tag count vs. 32 layer limit
- **Cons**: String comparison on every collision callback is slower than layer-matrix filtering which happens at the physics engine level; no way to prevent collisions before they happen (callbacks fire even for unwanted pairs); error-prone due to string typos; cannot use LayerMask for raycast filtering
- **Rejection Reason**: Layer-based filtering is evaluated by the physics engine before callbacks fire, preventing unnecessary collision detection entirely. Tag-based filtering only works reactively after collision is detected, wasting CPU on unwanted collision pairs. Ground check raycasts specifically require LayerMask, making tags insufficient for the most critical use case.

### Alternative 2: Composite Colliders with Shared Layers
- **Description**: Use fewer layers (e.g., one "Interactable" layer for hazards, collectibles, and room boundaries) and distinguish between object types using composite colliders or component checks in callbacks.
- **Pros**: Uses fewer of the limited 32 layers; simpler collision matrix; allows grouping related objects
- **Cons**: Collision matrix cannot distinguish between hazards and collectibles on the same layer — all callbacks fire for both; requires additional runtime checks to determine object type; makes the collision matrix less readable and harder to debug; loses the ability to selectively disable entire categories of collision
- **Rejection Reason**: With only 5 custom layers (8-12), we are nowhere near the 32-layer limit. Dedicating one layer per object category provides the clearest collision matrix, the best debugging experience, and zero ambiguity in collision callbacks. Premature optimization of layer count adds complexity with no benefit.

### Alternative 3: Dynamic Layer Assignment at Runtime
- **Description**: Assign layers dynamically based on object state. For example, a collectible might switch layers when collected, or a hazard might change layers when deactivated.
- **Pros**: Maximum flexibility — objects can change collision behavior without separate prefabs; supports complex state machines where an object's collision role changes
- **Cons**: Extremely error-prone — layer changes mid-frame can cause missed or phantom collisions; hard to debug because layer state is transient; collision matrix becomes unpredictable; violates the principle of least surprise for other developers
- **Rejection Reason**: Static layer assignment is simpler, more predictable, and sufficient for Project Jetpack's needs. If an object needs to stop colliding (e.g., a collected pickup), disabling the collider or deactivating the GameObject is safer and more idiomatic than changing its layer at runtime.

## Consequences

### Positive
- Ground check raycast filters precisely to layer 8, eliminating false positives from other colliders
- Collision matrix prevents unnecessary collision detection between unrelated object categories (e.g., hazards and collectibles never interact)
- Each layer has a single, clear purpose — debugging collision issues is straightforward by checking object layer assignment
- Sorting layers guarantee consistent render order across all scenes without per-sprite sorting hacks
- The LayerMask fallback in Awake() makes ground detection resilient to the known serialization bug
- Only 5 of 24 available custom layers are used, leaving ample room for future object categories

### Negative
- Layer names and numbers must be kept in sync between Unity's Tag & Layer settings and code — renaming a layer in the editor without updating code references will break collision filtering
- The LayerMask serialization bug workaround adds a code fallback that masks the root cause rather than fixing it — developers must remember this workaround exists
- Sorting layer order is implicit (defined by position in the list, not by explicit numeric values) — reordering in the editor can break render order

### Risks
- **Risk**: A developer adds a new object type to an existing layer instead of allocating a new one, causing unintended collision interactions
  - **Mitigation**: This ADR serves as the authoritative reference for layer allocation. All new object types must be documented here before layer assignment.
- **Risk**: The LayerMask serialization bug is fixed in a future Unity version, making the Awake() fallback unnecessary dead code
  - **Mitigation**: The fallback is a no-op when groundLayer is correctly serialized (the `if` check costs nothing). Remove it only after confirming the fix in the target Unity version.
- **Risk**: Collision matrix settings in ProjectSettings are accidentally modified, breaking layer interactions
  - **Mitigation**: Version-control ProjectSettings/Physics2DSettings.asset and review changes to this file in code review.

## GDD Requirements Addressed

| GDD System | Requirement | How This ADR Addresses It |
|------------|-------------|--------------------------|
| player-movement.md | Ground check must detect only ground surfaces | Layer 8 (Ground) with LayerMask raycast filtering |
| jetpack-system.md | Wall detection for boost interaction | Layer 8 (Ground) used for wall raycast checks |
| hazard systems | Hazards kill player on trigger contact | Layer 10 (Hazard) with Player-Hazard collision pair enabled as trigger |
| collectible systems | Gas recharge pickups collected on player contact | Layer 11 (Collectible) with Player-Collectible collision pair enabled as trigger |
| room transitions | Room boundaries trigger stage transitions | Layer 12 (RoomBoundary) with Player-RoomBoundary collision pair enabled as trigger |

## Performance Implications
- **CPU**: Layer-based collision filtering is evaluated at the physics engine level (native C++ in Unity), making it faster than any script-based filtering. The collision matrix prevents broadphase checks between non-interacting layers, reducing total collision pair evaluations.
- **Memory**: Negligible — layer assignment is a single integer per GameObject; LayerMask is a single 32-bit bitmask
- **Load Time**: No impact — layers are defined in project settings and assigned at edit time
- **Network**: N/A — single-player game

## Migration Plan
1. Open Unity Editor > Edit > Project Settings > Tags and Layers
2. Assign layer names: 8=Ground, 9=Player, 10=Hazard, 11=Collectible, 12=RoomBoundary
3. Create tags: Player, GasRecharge, RoomTransition, Hazard, SecondaryBooster
4. Open Edit > Project Settings > Physics 2D and configure the collision matrix per the table above
5. Create sorting layers in order: Background, Tilemap, Player, Foreground, UI
6. Assign existing GameObjects to their correct layers and sorting layers
7. Add the groundLayer fallback to PlayerController.Awake() if not already present
8. Verify ground check raycast works in all existing scenes after layer assignment

## Validation Criteria
- PlayerController ground check raycast returns true only when standing on a Ground-layer collider, not on Hazard/Collectible/RoomBoundary colliders
- Player-Hazard trigger contact invokes OnTriggerEnter2D and kills the player
- Player-Collectible trigger contact invokes OnTriggerEnter2D and triggers pickup logic
- Player-RoomBoundary trigger contact invokes OnTriggerEnter2D and initiates room transition
- Hazard, Collectible, and RoomBoundary objects do not generate collision callbacks with each other
- groundLayer reads as layer 8 (value 256 = 1 << 8) in PlayerController at runtime, whether serialized correctly or recovered by the Awake() fallback
- Sorting layers render in correct order: Background behind Tilemap behind Player behind Foreground behind UI
- Physics2DSettings.asset in version control reflects the expected collision matrix

## Related Decisions
- ADR-0001: Component player architecture — PlayerController uses Ground layer for ground check
- ADR-0003: Deterministic gravity — ground detection depends on Ground layer raycast
- ADR-0004: Booster activation pattern — wall/ground detection uses layer filtering
- ADR-0005: Post-boost velocity halving — ground state detection depends on this ADR's layer setup
- GDD: `design/gdd/player-movement.md`
- GDD: `design/gdd/jetpack-system.md`
