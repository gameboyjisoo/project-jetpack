# Control Manifest — Project Jetpack

> **Manifest Version**: 1
> **Last Updated**: 2026-04-19
> **Engine**: Unity 6 (6000.0.34f1)
> **Language**: C#

This document defines the flat programmer rules enforced per architectural layer. Stories embed the Manifest Version at creation time; `/story-done` checks for staleness.

---

## Foundation Layer

**Systems**: Input System, Physics2D, Gravity System, Fuel System (JetpackGas)

### Required

- All physics operations use `rb.linearVelocity` (Unity 6 API), never `rb.velocity`
- Input reading in `Update()`, all physics in `FixedUpdate()`
- Button edge detection via manual previous-frame tracking (`IsPressed()` + `wasPressed`), not `WasPressedThisFrame()`
- InputActionAsset loaded via `Resources.Load<InputActionAsset>("PlayerInput")` — no PlayerInput component on GameObjects
- Enable/Disable input actions in `OnEnable()`/`OnDisable()`
- Physics2D gravity set to -20 in Project Settings
- All ground/platform Rigidbody2D must be **Static** (not Dynamic)
- Ground check uses LayerMask for layer 8; code fallback in `Awake()` if mask reads as 0

### Forbidden

- `rb.velocity` — deprecated in Unity 6
- `WasPressedThisFrame()` — unreliable across Input System update modes
- `Find()`, `FindObjectOfType()` in production code
- `SendMessage()` — use direct references or events
- Setting velocity in `Update()` — all physics in `FixedUpdate()`
- Dynamic Rigidbody2D on static geometry

### Guardrails

- Project gravity (-20) is tuned for snappy feel; changing it affects ALL movement values
- Layer 8 is reserved for Ground; layers 9-12 allocated (see ADR-0007)
- 16 PPU everywhere — 1 tile = 1 Unity unit; all sprites use Point filtering, no compression

---

## Core Layer

**Systems**: Player Movement, Jump, Jetpack, Secondary Booster, Camera, Room System

### Required

- Player components follow single-responsibility split (ADR-0001)
- Components receive references via `Init()` in PlayerController.Awake()
- FixedUpdate pipeline order must be maintained (see CLAUDE.md line 29-38)
- Gravity = 0 during ALL propelled states — the deterministic air movement axiom (ADR-0003)
- Jetpack activation is edge-detected (press, not hold) — ADR-0004
- Post-boost velocity halving is mode-specific (ADR-0005)
- External systems reference player state via PlayerController public properties
- `[SerializeField] private` preferred over `public` for Inspector fields

### Forbidden

- Breaking the FixedUpdate pipeline order without updating all dependent components
- Applying gravity during propelled states (jetpack, secondary boost)
- Per-frame velocity setting during jetpack boost (velocity set once on activation)
- Direct coupling between Track A (Player/) and Track B (Level/, Camera/) code — use events

### Guardrails

- Tuning values are on component Inspectors (will move to PlayerTuning ScriptableObject)
- Jump and jetpack are mutually exclusive — can't jump while jetpacking
- PlayerMovement disabled during secondary boost (no input conflict)

---

## Feature Layer

**Systems**: Hazards, Gimmick Framework, Chapter Configuration, Booster Mode Swapping

### Required

- Gimmicks communicate via Event Bus (planned GameEventBus.cs), never direct player references
- Hazards use Layer 10 + trigger colliders
- Collectibles use Layer 11
- Room boundaries use Layer 12
- Gas recharge pickups tag: "GasRecharge"
- All gimmick parameters configurable per chapter via ChapterConfig (planned)

### Forbidden

- Gimmick code directly modifying player velocity or state — go through events
- Hardcoded chapter-specific values in gimmick scripts

### Guardrails

- Room system currently inactive (camera in follow mode) — will activate when real levels exist
- Respawn system exists but not fully integrated with hazard layer

---

## Presentation Layer

**Systems**: Animation, Fuel Feedback (Particles + Audio), UI

### Required

- Fuel state communicated via diegetic feedback only — no persistent HUD bar (ADR-0006)
- Particle color gradient: cyan (full) → orange (50%) → red (empty)
- Audio burst interval widens with fuel drain; pitch drops from 1.0 to 0.55
- Animator parameters driven by PlayerAnimator.cs; gracefully skip if no controller assigned
- Sprite flipping via `transform.localScale.x *= -1`
- All sprites: Point filtering, no compression, 16 PPU

### Forbidden

- Adding persistent HUD elements for moment-to-moment mechanics without design approval
- Allocations in hot paths (Update, physics callbacks) for feedback systems

### Guardrails

- GasMeterUI.cs exists but intentionally not wired up — legacy, may return as accessibility option
- All current sprites are Cave Story placeholders — will be replaced with original art in Phase 5
- Animator controller not yet connected — warnings suppressed via null check
