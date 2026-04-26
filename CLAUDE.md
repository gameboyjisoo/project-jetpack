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
- **No wall nudge**: Wall nudge was removed — it conflicted with the gravity=0 axiom (any upward velocity during horizontal boost persisted forever). Horizontal boost into a wall = stuck, fuel draining. Deterministic: teaches "don't waste fuel on walls."
- **Jetpack blocked during dash**: Pressing jetpack while dashing is ignored. Prevents frozen-in-air bug (dash overrides velocity while jetpack disables gravity and movement).
- **Boost modes** tracked via `boostMode`: 0=off, 1=horizontal, 2=up, 3=down.
- **Jump and jetpack are mutually exclusive** — can't jump while jetpacking.
- Gas system is separate: `JetpackGas.cs` with events. Fuel state is communicated via `JetpackParticles.cs` (visual) and `JetpackAudioFeedback.cs` (audio) — no HUD bar.

### Secondary Booster (SecondaryBooster.cs)
- **Swappable mode system** via `SecondaryMode` enum: **Dash** (default) or **Gun**.
- **Dash mode**: 1 ammo (recharges on ground), 8-direction fixed-distance burst. Celeste-style freeze frame (0.05s) on activation. Smooth decay at end (0.06s). 25% horizontal momentum retained after dash (vertical zeroed). Dash buffer (0.1s) for forgiving input. `Recharge()` method for mid-air pickups.
- **Gun mode**: FREE to use (no ammo, no fuel cost). Fires projectile in aim direction (8-dir). No movement effect on player. Challenge is aiming mid-flight while managing jetpack trajectory.
- **Mode swap**: `SwapMode(SecondaryMode)` public method for gimmicks/pickups. Publishes `SecondaryModeChanged` event.
- **Wavedash** (implemented): Diagonal-down air dash + ground contact → horizontal speed at 1.2× dash speed (38.4 units/sec). Speed maintained for 0.12s (jump window to carry speed). Only triggers from airborne dashes. Core to fuel economy — fuel-free movement option for skilled players.

### Camera (RoomCamera.cs)
- **Three modes**: Room-follow (follows player clamped to room bounds), room-snap transition (smooth-step lerp over 0.3s between rooms), or free-follow (fallback when no rooms exist).
- **Room-following**: For rooms larger than the viewport, camera smoothly follows the player but clamps to room edges so nothing outside the room is shown. For single-screen rooms, this degrades to center-lock.
- **Room transitions**: `SetRoom(Room)` for instant snap (clamped to player position), `TransitionToRoom(Room)` for smooth lerp.
- Orthographic size 8.5. Rooms can be any size — camera adapts automatically.

### Animation (PlayerAnimator.cs)
- Drives Animator parameters (IsRunning, IsGrounded, IsJetpacking, VelocityY) from PlayerController state.
- Gracefully skips if no AnimatorController is assigned.
- Sprite flipping via `transform.localScale.x *= -1` in PlayerMovement.

### Level Generation (DELETED)
- `MovementTestLevel.cs` and `Chapter1Generator.cs` were **deleted** (2026-04-21). Code-generated rooms caused invisible walls and broken transitions.
- **Build rooms using Tilemaps** via Unity editor or Coplay MCP `execute_script`. See `design/levels/chapter1-room-designs.md` for layouts.

### Room System (Room.cs, RoomManager.cs)
- Rooms defined by `Room` MonoBehaviour with size, spawn point, and room ID. `Init()` method for runtime setup.
- `RoomManager` singleton detects player crossing room boundaries via bounds checking in Update. Leave the `rooms` array empty — it auto-discovers via `FindObjectsByType<Room>` at Start.
- Publishes `RoomEntered`, `RoomTransitionStarted`, `RoomTransitionCompleted` via Event Bus.
- **4 tutorial rooms built and playable** in TestRoom.unity (ch1-room-01 through ch1-room-04, each 60×34 units).

### Tilemap Setup (critical conventions)
- Ground tiles use `Assets/Tiles/GroundTile.asset` (16×16 white sprite, Point filter, 16 PPU).
- Persistent sprite for interactables: `Assets/Tiles/GroundSprite.png` (16×16, Point filter, 16 PPU).
- Tilemap GameObjects: Grid parent → Walls child. Walls must be on **Layer 8 (Ground)**.
- **Rigidbody2D must be Static**. Add `CompositeCollider2D` for performance.
- **CompositeCollider2D geometryType MUST be Polygons** (not Outlines). Outlines creates edge colliders that break the dual-raycast ground check — rays starting just below the floor surface hit the bottom edge's downward normal, failing the `normal.y > 0.7` check.
- TilemapCollider2D compositeOperation = Merge.

### How to Build a Room (Level Editor Workflow, 2026-04-26)
1. **Create Room shell**: `Project Jetpack > New Room` (Ctrl+Shift+R). Auto-positions, creates Room + Grid + Walls (with colliders) + Interactables (with SpawnTileManager) + SpawnPoint. Paints border walls with transition openings.
2. **Paint walls**: Window > 2D > Tile Palette → select a Cave Story palette (e.g., PrtCave Palette) → **Active Target → Walls** → paint ground, walls, platforms.
3. **Paint interactables**: Tile Palette → select **Interactables Palette** → **Active Target → Interactables** → paint hazards (orange spikes), pickups (cyan circle, magenta diamond), fuel gates (colored bars), spawn point (green arrow).
4. **At runtime**: `SpawnTileManager` on the Interactables tilemap spawns prefabs from `Assets/Prefabs/Interactables/` at each SpawnTile position, then clears the placeholder visuals.
5. **Save scene** (Ctrl+S) after making changes.

**Key rule**: Always check the **Active Target** dropdown before painting — Walls for ground tiles, Interactables for hazards/pickups/gates. Wrong target = broken behavior.

### Gimmicks (Assets/Scripts/Gimmicks/)
- **FuelGate.cs**: Barrier that opens when player's fuel matches required tier (High/Mid/Low). Color-matched to exhaust gradient (cyan/orange/red). Real-time response. `Init()` method for runtime setup. Event Bus integration.
- Three fuel tiers defined as `FuelTier` enum in JetpackGas.cs: High (50-100%), Mid (20-50%), Low (0-20%).

### Pickups & Hazards
- **GasRechargePickup.cs**: Mid-air pickup, recharges jetpack fuel. Bobbing animation. Respawns when player lands.
- **DashRechargePickup.cs**: Mid-air pickup, recharges dash ammo. Respawns when player lands.
- **Hazard.cs**: Kills player on trigger contact (layer 10). Triggers `PlayerRespawn.Die()`.
- **PlayerRespawn.cs**: Death → fade → respawn at room spawn point → invincibility flash. Publishes `PlayerDied` event.
- **Projectile.cs**: Simple projectile for gun mode. Moves forward, destroys on Ground layer contact or after lifetime.

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
- Layer 8: Ground (used for ground check — code fallback forces layer 8 if mask is 0)
- Layer 9: Player
- Layer 10: Hazard
- Layer 11: Collectible
- Layer 12: RoomBoundary
- Tags: Player, GasRecharge, RoomTransition, Hazard, SecondaryBooster
- Sorting layers: **NEED TO BE RE-ADDED** — only Default exists currently. Should be: Default > Background > Tilemap > Player > Foreground > UI. Add them in Edit → Project Settings → Tags and Layers → Sorting Layers.

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
- Tilemap CompositeCollider2D must use **Polygons** geometry, not Outlines (see Tilemap Setup section).
- Dash ammo does NOT recharge while `IsBoosting` — prevents upward ground dashes from giving a free second dash.
- Jetpack CANNOT activate during dash (`isDashing` guard in `PlayerJetpack.Tick`). Dash and jetpack are mutually exclusive at activation time.
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
| ~~wallNudgeSpeed~~ | ~~2~~ | ~~PlayerJetpack~~ | REMOVED (2026-04-21) — conflicted with gravity=0 axiom |
| ~~wallCheckDistance~~ | ~~0.6~~ | ~~PlayerJetpack~~ | REMOVED (2026-04-21) — was only used for wall nudge |
| fallGravityMultiplier | 2.0 | PlayerGravity | Fast fall |
| apexGravityMultiplier | 0.5 | PlayerGravity | Half-gravity at jump peak (requires holding jump) |
| apexThreshold | 2.5 | PlayerGravity | Velocity below which apex kicks in |
| maxFallSpeed | 20 | PlayerGravity | Reduced from 30 for proportional feel |
| boostSpeed (dash) | 32 | SecondaryBooster | Punchy burst, covers ~4.8 tiles in 0.15s |
| boostDuration (dash) | 0.15s | SecondaryBooster | Celeste-length dash |
| maxAmmo (dash) | 1 | SecondaryBooster | Single dash, recharges on ground. Pickups can recharge mid-air. |
| freezeFrameDuration | 0.05s | SecondaryBooster | Celeste-style freeze on dash activation (~3 frames) |
| endDecayTime | 0.06s | SecondaryBooster | Smooth deceleration at end of dash (not hard stop) |
| momentumRetain | 0.25 | SecondaryBooster | Keep 25% horizontal velocity after dash (vertical zeroed) |
| wavedashSpeedMultiplier | 1.2 | SecondaryBooster | Wavedash = 1.2× dash speed (38.4 units/sec). Faster than normal dash. |
| wavedashKeepTime | 0.12s | SecondaryBooster | Speed maintained for 0.12s after wavedash — jump window to carry speed |
| Project gravity | -20 | Physics2D settings | |

## Planned Architecture (see `docs/superpowers/specs/2026-04-07-granular-development-plan-design.md`)

The project is being developed via two parallel tracks:
- **Track A (Feel):** Refactor PlayerController into focused components, build runtime tuning panel, nail movement feel, add momentum/wavedash system. Owns `Player/`, `UI/`.
- **Track B (Infrastructure):** Room-snapping camera, gimmick framework, event bus, chapter configuration. Owns `Level/`, `Camera/`, `Gimmicks/`, `Core/`.

Key planned systems:
- `PlayerTuning.cs` — ScriptableObject centralizing ALL tuning values (replaces scattered inspector fields)
- `GravityHandler.cs` — Priority-based override system (jetpack/boost request gravity=0, gimmicks can override)
- `GameEventBus.cs` — Central pub/sub decoupling gimmicks from player/camera/UI systems ✓ (implemented, 9 publishes wired)
- `ChapterConfig.cs` — Per-chapter rules (default booster mode, fuel limits, gimmick palette)
- `BoosterSwapZone.cs` — Mid-room booster mode changes (temporary or permanent)

Gimmick design: See `design/gdd/design-direction.md` for full design identity. Gimmicks must interact with the fuel system. Three categories: maneuvering challenges, fuel-state gates, and hybrids. Three fuel tiers: High (50-100%, cyan), Mid (20-50%, orange), Low (0-20%, red). Gates use matching colors.

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

**Wavedash is the key enabler**: Skilled players use wavedash (fuel-free horizontal movement) to conserve jetpack fuel, arriving at fuel forks with more options. Without wavedash, the 1-second jetpack is too short for meaningful fuel management choices.

**Secondary mode swap**: Dash (costs 1 ammo charge, burst repositioning) vs. Gun (free to use, ranged interaction, no repositioning). Gun does NOT consume fuel or ammo. Swapping is a fuel management decision — gun lets you hit distant switches without spending fuel to fly there. The gun challenge is aiming accurately WHILE mid-flight, not "shoot instead of fly."

**Design targets**:
- **Jetpack**: Cave Story's Booster 2.0 — sustained directional thrust, the primary traversal tool.
- **Fuel feedback**: Diegetic only (particles + audio). No persistent HUD bar. The fuel state is analog and readable by both player and environment.
- **Gimmicks must interact with the fuel system** — no generic platformer gimmicks that ignore fuel.

## Current State (updated 2026-04-26)
**Project phase: Pre-Production** (passed Technical Setup gate 2026-04-19).

**Movement systems complete and tuned:**
- Walk, jump, jetpack, dash all working with Celeste-accurate feel
- Wavedash implemented (diagonal-down air dash → horizontal speed conversion)
- Gun mode implemented (secondary swap, free ranged projectile, no movement effect)
- Freeze frames, momentum retain, corner correction, dash buffer all working
- Zero-friction physics material prevents wall sticking
- Dual raycast ground check immune to wall false positives

**Infrastructure:**
- Event Bus implemented (ADR-0008) with 9 publishes across player systems
- Fuel-state gates implemented (3 tiers: High/Mid/Low matching exhaust colors). Gates on Layer 8 (Ground) so player can stand on them, jump, and recharge fuel/dash.
- Hazards + death/respawn working (Hazard.cs, PlayerRespawn.cs)
- Fuel + dash pickups working (GasRechargePickup.cs, DashRechargePickup.cs)
- Room camera follows player within large rooms, clamped to bounds, smooth transitions between rooms
- Design direction document defines two-pillar identity (maneuvering + fuel timing)

**Level Editor (built 2026-04-26):**
- **Tile Palettes**: 5 Cave Story palettes (PrtCave, PrtMimi, PrtOside, PrtFall, PrtHell) + Interactables Palette. All in `Assets/Tiles/Palettes/`.
- **672 tile assets** sliced from Cave Story spritesheets (16×16, Point filter, 16 PPU). Uses Unity 6 `ISpriteEditorDataProvider` API.
- **SpawnTile system** (`Assets/Scripts/Tiles/`): Custom `TileBase` subclass for interactables. Paint them like tiles in the editor; `SpawnTileManager` spawns the real prefab and clears the placeholder at runtime.
- **Interactable prefabs** (`Assets/Prefabs/Interactables/`): Hazard, FuelPickup, DashPickup, FuelGate_High/Mid/Low, SpawnPoint. Each has shape-coded placeholder sprites (spikes, circle, diamond, bars, arrow).
- **Room creation tool**: `Project Jetpack > New Room` (Ctrl+Shift+R) — editor window that auto-positions rooms, creates full Room shell with Walls + Interactables tilemaps, paints border walls with transition openings.
- **Two-tilemap workflow per room**: `Walls` (ground tiles, Layer 8, colliders) and `Interactables` (SpawnTiles, no colliders, SpawnTileManager). Active Target dropdown in Tile Palette selects which to paint on.
- **Placeholder sprites** (`Assets/Sprites/Placeholders/`): Shape-coded 16×16 PNGs — spikes (hazard), circle (fuel), diamond (dash), bars (gate), arrow (spawn). Color-tinted per type.

**Scene state (TestRoom.unity):**
- **Coplay MCP installed and working** (`.mcp.json`, `Packages/Coplay/`). Claude Code can create/modify scene objects, run editor scripts, play/stop the game.
- **Old MCP rooms deleted** (2026-04-26). Only user-created rooms remain.
- **ch1-Room-01** (user-built, in progress): 60×34, PrtCave tiles for walls, hazard spikes, Low fuel gate. Tutorial design: safe space → fuel gate comprehension check → danger zone with spikes requiring jetpack+dash combo.
- Room transitions via 6-tile openings in left/right walls (y = -8 to -3). RoomManager auto-detects boundary crossings.
- Ground tile asset at `Assets/Tiles/GroundTile.asset` (white, 16px, Point filter). Cave Story tiles at `Assets/Tiles/PrtCave/` etc.

Architecture documented: 5 GDDs + design-direction.md, 8 ADRs, master architecture doc, traceability matrix.

## What's NOT Done Yet

### Level design
- **Level editor workflow: DONE** (2026-04-26) — Tile palettes, SpawnTile system, Room creation tool, prefabs all working. Developer can create rooms entirely in Unity editor.
- **Room transition polish** — transitions work but may need tuning (camera lerp, player input during transition)
- **Gun swap zone + shootable target** — need `BoosterSwapZone.cs` and target interaction scripts
- **ch1-Room-01 in progress** — user is hand-designing the tutorial level

### Gameplay features
- **Wall slide** — not originally planned, worth considering
- Runtime tuning panel not yet built (PlayerTuning ScriptableObject)
- No screen shake or general juice beyond jetpack exhaust feedback
- **Landing feedback needed**: squash animation, dust particles, camera response
- **Air state visual distinction needed**: rising/apex/falling sprite states
- **Dash trail / afterimage effect** — nice-to-have, after gameplay complete
- No SFX or music beyond jetpack audio feedback
- No title screen or persistent HUD (intentional — minimal UI philosophy)
- Sprites are Cave Story placeholders (original art Phase 5)
- Animator controller not connected (placeholder sprites)

### Infrastructure
- tr-registry.yaml needs entries populated from GDDs
- sprint-status.yaml not yet created
- No stories created yet

## Known Issues
- Scene file `groundLayer` mask doesn't persist — code fallback runs every frame in CheckGround()
- **SerializeField values in Inspector override script defaults** — after changing defaults in code, you MUST manually update Inspector values or right-click component → Reset
- Architecture review flagged: dependency chain described differently across docs — needs reconciliation
- **MCP execute_script + SerializedObject on existing components may not persist** — use MCP `set_property` tool to change serialized fields on existing components instead
- **Ghost duplicate GameObjects can appear** when editor scripts create objects with the same name as existing ones. Always check `FindObjectsByType` count after scene modifications.
- **Runtime-created sprites don't persist** — `Sprite.Create(new Texture2D(...))` in editor scripts produces sprites that are lost on scene save/reload. Always use `AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Tiles/GroundSprite.png")` for persistent sprite references. Use `drawMode = SpriteDrawMode.Tiled` with `sr.size` to scale the visual to match collider area.
- **ASCII layout rows must be exactly 30 chars** — legacy room build scripts scale ASCII 2× to fill 60×34 rooms. No longer relevant since rooms are now built via Tile Palette, but old scripts remain in `Assets/Editor/`.

## Level Editor Setup (2026-04-26)
- **Cave Story tilesets sliced**: All 5 Prt sheets (Cave, Mimi, Oside, Fall, Hell) fixed to 16 PPU, Point filter, sliced into 16×16 grids via `ISpriteEditorDataProvider` (Unity 6 API — `TextureImporter.spritesheet` is deprecated and doesn't work).
- **SpawnTile system**: Custom `TileBase` that shows a placeholder sprite in editor. `SpawnTileManager` (on Interactables tilemap) spawns prefabs in `Awake()` and clears tile visuals. SpawnTile itself has no runtime logic.
- **Two-tilemap room structure**: Each room has `Walls` (ground, colliders) and `Interactables` (SpawnTiles, SpawnTileManager). Must select correct Active Target in Tile Palette.
- **FuelGate prefabs on Layer 8**: Gates act as ground — player can stand, jump, and recharge fuel/dash on them.
- **EditorUtility.SetDirty + SerializedObject on ScriptableObjects doesn't persist sprite references** — had to directly edit .asset YAML files to update GUIDs. The `set_property` MCP tool or YAML editing is more reliable for asset modifications.
- **Old MCP rooms deleted**: MCP_Room_01 through MCP_Room_04 removed from TestRoom.unity. Only user-created rooms remain.

## Movement Fixes Applied (2026-04-19)
- **Zero-friction physics material**: Applied at runtime to player collider + rigidbody. Prevents sticking to walls mid-air. Ground movement unaffected (velocity-based, not friction-based).
- **Ground check uses dual raycasts**: Two downward raycasts from feet edges instead of OverlapBox. Immune to wall false positives. Normal check (`y > 0.7`) rejects non-floor surfaces.
- **Ground check uses rb.position**: Physics-authoritative position instead of interpolated transform.position. Fixes detection issues when Player is selected in Inspector during play mode.
- **Corner correction**: Celeste-style nudge when head clips a platform corner while rising. One-side-blocked detection with cooldown to prevent repeated nudging.
- **Dash freeze frames**: 0.05s time freeze on dash activation (Celeste-style). Uses unscaledDeltaTime.
- **Dash momentum retain**: 25% velocity preserved after dash decay instead of hard stop.
- **Dash input buffer**: 0.1s buffer matching jump buffer forgiveness.
- **Dash ammo**: Reduced to 1 (from 3). Recharges on ground. `Recharge()` public method for mid-air pickups.
- **Jetpack wall nudge**: Now fires once per boost (was every frame, causing infinite wall climbing). **Later removed entirely** — see Fixes Applied 2026-04-21.
- **Wavedash**: Diagonal-down dash near ground converts to horizontal speed (1.2× dash speed = 38.4). Speed maintained for 0.12s (jump window). Only triggers from airborne dashes (ground dashes are normal). PlayerMovement skips tick during wavedash window.

## Next Steps
1. ~~**Level editor workflow**~~ ✓ (2026-04-26) — tile palettes, SpawnTile system, Room tool, prefabs
2. **Finish ch1-Room-01 tutorial** — user is hand-designing; collaborate on layout
3. **Wire up gun swap zone + shootable targets** — BoosterSwapZone.cs, target interaction system
4. **Sprint plan** — structure Vertical Slice work into sprints
5. **Landing feedback** — squash animation, dust particles, camera response
6. **Basic SFX** — jump, land, dash, death sounds

## Long-Term Plan

### Phase 1 — Core Prototype (COMPLETE)
**Track A:** ~~Refactor PlayerController~~ ✓ → ~~implement gravity axiom~~ ✓ → ~~movement feel tuning pass~~ ✓ → ~~wavedash~~ ✓ → ~~gun mode~~ ✓ → build tuning panel.
**Track B:** ~~Event bus~~ ✓ → ~~room-snapping camera~~ ✓ + respawn → fuel-state gates → gimmick framework → chapter configuration.

### Phase 2 — Chapter 1 (Tutorial)
4-room tutorial (compressed from original 15-room design). Room transition effects. Basic SFX. Death particles & screen shake. Title screen. Developer-facing level editing workflow.

### Phase 3 — Chapter 2+ (Gimmick Chapters)
Each chapter introduces one new gimmick (wind turbines, gravity switches, closing platforms, gun mode puzzles, blind zones, etc.). 15-20 rooms per chapter. Booster mode can change mid-chapter or mid-room via BoosterSwapZone.

### Phase 4 — Speedrun & Mastery Layer
Momentum preservation tech. Wavedash chains. Jetpack cancel techniques. Hidden skips. In-game timer.

### Phase 5 — Polish & Original Art
Replace all Cave Story sprites. Original character, tilesets, effects. Soundtrack.

### Phase 6 — Release
4-5 chapters (60-80 rooms). B-side hard mode. Settings menu.

## Fixes Applied (2026-04-21)
- **Coplay MCP installed**: `.mcp.json` configured with `coplay-mcp-server@latest`. Claude Code can now interact with Unity editor (create GameObjects, set properties, run scripts, play/stop game).
- **RoomCamera follows within large rooms**: Camera smoothly follows player, clamped to room bounds. Degrades to center-lock for single-screen rooms. `ClampToRoom()` helper handles both SetRoom and UpdateRoomFollow.
- **CompositeCollider2D Polygons fix**: Outlines geometry broke ground detection. Rays starting just below floor surface hit bottom edge (downward normal), failing `normal.y > 0.7`. Switching to Polygons makes the collider solid.
- **Dash ammo recharge guard**: Added `!IsBoosting` to ground recharge check in SecondaryBooster.Update(). Prevents upward ground dashes from instantly refilling ammo before the player leaves the floor.
- **Room tilemap setup**: Created `Assets/Tiles/GroundTile.asset` and `GroundSprite.png` (16×16, Point filter, 16 PPU). Two rooms built via editor scripts with proper Grid → Tilemap → TilemapCollider2D → CompositeCollider2D (Polygons, Static Rigidbody2D) on Layer 8.
- **Room exit openings**: Punched 3-tile-tall openings in connecting walls (rows 18-20) so player can walk between rooms.
- **Jetpack blocked during dash**: Added `isDashing` parameter to `PlayerJetpack.Tick()`. PlayerController passes `secondaryBooster.IsBoosting`. Prevents frozen-in-air bug where dash overrides velocity while jetpack disables gravity and movement.
- **Wall nudge removed**: Removed wall nudge from PlayerJetpack entirely. With gravity=0 during horizontal boost, any upward velocity from the nudge persisted forever, causing infinite wall climbing. Now horizontal boost into a wall = zero velocity, fuel draining.
- **Rooms populated with hazards and pickups**: Room 1 has 5 spike strips + fuel/dash pickups. Room 2 has spike floor + 3 fuel pickups + 2 dash pickups. All created via editor script with proper layers and components.

## Multi-Session Protocol
When running parallel Claude Code sessions, follow the file ownership rules in the design spec:
- **Track A sessions** only touch: `Assets/Scripts/Player/`, `Assets/Scripts/UI/`
- **Track B sessions** only touch: `Assets/Scripts/Level/`, `Assets/Scripts/Camera/`, `Assets/Scripts/Gimmicks/`, `Assets/Scripts/Core/`
- Commit to named branches: `track-a/<description>` or `track-b/<description>`
- Player-affecting actions from Track B go through GameEventBus, never direct calls
