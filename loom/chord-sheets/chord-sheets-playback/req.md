---
type: req
id: rq_01KXJJKWAQEQJNXPGPF3569H16
title: Chord Sheets playback — animated bar marker over ChordSheetR — Requirements
status: locked
created: 2026-07-15
updated: 2026-07-15
version: 1
design_version: 1
tags: []
parent_id: de_01KXJJJYD9XBRYED1F8HYTG74H
requires_load: []
---
# Chord Sheets playback — animated bar marker over ChordSheetR — Requirements

> Built against `design.md`. Open decisions D1–D4 are set to their **recommended** answers below (D1-a combined request, D2 cell+sub-chord, D3 builder-side `%` coordinate, D4 hidden staff + optional toggle). Confirm/adjust before this req is locked.

### ✅ Included

- `IN1` Extract a shared JS **`ChordFlowPlayback`** (`window.ChordFlowPlayback`, new `wwwroot/`) from `score-render-component.js`: it owns the alphaTab `AlphaTabApi(player)` + its surface, transport (`load`, `play/playPause`, `stop`, `setTempo`, per-track volume), soundfont (list + persisted-choice apply), and emits `onBeat(bar,beat)` (from `activeBeatsChanged`, 1-based), `onStateChange`, `onFinished`.
- `IN2` Refactor **`ScoreR`** to compose a `ChordFlowPlayback` internally for all transport/audio/beat, while **re-exposing its current public handle unchanged** (visible staff + notation controls stay in ScoreR).
- `IN3` **ScoreR parity** — the Practice view (`app.js`) and Content-preview (`content-crud.js`) require **no edits** and behave identically (play/pause/stop, tempo, cursor, soundfont, `onBeat`, schedule).
- `IN4` **`ChordSheetBuilder` emits a `cellSchedule`** — a flat, time-ordered array of `CellScheduleEntry(Bar, Beat, Section, Row, Cell, Chord)` (0-based; same `(bar,beat)` origin as the audio timeline), one entry per (cell, chord-segment) onset.
- `IN5` A **`%` `RepeatOfPrev` cell gets its own `cellSchedule` entry** at its own bar downbeat (its own Section/Row/Cell, `Chord=0`) — D3.
- `IN6` The **`chordSheet` handler returns `sheet` + `cellSchedule` + the song's `alphaTex`** in `chordSheetResult`, all from one realized-Song pass (alignment by construction) — D1-a.
- `IN7` **`ChordSheetR` becomes addressable:** each bar-cell wrapped in `<g data-section data-row data-cell>`, each split-cell chord segment in a nested `<g data-chord>`; in **both layouts** (A and B).
- `IN8` **`ChordSheetR.highlight(section,row,cell,chord?)` + `clearHighlight()`** — toggle a screen-only "playing" state on the addressed `<g>` by **re-querying the current SVG** (no held node refs); the page re-applies the last highlight after any re-render.
- `IN9` **Highlight granularity:** cell (=bar) level always, plus the active **chord segment** within a split bar (D2); adornments ride the cell wash, not separately animated.
- `IN10` **Highlight visual reads in light and dark**, from the shared palette; **screen-only** — never present in `toSvgString`/`toPngBlob`/`lightSvg` export output.
- `IN11` **Chord Sheets page owns a `ChordFlowPlayback`** (hidden staff surface) + a transport strip: loads the returned `tex`, builds a `bar:beat→cell` map from `cellSchedule`, drives `highlight` from `onBeat`, and `clearHighlight` on stop/finish.
- `IN12` **Start / stop / seek** — marker appears on play, clears on stop, lands on the correct cell on reposition; sub-onset beats keep the last cell highlighted.
- `IN13` **Dogfood** — Jazz Blues + a pop song, both layouts: marker tracks the sounding bar/cell, matches the ScoreR cursor beat-for-beat, handles a split bar and a `%` bar, clears on stop; light/dark on-screen; export unaffected.

### ❌ Excluded

- `EX1` No **shared global transport bus** (option c) — each page owns its own `ChordFlowPlayback`.
- `EX2` No **export of the animated state** — export stays the v1 static light snapshot.
- `EX3` No **new Core music theory** beyond the `cellSchedule` projection over the existing model.
- `EX4` No **separate per-adornment animation** (tone strip / fret diagram) in v1.
- `EX5` No **accuracy / timing detection**.
- `EX6` None of the **sibling v2 overlays** — non-diatonic analysis markers, scale/improv overlay, guide-tone lines, advanced Layout-A engraving.

### ⛓ Constraints

- `C1` **`ChordSheetR` stays a dumb view** — zero music theory, **zero alphaTab dependency**; export remains one self-contained `<svg>`.
- `C2` **ScoreR's public handle is preserved** — the extraction is internal; Practice + Content-preview are the parity oracle (IN3).
- `C3` **`cellSchedule` derives from existing kernel types** (SongExpander / HarmonicBar / ChordSpan) with **no new theory**, and shares the score's `(bar,beat)` origin (same realized Song).
- `C4` **Marker driven purely by attribute/class toggling** — **no Core round-trip during playback**; each page owns its own engine (option a).
- `C5` **Highlight is screen-only** and light/dark-safe; export is inert by construction.
- `C6` **Ref-sync** — update `chordflow-architecture-reference` (new `ChordFlowPlayback`, ScoreR refactor, `chordSheetResult` contract change) and `chordflow-domain-model-reference` (`cellSchedule` projection) in the same unit of work as the code.