# Chapter 1: Tutorial Room Designs

> **Status**: Built (4 rooms, compressed from original 15-room draft)
> **Updated**: 2026-04-26
> **Rooms**: 4 (built), original 15-room designs preserved below for reference
> **Room Size**: 60×34 units (ASCII designed at 30×14, scaled 2×)
> **Tile Size**: 1×1 units (16px at 16 PPU)

> **Note (2026-04-26)**: Tutorial compressed from 15 rooms to 4. Each room teaches multiple mechanics. The original 15 designs are preserved below but are NOT built — only the 4 compressed rooms exist in the scene. Build scripts at `Assets/Editor/BuildTutorialRooms.cs`.

## Built Rooms (4-room compressed tutorial)

### Room 1: "Move and Fly"
**Teaches**: Walk, jump, first spikes, jetpack (forced by tall wall), fuel awareness (mid-point landing)
- Left section: flat ground → staircase platforms → spike gap (first death)
- Middle: tall wall that can't be jumped → forces jetpack discovery
- Right section: wide spike pit → mid-point platform to refuel → exit

### Room 2: "Dash and Survive"
**Teaches**: Dash exists (small gaps), dash+jetpack combo (wide gap), dash pickups
- Small spike gaps dashable without jetpack → teaches dash
- Wide spike gap needs jetpack to cross + dash for precise landing
- Fuel and dash pickups placed strategically

### Room 3: "The Fork"
**Teaches**: Fuel gates (High cyan + Low red), fuel-state routing
- Spawn on center platform, two paths diverge
- Left: High fuel gate (>50%) — easier path if you conserved fuel
- Right: Low fuel gate (<20%) — harder path if you burned fuel
- Both paths lead to exit at top center
- **THIS is where the game's identity clicks**

### Room 4: "Graduation"
**Teaches**: Everything combined + gun mode swap + shootable target
- Fuel fork (High left, Low right)
- Gun swap zone (green, visual placeholder) + target (yellow, visual placeholder)
- Hazards, fuel pickup, dash pickup
- Multiple valid solutions depending on skill

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

### How to Build a Room in Unity

1. **Create a new Scene** or section in the existing scene
2. **Add a Tilemap**:
   - GameObject → 2D Object → Tilemap → Rectangular
   - Import Cave Story tilesets as Tile Palette (Window → 2D → Tile Palette)
   - Paint the room geometry (walls, floors, platforms)
   - Set Tilemap Collider 2D on the tilemap (for collision)
   - Set the tilemap's layer to **Ground (8)**
3. **Add a Room component**:
   - Create an empty GameObject at the room's center
   - Add `Room` component, set `roomSize` to (30, 17)
   - Set `roomId` to "ch1-room-01" etc.
   - Create a child empty as the spawn point, assign to `spawnPoint`
4. **Add interactive elements**:
   - Drag in FuelGate prefab, set required tier
   - Place hazard objects (BoxCollider2D trigger, Hazard component, layer 10)
   - Place fuel/dash pickups
5. **Connect rooms**:
   - Place rooms side by side (Room 1 center at x:0, Room 2 at x:30, etc.)
   - RoomManager detects when player crosses bounds
   - Camera lerps to new room automatically

### Room Naming Convention
- `ch1-room-01` through `ch1-room-15`
- Scene: `Chapter1.unity` (or all rooms in one scene, spaced 30 units apart)
