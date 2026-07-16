---
type: plan
id: pl_01KXNY6E2FFJX323JF5XMZGQMZ
title: Metronome-true Sheet marker — PlaybackClock + "position" event
status: done
created: 2026-07-16
updated: 2026-07-16
version: 1
design_version: 4
req_version: 1
tags: []
parent_id: de_01KXNW6QFHCS15HWPXJF66VS54
requires_load: []
target_version: 0.1.0
steps:
  - id: playbackclock-position-engine-event
    order: 1
    status: done
    description: "PlaybackClock in playback-component.js: named class (bar-start cache from api.score.masterBars rebuilt per load(), tick → (bar, quarterBeat) via binary search, ticksPerStep=960 param, dedupe latch reset on load()); ChordFlowPlayback composes it, subscribes api.playerPositionChanged, and emits \"position\" (1-based) on the existing bus; out-of-range/count-in ticks clamp per the guard."
    files_touched: [src/ChordFlow.Desktop/wwwroot/playback-component.js]
    blocked_by: []
    satisfies: [IN1, IN2, IN3, IN4, IN5, C2, C3]
  - id: shell-fan-out-sheet-marker-mode
    order: 2
    status: done
    description: "Consumers: app.js shell subscribes engine.on(\"position\") → sheetView.onPosition(bar, quarterBeat); chord-sheets.js adds onPosition driving the marker only in markerMode === \"metronome\" (same scheduleByBar downbeat-cell lookup + highlightBeat), onBeat narrowed to drive only \"chord\" mode; both paths share lastMarkerKey."
    files_touched: [src/ChordFlow.Desktop/wwwroot/app.js, src/ChordFlow.Desktop/wwwroot/chord-sheets.js]
    blocked_by: [playbackclock-position-engine-event]
    satisfies: [IN6, IN7]
  - id: validation-walk-devtools-check
    order: 3
    status: done
    description: "Verify: run the idea's Validation walk — charleston (X...--X.--------) straight + swing steps evenly 4/quarter-bar in metronome mode; Per-chord + Now/Next unchanged; pickup (\\ac) song stays aligned; tempo change follows; mode switch mid-playback clean — plus a CHORDFLOW_DEVTOOLS console check that \"position\" emits exactly 4 steps per 4/4 bar."
    files_touched: []
    blocked_by: [shell-fan-out-sheet-marker-mode]
    satisfies: [C1]
  - id: architecture-ref-sync
    order: 4
    status: done
    description: "Update the architecture ref in the same unit of work: ChordFlowPlayback composes PlaybackClock; event bus emits beat · position · stateChange · ready · finished · soundFontsListed; Sheet marker's metronome mode consumes \"position\"."
    files_touched: [loom/refs/chordflow-architecture-reference.md]
    blocked_by: [shell-fan-out-sheet-marker-mode]
    satisfies: [IN8]
---
# Metronome-true Sheet marker — PlaybackClock + "position" event

## Goal

Give the Sheet view's Visual-metronome marker a time-based clock: a named `PlaybackClock` class composed inside `ChordFlowPlayback` derives `(bar, quarterBeat)` from alphaTab's `playerPositionChanged` ticks (bar starts from the score model so pickup bars stay aligned; `ticksPerStep = 960` constructor param), deduped and emitted as a new `"position"` event on the engine's existing multi-subscriber bus. The shell fans it out to the Sheet view, whose metronome marker mode switches to the even, time-linear signal while Per-chord mode, Now/Next, and the score cursor stay on the event-driven `"beat"`. JS-only — no C#/bridge change.

---

## Steps

| Done | # | Step | Files touched | Blocked by | Satisfies |
|---|---|---|---|---|---|
| ✅ | 1 | PlaybackClock in playback-component.js: named class (bar-start cache from api.score.masterBars rebuilt per load(), tick → (bar, quarterBeat) via binary search, ticksPerStep=960 param, dedupe latch reset on load()); ChordFlowPlayback composes it, subscribes api.playerPositionChanged, and emits "position" (1-based) on the existing bus; out-of-range/count-in ticks clamp per the guard. | src/ChordFlow.Desktop/wwwroot/playback-component.js | — | IN1, IN2, IN3, IN4, IN5, C2, C3 |
| ✅ | 2 | Consumers: app.js shell subscribes engine.on("position") → sheetView.onPosition(bar, quarterBeat); chord-sheets.js adds onPosition driving the marker only in markerMode === "metronome" (same scheduleByBar downbeat-cell lookup + highlightBeat), onBeat narrowed to drive only "chord" mode; both paths share lastMarkerKey. | src/ChordFlow.Desktop/wwwroot/app.js, src/ChordFlow.Desktop/wwwroot/chord-sheets.js | playbackclock-position-engine-event | IN6, IN7 |
| ✅ | 3 | Verify: run the idea's Validation walk — charleston (X...--X.--------) straight + swing steps evenly 4/quarter-bar in metronome mode; Per-chord + Now/Next unchanged; pickup (\ac) song stays aligned; tempo change follows; mode switch mid-playback clean — plus a CHORDFLOW_DEVTOOLS console check that "position" emits exactly 4 steps per 4/4 bar. | — | shell-fan-out-sheet-marker-mode | C1 |
| ✅ | 4 | Update the architecture ref in the same unit of work: ChordFlowPlayback composes PlaybackClock; event bus emits beat · position · stateChange · ready · finished · soundFontsListed; Sheet marker's metronome mode consumes "position". | loom/refs/chordflow-architecture-reference.md | shell-fan-out-sheet-marker-mode | IN8 |
---

### Legend

| Symbol | Meaning |
|--------|---------|
| ✅ | Done |
| 🔄 | In Progress |
| 🔳 | Pending |
| ❌ | Cancelled |
