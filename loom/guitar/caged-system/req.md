---
type: req
id: rq_01KVK5K1SZFW58W65WWGNW00CX
title: CAGED system — the derivation engine (subsumes authored voicings) — Requirements
status: locked
created: 2026-06-20
updated: 2026-06-20
version: 1
design_version: 3
tags: []
parent_id: de_01KVK5JEFP67KM8213ZPZGGSSC
requires_load: []
---
# CAGED system — the derivation engine (subsumes authored voicings) — Requirements

### ✅ Included

- `IN1` Derivation function `derive(quality, shape, root, neckRegion) → ChordShape` (per-string fret/muted + anchor finger + box kind), reusing `PitchClass` + `Fretboard` + [[interval-lattice]] + [[octave-shapes]] anchor query + [[chord-qualities]] formulas. No authored fret tables.
- `IN2` Anchor-finger derivation: root's rank in the placed span → anchor finger + reach direction, generating per-shape Left/Right margins (incl. the b3-vs-3 minor/major flip in the A/E/D shapes; C/G pinky-anchored).
- `IN3` CAGED-zone envelope as a single **global anchor-relative reach table** (per-finger behind/ahead), seeded from ergonomics and calibrated by the frets oracle — **not** a flat fret cap; admits stretchy shapes. Plus used-zone minimization (minimal contiguous `[min,max]`).
- `IN4` Whole-box joint candidate selection for the B-string tax: minimize worst same-string stretch, tiebreak minimal total span, tiebreak closest to zone center, deterministic final tiebreak.
- `IN5` Box filtering: **main box = 2 roots** shows all the quality's intervals; **partial box = 1 root** shows only the rule-satisfying subset — the derived usable-subset/playability signal.
- `IN6` Frets golden oracle: regenerate each of the 34 authored voicings at root C and assert fret-equality against `packages/default-pack`.
- `IN7` Anchor-finger golden oracle: annotate each authored voicing with one **anchor-finger** field and assert the derived anchor matches.
- `IN8` Dogfood fretboard UI page rendering a derived shape (frets + anchor + box kind), built on [[fretboard-render-component]].

### ❌ Excluded

- `EX1` Scales & arpeggio overlays (same octave-shape skeleton, deferred to the next thread).
- `EX2` Replacing or deleting the authored-voicing content pipeline — the engine complements it (generate → optionally persist), never removes the DSL/pack path.
- `EX3` Extended/altered qualities beyond the [[chord-qualities]] formula table (6/9/11/13/sus/alt) — additive later.
- `EX4` Alternate tunings (`Fretboard` is fixed-tuning in v1).
- `EX5` Full 6-string fingering as an oracle — fingering is non-unique; only the anchor finger is asserted (see `IN7`).

### ⛓ Constraints

- `C1` Zero authored fret/finger tables — every shape is derived from the four locked substrates; the only new datum is the one global reach table (`IN3`).
- `C2` Lives in `ChordFlow.Core` `Instruments/Guitar/`, no UI/host references; dependency direction Domain ← Instruments/Guitar (per [[instrument-boundary]] / [[chordflow-architecture-reference]]).
- `C3` Substrates [[intervals]], [[interval-lattice]], [[octave-shapes]], [[chord-qualities]] are `status: done` and are consumed, not re-authored.
- `C4` The reach table (`IN3`) is one global table, never per-shape; the frets oracle (`IN6`) is its calibration authority.
- `C5` Both oracles run at root C against the existing 34 authored voicings — the engine must not require changing authored frets to pass.