# Technical Preferences

<!-- Populated from CLAUDE.md and project inspection (2026-04-19). -->
<!-- All agents reference this file for project-specific standards and conventions. -->

## Engine & Language

- **Engine**: Unity 6 (6000.0.34f1)
- **Language**: C#
- **Rendering**: 2D / URP (Universal Render Pipeline) / Orthographic camera
- **Physics**: Physics2D (gravity: -20, continuous collision detection)

## Input & Platform

- **Target Platforms**: PC (Windows)
- **Input Methods**: Keyboard/Mouse, Gamepad
- **Primary Input**: Keyboard
- **Gamepad Support**: Full (Left Stick/D-pad, Button South=Jump, RT=Jetpack, Button West=Fire)
- **Touch Support**: None
- **Platform Notes**: New Input System (not legacy). InputActionAsset loaded at runtime via Resources.Load. No PlayerInput component on GameObjects.

## Naming Conventions

- **Classes**: PascalCase (e.g., PlayerController, JetpackGas, RoomManager)
- **Variables**: camelCase (e.g., moveSpeed, boostSpeed, coyoteTimeCounter)
- **Signals/Events**: On + PascalCase (e.g., OnGasChanged, OnGasEmpty, OnGasRecharged)
- **Files**: PascalCase.cs (e.g., PlayerJump.cs, SecondaryBooster.cs)
- **Scenes/Prefabs**: PascalCase (e.g., TestRoom.unity)
- **Constants**: UPPER_SNAKE_CASE or static readonly camelCase (C# convention)

## Performance Budgets

- **Target Framerate**: 60 fps
- **Frame Budget**: 16.67ms
- **Draw Calls**: No hard limit (2D pixel art, minimal draw calls expected)
- **Memory Ceiling**: No hard limit (small 2D project)

## Testing

- **Framework**: Unity Test Runner (NUnit) — not yet configured
- **Minimum Coverage**: Not yet defined
- **Required Tests**: Balance formulas, gameplay systems (jump feel, jetpack timing)

## Forbidden Patterns

- DO NOT use `rb.velocity` — deprecated in Unity 6. MUST use `rb.linearVelocity`.
- DO NOT use `WasPressedThisFrame()` for button edge detection — unreliable depending on Input System update mode. MUST use manual edge detection (track previous frame state via `IsPressed()`).
- DO NOT use Dynamic Rigidbody2D for platforms/ground — MUST be Static, otherwise they fall.
- DO NOT use `Find()`, `FindObjectOfType()` in production code — use dependency injection or Inspector references.
- DO NOT use `SendMessage()` — use direct references or events.
- DO NOT set velocity in Update() — all physics in FixedUpdate().
- Ground check `groundLayer` mask may revert to 0 in scene file — code MUST have fallback to layer 8 in Awake().
- All sprites: Point filtering, no compression, 16 pixels per unit.

## Allowed Libraries / Addons

- Unity Input System (com.unity.inputsystem) — actively used
- Unity 2D Physics (Physics2D) — actively used
- Unity 2D Animation — placeholder, not yet connected
- TextMeshPro — for UI text if needed

## Architecture Decisions Log

- ADR-0001: Component-Based Player Architecture (pending creation)
- ADR-0002: Input Timing Strategy (pending creation)
- ADR-0003: Deterministic Gravity During Propulsion (pending creation)
- ADR-0004: Booster 2.0 Activation Pattern (pending creation)
- ADR-0005: Post-Boost Velocity Halving (pending creation)
- ADR-0006: Diegetic Fuel Feedback System (pending creation)
- ADR-0007: Physics Layer Allocation (pending creation)

## Engine Specialists

- **Primary**: unity-specialist
- **Language/Code Specialist**: unity-specialist (C# / MonoBehaviour patterns)
- **Shader Specialist**: unity-shader-specialist (minimal use — 2D pixel art)
- **UI Specialist**: unity-ui-specialist (minimal UI philosophy — diegetic feedback preferred)
- **Additional Specialists**: unity-dots-specialist (not needed currently — standard MonoBehaviour architecture)
- **Routing Notes**: Most work routes to unity-specialist. Shader specialist only if custom shader work begins. UI specialist if HUD/menu work begins.

### File Extension Routing

| File Extension / Type | Specialist to Spawn |
|-----------------------|---------------------|
| Game code (.cs) | unity-specialist |
| Shader / material files (.shader, .shadergraph) | unity-shader-specialist |
| UI / screen files (Canvas, UI Toolkit) | unity-ui-specialist |
| Scene / prefab / level files (.unity, .prefab) | unity-specialist |
| Native extension / plugin files | unity-specialist |
| General architecture review | unity-specialist |
