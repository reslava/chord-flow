---
type: plan
id: pl_01KTS51HQ69AEZASGY4KN1BH0N
title: Core/Host Project Split — Implementation
status: done
created: 2026-06-10
updated: 2026-06-10
version: 1
design_version: 3
tags: []
parent_id: de_01KTRW1F3JRETAXP21J04DRKDV
requires_load: []
target_version: 0.1.0
actual_release: 0.4.0
steps:
  - id: scaffold-chordflow-core
    order: 1
    status: done
    description: Scaffold ChordFlow.Core (net10.0) and add it to the solution
    files_touched: [ChordFlow.sln, src/ChordFlow.Core/ChordFlow.Core.csproj]
    blocked_by: []
    satisfies: [IN1, C1, C4]
  - id: move-engine-code-into-core
    order: 2
    status: done
    description: Move Domain, Rendering, Features into ChordFlow.Core and renamespace to ChordFlow.Core.*
    files_touched: [src/ChordFlow.Core/Domain/, src/ChordFlow.Core/Rendering/, src/ChordFlow.Core/Features/, src/ChordFlow.App/]
    blocked_by: [1]
    satisfies: [IN2, IN7, EX4]
  - id: move-bridge-dtos-persistence-into-core
    order: 3
    status: done
    description: Move bridge DTOs to Core/Bridge and the SQLite store + EF Migrations to Core/Persistence
    files_touched: [src/ChordFlow.Core/Bridge/, src/ChordFlow.Core/Persistence/, src/ChordFlow.Core/Persistence/Migrations/, src/ChordFlow.App/Infrastructure/, src/ChordFlow.App/Migrations/]
    blocked_by: [2]
    satisfies: [IN4, IN5, C5]
  - id: scaffold-chordflow-desktop-move-host
    order: 4
    status: done
    description: "Scaffold ChordFlow.Desktop (net10.0-windows) and move the host: shell, WebHost transport, wwwroot"
    files_touched: [ChordFlow.sln, src/ChordFlow.Desktop/ChordFlow.Desktop.csproj, src/ChordFlow.Desktop/WebHost/, src/ChordFlow.Desktop/wwwroot/, src/ChordFlow.App/]
    blocked_by: [3]
    satisfies: [IN3, IN4, C2, C3, C4]
  - id: rename-retarget-test-project
    order: 5
    status: done
    description: Rename test project to ChordFlow.Core.Tests and retarget it at ChordFlow.Core
    files_touched: [ChordFlow.sln, tests/ChordFlow.Core.Tests/ChordFlow.Core.Tests.csproj, tests/ChordFlow.Tests/]
    blocked_by: [2]
    satisfies: [IN6, IN7]
  - id: reconcile-solution-drop-chordflow-app
    order: 6
    status: done
    description: Fix solution + references, enforce Desktop → Core, and remove the old ChordFlow.App project
    files_touched: [ChordFlow.sln, src/ChordFlow.App/]
    blocked_by: [4, 5]
    satisfies: [IN1, IN7, C2]
  - id: verify-the-refactor
    order: 7
    status: done
    description: "Verify: build, assert Core has zero UI references, run tests, smoke-test the app"
    files_touched: [src/ChordFlow.Core/ChordFlow.Core.csproj]
    blocked_by: [6]
    satisfies: [C1, C6]
---
# Core/Host Project Split — Implementation

## Goal

Split the single `src/ChordFlow.App` project into `ChordFlow.Core` (host-agnostic engine — Domain, Rendering, Features, Bridge DTOs, Persistence) and `ChordFlow.Desktop` (WinForms + WebView2 host — shell, WebHost transport, wwwroot), with strict Desktop → Core dependency direction. This is a pure structural refactor: code moves and is renamespaced, but no behavior changes. The payoff is a compile-time guarantee that the engine cannot reference a UI/host package, making the long-standing "engine stays UI-agnostic" rule self-enforcing and a future web host an additive project rather than a rewrite. Verified by: solution builds, Core has zero UI references, all existing tests pass, and the app still launches/renders/plays.

---

## Steps

| Done | # | Step | Files touched | Blocked by | Satisfies |
|---|---|---|---|---|---|
| ✅ | 1 | Scaffold ChordFlow.Core (net10.0) and add it to the solution | ChordFlow.sln, src/ChordFlow.Core/ChordFlow.Core.csproj | — | IN1, C1, C4 |
| ✅ | 2 | Move Domain, Rendering, Features into ChordFlow.Core and renamespace to ChordFlow.Core.* | src/ChordFlow.Core/Domain/, src/ChordFlow.Core/Rendering/, src/ChordFlow.Core/Features/, src/ChordFlow.App/ | 1 | IN2, IN7, EX4 |
| ✅ | 3 | Move bridge DTOs to Core/Bridge and the SQLite store + EF Migrations to Core/Persistence | src/ChordFlow.Core/Bridge/, src/ChordFlow.Core/Persistence/, src/ChordFlow.Core/Persistence/Migrations/, src/ChordFlow.App/Infrastructure/, src/ChordFlow.App/Migrations/ | 2 | IN4, IN5, C5 |
| ✅ | 4 | Scaffold ChordFlow.Desktop (net10.0-windows) and move the host: shell, WebHost transport, wwwroot | ChordFlow.sln, src/ChordFlow.Desktop/ChordFlow.Desktop.csproj, src/ChordFlow.Desktop/WebHost/, src/ChordFlow.Desktop/wwwroot/, src/ChordFlow.App/ | 3 | IN3, IN4, C2, C3, C4 |
| ✅ | 5 | Rename test project to ChordFlow.Core.Tests and retarget it at ChordFlow.Core | ChordFlow.sln, tests/ChordFlow.Core.Tests/ChordFlow.Core.Tests.csproj, tests/ChordFlow.Tests/ | 2 | IN6, IN7 |
| ✅ | 6 | Fix solution + references, enforce Desktop → Core, and remove the old ChordFlow.App project | ChordFlow.sln, src/ChordFlow.App/ | 4, 5 | IN1, IN7, C2 |
| ✅ | 7 | Verify: build, assert Core has zero UI references, run tests, smoke-test the app | src/ChordFlow.Core/ChordFlow.Core.csproj | 6 | C1, C6 |
---

### Legend

| Symbol | Meaning |
|--------|---------|
| ✅ | Done |
| 🔄 | In Progress |
| 🔳 | Pending |
| ❌ | Cancelled |

<!-- step:scaffold-chordflow-core -->
### Step 1 — Scaffold ChordFlow.Core

Create `src/ChordFlow.Core/ChordFlow.Core.csproj` targeting `net10.0` (no `UseWindowsForms`, no WebView2 package). Carry over only host-agnostic package refs the moved code needs (EF Core / `Microsoft.Data.Sqlite`, `System.Text.Json`). Add the project to `ChordFlow.sln`. The empty project must build.

<!-- step:move-engine-code-into-core -->
### Step 2 — Move engine code into Core

Move `Domain/`, `Rendering/`, `Features/` from `ChordFlow.App` into `ChordFlow.Core`. Rename namespaces `ChordFlow.App.{Domain,Rendering,Features}` → `ChordFlow.Core.{...}`. No logic changes. Core compiles on its own.

<!-- step:move-bridge-dtos-persistence-into-core -->
### Step 3 — Move Bridge DTOs + Persistence into Core

Split the old `Infrastructure/`: the JSON-envelope DTOs (host-agnostic contract shapes) go to `Core/Bridge/`; the SQLite store + DbContext + design-time factory go to `Core/Persistence/`, and the EF `Migrations/` folder moves under `Core/Persistence/Migrations/`. Renamespace accordingly. `Domain/` keeps its no-I/O guarantee (C5). Leave the WebView2 transport/router behind for step 4.

<!-- step:scaffold-chordflow-desktop-move-host -->
### Step 4 — Scaffold ChordFlow.Desktop + move host

Create `src/ChordFlow.Desktop/ChordFlow.Desktop.csproj` (`net10.0-windows`, `UseWindowsForms=true`, references `ChordFlow.Core` + `Microsoft.Web.WebView2.WinForms`); add to solution. Move the WinForms shell/`Program.cs`/form, the WebView2 bridge transport + web-message router into `Desktop/WebHost/`, and `wwwroot/` into `Desktop/`. Renamespace to `ChordFlow.Desktop.*`. `wwwroot` stays host-neutral (C3).

<!-- step:rename-retarget-test-project -->
### Step 5 — Rename + retarget test project

Rename `tests/ChordFlow.Tests` → `tests/ChordFlow.Core.Tests` (folder, `.csproj`, assembly/namespace). Replace its project reference with `ChordFlow.Core`. `net10.0`. Tests target Domain + Rendering, which now live in Core.

<!-- step:reconcile-solution-drop-chordflow-app -->
### Step 6 — Reconcile solution + drop ChordFlow.App

Remove `ChordFlow.App` from the solution and delete the now-empty project. Confirm the only project references are Desktop → Core and Core.Tests → Core (C2). Fix any stragglers (entry point, DI wiring, csproj globs).

<!-- step:verify-the-refactor -->
### Step 7 — Verify the refactor

Build the whole solution. Assert `ChordFlow.Core.csproj` has no WebView2/WinForms reference and no `UseWindowsForms` (C1 — the structural guarantee). Run the test suite — all green (C6). Launch the app and confirm it renders an exercise and plays with the synced cursor (C6), proving the bridge transport survived the move.
