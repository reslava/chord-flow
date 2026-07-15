---
type: plan
id: pl_01KXKBK90BXSAD7CPTA752MET4
title: "Docs: architecture ref + README reflect ChordFlowPlayback / ChordSheetR / play-along"
status: done
created: 2026-07-15
updated: 2026-07-15
version: 1
design_version: 1
req_version: 2
tags: []
parent_id: de_01KXJJJYD9XBRYED1F8HYTG74H
requires_load: []
target_version: 0.1.0
steps:
  - id: added-chord-sheet-render-component-chordsheetr
    order: 1
    status: done
    description: Added chord-sheet-render-component (ChordSheetR) to the architecture reference's UI dumb-views box (it was missing) and made the ScoreR-becomes-a-thinner-notation-only-layer framing explicit in the ChordFlowPlayback seam paragraph.
    files_touched: []
    blocked_by: []
    satisfies: []
  - id: updated-the-readme-chord-sheets-feature
    order: 2
    status: done
    description: "Updated the README Chord Sheets feature: retitled to 'print and play along with your songs' and added a play-along sentence (marker follows the music in time; Visual metronome / Per chord modes; same synchronized playback engine as the tablature view)."
    files_touched: []
    blocked_by: []
    satisfies: []
---
# Docs: architecture ref + README reflect ChordFlowPlayback / ChordSheetR / play-along

## Goal

Quick-ship record of 2 completed changes.

---

## Steps

| Done | # | Step | Files touched | Blocked by | Satisfies |
|---|---|---|---|---|---|
| ✅ | 1 | Added chord-sheet-render-component (ChordSheetR) to the architecture reference's UI dumb-views box (it was missing) and made the ScoreR-becomes-a-thinner-notation-only-layer framing explicit in the ChordFlowPlayback seam paragraph. | — | — | — |
| ✅ | 2 | Updated the README Chord Sheets feature: retitled to 'print and play along with your songs' and added a play-along sentence (marker follows the music in time; Visual metronome / Per chord modes; same synchronized playback engine as the tablature view). | — | — | — |
---

### Legend

| Symbol | Meaning |
|--------|---------|
| ✅ | Done |
| 🔄 | In Progress |
| 🔳 | Pending |
| ❌ | Cancelled |
