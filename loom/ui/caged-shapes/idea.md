---
type: idea
id: id_01KVJDCQHPYT5V3H7VBN5AXJYJ
title: CAGED shapes — fretboard dogfood page (octave-shapes visual check)
status: done
created: 2026-06-20
updated: 2026-06-20
version: 2
tags: []
parent_id: null
requires_load: []
---
# CAGED shapes — fretboard dogfood page (octave-shapes visual check)

## Origin

The dogfood page `[[octave-shapes]]` commits to but defers — req `EX5` + the standing guitar-weave dogfood rule. `guitar/octave-shapes` shipped its Core query (`OctaveShape`: `AnchorsFor` / `Zone` / `Boxes`); **this thread renders it** for a fast visual check before `[[caged-system]]` builds chords on top. Sibling of `[[intervals-scales]]` (the `[[interval-lattice]]` dogfood) — same `[[fretboard-render-component]]`, whose horizontal orientation already shipped there.

## The idea

A **CAGED Shapes** screen: pick a CAGED shape (C/A/G/E/D), a root note, and a neck region; the app lights up that shape's **root anchors** on the fretboard and shows its **octave zone**. Fast visual confirmation that `OctaveShape` places the five skeletons where they belong — including the **D-shape octave-up anchor** (str2 +3, the in-window-unison trap we just closed) and the G/E **str1 = str6 same-fret** anchors.

Data spine — mostly existing seams:

**place via `OctaveShape` (exists) → producer builds `FretboardDiagram` → render horizontal (shipped) → page chrome.**

### Reused as-is

- `OctaveShape.AnchorsFor(root, shape, minFret, maxFret)` → the shape's root anchors; `Zone(...)` → the octave-zone span; `Boxes(shape)` → the string-set partition.
- `[[fretboard-render-component]]` (the SVG view — horizontal orientation + per-control visibility flags shipped via `[[intervals-scales]]`).
- `FretboardDiagram` (flat marker list + `FretMin`/`FretMax` window).

### What's genuinely new

1. A **CAGED-shape producer**: `(shape, root, region) → FretboardDiagram` — anchors as markers, root highlighted. A new producer of `FretboardDiagram` alongside the voicing and scale producers (no view change).
2. **Octave-zone visualization** — *design question:* shade a **fret band** (a small new component capability) vs. simply set `FretMin`/`FretMax` to the zone (reuse the existing window). Drawing the **CAGED boxes** as an explicit layer is likely a later additive step once anchors + zone read well.
3. Page chrome: a **CAGED-shape selector** (C/A/G/E/D) + **root-note selector** (+ optional neck-region control).

### Coloring

Root anchors highlighted (root-red, mirroring the scales page); a second accent on the secondary-string anchors so the octave stack reads at a glance. Page-owned palette; the component stays a dumb drawer.

## In scope

- The CAGED Shapes page: shape selector + root selector, rendering `OctaveShape` anchors + octave zone on a horizontal fretboard.
- A **CAGED-shape producer** (`(shape, root, region) → FretboardDiagram`) via `OctaveShape`.
- Whatever minimal `[[fretboard-render-component]]` tweak the zone visualization needs (decided at design — band-shade vs. window).

## Out of scope / deferred

- **Chord-quality rendering** (full CAGED chords) — `[[caged-system]]`'s; this page may later host it.
- **CAGED boxes** as an explicit drawn layer — additive once anchors + zone read well.
- Persistence, page polish, alternate tunings.
- A root-*fret* picker (root-note + region only in v1).

## Dependencies

`[[octave-shapes]]` (the `OctaveShape` Core query, shipped) · `[[fretboard-render-component]]` (SVG view, shipped — horizontal via `[[intervals-scales]]`) · `[[intervals-scales]]` (sibling dogfood; the orientation / control-flag precedent).

## Validation

Step through C/A/G/E/D at a few keys and confirm the root anchors land on the right strings/frets and the octave zone spans correctly — especially **D** (str2 octave-up, not the unison) and **G/E** (str1 = str6 same fret). This page **is** the dogfood harness for `[[octave-shapes]]`.

Related: `[[octave-shapes]]`, `[[caged-system]]`, `[[interval-lattice]]`, `[[fretboard-render-component]]`, `[[intervals-scales]]`, `[[interval-derivation-engine-vision]]`, `[[chordflow-architecture-reference]]`.