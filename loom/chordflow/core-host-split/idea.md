---
type: idea
id: id_01KTRRCRC5ES07FP464QHF7QPM
title: Core/Host Project Split
status: done
created: "2026-06-10T00:00:00.000Z"
updated: 2026-06-10
version: 3
tags: []
parent_id: null
requires_load: []
---
# Core/Host Project Split

## 1. Motivation

The MVP currently lives in a single project (`src/ChordFlow.App/`) with `Domain/`, `Rendering/`, `Features/`, `Infrastructure/` as folders. The global ctx already mandates that **the engine stays UI-agnostic** so a web/cross-platform front-end is an *additive* option, not a rewrite. Today that rule is enforced only by discipline — nothing physically stops engine code from referencing WinForms or WebView2.

Splitting the solution into separate projects makes the constraint **structural instead of aspirational**: an engine project that does not reference a UI/host package literally cannot compile a WinForms or WebView2 call. The cost of doing this **early — while the codebase is small — is far lower than retrofitting it later**.

## 2. Proposed structure

```
src/
  ChordFlow.Core/      — Domain + Rendering + Features (+ persistence). Zero UI/host references.
  ChordFlow.Desktop/   — WinForms + WebView2 host. References Core. Owns wwwroot + the C#↔JS bridge transport.
tests/
  ChordFlow.Core.Tests/
```

- **`ChordFlow.Core`** — the host-agnostic engine: pure music kernel + renderer + feature slices. Produces alphaTex strings; knows nothing about how they are displayed.
- **`ChordFlow.Desktop`** — the host: WinForms shell, the WebView2 control, virtual-host wiring, the JSON-envelope bridge *transport*, and `wwwroot`.
- Dependency direction is strictly **Desktop → Core**, never the reverse.

## 3. Why now (not later)

- The codebase is still small — moving files between projects is cheap today, expensive once Infrastructure/SQLite and more feature slices land.
- Makes the UI-agnostic rule **self-enforcing at compile time**, removing a whole class of accidental coupling before it can start.
- Sets up the future web/cross-platform host as a clean additive `ChordFlow.Web` project (serve the same `wwwroot` + one JSON endpoint wrapping Core), with **no engine changes**.

## 4. Resolved decisions

1. **Project naming** — `ChordFlow.Core` (engine) + `ChordFlow.Desktop` (host). Not `.Engine`.
2. **`wwwroot` location** — **stays inside `ChordFlow.Desktop`** for now (Option A). Static assets have no compile-time coupling to prevent and are cheap to move later (one virtual-host path string, not a reference graph); the portability boundary that matters is the **host-neutral `wwwroot` + bridge isolated in one JS module** (already a ctx constraint), not the project location. Promote `wwwroot` to a shared Razor Class Library only when a real second host (web/cross-platform) appears.
3. **Bridge contract** — the JSON-envelope **DTOs live in `ChordFlow.Core`** so any host implements the same contract; the **transport** (WebView2 message router + virtual host) lives in `ChordFlow.Desktop`.
4. **Test layout** — start with a single `ChordFlow.Core.Tests`; split per-area as it grows (it is expected to grow).
5. **Persistence (decided in design)** — the SQLite store lives in **`ChordFlow.Core`**, not Desktop, so a future web host reuses it; `Domain/` stays I/O-free. See the design doc.

## 5. Scope

Purely a **structural refactor** of the solution into Core + Desktop projects with clean dependency direction. **No behavior change, no new features.** The MVP design and the domain model are unaffected — only *where the code physically lives*.

## 6. Origin

Raised in `loom/meta/general/chats/general-chat-001.md` (Q1/Q2 on `src/` layout and multi-platform readiness).
