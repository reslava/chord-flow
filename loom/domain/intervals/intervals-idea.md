---
type: idea
id: id_01KTXEQADETHX82GXAV8T5A5GT
title: Intervals — the theory substrate (deferred, captured)
status: draft
created: 2026-06-12
updated: 2026-06-18
version: 3
tags: []
parent_id: null
requires_load: []
---
# Intervals — the theory substrate (deferred, captured)

## The idea

A first-class **interval** layer is the substrate under most music theory: scales,
triads, arpeggios, chord construction, chord/guide tones. It is the **theory vocabulary**
the [[caged-system]] derivation engine reads. The *fretboard projection* of intervals —
**where each interval sits on the neck** — is its guitar counterpart and now lives in
[[interval-lattice]] (`guitar` weave), split out of this idea during the theory/guitar
realignment.

> **Why now:** the engine derives every CAGED chord shape from `quality intervals ×
> octave shape`. That needs (a) a defined interval **vocabulary** — here — and (b) knowing
> **where each interval sits on the fretboard** — the guitar projection, [[interval-lattice]].

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

## In scope (when scheduled)

- The interval vocabulary above as data (degree → semitones + canonical spelling, with
  the aug/dim7 overrides).
- A refactor target: `Scale` / triads / arpeggios / `QualityIntervals` *derive from* this
  shared theory layer rather than each computing semitones ad hoc.

## Out of scope / deferred

- The **fretboard interval-position lattice** (where each interval sits on the neck) —
  split out to [[interval-lattice]] in the `guitar` weave; it's guitar geometry, not theory.
- Full spelling-aware `Interval` type (P5/M3/m7…) if the flats-plus-overrides scheme
  proves sufficient — keep minimal.
- Alternate tunings (a fretboard concern — see [[interval-lattice]]).

## Validation

Through [[caged-system]], this vocabulary + [[interval-lattice]] + [[octave-shapes]] +
[[chord-qualities]] must reproduce the 34 hand-authored CAGED voicings
(`packages/default-pack`) exactly — the golden oracle.

Related: [[interval-lattice]], [[caged-system]], [[octave-shapes]], [[chord-qualities]], [[interval-derivation-engine-vision]], [[chordflow-domain-model-reference]], [[design-philosophy-durable-over-minimal]], the `guitar-voicings` & `packages/default-pack` threads.
