---
type: idea
id: id_01KTXEQADETHX82GXAV8T5A5GT
title: Intervals — the theory substrate (deferred, captured)
status: done
created: 2026-06-12
updated: 2026-06-19
version: 8
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
- **Spelling is role-keyed, not pitch-keyed** — `label(semitone, role)`, not a flat
  `semitone → name` map. The "overrides" (`#5` for **aug** instead of `b6`, `bb7` for
  **dim7** instead of `6`) are just where the chord-tone *role* disambiguates a shared
  pitch class: semitone 3 is `b3` (minor 3rd) *or* `#9` (tension); semitone 8 is `#5`
  *or* `b6`/`b13`; semitone 9 is `bb7` *or* `6`/`13`. The role (Third/Fifth/Seventh vs.
  out-of-chord tension) picks the name. This is exactly the gap the dim7 work hit in
  `ChordTones` (`bb7` = 9 semitones classifying as a Seventh) — and it is **already
  implemented ad-hoc** in `VoicingDiagram.IntervalLabel(semitone, role)`. Recorded here
  as the general rule, to be centralized.

## In scope (when scheduled)

- A **role-keyed interval speller** — `label(semitone, role)` — as the single spelling
  authority. It owns **both label spaces**: the simple octave chord-tone degrees
  (`R b3 3 b5 5 #5 b7 bb7 7` …) *and* the compound **tension** names for out-of-chord
  notes (`b9 9 #9 11 #11 b13 13` …). Both already exist, split across
  `VoicingDiagram.IntervalLabel` (role-aware chord tones) and `VoicingDiagram.GenericLabel`
  (tensions) — this layer is where they become one authority.
- **First consumer = `VoicingDiagram`** — pull `IntervalLabel` + `GenericLabel` out into
  the shared `Domain` speller so the diagram delegates. This is the concrete, testable win.
  Then, *if* a spelling-aware `Interval` type proves worth it, `Scale` / triads / arpeggios /
  `QualityIntervals` can lean on it too — but those already own their own semitone arrays,
  so name a real call-site change rather than assuming they "derive from" it for free.

## Out of scope / deferred

- The **fretboard interval-position lattice** (where each interval sits on the neck) —
  split out to [[interval-lattice]] in the `guitar` weave; it's guitar geometry, not theory.
- Full spelling-aware `Interval` type (P5/M3/m7…) if the flats-plus-overrides scheme
  proves sufficient — keep minimal.
- Alternate tunings (a fretboard concern — see [[interval-lattice]]).

## Validation

**Immediate oracle:** the new speller must reproduce `VoicingDiagram`'s current
`IntervalLabel`/`GenericLabel` output byte-for-byte — that existing code is the de-facto
spec, and the centralization is correct iff the diagram's labels are unchanged.

**End-to-end oracle:** through [[caged-system]], this vocabulary + [[interval-lattice]] +
[[octave-shapes]] + [[chord-qualities]] must reproduce the 34 hand-authored CAGED voicings
(`packages/default-pack`) exactly — the golden oracle.

Related: [[interval-lattice]], [[caged-system]], [[octave-shapes]], [[chord-qualities]], [[interval-derivation-engine-vision]], [[chordflow-domain-model-reference]], [[design-philosophy-durable-over-minimal]], the `guitar-voicings` & `packages/default-pack` threads.
