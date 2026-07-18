---
type: req
id: rq_01KXTA1111DFBCNMPBF7BF7FK0
title: Content page mounts the shared render surfaces (ScoreR + ChordSheetR + toggle) like Practice — Requirements
status: locked
created: 2026-07-18
updated: 2026-07-18
version: 1
design_version: 4
tags: []
parent_id: de_01KXT9SEKXG87T69JPW7AQ018T
requires_load: []
---
# Content page mounts the shared render surfaces (ScoreR + ChordSheetR + toggle) like Practice — Requirements

### ✅ Included

- `IN1` Extract a reusable composite JS component (`ChordFlowRenderSurface`) that owns the shared render surface: `ScoreR` (`transport:false`) + `ChordSheetR` + the Score⇄Sheet toggle + a page-level `PlayerControlsR` bound to the one engine + the beat/position marker fan-out.
- `IN2` The Content page's progression/song preview mounts the composite (Score + Sheet behind the toggle + shared transport), replacing today's bare ScoreR-only preview.
- `IN3` The Practice page (`app.js`) mounts the same composite — it becomes a composite-consumer, keeping HarmonyControlsR + Now/Next around it.
- `IN4` The Content progression/song preview routes through `ExerciseRendering.RenderWithSheet`, so the score, the chord sheet, and the cell schedule all derive from one realized-song + CompingPlan pass.
- `IN5` `EntityPreviewEnvelope` carries the chord-sheet projection (`Sheet` + `CellSchedule`) for score-kind previews (populated for progression/song; null for rhythm and voicing).
- `IN6` The Score⇄Sheet toggle works mid-playback on Content — one engine across the toggle, both markers keep tracking — matching Practice's behavior.
- `IN7` A minor progression previews correct chords + `\ks` in BOTH the score and the sheet surface on Content (the regression the divergence hid).

### ❌ Excluded

- `EX1` Not merging the two pages — Content stays an editor, Practice stays practice; only the render surface converges.
- `EX2` Not moving authoring controls (tonality/comping/DSL) into Practice, nor performance controls (difficulty/lead/voicing-window) into Content.
- `EX3` No new render capability — consolidation only.
- `EX4` Now/Next boards are NOT added to Content (a Practice performance affordance, not part of the render surface).

### ⛓ Constraints

- `C1` The composite is a dumb view — zero music theory in JS; the sheet model, tex, and schedules are all built in Core.
- `C2` Per-entity degradation: progression/song = composite with sheet; rhythm = composite score-only (toggle hidden); voicing = the existing `ChordFlowFretboard` path, NOT the composite.
- `C3` Content has no HarmonyControlsR, so the composite's ScoreR keeps its opt-in key/feel pickers (Content's live transpose/feel); tempo stays a `PlayerControlsR` param on both pages.
- `C4` No duplicated composition logic — the toggle + single-engine + dual-marker wiring lives in exactly one place, so a ScoreR/ChordSheetR/PlayerControlsR fix lands once and both pages benefit.
- `C5` Update `chordflow-architecture-reference.md` in the same unit of work (the new render-surface composite is an app-architecture / component-boundary change).
