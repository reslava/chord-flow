---
type: idea
id: id_01KVQGK9R3CNMJZRZVN3V65SJB
title: Triplet Feel (\tf) — span/song-level swing
status: draft
created: 2026-06-22
version: 1
tags: []
parent_id: null
requires_load: []
---
# Triplet Feel (\tf) — span/song-level swing

## The idea

A **triplet feel / swing** that applies across a **run of bars or a whole song** — the way a jazz-blues is
"swung" end-to-end — rather than per-beat. alphaTab has a **native directive for exactly this: `\tf`
(tripletFeel)**, which swings both rendering and playback for the span it covers.

## Relationship to our existing `Feel` model (the design fork)

This **overlaps** `Feel { Straight, Swing, Shuffle, Triplet }`, which today is applied as a
**playback-time tick warp** (`FeelTransform` / `AlphaTexRenderer.WarpBars`) — we reshape event ticks
ourselves before emitting. `\tf` is a **native engine directive** that lets alphaTab own the swing.

So there's a real design decision before any code:
- **Keep warping ticks ourselves** (current `FeelTransform`) — full control, but we reimplement what the
  engine already does, and the notation still *looks* straight.
- **Delegate to `\tf`** — alphaTab swings render + playback natively and the score reads correctly; we'd
  emit `\tf <feel>` for a span and stop warping those events. Question: how `\tf` interacts with our
  tick-grid model and `Feel` enum, and whether the two approaches can coexist or one replaces the other.

This is a design-doc conversation, not a quick add.

## Scope to start

alphaTab supports many `\tf` values; **start with just**: `none`, `triplet16th`, `triplet8th`.
(Full value list captured in `anacrusis-chat-001` for reference.)

## Out of scope / distinct axis

- **Per-beat tuplets (`tu`)** are a *different* axis (a single beat subdivided), already modeled. Note: a
  report that `tu` triplets aren't rendering in the app should be chased as a **separate small bug**, not
  conflated with `\tf`.

## Open questions

- Does `\tf` replace `FeelTransform` for swing/shuffle, or layer with it? (C4 in the domain req says Feel
  is never baked into a pattern — `\tf` as a render-time directive is consistent with that.)
- Per-section vs whole-song span — and how it interacts with the Song layer / section repeats.

Related: `domain/rhythm` (the `Feel`/`FeelTransform` model), [[chordflow-domain-model-reference]],
`chordflow-dsl-reference`, the alphaTex `\tf` reference.