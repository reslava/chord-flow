---
type: idea
id: id_01KXNW5MSK9CMN57M9B4QEFP23
title: Metronome-true Sheet marker — a time-based playback clock
status: done
created: 2026-07-16
version: 1
tags: []
parent_id: null
requires_load: [rf_01KTSAPAT132QTEY5BEPRKS3MB]
---
# Metronome-true Sheet marker — a time-based playback clock

## Problem

The Sheet view's **Visual metronome** marker mode is not metronomic. The engine's `beat` event comes from
alphaTab's `activeBeatsChanged` (`playback-component.js`), and the emitted beat number is **`beat.index` — the
index of the rendered note/rest event within the bar, not a quarter-note position**. The clock is
**event-driven, not time-driven**: for a sparse comping like charleston (`X...--X.--------`) the events fire at
note/rest boundaries, so the marker visibly accelerates through the silences and drags through the long notes —
"a weird sensation … really annoying" in Sheet view (found while validating `ui/harmony-controls-r`; reproduced
with straight and swing feel alike).

Two things this is **not**:

- Not the alphaTab *cursor* — that's already invisible in Sheet view (the score surface is collapsed); the
  jitter is our own marker stepping on the irregular signal.
- Not fixable by hiding/re-styling cursors — the score view's native cursor easing is inherent to notation
  engraving (verified on alphaTab v1.8.3, v1.9.0, and even Guitar Pro: engraved widths are not proportional to
  durations, so a time-linear cursor over duration-nonlinear spacing always looks uneven). That part stays as is.

## Idea

Give the Visual-metronome mode a **time-based clock**. alphaTab exposes **`playerPositionChanged`** — a
continuous tick/time position that is time-linear regardless of how the rhythm is engraved:

1. **Engine** (`playback-component.js`): subscribe `api.playerPositionChanged`, derive `(bar, quarterBeat)`
   from `currentTick` (960 ticks per quarter in alphaTab's MIDI model), and emit a new **`"position"`** bus
   event alongside the existing `"beat"` — deduped, so consumers see one event per quarter step, not ~60 Hz.
2. **Sheet view** (`chord-sheets.js`): **Visual metronome** mode consumes `"position"` → the marker steps once
   per quarter, perfectly even, silence or note. **Per chord** mode stays on the event-driven signal (chord
   onsets *are* events — correct as-is).
3. **Untouched**: the score view's native cursor (inherent easing accepted), Now/Next (chord changes are
   events), the bridge (purely a JS playback concern — no C# change).

## Scope

- **In**: the `"position"` engine event (tick → bar/quarter derivation, robust to pickup/`\ac` bars — bar
  starts from the score model, not a fixed 3840 division); the Sheet marker's metronome mode switching to it;
  the shell fan-out wiring.
- **Out**: the score view's native cursor behavior; Now/Next signal; any C#/bridge change; alphaTab
  version upgrade (we're on v1.8.3; the `next.alphatab.net` cursors API is future-version material).

## Validation

- Play charleston (`X...--X.--------`) in Sheet view, Visual metronome mode: the marker steps evenly —
  4 steps per 4/4 bar, one per quarter — through notes, sustains and silences alike; straight AND swing.
- Per-chord mode and Now/Next behave exactly as before.
- A song with a pickup (`\ac`) bar keeps the marker aligned after the anacrusis.
- Tempo change mid-session: the step rate follows the new tempo (ticks are tempo-independent; the clock is).