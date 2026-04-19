# ADR-0008: Central Event Bus

## Status

Accepted

## Date

2026-04-20

## Last Verified

2026-04-20

## Decision Makers

Developer + Claude Code

## Summary

Introduce a static generic event bus (`GameEventBus`) as the central pub/sub system decoupling Track A (player systems) from Track B (level, camera, gimmicks). This enables the swappable secondary mode system (dash → gun → future modes) and the gimmick framework to communicate without direct references.

## Engine Compatibility

| Field | Value |
|-------|-------|
| **Engine** | Unity 6 (6000.0.34f1) |
| **Domain** | Core / Architecture |
| **Knowledge Risk** | LOW — uses standard C# generics, no Unity-specific API |
| **References Consulted** | None needed — pure C# pattern |
| **Post-Cutoff APIs Used** | None |
| **Verification Required** | None |

## ADR Dependencies

| Field | Value |
|-------|-------|
| **Depends On** | None — foundational infrastructure |
| **Enables** | Gimmick framework, Chapter Configuration, Room transitions, Camera events, Booster mode swapping |
| **Blocks** | All Track B work that needs to communicate with player systems |
| **Ordering Note** | Must be implemented before any gimmick or room transition work |

## Context

### Problem Statement

Track A (Player/) and Track B (Level/, Camera/, Gimmicks/) need to communicate without direct coupling. Currently, any system that needs to react to player actions must hold a direct reference to the specific player component. This creates tight coupling that makes the swappable mode system (dash → gun → future modes) difficult — every subscriber would need to know about every possible mode implementation.

### Current State

- Player systems fire C# events on individual components (e.g., `JetpackGas.OnGasChanged`)
- Subscribers need direct references to specific components
- No way for gimmicks to affect player without knowing exact component structure
- No way for camera to react to game events without direct player reference
- The planned BoosterSwapZone gimmick can't notify systems of mode changes

### Constraints

- Solo developer — must be simple to use and debug
- Small project — no need for enterprise-scale event systems
- Must support typed event data (not stringly-typed)
- Must work with Unity's lifecycle (subscribe in OnEnable, unsubscribe in OnDisable)
- Must support the secondary mode swap use case: systems subscribe to `SecondaryUsed` regardless of whether the active mode is dash, gun, or something else

### Requirements

- Publish/subscribe from any script without requiring references to other systems
- Typed event structs with compile-time safety
- Zero setup per event (no ScriptableObject assets or Inspector wiring)
- Easy to add new events as systems are built
- Negligible performance overhead

## Decision

Implement `GameEventBus` as a **static generic class** using `Dictionary<Type, Delegate>` for event storage. Events are C# structs published via `GameEventBus.Publish<T>(T evt)` and subscribed via `GameEventBus.Subscribe<T>(Action<T> handler)`.

### Architecture

```
[PlayerJump]                          [RoomCamera]
     |                                     |
     | Publish<PlayerLanded>(evt)          | Subscribe<PlayerLanded>(OnLanded)
     v                                     ^
  +------------------------------------------+
  |            GameEventBus                  |
  |  Dictionary<Type, Delegate>              |
  |                                          |
  |  Subscribe<T>(Action<T>)                 |
  |  Unsubscribe<T>(Action<T>)              |
  |  Publish<T>(T eventData)                 |
  |  Clear()                                 |
  +------------------------------------------+
     ^                                     |
     | Subscribe<GimmickActivated>(...)    | Publish<RoomEntered>(evt)
     |                                     |
[SecondaryBooster]                    [RoomManager]
```

### Key Interfaces

```csharp
// The bus — ~30 lines, static, generic
public static class GameEventBus
{
    public static void Subscribe<T>(Action<T> handler) where T : struct;
    public static void Unsubscribe<T>(Action<T> handler) where T : struct;
    public static void Publish<T>(T eventData) where T : struct;
    public static void Clear(); // For scene transitions
}

// Event structs — one per event type
public struct PlayerLanded { public Vector2 Position; public float FallSpeed; }
public struct PlayerDied { public Vector2 Position; }
public struct PlayerJumped { public Vector2 Position; }
public struct SecondaryUsed { public Vector2 Direction; public string ModeName; }
public struct RoomEntered { public Room Room; public Vector2 SpawnPoint; }
public struct GimmickActivated { public string GimmickId; }
```

### Implementation Guidelines

- Events are **structs** (no heap allocation, no GC pressure)
- `where T : struct` constraint enforces value types
- Subscribers must unsubscribe in `OnDisable()` to prevent stale references
- `Clear()` called on scene load to prevent cross-scene leaks
- All event structs live in a single file (`Assets/Scripts/Core/GameEvents.cs`) for discoverability
- The bus itself lives at `Assets/Scripts/Core/GameEventBus.cs`

## Alternatives Considered

### Alternative 1: Singleton MonoBehaviour

- **Description**: Event bus as a MonoBehaviour on a DontDestroyOnLoad GameObject
- **Pros**: Can use coroutines for delayed events, visible in Hierarchy, familiar Unity pattern
- **Cons**: Requires scene setup, tempting to add logic to the singleton, slightly more boilerplate
- **Estimated Effort**: Similar to static class
- **Rejection Reason**: Added complexity without benefit for this project. Coroutine-delayed events not needed — all events are synchronous.

### Alternative 2: ScriptableObject Event Channels

- **Description**: Each event type is a ScriptableObject asset. Systems reference channels in the Inspector.
- **Pros**: Most decoupled — Inspector wiring visible, designers can configure without code, each channel is independently debuggable
- **Cons**: Must create an asset per event type (~20+ assets), Inspector wiring for every subscriber, overkill for solo dev
- **Estimated Effort**: 2-3× more setup than static class
- **Rejection Reason**: Too much ceremony for a solo project with 15-20 event types. The benefit of Inspector visibility doesn't outweigh the setup cost.

### Alternative 3: Direct C# Events on Components (Current Approach)

- **Description**: Keep using `event Action<T>` on individual components (like JetpackGas.OnGasChanged)
- **Pros**: Zero infrastructure, simple, already working for fuel events
- **Cons**: Requires direct references to publishers, tight coupling, can't support swappable modes (subscribers need to know the exact component), Track B can't react to Track A events without references
- **Rejection Reason**: Doesn't scale to the gimmick framework or mode swapping requirements.

## Consequences

### Positive

- Track A and Track B fully decoupled — can be developed in parallel without merge conflicts
- Swappable secondary modes publish the same event type regardless of implementation
- New gimmicks can react to player events without touching player code
- Camera, audio, VFX systems subscribe once and respond to any publisher
- Zero allocation per event (struct-based)

### Negative

- No compile-time guarantee that an event has subscribers (fire-and-forget by design)
- Debugging requires logging in the bus — can't inspect subscriptions in Unity Inspector
- Must remember to unsubscribe in OnDisable or risk stale callbacks

### Neutral

- Existing component events (JetpackGas.OnGasChanged) can coexist — migrate gradually, not all at once

## Risks

| Risk | Probability | Impact | Mitigation |
|------|------------|--------|-----------|
| Forgotten unsubscribe causes stale callbacks | Medium | Low | Convention: always pair Subscribe in OnEnable with Unsubscribe in OnDisable |
| Event ordering dependencies | Low | Medium | Document expected publish order; keep events independent where possible |
| Too many event types become hard to track | Low | Low | All events in one file (GameEvents.cs) for discoverability |

## Performance Implications

| Metric | Before | Expected After | Budget |
|--------|--------|---------------|--------|
| CPU (per event publish) | N/A | <0.01ms (dictionary lookup + delegate invoke) | 0.1ms |
| Memory | N/A | ~1KB (dictionary + delegates) | Negligible |
| GC Allocation | N/A | Zero per publish (struct events) | Zero |

## Migration Plan

1. Create `Assets/Scripts/Core/GameEventBus.cs` and `Assets/Scripts/Core/GameEvents.cs`
2. Start with 3-4 core events: PlayerLanded, PlayerDied, RoomEntered, SecondaryUsed
3. Migrate existing component events gradually (JetpackGas can keep its own events AND publish to the bus)
4. Track B systems subscribe to bus events instead of requiring Track A references

**Rollback plan**: Delete the two files. Revert to direct references. No other code changes needed since adoption is gradual.

## Validation Criteria

- [ ] Systems can publish and subscribe without direct references to each other
- [ ] SecondaryBooster publishes `SecondaryUsed` regardless of active mode (dash or gun)
- [ ] Camera can subscribe to player events without importing player scripts
- [ ] No GC allocation on event publish (verified via Profiler)
- [ ] Unsubscribe prevents stale callbacks after object destruction

## GDD Requirements Addressed

| GDD Document | System | Requirement | How This ADR Satisfies It |
|-------------|--------|-------------|--------------------------|
| `design/gdd/secondary-booster.md` | Secondary Booster | Swappable mode system (dash → gun) | Modes publish same event type; subscribers don't know which mode is active |
| `design/gdd/fuel-feedback.md` | Fuel Feedback | Fuel state communicated to presentation systems | Fuel events published to bus; particles/audio subscribe without direct JetpackGas reference |
| `design/gdd/systems-index.md` | Gimmick Framework | Gimmicks affect gameplay without direct player coupling | Gimmicks publish/subscribe via bus |

## Related

- ADR-0001: Component architecture — bus replaces the need for external systems to reference PlayerController properties
- ADR-0006: Diegetic fuel feedback — feedback systems can subscribe to fuel events via bus
- Planned: Gimmick Framework, Chapter Configuration, Room transitions all depend on this bus
