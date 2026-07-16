---
type: req
id: rq_01KXMS4EVN3ABZAG3ERTAF11YN
title: Now/Next boards on Chord Sheets — one shared current/next-chord feed — Requirements
status: draft
created: 2026-07-16
version: 1
design_version: 1
tags: []
parent_id: de_01KXMS3Z1MTACNXG5R4NXKA5TR
requires_load: []
---
# Now/Next boards on Chord Sheets — one shared current/next-chord feed — Requirements

### ✅ Included

- `IN1` One shared Core producer (`RealizedSong` + `CompingPlan` → `ChordChange[]` of `{ bar, beat 0-based, chord label, comped FretboardDiagram }`), extracted from the current exercise-render by-product and called by both the exercise/loadScore path and `ChordSheetHandler`. Reuse the existing `ChordChange` DTO.
- `IN2` `ChordSheetHandler` resolves the comped grips for the now/next schedule **unconditionally** (not gated by the sheet's below-cell adornment toggle).
- `IN3` `chordSheetResult` gains an additive `chordSchedule` field (`ChordChange[]`, same wire shape as Practice's `loadScore` schedule); `cellSchedule` and `tex` unchanged.
- `IN4` The existing `ChordFlowNowNext` component mounted on the Chord Sheets page (above the sheet), fed `chordSchedule` and driven by the shared `engine.on("beat")` bus.
- `IN5` The Chord Sheets page passes `onToggleNowNext` to PlayerControlsR so its now-next toggle shows/hides the boards (default visible, matching Practice).
- `IN6` **Per-chord granularity:** the now/next boards advance per chord change regardless of the sheet's marker mode (visual-metronome or per-chord).
- `IN7` Update `loom/refs/chordflow-architecture-reference.md` (§6 data-flow + the now-next seam) in the same unit of work; add the producer to `chordflow-domain-model-reference.md` if it lands as a named Core type.

### ❌ Excluded

- `EX1` Any change to `ChordFlowNowNext` itself beyond mounting/feeding it (reuse as-is).
- `EX2` A new/parallel schedule DTO — the existing `ChordChange` is the single feed shape.
- `EX3` Putting the now/next boards inside the ChordSheetR SVG — they are page-mounted (SVG/export untouched).
- `EX4` A new bridge verb — `chordSchedule` is an additive field on the existing `chordSheetResult`.
- `EX5` An automated JS test harness for `wwwroot` (manual dogfood, as elsewhere).

### ⛓ Constraints

- `C1` `cellSchedule` (marker cell-addressing) and `chordSchedule` (now/next chord+grip) stay **separate projections** of the same realized-song walk.
- `C2` **ChordSheetR stays a pure SVG drawer** — export/screen parity intact; boards live in the page.
- `C3` **Practice parity:** the producer extraction leaves Practice's now/next behavior unchanged (Practice is the oracle for the shared feed).
- `C4` Reuse the `engine.on("beat")` bus + the optional PlayerControlsR now-next toggle from `ui/player-controls-component`; no new component and no new event plumbing.