# CLAUDE.md — Project Jetpack

## What This Project Is
A fast-paced 2D pixel art platformer built in Unity 6 (6000.0.34f1). Inspired by Cave Story's Booster 2.0 jetpack and Celeste's room-based stage design. The core formula is "4-directional jetpack + chapter gimmicks."

The design document (Korean) is at: `C:\Users\pz_ma\OneDrive - KRAFTON\문서\Knights (원탁의 기사)\My Proposals\2024.11 Project Jetpack\Project Jetpack.docx`

## Project Location
`C:\Jisoo's Stuff\Personal Projects\Unity\Project Jetpack`

## How We Work
This project is built step-by-step, collaboratively. Each change should be small and testable — "add screen shake on landing" sized. Always discuss game design before coding. Never cram multiple features into one session. This is a middle ground between a tutorial and automated dev — the developer should understand every change.

## Architecture

### Movement System (PlayerController.cs)
- Uses Rigidbody2D physics with `rb.linearVelocity` (Unity 6 API, not deprecated `rb.velocity`).
- **Input reading in Update, all physics in FixedUpdate.** Jump/fire inputs use manual edge detection (tracking previous frame state via `IsPressed()`), NOT `WasPressedThisFrame()` which is unreliable depending on Input System update mode.
- Input actions loaded from `Assets/Resources/PlayerInput.inputactions` via `Resources.Load` at runtime. No PlayerInput component needed on the GameObject.
- **Ground movement**: Celeste-style — `MoveTowards` for crisp instant acceleration/deceleration (not force-based). Air control is slightly reduced (80% accel, 50% decel).
- **Jump**: Variable height (hold for full, tap for short). Apex gravity reduction (`apexGravityMultiplier`) only applies during jump arcs (`didJump` flag), not after jetpack.
- **Coyote time** (0.08s) and **jump buffer** (0.1s) for forgiving input.

### Jetpack System (Booster 2.0 Style)
- **Hold-to-use**, 4 cardinal directions only. Direction = most recently pressed direction key (edge-detected per frame).
- **Pure cardinal movement** — gravity fully disabled during boost, velocity set directly.
- **~1 second of fuel** (100 gas, 100/sec drain). Recharges on landing.
- **On release or gas empty**: horizontal velocity halved (drift), upward velocity halved, downward velocity zeroed (gravity takes over naturally). This creates the Booster 2.0 coast/drift feel.
- **Jump and jetpack are mutually exclusive** — can't jump while jetpacking.
- Gas system is separate: `JetpackGas.cs` with events for UI.

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

### UI (GasMeterUI.cs)
- Smoothly animated gas fill bar. Not wired up in the scene yet.

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
| jumpForce | 18 | Tall, expressive jumps |
| jumpCutMultiplier | 0.3 | Big difference tap vs hold |
| coyoteTime | 0.08s | Forgiving but not sloppy |
| jumpBufferTime | 0.1s | |
| jetpackThrust | 11 | Slightly above walk speed |
| gasConsumptionRate | 100 | ~1 second of boost |
| fallGravityMultiplier | 2.0 | Fast fall |
| apexGravityMultiplier | 0.4 | Hang time at jump peak |
| apexThreshold | 3.0 | Velocity below which apex kicks in |
| maxFallSpeed | 30 | |
| Project gravity | -20 | In Physics2D settings |

## Current State
Movement prototype with Celeste-style ground controls and Booster 2.0-style jetpack. Currently playtesting movement feel in a runtime-generated test level. Jump, walk, jetpack, and secondary booster all functional.

## What's NOT Done Yet
- Movement feel is being actively tuned (current session)
- No screen shake, particles, or juice
- No SFX or music
- No title screen or UI beyond the gas meter script (not wired up)
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
