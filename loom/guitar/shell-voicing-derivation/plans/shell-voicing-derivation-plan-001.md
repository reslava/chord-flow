---
type: plan
id: pl_01KW4G5AP5V1K4P17M0CB03118
title: Shell voicing derivation — implementation (v2, 2-form derivation)
status: done
created: 2026-06-27
updated: 2026-06-27
version: 1
design_version: 1
req_version: 2
tags: []
parent_id: de_01KW481X6B93Y3QPX8G2E7XMP2
requires_load: []
target_version: 0.1.0
steps:
  - id: voicingfamily-shellreduction-doubled-shell
    order: 1
    status: done
    description: "VoicingFamily enum (Caged/DoubledShell/Shell + tokens caged/dshell/shell) and ShellReduction.MuteFifth(ChordShape): mutes the strings whose chord-tone function is the fifth (via ChordTones/QualityFormulas), keeps root/3rd/7th/6th incl. doublings, never repacks. Pure. Unit tests."
    files_touched: [src/ChordFlow.Core/Instruments/Guitar/Caged/VoicingFamily.cs, src/ChordFlow.Core/Instruments/Guitar/Caged/ShellReduction.cs, tests/ChordFlow.Core.Tests/ShellReductionTests.cs]
    blocked_by: []
    satisfies: [IN1, IN2, IN3, IN4, C1, C3, C5]
  - id: shellderivation-2-form-compact-shell
    order: 2
    status: done
    description: "ShellDerivation.Derive(quality, CagedShape form C|E, root, minFret, maxFret) -> ChordShape: the 2-form compact-shell deriver. Root on s5 (C) / s6 (E); guide tones on s4+s3 (C: s4=3rd,s3=7th|6th; E: s4=7th|6th,s3=3rd); each guide tone at the occurrence nearest the root fret. Reuses IntervalLattice/OctaveShape/QualityFormulas, no authored frets. Pure. Unit tests."
    files_touched: [src/ChordFlow.Core/Instruments/Guitar/Caged/ShellDerivation.cs, tests/ChordFlow.Core.Tests/ShellDerivationTests.cs]
    blocked_by: []
    satisfies: [IN13, IN3, C1, C3, C5]
  - id: 4-segment-automaticvoicingid
    order: 3
    status: done
    description: "Extend AutomaticVoicingId to 4-segment auto:{family}:{token}:{shape}; shell shape segment in {C,E}; remove the 3-segment form; TryParse requires 4 segments. Tests round-trip every family and reject the old form."
    files_touched: [src/ChordFlow.Core/Instruments/Guitar/Caged/AutomaticVoicingId.cs, tests/ChordFlow.Core.Tests/AutomaticVoicingIdTests.cs]
    blocked_by: []
    satisfies: [IN5]
  - id: catalog-family-dimension
    order: 4
    status: done
    description: "CagedVoicingCatalog.Combos carries (VoicingFamily, Quality, CagedShape) with ShapesFor(family, quality): caged over all 46; doubled-shell over the 7th/6th qualities x their CAGED shapes; shell over the 7th/6th qualities x {C,E}. One source of truth for listing + resolver + coverage. Tests."
    files_touched: [src/ChordFlow.Core/Instruments/Guitar/Caged/CagedVoicingCatalog.cs, tests/ChordFlow.Core.Tests/CagedVoicingCatalogTests.cs]
    blocked_by: [family-reduction]
    satisfies: [IN6, C7]
  - id: resolver-family-dispatch-knob-caged-fallback
    order: 5
    status: done
    description: "Add Family to VoicingSource (RenderOptions), default caged. CompingResolver dispatches per family (caged->Derive; doubled-shell->Derive+MuteFifth; shell->ShellDerivation) and falls back to the caged family for a chord whose quality has no shell, before the source fallback chain. Tests: each family comps correctly; triad under shell falls back to caged; Family=caged unchanged."
    files_touched: [src/ChordFlow.Core/Rendering/RenderOptions.cs, src/ChordFlow.Core/Features/Voicings/CompingResolver.cs, tests/ChordFlow.Core.Tests/CompingResolverTests.cs]
    blocked_by: [family-reduction, shell-derivation, catalog]
    satisfies: [IN7, C2, C4]
  - id: listing-source-family-rows
    order: 6
    status: done
    description: EngineVoicingSource lists the family rows with family-qualified display names (e.g. 'Dominant 7 (shell) - E shape'). No common/extended (dropped, EX6). Tests.
    files_touched: [src/ChordFlow.Core/Features/Voicings/EngineVoicingSource.cs, tests/ChordFlow.Core.Tests/EngineVoicingSourceTests.cs]
    blocked_by: [identity, catalog]
    satisfies: [IN8, C7]
  - id: shell-golden-oracle-coverage-tests
    order: 7
    status: done
    description: "Shell golden oracle: the 12 authored grips (C,E x dom7/min7/maj7/dim7/6/m6, root C) that ShellDerivation must reproduce. m7b5 derived (= min7 grip), validated structurally. Plus doubled-shell structural validation, catalog coverage (every offered (family,quality,shape) resolves, no throw), and the Family=caged no-regression check."
    files_touched: [tests/ChordFlow.Core.Tests/ShellOracleTests.cs, tests/ChordFlow.Core.Tests/VoicingCatalogCoverageTests.cs]
    blocked_by: [shell-derivation, resolver]
    satisfies: [IN14]
  - id: retire-beginnershellstrategy-voicingbook
    order: 8
    status: done
    description: "Retire the legacy strategy path (shells supersede it): remove BeginnerShellStrategy + IVoicingStrategy from production, delete VoicingBook, rewire GuitarInstrument/VoicingStore off it (if a live caller remains it surfaces here)."
    files_touched: [src/ChordFlow.Core/Instruments/Guitar/Voicings/BeginnerShellStrategy.cs, src/ChordFlow.Core/Instruments/Guitar/Voicings/IVoicingStrategy.cs, src/ChordFlow.Core/Instruments/Guitar/Voicings/VoicingBook.cs, src/ChordFlow.Core/Instruments/Guitar/GuitarInstrument.cs, src/ChordFlow.Core/Persistence/VoicingStore.cs]
    blocked_by: [oracle-tests]
    satisfies: [IN9]
  - id: ref-sync-domain-model
    order: 9
    status: done
    description: "Ref-sync in the same unit of work: add ShellDerivation / ShellReduction / VoicingFamily and the family dimension on the automatic voicing pipeline to the domain-model reference."
    files_touched: [loom/refs/chordflow-domain-model-reference.md]
    blocked_by: [resolver]
    satisfies: [IN11]
  - id: dogfood-fretboard-render
    order: 10
    status: done
    description: "Dogfood: render the derived shell families on the fretboard UI page (guitar-weave dogfood rule) - visually confirm the shell + doubled-shell grips before building difficulty bands on top."
    files_touched: [src/ChordFlow.Desktop/wwwroot/caged-chords.js]
    blocked_by: [resolver]
    satisfies: [IN12]
---
# Shell voicing derivation — implementation (v2, 2-form derivation)

## Goal

Add shell voicings as `automatic` voicing families on the post-flip pipeline, per the chat-001 pivot. `shell` is a new 2-form compact-shell derivation (`ShellDerivation`: 5th-string-root `C` and 6th-string-root `E`, guide tones on s4+s3, each at the occurrence nearest the root), verified by the 12-grip authored golden oracle; `doubled-shell` is the chord-minus-5th reduction (`ShellReduction.MuteFifth`) inheriting the CAGED oracle; `caged` is the existing full chord. The family dimension threads through `AutomaticVoicingId` (4-segment, shell shape ∈ {C,E}), `CagedVoicingCatalog`, the `EngineVoicingSource` listing, and `CompingResolver` (a `Family` knob on `VoicingSource`, default `caged` for zero regression, with a `caged` fallback for chords with no shell). The legacy `BeginnerShellStrategy`/`IVoicingStrategy`/`VoicingBook` path is retired. Validated by `ShellDerivation`/`ShellReduction` unit tests, the 12-grip shell oracle, doubled-shell structural + catalog coverage + `Family=caged` no-regression tests, the domain-model ref-sync, and the fretboard dogfood. Implements the locked req v2 (IN1–IN14 / C1–C7).

---

## Steps

| Done | # | Step | Files touched | Blocked by | Satisfies |
|---|---|---|---|---|---|
| ✅ | 1 | VoicingFamily enum (Caged/DoubledShell/Shell + tokens caged/dshell/shell) and ShellReduction.MuteFifth(ChordShape): mutes the strings whose chord-tone function is the fifth (via ChordTones/QualityFormulas), keeps root/3rd/7th/6th incl. doublings, never repacks. Pure. Unit tests. | src/ChordFlow.Core/Instruments/Guitar/Caged/VoicingFamily.cs, src/ChordFlow.Core/Instruments/Guitar/Caged/ShellReduction.cs, tests/ChordFlow.Core.Tests/ShellReductionTests.cs | — | IN1, IN2, IN3, IN4, C1, C3, C5 |
| ✅ | 2 | ShellDerivation.Derive(quality, CagedShape form C\|E, root, minFret, maxFret) -> ChordShape: the 2-form compact-shell deriver. Root on s5 (C) / s6 (E); guide tones on s4+s3 (C: s4=3rd,s3=7th\|6th; E: s4=7th\|6th,s3=3rd); each guide tone at the occurrence nearest the root fret. Reuses IntervalLattice/OctaveShape/QualityFormulas, no authored frets. Pure. Unit tests. | src/ChordFlow.Core/Instruments/Guitar/Caged/ShellDerivation.cs, tests/ChordFlow.Core.Tests/ShellDerivationTests.cs | — | IN13, IN3, C1, C3, C5 |
| ✅ | 3 | Extend AutomaticVoicingId to 4-segment auto:{family}:{token}:{shape}; shell shape segment in {C,E}; remove the 3-segment form; TryParse requires 4 segments. Tests round-trip every family and reject the old form. | src/ChordFlow.Core/Instruments/Guitar/Caged/AutomaticVoicingId.cs, tests/ChordFlow.Core.Tests/AutomaticVoicingIdTests.cs | — | IN5 |
| ✅ | 4 | CagedVoicingCatalog.Combos carries (VoicingFamily, Quality, CagedShape) with ShapesFor(family, quality): caged over all 46; doubled-shell over the 7th/6th qualities x their CAGED shapes; shell over the 7th/6th qualities x {C,E}. One source of truth for listing + resolver + coverage. Tests. | src/ChordFlow.Core/Instruments/Guitar/Caged/CagedVoicingCatalog.cs, tests/ChordFlow.Core.Tests/CagedVoicingCatalogTests.cs | family-reduction | IN6, C7 |
| ✅ | 5 | Add Family to VoicingSource (RenderOptions), default caged. CompingResolver dispatches per family (caged->Derive; doubled-shell->Derive+MuteFifth; shell->ShellDerivation) and falls back to the caged family for a chord whose quality has no shell, before the source fallback chain. Tests: each family comps correctly; triad under shell falls back to caged; Family=caged unchanged. | src/ChordFlow.Core/Rendering/RenderOptions.cs, src/ChordFlow.Core/Features/Voicings/CompingResolver.cs, tests/ChordFlow.Core.Tests/CompingResolverTests.cs | family-reduction, shell-derivation, catalog | IN7, C2, C4 |
| ✅ | 6 | EngineVoicingSource lists the family rows with family-qualified display names (e.g. 'Dominant 7 (shell) - E shape'). No common/extended (dropped, EX6). Tests. | src/ChordFlow.Core/Features/Voicings/EngineVoicingSource.cs, tests/ChordFlow.Core.Tests/EngineVoicingSourceTests.cs | identity, catalog | IN8, C7 |
| ✅ | 7 | Shell golden oracle: the 12 authored grips (C,E x dom7/min7/maj7/dim7/6/m6, root C) that ShellDerivation must reproduce. m7b5 derived (= min7 grip), validated structurally. Plus doubled-shell structural validation, catalog coverage (every offered (family,quality,shape) resolves, no throw), and the Family=caged no-regression check. | tests/ChordFlow.Core.Tests/ShellOracleTests.cs, tests/ChordFlow.Core.Tests/VoicingCatalogCoverageTests.cs | shell-derivation, resolver | IN14 |
| ✅ | 8 | Retire the legacy strategy path (shells supersede it): remove BeginnerShellStrategy + IVoicingStrategy from production, delete VoicingBook, rewire GuitarInstrument/VoicingStore off it (if a live caller remains it surfaces here). | src/ChordFlow.Core/Instruments/Guitar/Voicings/BeginnerShellStrategy.cs, src/ChordFlow.Core/Instruments/Guitar/Voicings/IVoicingStrategy.cs, src/ChordFlow.Core/Instruments/Guitar/Voicings/VoicingBook.cs, src/ChordFlow.Core/Instruments/Guitar/GuitarInstrument.cs, src/ChordFlow.Core/Persistence/VoicingStore.cs | oracle-tests | IN9 |
| ✅ | 9 | Ref-sync in the same unit of work: add ShellDerivation / ShellReduction / VoicingFamily and the family dimension on the automatic voicing pipeline to the domain-model reference. | loom/refs/chordflow-domain-model-reference.md | resolver | IN11 |
| ✅ | 10 | Dogfood: render the derived shell families on the fretboard UI page (guitar-weave dogfood rule) - visually confirm the shell + doubled-shell grips before building difficulty bands on top. | src/ChordFlow.Desktop/wwwroot/caged-chords.js | resolver | IN12 |
---

### Legend

| Symbol | Meaning |
|--------|---------|
| ✅ | Done |
| 🔄 | In Progress |
| 🔳 | Pending |
| ❌ | Cancelled |
