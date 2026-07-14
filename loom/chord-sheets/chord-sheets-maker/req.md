---
type: req
id: rq_01KXGSABWKS0GQPYQGJEM8JH8A
title: Chord Sheets — ChordSheetR design — Requirements
status: locked
created: 2026-07-14
updated: 2026-07-14
version: 1
design_version: 1
tags: []
parent_id: de_01KXGRA16DWE7FKJJEX3A2ZAXH
requires_load: []
---
# Chord Sheets — ChordSheetR design — Requirements

### ✅ Included

- `IN1` A Core **`ChordSheet` model** in `Rendering/ChordSheet/` — pure/immutable records `Header → Sections → Rows → Cells → ChordRef → Tone`, instrument-agnostic (the only guitar edge is an optional `FretboardDiagram?` on `ChordRef`).
- `IN2` A **`ChordSheetBuilder`** Features slice: resolves a Song/Progression through the content stores (`ExerciseRefs`), realizes it with `Transposer` in the requested key, and walks bars into the model (sections from the Song's part structure, rows chunked at `barsPerRow`, default 4).
- `IN3` A dedicated bridge verb **`chordSheet`** → reply **`chordSheetResult`** / error **`chordSheetError`**, routed via `WebMessageRouter` to a Features handler that calls the builder.
- `IN4` A JS render component **`ChordSheetR`** (`window.ChordFlowChordSheet`, `wwwroot/chord-sheet-render-component.js`) that composes **SVG** from the model — a dumb view, sibling of `ChordFlowScore`.
- `IN5` **Both layouts** — A (flowing engraved leadsheet: 4 bars/row, `|` separators, boxed section tags, superscript quality, slash-chord fraction) and B (fixed grid: box-per-bar matrix, bordered section blocks, multi-chord cells) — rendered from the *same* model.
- `IN6` **Notation display = primary token + an optional small secondary line**, toggleable (letter / Nashville / Roman), driven purely in JS from fields the model already carries.
- `IN7` The **Roman/function label is the honest diatonic degree only** in v1 (no secondary-dominant/borrowed inference).
- `IN8` **Key realization** — any key, **song key by default**, via `Transposer`.
- `IN9` A nullable **`Capo`** on `Song` + a `capo <n>` `SongParser` directive, surfaced to the sheet header and capo-aware display.
- `IN10` **Adornment = both, per-sheet toggle**: a **tone strip** (spelled tones, note-name ⇄ interval-degree label toggle, function-coloured) and a **fret diagram** (embedded FretR box, using the existing comping / difficulty-band voicing selection).
- `IN11` **Theming = FretR's CSS-custom-property pattern** (`light`/`dark`/`auto` + `setTheme`); **export pins the light token set**.
- `IN12` **Export** — SVG (serialize the composed DOM) + PNG (canvas) in JS, and **PDF via the Desktop host** `CoreWebView2.PrintToPdfAsync` (an `exportChordSheet` verb), against a print-styled light render.
- `IN13` The **`%` simile** in both layouts, with `RepeatOfPrev` **computed in Core** (bar-equality) and the glyph drawn in JS.
- `IN14` A **header block** (title / artist / key / tempo / feel / time-signature / capo) + **boxed section tags**.
- `IN15` **Harmonic-rhythm-aware cell splitting** — a multi-chord bar's cell is split from the `ChordSpan` beat proportions (`Beats`/`BarTicks` on the model).
- `IN16` **Dogfood**: render the Jazz Blues song + a pop song in both layouts and export a light PDF, eyeballed against `docs/internal/chord-sheets/`.

### ❌ Excluded

- `EX1` Animated playback beat-marker / current-bar highlight (v2).
- `EX2` Non-diatonic analysis markers (secondary dominants, borrowed/mixture, tritone subs) — consumed later from the `harmonic-analysis` thread.
- `EX3` Scale / mode + improv-target overlay.
- `EX4` Guide-tone / voice-leading lines between chords.
- `EX5` Advanced Layout-A engraving — true repeats `𝄆:𝄇`, 1st/2nd endings, coda/segno, D.C., fermata — and the Song-model structure they require.
- `EX6` Any standalone/CLI export path outside the in-app rendered view.
- `EX7` New voicing/derivation logic — the diagram reuses the existing comping selection only.
- `EX8` Faked or inferred harmony labels beyond the honest diatonic degree.

### ⛓ Constraints

- `C1` **ChordSheetR carries zero music theory** — it is a dumb view; all derivation is in Core (like FretR/ScoreR).
- `C2` Every model field is **derived from existing kernel types** (`Transposer`, `ChordSymbol`, `ChordTones`, `NoteSpeller`, `IntervalSpeller`, `HarmonicBar`, `CompingResolver`) — no new music theory.
- `C3` **Notation / theme / layout toggles are pure JS with no Core round-trip**; only key / adornment / voicing changes re-request a render.
- `C4` **Core stays pixel-free** — export is composed in JS + host-native; no external CDN and no vendored PDF library (CSP-safe).
- `C5` Follow the established **render-component pattern** (Core model producer + Bridge verb + dumb JS drawer); theming mirrors FretR incl. light-on-export.
- `C6` The `Song.Capo` addition keeps existing content **byte-identical** (nullable; `null` ⇒ no capo, no change to any current output).
- `C7` **Ref-sync**: update `chordflow-architecture-reference` and `chordflow-domain-model-reference` in the same unit of work as the code.
