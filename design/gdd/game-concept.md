# Game Concept: Project Jetpack

> **Status**: Approved
> **Created**: 2026-04-07
> **Last Updated**: 2026-04-26

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
| **Cave Story** | Booster 2.0 jetpack feel, directional boost, fuel limit, end-boost velocity halving | Story/RPG elements, weapon system |
| **Celeste** | Room-based stages, ground movement feel, variable jump, coyote time, speedrun culture | Dash mechanic (replaced by jetpack), narrative focus |

## Target Audience

- Players who enjoy tight 2D platformers (Celeste, Meat Boy, Cave Story)
- Speedrun community (momentum tech, wavedash chains, hidden skips)
- Players who appreciate learning through failure and mastery

## Scope

| Phase | Content | Milestone |
|-------|---------|-----------|
| Phase 1 | Core prototype — movement + jetpack + test room | Complete |
| Phase 2 | Chapter 1 (Tutorial) — 4 rooms, SFX, death/respawn, level editor workflow, title screen | Vertical Slice |
| Phase 3 | Chapter 2+ — 15-20 rooms per chapter, gimmick framework | Alpha |
| Phase 4 | Speedrun layer — momentum tech, wavedash, timer | Beta |
| Phase 5 | Polish — original art, soundtrack (replace Cave Story placeholders) | Polish |
| Phase 6 | Release — 4-5 chapters (60-80 rooms), B-side hard mode, settings | Release |

## Level Creation

Levels are built using Unity's Tilemap system. Each room is a 60×34 unit area (at 16 PPU = 960×544 pixels) containing:
- **Tilemap** (Grid → Walls child) on Layer 8 with CompositeCollider2D (Polygons)
- **Interactables** as child GameObjects: hazards (Layer 10), pickups (Layer 11), fuel gates, swap zones

**Current workflow**: Rooms are defined as ASCII art (30×14 chars, scaled 2×) in C# editor scripts and built via Coplay MCP. This works for Claude Code but is not developer-friendly.

**Target workflow** (not yet built): The developer should be able to create rooms directly in the Unity editor by:
1. Painting tiles using a **Tile Palette** with labeled tile types
2. Dragging **prefabs** for hazards, pickups, gates, and swap zones
3. Using an **editor tool** that creates properly-configured Room shells (Room component + Grid + Tilemap + colliders) in one click
4. Duplicating a **room template prefab** and repositioning it

This is a high priority for Phase 2 — the developer needs to iterate on level designs without writing code.

## Unique Selling Point

The jetpack creates a unique movement vocabulary that no other platformer offers. It's not a dash (Celeste), not a double jump, not a glide — it's a committed directional burst with fuel management. Combined with the secondary booster's 8-direction dash, players have a rich movement toolkit that rewards mastery.

## Design Document (Korean)

Full design document: `C:\Users\pz_ma\OneDrive - KRAFTON\문서\Knights (원탁의 기사)\My Proposals\2024.11 Project Jetpack\Project Jetpack.docx`
