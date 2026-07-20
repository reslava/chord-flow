---
type: done
id: pl_01KY0C0FFZJVZN3PNABRA8A8KR-done
title: Done — Flip List() to the denormalized columns + backfill legacy rows
status: done
created: 2026-07-20
version: 3
tags: []
parent_id: pl_01KY0C0FFZJVZN3PNABRA8A8KR
requires_load: []
---
# Done — Flip List() to the denormalized columns + backfill legacy rows

## Step 1 — Change ContentSummaries.Build to accept genre/subgenre/tags directly (no CatalogMetadata intermediary), and update each store's List() to select the denormalized columns (Genre/Subgenre/DeserializeTags(Tags)) instead of CatalogHeader.Parse(x.Dsl).Metadata.

Flipped the list read path from the DSL header to the denormalized columns.

**`ContentSummaries.Build` (IContentStore.cs)** — row tuple changed from `(…, CatalogMetadata Meta)` to `(…, string? Genre, string? Subgenre, IReadOnlyList<string> Tags)`; projects the three straight onto `ContentSummary`. The list path no longer speaks `CatalogMetadata` (IN2, shape B).

**Store `List()` updates:**
- `VoicingStore` / `DrumGrooveStore` — select `Genre`/`Subgenre`/`Tags` columns; `CatalogHeader.DeserializeTags(Tags)` for tags. **No `CatalogHeader.Parse` remains** on either list path (IN3).
- `ProgressionStore` — reads columns for g/s/t; `IsMinorTonality` (header → `InitialKeyIsMinor`) left untouched (C3/EX2).
- `SongStore` — reads columns for g/s/t; `SeedsOf` (header+body → key/feel/tempo/minor) left untouched (C3/EX2).
- `RhythmPatternStore` — updated to the new tuple shape (nulls + empty tags); rhythm has no columns.

**Tests (`ContentCrudStoreTests`):** replaced the old header-based `ProgressionList_SurfacesGenreSubgenreTags_FromTheCatalogHeader` with `…_FromTheColumns_NotTheHeader` — the row's header and columns intentionally disagree and the **column value wins** (proves the header is no longer read). Added the same column-wins coverage for Voicing and Drums, and `SongList_SurfacesGenreTags_FromTheColumns_WhileSeedsStayFromTheDsl` (metadata from columns, `InitialKeyIsMinor` still parsed from the DSL `key Am` — C3).

Build clean; full Core suite green (1161 passed).

## Step 2 — Add CatalogColumnBackfill.Run(db): for each catalog entity, parse the row's DSL header and, where a column disagrees, set it from the header (columns ← header), SaveChanges once. Wire it into Program.cs after ContentSourceMigration.Run(db). Idempotent, cheap.

Added the one-time-but-idempotent backfill and wired it into startup.

**New `Persistence/CatalogColumnBackfill.cs`** — `Run(ChordFlowDbContext db)` returns the count of rows updated. A generic `Reconcile<T>(DbSet<T>) where T : ICatalogEntity` loads each catalog set tracked, parses `CatalogHeader.Parse(row.Dsl).Metadata`, and overwrites `Genre`/`Subgenre`/`Tags` (`SerializeTags`) **only when they differ** from the header value — so a consistent row is never marked modified. Reconciles Progressions/Songs/Voicings/DrumGrooves (Rhythm has no catalog metadata — skipped); a single `db.SaveChanges()` at the end. Reconciles **columns ← header** (header canonical — C1); idempotent by construction (C4).

**`Program.cs`** — `CatalogColumnBackfill.Run(db)` invoked immediately after `ContentSourceMigration.Run(db)` in the startup DB block (after `Migrate` + `DefaultPack.ImportInto`).

**Tests (`ContentCrudStoreTests`):**
- `Backfill_PopulatesLegacyRowColumns_FromTheHeader_SoTheFlippedListSurfacesThem` — a legacy user row (header set, columns empty); before `Run`, the flipped `List()` shows null genre; after `Run` (returns 1) the columns match the header and `List()` surfaces the metadata (IN4).
- `Backfill_IsIdempotent_ASecondRunWritesNothing` — a legacy progression + a bare voicing (no header); first `Run` returns 1 (only the legacy row), second returns 0 — consistent rows never churned (C4).

Desktop build clean (only the pre-existing unrelated WindowsBase MSB3277 warning); full Core suite green (1163 passed).

## Step 3 — Update the ContentSummary XML doc-comment (no longer 'List still reads the header; switching the read path is deferred') and the list read-path note in chordflow-architecture-reference.md — same unit of work.

Synced the docs/refs that described the old header-read path.

- **`IContentStore.cs`** — the `ContentSummary` XML doc-comment now says genre/subgenre/tags are read from the denormalized `ICatalogEntity` columns (reconciled from the canonical header on write + by the startup `CatalogColumnBackfill`), replacing *"`List` still reads the header … switching the read path is deferred."*
- **`Entities/ICatalogEntity.cs`** — the interface doc-comment's stale closing sentence updated the same way (columns populated on every write + reconciled by `CatalogColumnBackfill`; `List()` reads them; header stays canonical).
- **`chordflow-architecture-reference.md`** (§5, via `loom_patch_doc`) — the `entitySave` metadata paragraph's parenthetical *"(though `List()` still reads the header … deferred)"* replaced with the flipped read path + the `CatalogColumnBackfill` reconcile note.

Core build clean (0 warnings) — the new `<see cref>`s resolve.
