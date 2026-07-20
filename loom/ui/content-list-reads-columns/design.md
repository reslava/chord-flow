---
type: design
id: de_01KY0A901J4AP6E4SQTQZG28H2
title: Content List() reads the denormalized columns (retire header-parse + backfill)
status: done
created: 2026-07-20
version: 1
idea_version: 1
tags: []
parent_id: id_01KY09RF449JNPQ451Q0P2YB04
requires_load: []
---
# Content List() reads the denormalized columns (retire header-parse + backfill)

## 1. Context

This thread is the deferred **EX2** of `content-metadata-editing`. That thread shipped **scope option A**: every catalog store's `Save` now populates the denormalized `ICatalogEntity` `Genre`/`Subgenre`/`Tags` columns from the final merged metadata — but each store's `List()` **still re-parses the DSL header** (`CatalogHeader.Parse(dsl).Metadata`) to surface catalog metadata. The columns are truthful; the read path just doesn't use them yet. This thread flips `List()` to read the columns and retires the per-row header parse — the A→B completion.

**Grounded in code (verified):**

- All four stores' `List()` parse the header for genre/subgenre/tags: `ProgressionStore.List` line 34, `SongStore.List` line 39, `VoicingStore.List` line 29, `DrumGrooveStore.List` line 31 — each hands `CatalogHeader.Parse(x.Dsl).Metadata` to the shared `ContentSummaries.Build`.
- **`ContentSummaries.Build` consumes only `Meta.Genre` / `Meta.Subgenre` / `Meta.Tags`** (`IContentStore.cs` line 127) — **not** description, **not** tonality. So a summary built from the three columns is complete; nothing else on the metadata record is read on the list path.
- The columns exist (`ICatalogEntity.Genre/Subgenre/Tags`) and are populated by both `PackImporter` (every pack row, on every import — lines 117–119, 155–201) and `Save` (user rows, from `content-metadata-editing` onward).
- `CatalogHeader.SerializeTags` / `DeserializeTags` round-trip the tags list 1:1 via JSON (`CatalogHeader.cs` 126–136) — the column ↔ header encoding is identical.

**The backfill is small (idea's key discovery, confirmed):** pack rows self-heal on every import; user rows saved since `content-metadata-editing` are truthful. The *only* gap is user rows saved **before** `content-metadata-editing` shipped — a tiny solo-dev set with a header but empty columns.

## 2. Two stores are only *partial* — keep their extra header parse

The idea flagged Song as partial; **`ProgressionStore` is partial too**, for a different reason. Neither can lose its header parse entirely:

- **`SongStore.List`** — `SeedsOf` (lines 53–70) parses the header **and the song body** for `InitialKey` / `DefaultFeel` / `DefaultTempo` / `InitialKeyIsMinor`. Only the **genre/subgenre/tags** read moves to columns; the seed parse stays.
- **`ProgressionStore.List`** — `IsMinorTonality` (lines 41–45) parses the **winning row's header** for `tonality:` → `InitialKeyIsMinor`. That stays; only genre/subgenre/tags move to columns.
- **`VoicingStore` / `DrumGrooveStore`** — the *only* two that flip fully: their `List()` parses the header for **nothing but** genre/subgenre/tags, so after the flip they touch no header at all.

`tonality` is deliberately **not** a denormalized column (only `Genre`/`Subgenre`/`Tags` are), so the Progression/Song tonality reads must stay header-derived. That is correct and in scope to preserve — do not try to "finish the job" by removing them.

## 3. Goal & non-goals

**Goal:** each catalog store's `List()` reads catalog metadata (genre/subgenre/tags) from the denormalized columns instead of parsing the DSL header, and legacy user rows are backfilled so nothing vanishes from the lists the instant the read flips.

**Non-goals / preserve:**
- **Song seeds + `InitialKeyIsMinor`** (`SeedsOf`) and **Progression `InitialKeyIsMinor`** (`IsMinorTonality`) — unchanged; their header/body parse stays (§2).
- **No schema change** — the columns already exist; this is a read-path + one-time-data change only.
- **No change to `Get` / `Save` / the bridge / the UI** — this is purely the `List()` read path + the backfill.

## 4. Mechanism

### 4.1 The read flip — feed `ContentSummaries.Build` from columns

`ContentSummaries.Build` currently takes a `CatalogMetadata` per row and reads only its `Genre`/`Subgenre`/`Tags`. Two shapes considered:

- **(A)** Keep the `Build` signature; each store constructs a throwaway `CatalogMetadata` from its columns (`new CatalogMetadata(row.Genre, row.Subgenre, CatalogHeader.DeserializeTags(row.Tags), null, default)`).
- **(B, chosen)** Change `Build`'s row tuple to carry `string? Genre, string? Subgenre, IReadOnlyList<string> Tags` **directly**, dropping the `CatalogMetadata` intermediary on the read path. The list path stops speaking `CatalogMetadata` at all — it reads columns → summary, which is exactly what the denormalization *means*.

**Chosen: (B)** — it removes the fiction of reconstructing a full metadata record (with null description / default tonality) just to read three fields, and makes the "columns are the list-path source" intent explicit in the shared helper's type. Each store's `List()` then selects `(Id, Name, Origin, PackId, Genre, Subgenre, DeserializeTags(Tags))` from the row.

After the flip:
- **Voicing / Drums** — `List()` reads columns only; no `CatalogHeader.Parse` remains.
- **Progression / Song** — `List()` reads columns for g/s/t; the separate `IsMinorTonality` / `SeedsOf` header parse stays untouched.

### 4.2 The backfill — a startup reconcile pass (established precedent)

The codebase already has this exact pattern: **`ContentSourceMigration.Run(db)`** runs at `Program.cs:99`, right after `db.Database.Migrate()` and `DefaultPack.ImportInto(db)` — an idempotent C# startup pass ("a no-op once migrated"). The backfill is a sibling of it.

- **Not an EF SQL migration** — the header parse is C# (`CatalogHeader.Parse`); expressing it in `migrationBuilder.Sql` would reimplement the parser in SQL. Rejected.
- **Not lazy "populate on next touch"** — that leaves `List()` needing a `column ?? parse-header` fallback, which *defeats the whole point* (retiring the header parse). Rejected.
- **Chosen: an idempotent startup reconcile** — `CatalogColumnBackfill.Run(db)` (new, in `Persistence/`), invoked in `Program.cs` after `ContentSourceMigration.Run(db)`. For each catalog entity's rows: parse the header, and where a column disagrees with the header value, set it from the header; `SaveChanges` once. This **reconciles columns ← header** (header stays canonical per parent-C2), so it is idempotent (a no-op once consistent), self-healing against any future drift, and cheap at catalog scale. It is a dedicated single-responsibility pass (not folded into `ContentSourceMigration`) so it is unit-testable in isolation.

Reconcile scope is **all rows**, not just user rows: packs are already truthful (importer runs first each startup), so reconciling them is a verified no-op; scoping to user-only would add a branch for no benefit. Simplicity wins.

## 5. Sequencing

1. **Read-path core** — change `ContentSummaries.Build` to accept g/s/t directly (shape B); update all four stores' `List()` to select the columns. Voicing/Drums lose their header parse; Progression/Song keep `IsMinorTonality`/`SeedsOf`. Store-level tests.
2. **Backfill** — `CatalogColumnBackfill.Run(db)` reconciling columns ← header for every catalog entity; wire it into `Program.cs` after `ContentSourceMigration.Run`. Backfill test (legacy row: header set, columns empty → after Run → columns truthful → `List()` surfaces it).
3. **Doc sync** — update the `ContentSummary` XML doc-comment (`IContentStore.cs` lines 80–84) which currently says *"`List` still reads the header; switching the read path is deferred"* → now reads the columns; and refresh `chordflow-architecture-reference.md` where it describes the list read path (ref-sync rule, same unit of work).

## 6. Testing

- **Read flip (per store):** a row whose **header and columns intentionally differ** (column genre = X, header genre = Y) — `List()` surfaces **X** (the column wins; proves the header is no longer read). Run *without* the backfill so the divergence survives.
- **Backfill:** a **legacy user row** (header set, columns null/empty) → run `CatalogColumnBackfill.Run` → columns now match the header → `List()` surfaces the metadata. Re-running `Run` is a no-op (idempotence).
- **No regression:** `SongStore` `InitialKey`/`DefaultFeel`/`DefaultTempo`/`InitialKeyIsMinor` and `ProgressionStore` `InitialKeyIsMinor` are unchanged after the flip; existing pack content still shows its genre/subgenre/tags in the Content + Practice filters.
- **Tags round-trip:** a row with `tags: [12-bar, beginner]` in the header and the matching serialized column surfaces the same list from the column path (`DeserializeTags`).
