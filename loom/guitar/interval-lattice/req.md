---
type: req
id: rq_01KVGJ226G4ZW439HVJYN5M0X4
title: Interval lattice — fretboard interval positions (guitar projection of intervals) — Requirements
status: locked
created: 2026-06-19
updated: 2026-06-19
version: 4
tags: []
parent_id: id_01KVDEEY1959RD07H63R5PFMVZ
requires_load: []
---
# Interval lattice — fretboard interval positions (guitar projection of intervals) — Requirements

Authoritative scope for the interval-lattice thread — extracted from `interval-lattice-idea.md`, `interval-lattice-design.md`, and chat-001. D1–D3 settled (chat-001): single-source tuning in `Fretboard` (`IN8`), pitch-class+window match (`IN6`), ship `LatticeInterval` (`IN5`). UI page is delivered in `ui/intervals-scales` (`EX6`).

### ✅ Included

- `IN1` Create `Instruments/Guitar/Geometry/IntervalLattice.cs` — a **static class**, namespace `ChordFlow.Instruments.Guitar`, pure (no I/O, no UI), unit-tested.
- `IN2` Octave-preserving absolute coordinate authored in **`Fretboard`** (`StringSemitoneBase {_,24,19,15,10,5,0}` + `AbsoluteSemitone(string,fret)`, alphaTab numbering 1 = high E … 6 = low E); `IntervalLattice.Absolute(FretPosition) → int` is a thin delegate to it (no tuning authored in the lattice).
- `IN3` Signed `Distance(FretPosition origin, FretPosition target) → int` = `Absolute(target) − Absolute(origin)` — the **canonical** value; labels are views over it.
- `IN4` Two label views, **both via `IntervalSpeller.Name`** (no re-authored vocabulary): `PitchClassLabel(int distance)` → `1…7` (mod-12) and an unfolded+octave+direction view (`8/9/11/15…`).
- `IN5` `readonly record struct LatticeInterval(int Semitones, string Label, int Octaves, int Direction)` as the unfolded-view carrier *(D3 — recommended: ship now)*.
- `IN6` Consumer query `PositionsOfInterval(FretPosition root, int semitones, int minFret, int maxFret) → IReadOnlyList<FretPosition>` — **pitch-class match within the fret window** *(D2)*, implemented **on top of** `Fretboard.PositionsFor` (no second neck-walk).
- `IN7` `LabelAt(FretPosition root, FretPosition target) → LatticeInterval` convenience (powers the dogfood UI).
- `IN8` Single-source the tuning *(D1 — settled)*: the absolute base in `Fretboard` is authoritative; derive `Fretboard.OpenPitchClass`/`PitchClassAt` from it so no second tuning table exists.
- `IN9` Unit tests: absolute-coordinate ↔ `Fretboard` consistency for every (string, fret); signed `Distance` correctness **including across the B string** (3→2 = 4, not 5); both label views; and the **golden check** that `PositionsOfInterval` reproduces the five octave-shape offsets (C −2, A +2, G −3, E +2, D +3, string-1 = string-6).
- `IN10` Ref-sync in the **same unit of work**: `chordflow-domain-model-reference` note that `IntervalSpeller.Name` is the vocabulary the guitar `IntervalLattice` projects; add `IntervalLattice` to the `Instruments/Guitar/Geometry/` inventory in domain-model + `chordflow-architecture-reference`.

### ❌ Excluded

- `EX1` Reverse `label → semitone` parser — no consumer yet.
- `EX2` A first-class `Interval` value type in `Domain` — deferred until needed.
- `EX3` Alternate tunings (`Fretboard` is fixed-tuning in v1).
- `EX4` Scale / arpeggio overlays (same lattice, additive once chords work).
- `EX5` The octave-shapes CAGED partition and chord-quality placement — owned by `octave-shapes` / `caged-system` (they *consume* this).
- `EX6` The dogfood **UI page implementation** itself — delivered in the `ui/intervals-scales` thread; this thread provides the Core query it renders.
- `EX7` Any change to the `IntervalSpeller` vocabulary or its data.

### ⛓ Constraints

- `C1` Consume `IntervalSpeller.Name`; **do not** re-author the semitone→label table guitar-side (single source of vocabulary).
- `C2` `Distance` is the canonical signed integer; labels are pure derived views — no separately stored label state.
- `C3` Respect the B-string irregularity (3→2 = 4 semitones) via the single tuning table — no uniform-+5 shortcut anywhere.
- `C4` Dependency direction: the lattice lives in `Instruments` and may depend on `Domain`, never the reverse — the `NetArchTest` Domain-edge guard stays green.
- `C5` `PositionsOfInterval` reuses `Fretboard.PositionsFor` — no duplicated neck traversal.
- `C6` Pure and deterministic: no I/O, no UI; fully unit-tested.
- `C7` Standing dogfood rule: the feature is visually validated by the intervals/scales fretboard UI page (`ui/intervals-scales`) before building chord-qualities / caged on top.
- `C8` Sequencing: depends on `intervals` + `instrument-boundary`; depended upon by `octave-shapes` + `caged-system`.