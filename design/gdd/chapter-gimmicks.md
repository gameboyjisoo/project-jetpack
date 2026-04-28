# Chapter Gimmicks — Concept Overview

> **Status**: Concept-level. Not yet formalized into full GDDs. Prototype first, then write detailed specs from what works.
>
> **Core rule** (from design-direction.md): Every gimmick MUST interact with the fuel system. No generic platformer obstacles.

## Chapter 1 — Fuel Gates (Tutorial) ✅ Implemented

Three-tier barriers (High/Mid/Low) that open based on player's current fuel state. Teaches fuel awareness. Safe space → gate comprehension check → danger zone with spikes.

---

## Chapter 2 — Gravity Switches

**Concept**: Environmental switches that flip gravity direction (down→up, down→left, etc.).

**Fuel interaction**: Gravity direction determines which surface is "ground" — and ground is where fuel recharges. Flip gravity and your refueling spots move. The jetpack itself is unaffected (gravity=0 axiom), so mid-flight feels identical, but landing/recharging changes completely.

**Ideas to explore**:
- Spikes in 3 directions + gravity switch = only one safe "floor" depending on current gravity
- "Drop" through a gap by flipping gravity, then jetpack/dash to safety before hitting the far wall
- Sections where gravity flips mid-room, forcing players to rethink routing on the fly
- Fuel gates on ceilings/walls that only become reachable (as landing surfaces) after a gravity flip

**Open questions**:
- How many gravity directions? Just up/down flip, or full 4-directional?
- Does the player trigger the switch, or is it automatic (zone-based)?
- Do fuel gates respond to gravity? (Leaning yes — gates are ground, gravity determines what's ground)

---

## Chapter 3 — Closing Platforms & Crushers

**Concept**: Timed platforms that open/close on a cycle, forcing approach timing and dash commitment.

**Fuel interaction**: Player has ~1 second of fuel. Can't hover for 4+ seconds waiting. Must time their approach to arrive as the platform opens, or use a nearby perch to wait and pounce with a dash.

**Ideas to explore**:
- **Timed gates**: Platform opens for 1s every 2-3s. Perch nearby, watch the cycle, dash through when open.
- **Ceiling crushers**: Detect player below, slam down fast. Can't walk past — must dash. Multiple in sequence = ammo management (1 dash charge, recharges on ground).
- **Fuel decision**: Burn fuel to hover closer for an easier dash, or save fuel by waiting on the perch for a harder, longer dash?

**Open questions**:
- Crusher telegraph time (how much warning)?
- Can crushers be combined with gravity switches in later chapters?
- Does the timed platform cycle reset when player dies, or is it persistent?

---

## Chapter 4 — Gun Mode Puzzles

**Concept**: Switches in unreachable locations that must be shot. Player hovers mid-air while the projectile travels to the target.

**Fuel interaction**: Gun is free (no fuel, no ammo), but hovering while waiting for the projectile to reach the switch costs fuel. Creates a distance/fuel tradeoff:
- Fly closer to switch first → less hover time → but fuel spent on movement
- Shoot from far away → longer hover → more fuel spent waiting

**Ideas to explore**:
- Shoot upward into a narrow shaft to hit a switch while hovering below
- Switch opens a fuel gate or closing platform — chain gun + hover + dash through before it closes
- Multiple switches that must be hit in sequence (player repositions between shots)
- Switches on timers (revert after X seconds, forcing fast follow-up action)

**Open questions**:
- Does the switch activate instantly on projectile contact, or with a delay?
- Can switches be hit from any direction, or only specific angles?
- How does mode swap (dash↔gun) factor in? Must the player give up dash to use gun?

---

## Chapter 5 — Blind Zones

**Concept**: Areas where the screen goes dark mid-jetpack. Player navigates by sound and fuel sense.

**Fuel interaction**: This is the payoff of diegetic-only feedback design. In the dark, the player still has:
- **Audio burst interval** (widens as fuel drains — tight bursts = full, gaps = empty)
- **Audio pitch** (drops from 1.0 → 0.55 with fuel)
- **Exhaust particle glow** (faint light in darkness? — could be the only visual)

Since jetpack speed is fixed and fuel drain is linear, a skilled player can estimate distance traveled by listening to fuel consumption.

**Ideas to explore**:
- Short blind sections (2-3 seconds) — fly straight to a known platform
- Blind zones with audio-only hazard cues (spike hum? crusher slam sound?)
- Progressive difficulty: first blind zones are straight lines, later ones have turns
- Faint exhaust glow as the ONLY light source — player sees ~1 tile around them

**Open questions**:
- How does the player know a blind zone is coming? (Visual cue at the entrance)
- Does the blind zone affect the whole screen or just a region?
- Can the player memorize the layout on a sighted first attempt, then execute blind?

---

## Chapter 6 — Crosshair (Timed Dashes)

**Concept**: A large crosshair/reticle follows the player. It tracks for several seconds, halts briefly, then fires — killing the player if they don't dash out of the way.

**Fuel interaction**: Dashes cost ammo (1 charge, recharges on ground). If the crosshair forces a dodge every cycle and you're airborne, you MUST have dash ammo available. Creates pressure to touch ground between shots to recharge.

**Ideas to explore**:
- Cycle: ~5s tracking + ~1s lock-on (halt) + fire. Tune from playtesting.
- Lock-on speed could scale with fuel level: full fuel = slow tracking (bright exhaust = easy to see), low fuel = fast tracking (dim = hard to track, crosshair compensates)
- Multiple crosshairs in late-chapter rooms
- Crosshair + other gimmicks (must dodge crosshair WHILE navigating closing platforms)

**Open questions**:
- Is the crosshair per-room or does it persist across rooms?
- Does the crosshair have a visual/audio telegraph before firing?
- Can the player destroy or disable the crosshair source?

---

## Cross-Chapter Modifier — Wind Turbines

**Concept**: NOT Celeste-style push. Wind affects fuel economy, not player movement.

**Option A — Fuel drain modifier**:
- Flying into wind = 2× fuel drain (halves effective range)
- Flying with wind = 0.5× fuel drain (doubles range)
- No physical push — player moves at normal speed, just burns fuel differently

**Option B — Player-powered turbines**:
- Jetpack exhaust near a turbine activates it (opens doors, moves platforms)
- Costs fuel to power — how much fuel will you spend on the shortcut vs. taking the harder route?

**Design rationale**: Appears starting mid-game as an amplifier for other gimmicks, not as its own chapter. Wind + gravity switches, wind + closing platforms, etc.

---

## Chapter Mapping Summary

| Chapter | Gimmick | Core Tension |
|---|---|---|
| 1 | Fuel gates | Learn fuel management |
| 2 | Gravity switches | Recharge spots move, rethink routing |
| 3 | Closing platforms + crushers | Timing + fuel budgeting |
| 4 | Gun puzzles | Routing efficiency, hover cost vs. travel cost |
| 5 | Blind zones | Navigate by audio/fuel sense |
| 6 | Crosshair | Forced dashes under pressure |
| 2+ | Wind turbines (modifier) | Amplifies other gimmicks via fuel drain |
