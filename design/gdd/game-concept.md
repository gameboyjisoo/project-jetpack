# Game Concept: Project Jetpack

> **Status**: Approved
> **Created**: 2026-04-07
> **Last Updated**: 2026-04-19

## Elevator Pitch

A fast-paced 2D pixel art platformer where you navigate tight rooms using a fuel-limited directional jetpack. Inspired by Cave Story's Booster 2.0 and Celeste's room-based stage design. The core formula is **"4-directional jetpack + chapter gimmicks."**

## Core Loop

1. **Enter a room** — each room is one screen (Celeste-style)
2. **Navigate hazards** using movement, jump, jetpack, and secondary boost
3. **Manage fuel** — jetpack has ~1 second of fuel, recharges on landing
4. **Reach the exit** — advance to the next room
5. **Learn new gimmicks** — each chapter introduces one new mechanic

## Game Pillars

### 1. Precision Movement Feel
Every input must feel responsive and predictable. The player should always know exactly where they'll end up. Movement should feel like Celeste (snappy ground control, variable jump, forgiving coyote/buffer) and Cave Story (directional jetpack, committed boost direction, fuel tension).

### 2. Readable Challenge
Rooms should be visually clear. The player reads the challenge, plans a route, and executes. Failure should feel fair — "I know what I did wrong." Fuel state is communicated through the jetpack itself (particles, audio), not a HUD bar.

### 3. Escalating Mechanical Depth
Each chapter introduces one new gimmick (wind turbines, gravity switches, closing platforms, gun mode puzzles, blind zones). Gimmicks combine with the core jetpack mechanic to create new challenges without adding new controls. Skilled players discover momentum tech and speedrun routes.

## Inspirations

| Game | What We Borrow | What We Don't |
|------|---------------|---------------|
| **Cave Story** | Booster 2.0 jetpack feel, directional boost, fuel limit, wall nudge | Story/RPG elements, weapon system |
| **Celeste** | Room-based stages, ground movement feel, variable jump, coyote time, speedrun culture | Dash mechanic (replaced by jetpack), narrative focus |

## Target Audience

- Players who enjoy tight 2D platformers (Celeste, Meat Boy, Cave Story)
- Speedrun community (momentum tech, wavedash chains, hidden skips)
- Players who appreciate learning through failure and mastery

## Scope

| Phase | Content | Milestone |
|-------|---------|-----------|
| Phase 1 | Core prototype — movement + jetpack + test room | Current |
| Phase 2 | Chapter 1 (Tutorial) — 10-15 rooms, SFX, death/respawn, title screen | Vertical Slice |
| Phase 3 | Chapter 2+ — 15-20 rooms per chapter, gimmick framework | Alpha |
| Phase 4 | Speedrun layer — momentum tech, wavedash, timer | Beta |
| Phase 5 | Polish — original art, soundtrack (replace Cave Story placeholders) | Polish |
| Phase 6 | Release — 4-5 chapters (60-80 rooms), B-side hard mode, settings | Release |

## Unique Selling Point

The jetpack creates a unique movement vocabulary that no other platformer offers. It's not a dash (Celeste), not a double jump, not a glide — it's a committed directional burst with fuel management. Combined with the secondary booster's 8-direction dash, players have a rich movement toolkit that rewards mastery.

## Design Document (Korean)

Full design document: `C:\Users\pz_ma\OneDrive - KRAFTON\문서\Knights (원탁의 기사)\My Proposals\2024.11 Project Jetpack\Project Jetpack.docx`
