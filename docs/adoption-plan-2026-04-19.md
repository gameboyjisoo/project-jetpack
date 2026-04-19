# Adoption Plan

> **Generated**: 2026-04-19
> **Project phase**: Technical Setup
> **Engine**: Unity 6 (6000.0.34f1) — configured in template preferences
> **Template version**: v1.0+

Work through these steps in order. Check off each item as you complete it.
Re-run `/adopt` anytime to check remaining gaps.

---

## Step 1: Fix Blocking Gaps

### 1a. Configure technical preferences
**Problem**: `.claude/docs/technical-preferences.md` has 28 `[TO BE CONFIGURED]` fields. All agents and skills reading this file get placeholder values instead of project-specific standards.
**Fix**: Populate from CLAUDE.md and project inspection.
**Time**: 15 min
- [x] All Engine & Language fields populated
- [x] All Input & Platform fields populated
- [x] Naming conventions populated
- [x] Performance budgets populated
- [x] Forbidden patterns populated (from CLAUDE.md conventions)
- [x] Engine specialists configured

### 1b. Create systems-index.md
**Problem**: No `design/gdd/systems-index.md` exists. Skills like `/gate-check`, `/create-stories`, and `/architecture-review` can't enumerate project systems.
**Fix**: Create from CLAUDE.md roadmap and architecture sections. Enumerate ~15 systems.
**Time**: 30 min
- [x] systems-index.md created with all identified systems (18 systems enumerated)
- [x] Status column uses only valid values (no parenthetical annotations)

### 1c. Create production/stage.txt
**Problem**: No `production/stage.txt` exists. Skills can't determine project phase.
**Fix**: Write `Technical Setup` (or appropriate phase after assessment).
**Time**: 5 min
- [x] stage.txt created

---

## Step 2: Fix High-Priority Gaps

### 2a. Create core GDDs (5 documents)
**Problem**: Zero GDD files exist. `/create-stories`, `/design-review`, `/gate-check` have nothing to work with. All mechanical specifications exist in CLAUDE.md but not in template format.
**Fix**: Extract from CLAUDE.md into template-compliant GDDs.
**Time**: 2-3 sessions

- [x] `design/gdd/player-movement.md` — Ground movement (Celeste-style)
- [x] `design/gdd/player-jump.md` — Jump system (variable height, coyote, buffer, apex)
- [x] `design/gdd/jetpack-system.md` — Booster 2.0-style jetpack
- [x] `design/gdd/secondary-booster.md` — 8-direction dash
- [x] `design/gdd/fuel-feedback.md` — Diegetic fuel feedback (particles + audio)

### 2b. Create core ADRs (7 decisions)
**Problem**: Zero ADR files. All architectural decisions exist informally in CLAUDE.md but lack formal structure, alternatives analysis, and GDD traceability.
**Fix**: Formalize each decision from CLAUDE.md.
**Time**: 2-3 sessions

- [x] `docs/architecture/adr-0001-component-player-architecture.md` — Player component split
- [x] `docs/architecture/adr-0002-input-timing-strategy.md` — Input in Update, physics in FixedUpdate
- [x] `docs/architecture/adr-0003-deterministic-gravity.md` — Gravity=0 during propulsion
- [x] `docs/architecture/adr-0004-booster-activation-pattern.md` — Booster 2.0 press-to-activate
- [x] `docs/architecture/adr-0005-post-boost-velocity-halving.md` — Mode-specific halving
- [x] `docs/architecture/adr-0006-diegetic-fuel-feedback.md` — No HUD, particle+audio feedback
- [x] `docs/architecture/adr-0007-physics-layer-allocation.md` — Layer organization

### 2c. Create control manifest
**Problem**: No `docs/architecture/control-manifest.md`. Stories can't be marked done without versioned manifest.
**Fix**: Run `/create-control-manifest` or create manually.
**Time**: 30 min
- [x] control-manifest.md created with version stamp

### 2d. Fix engine version reference
**Problem**: `docs/engine-reference/unity/VERSION.md` says Unity 6.3 LTS but project uses Unity 6 (6000.0.34f1).
**Fix**: Update VERSION.md to match actual project version.
**Time**: 5 min
- [x] VERSION.md updated to Unity 6 (6000.0.34f1)

---

## Step 3: Bootstrap Infrastructure

### 3a. Register existing requirements (creates tr-registry.yaml entries)
Run `/architecture-review` after ADRs exist — bootstraps TR registry from GDDs and ADRs.
**Time**: 1 session
- [ ] tr-registry.yaml populated with entries

### 3b. Create architecture traceability matrix
Instantiate `docs/architecture/architecture-traceability.md` from template.
**Time**: 30 min (after ADRs created)
- [x] architecture-traceability.md created as live document

### 3c. Create sprint tracking file
Run `/sprint-plan update` once sprint structure is defined.
**Time**: 5 min
- [ ] production/sprint-status.yaml created

### 3d. Set authoritative project stage
Run `/gate-check technical-setup` to validate and write stage.txt authoritatively.
**Time**: 5 min
- [x] production/stage.txt written (manual — gate-check validation deferred)

---

## Step 4: Medium-Priority Gaps

### 4a. Create game-concept.md
**Problem**: No formal game concept document. Concept exists in CLAUDE.md and README.md.
**Fix**: Extract into `design/gdd/game-concept.md`.
- [x] game-concept.md created

### 4b. Create production/review-mode.txt
**Problem**: No review mode configured.
**Fix**: Choose review intensity (full/lean/solo).
- [x] review-mode.txt created (set to "lean")

### 4c. Populate entities registry
**Problem**: `design/registry/entities.yaml` exists but has zero entries.
**Fix**: Populate after GDDs define formulas and constants.
- [ ] entities.yaml populated with project values

---

## Step 5: Optional Improvements

### 5a. Create stories from GDDs
Once GDDs, ADRs, and infrastructure exist, use `/create-stories` to generate sprint-ready stories.
- [ ] Stories created for core systems

### 5b. Set up test framework
Configure NUnit/Unity Test Runner for C# scripts.
- [ ] Test framework configured
- [ ] Initial tests for core movement

---

## What to Expect from Existing Code

The project has 19 C# scripts implementing a working movement prototype. These scripts continue to work as-is. The adoption process documents what exists — it does not require rewriting code. GDDs and ADRs formalize decisions that are already implemented.

---

## Re-run

Run `/adopt` again after completing Steps 1-2 to verify all blocking and high gaps are resolved.
