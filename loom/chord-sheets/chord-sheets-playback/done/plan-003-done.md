---
type: done
id: pl_01KXK8XJC2BVPSQ6GKPAWWDQAK-done
title: "Done — Fix: Below-cell adornment stopped showing (renderNow-reuse regression)"
status: done
created: 2026-07-15
version: 1
tags: []
parent_id: pl_01KXK8XJC2BVPSQ6GKPAWWDQAK
requires_load: []
---
# Done — Fix: Below-cell adornment stopped showing (renderNow-reuse regression)

Quick-shipped — recorded already-completed work:

1. Fixed the Chord Sheets 'Below cell' adornment (tone strip / fret diagram) showing nothing: Plan 2's renderNow now reuses the ChordSheetR component, but the 'Below cell' handler only called requestSheet() and never updated the reused component's adornment flags (frozen at creation as 'none'). The handler now also calls view.setAdornments({tones,diagrams}) so tones paint immediately and diagrams appear when the re-fetch returns their Core-computed data. node --check clean; C# untouched (920 tests still green).
