---
type: done
id: pl_01KW2GBV0668GBT59RDW1PAX3G-done
title: Done — Derive CAGED 6th voicings — implementation
status: done
created: 2026-06-27
version: 1
tags: []
parent_id: pl_01KW2GBV0668GBT59RDW1PAX3G
requires_load: []
---
# Done — Derive CAGED 6th voicings — implementation

## Step 1 — Add `Quality.Major6`/`Minor6` and their `QualityFormulas` rows `"1 3 5 6"` / `"1 b3 5 6"`; add the derived `{0,4,7,9}`/`{0,3,7,9}` cases to QualityFormulasTests + ChordTonesTests.

Added `Quality.Major6`/`Minor6` + `QualityFormulas` rows `1 3 5 6` / `1 b3 5 6`; `QualityIntervals` derives `{0,4,7,9}`/`{0,3,7,9}`. Tests in QualityFormulasTests + ChordTonesTests.

## Step 2 — `CagedDerivation`: add the E-shape exception for `{HalfDiminished7, Diminished7, Major6, Minor6}` — mute string 5 in candidate building and grant the index behind-1 stretch-back (window low edge = bassFret−1). Keep Diminished7's existing un-gated stretch-back.

`CagedDerivation`: `EShapeSkipString5 = {m7b5, dim7, 6, m6}`; in the E shape only, mute string 5 (skip in candidate building) and grant the index behind-1 stretch-back (`allowStretchBack = stacksUp && (dim7 || eShapeException)`). Other shapes byte-identical.

## Step 3 — Update the m7b5 & dim7 E golden-oracle grips to `8 x 8 8 7 8` / `8 x 7 8 7 8` in `CagedDerivationOracleTests.Authored` and the two fixture `.dsl` files; confirm derived == authored for all oracle cells.

m7b5 E → `8 x 8 8 7 8`, dim7 E → `8 x 7 8 7 8` in `CagedDerivationOracleTests.Authored` + the two fixtures. Oracle proves derived == authored (the tweak reproduces Rafa's grips).

## Step 4 — `VoicingDslParser`: accept `6` → `Major6` and `m6`/`-6` → `Minor6` quality suffixes; add parser tests.

`VoicingDslParser` suffixes `6`→Major6, `m6`/`-6`→Minor6; matching `VoicingDslWriter` inverse (`6`/`m6`) for the catalog round-trip. Parser tests added.

## Step 5 — Add `Major6`/`Minor6` to `CagedVoicingCatalog` as five-shape qualities; add their `EngineVoicingSource.DisplayNames` and `caged-chords.js` `QUALITIES` entries ("6"/"m6"); update coverage/count tests to expect the new families derive no-throw across all shapes and roots.

`CagedVoicingCatalog` 6/m6 as five-shape qualities (36→46); `EngineVoicingSource.DisplayNames`, `caged-chords.js` QUALITIES, plus required collaborators `AutomaticVoicingId` (tokens) + `ChordSymbol` (suffixes). Coverage test reworked: 46 offered ⊇ oracle-verified, 6/m6 derive fully-spelled at canonical region + realize across roots. `EngineVoicingSourceTests` count 36→46.

## Step 6 — E-shape behind-1 anchor finger → middle: `AnchorFinger.Derive` gains a stretch-back branch (index pinned to the low-edge stretch fret, fingers count up one-per-fret), gated in `CagedDerivation` to the E-shape behind-1 case (A/D dim7 keep Index); m7b5 E & dim7 E fixtures back to `anchor:m`.

IN11 — `AnchorFinger.Derive` gains `indexOnStretchBack` branch (index pinned to low-edge stretch fret → root +1 = middle). `CagedDerivation` passes it when `eShapeException && boxMin == stretchBackFret`. m7b5 E / dim7 E fixtures `anchor:m`. A/D dim7 keep Index (E-gated).

## Step 7 — Quality-resolved chord-tone labels: add `ChordToneFunction.Sixth`; classify chord tones from the `QualityFormulas` degree (degree 6 → Sixth, 7 → Seventh) so semitone 9 reads `6` for 6/m6 and `bb7` for dim7; `IntervalSpeller.Label` Sixth → `"6"`; `fretboard-render-component.js` gains a `sixth` colour (`#f59e0b`) + legend; update ChordTones/diagram tests.

IN10 — `ChordToneFunction.Sixth`; `ChordTones.Of` classifies from the formula degree (degree 6→Sixth, 7→Seventh) so semitone 9 resolves by quality; `IntervalSpeller.Degree(token)` added (factored shared `ParseToken`); `IntervalSpeller.Label` Sixth→`6`. `ChordShapeDiagram` + `RealizedVoicingDiagram` route through the shared classifier; both `FunctionName` gain `sixth`. `fretboard-render-component.js` sixth colour `#f59e0b` + legend, and the legend now sorts by interval degree. Tests in ChordTones/IntervalSpeller/ChordShapeDiagram.

## Step 8 — Dogfood: run the app, render 6 and m6 across all CAGED shapes on the fretboard page; Rafa reviews the derived grips (E grips against the known `8 x 7 9 8 8` / `8 x 7 8 8 8`, other shapes by eye). STOP for his blessing before capture.

Dogfood visual gate — Rafa reviewed 6/m6 across C/A/G/E/D and blessed ("Good"), after the IN10/IN11 fixes from his first-round review. Legend-sort tweak applied per his note.

## Step 9 — Capture the confirmed 6/m6 grips into the golden oracle: new `fixtures/caged-oracle/*.dsl`, added `CagedDerivationOracleTests.Authored` rows, and bumped fixture/`ExpectedVoicingCount` assertions.

Captured all 10 confirmed grips (Major6/Minor6 × CAGED) into the golden oracle: new `fixtures/caged-oracle/{maj6,m6}_{c,a,g,e,d}shape.dsl` (with derived anchors r/i/p/m), 10 rows in `CagedDerivationOracleTests.Authored`, `ExpectedVoicingCount` 36→46. All round-trip derived == authored.

## Step 10 — Sync `chordflow-domain-model-reference.md`: the two new qualities (Major6/Minor6 + formulas), the E-shape skip-string-5 + behind-1 stretch-back derivation rule, the E-shape behind-1 anchor=middle rule, and the new `ChordToneFunction.Sixth` + formula-degree chord-tone classification.

Ref-synced `chordflow-domain-model-reference.md`: 11 qualities, formula-degree `ChordToneFunction.Sixth`, `IntervalSpeller.Degree`/`6` label, E-shape skip-string-5 + behind-1 rule, E-shape behind-1 anchor=middle, 46-grip oracle, marker `sixth` colour-key. Full suite 746 green.
