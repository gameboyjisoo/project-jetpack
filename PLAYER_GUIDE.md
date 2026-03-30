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
| Jetpack (hold) | RT (Right Trigger) |
| Fire (Secondary Booster) | X (West) |

### How Movement Works
- **Ground movement** is Celeste-style — near-instant acceleration and deceleration, snappy direction changes.
- **Jump** is variable height — tap for short hop, hold for full jump. Has coyote time (0.08s) and jump buffering (0.1s). Floats briefly at the apex.
- **Jetpack** (Booster 2.0 style): hold to boost in 4 cardinal directions. Direction = most recently pressed arrow key. Pure cardinal movement, no gravity during boost. ~1 second of fuel, recharges on landing. On release: horizontal velocity halves (drift), upward velocity halves, downward transitions to normal gravity.
- **Secondary Booster** fires a projectile and pushes you in the opposite direction (recoil). 3 shots, recharges on ground. For precise micro-adjustments.

---

## Project Structure

### Scripts
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
    SecondaryBooster.cs        — Recoil weapon / precision movement
    PlayerAnimator.cs          — Drives Animator from player state
  UI/
    GasMeterUI.cs              — Gas fill bar (not wired up yet)
```

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
| Jump Force | 18 | Jump height |
| Jump Cut Multiplier | 0.3 | How much tap-jump is cut |
| Jetpack Thrust | 11 | Boost speed (near walk speed) |
| Gas Consumption Rate | 100 | ~1 second of fuel |
| Fall Gravity Multiplier | 2.0 | Fast fall speed |
| Apex Gravity Multiplier | 0.4 | Jump peak hang time |
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

2. **Create a private repo on GitHub:**
   - Go to https://github.com/new
   - Repository name: `project-jetpack`
   - Select **Private**
   - Do NOT check any boxes (no README, no .gitignore, no license)
   - Click **Create repository**

3. **Open a terminal** and run these commands:
   ```bash
   cd "C:\Jisoo's Stuff\Personal Projects\Unity\Project Jetpack"
   git init
   git add -A
   git commit -m "Initial commit: movement prototype"
   git branch -M main
   git remote add origin https://github.com/YOUR_USERNAME/project-jetpack.git
   git push -u origin main
   ```
   GitHub will prompt you to log in via browser or token.

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
git clone https://github.com/YOUR_USERNAME/project-jetpack.git
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
