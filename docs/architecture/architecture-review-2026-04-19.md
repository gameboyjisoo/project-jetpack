# Architecture Review Report

**Date**: 2026-04-19
**Reviewer**: architecture-review (automated)
**ADRs Reviewed**: 7
**Engine**: Unity 6 (6000.0.34f1)

## Consistency Check

**Status: PASS with minor finding**

All 7 ADRs reference the same engine version: **Unity 6 (6000.0.34f1)**. All have status **Accepted** and share the same creation date (2026-04-14). No contradictions were found between ADR decisions -- each ADR's rules are compatible with all others.

- ADR-0003 defines gravity suppression during propelled states; ADR-0004 and ADR-0005 correctly depend on and reference this axiom.
- ADR-0004 sets `boostMode` on activation; ADR-0005 correctly reads `boostMode` on termination.
- ADR-0001 defines the 9-step pipeline; ADR-0003 (gravity), ADR-0004 (jetpack), and ADR-0002 (input) are consistent with the pipeline ordering.
- All ADRs that reference velocity use `rb.linearVelocity` (the Unity 6 API), never the deprecated `rb.velocity`.

**No contradictions detected between ADRs.**

## Dependency Integrity

**Status: CONCERN -- dependency chain described inconsistently across documents**

The ADR dependency chain is acyclic and all referenced ADRs exist. However, three documents describe the chain differently:

| Source | Described Chain |
|--------|----------------|
| **architecture.md** (master doc) | ADR-0007 -> ADR-0001 -> ADR-0002; ADR-0001 -> ADR-0003 -> ADR-0004 -> ADR-0005; ADR-0006 independent |
| **architecture-traceability.md** | ADR-0007 -> ADR-0002 -> ADR-0003 -> ADR-0004 -> ADR-0005; ADR-0006 independent; ADR-0001 "depends on all others" |
| **ADRs themselves** | ADR-0001: no deps; ADR-0002: no deps (says it "enables" ADR-0001); ADR-0003: no deps; ADR-0007: no deps (enables ADR-0001, 0003, 0004, 0005) |

Specific discrepancies:

1. **ADR-0001** declares "None" for dependencies in its own text, but the master architecture doc shows it depending on ADR-0007. ADR-0007 lists ADR-0001 as something it "enables," which is the reverse direction. These are compatible but the master doc's arrow (ADR-0007 -> ADR-0001) implies ADR-0001 depends on ADR-0007, which ADR-0001 does not acknowledge.

2. **ADR-0002** declares "None" for dependencies and says it "enables ADR-0001." The master doc shows the reverse: ADR-0001 -> ADR-0002. The traceability doc shows ADR-0007 -> ADR-0002. All three documents disagree on ADR-0002's position in the chain.

3. **ADR-0003** declares "None" for dependencies, but ADR-0007 claims to enable it ("ground detection depends on Ground layer raycast"). The master doc shows ADR-0001 -> ADR-0003, but ADR-0003 makes no mention of ADR-0001.

**Recommendation**: Reconcile the dependency descriptions. The ADRs themselves should be the source of truth. Update the master architecture doc and traceability matrix to match the ADRs' self-declared dependency fields. Suggested canonical chain:

```
ADR-0007 (foundation, no deps)
ADR-0001 (no deps -- foundational architecture)
ADR-0002 (no deps -- foundational input)
ADR-0003 (no deps -- foundational axiom)
    -> ADR-0004 (depends on ADR-0003)
        -> ADR-0005 (depends on ADR-0003, ADR-0004)
ADR-0006 (independent)
```

**No cycles detected. All referenced ADR numbers (0001-0007) exist.**

## GDD Traceability

**Status: PASS**

Per the traceability matrix (`architecture-traceability.md`):

- **5 of 5 GDD requirements** are covered by ADRs (player-movement, player-jump, jetpack-system, secondary-booster, fuel-feedback).
- **0 GDD requirements** are partially covered or uncovered.
- **ADR-0007** has no direct GDD traceability, which is acknowledged and appropriate -- it is a foundational infrastructure decision that enables all physics-dependent GDDs.
- **16 individual requirements** (REQ-MOV-001 through REQ-FDB-003) are mapped to specific ADRs with full coverage.

The reverse index confirms every ADR traces back to at least one GDD (except ADR-0007, which is foundational).

**No traceability gaps in documented GDDs.**

## Engine Compatibility

**Status: CONCERN -- two deprecated API usages identified**

Checked all ADR-specified APIs against `docs/engine-reference/unity/deprecated-apis.md`:

### 1. `Resources.Load()` -- DEPRECATED

**ADR-0002** mandates `Resources.Load<InputActionAsset>("PlayerInput")` as the input wiring strategy. The deprecated APIs reference document lists `Resources.Load()` as deprecated in favor of **Addressables** (`Addressables.LoadAssetAsync()`).

- **Severity**: Low. `Resources.Load` still functions in Unity 6 and is unlikely to be removed soon. For a small single-player project with one InputActionAsset, the practical risk is minimal. However, this is technically a deprecated API usage.
- **Recommendation**: Acknowledge in ADR-0002 as a known deviation. If the project grows to need async loading or memory management, migrate to Addressables. For now, the simplicity benefit outweighs the deprecation concern.

### 2. Legacy Particle System -- DEPRECATED

**ADR-0006** uses Unity's `ParticleSystem` for jetpack exhaust feedback. The deprecated APIs document lists the Legacy Particle System as deprecated in favor of **Visual Effect Graph** (VFX Graph).

- **Severity**: Low. The `ParticleSystem` component (Shuriken, not the pre-Shuriken "Legacy Particle System") is still fully supported in Unity 6. The deprecated-apis document's entry likely refers to the pre-Shuriken legacy system, not the modern ParticleSystem/Shuriken system. VFX Graph is GPU-accelerated and overkill for a 2D pixel art game's simple exhaust particles.
- **Recommendation**: No action needed. Clarify in the deprecated-apis reference that modern ParticleSystem (Shuriken) is not deprecated -- only the pre-5.0 Legacy Particle component is.

### APIs correctly used (no issues):

- `Rigidbody2D.linearVelocity` -- correct Unity 6 API (project convention explicitly bans deprecated `velocity`)
- Unity New Input System (`InputActionAsset`, `IsPressed()`) -- correct, not using deprecated `Input.GetKey()` family
- `Physics2D.OverlapBox`, `Physics2D.Raycast` -- not deprecated
- `AudioSource.PlayOneShot` -- not deprecated

## Coverage Analysis

**Status: CONCERN -- 11 of 18 systems lack ADRs**

Per `design/gdd/systems-index.md`, the project has 18 identified systems. ADRs currently cover 7 systems (counting physics layers as a system):

| # | System | Has ADR? | Notes |
|---|--------|----------|-------|
| 1 | Player Movement | Yes (ADR-0001) | |
| 2 | Player Jump | Yes (ADR-0001) | |
| 3 | Jetpack System | Yes (ADR-0003, 0004, 0005) | |
| 4 | Secondary Booster | Yes (ADR-0001) | |
| 5 | Fuel System | Yes (ADR-0006) | |
| 6 | Gravity System | Yes (ADR-0003) | |
| 7 | Player Animation | **No** | In Progress -- placeholder only |
| 8 | Camera System | **No** | In Progress -- noted as gap in traceability doc |
| 9 | Room System | **No** | In Progress -- noted as gap in traceability doc |
| 10 | Respawn System | **No** | In Progress |
| 11 | Event Bus | **No** | Not Started -- architecture doc flags scope as open question |
| 12 | Gimmick Framework | **No** | Not Started -- Phase 2+ |
| 13 | Chapter Configuration | **No** | Not Started -- Phase 2+ |
| 14 | Booster Mode Swapping | **No** | Not Started -- Alpha priority |
| 15 | Momentum / Wavedash | **No** | Not Started -- Full Vision priority |
| 16 | Runtime Tuning Panel | **No** | Not Started |
| 17 | Audio System | **No** | In Progress |
| 18 | Hazard System | **No** | In Progress |

Additionally, **Physics Layers** are covered by ADR-0007 and **Input System** is covered by ADR-0002, though these are infrastructure rather than gameplay systems.

**Systems that most urgently need ADRs** (based on priority and current progress):

1. **Event Bus (System #11)** -- Vertical Slice priority, Not Started, has an open architecture question (static vs instance-based). Needs an ADR before implementation.
2. **Camera System (System #8)** -- MVP priority, In Progress. Room-snap behavior will affect level design. Needs an ADR.
3. **Respawn System (System #10)** -- Vertical Slice priority, In Progress. Death/respawn flow affects player experience fundamentally.
4. **Hazard System (System #18)** -- Vertical Slice priority, In Progress. Closely tied to physics layers (ADR-0007) and respawn.

### Foundation Layer Coverage

**Status: PASS**

The architecture document defines three Foundation-layer systems:

| Foundation System | ADR | Status |
|-------------------|-----|--------|
| Input System | ADR-0002 | Covered |
| Physics2D | ADR-0007 | Covered |
| Fuel System (JetpackGas) | ADR-0006 | Covered |

**All Foundation-layer systems have ADRs.**

## Verdict: CONCERNS

The architecture is fundamentally sound. All 7 ADRs are internally consistent, reference the correct engine version, use non-deprecated core APIs (with two low-severity exceptions), and trace cleanly to GDD requirements. The Foundation layer is fully covered. No dependency cycles exist.

Three concerns should be addressed:

1. **Dependency chain inconsistency** (medium priority): The master architecture doc, traceability matrix, and individual ADRs describe three different dependency chains. Reconcile these to a single source of truth to prevent confusion as the project grows.

2. **Deprecated API awareness** (low priority): `Resources.Load` (ADR-0002) is technically deprecated. Acknowledge this as a known trade-off in the ADR. The ParticleSystem usage (ADR-0006) is likely a false positive in the deprecated-apis reference -- clarify the reference doc.

3. **ADR coverage gap** (medium priority, expected): 11 of 18 systems lack ADRs. This is expected at the current project phase (MVP systems are covered; Vertical Slice and later systems are not). The Event Bus system is the most urgent gap, as it has an unresolved architectural question that blocks Track B work.

No blocking issues. The existing ADR set provides a solid foundation for the MVP milestone.
