---
type: req
id: rq_01KY0A9TH53H8GDVKJR2YK70J8
title: Content List() reads the denormalized columns (retire header-parse + backfill) — Requirements
status: locked
created: 2026-07-20
updated: 2026-07-20
version: 1
design_version: 1
tags: []
parent_id: de_01KY0A901J4AP6E4SQTQZG28H2
requires_load: []
---
# Content List() reads the denormalized columns (retire header-parse + backfill) — Requirements

### ✅ Included

- `IN1` Each catalog store's `List()` reads catalog metadata (**genre / subgenre / tags**) from the denormalized `ICatalogEntity` columns (`Genre` / `Subgenre` / `CatalogHeader.DeserializeTags(Tags)`) instead of `CatalogHeader.Parse(dsl).Metadata` — for Progression, Song, Voicing, and Drums.
- `IN2` `ContentSummaries.Build`'s row shape carries `genre` / `subgenre` / `tags` **directly** (design §4.1 shape B); the list projection no longer constructs a `CatalogMetadata` intermediary.
- `IN3` After the flip, `VoicingStore.List` and `DrumGrooveStore.List` parse **no** DSL header at all (they read only the columns).
- `IN4` A one-time, idempotent startup reconcile pass — `CatalogColumnBackfill.Run(db)` — populates each catalog entity's `Genre`/`Subgenre`/`Tags` columns from that row's DSL header where they disagree, wired into `Program.cs` **after** `ContentSourceMigration.Run(db)`, so legacy user rows (header set, columns empty) still surface their metadata once the read flips.
- `IN5` Update the `ContentSummary` XML doc-comment (`IContentStore.cs` — currently *"`List` still reads the header; switching the read path is deferred"*) and the list read-path note in `chordflow-architecture-reference.md` — same unit of work (ref-sync rule).

### ❌ Excluded

- `EX1` Any **schema change** or new column — the denormalized columns already exist; this is a read-path + one-time-data change only.
- `EX2` Removing the **tonality / seed** header parse — `ProgressionStore.IsMinorTonality` (header → `InitialKeyIsMinor`) and `SongStore.SeedsOf` (header + body → key/feel/tempo/minor) keep their parse; only the genre/subgenre/tags read moves.
- `EX3` Any change to `Get`, `Save`, the C#↔JS bridge, or the editor UI — this thread is the `List()` read path + the backfill only.
- `EX4` An EF **SQL data migration** or a **lazy on-touch** populate as the backfill mechanism — rejected (SQL can't run the C# header parser; lazy would keep a header-parse fallback in `List()`, defeating the retire goal). The backfill is the startup reconcile pass (IN4).

### ⛓ Constraints

- `C1` The DSL header stays the **canonical** source of catalog metadata; the columns are a derived cache. The backfill reconciles **columns ← header**, never the reverse (preserves `content-metadata-editing` C2).
- `C2` `List()` read from columns must be **equivalent** to today's header-parsed genre/subgenre/tags for a consistent row — tags via the `SerializeTags`/`DeserializeTags` 1:1 round-trip.
- `C3` `SongStore`'s `InitialKey` / `DefaultFeel` / `DefaultTempo` / `InitialKeyIsMinor` and `ProgressionStore`'s `InitialKeyIsMinor` are **unchanged** by the flip.
- `C4` `CatalogColumnBackfill.Run` is **idempotent** — a no-op once every row's columns already match its header — and cheap at catalog scale (dozens–hundreds of rows).
