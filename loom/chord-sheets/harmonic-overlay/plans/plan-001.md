---
type: plan
id: pl_01KXV0SB2EXT3Z6YY4EWD5G7ER
title: Harmonic-analysis overlay — implementation
status: done
created: 2026-07-18
updated: 2026-07-18
version: 1
design_version: 1
req_version: 1
tags: []
parent_id: de_01KXV0JDNCMH8MJV7Y10A7V8R1
requires_load: []
target_version: 0.1.0
actual_release: 0.15.0
steps:
  - id: core-rendering-formatter
    order: 1
    status: done
    description: Add a pure glyph-only `HarmonicAnalysisFormatter` in `Rendering/ChordSheets/`.
    files_touched: [src/ChordFlow.Core/Rendering/ChordSheets/HarmonicAnalysisFormatter.cs]
    blocked_by: []
    satisfies: [IN3, IN5, C2]
  - id: chordref-fields-builder-consumes-analyzer
    order: 2
    status: done
    description: Add `Analysis` + `Category` strings to `ChordRef`; wire `ChordSheetBuilder.ToChordRef` to the analyzer + formatter; retire `RomanFunction`.
    files_touched: [src/ChordFlow.Core/Rendering/ChordSheets/ChordSheet.cs, src/ChordFlow.Core/Features/ChordSheets/ChordSheetBuilder.cs]
    blocked_by: [core-rendering-formatter]
    satisfies: [IN1, IN2, IN4, IN10, C4]
  - id: builder-golden-tests
    order: 3
    status: done
    description: Assert the new `Roman`/`Analysis`/`Category` fields in `ChordSheetBuilderTests`, incl. dominant-blues, borrowed-iv, secondary-dominant, and a minor-key fixture.
    files_touched: [tests/ChordFlow.Core.Tests/ChordSheets/ChordSheetBuilderTests.cs]
    blocked_by: [chordref-fields-builder-consumes-analyzer]
    satisfies: [IN11, IN10]
  - id: chordsheetr-overlay-strip-selector
    order: 4
    status: done
    description: Add the 3-state Roman sub-mode (Diatonic / Analysis / Both) + theme-aware category colour to ChordSheetR and the sheet strip.
    files_touched: [src/ChordFlow.Desktop/wwwroot/chord-sheet-render-component.js, src/ChordFlow.Desktop/wwwroot/chord-sheets.js]
    blocked_by: [chordref-fields-builder-consumes-analyzer]
    satisfies: [IN6, IN7, IN8, IN9, C1, C3]
  - id: reference-doc-sync
    order: 5
    status: done
    description: Update the domain-model + architecture refs for the new fields, formatter, builder consumption, and the overlay.
    files_touched: [loom/refs/chordflow-domain-model-reference.md, loom/refs/chordflow-architecture-reference.md]
    blocked_by: [chordsheetr-overlay-strip-selector]
    satisfies: [IN12]
---
# Harmonic-analysis overlay — implementation

## Goal

Render the pure `HarmonicAnalyzer` pass's functional labels on ChordSheetR. A new Core `Rendering/ChordSheets` formatter turns each chord's `ChordAnalysis` (+ key) into glyph strings; `ChordSheetBuilder` consumes the analyzer over its already-in-scope `(realized chord, section.Key)` spans, re-sources the honest `Roman` from the analyzer's `Function` (retiring the inline `RomanFunction`), and carries pre-formatted `Roman` + `Analysis` glyph + `Category` colour-key strings on `ChordRef`. Those fields ride the existing unified `loadScore` / `entityPreview` reply, so ChordSheetR gains a 3-state Roman sub-mode (Diatonic / Analysis / Both) and theme-aware non-diatonic colour as a pure re-draw on both the Practice and Content surfaces — no new bridge verb, no round-trip. The analyzer stays glyph-free and ChordSheetR stays a dumb drawer throughout; major and minor tonics work by construction (the analyzer is minor-symmetric and the builder passes the section's real key).

---

## Steps

| Done | # | Step | Files touched | Blocked by | Satisfies |
|---|---|---|---|---|---|
| ✅ | 1 | Add a pure glyph-only `HarmonicAnalysisFormatter` in `Rendering/ChordSheets/`. | src/ChordFlow.Core/Rendering/ChordSheets/HarmonicAnalysisFormatter.cs | — | IN3, IN5, C2 |
| ✅ | 2 | Add `Analysis` + `Category` strings to `ChordRef`; wire `ChordSheetBuilder.ToChordRef` to the analyzer + formatter; retire `RomanFunction`. | src/ChordFlow.Core/Rendering/ChordSheets/ChordSheet.cs, src/ChordFlow.Core/Features/ChordSheets/ChordSheetBuilder.cs | core-rendering-formatter | IN1, IN2, IN4, IN10, C4 |
| ✅ | 3 | Assert the new `Roman`/`Analysis`/`Category` fields in `ChordSheetBuilderTests`, incl. dominant-blues, borrowed-iv, secondary-dominant, and a minor-key fixture. | tests/ChordFlow.Core.Tests/ChordSheets/ChordSheetBuilderTests.cs | chordref-fields-builder-consumes-analyzer | IN11, IN10 |
| ✅ | 4 | Add the 3-state Roman sub-mode (Diatonic / Analysis / Both) + theme-aware category colour to ChordSheetR and the sheet strip. | src/ChordFlow.Desktop/wwwroot/chord-sheet-render-component.js, src/ChordFlow.Desktop/wwwroot/chord-sheets.js | chordref-fields-builder-consumes-analyzer | IN6, IN7, IN8, IN9, C1, C3 |
| ✅ | 5 | Update the domain-model + architecture refs for the new fields, formatter, builder consumption, and the overlay. | loom/refs/chordflow-domain-model-reference.md, loom/refs/chordflow-architecture-reference.md | chordsheetr-overlay-strip-selector | IN12 |
---

### Legend

| Symbol | Meaning |
|--------|---------|
| ✅ | Done |
| 🔄 | In Progress |
| 🔳 | Pending |
| ❌ | Cancelled |

<!-- step:core-rendering-formatter -->
### Step 1 — Core Rendering formatter

New static class, no I/O, may reference `Music.Harmony`.

- `HonestDegree(RomanDegree) → string` — the numeral + case-by-quality + quality suffix + accidental-prefix logic moved out of `ChordSheetBuilder.RomanFunction` (identical output shape — analysis-thread IN9's "agree by construction").
- `Glyph(ChordAnalysis, Key) → string` — Diatonic / Borrowed / TritoneSub / Chromatic ⇒ `HonestDegree(Function)`; SecondaryDominant ⇒ `"V7/" + targetRoman`; SecondaryLeadingTone ⇒ `("vii°" | "vii°7") + "/" + targetRoman` (° vs °7 read off `Function.Quality`). `targetRoman` = the target degree's numeral cased by its own diatonic quality in the key, via `DiatonicChord.Build(Scale.ForKey(key), target)`. Formatting only — no new theory.
- `CategoryKey(HarmonicCategory) → string` — the colour-key vocabulary (`diatonic`/`secondaryDominant`/`secondaryLeadingTone`/`borrowed`/`tritoneSub`/`chromatic`).

Unit-test the formatter directly (glyph strings for one fixture per category).

<!-- step:chordref-fields-builder-consumes-analyzer -->
### Step 2 — ChordRef fields + builder consumes analyzer

`ChordRef`: add `string Analysis` (functional glyph) + `string Category` (colour-key), keep `Roman`; update the record's XML docs (drop the "v1 honest-only" caveat, note the analyzer is now the source).

`ToChordRef`: call `HarmonicAnalyzer.Analyze(chord, key)` once per span, then fill `Roman = HonestDegree(analysis.Function)`, `Analysis = Glyph(analysis, key)`, `Category = CategoryKey(analysis.Category)`. Delete the private `RomanFunction` + its `Numerals`/suffix/accidental helpers (moved to the formatter in step 1). Major/minor works by construction — `section.Key` carries `IsMinor` and the analyzer is minor-symmetric. Build stays green (model + only construction site change together).

<!-- step:builder-golden-tests -->
### Step 3 — Builder golden tests

Cases: the dominant-blues `I7 IV7 V7` must-not-over-label (all read as blues `Chromatic`/diatonic, not `V7/x`); a borrowed `iv` (Category `borrowed`, `Analysis == Roman == iv`); a secondary dominant (`Analysis == V7/ii`, `Roman == VI7` — the divergence case); a minor-key fixture proving major/minor symmetry through the builder. Hand-reason the expected glyph strings, then paste the actual builder output beside them for independent verification (golden-oracle house rule).

<!-- step:chordsheetr-overlay-strip-selector -->
### Step 4 — ChordSheetR overlay + strip selector

`chord-sheets.js` (ChordFlowSheetView) display strip: the Roman/notation control gains the 3-state selector (Diatonic = overlay off / Analysis = default / Both). Pure re-render, no round-trip.

`chord-sheet-render-component.js` (ChordSheetR): draw `Roman` vs `Analysis` vs the paired `Roman → Analysis` form (pair shown only where the two differ) per the state; tint the chord token (or a small marker) by `Category` via a category palette with a light and a dark variant, beside FretR's function palette. Dumb-drawer only — no music theory in JS; the fields already ride the existing reply, so nothing round-trips. Appears on both Practice and Content via the shared render surface (progression/song; rhythm stays score-only).

<!-- step:reference-doc-sync -->
### Step 5 — Reference-doc sync

`chordflow-domain-model-reference`: the new `ChordRef.Analysis`/`Category` fields, the `Rendering/ChordSheets` analysis formatter, and `ChordSheetBuilder` now consuming `HarmonicAnalyzer` (with `RomanFunction` retired). `chordflow-architecture-reference`: ChordSheetR's analysis overlay + 3-state selector, and that it rides the existing `loadScore` / `entityPreview` reply with no new bridge verb. Same unit of work as the code (ref-sync rule).
