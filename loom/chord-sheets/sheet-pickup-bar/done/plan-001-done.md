---
type: done
id: pl_01KXPGP8DSQEQX2T987AYG7AA0-done
title: Done — sheet-pickup-bar Plan
status: done
created: 2026-07-16
version: 6
tags: []
parent_id: pl_01KXPGP8DSQEQX2T987AYG7AA0
requires_load: []
---
# Done — sheet-pickup-bar Plan

## Step 1 — Add trailing-optional `IsPickup` to `ChordSheetCell`; add `PickupMeasure? pickup` param to `ChordSheetBuilder.Build`; emit the lead-in cell (first chord of first section, `BarTicks = pickup.LengthTicks`, prepended to section 0 / row 0, before the section walk so simile never sees it) plus its downbeat `CellScheduleEntry` at bar 0, with `globalBar` counting the pickup; pass `exercise.Comping.Pickup` from `ExerciseRendering.RenderWithSheet`; fix the now-wrong `globalBar` comment and `CellScheduleEntry` XML doc.

- `ChordSheet.cs`: `ChordSheetCell` gained trailing-optional `IsPickup = false` (existing construction sites compile unchanged) with doc text for the lead-in semantics; `CellScheduleEntry` XML doc gained the explicit **bar-index contract** paragraph (bar counts every rendered bar including a pickup — lead-in = bar 0, first full bar = 1 — one axis shared with `ChordChange.Bar` and alphaTab master bars).
- `ChordSheetBuilder.cs`: `Build` gained `PickupMeasure? pickup = null`. The lead-in cell is built **outside** the section walk (so `previous` never sees it — C3) as `ToChordRef(firstSpan) with { DurationTicks = pickup.LengthTicks }` — reusing the span-keyed `CompingPlan.For` lookup so the lead-in carries the same voicing/diagram as bar 1, mirroring the renderer's `\ac` rule — then prepended to section 0 / row 0 (`hasLeadIn` shifts that row's full-bar `cellIndex` by +1). Its downbeat `CellScheduleEntry` is emitted at bar 0 and `globalBar` counts it, restoring builder ↔ renderer ↔ alphaTab alignment (D1). Same guard as the renderer (`Sections.Count > 0 && Bars.Count > 0`). The stale `globalBar` comment replaced with the corrected contract.
- `ExerciseRendering.cs`: `RenderWithSheet` passes `exercise.Comping.Pickup` (the pickup lives on the RhythmPattern, D2).
- `dotnet build` ChordFlow.Core: 0 warnings, 0 errors. `OverlaySchedule` untouched as designed.

## Step 2 — Builder tests with a pickup: lead-in cell present/flagged/first-chord/`BarTicks` correct; schedule bar 0 = lead-in and full bars shifted +1; first full bar never `RepeatOfPrev`; `OverlaySchedule` attaches a mid-bar onset to the correct shifted bar. Without a pickup: result equal to today's output (byte-identical regression).

Extended `tests/ChordFlow.Core.Tests/ChordSheets/ChordSheetBuilderTests.cs` with a pickup section (5 new tests, shared `QuarterPickup` fixture + `BuildWithPickup` helper):

- `Build_WithPickup_PrependsLeadInCellToFirstRow` — row 0 = BarsPerRow + 1 cells; lead-in flagged, first-chord (`C`), `BarTicks`/`DurationTicks` = 48.
- `Build_WithPickup_ScheduleCountsLeadInAsBarZero` — lead-in = bar 0 at (0,0,0,0,0,0); full bars shift +1 with row-0 cell indices shifted +1 (D1/IN2).
- `Build_WithPickup_FirstFullBarIsNeverASimileOfTheLeadIn` — `"1 1"`: first full bar keeps real chords; second still a `%` (C3).
- `Build_WithoutPickup_IsUnchanged` — null pickup: no `IsPickup` cells, 4-cell row 0, `BarSchedule` value-equal to the baseline build (IN6; full-sheet record equality unusable — `IReadOnlyList` members compare by reference — so the regression asserts the value-record schedule + structure).
- `OverlaySchedule_WithPickup_AttachesMidBarOnsetToTheAlignedBar` — first `OverlaySchedule` test in the suite: `"1 17_47"` + pickup, renderer-shaped schedule (`\ac` change at (0,0), mid-bar F7 at (2,2)) → the onset lands on the split bar's cell (2,2,0,0,2,1), 4 entries, (bar,beat)-ordered. Before D1 this attached one bar late.

`dotnet test --filter ChordSheetBuilderTests`: **22/22 passed** (17 pre-existing untouched).

## Step 3 — Draw an `isPickup` cell at width `max(0.4, barTicks/fullBarTicks) × BAR_W` with a muted "pickup" annotation, in Layout A and Layout B; beat-highlight columns = `ceil(barTicks/48)` so the Visual-metronome marker steps the pickup's real quarters; row layout accommodates the extra row-0 cell; exports render the same SVG.

`chord-sheet-render-component.js` — the lead-in cell drawing, both layouts, one code path (the SVG is shared by screen + export, so SVG/PNG/PDF get it for free):

- **`cellW(cell)` / `rowWidth(cells)`**: per-cell width — full bar = `BAR_W`; `isPickup` = `max(0.4, barTicks / (beatsPerBar()×48)) × BAR_W` (D4's floored proportional rule).
- **`drawRow`**: switched from uniform `x + i*BAR_W` to a cumulative `cx`; cell backdrop, beat columns, Layout-B border, and Layout-A closing barline all use the per-cell `w`. Beat-highlight columns for the lead-in = `max(1, ceil(barTicks/48))` (its real quarters — exactly the steps the clock emits for the `\ac` bar); full bars keep `beatsPerBar()`.
- **`drawPickupTag`**: small muted italic "pickup" at the top of the cell (annotation half of D4).
- **`layoutSection` (Layout A)**: the lead-in never consumes a re-wrap bar slot — full bars chunk to `barsPerRow`, then the lead-in prepends to line 1, reproducing Core's row-0 shape so the marker's (row, cell) addressing stays schedule-aligned. Layout B rows come pre-chunked from Core (already correct).
- **`buildSheetSvg` / `maxRowWidth`**: content width is now the widest row in **pixels** (`rowWidth`), not a column count — row 0 is one cell longer but narrower than (barsPerRow+1) full bars; `maxCols` deleted (unused). Header rule spans `contentW`.
- Model-shape doc comment gains `isPickup`. `node --check`: syntax OK. No-pickup sheets: `cellW` returns `BAR_W` for every cell and layout A finds no lead-in → geometry identical to before.

## Step 4 — Update the domain-model ref's "Chord-sheet presentation model" + "Playback projection" sections to the new bar-index contract (builder bars = alphaTab master bars, pickup included as bar 0; `IsPickup` lead-in cell; `Build`'s `PickupMeasure?` param).

Ref sync via `loom_patch_doc` on `chordflow-domain-model-reference` (3 surgical patches):

- **Chord-sheet presentation model**: `ChordSheetCell` signature now shows `IsPickup`; added the lead-in rule — `Build`'s `PickupMeasure?` param (fed from `exercise.Comping.Pickup` by `RenderWithSheet`), first-chord voicing per the renderer's `\ac` rule, real-length `BarTicks`, prepended to section 0 / row 0 outside simile detection, drawn narrower with a "pickup" tag and real-quarter beat columns.
- **Playback projection**: added the explicit **bar-index contract** — the schedule's bar axis counts every rendered bar including the pickup lead-in (bar 0), one axis with `ChordChange.Bar` and alphaTab master bars; noted the pre-fix off-by-one it repairs.

**Process note:** this step's ✅ was set out of order — I mistyped stepNumber 4 when completing step 3, and Loom has no un-complete surface, so I performed step 4's work immediately after to make the mark truthful (step 3 then marked done as well). The plan's step states are accurate as of this note.

## Step 5 — Build + run all tests; then the walk — pickup song in Layout A/B shows the narrow annotated lead-in cell; playing it, the Visual-metronome marker steps the pickup's real quarters then bar 1 on the downbeat (no more one-cell-ahead drift) and Per-chord mode highlights the lead-in during the anacrusis; a mid-bar-split pickup song lands sub-chord highlights in the right bars; a no-pickup song renders and tracks exactly as today; SVG/PNG/PDF exports include the lead-in. CDP harness (metronome-true-marker pattern) optional for the marker-sequence assert.

**Automated verification + Rafa's visual walk — both passed.**

- **Full suite: 923/923 passed** (`dotnet test`, ChordFlow.Core.Tests), including one extra test added during verify: `Render_Schedule_WithPickup_CountsTheAnacrusisAsBarZero` in `AlphaTexRendererTests` — no test pinned the renderer's half of the bar axis, so this fact proves the `\ac` bar consumes render bar 0 and a full-bar change lands at bar 2 (`(0,0,Bb7),(2,0,Eb7)` for `"17 47"` + 1-quarter pickup). Passed first run — numeric confirmation of the off-by-one premise from the renderer side.
- **Headless SVG geometry harness** (scratchpad `sheet-pickup-dom-test.js`, a minimal DOM stub over the real component file — not committed): **20/20 checks** — Layout A re-wrap parity with Core (lead-in + 4 on row 0, remainder on row 1), 48-tick lead-in floored at 0.4×BAR_W (55.2) with 1 beat column, 96-tick lead-in proportional (69) with 2 beat columns, full bars keep BAR_W/4 columns, bar 1 starts flush after the narrow cell, "pickup" tag present, Layout B border matches the narrow cell, sheet width = widest row in pixels, and the no-pickup model reproduces legacy geometry exactly (uniform x positions, width 584, no tag).
- **Rafa's visual walk — passed** ("worked nicely"): authored pickups of 1, 2, and 3 quarters (the default pack ships no `PICKUP:` rhythm), all rendered well — the lead-in cell shows narrow + annotated and the marker tracks it. The proportional width rule got exercised across three lengths (floored 0.4 at 1 quarter, 0.5 at 2, 0.75 at 3).
