---
type: plan
id: pl_01KVGJYB6CSG4XS6DDJYB90NBW
title: Interval lattice — implementation
status: done
created: 2026-06-19
updated: 2026-06-19
version: 1
design_version: 1
req_version: 4
tags: []
parent_id: de_01KVGJ0P3FC7P36EQ0ZN6FJGEB
requires_load: []
target_version: 0.1.0
steps:
  - id: single-source-tuning-in-fretboard
    order: 1
    status: done
    description: "Single-source the tuning in Fretboard: author the octave-preserving absolute base and derive the mod-12 lookups from it"
    files_touched: [src/ChordFlow.Core/Instruments/Guitar/Geometry/Fretboard.cs, tests/ChordFlow.Core.Tests/FretboardTuningTests.cs]
    blocked_by: []
    satisfies: [IN8, IN2, C3]
  - id: intervallattice-core-absolute-distance
    order: 2
    status: done
    description: IntervalLattice core — static class with Absolute (delegate) and signed Distance
    files_touched: [src/ChordFlow.Core/Instruments/Guitar/Geometry/IntervalLattice.cs, tests/ChordFlow.Core.Tests/IntervalLatticeTests.cs]
    blocked_by: []
    satisfies: [IN1, IN2, IN3, C2, C4, C6]
  - id: label-views-latticeinterval
    order: 3
    status: done
    description: Label views over the canonical distance, both via IntervalSpeller.Name; add the LatticeInterval record
    files_touched: [src/ChordFlow.Core/Instruments/Guitar/Geometry/LatticeInterval.cs, src/ChordFlow.Core/Instruments/Guitar/Geometry/IntervalLattice.cs, tests/ChordFlow.Core.Tests/IntervalLatticeTests.cs]
    blocked_by: []
    satisfies: [IN4, IN5, C1]
  - id: positionsofinterval-labelat
    order: 4
    status: done
    description: "Consumer queries: PositionsOfInterval (pitch-class + window, on Fretboard.PositionsFor) and LabelAt"
    files_touched: [src/ChordFlow.Core/Instruments/Guitar/Geometry/IntervalLattice.cs, tests/ChordFlow.Core.Tests/IntervalLatticeTests.cs]
    blocked_by: []
    satisfies: [IN6, IN7, C5]
  - id: golden-octave-shape-validation
    order: 5
    status: done
    description: "Golden oracle test: the lattice reproduces the five octave-shape root offsets"
    files_touched: [tests/ChordFlow.Core.Tests/IntervalLatticeTests.cs]
    blocked_by: []
    satisfies: [IN9]
  - id: ref-sync
    order: 6
    status: done
    description: "Ref-sync: domain-model + architecture references"
    files_touched: [loom/refs/chordflow-domain-model-reference.md, loom/refs/chordflow-architecture-reference.md]
    blocked_by: []
    satisfies: [IN10]
---
# Interval lattice — implementation

## Goal

Implement `IntervalLattice` — the base guitar interval primitive that projects the `[[intervals]]` vocabulary onto the fretboard. Single-source the tuning by making the octave-preserving absolute coordinate authoritative in `Fretboard` (deriving the existing mod-12 pitch-class lookups from it), then build the static `IntervalLattice` over it: signed `Distance`, two label views (`PitchClassLabel` and the unfolded `LatticeInterval`) both via `IntervalSpeller.Name` with no re-authored vocabulary, and the `PositionsOfInterval` (pitch-class + fret window, on top of `Fretboard.PositionsFor`) + `LabelAt` consumer queries. Validate with unit tests including the headline golden check that the lattice reproduces the five octave-shape root offsets (the unison/octave special case). Pure `ChordFlow.Core` (no I/O, no UI; the `Domain ↛ Instruments` arch guard stays green). The dogfood UI page is delivered separately in `ui/intervals-scales`.

---

## Steps

| Done | # | Step | Files touched | Blocked by | Satisfies |
|---|---|---|---|---|---|
| ✅ | 1 | Single-source the tuning in Fretboard: author the octave-preserving absolute base and derive the mod-12 lookups from it | src/ChordFlow.Core/Instruments/Guitar/Geometry/Fretboard.cs, tests/ChordFlow.Core.Tests/FretboardTuningTests.cs | — | IN8, IN2, C3 |
| ✅ | 2 | IntervalLattice core — static class with Absolute (delegate) and signed Distance | src/ChordFlow.Core/Instruments/Guitar/Geometry/IntervalLattice.cs, tests/ChordFlow.Core.Tests/IntervalLatticeTests.cs | — | IN1, IN2, IN3, C2, C4, C6 |
| ✅ | 3 | Label views over the canonical distance, both via IntervalSpeller.Name; add the LatticeInterval record | src/ChordFlow.Core/Instruments/Guitar/Geometry/LatticeInterval.cs, src/ChordFlow.Core/Instruments/Guitar/Geometry/IntervalLattice.cs, tests/ChordFlow.Core.Tests/IntervalLatticeTests.cs | — | IN4, IN5, C1 |
| ✅ | 4 | Consumer queries: PositionsOfInterval (pitch-class + window, on Fretboard.PositionsFor) and LabelAt | src/ChordFlow.Core/Instruments/Guitar/Geometry/IntervalLattice.cs, tests/ChordFlow.Core.Tests/IntervalLatticeTests.cs | — | IN6, IN7, C5 |
| ✅ | 5 | Golden oracle test: the lattice reproduces the five octave-shape root offsets | tests/ChordFlow.Core.Tests/IntervalLatticeTests.cs | — | IN9 |
| ✅ | 6 | Ref-sync: domain-model + architecture references | loom/refs/chordflow-domain-model-reference.md, loom/refs/chordflow-architecture-reference.md | — | IN10 |
---

### Legend

| Symbol | Meaning |
|--------|---------|
| ✅ | Done |
| 🔄 | In Progress |
| 🔳 | Pending |
| ❌ | Cancelled |

<!-- step:single-source-tuning-in-fretboard -->
### Step 1 — Single-source tuning in Fretboard

Add `StringSemitoneBase = {_,24,19,15,10,5,0}` (alphaTab numbering, 1=high E…6=low E) and `AbsoluteSemitone(int string, int fret)` to `Fretboard`. Re-derive `PitchClassAt`/`OpenPitchClass` from the absolute base (`pc = (4 + AbsoluteSemitone) % 12`, 4 = open low-E PC) so no second tuning table exists. Test: `PitchClassAt` behaviour unchanged for every (string, fret); the B-string +4 step (string 3→2) is encoded (`15 → 19`).

<!-- step:intervallattice-core-absolute-distance -->
### Step 2 — IntervalLattice core: Absolute + Distance

Create static `IntervalLattice` in `ChordFlow.Instruments.Guitar`. `Absolute(FretPosition)` delegates to `Fretboard.AbsoluteSemitone`. `Distance(FretPosition origin, FretPosition target) = Absolute(target) − Absolute(origin)` — signed, the canonical value. Tests: distance across the B string (string 3→2 fret 0 = 4, not 5), descending/negative distances, two-octave (string 6→1 same fret = 24).

<!-- step:label-views-latticeinterval -->
### Step 3 — Label views + LatticeInterval

Add `readonly record struct LatticeInterval(int Semitones, string Label, int Octaves, int Direction)`. `PitchClassLabel(int d) = IntervalSpeller.Name(((d % 12) + 12) % 12)` → 1…7. `Describe(int d) = LatticeInterval(d, IntervalSpeller.Name(|d|), |d|/12, sign(d))` → 8/9/11/15…. No re-authored table — all labels route through `IntervalSpeller.Name`. Tests cover 1…7, the unfolded 8/9/15, and descending direction.

<!-- step:positionsofinterval-labelat -->
### Step 4 — PositionsOfInterval + LabelAt

`PositionsOfInterval(FretPosition root, int semitones, int minFret, int maxFret) → IReadOnlyList<FretPosition>`: pitch-class match within the window, implemented on top of `Fretboard.PositionsFor(rootPc + semitones)` filtered to [minFret, maxFret] — no second neck-walk. `LabelAt(FretPosition root, FretPosition target) = Describe(Distance(root, target))`. Tests for window bounds and multiple in-range octaves.

<!-- step:golden-octave-shape-validation -->
### Step 5 — Golden octave-shape validation

Headline validation (the unison/octave special case, distance ≡ 0 mod 12): assert the lattice reproduces every octave-shape root offset — C string5→string2 = −2, A 5→3 = +2, G 6→3 = −3 and 6→1 = same fret, E 6→4 = +2 and 6→1 = same fret, D 4→2 = +3. This is the first slice of the caged-system golden oracle.

<!-- step:ref-sync -->
### Step 6 — Ref-sync

domain-model-reference: one line that `IntervalSpeller.Name` is the vocabulary the guitar `IntervalLattice` projects. Add `IntervalLattice` (+ the absolute-coordinate role of `Fretboard`) to the `Instruments/Guitar/Geometry/` inventory in domain-model and architecture references. Same unit of work as the code.
