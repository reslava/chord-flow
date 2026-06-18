---
type: idea
id: id_01KVCTCBE0AXZH6FX2HJ9ZA1YH
title: Theory / Instrument boundary + concrete Guitar adapter
status: draft
created: 2026-06-18
updated: 2026-06-18
version: 2
tags: []
parent_id: null
requires_load: []
---
# Theory / Instrument boundary + concrete Guitar adapter

## Origin

Designed in chat `loom/meta/general/chats/general-chat-005.md` (id `ch_01KVCPZHPD5FBMENZTFRH4FD0J`) — "Music theory · Instruments adapters · UI components". Read that chat for the full reasoning behind every decision below.

## Why

`Domain/` is described as "music-theory-first" but actually holds two concerns in one coat: **pure, instrument-agnostic music theory** and **guitar-specific** realization (standard tuning, frets, strings, CAGED). Separating them makes the kernel provably instrument-agnostic and turns guitar into an opt-in adapter — the precondition for the interval-derivation engine (which *derives* guitar shapes from pure theory) and the foundation any future instrument would plug into.

## Decision (settled in the origin chat)

- A **boundary, not a separate assembly.** With one real instrument, a separate C# project would be a speculative split with no second consumer; a namespace boundary inside Core gets the durability with none of the dependency-graph churn.
- Build a **concrete first-class `GuitarInstrument` adapter** now — a deliberate public surface, not a free-floating interface.
- **Defer the `IInstrument` interface** — an interface with one implementer and no polymorphic caller asserts a guess rather than expressing a discovered truth. It is born later, with its first real caller, in the `instrument-rendering` thread.

## In scope

1. Create `Instruments/Guitar/` and move the guitar-specific types out of `Domain/`:
   - geometry: `Fretboard`, `FretPosition`
   - realize: `Voicing`, `VoicingBook`, `BeginnerShellStrategy`, `VoicingShape`, `CagedShape`, `VoicingRealizer`
   - diagrams: `FretboardDiagram`, `FretboardMarker`, `MarkerShape`, `VoicingDiagram`
2. `Domain/` remains **pure theory** (PitchClass, Quality/QualityIntervals, ChordTone, Chord, Scale, DiatonicChord, RomanDegree/ScaleDegree, Progression, Transposer, NoteSpeller, ChordSymbol, the 48-PPQ rhythm grid, LeadTargets). Namespaces updated accordingly.
3. **Architecture test** in `ChordFlow.Core.Tests`: assert no type under `ChordFlow.Core.Domain` references `ChordFlow.Core.Instruments`. This guards the **Domain edge only** — it must NOT forbid `Rendering → Instruments` (the renderer legitimately consumes guitar fret positions for tab).
4. Give guitar a deliberate **`GuitarInstrument` adapter surface** — a facade over `VoicingBook` / `Fretboard` / `VoicingDiagram`: realize a chord → a guitar voicing carrying its fret positions; produce a `FretboardDiagram`.
5. Update the refs in the **same unit of work**: `chordflow-architecture-reference` + `chordflow-domain-model-reference` to show the Theory ↔ Guitar boundary.

## Out of scope (→ `instrument-rendering` thread)

- The `IInstrument` interface.
- The notation/tab renderer fork.
- A `Pitch(pc, octave)` theory type.
- Piano or any second instrument.

## Invariants

- Dependency direction: `Domain ← Instruments ← Rendering` (arrows point up; only the Domain→Instruments edge is compiler/test-enforced).
- Nothing built here is throwaway — it's the substrate the deferred interface is later extracted from.

## Sequencing

Build **before** the intervals / derivation-engine feature (per the origin chat). Depended upon by `instrument-rendering`.

This thread **founds the `guitar` weave's code area** (`Instruments/Guitar/`): the guitar derivation threads — `octave-shapes`, `interval-lattice`, `caged-system`, `guitar-voicings` — all target the area it creates.
