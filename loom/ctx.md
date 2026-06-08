---
type: ctx
id: loom-ctx
title: Loom — Global Context
status: active
created: "2026-06-07T00:00:00.000Z"
updated: 2026-06-08
version: 5
tags: [ctx, summary]
parent_id: null
requires_load: []
source_hash: 61c479f6d5a2f19917ec21349afc4694cf705f66
---
# Loom — Global Context

**Read at the start of every session.**

## 1. What this project is

**ChordFlow — Rhythm & Progression Trainer for Guitar.** A **local, desktop-first** app that helps guitarists practice **rhythm patterns over chord progressions**. The core is an **exercise-generation engine** (progressions × keys × rhythms × voicings × difficulty), not a tab viewer. Exercises are rendered as guitar tablature with **synchronized playback** (highlighted beat in time) via [alphaTab](https://www.alphatab.net/); the engine emits the **alphaTex** text DSL as its render format. Solo-dev MVP: simplest, most independent, ~$0 operating cost, C# where possible.

## 2. Architecture (baseline — see `loom/chordflow/mvp/mvp-design.md`)

- **Distribution:** desktop-first via **WinForms + the official `Microsoft.Web.WebView2` control** (migrated from Photino.NET, whose composition controller rendered a black window on the .NET 10 + WebView2-149 stack — see `loom/refs/photino-net-desktop-host-reference.md`). The WebView serves the local `wwwroot` over an in-process `https://chordflow.local/` virtual host — no HTTP server, no localhost port, no cloud. **Windows-only**; the engine stays UI-agnostic, so a cross-platform / web front-end remains an additive future option.
- **Stack:** **C# engine + JS + alphaTab.** C#↔JS bridge is a narrow JSON-envelope string protocol; the real payload is just the **alphaTex string**.
- **Style:** **vertical slices over a shared Domain kernel** (no MediatR, no ceremonial layering).
  - `Domain/` — pure, immutable **music-theory-first** kernel: harmony (PitchClass, interval-backed Quality, Chord, Scale + diatonic generation, NoteSpeller, Transposer), voicings (Voicing + strategy, VoicingBook, Fretboard), a **48-PPQ tick-grid rhythm model** (RhythmPattern/RhythmEvent/TimeSignature) with feel/accent/stroke overlays, and lead TargetZones. No I/O. Unit-tested. **Full map: `loom/refs/chordflow-domain-model-reference.md`.**
  - `Rendering/` — `AlphaTexRenderer` (the **only** alphaTex-aware code) + the `RhythmQuantizer` (tick grid → `:N` slots). Isolated seam for future MIDI/GuitarPro/MusicXML exporters.
  - `Features/` — GenerateExercise, PracticeSession, ExerciseLibrary, Progress.
  - `Infrastructure/` — SQLite (stores exercise *definitions*, regenerates alphaTex on load), WinForms + WebView2 host, WebView bridge.
- **MVP scope:** 12-bar blues × 12 keys × {beat-1, beat-1+3, quarters} × beginner shell voicings + render/play with cursor + SQLite save. No accuracy detection in v1.

## 3. Reference docs (load when designing/implementing features, the domain, the renderer, or the WebView layer)

- **ChordFlow domain model** — `loom/refs/chordflow-domain-model-reference.md` (id `rf_01KTM41K36DYJ0CE44FE7TMCGH`). Map of the music kernel: harmony, the 48-PPQ tick rhythm grid, voicings, feel/accent/stroke overlays, lead targets, the quantizer/render seam, and the `Exercise` pipeline. **Load when designing/implementing features or touching the domain.**
- **alphaTex syntax** — `loom/refs/alphatex-syntax-reference.md` (id `rf_01KTHJN829FMW964FTNCFSS2GM`). Verified metadata directives, notes (`fret.string`), stateful `:N` durations, `( )` chord groups, `r` rests, `\ts`/`\ks`/`\tempo`.
- **alphaTab JS API** — `loom/refs/alphatab-js-api-reference.md` (id `rf_01KTHJNV034RMM23TNY1RXF4SR`). Verified `player.enablePlayer`/`player.soundFont`, `api.tex`, `playPause`/`stop`, events (`playerReady`, `playerStateChanged`, `playedBeatChanged`, `soundFontLoaded`, …).
- **Desktop host (Photino → WinForms/WebView2)** — `loom/refs/photino-net-desktop-host-reference.md`. Why the host migrated and how the WebView2 virtual-host serving works.
- Origin exploration: `loom/refs/chats/refs-chat-001.md`.

## 4. Rules

- All writes to `loom/**/*.md` go through MCP tools.
- Chat docs are the conversation surface — reply inside them under `## AI:`.
- After each step, state what was done and what is next, then STOP.
