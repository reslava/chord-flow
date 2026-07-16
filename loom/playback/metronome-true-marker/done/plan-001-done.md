---
type: done
id: pl_01KXNY6E2FFJX323JF5XMZGQMZ-done
title: Done — Metronome-true Sheet marker — PlaybackClock + "position" event
status: done
created: 2026-07-16
version: 4
tags: []
parent_id: pl_01KXNY6E2FFJX323JF5XMZGQMZ
requires_load: []
---
# Done — Metronome-true Sheet marker — PlaybackClock + "position" event

## Step 1 — PlaybackClock in playback-component.js: named class (bar-start cache from api.score.masterBars rebuilt per load(), tick → (bar, quarterBeat) via binary search, ticksPerStep=960 param, dedupe latch reset on load()); ChordFlowPlayback composes it, subscribes api.playerPositionChanged, and emits "position" (1-based) on the existing bus; out-of-range/count-in ticks clamp per the guard.

Added `PlaybackClock` + the `"position"` event to `src/ChordFlow.Desktop/wwwroot/playback-component.js`.

- **`PlaybackClock` class** (module-level, before `create()`): tick → `(bar, quarterBeat)` (1-based) with a `ticksPerStep` constructor param (default 960 = quarter, IN3), a lazily rebuilt bar-timeline cache, binary-search lookup, and a `"bar:quarter"` dedupe latch — `step()` returns a value once per step change, null otherwise (IN1, IN4, C2).
- **Implementation finding (design's "verify at implementation" flag confirmed real):** on alphaTab v1.8.3 the score model's `masterBar.start` is the WRONG timeline — `Score.addMasterBar` sets `start += prev.isAnacrusis ? 0 : prev.calculateDuration()`, i.e. a pickup bar is zero-width in the model, while the sequencer accumulates real durations (`currentTick += masterBar.calculateDuration()`). The clock therefore caches from **`api.tickCache.masterBars`** (MasterBarTickLookup: `masterBar` / `start` / `tickDuration` — verified present in the shipped bundle), which IS the playback timeline the synth reports ticks against — anacrusis-correct by construction (IN2, within design intent: model-derived starts, never fixed 3840). Cache identity-keys on the `tickCache` object so it self-rebuilds once per loaded score with no event coordination; `reset()` (latch + cache) runs on `scoreLoaded` (IN4).
- **Engine wiring** (inside the `if (player)` block): subscribes `api.playerPositionChanged` (args verified: `{currentTick, endTick, isSeek, …}`), feeds the clock, emits `"position"` on the existing multi-subscriber bus; `position: []` added to the listeners map + header/bus docs updated (IN5).
- **Guards (C3):** ticks past the last bar-end return null (marker keeps its last position, mirroring `onBeat`'s guard); pre-score/count-in ticks clamp to the first entry (at worst one early first-quarter highlight). Additionally the emission is gated on `isPlaying` (tracked from `playerStateChanged`) because `stop()` seeks back to tick 0 — without the gate the stop echo would re-highlight bar 1 beat 1 after the marker was cleared.
- `node --check` passes.

## Step 2 — Consumers: app.js shell subscribes engine.on("position") → sheetView.onPosition(bar, quarterBeat); chord-sheets.js adds onPosition driving the marker only in markerMode === "metronome" (same scheduleByBar downbeat-cell lookup + highlightBeat), onBeat narrowed to drive only "chord" mode; both paths share lastMarkerKey.

Wired the two consumers.

- **`chord-sheets.js`**: new `onPosition(bar, quarterBeat)` — guards `markerMode !== "metronome"`, same `scheduleByBar` downbeat-cell lookup + `highlightBeat(section, row, cell, quarterBeat − 1)` as the old metronome branch, sharing `lastMarkerKey` (the mode select already resets it, so mid-playback mode switches stay clean) (IN7). `onBeat` narrowed to drive only `"chord"` mode (the per-chord segment walk unchanged); its metronome branch removed. Both exported on the handle; header + section comments updated to name the two signals (event → Per-chord, time clock → Visual-metronome).
- **`app.js`**: page-level fan-out `view.getEngine().on("position", (bar, q) => sheetView.onPosition(bar, q))` next to the existing beat fan-out — via the engine handle, no ScoreR passthrough opt (IN6). The `onBeat` fan-out comment now marks it as the EVENT signal (Now/Next, Per-chord, `beatChanged` bridge echo — all unchanged, EX2).
- `node --check` passes on both files.

## Step 3 — Verify: run the idea's Validation walk — charleston (X...--X.--------) straight + swing steps evenly 4/quarter-bar in metronome mode; Per-chord + Now/Next unchanged; pickup (\ac) song stays aligned; tempo change follows; mode switch mid-playback clean — plus a CHORDFLOW_DEVTOOLS console check that "position" emits exactly 4 steps per 4/4 bar.

Verification ran in two halves (C1):

**Rafa's visual walk** — passed ("all working good"): charleston straight + swing steps evenly in Visual-metronome mode; Per-chord + Now/Next unchanged; tempo change follows; mode switch mid-playback clean. One out-of-scope observation: the **sheet does not render the pickup (`\ac`) bar itself** — a ChordSheetBuilder (C#) model gap, excluded from this thread (EX3), captured for a follow-up thread.

**Automated CDP harness** (scratchpad `verify-position-clock.mjs` / `verify-replay.mjs`): attaches to the running app via the WebView2 remote-debugging port, dispatches a **trusted click** (Chromium autoplay policy blocks the synth's AudioContext without a user gesture — a headless `play()` reports Playing but time never advances), hand-feeds alphaTex to `window.__cfEngine`, plays for real, and asserts the emitted `"position"` sequence:

- **A — 2 bars of 4/4 whole notes** → `1:1 1:2 1:3 1:4 2:1 2:2 2:3 2:4` PASS (the event-driven signal would fire once per bar; 4 steps/bar proves metronome-trueness through sustains).
- **B — `\ac` pickup (1 quarter) + 2 full bars** → `1:1 2:1..2:4 3:1..3:4` PASS (anacrusis bar gets exactly its real length — numeric proof of the tickCache-timeline choice).
- **Replay of the same score** → both runs emit `1:1 … 2:4` PASS.

**Two defects found by the harness and fixed in `playback-component.js`:**
1. `MasterBarTickLookup` has **no `tickDuration`** on v1.8.3 — entries carried `end: NaN`, silently disabling the past-the-end guard. Fixed: use the lookup's plain `end` property (runtime-probed).
2. **End-of-playback seek echo + latch staleness**: alphaTab seeks back to tick 0 BEFORE the stopped state-change lands, so the `isPlaying` gate alone let a trailing `1:1` through — and the latch then held `"1:1"`, which would swallow the first step of a replay. Fixed: skip `isSeek` position reports (streaming reports resume within ~50ms, nothing missed) + `clock.resetLatch()` on `stopped`.

Design doc's tick-source bullet updated to the resolved facts. `node --check` + rebuild + re-run: all three checks PASS.

## Step 4 — Update the architecture ref in the same unit of work: ChordFlowPlayback composes PlaybackClock; event bus emits beat · position · stateChange · ready · finished · soundFontsListed; Sheet marker's metronome mode consumes "position".

Updated `loom/refs/chordflow-architecture-reference.md` (6 surgical patches via `loom_patch_doc`, IN8):

- **Playback engine seam (§7)**: `ChordFlowPlayback` now documented as composing **`PlaybackClock`** — tick → `(bar, quarterBeat)` against the playback tick timeline (`api.tickCache.masterBars`, explicitly NOT the score model's `masterBar.start` which zero-widths a pickup bar on v1.8.3), `ticksPerStep` create-opt, the `"position"` bus event as the time-linear signal for metronome-shaped consumers vs the event-driven `"beat"` for event-shaped ones.
- Bus subscription list: `engine.on("beat" | "position" | …)`.
- Practice-page diagram: engine box gains the `composes PlaybackClock` line; `emits:` line gains `position`; the fan-out note now includes the clock's `"position"` steps.
- **Chord-sheet playback**: Visual metronome mode documented as following the `"position"` clock (even through sustains/silences); Per chord stays event-driven.
- §2 solution-shape tree: `playback-component.js` entry mentions the composed PlaybackClock.
