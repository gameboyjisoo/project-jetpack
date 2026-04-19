# CLAUDE.md — Project Jetpack

## What This Project Is
A fast-paced 2D pixel art platformer built in Unity 6 (6000.0.34f1). Inspired by Cave Story's Booster 2.0 jetpack and Celeste's room-based stage design. The core formula is "4-directional jetpack + chapter gimmicks."

The design document (Korean) is at: `C:\Users\pz_ma\OneDrive - KRAFTON\문서\Knights (원탁의 기사)\My Proposals\2024.11 Project Jetpack\Project Jetpack.docx`

## Project Location
- Local: `C:\Jisoo's Stuff\Personal Projects\Unity\Project Jetpack`
- GitHub: https://github.com/gameboyjisoo/project-jetpack (private)

## How We Work
This project is built step-by-step, collaboratively. Each change should be small and testable — "add screen shake on landing" sized. Always discuss game design before coding. Never cram multiple features into one session. This is a middle ground between a tutorial and automated dev — the developer should understand every change.

## Architecture

### Movement System (PlayerController.cs)
- Uses Rigidbody2D physics with `rb.linearVelocity` (Unity 6 API, not deprecated `rb.velocity`).
- **Input reading in Update, all physics in FixedUpdate.** Jump/fire inputs use manual edge detection (tracking previous frame state via `IsPressed()`), NOT `WasPressedThisFrame()` which is unreliable depending on Input System update mode.
- Input actions loaded from `Assets/Resources/PlayerInput.inputactions` via `Resources.Load` at runtime. No PlayerInput component needed on the GameObject.
- **Ground movement**: Celeste-style — `MoveTowards` for crisp instant acceleration/deceleration (not force-based). Air control uses single 0.65 multiplier (Celeste's `AirMult`).
- **Jump**: Celeste-accurate variable height via `varJumpTimer` (0.2s window maintains upward speed while holding jump; releasing lets gravity decelerate naturally — no instant velocity cut). Horizontal boost (`jumpHBoost`) added in input direction on jump. Apex gravity (0.5×) requires holding jump AND `|vy| < apexThreshold`.
- **Coyote time** (0.1s, Celeste's `JumpGraceTime`) and **jump buffer** (0.1s) for forgiving input.

### Jetpack System (Booster 2.0 Style)
- **Press-to-activate**, 4 cardinal directions only. Direction = most recently pressed direction key (edge-detected per frame). Jetpack button must be newly pressed while airborne (edge-detected, not hold-to-start).
- **Booster 2.0-accurate activation**: on first press, velocity set to `boostSpeed` (~1.9× moveSpeed, matching Cave Story's terminal velocity ratio) in the chosen cardinal direction. Perpendicular axis zeroed. Velocity is set ONCE on activation, not every frame.
- **Deterministic air movement (DESIGN AXIOM)**: Gravity = 0 during ALL jetpack directions. The player must know exactly where they'll end up — no invisible gravity math during propelled states. Gravity also = 0 for a brief tunable window during secondary boost recoil. Gravity only applies during unpropelled airborne states (with Celeste-style apex float and fast fall modifiers).
- **~1 second of fuel** (100 gas, 100/sec drain). Recharges on landing. Fuel parameters are configurable per chapter (shorter duration + mid-air pickups as a difficulty lever).
- **Mode-specific halving on release/empty** (matches Cave Story exactly): horizontal boost → halve X velocity only. Upward boost → halve Y velocity only. Downward boost → NO halving.
- **Wall nudge**: during horizontal boost, hitting a wall sets a small upward velocity (`wallNudgeSpeed`), allowing wall climbing — matches Booster 2.0's `ym = -0x100` on wall contact.
- **Boost modes** tracked via `boostMode`: 0=off, 1=horizontal, 2=up, 3=down.
- **Jump and jetpack are mutually exclusive** — can't jump while jetpacking.
- Gas system is separate: `JetpackGas.cs` with events. Fuel state is communicated via `JetpackParticles.cs` (visual) and `JetpackAudioFeedback.cs` (audio) — no HUD bar.

### Secondary Booster (SecondaryBooster.cs)
- Fires a projectile (if prefab assigned) and applies recoil in the opposite direction.
- 3 ammo, recharges on ground, short cooldown between shots.
- Uses same input loading pattern as PlayerController (Resources.Load + manual edge detection).
- **Swappable mode system (planned):** SecondaryBooster becomes a host delegating to `IBoosterMode` implementations. Default is `RecoilBooster` (current behavior). `GunBooster` fires aimed projectiles at switch targets. Each mode defines its own recoil, ammo rules, and feedback.
- **Mode swapping is flexible:** Can happen at chapter-level (via ChapterConfig), room-level (on room entry), or mid-room (via BoosterSwapZone gimmick or pickup). Swaps can be permanent or temporary (reverts when leaving zone).
- **Wavedash / momentum tech (planned):** Diagonal-downward recoil near ground converts into horizontal ground speed. Landing from jetpack boost at an angle preserves momentum. Enables Celeste/Melee-style speed tech for skilled players.

### Camera (RoomCamera.cs)
- **Currently**: simple follow camera for movement testing (orthographic size 8.5).
- **Eventually**: will return to Celeste-style room-snapping (one screen per room, lerp on transition).

### Animation (PlayerAnimator.cs)
- Drives Animator parameters (IsRunning, IsGrounded, IsJetpacking, VelocityY) from PlayerController state.
- Gracefully skips if no AnimatorController is assigned.
- Sprite flipping via `transform.localScale.x *= -1` in PlayerController.

### Test Level (MovementTestLevel.cs)
- Runtime-spawned test environment. Disables old scene platforms and creates a 120x40 unit enclosed area with scattered platforms, a staircase, and a tight vertical corridor.
- Temporary — will be removed once real levels exist.

### Room System (Room.cs, RoomManager.cs)
- Rooms defined by `Room` MonoBehaviour with size and spawn point.
- `RoomManager` singleton detects player crossing room boundaries.
- Currently inactive (camera is in follow mode, only one test room exists).

### Jetpack Fuel Feedback (Minimal UI Philosophy)
The game uses **diegetic feedback** instead of HUD bars — the player reads fuel state from the jetpack itself, not from a UI element. This keeps the screen clean and the player's eyes on the action.

- **Exhaust color shift** (`JetpackParticles.cs`): Particle color shifts through a two-stage gradient as fuel drains — cyan (full) to orange (mid) to red (empty). Below 20% fuel, emission sputters on/off rapidly to signal urgency.
- **Audio burst feedback** (`JetpackAudioFeedback.cs`): Plays a short burst SFX (`SE_14_2E.wav`) repeatedly via `PlayOneShot`. At full fuel, bursts fire near-consecutively (0.08s interval, matching Cave Story's rapid-fire feel). As fuel drains, the interval widens to 0.3s (audible dead time between bursts). Pitch drops from 1.0 to 0.55 with fuel. Below 30% fuel, random pitch jitter adds a "struggling engine" feel. On empty, a dry-fire click plays once.
- **GasMeterUI.cs** exists as a legacy fill bar but is **intentionally not wired up**. The design direction is to avoid persistent HUD elements for moment-to-moment mechanics. A bar may return later as an accessibility option.

**Design principle**: If the player needs to look away from the character to understand their state, the feedback has failed. All fuel cues are embodied on/around the player sprite or in audio.

### Input
- Uses Unity's New Input System (not legacy).
- Input Actions asset at `Assets/Resources/PlayerInput.inputactions`.
- **Key bindings**: WASD/Arrows = Move, Space/C = Jump, Z/Left Shift = Jetpack, X = Fire (Secondary Booster).
- Gamepad supported (left stick/dpad, button south = jump, right trigger = jetpack, button west = fire).

### Layers & Tags
- Layer 8: Ground (used for ground check — if not set in Inspector, code forces it to layer 8)
- Layer 9: Player
- Layer 10: Hazard
- Layer 11: Collectible
- Layer 12: RoomBoundary
- Tags: Player, GasRecharge, RoomTransition, Hazard, SecondaryBooster
- Sorting layers: Default > Background > Tilemap > Player > Foreground > UI

### Sprites
- All sprites use **Point filtering, no compression, 16 pixels per unit**.
- MyChar.png (200x64) is sliced into 20 named 16x16 sprites (Cave Story placeholders).
- Platforms in test level use runtime-generated 16x16 white square sprites with colored tinting.

## Important Conventions
- Physics uses `rb.linearVelocity` (Unity 6), not `rb.velocity`.
- 16 PPU (pixels per unit) everywhere — 1 tile = 1 Unity unit.
- Project gravity is -20 (set in Physics2D settings, stronger than default for snappier feel).
- Input reading in Update, physics in FixedUpdate. Never set velocity in Update.
- Manual edge detection for button presses (`IsPressed()` + previous frame tracking), not `WasPressedThisFrame()`.
- All scripts that need input load the InputActionAsset from Resources and call Enable/Disable in OnEnable/OnDisable.
- Ground/platform Rigidbody2D must be **Static** (not Dynamic), otherwise they fall.
- Scene file `groundLayer` mask may revert to 0 — code fallback in Awake forces it to layer 8.

## Current Tuning Values (updated 2026-04-19)
| Parameter | Value | Component | Notes |
|---|---|---|---|
| moveSpeed | 6 | PlayerMovement | Reduced from 10 for tighter feel |
| groundAccel/Decel | 120/120 | PlayerMovement | Celeste-level snappy |
| airMult | 0.65 | PlayerMovement | Celeste's single air control multiplier |
| jumpForce | 8 | PlayerJump | ~3.5× character height full hold, ~1.6× short tap |
| varJumpTime | 0.2s | PlayerJump | Celeste variable jump hold window (12 frames) |
| jumpHBoost | 2.4 | PlayerJump | 40% of moveSpeed (Celeste ratio) |
| coyoteTime | 0.1s | PlayerJump | Celeste's JumpGraceTime (6 frames) |
| jumpBufferTime | 0.1s | PlayerJump | |
| boostSpeed (jetpack) | 11 | PlayerJetpack | ~1.83× moveSpeed (Booster 2.0 ratio) |
| gasConsumptionRate | 100 | PlayerJetpack | ~1 second of boost |
| wallNudgeSpeed | 2 | PlayerJetpack | Upward push when hitting wall during horizontal boost |
| wallCheckDistance | 0.6 | PlayerJetpack | Raycast distance for wall detection |
| fallGravityMultiplier | 2.0 | PlayerGravity | Fast fall |
| apexGravityMultiplier | 0.5 | PlayerGravity | Half-gravity at jump peak (requires holding jump) |
| apexThreshold | 2.5 | PlayerGravity | Velocity below which apex kicks in |
| maxFallSpeed | 20 | PlayerGravity | Reduced from 30 for proportional feel |
| boostSpeed (dash) | 32 | SecondaryBooster | Punchy burst, covers ~4.8 tiles in 0.15s |
| boostDuration (dash) | 0.15s | SecondaryBooster | Celeste-length dash |
| maxAmmo (dash) | 1 | SecondaryBooster | Single dash, recharges on ground. Pickups can recharge mid-air. |
| freezeFrameDuration | 0.05s | SecondaryBooster | Celeste-style freeze on dash activation (~3 frames) |
| endDecayTime | 0.06s | SecondaryBooster | Smooth deceleration at end of dash (not hard stop) |
| momentumRetain | 0.25 | SecondaryBooster | Keep 25% velocity after dash (momentum carry) |
| Project gravity | -20 | Physics2D settings | |

## Planned Architecture (see `docs/superpowers/specs/2026-04-07-granular-development-plan-design.md`)

The project is being developed via two parallel tracks:
- **Track A (Feel):** Refactor PlayerController into focused components, build runtime tuning panel, nail movement feel, add momentum/wavedash system. Owns `Player/`, `UI/`.
- **Track B (Infrastructure):** Room-snapping camera, gimmick framework, event bus, chapter configuration. Owns `Level/`, `Camera/`, `Gimmicks/`, `Core/`.

Key planned systems:
- `PlayerTuning.cs` — ScriptableObject centralizing ALL tuning values (replaces scattered inspector fields)
- `GravityHandler.cs` — Priority-based override system (jetpack/boost request gravity=0, gimmicks can override)
- `GameEventBus.cs` — Central pub/sub decoupling gimmicks from player/camera/UI systems
- `ChapterConfig.cs` — Per-chapter rules (default booster mode, fuel limits, gimmick palette)
- `BoosterSwapZone.cs` — Mid-room booster mode changes (temporary or permanent)

Gimmick ideas: wind turbines (on/off intervals), gravity switches, closing platforms, fuel pickups, shootable switch targets, blind zones.

## Claude Code Game Studios Framework (installed 2026-04-19)
The project uses the Claude Code Game Studios framework with 49 specialized agents, 72 skills, 12 hooks, and 11 coding standards. Key locations:
- **GDDs**: `design/gdd/` — 5 core GDDs (player-movement, player-jump, jetpack-system, secondary-booster, fuel-feedback) + game-concept + systems-index (18 systems)
- **ADRs**: `docs/architecture/` — 7 ADRs covering all Foundation/Core decisions
- **Master architecture**: `docs/architecture/architecture.md` (645 lines, consolidates all ADRs)
- **Control manifest**: `docs/architecture/control-manifest.md` — programmer rules per layer
- **Traceability**: `docs/architecture/architecture-traceability.md` — 17 requirements mapped
- **Art bible**: `design/art/art-bible.md` — pixel art standards (placeholder art, original in Phase 5)
- **Accessibility**: `design/accessibility-requirements.md` — Basic tier
- **UX patterns**: `design/ux/interaction-patterns.md` — 5 core interaction patterns
- **Adoption plan**: `docs/adoption-plan-2026-04-19.md` — migration checklist with status
- **Production**: `production/stage.txt` = Pre-Production, `production/review-mode.txt` = lean
- **Tests**: `Assets/Tests/EditMode/` (4 example tests), `Assets/Tests/PlayMode/`, CI/CD at `.github/workflows/tests.yml`

## Design Identity (see `design/gdd/design-direction.md` for full document)
The game is NOT "Celeste with a jetpack." It's built on **two pillars**:
1. **Maneuvering Skill** (Celeste DNA) — tight corridors, precision gaps, chained movement. Jump, ground movement, and dash feel exactly like Celeste.
2. **Fuel Timing** (what's uniquely ours) — the environment reacts to the player's analog fuel state. Fuel-threshold barriers, fuel-reactive platforms, fuel-drain zones. The challenge is WHEN you spend fuel, not just WHERE.

The fusion: maneuvering IS fuel spending. Navigating a corridor drains fuel at a rate determined by your skill. The level asks: can you arrive at the fuel gate with the right amount remaining?

**Secondary mode swap**: Dash (costs 1 ammo charge, burst repositioning) vs. Gun (free to use, ranged interaction, no repositioning). Swapping is a fuel management decision — gun lets you hit distant switches without spending fuel to fly there.

**Design targets**:
- **Jetpack**: Cave Story's Booster 2.0 — sustained directional thrust, the primary traversal tool.
- **Fuel feedback**: Diegetic only (particles + audio). No persistent HUD bar. The fuel state is analog and readable by both player and environment.
- **Gimmicks must interact with the fuel system** — no generic platformer gimmicks that ignore fuel.

## Current State (updated 2026-04-19)
**Project phase: Pre-Production** (passed Technical Setup gate 2026-04-19).

Movement prototype with Celeste-style ground controls, Booster 2.0-style jetpack, and 8-direction secondary dash. **PlayerController refactored** into 6 focused components (2026-04-14). **Gravity axiom implemented** (gravity=0 during all propelled states). Diegetic fuel feedback working (exhaust particles + audio pitch decay). Currently playtesting movement feel in a runtime-generated test level. All MVP core systems functional.

**Movement tuning pass (2026-04-19):** All values retuned for Celeste-accurate proportions. Jump height reduced from ~12× to ~3.5× character height. Dash changed to 1 ammo (from 3) with Celeste-style freeze frames, smooth decay, and 25% momentum retention. Corner correction added (nudges player when clipping platform corners while rising). Dash input buffer added (0.1s, matches jump buffer). Walk, jetpack, and dash speeds all reduced proportionally. See tuning table below for exact values.

Architecture fully documented: 5 GDDs, 7 ADRs, master architecture doc, traceability matrix. Gate check passed with CONCERNS (non-blocking: dependency chain inconsistency across docs, Resources.Load deprecation awareness, 11/18 systems still need ADRs).

## What's NOT Done Yet
- **Jetpack speed may need retune** — at 11 units/sec with 1s fuel, jetpack covers ~11 tiles. May feel too far compared to the tighter ground game. Needs dedicated playtesting.
- **Wavedash / dash-cancel-to-jump** — planned tech: allow jump during dash decay phase to convert horizontal momentum into jump arc (Celeste hyper dash equivalent)
- **Wall slide** — not originally planned, but worth considering as a movement option. Could complement or conflict with jetpack wall nudge.
- Runtime tuning panel not yet built (PlayerTuning ScriptableObject)
- No screen shake or general juice beyond jetpack exhaust feedback
- **Landing feedback needed**: squash animation, dust particles, subtle camera response on landing — makes landings feel impactful instead of silent
- **Air state visual distinction needed**: player sprite should visually differ between rising, apex, and falling states — lets the player read the jump arc
- **Dash trail / afterimage effect** — nice-to-have visual feedback for dash, implement after gameplay is complete
- No SFX or music beyond jetpack audio feedback
- No title screen or persistent HUD (intentional — minimal UI philosophy)
- No actual level design beyond the test room
- Death/respawn system exists (PlayerRespawn.cs) but not fully integrated with hazards
- No gimmick framework or event bus yet (Event Bus ADR is most urgent gap)
- Sprites are Cave Story placeholders (original art Phase 5)
- Animator controller not connected (placeholder sprites)
- tr-registry.yaml needs entries populated from GDDs
- sprint-status.yaml not yet created
- No stories created yet (use `/create-stories` from GDDs)

## Known Issues
- Scene file `groundLayer` mask doesn't persist — code fallback runs every frame in CheckGround() (not just Awake)
- Old scene platforms (Ground, Platform_Left/Right/Top) still exist but disabled at runtime by MovementTestLevel
- Animator throws warning if no controller assigned (handled with null check)
- **SerializeField values in Inspector override script defaults** — after changing defaults in code, you MUST manually update Inspector values or right-click component → Reset. This caused significant debugging time during movement tuning.
- Architecture review flagged: dependency chain described differently across master doc, traceability matrix, and individual ADRs — needs reconciliation

## Movement Fixes Applied (2026-04-19)
- **Zero-friction physics material**: Applied at runtime to player collider + rigidbody. Prevents sticking to walls mid-air. Ground movement unaffected (velocity-based, not friction-based).
- **Ground check uses dual raycasts**: Two downward raycasts from feet edges instead of OverlapBox. Immune to wall false positives. Normal check (`y > 0.7`) rejects non-floor surfaces.
- **Ground check uses rb.position**: Physics-authoritative position instead of interpolated transform.position. Fixes detection issues when Player is selected in Inspector during play mode.
- **Corner correction**: Celeste-style nudge when head clips a platform corner while rising. One-side-blocked detection with cooldown to prevent repeated nudging.
- **Dash freeze frames**: 0.05s time freeze on dash activation (Celeste-style). Uses unscaledDeltaTime.
- **Dash momentum retain**: 25% velocity preserved after dash decay instead of hard stop.
- **Dash input buffer**: 0.1s buffer matching jump buffer forgiveness.
- **Dash ammo**: Reduced to 1 (from 3). Recharges on ground. `Recharge()` public method for mid-air pickups.
- **Jetpack wall nudge**: Now fires once per boost (was every frame, causing infinite wall climbing).

## Next Steps
1. **Playtest jetpack speed** — at 11 units/sec with 1s fuel, may cover too much distance relative to tighter ground game
2. **Event Bus ADR** — most urgent architecture gap, blocks Track B work
3. **Start Pre-Production work**: prototype Vertical Slice (Chapter 1 tutorial rooms)
4. **Sprint plan** — use `/sprint-plan` to create first sprint structure
5. **Create stories** — use `/create-stories` to generate from GDDs

## Long-Term Plan

### Phase 1 — Core Prototype (COMPLETE)
**Track A:** ~~Refactor PlayerController~~ ✓ → ~~implement gravity axiom~~ ✓ → ~~movement feel tuning pass~~ ✓ → build tuning panel → wavedash/momentum system → booster mode framework.
**Track B:** Event bus + Core/ → room-snapping camera + respawn → gimmick framework → chapter configuration.

### Phase 2 — Chapter 1 (Tutorial)
10-15 rooms teaching mechanics progressively. Room transition effects. Basic SFX. Death particles & screen shake. Title screen.

### Phase 3 — Chapter 2+ (Gimmick Chapters)
Each chapter introduces one new gimmick (wind turbines, gravity switches, closing platforms, gun mode puzzles, blind zones, etc.). 15-20 rooms per chapter. Booster mode can change mid-chapter or mid-room via BoosterSwapZone.

### Phase 4 — Speedrun & Mastery Layer
Momentum preservation tech. Wavedash chains. Jetpack cancel techniques. Hidden skips. In-game timer.

### Phase 5 — Polish & Original Art
Replace all Cave Story sprites. Original character, tilesets, effects. Soundtrack.

### Phase 6 — Release
4-5 chapters (60-80 rooms). B-side hard mode. Settings menu.

## Multi-Session Protocol
When running parallel Claude Code sessions, follow the file ownership rules in the design spec:
- **Track A sessions** only touch: `Assets/Scripts/Player/`, `Assets/Scripts/UI/`
- **Track B sessions** only touch: `Assets/Scripts/Level/`, `Assets/Scripts/Camera/`, `Assets/Scripts/Gimmicks/`, `Assets/Scripts/Core/`
- Commit to named branches: `track-a/<description>` or `track-b/<description>`
- Player-affecting actions from Track B go through GameEventBus, never direct calls
