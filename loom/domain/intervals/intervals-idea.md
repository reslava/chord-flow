---
type: idea
id: id_01KTXEQADETHX82GXAV8T5A5GT
title: Intervals — the theory substrate (deferred, captured)
status: draft
created: "2026-06-12T00:00:00.000Z"
updated: 2026-06-14
version: 2
tags: []
parent_id: null
requires_load: []
---
# Intervals — the theory substrate (deferred, captured)

## The idea

A first-class **interval** layer is the substrate under most music theory: scales,
triads, arpeggios, chord construction, chord/guide tones — and **interval mapping on
the fretboard**, the most useful practice lens of all. Originally captured as a deferred
stub; it is now the **first building block of the CAGED derivation engine** (Rafa's
direction — see [[caged-system]]), so this idea is promoted from "someday" to "the
substrate the engine reads."

> **Why now:** the engine derives every CAGED chord shape from `quality intervals ×
> octave shape`. That needs (a) a defined interval vocabulary and (b) knowing **where
> each interval sits on the fretboard** relative to a root. Both live here.

## Interval vocabulary (flats-only, with two spelling overrides)

```
1  b2  2  b3  3  4  b5  5  b6  6  b7  7  8(octave)
```

- Default spelling is **flats** (one name per pitch-class distance — no sharp/flat
  duplication to reason about).
- **Overrides where the chord demands it:** `#5` for **aug** (instead of `b6`), and
  `bb7` for **dim7** (instead of `6`). These keep the chord-tone *function* correct
  (an augmented 5th, a diminished 7th) even though the pitch class coincides. This is
  exactly the gap the dim7 work already hit in `ChordTones` (the `bb7` = 9 semitones
  classifying as a Seventh) — recorded here as the general rule.

## Fretboard interval positions (the real deliverable)

For a root anywhere on the neck, **where does each interval sit**, computed across
**two octaves in both directions (left and right of the root)**. The same-note
(octave/unison) positions are exactly the **[[octave-shapes]]** root maps; intervals
generalize that to every degree. Example seed (root on string 6):

> `b3` → string 5, **−1 fret** · string 3, **+1 fret** (derivable from the octave-shape
> offset formulas).

The output is a per-(string, root) interval lattice the engine queries when it places a
quality's tones onto an octave shape.

## In scope (when scheduled)

- The interval vocabulary above as data (degree → semitones + canonical spelling, with
  the aug/dim7 overrides).
- The fretboard interval-position lattice (2 octaves L/R), derived from the octave-shape
  offsets — shared by chords, scales, arpeggios.
- A refactor target: `Scale` / triads / arpeggios / `QualityIntervals` *derive from* this
  shared layer rather than each computing semitones ad hoc.

## Out of scope / deferred

- Full spelling-aware `Interval` type (P5/M3/m7…) if the flats-plus-overrides scheme
  proves sufficient — keep minimal.
- Alternate tunings (the `Fretboard` is fixed-tuning in v1).

## Validation

The 34 hand-authored CAGED voicings (`packages/default-pack`, now shipped) are the
**golden oracle**: the interval lattice + [[octave-shapes]] + [[chord-qualities]] must,
through [[caged-system]], reproduce those exact frets.

Related: [[caged-system]], [[octave-shapes]], [[chord-qualities]], [[interval-derivation-engine-vision]], [[chordflow-domain-model-reference]], [[design-philosophy-durable-over-minimal]], the `voicings` & `packages/default-pack` threads.