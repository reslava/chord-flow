---
type: plan
id: pl_01KXPGP8DSQEQX2T987AYG7AA0
title: sheet-pickup-bar Plan
status: done
created: 2026-07-16
updated: 2026-07-16
version: 1
design_version: 1
req_version: 1
tags: []
parent_id: de_01KXPGHDR86C74B1XK3VNQN7HS
requires_load: []
target_version: 0.1.0
steps:
  - id: model-builder-lead-in-cell-as
    order: 1
    status: done
    description: Add trailing-optional `IsPickup` to `ChordSheetCell`; add `PickupMeasure? pickup` param to `ChordSheetBuilder.Build`; emit the lead-in cell (first chord of first section, `BarTicks = pickup.LengthTicks`, prepended to section 0 / row 0, before the section walk so simile never sees it) plus its downbeat `CellScheduleEntry` at bar 0, with `globalBar` counting the pickup; pass `exercise.Comping.Pickup` from `ExerciseRendering.RenderWithSheet`; fix the now-wrong `globalBar` comment and `CellScheduleEntry` XML doc.
    files_touched: [src/ChordFlow.Core/Rendering/ChordSheets/ChordSheet.cs, src/ChordFlow.Core/Features/ChordSheets/ChordSheetBuilder.cs, src/ChordFlow.Core/Features/ExerciseRendering.cs]
    blocked_by: []
    satisfies: [IN1, IN2, IN3, IN5, C2, C3]
  - id: c-tests-pickup-projection-schedule-alignment
    order: 2
    status: done
    description: "Builder tests with a pickup: lead-in cell present/flagged/first-chord/`BarTicks` correct; schedule bar 0 = lead-in and full bars shifted +1; first full bar never `RepeatOfPrev`; `OverlaySchedule` attaches a mid-bar onset to the correct shifted bar. Without a pickup: result equal to today's output (byte-identical regression)."
    files_touched: [tests/ChordFlow.Core.Tests/]
    blocked_by: [model-builder-lead-in-cell-as]
    satisfies: [IN6, C1, C3]
  - id: chordsheetr-draw-the-lead-in-cell
    order: 3
    status: done
    description: Draw an `isPickup` cell at width `max(0.4, barTicks/fullBarTicks) × BAR_W` with a muted "pickup" annotation, in Layout A and Layout B; beat-highlight columns = `ceil(barTicks/48)` so the Visual-metronome marker steps the pickup's real quarters; row layout accommodates the extra row-0 cell; exports render the same SVG.
    files_touched: [src/ChordFlow.Desktop/wwwroot/chord-sheet-render-component.js]
    blocked_by: [model-builder-lead-in-cell-as]
    satisfies: [IN4]
  - id: ref-sync-domain-model-reference
    order: 4
    status: done
    description: Update the domain-model ref's "Chord-sheet presentation model" + "Playback projection" sections to the new bar-index contract (builder bars = alphaTab master bars, pickup included as bar 0; `IsPickup` lead-in cell; `Build`'s `PickupMeasure?` param).
    files_touched: [loom/refs/chordflow-domain-model-reference.md]
    blocked_by: [model-builder-lead-in-cell-as]
    satisfies: [IN7]
  - id: verify-validation-walk
    order: 5
    status: done
    description: Build + run all tests; then the walk — pickup song in Layout A/B shows the narrow annotated lead-in cell; playing it, the Visual-metronome marker steps the pickup's real quarters then bar 1 on the downbeat (no more one-cell-ahead drift) and Per-chord mode highlights the lead-in during the anacrusis; a mid-bar-split pickup song lands sub-chord highlights in the right bars; a no-pickup song renders and tracks exactly as today; SVG/PNG/PDF exports include the lead-in. CDP harness (metronome-true-marker pattern) optional for the marker-sequence assert.
    files_touched: []
    blocked_by: [c-tests-pickup-projection-schedule-alignment, chordsheetr-draw-the-lead-in-cell]
    satisfies: [C1, IN6]
---
# sheet-pickup-bar Plan

## Goal

Render a song's pickup (`\ac`) bar in the Chord Sheet as a visually distinct lead-in cell, and — the correctness core — fix the builder's bar-index contract so builder bars align with the renderer's BarIndex / alphaTab master bars when a pickup exists (the lead-in cell becomes schedule bar 0). This repairs the existing off-by-one in both sheet-marker modes and in OverlaySchedule's mid-bar onset attachment, with zero changes to the playback engine or the JS schedule consumers (design D1–D5; req rq_01KXPGHXZKXK9EDBQSNH569J34).

---

## Steps

| Done | # | Step | Files touched | Blocked by | Satisfies |
|---|---|---|---|---|---|
| ✅ | 1 | Add trailing-optional `IsPickup` to `ChordSheetCell`; add `PickupMeasure? pickup` param to `ChordSheetBuilder.Build`; emit the lead-in cell (first chord of first section, `BarTicks = pickup.LengthTicks`, prepended to section 0 / row 0, before the section walk so simile never sees it) plus its downbeat `CellScheduleEntry` at bar 0, with `globalBar` counting the pickup; pass `exercise.Comping.Pickup` from `ExerciseRendering.RenderWithSheet`; fix the now-wrong `globalBar` comment and `CellScheduleEntry` XML doc. | src/ChordFlow.Core/Rendering/ChordSheets/ChordSheet.cs, src/ChordFlow.Core/Features/ChordSheets/ChordSheetBuilder.cs, src/ChordFlow.Core/Features/ExerciseRendering.cs | — | IN1, IN2, IN3, IN5, C2, C3 |
| ✅ | 2 | Builder tests with a pickup: lead-in cell present/flagged/first-chord/`BarTicks` correct; schedule bar 0 = lead-in and full bars shifted +1; first full bar never `RepeatOfPrev`; `OverlaySchedule` attaches a mid-bar onset to the correct shifted bar. Without a pickup: result equal to today's output (byte-identical regression). | tests/ChordFlow.Core.Tests/ | model-builder-lead-in-cell-as | IN6, C1, C3 |
| ✅ | 3 | Draw an `isPickup` cell at width `max(0.4, barTicks/fullBarTicks) × BAR_W` with a muted "pickup" annotation, in Layout A and Layout B; beat-highlight columns = `ceil(barTicks/48)` so the Visual-metronome marker steps the pickup's real quarters; row layout accommodates the extra row-0 cell; exports render the same SVG. | src/ChordFlow.Desktop/wwwroot/chord-sheet-render-component.js | model-builder-lead-in-cell-as | IN4 |
| ✅ | 4 | Update the domain-model ref's "Chord-sheet presentation model" + "Playback projection" sections to the new bar-index contract (builder bars = alphaTab master bars, pickup included as bar 0; `IsPickup` lead-in cell; `Build`'s `PickupMeasure?` param). | loom/refs/chordflow-domain-model-reference.md | model-builder-lead-in-cell-as | IN7 |
| ✅ | 5 | Build + run all tests; then the walk — pickup song in Layout A/B shows the narrow annotated lead-in cell; playing it, the Visual-metronome marker steps the pickup's real quarters then bar 1 on the downbeat (no more one-cell-ahead drift) and Per-chord mode highlights the lead-in during the anacrusis; a mid-bar-split pickup song lands sub-chord highlights in the right bars; a no-pickup song renders and tracks exactly as today; SVG/PNG/PDF exports include the lead-in. CDP harness (metronome-true-marker pattern) optional for the marker-sequence assert. | — | c-tests-pickup-projection-schedule-alignment, chordsheetr-draw-the-lead-in-cell | C1, IN6 |
---

### Legend

| Symbol | Meaning |
|--------|---------|
| ✅ | Done |
| 🔄 | In Progress |
| 🔳 | Pending |
| ❌ | Cancelled |
