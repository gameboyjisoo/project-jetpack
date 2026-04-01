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

- **Ground movement** is Celeste-style — near-instant acceleration, snappy direction changes.
- **Jump** has variable height (hold for full, tap for short), coyote time, jump buffering, and half-gravity hang time at the apex.
- **Jetpack** works like Cave Story's Booster 2.0 — press to activate while airborne, hold to sustain. Boosts at ~1.9x run speed in 4 cardinal directions. Gravity stays active during horizontal and downward boost. ~1 second of fuel, recharges on landing.
- **Secondary Booster** fires a projectile and pushes you in the opposite direction via recoil.

## Current State

**Phase 1 — Core Prototype (in progress)**

Movement prototype with Celeste-style ground controls and Booster 2.0 jetpack. Currently playtesting movement feel in a runtime-generated test level. Jump, walk, jetpack, and secondary booster all functional. Diegetic fuel feedback via exhaust particles and audio.

## Roadmap

| Phase | Description |
|---|---|
| 1. Core Prototype | Jetpack, jump, movement, secondary booster, room system, camera |
| 2. Chapter 1 (Tutorial) | 10-15 rooms, room transitions, basic SFX, death/respawn, title screen |
| 3. Chapter 2+ (Gimmicks) | Each chapter introduces one new gimmick, 15-20 rooms per chapter |
| 4. Speedrun Layer | Momentum tech, jetpack cancels, hidden skips, in-game timer |
| 5. Polish & Original Art | Replace placeholder sprites, original soundtrack |
| 6. Release | 4-5 chapters (60-80 rooms), B-side hard mode, settings menu |

## Setup

1. Clone the repo: `git clone https://github.com/gameboyjisoo/project-jetpack.git`
2. Open in **Unity Hub** with Unity **6000.0.34f1**
3. Open scene: `Assets/Scenes/TestRoom.unity`
4. Hit Play

## Built With

- Unity 6 (6000.0.34f1)
- Unity New Input System
- Cave Story placeholder sprites (to be replaced with original art)
