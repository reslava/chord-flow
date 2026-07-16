---
type: req
id: rq_01KXNY5MWJH30RZ8M02P35BQX5
title: Metronome-true Sheet marker — design — Requirements
status: locked
created: 2026-07-16
updated: 2026-07-16
version: 1
design_version: 4
tags: []
parent_id: de_01KXNW6QFHCS15HWPXJF66VS54
requires_load: []
---
# Metronome-true Sheet marker — design — Requirements

### ✅ Included

- `IN1` A named **`PlaybackClock`** class in `playback-component.js`, composed by `ChordFlowPlayback`, deriving `(bar, quarterBeat)` (1-based) from alphaTab `playerPositionChanged` ticks.
- `IN2` Bar starts derived from the **score model** (master bars), cached once per `load()` — correct across pickup (`\ac`) bars; lookup by binary search over the cached starts.
- `IN3` Step divisor as a constructor param (`ticksPerStep = 960`, default = quarter).
- `IN4` Dedupe latch: at most one emission per `(bar, quarterBeat)` change; latch reset + cache rebuild on `load()`.
- `IN5` A new **`"position"`** event on the engine's existing multi-subscriber `on(event, cb)` bus, alongside the unchanged `"beat"`.
- `IN6` Shell fan-out in `app.js`: `engine.on("position", …)` → `sheetView.onPosition(bar, quarterBeat)`.
- `IN7` Sheet view `onPosition(bar, quarterBeat)` drives the marker **only** when `markerMode === "metronome"`; `onBeat` drives **only** `"chord"` mode; both share `lastMarkerKey` so mid-playback mode switching stays clean.
- `IN8` Architecture ref updated in the same unit of work: `ChordFlowPlayback` composes `PlaybackClock`; bus emits `beat · position · …`.

### ❌ Excluded

- `EX1` The score view's native cursor behavior (inherent engraving easing accepted).
- `EX2` Any change to the Now/Next signal or the `beatChanged` bridge echo (they stay on `"beat"`).
- `EX3` Any C#/bridge change.
- `EX4` alphaTab version upgrade (stay on v1.8.3).
- `EX5` Sub-beat resolutions, per-consumer resolutions, or count-in tick emission (beyond the divisor param).

### ⛓ Constraints

- `C1` JS-only change — no C# tests; verify via the idea's Validation walk (charleston straight + swing, pickup song, tempo change, mode switch mid-playback) plus a `CHORDFLOW_DEVTOOLS` console check that `"position"` emits exactly 4 steps per 4/4 bar.
- `C2` `"position"` payload is `(bar, quarterBeat)`, 1-based, matching `"beat"`'s convention.
- `C3` Out-of-range ticks (beyond the cached starts) keep the last marker, mirroring `onBeat`'s guard; count-in positions clamp to the first bar (at worst one early highlight — verify visually).