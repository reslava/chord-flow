# ChordFlow

**Rhythm & Progression Trainer for Guitar** — a local, desktop-first app that helps
guitarists practice **rhythm patterns over chord progressions**. The core is an
exercise-generation engine (progressions × keys × rhythms × voicings), rendered as
guitar tablature with **synchronized playback** via [alphaTab](https://www.alphatab.net/).

> **Status:** v0.2.0 — adds SQLite persistence, a saved-exercise library, practice
> tracking, and the on-screen builder controls. This MVP is the starting point for a
> broader rhythm/lead chord-progression trainer. Windows-only for now.

## Features (v0.2.0)

- 12-bar blues, transposable to **all 12 keys** (computed movable shell voicing)
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
dotnet run --project src/ChordFlow.App
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
src/ChordFlow.App/
  Domain/          pure music kernel (no I/O, unit-tested)
  Rendering/       AlphaTexRenderer (only alphaTex-aware code)
  Features/        GenerateExercise, PracticeSession, ExerciseLibrary, Progress
  Infrastructure/  WebView2 host bridge, message router, SQLite (EF Core) store
  Migrations/      EF Core migrations (SQLite schema)
  wwwroot/         index.html, app.js, alphaTab.min.js, font/, soundfont/
tests/ChordFlow.Tests/   xUnit (Domain + Rendering)
```

Saved exercises live in a local SQLite file at `%LOCALAPPDATA%\ChordFlow\chordflow.db`
(no server, no network).

## Third-party assets & licenses

- **alphaTab** — Mozilla Public License 2.0
- **Bravura** music font — SIL Open Font License 1.1
- **Sonivox** GM soundfont — Apache License 2.0

See `CHANGELOG.md` for release history.
