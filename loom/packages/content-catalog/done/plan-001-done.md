---
type: done
id: pl_01KTXKNDWB6S0EV90YGPMA8VMZ-done
title: Done — Content packages & catalog — model then tooling
status: done
created: "2026-06-12T00:00:00.000Z"
version: 6
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

## Step 4 — Pack bundle format — manifest.json (id/name/version/kind/provenance/requires) + per-kind folders of .dsl; manifest kind = coarse pack-type, each definition's kind derives from its folder (mixed packs)

**Pack bundle format (IN4, C4, C5).** New `Features/Packs/` slice — the format + the read seam; importing is step 5. Pure parsing split from the directory I/O so the model is testable without temp dirs.

- **`ContentKind.cs`** — `enum ContentKind { Progression, Song, Rhythm, Voicing }` + `ContentKinds.Folder()` (the single kind↔subfolder mapping: `progressions`/`songs`/`rhythms`/`voicings`) and `ContentKinds.All` (enumeration order — progressions/rhythms/voicings before songs so a song's same-pack refs already exist as rows; resolution is fail-loud at realize time regardless). This is the per-definition kind (from the folder, C4), distinct from the manifest's coarse pack-type `kind`.
- **`PackManifest.cs`** — `record PackManifest(Id, Name, Version, Kind, Provenance, Requires)` + pure `Parse(json)` via `System.Text.Json` (in-box, no new package). Tolerant: a private DTO maps missing fields to defaults (name→id, version→`0.0.0`, kind→`content`, requires→empty); **id is required** (it becomes each imported row's `PackId`) → `FormatException` if missing; malformed JSON → `FormatException`. `ContentKindLabel = "content"` const.
- **`PackDefinition.cs`** — `record PackDefinition(Kind, Id, Name, Dsl)` + `record ContentPack(Manifest, Definitions)`.
- **`PackDefinitionFile.cs`** — pure `Read(kind, fileName, fileText)`: **filename stem = Id** (design §6.4); peels an optional leading **`name:`** line from the header block (recognized anywhere in the contiguous header, not just first line), falling back to a title-cased id; leaves the catalog header (`genre`/`subgenre`/`tags`) in the stored `Dsl` so the importer denormalizes it exactly like seeding. The `name:` line is removed from `Dsl` because it is not part of any entity grammar; an empty `name:` value → title-cased id.
- **`PackReader.cs`** — the I/O seam: `ReadFromDirectory(dir)` reads `manifest.json` (`FileNotFoundException`/`DirectoryNotFoundException` when absent), validates `kind == "content"` (`NotSupportedException` for future soundfont/theme), then walks each present kind folder reading `*.dsl` (each folder optional → mixed packs), composing the two pure parsers.

**Tests** — new `PackFormatTests` (16): manifest full/defaults/missing-id/malformed; per-file identity (filename=id, name-peeled + catalog-header survives and re-parses via `CatalogHeader.Parse`, name-after-catalog-lines, no-name title-case, empty-name fallback); reader (mixed pack, absent folders skipped, missing manifest, unsupported kind, missing directory) using a throwaway temp-dir `TempPack` helper. **327/327 pass.**

**Forward note (not this thread):** the voicing load path (`VoicingStore.LoadShapes`) does not yet strip a catalog header before `VoicingDslParser.Parse`. It doesn't bite here — the default pack (step 6) generalizes today's seed (progressions/songs/rhythms only; voicings have no seed). When `packages/default-pack` ships authored voicings with catalog headers, that strip must be added there.

## Step 5 — Idempotent import-by-Id — upsert by definition Id (no duplicates), reusing the seed importer; resolve-time fail-loud when a referenced definition is missing

**Idempotent import-by-Id + the coexistence storage (IN5, IN8, C2; resolves the IN3 storage parked at step 3).** Decisions settled with the user in `content-catalog-chat-002`: **D2 = Option A** (composite `(Id, Origin)` PK so tiers physically coexist), **D3 = caller-declared origin**.

- **Composite PK `(Id, Origin)`** on all four content tables (`OnModelCreating`: `HasKey(x => new { x.Id, x.Origin })`). BuiltIn/Pack/UserDefined copies of one id coexist as separate rows; "no duplicates" (IN5) = no two rows of the same `(Id, Origin)`. **EF migration `20260613190718_ContentCompositeKeyIdOrigin`** — drops + re-adds each PK as `(Id, Origin)` (SQLite table-rebuild at SQL-gen time; applies cleanly in every in-memory-SQLite test).
- **`OriginResolver` finally wired into the real load paths** (was pure-but-unused after step 3): `ProgressionStore.Find`/`RhythmPatternStore.Find` load all rows for an id and `ResolveOne` the top tier; `VoicingStore.LoadShapes` `Resolve`s one shape per id; `Find` resolves one. Non-destructive falls out — delete a higher tier and the next wins next resolve.
- **`VoicingStore` now strips an optional catalog header** (`CatalogHeader.Parse`) before `VoicingDslParser`, matching the progression/song load path — closes the forward-note gap from step 4 (so an imported voicing carrying genre/tags loads correctly).
- **`Persistence/Entities/ICatalogEntity.cs`** — `ICatalogEntity : IOriginated` adds the shared mutable catalog fields (`Name`/`Dsl`/`PackId`/`Genre`/`Subgenre`/`Tags`); `ProgressionEntity`/`SongEntity`/`VoicingEntity` implement it so one generic upsert serves all three.
- **`Features/Packs/PackImporter.cs`** — `Import(ContentPack, Origin)`: guards origin ∈ {BuiltIn, Pack} (UserDefined → `ArgumentException`); `PackId = manifest.Id` only for `Pack`; upserts each definition by **key lookup `set.Find(id, origin)`** (avoids translating an interface-typed predicate), denormalizing the catalog header into columns exactly like seeding and keeping the canonical header in the stored `Dsl`; one `SaveChanges`; returns the count. **IN8** (fail-loud refs) is the existing `SongExpander.Resolve` throw at realize time — the importer adds no pre-validation, it just doesn't swallow it.
- **`SeedBuiltIn*` existence checks** are now Origin-aware (`Where(Origin == BuiltIn)`) so a coexisting Pack/user row of the same id can't suppress built-in seeding under the composite key.

**Tests** — new `PackImportTests` (7): Pack-origin stamping + catalog denormalization; idempotent same-tier upsert (no dup, values replaced); BuiltIn import (null PackId); UserDefined rejected; **three tiers coexist and resolve highest non-destructively** via `ProgressionStore`; rhythm round-trips through `RhythmPatternStore`; missing-progression song ref fails loud at `SongExpander.Expand` (IN8). **334/334 pass; full solution builds (Desktop→Core intact).**

**Refs updated** — `chordflow-domain-model-reference` §6 (composite PK, `IOriginated`/`ICatalogEntity`, resolver-wired stores, VoicingStore header-strip) and `chordflow-architecture-reference` §3 (the `Packs` Features slice).

## Step 6 — Default-pack import path — generalize today's SeedData structure into the first bundle (manifest + per-kind .dsl files) and import it at first run via the same idempotent importer; curated content (incl. authored voicings) is the packages/default-pack thread

**Default-pack import path (IN6).** Decision **D4 = A**: the on-disk bundle is now the canonical source for built-in content; the hardcoded seed is retired.

- **New bundle `src/ChordFlow.Core/Content/default-pack/`** — `manifest.json` (`id: default`, `kind: content`) + `progressions/{12bar_blues,jazz_blues_turnaround}.dsl` + `rhythms/{beat_1,beat_1_3,quarters}.dsl` + `songs/blues_song_demo.dsl`. Each file = `name:` header + (catalog header for the song) + the entity grammar, authored **verbatim** from the old `SeedData` strings → byte-identical imports.
- **csproj** — `<Content Include="Content\**\*" CopyToOutputDirectory="PreserveNewest" />`; verified the bundle copies transitively to **both** the Desktop host output and the test bin.
- **`Features/Packs/DefaultPack.cs`** — `Directory` (resolved from `AppContext.BaseDirectory`, host-agnostic), `Load()` → `PackReader`, `ImportInto(db)` → `PackImporter.Import(pack, Origin.BuiltIn)` (idempotent, safe every run).
- **`Program.cs`** — the three `db.SeedBuiltIn*()` calls replaced by a single `DefaultPack.ImportInto(db)` after `Migrate()`.
- **Retired** — `SeedData.BuiltIn{Progressions,Songs,RhythmPatterns}` lists + the `ProgressionDefinition`/`SongDefinition`/`RhythmPatternDefinition` records, and `ChordFlowDbContext.SeedBuiltIn{Progressions,Songs,RhythmPatterns}()` (+ the now-unused `using ChordFlow.Domain;`). **Kept** the live domain constants (`TwelveBarBlues`, `Beat1`/`Beat1And3`/`Quarters`, `RhythmPatterns`, `AllMajorKeys`).
- **Fix** — `PackDefinitionFile.Read` now `TrimEnd()`s the parsed DSL: a `.dsl` file's conventional trailing newline was leaking into the last chord token (`"57\n"` → unknown-suffix `FormatException`). Trailing whitespace is meaningless in every grammar.

**Tests** — the five seed/persistence test files (`ProgressionSeedTests`, `RhythmPatternSeedTests`, `SongSeedTests`, `SongPersistenceTests`, `RhythmPatternPersistenceTests`) rewritten to drive the default pack (`DefaultPack.Load()` for theory data, `DefaultPack.ImportInto(db)` for the DB path) instead of the removed `SeedData.BuiltIn*`/`SeedBuiltIn*`. **334/334 pass; full solution builds; bundle present in Desktop output.**

**Refs updated** — `chordflow-domain-model-reference` §3/§6 (domain-constants row, rhythm/voicing store rows, `DefaultPack.ImportInto` replaces the seeder row) and `chordflow-architecture-reference` §7 ("content is data" now realized via the default pack). The `chordflow-dsl-reference` "Content packs" section (added in step 4) already documents the bundle + per-file identity end-users/pack authors see.

**Handoff to `packages/default-pack`:** the bundle + import path exist with the migrated prog/song/rhythm content; that thread now curates/grows it (authored CAGED voicings — recall `VoicingStore` already strips catalog headers as of step 5 — plus more genres) purely by adding files.
