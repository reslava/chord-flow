---
type: plan
id: pl_01KXK8XJC2BVPSQ6GKPAWWDQAK
title: "Fix: Below-cell adornment stopped showing (renderNow-reuse regression)"
status: done
created: 2026-07-15
updated: 2026-07-15
version: 1
design_version: 1
req_version: 1
tags: []
parent_id: de_01KXJJJYD9XBRYED1F8HYTG74H
requires_load: []
target_version: 0.1.0
actual_release: 0.15.0
steps:
  - id: fixed-the-chord-sheets-below-cell
    order: 1
    status: done
    description: "Fixed the Chord Sheets 'Below cell' adornment (tone strip / fret diagram) showing nothing: Plan 2's renderNow now reuses the ChordSheetR component, but the 'Below cell' handler only called requestSheet() and never updated the reused component's adornment flags (frozen at creation as 'none'). The handler now also calls view.setAdornments({tones,diagrams}) so tones paint immediately and diagrams appear when the re-fetch returns their Core-computed data. node --check clean; C# untouched (920 tests still green)."
    files_touched: []
    blocked_by: []
    satisfies: []
---
# Fix: Below-cell adornment stopped showing (renderNow-reuse regression)

## Goal

Fixed the Chord Sheets 'Below cell' adornment (tone strip / fret diagram) showing nothing: Plan 2's renderNow now reuses the ChordSheetR component, but the 'Below cell' handler only called requestSheet() and never updated the reused component's adornment flags (frozen at creation as 'none'). The handler now also calls view.setAdornments({tones,diagrams}) so tones paint immediately and diagrams appear when the re-fetch returns their Core-computed data. node --check clean; C# untouched (920 tests still green).

---

## Steps

| Done | # | Step | Files touched | Blocked by | Satisfies |
|---|---|---|---|---|---|
| ✅ | 1 | Fixed the Chord Sheets 'Below cell' adornment (tone strip / fret diagram) showing nothing: Plan 2's renderNow now reuses the ChordSheetR component, but the 'Below cell' handler only called requestSheet() and never updated the reused component's adornment flags (frozen at creation as 'none'). The handler now also calls view.setAdornments({tones,diagrams}) so tones paint immediately and diagrams appear when the re-fetch returns their Core-computed data. node --check clean; C# untouched (920 tests still green). | — | — | — |
---

### Legend

| Symbol | Meaning |
|--------|---------|
| ✅ | Done |
| 🔄 | In Progress |
| 🔳 | Pending |
| ❌ | Cancelled |
