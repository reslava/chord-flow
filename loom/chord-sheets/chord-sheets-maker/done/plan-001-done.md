---
type: done
id: pl_01KXGSCVXQEEK70B1ZTMH4QY34-done
title: Done — Chord Sheets — ChordSheetR v1
status: done
created: 2026-07-14
version: 8
tags: []
parent_id: pl_01KXGSCVXQEEK70B1ZTMH4QY34
requires_load: []
---
# Done — Chord Sheets — ChordSheetR v1

## Step 1 — Add a nullable Capo to the Song record and a `capo <n>` SongParser directive (peer of `key`/`tempo`/`feel`), round-tripped through SongEntity/SongStore; existing content stays byte-identical.

Added a nullable presentational `Capo` (fret 1–12) to the Song model + a `capo <fret>` SongParser directive.

**Files**
- `Music/Songs/Song.cs` — new `int? Capo` property (documented as the presentational peer of `DefaultFeel`/`DefaultTempo`: it does *not* transpose the realized harmony, only records the capo for display); threaded through the private ctor and the `FromSections` factory (new optional `int? capo = null` param, placed before `voices`).
- `Music/Songs/SongParser.cs` — `CapoKeyword = "capo"` + `MinCapo=1`/`MaxCapo=12`; a position-independent `capo <fret>` stream directive (dup-guard, range-validated, `FormatException` on bad input) mirroring the `tempo` directive; added `capo` to the reserved-keyword guard in `TrySplitDefinition`; passes `capo` to `FromSections`; class doc-comment updated.
- `tests/ChordFlow.Core.Tests/SongParserTests.cs` — a `capo` test section mirroring the tempo tests (sets value, null-when-absent, position-independent, out-of-range/malformed throws, duplicate throws, reserved-keyword-as-part throws, verbatim round-trip).

**Decision / deviation from the plan file list:** no `SongEntity` change was needed. `SongStore` persists the raw DSL body **verbatim** and re-parses on load (there is no `Song→DSL` writer), so the `capo` directive round-trips through the stored DSL string for free — same mechanism that already carries `feel`/`tempo`. Capo range fixed at 1–12 (a capo above the 12th fret is not a real placement).

**Verification:** `ChordFlow.Core` builds clean (0 warnings); `SongParserTests` 65/65, all `~Song` tests 122/122.

## Step 2 — Define the pure/immutable ChordSheet model in Rendering/ChordSheet/ (Header, Section, Row, Cell, ChordRef, Tone) — instrument-agnostic, the only guitar edge being an optional FretboardDiagram? on ChordRef.

Defined the pure/immutable `ChordSheet` model.

**File:** `src/ChordFlow.Core/Rendering/ChordSheets/ChordSheet.cs` — records `ChordSheet` → `ChordSheetHeader`, `ChordSheetSection` → `ChordSheetRow` → `ChordSheetCell` → `ChordRef` → `ChordSheetTone`. Instrument-agnostic except the optional `ChordRef.Diagram` (`FretboardDiagram?`), which is exactly why the model sits in `Rendering/` (the allowed `Rendering → Instruments` edge) and not `Music/`. `ChordRef` carries all three notations (Concrete/Degree/Roman) + both tone labels (Note/Interval) so JS notation/label toggles need no round-trip (IN6/C3); `ChordSheetCell.RepeatOfPrev` + `BarTicks` and `ChordRef.DurationTicks` back the `%` simile and beat-proportional cell splitting.

**Decisions / deviations:**
- Namespace/folder is **plural** `ChordFlow.Rendering.ChordSheets` (folder `Rendering/ChordSheets/`), not the design's singular `Rendering/ChordSheet/`, to avoid a `ChordSheet` type-vs-namespace name clash.
- **Omitted the `Analysis` field / a `ChordAnalysis` type** from v1. The design sketch listed `analysis?` as "null in v1"; introducing a placeholder type now would be thrown away or would constrain the separate `[[harmonic-analysis]]` thread. Records make it a trivial additive field later, so v1 stays free of speculative types (EX2 excludes analysis anyway).

**Verification:** `ChordFlow.Core` builds clean (0 warnings). No unit test — a logic-free DTO is exercised by the step-3 builder tests.

## Step 3 — Features slice: resolve the Song/Progression via ExerciseRefs, realize with Transposer in the requested key, walk bars into Sections/Rows/Cells; per chord fill Concrete (ChordSymbol), Degree (RomanDegree), Roman (honest diatonic degree), Beats/BarTicks (HarmonicBar/ChordSpan), Tones (ChordTones+NoteSpeller+IntervalSpeller.Label), Diagram (CompingResolver when the diagram adornment is on); compute RepeatOfPrev by bar-equality.

Added the `ChordSheetBuilder` Features slice — the pure projection of a `RealizedSong` into the `ChordSheet` model.

**Files**
- `Features/ChordSheets/ChordSheetBuilder.cs` — `ChordSheetOptions(BarsPerRow=4)` + `Build(Song, RealizedSong, Key sheetKey, TimeSignature, ChordSheetOptions, CompingPlan? comping)`. Walks sections → chunks bars into rows of `BarsPerRow` → cells; per span emits a `ChordRef` with `Concrete` (`ChordSymbol.Format`), `Degree` (Nashville token), `Roman` (honest diatonic function), `DurationTicks`, `Tones` (`ChordTones.Of` + `NoteSpeller` + `IntervalSpeller.Label` + function colour-key), and `Diagram` (`RealizedVoicingDiagram.Build` from `comping.For(span)` when a plan is supplied, else null). `%` simile is derived in Core via `BarsEqual` (ordered spans equal by concrete `Chord` + `DurationTicks`), scoped per-section so a section never opens with a `%`.
- `tests/ChordFlow.Core.Tests/ChordSheets/ChordSheetBuilderTests.cs` — 15 tests: row chunking, the three-notation table (dominant/maj7/dim7/chromatic `#4dim7`), tone-strip spelling+function, simile detection (and non-detection), multi-chord even-split cell, header metadata (capo/tempo/feel/key/timesig), key-name reflects the sheet key, diagram null-without/set-with comping, and the `BarsPerRow=0` guard.

**Design choices (kept the builder store-free & pure):**
- `Build` takes an already-`Expand`ed `RealizedSong` + an optional pre-resolved `CompingPlan` (the handler owns the I/O seam, mirroring `ExerciseRendering`), so the builder needs no `IProgressionStore`/voicing infra and is trivially testable.
- **Nashville + Roman token formatters live as private helpers in the builder** (presentation formatting of an existing `RomanDegree`, not new theory) — no canonical writer existed. `Roman` formats only the degree's own quality (no secondary-dominant/borrowed inference — IN7/EX2).
- **`Header.Artist = null`**: the `Song` model has no artist field yet (same kind of small gap as capo was). Noted as a possible fast-follow; not in the req. Title = `Song.Name`.

**Verification:** Core builds clean (0 warnings after dropping a no-op `ThrowIfNull` on the value-type `TimeSignature`); `ChordSheetBuilderTests` 15/15.

## Step 4 — Add the inbound `chordSheet` envelope {harmonyEntity, harmonyId, keyPitchClass?, barsPerRow?, adornment, voicing?} + `chordSheetResult`/`chordSheetError` replies; wire WebMessageRouter → a ChordSheetHandler that calls the builder and serializes the model.

Added the `chordSheet` bridge verb + handler, wired into the host.

**Files**
- `Bridge/WebMessageRouter.cs` — `ChordSheetRequest` record (HarmonyEntity/HarmonyId/KeyPitchClass/BarsPerRow/Adornment/Voicing), `ChordSheetRequested` event, `case "chordSheet"` dispatch (reusing `ParseVoicingSource(renderOptions.voicing)` for the comping source), and `int? BarsPerRow, string? Adornment` on `InboundEnvelope`.
- `Features/ChordSheets/ChordSheetEnvelopes.cs` — `ChordSheetResultEnvelope(ChordSheet, "chordSheetResult")` + `ChordSheetErrorEnvelope(message, "chordSheetError")`.
- `Features/ChordSheets/ChordSheetHandler.cs` — DB-backed handler (short-lived context per request, like the others). Resolves harmony via `ExerciseRefs.ResolveHarmony`, computes `baseKey = keyOverride ?? song.InitialKey`, `SongExpander.Expand`s, resolves a `CompingPlan` **only when the adornment is diagram/both** (`CompingResolver.Resolve` with `StoredVoicingSource`/`VoicingReferenceSource`), then calls the pure `ChordSheetBuilder`.
- `Desktop/Program.cs` — `new ChordSheetHandler(dbOptions)` + `router.ChordSheetRequested +=` wiring that fails loud into a `chordSheetError` (the sheet-page peer of `scaleError`); added the `using`.
- `tests/ChordFlow.Core.Tests/ChordSheets/ChordSheetHandlerTests.cs` — 5 tests over in-memory SQLite: resolves a seeded progression into a sheet, key-override realizes in that key (C→G), adornment `none` leaves diagrams null vs `both` fills them, and a missing reference throws.

**Deviation from the plan file list:** put the outbound envelopes in `Features/ChordSheets/ChordSheetEnvelopes.cs` (not `Bridge/`) and the inbound `ChordSheetRequest` in the router, matching the existing `VoicingGrid`/`VoicingDerive` convention (feature-local envelopes; request records beside `VoicingGridFilter`). Adornment defaults to `none` for a bare request (the page always sends the real mode).

**Verification:** full solution builds clean (the only warnings are pre-existing `MSB3277` WindowsBase conflicts in the Desktop/WebView2 project, not from this change). ChordSheets tests 20/20 (15 builder + 5 handler).

## Step 5 — New wwwroot/chord-sheet-render-component.js (window.ChordFlowChordSheet): create/render/dispose over the model; shared SVG primitives (chord token with superscript quality + slash fraction, section tag, tone strip, `%`); FretR-style CSS-custom-property theming (light/dark/auto + setTheme, light pinned on export); embeds a ChordFlowFretboard mini-box for the diagram adornment. Header block drawn here.

Wrote `wwwroot/chord-sheet-render-component.js` (`window.ChordFlowChordSheet`, ChordSheetR) — the pure-SVG render component (option A per chat-001). `create(container, opts) → { render, setLayout, setNotation, setToneLabels, setAdornments, setTheme, setBarsPerRow, svgElement, renderForced, dispose }`. Shared primitives: `drawHeader` (title/artist + key/capo/tempo/timesig/feel subline), `drawChord` (primary token + optional secondary line), `drawToneStrip` (one function-coloured segment per tone, note⇄interval label), `drawDiagram` (a compact in-SVG vertical chord box reusing FretR's `FUNCTION_COLORS` + the `FretboardDiagram` model — NOT FretR's DOM), `drawSimile` (the one-bar-repeat mark). FretR-style `THEMES` light/dark + `resolveTheme` for `auto` (prefers-color-scheme); `renderForced(theme)` pins light for export. The whole sheet is one self-contained `<svg>` (screen == export).

## Step 6 — Arrange the shared primitives as a flowing leadsheet: 4 bars/row with `|` separators, boxed section tags above a section's first bar, the header block on top.

Layout A (flowing leadsheet) in the same file: `layoutSection` re-wraps a section's bars to `barsPerRow`; `drawRow` draws a `|` barline at each bar's left edge (+ a closing line after the row's last bar) with no cell borders; boxed section tag above the first row; header on top. Cells drawn by the shared `drawCell`.

## Step 7 — Arrange the shared primitives as a fixed grid: box-per-bar matrix (barsPerRow columns), bordered section blocks, multi-chord cells split by beat proportion, tone-strip / diagram slot under each cell.

Layout B (fixed grid) in the same file: `layoutSection` keeps Core's pre-chunked rows; `drawRow` frames each bar in a bordered cell; `drawSection` draws a section-block border around the rows (the B2 reference idiom). Multi-chord bars split the cell by each chord's `durationTicks/barTicks` share; the tone-strip + diagram adornments render under single-chord cells (multi-chord cells show split tokens only, a v1 clarity choice).

## Step 8 — setNotation({primary, secondary}) (letter/Nashville/Roman), setToneLabels(notes|intervals), setLayout(A|B), setTheme — all pure JS over fields the model already carries, no Core round-trip.

Toggles (pure JS over the carried model, no round-trip): `setLayout(A|B)`, `setNotation({primary,secondary})` (concrete/nashville/roman, secondary nullable), `setToneLabels(notes|intervals)`, `setAdornments({tones,diagrams})`, `setTheme(light|dark|auto)`, `setBarsPerRow`. Verification: `node --check` clean; a headless DOM-shim smoke test rendered every layout × adornment × theme combo + the multi-chord split, simile, and all toggles (incl. `renderForced`) with no runtime error and a correctly-sized `<svg>`. Full visual check deferred to the step-11 dogfood.

## Step 9 — Mount a top-level Chord Sheets view in index.html (lazy views/onShow like Scales/Voicings), with harmony/key/layout/notation/adornment/theme controls; issue the `chordSheet` verb only on key/adornment/voicing change, all other toggles JS-only; add bridge.js fan-out for the new envelopes.

Added the Chord Sheets nav view + the interactive HTML shell.

**Files**
- `wwwroot/chord-sheets.js` (`window.ChordFlowChordSheets`) — the shell/page (the "ChordSheetUIR"): builds a toolbar (Sheet/Key/Layout/Chords/+line/Below-cell/Tone-labels/Theme) and drives `ChordSheetR`. **Recompute vs pure-JS split (C3):** harmony/key/adornment changes send the `chordSheet` verb; layout/notation/tone-labels/theme are pure-JS `renderNow()` over the held model. Populates the harmony dropdown by requesting `entityList` for `song` + `progression` and merging the replies. Fail-loud via `chordSheetError`.
- `wwwroot/index.html` — `navChordSheets` nav button, the `#chord-sheets-view` / `#chord-sheets-page` mount, and the two script includes (`chord-sheet-render-component.js` + `chord-sheets.js`).
- `wwwroot/app.js` — registered the `chordSheets` view in the `views` registry (lazy `onShow → ChordFlowChordSheets.show()`, same pattern as Scales/Voicings); the existing loop wires the nav click.

**Note:** `bridge.js` needed **no change** — its inbound fan-out is already generic (`onReceive` sees every message), so the page just registers its own handler (the plan listed bridge.js as a maybe-touch).

**Verification:** `node --check` clean on the page module; the render component was smoke-tested in step 8. Full visual/interaction check is the step-11 dogfood (running the app).

## Step 10 — export('svg') serializes the composed SVG DOM; export('png') draws it to a canvas → blob; export('pdf') issues an exportChordSheet verb the Desktop host services via CoreWebView2.PrintToPdfAsync against a print-styled light render. Core stays pixel-free; no vendored PDF lib.

Export — SVG + PNG (client-side) and PDF (host-native), all pinned LIGHT (IN11).

**Files**
- `chord-sheet-render-component.js` — refactored: extracted `buildSheetSvg(themeName)` (shared by on-screen render + export, so screen == export), then added `toSvgString()` (serialize a light `<svg>` standalone), `toPngBlob(scale, cb)` (rasterize the light SVG onto a canvas via a `data:image/svg+xml` `<img>` → PNG blob; white fill; no external lib), and `lightSvg()` (detached light `<svg>` for the PDF print container). Removed the interim `renderForced`.
- `chord-sheets.js` — Export SVG/PNG/PDF buttons (right-aligned). SVG/PNG download client-side via a blob `<a download>` named after the sheet. PDF injects `lightSvg()` into `#chord-sheet-print` and sends `exportChordSheet`; the `chordSheetPdfDone` reply tears the print container back down (also on cancel/error).
- `index.html` — `#chord-sheet-print` body child + an `@media print` rule that shows only it (hides everything else) so the host's page-print captures just the sheet on white paper.
- `Bridge/WebMessageRouter.cs` — `ExportChordSheetPdfRequested` event + `case "exportChordSheet"`.
- `Features/ChordSheets/ChordSheetEnvelopes.cs` — `ChordSheetPdfDoneEnvelope(Ok, Path?, Message?)`.
- `Desktop/Program.cs` — wired the export: `SaveFileDialog` → `core.PrintToPdfAsync(path, null)` (WebView2 native, honors the `@media print` CSS) → `chordSheetPdfDone` reply; a cancel replies `Ok=false` (no lib, C4).

**Verification:** solution builds clean (only the pre-existing WindowsBase warnings); **full Core suite 916/916**; JS `node --check` clean; the headless smoke test now also exercises `lightSvg()` + `toSvgString()` across every layout/adornment/theme. The interactive PDF print + on-screen appearance get their visual confirmation in step 11.

## Step 11 — Render the Jazz Blues song + a pop song in both layouts × both notation modes × both adornments; export light PDFs and eyeball against docs/internal/chord-sheets/. Update chordflow-architecture-reference (new component + chordSheet/exportChordSheet verbs + Rendering-as-presentation-seam) and chordflow-domain-model-reference (ChordSheet model + ChordSheetBuilder + Song.Capo).

Dogfood + ref-sync.

**Dogfood (headless).** The desktop GUI can't be eyeballed in this environment, so I drove the **real** `ChordSheetR` component with a realistic 12-bar-Blues-in-C model (mirroring the unit-tested builder output) and serialized actual SVG markup to an HTML artifact across 4 variants: Layout A (Letter + Roman line, no adornment), Layout B (Letter + tone strip + fret diagrams), Layout A (Nashville + interval tone strip), Layout B (Roman, dark). Markup sanity checks all pass (section tag, chord tokens, Nashville `2-7`, Roman `ii7`, spelled tones, `<svg>`, diagram circles). Sent the HTML to Rafa to eyeball. The `%` simile, multi-chord turnaround split, and all notations/adornments/themes render. **The in-app run (live Chord Sheets tab, real PDF via PrintToPdfAsync) still wants a human run — flagged to Rafa.**

**Ref-sync (both docs updated in this unit of work):**
- `chordflow-architecture-reference` — added the two wwwroot files; a ChordSheetR render-component paragraph (pure-SVG, screen==export, dumb drawer, reuses FretR model+palette not its DOM, the design reversal) + the `chordSheet`/`exportChordSheet` verbs + the HTML shell; and reframed `Rendering/` as the presentation/export seam owning the instrument-agnostic `ChordSheet` model.
- `chordflow-domain-model-reference` — added `Song.Capo` (presentational invariant) + a "Chord-sheet presentation model" subsection (the `ChordSheet` records + `ChordSheetBuilder` as a pure `RealizedSong` projection).

**Overall verification:** solution builds clean; full Core suite **916/916**; JS `node --check` + smoke + dogfood all green.
