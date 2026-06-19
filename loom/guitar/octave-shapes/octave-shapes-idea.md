---
type: idea
id: id_01KV2WVWE75SPHV91HX03J4DTG
title: Octave shapes — the 5 CAGED root maps (engine skeleton)
status: draft
created: 2026-06-14
updated: 2026-06-19
version: 3
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
[[interval-lattice]] fretboard lattice → the chord shape falls out.

> **Sequencing (corrected 2026-06-19):** octave-shapes is the **unison/octave special
> case of [[interval-lattice]]** and now builds **on** it. The inter-root fret offsets
> below are *derivable* from the lattice (`STRING_OFFSET` + mod 12), not authored here —
> see "What this thread actually stores." (Previously the dependency was wired backwards;
> interval-lattice no longer depends on this thread.)

## The 5 octave shapes (Rafa's maps)

Root strings per shape, with the secondary-root fret offset relative to the primary root
(primary = the first string listed; resolved 2026-06-19):

| Shape | Root strings | Primary | Offsets (relative to the primary root) |
|-------|--------------|---------|----------------------------------------|
| **C** | 5, 2         | 5       | string-2 root = string-5 root **−2 frets** (left) |
| **A** | 5, 3         | 5       | string-3 root = string-5 root **+2 frets** (right) |
| **G** | 6, 3, 1      | 6       | string-3 root **−3** · string-1 root **same fret** as string-6 (2 octaves up) |
| **E** | 6, 4, 1      | 6       | string-4 root **+2** · string-1 root **same fret** as string-6 (2 octaves up) |
| **D** | 4, 2         | 4       | string-2 root = string-4 root **+3 frets** (right) |

(strings numbered 6 = low E … 1 = high E, matching `Fretboard`/the voicing DSL.) The
string-1 roots in G and E land at the **same fret** as the string-6 root because strings 1
and 6 are the same pitch class two octaves apart. A negative offset near the nut **wraps
+12** (mod 12) — the lattice finding the nearest same-pitch position.

These offsets are the unison/octave (interval `1`/`8`/`15`) slice of the [[interval-lattice]]
fretboard lattice — and they fall straight out of it: `distance ≡ 0 (mod 12)`.

## What this thread actually stores

The offsets above are **derived from [[interval-lattice]]**, so this thread does **not**
store a second fret-offset table that could drift. The only **authored data** is the CAGED
partition — which same-pitch anchors group into which named shape:

```
{ shape → ordered root strings, primary string }
```

…which the lattice doesn't itself express (it's a pedagogical grouping layered on the pure
geometry). The offset numbers above are kept as **validation examples** for that grouping,
not as the source of truth.

## In scope (when scheduled)

- The five CAGED root-string partitions as data (shape → root strings + primary string).
- The query the engine uses: **"given a root pitch + a CAGED shape + a target neck
  position, where are its octave anchors"** — **target/zone-relative (option c, resolved
  2026-06-19)**: a shape recurs every 12 frets, so anchors are returned relative to a
  caller-supplied neck region; lowest-occurrence and all-in-window are special cases of this.
  Frets are computed via the [[interval-lattice]], not stored.
- Establishes the CAGED **zone/area** each shape occupies (the basis of the Zone/Area
  authoring rule already used for the voicings — keep intervals inside the shape's zone).

## Out of scope / deferred

- The per-string fret math — owned by [[interval-lattice]]; this thread queries it.
- Scale-shape and arpeggio-shape skeletons (same octave-shape anchors, different overlay)
  — additive once chords work.

## Dependencies

`[[interval-lattice]]` (the base geometry this is the special case of) + `[[intervals]]`
(vocabulary) + `[[instrument-boundary]]`. Consumed by `[[caged-system]]`.

## Validation

Through [[caged-system]], these maps + [[interval-lattice]] + [[chord-qualities]] must
reproduce the 34 hand-authored CAGED voicings (`packages/default-pack`) exactly — the
golden oracle.

**Dogfood (standing guitar-weave rule):** ship a fretboard UI page showing each CAGED
shape's root anchors + zone on the neck — fast visual confirmation before building chords on
top. Built on the [[fretboard-render-component]].

Related: [[caged-system]], [[interval-lattice]], [[intervals]], [[chord-qualities]], [[interval-derivation-engine-vision]], [[fretboard-render-component]], [[chordflow-domain-model-reference]], the `guitar-voicings` & `packages/default-pack` threads.