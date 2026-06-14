---
type: idea
id: id_01KV2WWWZ6PYT4Y4VCT4A5KD8N
title: CAGED system — the derivation engine (subsumes authored voicings)
status: draft
created: 2026-06-14
version: 1
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
3. finds each interval near those anchors using the [[intervals]] fretboard lattice
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

## In scope (when scheduled)

- The derivation algorithm: (quality, CAGED shape, root) → realized fret shape, reusing
  `PitchClass` + `Fretboard`.
- The **partial/usable-subset** signal per shape (the deferred per-position playability
  hint from the voicings design §7 — "here, play strings 4–1") falls out naturally here,
  since the engine knows which intervals land where in the zone.
- Golden-oracle test: regenerate the 34 authored voicings and assert fret-equality.

## Out of scope / deferred

- Scales & arpeggios overlays (same octave-shape skeleton, next step after chords).
- Replacing the authored-voicing content pipeline — the engine **complements** it
  (generate → optionally persist as authored), it doesn't delete the DSL/pack path.

## Dependencies

Builds on [[intervals]] (vocabulary + fretboard lattice), [[octave-shapes]] (root maps),
[[chord-qualities]] (formulas). This thread is the integrator; design it after the three
substrates have ideas locked.

Related: [[interval-derivation-engine-vision]], [[caged-c-full-include-all-shapes]], [[chordflow-domain-model-reference]], the `voicings` & `packages/default-pack` threads.