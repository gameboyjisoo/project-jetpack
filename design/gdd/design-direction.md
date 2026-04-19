# Design Direction: Project Jetpack

> **Status**: Approved
> **Created**: 2026-04-20
> **Last Updated**: 2026-04-20
> **Context**: This document captures a critical design conversation about what makes Project Jetpack unique and how it differs from Celeste. This is the foundational design identity that all future gimmicks, rooms, and chapters should be built on.

---

## The Problem We Solved

The original pitch was "Celeste but with the Cave Story jetpack." But Celeste already has extensive aerial gameplay (8-direction dash, wall climb, dash crystals, extended air sequences). Simply adding a jetpack to Celeste's formula doesn't create a new game — it creates "Celeste with more air time."

The question became: **what can a player do in Project Jetpack that they literally cannot do in Celeste?**

---

## What Makes Project Jetpack Unique

### Celeste's Air Game: Instant and Binary
Every air action in Celeste is **instant and committed**. Dash fires, it's done. You either have a dash or you don't. Air time is fractions of a second. The challenge is spatial: "how do I get from A to B?"

### Project Jetpack's Air Game: Sustained and Analog
The jetpack gives **sustained, directed flight** for up to a full second. The fuel gauge is **analog** — a continuous value from 100% to 0%, communicated through diegetic feedback (particle color, audio pitch). The player is actively controlling direction during flight, and the environment has time to react.

**Key differences from Celeste:**
1. You can **hover in place** (up-boost against gravity)
2. You can **change your mind mid-air** (end boost early, dash a different direction)
3. You are **in the air long enough for the level to change around you**
4. Your **fuel state is visible and readable** by both the player and the environment

---

## The Two Pillars

### Pillar 1: Maneuvering Skill (Celeste DNA)
- Navigate tight spaces with precise jetpack direction choices
- Chain jetpack → dash → freefall through obstacle corridors
- Corner correction, coyote time, and forgiving inputs reward good execution
- Rooms get physically harder (narrower gaps, more hazards, tighter timing)
- Jump, ground movement, and dash feel **exactly like Celeste**

### Pillar 2: Fuel Timing (What's Uniquely Ours)
- The environment **reacts to your fuel state**
- You have to manage **WHEN** you spend fuel, not just **WHERE**
- Some obstacles require full fuel, others require empty fuel
- The diegetic feedback (particles shifting cyan→orange→red, audio pitch dropping) is your clock
- Fuel is analog, not binary — the level can read your exact percentage

### The Fusion
Maneuvering IS fuel spending. They're the same action. A room that demands precise jetpacking through a corridor is also draining your fuel at a specific rate. The challenge is: can you navigate skillfully enough to arrive at the fuel-gated obstacle with the right amount remaining?

---

## Gimmick Categories

### Category 1: Maneuvering Challenges
Classic platformer obstacles that test jetpack and dash precision.
- Tight corridors requiring precise horizontal boost
- Hazard gauntlets (spikes, moving hazards)
- Precision gaps requiring specific boost direction + timing
- Freefall sections between boost segments

### Category 2: Fuel-State Gates
Obstacles that read the player's fuel level and react accordingly.
- **Fuel-threshold barriers**: Doors that only open when fuel is below/above a certain percentage
- **Fuel-reactive platforms**: Surfaces that exist only at certain fuel levels (full fuel = different path than empty fuel)
- **Fuel-drain zones**: Areas that consume fuel faster, forcing route optimization
- **Fuel-locked hazards**: Hazards that activate when you're jetpacking (detecting exhaust/fuel burn) — forces alternating between boost and freefall

### Category 3: Hybrid
Rooms that demand precise maneuvering AND specific fuel states.
- Navigate a corridor efficiently enough to arrive at a fuel gate with the right level
- Multiple paths through a room gated by different fuel thresholds — your route depends on how much fuel you have when you reach the fork

---

## Secondary Mode System

The secondary slot swaps between **dash** and **gun** (and potentially other modes in future chapters).

### Dash Mode (Default)
- 1 ammo, recharges on ground
- Celeste-exact feel: instant burst, 8 directions, freeze frame
- **Costs your one dash charge** — a precious repositioning tool
- Mid-air dash pickups can recharge it (level-placed)

### Gun Mode (Swap)
- **Free to use** — does NOT consume fuel or dash ammo
- Fires a projectile to hit distant switches, targets, triggers
- **No repositioning ability** — you lose your burst movement tool
- Trade-off: free ranged interaction vs. losing your emergency repositioning

### The Design Decision
Swapping to gun mode is a **fuel management decision**, not just a combat option. With the gun, you can interact with distant fuel-state switches WITHOUT spending fuel to reach them — but you can't dash to safety if you mess up. With the dash, you can reposition precisely but can't hit anything at range.

Rooms can force this choice: "There's a fuel-gate switch across the room. Do you dash over to hit it (spending your dash charge and fuel to fly there) or swap to gun and shoot it from here (keeping your fuel but losing your escape option)?"

---

## What This Means for Level Design

### Tutorial Chapter (Vertical Slice)
1. **Rooms 1-3**: Walk, jump basics (pure Celeste)
2. **Rooms 4-6**: Jetpack introduction (4 directions, fuel management)
3. **Rooms 7-8**: Dash introduction (precision repositioning)
4. **Rooms 9-10**: First fuel-state gate (simple: "drain fuel to open door")
5. **Rooms 11-12**: Maneuvering + fuel management combined
6. **Room 13-15**: Chapter finale combining all mechanics

### Later Chapters
Each chapter introduces ONE new fuel-state gimmick type. The maneuvering difficulty escalates independently. The combination of new gimmick + harder maneuvering creates exponential complexity from linear additions.

---

## What NOT to Do

- Don't add gimmicks that work identically to Celeste equivalents (wind that just pushes you = Celeste Chapter 4)
- Don't make gimmicks that ignore the fuel system (generic moving platforms with no fuel interaction)
- Don't require gun mode to progress unless the room explicitly teaches the swap
- Don't make fuel management feel like resource grinding — it should feel like a puzzle, not a chore

---

## Open Questions

| Question | Status |
|----------|--------|
| How many fuel-threshold levels should the game distinguish? (e.g., full/half/empty vs. continuous percentage) | Open |
| Should fuel-state gates show their threshold visually? (e.g., a door with a color matching the particle gradient) | Open |
| Can the player see their exact fuel percentage, or only read it from diegetic cues? | Decided: diegetic only, no HUD |
| Should gun mode have limited ammo or be truly free? | Decided: free to use, no ammo cost |
| How does wavedash interact with fuel? (drain fuel? free?) | Open |
