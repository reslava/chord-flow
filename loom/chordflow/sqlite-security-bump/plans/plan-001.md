---
type: plan
id: pl_01KXVR4GCRQC0PDKRYRD4HQFHG
title: Clear NU1903 — pin SQLitePCLRaw 2.1.12 + add a vuln check to the release runbook
status: done
created: 2026-07-18
updated: 2026-07-18
version: 1
design_version: 1
tags: []
parent_id: null
requires_load: []
target_version: 0.1.0
steps:
  - id: cleared-nu1903-ghsa-2m69-gcr7-jv3q
    order: 1
    status: done
    description: "Cleared NU1903 (GHSA-2m69-gcr7-jv3q): added a top-level `PackageReference` to `SQLitePCLRaw.bundle_e_sqlite3` 2.1.12 in `ChordFlow.Core.csproj`, overriding the vulnerable transitive `SQLitePCLRaw.lib.e_sqlite3` 2.1.11 pulled by `Microsoft.EntityFrameworkCore.Sqlite` 10.0.8 (a direct ref wins over the transitive one; same 2.1.x line Microsoft.Data.Sqlite expects, so no major-version compat risk). Verified: `dotnet list package --vulnerable --include-transitive` clean across all three projects, `build -c Release` NU1903-free (down to just the pre-existing WebView2 warning), full suite 1058/1058 green — the Persistence/PackImport/SongPersistence tests exercise the DB round-trip through the patched native lib."
    files_touched: []
    blocked_by: []
    satisfies: []
  - id: added-a-periodic-vulnerable-dependency-check
    order: 2
    status: done
    description: "Added a periodic vulnerable-dependency check to the release runbook: a `dotnet list package --vulnerable --include-transitive` item in the RELEASING.md pre-tag checklist and a matching gate in the `.claude/commands/do-release.md` step-6 sequence, so an advisory is caught before tagging rather than first surfacing mid-release."
    files_touched: []
    blocked_by: []
    satisfies: []
---
# Clear NU1903 — pin SQLitePCLRaw 2.1.12 + add a vuln check to the release runbook

## Goal

Quick-ship record of 2 completed changes.

---

## Steps

| Done | # | Step | Files touched | Blocked by | Satisfies |
|---|---|---|---|---|---|
| ✅ | 1 | Cleared NU1903 (GHSA-2m69-gcr7-jv3q): added a top-level `PackageReference` to `SQLitePCLRaw.bundle_e_sqlite3` 2.1.12 in `ChordFlow.Core.csproj`, overriding the vulnerable transitive `SQLitePCLRaw.lib.e_sqlite3` 2.1.11 pulled by `Microsoft.EntityFrameworkCore.Sqlite` 10.0.8 (a direct ref wins over the transitive one; same 2.1.x line Microsoft.Data.Sqlite expects, so no major-version compat risk). Verified: `dotnet list package --vulnerable --include-transitive` clean across all three projects, `build -c Release` NU1903-free (down to just the pre-existing WebView2 warning), full suite 1058/1058 green — the Persistence/PackImport/SongPersistence tests exercise the DB round-trip through the patched native lib. | — | — | — |
| ✅ | 2 | Added a periodic vulnerable-dependency check to the release runbook: a `dotnet list package --vulnerable --include-transitive` item in the RELEASING.md pre-tag checklist and a matching gate in the `.claude/commands/do-release.md` step-6 sequence, so an advisory is caught before tagging rather than first surfacing mid-release. | — | — | — |
---

### Legend

| Symbol | Meaning |
|--------|---------|
| ✅ | Done |
| 🔄 | In Progress |
| 🔳 | Pending |
| ❌ | Cancelled |
