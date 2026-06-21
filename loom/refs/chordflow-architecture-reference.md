---
type: reference
id: rf_01KTSAPAT132QTEY5BEPRKS3MB
title: ChordFlow Architecture
status: active
created: 2026-06-10
updated: 2026-06-21
version: 40
tags: []
parent_id: null
requires_load: []
slug: chordflow-architecture
description: "Map of ChordFlow's architecture: the Core/Desktop project split, vertical slices over the Music theory kernel, the rendering seam, the C#↔JS bridge contract, persistence, the desktop host, and the seams that keep a future web/cross-platform host additive."
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
      Music/             pure music kernel — instrument-agnostic (no I/O, no UI)
      Instruments/Guitar/ the guitar adapter: Fretboard geometry + IntervalLattice + OctaveShape, voicings/CAGED, diagrams, GuitarInstrument
      Rendering/          AlphaTexRenderer + RhythmQuantizer (only alphaTex-aware code)
      Features/           GenerateExercise, PracticeSession, ExerciseLibrary, Progress
      Bridge/             C#↔JS envelope DTOs + inbound WebMessageRouter (host-agnostic)
      Persistence/        SQLite store (EF Core) + migrations
    ChordFlow.Desktop/    net10.0-windows  — the host. The ONLY project with a UI package.
      Program.cs          entry point + bridge wiring
      WebHost/            WebView2 transport (WebView2Bridge) + virtual-host mapping
      wwwroot/            index.html (Practice ⇄ Content ⇄ Scales ⇄ Debug views), bridge.js (shared transport), score-render-component.js (shared alphaTex→alphaTab render/transport component), app.js (Practice), content-crud.js (Content editor) + fretboard-render-component.js (shared SVG fretboard render component) + fretboard-sandbox.html (hand-fed dev harness for it), scales.js (Scales: interval-set → lit neck), alphatex-inspector.js (Debug: live edit + render the engine's alphaTex), alphaTab, font/, soundfont/
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

### Music/ — the music kernel
Pure, immutable, fully unit-tested, **no I/O**, and **instrument-agnostic** (references nothing under `Instruments/` — compiler-IL-enforced by an architecture test, §7). Organized as **concept-named flat-sibling namespaces** under `ChordFlow.Music`: **Harmony** (PitchClass, interval-backed Quality, Chord, Scale + diatonic generation, NoteSpeller/IntervalSpeller), **Rhythm** (the **48-PPQ tick-grid model** — multi-bar RhythmPattern/PatternBar/RhythmEvent/TimeSignature — with feel/accent/stroke overlays and the Rhythm-DSL `RhythmPatternParser`), **Progressions** (harmonic bars/spans for multi-chord-per-bar progressions, the Nashville `ProgressionParser`, and `Transposer` — which realizes a key-independent progression into concrete chords), **Songs** (`Song`/`SongParser`/`SongExpander` + the `IProgressionStore` port), and **Melody** (`LeadTargets` derives guide-tone `TargetZone`s as **pitch classes** — resolving them to frets is a guitar concern, on `GuitarInstrument.ResolveLead`). The sub-namespaces form an **acyclic DAG with Harmony/Rhythm as sinks**, frozen by `MusicLayeringTests` (§7). The composed practice unit (`Exercise`, `Difficulty`) sits outside the theory kernel in `ChordFlow.Exercises`. Full map: `chordflow-domain-model-reference.md`.

### Instruments/Guitar/ — the guitar adapter
Everything guitar-specific, kept out of the kernel so `Music/` stays provably instrument-agnostic. Geometry (`Fretboard`, `FretPosition`), realization (`Voicing` + `IVoicingStrategy`/`BeginnerShellStrategy`, `VoicingBook`, the CAGED voicing types `VoicingShape`/`CagedShape`/`VoicingRealizer` + DSL, `VoicingDiagram`), the **`Caged/` derivation engine** (`CagedDerivation`/`ChordShape`/`HandReach`/`AnchorFinger`/`CandidateSelector`) that *computes* CAGED grips from theory with no authored fret tables (oracle-proven against the authored pack), the `Geometry/` primitives (`IntervalLattice`, `OctaveShape`), and the spatial `FretboardDiagram` carrier. The concrete **`GuitarInstrument`** facade is the deliberate public surface over these (`Realize` a chord → a fret `Voicing`, `Diagram` a shape → `FretboardDiagram`, `ResolveLead` a target zone → fret positions). A namespace boundary inside Core, **not** a separate assembly (one real instrument needs no project split). The polymorphic `IInstrument` is deferred until its first caller (`instrument-rendering`). Full map: `chordflow-domain-model-reference.md`.

### Rendering/ — the only alphaTex-aware code
`AlphaTexRenderer : IScoreRenderer` maps a `RealizedSong → string` (alphaTex) and is **pure/store-free**: the `Exercise → RealizedSong` expansion (the one I/O seam — it needs the `IProgressionStore`) lives in Features (`ExerciseRendering`), so the renderer never resolves references (merge decision (a); there is no `Render(Exercise)` overload). The `RhythmQuantizer` collapses the tick grid into `:N` duration slots. This isolation is the **exporter seam**: a future MIDI/GuitarPro/MusicXML exporter is a new `IScoreRenderer`, not a rewrite.

### Features/ — vertical slices
Each is a class with a method composing Music + Rendering + Persistence — **no mediator, no ceremonial layering**. `GenerateExercise` (definition → alphaTex), `PracticeSession` (play/stop/tempo + position echoes), `ExerciseLibrary` (save/list/reload — regenerating alphaTex on load, never storing it), `Progress` (mark-practiced records), `ContentCrud` (the generic CRUD surface behind the `entity*` bridge family: maps an entity discriminator to its `IContentStore` for list/get/save/delete, builds score/diagram previews, raises `VoicingsChanged` for live-refresh), `Packs` (the open-core content layer: `PackReader` loads a `manifest.json` + per-kind `.dsl` folder bundle from disk; `PackImporter` upserts it idempotently by the composite `(Id, Origin)` key, caller-declaring the tier — BuiltIn for the default pack, Pack for third-party. A pack is data-only; importing one needs zero engine change), `SoundFontLibrary` (the playback-soundfont slice: composes the host's `ISoundFontCatalog` — the Core-side discovery seam, implemented by the host scanning `wwwroot/soundfont` — with `IAppSettings` to list the available `.sf2` fonts + the persisted global choice, and to persist a new selection; no alphaTex/Music involvement, the font only chooses the synth bank).

### Bridge/ — the host-agnostic contract
The JSON envelope DTOs (e.g. `StatusEnvelope`, `LoadScoreEnvelope`, `PracticeRecordedEnvelope` — the outbound ones live with their feature) and `IBridge` (the C#→JS send abstraction features depend on), plus `WebMessageRouter`, which parses inbound JSON and raises typed events. The router is host-agnostic on purpose: any host reuses it; only the *transport* differs.

### Persistence/ — SQLite via EF Core
Stores exercise **definitions** and practice events, **never alphaTex** (it's regenerated on load, so a renderer fix improves every saved exercise). One local file at `%LOCALAPPDATA%\ChordFlow\chordflow.db`; migrations apply on startup. A small **`AppSettings`** key/value table (via `IAppSettings`/`AppSettingsStore`) holds app-wide preferences that aren't content — e.g. the global playback soundfont choice; the store takes `DbContextOptions` and opens a short-lived context per access (an app-lifetime singleton, not a per-request content store). Lives in Core (not the host) so a future web host reuses it. The four content stores (`ProgressionStore`/`SongStore`/`RhythmPatternStore`/`VoicingStore`) implement a shared **`IContentStore`** (list/get/save/delete in DSL strings) for the CRUD UI; writes only ever target the `UserDefined` tier (editing a BuiltIn/Pack id writes a shadow; delete = delete or revert), and voicing saves canonicalize to C. `Music/` stays I/O-free — persistence is a separate Core sub-area.

---

## 4. The desktop host (Desktop)

A WinForms `Form` hosts a dock-filled `WebView2` control (windowed controller — the path that renders on the .NET 10 + WebView2-149 stack, where Photino's composition controller rendered black; see `photino-net-desktop-host-reference.md`). `wwwroot` is served over an in-process `https://chordflow.local/` virtual host via `SetVirtualHostNameToFolderMapping` — **no HTTP server, no localhost port**, and a real `https` origin so alphaTab's soundfont fetch isn't CORS-blocked. `WebView2Bridge` (the one type touching `CoreWebView2`) implements `IBridge` and forwards inbound messages to the Core `WebMessageRouter`.

**Packaging / distribution.** The host ships as a **self-contained, single-file `ChordFlow.exe`** (`<AssemblyName>ChordFlow</AssemblyName>`; `win-x64`) with its `wwwroot/` tree as **loose files beside the exe** — single-file embeds only the .NET runtime, *not* `Content`, which is required: the host serves `wwwroot` from disk and the soundfont catalog scans `wwwroot/soundfont` at runtime, so embedding would break both. The small default GM soundfont (`sonivox.sf2`, Apache-2.0) is **committed** and copied to the publish output (no build-time download — the build is hermetic); larger banks stay out of the repo and are user-added. The release artifact is that `ChordFlow.exe` + `wwwroot/` zipped and attached to a GitHub release by the tag-driven `release` workflow (see `RELEASING.md`).

---

## 5. The C#↔JS bridge — a narrow string protocol

`CoreWebView2.PostWebMessageAsString` (C#→JS) and the `WebMessageReceived` event (JS→C#). Small JSON envelopes; **the payload that matters is just the alphaTex string**. The envelope `type` string is the entire contract surface (`loadScore` / `play` / `stop` / `setTempo` / `generate` / `save` / `listExercises` / `loadExercise` / `markPracticed` / `ready` / `playbackFinished` / `beatChanged` / `status` …), plus the **generic content-CRUD family** `entityList` / `entityGet` / `entityPreview` / `entitySave` / `entityDelete` (each carrying an `entity` discriminator) and its replies (`entityLoaded` / `entityPreview` / `entityParseError` / `entitySaved` / `entityDeleted`), plus the **playback-soundfont pair** `listSoundFonts` / `setSoundFont` (inbound) with the `soundFontsListed` reply (`{fonts:[{id,name}], selectedId}`), plus the **Scales** verb `scalePreview` (`{intervals, rootPitchClass}`) with its `scaleDiagram` / `scaleError` replies (a `FretboardDiagram` for the shared fretboard view, or an inline parse message). Soundfont is *not* a render input, so it carries no `renderOptions` and never triggers a C# re-render. On the JS side a shared `bridge.js` module owns the `chrome.webview` plumbing and **fans inbound messages out to every view** (Practice `app.js`, Content `content-crud.js` + the shared SVG `fretboard-render-component.js`, Scales `scales.js`, and the Debug `alphatex-inspector.js` — which caches each `loadScore.tex` off the fan-out so its editor can load the last engine output); each view ignores envelope types it doesn't own. The three **render-producing** verbs (`generate` / `entityPreview` / `loadExercise`) carry an optional **`renderOptions`** bag (`showChordNames` / `showChordDiagrams` / `voicing`) mapped to the Core `RenderOptions`; absent ⇒ today's render (backward-compatible). The `generate` verb additionally carries the chosen **content references + params** — a harmony discriminator (`harmonyEntity` ∈ `song`/`progression`) + `harmonyId`, a `compingPatternId`, an optional `leadPatternId`, plus `keyPitchClass` / `tempo` / `difficulty` / `feel` — which `GenerateExercise.Build` resolves from the content stores via the shared **`ExerciseRefs`** seam into a canonical `Exercise` (the saved-exercise **load** path resolves the same way: `SongId` tries the Song store, else a lifted Progression). The Practice `app.js` populates its harmony/comping/lead pickers from the existing `entityList` replies — no new bridge verb.

**The shared render component (`score-render-component.js`, `window.ChordFlowScore`)** is the single owner of *alphaTex string → alphaTab notation + transport*. Every score-showing view consumes it (Practice and the Content progression/song/rhythm preview both in full-player mode), so there is exactly one alphaTab integration + settings source. Options split two ways: **player-kind** (metronome, count-in, soundfont pick) applied locally via the alphaTab API — the soundfont picker swaps the synth font live with `loadSoundFontFromUrl` and persists the choice via `setSoundFont`; **content-kind** (chord names, diagrams, voicing) fire `onNeedsRerender(renderOptions)` so the consumer re-requests a C# render — alphaTex is never built in JS.

**The shared fretboard render component (`fretboard-render-component.js`, `window.ChordFlowFretboard`)** is the *spatial twin* of `ChordFlowScore`: the single owner of *Core-computed `FretboardDiagram` marker model → SVG fretboard*. `create(container, opts) → { render(model), setLabelMode, setOrientation, dispose }`; it owns its toolbar and an auto-built legend, and draws a **vertical chord-box or horizontal neck** (`orientation`) from a flat **marker list** (color = interval via the default 5-colour function palette, or an override per-interval palette **with an optional `"*"` fallback colour** for unlisted intervals — e.g. the Scales page's root-red/rest-black `{ "1":"#e2574c", "*":"#000" }`; shape = layer; open/muted/barre chrome; auto-fit fret window or toolbar min/max). Its toolbar controls each have a **per-control visibility flag** (`controls: { orientation, fretWindow, label, legend }`, all on by default) so a consumer hides what it fixes — the voicing fret-box hides orientation+fretWindow (stays the byte-identical vertical box); the Scales page locks `orientation:false` (always horizontal). Like `ChordFlowScore` it is a **dumb view** — zero music theory in JS; theory lives in Core (`Music/Diagrams/FretboardDiagram` + producers). Producers: the voicing fret-box (`VoicingDiagram.Build`, Content/Voicings preview) and the scale/interval-set diagram (`IntervalSetDiagram.Build`, the **Scales** view — type an interval set + root, the `scalePreview` verb returns a lit neck; the `interval-lattice` dogfood); arpeggio/CAGED producers attach additively as the derivation-engine threads ship. `fretboard-sandbox.html` is a standalone hand-fed harness (not in nav) for rendering arbitrary marker sets before those producers exist.

---

## 6. Data flow (one exercise)

```
UI picks harmony (Song/Progression) + comping + optional lead + params (key/tempo/difficulty/feel)
  → generate envelope carrying those references + params (JS→C#)
  → GenerateExercise.Build → ExerciseRefs resolves the references from the stores → Exercise (definition)
  → ExerciseRendering: expand Song (KeyOverride ?? InitialKey) → RealizedSong, then AlphaTexRenderer.Render(RealizedSong, comping, …, lead?) → alphaTex string
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

### Theory ↔ instrument boundary (live — `guitar/instrument-boundary`)

The kernel is split into **pure theory** and a **guitar adapter**, so `Music/` is provably instrument-agnostic and guitar is an opt-in adapter (origin chat `loom/meta/general/chats/general-chat-005.md`):

- **`Music/`** is **pure, instrument-agnostic music theory** (harmony, the rhythm grid, scales, the interval *vocabulary*, progression/song, lead targets as pitch classes).
- **`Instruments/Guitar/`** holds everything guitar — tuning/`Fretboard` geometry, fret voicings, CAGED, fretboard diagrams — behind the concrete **`GuitarInstrument`** facade. A **namespace boundary inside `ChordFlow.Core`**, not a separate assembly.
- Enforced by an **architecture test** (`NetArchTest.Rules`, IL-level): no type under `ChordFlow.Music` may reference `ChordFlow.Instruments`. `Rendering → Instruments` and `Persistence → Instruments` stay *allowed* — the tab renderer and voicing store consume fret positions; only the **Music edge** is guarded. A companion **`MusicLayeringTests`** freezes the `Music.*` sub-namespace edges as an acyclic DAG (`Harmony`/`Rhythm` are sinks; `Progressions → Harmony/Rhythm`, `Songs → Progressions/Harmony/Rhythm`, `Melody → Harmony`).
- The concrete **`GuitarInstrument`** adapter is the live surface; the polymorphic **`IInstrument`** interface is still deferred until its first real caller exists (the notation/tab renderer fork — `instrument-rendering`).

Shape (arrows point up; only the `Music → Instruments` edge is test-enforced):

```
┌──────────────────────────────────────────────────────────────────┐
│ Music/   PURE MUSIC THEORY — instrument-agnostic                  │
│   harmony · rhythm (48-PPQ) · scales · intervals (vocabulary) ·    │
│   progression/song · lead targets                                  │
│   output vocabulary → ChordTones / PitchClasses / Pitch(pc+octave) │
│   RULE: references nothing below.   ◄── architecture test guards   │
└───────────────┬────────────────────────────────────────────────────┘
                │ theory in; never referenced back by Music
                ▼
┌──────────────────────────────────────────────────────────────────┐
│ Instruments/                                                       │
│   IInstrument  (thin: Realize → pitches)  [deferred until a caller]│
│   Guitar/   ← the only real adapter today                          │
│     geometry: tuning · Fretboard · IntervalLattice · OctaveShape   │
│     realize:  Voicing · VoicingBook · CAGED · VoicingRealizer      │
│     diagram:  FretboardDiagram · VoicingDiagram                    │
│   « Piano/ — extension point, not built »                          │
└───────────────┬────────────────────────────────────────────────────┘
                │ agnostic pitches + guitar fret positions
                ▼
┌──────────────────────────────────────────────────────────────────┐
│ Rendering/   export seam (IScoreRenderer)                          │
│   AlphaTexRenderer → guitar tab today                              │
│     · notation/staff = agnostic    · tab = guitar fret positions   │
│     « future fork: agnostic-notation ∥ instrument-tab »            │
└───────────────┬────────────────────────────────────────────────────┘
                ▼  alphaTex string + FretboardDiagram  (Bridge DTOs)
┌──────────────────────────────────────────────────────────────────┐
│ UI (JS) — dumb views                                               │
│   score-render-component       (notation, any instrument)          │
│   fretboard-render-component   (guitar spatial SVG)                │
└──────────────────────────────────────────────────────────────────┘
```

The structural move + `GuitarInstrument` adapter landed in `guitar/instrument-boundary`; the renderer fork + the polymorphic `IInstrument` are still to come in `chordflow/instrument-rendering`. The diagram's future-fork annotations (`« future fork… »`, `IInstrument [deferred…]`) mark what that thread adds.

---

## 8. Pointers

- Global context & current architecture baseline: `loom/ctx.md`
- Music kernel detail: `loom/refs/chordflow-domain-model-reference.md`
- Progression DSL (end-user): `loom/refs/chordflow-dsl-reference.md`
- alphaTex syntax / alphaTab API: `loom/refs/alphatex-syntax-reference.md`, `loom/refs/alphatab-js-api-reference.md`
- Host migration rationale: `loom/refs/photino-net-desktop-host-reference.md`
- The split's idea/design/plan: `loom/chordflow/core-host-split/`
