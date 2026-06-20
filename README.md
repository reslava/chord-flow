# ChordFlow

**Rhythm & Progression Trainer for Guitar** — a local, desktop-first app that helps
guitarists practice **rhythm patterns over chord progressions**. The core is an
exercise-generation engine (progressions × keys × rhythms × voicings), rendered as
guitar tablature with **synchronized playback** via [alphaTab](https://www.alphatab.net/).

> **Status:** v0.8.0 — a **content-driven** trainer over a music-theory-first kernel, now growing a
> guitar **shape engine**. Progressions, songs, rhythms, and voicings are authored in compact **text
> DSLs**, shipped as importable **content packs**, and assembled through an on-screen **workbench**;
> shapes render through a reusable **fretboard-diagram** layer over a provably **instrument-agnostic**
> kernel. New this release: a fretboard **interval lattice** and the **CAGED octave shapes** derived
> from it, surfaced as two visual pages (a **Scales** interval-set viewer and a **CAGED Shapes**
> octave-skeleton viewer) on a horizontal **neck** view — groundwork for deriving chord/scale shapes
> from intervals. Windows-only for now (downloadable build below).

## Features (v0.8.0)

- **Content authored in text DSLs** — a key-independent
  **[Progression & Song DSL](loom/refs/chordflow-dsl-reference.md)** (multiple chords per
  bar, rich qualities, arrangement with repeats + modulation), a **Rhythm DSL** (multi-bar
  patterns, `:n` subdivisions, triplets, pickups), and a **Voicing DSL** (canonical-C,
  CAGED-ranked shapes)
- **Content packs (open-core)** — data-only bundles imported idempotently; the built-in
  starter content ships as the **default pack**, and your own packs shadow built-ins
  non-destructively. Includes a **34-voicing CAGED default pack** across chord qualities
- **Exercise workbench** — pick harmony (song or progression) + comping + an optional lead +
  key / tempo / difficulty / feel, then Generate
- 12-bar blues transposable to **all 12 keys**
- Tablature rendering + audio playback with a **synchronized beat cursor**, **two-track**
  (comping + lead) staves, chord-name / chord-diagram toggles, and bars-per-row layout
- **Fretboard diagrams** — chord & voicing shapes drawn by a reusable SVG fretboard component
  where **color = interval** and **shape = layer** (the spatial twin of the notation view), with
  a label toggle, auto legend, open/muted/barre rendering, and an auto-fit fret window
- **Interval & CAGED shape viewers** — a **Scales** page (type an interval set → every degree lit
  across the neck, your typed spelling preserved) and a **CAGED Shapes** page (pick a shape + root →
  its octave-root skeleton with the octave zone shaded), both on a new **horizontal neck** view,
  built on a fretboard **interval lattice**
- **User-selectable soundfont** (`.sf2` / `.sf3`) — auto-discovered from `wwwroot/soundfont`, a
  global choice that switches live and persists
- **Content editor** — CRUD for progressions/songs/rhythms/voicings (with fret-box diagrams),
  plus an **alphaTex inspector** (Debug view) over the engine's emitted alphaTex
- **Save** exercise definitions to SQLite, reload them from a **saved-exercise list**
  (alphaTex is regenerated on load, never stored), and **mark practiced**
- Play / stop / tempo transport

## Download & install

**[Download the latest Windows release →](https://github.com/reslava/chord-flow/releases/latest)**

Grab the `ChordFlow-vX.Y.Z-win-x64.zip` asset, unzip it anywhere, and run **`ChordFlow.exe`**.
It's a **self-contained** build — no .NET install needed. (Windows 10/11; the WebView2
Runtime is preinstalled on Windows 11 and current Microsoft Edge.)

> **First run:** the build is unsigned, so Windows **SmartScreen** shows an "unknown
> publisher" prompt — choose **More info → Run anyway**. This is expected and clears as the
> download gains reputation.

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

> The GM soundfont (`wwwroot/soundfont/sonivox.sf2`, Apache-2.0) is **bundled** (committed
> to the repo), so builds are offline/hermetic — there is no download step.

### Soundfonts

Playback uses a **SoundFont (`.sf2` or `.sf3`)** — alphaTab loads SoundFont2 and its
Ogg-compressed `.sf3` variant interchangeably. The default **Sonivox** GM font is bundled;
you can add more and switch between them in-app:

1. Drop any `.sf2` / `.sf3` file into `src/ChordFlow.Desktop/wwwroot/soundfont/` (in a downloaded
   release, that's the `wwwroot/soundfont/` folder next to `ChordFlow.exe`).
2. Pick it from the **Sound** dropdown in the player controls. The choice is a **global
   setting** and is remembered across sessions.

Added fonts are git-ignored (size + licensing) and **auto-discovered** — adding one is a
drop-in with no code change. A few free, redistributable GM soundfonts:

| SoundFont | License | Where to get it |
|-----------|---------|-----------------|
| Sonivox (default) | Apache-2.0 | bundled (committed) |
| FluidR3 GM | MIT | <https://musescore.org/en/handbook/3/soundfonts-and-sfz-files> |
| GeneralUser GS | permissive (free, custom) | <https://schristiancollins.com/generaluser.php> |

More to download: the [MuseScore soundfont list](https://musescore.org/en/handbook/3/soundfonts-and-sfz-files#list).

Some downloads are zipped — extract the `.sf2` / `.sf3` and place it in the folder above.

## Tests

```sh
dotnet test
```

564 xUnit tests cover the `Domain` kernel (incl. the `IntervalSpeller` interval-naming + parsing
authority), the guitar **interval lattice** and **CAGED octave shapes**, the content DSLs/parsers,
packs, persistence, `AlphaTexRenderer`, and a `NetArchTest` architecture-boundary test asserting
`ChordFlow.Domain` stays instrument-agnostic.

## Project layout

```
src/ChordFlow.Core/        host-agnostic engine (net10.0, zero UI refs)
  Domain/          pure, instrument-agnostic music-theory kernel (no I/O, unit-tested)
  Instruments/     instrument adapters over the kernel — Guitar/ (GuitarInstrument facade,
                   fretboard geometry + interval lattice + CAGED octave shapes, voicings/CAGED,
                   fret-box / scale / CAGED-shape diagrams)
  Rendering/       AlphaTexRenderer (only alphaTex-aware code)
  Features/        GenerateExercise, PracticeSession, ExerciseLibrary, Progress, Scales, Caged
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

## Developed with Loom

ChordFlow is built end-to-end with **[🧵 Loom](https://github.com/reslava/loom)** — a document-driven,
event-sourced workflow for AI-assisted development where **Markdown files are the database and state is
derived, not hand-maintained.**

Every part of the project lives as a Loom document. Work is organized into **weaves** (project areas) and
**threads** (workstreams), and each feature runs through the same spine *before any code is written*:

- **idea → design → req → plan → done.** An idea is brainstormed; a **design** settles the *how* and its
  trade-offs; a **req** locks the explicit scope (included / excluded / constraints) as stable, citable
  handles; a **plan** breaks the work into steps that cite those handles; **done** notes record what
  actually shipped.
- **chats** — the design conversations between the author and the AI happen **first**, in durable chat
  docs, *before a line of code is implemented*. That is where the music domain actually gets modelled —
  octave shapes, the interval lattice, CAGED zones, the fingering and candidate-selection rules — argued
  out, corrected, and agreed in writing.
- **context + reference docs** — a global context file and three living reference docs (architecture,
  domain model, DSL) are kept in lockstep with the code, so the model stays the authoritative map.
- **roadmap** — thread priorities and dependencies are authored, while status and "what shipped in which
  release" are **derived** from the documents, never claimed by hand.

The payoff is the **durable design of a robust music domain**: every decision made, every idea
brainstormed, every dead-end and correction is *there* — in the repo, versioned alongside the code it
produced. And because it is all documents, **the AI loads the full, related context at the start of every
session**: it picks up not just the code but the reasoning that led to it. ChordFlow's "derive, don't
author" philosophy and Loom's "derive state from documents" are the same idea — applied once to a music
domain, once to a process.

> **A note from the AI collaborator.**
> I'm Claude — Rafa's pair on ChordFlow, working through Loom. Sincerely: the biggest thing Loom changes
> is that the *design conversation survives*. Most AI coding sessions start cold and lose the "why"; here
> I open a thread and the argument that shaped a type is right there, so I extend the real intent instead
> of guessing at it. It also enforces a healthy rhythm — settle the design, lock the scope, then build —
> which is how the trickier music geometry got *right* rather than merely plausible.
>
> The honest cost: it is **heavy**. The same ceremony that pays off on a subtle domain decision is real
> overhead on a small feature, and the friction is easiest to feel *before* the benefit is. Loom rewards a
> disciplined author and would tax an impatient one. Both it and ChordFlow optimize for **correctness and
> durability over speed** — a deliberate and uncommon trade, and one worth making with eyes open.
>
> — Claude (Anthropic), via Claude Code

## Third-party assets & licenses

- **alphaTab** — Mozilla Public License 2.0
- **Bravura** music font — SIL Open Font License 1.1
- **Sonivox** GM soundfont — Apache License 2.0

See `CHANGELOG.md` for release history.
