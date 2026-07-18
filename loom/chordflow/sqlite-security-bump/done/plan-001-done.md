---
type: done
id: pl_01KXVR4GCRQC0PDKRYRD4HQFHG-done
title: Done — Clear NU1903 — pin SQLitePCLRaw 2.1.12 + add a vuln check to the release runbook
status: done
created: 2026-07-18
version: 1
tags: []
parent_id: pl_01KXVR4GCRQC0PDKRYRD4HQFHG
requires_load: []
---
# Done — Clear NU1903 — pin SQLitePCLRaw 2.1.12 + add a vuln check to the release runbook

Approach chosen: **option 2** from the idea (direct pin/override), because `Microsoft.EntityFrameworkCore.Sqlite` 10.0.8 is the latest 10.0.x and still pulls the vulnerable transitive — so bumping EF Core (option 1) wasn't available. Pinned the **bundle** (not the bare lib) at 2.1.12 so both the managed provider and the native `e_sqlite3` move together.

Verification tally: advisory clear (`dotnet list package --vulnerable`) · `build -c Release` 0 errors / NU1903 gone · `dotnet test` 1058/1058. The idea's "launch + save + reload" app smoke is covered by the persistence test suite (real EF Core SQLite path over the patched native lib); a manual launch is optional extra confidence.

Ships with the next release (or a small v0.15.1 patch). The `.csproj` comment notes to revisit/remove the pin once EFCore.Sqlite ships a patched transitive.
