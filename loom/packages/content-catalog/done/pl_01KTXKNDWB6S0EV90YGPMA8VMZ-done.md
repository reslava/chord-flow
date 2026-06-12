---
type: done
id: pl_01KTXKNDWB6S0EV90YGPMA8VMZ-done
title: Done — Content packages & catalog — model then tooling
status: done
created: "2026-06-12T00:00:00.000Z"
version: 3
tags: []
parent_id: pl_01KTXKNDWB6S0EV90YGPMA8VMZ
requires_load: []
---
# Done — Content packages & catalog — model then tooling

## Step 1 — Catalog-metadata DSL header (genre/subgenre/tags) — parse into denormalized entity columns; canonical header stays source of truth and round-trips 1:1, across all four content entities

**Catalog-metadata mechanism (IN1, C1, C3).** Shared, entity-layer header parse/serialize + denormalized columns on the one existing content entity; the other three adopt it for free when persisted.

- **New `Persistence/CatalogMetadata.cs`** — `record CatalogMetadata(Genre, Subgenre, Tags)` value (Entity layer, never `Domain/` — C1), with `Empty` + `IsEmpty`.
- **New `Persistence/CatalogHeader.cs`** — the shared mechanism: `Parse(dsl) → (metadata, body)` strips an optional contiguous `genre:`/`subgenre:`/`tags:` leading block; `Serialize(meta, body)` is its deterministic inverse (round-trips 1:1, C3); `SerializeTags`/`DeserializeTags` for the JSON `TEXT` column. The pure `ProgressionParser` only ever receives the stripped body (C1).
- **`ProgressionEntity`** — added denormalized `Genre`/`Subgenre` (nullable) + `Tags` (JSON `TEXT`, default `[]`); `Dsl` doc updated to note the optional leading header.
- **`ChordFlowDbContext`** — seeding now denormalizes the header into the columns (no-op for today's header-less built-ins); `Tags` column defaults to `[]`.
- **EF migration `20260612100912_AddCatalogMetadata`** — additive: three columns only.
- **Tests** — new `CatalogHeaderTests` (no-header passthrough, full-header extraction, stop-at-first-non-header, Serialize↔Parse round-trip, tags JSON round-trip, header-body feeds Domain parser); extended `ProgressionPersistenceTests` (columns round-trip + `json_each()` tag filter proving C3). **173/173 pass.**

## Step 2 — Origin provenance model (BuiltIn / UserDefined / Pack{PackId}) stored as an entity column — discriminator + optional PackId

**Origin provenance model (IN2, C1).** Moved + renamed the provenance enum to the Entity layer and added the pack discriminator.

- **New `Persistence/Origin.cs`** — content-neutral `enum Origin { BuiltIn, UserDefined, Pack }` (was `Domain/ProgressionOrigin` with only BuiltIn/UserDefined). Lives in `Persistence/` per C1 (provenance is Entity-layer, never `Domain/`). Doc notes the `UserDefined > Pack > BuiltIn` precedence is resolved explicitly by the step-3 resolver, not by enum declaration order.
- **Deleted `Domain/ProgressionOrigin.cs`** — and dropped the now-unused `using ChordFlow.Domain;` from `ProgressionEntity`.
- **`ProgressionEntity`** — `Origin` property retyped to `Origin`; added nullable `PackId` (non-null only for `Origin.Pack`) — the design's "discriminator + optional PackId".
- **`ChordFlowDbContext`** — seeding + doc reference updated to `Origin.BuiltIn`.
- **EF migration `20260612101411_AddPackProvenance`** — additive: one nullable `PackId TEXT` column (the `Origin` string column already existed; `Pack` is just another name value).
- **Tests** — `ProgressionPersistenceTests`/`ProgressionSeedTests` updated to `Origin.*`; new `PackProvenance_StoresOriginByName_AndRoundTripsPackId` (Origin=`Pack` stored by name, PackId round-trips). **174/174 pass; full solution builds (Desktop→Core intact).**
- **Ref doc** — `chordflow-domain-model-reference.md` §6 updated for the rename, the catalog columns/`CatalogHeader`, `PackId`, and the header-stripping round-trip.

## Step 3 — Id-keyed Origin resolver with precedence UserDefined > Pack > BuiltIn, shadowing non-destructively (lower tiers remain as fallback)

**Origin precedence resolver (IN3).** Built as a pure, generic selection mechanism (option B — storage that lets tiers physically coexist is deferred to step 5/import).

- **New `Persistence/IOriginated.cs`** — `interface IOriginated { string Id; Origin Origin; }`; every content entity implements it so one resolver serves all four types.
- **New `Persistence/OriginResolver.cs`** — `Rank(Origin)` (`UserDefined` 2 > `Pack` 1 > `BuiltIn` 0), `Resolve<T>` (one winner per id, first-seen id order, tie keeps first-seen), `ResolveOne<T>(candidates, id)`. **Pure/read-only — selects, never mutates**, so non-destructive shadowing falls out by construction (removing a higher tier lets the next win on the next resolve).
- **`ProgressionEntity : IOriginated`** — existing `Id`/`Origin` satisfy the interface.
- **Tests** — new `OriginResolverTests` (rank ordering, highest-tier-per-id, first-seen ordering, the non-destructive fallback chain UserDefined→Pack→BuiltIn, unknown-id null, rank-tie keeps first). **182/182 pass.**

Phase 1 (catalog-metadata mechanism · Origin model · resolver) is complete. IN7 (`Song.OfProgression`) defers to the `song` thread; Phase 2 (IN4/IN5/IN6/IN8 — pack format, import, default pack) remains, with the coexistence-storage decision to be made at step 5.
