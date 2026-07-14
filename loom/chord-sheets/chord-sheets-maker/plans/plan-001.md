---
type: plan
id: pl_01KXGSCVXQEEK70B1ZTMH4QY34
title: Chord Sheets — ChordSheetR v1
status: done
created: 2026-07-14
updated: 2026-07-14
version: 1
design_version: 1
req_version: 1
tags: []
parent_id: de_01KXGRA16DWE7FKJJEX3A2ZAXH
requires_load: []
target_version: 0.1.0
steps:
  - id: song-capo
    order: 1
    status: done
    description: Add a nullable Capo to the Song record and a `capo <n>` SongParser directive (peer of `key`/`tempo`/`feel`), round-tripped through SongEntity/SongStore; existing content stays byte-identical.
    files_touched: [src/ChordFlow.Core/Music/Songs/Song.cs, src/ChordFlow.Core/Music/Songs/SongParser.cs, src/ChordFlow.Core/Persistence/SongEntity.cs, tests/ChordFlow.Core.Tests/Songs/SongParserTests.cs]
    blocked_by: []
    satisfies: [IN9, C6, C2]
  - id: chordsheet-model
    order: 2
    status: done
    description: Define the pure/immutable ChordSheet model in Rendering/ChordSheet/ (Header, Section, Row, Cell, ChordRef, Tone) — instrument-agnostic, the only guitar edge being an optional FretboardDiagram? on ChordRef.
    files_touched: [src/ChordFlow.Core/Rendering/ChordSheet/ChordSheet.cs]
    blocked_by: []
    satisfies: [IN1, C1, C2]
  - id: chordsheetbuilder
    order: 3
    status: done
    description: "Features slice: resolve the Song/Progression via ExerciseRefs, realize with Transposer in the requested key, walk bars into Sections/Rows/Cells; per chord fill Concrete (ChordSymbol), Degree (RomanDegree), Roman (honest diatonic degree), Beats/BarTicks (HarmonicBar/ChordSpan), Tones (ChordTones+NoteSpeller+IntervalSpeller.Label), Diagram (CompingResolver when the diagram adornment is on); compute RepeatOfPrev by bar-equality."
    files_touched: [src/ChordFlow.Core/Features/ChordSheets/ChordSheetBuilder.cs, tests/ChordFlow.Core.Tests/ChordSheets/ChordSheetBuilderTests.cs]
    blocked_by: [song-capo, chordsheet-model]
    satisfies: [IN2, IN7, IN8, IN13, IN15, C2]
  - id: chordsheet-bridge-verb
    order: 4
    status: done
    description: Add the inbound `chordSheet` envelope {harmonyEntity, harmonyId, keyPitchClass?, barsPerRow?, adornment, voicing?} + `chordSheetResult`/`chordSheetError` replies; wire WebMessageRouter → a ChordSheetHandler that calls the builder and serializes the model.
    files_touched: [src/ChordFlow.Core/Bridge/ChordSheetEnvelopes.cs, src/ChordFlow.Core/Bridge/WebMessageRouter.cs, src/ChordFlow.Core/Features/ChordSheets/ChordSheetHandler.cs, tests/ChordFlow.Core.Tests/ChordSheets/ChordSheetHandlerTests.cs]
    blocked_by: [chordsheetbuilder]
    satisfies: [IN3, C5]
  - id: chordsheetr-skeleton-shared-primitives-theming
    order: 5
    status: done
    description: "New wwwroot/chord-sheet-render-component.js (window.ChordFlowChordSheet): create/render/dispose over the model; shared SVG primitives (chord token with superscript quality + slash fraction, section tag, tone strip, `%`); FretR-style CSS-custom-property theming (light/dark/auto + setTheme, light pinned on export); embeds a ChordFlowFretboard mini-box for the diagram adornment. Header block drawn here."
    files_touched: [src/ChordFlow.Desktop/wwwroot/chord-sheet-render-component.js]
    blocked_by: [chordsheet-model]
    satisfies: [IN4, IN11, IN14, C1, C5]
  - id: layout-a-flowing-leadsheet
    order: 6
    status: done
    description: "Arrange the shared primitives as a flowing leadsheet: 4 bars/row with `|` separators, boxed section tags above a section's first bar, the header block on top."
    files_touched: [src/ChordFlow.Desktop/wwwroot/chord-sheet-render-component.js]
    blocked_by: [chordsheetr-skeleton-shared-primitives-theming]
    satisfies: [IN5, IN14]
  - id: layout-b-fixed-grid
    order: 7
    status: done
    description: "Arrange the shared primitives as a fixed grid: box-per-bar matrix (barsPerRow columns), bordered section blocks, multi-chord cells split by beat proportion, tone-strip / diagram slot under each cell."
    files_touched: [src/ChordFlow.Desktop/wwwroot/chord-sheet-render-component.js]
    blocked_by: [chordsheetr-skeleton-shared-primitives-theming]
    satisfies: [IN5, IN15]
  - id: notation-adornment-tone-label-toggles
    order: 8
    status: done
    description: setNotation({primary, secondary}) (letter/Nashville/Roman), setToneLabels(notes|intervals), setLayout(A|B), setTheme — all pure JS over fields the model already carries, no Core round-trip.
    files_touched: [src/ChordFlow.Desktop/wwwroot/chord-sheet-render-component.js]
    blocked_by: [layout-a-flowing-leadsheet, layout-b-fixed-grid]
    satisfies: [IN6, IN10, C3]
  - id: chord-sheets-nav-view-page-wiring
    order: 9
    status: done
    description: Mount a top-level Chord Sheets view in index.html (lazy views/onShow like Scales/Voicings), with harmony/key/layout/notation/adornment/theme controls; issue the `chordSheet` verb only on key/adornment/voicing change, all other toggles JS-only; add bridge.js fan-out for the new envelopes.
    files_touched: [src/ChordFlow.Desktop/wwwroot/index.html, src/ChordFlow.Desktop/wwwroot/chord-sheets.js, src/ChordFlow.Desktop/wwwroot/bridge.js]
    blocked_by: [chordsheet-bridge-verb, notation-adornment-tone-label-toggles]
    satisfies: [IN8, IN10, C3]
  - id: export-svg-png-js-pdf-host
    order: 10
    status: done
    description: export('svg') serializes the composed SVG DOM; export('png') draws it to a canvas → blob; export('pdf') issues an exportChordSheet verb the Desktop host services via CoreWebView2.PrintToPdfAsync against a print-styled light render. Core stays pixel-free; no vendored PDF lib.
    files_touched: [src/ChordFlow.Desktop/wwwroot/chord-sheet-render-component.js, src/ChordFlow.Desktop/wwwroot/chord-sheets.js, src/ChordFlow.Desktop/WebHost/WebView2Bridge.cs]
    blocked_by: [chord-sheets-nav-view-page-wiring]
    satisfies: [IN12, C4]
  - id: dogfood-ref-sync
    order: 11
    status: done
    description: Render the Jazz Blues song + a pop song in both layouts × both notation modes × both adornments; export light PDFs and eyeball against docs/internal/chord-sheets/. Update chordflow-architecture-reference (new component + chordSheet/exportChordSheet verbs + Rendering-as-presentation-seam) and chordflow-domain-model-reference (ChordSheet model + ChordSheetBuilder + Song.Capo).
    files_touched: [loom/refs/chordflow-architecture-reference.md, loom/refs/chordflow-domain-model-reference.md]
    blocked_by: [export-svg-png-js-pdf-host]
    satisfies: [IN16, C7]
---
# Chord Sheets — ChordSheetR v1

## Goal

Build ChordSheetR v1: a Core-computed instrument-agnostic `ChordSheet` model produced by a `ChordSheetBuilder` Features slice, handed over a dedicated `chordSheet` bridge verb to a new dumb JS render component (`window.ChordFlowChordSheet`) that composes SVG in two layouts (A flowing leadsheet, B fixed grid), with primary+secondary notation, honest diatonic function labels, key+capo realization, both cell adornments (tone strip + FretR fret diagram), FretR-style theming, derived `%` simile, and SVG/PNG/PDF export (PDF host-native via WebView2 PrintToPdfAsync). No new music theory — every model field derives from existing kernel types; all pixels live in JS; both reference docs are updated as the code lands.

---

## Steps

| Done | # | Step | Files touched | Blocked by | Satisfies |
|---|---|---|---|---|---|
| ✅ | 1 | Add a nullable Capo to the Song record and a `capo <n>` SongParser directive (peer of `key`/`tempo`/`feel`), round-tripped through SongEntity/SongStore; existing content stays byte-identical. | src/ChordFlow.Core/Music/Songs/Song.cs, src/ChordFlow.Core/Music/Songs/SongParser.cs, src/ChordFlow.Core/Persistence/SongEntity.cs, tests/ChordFlow.Core.Tests/Songs/SongParserTests.cs | — | IN9, C6, C2 |
| ✅ | 2 | Define the pure/immutable ChordSheet model in Rendering/ChordSheet/ (Header, Section, Row, Cell, ChordRef, Tone) — instrument-agnostic, the only guitar edge being an optional FretboardDiagram? on ChordRef. | src/ChordFlow.Core/Rendering/ChordSheet/ChordSheet.cs | — | IN1, C1, C2 |
| ✅ | 3 | Features slice: resolve the Song/Progression via ExerciseRefs, realize with Transposer in the requested key, walk bars into Sections/Rows/Cells; per chord fill Concrete (ChordSymbol), Degree (RomanDegree), Roman (honest diatonic degree), Beats/BarTicks (HarmonicBar/ChordSpan), Tones (ChordTones+NoteSpeller+IntervalSpeller.Label), Diagram (CompingResolver when the diagram adornment is on); compute RepeatOfPrev by bar-equality. | src/ChordFlow.Core/Features/ChordSheets/ChordSheetBuilder.cs, tests/ChordFlow.Core.Tests/ChordSheets/ChordSheetBuilderTests.cs | song-capo, chordsheet-model | IN2, IN7, IN8, IN13, IN15, C2 |
| ✅ | 4 | Add the inbound `chordSheet` envelope {harmonyEntity, harmonyId, keyPitchClass?, barsPerRow?, adornment, voicing?} + `chordSheetResult`/`chordSheetError` replies; wire WebMessageRouter → a ChordSheetHandler that calls the builder and serializes the model. | src/ChordFlow.Core/Bridge/ChordSheetEnvelopes.cs, src/ChordFlow.Core/Bridge/WebMessageRouter.cs, src/ChordFlow.Core/Features/ChordSheets/ChordSheetHandler.cs, tests/ChordFlow.Core.Tests/ChordSheets/ChordSheetHandlerTests.cs | chordsheetbuilder | IN3, C5 |
| ✅ | 5 | New wwwroot/chord-sheet-render-component.js (window.ChordFlowChordSheet): create/render/dispose over the model; shared SVG primitives (chord token with superscript quality + slash fraction, section tag, tone strip, `%`); FretR-style CSS-custom-property theming (light/dark/auto + setTheme, light pinned on export); embeds a ChordFlowFretboard mini-box for the diagram adornment. Header block drawn here. | src/ChordFlow.Desktop/wwwroot/chord-sheet-render-component.js | chordsheet-model | IN4, IN11, IN14, C1, C5 |
| ✅ | 6 | Arrange the shared primitives as a flowing leadsheet: 4 bars/row with `\|` separators, boxed section tags above a section's first bar, the header block on top. | src/ChordFlow.Desktop/wwwroot/chord-sheet-render-component.js | chordsheetr-skeleton-shared-primitives-theming | IN5, IN14 |
| ✅ | 7 | Arrange the shared primitives as a fixed grid: box-per-bar matrix (barsPerRow columns), bordered section blocks, multi-chord cells split by beat proportion, tone-strip / diagram slot under each cell. | src/ChordFlow.Desktop/wwwroot/chord-sheet-render-component.js | chordsheetr-skeleton-shared-primitives-theming | IN5, IN15 |
| ✅ | 8 | setNotation({primary, secondary}) (letter/Nashville/Roman), setToneLabels(notes\|intervals), setLayout(A\|B), setTheme — all pure JS over fields the model already carries, no Core round-trip. | src/ChordFlow.Desktop/wwwroot/chord-sheet-render-component.js | layout-a-flowing-leadsheet, layout-b-fixed-grid | IN6, IN10, C3 |
| ✅ | 9 | Mount a top-level Chord Sheets view in index.html (lazy views/onShow like Scales/Voicings), with harmony/key/layout/notation/adornment/theme controls; issue the `chordSheet` verb only on key/adornment/voicing change, all other toggles JS-only; add bridge.js fan-out for the new envelopes. | src/ChordFlow.Desktop/wwwroot/index.html, src/ChordFlow.Desktop/wwwroot/chord-sheets.js, src/ChordFlow.Desktop/wwwroot/bridge.js | chordsheet-bridge-verb, notation-adornment-tone-label-toggles | IN8, IN10, C3 |
| ✅ | 10 | export('svg') serializes the composed SVG DOM; export('png') draws it to a canvas → blob; export('pdf') issues an exportChordSheet verb the Desktop host services via CoreWebView2.PrintToPdfAsync against a print-styled light render. Core stays pixel-free; no vendored PDF lib. | src/ChordFlow.Desktop/wwwroot/chord-sheet-render-component.js, src/ChordFlow.Desktop/wwwroot/chord-sheets.js, src/ChordFlow.Desktop/WebHost/WebView2Bridge.cs | chord-sheets-nav-view-page-wiring | IN12, C4 |
| ✅ | 11 | Render the Jazz Blues song + a pop song in both layouts × both notation modes × both adornments; export light PDFs and eyeball against docs/internal/chord-sheets/. Update chordflow-architecture-reference (new component + chordSheet/exportChordSheet verbs + Rendering-as-presentation-seam) and chordflow-domain-model-reference (ChordSheet model + ChordSheetBuilder + Song.Capo). | loom/refs/chordflow-architecture-reference.md, loom/refs/chordflow-domain-model-reference.md | export-svg-png-js-pdf-host | IN16, C7 |
---

### Legend

| Symbol | Meaning |
|--------|---------|
| ✅ | Done |
| 🔄 | In Progress |
| 🔳 | Pending |
| ❌ | Cancelled |
