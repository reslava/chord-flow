---
type: req
id: rq_01KTXK59QABEQ6T9EZVSR9SEEG
title: Content packages & catalog — open-core content distribution — Requirements
status: locked
created: 2026-06-12
updated: 2026-06-12
version: 1
design_version: 6
tags: []
parent_id: de_01KTXEQWS29T4T2S0GKP7C23AB
requires_load: []
---
# Content packages & catalog — open-core content distribution — Requirements

### ✅ Included

- `IN1` Catalog-metadata model — `genre` / `subgenre` / `tags` on all four content entities (`Progression`, `Song`, `RhythmPattern`, `Voicing`), carried as a self-describing DSL header and parsed/denormalized into entity columns; the DSL header stays the canonical source and round-trips 1:1.
- `IN2` Provenance `Origin` model — `BuiltIn` / `UserDefined` / `Pack{PackId}`, stored as an entity column (discriminator + optional `PackId`).
- `IN3` Single Id-keyed `Origin` resolver with precedence `UserDefined > Pack > BuiltIn`, shadowing non-destructively (lower tiers remain on disk as fallback, so removing a local restores the next tier down).
- `IN4` Pack bundle format — a `manifest.json` (`id`, `name`, `version`, `kind`, `provenance`, `requires`) plus per-kind folders (`progressions/`, `songs/`, `rhythms/`, `voicings/`) of `.dsl` definitions.
- `IN5` Idempotent import-by-Id — re-importing upserts by definition Id with no duplicates; headless, reusing the existing seed importer.
- `IN6` Default pack = today's `SeedData` generalized into the first bundle (the free starter set), imported at first run.
- `IN7` `Song.OfProgression` inherits the source progression's `genre` / `subgenre` / `tags` (the lift copies, never empties).
- `IN8` Resolve-time fail-loud when a pack's definition references a missing definition (same rule as Song→Progression refs).

### ❌ Excluded

- `EX1` Pack authoring / export tooling.
- `EX2` Pack versioning, dependency resolution, and signing / integrity.
- `EX3` Import UI surface — the model + headless idempotent import land now; the UI is deferred.
- `EX4` Sell flow + entitlement / licensing.

### ⛓ Constraints

- `C1` Catalog metadata and `Origin` live on the Entity layer (`ChordFlow.Core/Persistence/`), never on pure `Domain/` records — `Domain/` stays music-theory-pure.
- `C2` Pack import is a Feature in `ChordFlow.Core/Features/`; the Desktop → Core dependency direction is unchanged (import UI, when built, is a host concern).
- `C3` Tags persist as a JSON-array `TEXT` column (v1), round-tripped 1:1 with the canonical DSL header; any later move to a join table must be an additive read-model migration that never touches the DSL header.
- `C4` Manifest `kind` is the coarse pack-type discriminator (`content` vs. future `soundfont`/`theme`); each definition's kind derives from its folder, not a per-file field — enabling mixed packs.
- `C5` A pack is data only — it adds rows, never code; importing a pack requires zero engine change.