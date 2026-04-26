# Project Jetpack — Player & Developer Guide

## Opening the Project
1. Open **Unity Hub**
2. Click **Open** and navigate to `C:\Jisoo's Stuff\Personal Projects\Unity\Project Jetpack`
3. Make sure Unity **6000.0.34f1** is selected as the editor version
4. Once open, open the scene: `Assets/Scenes/TestRoom.unity`

## Controls

### Keyboard
| Action | Primary | Alt |
|---|---|---|
| Move | WASD | Arrow Keys |
| Jump | Space | C |
| Jetpack (hold) | Z | Left Shift |
| Fire (Secondary Booster) | X | — |

### Gamepad
| Action | Button |
|---|---|
| Move | Left Stick / D-pad |
| Jump | A (South) |
| Jetpack (press to activate, hold) | RT (Right Trigger) |
| Fire (Secondary Booster) | X (West) |

### How Movement Works
- **Ground movement** is Celeste-style — near-instant acceleration and deceleration, snappy direction changes. Air control at 65% of ground.
- **Jump** is Celeste-style variable height — hold jump to maintain upward speed for 0.2 seconds, release to let gravity take over naturally (no instant velocity cut). Adds a small horizontal boost in your input direction. Has coyote time (0.1s) and jump buffering (0.1s). Floats at the apex with half gravity while holding jump.
- **Jetpack** (Booster 2.0 style): press to activate while airborne, hold to sustain. Boosts in 4 cardinal directions at ~1.83× run speed. Direction = most recently pressed arrow key. **Gravity is completely disabled during ALL jetpack directions** — you go exactly where you point, for exactly the distance your fuel allows. No invisible sinking or drift. This is a core design axiom. ~1 second of fuel, recharges on landing. On release: horizontal boost halves X velocity only, upward boost halves Y velocity only, downward boost has no halving.
- **Secondary Booster** is now a swappable mode system with two options:
  - **Dash mode** (default): 1 ammo (recharges on ground), 8-direction fixed-distance burst. Celeste-style freeze frame (0.05s) on activation. 25% horizontal momentum retained after dash. Dash pickups recharge mid-air. Has dash buffer (0.1s) for forgiving input.
  - **Gun mode**: FREE to use (no ammo, no fuel cost), fires projectile in aim direction (8-dir). No movement effect on player. Challenge is aiming accurately mid-flight while managing jetpack trajectory.
  - Modes swap via zones in levels (temporary or permanent).
- **Wavedash** (core tech for fuel economy): diagonal-down air dash near ground converts to 1.2× dash horizontal speed (38.4 units/sec). Speed maintained for 0.12s (jump window to carry it). Only triggers from airborne dashes — ground dashes are normal. This is how skilled players conserve jetpack fuel and arrive at fuel gates with more options.
- **Fuel gates** react to your fuel state: cyan gates need >50% fuel (High tier), orange gates need 20-50% (Mid tier), red gates need <20% (Low tier). The environment cares about when you spend fuel, not just where.

### Reading Your Fuel (No HUD Bar)
There is no fuel bar on screen. Instead, the jetpack tells you its state directly:

- **Watch the exhaust**: Bright cyan = full. Turns orange at half, red near empty. When it starts sputtering/flickering, you have about 0.2 seconds left.
- **Listen to the engine**: At full fuel, the jetpack fires rapid bursts (like Cave Story's Booster). As fuel drains, the bursts slow down with gaps between them, and the pitch drops. Near empty, the pitch wobbles erratically. When you hear a dry click, you're out.
- Both cues work together — after a few runs, reading fuel becomes instinctive.

---

## Project Structure

### Scripts (Current)
```
Assets/Scripts/
  Camera/
    RoomCamera.cs              — Room-follow + room-snap transition camera
  Core/
    GameEventBus.cs            — Central pub/sub event system
  Gimmicks/
    FuelGate.cs                — Barrier that opens based on fuel tier (High/Mid/Low)
  Level/
    Room.cs                    — Room boundary definition (size, spawn, ID)
    RoomManager.cs             — Auto-discovers rooms, detects transitions
    GasRechargePickup.cs       — Mid-air fuel pickup, respawns on landing
    DashRechargePickup.cs      — Mid-air dash pickup, respawns on landing
    Hazard.cs                  — Kills player on trigger contact
  Player/
    PlayerController.cs        — Orchestrator (ticks all sub-components)
    PlayerMovement.cs          — Ground movement (Celeste-style MoveTowards)
    PlayerJump.cs              — Variable jump, coyote time, buffer, apex gravity
    PlayerJetpack.cs           — 4-direction boost, fuel consumption, velocity halving
    PlayerGravity.cs           — Fast fall, apex float, gravity modifiers
    SecondaryBooster.cs        — Dash mode + Gun mode swap system
    JetpackGas.cs              — Fuel resource with tier events (High/Mid/Low)
    PlayerAnimator.cs          — Drives Animator from player state
    PlayerRespawn.cs           — Death → respawn at room spawn point
    JetpackParticles.cs        — Exhaust color shift (cyan→orange→red)
    JetpackAudioFeedback.cs    — Engine pitch decay & sputter SFX
    Projectile.cs              — Gun mode projectile
  UI/
    GasMeterUI.cs              — Legacy gas bar (intentionally not used)
```

### Other Key Locations
```
Assets/Resources/PlayerInput.inputactions  — Input bindings (loaded at runtime)
Assets/Scenes/TestRoom.unity               — Main test scene (ch1-Room-01 in progress)
Assets/Tiles/                              — Tile assets (PrtCave, PrtMimi, etc. + Interactables)
Assets/Tiles/Palettes/                     — Tile Palettes for painting rooms
Assets/Prefabs/Interactables/              — Prefabs spawned by SpawnTiles at runtime
Assets/Scripts/Tiles/                      — SpawnTile + SpawnTileManager system
Assets/Sprites/Player/MyChar.png           — Placeholder sprites (Cave Story)
Assets/Sprites/Placeholders/               — Shape-coded placeholder sprites for interactables
Assets/Editor/                             — Level editor tools (LevelEditorSetup, RoomTools, etc.)
ProjectSettings/                           — Physics2D gravity is -20
design/gdd/                                — Game design documents (5 GDDs + design direction)
design/levels/chapter1-room-designs.md     — Room designs and layout reference
```

### Current Tuning Values
| Parameter | Value | What it does |
|---|---|---|
| Move Speed | 6 | Ground run speed (reduced from 10 for tighter feel) |
| Ground Accel/Decel | 120/120 | Near-instant speed changes (Celeste-level snappy) |
| Air Mult | 0.65 | Air control (Celeste's AirMult) |
| Jump Force | 8 | ~3.5× character height full hold, ~1.6× short tap |
| Var Jump Time | 0.2s | Hold window for variable jump height |
| Jump H Boost | 2.4 | 40% of moveSpeed horizontal boost on jump |
| Coyote Time | 0.1s | Jump grace after leaving ground |
| Boost Speed (jetpack) | 11 | ~1.83× moveSpeed (Booster 2.0 ratio) |
| Gas Consumption Rate | 100 | ~1 second of fuel |
| Boost Speed (dash) | 32 | Punchy burst, covers ~4.8 tiles in 0.15s |
| Boost Duration (dash) | 0.15s | Celeste-length dash |
| Max Dash Ammo | 1 | Single dash, recharges on ground |
| Wavedash Speed Mult | 1.2 | 38.4 units/sec horizontal conversion |
| Fall Gravity Mult | 2.0 | Fast fall speed |
| Apex Gravity Mult | 0.5 | Jump peak hang time (requires holding jump) |
| Max Fall Speed | 20 | Terminal velocity (reduced from 30) |
| Project Gravity | -20 | In Physics2D settings |

---

## Working with Claude Code

### Getting Started Each Session
1. Open terminal in the project: `cd "C:\Jisoo's Stuff\Personal Projects\Unity\Project Jetpack"`
2. Launch: `claude`
3. Claude reads `CLAUDE.md` automatically and knows the project context.

### Workflow Loop
1. Tell Claude what to change — Claude edits scripts/scene files
2. Unity hot-reloads automatically
3. Hit Play and test
4. Come back to Claude — "the jump is too floaty" or "that works, now add X"

### Tips
- Be specific: "slow the jetpack down by 30%" not "fix the movement"
- Describe game feel: "landing should feel heavier"
- Paste Unity console errors directly for debugging
- Use `/commit` to save your work with a good message
- Tuning values are scattered across component Inspector fields — roadmap includes a centralized PlayerTuning ScriptableObject

### Parallel Sessions
You can run multiple Claude Code sessions simultaneously on different tracks:
- **Track A** sessions work on player feel (Player/, UI/ scripts only)
- **Track B** sessions work on infrastructure (Level/, Camera/, Gimmicks/, Core/ scripts only)
- Each session commits to its own branch (`track-a/...` or `track-b/...`) — you merge to main yourself

### When to Use Unity Editor vs Claude Code
| Task | Use |
|---|---|
| Writing/editing scripts | Claude Code |
| Tweaking values in code | Claude Code |
| Live-tweaking values during Play | Unity Editor |
| Painting tilemaps | Unity Editor |
| Level design & room layout | Unity Editor |
| Debugging errors | Claude Code |
| Adding new systems | Claude Code |
| Playtesting | Unity Editor |

---

## Uploading to GitHub

### First Time Setup

1. **Install Git** if you don't have it: https://git-scm.com/download/win

2. **The repo is already created:** https://github.com/gameboyjisoo/project-jetpack (private)

3. Git is already initialized and the first commit has been pushed. You're good to go.

### Saving Your Progress (After Changes)

From the terminal:
```bash
cd "C:\Jisoo's Stuff\Personal Projects\Unity\Project Jetpack"
git add -A
git commit -m "Describe what changed"
git push
```

Or from Claude Code, just type `/commit` and Claude will generate a commit message for you. Then run `git push` to upload.

### Downloading on Another Machine

```bash
git clone https://github.com/gameboyjisoo/project-jetpack.git
```
Then open the folder in Unity Hub.

### Going Back to a Previous Save

```bash
git log --oneline              # list all saves
git checkout <commit-hash>     # view a previous save (read-only)
git checkout main              # go back to latest
```

To permanently undo a specific change:
```bash
git revert <commit-hash>       # creates a new save that undoes that change
```

---

## Troubleshooting

| Problem | Fix |
|---|---|
| Character falls through ground | Ground Layer on PlayerController should be "Ground" (code forces layer 8 as fallback) |
| No movement / input not working | Check that `Assets/Resources/PlayerInput.inputactions` exists |
| Animator warnings in console | Expected — animator controller not fully connected, warnings are suppressed |
| Game looks stretched / mobile | Set Game tab aspect ratio dropdown to **16:9** |
| Can't find input actions file | It's in `Assets/Resources/`, not `Assets/Scripts/Player/` |
| SerializeField values in Inspector override script defaults | After changing defaults in code, manually update Inspector values or right-click component → Reset |
