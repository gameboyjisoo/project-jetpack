# Systems Index: Project Jetpack

> **Status**: Draft
> **Created**: 2026-04-19
> **Last Updated**: 2026-04-19
> **Source Concept**: CLAUDE.md (game concept not yet extracted to design/gdd/game-concept.md)

---

## Overview

Project Jetpack is a fast-paced 2D pixel art platformer inspired by Cave Story's Booster 2.0 jetpack and Celeste's room-based stage design. The core loop is "navigate rooms using jetpack + chapter gimmicks." Systems support tight movement feel, diegetic feedback, and escalating mechanical complexity across chapters. The game needs precise physics, responsive input, room-based level structure, and a gimmick framework that introduces new challenges per chapter.

---

## Systems Enumeration

| # | System Name | Category | Priority | Status | Design Doc | Depends On |
|---|-------------|----------|----------|--------|------------|------------|
| 1 | Player Movement | Core | MVP | Implemented | design/gdd/player-movement.md | Input System, Physics2D |
| 2 | Player Jump | Core | MVP | Implemented | design/gdd/player-jump.md | Player Movement, Gravity System |
| 3 | Jetpack System | Core | MVP | Implemented | design/gdd/jetpack-system.md | Fuel System, Gravity System, Input System |
| 4 | Secondary Booster | Core | MVP | Implemented | design/gdd/secondary-booster.md | Input System, Gravity System |
| 5 | Fuel System | Core | MVP | Implemented | design/gdd/fuel-feedback.md | None |
| 6 | Gravity System | Core | MVP | Implemented | — | Physics2D |
| 7 | Player Animation | Presentation | MVP | In Progress | — | Player Movement, Jetpack System |
| 8 | Camera System | Core | MVP | In Progress | — | Player Controller, Room System |
| 9 | Room System | Core | Vertical Slice | In Progress | — | Camera System |
| 10 | Respawn System | Core | Vertical Slice | In Progress | — | Room System, Hazard System |
| 11 | Event Bus | Core | Vertical Slice | Not Started | — | None |
| 12 | Gimmick Framework | Gameplay | Vertical Slice | Not Started | — | Event Bus, Chapter Config |
| 13 | Chapter Configuration | Gameplay | Vertical Slice | Not Started | — | Room System, Gimmick Framework |
| 14 | Booster Mode Swapping | Gameplay | Alpha | Not Started | — | Secondary Booster, Chapter Config |
| 15 | Momentum / Wavedash | Gameplay | Full Vision | Not Started | — | Player Movement, Secondary Booster |
| 16 | Runtime Tuning Panel | Meta | Vertical Slice | Not Started | — | All player systems |
| 17 | Audio System | Audio | Vertical Slice | In Progress | — | None |
| 18 | Hazard System | Gameplay | Vertical Slice | In Progress | — | Physics2D, Respawn System |

---

## Categories

| Category | Description | Systems in Project |
|----------|-------------|--------------------|
| **Core** | Foundation systems everything depends on | Player Movement, Jump, Jetpack, Gravity, Fuel, Camera, Room, Event Bus |
| **Gameplay** | Systems that create challenge and variety | Gimmick Framework, Chapter Config, Booster Mode Swapping, Momentum/Wavedash, Hazards |
| **Presentation** | Visual and audio feedback | Player Animation, Audio System |
| **Meta** | Development and tuning tools | Runtime Tuning Panel |

---

## Priority Tiers

| Tier | Definition | Target Milestone | Systems |
|------|------------|------------------|---------|
| **MVP** | Core loop: move, jump, jetpack through a test room | First playable prototype | Movement, Jump, Jetpack, Secondary Booster, Fuel, Gravity, Animation, Camera (follow) |
| **Vertical Slice** | One complete chapter with rooms, hazards, respawn | Vertical slice demo | Room System, Respawn, Event Bus, Gimmick Framework, Chapter Config, Runtime Tuning, Audio, Hazards |
| **Alpha** | Multiple chapters, mode swapping, all mechanics | Alpha milestone | Booster Mode Swapping |
| **Full Vision** | Speedrun tech, polish, original art | Beta / Release | Momentum/Wavedash |

---

## Dependency Map

### Foundation Layer (no dependencies)

1. Input System (Unity built-in) — all player actions flow from here
2. Physics2D (Unity built-in) — collision, gravity, raycasts
3. Fuel System (JetpackGas.cs) — standalone resource with events

### Core Layer (depends on foundation)

1. Gravity System (PlayerGravity.cs) — depends on: Physics2D
2. Player Movement (PlayerMovement.cs) — depends on: Input, Physics2D
3. Player Jump (PlayerJump.cs) — depends on: Movement, Gravity
4. Jetpack System (PlayerJetpack.cs) — depends on: Fuel, Gravity, Input
5. Secondary Booster (SecondaryBooster.cs) — depends on: Input, Gravity
6. Event Bus (planned) — depends on: none (pure pub/sub)

### Feature Layer (depends on core)

1. Camera System (RoomCamera.cs) — depends on: Player Controller
2. Room System (Room.cs, RoomManager.cs) — depends on: Camera
3. Respawn System (PlayerRespawn.cs) — depends on: Room System
4. Hazard System (Hazard.cs) — depends on: Physics2D, Respawn
5. Gimmick Framework (planned) — depends on: Event Bus, Room System
6. Chapter Configuration (planned) — depends on: Room System, Gimmick Framework
7. Booster Mode Swapping (planned) — depends on: Secondary Booster, Chapter Config

### Presentation Layer (depends on features)

1. Player Animation (PlayerAnimator.cs) — depends on: Movement, Jetpack
2. Fuel Feedback (JetpackParticles.cs, JetpackAudioFeedback.cs) — depends on: Fuel System
3. Audio System (planned) — depends on: Event Bus

### Polish Layer (depends on everything)

1. Momentum/Wavedash (planned) — depends on: Movement, Secondary Booster
2. Runtime Tuning Panel (planned) — depends on: all player systems

---

## Recommended Design Order

| Order | System | Priority | Layer | Est. Effort |
|-------|--------|----------|-------|-------------|
| 1 | Player Movement | MVP | Core | S (extract from CLAUDE.md) |
| 2 | Player Jump | MVP | Core | S (extract from CLAUDE.md) |
| 3 | Jetpack System | MVP | Core | M (complex — 4 directions, fuel, wall nudge) |
| 4 | Secondary Booster | MVP | Core | S (extract from CLAUDE.md) |
| 5 | Fuel Feedback | MVP | Presentation | S (extract from CLAUDE.md) |
| 6 | Camera System | Vertical Slice | Feature | S (simple follow → room snap) |
| 7 | Room System | Vertical Slice | Feature | M (room transitions, spawn points) |
| 8 | Event Bus | Vertical Slice | Core | S (standard pub/sub) |
| 9 | Gimmick Framework | Vertical Slice | Feature | M (extensible gimmick interface) |
| 10 | Chapter Configuration | Vertical Slice | Feature | M (per-chapter rules) |

---

## Circular Dependencies

- **Respawn ↔ Room System**: Respawn needs room spawn points; room transitions need to know if player is alive. Resolution: Room System owns spawn data, Respawn reads it. Room transitions check PlayerRespawn.isAlive before triggering.

---

## High-Risk Systems

| System | Risk Type | Risk Description | Mitigation |
|--------|-----------|-----------------|------------|
| Gimmick Framework | Design | Extensibility vs. complexity — each gimmick type has unique behavior | Prototype 2-3 gimmicks before finalizing interface |
| Momentum/Wavedash | Design | Must feel fair to non-speedrunners while rewarding skilled play | Playtest extensively; make it optional, not required |
| Booster Mode Swapping | Technical | Mid-room mode changes could create inconsistent player state | Careful state machine transitions; validate on swap |

---

## Progress Tracker

| Metric | Count |
|--------|-------|
| Total systems identified | 18 |
| Design docs created | 5 |
| Design docs reviewed | 0 |
| Design docs approved | 0 |
| MVP systems with GDDs | 5/8 (Movement, Jump, Jetpack, Secondary Booster, Fuel Feedback) |
| Vertical Slice systems with GDDs | 0/8 |

---

## Next Steps

- [x] Create 5 core GDDs (Movement, Jump, Jetpack, Secondary Booster, Fuel Feedback)
- [x] Pass Technical Setup gate (2026-04-19)
- [ ] Review and approve this systems enumeration
- [ ] Run `/design-review` on each completed GDD
- [ ] Create remaining MVP GDDs (Gravity System, Animation, Camera)
- [ ] Prototype the gimmick framework early (`/prototype gimmick-framework`)
- [ ] Create Event Bus ADR (most urgent architecture gap)
