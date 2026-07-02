---
type: idea
id: id_01KV2WWWZ6PYT4Y4VCT4A5KD8N
title: CAGED system — the derivation engine (subsumes authored voicings)
status: done
created: 2026-06-14
updated: 2026-06-20
version: 5
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
- **CAGED-zone envelope = an anchor-relative reach table (resolved 2026-06-20, chat-001 §2).**
  The envelope is **not** a flat fret constant — it is a single **global per-finger reach
  table** (how far behind / ahead each finger can stretch from the anchor, grounded in hand
  ergonomics, e.g. index ≈ +3/−1, pinky ≈ −3/+1). The anchor finger's entry sets how far
  past the octave zone the box may reach. This resolves the rare/stretchy-shape tension
  ([[caged-c-full-include-all-shapes]]) by construction — the stretch is whatever the
  anchor finger physically allows, never a cap that prunes shapes. The **used zone** is the
  actual `[min,max]` a given quality occupies inside the envelope; minimize the used-zone
  width, prefer a contiguous region. The exact reach numbers are seeded from ergonomics and
  **calibrated by the frets oracle**.
- **Candidate selection (the B-string tax).** The string 3→2 = 4-semitone gap makes an
  interval land as a **unison on two strings** inside one box (e.g. b5 in E / Key A:
  str3 f8 *or* str2 f4, both abs-coord 23). Resolve by a **whole-box joint minimization**
  — not greedy per interval, because the choices couple: over all candidate assignments,
  minimize the **worst same-string stretch**, tiebreak **minimal total span**, final
  tiebreak **closest to the zone center**. The search is tiny (≤2–3 candidates per
  interval) so brute force suffices.

**Box filtering for display (resolved 2026-06-20, chat-001 §3).** Grounded in
[[octave-shapes]]' derived CAGED boxes, where the root strings cut a shape into boxes:

- a **main box** sits between two consecutive root strings → it contains **2 roots** (a
  complete octave) and shows **all** of the quality's intervals;
- a **partial box** reaches past an outer root toward string 6 / string 1 → it contains
  only **1 root** and shows only the subset of intervals that land in it under the rules
  above.

Root-count (2 vs 1) is the derived discriminator; interval-completeness is its consequence.

## In scope (when scheduled)

- The derivation algorithm: (quality, CAGED shape, root, neck region) → realized fret
  shape, reusing `PitchClass` + `Fretboard` + the [[interval-lattice]] +
  [[octave-shapes]] query + [[chord-qualities]] formulas.
- The **partial/usable-subset** signal per shape (the deferred per-position playability
  hint from the voicings design §7 — "here, play strings 4–1") falls out naturally here,
  since the engine knows which intervals land where in the zone — it is exactly the
  partial-box (1-root) filter above.
- The placement rules above as code: anchor-finger derivation, the anchor-relative reach
  table + used-zone minimization, and the whole-box candidate-selection search — consuming
  [[octave-shapes]]' boxes + octave zone and the [[interval-lattice]] positions.
- **Two golden oracles** against the 34 authored voicings at C:
  - *Frets oracle* — regenerate each (quality, shape) and assert fret-equality.
  - *Anchor-finger oracle (resolved 2026-06-20, chat-001 §1)* — annotate each authored
    voicing with its **anchor finger** (one field, not full fingering — fingering is
    non-unique, anchor is what the rule predicts) and assert the derived anchor matches.

## Out of scope / deferred

- Scales & arpeggios overlays (same octave-shape skeleton, next step after chords).
- Replacing the authored-voicing content pipeline — the engine **complements** it
  (generate → optionally persist as authored), it doesn't delete the DSL/pack path.
- Extended/altered qualities beyond the [[chord-qualities]] table (additive later).
- Alternate tunings (`Fretboard` is fixed-tuning in v1).

## Grounding status (chat-001, 2026-06-20)

Fully grounded: every placement rule traces to a locked substrate, and both the
frets-oracle and the new anchor-finger oracle make the rules falsifiable. The four
substrates ([[intervals]], [[interval-lattice]], [[octave-shapes]], [[chord-qualities]])
are all `status: done`, so this integrator thread is unblocked.

## Dependencies

Builds on [[intervals]] (vocabulary), [[interval-lattice]] (fretboard positions),
[[octave-shapes]] (root maps), [[chord-qualities]] (formulas). This thread is the
integrator; designed after the four substrates locked.

Related: [[interval-derivation-engine-vision]], [[caged-c-full-include-all-shapes]], [[interval-lattice]], [[chordflow-domain-model-reference]], the `guitar-voicings` & `packages/default-pack` threads.