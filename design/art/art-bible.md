# Art Bible: Project Jetpack

## Document Status
- **Version**: 1.0
- **Last Updated**: 2026-04-19
- **Owned By**: art-director
- **Status**: Draft

## Visual Identity Summary

Project Jetpack is a 2D pixel art platformer with a retro aesthetic rooted in the 16x16 tile grid. The visual language prioritizes clean, readable rooms where the player can parse every challenge at a glance. Underground and otherworldly environments use muted background palettes to push the player character and interactive elements forward, creating a clear visual hierarchy without relying on persistent HUD elements.

## Reference Board

| Reference | Medium | What We're Taking |
| --------- | ------ | ----------------- |
| Cave Story | Game | Pixel art style, underground tilesets, 16x16 grid, sprite proportions, feedback effects |
| Celeste | Game | Clean room layouts, screen-by-screen level design clarity, minimal UI during gameplay |
| Mega Man X | Game | Responsive character animation language, clear projectile readability |
| Kero Blaster | Game | Studio Pixel's refined sprite economy, small-canvas expressiveness |

## Color Palette

### Primary Palette

[PLACEHOLDER -- original art Phase 5]

Current sprites use Cave Story's existing palette. The following captures the intended direction for original art:

| Name | Hex (approx.) | Usage |
| ---- | ------------- | ----- |
| Cave Dark | #2A2A3A | Primary background fill for underground stages |
| Cave Mid | #5A5A7A | Secondary rock and wall tones |
| Sky Blue | #80C0E0 | Outdoor / Oside-style open sky backgrounds |
| Lava Orange | #E06020 | Hell-themed environment accents, danger zones |
| Exhaust Cyan | #00E0FF | Jetpack exhaust initial burst |
| Exhaust Orange | #FFA020 | Jetpack exhaust mid-thrust |
| Exhaust Red | #FF3030 | Jetpack exhaust overheat / max thrust |
| Player White | #F0F0F0 | Player character highlight, ensures readability |

### Emotional Color Mapping

| Game State | Dominant Colors | Mood |
| ---------- | --------------- | ---- |
| Exploration | Cool blues, muted grays | Curiosity, calm tension |
| Traversal challenge | Neutral stone tones, clear contrast on hazards | Focus, precision |
| Safe zones | Warmer earth tones, softer lighting | Relief, brief rest |
| Danger / hazards | Reds, oranges, high-contrast warning colors | Urgency, alertness |
| Jetpack flight | Cyan-to-orange exhaust gradient against dark bg | Exhilaration, momentum |

## Art Style

### Rendering Style

Pixel art, 16x16 base tile size. All sprites use **Point (no filter)** sampling with **no compression**. Pixels Per Unit (PPU) is locked at **16** across all assets to maintain a consistent pixel-perfect grid.

### Proportions

- **Player character**: Fits within a 16x16 bounding box (one tile). [PLACEHOLDER -- original art Phase 5] Current placeholder is Cave Story's MyChar (200x64 spritesheet sliced into 20 named frames).
- **Environment tiles**: 16x16 pixels each, composing rooms on a uniform grid.
- **Camera**: Orthographic, size 8.5 -- this frames roughly 17 tiles vertically, giving a comfortable view of the play space.

### Level of Detail

- **Characters**: Low pixel count demands expressive silhouettes and strong color contrast over fine detail. Each frame should read clearly at native resolution.
- **Environments**: Tilesets use 2-3 value steps per material (highlight, mid, shadow). Decorative tiles may break the grid for organic feel but must snap to the 16px grid for collision.
- **UI**: Minimal pixel art elements. Text uses a pixel font at integer scaling. Icons are 16x16 or 8x8.
- **VFX**: Small particle sprites (4x4 to 8x8), relying on color and motion rather than detail.

### Visual Hierarchy

1. **Player character** -- always the most prominent element. Bright, high-contrast silhouette against the environment.
2. **Hazards and interactables** -- use warning colors (red, orange) or distinct shape language to stand out from passive terrain.
3. **Foreground terrain** -- readable but subordinate to the player. Mid-value palette.
4. **Background decoration** -- lowest contrast, desaturated. Must never compete with gameplay-relevant elements.
5. **UI overlays** -- drawn on topmost sorting layer, used sparingly. Diegetic feedback (screen shake, particle bursts) preferred over HUD bars.

## Character Art Standards

[PLACEHOLDER -- original art Phase 5]

**Current state**: Player uses Cave Story's MyChar.png (200x64 spritesheet, 20 named sprites covering idle, walk, jump, fall, and directional variants).

**Guidelines for original art**:
- 16x16 pixel bounding box per frame.
- Strong silhouette -- the player must be identifiable against any tileset background within 1 pixel of border contrast.
- Limited palette per character: 4-6 colors max (including outline).
- Animation keyframes over tweens -- 2-4 frames per action to match retro cadence.
- Jetpack visual attachment must be visible in all poses to reinforce the core mechanic identity.
- Directional sprites: left/right facing at minimum; no rotation, only mirrored or redrawn frames.

## Environment Art Standards

[PLACEHOLDER -- original art Phase 5]

**Current state**: 5 Cave Story tilesets used as placeholders:
| Tileset | Visual Theme | Intended Use |
| ------- | ------------ | ------------ |
| PrtCave | Dark underground rock | Core cave stages |
| PrtFall | Overgrown/waterfall caverns | Mid-game nature stages |
| PrtHell | Lava, fire, brimstone | Late-game danger stages |
| PrtMimi | Interior/mechanical | Constructed interiors |
| PrtOside | Open sky, exterior | Outdoor / sky stages |

**Guidelines for original art**:
- Each tileset must contain: solid fill, top edge, side edges, corners (inner/outer), one-way platforms, decorative variants.
- Background tiles are drawn at reduced value range (darker, less saturated) to separate from collision geometry.
- Rooms should be buildable from a single tileset with optional accent pieces from a shared decoration set.
- Maximum tileset atlas size: 256x256 pixels (16x16 tiles = 256 tile slots per set).
- Parallax background layers: 1-2 additional layers at 0.5x and 0.25x scroll rate, using simplified/blurred versions of the environment palette.

## UI Art Standards

[PLACEHOLDER -- original art Phase 5]

**Current state**: Cave Story placeholder UI sprites (ArmsImage, ItemImage, TextBox, Title).

**Design principles**:
- **Minimal persistent HUD**: No health bars, ammo counters, or ability meters during standard gameplay. Jetpack fuel communicated through diegetic visual cues (exhaust color shift: cyan -> orange -> red).
- **Contextual UI only**: Prompts and indicators appear only when relevant and fade when not needed.
- **Text boxes**: Pixel art bordered panels for dialogue/narrative moments. Maximum width ~75% of screen. Pixel font at integer scale.
- **Menu screens**: Simple list-based navigation. Selected items highlighted with color shift or bracket indicators, not complex widget art.
- **Typography**: Pixel font, monospaced or near-monospaced. Minimum legible size: 8px glyph height at native resolution.
- **Icon style**: 16x16 or 8x8 pixel icons, matching the game's tile grid. Silhouette-first design -- recognizable without color.

## VFX Standards

**Particle style**: Small pixel sprites (4x4 to 8x8), spawned in bursts with randomized velocity and short lifetimes. No alpha blending -- particles use solid pixel colors and simply appear/disappear.

**Jetpack exhaust** (primary VFX):
- Color gradient tied to fuel state: **cyan (#00E0FF)** at full -> **orange (#FFA020)** at mid -> **red (#FF3030)** at low/empty.
- 2-4 particle sprites per burst, emitting downward from the jetpack attachment point.
- Particle lifetime: 0.15-0.3 seconds. Slight upward drift after spawn to simulate heat dissipation.

**Projectile effects**:
- Source: Bullet.png spritesheet. [PLACEHOLDER -- original art Phase 5]
- Clear directional motion. Projectiles must contrast against all environment palettes.

**Feedback / caret effects**:
- Source: Caret.png spritesheet. [PLACEHOLDER -- original art Phase 5]
- Used for hit confirmation, pickups, and environmental interaction feedback.
- Brief flash frames (1-3 frames), high brightness, centered on interaction point.

**Screen effects**:
- Screen shake: 1-3 pixel displacement, 0.1-0.2 second duration. Reserved for impactful moments (landing from height, taking damage, explosions).
- No post-processing filters (bloom, vignette, etc.) -- keep the pixel-perfect aesthetic clean.

## Asset Production Standards

### Naming Convention

`[category]_[name]_[variant]_[size].[ext]`

Examples:
- `char_player_idle_16x16.png`
- `tile_cave_solid_16x16.png`
- `vfx_exhaust_cyan_8x8.png`
- `ui_textbox_default_128x48.png`

### Texture Standards

| Category | Max Resolution | Format | Filtering | Compression | PPU |
| -------- | -------------- | ------ | --------- | ----------- | --- |
| Characters | 256x256 (spritesheet) | PNG | Point (no filter) | None | 16 |
| Environments | 256x256 (tileset atlas) | PNG | Point (no filter) | None | 16 |
| UI | 256x128 | PNG | Point (no filter) | None | 16 |
| VFX | 128x128 (spritesheet) | PNG | Point (no filter) | None | 16 |

### Sprite Import Settings (Unity)

All sprite assets must use these import settings:
- **Sprite Mode**: Single or Multiple (for spritesheets)
- **Pixels Per Unit**: 16
- **Filter Mode**: Point (no filter)
- **Compression**: None
- **Max Size**: Sufficient to contain the source without downscaling

### Sorting Layers

Rendering order from back to front:

| Order | Sorting Layer | Contents |
| ----- | ------------- | -------- |
| 0 | Default | Fallback / unused |
| 1 | Background | Parallax layers, distant scenery |
| 2 | Tilemap | Environment collision tiles, platforms |
| 3 | Player | Player character, NPCs, enemies |
| 4 | Foreground | Foreground decorations that overlap the player |
| 5 | UI | HUD elements, text boxes, menus |

### Animation Standards

- **Frame rate**: 8-12 FPS for character animations (retro cadence).
- **Idle**: 2-4 frames, subtle motion (breathing, blinking).
- **Walk/Run**: 4-6 frames per cycle.
- **Jump/Fall**: 2 frames (ascend pose, descend pose). Transitions are instant.
- **Jetpack thrust**: 2 frame loop with exhaust particle emission synced to frame changes.
- All animations use Unity's Animator with simple state machines -- no blend trees for pixel art.

## Accessibility

- **Colorblind safety**: Hazards and interactables are distinguished by shape and animation, never by color alone. Jetpack fuel state uses both color shift AND particle density change.
- **Minimum text size**: 8px glyph height at native resolution, scaling to integer multiples at higher display resolutions.
- **High contrast mode**: [PLACEHOLDER -- original art Phase 5] Plan to offer an optional high-contrast outline toggle that adds a 1px bright border around the player and hazards.
- **Readability**: Game pillar #2 (read challenges at a glance) inherently supports accessibility -- if a room reads well for sighted players, contrast and shape language are already strong.
- **Icon + color**: All UI indicators use icon or shape differentiation in addition to color. No game-critical information conveyed through color alone.
