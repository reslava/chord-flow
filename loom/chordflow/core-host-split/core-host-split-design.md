---
type: design
id: de_01KTRW1F3JRETAXP21J04DRKDV
title: Core/Host Project Split — Design
status: done
created: "2026-06-10T00:00:00.000Z"
updated: 2026-06-10
version: 3
tags: []
parent_id: id_01KTRRCRC5ES07FP464QHF7QPM
requires_load: []
---
# Core/Host Project Split — Design

Translates `core-host-split-idea.md` into a concrete project structure. This is a **pure structural refactor** — no behavior change. It **supersedes `mvp-design.md` §1 "Solution layout"** (the single `ChordFlow.App` project); everything else in the MVP design (domain types, rendering, feature logic, bridge envelope shape) is unaffected — only project boundaries move.

---

## 1. Decisions

| # | Decision | Rationale |
|---|----------|-----------|
| 1 | Names: **`ChordFlow.Core`** + **`ChordFlow.Desktop`** | `.Core` over `.Engine`; `.Desktop` over `.App` (the host is one of potentially several). |
| 2 | `wwwroot` stays in **`ChordFlow.Desktop`** (Option A) | Static assets have no compile-time coupling to prevent and are a one-path-string move later. Portability comes from the host-neutral `wwwroot` + isolated JS bridge module, not the project location. |
| 3 | Bridge **envelope DTOs in Core**, **transport in Desktop** | Any host implements the same contract; only the transport is host-specific. |
| 4 | One **`ChordFlow.Core.Tests`** to start | Split per-area as it grows. |
| 5 | **SQLite persistence lives in Core** (`Persistence/`), not Desktop | **Settled 2026-06-10.** A future web host also needs persistence; if it sat in Desktop, the web host couldn't reuse it. `Domain/` stays I/O-free — persistence is a Core sub-area, not part of Domain. See §6. |

---

## 2. Target solution layout

```
ChordFlow.sln
  src/
    ChordFlow.Core/              net10.0  — host-agnostic engine, zero UI refs
      Domain/                    pure music kernel (no I/O, no UI)
      Rendering/                 AlphaTexRenderer + RhythmQuantizer
      Features/                  GenerateExercise, PracticeSession, ExerciseLibrary, Progress
      Bridge/                    JSON-envelope DTOs (request/result contracts)
      Persistence/               SQLite store + EF migrations (Microsoft.Data.Sqlite / EF Core)
    ChordFlow.Desktop/           net10.0-windows, UseWindowsForms — host only
      Program.cs / MainForm      WinForms shell + WebView2 control
      WebHost/                   virtual-host mapping + bridge transport (web-message router)
      wwwroot/                   index.html, app.js (bridge module), alphaTab, font/, soundfont/
  tests/
    ChordFlow.Core.Tests/        net10.0  — targets Domain + Rendering (+ Persistence later)
```

---

## 3. Project references & dependency direction

```
ChordFlow.Desktop ──► ChordFlow.Core ◄── ChordFlow.Core.Tests
   (WinForms,            (no UI refs)
    WebView2)
```

- **`ChordFlow.Core`** references only host-agnostic packages (`Microsoft.Data.Sqlite`/EF Core, `System.Text.Json`). **No** `Microsoft.Web.WebView2`, **no** `UseWindowsForms`. It physically cannot call WinForms — that is the whole point.
- **`ChordFlow.Desktop`** references `ChordFlow.Core` + `Microsoft.Web.WebView2.WinForms` (+ WinForms). It is the only project that knows about windows, controls, and the WebView.
- **`ChordFlow.Core.Tests`** references `ChordFlow.Core` only.
- Direction is strictly one-way: **Desktop → Core**, **Tests → Core**. Nothing references Desktop except the entry point.

**TFMs:** Core `net10.0` (portable — a future ASP.NET host on `net10.0` references it unchanged); Desktop `net10.0-windows` with `<UseWindowsForms>true</UseWindowsForms>`; Tests `net10.0`.

---

## 4. The bridge contract split

- **`Core/Bridge/`** — the envelope + typed payloads as plain records: an outer `BridgeRequest { string Action; … }` / `BridgeResponse`, plus the per-action command/result shapes (e.g. `GenerateExerciseRequest`, `ExerciseResult { string AlphaTex; … }`). Serializer-agnostic; Core defines the *shape*, not the wire mechanics.
- **`Desktop/WebHost/`** — the transport: receives `CoreWebView2.WebMessageReceived`, deserializes the envelope, dispatches to the Core feature, serializes the result, and `PostWebMessageAsString` back. Its JS counterpart is the single `wwwroot/app.js` bridge module — the rest of the UI talks to *that*, never to `window.chrome.webview` directly.
- **Net effect:** a future web host re-implements only the transport (`fetch`/JSON endpoint); the contract and all feature logic are reused from Core unchanged.

---

## 5. `wwwroot` placement (Option A) and the future trigger

`wwwroot` stays in `ChordFlow.Desktop`. It must remain **host-neutral**: no WebView2-specific APIs leak past the `app.js` bridge module. **Trigger to extract** into a shared Razor Class Library (`_content/ChordFlow.WebAssets/…`): when a real second host appears. At that point the WebView2 virtual host maps through the `_content` path — called out here so the future wiring is no surprise. Not built now.

---

## 6. Persistence placement (settled)

The SQLite store goes in **`ChordFlow.Core/Persistence/`**, not in the desktop host, because persistence is host-agnostic and a future web host must reuse it. `Domain/` keeps its "no I/O" guarantee — it is a folder inside Core, distinct from `Persistence/`. The existing EF `Migrations/` move here too. **Optional future split:** a dedicated `ChordFlow.Infrastructure` project if Core's data/I/O concerns grow heavy; MVP stays at two projects to avoid premature structure.

---

## 7. Migration map (current → new)

| Current (`src/ChordFlow.App/…`) | New |
|---|---|
| `Domain/` | `ChordFlow.Core/Domain/` |
| `Rendering/` | `ChordFlow.Core/Rendering/` |
| `Features/` | `ChordFlow.Core/Features/` |
| `Infrastructure/` SQLite store | `ChordFlow.Core/Persistence/` |
| `Migrations/` (EF) | `ChordFlow.Core/Persistence/Migrations/` |
| bridge envelope DTOs (wherever they live today) | `ChordFlow.Core/Bridge/` |
| `Infrastructure/` WebView2 bridge + message router | `ChordFlow.Desktop/WebHost/` |
| WinForms host / `Program.cs` / form | `ChordFlow.Desktop/` |
| `wwwroot/` | `ChordFlow.Desktop/wwwroot/` |
| `tests/ChordFlow.Tests/` | `tests/ChordFlow.Core.Tests/` |

Namespaces move `ChordFlow.App.*` → `ChordFlow.Core.*` / `ChordFlow.Desktop.*`. The `.sln` gains the new projects and drops `ChordFlow.App`.

---

## 8. Verification

- Solution builds; **`ChordFlow.Core.csproj` has zero UI package references and no `UseWindowsForms`** (the structural guarantee — worth an explicit assertion/check).
- All existing tests green (they only target Domain + Rendering, which moved wholesale).
- Manual smoke: app launches, renders an exercise, plays with the synced cursor — proving the bridge transport survived the move.
- Risk is **low**: mechanical namespace churn touches every file but changes no behavior.

---

## 9. Out of scope

- The web/cross-platform host itself.
- Extracting `wwwroot` into an RCL.
- A separate `ChordFlow.Infrastructure` project.
