---
type: idea
id: id_01KV2WVWE75SPHV91HX03J4DTG
title: Octave shapes — the 5 CAGED root maps (engine skeleton)
status: draft
created: 2026-06-14
version: 1
tags: []
parent_id: null
requires_load: []
---
# Octave shapes — the 5 CAGED root maps (engine skeleton)

## The idea

Each of the five CAGED shapes is anchored by where the **same note** (the root and its
octaves) sits in that shape's neck zone. These five **octave shapes** are the skeleton
the [[caged-system]] engine hangs everything on: place the root on its octave-shape
strings, then find each [[chord-qualities|quality]] interval nearby using the
[[intervals]] fretboard lattice → the chord shape falls out.

## The 5 octave shapes (Rafa's maps)

Root strings per shape, with the secondary-root fret offset relative to the primary root:

| Shape | Root strings | Offsets (relative to the primary root) |
|-------|--------------|----------------------------------------|
| **C** | 5, 2         | string-2 root = string-5 root **−2 frets** (left) |
| **A** | 5, 3         | string-3 root = string-5 root **+2 frets** (right) |
| **G** | 6, 3, 1      | string-3 root = string-6 root **−3 frets** (left) |
| **E** | 6, 4, 1      | string-4 root = string-6 root **+2 frets** (right) |
| **D** | 4, 2         | string-2 root = string-4 root **+3 frets** (right) |

(strings numbered 6 = low E … 1 = high E, matching `Fretboard`/the voicing DSL.) The
string-1 roots in G and E are the string-6 root an octave up on the same string family.

These offsets are the unison/octave special case of the [[intervals]] fretboard lattice —
the lattice generalizes them to every degree.

## In scope (when scheduled)

- The five octave-shape root maps as data (shape → root strings + inter-root fret offsets).
- The query the engine uses: "given a root pitch and a CAGED shape, where are its octave
  anchors on the neck."
- Establishes the CAGED **zone/area** each shape occupies (the basis of the Zone/Area
  authoring rule already used for the voicings — keep intervals inside the shape's zone).

## Out of scope / deferred

- Scale-shape and arpeggio-shape skeletons (same octave-shape anchors, different overlay)
  — additive once chords work.

## Validation

Through [[caged-system]], these maps + [[intervals]] + [[chord-qualities]] must reproduce
the 34 hand-authored CAGED voicings (`packages/default-pack`) exactly — the golden oracle.

Related: [[caged-system]], [[intervals]], [[chord-qualities]], [[interval-derivation-engine-vision]], [[chordflow-domain-model-reference]], the `voicings` & `packages/default-pack` threads.