# ADR-0006: Diegetic Fuel Feedback System

## Status
Accepted

## Date
2026-04-14

## Last Verified
2026-04-19

## Decision Makers
- Project lead (pz_ma)

## Summary
No HUD bar for fuel state. Instead, communicate fuel state through diegetic feedback on and around the player character: particle color gradients and sputter effects (JetpackParticles.cs), plus dynamic SFX interval/pitch manipulation (JetpackAudioFeedback.cs). Design principle: "If the player needs to look away from the character to understand their state, the feedback has failed."

## Engine Compatibility

| Field | Value |
|-------|-------|
| **Engine** | Unity 6 (6000.0.34f1) |
| **Domain** | VFX / Audio |
| **Knowledge Risk** | MEDIUM — Unity 6 is post-LLM-cutoff |
| **References Consulted** | Unity ParticleSystem API, AudioSource.PlayOneShot API, ParticleSystem.MainModule documentation |
| **Post-Cutoff APIs Used** | None confirmed — ParticleSystem and AudioSource APIs are stable across Unity versions |
| **Verification Required** | Confirm ParticleSystem color-over-lifetime gradient updates at runtime behave as expected in Unity 6; confirm AudioSource pitch modification applies immediately to currently playing clips via PlayOneShot |

## ADR Dependencies

| Field | Value |
|-------|-------|
| **Depends On** | None — standalone design decision; reads fuel state but does not modify it |
| **Enables** | ADR-0005 (fuel-empty event triggers boost deactivation and velocity halving) |
| **Blocks** | Fuel system cannot be considered player-facing until feedback is implemented |
| **Ordering Note** | Can be implemented independently of other ADRs; only requires a normalized fuel value (0.0 to 1.0) exposed by the fuel system |

## Context

### Problem Statement
The player needs to know how much jetpack fuel remains so they can make informed decisions about when to boost and when to conserve. The conventional solution is a HUD bar, but HUD elements pull the player's eyes away from their character — exactly where attention must be during tight platforming. We need a feedback system that communicates fuel state without requiring the player to look anywhere other than where they are already looking: at their character.

### Constraints
- Player character is 16x16 pixels — sprite-based feedback is too subtle at this resolution
- Feedback must be perceivable during fast movement and screen shake
- Must degrade gracefully on low-end hardware (particle count, audio channels)
- Must not obscure platforming hazards or level geometry
- GasMeterUI.cs exists as a traditional HUD bar but is intentionally not wired up; it may return as an accessibility toggle

### Requirements
- Visual feedback must communicate fuel level from full to empty through color and behavior
- Audio feedback must communicate fuel level through SFX pacing and pitch
- Low-fuel state must create urgency without being annoying
- Empty-fuel state must have a clear terminal cue
- All feedback must be localized to the player character's position on screen

## Decision

Fuel state is communicated through two parallel diegetic systems that reinforce each other: particles (visual) and SFX (audio). Neither system requires the player to look away from their character.

### Visual Feedback — JetpackParticles.cs

A ParticleSystem attached to the player character provides the primary visual channel:

1. **Color gradient**: The particle system's color-over-lifetime maps to fuel percentage.
   - 100% fuel: **cyan** — clean, healthy exhaust
   - 50% fuel: **orange** — warming, draining
   - 0% fuel: **red** — critical, about to die
   - Gradient is continuous, not stepped — intermediate fuel levels produce intermediate colors
2. **Sputter effect**: Below **20% fuel**, the particle emission toggles on and off at **0.06-second intervals**, creating a visible stutter in the exhaust trail. This is achieved by toggling the emission module's enabled state on a timer, not by modifying emission rate (which would produce a gradual fade rather than a sharp on/off sputter).

### Audio Feedback — JetpackAudioFeedback.cs

An AudioSource on the player character provides the audio channel, using `PlayOneShot` for burst-style SFX:

1. **Base SFX**: `SE_14_2E.wav` — a short burst clip played repeatedly at intervals via `PlayOneShot`. This produces a rhythmic, mechanical sound rather than a continuous loop.
2. **Interval mapping** (fuel-to-rhythm):
   - 100% fuel: **0.08s interval** — rapid-fire bursts, a dense mechanical hum with a Cave Story feel
   - 0% fuel: **0.3s interval** — slow, labored bursts
   - Interval scales **linearly** between these bounds as fuel drains
3. **Pitch mapping** (fuel-to-tone):
   - 100% fuel: **pitch 1.0** — normal tone
   - 0% fuel: **pitch 0.55** — low, strained tone
   - Pitch scales **linearly** between these bounds as fuel drains
4. **Pitch jitter**: Below **30% fuel**, a random jitter of **+/-0.2** is added to the base pitch on each burst. This creates an unstable, "struggling engine" quality that communicates danger through sound texture alone.
5. **Empty cue**: When fuel reaches 0, a single **dry-fire click** SFX plays. No further burst SFX are emitted. This is the terminal audio signal that boost is over.

### Design Principle

> "If the player needs to look away from the character to understand their state, the feedback has failed."

All feedback is anchored to the player character's world-space position. There is no HUD element, no screen-space overlay, and no UI widget. The player's peripheral vision and ears do all the work while their focused attention stays on the platforming.

### Legacy Note

GasMeterUI.cs exists in the codebase as a traditional HUD fuel bar. It is intentionally not wired up to any fuel data source. It may be re-enabled as an accessibility option in the future for players who prefer explicit numerical feedback.

### Architecture Diagram

```
[Fuel System]
   |
   | fuelNormalized (0.0 — 1.0)
   |
   +---------------------------+
   |                           |
   v                           v
[JetpackParticles.cs]    [JetpackAudioFeedback.cs]
   |                           |
   |  Color gradient:          |  Burst SFX (SE_14_2E.wav):
   |  cyan -> orange -> red    |  interval: 0.08s -> 0.3s
   |                           |  pitch:    1.0   -> 0.55
   |  Sputter (< 20%):        |
   |  emission on/off @ 0.06s  |  Jitter (< 30%):
   |                           |  pitch +/- 0.2
   |                           |
   |                           |  Empty (== 0%):
   |                           |  dry-fire click
   |                           |
   v                           v
[ParticleSystem]         [AudioSource.PlayOneShot]
   (child of player)       (on player GameObject)


[GasMeterUI.cs] — exists but NOT wired up
   (may return as accessibility option)
```

### Key Interfaces

```csharp
// JetpackParticles.cs
public class JetpackParticles : MonoBehaviour
{
    [SerializeField] private ParticleSystem jetExhaust;
    [SerializeField] private Gradient fuelColorGradient; // cyan -> orange -> red

    private float sputterTimer;
    private bool sputterOn = true;
    private const float SputterInterval = 0.06f;
    private const float SputterThreshold = 0.2f;

    public void UpdateFuelVisuals(float fuelNormalized)
    {
        // Color gradient
        var main = jetExhaust.main;
        main.startColor = fuelColorGradient.Evaluate(1f - fuelNormalized);

        // Sputter below threshold
        var emission = jetExhaust.emission;
        if (fuelNormalized < SputterThreshold && fuelNormalized > 0f)
        {
            sputterTimer += Time.deltaTime;
            if (sputterTimer >= SputterInterval)
            {
                sputterOn = !sputterOn;
                sputterTimer = 0f;
            }
            emission.enabled = sputterOn;
        }
        else
        {
            emission.enabled = fuelNormalized > 0f;
            sputterTimer = 0f;
            sputterOn = true;
        }
    }
}

// JetpackAudioFeedback.cs
public class JetpackAudioFeedback : MonoBehaviour
{
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip burstClip;    // SE_14_2E.wav
    [SerializeField] private AudioClip dryFireClip;

    private float burstTimer;
    private bool dryFirePlayed;

    private const float IntervalFull = 0.08f;
    private const float IntervalEmpty = 0.3f;
    private const float PitchFull = 1.0f;
    private const float PitchEmpty = 0.55f;
    private const float JitterThreshold = 0.3f;
    private const float JitterRange = 0.2f;

    public void UpdateFuelAudio(float fuelNormalized)
    {
        if (fuelNormalized <= 0f)
        {
            if (!dryFirePlayed)
            {
                audioSource.PlayOneShot(dryFireClip);
                dryFirePlayed = true;
            }
            return;
        }

        dryFirePlayed = false;

        float interval = Mathf.Lerp(IntervalEmpty, IntervalFull, fuelNormalized);
        float pitch = Mathf.Lerp(PitchEmpty, PitchFull, fuelNormalized);

        if (fuelNormalized < JitterThreshold)
        {
            pitch += Random.Range(-JitterRange, JitterRange);
        }

        burstTimer += Time.deltaTime;
        if (burstTimer >= interval)
        {
            audioSource.pitch = pitch;
            audioSource.PlayOneShot(burstClip);
            burstTimer = 0f;
        }
    }
}
```

## Alternatives Considered

### Alternative 1: Traditional HUD Bar
- **Description**: A standard fuel gauge bar positioned at a screen edge, filling/draining as fuel is consumed and recharged, with a clear numeric readout
- **Pros**: Universally understood; precise numerical communication; easy to implement (GasMeterUI.cs already exists)
- **Cons**: Forces player to look away from their character during the most critical gameplay moments (mid-air boost decisions); competes for screen real estate; breaks the diegetic design philosophy; player eyes leave the action
- **Rejection Reason**: Violates the core design principle. The moments when fuel information is most critical (mid-boost, low fuel) are exactly the moments when looking away from the character is most dangerous.

### Alternative 2: Screen-Edge Vignette / Color Shift
- **Description**: As fuel drains, apply a post-processing vignette or color grade shift to the screen edges (e.g., edges darken or tint red at low fuel)
- **Pros**: Atmospheric; visible in peripheral vision without looking away from center screen; does not add discrete UI elements
- **Cons**: Imprecise — communicates "low" vs "not low" but not intermediate states; can be confused with damage vignettes or other screen effects; post-processing cost on low-end hardware; color shifts may be invisible to colorblind players
- **Rejection Reason**: Too imprecise for a system where fuel management is a core skill. Players need to read approximate fuel level continuously, not just receive a binary low-fuel warning. Also overlaps with potential damage/health feedback.

### Alternative 3: Controller Rumble Intensity
- **Description**: Map fuel level to haptic feedback intensity — stronger rumble as fuel depletes, no rumble on empty
- **Pros**: Intuitive physical feedback; does not use any visual screen space; immediate and visceral
- **Cons**: Not accessible to keyboard-only players; not all controllers support fine-grained rumble; cannot be the primary feedback channel for a PC game; no visual component for deaf/hard-of-hearing players
- **Rejection Reason**: Excludes keyboard players entirely, which is the primary input method for a PC platformer. Could be added as a supplementary channel alongside the diegetic systems in the future, but cannot replace them.

### Alternative 4: Character Sprite Changes
- **Description**: Swap or tint the player character's sprite based on fuel state (e.g., character flashes red at low fuel, or jetpack sprite visibly dims)
- **Pros**: Directly attached to the character; no additional particles or audio needed; simple implementation
- **Cons**: Player character is 16x16 pixels — subtle sprite changes are nearly invisible during fast movement; tinting the entire sprite conflicts with damage flash feedback; limited expressiveness at low resolution
- **Rejection Reason**: The 16x16 pixel constraint makes sprite-based feedback too subtle to be reliable. Particles extend beyond the sprite bounds and are visible at any movement speed. Audio is resolution-independent.

## Consequences

### Positive
- Player never needs to look away from their character to read fuel state — attention stays on the action
- Two parallel channels (visual + audio) reinforce each other — players who mute sound still get particles, hearing-impaired players still get color/sputter
- The feedback creates an escalating tension curve as fuel drains: calm cyan hum at full, frantic red sputter with pitch jitter at low, silence at empty
- Sputter and pitch jitter create a visceral "struggling machine" feel that matches the Cave Story mechanical aesthetic
- GasMeterUI.cs is preserved as a dormant accessibility option that can be toggled on without architectural changes

### Negative
- Less precise than a numeric HUD bar — players cannot read exact fuel percentage, only approximate range
- Two new MonoBehaviours (JetpackParticles, JetpackAudioFeedback) add to the player GameObject's component count
- Particle color gradient requires artist tuning to ensure readability across all background colors in all levels
- Rapid PlayOneShot calls at 0.08s intervals may stack audio clips if the burst clip is longer than 0.08s — clip length must be kept under the minimum interval

### Risks
- **Risk**: Particle color gradient may be unreadable for colorblind players (cyan/red are distinguishable for most, but orange intermediate may blend)
  - **Mitigation**: Pair color with the sputter behavior (below 20%) and audio pitch shift as redundant channels; consider adding a high-contrast accessibility gradient option; GasMeterUI.cs can be activated as a fallback
- **Risk**: PlayOneShot at 0.08s intervals could cause audio clipping or performance issues if burst clip overlaps with itself
  - **Mitigation**: Ensure SE_14_2E.wav is shorter than 0.08s; if not, trim it or use AudioSource.Play with restart semantics instead of PlayOneShot
- **Risk**: Sputter toggle at 0.06s intervals may cause visible flickering that triggers photosensitivity in some players
  - **Mitigation**: Test sputter frequency against photosensitivity guidelines (WCAG recommends no flashing above 3Hz; 0.06s = ~8.3Hz toggles, but emission on/off is particle appearance, not full-screen flash); consider raising the interval if flagged; provide accessibility option to disable sputter and use steady red instead
- **Risk**: Random pitch jitter below 30% may sound chaotic rather than tense if the jitter range is too wide
  - **Mitigation**: JitterRange (0.2) is a tunable constant; playtest and narrow if needed

## GDD Requirements Addressed

| GDD System | Requirement | How This ADR Addresses It |
|------------|-------------|--------------------------|
| fuel-feedback.md | Fuel state must be readable without HUD | Diegetic particle and audio feedback anchored to player character |
| fuel-feedback.md | Visual fuel indicator via particle color | Cyan (full) -> orange (50%) -> red (empty) gradient on jetpack exhaust |
| fuel-feedback.md | Low-fuel visual urgency | Emission sputter on/off at 0.06s intervals below 20% fuel |
| fuel-feedback.md | Audio fuel indicator via SFX rhythm | Burst SFX (SE_14_2E.wav) interval widens from 0.08s to 0.3s as fuel drains |
| fuel-feedback.md | Audio fuel indicator via pitch | Pitch drops from 1.0 to 0.55 as fuel drains |
| fuel-feedback.md | Low-fuel audio urgency | Pitch jitter +/-0.2 below 30% fuel ("struggling engine") |
| fuel-feedback.md | Empty-fuel terminal cue | Single dry-fire click SFX on fuel depletion |

## Performance Implications
- **CPU**: Low — JetpackParticles updates one gradient evaluation and one emission toggle per frame during boost; JetpackAudioFeedback runs a timer comparison and occasional PlayOneShot call per frame during boost. Neither system runs when boost is inactive.
- **Memory**: Negligible — one Gradient asset, two AudioClip references (SE_14_2E.wav, dry-fire click), a handful of float/bool fields per component
- **Load Time**: No impact — assets are serialized on the player prefab
- **Network**: N/A — single-player game
- **Audio Channels**: PlayOneShot at 0.08s minimum interval with sub-0.08s clips means at most 1 burst clip playing at any time. Dry-fire click is a single one-shot. Total channel usage: 1-2 during boost, 0 otherwise.

## Migration Plan
If a previous fuel feedback implementation exists (e.g., HUD bar via GasMeterUI.cs), disable it by removing the fuel-update binding but preserve the script for future accessibility use. Add JetpackParticles.cs and JetpackAudioFeedback.cs as new components on the player GameObject. Wire both components to receive fuelNormalized from the fuel system each frame during active boost. No existing systems need modification — this ADR is additive.

## Validation Criteria
- At 100% fuel: particles are cyan, burst interval measured at approximately 0.08s, pitch measured at 1.0
- At 50% fuel: particles are orange, burst interval measured at approximately 0.19s, pitch measured at approximately 0.775
- At 20% fuel: particles are red-orange, sputter on/off behavior is visually confirmed, burst interval measured at approximately 0.256s
- Below 20% fuel: emission visibly toggles on and off; toggling is not present above 20%
- Below 30% fuel: pitch values vary between bursts by up to +/-0.2 (jitter confirmed); jitter is not present above 30%
- At 0% fuel: particles stop, dry-fire click plays exactly once, no further burst SFX
- Fuel refill from 0% to 100%: particles and audio smoothly return to full-fuel state; dry-fire click does not replay
- GasMeterUI.cs is present in the project but has no active bindings to the fuel system
- Playtest confirmation: 3 out of 3 testers can describe their approximate fuel level without being told about the feedback system

## Related Decisions
- ADR-0004: Booster activation pattern — boost activation begins fuel consumption, which drives this ADR's feedback
- ADR-0005: Post-boost velocity halving — fuel-empty event triggers boost deactivation via ADR-0005
- GDD: `design/gdd/fuel-feedback.md`
