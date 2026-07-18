---
type: idea
id: id_01KXVQEBZ571RNJQB6SKFR2E28
title: Clear NU1903 — bump the vulnerable SQLitePCLRaw transitive dependency
status: done
created: 2026-07-18
version: 1
tags: []
parent_id: null
requires_load: []
---
# Clear NU1903 — bump the vulnerable SQLitePCLRaw transitive dependency

## Problem

The **v0.15.0 Release build** surfaced a dependency advisory:

```
warning NU1903: Package 'SQLitePCLRaw.lib.e_sqlite3' 2.1.11 has a known high severity
vulnerability, https://github.com/advisories/GHSA-2m69-gcr7-jv3q
```

It's a **warning, not an error** (it didn't block the release), but the vulnerable native SQLite
bundle should be cleared so the build stays clean and the SQLite engine stays current.

**How it's pulled in (transitive):**
`ChordFlow.Core` references `Microsoft.EntityFrameworkCore.Sqlite 10.0.8`, which brings
`Microsoft.Data.Sqlite → SQLitePCLRaw.bundle_e_sqlite3 → SQLitePCLRaw.lib.e_sqlite3 2.1.11` (the
flagged native package). Nothing in the repo references it directly.

## Honest exposure (low, but worth clearing)

ChordFlow's SQLite footprint is deliberately tiny and offline: **one local file**
(`%LOCALAPPDATA%\ChordFlow\chordflow.db`) holding the user's own exercise *definitions* — **no
network, no server, no untrusted/remote SQL input**, and the EF Core tooling is build-only
(`PrivateAssets=all` on Design). So practical exploitability is low. This is dependency hygiene, not
an incident — resolve it on principle and to keep the release build warning-free.

## What we want

- The **NU1903 warning gone** from `restore` / `build -c Release`.
- The native `e_sqlite3` on a **patched version** (past the advisory's fixed SQLite).
- **Tests green**, **no runtime/behavior change** — a pure dependency bump.

## Approach (pick the least-surface option)

1. **Bump EF Core** — check whether a newer `Microsoft.EntityFrameworkCore.Sqlite 10.0.x` already
   pulls a patched `SQLitePCLRaw` transitively. Cleanest if it exists (one version bump, no direct
   ref added).
2. **Pin the bundle directly** — add a top-level `PackageReference` to
   `SQLitePCLRaw.bundle_e_sqlite3` (and/or `.lib.e_sqlite3`) at the fixed version to **override** the
   transitive `2.1.11`. The surgical fix if EF Core hasn't rolled it forward yet.
3. **Version override / CPM** — a NuGet version override (or central package management) if we'd
   rather not add a direct reference.

Prefer 1 if a fixed EF Core is available; otherwise 2 is the minimal, explicit override. Confirm the
chosen `SQLitePCLRaw` version bundles a SQLite **past** the advisory's fixed release.

## Validation

- `dotnet build -c Release` shows **no NU1903** (and no new advisories introduced).
- `dotnet list package --vulnerable --include-transitive` is **clean** for this advisory.
- Full `dotnet test` **green** — the `Persistence` / `PackImport` / `SongPersistence` tests exercise
  the real DB path.
- **Smoke the app**: launch, save an exercise, reload it from the saved-exercise list — the local DB
  still round-trips.

## Notes

- Ships as a small **patch (v0.15.1)** or rides the next release.
- This is the first **dependency-hygiene** thread; consider a periodic
  `dotnet list package --vulnerable --include-transitive` check as part of the release runbook so
  advisories are caught before a release, not during it.
