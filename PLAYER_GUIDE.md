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
- **Jetpack** (Booster 2.0 style): press to activate while airborne, hold to sustain. Boosts in 4 cardinal directions at ~1.9× run speed. Direction = most recently pressed arrow key. **Gravity is completely disabled during jetpack** — you go exactly where you point, for exactly the distance your fuel allows. No invisible sinking or drift. ~1 second of fuel, recharges on landing. On release: horizontal boost halves X velocity, upward boost halves Y velocity, downward boost has no halving. Hitting a wall during horizontal boost nudges you upward.
- **Secondary Booster** fires a projectile and pushes you in the opposite direction (recoil). **Gravity is briefly disabled during the recoil burst** so you travel on a clean, predictable vector. 3 shots, recharges on ground. The booster's behavior can change depending on the chapter or room — in some areas it becomes a gun for hitting switches, while in others it stays as the default recoil tool.
- **Wavedash** (advanced tech): fire the secondary booster diagonally downward near the ground to convert recoil into horizontal ground speed. Chain wavedashes to build momentum beyond normal run speed.

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
    RoomCamera.cs              — Follow camera (will become room-snapping later)
  Level/
    Room.cs                    — Defines room boundaries
    RoomManager.cs             — Detects room transitions
    MovementTestLevel.cs       — Runtime test level (temporary)
  Player/
    PlayerController.cs        — Core movement (ground, jump, jetpack)
    JetpackGas.cs              — Gas resource system with events
    SecondaryBooster.cs        — Recoil weapon / precision movement (will become swappable mode host)
    PlayerAnimator.cs          — Drives Animator from player state
    JetpackParticles.cs        — Exhaust color shift & sputter (fuel visual cue)
    JetpackAudioFeedback.cs    — Engine pitch decay & sputter SFX (fuel audio cue)
  UI/
    GasMeterUI.cs              — Legacy gas fill bar (not used — minimal UI philosophy)
```

### Planned Structure (after refactor)
See `docs/superpowers/specs/2026-04-07-granular-development-plan-design.md` §6 for the full target file structure. Key additions: `Core/` (event bus, shared types), `Gimmicks/` (environmental effects), `Player/Boosters/` (swappable booster modes), `UI/TuningPanel.cs` (runtime parameter tuning).

### Other Key Locations
```
Assets/Resources/PlayerInput.inputactions  — Input bindings (loaded at runtime)
Assets/Scenes/TestRoom.unity               — Main test scene
Assets/Sprites/Player/MyChar.png           — Placeholder sprites (Cave Story)
Assets/Animations/                         — Animation clips and controller
ProjectSettings/                           — Physics2D gravity is -20
```

### Current Tuning Values (in PlayerController Inspector)
| Parameter | Value | What it does |
|---|---|---|
| Move Speed | 10 | Ground run speed |
| Ground Accel/Decel | 120/120 | Near-instant speed changes |
| Air Mult | 0.65 | Air control (Celeste's AirMult) |
| Jump Force | 18 | Jump height |
| Var Jump Time | 0.2s | Hold window for variable jump height (Celeste) |
| Jump H Boost | 2.5 | Horizontal boost added on jump (Celeste) |
| Coyote Time | 0.1s | Jump grace after leaving ground (Celeste) |
| Boost Speed | 19 | Jetpack activation velocity (~1.9× run speed, Booster 2.0) |
| Gas Consumption Rate | 100 | ~1 second of fuel |
| Wall Nudge Speed | 2 | Upward push when hitting wall during horizontal boost |
| Fall Gravity Multiplier | 2.0 | Fast fall speed |
| Apex Gravity Multiplier | 0.5 | Jump peak hang time (requires holding jump, Celeste) |
| Max Fall Speed | 30 | Terminal velocity |

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
- Once the tuning panel is built (F1 toggle), use it to live-adjust movement values during Play mode — then tell Claude Code which preset to lock in

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
| Animator warnings in console | Expected — animator controller not connected yet, warnings are suppressed |
| Game looks stretched / mobile | Set Game tab aspect ratio dropdown to **16:9** |
| Can't find input actions file | It's in `Assets/Resources/`, not `Assets/Scripts/Player/` |
