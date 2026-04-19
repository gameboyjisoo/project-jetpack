# Accessibility Requirements: Project Jetpack

> **Status**: Draft
> **Author**: pz_ma
> **Last Updated**: 2026-04-19
> **Accessibility Tier Target**: Basic
> **Platform(s)**: PC (keyboard + gamepad)
> **External Standards Targeted**:
> - WCAG 2.1 Level A (partial -- limited UI surface)
> - Xbox Accessibility Guidelines (XAG): N/A (PC-only for now)
> **Accessibility Consultant**: None engaged
> **Linked Documents**: `design/ux/interaction-patterns.md`

> **Why this document exists**: Per-screen accessibility annotations belong in
> UX specs. This document captures the project-wide accessibility commitments,
> the feature matrix across all systems, the test plan, and the audit history.
> It is created once during Technical Setup by the UX designer and producer,
> then updated as features are added and audits are completed. If a feature
> conflicts with a commitment made here, this document wins -- change the feature,
> not the commitment, unless a formal revision is approved.

---

## Accessibility Tier Definition

### This Project's Commitment

**Target Tier**: Basic

**Rationale**: Project Jetpack is a 2D pixel art platformer with fast-twitch
movement mechanics (jetpack, dash, variable-height jump). The primary
accessibility barriers are motor (precise timing, rapid directional input) and
visual (color-coded diegetic feedback for fuel state). The game currently has no
dialogue, no text-heavy systems, and no persistent HUD -- feedback is delivered
through particle colors and audio pitch. This diegetic design philosophy means
the game already provides redundant feedback channels (visual particles + audio
pitch for fuel state), which is accessibility-positive. Basic tier is appropriate
for the current scope: a solo prototype/vertical slice with minimal UI. Motor
accessibility barriers inherent to the platformer genre (precise timing, rapid
input) are partially mitigated by forgiveness windows (coyote time, jump buffer)
but are a known limitation of the genre commitment. Elevating to Standard tier
would require settings menus and input remapping UI that do not yet exist (Phase
2+).

**Features explicitly in scope (beyond tier baseline)**:
- Input remapping support via Unity New Input System (runtime rebinding
  capability exists at the framework level, though no rebinding UI is built yet)
- Redundant audio channel for fuel state (audio pitch shifts mirror particle
  color gradient -- provides non-visual feedback)

**Features explicitly out of scope**:
- Settings menu UI (no menus exist yet -- Phase 2+)
- Full input remapping UI (New Input System supports rebinding, but no player-
  facing rebinding screen exists)
- Subtitle system (no dialogue exists in the game)
- Screen reader support (no menu system to expose)
- Difficulty assist modes (genre commitment to precision platforming)

---

## Visual Accessibility

| Feature | Target Tier | Scope | Status | Implementation Notes |
|---------|-------------|-------|--------|---------------------|
| Colorblind considerations -- fuel gradient | Basic | Jetpack particle system | Needs Review | The fuel state gradient is cyan (full) -> orange (mid) -> red (empty). The orange-to-red transition is problematic for red-green colorblind players (protanopia, deuteranopia -- ~7% of men). The audio pitch channel provides a redundant non-visual signal, partially mitigating this. Future consideration: add a colorblind-friendly palette option or shape-based particle variation. |
| Color-as-only-indicator audit | Basic | All gameplay feedback | Partial | See audit table below. Audio pitch provides a redundant channel for fuel state, but particle color is currently the only *visual* differentiator for fuel level. |
| Screen flash / strobe risk | Basic | VFX, particle bursts | Not Assessed | Dash and jetpack activation produce particle bursts. Assess against Harding FPA standard (no more than 3 flashes/sec above luminance threshold). |
| Screen shake toggle | Basic | Camera system | Not Implemented | Screen shake is planned but not yet implemented. When added, it must ship with a toggle in accessibility settings. |
| Brightness/gamma controls | Basic | Global | Not Started | Deferred to Phase 2+ (settings menu required). |

### Color-as-Only-Indicator Audit

| Location | Color Signal | What It Communicates | Non-Color Backup | Status |
|----------|-------------|---------------------|-----------------|--------|
| Jetpack exhaust particles | Cyan -> Orange -> Red gradient | Fuel level (full -> mid -> empty) | Audio pitch shifts (high pitch = full, low pitch = empty). No visual shape or size change currently. | Partial -- audio backup exists, but no visual non-color backup. |
| Dash trail particles | Color variation | Dash activation feedback | Animation movement itself is the primary feedback. | Acceptable |

---

## Motor Accessibility

| Feature | Target Tier | Scope | Status | Implementation Notes |
|---------|-------------|-------|--------|---------------------|
| Input remapping (framework) | Basic | All gameplay inputs | Supported | Unity New Input System supports runtime rebinding. No player-facing rebinding UI yet. Keyboard and gamepad bindings are defined in Input Actions asset. |
| Input method switching | Basic | PC | Working | Player can switch between keyboard and gamepad at any time. No prompt icon switching implemented (no HUD prompts exist). |
| Forgiveness windows | N/A (design) | Jump timing | Implemented | Coyote time (0.1s) and jump buffer (0.1s) reduce precision requirements for jump timing. These are accessibility-positive design decisions baked into the movement system. |
| One-hand mode | Out of scope | -- | -- | Simultaneous movement + action input (e.g., holding direction while pressing jetpack) requires two hands on keyboard. Gamepad right trigger for jetpack allows one-handed play if movement is on left stick, but jump (Button South) and dash (Button West) still require the other hand. |
| Hold-to-press alternatives | N/A | -- | N/A | Jetpack and dash use press-to-activate (committed action), not hold. Jump uses variable hold (0.2s window) for height control. No sustained holds required. |

---

## Cognitive Accessibility

| Feature | Target Tier | Scope | Status | Implementation Notes |
|---------|-------------|-------|--------|---------------------|
| Pause anywhere | Basic | All gameplay states | Not Assessed | No pause system exists yet. Must be added in Phase 2+. |
| Minimal cognitive load (by design) | N/A | Core gameplay | Inherent | The game has no inventory, no dialogue trees, no quest tracking, no economy. The player tracks only: position, fuel level, and immediate threats. This is an accessibility strength of the minimal design. |
| Diegetic feedback clarity | N/A | Fuel system | Implemented | Fuel state is communicated via two redundant channels (particle color + audio pitch), reducing cognitive load vs. reading a HUD number. |

---

## Auditory Accessibility

| Feature | Target Tier | Scope | Status | Implementation Notes |
|---------|-------------|-------|--------|---------------------|
| Subtitles | Basic | All voiced content | N/A | No dialogue exists in the game. If dialogue is added in the future, subtitles must be implemented before shipping. |
| Independent volume controls | Basic | Music / SFX buses | Not Started | Deferred to Phase 2+ (settings menu required). |
| Audio as accessibility channel | N/A | Fuel system | Implemented | Audio pitch shifts provide a non-visual channel for fuel state. This is accessibility-relevant: deaf or hard-of-hearing players lose this channel and must rely solely on particle color. |

### Gameplay-Critical SFX Audit

| Sound Effect | What It Communicates | Visual Backup | Caption Required | Status |
|-------------|---------------------|--------------|-----------------|--------|
| Jetpack audio pitch (high->low) | Fuel level depleting | Particle color gradient (cyan->orange->red) | N/A (no subtitle system) | Implemented -- redundant visual channel exists |
| Jump sound | Jump activation | Player sprite animation | N/A | Implemented |
| Dash sound | Dash activation | Dash trail particles + movement | N/A | Implemented |
| Landing sound | Ground contact | Player sprite grounded state | N/A | Implemented |

---

## Diegetic Feedback as Accessibility Strategy

> **Project-specific note**: Project Jetpack intentionally uses diegetic feedback
> (particle colors + audio pitch) instead of a traditional HUD. This design
> decision has mixed accessibility implications:
>
> **Accessibility-positive**:
> - Audio pitch provides a redundant non-visual channel for fuel state
> - No HUD clutter means less visual noise and lower cognitive load
> - Feedback is spatially located on the player character, reducing eye travel
>
> **Accessibility-negative**:
> - Color-dependent visual channel (particle gradient) is the only visual
>   indicator of fuel level -- problematic for colorblind players
> - No numeric/bar representation of fuel for players who prefer precise values
> - Deaf or hard-of-hearing players lose the audio pitch channel and must rely
>   solely on particle color
>
> **Future consideration**: `GasMeterUI.cs` exists in the codebase but is
> intentionally unwired. This component could serve as an optional HUD bar
> for fuel state, toggled on in an accessibility settings menu. This would
> provide a third, non-color, non-audio channel for fuel information. Recommend
> wiring this as an opt-in accessibility option in Phase 2+.

---

## Known Intentional Limitations

| Feature | Tier Required | Why Not Included | Risk / Impact | Mitigation |
|---------|--------------|-----------------|--------------|------------|
| Settings menu / accessibility options UI | Basic+ | No menu system exists yet (Phase 2+). All current accessibility features are hardcoded or framework-level. | Players cannot customize accessibility features at runtime. | Input remapping available via New Input System at framework level. Settings UI is Phase 2+ priority. |
| Colorblind mode for fuel particles | Standard | Requires particle system rework and a settings toggle. No settings menu exists. | Red-green colorblind players (~7% of men) may have difficulty distinguishing orange from red in the fuel gradient. | Audio pitch provides a redundant non-visual channel. Future: colorblind palette option + optional HUD bar (GasMeterUI.cs). |
| Difficulty assist modes | Standard | Genre commitment to precision platforming. Forgiveness windows (coyote time, jump buffer) are the current mitigation. | Players with motor impairments may find the game inaccessible. | Forgiveness windows reduce timing precision requirements. Celeste-style assist mode is a future consideration but not in current scope. |
| Screen reader support | Comprehensive | No menu or text UI exists to expose. | Blind players cannot access the game. | The game is entirely visual/audio -- screen reader support would require a fundamentally different interaction model. |

---

## Open Questions

| Question | Owner | Deadline | Resolution |
|----------|-------|----------|-----------|
| Should GasMeterUI.cs be wired as an opt-in accessibility option before the full settings menu exists (e.g., via a keyboard shortcut toggle)? | pz_ma | Before vertical slice | Unresolved |
| What colorblind-friendly palette would work for the fuel gradient while maintaining the diegetic art style? | pz_ma | Before Phase 2 | Unresolved |
| When screen shake is implemented, should the default be on or off? (Accessibility best practice: off by default, or prominently offer the choice at first launch.) | pz_ma | Before screen shake implementation | Unresolved |
