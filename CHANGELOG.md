# Changelog

All notable changes to ChordFlow are documented here. The format is based on
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this project
adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.7.0] — 2026-06-19

A foundation release that hardens the **theory ↔ instrument boundary**: the music kernel is
now provably instrument-agnostic, guitar specifics sit behind a `GuitarInstrument` facade, and
interval spelling is centralized in a single authority — groundwork for the interval-derived
shape engine. Plus `.sf3` soundfont support.

### Added
- **`.sf3` soundfont discovery** — the soundfont picker now lists `.sf3` files alongside `.sf2`
  (alphaTab loads the Ogg-compressed `.sf3` variant interchangeably). Drop either format into
  `wwwroot/soundfont/` and it is auto-discovered. The README documents the dual-format support
  and links the MuseScore soundfont list.

### Changed
- **Theory ↔ instrument boundary split.** Guitar-specific types (fretboard geometry,
  `Voicing` / `VoicingBook` / CAGED / strategy realization, the fretboard & voicing diagrams)
  moved out of `Domain/` into `Instruments/Guitar/` (namespace `ChordFlow.Instruments.Guitar`),
  leaving `Domain/` a pure, instrument-agnostic theory kernel. A new **`GuitarInstrument`**
  facade (`Realize` / `Diagram` / `ResolveLead`) is the public surface; `LeadTargets` is trimmed
  to pitch-class output (fret resolution moves to `GuitarInstrument.ResolveLead`). The boundary
  is enforced by a `NetArchTest.Rules` architecture test — `ChordFlow.Domain` must not depend on
  `ChordFlow.Instruments`.
- **`IntervalSpeller` — one interval-spelling authority.** A new `Domain/IntervalSpeller` (the
  interval peer of `NoteSpeller`) centralizes interval naming: `Name(semitone)` is the computed,
  unfolded flats **substrate vocabulary** (the 2nd octave yields `9/10/11/13…` for free);
  `Label(semitone, role)` is the **role-keyed** chord-context spelling with conventional tensions
  (`#9/#11/b13`). `VoicingDiagram` now delegates to it (its inline label logic removed) — diagram
  labels are byte-for-byte unchanged.

### Tests
- Full `ChordFlow.Core` xUnit suite green (454), including the new architecture-boundary test.

## [0.6.0] — 2026-06-18

A reusable **fretboard diagram** rendering layer. Chord/scale shapes are now drawn by a
single dumb SVG component over a Core-computed marker model — the spatial twin of the
shared `ChordFlowScore` notation component — replacing the old one-off chord-diagram view
and laying the groundwork for the interval-derived shape engine.

### Added
- **`ChordFlowFretboard` SVG component** (`fretboard-render-component.js`) — a dumb SVG view
  over a Core-computed `FretboardDiagram` marker model: a flat, many-per-string marker list
  where **color = interval** (default function palette or a per-interval override), **shape =
  layer**, with an owned label toggle + auto legend, open/muted/barre rendering, and an
  auto-fit fret window. The spatial counterpart to `ChordFlowScore`.
- **Core `FretboardDiagram` marker model** — new `Domain/Diagrams/FretboardDiagram`
  (`FretboardDiagram` / `FretboardMarker` / `MarkerShape`); `Function` is a string color-key
  (`root`…`tension`).
- **`fretboard-sandbox.html`** — a hand-fed harness for the new component.

### Changed
- **`VoicingDiagram.Build` recast onto `FretboardDiagram`** as its first producer; the old
  `DiagramModel` / `DiagramString` parallel path is removed and
  `EntityPreviewEnvelope.Diagram` retyped. The Content/Voicings preview is retrofitted onto
  the new component and the old `chord-diagram.js` is deleted (no drifting second path).

### Tests
- Full `ChordFlow.Core` xUnit suite green (399/399).

## [0.5.0] — 2026-06-17

ChordFlow grows from a hardcoded 12-bar-blues demo into a **content-driven trainer**:
progressions, songs, rhythms, and voicings are all authored in compact text DSLs,
distributed as importable **content packs**, and assembled into exercises through an
on-screen **workbench** — over a rebuilt rendering/UI layer. Plus a one-command,
tag-driven **release pipeline** that ships a downloadable Windows build.

### Added
- **Song arrangement layer** — a pure arrangement of progressions (repetition, modulation,
  section order) via `SongExpander`, a line-oriented **Song DSL**, and `Render(RealizedSong)`.
  Harmony stays in the progression; the song layer slots in above `Transposer`.
- **Rhythm DSL** — multi-bar rhythm patterns in an `X/./-` glyph DSL with `:n` subdivisions,
  pickup measures, and triplet rendering (`{tu N}`), plus rhythm-pattern persistence and
  DSL-derived (single-source-of-truth) seed patterns.
- **Authored voicing content pillar** — canonical-C, inherently-movable voicings in a
  **Voicing DSL** with CAGED-shape ranking, a stored-first `VoicingBook`, realize/transpose,
  and persistence; a curated **default pack of 34 pitch-verified CAGED voicings**
  (maj/min/dom7/maj7/m7 × full CAGED + m7b5/dim7/aug grips). New `Quality.Diminished7`.
- **Content packs (open-core)** — data-only bundles (`manifest.json` + per-kind `.dsl`
  folders) imported idempotently by a composite `(Id, Origin)` key with non-destructive
  shadowing (UserDefined > Pack > BuiltIn); the built-in starter content now ships as the
  default pack. Catalog metadata (genre/subgenre/tags) + `Origin` provenance.
- **Exercise workbench** — generate over the canonical `Exercise` via content references:
  harmony (song/progression) + comping + optional lead + params (key/tempo/difficulty/feel),
  resolved through a shared `ExerciseRefs` seam. Harmony/comping/lead pickers, difficulty/feel
  controls, and per-track volume sliders.
- **Shared score render component + content-CRUD surface** — one `ChordFlowScore` JS
  component owns the alphaTex → alphaTab render/transport (replacing two drifted instances);
  a generic DSL-entity CRUD editor (`entity*` bridge family) with voicing fret-box diagrams;
  a Practice ⇄ Content view toggle.
- **Chord-diagram display toggles** — independent chord names, diagrams-over-staff, and
  diagrams-on-top.
- **alphaTex inspector (Debug view)** — show/edit the engine's emitted alphaTex and
  render/play it through its own player.
- **User-selectable soundfont library** — pick the playback soundfont (auto-discovered from
  `wwwroot/soundfont`), a global persisted choice that switches the synth font live; backed
  by a new Core `AppSettings` key/value store.
- **Release pipeline** — a tag-driven GitHub Actions release (`guard → build-test → release`)
  that publishes a self-contained, single-file **`ChordFlow.exe` + `wwwroot`** zip and cuts a
  GitHub release with the changelog as notes; driven by a `/do-release` command and
  [`RELEASING.md`](RELEASING.md).

### Changed
- **One canonical `Exercise`** — merged `Exercise`/`SongExercise` into a single
  `Exercise(Song, Comping, Lead?, KeyOverride?, …)`; a bare progression is lifted via
  `Song.OfProgression` so everything rides one Song → render path. The renderer stays pure
  (Song expansion moved to a `Features` I/O seam). An optional lead renders as a second track
  of dead notes.
- **Shipped executable renamed to `ChordFlow.exe`** (`<AssemblyName>`); the default Sonivox
  GM soundfont is now **committed/bundled** instead of fetched at build time, so builds are
  hermetic.
- Two-track exercises render both staves; bars-per-row layout is controllable (4/row default
  + an Auto-layout toggle).

### Fixed
- The last partial system now stretches to full width in fixed 4-bar layout
  (`justifyLastSystem`); previously only natural in Auto layout.
- A render failure path and the saved-exercise load path (previously hard-wired to the seed
  blues) now resolve through the shared reference seam like the generate path.

### Tests
- Full `ChordFlow.Core` xUnit suite green (verified in CI on every tagged release).

## [0.4.0] — 2026-06-10

Harmonic rhythm + a clean engine/host split. Progressions gain multiple chords per
bar via a key-independent text DSL, and the single project is split into a
host-agnostic engine and a thin desktop host so the "engine stays UI-agnostic" rule
is enforced by the compiler.

### Added
- **Harmonic-rhythm layer — multi-chord-per-bar progressions.** `HarmonicBar` /
  `ChordSpan` let a single bar hold several chords, each with its own tick duration.
- **Progression DSL** — a Nashville-style, key-independent notation parsed by
  `ProgressionParser`: bars separated by spaces, chords within a bar by `_`, scale
  degrees `1`–`7` with quality suffixes (`-`/`m`, `7`, `-7`/`m7`, `maj7`/`^7`,
  `°`/`dim`, `ø`/`m7b5`, `+`/`aug`), and per-chord durations via even split or
  explicit `:slots`. End-user guide:
  [`chordflow-dsl-reference.md`](loom/refs/chordflow-dsl-reference.md).
- **Documentation** — a public [Progression DSL guide](loom/refs/chordflow-dsl-reference.md)
  (linked from the README) and an [architecture overview](loom/refs/chordflow-architecture-reference.md),
  both as `loom/refs/` reference docs.

### Changed
- **Project split — `ChordFlow.App` → `ChordFlow.Core` + `ChordFlow.Desktop`.** The
  host-agnostic engine (Domain, Rendering, Features, the `Bridge/` contract, and
  `Persistence/`) moved to `ChordFlow.Core` (`net10.0`, **zero UI references**); the
  WinForms + WebView2 host moved to `ChordFlow.Desktop` (`net10.0-windows`).
  Dependency direction is strictly Desktop → Core, so the UI-agnostic-engine rule is
  now a compile-time guarantee and a future web host is additive rather than a
  rewrite. The former `Infrastructure/` split into `Core/Bridge/` (the envelope
  contract + the host-agnostic `WebMessageRouter`), `Core/Persistence/` (SQLite + EF
  migrations), and `Desktop/WebHost/` (the `WebView2Bridge` transport). Pure
  structural refactor — no behavior change.
- Test project renamed `ChordFlow.Tests` → `ChordFlow.Core.Tests` and retargeted to
  plain `net10.0` (no longer Windows-bound).

### Tests
- 163 xUnit tests (was 106); all green.

## [0.3.0] — 2026-06-08

Phase 4 — music-theory-first domain. A focused rewrite of the `Domain/` kernel so
transposition, diatonic generation, voicings, rhythm, swing/shuffle, and lead
targets are all *derived*, never hand-authored. The sequential `Beat` rhythm model
is replaced by a positional 48-PPQ tick grid. No UI or persistence-schema changes.

### Added
- **Harmony** — `Quality` backed by interval sets (8 v1 qualities) via
  `QualityIntervals`; chord-relative `ChordTone`/`ChordTones` (the b7-of-G7 bridge);
  first-class `Scale` + `DiatonicChord.Build` (I maj7 … vii m7b5); `NoteSpeller`
  (per-key spelling, promoted out of the renderer); `ScaleDegree` distinct from
  `RomanDegree` (two degree frames).
- **Voicings** — optional diagram metadata on `Voicing` (`BarreFret`/`FirstFret`/
  muted strings); `IVoicingStrategy` + `BeginnerShellStrategy`; `VoicingBook` is now
  a strategy dispatcher; `Fretboard` geometry.
- **Rhythm (tick grid)** — `TickGrid` (PPQ 48), `TimeSignature`, `RhythmEvent`,
  `Stroke`/`Accent`, `PickupMeasure`, and a tick-based `RhythmPattern`. A
  `RhythmQuantizer` in the `Rendering/` seam compiles the grid to `:N` slots.
- **Overlays** — `Feel` + `FeelTransform` (playback-time long-short warp),
  `AccentPattern` (backbeat), `StrokeOverlay` — composable, never stored on a pattern.
- **Lead training** — `TargetZone`/`Importance`/`LeadTargets`; guide tones (3 & 7)
  derived from the interval sets and resolved to fretboard positions (domain only).
- **Reference doc** — `loom/refs/chordflow-domain-model-reference.md` mapping the kernel;
  linked from `loom/ctx.md`.

### Changed
- **Rhythm model migrated** from sequential `Beat(Duration, IsHit)` to the positional
  tick grid; `Beat`/`Duration` removed and the renderer's inline duration logic replaced
  by the quantizer. Existing alphaTex output is byte-identical for the MVP patterns.
- `Transposer` now consumes a `Scale`; the renderer derives spelling from `NoteSpeller`
  and the `\ts` header from the pattern's `TimeSignature`.
- `Exercise` gained a `Feel` field (defaults to Straight; applied at render time, not stored).

### Tests
- 106 xUnit tests (was 39); all green. Includes a Bb 12-bar-blues end-to-end render
  smoke check through the new path.

## [0.2.0] — 2026-06-08

Phase 3 — persistence & UI. ChordFlow becomes a usable trainer: build an exercise
on screen, save it, reload it, and track practice. The voicing engine now covers
all 12 keys.

### Added
- **SQLite persistence (EF Core)** — `ChordFlowDbContext` with `Exercises`
  (definition fields only — never alphaTex) and `PracticeRecords`; initial
  migration, applied on startup. Local file at `%LOCALAPPDATA%\ChordFlow\chordflow.db`
  (no server, no localhost port).
- **`ExerciseLibrary` slice** — save an exercise definition, list saved exercises,
  reload one (alphaTex **regenerated** from the definition on load, never stored).
- **`Progress` slice** — "mark practiced" records a practice event (no accuracy /
  scoring); an unsaved exercise is saved first so the record always has a target.
- **Builder UI** — key picker (12 keys), rhythm picker, tempo, Generate, Save,
  Mark-practiced, and a clickable saved-exercise list with a ✅ practiced marker.
  Each control posts a bridge envelope to its slice.
- **Bridge vocabulary** — added `generate` / `save` / `listExercises` /
  `loadExercise` / `markPracticed` (in) and `exerciseList` / `practiceRecorded` /
  `status` (out).

### Changed
- **`VoicingBook` generalized to a computed movable dom7 shell shape** covering all
  12 keys (`(s5:R, s4:R-1, s3:R)`, `R` in 1..12). Previously a 3-row hand-authored
  table that only rendered the Bb blues, so the key picker silently failed off Bb.
  Reproduces the original Bb7/Eb7/F7 frets exactly. (Design §2/§6 amended.)
- Transport (play/stop/tempo) now routes through the `PracticeSession` slice rather
  than driving alphaTab directly from JS.

### Fixed
- A render failure no longer silently drops the bridge message (which looked like a
  control "doing nothing") — the host now surfaces a `status` error, and an exercise
  that doesn't render can't be saved.

### Tests
- 39 xUnit tests (was 26); the `VoicingBook` suite now covers all 12 roots.

## [0.1.0] — 2026-06-08

First tagged release: the engine generates a 12-bar blues exercise, renders it
as tablature, and plays it back with a synchronized beat cursor.

### Added
- **Music engine (`Domain/`)** — pure, immutable kernel (Key, Chord, Progression,
  RhythmPattern, Voicing) with a `Transposer` and a `VoicingBook`. 12-bar blues
  transposable to all 12 keys; rhythm patterns beat-1, beats-1&3, quarters;
  beginner shell voicings.
- **`AlphaTexRenderer`** — turns an `Exercise` into an alphaTex string; the sole
  alphaTex-aware component (renderer seam for future MIDI/GuitarPro/MusicXML).
- **Desktop host** — WinForms + WebView2, serving the local `wwwroot` over an
  in-process `https://chordflow.local/` virtual host (no web server, no localhost
  port). Renders tablature via [alphaTab](https://www.alphatab.net/) and plays it
  with a bundled GM soundfont.
- **C#↔JS bridge** — narrow JSON-envelope protocol over `chrome.webview`
  (`loadScore`/`play`/`stop`/`setTempo` out; `ready`/`playbackFinished`/
  `beatChanged` in); payload is the alphaTex string.
- **Playback** — play / stop / tempo transport with a synchronized beat cursor,
  current-bar highlight, and active-note highlighting.
- **Tests** — 26 xUnit tests over the Domain kernel and `AlphaTexRenderer`.

### Changed
- **Desktop host migrated from Photino.NET to WinForms + the official
  `Microsoft.Web.WebView2` control.** Photino's WebView2 *composition* controller
  renders a black window on the .NET 10 + WebView2-149 stack; the WinForms
  *windowed* controller renders correctly. Only the host (`Infrastructure/`) and
  the `app.js` transport shim changed — the engine, renderer, feature slices, and
  the bridge envelope contract were untouched. Rationale:
  `loom/refs/photino-net-desktop-host-reference.md`.

### Known limitations
- **Windows-only** (WinForms host). The engine stays UI-agnostic, so a
  cross-platform / web front-end remains an additive future option.
- No persistence or on-screen pickers yet (SQLite save + key/rhythm/tempo UI are
  the next phase).
- No audio-input accuracy detection (out of scope for v1).

[0.7.0]: https://github.com/reslava/chord-flow/releases/tag/v0.7.0
[0.6.0]: https://github.com/reslava/chord-flow/releases/tag/v0.6.0
[0.5.0]: https://github.com/reslava/chord-flow/releases/tag/v0.5.0
[0.4.0]: https://github.com/reslava/chord-flow/releases/tag/v0.4.0
[0.3.0]: https://github.com/reslava/chord-flow/releases/tag/v0.3.0
[0.2.0]: https://github.com/reslava/chord-flow/releases/tag/v0.2.0
[0.1.0]: https://github.com/reslava/chord-flow/releases/tag/v0.1.0
