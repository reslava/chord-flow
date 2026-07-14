---
type: reference
id: rf_01KTSAPAT132QTEY5BEPRKS3MB
title: ChordFlow Architecture
status: active
created: 2026-06-10
updated: 2026-07-14
version: 69
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
      wwwroot/            index.html (Practice ⇄ Content ⇄ Scales views), bridge.js (shared transport), score-render-component.js (shared alphaTex→alphaTab render/transport component — incl. the opt-in `debugPanel` alphaTex scratchpad), app.js (Practice), content-crud.js (Content editor) + fretboard-render-component.js (shared SVG fretboard render component) + fretboard-sandbox.html (hand-fed dev harness for it), scales.js (Scales: interval-set → lit neck), chord-sheet-render-component.js (ChordSheetR — pure-SVG chord sheets) + chord-sheets.js (the Chord Sheets page shell), alphaTab, font/, soundfont/
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
Everything guitar-specific, kept out of the kernel so `Music/` stays provably instrument-agnostic. Geometry (`Fretboard`, `FretPosition`), realization (`Voicing` + `IVoicingStrategy`/`BeginnerShellStrategy`, `VoicingBook`, the CAGED voicing types `VoicingShape`/`CagedShape`/`VoicingRealizer` + DSL, `VoicingDiagram`), the **`Caged/` derivation engine** (`CagedDerivation`/`ChordShape`/`HandReach`/`AnchorFinger`/`CandidateSelector`) that *computes* CAGED grips from theory with no authored fret tables (oracle-proven against the 36-grip **test-only oracle fixture** — the engine, not the pack, is the app's `automatic` voicing source; engine-derived-as-app-source), the `Geometry/` primitives (`IntervalLattice`, `OctaveShape`), and the spatial `FretboardDiagram` carrier. The concrete **`GuitarInstrument`** facade is the deliberate public surface over these (`Realize` a chord → a fret `Voicing`, `Diagram` a shape → `FretboardDiagram`, `ResolveLead` a target zone → fret positions). A namespace boundary inside Core, **not** a separate assembly (one real instrument needs no project split). The polymorphic `IInstrument` is deferred until its first caller (`instrument-rendering`). Full map: `chordflow-domain-model-reference.md`.

### Rendering/ — the only alphaTex-aware code
`AlphaTexRenderer : IScoreRenderer` maps a `RealizedSong → string` (alphaTex) and is **pure/store-free**: the `Exercise → RealizedSong` expansion (the one I/O seam — it needs the `IProgressionStore`) lives in Features (`ExerciseRendering`), so the renderer never resolves references (merge decision (a); there is no `Render(Exercise)` overload). It also **no longer selects voicings** (engine-derived-as-app-source D4=(B)): the Features comping resolver (`CompingResolver`) builds a `CompingPlan` (chord → grip, from the chosen voicing source + ranking) that the renderer formats — so a future exporter or the now/next fretboards draw from the same resolved grips. The `RhythmQuantizer` collapses the tick grid into `:N` duration slots. This isolation is the **exporter seam**: a future MIDI/GuitarPro/MusicXML exporter is a new `IScoreRenderer`, not a rewrite. **`Rendering/` is now the presentation/export seam more broadly** — beyond alphaTex it also owns the instrument-agnostic **`ChordSheet` presentation model** (`Rendering/ChordSheets/`: `Header → Sections → Rows → Cells → ChordRef → Tone`, its only guitar edge an optional `FretboardDiagram?` on `ChordRef`, which is why it sits here — the allowed `Rendering → Instruments` edge — not in `Music/`). The `ChordSheet` is built by the `ChordSheetBuilder` Features slice from a `RealizedSong` (concrete/Nashville/Roman notations, spelled tone strips, `%` similes, comped fret diagrams — no new music theory) and drawn by ChordSheetR.

### Features/ — vertical slices
Each is a class with a method composing Music + Rendering + Persistence — **no mediator, no ceremonial layering**. `GenerateExercise` (definition → alphaTex), `PracticeSession` (play/stop/tempo + position echoes), `ExerciseLibrary` (save/list/reload — regenerating alphaTex on load, never storing it), `Progress` (mark-practiced records), `ContentCrud` (the generic CRUD surface behind the `entity*` bridge family: maps an entity discriminator to its `IContentStore` for list/get/save/delete, builds score/diagram previews, raises `VoicingsChanged` for live-refresh), `Packs` (the open-core content layer: `PackReader` loads a `manifest.json` + per-kind `.dsl` folder bundle from disk; `PackImporter` upserts it idempotently by the composite `(Id, Origin)` key, stamping `Origin.Pack` with the manifest id as each row's `PackId` (the default pack is just a package — there is no special tier). A pack is data-only; importing one needs zero engine change. `ContentSourceMigration` retires legacy `BuiltIn` rows + forks legacy same-id user shadows on startup — a no-op once migrated), `SoundFontLibrary` (the playback-soundfont slice: composes the host's `ISoundFontCatalog` — the Core-side discovery seam, implemented by the host scanning `wwwroot/soundfont` — with `IAppSettings` to list the available `.sf2` fonts + the persisted global choice, and to persist a new selection; no alphaTex/Music involvement, the font only chooses the synth bank).

### Bridge/ — the host-agnostic contract
The JSON envelope DTOs (e.g. `StatusEnvelope`, `LoadScoreEnvelope`, `PracticeRecordedEnvelope` — the outbound ones live with their feature) and `IBridge` (the C#→JS send abstraction features depend on), plus `WebMessageRouter`, which parses inbound JSON and raises typed events. The router is host-agnostic on purpose: any host reuses it; only the *transport* differs.

### Persistence/ — SQLite via EF Core
Stores exercise **definitions** and practice events, **never alphaTex** (it's regenerated on load, so a renderer fix improves every saved exercise). One local file at `%LOCALAPPDATA%\ChordFlow\chordflow.db`; migrations apply on startup. A small **`AppSettings`** key/value table (via `IAppSettings`/`AppSettingsStore`) holds app-wide preferences that aren't content — e.g. the global playback soundfont choice; the store takes `DbContextOptions` and opens a short-lived context per access (an app-lifetime singleton, not a per-request content store). Lives in Core (not the host) so a future web host reuses it. The four content stores (`ProgressionStore`/`SongStore`/`RhythmPatternStore`/`VoicingStore`) implement a shared **`IContentStore`** (list/get/save/delete in DSL strings) for the CRUD UI. Provenance is two stored tiers — **`Pack`** (carrying the source pack's `PackId`; the default pack is an ordinary package, id `"default"`) and **`UserDefined`** — plus the computed **`automatic`** source that isn't stored (the `Origin.BuiltIn` tier was retired in `content-source-model`). The **multi-source model** (`content-source-model`): `List()` is **additive** — one `ContentSummary` per `(id, source)`, never collapsed — each tagged with its `ContentSource`; writes are **user-only, fork-on-edit** — `Save` updates an existing user row in place, but editing a package item mints a **new** user row with a fresh id (never a same-id shadow), so the package original stays listed; `Delete` removes only the user row (no "revert"). Voicing saves canonicalize to C. `Music/` stays I/O-free — persistence is a separate Core sub-area.

---

## 4. The desktop host (Desktop)

A WinForms `Form` hosts a dock-filled `WebView2` control (windowed controller — the path that renders on the .NET 10 + WebView2-149 stack, where Photino's composition controller rendered black; see `photino-net-desktop-host-reference.md`). `wwwroot` is served over an in-process `https://chordflow.local/` virtual host via `SetVirtualHostNameToFolderMapping` — **no HTTP server, no localhost port**, and a real `https` origin so alphaTab's soundfont fetch isn't CORS-blocked. `WebView2Bridge` (the one type touching `CoreWebView2`) implements `IBridge` and forwards inbound messages to the Core `WebMessageRouter`.

**Packaging / distribution.** The host ships as a **self-contained, single-file `ChordFlow.exe`** (`<AssemblyName>ChordFlow</AssemblyName>`; `win-x64`) with its `wwwroot/` tree as **loose files beside the exe** — single-file embeds only the .NET runtime, *not* `Content`, which is required: the host serves `wwwroot` from disk and the soundfont catalog scans `wwwroot/soundfont` at runtime, so embedding would break both. The small default GM soundfont (`sonivox.sf2`, Apache-2.0) is **committed** and copied to the publish output (no build-time download — the build is hermetic); larger banks stay out of the repo and are user-added. The release artifact is that `ChordFlow.exe` + `wwwroot/` zipped and attached to a GitHub release by the tag-driven `release` workflow (see `RELEASING.md`).

---

## 5. The C#↔JS bridge — a narrow string protocol

`CoreWebView2.PostWebMessageAsString` (C#→JS) and the `WebMessageReceived` event (JS→C#). Small JSON envelopes; **the payload that matters is just the alphaTex string**. The envelope `type` string is the entire contract surface (`loadScore` / `play` / `stop` / `setTempo` / `generate` / `save` / `listExercises` / `loadExercise` / `markPracticed` / `ready` / `playbackFinished` / `beatChanged` / `status` …), plus the **generic content-CRUD family** `entityList` / `entityGet` / `entityPreview` / `entitySave` / `entityDelete` (each carrying an `entity` discriminator) and its replies (`entityLoaded` / `entityPreview` / `entityParseError` / `entitySaved` / `entityDeleted`), plus the **playback-soundfont pair** `listSoundFonts` / `setSoundFont` (inbound) with the `soundFontsListed` reply (`{fonts:[{id,name}], selectedId}`), plus the **staff-display-profile pair** `getStaffProfile` / `setStaffProfile` (inbound) with the `staffProfile` reply (`{profile}`) — a display-only score-view preference (tab/standard/both) persisted via the same `AppSettings` store as the soundfont choice, plus the **Scales** verb `scalePreview` (`{intervals, rootPitchClass}`) with its `scaleDiagram` / `scaleError` replies (a `FretboardDiagram` for the shared fretboard view, or an inline parse message), plus the **GuitarVoicingsR** verb `voicingGrid` (`{root, sources[], families[], thirds[], fifths[], sevenths[]}` — a single global root + the multi-select enabled-token sets per filter level) with its `voicingGridResult` reply (`{cells:[{id, title, family, quality, shape, diagram:FretboardDiagram}]}`): the whole faceted grid filtered from `CagedVoicingCatalog` (the single combo source of truth) and realized via the shared `FamilyVoicing` → `RealizedVoicingDiagram` path in **one round-trip** (no N+1 per cell). Filter semantics: OR within a level (set membership) / AND across levels, a `null` level unconstrained, an empty result an empty cell list (never an error). Only the `automatic` source yields cells today; `package`/`user` stay in the wire shape but produce nothing until a stored-combo enumeration source lands. Soundfont is *not* a render input, so it carries no `renderOptions` and never triggers a C# re-render. On the JS side a shared `bridge.js` module owns the `chrome.webview` plumbing and **fans inbound messages out to every view** (Practice `app.js`, Content `content-crud.js` + the shared SVG `fretboard-render-component.js`, Scales `scales.js`); each view ignores envelope types it doesn't own. The three **render-producing** verbs (`generate` / `entityPreview` / `loadExercise`) carry an optional **`renderOptions`** bag (`showChordNames` / `showChordDiagrams` / a structured **`voicing`** source `{kind, minFret?, maxFret?, packageId?, ranking?}`) mapped to the Core `RenderOptions`; the `voicing` knob feeds the Features comping resolver (absent ⇒ automatic / full neck / Closest; engine-derived-as-app-source). The `generate` verb additionally carries the chosen **content references + params** — a harmony discriminator (`harmonyEntity` ∈ `song`/`progression`) + `harmonyId`, a `compingPatternId`, an optional `leadPatternId`, plus `keyPitchClass` / `tempo` / `difficulty` / `feel` — which `GenerateExercise.Build` resolves from the content stores via the shared **`ExerciseRefs`** seam into a canonical `Exercise` (the saved-exercise **load** path resolves the same way: `SongId` tries the Song store, else a lifted Progression). The Practice `app.js` populates its harmony/comping/lead pickers from the existing `entityList` replies — no new bridge verb. Every `entityList` item carries a **`source`** (`package`/`user`/`automatic`) and, for package items, the source pack's display **`packName`** — the Content view renders a source badge per item and a transient source filter (content-source-model: every source shown, none hidden; package/automatic items are read-only with "Duplicate to user"; no "Revert"). The handler also exposes a **union seam** (`IComputedContentSource`) so a non-store computed source can join a kind's list — **filled by `EngineVoicingSource`**, which lists the 36 engine-derived `automatic` voicing families (`auto:dom7:E` …; engine-derived-as-app-source). The **song** `entityList` items additionally carry an **`initialKey`** (the song's `InitialKey` tonic pitch class, 0–11; null for the key-independent entities), so selecting a song in the harmony picker **seeds the key control from its authored key** (a progression seeds C); the sent `keyPitchClass` still becomes `KeyOverride`, so a manual override wins for the current selection and the saved-exercise load path (its own stored `KeyOverride`) is untouched (`play-ui-key-init`). The Content `content-crud.js` **progression/song preview** likewise carries an optional **`compingPatternId`** on its `entityPreview` envelope — chosen from a comping picker on the Content page (a content-selection knob, *not* part of the content-agnostic `ChordFlowScore`), populated from the rhythm `entityList` — which `ContentCrudHandler.Preview` resolves through the same **`ExerciseRefs.ResolvePattern`** seam (blank ⇒ the `beat_1_3` default; an unknown id fails loud as `entityParseError`). This replaced the previously hard-wired `SeedData.Quarters` preview comping; the picker is transient (resets to `beat_1_3` each page load).

**The shared render component (`score-render-component.js`, `window.ChordFlowScore`)** is the single owner of *alphaTex string → alphaTab notation + transport*. Every score-showing view consumes it (Practice and the Content progression/song/rhythm preview both in full-player mode), so there is exactly one alphaTab integration + settings source. Options split two ways: **player-kind** (metronome, count-in, soundfont pick) applied locally via the alphaTab API — the soundfont picker swaps the synth font live with `loadSoundFontFromUrl` and persists the choice via `setSoundFont`; the **staff-display profile** (tab / standard / both) is likewise display-only — it flips each staff's `showStandardNotation` / `showTablature` model flags + `api.render()` (re-asserted on `scoreLoaded`; no C# re-render, the `barsPerRow`/Auto-layout sibling) and persists globally via `setStaffProfile` → `AppSettings`; **content-kind** (chord names, diagrams, voicing) fire `onNeedsRerender(renderOptions)` so the consumer re-requests a C# render — alphaTex is never built in JS. An opt-in **`debugPanel`** flag adds a collapsed, editable alphaTex scratchpad under the staff (textarea + *Render from alphaTex* / *Reload from engine* + the alphaTab version label): it shows the exact tex last rendered and re-renders edits locally through the same alphaTab instance (dirty-state until reload), bypassing C#. This diagnostic surface **replaced the standalone Debug view** (`alphatex-inspector.js`, retired) — the scratchpad now sits on every score-rendering page.

**The shared fretboard render component (`fretboard-render-component.js`, `window.ChordFlowFretboard`)** is the *spatial twin* of `ChordFlowScore`: the single owner of *Core-computed `FretboardDiagram` marker model → SVG fretboard*. `create(container, opts) → { render(model), setLabelMode, setOrientation, setTheme, dispose }`; it owns its toolbar and an auto-built legend, and draws a **vertical chord-box or horizontal neck** (`orientation`) from a flat **marker list** (color = interval via the default 5-colour function palette, or an override per-interval palette **with an optional `"*"` fallback colour** for unlisted intervals — e.g. the Scales page's root-red/rest-black `{ "1":"#e2574c", "*":"#000" }`; shape = layer; open/muted/barre chrome; auto-fit fret window or toolbar min/max). Its toolbar controls each have a **per-control visibility flag** (`controls: { orientation, fretWindow, label, legend, theme }`, all on by default) so a consumer hides what it fixes — the voicing fret-box hides fretWindow (its auto-fit window is fixed); inside a **GuitarVoicingsR grid** each cell locks `orientation:false` + `theme:false` so the grid's one global orientation/theme toggle drives every cell. The standalone diagram pages (Scales, CAGED octave shapes, CAGED Chords, the Content voicing preview) **expose** the orientation toggle (each defaults to the layout that fits, but the user can flip it). Two optional create-opts support the grid: a per-cell **`title`** (a heading like "Dominant 7 (shell) — E shape" that overrides the diagram's own `model.title`) and a synthetic **`id`** (e.g. `auto:shell:dom7:E`) rendered with a **copy-to-clipboard** control — the oracle/debug handle and the seed of a future "explain this voicing" affordance. A **`theme: "light" | "dark"`** opt + matching **`setTheme`** method theme the component's **whole render surface** — a themed root wrapper owns the **background** behind the toolbar+SVG+legend (so the toggle actually changes the background rather than inheriting the host container's), plus the toolbar/legend foreground text, the buttons/inputs, and the SVG chrome (nut, lines, fret numbers, position label, muted `✕`). `light` = white surface + dark foreground; `dark` = dark-grey surface + light foreground (white fret numbers + `✕`). Default `light`. The marker function/interval palette is unchanged (it reads on both). Like `ChordFlowScore` it is a **dumb view** — zero music theory in JS; theory lives in Core (`Music/Diagrams/FretboardDiagram` + producers). Producers: the voicing fret-box (`VoicingDiagram.Build`, Content/Voicings preview) and the scale/interval-set diagram (`IntervalSetDiagram.Build`, the **Scales** view — type an interval set + root, the `scalePreview` verb returns a lit neck; the `interval-lattice` dogfood); arpeggio/CAGED producers attach additively as the derivation-engine threads ship. `RealizedVoicingDiagram.Build` is the **real-root** voicing producer (a concrete `Voicing` at its actual root; `VoicingDiagram.Build` is now its canonical-C special case, delegating to it), and a playback-aware consumer — `now-next-fretboards.js` (`window.ChordFlowNowNext`) — mounts **two** of these fret-boxes above the Practice score to show the current + next chord, synced to playback off the `loadScore` chord schedule (below). `fretboard-sandbox.html` is a standalone hand-fed harness (not in nav) for rendering arbitrary marker sets before those producers exist.

**The faceted voicings grid (`guitar-voicings-render-component.js`, `window.ChordFlowGuitarVoicings`)** — GuitarVoicingsR — is a *projection + layout* over the engine catalog and the **visual oracle** for the voicings subsystem: `create(container, opts) → { show, dispose, setOrientation, setLabelMode, setTheme }`. It owns a faceted **toggle-button filter stack** (Root single-select + multi-select Source / Family / 3rd / 5th / 7th, styled like the Content → Voicings → Definitions chips), issues the `voicingGrid` verb on any change (sending the enabled-token sets — all-on ⇒ everything), and renders the `voicingGridResult` reply as a **rows-by-quality × cols-by-shape** grid of **FretR** chord-boxes (one `ChordFlowFretboard` per cell, created with `controls.orientation:false`/`theme:false` etc. + `theme:"dark"` so only its title + id + copy header shows). Its three global display toggles — **orientation** (vertical/horizontal), **label mode** (intervals/notes) and **theme** (dark/light; the grid defaults dark to match the cell background) — fan out to every cell via the live FretR handles **without a re-fetch**; only Root/filter changes round-trip. A dumb view (C1): the facet chips are filter labels, the engine derives the facets and realizes the cells. An empty result renders an inline "no voicings match" message, never an error. Mounted as the **Voicings** top-level nav view in `index.html`, lazily created into `#voicings-mount` on first tab show (the same `views`/`onShow` pattern as Scales/CAGED).

**The Voicings Engine inspector (`voicings-engine.js`, `window.ChordFlowVoicingsEngine`)** — the introspectable operator dogfood page (voicings-engine thread). It fetches the operator catalog once via the **`voicingOperators`** verb (`{}` → `{operators:[{family, kind, displayName, params:[{name, kind, values?, default?, min?, max?}], eligibleShapesByQuality:[{quality, shapes}]}]}`) to drive schema-based controls (operator + quality + root + the operator's declared params), then issues the **`voicingDerive`** verb (`{family, quality, rootPitchClass, shape, minFret, maxFret}`) on any change and receives **`voicingDerivation`** (`{id, family, kind, toneSelection:[{interval, intervalLabel, function, note}], realizationSteps:[{kind, label}], diagram:FretboardDiagram}`): the abstract voicing + the ordered "show your work" derivation steps in the left column, the realized grip (a single `ChordFlowFretboard` chord-box) in the right. Both verbs are one `VoicingDeriveHandler` (Features/Voicings) over the `VoicingOperators` registry — the reified guitar Voicings Engine (`voicings-engine-rules-reference` §4); invalid input replies **`voicingDeriveError`** (UI-safe fail-loud). Mounted as the **Voicings Engine** top-level nav view, lazy `views`/`onShow` like the others.

**The chord-sheet render component (`chord-sheet-render-component.js`, `window.ChordFlowChordSheet`)** — ChordSheetR — draws a Core `ChordSheet` model as **one self-contained `<svg>`** (leadsheet **Layout A** or grid **Layout B**), the whole sheet — chord tokens, tone strips, compact fret diagrams, `%` similes — in a single node so the *same* SVG serves both the on-screen body and export (screen == export). It is a **dumb drawer** (C1): all notations/tones/diagrams are resolved in Core; JS notation/adornment/layout/theme toggles are pure re-renders (no round-trip, C3). It reuses FretR's **diagram model + function palette**, not FretR's DOM component (a deliberate reversal of the original "embed FretR" design, so the sheet stays one SVG under the no-external-libs CSP — see `chord-sheets-maker` design §Implementation-note). Its **HTML shell** `chord-sheets.js` (`window.ChordFlowChordSheets`, the **Chord Sheets** nav view) owns the controls + export and wraps the SVG. The bridge verbs: **`chordSheet`** `{harmonyEntity, harmonyId, keyPitchClass?, barsPerRow?, adornment, voicing?}` → **`chordSheetResult`** `{sheet}` / **`chordSheetError`** (built by `ChordSheetHandler` → `ChordSheetBuilder`; the handler resolves comping voicings only for the `diagram`/`both` adornments), and **`exportChordSheet`** → the host prints the print-styled light page via WebView2 `PrintToPdfAsync` (SVG/PNG export are client-side; no PDF library, C4).

---

## 6. Data flow (one exercise)

```
UI picks harmony (Song/Progression) + comping + optional lead + params (key/tempo/difficulty/feel)
  → generate envelope carrying those references + params (JS→C#)
  → GenerateExercise.Build → ExerciseRefs resolves the references from the stores → Exercise (definition)
  → ExerciseRendering: expand Song (KeyOverride ?? InitialKey) → RealizedSong, then AlphaTexRenderer.Render(RealizedSong, comping, …, lead?) → alphaTex string
  → loadScore envelope (C#→JS) — also carries a chord **schedule** (one ChordChange per chord change: 0-based bar/beat + a FretboardDiagram of the comped voicing), produced as a by-product of the render pass so it can't drift from the tab; ChordFlowNowNext drives the now/next fretboards off it
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
