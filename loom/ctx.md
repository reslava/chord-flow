---
type: ctx
id: loom-ctx
title: Loom — Global Context
status: active
created: 2026-06-07
updated: 2026-06-21
version: 10
tags: [ctx, summary]
parent_id: null
requires_load: []
source_hash: 61c479f6d5a2f19917ec21349afc4694cf705f66
---
# Loom — Global Context

**Read at the start of every session.**

## 1. What this project is

**ChordFlow — Rhythm & Progression Trainer for Guitar.** A **local, desktop-first** app that helps guitarists practice **rhythm patterns over chord progressions**. The core is an **exercise-generation engine** (progressions × keys × rhythms × voicings × difficulty), not a tab viewer. Exercises are rendered as guitar tablature with **synchronized playback** (highlighted beat in time) via [alphaTab](https://www.alphatab.net/); the engine emits the **alphaTex** text DSL as its render format. Solo-dev MVP: simplest, most independent, ~$0 operating cost, C# where possible.

## 2. Architecture (baseline — see `loom/refs/chordflow-architecture-reference.md` + `loom/chordflow/mvp/mvp-design.md`)

- **Distribution:** desktop-first via **WinForms + the official `Microsoft.Web.WebView2` control** (migrated from Photino.NET, whose composition controller rendered a black window on the .NET 10 + WebView2-149 stack — see `loom/refs/photino-net-desktop-host-reference.md`). The WebView serves the local `wwwroot` over an in-process `https://chordflow.local/` virtual host — no HTTP server, no localhost port, no cloud. **Windows-only today, but multi-platform-ready by construction:** the engine lives in **`ChordFlow.Core`** (`net10.0`, **zero UI/host references** — a compile-time guarantee), and the WinForms+WebView2 host lives in a separate **`ChordFlow.Desktop`** project (`net10.0-windows`); dependency direction is strictly **Desktop → Core**. `wwwroot` is host-neutral with the C#↔JS bridge isolated in one small JS module. A web/cross-platform front-end is therefore an *additive* project (serve the same `wwwroot` + one JSON endpoint wrapping Core), not a rewrite. (Split delivered in the `core-host-split` thread.)
- **Stack:** **C# engine + JS + alphaTab.** C#↔JS bridge is a narrow JSON-envelope string protocol; the real payload is just the **alphaTex string**.
- **Style:** **vertical slices over a shared `Music` theory kernel** (no MediatR, no ceremonial layering). Inside `ChordFlow.Core`:
  - `Music/` — pure, immutable **music-theory-first** kernel, split into concept-named **flat-sibling** namespaces under `ChordFlow.Music`: **Harmony** (PitchClass, interval-backed Quality, Chord, Scale + diatonic generation, NoteSpeller/IntervalSpeller), **Rhythm** (the **48-PPQ tick-grid model** — RhythmPattern/RhythmEvent/TimeSignature — with feel/accent/stroke overlays), **Progressions** (harmonic bars/spans + the Nashville-style `ProgressionParser`, and `Transposer` progression-realization), **Songs** (Song/SongParser/SongExpander + the `IProgressionStore` port), and **Melody** (lead `TargetZone`s as pitch classes). The dependency graph is an acyclic DAG with **Harmony/Rhythm as sinks** (NetArchTest-enforced). No I/O. Unit-tested. **Full map: `loom/refs/chordflow-domain-model-reference.md`.** The composed practice unit (`Exercise`, `Difficulty`) lives outside the theory kernel in `ChordFlow.Exercises/`.
  - `Rendering/` — `AlphaTexRenderer` (the **only** alphaTex-aware code) + the `RhythmQuantizer` (tick grid → `:N` slots). Isolated seam for future MIDI/GuitarPro/MusicXML exporters.
  - `Features/` — GenerateExercise, PracticeSession, ExerciseLibrary, Progress.
  - `Bridge/` — C#↔JS envelope DTOs + `IBridge` + the host-agnostic `WebMessageRouter`. The bridge *contract*; the transport is the host's.
  - `Persistence/` — SQLite via EF Core (stores exercise *definitions*, regenerates alphaTex on load) + migrations. **Seed/library content loads from importable definition bundles — never hardcoded** — so curated content packs (free starter set or future paid packs) stay an *additive data drop*, not a code change.
  - The **`ChordFlow.Desktop`** host owns: the WinForms shell, the `WebView2Bridge` transport, the virtual-host wiring, and `wwwroot`.
- **MVP scope:** 12-bar blues × 12 keys × {beat-1, beat-1+3, quarters} × beginner shell voicings + render/play with cursor + SQLite save. No accuracy detection in v1.

## 3. Reference docs (load when designing/implementing features, the domain, the renderer, or the WebView layer)

> **Always-load / always-update (see the contract's "Reference-doc sync (required)").** Before reasoning about a **core DSL**, the **domain/kernel**, or the **app architecture**, LOAD the matching ref first — it is the authoritative map: DSL → `chordflow-dsl-reference` · domain/renderer → `chordflow-domain-model-reference` · architecture/boundaries → `chordflow-architecture-reference`. And whenever you change one of those areas, UPDATE its ref in the same unit of work.

- **ChordFlow architecture** — `loom/refs/chordflow-architecture-reference.md` (id `rf_01KTSAPAT132QTEY5BEPRKS3MB`). The *system* view: the Core/Desktop split, one-way dependency direction, the layers inside Core, the C#↔JS bridge contract, data flow, and the seams that keep a future web host additive. **Load when reasoning about project boundaries or where new code belongs.**
- **ChordFlow domain model** — `loom/refs/chordflow-domain-model-reference.md` (id `rf_01KTM41K36DYJ0CE44FE7TMCGH`). The *music-theory* view of the kernel: harmony, the 48-PPQ tick rhythm grid, voicings, feel/accent/stroke overlays, lead targets, the quantizer/render seam, and the `Exercise` pipeline. **Load when designing/implementing features or touching the domain.**
- **ChordFlow DSL (end-user)** — `loom/refs/chordflow-dsl-reference.md` (id `rf_01KTSAQ6990GY3J4CZ7HPVPW6K`). The Progression DSL: Nashville scale-degree notation, bars (space) / chords (`_`), quality suffixes, even-split vs explicit `:slots` durations. Public-facing — linked from the README.
- **alphaTex syntax** — `loom/refs/alphatex-syntax-reference.md` (id `rf_01KTHJN829FMW964FTNCFSS2GM`). Verified metadata directives, notes (`fret.string`), stateful `:N` durations, `( )` chord groups, `r` rests, `\ts`/`\ks`/`\tempo`.
- **alphaTab JS API** — `loom/refs/alphatab-js-api-reference.md` (id `rf_01KTHJNV034RMM23TNY1RXF4SR`). Verified `player.enablePlayer`/`player.soundFont`, `api.tex`, `playPause`/`stop`, events (`playerReady`, `playerStateChanged`, `playedBeatChanged`, `soundFontLoaded`, …).
- **Desktop host (Photino → WinForms/WebView2)** — `loom/refs/photino-net-desktop-host-reference.md`. Why the host migrated and how the WebView2 virtual-host serving works.
- Origin exploration: `loom/refs/chats/refs-chat-001.md`.

## 4. Rules

- All writes to `loom/**/*.md` go through MCP tools.
- Chat docs are the conversation surface — reply inside them under `## AI:`.
- After each step, state what was done and what is next, then STOP.
- **Guitar-weave dogfood rule:** every new guitar feature ships with a fretboard UI page that visualizes it (built on the `fretboard-render-component`) — fast visual confirmation before building the next layer on top. Add a "dogfood: render on the fretboard UI page" line to each guitar idea's Validation section.
