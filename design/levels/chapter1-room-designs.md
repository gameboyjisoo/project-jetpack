# Chapter 1: Tutorial Room Designs

> **Status**: In Progress (user hand-designing rooms via Level Editor)
> **Updated**: 2026-04-26
> **Room Size**: 60×34 units
> **Tile Size**: 1×1 units (16px at 16 PPU)
> **Build method**: Level Editor Workflow — Tile Palette (PrtCave + Interactables) + Room Tool (`Project Jetpack > New Room`)

> **Note (2026-04-26)**: Old MCP-generated rooms (MCP_Room_01 through 04) were deleted. Tutorial rooms are now hand-designed by the developer using the Tile Palette workflow. The original 15 ASCII room designs below are reference material, not built.

## Current Rooms (hand-designed)

### ch1-Room-01 (in progress)
**Teaches**: Fuel state as a key (Low gate), mechanic combination (jetpack + dash)
**Tutorial design pattern**: Safe space → comprehension check (gate) → danger zone

Layout (lower-left section built so far):
- Player spawns top-left on a ledge
- Drops down a corridor with NO hazards — safe space to experiment with jetpack
- Low fuel gate (red) blocks the only way forward — player must burn fuel to reach 0-20%
- After the gate: spike rows slightly longer than max jetpack range (~12-13 units) — forces player to combine jetpack + dash to cross
- Fuel recharges on landing after the gate, so spikes test full-fuel capabilities + dash combo, not depleted survival

**Design principles demonstrated:**
- Safe space before the lesson (no hazards before the gate)
- Gate as comprehension check ("do you understand fuel states?"), not skill test
- Obstacle sizing forces specific mechanic combinations (spikes > jetpack range = must use both jetpack and dash)
- Each landing resets fuel — sections are independent fuel puzzles

---

## Original 15-Room Designs (REFERENCE ONLY — not built)

## Room Layout Legend

```
# = solid wall/floor (Ground layer 8, Tilemap)
. = empty space (air)
S = player spawn point
E = exit (transition to next room)
X = hazard/spikes (layer 10, trigger)
F = fuel pickup (cyan, recharges jetpack)
D = dash pickup (magenta, recharges dash)
G = fuel gate — append tier: GH (high/cyan), GM (mid/orange), GL (low/red)
W = mode swap zone (dash → gun)
T = shootable switch/target
P = moving platform
```

## Rooms 1-3: Walk & Jump Basics

These rooms teach ONLY walk and jump. No jetpack, no dash. Pure Celeste fundamentals.

### Room 1: "First Steps"
**Teaches**: Walk left/right, basic platforming
**No hazards, no pressure — just learn to move.**

```
##############################
#............................#
#............................#
#............................#
#............................#
#............................#
#............................#
#............................#
#............................#
#.S..........####............#
#..........##....##....####..#
#........##..........##....E.#
#......##....................#
##############################
```

- Spawn on the left, platforms staircase right and up
- Exit is on the right wall, elevated — must climb platforms
- No gaps, no deaths possible — pure confidence builder

---

### Room 2: "First Gap"
**Teaches**: Jump across a gap, variable jump height
**First pit — but shallow and non-lethal (landing platform below).**

```
##############################
#............................#
#............................#
#............................#
#............................#
#............................#
#............................#
#............................#
#.S.....   ......   .....E..#
#.####.#   .####.   .####.###
#......#   ......   ........#
#......#####....#####.......#
#............................#
##############################
```

- Three platforms with gaps between them
- Gaps are 3 tiles wide (jumpable with basic jump, no coyote needed)
- Lower floor catches missed jumps — player can try again
- Short tap jump vs full jump both work, but full jump is easier

---

### Room 3: "Commitment"
**Teaches**: Precise jumping, coyote time, first hazards
**First spikes — dying teaches that red = danger.**

```
##############################
#............................#
#............................#
#............................#
#............................#
#.S..........................#
#.####.......####.......##E.#
#......XXXXX......XXXXX.....#
#......#####......#####.....#
#............................#
#............................#
#............................#
#............................#
##############################
```

- Platforms over spike pits
- Gaps are 5 tiles wide — requires full jump + good timing
- Coyote time helps players who jump late off the edge
- First death experience — teaches the respawn cycle
- Spikes are visually distinct (red) from platforms

---

## Rooms 4-6: Jetpack Introduction

These rooms introduce the jetpack. The player MUST use it to progress.

### Room 4: "Liftoff"
**Teaches**: Jetpack exists, upward boost
**A vertical gap that can't be jumped — only jetpacked.**

```
##############################
#............................#
#............................#
#.............E..............#
#............####............#
#............................#
#............................#
#............................#
#............................#
#............................#
#....####################....#
#............................#
#.S..........................#
#............................#
##############################
```

- Spawn at bottom, exit on a high platform
- The height gap is too tall for a jump (~8 tiles up)
- Player discovers jetpack (Z key) — upward boost reaches the platform
- Simple, vertical, one mechanic

---

### Room 5: "Four Directions"
**Teaches**: Jetpack works in all 4 cardinal directions, horizontal boost
**Multiple elevated platforms requiring different boost directions.**

```
##############################
#............................#
#.####.......................#
#..........####..............#
#....................####....#
#............................#
#............................#
#....................####.E..#
#.S..........................#
#.####.......####............#
#............................#
#............................#
#............................#
##############################
```

- Platforms at varying heights and horizontal positions
- Some require upward boost, others horizontal
- Player naturally experiments with direction + jetpack
- Multiple valid routes through the room

---

### Room 6: "Fuel Awareness"
**Teaches**: Fuel runs out, you need to land to recharge
**A long horizontal gap with a mid-point landing platform.**

```
##############################
#............................#
#............................#
#............................#
#............................#
#............................#
#............................#
#.S..............##.......E..#
#.####...............####.####
#....XXXXXXXXXXXXXXXX........#
#....##################......#
#............................#
#............................#
##############################
```

- Spawn on left platform, exit on right
- Spike pit below — must fly across
- Gap is too wide for one jetpack burst (~15 tiles)
- Mid-point platform lets you land, refuel, and continue
- First lesson: fuel is limited, plan your landings

---

## Rooms 7-8: Dash Introduction

### Room 7: "Quick Burst"
**Teaches**: Dash exists, covers short distance fast
**A series of small gaps that are easier with dash than jetpack.**

```
##############################
#............................#
#............................#
#............................#
#............................#
#............................#
#............................#
#.S.......   ...   ...   .E.#
#.####.##. X .#. X .#. X .###
#..........XXX...XXX...XXX...#
#..........###...###...###...#
#............................#
#............................#
##############################
```

- Small platforms with spike-filled gaps (3 tiles each)
- Jump works but dash is cleaner and faster
- Player discovers dash naturally: "I have another button"
- 1 ammo = must land between each gap to recharge

---

### Room 8: "Dash + Jetpack"
**Teaches**: Combining dash and jetpack in sequence
**A gap that requires jetpack to cross, then a precise dash landing.**

```
##############################
#............................#
#............................#
#............................#
#............................#
#............................#
#.S..........................#
#.####...................##E.#
#.....XXXXXXXXXXXXXXXX..X....#
#.....##################X....#
#.......................X....#
#............................#
#............................#
##############################
```

- Wide spike gap requiring jetpack to cross
- Landing platform is narrow with spikes on the right
- Must dash to precisely stop on the platform (momentum retain helps)
- First combo: jetpack for distance, dash for precision

---

## Rooms 9-10: Pattern A — Drain to Enter

### Room 9: "Red Gate"
**Teaches**: Fuel gates exist, drain fuel to open red gate
**Simple room with a red gate blocking the exit.**

```
##############################
#............................#
#............................#
#............................#
#............................#
#..........GL................#
#..........||................#
#.S........||.............E..#
#.####.....||.............####
#..........||................#
#..........||................#
#............................#
##############################
```

- Red/low fuel gate blocks the path
- Player must jetpack around (wasting fuel) until below 20%
- Gate opens when exhaust turns red — visual resonance
- Simple flat room, no hazards — focus on the gate mechanic

---

### Room 10: "Drain and Dodge"
**Teaches**: Drain fuel while navigating hazards (maneuvering IS fuel spending)

```
##############################
#............................#
#.....XXXXX..................#
#............................#
#..............XXXXX.........#
#............................#
#.S.......GL.............E..#
#.####....||..............####
#....XXXXX||.................#
#....#####||.................#
#..........XXXXX.............#
#............................#
##############################
```

- Hazards scattered in the space before the red gate
- Must jetpack around to dodge AND drain fuel
- Sloppy flying = death. Efficient flying = arrive at gate in red tier
- First hybrid: maneuvering skill + fuel timing

---

## Rooms 11-12: Pattern B — Conserve to Enter

### Room 11: "Blue Gate"
**Teaches**: Some gates need HIGH fuel — don't waste it

```
##############################
#............................#
#............................#
#............................#
#.............GH.............#
#.............||.............#
#.S...........||..........E..#
#.####........||..........####
#.............||.............#
#XXXXXXXXXXXXXXXXXXXXXX......#
#######################......#
#............................#
##############################
```

- Cyan/high gate blocks the path
- Spike floor means you MUST fly — but flying drains fuel
- The challenge: fly over spikes efficiently enough to arrive above 50%
- Short, direct jetpack path = enough fuel. Wandering = locked out.

---

### Room 12: "Efficient Flight"
**Teaches**: Route optimization under fuel pressure

```
##############################
#............................#
#.......XXXXX....XXXXX......#
#............................#
#....XXXXX....XXXXX....GH...#
#............................||
#.S..........................||
#.####.......................||
#....XXXXXXXXXXXXXXXXXX...E..#
#....##################...####
#............................#
#............................#
##############################
```

- Spike hazards create an obstacle course above a spike floor
- Must navigate around hazards without spending too much fuel
- Cyan gate at the end — fuel must be above 50%
- Multiple routes: risky short path (saves fuel) vs safe long path (spends more)

---

## Room 13: Pattern C — Fuel Fork

### Room 13: "The Fork"
**Teaches**: Fuel state determines your path — THE key design moment

```
##############################
#.............E..............#
#............####............#
#............................#
#..GH....................GL..#
#..||....................||..#
#..||....................||..#
#..||....................||..#
#..||......####.S......||..#
#..||......####........||..#
#..||....................||..#
#..||..XXXXXXXXXXXXXX..||..#
#..||..##############..||..#
##############################
```

- Spawn in the middle on an elevated platform
- Left path: cyan gate (high fuel, easier obstacles behind it)
- Right path: red gate (low fuel, harder but shorter path to exit)
- Exit is at the top center — both paths lead there
- Player's fuel state at the fork determines their route
- THIS is where the game's identity clicks: "my fuel level changes my experience"

---

## Room 14: Pattern E — Gun Mid-Flight

### Room 14: "Aim and Fire"
**Teaches**: Gun mode, shooting switches while flying

```
##############################
#............................#
#............................#
#.............T..............#
#............................#
#............................#
#.S..........####.........E..#
#.####...................#####
#....XXXXXXXXXXXXXXXX........#
#....##################......#
#............................#
#....W.......................#
##############################
```

- Mode swap zone (W) near spawn switches dash → gun
- Spike pit requires jetpacking across
- Target (T) above mid-flight — must shoot it while jetpacking
- Hitting the target opens the exit platform or removes a barrier
- The challenge: aim accurately WHILE managing flight trajectory

---

## Room 15: Finale

### Room 15: "Everything Together"
**Teaches**: All mechanics combined — the graduation test

```
##############################
#.............E..............#
#............####............#
#............................#
#..GH.......T........GL.....#
#..||................||.....#
#..||................||.....#
#..||.....####.......||.....#
#..||................||.....#
#..||..XXXXXXXXXX....||.....#
#..||..##########..F.||.....#
#.S..........W...D...........#
#.####.......................#
##############################
```

- Fuel fork (cyan left, red right)
- Mode swap zone + target (gun needed for one path)
- Fuel and dash pickups placed strategically
- Hazards throughout
- Multiple valid solutions depending on skill level
- Completing this room = you understand the game

---

## Scene Setup Guide

### How to Build a Room in Unity (Level Editor Workflow)

1. **Create Room shell**: `Project Jetpack > New Room` (Ctrl+Shift+R). Auto-positions the room, creates Room + Grid + Walls (with colliders) + Interactables (with SpawnTileManager) + SpawnPoint. Paints border walls with transition openings.
2. **Paint walls/floors**: Window > 2D > Tile Palette → select a Cave Story palette (e.g., PrtCave Palette) → **Active Target → Walls** → paint.
3. **Paint interactables**: Tile Palette → select **Interactables Palette** → **Active Target → Interactables** → paint hazards (orange spikes), pickups (cyan circle, magenta diamond), fuel gates (colored bars), spawn point (green arrow).
4. **At runtime**: `SpawnTileManager` spawns prefabs at each SpawnTile position and clears placeholder visuals.
5. **Save scene** (Ctrl+S) after changes.

**Key rule**: Always check the **Active Target** dropdown — Walls for ground tiles, Interactables for hazards/pickups/gates.

### Room Naming Convention
- User-designed rooms: `ch1-Room-XX` (capital R)
- Claude-generated rooms: `ch1-room-XX` (lowercase r)
- Scene: `TestRoom.unity` (all rooms in one scene, 60 units apart)
- Room size: 60×34 units (default from Room Tool)
