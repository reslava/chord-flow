---
type: req
id: rq_01KVEBFGJDRV3N9CMXJS856A96
title: Theory / Instrument boundary + concrete Guitar adapter — Requirements
status: locked
created: 2026-06-18
updated: 2026-06-18
version: 1
tags: []
parent_id: id_01KVCTCBE0AXZH6FX2HJ9ZA1YH
requires_load: []
---
# Theory / Instrument boundary + concrete Guitar adapter — Requirements

Authoritative scope for the Theory/Instrument boundary thread — extracted from `instrument-boundary-idea.md`, the settled `instrument-boundary-design.md`, and chat-001.

### ✅ Included

- `IN1` Create the `Instruments/Guitar/` code area and move guitar **geometry** out of `Domain/`: `Fretboard`, `FretPosition`.
- `IN2` Move guitar **realize** types out of `Domain/`: `Voicing`, `IVoicingStrategy`, `BeginnerShellStrategy`, `VoicingBook`, `VoicingShape`, `CagedShape`, `VoicingRealizer`, `VoicingDslParser`, `VoicingDslWriter`, `VoicingDiagram`.
- `IN3` Move the guitar **diagram carrier** out of `Domain/`: `FretboardDiagram`, `FretboardMarker`, `MarkerShape`.
- `IN4` All moved/new guitar types use namespace `ChordFlow.Instruments.Guitar` (flat; `Geometry/`/`Voicings/`/`Diagrams/` folders are organizational only). Update every consumer's `using`.
- `IN5` `Domain/` remains **pure instrument-agnostic theory** after the move (harmony, scales, interval vocabulary, progression/song, the 48-PPQ rhythm grid, lead targets).
- `IN6` Split `LeadTargets`: keep `GuideTones` + `PitchClassOf` (pitch-class output) in `Domain/`; relocate the fret-resolving method to a `ResolveLead(Chord, TargetZone, maxFret) → IReadOnlyList<FretPosition>` method on `GuitarInstrument`.
- `IN7` Build a concrete first-class **`GuitarInstrument`** adapter facade over `VoicingBook`/`Fretboard`/`VoicingDiagram`: realize a chord → a guitar voicing carrying its fret positions; produce a `FretboardDiagram`; resolve a lead target zone → fret positions.
- `IN8` Architecture test in `ChordFlow.Core.Tests` using **`NetArchTest.Rules`**: assert no type under `ChordFlow.Domain` depends on `ChordFlow.Instruments`.
- `IN9` Update the refs in the **same unit of work**: `chordflow-architecture-reference` + `chordflow-domain-model-reference` to show the Theory↔Guitar boundary as live structure and correct the namespace.

### ❌ Excluded

- `EX1` The polymorphic `IInstrument` interface — deferred to `instrument-rendering` (born with its first real caller).
- `EX2` The notation/tab renderer fork (agnostic-notation ∥ instrument-tab).
- `EX3` A `Pitch(pc, octave)` theory type.
- `EX4` Piano or any second instrument.
- `EX5` Forcing existing callers (`AlphaTexRenderer`, `ContentCrud`, `VoicingStore`) **through** the `GuitarInstrument` facade — the facade is additive this thread; callers keep using the moved types directly.
- `EX6` A separate C# assembly/project for instruments — this is a **namespace boundary inside `ChordFlow.Core`**, not an assembly split.

### ⛓ Constraints

- `C1` Dependency direction `Domain ← Instruments ← Rendering`. **Only the `Domain → Instruments` edge is test-enforced.** `Rendering → Instruments` and `Persistence → Instruments` must stay allowed (the tab renderer and voicing store legitimately consume guitar types).
- `C2` New-area namespace is `ChordFlow.Instruments.Guitar`. The assembly is `ChordFlow.Core` but its root namespace is flat `ChordFlow.*` — **not** `ChordFlow.Core.*`.
- `C3` Architecture-test mechanism is `NetArchTest.Rules` (test-only NuGet; IL-level dependency analysis so method-body references are caught).
- `C4` The arch test must pass **green** at thread end — i.e. every boundary-crosser the move exposes (`LeadTargets.Resolve` per `IN6`, `IVoicingStrategy` per `IN2`) is actually resolved, not papered over.
- `C5` Nothing built here is throwaway — it is the substrate the deferred `IInstrument` is later extracted from.
- `C6` Sequencing: build **before** the intervals / derivation-engine thread; depended upon by `instrument-rendering`. This thread founds the `guitar` weave's `Instruments/Guitar/` code area.
