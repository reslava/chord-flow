---
type: idea
id: id_01KTVVSFHBF3995TWWEF09WJHH
title: Multi-bar Rhythm Patterns — per-bar timing variation
status: draft
created: "2026-06-11T00:00:00.000Z"
updated: 2026-06-11
version: 2
tags: []
parent_id: null
requires_load: []
---
# Multi-bar Rhythm Patterns — per-bar timing variation

## The idea

`RhythmPattern` is **already multi-bar-shaped** as of the `rhythm` thread
(`Bars: IReadOnlyList<PatternBar>`, with `RhythmPattern.SingleBar(...)` for the
common case) and the rhythm DSL already **parses** multi-bar input (`|`
separator). So a pattern can vary bar-to-bar today — most obviously a **fill in
the last bar**:

```text
X...X...X...X... |
X...X...X...X... |
X...X...X...X... |
X...X...X.X.XX..      # fill
```

What this thread owns is therefore **not a type change** — it's the *behaviour*
of using multi-bar patterns inside the Exercise/Song pipeline.

> **Why no refactor here:** per [[design-philosophy-durable-over-minimal]] we
> adopted the durable multi-bar **type up front** in the `rhythm` thread rather
> than shipping single-bar and breaking it later. This thread is the *additive
> feature layer* on top of that type.

## What this thread adds

### 1. Pattern ↔ progression alignment (refine the v1 default)

The rhythm slice ships a simple, defined default: **cyclic tiling** — progression
bar *i* uses `pattern.Bars[i % m]`. That's correct but blunt. This thread adds
the musical refinements:

- **Section-anchored fills** — the fill bar lands on the *last* bar of a section /
  progression / repeat, not merely every *m*-th bar.
- **Divisibility rules / validation** — when `n % m != 0`, decide: tile-and-
  truncate, require divisibility (error), or stretch. Surface it loudly rather
  than silently mis-aligning.
- **Pickup-into-section** interaction with the existing `PickupMeasure`.

### 2. Interaction with the Song layer

A `Song` spans many bars across sections; `song-design` decision **D** keeps
**one** `RhythmPattern` per song, so a multi-bar pattern stays a single reusable
asset — it does **not** reopen decision D. The alignment rule above applies
**per realized section** (each `RealizedSection` has its own bar count).

## Open questions to resolve when we start

- **Alignment when `n % m != 0`** — tile/truncate vs require-divisible vs stretch.
  (Leaning: tile, with fills anchored to section ends; validate and warn.)
- **Fill semantics** — is "fill" just "the last bar of the pattern, used on the
  last bar of the section," or a first-class tagged concept?
- **Per-section vs whole-song alignment phase** when a Song repeats a section.

## Status

Idea only — the *type* and `|` parsing live in the `rhythm` thread; this captures
the *alignment/fill semantics* so they aren't lost. No design/plan until the
`rhythm` slice lands and we choose to start it.

Related: [[chordflow-domain-model-reference]], [[design-philosophy-durable-over-minimal]], the `rhythm` thread, the `song` thread.