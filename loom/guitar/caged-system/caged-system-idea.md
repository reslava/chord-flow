---
type: idea
id: id_01KV2WWWZ6PYT4Y4VCT4A5KD8N
title: CAGED system — the derivation engine (subsumes authored voicings)
status: draft
created: 2026-06-14
updated: 2026-06-20
version: 4
tags: []
parent_id: null
requires_load: []
---
# CAGED system — the derivation engine (subsumes authored voicings)

## The idea

The capstone: **derive** CAGED chord shapes instead of hand-typing frets. Given a
**quality** and a **CAGED shape**, the engine:

1. takes the quality's interval formula ([[chord-qualities]]),
2. anchors the shape's root(s) via its [[octave-shapes|octave shape]] in a neck zone,
3. finds each interval near those anchors using the [[interval-lattice]] fretboard lattice
   (Zone/Area rule — stay inside the shape's octave zone),
4. emits the fret shape.

`quality intervals × octave shape → chord shape`, computed, not authored. This is the
**superset that subsumes** the 34 hand-authored voicings in `packages/default-pack`:
those become the **golden oracle** — engine output for each (quality, shape) at C must
equal the authored frets. Once proven, the engine can generate far more (all qualities ×
all shapes × the partial/zone-local "chunks" Rafa flagged as the genuinely playable
parts) and later **scales and arpeggios** on the same skeleton.

## Why this is the durable direction

It replaces hand-derived fret tables (error-prone, finite) with a generator grounded in
theory — aligned with [[design-philosophy-durable-over-minimal]] and
[[chordflow-mvp-is-a-foundation]]. The authored voicings ship now and keep working; the
engine is built behind them and validated against them, never blocking content.

## Placement rules (resolved in octave-shapes-chat-002, 2026-06-20)

Step 3 ("find each interval, stay in the zone") is governed by three **derived** rules —
all consequences of where the [[interval-lattice]] places the quality's tones, nothing
authored:

- **Anchor finger = the root's rank in the placed span.** Root is the lowest fret in the
  box → anchor index → the hand reaches **right**; root highest → anchor pinky → reach
  **left**; root inside → anchor middle/ring → reach both. This *generates* the per-shape
  Left/Right margins (and their major-vs-minor flip in the index/middle shapes A, E, D —
  b3 vs 3 moves which tone is the extreme, so it moves the root's rank). C and G are
  pinky-anchored → fixed `left 1, right 0`.
- **CAGED-zone envelope vs. used zone.** The envelope is the max reach the anchor finger
  allows past the octave zone (≈ a 4-finger / 4-fret hand span); the **used zone** is the
  actual `[min,max]` a given quality occupies inside it. Minimize the used-zone width;
  prefer a contiguous region.
- **Candidate selection (the B-string tax).** The string 3→2 = 4-semitone gap makes an
  interval land as a **unison on two strings** inside one box (e.g. b5 in E / Key A:
  str3 f8 *or* str2 f4, both abs-coord 23). Resolve by a **whole-box joint minimization**
  — not greedy per interval, because the choices couple: over all candidate assignments,
  minimize the **worst same-string stretch**, tiebreak **minimal total span**, final
  tiebreak **closest to the zone center**. The search is tiny (≤2–3 candidates per
  interval) so brute force suffices.

**Box filtering for display:** a **main box** shows all of the quality's intervals; a
**secondary / partial box** shows only the intervals that satisfy the rules above.

## In scope (when scheduled)

- The derivation algorithm: (quality, CAGED shape, root) → realized fret shape, reusing
  `PitchClass` + `Fretboard`.
- The **partial/usable-subset** signal per shape (the deferred per-position playability
  hint from the voicings design §7 — "here, play strings 4–1") falls out naturally here,
  since the engine knows which intervals land where in the zone.
- The placement rules above as code: anchor-finger derivation, used-zone minimization, and
  the whole-box candidate-selection search — consuming [[octave-shapes]]' boxes + octave
  zone and the [[interval-lattice]] positions.
- Golden-oracle test: regenerate the 34 authored voicings and assert fret-equality. If the
  pack records **fingering** (not just frets), also assert the derived anchor finger — a
  second oracle for the rules above.

## Out of scope / deferred

- Scales & arpeggios overlays (same octave-shape skeleton, next step after chords).
- Replacing the authored-voicing content pipeline — the engine **complements** it
  (generate → optionally persist as authored), it doesn't delete the DSL/pack path.

## Dependencies

Builds on [[intervals]] (vocabulary), [[interval-lattice]] (fretboard positions),
[[octave-shapes]] (root maps), [[chord-qualities]] (formulas). This thread is the
integrator; design it after the four substrates have ideas locked.

Related: [[interval-derivation-engine-vision]], [[caged-c-full-include-all-shapes]], [[interval-lattice]], [[chordflow-domain-model-reference]], the `guitar-voicings` & `packages/default-pack` threads.
