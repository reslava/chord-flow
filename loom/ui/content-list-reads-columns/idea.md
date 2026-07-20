---
type: idea
id: id_01KY09RF449JNPQ451Q0P2YB04
title: Content List() reads the denormalized columns (retire header-parse + backfill)
status: draft
created: 2026-07-20
version: 1
tags: []
parent_id: null
requires_load: []
---
# Content List() reads the denormalized columns (retire header-parse + backfill)

## The notion

`content-metadata-editing` started **populating** the denormalized `ICatalogEntity` `Genre`/`Subgenre`/`Tags` columns on every user save (its scope option **A**), but each store's `List()` still re-parses the DSL header (`CatalogHeader.Parse(dsl).Metadata`) to surface catalog metadata for `ContentSummary`. This thread completes the denormalization: flip the list read path to the **columns** and retire the per-row header parse — the columns finally earn their keep. It is the deferred **EX2** of `content-metadata-editing` (the A→B completion).

## Why it was deferred

Option A shipped because it's small, safe, and needs no migration; B (this thread) adds a data backfill and touches every store's `List()` for a perf win that isn't needed at current catalog scale (dozens–hundreds of rows). Now that saves reliably write the columns, B collapses to "flip `List()` to read + backfill the few historical rows."

## The backfill is smaller than it first looked

A key discovery from implementing A: the **`PackImporter` already populates the columns** for every pack row (`PackImporter.cs` — it parses the header and sets `Genre`/`Subgenre`/`Tags` on upsert). So:

- **Pack rows** — already truthful (importer-populated), refreshed on each import.
- **User rows saved from `content-metadata-editing` onward** — truthful (the `Save` change).
- **Only gap:** user rows saved *before* `content-metadata-editing` shipped — a small, solo-dev set.

So the "backfill every existing row" framing from the A-vs-B discussion was pessimistic: only legacy user rows need it, and packs self-heal on import.

## Shape

- Each catalog store's `List()` (Progression / Song / Voicing / Drums) reads `Genre`/`Subgenre`/`CatalogHeader.DeserializeTags(Tags)` from the row instead of `CatalogHeader.Parse(dsl).Metadata`.
- **Song is only partial:** its `List()` also parses the header for `tonality:` (→ `InitialKeyIsMinor`) and parses the song *body* for the key/feel/tempo seeds — so the header parse there stays; **only the genre/subgenre/tags read** moves to columns. Don't regress `InitialKeyIsMinor` or the seeds.
- **Backfill** the legacy user rows so nothing vanishes from lists the instant the read flips. Candidate mechanisms (open): a one-shot EF data migration (read `Dsl` → parse header → set columns), a startup reconcile pass, or a lazy "populate on next touch." Given the tiny row count, the simplest safe option wins.

## Open questions (for design)

- **Backfill mechanism** — one-shot migration vs startup reconcile vs lazy. Lean: whichever is smallest/safest given the row count.
- **Tags encoding** — confirm the column ↔ header round-trip is 1:1 (`CatalogHeader.SerializeTags`/`DeserializeTags`) so the flipped read matches today's header-parsed values byte-for-byte.
- **Is the perf win worth it yet?** — this is pure cleanup/perf; the feature is complete without it. Pick it up when the read-path tidiness (or a scaling need) is actually wanted.

## Validation

- Store `List()` tests: genre/subgenre/tags come from the **columns** (assert via a row whose header and columns intentionally differ — the column value wins).
- A legacy user row (header set, columns empty) still surfaces its metadata after the backfill.
- Existing pack content still shows its genre/subgenre/tags in the Content + Practice filters after the flip.
- Song's `InitialKeyIsMinor` + key/feel/tempo seeds are unchanged (the header parse there stays).
