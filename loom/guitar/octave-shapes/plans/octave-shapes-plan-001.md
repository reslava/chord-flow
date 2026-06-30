---
type: plan
id: pl_01KVJ7N2RMCM2M8BJ6BDC704A1
title: Octave shapes — implementation
status: done
created: 2026-06-20
updated: 2026-06-20
version: 1
design_version: 4
req_version: 1
tags: []
parent_id: de_01KVJ7M0EAS5PXGQ0W7T67ZPKX
requires_load: []
target_version: 0.1.0
actual_release: 0.8.0
steps:
  - id: partition-data
    order: 1
    status: done
    description: OctaveShape static class + the authored CAGED partition (RootStrings, primary-first) for all five shapes
    files_touched: [src/ChordFlow.Core/Instruments/Guitar/Geometry/OctaveShape.cs, tests/ChordFlow.Core.Tests/OctaveShapeTests.cs]
    blocked_by: []
    satisfies: [IN1, IN2, C1, C3, C4, C5, C6]
  - id: option-c-anchor-query
    order: 2
    status: done
    description: "Anchor query (option c): AnchorsFor(root, shape, minFret, maxFret) via IntervalLattice / Fretboard"
    files_touched: [src/ChordFlow.Core/Instruments/Guitar/Geometry/OctaveShape.cs, tests/ChordFlow.Core.Tests/OctaveShapeTests.cs]
    blocked_by: []
    satisfies: [IN3, C1, C2]
  - id: octave-zone-caged-boxes
    order: 3
    status: done
    description: "Derived geometry: octave zone (anchor fret span) + CAGED boxes (string-set partition from the roots)"
    files_touched: [src/ChordFlow.Core/Instruments/Guitar/Geometry/OctaveShape.cs, tests/ChordFlow.Core.Tests/OctaveShapeTests.cs]
    blocked_by: []
    satisfies: [IN4, IN5]
  - id: golden-validation
    order: 4
    status: done
    description: "Golden oracle tests: offsets at Key C, octave-zone spans, and box partitions for all five shapes"
    files_touched: [tests/ChordFlow.Core.Tests/OctaveShapeTests.cs]
    blocked_by: []
    satisfies: [IN6]
  - id: ref-sync
    order: 5
    status: done
    description: "Ref-sync: add OctaveShape to the Instruments/Guitar/Geometry inventory in domain-model + architecture references"
    files_touched: [loom/refs/chordflow-domain-model-reference.md, loom/refs/chordflow-architecture-reference.md]
    blocked_by: []
    satisfies: [IN7]
---
# Octave shapes — implementation

## Goal

Implement `OctaveShape` — the static CAGED root-string partition (the only authored data) plus the queries derived on top of the shipped `IntervalLattice`: the option-c target/zone-relative anchor query, the octave zone (fret span of the anchors), and the CAGED boxes (string-set partition cut by the root strings). The idea's per-string offsets stay validation examples, never stored (C1). Validate with unit tests including the golden slice — anchors reproduce the five offsets at Key C, the octave-zone spans, and the box partitions for all five shapes. Pure `ChordFlow.Core` (no I/O, no UI; reuse `CagedShape`/`FretPosition`; the `Domain ↛ Instruments` arch guard stays green). The dogfood UI page is delivered separately in the `ui` weave.

---

## Steps

| Done | # | Step | Files touched | Blocked by | Satisfies |
|---|---|---|---|---|---|
| ✅ | 1 | OctaveShape static class + the authored CAGED partition (RootStrings, primary-first) for all five shapes | src/ChordFlow.Core/Instruments/Guitar/Geometry/OctaveShape.cs, tests/ChordFlow.Core.Tests/OctaveShapeTests.cs | — | IN1, IN2, C1, C3, C4, C5, C6 |
| ✅ | 2 | Anchor query (option c): AnchorsFor(root, shape, minFret, maxFret) via IntervalLattice / Fretboard | src/ChordFlow.Core/Instruments/Guitar/Geometry/OctaveShape.cs, tests/ChordFlow.Core.Tests/OctaveShapeTests.cs | — | IN3, C1, C2 |
| ✅ | 3 | Derived geometry: octave zone (anchor fret span) + CAGED boxes (string-set partition from the roots) | src/ChordFlow.Core/Instruments/Guitar/Geometry/OctaveShape.cs, tests/ChordFlow.Core.Tests/OctaveShapeTests.cs | — | IN4, IN5 |
| ✅ | 4 | Golden oracle tests: offsets at Key C, octave-zone spans, and box partitions for all five shapes | tests/ChordFlow.Core.Tests/OctaveShapeTests.cs | — | IN6 |
| ✅ | 5 | Ref-sync: add OctaveShape to the Instruments/Guitar/Geometry inventory in domain-model + architecture references | loom/refs/chordflow-domain-model-reference.md, loom/refs/chordflow-architecture-reference.md | — | IN7 |
---

### Legend

| Symbol | Meaning |
|--------|---------|
| ✅ | Done |
| 🔄 | In Progress |
| 🔳 | Pending |
| ❌ | Cancelled |

<!-- step:partition-data -->
### Step 1 — Partition data

Create static `OctaveShape` in `ChordFlow.Instruments.Guitar`, mirroring `Fretboard`/`IntervalLattice` style. Reuse the existing `CagedShape` enum and `FretPosition` — no parallel shape type (C6). Author the partition as the single source of truth: C `{5,2}` p5, A `{5,3}` p5, G `{6,3,1}` p6, E `{6,4,1}` p6, D `{4,2}` p4, ordered primary-first (alphaTab 1=high E…6=low E). `RootStrings(CagedShape) → IReadOnlyList<int>`. No fret offsets stored (C1). Tests: the five partitions, primary-first ordering.

<!-- step:option-c-anchor-query -->
### Step 2 — Option-c anchor query

`AnchorsFor(PitchClass root, CagedShape shape, int minFret, int maxFret) → IReadOnlyList<FretPosition>` — the shape's root anchors in the window, restricted to the shape's root strings, built on `IntervalLattice.PositionsOfInterval`/`Fretboard.PositionsFor` (no second neck-walk, C2). Frets derived, never stored (C1). Lowest-occurrence and all-in-window are special cases of the window. Tests: anchors land on the partition's strings; window bounds respected; recurs every 12 frets.

<!-- step:octave-zone-caged-boxes -->
### Step 3 — Octave zone + CAGED boxes

`Zone(root, shape, minFret, maxFret) → OctaveZone(int MinFret,int MaxFret)` = `[min,max]` of the anchors (IN4). `Boxes(CagedShape) → IReadOnlyList<CagedBox>` with `readonly record struct CagedBox(int LowString,int HighString,bool IsMain)` (IN5): sort root strings → partial-below `(6,maxRoot)` if `maxRoot<6`, a main box between each consecutive root pair, partial-above `(minRoot,1)` if `minRoot>1`; `IsMain` only between roots. Key-independent. Tests inline alongside the golden step.

<!-- step:golden-validation -->
### Step 4 — Golden validation

Headline validation: anchors reproduce every offset at Key C (C str5→str2 = −2, A 5→3 = +2, G 6→3 = −3 and 6→1 same fret, E 6→4 = +2 and 6→1 same fret, D 4→2 = +3); octave-zone spans (E → 8–10, C → 1–3); `Boxes` matches the chat table for all five (C `6,5·5,2*·2,1`, A `6,5·5,3*·3,1`, G `6,3*·3,1*`, E `6,4*·4,1*`, D `6,4·4,2*·2,1`). This composes with the interval-lattice golden test as the next slice of the caged-system oracle.

<!-- step:ref-sync -->
### Step 5 — Ref-sync

Same unit of work as the code: add `OctaveShape` (the CAGED root-string partition + derived octave zone / boxes) to the `Instruments/Guitar/Geometry/` inventory in `chordflow-domain-model-reference` and `chordflow-architecture-reference`, noting it is the special case built on `IntervalLattice`.
