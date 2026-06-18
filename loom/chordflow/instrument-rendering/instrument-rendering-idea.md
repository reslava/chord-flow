---
type: idea
id: id_01KVCTCN9M2NMG56CQ0BSW9SQ3
title: Instrument-aware rendering fork + IInstrument seam
status: draft
created: 2026-06-18
version: 1
tags: []
parent_id: null
requires_load: []
---
# Instrument-aware rendering fork + IInstrument seam

## Origin

Designed in chat `loom/meta/general/chats/general-chat-005.md` (id `ch_01KVCPZHPD5FBMENZTFRH4FD0J`). Read that chat for the full reasoning.

## Status: parked

Captured now so it lives on the roadmap, but **built only when the need arises** (after `instrument-boundary`, and likely alongside whatever first demands instrument-agnostic notation). This is the deferred half of the instrument separation.

## Why

Today `AlphaTexRenderer` emits **guitar tab** (`fret.string`) only — the one renderer is guitar-coupled. To support instrument-agnostic notation (and to extract a *real*, validated `IInstrument`), the score path needs to fork.

## In scope

1. **Fork the score path:**
   - **Standard notation** — agnostic, rendered from sounding pitches. Sharable across instruments.
   - **Tab** — guitar-only, rendered from fret positions. Tab is shown **only on guitar tracks**; a piano track renders to a grand staff and never to tab (settled in the origin chat).
2. **Extract `IInstrument` here** — born with the renderer fork as its first real polymorphic caller, so the contract is *shaped by real usage* rather than guessed from a single implementer.
3. Likely introduce a **`Pitch(PitchClass, int Octave)`** theory type — the agnostic sounding-pitch carrier the notation path consumes.

## Key design tension to resolve (the reason it's deferred)

The agnostic output (sounding pitches) is **entangled** with a guitar-specific decision: *which* voicing, at *what* difficulty, one or a ranked set. `Realize(Chord) → Pitches` is underdetermined — the pitches come from a chosen voicing, and voicing-selection is the most guitar-shaped thing in the kernel. The renderer fork is the place that entanglement gets untangled into `IInstrument`'s actual contract. Designing the interface before that caller exists would bake in a guess about selection that a second instrument would rewrite.

## Depends on

`instrument-boundary` (the `Instruments/Guitar/` boundary + concrete `GuitarInstrument` adapter must exist first).

## Out of scope

The structural boundary, the namespace test, and the concrete guitar adapter — all in `instrument-boundary`.
