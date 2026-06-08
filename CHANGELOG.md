# Changelog

All notable changes to ChordFlow are documented here. The format is based on
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this project
adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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

[0.3.0]: https://github.com/reslava/chord-flow/releases/tag/v0.3.0
[0.2.0]: https://github.com/reslava/chord-flow/releases/tag/v0.2.0
[0.1.0]: https://github.com/reslava/chord-flow/releases/tag/v0.1.0
