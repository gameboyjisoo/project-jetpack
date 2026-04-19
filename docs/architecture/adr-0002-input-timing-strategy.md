# ADR-0002: Input Timing Strategy

> **Status**: Accepted
> **Date**: 2026-04-14
> **Knowledge Risk**: HIGH — Input System behavior varies between Unity versions
> **Engine**: Unity 6 (6000.0.34f1), New Input System package

---

## Context

Project Jetpack is a fast-paced 2D platformer where precise, frame-accurate input is non-negotiable. The player's core loop — running, jumping, firing the jetpack, and using the secondary booster — all depend on inputs being detected reliably every single frame, with no dropped presses.

Unity's New Input System offers several ways to detect button presses, but their behavior changes depending on the **Input System update mode** setting (Process Events in Fixed Update, Process Events in Dynamic Update, or Process Events Manually). The convenience method `WasPressedThisFrame()` is documented to return true only during the frame a button transitions from released to pressed, but in practice it can miss inputs entirely when the update mode does not align with the code's execution context (e.g., calling it in `Update()` while events process in `FixedUpdate()`, or vice versa).

The project also needs a lightweight input wiring strategy. The default Unity approach of attaching a `PlayerInput` component to the player GameObject adds overhead and coupling that is unnecessary for a single-player game with a small, fixed set of actions.

---

## Decision

**Read all input in `Update()`. Apply all physics in `FixedUpdate()`. Use manual edge detection for button presses.**

Specifically:

1. **Input actions are loaded from `Assets/Resources/PlayerInput.inputactions`** via `Resources.Load<InputActionAsset>()` at runtime. No `PlayerInput` component is placed on any GameObject.

2. **Every script that needs input** loads the `InputActionAsset` from Resources and calls `Enable()` in `OnEnable()` and `Disable()` in `OnDisable()` on the relevant action maps.

3. **Button press detection uses manual edge detection**, not `WasPressedThisFrame()`:
   - Each script stores a `bool wasPressed` field representing the button's state on the previous frame.
   - In `Update()`, the script reads `action.IsPressed()` and compares it to `wasPressed`.
   - A rising edge (`!wasPressed && action.IsPressed()`) means the button was just pressed this frame.
   - `wasPressed` is updated to the current state at the end of the `Update()` check.

4. **Physics application** (velocity changes, forces, raycasts) happens in `FixedUpdate()`, consuming flags or values that `Update()` set.

---

## Rationale

- **`WasPressedThisFrame()` is unreliable.** Depending on the Input System update mode, it can silently drop presses. This is a known issue that has persisted across multiple Unity versions. For a platformer where a single missed jump input means death, this is unacceptable.

- **Manual edge detection is deterministic.** By comparing the current `IsPressed()` state against the previous frame's state, we get a reliable rising-edge signal regardless of Input System update mode. The cost is one extra `bool` per action per script — trivial.

- **`Resources.Load` keeps input wiring simple and decoupled.** Any script can independently load the same `InputActionAsset` without needing a reference to a shared component or singleton. This aligns with the component architecture (ADR-0001) where each behavior script is self-contained.

- **No `PlayerInput` component reduces overhead.** The `PlayerInput` component adds event routing, player management, and control scheme switching machinery that Project Jetpack does not need. Loading actions directly is lighter and gives full control over when actions are enabled.

- **`Update()` for input, `FixedUpdate()` for physics** is the standard Unity pattern. Input polling in `FixedUpdate()` can miss presses that occur between fixed timesteps. Input polling in `Update()` captures every frame.

---

## GDD Requirements

This decision is a foundation for all player-facing input systems. The following GDD documents depend on reliable, frame-accurate input detection:

| GDD Document | Dependency |
|---|---|
| `design/gdd/player-movement.md` | Horizontal input must be read every frame for responsive ground/air control |
| `design/gdd/player-jump.md` | Jump press detection must never drop inputs; coyote time and jump buffering rely on precise frame timing |
| `design/gdd/jetpack-system.md` | Jetpack activation (hold) and direction input must be read every frame; fuel burn rate depends on continuous input |
| `design/gdd/secondary-booster.md` | Booster is a single-press action with a cooldown; a missed press during a tight platforming sequence is a death sentence |

---

## Alternatives Considered

### 1. Use `WasPressedThisFrame()` directly

- **Pros**: Simpler code, one fewer `bool` per action, no manual state tracking.
- **Cons**: Unreliable depending on Input System update mode. Can silently drop button presses. Behavior may change between Unity versions.
- **Verdict**: Rejected. The simplicity is not worth the risk of dropped inputs in a precision platformer.

### 2. Use `PlayerInput` component on the player GameObject

- **Pros**: Unity's recommended approach. Handles action map switching, control scheme detection, and multiplayer routing automatically.
- **Cons**: Adds unnecessary overhead for a single-player game. Requires the component to be on a specific GameObject, creating coupling. Event-based callbacks (`SendMessages`, `UnityEvents`, `C# Events`) add indirection that makes the input-to-physics pipeline harder to reason about.
- **Verdict**: Rejected. The component solves problems Project Jetpack does not have, at the cost of added complexity and reduced control.

### 3. Use the legacy Input Manager (`Input.GetKeyDown`, etc.)

- **Pros**: Battle-tested, simple API, frame-accurate by design.
- **Cons**: Deprecated in Unity 6. Does not support rebinding, gamepads beyond XInput, or modern input features without additional code. Could be removed in a future Unity version.
- **Verdict**: Rejected. Using a deprecated system in Unity 6 is a forward-compatibility risk.

---

## Consequences

### Positive

- **Zero dropped inputs.** Manual edge detection is immune to Input System update mode quirks.
- **Decoupled input wiring.** Any script can independently load and use input actions without depending on a shared component.
- **Clear separation of concerns.** Input reading (`Update()`) and physics application (`FixedUpdate()`) are cleanly separated.
- **Portable pattern.** The manual edge detection pattern works identically regardless of future Input System update mode changes.

### Negative

- **Boilerplate per script.** Each script that detects button presses needs a `wasPressed` bool and the comparison logic. This is a small amount of repeated code.
- **Developer discipline required.** New contributors must remember to use the manual pattern, not `WasPressedThisFrame()`. Mitigated by code review and this ADR.
- **Knowledge risk is HIGH.** Unity's Input System behavior may change in future versions. If a future Unity update fixes `WasPressedThisFrame()` reliability, this ADR should be revisited — but the manual approach will still work correctly.

### Neutral

- **`Resources.Load` has a minor first-load cost.** The `InputActionAsset` is small; this is negligible.

---

## Dependencies

- **Depends On**: None. This is a foundational decision.
- **Enables**: ADR-0001 (component architecture needs reliable input) — the per-component input loading pattern described here is what makes self-contained behavior scripts possible.

---

## Implementation Notes

```csharp
// Example: Manual edge detection pattern
private InputAction jumpAction;
private bool wasJumpPressed;

private void OnEnable()
{
    var asset = Resources.Load<InputActionAsset>("PlayerInput");
    jumpAction = asset.FindAction("Player/Jump");
    jumpAction.Enable();
}

private void OnDisable()
{
    jumpAction.Disable();
}

private void Update()
{
    bool isPressed = jumpAction.IsPressed();
    bool justPressed = isPressed && !wasJumpPressed;
    wasJumpPressed = isPressed;

    if (justPressed)
    {
        // Set flag for FixedUpdate to consume
        jumpRequested = true;
    }
}

private void FixedUpdate()
{
    if (jumpRequested)
    {
        // Apply jump velocity
        jumpRequested = false;
    }
}
```

---

## Review Notes

- If Unity fixes `WasPressedThisFrame()` reliability in a future Input System update, re-evaluate this decision. The manual pattern will still work, but the simpler API would reduce boilerplate.
- If the project ever needs multiplayer or complex control scheme switching, revisit the decision to avoid `PlayerInput` component.
