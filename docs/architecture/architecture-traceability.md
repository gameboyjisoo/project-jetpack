# Architecture Traceability Matrix — Project Jetpack

> **Last Updated**: 2026-04-19
> **Engine**: Unity 6 (6000.0.34f1)
> **GDDs Indexed**: 5
> **ADRs Indexed**: 7

---

## Coverage Summary

| Metric | Count |
|--------|-------|
| GDD requirements covered by ADRs | 5/5 |
| GDD requirements partially covered | 0 |
| GDD requirements with no ADR | 0 |
| ADRs with no GDD traceability | 1 (ADR-0007 — foundational) |

---

## Traceability Matrix

| Req ID | GDD | System | Requirement | ADRs | Status |
|--------|-----|--------|-------------|------|--------|
| REQ-MOV-001 | player-movement.md | Player Movement | Celeste-style MoveTowards accel/decel | ADR-0001, ADR-0002 | Implemented |
| REQ-MOV-002 | player-movement.md | Player Movement | Air control multiplier (0.65) | ADR-0001 | Implemented |
| REQ-JMP-001 | player-jump.md | Player Jump | Variable height jump (0.2s hold window) | ADR-0001, ADR-0002, ADR-0003 | Implemented |
| REQ-JMP-002 | player-jump.md | Player Jump | Coyote time (0.1s) + jump buffer (0.1s) | ADR-0001, ADR-0002 | Implemented |
| REQ-JMP-003 | player-jump.md | Player Jump | Apex gravity (0.5× when holding + low vy) | ADR-0003 | Implemented |
| REQ-JET-001 | jetpack-system.md | Jetpack | 4-cardinal press-to-activate | ADR-0003, ADR-0004 | Implemented |
| REQ-JET-002 | jetpack-system.md | Jetpack | Velocity set once on activation (not per-frame) | ADR-0004 | Implemented |
| REQ-JET-003 | jetpack-system.md | Jetpack | Gravity=0 during boost (deterministic axiom) | ADR-0003 | Implemented |
| REQ-JET-004 | jetpack-system.md | Jetpack | Mode-specific velocity halving on release | ADR-0005 | Implemented |
| REQ-JET-005 | jetpack-system.md | Jetpack | Wall nudge during horizontal boost | ADR-0005 | Implemented |
| REQ-JET-006 | jetpack-system.md | Jetpack | ~1s fuel, recharges on landing | ADR-0004 | Implemented |
| REQ-SEC-001 | secondary-booster.md | Secondary Booster | 8-direction fixed-distance dash | ADR-0001, ADR-0003 | Implemented |
| REQ-SEC-002 | secondary-booster.md | Secondary Booster | 3 ammo, recharge on ground | ADR-0001 | Implemented |
| REQ-SEC-003 | secondary-booster.md | Secondary Booster | Gravity suppressed during dash | ADR-0003 | Implemented |
| REQ-FDB-001 | fuel-feedback.md | Fuel Feedback | Particle color shift (cyan→orange→red) | ADR-0006 | Implemented |
| REQ-FDB-002 | fuel-feedback.md | Fuel Feedback | Audio burst interval + pitch decay | ADR-0006 | Implemented |
| REQ-FDB-003 | fuel-feedback.md | Fuel Feedback | No persistent HUD bar | ADR-0006 | Implemented |

---

## Known Gaps

### Foundation Layer
- No gaps — all foundation systems (Input, Physics, Gravity) have ADRs

### Core Layer
- Camera system has no GDD or ADR yet (currently simple follow, room-snapping planned)
- Room system has no GDD or ADR yet (implemented but inactive)

### Feature Layer
- Gimmick framework not yet designed (Phase 2+)
- Chapter configuration not yet designed (Phase 2+)
- Event bus not yet designed (needed for gimmick decoupling)

### Presentation Layer
- Animation system has no GDD (placeholder sprites, controller not connected)
- Audio system beyond jetpack feedback not yet designed

---

## ADR → GDD Coverage (Reverse Index)

| ADR | GDD(s) Addressed | Coverage |
|-----|-------------------|----------|
| ADR-0001 | player-movement, player-jump, jetpack-system, secondary-booster | Full |
| ADR-0002 | player-movement, player-jump, jetpack-system, secondary-booster | Full |
| ADR-0003 | player-jump, jetpack-system, secondary-booster | Full |
| ADR-0004 | jetpack-system | Full |
| ADR-0005 | jetpack-system | Full |
| ADR-0006 | fuel-feedback | Full |
| ADR-0007 | Foundational (enables all physics-dependent GDDs) | N/A |

---

## Superseded Requirements

None — all requirements are current as of initial documentation pass.

---

## Cross-ADR Conflicts

None detected. ADR dependency chain is clean:
- ADR-0007 (foundation) → ADR-0002 (input) → ADR-0003 (gravity) → ADR-0004 (activation) → ADR-0005 (halving)
- ADR-0006 (feedback) is independent
- ADR-0001 (architecture) depends on all others being implementable
