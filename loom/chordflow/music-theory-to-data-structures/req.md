---
type: req
id: rq_01KTM0Y9B6JMQNJDA8THQG4WCR
title: Music-Theory Domain Model — Requirements
status: locked
created: "2026-06-08T00:00:00.000Z"
updated: 2026-06-08
version: 1
tags: []
parent_id: null
requires_load: []
---
# Music-Theory Domain Model — Requirements

### ✅ Included

- `IN1` — `PitchClass` stays an `int 0..11`; a pure `NoteSpeller(PitchClass, Key) → name` resolves sharp/flat spelling per key (promoted out of the renderer's hardcoded arrays).
- `IN2` — First-class `Scale(Tonic, int[] Intervals)`; `Transposer` refactored to consume a `Scale` instead of owning the offset arrays.
- `IN3` — `Quality` enum **backed by interval sets** for the 8 v1 qualities (see `C5`); chord tones derived from these.
- `IN4` — Two distinct degree concepts: `ScaleDegree` (key-relative, for progressions) and `ChordTone` (chord-relative interval, for voicings + lead targets).
- `IN5` — Diatonic chord generator `BuildDiatonicChord(Scale, ScaleDegree) → Chord`.
- `IN6` — `Voicing` remains an `IReadOnlyList<FretPosition>`, plus optional diagram metadata (`BarreFret?`, muted strings, `FirstFret`) for the alphaTex `\chord (...)` directive.
- `IN7` — Voicing selection as a strategy (`Difficulty → shape chooser`); the existing algorithmic shell shape becomes the first Beginner strategy.
- `IN8` — Rhythm migrated to a positional **tick grid**: `RhythmEvent(Position, Length, Stroke, Accent)` + `RhythmPattern(Name, Events, TimeSignature)`, ticks at a fixed **48 PPQ**.
- `IN9` — `TimeSignature` type (4/4 only in v1); bar length in ticks derived from it × PPQ.
- `IN10` — `Feel { Straight, Swing, Shuffle, Triplet }` applied as a playback-time transform; `AccentPattern` and `Stroke` as separate composable overlays.
- `IN11` — Pickup/anacrusis modeled as its own short leading measure (not a negative position).
- `IN12` — A **grid → alphaTex quantizer** inside the `Rendering/` seam: walks events in tick order, emits `:N` tokens, rests for gaps, ties across the grid.
- `IN13` — The three MVP rhythm patterns (beat-1, beat-1+3, quarters) ported to the tick model.
- `IN14` — Lead `TargetZone(ChordTone, Importance)` domain layer; guide tones (3 & 7) derived from interval sets and resolvable to fretboard positions.
- `IN15` — `Exercise` unifying object + the end-to-end engine pipeline rewired onto the new model.

### ❌ Excluded

- `EX1` — 32nd-note / quintuplet support (PPQ stays 48; revisit only if a use case demands it).
- `EX2` — Compound (6/8) and odd (5/4, 7/8) meters; 4/4 only for v1.
- `EX3` — One-time data migration of persisted MVP exercises — regenerate from definitions; wiping the dev DB is acceptable.
- `EX4` — Accuracy / pitch detection.
- `EX5` — Lead-training fretboard **UI** view (domain types only this thread).
- `EX6` — Persistence schema redesign and Photino/JS UI work beyond the minimum needed to keep the app rendering.

### ⛓ Constraints

- `C1` — PPQ is fixed at **48** (divisible by 4 → 16th = 12 ticks, by 3 → eighth-triplet = 16 ticks) so subdivisions compose in one grid.
- `C2` — `AlphaTexRenderer` stays the **only** alphaTex-aware code; quantizer + spelling live in the Domain/Rendering seam.
- `C3` — Domain kernel stays pure (no I/O) and unit-tested.
- `C4` — `Feel` is never baked into a `RhythmPattern`; spelling is never stored — both are derived.
- `C5` — v1 quality interval sets: `Maj {0,4,7}`, `Min {0,3,7}`, `Dom7 {0,4,7,10}`, `Maj7 {0,4,7,11}`, `Min7 {0,3,7,10}`, `m7b5 {0,3,6,10}`, `Dim {0,3,6}`, `Aug {0,4,8}`.
- `C6` — Build on the existing MVP types where they are already correct; only the rhythm layer is a true migration/replacement.
- `C7` — After each migration step the solution must still build and tests stay green (new tick types added in parallel before the old `Beat` model is removed).
