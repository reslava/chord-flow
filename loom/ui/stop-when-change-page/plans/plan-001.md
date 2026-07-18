---
type: plan
id: pl_01KXMZ37JDP9V055BEPJ4P0P1H
title: Stop all playback on page change
status: done
created: 2026-07-16
updated: 2026-07-16
version: 1
design_version: 1
tags: []
parent_id: null
requires_load: []
target_version: 0.1.0
actual_release: 0.15.0
steps:
  - id: added-a-live-player-engine-registry
    order: 1
    status: done
    description: "Added a live player-engine registry to ChordFlowPlayback (playback-component.js): each player-mode engine self-registers on create() and drops out on dispose(); exposed ChordFlowPlayback.stopAll() that stops every registered engine."
    files_touched: []
    blocked_by: []
    satisfies: []
  - id: wired-app-js-show-the-view
    order: 2
    status: done
    description: Wired app.js show() (the view toggle) to call ChordFlowPlayback.stopAll() before switching views, so changing pages silences audio left playing on the page you leave — covers Practice, Content preview, Chord Sheets, and any future sound surface with zero per-view wiring.
    files_touched: []
    blocked_by: []
    satisfies: []
  - id: verified-solution-builds-clean-full-922
    order: 3
    status: done
    description: "Verified: solution builds clean, full 922-test suite passes, both edited JS files pass node --check."
    files_touched: []
    blocked_by: []
    satisfies: []
---
# Stop all playback on page change

## Goal

Quick-ship record of 3 completed changes.

---

## Steps

| Done | # | Step | Files touched | Blocked by | Satisfies |
|---|---|---|---|---|---|
| ✅ | 1 | Added a live player-engine registry to ChordFlowPlayback (playback-component.js): each player-mode engine self-registers on create() and drops out on dispose(); exposed ChordFlowPlayback.stopAll() that stops every registered engine. | — | — | — |
| ✅ | 2 | Wired app.js show() (the view toggle) to call ChordFlowPlayback.stopAll() before switching views, so changing pages silences audio left playing on the page you leave — covers Practice, Content preview, Chord Sheets, and any future sound surface with zero per-view wiring. | — | — | — |
| ✅ | 3 | Verified: solution builds clean, full 922-test suite passes, both edited JS files pass node --check. | — | — | — |
---

### Legend

| Symbol | Meaning |
|--------|---------|
| ✅ | Done |
| 🔄 | In Progress |
| 🔳 | Pending |
| ❌ | Cancelled |
