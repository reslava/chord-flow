---
type: design
id: de_01KXGRA16DWE7FKJJEX3A2ZAXH
title: Chord Sheets — ChordSheetR design
status: done
created: 2026-07-14
updated: 2026-07-14
version: 2
idea_version: 1
tags: []
parent_id: id_01KXGQHYZ9WS1YFBQRWYXQWHSJ
requires_load: []
---
# Chord Sheets — ChordSheetR design

> **Implementation note (agreed in chat-001, during the build):** the "embed FretR mini-boxes" idea in §6/§9 was reversed. FretR renders an HTML `<div>`, which can't live inside a single `<svg>`, and the export contract (SVG+PNG+PDF, no external libs) needs the sheet to *be* one SVG. So the split is: **`ChordSheetR`** = a pure self-contained `<svg>` render component (draws its own compact fret diagrams, reusing FretR's diagram *model* + *palette*) used for **both** the on-screen body **and** export (screen == export by construction); the **HTML shell / page** wraps it with controls + export + playback highlighting (toggling `<g data-bar>` attributes) and hosts the separate FretR now/next boards. One layout engine, no screen/export parity drift.

## 1. One sentence

A **Core-computed, instrument-agnostic `ChordSheet` model** is handed over the bridge to a new JS render component **`ChordSheetR`** (`window.ChordFlowChordSheet`, sibling of `ChordFlowScore`), which composes **SVG** in one of two layouts (A flowing leadsheet / B fixed grid) and exports to SVG/PNG/PDF. All music logic stays in Core; ChordSheetR is a dumb view, exactly like FretR.

## 2. Placement (where each piece lives)

Following the established render-component pattern (`score-render-component.js` / `fretboard-render-component.js` — a Core model producer + a Bridge verb + a dumb JS drawer):

| Piece | Home | Analogue |
|-------|------|----------|
| `ChordSheet` model (DTO) | **`Rendering/ChordSheet/`** in Core | `FretboardDiagram` is the spatial carrier; this is the *page* carrier |
| `ChordSheetBuilder` (producer) | **`Features/ChordSheets/`** | `ExerciseRendering` / `ContentCrud.Preview` (the I/O + composition seam) |
| Bridge verb `chordSheet` → `chordSheetResult` | `Bridge/` + the feature | `voicingGrid`/`scalePreview` verbs |
| `ChordSheetR` (`window.ChordFlowChordSheet`) | `wwwroot/chord-sheet-render-component.js` | `score-render-component.js` |

**Why the model is instrument-agnostic but not in `Music/`:** the sheet is a projection of harmony (sections/bars/chords/degrees/tones — all `Music`), but the *optional* fret-diagram adornment is a `FretboardDiagram` (guitar). Since `Music/` may not reference `Instruments/`, the composed carrier lives one layer out, in `Rendering/`. This reframes `Rendering/` from "alphaTex-only" to **the presentation/export seam** (alphaTex is one target; the chord sheet is a second) — consistent with the "Rendering is the export seam" framing in the architecture ref. *(Open decision D1 — see §11.)*

## 3. The `ChordSheet` model (Core DTO)

Pure/immutable records, like `FretboardDiagram`. Core fills every field it *can*; the JS decides what to paint from options.

```
ChordSheet(
  Header  Header,
  IReadOnlyList<Section> Sections )

Header( string Title, string? Artist, string KeyName, int? Tempo,
        string? Feel, string TimeSig, int? Capo )

Section( string? Label, IReadOnlyList<Row> Rows )   // Label = "Verse"/"A"/…
Row( IReadOnlyList<Cell> Cells )                     // barsPerRow cells (default 4)
Cell( IReadOnlyList<ChordRef> Chords, bool RepeatOfPrev, int BarTicks )
      // RepeatOfPrev ⇒ render "%"; Chords empty when RepeatOfPrev

ChordRef(
  string   Concrete,     // "C", "Fmaj7", "F/C"  (ChordSymbol.Format)
  string   Degree,       // "1", "5-", "#4"      (Nashville, from RomanDegree)
  string   Roman,        // "I", "V7", "ii"      (diatonic function label, v1 = honest degree)
  int      Beats,        // span length → cell-split proportion (from ChordSpan.DurationTicks)
  IReadOnlyList<Tone> Tones,       // spelled chord tones for the tone-strip adornment
  FretboardDiagram? Diagram,       // comping voicing, only when the diagram adornment is on
  ChordAnalysis? Analysis )        // null in v1; filled from harmonic-analysis thread later

Tone( string Note,        // "E", "G#"           (NoteSpeller)
      string Interval,    // "R", "3", "b7"      (IntervalSpeller.Label(sem, role))
      string Function )   // "root"/"third"/…    (colour key, FretR's vocabulary)
```

Every field is derived from types that already exist: `Transposer.Realize` → concrete chords + `RomanDegree`; `ChordSymbol.Format` → `Concrete`; `ChordTones` + `NoteSpeller` + `IntervalSpeller.Label` → `Tones`; `HarmonicBar`/`ChordSpan` → `Beats`/`BarTicks`; the Features `CompingResolver` → `Diagram`. **No new music theory** — only composition.

## 4. `ChordSheetBuilder` (Features slice)

`Build(harmonyRef, options) → ChordSheet`. Mirrors `ExerciseRendering`: it is the I/O seam (resolves a Song/Progression through the stores via `ExerciseRefs`), realizes it with `Transposer` in the requested key, walks bars into `Section`/`Row`/`Cell` (rows chunked at `barsPerRow`, sections from the Song's part structure), and per chord fills the `ChordRef`. Options bag: `{ key?, barsPerRow=4, adornment: none|tones|diagram|both, voicingSource (comping selection), notation-independent }` — notation *display* choices (letter/Nashville/Roman, primary+secondary, note-names vs intervals, layout, theme) are **JS-side**, because the model already carries every alternative; only *key* and *adornment/voicing* change what Core computes.

**`%` simile is computed in Core** (`RepeatOfPrev`): the builder compares a bar's realized chord content to its predecessor and sets the flag — the JS just prints `%`. Core owns the equality; JS owns the glyph. *(Decision D4.)*

## 5. Bridge verb

New dedicated verb (not an overload of `entityPreview`, whose model is `ChordFlowScore`-shaped):

- **inbound** `chordSheet` `{ harmonyEntity: song|progression, harmonyId, keyPitchClass?, barsPerRow?, adornment, voicing? }`
- **reply** `chordSheetResult` `{ sheet: ChordSheet }` · **error** `chordSheetError { message }` (UI-safe fail-loud, like `voicingDeriveError`).

`bridge.js` fans it out; the Chord Sheets view owns these envelope types and ignores others. A notation/theme/layout toggle that needs no Core recompute (letter⇄Nashville⇄Roman, note⇄interval, A⇄B, light⇄dark) is **pure JS** and never round-trips; only key / adornment / voicing changes re-request.

## 6. `ChordSheetR` component API

```
window.ChordFlowChordSheet.create(container, opts) → {
  render(sheet),                       // draw a ChordSheet model
  setLayout("A" | "B"),
  setNotation({ primary, secondary }), // e.g. primary:"concrete", secondary:"roman"|null
  setToneLabels("notes" | "intervals"),
  setTheme("light" | "dark" | "auto"),
  export("svg" | "png" | "pdf"),
  dispose()
}
```

- **SVG-first**: both layouts compose an `<svg>` DOM (measured text, `<g>` per bar/cell). One code path builds the shared primitives (chord token, superscript quality, slash fraction, section tag, tone strip, `%`); Layout A arranges them **flowing** (4 bars/row with `|` separators, section tags above the first bar), Layout B arranges them in a **CSS-grid-like fixed box matrix** with bordered section blocks. Same model, same palette → they read as one system.
- **Diagram adornment** embeds **FretR** mini chord-boxes per cell (as GuitarVoicingsR does: one `ChordFlowFretboard` per diagram, `controls` all off, theme locked to the sheet's), fed the `ChordRef.Diagram`.

## 7. Theming (FretR's exact pattern)

CSS custom properties on a themed root wrapper (`--cs-bg`, `--cs-ink`, `--cs-rule`, `--cs-accent`, plus the shared **function palette** reused from FretR for tone/diagram colour so a `3rd` is the same hue everywhere). `light`/`dark`/`auto` + `setTheme`. **Export pins the light token set** regardless of on-screen theme (PDF is always light). Embedded FretR boxes inherit the sheet's theme.

## 8. Notation display (primary + optional secondary)

Confirmed: **primary token + an optional small secondary line**, toggleable (not a one-at-a-time mode). The model carries `Concrete` / `Degree` / `Roman` simultaneously; `setNotation({primary, secondary})` selects which is the big token and which (if any) the small line beneath. v1 **`Roman` is the honest diatonic degree only** — no secondary-dominant/borrowed guessing; those labels arrive when the `[[harmonic-analysis]]` pass fills `ChordRef.Analysis` (v2).

## 9. Adornments (both, per-sheet toggle)

1. **Tone strip** — from `ChordRef.Tones`; a thin sub-row under the cell, one segment per tone, coloured by `Function`, label toggling **note-name ⇄ interval-degree** (both carried, no recompute). Dogfoods `NoteSpeller`/`IntervalSpeller`.
2. **Fret diagram** — an embedded FretR box from `ChordRef.Diagram`, using the **comping / difficulty-band voicing selection** already used elsewhere (`voicing` option → `CompingResolver`).

Both can show together (the B3 reference shows the strip; the fret diagram is the same slot, stacked).

## 10. Export

Core stays pixel-free; export is JS + host-native (no vendored PDF lib, no external CDN — CSP-safe):

- **SVG** — serialize the composed `<svg>` DOM (pure JS).
- **PNG** — draw that SVG onto a `<canvas>`, `toBlob` (pure JS).
- **PDF** — the **Desktop host** prints via WebView2 `CoreWebView2.PrintToPdfAsync` against a print-styled (light) render of the sheet — host-native, no library. A small `exportChordSheet {format}` verb lets JS ask the host to write the file. *(Open decision D2 — alternative: vendor a tiny SVG→PDF JS lib locally. Recommend host-native print.)*

## 11. Open decisions (want sign-off before req/plan)

- **D1 — model home.** `Rendering/ChordSheet/` (recommended; reframes Rendering as the presentation/export seam) vs `Features/ChordSheets/` (keep Rendering strictly alphaTex). Ref-doc impact either way.
- **D2 — PDF mechanism.** WebView2 host `PrintToPdfAsync` (recommended, host-native) vs a locally-vendored JS PDF lib.
- **D3 — Capo.** Layout A shows a capo (`(Capo 3rd fret)` + "play C shapes, sounds in E♭"). The Song model has no `Capo` today. Add a nullable `Capo` to `Song`/`SongParser` now (small, enables the capo header + capo-aware display in v1) or defer capo to a fast-follow? Recommend **add now**.

## 12. Scope & phasing

- **v1:** model + builder + verb + `ChordSheetR`; Layouts A & B; notation primary+secondary (letter/Nashville/Roman-diatonic); key realization (song-key default) + capo (if D3=add); adornment both (tone strip note⇄interval, fret diagram via comping); derived `%`; light/dark + light-on-export; export SVG/PNG/PDF.
- **v2 (captured):** animated playback marker (reuse Layout-B highlight + `playedBeatChanged`, and the Practice `loadScore` chord schedule); non-diatonic markers from `[[harmonic-analysis]]`; scale/improv overlay; guide-tone voice-leading lines; advanced Layout-A engraving (true repeats/endings/coda/segno/D.C.) once the Song model carries that structure.

## 13. Non-goals (v1)

- No faked analysis — `Roman` is the honest diatonic degree; `Analysis` stays null until the analysis thread lands.
- No Song-model repeat/ending/coda structure in v1 (plain barlines + `%`).
- No standalone/CLI export path — export flows from the in-app rendered SVG.
- No new voicing logic — the diagram reuses the existing comping selection.

## 14. Validation / dogfood

- Render **Jazz Blues** + a pop song (Elton-John-style reference) in **both layouts × both notation modes × both adornments**; export each to **PDF (light)** and eyeball against `docs/internal/chord-sheets/`.
- Toggle Nashville ⇄ song key ⇄ another key; confirm the realization matches `Transposer`.
- Light/dark parity on-screen; light pinned in the PDF.
- Confirm layout/notation/theme toggles do **not** round-trip to Core (only key/adornment/voicing do).

## 15. Reference-doc impact (per the ref-sync rule)

Landing this updates **`chordflow-architecture-reference`** (new render component + the `chordSheet`/`exportChordSheet` verbs + Rendering-as-presentation-seam framing) and **`chordflow-domain-model-reference`** (the `ChordSheet` model + `ChordSheetBuilder`, and `Song.Capo` if D3=add), in the same unit of work as the code.
