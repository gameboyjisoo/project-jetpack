# Project Jetpack

A fast-paced 2D pixel art platformer built in **Unity 6** (6000.0.34f1).

Inspired by **Cave Story's Booster 2.0** jetpack and **Celeste's** room-based stage design. The core formula is **4-directional jetpack + chapter gimmicks**.

## Controls

| Action | Keyboard | Gamepad |
|---|---|---|
| Move | WASD / Arrow Keys | Left Stick / D-pad |
| Jump | Space / C | A (South) |
| Jetpack | Z / Left Shift | RT (Right Trigger) |
| Fire (Secondary Booster) | X | X (West) |

## How It Plays

The game fuses two design pillars: **Maneuvering Skill** (Celeste-tight platforming) and **Fuel Timing** (analog fuel state shapes the level design).

- **Ground movement** is Celeste-style — near-instant acceleration, snappy direction changes.
- **Jump** has variable height (hold for full, tap for short), coyote time, jump buffering, and half-gravity hang time at the apex.
- **Jetpack** works like Cave Story's Booster 2.0 — press to activate while airborne, hold to sustain. Boosts at ~1.9x run speed in 4 cardinal directions. No gravity during boost (deterministic trajectory). ~1 second of fuel, recharges on landing. Skilled players use **wavedash** (diagonal-down air dash → horizontal speed conversion) to conserve fuel.
- **Dash** is a single recharging burst in 8 directions. Recharges on ground, can be recharged mid-air via pickup.
- **Gun Mode** (swappable secondary) fires a projectile in your aim direction. Free to use (no ammo, no fuel cost). Challenge is aiming mid-flight while managing jetpack trajectory.
- **Fuel feedback** is diegetic — exhaust particles shift color (cyan > orange > red) as fuel drains, and audio bursts widen/deepen. No HUD bar.

## Current State

**Phase 1 — Core Prototype (COMPLETE)**

All core movement systems implemented and tuned: walk, jump, jetpack, dash, wavedash, and gun mode. Game architecture complete (event bus, room system, camera with smooth transitions).

**Playable content**: Tutorial level (Chapter 1) being hand-designed in `TestRoom.unity`. Level editor workflow with Tile Palettes (Cave Story tilesets + interactables) and Room creation tool. Fuel-state gates (3 tiers: High/Mid/Low) working. Death/respawn with invincibility flash. Hazards and mid-air pickups (fuel recharge, dash recharge) fully integrated.

**Feedback systems**: All diegetic — exhaust particle gradient (cyan > orange > red), audio bursts with pitch/frequency modulation, no persistent HUD bar.

## Roadmap

| Phase | Description | Status |
|---|---|---|
| 1. Core Prototype | Movement, jetpack, dash, gun mode, room system, camera, fuel gates, death/respawn | COMPLETE |
| 2. Chapter 1 (Tutorial) | Tutorial rooms hand-designed via Level Editor, room transitions, basic SFX, title screen | Next (level editor done, ch1-Room-01 in progress) |
| 3. Chapter 2+ (Gimmicks) | Each chapter introduces one new gimmick, 15-20 rooms per chapter | Planned |
| 4. Speedrun Layer | Momentum preservation, jetpack cancels, hidden skips, in-game timer | Planned |
| 5. Polish & Original Art | Replace placeholder sprites, original soundtrack | Phase 5 |
| 6. Release | 4-5 chapters (60-80 rooms), B-side hard mode, settings menu | Post-alpha |

## Setup

1. Clone the repo: `git clone https://github.com/gameboyjisoo/project-jetpack.git`
2. Open in **Unity Hub** with Unity **6000.0.34f1**
3. Open scene: `Assets/Scenes/TestRoom.unity`
4. Hit Play to test the tutorial level

**Building new rooms**: Use `Project Jetpack > New Room` (Ctrl+Shift+R) to create room shells. Paint walls with Cave Story tile palettes (Active Target → Walls), paint interactables with the Interactables Palette (Active Target → Interactables). See CLAUDE.md "How to Build a Room" for full workflow.

## Built With

- **Unity 6** (6000.0.34f1)
- **Unity New Input System** (WASD/Arrows, Space, Z, X, or Gamepad)
- **Physics2D**: Static tilemaps with CompositeCollider2D, dual-raycast ground detection
- **Event Bus** (GameEventBus.cs) for decoupled gimmick/player/camera interaction
- **Diegetic feedback**: Particle effects and audio only (no HUD)
- **Cave Story placeholder sprites** (to be replaced with original art in Phase 5)
- **Claude Code Game Studios framework** (49 agents, 72 skills, design documentation)
