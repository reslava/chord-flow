---
type: ctx
id: loom-ctx
title: Loom — Global Context
status: active
created: "2026-06-07T00:00:00.000Z"
updated: 2026-06-07
version: 3
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

- **Distribution:** desktop-first via **Photino.NET** (system WebView, no HTTP server, no localhost, no cloud). Web/PWA kept as an *additive* Phase-2 option (engine is UI-agnostic), not built.
- **Stack:** **C# engine + JS + alphaTab.** C#↔JS bridge is a narrow JSON-envelope string protocol; the real payload is just the **alphaTex string**.
- **Style:** **vertical slices over a shared Domain kernel** (no MediatR, no ceremonial layering).
  - `Domain/` — pure music kernel (Key, Chord, Progression, RhythmPattern, Voicing, Transposer, VoicingBook). No I/O. Unit-tested.
  - `Rendering/` — `AlphaTexRenderer`: the **only** alphaTex-aware code. Isolated seam for future MIDI/GuitarPro/MusicXML exporters.
  - `Features/` — GenerateExercise, PracticeSession, ExerciseLibrary, Progress.
  - `Infrastructure/` — SQLite (stores exercise *definitions*, regenerates alphaTex on load), Photino host, WebView bridge.
- **MVP scope:** 12-bar blues × 12 keys × {beat-1, beat-1+3, quarters} × beginner shell voicings + render/play with cursor + SQLite save. No accuracy detection in v1.

## 3. Reference docs (load when implementing the renderer or the WebView layer)

- **alphaTex syntax** — `loom/refs/alphatex-syntax-reference.md` (id `rf_01KTHJN829FMW964FTNCFSS2GM`). Verified metadata directives, notes (`fret.string`), stateful `:N` durations, `( )` chord groups, `r` rests, `\ts`/`\ks`/`\tempo`.
- **alphaTab JS API** — `loom/refs/alphatab-js-api-reference.md` (id `rf_01KTHJNV034RMM23TNY1RXF4SR`). Verified `player.enablePlayer`/`player.soundFont`, `api.tex`, `playPause`/`stop`, events (`playerReady`, `playerStateChanged`, `playedBeatChanged`, `soundFontLoaded`, …).
- Origin exploration: `loom/refs/chats/refs-chat-001.md`.

## 4. Rules

- All writes to `loom/**/*.md` go through MCP tools.
- Chat docs are the conversation surface — reply inside them under `## AI:`.
- After each step, state what was done and what is next, then STOP.
