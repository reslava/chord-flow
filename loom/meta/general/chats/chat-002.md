---
type: chat
id: ch_01KTZPVPNN9C62KN3DJYZF89NY
title: chordflow-db
status: done
created: 2026-06-13
updated: 2026-06-18
version: 2
tags: []
parent_id: null
requires_load: []
last_ai_block: 1
---
# chordflow-db

## Rafa:

Why db `C:\Users\Rafa\AppData\Local\ChordFlow\chordflow.db` only have:
Progressions
Exercises
PracticeRecords

tables yet
and do not have: Songs, Rhythms, Packages tables or any related to last threads done?

## AI:

Two separate things are going on here — one is "by design," the other is a stale DB file.

## 1. There is no `Packages` table — and there never was

Content-catalog Phase 1 did **not** add a `Packages` table. It added **columns to existing tables**, not a new entity:

- `AddCatalogMetadata` → adds `Genre`, `Subgenre`, `Tags` columns to **Progressions**.
- `AddPackProvenance` → adds a `PackId` column to **Progressions** (and `SongEntity`/`RhythmPatternEntity` carry `PackId` from birth).

So "pack" provenance is a **column on each content row** (`PackId` = which pack it came from), not a packages table. A content pack is an *importable definition bundle* (an additive data drop), not a DB-resident entity. Don't expect a `Packages` table to appear.

## 2. Songs and RhythmPatterns tables *do* exist in code — your DB file is just stale

The migrations are all written and committed:

| Migration | Date | Effect |
|---|---|---|
| `InitialCreate` | 06-08 | `Exercises`, `PracticeRecords` |
| `AddProgressions` | 06-09 | `Progressions` |
| `AddCatalogMetadata` | 06-12 | +columns on Progressions |
| `AddPackProvenance` | 06-12 | +`PackId` on Progressions |
| `AddSongs` | 06-12 | **`Songs` table** |
| `AddRhythmPatterns` | 06-12 | **`RhythmPatterns` table** |

`Program.cs:67` calls `db.Database.Migrate()` on every startup, which applies all pending migrations automatically. So why are the last four missing from your file?

**Your DB file was last written `2026-06-10 19:43`** — that's *before* the four 06-12 migrations existed. The app hasn't been launched since the Song / Rhythm / catalog threads landed. Your `__EFMigrationsHistory` is frozen at the `AddProgressions` state, which is exactly the three tables you see.

**Fix: just relaunch the app.** `Migrate()` will apply the four pending migrations — creating `Songs` and `RhythmPatterns`, and adding the catalog/provenance columns to `Progressions`. No reset needed; your existing data stays.

## One real gap I noticed while checking

`Program.cs` seeds progressions and rhythm patterns on startup but **not songs**:

```csharp
db.SeedBuiltInProgressions();
db.SeedBuiltInRhythmPatterns();
// ← no db.SeedBuiltInSongs();
```

`SeedBuiltInSongs()` exists on the context (and is idempotent like the others) but is never called. So after you relaunch, the `Songs` table will be created **but empty** — that's a wiring miss, not a schema problem. Want me to add the `db.SeedBuiltInSongs();` line? (One-line change, but it's a code edit — say the word and I'll do it as a proper step.)