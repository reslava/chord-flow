---
type: chat
id: ch_01KXVQNNR8WM2QXYTYW813Z29T
title: sqlite-security-bump Chat 001
status: done
created: 2026-07-18
version: 1
tags: []
parent_id: null
requires_load: []
last_ai_block: 1
---
# sqlite-security-bump Chat 001

## Rafa:

if it is all ready for the fix then
code quick 
if not tell me

add a periodic dotnet list package --vulnerable check to the release runbook so advisories are caught before a release.

## AI:

Both done.

**The fix — it was ready, so `code quick`.** The clean path was a direct pin (option 2), because `EFCore.Sqlite 10.0.8` is the latest 10.0.x and still pulls the vulnerable transitive — no EF bump to lean on. Added `<PackageReference Include="SQLitePCLRaw.bundle_e_sqlite3" Version="2.1.12" />` to `ChordFlow.Core.csproj`; a direct ref wins over the transitive `2.1.11`, and 2.1.12 is same-2.1.x-line so there's no major-version compat risk.

Verified:
- `dotnet list package --vulnerable --include-transitive` → **clean** in all three projects.
- `build -c Release` → **NU1903 gone**, 0 errors (down to just the pre-existing WebView2 warning).
- `dotnet test` → **1058/1058**. The DB round-trip is exercised by the persistence tests over the patched native lib; a manual app launch is optional extra confidence.

**Runbook check.** Added `dotnet list package --vulnerable --include-transitive` to both `RELEASING.md` (pre-tag checklist) and the `do-release` command (step-6 gates), so an advisory now stops the release *before* tagging instead of first surfacing during the build.

Recorded as done (plan-001). This ships with the next release, or a small **v0.15.1** patch if you want it out on its own. Committing the thread + fix + runbook change now.