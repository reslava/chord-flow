---
type: idea
id: id_01KVRK4H1NJAAYE5K377BJXEQK
title: Default triplet feel as Song catalog metadata
status: done
created: 2026-06-22
version: 1
tags: []
parent_id: null
requires_load: []
---
# Default triplet feel as Song catalog metadata

## The idea

Let a **Song** (and maybe a Progression / Rhythm) carry a **default / suggested triplet feel** so that
**picking that content pre-selects the swing** — e.g. a jazz blues defaults to `Triplet8th`, a straight
rock tune to `None` — instead of the user re-choosing the feel every time.

This is the deferred follow-up to the shipped [[triplet-feel-idea]] thread (v0.10.0), which deliberately
kept feel a **play-time choice** persisted on the *Exercise*, not the content.

## The tension — this needs a C4 decision first

Domain invariant **C4** says feel is *never baked into a Progression / Song / Rhythm* — it's chosen at
play time (that's exactly what kept the triplet-feel thread clean). A default feel **on the content**
appears to contradict C4.

The likely resolution: treat the default as **catalog metadata** — a *suggestion* the exercise
generator reads as the initial feel, in the same family as `genre:` / `subgenre:` / `tags:` — kept
strictly distinct from the **realized rhythm** (which still carries no feel; the swing still happens at
render via `\tf`). That is a real, deliberate **C4 carve-out / amendment**, and it's the first thing to
settle in design before any code.

## Scope to decide

- **Which entities?** Song first (the obvious home). Progression / Rhythm too, or only Song?
- **Where does it live?** A catalog-header field (e.g. `feel: triplet8th`) parsed by the catalog-header
  layer, stored as a metadata column — *not* a grammar token in the body (keeps the body feel-free).
- **Generator wiring:** when an exercise is generated from content carrying a default feel, that becomes
  the initial `Exercise.TripletFeel`; the play-time control still overrides it (Exercise param wins).
- **Interaction with the play-time param:** content default → seeds the Exercise → user can change it in
  the score transport (the override path already exists).

## Open questions

- Is a per-content default enough, or will users eventually want **per-section** feel (which is a bigger,
  separate axis — explicitly out of scope of the triplet-feel thread)?
- Does the default feel travel in **content packs** (it's just another catalog-metadata field, so yes by
  construction) — confirm the pack format carries it.

Related: [[triplet-feel-idea]], [[triplet-feel-design]], `chordflow-dsl-reference`,
[[chordflow-domain-model-reference]] (C4).