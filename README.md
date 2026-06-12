# ChordFlow

**Rhythm & Progression Trainer for Guitar** — a local, desktop-first app that helps
guitarists practice **rhythm patterns over chord progressions**. The core is an
exercise-generation engine (progressions × keys × rhythms × voicings), rendered as
guitar tablature with **synchronized playback** via [alphaTab](https://www.alphatab.net/).

> **Status:** v0.4.0 — a music-theory-first domain kernel, **multi-chord-per-bar
> progressions** written in a simple text DSL, and a clean **Core / Desktop** split
> (a host-agnostic engine + the WinForms/WebView2 host). Builds on v0.2.0's SQLite
> persistence, saved-exercise library, practice tracking, and on-screen builder. This
> MVP is the starting point for a broader rhythm/lead chord-progression trainer.
> Windows-only for now.

## Features (v0.4.0)

- 12-bar blues, transposable to **all 12 keys** (computed movable shell voicing)
- **Chord progressions** in a compact, key-independent **[Progression DSL](loom/refs/chordflow-dsl-reference.md)** — multiple chords per bar, with rich chord qualities
- Rhythm patterns: beat 1 · beats 1 & 3 · quarters
- On-screen **builder**: key picker, rhythm picker, tempo, Generate
- Tablature rendering + audio playback with a **synchronized beat cursor** and
  active-note highlighting
- Play / stop / tempo transport
- **Save** exercise definitions to SQLite, reload them from a **saved-exercise list**
  (alphaTex is regenerated on load, never stored)
- **Mark practiced** — records a practice event (✅ marker + count in the list)

## Tech stack

- **C# / .NET 10** engine — a pure `Domain/` music kernel + `Rendering/AlphaTexRenderer`
  (the only alphaTex-aware code)
- **WinForms + WebView2** desktop host — serves `wwwroot` over an in-process
  `https://chordflow.local/` virtual host (no web server, no localhost port)
- **alphaTab** (JS build) for notation + playback; bundled Bravura music font and
  Sonivox GM soundfont
- Architecture: **vertical slices over a shared Domain kernel** (no MediatR)

## Requirements

- **Windows 10/11** with the **WebView2 Runtime** (preinstalled on Windows 11 and with
  current Microsoft Edge)
- **.NET 10 SDK** to build

## Build & run

```sh
dotnet build
dotnet run --project src/ChordFlow.Desktop
```

> The GM soundfont (`wwwroot/soundfont/sonivox.sf2`) is **fetched at build time** the
> first time you build (it is not committed — see `.gitignore`). The first build
> therefore needs network access; subsequent builds reuse the cached file.

## Tests

```sh
dotnet test
```

39 xUnit tests cover the `Domain` kernel and `AlphaTexRenderer`.

## Project layout

```
src/ChordFlow.Core/        host-agnostic engine (net10.0, zero UI refs)
  Domain/          pure music kernel (no I/O, unit-tested)
  Rendering/       AlphaTexRenderer (only alphaTex-aware code)
  Features/        GenerateExercise, PracticeSession, ExerciseLibrary, Progress
  Bridge/          C#↔JS envelope DTOs + inbound message router (host-agnostic)
  Persistence/     SQLite (EF Core) store + migrations
src/ChordFlow.Desktop/     WinForms + WebView2 host (net10.0-windows)
  Program.cs       host entry point + bridge wiring
  WebHost/         WebView2 transport bridge
  wwwroot/         index.html, app.js, alphaTab.min.js, font/, soundfont/
tests/ChordFlow.Core.Tests/   xUnit, targets ChordFlow.Core
```

Saved exercises live in a local SQLite file at `%LOCALAPPDATA%\ChordFlow\chordflow.db`
(no server, no network).

## Documentation

- **[DSL guide](loom/refs/chordflow-dsl-reference.md)** — the **Progression DSL** (key-independent, Nashville-style chords: bars, splits, qualities, durations) and the **Song DSL** (arrange progressions into a piece: definitions, repeats, modulation).
- **[Architecture overview](loom/refs/chordflow-architecture-reference.md)** — how the engine, renderer, bridge, and desktop host fit together.

## Third-party assets & licenses

- **alphaTab** — Mozilla Public License 2.0
- **Bravura** music font — SIL Open Font License 1.1
- **Sonivox** GM soundfont — Apache License 2.0

See `CHANGELOG.md` for release history.
