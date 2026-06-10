---
type: req
id: rq_01KTS50QAYJX82T6YSJZCJJGVC
title: Core/Host Project Split — Requirements
status: locked
created: "2026-06-10T00:00:00.000Z"
updated: 2026-06-10
version: 1
tags: []
parent_id: id_01KTRRCRC5ES07FP464QHF7QPM
requires_load: []
---
# Core/Host Project Split — Requirements

Authoritative scope for the Core/Host project split — a pure structural refactor of the existing single `ChordFlow.App` project. Derived from `core-host-split-design.md` and the decisions settled in `general-chat-001.md`.

### ✅ Included

- `IN1` Split the single `src/ChordFlow.App` project into two: **`ChordFlow.Core`** (engine) and **`ChordFlow.Desktop`** (host).
- `IN2` Move `Domain/`, `Rendering/`, and `Features/` into `ChordFlow.Core`.
- `IN3` Move the WinForms shell, the WebView2 control, virtual-host wiring, the bridge transport (web-message router), and `wwwroot/` into `ChordFlow.Desktop`.
- `IN4` Place the JSON-envelope bridge **DTOs** in `ChordFlow.Core/Bridge/`; keep the **transport** in `ChordFlow.Desktop/WebHost/`.
- `IN5` Place the SQLite store and the EF `Migrations/` in `ChordFlow.Core/Persistence/`.
- `IN6` Rename the test project `ChordFlow.Tests` → `ChordFlow.Core.Tests`, referencing `ChordFlow.Core`.
- `IN7` Update `ChordFlow.sln`, all namespaces (`ChordFlow.App.*` → `ChordFlow.Core.*` / `ChordFlow.Desktop.*`), and project references; remove the old `ChordFlow.App` project.

### ❌ Excluded

- `EX1` The web / cross-platform host itself (this thread only makes it possible).
- `EX2` Extracting `wwwroot` into a Razor Class Library — deferred until a real second host exists.
- `EX3` A separate `ChordFlow.Infrastructure` project — deferred; MVP stays at two projects.
- `EX4` Any behavior change, new feature, or domain/rendering logic change. Code moves; it does not change.

### ⛓ Constraints

- `C1` `ChordFlow.Core` must carry **zero UI/host package references** and no `UseWindowsForms` — the split's compile-time guarantee.
- `C2` Dependency direction strictly one-way: **Desktop → Core**, **Tests → Core**. Nothing references Desktop but the entry point.
- `C3` `wwwroot` stays **host-neutral**: no WebView2-specific APIs leak past the single `app.js` bridge module.
- `C4` Target frameworks: Core `net10.0`, Desktop `net10.0-windows`, Tests `net10.0`.
- `C5` `Domain/` keeps its **no-I/O** guarantee even though `Persistence/` lives in the same Core project.
- `C6` All existing tests stay green and the app still launches, renders an exercise, and plays with the synced cursor (smoke).
