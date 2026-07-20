---
type: plan
id: pl_01KY0C0FFZJVZN3PNABRA8A8KR
title: Flip List() to the denormalized columns + backfill legacy rows
status: done
created: 2026-07-20
updated: 2026-07-20
version: 1
design_version: 1
req_version: 1
tags: []
parent_id: de_01KY0A901J4AP6E4SQTQZG28H2
requires_load: []
target_version: 0.1.0
steps:
  - id: read-path-flip-build-four-stores
    order: 1
    status: done
    description: Change ContentSummaries.Build to accept genre/subgenre/tags directly (no CatalogMetadata intermediary), and update each store's List() to select the denormalized columns (Genre/Subgenre/DeserializeTags(Tags)) instead of CatalogHeader.Parse(x.Dsl).Metadata.
    files_touched: [src/ChordFlow.Core/Persistence/IContentStore.cs, src/ChordFlow.Core/Persistence/ProgressionStore.cs, src/ChordFlow.Core/Persistence/SongStore.cs, src/ChordFlow.Core/Persistence/VoicingStore.cs, src/ChordFlow.Core/Persistence/DrumGrooveStore.cs, tests/ChordFlow.Core.Tests/ContentCrudStoreTests.cs]
    blocked_by: []
    satisfies: [IN1, IN2, IN3, C2, C3]
  - id: backfill-catalogcolumnbackfill-startup-reconcile-pass
    order: 2
    status: done
    description: "Add CatalogColumnBackfill.Run(db): for each catalog entity, parse the row's DSL header and, where a column disagrees, set it from the header (columns ← header), SaveChanges once. Wire it into Program.cs after ContentSourceMigration.Run(db). Idempotent, cheap."
    files_touched: [src/ChordFlow.Core/Persistence/CatalogColumnBackfill.cs, src/ChordFlow.Desktop/Program.cs, tests/ChordFlow.Core.Tests/ContentCrudStoreTests.cs]
    blocked_by: []
    satisfies: [IN4, C1, C4]
  - id: doc-ref-sync
    order: 3
    status: done
    description: Update the ContentSummary XML doc-comment (no longer 'List still reads the header; switching the read path is deferred') and the list read-path note in chordflow-architecture-reference.md — same unit of work.
    files_touched: [src/ChordFlow.Core/Persistence/IContentStore.cs, loom/refs/chordflow-architecture-reference.md]
    blocked_by: [read-path-flip-build-four-stores, backfill-catalogcolumnbackfill-startup-reconcile-pass]
    satisfies: [IN5]
---
# Flip List() to the denormalized columns + backfill legacy rows

## Goal

Complete the A→B denormalization of catalog metadata (EX2 of content-metadata-editing). Today every catalog store's List() re-parses the DSL header (CatalogHeader.Parse(dsl).Metadata) purely to surface genre/subgenre/tags, even though Save + PackImporter already populate the denormalized ICatalogEntity columns. This plan flips the four stores' List() read path to the columns, retires the per-row header parse (fully for Voicing/Drums; only the g/s/t read for Progression/Song, which keep their tonality/seed header parse), and adds a one-time idempotent startup reconcile pass so legacy pre-content-metadata-editing user rows (header set, columns empty) don't vanish the instant the read flips. No schema change, no Get/Save/bridge/UI change — read path + backfill only.

---

## Steps

| Done | # | Step | Files touched | Blocked by | Satisfies |
|---|---|---|---|---|---|
| ✅ | 1 | Change ContentSummaries.Build to accept genre/subgenre/tags directly (no CatalogMetadata intermediary), and update each store's List() to select the denormalized columns (Genre/Subgenre/DeserializeTags(Tags)) instead of CatalogHeader.Parse(x.Dsl).Metadata. | src/ChordFlow.Core/Persistence/IContentStore.cs, src/ChordFlow.Core/Persistence/ProgressionStore.cs, src/ChordFlow.Core/Persistence/SongStore.cs, src/ChordFlow.Core/Persistence/VoicingStore.cs, src/ChordFlow.Core/Persistence/DrumGrooveStore.cs, tests/ChordFlow.Core.Tests/ContentCrudStoreTests.cs | — | IN1, IN2, IN3, C2, C3 |
| ✅ | 2 | Add CatalogColumnBackfill.Run(db): for each catalog entity, parse the row's DSL header and, where a column disagrees, set it from the header (columns ← header), SaveChanges once. Wire it into Program.cs after ContentSourceMigration.Run(db). Idempotent, cheap. | src/ChordFlow.Core/Persistence/CatalogColumnBackfill.cs, src/ChordFlow.Desktop/Program.cs, tests/ChordFlow.Core.Tests/ContentCrudStoreTests.cs | — | IN4, C1, C4 |
| ✅ | 3 | Update the ContentSummary XML doc-comment (no longer 'List still reads the header; switching the read path is deferred') and the list read-path note in chordflow-architecture-reference.md — same unit of work. | src/ChordFlow.Core/Persistence/IContentStore.cs, loom/refs/chordflow-architecture-reference.md | read-path-flip-build-four-stores, backfill-catalogcolumnbackfill-startup-reconcile-pass | IN5 |
---

### Legend

| Symbol | Meaning |
|--------|---------|
| ✅ | Done |
| 🔄 | In Progress |
| 🔳 | Pending |
| ❌ | Cancelled |

<!-- step:read-path-flip-build-four-stores -->
### Step 1 — Read-path flip — Build + four stores read columns

**Build (shape B):** change the row tuple `ContentSummaries.Build` accepts from `(Id, Name, Origin, PackId, CatalogMetadata Meta)` to `(Id, Name, Origin, PackId, string? Genre, string? Subgenre, IReadOnlyList<string> Tags)`, projecting those three straight onto the `ContentSummary`. The list projection no longer speaks `CatalogMetadata`.

**Per store `List()`** — select the columns from the row:
- `VoicingStore.List` / `DrumGrooveStore.List` — read `v.Genre, v.Subgenre, CatalogHeader.DeserializeTags(v.Tags)`; **no `CatalogHeader.Parse` remains** (IN3).
- `ProgressionStore.List` — read columns for g/s/t; **keep** `IsMinorTonality` (header → `InitialKeyIsMinor`) untouched (C3, EX2).
- `SongStore.List` — read columns for g/s/t; **keep** `SeedsOf` (header + body → InitialKey/DefaultFeel/DefaultTempo/InitialKeyIsMinor) untouched (C3, EX2).

**Tests (`ContentCrudStoreTests`):** per store, a row whose **column g/s/t differs from its header** — assert `List()` surfaces the **column** value (proves the header is no longer the read source). Run without the backfill so the divergence survives. Assert Song seeds + both stores' `InitialKeyIsMinor` are unchanged (C3), and a `tags: [12-bar, beginner]` column round-trips via `DeserializeTags` (C2).

<!-- step:backfill-catalogcolumnbackfill-startup-reconcile-pass -->
### Step 2 — Backfill — CatalogColumnBackfill startup reconcile pass

New `CatalogColumnBackfill.Run(ChordFlowDbContext db)` in `Persistence/`, mirroring the existing `ContentSourceMigration.Run` startup-pass shape. Reconcile **columns ← header** (header canonical, C1) across all four catalog entities' rows — packs are already truthful so their pass is a verified no-op; scoping to user-only buys nothing. Only mark a row dirty when a column actually differs, then a single `SaveChanges`.

Wire into `src/ChordFlow.Desktop/Program.cs` immediately after `ContentSourceMigration.Run(db)` (~line 99).

**Tests:** a **legacy row** (header set, columns null/empty) → `Run` → columns now match the header → `List()` (from step 1) surfaces the metadata (IN4). Re-running `Run` mutates nothing and issues no write (idempotence, C4).

<!-- step:doc-ref-sync -->
### Step 3 — Doc/ref sync

`IContentStore.cs` lines ~80-84: the `ContentSummary` doc-comment currently says g/s/t are *'Read from the row's own DSL header … List still reads the header; switching the read path is deferred'* — update to: read from the denormalized columns (backfilled/reconciled from the canonical header). Refresh the matching list read-path description in `chordflow-architecture-reference.md` (ref-sync rule; `loom/refs/*` is gate-excluded — edit via `loom_patch_doc`/`loom_update_doc`).
