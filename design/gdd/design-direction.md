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

**Wavedash is the key enabler**: Without wavedash, the 1-second jetpack is too short to create meaningful fuel management choices. Wavedash gives skilled players a **fuel-free movement option** — they can cover horizontal distance without spending jetpack fuel. This creates the headroom for fuel forks: a player who wavedashes through early sections arrives at the fork with more fuel than one who jetpacked, unlocking different routes.

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
See "Tutorial Chapter Layout" section below for the full 15-room breakdown with patterns.

### Later Chapters
Each chapter introduces ONE new fuel-state gimmick type. The maneuvering difficulty escalates independently. The combination of new gimmick + harder maneuvering creates exponential complexity from linear additions.

---

## What NOT to Do

- Don't add gimmicks that work identically to Celeste equivalents (wind that just pushes you = Celeste Chapter 4)
- Don't make gimmicks that ignore the fuel system (generic moving platforms with no fuel interaction)
- Don't require gun mode to progress unless the room explicitly teaches the swap
- Don't make fuel management feel like resource grinding — it should feel like a puzzle, not a chore

---

## Fuel Tier System (Decided 2026-04-20)

Three tiers matching the exhaust particle gradient. Gates use the same color language.

| Tier | Fuel Range | Exhaust Color | Gate Color | Thresholds |
|------|-----------|---------------|------------|------------|
| **High** | 50-100% | Cyan | Cyan glow | `midThreshold = 0.5` |
| **Mid** | 20-50% | Orange | Orange glow | `lowThreshold = 0.2` |
| **Low** | 0-20% | Red + sputter | Red glow | |

**Implementation**: `FuelTier` enum in JetpackGas.cs. `FuelGate.cs` in Assets/Scripts/Gimmicks/. Gates open/close in real-time as fuel changes. Color-matched visual feedback. Event Bus integration.

## Wavedash (Implemented 2026-04-20)

Diagonal-down dash near ground → horizontal speed conversion. **Does not consume jetpack fuel** — only costs the 1 dash ammo charge. This is the fuel-free movement option that creates the skill/fuel management headroom.

- Speed: 1.2× dash speed (38.4 units/sec)
- Speed maintained for 0.12s (jump window to carry into air)
- Only triggers from airborne dashes (ground dashes are normal)
- Skilled players use wavedash to conserve jetpack fuel for fuel-gated sections

## Gun Mode Clarification (2026-04-20)

The gun challenge is NOT "shoot instead of fly." It's: **aim and fire accurately WHILE managing jetpack trajectory mid-flight.** Example: jetpacking right, but must shoot a switch above you at the right moment. Three simultaneous challenges: maneuvering + fuel timing + aim precision.

## Room Design Patterns (Decided 2026-04-20)

Vertical Slice uses patterns A + B + C + E:

| Pattern | Name | Description | Teaches |
|---------|------|-------------|---------|
| A | Drain to Enter | Red gate blocks path. Jetpack around to drain fuel below 20%. | Fuel gates exist |
| B | Conserve to Enter | Cyan gate at end of hazard gauntlet. Use too much fuel dodging = locked out. | Fuel efficiency matters |
| C | Fuel Fork | Path splits into cyan route (high fuel, easier) and red route (low fuel, harder). | Fuel state affects your path |
| E | Gun Mid-Flight | Shoot switches while jetpacking. Aim precision under flight pressure. | Secondary mode skill |

### Tutorial Chapter Layout (Vertical Slice)
1. **Rooms 1-3**: Walk, jump basics (pure Celeste)
2. **Rooms 4-6**: Jetpack introduction (4 directions, fuel management)
3. **Rooms 7-8**: Dash introduction (precision repositioning)
4. **Rooms 9-10**: Pattern A — first fuel-state gate (drain to open)
5. **Rooms 11-12**: Pattern B — conserve fuel through hazards
6. **Room 13**: Pattern C — fuel fork (two paths based on fuel state)
7. **Room 14**: Gun introduction + Pattern E (shoot mid-flight)
8. **Room 15**: Finale combining all patterns

## Open Questions

| Question | Status |
|----------|--------|
| How many fuel-threshold levels should the game distinguish? | Decided: Three tiers (High/Mid/Low) matching exhaust colors |
| Should fuel-state gates show their threshold visually? | Decided: Color-matched to exhaust gradient + particles when resonating |
| Can the player see their exact fuel percentage, or only read it from diegetic cues? | Decided: diegetic only, no HUD |
| Should gun mode have limited ammo or be truly free? | Decided: free to use, no ammo cost |
| How does wavedash interact with fuel? | Decided: does NOT consume jetpack fuel, only costs 1 dash ammo |
| Should wavedash chain into jump (hyper dash)? | Partially implemented: speed maintained for 0.12s jump window. Full chain tech deferred. |
| What audio/visual cues should gates emit when they match the player's fuel state? | Open |
| Should fuel-drain zones exist (areas that consume fuel faster)? | Open — fits the design but not yet implemented |
