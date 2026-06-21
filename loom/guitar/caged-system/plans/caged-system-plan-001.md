---
type: plan
id: pl_01KVK6A9EVKMXY5ZTWAQKKSEZR
title: CAGED derivation engine — derive shapes from theory
status: done
created: 2026-06-20
updated: 2026-06-21
version: 1
design_version: 1
req_version: 1
tags: []
parent_id: de_01KVK5JEFP67KM8213ZPZGGSSC
requires_load: []
target_version: 0.1.0
actual_release: 0.9.0
steps:
  - id: hand-reach-model-envelope
    order: 1
    status: done
    description: "Add the Finger enum and the single global anchor-relative reach table (index 1/3, middle 1/1, ring 1/1 placeholder, pinky 4/0) plus the envelope computation: given an anchor finger + the OctaveShape octave zone, the [min,max] fret window the box may occupy past the zone. One global table, never per-shape; pure geometry beside IntervalLattice/OctaveShape."
    files_touched: [src/ChordFlow.Core/Instruments/Guitar/Caged/HandReach.cs]
    blocked_by: []
    satisfies: [IN3, C4, C1]
  - id: anchor-finger-derivation
    order: 2
    status: done
    description: "Derive the anchor finger from the root's rank in the placed span: root lowest fret → index (reach right); root highest → pinky (reach left); root inside → middle/ring (reach both); C & G pinky-anchored. This generates the per-shape L/R margins, including the b3-vs-3 minor/major flip in the A/E/D shapes."
    files_touched: [src/ChordFlow.Core/Instruments/Guitar/Caged/AnchorFinger.cs]
    blocked_by: [1]
    satisfies: [IN2]
  - id: whole-box-candidate-selection-b-string
    order: 3
    status: done
    description: "Enumerate each quality interval's candidate positions via IntervalLattice.PositionsOfInterval within the box string-set + envelope, then the whole-box lexicographic joint minimization (choices couple, so not greedy): minimize worst same-string stretch, tiebreak minimal total span, tiebreak closest to zone center, deterministic final tiebreak (lower string then lower fret). Resolves the str3→2 = 4-semitone unison-on-two-strings case."
    files_touched: [src/ChordFlow.Core/Instruments/Guitar/Caged/CandidateSelector.cs]
    blocked_by: [1]
    satisfies: [IN4]
  - id: derive-integrator-chordshape
    order: 4
    status: done
    description: Wire OctaveShape.AnchorsFor/Zone/Boxes + the QualityIntervals formula + candidate selection (3) + anchor finger (2) + envelope (1) into derive(quality, shape, root, neckRegion) → ChordShape (per-string fret/muted + anchor finger + box kind). Main box (2 roots) keeps all the quality's intervals; partial box (1 root) keeps only the rule-satisfying subset (the derived usable-subset signal).
    files_touched: [src/ChordFlow.Core/Instruments/Guitar/Caged/CagedDerivation.cs, src/ChordFlow.Core/Instruments/Guitar/Caged/ChordShape.cs]
    blocked_by: [2, 3]
    satisfies: [IN1, IN5]
  - id: frets-golden-oracle
    order: 5
    status: done
    description: "Test: for each of the 34 authored voicings (VoicingShape canonical-C, dedup by (Quality,Shape)), assert derive(quality, shape, C, regionAtC) equals the authored frets. The neckRegion convention at C reuses the octave-shapes target/zone-relative query (region containing the authored frets). Calibrate the reach-table numbers (1) against any miss — one global edit, never per-shape."
    files_touched: [tests/ChordFlow.Core.Tests/CagedFretsOracleTests.cs]
    blocked_by: [4]
    satisfies: [IN6, C5]
  - id: anchor-finger-annotation-oracle
    order: 6
    status: done
    description: Extend the voicing DSL grammar with one optional anchor-finger token (VoicingDslParser/Writer + VoicingShape), annotate the 34 Content/default-pack/voicings/*.dsl with their anchor finger, and assert the derived anchor matches. Anchor only — not full 6-string fingering (non-unique). Ref-sync the chordflow-dsl-reference for the new token.
    files_touched: [src/ChordFlow.Core/Instruments/Guitar/Voicings/VoicingDslParser.cs, src/ChordFlow.Core/Instruments/Guitar/Voicings/VoicingDslWriter.cs, src/ChordFlow.Core/Instruments/Guitar/Voicings/VoicingShape.cs, src/ChordFlow.Core/Content/default-pack/voicings/, tests/ChordFlow.Core.Tests/CagedAnchorFingerOracleTests.cs, loom/refs/chordflow-dsl-reference.md]
    blocked_by: [4]
    satisfies: [IN7]
  - id: reference-doc-sync
    order: 7
    status: done
    description: Update chordflow-domain-model-reference with the CAGED derivation engine (derive(), ChordShape, the reach table, the two oracles) and confirm chordflow-architecture-reference placement (Instruments/Guitar/Caged) — the mandatory same-unit reference sync.
    files_touched: [loom/refs/chordflow-domain-model-reference.md, loom/refs/chordflow-architecture-reference.md]
    blocked_by: [4]
    satisfies: [C2]
---
# CAGED derivation engine — derive shapes from theory

## Goal

Implement the CAGED derivation engine: `derive(quality, shape, root, neckRegion) → ChordShape`, computed from the four locked substrates (QualityIntervals formulas, OctaveShape.AnchorsFor/Zone/Boxes, IntervalLattice.PositionsOfInterval/Distance, Fretboard) with no authored fret tables. Build bottom-up: first the one new datum — the global anchor-relative reach table + envelope (1) — then the anchor-finger derivation (2) and the whole-box candidate selection that resolves the B-string tax (3), then wire them into the derive() integrator returning a ChordShape that carries frets + anchor finger + box kind, with main-box (2 roots) showing all intervals and partial-box (1 root) the rule-satisfying subset (4). Validate against the 34 authored voicings with two golden oracles: fret-equality at C (5, which also calibrates the reach numbers) and a new one-token anchor-finger annotation on the .dsl files (6). Ship the standing dogfood fretboard page (7) and sync the domain-model/architecture refs (8). The engine complements the authored pipeline — it never deletes the DSL/pack path. Steps cite the locked req (IN1–IN8 / C1–C5, EX5).

---

## Steps

| Done | # | Step | Files touched | Blocked by | Satisfies |
|---|---|---|---|---|---|
| ✅ | 1 | Add the Finger enum and the single global anchor-relative reach table (index 1/3, middle 1/1, ring 1/1 placeholder, pinky 4/0) plus the envelope computation: given an anchor finger + the OctaveShape octave zone, the [min,max] fret window the box may occupy past the zone. One global table, never per-shape; pure geometry beside IntervalLattice/OctaveShape. | src/ChordFlow.Core/Instruments/Guitar/Caged/HandReach.cs | — | IN3, C4, C1 |
| ✅ | 2 | Derive the anchor finger from the root's rank in the placed span: root lowest fret → index (reach right); root highest → pinky (reach left); root inside → middle/ring (reach both); C & G pinky-anchored. This generates the per-shape L/R margins, including the b3-vs-3 minor/major flip in the A/E/D shapes. | src/ChordFlow.Core/Instruments/Guitar/Caged/AnchorFinger.cs | 1 | IN2 |
| ✅ | 3 | Enumerate each quality interval's candidate positions via IntervalLattice.PositionsOfInterval within the box string-set + envelope, then the whole-box lexicographic joint minimization (choices couple, so not greedy): minimize worst same-string stretch, tiebreak minimal total span, tiebreak closest to zone center, deterministic final tiebreak (lower string then lower fret). Resolves the str3→2 = 4-semitone unison-on-two-strings case. | src/ChordFlow.Core/Instruments/Guitar/Caged/CandidateSelector.cs | 1 | IN4 |
| ✅ | 4 | Wire OctaveShape.AnchorsFor/Zone/Boxes + the QualityIntervals formula + candidate selection (3) + anchor finger (2) + envelope (1) into derive(quality, shape, root, neckRegion) → ChordShape (per-string fret/muted + anchor finger + box kind). Main box (2 roots) keeps all the quality's intervals; partial box (1 root) keeps only the rule-satisfying subset (the derived usable-subset signal). | src/ChordFlow.Core/Instruments/Guitar/Caged/CagedDerivation.cs, src/ChordFlow.Core/Instruments/Guitar/Caged/ChordShape.cs | 2, 3 | IN1, IN5 |
| ✅ | 5 | Test: for each of the 34 authored voicings (VoicingShape canonical-C, dedup by (Quality,Shape)), assert derive(quality, shape, C, regionAtC) equals the authored frets. The neckRegion convention at C reuses the octave-shapes target/zone-relative query (region containing the authored frets). Calibrate the reach-table numbers (1) against any miss — one global edit, never per-shape. | tests/ChordFlow.Core.Tests/CagedFretsOracleTests.cs | 4 | IN6, C5 |
| ✅ | 6 | Extend the voicing DSL grammar with one optional anchor-finger token (VoicingDslParser/Writer + VoicingShape), annotate the 34 Content/default-pack/voicings/*.dsl with their anchor finger, and assert the derived anchor matches. Anchor only — not full 6-string fingering (non-unique). Ref-sync the chordflow-dsl-reference for the new token. | src/ChordFlow.Core/Instruments/Guitar/Voicings/VoicingDslParser.cs, src/ChordFlow.Core/Instruments/Guitar/Voicings/VoicingDslWriter.cs, src/ChordFlow.Core/Instruments/Guitar/Voicings/VoicingShape.cs, src/ChordFlow.Core/Content/default-pack/voicings/, tests/ChordFlow.Core.Tests/CagedAnchorFingerOracleTests.cs, loom/refs/chordflow-dsl-reference.md | 4 | IN7 |
| ✅ | 7 | Update chordflow-domain-model-reference with the CAGED derivation engine (derive(), ChordShape, the reach table, the two oracles) and confirm chordflow-architecture-reference placement (Instruments/Guitar/Caged) — the mandatory same-unit reference sync. | loom/refs/chordflow-domain-model-reference.md, loom/refs/chordflow-architecture-reference.md | 4 | C2 |
---

### Legend

| Symbol | Meaning |
|--------|---------|
| ✅ | Done |
| 🔄 | In Progress |
| 🔳 | Pending |
| ❌ | Cancelled |
