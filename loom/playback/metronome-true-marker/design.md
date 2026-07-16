---
type: design
id: de_01KXNW6QFHCS15HWPXJF66VS54
title: Metronome-true Sheet marker — design
status: done
created: 2026-07-16
updated: 2026-07-16
version: 5
idea_version: 1
tags: []
parent_id: id_01KXNW5MSK9CMN57M9B4QEFP23
requires_load: []
---
# Metronome-true Sheet marker — design

## Decision

The Visual-metronome marker gets a **time-based clock** — **`PlaybackClock`**, a named component composed inside `ChordFlowPlayback` (decided in chat-001) — derived from alphaTab's `playerPositionChanged`
(tick position), emitted as a new engine bus event. The event-driven `"beat"` signal stays for everything that
is genuinely event-shaped (Per-chord marker, Now/Next, the score cursor's own machinery).

## The signal chain (today → target)

```
today:   api.activeBeatsChanged ──► engine "beat" (bar, beat.index+1)  ──► shell fan-out ──► sheet marker (BOTH modes)
target:  api.activeBeatsChanged ──► engine "beat"      (unchanged)     ──► Per-chord mode, Now/Next, beatChanged bridge echo
         api.playerPositionChanged ► engine "position" (bar, quarter)  ──► Visual-metronome mode
```

## Engine — `playback-component.js`

- **`PlaybackClock` — the named component.** The tick → (bar, quarterBeat) derivation, the bar-start cache,
  and the dedupe latch live in a `PlaybackClock` class inside `playback-component.js`, composed by
  `ChordFlowPlayback` — not a standalone module: it has no UI and no independent lifecycle; its only inputs
  (the api, the score model, the load/reset moments) are things the engine already owns. **The reuse contract
  is the bus event, not the file layout** — a future metronome widget subscribes via
  `engine.on("position", …)` with zero new wiring; extracting a standalone module later is additive. The step
  divisor is a constructor param (`ticksPerStep = 960`, default = quarter) so a future sub-beat consumer is a
  parameter change, not a refactor.
- Subscribe `api.playerPositionChanged` (fires continuously during playback with the current tick/time; on
  alphaTab **v1.8.3** the event args carry `currentTick` — verify the exact arg shape at implementation, it's
  the documented player position event).
- **Tick → (bar, quarterBeat)**: alphaTab's MIDI model uses **960 ticks per quarter**. Do NOT divide by a fixed
  3840/bar: a pickup (`\ac`) bar is shorter (the `domain/anacrusis` thread ships real pickup bars), so bar
  boundaries must come from the **playback model** — *(resolved at implementation)* NOT `masterBar.start`:
  on v1.8.3 the score model zero-widths a pickup bar (`start += prev.isAnacrusis ? 0 : duration`), so it
  drifts off the synth for any `\ac` score. The honest source is **`api.tickCache.masterBars`** (each entry:
  `masterBar`/`start`/`end` in real playback ticks — no `tickDuration` on this build), cached once per loaded
  score (identity-keyed on the tickCache object). Lookup = binary
  search over the cached starts; `quarterBeat = floor((tick − barStart) / 960) + 1`.
- **Dedupe before emitting**: `playerPositionChanged` fires at animation rate. Track the last emitted
  `(bar, quarterBeat)` and `emit("position", bar, quarterBeat)` (1-based, matching `"beat"`'s convention) only
  on change — consumers see at most one event per quarter step.
- Reset the dedupe latch + rebuild the bar-start cache on `load()`; nothing to do on stop (the next play
  re-emits from its first position).
- The `"position"` bus rides the existing multi-subscriber `on(event, cb)` mechanism — a new event name, zero
  changes to the existing buses. Create-time sugar (`onPosition`) optional; the shell subscribes via `on()`.

## Consumers

- **Shell (`app.js`)**: alongside the existing beat fan-out, subscribe `view.getEngine().on("position", (bar, q) =>
  sheetView.onPosition(bar, q))`. (ScoreR doesn't need a passthrough opt — the engine handle is already exposed
  via `getEngine()` for exactly this kind of page-level wiring.)
- **Sheet view (`chord-sheets.js`)**: new `onPosition(bar, quarterBeat)` — drives the highlight **only when
  `markerMode === "metronome"`**: same cell lookup as today (`scheduleByBar` downbeat entry → the bar's cell),
  then `view.highlightBeat(section, row, cell, quarterBeat − 1)`. `onBeat` keeps driving **only** the
  `"chord"` mode (the active-segment walk is unchanged). The mode select already resets `lastMarkerKey`; both
  paths share it, so switching modes mid-playback stays clean.
- **Unchanged**: Now/Next (`"beat"` — chord onsets are events), the `beatChanged` bridge echo, the score view's
  native cursor, all C# code.

## Edge cases

- **Pickup (`\ac`) bar**: handled by the model-derived bar starts (the whole reason for them).
- **Count-in**: alphaTab positions during count-in are before tick 0 / the first bar — the binary search clamps
  to bar 0; the dedupe latch means at worst one early highlight of bar 1's first quarter. Acceptable; verify
  visually.
- **Swing (`\tf`)**: triplet feel changes *when* notes sound within the beat, not where quarters fall — the
  quarter clock is unaffected (validated in the idea's scenarios).
- **Tempo changes**: ticks are tempo-independent; `playerPositionChanged` timing follows the synth, so the
  step rate follows tempo for free.
- **Out-of-range bar** (tick beyond the cached starts): keep the last marker, mirroring today's out-of-range
  guard in `onBeat`.

## Why not …

- **Fixed 3840-tick bars**: breaks after any pickup bar; the model walk costs one pass per load.
- **A JS `setInterval` clock**: drifts against the synth, breaks on tempo change/count-in/seek — the synth's
  own position is the only honest clock.
- **Hiding/re-styling the native cursor** (the `next.alphatab.net` cursors API): irrelevant to the Sheet
  marker (the cursor is already hidden there) and doesn't fix the signal.

## Test/verify notes

- JS-only change — no C# tests. Verify with the idea's Validation walk (charleston straight + swing, pickup
  song, tempo change, mode switching mid-playback), plus a `CHORDFLOW_DEVTOOLS` console check that
  `"position"` emits exactly 4 steps per 4/4 bar.
- Architecture ref: update the ChordFlowPlayback event-bus description (`emits: beat · position · …`) + name
  the composed `PlaybackClock` component in the
  same unit of work (CLAUDE-LOCAL reference-doc sync).