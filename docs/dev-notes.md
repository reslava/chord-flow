<img src="../images/icon.png" width="96" align="right" alt="ChordFlow app icon">

# ChordFlow — Developer Notes

Build, test, and project-structure notes for working on ChordFlow itself. If you just
want to *use* the app, see the **[User Guide](user-guide.md)**; to *author content*, see
the **[DSL Guide](dsl-guide.md)**.

The design rationale for the architecture below — the Core/Desktop split, the one-way
dependency direction, the bridge contract, and the seams that keep a future web host
additive — lives in the collaborator reference docs under
[`loom/refs/`](../loom/refs/chordflow-architecture-reference.md).

---

## Tech stack

- **C# / .NET 10** engine — a pure `Music/` music-theory kernel + `Rendering/AlphaTexRenderer`
  (the only alphaTex-aware code).
- **WinForms + WebView2** desktop host — serves `wwwroot` over an in-process
  `https://chordflow.local/` virtual host (no web server, no localhost port).
- **alphaTab** (JS build) for notation + playback; bundled Bravura music font and Sonivox
  GM soundfont.
- Architecture: **vertical slices over a shared `Music` theory kernel** (no MediatR).

## Requirements

- **.NET 10 SDK** to build.
- **Windows 10/11** with the **WebView2 Runtime** to run (preinstalled on Windows 11 and
  with current Microsoft Edge).

## Build & run

```sh
dotnet build
dotnet run --project src/ChordFlow.Desktop
```

> The GM soundfont (`wwwroot/soundfont/sonivox.sf2`, Apache-2.0) is **bundled** (committed
> to the repo), so builds are offline/hermetic — there is no download step.

## Tests

```sh
dotnet test
```

The xUnit suite (1058 tests) covers the `Music` kernel (incl. the `IntervalSpeller` interval-naming +
parsing authority and `QualityFormulas`), the guitar **interval lattice**, **CAGED octave
shapes**, the **CAGED chord-derivation engine** (an introspectable **operator library** with
derivation traces) + **voicing families** (shell / doubled-shell, against a golden oracle),
the **CompingResolver** + multi-source content model + **per-chord DSL voicings**, the
**chord-sheet builder** (Song → sheet projection) + `chordSheet` handler, the content
DSLs/parsers (incl. the dotted/tie Rhythm DSL, chromatic progression degrees, and the Song
`tempo`/`feel`/`capo` directives), the **default-pack content** (every seed song parses,
expands, and renders), packs, persistence, `AlphaTexRenderer`, and `NetArchTest`
architecture-boundary tests asserting `ChordFlow.Music` stays instrument-agnostic and the
`Music.*` sub-namespaces stay an acyclic DAG.

## Project layout

```
src/ChordFlow.Core/        host-agnostic engine (net10.0, zero UI refs)
  Music/           pure, instrument-agnostic music-theory kernel (no I/O, unit-tested) —
                   Harmony/ Rhythm/ Melody/ Progressions/ Songs/ (flat-sibling DAG)
  Exercises/       the composed practice unit (Exercise + Difficulty)
  Instruments/     instrument adapters over the kernel — Guitar/ (GuitarInstrument facade,
                   fretboard geometry + interval lattice + CAGED octave shapes, voicings/CAGED,
                   fret-box / scale / CAGED-shape diagrams)
  Rendering/       the presentation/export seam — AlphaTexRenderer (alphaTex) +
                   ChordSheets/ (the instrument-agnostic chord-sheet model)
  Features/        GenerateExercise, PracticeSession, ExerciseLibrary, Progress, Scales, Caged,
                   Voicings (CompingResolver + EngineVoicingSource), ChordSheets (ChordSheetBuilder),
                   ContentCrud, Packs
  Bridge/          C#↔JS envelope DTOs + inbound message router (host-agnostic)
  Persistence/     SQLite (EF Core) store + migrations
  Content/         the default content pack (progressions/ songs/ rhythms/ voicings/)
src/ChordFlow.Desktop/     WinForms + WebView2 host (net10.0-windows)
  Program.cs       host entry point + bridge wiring
  WebHost/         WebView2 transport bridge
  wwwroot/         index.html, app.js, alphaTab.min.js, font/, soundfont/
tests/ChordFlow.Core.Tests/   xUnit, targets ChordFlow.Core
```

Saved exercises live in a local SQLite file at `%LOCALAPPDATA%\ChordFlow\chordflow.db`
(no server, no network).

## Content authoring

The starter content ships as the **default pack** under `src/ChordFlow.Core/Content/default-pack/`
— plain `.dsl` files in `progressions/`, `songs/`, `rhythms/`, and `voicings/`, imported at
startup. Songs are **auto-discovered** from the folder (the filename stem is the id); no manifest
edit is needed to add one. The full grammar for each file is the **[DSL Guide](dsl-guide.md)**.
