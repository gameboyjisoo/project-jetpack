# Interaction Pattern Library: Project Jetpack

> **Status**: Draft
> **Author**: pz_ma
> **Last Updated**: 2026-04-19
> **Version**: 0.1
> **Engine**: Unity 6
> **UI Framework**: N/A (no menu UI currently -- diegetic feedback only)
> **Related Documents**:
> - `design/accessibility-requirements.md` -- accessibility commitments
>
> **Why this document exists**: Project Jetpack currently has no menu UI and
> no persistent HUD. All player feedback is diegetic (particle colors, audio
> pitch) or implicit (sprite animation, movement response). This is intentional
> -- the design philosophy prioritizes in-world feedback over UI overlays.
>
> This document captures the interaction patterns that DO exist: the gameplay
> input patterns that define how the player controls the character. When menu
> UI is added in Phase 2+, standard control patterns (buttons, toggles, sliders)
> will be added here following the template conventions.

---

## Design Philosophy

The game has **no menus, no inventory, no dialogue, and no persistent HUD**.
Player state is communicated through diegetic feedback: particle colors and
audio pitch for fuel level, sprite animation for movement state. This is a
deliberate design choice, not a gap.

Future phases (Phase 2+) will add: title screen, pause menu, settings. When
those are built, this document will expand to cover standard UI patterns.

---

## Input Bindings

### Keyboard

| Action | Primary | Alternate |
|--------|---------|-----------|
| Move | WASD | Arrow Keys |
| Jump | Space | C |
| Jetpack | Z | Left Shift |
| Fire/Dash | X | -- |

### Gamepad

| Action | Binding |
|--------|---------|
| Move | Left Stick (analog) |
| Jump | Button South (A / Cross) |
| Jetpack | Right Trigger |
| Fire/Dash | Button West (X / Square) |

Input is handled via Unity New Input System, which supports runtime rebinding
at the framework level. No player-facing rebinding UI exists yet.

---

## Pattern Catalog Index

| Pattern Name | Category | Description | Status |
|-------------|----------|-------------|--------|
| Press-to-Activate | Gameplay Input | Single press triggers committed action (jetpack, dash) | Stable |
| Direction-from-Input | Gameplay Input | Action inherits direction from movement keys held at activation | Stable |
| Diegetic State Display | Feedback | Game state shown via in-world particles and audio, not HUD | Stable |
| Variable Hold | Gameplay Input | Jump height controlled by hold duration | Stable |
| Forgiveness Windows | Gameplay Input | Coyote time and jump buffer accept mistimed inputs | Stable |

---

## Gameplay Input Patterns

---

### Press-to-Activate

**Category**: Gameplay Input
**Status**: Stable
**Used By**: Jetpack activation, Fire/Dash

**Description**: A single button press triggers the action immediately. The
action is committed -- there is no hold-to-sustain or hold-to-charge. Once
pressed, the action executes fully.

**Behavior**:

| Input | Response |
|-------|----------|
| Button down | Action begins immediately |
| Button held | No additional effect (not a hold input) |
| Button released | No effect (action already committed on press) |

**Design Rationale**: Committed actions create higher-stakes decisions than
hold-to-sustain patterns. The player must choose when to use limited resources
(fuel, dash cooldown) rather than feathering them. This is core to the
Cave Story jetpack feel -- each activation is a deliberate choice.

**Accessibility Notes**: Press-to-activate is more accessible than hold-to-
sustain for players with motor impairments that affect sustained grip. No
hold-to-toggle alternative is needed because the input is already a single
press.

---

### Direction-from-Input

**Category**: Gameplay Input
**Status**: Stable
**Used By**: Jetpack (4-directional), Fire/Dash (8-directional)

**Description**: The direction of an action is determined by which movement
keys are held at the moment of activation. If no movement keys are held,
the action defaults to the player's current facing direction.

**Behavior**:

| Movement Input at Activation | Jetpack Direction | Dash Direction |
|------------------------------|-------------------|----------------|
| Up | Up | Up |
| Down | Down | Down |
| Left | Left | Left |
| Right | Right | Right |
| Up + Right | -- (snaps to nearest cardinal) | Up-Right (diagonal) |
| Up + Left | -- (snaps to nearest cardinal) | Up-Left (diagonal) |
| Down + Right | -- (snaps to nearest cardinal) | Down-Right (diagonal) |
| Down + Left | -- (snaps to nearest cardinal) | Down-Left (diagonal) |
| No input | Player facing direction | Player facing direction |

**Design Rationale**: Decoupling aim from a separate aim stick/button keeps
the control scheme minimal (no aim button, no aim state). The player's
movement intention IS their aim intention. This works because both jetpack
and dash are short, committed actions -- the player sets direction, then fires.

**Accessibility Notes**: This pattern reduces the number of simultaneous inputs
required. The player holds a direction (one hand) and presses an action button
(other hand or same hand). No simultaneous multi-button press is required.

---

### Diegetic State Display

**Category**: Feedback
**Status**: Stable
**Used By**: Fuel system (jetpack)

**Description**: Game state is communicated through in-world visual and audio
feedback rather than a HUD overlay. The jetpack exhaust particles change color
and the audio pitch shifts to indicate fuel level.

**Feedback Channels**:

| Fuel Level | Particle Color | Audio Pitch | Meaning |
|------------|---------------|-------------|---------|
| Full | Cyan | High | Fuel plentiful |
| Mid | Orange | Medium | Fuel depleting |
| Empty | Red | Low | Fuel critical/exhausted |

**Design Rationale**: Diegetic feedback reduces screen clutter and keeps the
player's eyes on the action rather than a HUD corner. The dual-channel
approach (visual + audio) provides redundancy.

**Accessibility Notes**: This pattern is accessibility-relevant in both
positive and negative ways. See `design/accessibility-requirements.md` for
the full analysis. Summary:
- Positive: Audio pitch provides a non-visual redundant channel
- Negative: Particle color gradient (orange->red) is problematic for red-green
  colorblind players; deaf/hard-of-hearing players lose the audio channel
- Future mitigation: Optional HUD bar via GasMeterUI.cs (exists but
  intentionally unwired)

---

### Variable Hold

**Category**: Gameplay Input
**Status**: Stable
**Used By**: Jump

**Description**: Jump height is controlled by how long the jump button is held,
within a fixed time window.

**Behavior**:

| Input | Response |
|-------|----------|
| Button down | Jump begins (minimum height guaranteed) |
| Button held (up to 0.2s) | Jump height increases proportionally |
| Button released (before 0.2s) | Jump reaches height proportional to hold duration |
| Button held (beyond 0.2s) | No additional height -- window has closed |

**Design Rationale**: Variable jump height is a genre standard for precision
platformers (Celeste, Hollow Knight, Cave Story). It gives the player fine
control over vertical positioning, which is essential for tight level design.
The 0.2s window is short enough to feel responsive but long enough to be
intentional.

**Accessibility Notes**: The 0.2s hold window is a motor accessibility
consideration -- players with tremor or delayed motor response may have
difficulty controlling jump height precisely. This is a known limitation
accepted as part of the precision platformer genre commitment. Forgiveness
windows (below) partially mitigate timing precision requirements for jump
initiation, though not for jump height control.

---

### Forgiveness Windows

**Category**: Gameplay Input
**Status**: Stable
**Used By**: Jump system

**Description**: Small timing windows that accept slightly mistimed inputs,
reducing frustration from near-miss jumps.

**Parameters**:

| Window | Duration | Behavior |
|--------|----------|----------|
| Coyote Time | 0.1s | After walking off a ledge, the player can still jump for 0.1s as if they were grounded. Prevents "I was still on the platform" frustration. |
| Jump Buffer | 0.1s | If the player presses jump up to 0.1s before landing, the jump executes immediately on landing. Prevents "I pressed it but nothing happened" frustration. |

**Design Rationale**: These windows are invisible to the player but dramatically
reduce perceived unfairness. Both values (0.1s) are industry-standard for the
genre. Celeste uses similar values. The windows do not make the game easier --
they make it feel fair.

**Accessibility Notes**: Forgiveness windows are an accessibility feature in
disguise. They reduce the precision required for jump timing, which benefits
players with motor impairments, input lag from wireless controllers, and
display lag from TV gaming setups. These values could be exposed as tunable
settings in an accessibility menu (longer windows = more forgiving) in a
future phase.

---

## Future Patterns (Phase 2+)

The following patterns will be documented when their corresponding features
are implemented:

- **Title Screen** -- screen layout, start game flow
- **Pause Menu** -- pause/resume, settings access, quit confirmation
- **Settings Screen** -- audio, controls, accessibility toggles
- **Button (Primary/Secondary)** -- standard menu button interactions
- **Toggle** -- on/off settings (screen shake, optional HUD)
- **Slider** -- volume controls, timing window adjustments
- **Screen Push/Pop** -- menu navigation transitions
- **Escape/Cancel** -- universal back behavior

---

## Open Questions

| Question | Owner | Deadline | Resolution |
|----------|-------|----------|-----------|
| Should forgiveness window durations (coyote time, jump buffer) be exposed as accessibility settings? | pz_ma | Phase 2 settings menu | Unresolved |
| When menus are added, should the interaction pattern library expand to full template coverage or remain minimal? | pz_ma | Phase 2 planning | Unresolved |
