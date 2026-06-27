---
type: plan
id: pl_01KW48GMEPS145SSTJZ4ARRY3Q
title: Shell voicing derivation — implementation
status: implementing
created: 2026-06-27
updated: 2026-06-27
version: 1
design_version: 1
req_version: 1
tags: []
parent_id: de_01KW481X6B93Y3QPX8G2E7XMP2
requires_load: []
target_version: 0.1.0
steps:
  - id: voicingfamily-shellreduction
    order: 1
    status: pending
    description: "VoicingFamily enum (Caged/DoubledShell/Shell + tokens) and the pure ShellReduction.Reduce(ChordShape, family): caged=identity, doubled-shell mutes the quality's fifth (from QualityFormulas), shell keeps one string per guide-tone degree (root/3rd/7th|6th, no doublings); mutes dropped strings without repacking; fail-loud if asked to shell a non-7th/6th quality. Unit tests."
    files_touched: [src/ChordFlow.Core/Instruments/Guitar/Caged/VoicingFamily.cs, src/ChordFlow.Core/Instruments/Guitar/Caged/ShellReduction.cs, tests/ChordFlow.Core.Tests/ShellReductionTests.cs]
    blocked_by: []
    satisfies: [IN1, IN2, IN3, IN4, C1, C3, C5]
  - id: 4-segment-automaticvoicingid
    order: 2
    status: pending
    description: "Extend AutomaticVoicingId to the 4-segment form auto:{family}:{token}:{shape}; remove the 3-segment form; TryParse requires 4 segments. Tests round-trip every family/quality/shape and reject the old form."
    files_touched: [src/ChordFlow.Core/Instruments/Guitar/Caged/AutomaticVoicingId.cs, tests/ChordFlow.Core.Tests/AutomaticVoicingIdTests.cs]
    blocked_by: []
    satisfies: [IN5]
  - id: catalog-family-dimension
    order: 3
    status: pending
    description: "CagedVoicingCatalog.Combos carries (VoicingFamily, Quality, CagedShape) with ShapesFor(family, quality): caged over all 46 combos, doubled-shell + shell over the 7th/6th qualities only (triads excluded). One source of truth for listing + resolver + coverage. Tests assert the family×quality offering."
    files_touched: [src/ChordFlow.Core/Instruments/Guitar/Caged/CagedVoicingCatalog.cs, tests/ChordFlow.Core.Tests/CagedVoicingCatalogTests.cs]
    blocked_by: [family-reduction]
    satisfies: [IN3, IN6, C7]
  - id: resolver-family-knob-caged-fallback
    order: 4
    status: pending
    description: "Add Family to VoicingSource (RenderOptions), default caged. CompingResolver.AutomaticCandidates reduces each derived shape to the requested family via ShellReduction, and falls back to the caged family for a chord whose quality has no shell, before the source fallback chain. Tests: shell family comps shells; triad under shell falls back to caged; Family=caged unchanged."
    files_touched: [src/ChordFlow.Core/Rendering/RenderOptions.cs, src/ChordFlow.Core/Features/Voicings/CompingResolver.cs, tests/ChordFlow.Core.Tests/CompingResolverTests.cs]
    blocked_by: [family-reduction, catalog-family-dimension]
    satisfies: [IN7, C2, C4]
  - id: listing-source-derived-common-extended
    order: 5
    status: pending
    description: EngineVoicingSource lists the family rows with family-qualified display names (e.g. 'Dominant 7 (shell) — E shape') and surfaces the derived common/extended classification from CagedShape.FamiliarityRank() (E,C=common; A,G,D=extended), computed never stored. Tests.
    files_touched: [src/ChordFlow.Core/Features/Voicings/EngineVoicingSource.cs, tests/ChordFlow.Core.Tests/EngineVoicingSourceTests.cs]
    blocked_by: [identity-family-segment, catalog-family-dimension]
    satisfies: [IN8, C7]
  - id: regression-oracle-coverage-tests
    order: 6
    status: pending
    description: "Regression oracle: copy BeginnerShellStrategy's 3-quality grip logic into a test fixture and assert the A-shape shell for dom7/min7/maj7 matches it. Catalog coverage: every (family, quality, shape) offered resolves to a valid, fully-spelled grip with no throw. Family=caged no-regression check."
    files_touched: [tests/ChordFlow.Core.Tests/ShellRegressionOracleTests.cs, tests/ChordFlow.Core.Tests/VoicingCatalogCoverageTests.cs]
    blocked_by: [family-reduction, catalog-family-dimension, resolver-family-knob]
    satisfies: [IN10]
  - id: retire-beginnershellstrategy-voicingbook
    order: 7
    status: pending
    description: "Retire the legacy strategy path now that shells supersede it and the oracle preserves its logic: remove BeginnerShellStrategy + IVoicingStrategy from production, delete VoicingBook, and rewire GuitarInstrument/VoicingStore off it (if a live caller remains it surfaces here)."
    files_touched: [src/ChordFlow.Core/Instruments/Guitar/Voicings/BeginnerShellStrategy.cs, src/ChordFlow.Core/Instruments/Guitar/Voicings/IVoicingStrategy.cs, src/ChordFlow.Core/Instruments/Guitar/Voicings/VoicingBook.cs, src/ChordFlow.Core/Instruments/Guitar/GuitarInstrument.cs, src/ChordFlow.Core/Persistence/VoicingStore.cs]
    blocked_by: [oracle-coverage-tests]
    satisfies: [IN9]
  - id: ref-sync-domain-model
    order: 8
    status: pending
    description: "Ref-sync in the same unit of work: add ShellReduction / VoicingFamily and the family dimension on the automatic voicing pipeline to the domain-model reference."
    files_touched: [loom/refs/chordflow-domain-model-reference.md]
    blocked_by: [resolver-family-knob]
    satisfies: [IN11]
  - id: dogfood-fretboard-render
    order: 9
    status: pending
    description: "Dogfood: render the derived shell families on the fretboard UI page (guitar-weave dogfood rule) — visually confirm the shell/doubled-shell grips per CAGED shape before building difficulty bands on top."
    files_touched: [src/ChordFlow.Desktop/wwwroot/caged-chords.js]
    blocked_by: [resolver-family-knob]
    satisfies: [IN12]
---
# Shell voicing derivation — implementation

## Goal

Add shell voicings as `automatic` voicing families by reducing the engine-derived `ChordShape` — no new spelling. A pure `ShellReduction.Reduce(ChordShape, VoicingFamily)` mutes the fifth (`doubled-shell`) or keeps one string per guide-tone degree (`shell`), classifying degrees via `QualityFormulas`; shell families are offered only for 7th/6th qualities (triads stay `caged`). The family dimension threads through `AutomaticVoicingId` (4-segment ids), `CagedVoicingCatalog`, the `EngineVoicingSource` listing (+ derived common/extended), and `CompingResolver` (a `Family` knob on `VoicingSource`, defaulting to `caged` for zero regression, with a `caged` fallback for chords that have no shell). The legacy `BeginnerShellStrategy`/`IVoicingStrategy`/`VoicingBook` path is retired, its 3-grip logic surviving only as the A-shape-shell regression oracle. Validated by reduction unit tests, the regression oracle, a catalog coverage test, the `Family=caged` no-regression check, the domain-model ref-sync, and the fretboard dogfood page. Implements the locked req (IN1–IN12 / C1–C7); builds on the shipped engine-derived-as-app-source + caged-sixth-voicings.

---

## Steps

| Done | # | Step | Files touched | Blocked by | Satisfies |
|---|---|---|---|---|---|
| 🔳 | 1 | VoicingFamily enum (Caged/DoubledShell/Shell + tokens) and the pure ShellReduction.Reduce(ChordShape, family): caged=identity, doubled-shell mutes the quality's fifth (from QualityFormulas), shell keeps one string per guide-tone degree (root/3rd/7th\|6th, no doublings); mutes dropped strings without repacking; fail-loud if asked to shell a non-7th/6th quality. Unit tests. | src/ChordFlow.Core/Instruments/Guitar/Caged/VoicingFamily.cs, src/ChordFlow.Core/Instruments/Guitar/Caged/ShellReduction.cs, tests/ChordFlow.Core.Tests/ShellReductionTests.cs | — | IN1, IN2, IN3, IN4, C1, C3, C5 |
| 🔳 | 2 | Extend AutomaticVoicingId to the 4-segment form auto:{family}:{token}:{shape}; remove the 3-segment form; TryParse requires 4 segments. Tests round-trip every family/quality/shape and reject the old form. | src/ChordFlow.Core/Instruments/Guitar/Caged/AutomaticVoicingId.cs, tests/ChordFlow.Core.Tests/AutomaticVoicingIdTests.cs | — | IN5 |
| 🔳 | 3 | CagedVoicingCatalog.Combos carries (VoicingFamily, Quality, CagedShape) with ShapesFor(family, quality): caged over all 46 combos, doubled-shell + shell over the 7th/6th qualities only (triads excluded). One source of truth for listing + resolver + coverage. Tests assert the family×quality offering. | src/ChordFlow.Core/Instruments/Guitar/Caged/CagedVoicingCatalog.cs, tests/ChordFlow.Core.Tests/CagedVoicingCatalogTests.cs | family-reduction | IN3, IN6, C7 |
| 🔳 | 4 | Add Family to VoicingSource (RenderOptions), default caged. CompingResolver.AutomaticCandidates reduces each derived shape to the requested family via ShellReduction, and falls back to the caged family for a chord whose quality has no shell, before the source fallback chain. Tests: shell family comps shells; triad under shell falls back to caged; Family=caged unchanged. | src/ChordFlow.Core/Rendering/RenderOptions.cs, src/ChordFlow.Core/Features/Voicings/CompingResolver.cs, tests/ChordFlow.Core.Tests/CompingResolverTests.cs | family-reduction, catalog-family-dimension | IN7, C2, C4 |
| 🔳 | 5 | EngineVoicingSource lists the family rows with family-qualified display names (e.g. 'Dominant 7 (shell) — E shape') and surfaces the derived common/extended classification from CagedShape.FamiliarityRank() (E,C=common; A,G,D=extended), computed never stored. Tests. | src/ChordFlow.Core/Features/Voicings/EngineVoicingSource.cs, tests/ChordFlow.Core.Tests/EngineVoicingSourceTests.cs | identity-family-segment, catalog-family-dimension | IN8, C7 |
| 🔳 | 6 | Regression oracle: copy BeginnerShellStrategy's 3-quality grip logic into a test fixture and assert the A-shape shell for dom7/min7/maj7 matches it. Catalog coverage: every (family, quality, shape) offered resolves to a valid, fully-spelled grip with no throw. Family=caged no-regression check. | tests/ChordFlow.Core.Tests/ShellRegressionOracleTests.cs, tests/ChordFlow.Core.Tests/VoicingCatalogCoverageTests.cs | family-reduction, catalog-family-dimension, resolver-family-knob | IN10 |
| 🔳 | 7 | Retire the legacy strategy path now that shells supersede it and the oracle preserves its logic: remove BeginnerShellStrategy + IVoicingStrategy from production, delete VoicingBook, and rewire GuitarInstrument/VoicingStore off it (if a live caller remains it surfaces here). | src/ChordFlow.Core/Instruments/Guitar/Voicings/BeginnerShellStrategy.cs, src/ChordFlow.Core/Instruments/Guitar/Voicings/IVoicingStrategy.cs, src/ChordFlow.Core/Instruments/Guitar/Voicings/VoicingBook.cs, src/ChordFlow.Core/Instruments/Guitar/GuitarInstrument.cs, src/ChordFlow.Core/Persistence/VoicingStore.cs | oracle-coverage-tests | IN9 |
| 🔳 | 8 | Ref-sync in the same unit of work: add ShellReduction / VoicingFamily and the family dimension on the automatic voicing pipeline to the domain-model reference. | loom/refs/chordflow-domain-model-reference.md | resolver-family-knob | IN11 |
| 🔳 | 9 | Dogfood: render the derived shell families on the fretboard UI page (guitar-weave dogfood rule) — visually confirm the shell/doubled-shell grips per CAGED shape before building difficulty bands on top. | src/ChordFlow.Desktop/wwwroot/caged-chords.js | resolver-family-knob | IN12 |
---

### Legend

| Symbol | Meaning |
|--------|---------|
| ✅ | Done |
| 🔄 | In Progress |
| 🔳 | Pending |
| ❌ | Cancelled |
