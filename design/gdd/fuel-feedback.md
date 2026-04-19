# Fuel Feedback System

> **Status**: Implemented
> **Layer**: Presentation
> **Priority**: MVP
> **Created**: 2026-04-19
> **Last Updated**: 2026-04-20
> **Source Files**: `JetpackParticles.cs`, `JetpackAudioFeedback.cs`, `JetpackGas.cs`, `FuelGate.cs`

> **IMPORTANT UPDATE (2026-04-20)**: The fuel feedback system now serves a dual purpose. It communicates fuel state to the PLAYER (diegetic particles + audio), AND the same color language is used by FUEL GATES in the environment. Three fuel tiers (High=cyan 50-100%, Mid=orange 20-50%, Low=red 0-20%) are defined as a `FuelTier` enum in JetpackGas.cs. Gates color-match to the exhaust gradient — when the player's exhaust matches the gate's color, the gate opens. See `design/gdd/design-direction.md` for full design context.

---

## 1. Overview

The Fuel Feedback system communicates the player's jetpack fuel state entirely through diegetic cues -- exhaust particle color, emission sputtering, burst SFX pacing, and pitch distortion -- rather than a traditional HUD bar. The player reads fuel from the jetpack itself: cyan exhaust means full, red means empty, rapid bursts mean plenty of fuel, widening silence between bursts means running out. This keeps the player's eyes on the character and the action at all times.

---

## 2. Player Fantasy

The player should feel like they are piloting a scrappy, mechanical jetpack that talks back to them. At full fuel, the exhaust roars with a bright cyan stream and rapid-fire bursts -- confidence, power, momentum. As fuel drains, the exhaust shifts to orange and then angry red, the engine coughs with widening gaps between bursts, the pitch drops, and the whole thing starts to sound like it is fighting to stay alive. The player never needs to glance at a corner of the screen. The jetpack on their back IS the fuel gauge. When the last sputter hits and the dry-fire click plays, the player knows it is over -- not because a bar hit zero, but because their machine just died in their ears and under their sprite.

---

## 3. Detailed Rules

### 3.1 Gas Resource (JetpackGas.cs)

The fuel resource that drives all feedback systems.

- **Max gas**: 100 units.
- **Drain rate**: 100 units/sec (consumed by `PlayerJetpack.Tick()` via `ConsumeGas()`).
- **Effective duration**: ~1 second of continuous boost.
- **Recharge**: Instant, full refill on landing (ground contact triggers `Recharge()`).
- **Mid-air recharge**: `RechargeFromPickup()` available for fuel pickups (planned Celeste-style dash crystals).
- **Events fired**:
  - `OnGasChanged(float percent)` -- every frame fuel changes, passes 0.0-1.0.
  - `OnGasEmpty()` -- fires once when fuel hits zero (previous frame had fuel > 0).
  - `OnGasRecharged()` -- fires once on recharge (only if fuel was below max).

### 3.2 Exhaust Color Shift (JetpackParticles.cs)

Visual fuel feedback via particle system color.

- **Two-stage gradient** mapped to `GasPercent` (0.0 to 1.0):
  - **Stage 1 (100% to 50%)**: Lerp from **cyan** `(0.4, 0.9, 1.0)` to **orange** `(1.0, 0.6, 0.1)`.
  - **Stage 2 (50% to 0%)**: Lerp from **orange** `(1.0, 0.6, 0.1)` to **red** `(1.0, 0.15, 0.1)`.
- Color is applied to `ParticleSystem.MainModule.startColor` every frame while jetpacking.
- Particles play when `player.IsJetpacking` is true, stop immediately when false.

### 3.3 Emission Sputter (JetpackParticles.cs)

Urgency signal at critically low fuel.

- **Threshold**: Below 20% fuel (`sputterThreshold = 0.2`).
- **Behavior**: Emission rate toggles between `baseEmissionRate` and `0` on a fixed timer.
- **Interval**: 0.06 seconds per toggle (`sputterInterval`). This produces a rapid flicker visible against the red exhaust color.
- **Reset**: Sputter state resets (`sputterOn = true`, `sputterTimer = 0`) when jetpack deactivates or fuel rises above threshold.

### 3.4 Audio Burst Feedback (JetpackAudioFeedback.cs)

Auditory fuel feedback via repeated burst SFX.

- **Clip**: `SE_14_2E.wav` played via `AudioSource.PlayOneShot()` (non-interrupting, allows overlaps at high fire rates).
- **Repeat interval** scales linearly with fuel:
  - **Full fuel (100%)**: 0.08s interval -- bursts fire near-consecutively, matching Cave Story's rapid-fire feel.
  - **Empty fuel (0%)**: 0.3s interval -- audible dead time between each burst.
  - Formula: `interval = Lerp(emptyFuelInterval, fullFuelInterval, gasPercent)`.
- **On activation**: First burst fires immediately (`burstTimer` set to 999 to guarantee instant trigger).

### 3.5 Pitch Distortion (JetpackAudioFeedback.cs)

Pitch communicates fuel level alongside interval.

- **Base pitch** scales linearly with fuel:
  - Full: 1.0 (normal).
  - Empty: 0.55 (noticeably lower, sluggish).
  - Formula: `pitch = Lerp(emptyPitch, fullPitch, gasPercent)`.
- **Pitch jitter** below 30% fuel (`pitchJitterAt = 0.3`):
  - Random offset in range `[-maxPitchJitter, +maxPitchJitter]` (default +/-0.2).
  - Jitter strength scales from 0 at 30% to full at 0%: `jitterStrength = 1 - (percent / pitchJitterAt)`.
  - Creates an unstable, "struggling engine" character.

### 3.6 Dry-Fire Click (JetpackAudioFeedback.cs)

Terminal feedback when fuel is fully depleted.

- Subscribes to `JetpackGas.OnGasEmpty` event.
- Plays `emptyClickClip` once via a separate `clickSource` AudioSource.
- Guarded by `playedEmptyClick` flag -- plays exactly once per jetpack activation.
- Flag resets on next jetpack activation (not on landing), preventing double-clicks.

### 3.7 Legacy HUD Bar (GasMeterUI.cs)

- File exists at `Assets/Scripts/UI/GasMeterUI.cs`.
- **Intentionally not wired up** -- no reference to `JetpackGas`, no event subscription.
- Kept in the codebase for potential future use as an accessibility option.
- Design direction: avoid persistent HUD elements for moment-to-moment mechanics.

---

## 4. Formulas

### Gas Drain
```
currentGas = max(0, currentGas - consumptionRate * deltaTime)
gasPercent = currentGas / maxGas
```
Where `consumptionRate = 100`, `maxGas = 100`. Effective boost duration = `maxGas / consumptionRate` = 1.0 second.

### Exhaust Color
```
if gasPercent > 0.5:
    t = (gasPercent - 0.5) / 0.5        // 1.0 at full, 0.0 at mid
    color = Lerp(midColor, fullColor, t)
else:
    t = gasPercent / 0.5                 // 1.0 at mid, 0.0 at empty
    color = Lerp(emptyColor, midColor, t)
```

### Burst Interval
```
interval = Lerp(emptyFuelInterval, fullFuelInterval, gasPercent)
         = Lerp(0.3, 0.08, gasPercent)
```
Note: Arguments are (empty, full, t) because higher gasPercent should produce the shorter (full) interval.

### Pitch
```
basePitch = Lerp(emptyPitch, fullPitch, gasPercent)
          = Lerp(0.55, 1.0, gasPercent)

if gasPercent < pitchJitterAt:
    jitterStrength = 1.0 - (gasPercent / pitchJitterAt)
    pitch = basePitch + Random(-maxPitchJitter, maxPitchJitter) * jitterStrength
else:
    pitch = basePitch
```

### Sputter Toggle
```
sputterTimer += deltaTime
if sputterTimer >= sputterInterval (0.06s):
    sputterTimer = 0
    sputterOn = !sputterOn

emissionRate = sputterOn ? baseRate : 0
```

---

## 5. Edge Cases

| Situation | Expected Behavior |
|---|---|
| Jetpack activated with 0 fuel | Jetpack should not activate (handled by `PlayerJetpack`). No particles, no bursts. Dry-fire click does not play (it only fires on the `OnGasEmpty` event, which requires transitioning from >0 to 0). |
| Fuel hits 0 mid-flight | `OnGasEmpty` fires, dry-fire click plays once. Particles stop when `IsJetpacking` becomes false (jetpack terminates on empty). Color would be deep red at the moment of cutoff. |
| Very short jetpack tap (<0.08s) | First burst fires immediately on activation. Particles flash briefly. If fuel did not drain below 20%, no sputter occurs. Color barely shifts from cyan. |
| Rapid jetpack on/off toggling | Each activation resets `burstTimer` to 999 (instant first burst) and resets `playedEmptyClick`. Sputter state resets on deactivation. No stale state carries over. |
| Mid-air fuel pickup recharges fuel | `OnGasRecharged` fires, `GasPercent` returns to 1.0. Color snaps back to cyan. Burst interval tightens to 0.08s. Pitch returns to 1.0. Sputter stops. |
| AudioSource or clip not assigned | Null checks guard all audio playback. System degrades silently -- particles still work, audio simply does not play. |
| ParticleSystem not assigned | `JetpackParticles.Update()` returns early. No visual feedback, but audio feedback still functions independently. |
| Fuel recharges on landing while sputter is active | Sputter resets because `IsJetpacking` becomes false on landing (particles stop). Next activation starts clean. |

---

## 6. Dependencies

| Dependency | Type | Direction | Notes |
|---|---|---|---|
| `JetpackGas.cs` | Data source | Reads `GasPercent`, `HasGas`; subscribes to `OnGasEmpty` | The fuel resource that drives all feedback |
| `PlayerController.cs` | State source | Reads `IsJetpacking` | Determines when feedback is active |
| `PlayerJetpack.cs` | Indirect | Calls `JetpackGas.ConsumeGas()` | Feedback reacts to fuel changes caused by jetpack |
| Unity ParticleSystem | Engine feature | Controls via `MainModule`, `EmissionModule` | Required for visual feedback |
| Unity AudioSource | Engine feature | `PlayOneShot()` for burst and click SFX | Two separate sources (engine + click) |
| `SE_14_2E.wav` | Asset | Assigned in Inspector | Burst SFX clip |
| Empty click clip | Asset | Assigned in Inspector | Dry-fire SFX clip (not yet specified) |

### What does NOT depend on this system

- `PlayerJetpack.cs` does not read feedback state. Fuel consumption is entirely independent of presentation.
- `JetpackGas.cs` has no knowledge of its listeners. It fires events blindly.
- No gameplay system depends on feedback -- this is a pure presentation layer.

---

## 7. Tuning Knobs

All values are serialized fields, adjustable in the Unity Inspector at edit time and runtime.

| Knob | Current Value | Component | What It Controls |
|---|---|---|---|
| `maxGas` | 100 | JetpackGas | Total fuel capacity |
| `fullColor` | (0.4, 0.9, 1.0) cyan | JetpackParticles | Exhaust color at 100% fuel |
| `midColor` | (1.0, 0.6, 0.1) orange | JetpackParticles | Exhaust color at 50% fuel |
| `emptyColor` | (1.0, 0.15, 0.1) red | JetpackParticles | Exhaust color at 0% fuel |
| `sputterThreshold` | 0.2 (20%) | JetpackParticles | Fuel % below which sputter begins |
| `sputterInterval` | 0.06s | JetpackParticles | Toggle rate for emission flicker |
| `fullFuelInterval` | 0.08s | JetpackAudioFeedback | Burst repeat rate at full fuel |
| `emptyFuelInterval` | 0.3s | JetpackAudioFeedback | Burst repeat rate at empty fuel |
| `fullPitch` | 1.0 | JetpackAudioFeedback | Pitch at full fuel |
| `emptyPitch` | 0.55 | JetpackAudioFeedback | Pitch at empty fuel |
| `pitchJitterAt` | 0.3 (30%) | JetpackAudioFeedback | Fuel % below which jitter starts |
| `maxPitchJitter` | 0.2 | JetpackAudioFeedback | Maximum random pitch offset (+/-) |
| `volume` | 0.6 | JetpackAudioFeedback | Burst SFX volume |

### Future tuning considerations

- **Gas consumption rate** lives on `PlayerJetpack.cs` (currently 100/sec), not on the feedback system. Changing it affects effective boost duration and therefore how quickly the feedback ramps.
- **Per-chapter fuel limits** (via `ChapterConfig`, planned) would change `maxGas`, automatically scaling all feedback since it is driven by `GasPercent`.
- The two-stage gradient breakpoint is hardcoded at 50%. If tuning reveals a different split feels better (e.g., 60/40), the lerp logic in `UpdateExhaustColor()` would need adjustment.

---

## 8. Acceptance Criteria

### Visual Feedback

- [ ] Exhaust particles are **cyan** when jetpack activates at full fuel.
- [ ] Exhaust particles are **orange** at approximately 50% fuel.
- [ ] Exhaust particles are **red** at approximately 0% fuel.
- [ ] Color transition is smooth and continuous (no popping or stepping).
- [ ] Below 20% fuel, particles visibly **sputter** (rapid flicker on/off).
- [ ] Sputter is fast enough to read as "warning" but not so fast it looks like a solid dimmed stream.
- [ ] Particles stop immediately when jetpack deactivates.

### Audio Feedback

- [ ] On jetpack activation, a burst SFX plays **immediately** (no delay).
- [ ] At full fuel, bursts are **near-consecutive** -- sounds like a continuous rapid-fire engine.
- [ ] At low fuel, bursts have **audible gaps** -- sounds like the engine is struggling.
- [ ] Pitch is noticeably **higher at full fuel** and **lower at low fuel**.
- [ ] Below 30% fuel, pitch has **random variation** per burst -- unstable, sputtering character.
- [ ] When fuel hits zero, a **dry-fire click** plays exactly once.
- [ ] No audio artifacts (clipping, pops) from rapid PlayOneShot calls at high fire rates.

### Diegetic Principle

- [ ] No HUD bar or UI element is visible during normal jetpack use.
- [ ] A player who has never seen the game can distinguish "lots of fuel" from "almost out" within 2-3 jetpack uses, using only visual and audio cues.
- [ ] All fuel information is perceivable while looking at the player character -- no need to glance at screen edges.

### Integration

- [ ] Feedback systems function independently -- disabling `JetpackParticles` does not break `JetpackAudioFeedback`, and vice versa.
- [ ] Null-safe: missing AudioSource, AudioClip, or ParticleSystem references produce no errors, only silent degradation.
- [ ] Fuel recharge (landing or pickup) resets all feedback to full-fuel state on next activation.

---

## Design Philosophy

> "If the player needs to look away from the character to understand their state, the feedback has failed."

This system exists because Project Jetpack's core loop is fast, tight, and directional. The player is making split-second decisions about when to boost, which direction to go, and whether they have enough fuel to reach the next platform. A HUD bar in the corner would pull their attention away from the one thing that matters: the character and the space around it.

The two feedback channels (visual + audio) are redundant by design. A player in a noisy room still gets color information. A player whose eyes are on the next platform still gets audio information. Together, they create an intuitive sense of fuel state that becomes second nature after a few rooms.

`GasMeterUI.cs` remains in the codebase as a deliberate design artifact. If accessibility testing reveals that some players need a traditional gauge, it can be wired up behind an options toggle without rearchitecting anything -- `JetpackGas` already fires the events it would need.
