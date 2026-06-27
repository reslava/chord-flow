---
type: plan
id: pl_01KW2GBV0668GBT59RDW1PAX3G
title: Derive CAGED 6th voicings — implementation
status: done
created: 2026-06-26
updated: 2026-06-27
version: 1
design_version: 1
req_version: 1
tags: []
parent_id: de_01KW2G8YYBE2QE4M78H519RH2A
requires_load: []
target_version: 0.1.0
steps:
  - id: qualities-major6-minor6
    order: 1
    status: done
    description: Add `Quality.Major6`/`Minor6` and their `QualityFormulas` rows `"1 3 5 6"` / `"1 b3 5 6"`; add the derived `{0,4,7,9}`/`{0,3,7,9}` cases to QualityFormulasTests + ChordTonesTests.
    files_touched: [src/ChordFlow.Core/Music/Harmony/Quality.cs, src/ChordFlow.Core/Music/Harmony/QualityFormulas.cs, tests/ChordFlow.Core.Tests/QualityFormulasTests.cs, tests/ChordFlow.Core.Tests/ChordTonesTests.cs]
    blocked_by: []
    satisfies: [IN1, IN2, C3]
  - id: eshape-skip5-stretchback
    order: 2
    status: done
    description: "`CagedDerivation`: add the E-shape exception for `{HalfDiminished7, Diminished7, Major6, Minor6}` — mute string 5 in candidate building and grant the index behind-1 stretch-back (window low edge = bassFret−1). Keep Diminished7's existing un-gated stretch-back."
    files_touched: [src/ChordFlow.Core/Instruments/Guitar/Caged/CagedDerivation.cs]
    blocked_by: [qualities-major6-minor6]
    satisfies: [IN3, C1, C2]
  - id: update-m7b5-dim7-eshape-oracle
    order: 3
    status: done
    description: Update the m7b5 & dim7 E golden-oracle grips to `8 x 8 8 7 8` / `8 x 7 8 7 8` in `CagedDerivationOracleTests.Authored` and the two fixture `.dsl` files; confirm derived == authored for all oracle cells.
    files_touched: [tests/ChordFlow.Core.Tests/CagedDerivationOracleTests.cs, tests/ChordFlow.Core.Tests/fixtures/caged-oracle/m7b5_eshape.dsl, tests/ChordFlow.Core.Tests/fixtures/caged-oracle/dim7_eshape.dsl]
    blocked_by: [eshape-skip5-stretchback]
    satisfies: [IN4]
  - id: voicing-dsl-6-m6-suffixes
    order: 4
    status: done
    description: "`VoicingDslParser`: accept `6` → `Major6` and `m6`/`-6` → `Minor6` quality suffixes; add parser tests."
    files_touched: [src/ChordFlow.Core/Instruments/Guitar/Voicings/VoicingDslParser.cs, tests/ChordFlow.Core.Tests/VoicingDslParserTests.cs]
    blocked_by: [qualities-major6-minor6]
    satisfies: [IN6]
  - id: catalog-ui-6-m6
    order: 5
    status: done
    description: Add `Major6`/`Minor6` to `CagedVoicingCatalog` as five-shape qualities; add their `EngineVoicingSource.DisplayNames` and `caged-chords.js` `QUALITIES` entries ("6"/"m6"); update coverage/count tests to expect the new families derive no-throw across all shapes and roots.
    files_touched: [src/ChordFlow.Core/Instruments/Guitar/Caged/CagedVoicingCatalog.cs, src/ChordFlow.Core/Features/Voicings/EngineVoicingSource.cs, src/ChordFlow.Desktop/wwwroot/caged-chords.js, tests/ChordFlow.Core.Tests/EngineVoicingCoverageTests.cs, tests/ChordFlow.Core.Tests/AutomaticVoicingDocTests.cs]
    blocked_by: [qualities-major6-minor6, eshape-skip5-stretchback]
    satisfies: [IN5, IN7, C4]
  - id: eshape-behind1-anchor-middle
    order: 6
    status: done
    description: "E-shape behind-1 anchor finger → middle: `AnchorFinger.Derive` gains a stretch-back branch (index pinned to the low-edge stretch fret, fingers count up one-per-fret), gated in `CagedDerivation` to the E-shape behind-1 case (A/D dim7 keep Index); m7b5 E & dim7 E fixtures back to `anchor:m`."
    files_touched: [src/ChordFlow.Core/Instruments/Guitar/Caged/AnchorFinger.cs, src/ChordFlow.Core/Instruments/Guitar/Caged/CagedDerivation.cs, tests/ChordFlow.Core.Tests/fixtures/caged-oracle/m7b5_eshape.dsl, tests/ChordFlow.Core.Tests/fixtures/caged-oracle/dim7_eshape.dsl]
    blocked_by: [eshape-skip5-stretchback]
    satisfies: [IN11]
  - id: sixth-chordtone-function
    order: 7
    status: done
    description: "Quality-resolved chord-tone labels: add `ChordToneFunction.Sixth`; classify chord tones from the `QualityFormulas` degree (degree 6 → Sixth, 7 → Seventh) so semitone 9 reads `6` for 6/m6 and `bb7` for dim7; `IntervalSpeller.Label` Sixth → `\"6\"`; `fretboard-render-component.js` gains a `sixth` colour (`#f59e0b`) + legend; update ChordTones/diagram tests."
    files_touched: [src/ChordFlow.Core/Music/Harmony/ChordTone.cs, src/ChordFlow.Core/Music/Harmony/ChordTones.cs, src/ChordFlow.Core/Music/Harmony/IntervalSpeller.cs, src/ChordFlow.Core/Instruments/Guitar/Diagrams/ChordShapeDiagram.cs, src/ChordFlow.Desktop/wwwroot/fretboard-render-component.js, tests/ChordFlow.Core.Tests/ChordTonesTests.cs]
    blocked_by: [qualities-major6-minor6]
    satisfies: [IN10, C6]
  - id: visual-check-6-m6
    order: 8
    status: done
    description: "Dogfood: run the app, render 6 and m6 across all CAGED shapes on the fretboard page; Rafa reviews the derived grips (E grips against the known `8 x 7 9 8 8` / `8 x 7 8 8 8`, other shapes by eye). STOP for his blessing before capture."
    files_touched: [src/ChordFlow.Desktop/wwwroot/caged-chords.js]
    blocked_by: [catalog-ui-6-m6, update-m7b5-dim7-eshape-oracle, eshape-behind1-anchor-middle, sixth-chordtone-function]
    satisfies: [C5]
  - id: capture-6-m6-oracle
    order: 9
    status: done
    description: "Capture the confirmed 6/m6 grips into the golden oracle: new `fixtures/caged-oracle/*.dsl`, added `CagedDerivationOracleTests.Authored` rows, and bumped fixture/`ExpectedVoicingCount` assertions."
    files_touched: [tests/ChordFlow.Core.Tests/CagedDerivationOracleTests.cs, tests/ChordFlow.Core.Tests/CagedOracleVoicingsTests.cs]
    blocked_by: [visual-check-6-m6]
    satisfies: [IN8, C5]
  - id: ref-sync-domain-model
    order: 10
    status: done
    description: "Sync `chordflow-domain-model-reference.md`: the two new qualities (Major6/Minor6 + formulas), the E-shape skip-string-5 + behind-1 stretch-back derivation rule, the E-shape behind-1 anchor=middle rule, and the new `ChordToneFunction.Sixth` + formula-degree chord-tone classification."
    files_touched: [loom/refs/chordflow-domain-model-reference.md]
    blocked_by: [capture-6-m6-oracle]
    satisfies: [IN9, IN10, IN11]
---
# Derive CAGED 6th voicings — implementation

## Goal

Extend the CAGED derivation engine to spell major-6 and minor-6 grips, and add the E-shape voicing tweak (mute string 5 + behind-1 index stretch-back) for the four string-5-awkward qualities (m7b5, dim7, 6, m6). Built bottom-up: the domain precursor (Major6/Minor6 quality + formulas) first, then the engine's E-shape exception, then the m7b5/dim7 E oracle update that proves the tweak, then the voicing-DSL suffix + catalog/UI wiring that makes 6/m6 derivable and visible on the fretboard dogfood page; a visual-review stop, then capture-to-oracle of the confirmed 6/m6 grips, and finally the domain-model ref-sync. Per req rq_01KW2G9V1TYQB3PVKYAEFE5ZFG (idea id_01KVYRNSY7JPFKF3TWYKJYWH6V).

---

## Steps

| Done | # | Step | Files touched | Blocked by | Satisfies |
|---|---|---|---|---|---|
| ✅ | 1 | Add `Quality.Major6`/`Minor6` and their `QualityFormulas` rows `"1 3 5 6"` / `"1 b3 5 6"`; add the derived `{0,4,7,9}`/`{0,3,7,9}` cases to QualityFormulasTests + ChordTonesTests. | src/ChordFlow.Core/Music/Harmony/Quality.cs, src/ChordFlow.Core/Music/Harmony/QualityFormulas.cs, tests/ChordFlow.Core.Tests/QualityFormulasTests.cs, tests/ChordFlow.Core.Tests/ChordTonesTests.cs | — | IN1, IN2, C3 |
| ✅ | 2 | `CagedDerivation`: add the E-shape exception for `{HalfDiminished7, Diminished7, Major6, Minor6}` — mute string 5 in candidate building and grant the index behind-1 stretch-back (window low edge = bassFret−1). Keep Diminished7's existing un-gated stretch-back. | src/ChordFlow.Core/Instruments/Guitar/Caged/CagedDerivation.cs | qualities-major6-minor6 | IN3, C1, C2 |
| ✅ | 3 | Update the m7b5 & dim7 E golden-oracle grips to `8 x 8 8 7 8` / `8 x 7 8 7 8` in `CagedDerivationOracleTests.Authored` and the two fixture `.dsl` files; confirm derived == authored for all oracle cells. | tests/ChordFlow.Core.Tests/CagedDerivationOracleTests.cs, tests/ChordFlow.Core.Tests/fixtures/caged-oracle/m7b5_eshape.dsl, tests/ChordFlow.Core.Tests/fixtures/caged-oracle/dim7_eshape.dsl | eshape-skip5-stretchback | IN4 |
| ✅ | 4 | `VoicingDslParser`: accept `6` → `Major6` and `m6`/`-6` → `Minor6` quality suffixes; add parser tests. | src/ChordFlow.Core/Instruments/Guitar/Voicings/VoicingDslParser.cs, tests/ChordFlow.Core.Tests/VoicingDslParserTests.cs | qualities-major6-minor6 | IN6 |
| ✅ | 5 | Add `Major6`/`Minor6` to `CagedVoicingCatalog` as five-shape qualities; add their `EngineVoicingSource.DisplayNames` and `caged-chords.js` `QUALITIES` entries ("6"/"m6"); update coverage/count tests to expect the new families derive no-throw across all shapes and roots. | src/ChordFlow.Core/Instruments/Guitar/Caged/CagedVoicingCatalog.cs, src/ChordFlow.Core/Features/Voicings/EngineVoicingSource.cs, src/ChordFlow.Desktop/wwwroot/caged-chords.js, tests/ChordFlow.Core.Tests/EngineVoicingCoverageTests.cs, tests/ChordFlow.Core.Tests/AutomaticVoicingDocTests.cs | qualities-major6-minor6, eshape-skip5-stretchback | IN5, IN7, C4 |
| ✅ | 6 | E-shape behind-1 anchor finger → middle: `AnchorFinger.Derive` gains a stretch-back branch (index pinned to the low-edge stretch fret, fingers count up one-per-fret), gated in `CagedDerivation` to the E-shape behind-1 case (A/D dim7 keep Index); m7b5 E & dim7 E fixtures back to `anchor:m`. | src/ChordFlow.Core/Instruments/Guitar/Caged/AnchorFinger.cs, src/ChordFlow.Core/Instruments/Guitar/Caged/CagedDerivation.cs, tests/ChordFlow.Core.Tests/fixtures/caged-oracle/m7b5_eshape.dsl, tests/ChordFlow.Core.Tests/fixtures/caged-oracle/dim7_eshape.dsl | eshape-skip5-stretchback | IN11 |
| ✅ | 7 | Quality-resolved chord-tone labels: add `ChordToneFunction.Sixth`; classify chord tones from the `QualityFormulas` degree (degree 6 → Sixth, 7 → Seventh) so semitone 9 reads `6` for 6/m6 and `bb7` for dim7; `IntervalSpeller.Label` Sixth → `"6"`; `fretboard-render-component.js` gains a `sixth` colour (`#f59e0b`) + legend; update ChordTones/diagram tests. | src/ChordFlow.Core/Music/Harmony/ChordTone.cs, src/ChordFlow.Core/Music/Harmony/ChordTones.cs, src/ChordFlow.Core/Music/Harmony/IntervalSpeller.cs, src/ChordFlow.Core/Instruments/Guitar/Diagrams/ChordShapeDiagram.cs, src/ChordFlow.Desktop/wwwroot/fretboard-render-component.js, tests/ChordFlow.Core.Tests/ChordTonesTests.cs | qualities-major6-minor6 | IN10, C6 |
| ✅ | 8 | Dogfood: run the app, render 6 and m6 across all CAGED shapes on the fretboard page; Rafa reviews the derived grips (E grips against the known `8 x 7 9 8 8` / `8 x 7 8 8 8`, other shapes by eye). STOP for his blessing before capture. | src/ChordFlow.Desktop/wwwroot/caged-chords.js | catalog-ui-6-m6, update-m7b5-dim7-eshape-oracle, eshape-behind1-anchor-middle, sixth-chordtone-function | C5 |
| ✅ | 9 | Capture the confirmed 6/m6 grips into the golden oracle: new `fixtures/caged-oracle/*.dsl`, added `CagedDerivationOracleTests.Authored` rows, and bumped fixture/`ExpectedVoicingCount` assertions. | tests/ChordFlow.Core.Tests/CagedDerivationOracleTests.cs, tests/ChordFlow.Core.Tests/CagedOracleVoicingsTests.cs | visual-check-6-m6 | IN8, C5 |
| ✅ | 10 | Sync `chordflow-domain-model-reference.md`: the two new qualities (Major6/Minor6 + formulas), the E-shape skip-string-5 + behind-1 stretch-back derivation rule, the E-shape behind-1 anchor=middle rule, and the new `ChordToneFunction.Sixth` + formula-degree chord-tone classification. | loom/refs/chordflow-domain-model-reference.md | capture-6-m6-oracle | IN9, IN10, IN11 |
---

### Legend

| Symbol | Meaning |
|--------|---------|
| ✅ | Done |
| 🔄 | In Progress |
| 🔳 | Pending |
| ❌ | Cancelled |

<!-- step:qualities-major6-minor6 -->
### Step 1 — qualities-major6-minor6

QualityIntervals derives the semitones from the formulas automatically. Verify FromIntervals still resolves uniquely (both sets are distinct from every existing quality).

<!-- step:eshape-skip5-stretchback -->
### Step 2 — eshape-skip5-stretchback

Gate strictly on `shape == CagedShape.E` so A/D/C/G derivations stay byte-identical (m7b5 A/D oracle must not regress). The skip-string-5 set and the stretch-back set are the same four qualities.

<!-- step:update-m7b5-dim7-eshape-oracle -->
### Step 3 — update-m7b5-dim7-eshape-oracle

This step is the proof that the two-toggle tweak reproduces Rafa's grips. Find the exact fixture filenames under fixtures/caged-oracle/ (stems feed the oracle ids).

<!-- step:voicing-dsl-6-m6-suffixes -->
### Step 4 — voicing-dsl-6-m6-suffixes

Needed so the 6/m6 oracle fixtures (step capture-6-m6-oracle) can be authored. Voicing-DSL only — no ProgressionParser change (EX1).

<!-- step:catalog-ui-6-m6 -->
### Step 5 — catalog-ui-6-m6

Five-shape like maj/min (caged-c-full: only m7b5/dim7/aug trim). Catalog grows 36→46. Verify no shape throws for 6/m6 across all 12 roots — any throw is a finding to surface.

<!-- step:visual-check-6-m6 -->
### Step 8 — visual-check-6-m6

Human verification gate — no code change beyond running the app. The next step depends on which grips Rafa confirms.

<!-- step:capture-6-m6-oracle -->
### Step 9 — capture-6-m6-oracle

Scope of captured shapes follows Rafa's review (E known; others per his call). Add fixture .dsl files using the new 6/m6 voicing-DSL suffixes.

<!-- step:ref-sync-domain-model -->
### Step 10 — ref-sync-domain-model

Reference-doc sync is part of the same unit of work (the contract's mandatory bidirectional ref rule).
