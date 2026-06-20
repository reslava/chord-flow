---
type: idea
id: id_01KV2WWAZDDC1XWFF5AGTJX197
title: Chord qualities — the interval formulas (engine input)
status: done
created: 2026-06-14
updated: 2026-06-20
version: 4
tags: []
parent_id: null
requires_load: []
---
# Chord qualities — the interval formulas (engine input)

## The idea

Every chord quality is just a **formula of intervals** over a root. Formalize that table
as the source the [[caged-system]] engine reads: pick a quality, get its intervals, lay
them onto an [[octave-shapes|octave shape]] via the [[interval-lattice]] fretboard lattice → the
CAGED chord shape. This is the data layer behind today's `Quality` enum + `QualityIntervals`
(which the engine should eventually *derive from*, not duplicate).

## Interval formulas (Rafa's table)

```
maj:   1  3  5
min:   1 b3  5
maj7:  1  3  5   7
7:     1  3  5  b7
m7:    1 b3  5  b7
m7b5:  1 b3 b5  b7
dim:   1 b3 b5          (diminished triad — retained)
dim7:  1 b3 b5 bb7      (symmetric; bb7 = 9 semitones)
aug:   1  3 #5
```

Spelling follows [[intervals]] (`#5` for aug, `bb7` for dim7). This is exactly the set
already in `QualityIntervals` (`Diminished7` added in the voicings work) — this thread
makes the **formula** the authoritative form and the semitone set a derived projection.

## In scope (when scheduled)

- The quality → interval-formula table as first-class data (degree+accidental, not raw
  semitones), with the canonical spelling per degree.
- `QualityIntervals` (and `ChordTones`' function classifier) **derive from** this formula
  layer instead of hand-listing semitones — single source of truth.
- Extensible to richer qualities (6, 9, 11, 13, sus, alt) additively — the formula form
  scales where a flat semitone list does not.

## Out of scope / deferred

- Extended/altered qualities themselves (just keep the door open).
- A chord *parser* for arbitrary symbol text — the DSL suffix tables stay as they are.

## Validation

Through [[caged-system]], these formulas + [[octave-shapes]] + [[interval-lattice]] must
reproduce the 34 hand-authored CAGED voicings (`packages/default-pack`) — golden oracle.

Related: [[caged-system]], [[intervals]], [[interval-lattice]], [[octave-shapes]], [[interval-derivation-engine-vision]], [[dim7-not-in-domain]], [[chordflow-domain-model-reference]], the `guitar-voicings` & `packages/default-pack` threads.
