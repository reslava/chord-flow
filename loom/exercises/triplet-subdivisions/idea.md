---
type: idea
id: id_01KY2K58KS7MSG3XZNE65GZ0E0
title: Triplet subdivisions in the rhythm generator
status: draft
created: 2026-07-21
version: 1
tags: []
parent_id: null
requires_load: []
---
# Triplet subdivisions in the rhythm generator

**Deferred (req `EX3`, Phase 5).** This idea records the **design-fit analysis** (Rafa's question, chat-001): do triplets fit the generator's model? **Yes — with zero core redesign.** Captured so we can pick it up cleanly.

## Why they fit for free

- **Tick grid.** `TickGrid.Ppq = 48` is divisible by **3** (eighth-triplet = 16 ticks) and **6** (16th-triplet = 8 ticks). The onset model already validates *"subdivision must divide 48"*, so `Block(3, …)` / `Block(6, …)` are valid **today** — no model change.
- **Kinds & figures.** `Placement(3, region, onsetCount)` enumerates triplet-grid bars just like the straight grid; triplet **figures** (swing/shuffle, triplet fills) are pure data in `GrooveFigures`. No engine change.
- **Projections.** Both `OnsetGrid → RhythmPattern` and `→ DrumGroove` are pure tick math → subdivision 3/6 flows through unchanged. The `RhythmQuantizer` + `DrumGrooveRenderer` **already emit triplets** as `{tu 3}` tuplets (domain-model ref), so drums and legato render them.

## Where triplets need real work (the honest part)

1. **Random strategy's base grid.** Today it's a fixed **sixteenth** grid (`BaseSubdivision = 4`); 3 doesn't divide 16. To place/mix triplets, the Random walk needs a **PPQ-aware base grid** (48 cells/beat) or a **per-beat subdivision** choice (a bar of straight beats + triplet beats — the Rhythm DSL already supports per-beat `:n` runs). This is the one chunky piece.
2. **Count overlay.** The `1 e & a` overlay assumes 4 sixteenths; triplets need a **triplet-count variant** (`1 & a` / `1 la li`) — display-only, in DrumsR.
3. **Region semantics** for the triplet grid (on-beat vs the two triplet off-positions) — minor.
4. **Legato + triplets** inherit the existing Phase-4 *syncopated-legato* caveat (a notatable-safe policy for arbitrary bars); regular triplet patterns render fine.

## Shape when picked up

Additive: a **subdivision option (`:3` / `:6`)** in the Pattern controls + resolver; a set of **triplet figures**; the **triplet count-overlay**; and the **Random base-grid rework** (#1). No change to the onset model, the projections, or the selection/behaviour layer. Sequence after the current Phase-2 line settles.
