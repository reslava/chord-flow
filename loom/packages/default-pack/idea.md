---
type: idea
id: id_01KV06Z7C68HW71XF76ESWT203
title: Default pack — the curated free starter content bundle (all entities)
status: done
created: "2026-06-13T00:00:00.000Z"
updated: 2026-06-14
version: 2
tags: []
parent_id: null
requires_load: []
---
# Default pack — the curated free starter content bundle (all entities)

## The idea

The **default pack**: the curated, free **starter content** that ships with
ChordFlow — the actual `.dsl` definitions for **every content entity**
(progressions, songs, rhythm patterns, **voicings**), packaged as the first
bundle and imported at first run through the **content-catalog** pack-import
tooling.

This thread owns the **content**; the `packages/content-catalog` thread owns the
**mechanism** (pack format, idempotent import, provenance). Clean split, matching
the layer-based weave organization: *content-catalog = how packs work; default-pack
= the curated bundle that flows through it.*

## Why a thread of its own (and why now)

- Voicings just landed as the fourth content entity, but the system has **zero
  authored voicings** — every lookup falls to the generated shell. The default
  pack is what makes the stored-first `VoicingBook` (and authored content
  generally) **observable** in the shipped app.
- Curating real content is ongoing, distinct work from the import machinery — and
  it grows (future paid packs are the same shape of work).

## In scope

- **Generalize today's `SeedData`** (the built-in progressions / songs / rhythm
  patterns, currently seeded per-entity via `SeedBuiltIn*`) into the **default
  bundle** — content-catalog Phase 2 step 6.
- **Author the built-in voicing content** — the meaty new part: real CAGED shapes
  for the common qualities (maj / min / dom7, and the rest) across the C·A·G·E·D
  families, authored once at the canonical **C** anchor in the Voicing DSL.
- Package as the bundle the idempotent importer ingests at first run.

## Explicitly NOT this thread

- **No per-entity `SeedBuiltInVoicings`** — that's the retired pattern; built-in
  voicings ride the **pack-import path**, not a bespoke seed method
  ([[design-philosophy-durable-over-minimal]]).
- The **import tooling / pack format / provenance** — that's `content-catalog`.
- **Paid / additional packs** — additive later, same machinery.
- **The authoring/import UI** — the `ui` weave.

## Dependency

**Blocked on `content-catalog` Phase 2** (plan steps 4–6: pack bundle format →
idempotent import → default-pack import path). Phase 2 is now **unblocked** (it was
waiting for voicings to be a real entity). Natural sequence: finish content-catalog
Phase 2 → author/package the default pack here → surface it in the `ui` weave.

Related: the `packages/content-catalog` thread (the mechanism), the
`domain/voicings` thread (the Voicing DSL + entity this content targets),
[[design-philosophy-durable-over-minimal]].