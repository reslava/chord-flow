---
type: done
id: pl_01KXV0SB2EXT3Z6YY4EWD5G7ER-done
title: Done — Harmonic-analysis overlay — implementation
status: done
created: 2026-07-18
version: 5
tags: []
parent_id: pl_01KXV0SB2EXT3Z6YY4EWD5G7ER
requires_load: []
---
# Done — Harmonic-analysis overlay — implementation

## Step 1 — Add a pure glyph-only `HarmonicAnalysisFormatter` in `Rendering/ChordSheets/`.

Added `src/ChordFlow.Core/Rendering/ChordSheets/HarmonicAnalysisFormatter.cs` — a pure, glyph-only static class (namespace `ChordFlow.Rendering.ChordSheets`; `Rendering → Music` is an allowed edge):

- `HonestDegree(RomanDegree) → string` — numeral + case-by-quality + quality suffix + accidental prefix. This is the verbatim body of `ChordSheetBuilder.RomanFunction` (kept ASCII `b`/`#` prefixes so the honest label is byte-identical to today's output — analysis IN9 "agree by construction"). To be moved-out in step 2.
- `Glyph(ChordAnalysis, Key) → string` — SecondaryDominant ⇒ `"V7/" + TargetLabel`; SecondaryLeadingTone ⇒ `("vii°"|"vii°7") + "/" + TargetLabel` (° vs °7 from `Function.Quality`); all other categories ⇒ `HonestDegree(Function)`. `TargetLabel` cases the tonicized degree's numeral by its own diatonic quality via `DiatonicChord.Build(Scale.ForKey(key), target)` (+ `°`/`ø` marker for dim/half-dim targets).
- `CategoryKey(HarmonicCategory) → string` — camelCase colour-key vocabulary (`diatonic`/`secondaryDominant`/`secondaryLeadingTone`/`borrowed`/`tritoneSub`/`chromatic`).

Decision confirmed in code: honest `Roman` and the `Analysis` glyph coincide for Borrowed/TritoneSub/Chromatic (colour carries the signal), diverge only for the two secondary categories.

Tests: `tests/ChordFlow.Core.Tests/ChordSheets/HarmonicAnalysisFormatterTests.cs` — one fixture per category (constructing `ChordAnalysis` directly, no analyzer dependency). 7 tests, all green; solution builds clean.

## Step 2 — Add `Analysis` + `Category` strings to `ChordRef`; wire `ChordSheetBuilder.ToChordRef` to the analyzer + formatter; retire `RomanFunction`.

**`ChordRef` (`Rendering/ChordSheets/ChordSheet.cs`):** added `string Analysis` (functional glyph) + `string Category` (colour-key) after `Roman`; rewrote the record + param XML docs (dropped the "v1 honest-only" caveat, documented Roman as analyzer-sourced and Analysis/Category as the overlay fields). No raw `ChordAnalysis` on the DTO — pre-formatted strings only.

**`ChordSheetBuilder.ToChordRef` (`Features/ChordSheets/ChordSheetBuilder.cs`):** now calls `HarmonicAnalyzer.Analyze(chord, key)` once per span (its first consumer), and fills `Roman = HarmonicAnalysisFormatter.HonestDegree(analysis.Function)`, `Analysis = Glyph(analysis, key)`, `Category = CategoryKey(analysis.Category)`. Deleted the private `RomanFunction` + `Numerals` (moved into the formatter in step 1); kept `AccidentalPrefix` (still used by `NashvilleToken`). Major/minor works by construction — `section.Key` carries `IsMinor`, the analyzer is minor-symmetric, and the lead-in cell's `ToChordRef(...) with { … }` inherits the new fields.

Build clean. All 29 `~ChordSheets` tests pass — the pre-existing `ChordSheetBuilderTests` Roman assertions were unaffected, confirming the honest label is byte-identical (analysis IN9 "agree by construction"). The only `ChordRef` construction site is `ToChordRef`, and only ChordSheets tests reference the type.

## Step 3 — Assert the new `Roman`/`Analysis`/`Category` fields in `ChordSheetBuilderTests`, incl. dominant-blues, borrowed-iv, secondary-dominant, and a minor-key fixture.

Added builder golden tests to `tests/ChordFlow.Core.Tests/ChordSheets/ChordSheetBuilderTests.cs` (+ an `AMinor` key const and a `BuildInKey(dsl, key)` helper):

- `Build_AnalysisTable` (Theory) — asserts `Roman`/`Analysis`/`Category` for: secondary dominant `67` → A7, `Roman VI7` ≠ `Analysis V7/ii` (the divergence case, `secondaryDominant`); borrowed `4-` → Fm, `iv`/`iv`/`borrowed`; tritone sub `b27` → Db7, `bII7`/`bII7`/`tritoneSub`; diatonic `2-7` → Dm7, `ii7`/`ii7`/`diatonic`.
- `Build_DominantBlues_DoesNotOverLabelSecondaryDominants` — `17 47 57`: I7 and IV7 stay `chromatic` (honest degree, **not** `V7/x`), only V7 (G7) is `diatonic`. The analyzer's must-not-over-label case, now asserted on the sheet.
- `Build_MinorKey_ReadsHarmonicMinorDominantAsDiatonic` — A-minor `6- 37` (parent-major/C frame per first-class-minor-keys) → Am reads `i` diatonic, E7 reads `V7` diatonic (the harmonic-minor raised-third dominant). Proves minor symmetry through the builder.

One fix during the step: my first minor fixture used `1- 57`, which the C-frame realized as Cm/G (degrees are authored in the relative major). Corrected to `6- 37` — the actual parent-major degrees for A-minor's i and V7.

All fixtures hand-reasoned through `HarmonicAnalyzer` then confirmed against actual builder output. 28 `ChordSheetBuilderTests` pass; **full suite 1058/1058 green** — the `ChordRef` field addition rippled nowhere.

## Step 4 — Add the 3-state Roman sub-mode (Diatonic / Analysis / Both) + theme-aware category colour to ChordSheetR and the sheet strip.

**`chord-sheet-render-component.js` (ChordSheetR):**
- Documented the new model fields (`analysis`, `category`) + the `analysis` create-opt in the header.
- Added a per-theme category palette `cat` on the `THEMES` tables (harmonic-overlay D3): one pastel per non-diatonic category, slightly deeper on the light surface, lighter on the dark; `diatonic` intentionally absent → neutral ink.
- `analysisMode` state (`"diatonic"|"analysis"|"both"`, default `"analysis"`) + `normalizeAnalysis`.
- `romanToken(chord)` — diatonic ⇒ `roman`; analysis ⇒ `analysis`; both ⇒ `roman → analysis` **only when they differ** (diatonic bars stay a single label). `token(chord, which)` routes the Roman slot through it; other notations verbatim.
- `tokenFill(chord, t)` — the primary token takes the category tint when the overlay is on (so a borrowed/secondary chord reads special even in Letter mode), neutral otherwise. `drawChord` uses `token()` + `tokenFill()`.
- `setAnalysis(mode)` setter, exposed on the handle.

**`chord-sheets.js` (ChordFlowSheetView):** added `analysis: "analysis"` to `state`; a 3-option **Analysis** selector (Diatonic / Analysis / Both) in the display strip after "+ line", wired to `view.setAnalysis` (a pure re-render, never re-requests — IN8/C3); passes `analysis: state.analysis` into `ChordFlowChordSheet.create`.

Both pages get it for free — the strip lives in the shared `ChordFlowSheetView` that both Practice and the Content preview mount via the render-surface composite (IN9). Both JS files pass `node --check`. Field-name casing is correct by parity: `Analysis`/`Category` are string props alongside the working `Roman`/`Concrete` under the same serializer. Core data proven by the full 1058-test suite; in-app visual eyeball (both themes, a jazz-blues sheet) is the natural closing dogfood check on next run.

## Step 5 — Update the domain-model + architecture refs for the new fields, formatter, builder consumption, and the overlay.

Ref-doc sync (via `loom_patch_doc`, version-bumped):

**`chordflow-domain-model-reference` (rf_01KTM41K36DYJ0CE44FE7TMCGH):** updated the `ChordRef` signature to `(Concrete, Degree, Roman, Analysis, Category, DurationTicks, Tones, Diagram?)`, and rewrote the per-chord derivation clause — `Roman` now sourced from `HarmonicAnalyzer.Analyze`'s `Function` (inline `RomanFunction` retired, one function source), plus the new `Analysis` glyph (via `HarmonicAnalysisFormatter` in `Rendering/ChordSheets/`) and `Category` colour-key.

**`chordflow-architecture-reference` (rf_01KTSAPAT132QTEY5BEPRKS3MB):** the Rendering §3 `ChordSheetBuilder` line now lists the functional glyph & category among the projected fields; the ChordSheetR §5 paragraph documents the new **Analysis** display-strip control (3-state Diatonic/Analysis/Both + theme-aware `Category` tint) and that it rides the existing `loadScore`/`entityPreview` reply with no new bridge verb, showing identically on Practice and Content.

Left the pre-existing "ChordSheetHandler serves the chordSheet verb" staleness in the domain ref untouched (already flagged retired in the arch ref; out of this thread's scope).
