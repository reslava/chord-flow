---
type: req
id: rq_01KVJ7HMPBSD287G6B5MFADT53
title: Octave shapes — the 5 CAGED root maps (engine skeleton) — Requirements
status: locked
created: 2026-06-20
updated: 2026-06-20
version: 1
tags: []
parent_id: id_01KV2WVWE75SPHV91HX03J4DTG
requires_load: []
---
# Octave shapes — the 5 CAGED root maps (engine skeleton) — Requirements

Authoritative scope for the octave-shapes thread — extracted from `octave-shapes-idea.md`, chat-001 and chat-002. The thread stores **only the CAGED root-string partition**; the octave zone + CAGED boxes are *derived* (chat-002), and the CAGED-zone envelope / fingering / candidate-selection were folded into `[[caged-system]]` (chat-002). Builds on the shipped `[[interval-lattice]]`.

### ✅ Included

- `IN1` Create `Instruments/Guitar/Geometry/OctaveShape.cs` — a **static class**, namespace `ChordFlow.Instruments.Guitar`, pure (no I/O, no UI), unit-tested. Reuses the existing `CagedShape` enum and `FretPosition` — no parallel shape type.
- `IN2` The **only authored data**: the CAGED partition per shape — ordered root strings + primary string: C `{5,2}` p5, A `{5,3}` p5, G `{6,3,1}` p6, E `{6,4,1}` p6, D `{4,2}` p4 (alphaTab numbering 1 = high E … 6 = low E). No fret offsets stored.
- `IN3` **Anchor query (option c — target/zone-relative):** given a root `PitchClass` + `CagedShape` + a fret window `[minFret,maxFret]`, return the shape's root anchors as `FretPosition`s, computed via `IntervalLattice`/`Fretboard` (frets derived, never stored). Lowest-occurrence and all-in-window are special cases of the windowed query.
- `IN4` **Octave zone** (derived): the `[minFret,maxFret]` fret span of a shape's anchors for a given root + window — e.g. Key C: E shape → 8–10, C shape → 1–3.
- `IN5` **CAGED boxes** (derived, key-independent): the string-set partition cut by the root strings — a **main box** (`*`, complete octave) between each consecutive pair of root strings, **partial boxes** reaching from the outer roots toward strings 6 / 1. C → `6,5 · 5,2* · 2,1`; G → `6,3* · 3,1*`. Pure function of the partition.
- `IN6` Unit tests — the **golden oracle slice**: anchors reproduce the five offsets at Key C (C −2, A +2, G −3, E +2, D +3, string-1 = string-6 same fret); octave-zone spans match (E → 8–10, C → 1–3); box partitions match the table for all five shapes.
- `IN7` Ref-sync in the **same unit of work**: add `OctaveShape` to the `Instruments/Guitar/Geometry/` inventory in `chordflow-domain-model-reference` + `chordflow-architecture-reference`.

### ❌ Excluded

- `EX1` Per-string **fret math** — owned by `[[interval-lattice]]`; this thread *queries* it, never re-authors offsets.
- `EX2` The **CAGED-zone envelope, used-zone minimization, anchor-finger rule, and candidate-selection** — folded into `[[caged-system]]` (octave-shapes-chat-002); content placement, not static skeleton.
- `EX3` **Chord-quality interval placement** (quality × shape → chord shape) — `[[caged-system]]`.
- `EX4` **Scale / arpeggio overlays** — same anchors, additive once chords work.
- `EX5` The dogfood **UI page implementation** — delivered in the `ui` weave per the standing dogfood rule; this thread provides the Core anchors/zone/boxes query it renders.
- `EX6` **Alternate tunings** (`Fretboard` is fixed-tuning in v1).
- `EX7` **Which shapes a quality uses / shape pruning** (the C-full authoring policy) — `[[caged-system]]`.

### ⛓ Constraints

- `C1` The **partition is the only source of truth**; every fret is derived from `IntervalLattice`/`Fretboard` — no second offset table that can drift (the offsets in the idea are validation examples only).
- `C2` Reuse `IntervalLattice.PositionsOfInterval` / `Fretboard.PositionsFor` — no second neck-walk.
- `C3` Dependency direction: lives in `Instruments`, may depend on `Domain`, never the reverse — the `NetArchTest` Domain-edge guard stays green.
- `C4` Pure and deterministic: no I/O, no UI; fully unit-tested.
- `C5` String numbering is alphaTab (1 = high E … 6 = low E), consistent with `FretPosition` / `Fretboard` / `CagedShape`.
- `C6` Reuse the existing `CagedShape` enum — do not introduce a parallel shape type.
- `C7` Standing dogfood rule: visually validated on the fretboard UI page (root anchors + zone) before `[[caged-system]]` builds chords on top.
- `C8` Sequencing: depends on `[[interval-lattice]]` + `[[intervals]]` + `[[instrument-boundary]]`; depended upon by `[[caged-system]]`.