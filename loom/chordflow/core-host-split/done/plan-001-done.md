---
type: done
id: pl_01KTS51HQ69AEZASGY4KN1BH0N-done
title: Done — Core/Host Project Split — Implementation
status: done
created: "2026-06-10T00:00:00.000Z"
version: 7
tags: []
parent_id: pl_01KTS51HQ69AEZASGY4KN1BH0N
requires_load: []
---
# Done — Core/Host Project Split — Implementation

## Step 1 — Scaffold ChordFlow.Core (net10.0) and add it to the solution

Scaffolded `src/ChordFlow.Core/ChordFlow.Core.csproj` — `net10.0`, `ImplicitUsings`/`Nullable` enabled. Carried over only the host-agnostic package refs: `Microsoft.EntityFrameworkCore.Sqlite` 10.0.8 + `Microsoft.EntityFrameworkCore.Design` 10.0.8 (`PrivateAssets=all`, build-only for `dotnet ef`). **No** WebView2, **no** `UseWindowsForms`, no `wwwroot` Content/soundfont target — those stay with the future `ChordFlow.Desktop` (C1 satisfied at the project level). Added to `ChordFlow.sln` via `dotnet sln add`. Empty project builds clean (0 warnings, 0 errors).

## Step 2 — Move Domain, Rendering, Features into ChordFlow.Core and renamespace to ChordFlow.Core.*

`git mv`'d `Domain/`, `Rendering/`, `Features/` from `ChordFlow.App` → `ChordFlow.Core` (history preserved). **No namespace edits needed**: the engine namespaces were already project-neutral (`ChordFlow.Domain`, `ChordFlow.Rendering`, `ChordFlow.Features.*`) — there was no `ChordFlow.App.*` to rename (only the EF Migrations carried `ChordFlow.App.Migrations`, handled in step 3). This made IN7 far smaller than the design assumed.

## Step 3 — Move bridge DTOs to Core/Bridge and the SQLite store + EF Migrations to Core/Persistence

Split the former `Infrastructure/` by responsibility:
- **`Core/Bridge/`** ← `IBridge.cs`, `StatusEnvelope.cs`, `WebMessageRouter.cs` → namespace `ChordFlow.Bridge`. **Design correction:** the design's migration table put the router in Desktop, but `PracticeSessionHandler` (a Core feature) takes `WebMessageRouter` in its ctor — leaving the router in Desktop would create a Core→Desktop reference, violating C2. The router is host-agnostic (pure JSON parse + events, no WebView2), so it correctly belongs in Core/Bridge. Only `WebView2Bridge` (uses `CoreWebView2`) stayed host-specific.
- **`Core/Persistence/`** ← `ChordFlowDbContext.cs`, `ChordFlowDbContextFactory.cs`, `Entities/`, and the EF `Migrations/` → namespaces `ChordFlow.Persistence` / `.Entities` / `.Migrations` (was `ChordFlow.App.Migrations`).
- Outbound envelope DTOs (`LoadScoreEnvelope`, `PracticeRecordedEnvelope`) were already defined in their Feature files (Core) — no move needed.
- Updated feature usings: `PracticeSession` → `ChordFlow.Bridge`; `Progress` + `ExerciseLibrary` → `ChordFlow.Persistence(.Entities)`. `Domain/` keeps its no-I/O guarantee (C5).

## Step 4 — Scaffold ChordFlow.Desktop (net10.0-windows) and move the host: shell, WebHost transport, wwwroot

Created `src/ChordFlow.Desktop/ChordFlow.Desktop.csproj` — `net10.0-windows`, `UseWindowsForms`, `OutputType=WinExe`, `ProjectReference` → `ChordFlow.Core`, `Microsoft.Web.WebView2` package, plus the carried-over `wwwroot` Content-copy item and the `FetchSoundfont` target. Added to `ChordFlow.sln`. Moved the host into it: `Program.cs` (root) and `WebHost/WebView2Bridge.cs` (namespace → `ChordFlow.Desktop.WebHost`, added `using ChordFlow.Bridge`), and `wwwroot/`. Program.cs's single `using ChordFlow.Infrastructure;` became `using ChordFlow.Bridge; using ChordFlow.Persistence; using ChordFlow.Desktop.WebHost;`. EF Core/Sqlite compile assets flow transitively from Core (C2/C3 hold; `wwwroot` stays host-neutral).

## Step 5 — Rename test project to ChordFlow.Core.Tests and retarget it at ChordFlow.Core

Renamed `tests/ChordFlow.Tests` → `tests/ChordFlow.Core.Tests` (dir + `.csproj`) via `git mv` (needed a `dotnet build-server shutdown` + `bin`/`obj` clear first — a build handle held the dir, "Permission denied"). Retargeted the csproj to plain **`net10.0`** (dropped `-windows`) and repointed the `ProjectReference` to `ChordFlow.Core`. This realized the cleanup the old csproj comment explicitly anticipated. Updated the two persistence-touching test files (`ProgressionPersistenceTests`, `ProgressionSeedTests`): `ChordFlow.Infrastructure(.Entities)` usings → `ChordFlow.Persistence(.Entities)`.

## Step 6 — Fix solution + references, enforce Desktop → Core, and remove the old ChordFlow.App project

Removed `ChordFlow.App` from `ChordFlow.sln` and `git rm -r`'d the project (only the empty `Infrastructure/.gitkeep` + csproj remained after the moves). Solution now lists exactly three projects: `ChordFlow.Core`, `ChordFlow.Desktop`, `ChordFlow.Core.Tests`. References confirmed one-way (C2): Desktop → Core, Core.Tests → Core; nothing references Desktop. **`dotnet build ChordFlow.sln` succeeds — 0 errors.** One pre-existing `MSB3277` WindowsBase-version warning on Desktop, sourced from the WebView2 package's bundled WPF assembly (`Microsoft.Web.WebView2.Wpf.dll` → `WindowsBase 5.0.0.0`); inherent to the package on net10, unrelated to the split, non-blocking.

## Step 7 — Verify: build, assert Core has zero UI references, run tests, smoke-test the app

**Verification — all green.**
- **C1 (zero UI refs):** `ChordFlow.Core.csproj` has no `WebView2`/`WinForms`/`UseWindowsForms`/`UseWPF` elements (comment-stripped check), and `dotnet list package --include-transitive` shows no UI package in Core's graph — only EF Core/Sqlite. The structural guarantee holds.
- **C6 (tests):** `dotnet test` → **163/163 passed**, now running on plain `net10.0` (not `-windows`) — itself proof the engine + persistence are host-agnostic.
- **C6 (smoke):** timed background launch of `ChordFlow.Desktop.exe` — process booted and stayed alive 9s with no startup crash / no error output, then force-terminated. (Visual render + synced-cursor confirmation left to a human eyeball — can't observe the GUI headless.)

**Straggler cleanup (step-6 "fix any stragglers"):** repo-wide scan for old names found and fixed — test class namespaces `ChordFlow.Tests` → `ChordFlow.Core.Tests` (13 files); `README.md` build command + project-layout block; `docs/internal/commands.md` run path; a `ChordFlow.App.csproj` mention in a `Program.cs` comment. Final full-solution rebuild + retest after cleanup: 0 errors, 163/163.
