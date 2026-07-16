---
type: design
id: de_01KXPGHDR86C74B1XK3VNQN7HS
title: ChordSheet model renders the pickup bar as a lead-in cell
status: done
created: 2026-07-16
version: 1
idea_version: 1
tags: []
parent_id: id_01KXP2Z8NKMBG4QYX7EA93PA83
requires_load: []
---
# ChordSheet model renders the pickup bar as a lead-in cell

## Context

A song with a pickup (`\ac`) bar renders it in the score view but not in the Chord Sheet: `ChordSheetBuilder.Build` walks `realized.Sections` only, so the pickup produces no cell. Grounding the idea against the code surfaced a second, worse defect the fix must own:

**The bar-index contract is broken for pickup songs (off-by-one).** The builder's `globalBar` counts full bars from 0 and its comment claims alignment with `AlphaTexRenderer.state.BarIndex` — but the renderer counts the `\ac` bar as bar 0 (`RenderBar` increments on it), and so do alphaTab's master bars (the `PlaybackClock` reports the pickup as 1-based bar 1, proven numerically in `playback/metronome-true-marker` test B). Consequences today, whenever a pickup exists:

- `chord-sheets.js` `onPosition`/`onBeat` do `scheduleByBar.get(bar - 1)` → during the anacrusis the **first full bar's cell** highlights, and every bar of the song highlights **one cell ahead** (observed in the metronome-true-marker validation walk as "sheet start at bar 2"; the origin chat's "correctly silent during the pickup" claim was wrong).
- `ChordSheetBuilder.OverlaySchedule` keys render-schedule bars against builder bars → mid-bar chord onsets attach **one bar late**.
- The first `ChordChange` is recorded in the pickup (render bar 0), so the Now/Next feed and the sheet disagree about where bar 0 is.

Emitting the pickup as **builder bar 0** fixes the missing cell *and* restores builder ↔ renderer ↔ alphaTab alignment by construction — no JS offsets, no consumer changes.

## Decisions

### D1 — Bar-index contract: the lead-in cell is schedule bar 0

`globalBar` counts the pickup as bar 0 (when present); full bars follow at 1, 2, …. `BarSchedule` gains a downbeat `CellScheduleEntry(Bar: 0, Beat: 0, …)` for the pickup cell. `OverlaySchedule` and `chord-sheets.js` are **unchanged** — indices now align by construction (the render schedule and the playback clock already count the pickup).

- *Alternative rejected:* a JS-side "+1 when pickup" offset — spreads pickup-awareness into every consumer and leaves the model dishonest about what plays.
- No-pickup songs: `globalBar` starts at the first full bar exactly as today — byte-identical output.

### D2 — Input threading: `Build` gains a `PickupMeasure? pickup` parameter

The pickup lives on the **RhythmPattern** (`exercise.Comping.Pickup`), not on `Song`/`RealizedSong` — the builder never sees rhythm today. `ChordSheetBuilder.Build(song, realized, sheetKey, ts, options, comping, pickup)` takes the `PickupMeasure?` directly (not the whole pattern: the builder is a harmony walk; it only needs `LengthTicks`). `ExerciseRendering.RenderWithSheet` passes `exercise.Comping.Pickup`.

- *Alternative rejected:* passing the full `RhythmPattern` — imports timing detail the projection doesn't use.

### D3 — Model shape: `IsPickup` flag on `ChordSheetCell`, prepended to section 0 / row 0

`ChordSheetCell` gains `bool IsPickup = false` (trailing optional — every existing construction site compiles unchanged). The lead-in cell:

- `Chords` = one `ChordRef` for the **first chord of the first section** (same rule as the renderer's `\ac` voicing), projected via the existing `ToChordRef` with `DurationTicks = pickup.LengthTicks`.
- `BarTicks = pickup.LengthTicks` (the natural width/beat-count datum).
- `RepeatOfPrev = false`, and the pickup is emitted **before** the per-section walk so `previous` stays null — the first full bar can never render as a `%` of the lead-in.
- **Placement:** prepended to section 0 / row 0, which then holds `barsPerRow + 1` cells. A real leadsheet shows the pickup at the start of the first line; a dedicated row would waste a printed line on a 1-beat bar. Only row 0's cell indices shift (+1); the schedule entries carry the shifted indices, so consumers stay index-blind.

### D4 — Drawing rule (ChordSheetR): proportional width with a floor, plus a "pickup" annotation

In both layouts the `isPickup` cell draws at `max(0.4, barTicks / fullBarTicks) × BAR_W` — proportional so a 1-quarter lead-in reads as short, floored so the chord symbol stays legible — with a small muted *pickup* label (top corner of the cell). `fullBarTicks` derives from the sheet's `timeSig` (192 in 4/4). Beat-highlight columns for the Visual-metronome marker: `ceil(barTicks / 48)` slices instead of the fixed full-bar count, so the marker steps the pickup's **real** quarters (the clock already emits exactly those). Exports (SVG/PNG/PDF) render the same SVG — no extra work.

### D5 — Doc/ref sync in the same unit of work

The `globalBar` comment, the `CellScheduleEntry` XML doc, and the domain-model ref's "Chord-sheet presentation model / Playback projection" sections all state the (previously wrong) alignment claim — all are updated to the new contract: *builder bars = alphaTab master bars, pickup included as bar 0*.

## Component changes

| Component | Change |
|---|---|
| `Rendering/ChordSheets/ChordSheet.cs` | `ChordSheetCell` + `IsPickup` (trailing optional); doc fixes on `CellScheduleEntry`. |
| `Features/ChordSheets/ChordSheetBuilder.cs` | `Build` + `PickupMeasure? pickup` param; emit lead-in cell + its bar-0 schedule entry; `globalBar` counts it; comment fix. `OverlaySchedule` untouched. |
| `Features/ExerciseRendering.cs` | `RenderWithSheet` passes `exercise.Comping.Pickup` to `Build`. |
| `wwwroot/chord-sheet-render-component.js` | `isPickup` cell: floored proportional width + annotation, real-quarter beat columns; both layouts. |
| `wwwroot/chord-sheets.js` | **No change** (D1 makes the existing `get(bar - 1)` correct). |
| `loom/refs/chordflow-domain-model-reference.md` | Contract text updated (D5). |

## Edge cases

- **No pickup** → output byte-identical to today (regression test).
- **Partial-quarter pickup** (`LengthTicks` not a multiple of 48, e.g. 72): beat columns = `ceil`, cell width still proportional; the clock's last quarter step is partial — marker behavior degrades gracefully (last column holds slightly long).
- **Pickup + mid-bar splits**: overlay onsets now land on the right bars (previously one late).
- **Per-chord marker during the pickup**: `entries[0]` of bar 0 → the lead-in cell highlights — correct, it sounds the first chord.
- **Empty first section** (`first.Bars.Count == 0`): mirror the renderer's guard — no lead-in cell emitted.

## Testing

- **C# (`ChordSheetBuilderTests`)**: with a pickup — lead-in cell present/flagged/first-chord/`BarTicks` correct; schedule bar 0 = lead-in, full bars shifted +1; first full bar never `RepeatOfPrev`; `OverlaySchedule` attaches a mid-bar onset to the correct (shifted) bar. Without a pickup — result equal to today's (golden regression).
- **JS**: no unit harness — verified by the validation walk; optionally the CDP harness from `metronome-true-marker` (drive playback, assert the marker's cell sequence starts at the lead-in).

## Validation walk

1. Pickup song, Layout A + B: lead-in cell visible, narrow, annotated; bar 1 starts the full-width cells.
2. Play: Visual-metronome marker steps the pickup's real quarters in the lead-in cell, then bar 1 on the downbeat — the whole song no longer runs one cell ahead; Per-chord mode highlights the lead-in during the anacrusis.
3. Mid-bar-split pickup song: sub-chord highlights land in the right bars.
4. No-pickup song: renders and tracks exactly as today.
5. SVG/PNG/PDF exports include the lead-in cell.