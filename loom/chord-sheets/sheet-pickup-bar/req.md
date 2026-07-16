---
type: req
id: rq_01KXPGHXZKXK9EDBQSNH569J34
title: ChordSheet model renders the pickup bar as a lead-in cell — Requirements
status: locked
created: 2026-07-16
updated: 2026-07-16
version: 1
design_version: 1
tags: []
parent_id: de_01KXPGHDR86C74B1XK3VNQN7HS
requires_load: []
---
# ChordSheet model renders the pickup bar as a lead-in cell — Requirements

### ✅ Included

- `IN1` The `ChordSheet` model carries the pickup as a **lead-in cell**: `ChordSheetCell.IsPickup` (trailing optional, default false), `Chords` = the first chord of the first section (the renderer's `\ac` rule), `BarTicks` = the pickup's `LengthTicks`, prepended to section 0 / row 0.
- `IN2` **Bar-index contract**: the lead-in cell is schedule bar 0; builder bars align with `AlphaTexRenderer.BarIndex` / alphaTab master bars for pickup songs — fixing the existing off-by-one in the sheet marker (both modes) and in `OverlaySchedule`'s mid-bar onset attachment. `OverlaySchedule` and `chord-sheets.js` stay unchanged.
- `IN3` The pickup is threaded into `ChordSheetBuilder.Build` as a `PickupMeasure?` parameter from `exercise.Comping.Pickup` (`ExerciseRendering.RenderWithSheet`).
- `IN4` `ChordSheetR` draws the lead-in cell distinctly in **both layouts**: width `max(0.4, barTicks/fullBarTicks) × BAR_W`, a muted "pickup" annotation, and beat-highlight columns = `ceil(barTicks/48)` (the pickup's real quarters). Exports (SVG/PNG/PDF) include it via the same SVG.
- `IN5` The playback `cellSchedule` covers the pickup bar (downbeat entry at bar 0) so both marker modes track the lead-in during the anacrusis.
- `IN6` A song **without** a pickup renders byte-identical to today (model, schedule, and drawing).
- `IN7` Doc/ref sync in the same unit of work: `globalBar` comment, `CellScheduleEntry` XML doc, and the domain-model ref's Chord-sheet sections updated to the new bar-index contract.

### ❌ Excluded

- `EX1` Playback engine / `PlaybackClock` changes (already anacrusis-correct).
- `EX2` Score view changes (already renders `\ac`).
- `EX3` DSL changes (pickup authoring already exists via `PICKUP:`).

### ⛓ Constraints

- `C1` C# unit tests cover the builder's pickup projection and schedule alignment (with-pickup + no-pickup regression); the JS drawing is verified via the validation walk (CDP harness optional).
- `C2` Projection only — no new music theory; the sheet model stays instrument-agnostic; the builder receives the `PickupMeasure`, never the full `RhythmPattern`.
- `C3` The lead-in cell never participates in simile detection — the first full bar must not render as `%` of the pickup.