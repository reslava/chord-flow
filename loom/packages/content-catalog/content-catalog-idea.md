---
type: idea
id: id_01KTXEPV11EAA4DDY2A8MWTCB4
title: Content packages & catalog — open-core content distribution
status: done
created: "2026-06-12T00:00:00.000Z"
updated: 2026-06-12
version: 2
tags: []
parent_id: null
requires_load: []
---
# Content packages & catalog — open-core content distribution

## The idea

ChordFlow is **open-core**: the app + engine + a free starter set are open;
**curated content packs** (genre exercise libraries, signature-song
progressions, extended voicing books) are the optional paid layer — the Anki
model (free engine, paid content), proven for practice tools, with **no server
and no paywalled core**. Sold (later) via Gumroad / Lemon Squeezy / itch.io as
plain file downloads.

This thread owns two **cross-cutting** concerns that every content entity
(`Progression`, `Song`, `RhythmPattern`, `Voicing`) adopts:

1. **Catalog metadata** — genre / subgenre / tags, for filtering + pack organization.
2. **Provenance** — where a definition came from (`BuiltIn` / `UserDefined` /
   `Pack`), and the **pack-readiness** of the content model so a pack is
   *literally a bundle of definitions + a manifest* — an additive data drop,
   zero engine change.

> **Principle:** content is **self-describing data**. A pack adds rows; it never
> adds code. The constraint already in `ctx.md` — *"seed/library content loads
> from importable definition bundles, never hardcoded"* — is the foundation this
> thread formalizes.

## Locked direction (from `exercises-definition-ui-chat-001`)

- **Pack-readiness of the model — now.** Every content entity needs a stable
  **Id**, a canonical **Dsl**, **provenance** (`Origin`), and **catalog
  metadata** (genre/subgenre/tags). Lock these into each entity design now (no
  users — durable over minimal).
- **Pack = bundle of `.dsl` definitions + a `manifest.json`** (id, name, version,
  kind, provenance, optional dependencies). Import is **idempotent by Id**.
- **Default pack = today's `SeedData` generalized** into the first bundle — the
  free starter set, imported at first run.
- **Catalog metadata is entity/catalog-level, never on the pure `Domain/`
  records** — keeps `Domain/` music-theory-pure (C3/C4). Carried as a
  self-describing **DSL header** (`genre:`, `subgenre:`, `tags:`) and
  denormalized into entity columns for filter queries; the DSL stays canonical.
- **`Song.OfProgression` inherits the source progression's genre** — not empty.
  Genre lives on the entity/catalog; the lift copies it.

## Scope

**In (now):** the catalog-metadata model (genre/subgenre/tags), the
provenance/`Origin` model (`BuiltIn`/`UserDefined`/`Pack{PackId}`), the pack
manifest schema, idempotent import-by-Id, the default pack = generalized seed.

**Deferred (additive — designed closer to release):** pack *tooling* —
authoring/export UI, versioning, signing, the import UI surface, and the optional
sell flow + entitlement/licensing (a Features concern, not domain). Captured here
so the cross-cutting entity requirements are explicit; built when there's content
worth packaging.

Related: [[chordflow-architecture-reference]], [[chordflow-domain-model-reference]], [[design-philosophy-durable-over-minimal]], the `song` / `rhythm` / `voicings` threads, `loom/meta/general/chats/general-chat-001.md`.