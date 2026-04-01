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
- **Gravity interaction**: gravity stays active during horizontal and downward boost (player sinks during horizontal boost, accelerates during downward). Gravity disabled during upward boost (thrust cancels gravity, maintaining speed — matches Booster 2.0's thrust/gravity equilibrium).
- **~1 second of fuel** (100 gas, 100/sec drain). Recharges on landing.
- **Mode-specific halving on release/empty** (matches Cave Story exactly): horizontal boost → halve X velocity only. Upward boost → halve Y velocity only. Downward boost → NO halving.
- **Wall nudge**: during horizontal boost, hitting a wall sets a small upward velocity (`wallNudgeSpeed`), allowing wall climbing — matches Booster 2.0's `ym = -0x100` on wall contact.
- **Boost modes** tracked via `boostMode`: 0=off, 1=horizontal, 2=up, 3=down.
- **Jump and jetpack are mutually exclusive** — can't jump while jetpacking.
- Gas system is separate: `JetpackGas.cs` with events. Fuel state is communicated via `JetpackParticles.cs` (visual) and `JetpackAudioFeedback.cs` (audio) — no HUD bar.

### Secondary Booster (SecondaryBooster.cs)
- Fires a projectile (if prefab assigned) and applies recoil in the opposite direction.
- 3 ammo, recharges on ground, short cooldown between shots.
- Uses same input loading pattern as PlayerController (Resources.Load + manual edge detection).
- Currently a prototype — design not finalized.

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

## Current Tuning Values
| Parameter | Value | Notes |
|---|---|---|
| moveSpeed | 10 | Near-instant with 120 accel |
| groundAccel/Decel | 120/120 | Celeste-level snappy |
| airMult | 0.65 | Celeste's single air control multiplier |
| jumpForce | 18 | Tall, expressive jumps |
| varJumpTime | 0.2s | Celeste variable jump hold window (12 frames) |
| jumpHBoost | 2.5 | Celeste horizontal boost on jump |
| coyoteTime | 0.1s | Celeste's JumpGraceTime (6 frames) |
| jumpBufferTime | 0.1s | |
| boostSpeed | 19 | ~1.9× moveSpeed (Booster 2.0 terminal velocity ratio) |
| gasConsumptionRate | 100 | ~1 second of boost |
| wallNudgeSpeed | 2 | Upward push when hitting wall during horizontal boost |
| fallGravityMultiplier | 2.0 | Fast fall |
| apexGravityMultiplier | 0.5 | Celeste's half-gravity at jump peak (requires holding jump) |
| apexThreshold | 4.0 | Velocity below which apex kicks in |
| maxFallSpeed | 30 | |
| wallCheckDistance | 0.6 | Raycast distance for wall detection during boost |
| Project gravity | -20 | In Physics2D settings |

## Current State
Movement prototype with Celeste-style ground controls and Booster 2.0-style jetpack. Currently playtesting movement feel in a runtime-generated test level. Jump, walk, jetpack, and secondary booster all functional.

## What's NOT Done Yet
- Movement feel is being actively tuned (current session)
- No screen shake or general juice beyond jetpack exhaust feedback
- No SFX or music beyond jetpack audio feedback (needs actual audio clips assigned)
- No title screen or persistent HUD (intentional — minimal UI philosophy)
- No actual level design beyond the test room
- No death/respawn system
- No momentum/acceleration tech for speedrunners
- Sprites are Cave Story placeholders
- Animator controller not connected (placeholder sprites)

## Known Issues
- Scene file `groundLayer` mask doesn't persist — relied on code fallback
- Old scene platforms (Ground, Platform_Left/Right/Top) still exist but are disabled at runtime by MovementTestLevel
- Animator throws warning if no controller assigned (handled with null check)

## Long-Term Plan

### Phase 1 — Core Prototype (IN PROGRESS)
Jetpack, jump, ground movement, secondary booster, room system, camera, gas pickups, gravity zones, animations, input bindings. **Currently tuning movement feel.**

### Phase 2 — Chapter 1 (Tutorial)
10-15 rooms teaching mechanics progressively. Room transition effects. Basic SFX. Death particles & screen shake. Title screen.

### Phase 3 — Chapter 2+ (Gimmick Chapters)
Each chapter introduces one new gimmick. 15-20 rooms per chapter.

### Phase 4 — Speedrun & Mastery Layer
Momentum preservation tech. Jetpack cancel techniques. Hidden skips. In-game timer.

### Phase 5 — Polish & Original Art
Replace all Cave Story sprites. Original character, tilesets, effects. Soundtrack.

### Phase 6 — Release
4-5 chapters (60-80 rooms). B-side hard mode. Settings menu.
