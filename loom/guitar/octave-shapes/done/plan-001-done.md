---
type: done
id: pl_01KVJ7N2RMCM2M8BJ6BDC704A1-done
title: Done — Octave shapes — implementation
status: done
created: 2026-06-20
version: 5
tags: []
parent_id: pl_01KVJ7N2RMCM2M8BJ6BDC704A1
requires_load: []
---
# Done — Octave shapes — implementation

## Step 1 — OctaveShape static class + the authored CAGED partition (RootStrings, primary-first) for all five shapes

`OctaveShape.cs` created — static class in `ChordFlow.Instruments.Guitar`, mirroring `Fretboard`/`IntervalLattice`. The sole authored datum is `RootStringsByShape` (C `{5,2}`, A `{5,3}`, G `{6,3,1}`, E `{6,4,1}`, D `{4,2}`), ordered primary-first = ascending octave. Reuses the existing `CagedShape` enum + `FretPosition` (no parallel type). Public `RootStrings(shape)` accessor.

## Step 2 — Anchor query (option c): AnchorsFor(root, shape, minFret, maxFret) via IntervalLattice / Fretboard

`AnchorsFor(root, shape, minFret, maxFret)` — option (a), one instance. The primary is anchored at its lowest occurrence ≥ `minFret` (found via `Fretboard.PositionsFor`, no second neck-walk); each later root string is placed at `abs = primaryAbs + k·12` (ascending octave), fret from `Fretboard.AbsoluteSemitone`. This octave-index anchoring is the chat-002 fix for the **D-shape trap** — the naive "any root position in the window" returns the in-window str2 unison (fret 1) instead of the +3 octave-up (fret 13). Returns empty when the root never falls on the primary string in the window. Design §3 was patched to match.

## Step 3 — Derived geometry: octave zone (anchor fret span) + CAGED boxes (string-set partition from the roots)

Derived geometry added to `OctaveShape.cs`. `Zone(root, shape, minFret, maxFret)` → `OctaveZone(MinFret, MaxFret)` = the [min,max] fret span of the anchors. `Boxes(shape)` → `IReadOnlyList<CagedBox>`: sort roots bass→treble, then partial-below `(6, maxRoot)` if `maxRoot < 6`, a main box between each consecutive root pair, partial-above `(minRoot, 1)` if `minRoot > 1`; `IsMain` only between roots. Added `readonly record struct OctaveZone(int MinFret, int MaxFret)` and `readonly record struct CagedBox(int BassString, int TrebleString, bool IsMain)`. Key-independent (pure function of the partition).

## Step 4 — Golden oracle tests: offsets at Key C, octave-zone spans, and box partitions for all five shapes

`OctaveShapeTests.cs` — 18 tests, all green (full Core suite **553 passed, 0 failed**). Golden oracle: the five offsets at Key C (C −2, A +2, G −3 & str1=str6, E +2 & str1=str6, D +3); octave-zone spans (E 8–10, C 1–3, A 3–5, G 5–8, D 10–13); box partitions for all five shapes matching the chat-002 table. Plus anchor behaviour (lands on root strings, primary = lowest occurrence, all sound the root, recurs every 12 frets, empty outside window) and the explicit **D-shape regression** (str2 = fret 13, not the in-window unison at fret 1).

## Step 5 — Ref-sync: add OctaveShape to the Instruments/Guitar/Geometry inventory in domain-model + architecture references

Ref-sync (same unit of work). `chordflow-domain-model-reference`: added an `OctaveShape` / `OctaveZone` / `CagedBox` row next to the `IntervalLattice` row (partition-only authored data, octave-index anchoring, zone, boxes; envelope/fingering/used-zone noted as caged-system's). `chordflow-architecture-reference`: added `OctaveShape` to the `Instruments/Guitar/` inventory line and to the geometry box (`tuning · Fretboard · IntervalLattice · OctaveShape`, ASCII border re-aligned).
