---
type: idea
id: id_01KTHJ61W7749XVY7AKGZV7F9D
title: ChordFlow MVP
status: done
created: "2026-06-07T00:00:00.000Z"
updated: 2026-06-08
version: 2
tags: []
parent_id: null
requires_load: []
---
# ChordFlow MVP

## 1. Concept

**ChordFlow** is a local desktop app that helps guitarists practice **rhythm patterns over chord progressions**. Its core is not a tab viewer but an **exercise-generation engine**: from a small rule set (progressions × keys × rhythm patterns × voicings × difficulty), it produces a large, varied library of practice exercises. Each exercise is rendered as guitar tablature with synchronized playback so the player sees the beat highlighted in time.

The product value is the **engine**; [alphaTab](https://www.alphatab.net/) is only the rendering + playback layer, fed via the **alphaTex** text DSL.

## 2. Target user

- Guitarists (beginner → intermediate) learning to keep time and internalize common progressions.
- Wants endless fresh exercises in any key, at controllable tempo, with a visual/audible metronome-like guide.

## 3. MVP scope (smallest thing that proves the idea)

1. **1 progression** — 12-bar blues — transposable to **all 12 keys**. (proves harmony engine)
2. **2–3 rhythm patterns** — beat-1-only, beat-1+3, quarter-notes. (proves rhythm engine)
3. **Beginner shell voicings** only — one voicing set. (proves voicing engine)
4. **Render + play with synced cursor** in the app window via alphaTab. (proves SDK integration)
5. **SQLite**: save an exercise, mark "practiced." (proves persistence) — no accuracy detection in v1.

This exercises every architectural seam, so later additions (more progressions, syncopation, difficulty auto-advance, audio-in accuracy) are **data + new slices, not re-architecture**.

## 4. Decided architecture (baseline)

- **Distribution:** **Desktop-first** (Photino.NET), engine kept UI-agnostic so a web/PWA front-end is an *additive* Phase-2 option, not a rewrite. Rationale: ~$0 operating cost, one-time-price monetization (Gumroad/itch/MS Store), cheapest path to validate demand.
- **Stack:** **C# engine + JavaScript + alphaTab**, hosted in a **Photino** window using the system WebView. C#↔JS bridge is deliberately narrow — C# emits an **alphaTex string**, JS calls `api.tex(...)` and drives playback. No HTTP server, no localhost port, no cloud.
- **Architecture style:** **Vertical slices over a shared Domain kernel.**
  - `Domain/` — pure music kernel: Key, Chord, Progression, RhythmPattern, Voicing, transposition. No I/O. Fully unit-tested.
  - `Rendering/` — `AlphaTexRenderer` (the only code that knows alphaTex syntax). Isolated seam for future MIDI / Guitar Pro / MusicXML exporters.
  - `Features/` — `GenerateExercise`, `PracticeSession`, `ExerciseLibrary`, `Progress` — each composes Domain + Rendering top-to-bottom.
  - `Infrastructure/` — SQLite, Photino host, WebView bridge.
- **No MediatR / no ceremonial layering** — single-process desktop app; a slice is a class with a method.
- **Persistence:** SQLite (offline), via EF Core or Dapper.
- **Audio:** alphaTab's built-in soundfont synthesis; ship a small GM soundfont.

## 5. Explicitly out of scope for MVP

- Audio-input accuracy detection / scoring.
- Multiple progressions beyond 12-bar blues.
- Intermediate/advanced voicing sets.
- Web/PWA distribution (kept architecturally open, not built).
- Exporters (MIDI/Guitar Pro/MusicXML) — the renderer seam exists, but only alphaTex is implemented.
- Accounts, sync, cloud anything.

## 6. Open questions / risks

- **WebView dependency:** WebView2 present on Win11; if cross-platform later, Photino uses WKWebView / WebKitGTK — needs render testing before promising macOS/Linux.
- **alphaTab JS vs .NET package:** JS build chosen (better supported, free cursor+playback). Confirm during design.
- **Soundfont licensing/size:** pick a small, redistributable GM soundfont.
- **alphaTex coverage:** confirm alphaTex can express the rhythm/voicing notation we need (rests, chord stacks, beat durations, shuffle feel) — verify against the alphaTex docs in the design doc.

## 7. Origin

Distilled from the exploration in `loom/refs/chats/refs-chat-001.md`.
