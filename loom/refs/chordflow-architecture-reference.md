---
type: reference
id: rf_01KTSAPAT132QTEY5BEPRKS3MB
title: ChordFlow Architecture
status: active
created: "2026-06-10T00:00:00.000Z"
updated: 2026-06-13
version: 4
tags: []
parent_id: null
requires_load: []
slug: chordflow-architecture
description: "Map of ChordFlow's architecture: the Core/Desktop project split, vertical slices over the Domain kernel, the rendering seam, the C#↔JS bridge contract, persistence, the desktop host, and the seams that keep a future web/cross-platform host additive."
---
# ChordFlow Architecture

Stable map of **how ChordFlow is structured and why**. Load when reasoning about project boundaries, where new code belongs, or how a future host plugs in. Complements the deeper kernel map in `chordflow-domain-model-reference.md` (this doc is the *system* view; that one is the *music-theory* view).

---

## 1. One sentence

A **host-agnostic C# engine** turns a compact exercise definition into an **alphaTex string**, and a thin **desktop host** (WinForms + WebView2) hands that string to **alphaTab** (JS) for notation + synchronized playback. The engine knows nothing about how it is displayed.

---

## 2. Solution shape (two projects + tests)

```
ChordFlow.sln
  src/
    ChordFlow.Core/       net10.0          — the engine. ZERO UI/host references.
      Domain/             pure music kernel (no I/O, no UI)
      Rendering/          AlphaTexRenderer + RhythmQuantizer (only alphaTex-aware code)
      Features/           GenerateExercise, PracticeSession, ExerciseLibrary, Progress
      Bridge/             C#↔JS envelope DTOs + inbound WebMessageRouter (host-agnostic)
      Persistence/        SQLite store (EF Core) + migrations
    ChordFlow.Desktop/    net10.0-windows  — the host. The ONLY project with a UI package.
      Program.cs          entry point + bridge wiring
      WebHost/            WebView2 transport (WebView2Bridge) + virtual-host mapping
      wwwroot/            index.html, app.js (bridge module), alphaTab, font/, soundfont/
  tests/
    ChordFlow.Core.Tests/ net10.0          — xUnit, targets Core
```

**The load-bearing rule — dependency direction is one-way:**

```
ChordFlow.Desktop ──► ChordFlow.Core ◄── ChordFlow.Core.Tests
   (WinForms, WebView2)   (no UI refs)
```

`ChordFlow.Core` references no UI/host package and sets no `UseWindowsForms`, so it **physically cannot** call WinForms or WebView2. The "engine stays UI-agnostic" rule is enforced by the compiler, not by discipline. (History: the engine + host lived in one `ChordFlow.App` project until the `core-host-split` thread separated them — see `loom/chordflow/core-host-split/`.)

---

## 3. Layers inside Core

### Domain/ — the music kernel
Pure, immutable, fully unit-tested, **no I/O**. Harmony (PitchClass, interval-backed Quality, Chord, Scale + diatonic generation, NoteSpeller, Transposer), voicings (Voicing + strategy, VoicingBook, Fretboard), a **48-PPQ tick-grid rhythm model** (multi-bar RhythmPattern/PatternBar/RhythmEvent/TimeSignature) with feel/accent/stroke overlays, harmonic bars/spans for multi-chord-per-bar progressions, a **parser family** (`ProgressionParser`, `SongParser`, and the Rhythm-DSL `RhythmPatternParser`), and lead TargetZones. Full map: `chordflow-domain-model-reference.md`.

### Rendering/ — the only alphaTex-aware code
`AlphaTexRenderer : IScoreRenderer` maps an `Exercise → string` (alphaTex). The `RhythmQuantizer` collapses the tick grid into `:N` duration slots. This isolation is the **exporter seam**: a future MIDI/GuitarPro/MusicXML exporter is a new `IScoreRenderer`, not a rewrite.

### Features/ — vertical slices
Each is a class with a method composing Domain + Rendering + Persistence — **no mediator, no ceremonial layering**. `GenerateExercise` (definition → alphaTex), `PracticeSession` (play/stop/tempo + position echoes), `ExerciseLibrary` (save/list/reload — regenerating alphaTex on load, never storing it), `Progress` (mark-practiced records), `Packs` (the open-core content layer: `PackReader` loads a `manifest.json` + per-kind `.dsl` folder bundle from disk; `PackImporter` upserts it idempotently by the composite `(Id, Origin)` key, caller-declaring the tier — BuiltIn for the default pack, Pack for third-party. A pack is data-only; importing one needs zero engine change).

### Bridge/ — the host-agnostic contract
The JSON envelope DTOs (e.g. `StatusEnvelope`, `LoadScoreEnvelope`, `PracticeRecordedEnvelope` — the outbound ones live with their feature) and `IBridge` (the C#→JS send abstraction features depend on), plus `WebMessageRouter`, which parses inbound JSON and raises typed events. The router is host-agnostic on purpose: any host reuses it; only the *transport* differs.

### Persistence/ — SQLite via EF Core
Stores exercise **definitions** and practice events, **never alphaTex** (it's regenerated on load, so a renderer fix improves every saved exercise). One local file at `%LOCALAPPDATA%\ChordFlow\chordflow.db`; migrations apply on startup. Lives in Core (not the host) so a future web host reuses it. `Domain/` stays I/O-free — persistence is a separate Core sub-area.

---

## 4. The desktop host (Desktop)

A WinForms `Form` hosts a dock-filled `WebView2` control (windowed controller — the path that renders on the .NET 10 + WebView2-149 stack, where Photino's composition controller rendered black; see `photino-net-desktop-host-reference.md`). `wwwroot` is served over an in-process `https://chordflow.local/` virtual host via `SetVirtualHostNameToFolderMapping` — **no HTTP server, no localhost port**, and a real `https` origin so alphaTab's soundfont fetch isn't CORS-blocked. `WebView2Bridge` (the one type touching `CoreWebView2`) implements `IBridge` and forwards inbound messages to the Core `WebMessageRouter`.

---

## 5. The C#↔JS bridge — a narrow string protocol

`CoreWebView2.PostWebMessageAsString` (C#→JS) and the `WebMessageReceived` event (JS→C#). Small JSON envelopes; **the payload that matters is just the alphaTex string**. The envelope `type` string is the entire contract surface (`loadScore` / `play` / `stop` / `setTempo` / `generate` / `save` / `listExercises` / `loadExercise` / `markPracticed` / `ready` / `playbackFinished` / `beatChanged` / `status` …). On the JS side a single `app.js` bridge module owns the `chrome.webview` plumbing; the rest of the UI talks only to that module.

---

## 6. Data flow (one exercise)

```
UI picks key/rhythm/tempo
  → generate envelope (JS→C#)
  → GenerateExercise.Build → Exercise (definition)
  → AlphaTexRenderer.Render → alphaTex string
  → loadScore envelope (C#→JS)
  → app.js: api.tex(tex)
  → alphaTab renders tablature + plays with a synced beat cursor
  → playedBeat/state events → beatChanged/playbackFinished (JS→C#)
```

Saving persists the **definition** only; reloading regenerates the alphaTex.

---

## 7. Why this is built to evolve

- **Compile-enforced UI-agnostic engine** → a web/cross-platform host is an *additive* `ChordFlow.Web` project (serve the same `wwwroot` + one JSON endpoint wrapping Core), not a rewrite. `wwwroot` extracts to a shared Razor Class Library only when that second host is real.
- **Rendering is a single seam** → new export formats are new `IScoreRenderer`s.
- **Content is data, not code** → built-in/library content loads from importable definition bundles, not hardcoded seed. The free starter set ships as the on-disk **default pack** (`Content/default-pack/`) imported on first run via `PackReader`/`PackImporter`; curated/paid packs are the same shape, an additive data drop. See `loom/ctx.md` and the `Packs` Features slice (§3).
- **Slices are independent** → a new feature (new progression, syncopation, difficulty auto-advance, audio-in accuracy) is a new class + data, touching one seam.

---

## 8. Pointers

- Global context & current architecture baseline: `loom/ctx.md`
- Music kernel detail: `loom/refs/chordflow-domain-model-reference.md`
- Progression DSL (end-user): `loom/refs/chordflow-dsl-reference.md`
- alphaTex syntax / alphaTab API: `loom/refs/alphatex-syntax-reference.md`, `loom/refs/alphatab-js-api-reference.md`
- Host migration rationale: `loom/refs/photino-net-desktop-host-reference.md`
- The split's idea/design/plan: `loom/chordflow/core-host-split/`
