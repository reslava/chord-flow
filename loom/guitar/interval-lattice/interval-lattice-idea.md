---
type: idea
id: id_01KVDEEY1959RD07H63R5PFMVZ
title: Interval lattice — fretboard interval positions (guitar projection of intervals)
status: draft
created: 2026-06-18
version: 1
tags: []
parent_id: null
requires_load: []
---
# Interval lattice — fretboard interval positions (guitar projection of intervals)

## Origin

Split out of the `domain/intervals` idea during the theory/guitar weave realignment (chat `loom/meta/general/chats/general-chat-005.md`, id `ch_01KVCPZHPD5FBMENZTFRH4FD0J`). The interval **vocabulary** (degree → semitones + canonical spelling, with the aug/dim7 overrides) stays pure theory in `[[intervals]]` (`domain`). **This** thread is its **guitar projection** — where each interval physically sits on the fretboard.

## The idea

For a root anywhere on the neck, **where does each interval sit** — computed across **two octaves in both directions** (left and right of the root). The same-note (octave/unison) positions are exactly the `[[octave-shapes]]` root maps; this lattice **generalizes that to every degree**. The output is a per-`(string, root)` interval lattice the `[[caged-system]]` engine queries when it places a quality's tones onto an octave shape.

Example seed (root on string 6):

> `b3` → string 5, **−1 fret** · string 3, **+1 fret** (derivable from the octave-shape offset formulas).

## In scope (when scheduled)

- The fretboard interval-position lattice (2 octaves L/R per string), derived from the `[[octave-shapes]]` offset formulas — shared by chords, scales, arpeggios.
- The engine query: "given a root pitch + neck position and an interval degree, where does that interval sit nearby."
- Reuses `PitchClass` + `Fretboard` (guitar geometry); consumes the interval vocabulary from `[[intervals]]`. Lives in the `guitar` weave's code area (`Instruments/Guitar/`, per `[[instrument-boundary]]`).

## Out of scope / deferred

- The interval **vocabulary** itself (degree → semitones, spelling, aug/dim7 overrides) — that's `[[intervals]]` in `domain`, the theory substrate this projects onto the neck.
- Alternate tunings (`Fretboard` is fixed-tuning in v1).
- Scale / arpeggio overlays (same lattice, additive once chords work).

## Dependencies

`[[intervals]]` (vocabulary, `domain`) + `[[octave-shapes]]` (the unison/octave special case this generalizes). Consumed by `[[caged-system]]` (the integrator) and `[[chord-qualities]]` formulas feed it the degrees to locate.

## Validation

Through `[[caged-system]]`, this lattice + `[[octave-shapes]]` + `[[chord-qualities]]` must reproduce the 34 hand-authored CAGED voicings (`packages/default-pack`) exactly — the golden oracle.

Related: `[[caged-system]]`, `[[intervals]]`, `[[octave-shapes]]`, `[[chord-qualities]]`, `[[interval-derivation-engine-vision]]`, `[[chordflow-domain-model-reference]]`.
