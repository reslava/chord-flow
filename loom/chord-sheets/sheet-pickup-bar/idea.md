---
type: idea
id: id_01KXP2Z8NKMBG4QYX7EA93PA83
title: ChordSheet model renders the pickup bar as a lead-in cell
status: done
created: 2026-07-16
version: 1
tags: []
parent_id: null
requires_load: []
---
# ChordSheet model renders the pickup bar as a lead-in cell

## Problem

A song with a pickup (`\ac`) bar renders the pickup in the score/tab view (the `domain/anacrusis` thread
ships real pickup bars), but the **Chord Sheet does not show it at all** — the sheet starts at the first
full bar. Found while validating `playback/metronome-true-marker`: during the pickup the sheet marker stays
correctly silent (the playback clock is anacrusis-aware and aligns from the first full bar — numerically
verified there), but visually the sheet is missing the lead-in the player is hearing and the tab is showing.
The gap is in the **`ChordSheetBuilder` projection** (C#): it emits no cell for the pickup bar.

## Idea

Render the pickup as a **lead-in cell** — a visually distinct (narrower / annotated) cell before the first
full bar, in both layouts:

1. **Model (`Rendering/ChordSheets/` + `ChordSheetBuilder`)**: emit the pickup bar as a first cell flagged
   as lead-in (e.g. `IsPickup` on the cell or row), carrying its chord(s) and beat count like any other cell.
2. **`ChordSheetR`**: draw the lead-in cell distinctly in Layout A (leadsheet) and Layout B (grid) — e.g.
   reduced width proportional to its real beat count, or a small "pickup" annotation; exports (SVG/PNG/PDF)
   include it since they render the same SVG.
3. **`cellSchedule`**: map the pickup bar (score bar 0) to the lead-in cell so both marker modes track it —
   the Visual-metronome marker then steps the pickup's actual quarters (the `"position"` clock already
   reports them: e.g. one step for a 1-quarter pickup) instead of staying dark.

## Scope

- **In**: `ChordSheetBuilder` + the `ChordSheet` model carrying the pickup; ChordSheetR drawing the lead-in
  cell (both layouts, exports included); `cellSchedule` covering the pickup bar.
- **Out**: the playback engine / `PlaybackClock` (already anacrusis-correct); the score view (already renders
  `\ac`); any DSL change (pickup authoring already exists).

## Validation

- A song with a `\ac` pickup shows the lead-in cell in Layout A and Layout B, visually distinct from full bars.
- During playback, the sheet marker highlights the lead-in cell for the pickup's real duration (both marker
  modes), then bar 1 on the downbeat — no dark gap, no misalignment.
- SVG/PNG/PDF exports include the lead-in cell.
- A song without a pickup renders exactly as today.