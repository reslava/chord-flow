---
type: plan
id: pl_01KY23FEVHPMCP0MWAN56K2AQ3
title: Phase 2 rework — Pattern strategy as bar-pattern kinds
status: done
created: 2026-07-21
updated: 2026-07-21
version: 1
design_version: 2
req_version: 4
tags: []
parent_id: de_01KY0RDXS9C7X93BX8Y1HVCMC3
requires_load: []
target_version: 0.1.0
steps:
  - id: rhythmkind-generated-families-figure-catalog
    order: 1
    status: done
    description: "Bar-pattern kinds (Core): a `RhythmKind` = an ordered set of bar patterns (each an OnsetBar). Generated families enumerated by rule — density (quarter/eighth by onset count 1..4) and eighth placement (on-beat / off-beat(&) / both). A curated **named-figure catalog** (GrooveFigures) with the ~16 figures from design §3a (Four-on-floor, Downbeats, Backbeat, Straight-8ths, Offbeats, Charleston, Reverse-Charleston, Tresillo, Cinquillo, Dotted-push, Habanera, Son/Rumba/Bossa clave 2-bar). Replace RhythmFamily. Unit tests: enumeration counts (2-onset quarters = 6, etc.), figure cell placements."
    files_touched: [src/ChordFlow.Core/Music/Rhythm/Generation/RhythmKind.cs, src/ChordFlow.Core/Music/Rhythm/Generation/GrooveFigures.cs, tests/ChordFlow.Core.Tests/Rhythm/Generation/RhythmKindTests.cs]
    blocked_by: []
    satisfies: [IN3]
  - id: patternselection-displacetransform-behaviours
    order: 2
    status: done
    description: "Selection + transform (Core): `PatternSelection` (Fixed(index) / Cycle / RandomInKind / FixedPlusRotating) drawing bar patterns from a kind across bars; `DisplaceTransform(cells)` shifting a pattern's onsets later (wrap in-bar); rework `SequenceBehaviour` to the multi-bar layer over bar patterns (RestBar / CallResponse / Sweep). Remove the old `BarOperator` (Isolate/Mask/Accumulate/Thin become density/placement kinds; Displace becomes the transform)."
    files_touched: [src/ChordFlow.Core/Music/Rhythm/Generation/PatternSelection.cs, src/ChordFlow.Core/Music/Rhythm/Generation/DisplaceTransform.cs, src/ChordFlow.Core/Music/Rhythm/Generation/SequenceBehaviour.cs, src/ChordFlow.Core/Music/Rhythm/Generation/BarOperator.cs]
    blocked_by: [rhythmkind-generated-families-figure-catalog]
    satisfies: [IN3]
  - id: patternstrategy-patternparams-rework
    order: 3
    status: done
    description: "Rework PatternStrategy + PatternParams: `PatternParams(Kind, Selection, Behaviours, Displace?, BarCount, Seed)`; PatternStrategy draws bar patterns from the kind via the selection, layers the behaviours, applies the optional Displace → OnsetGrid; deterministic. Rework the Pattern tests in RhythmGeneratorTests (kind selection shapes, Displace, Sweep-over-index, determinism)."
    files_touched: [src/ChordFlow.Core/Music/Rhythm/Generation/PatternStrategy.cs, src/ChordFlow.Core/Music/Rhythm/Generation/PatternParams.cs, tests/ChordFlow.Core.Tests/Rhythm/Generation/RhythmGeneratorTests.cs]
    blocked_by: [patternselection-displacetransform-behaviours]
    satisfies: [IN2, IN3, IN6]
  - id: wire-rhythm-generator-page-pattern-controls
    order: 4
    status: done
    description: "Wire + page controls: RhythmGenerationRequest's Pattern fields change (kind selector {source, subdivision, descriptor/figure-id} + selection + displace + barCount, replacing family/operator/behaviour); RhythmRequestResolver maps them to PatternParams (fail-loud on unknown kind/figure); the handler is unchanged. Rhythm Generator page: a grouped **Kind** picker (Density / Placement / Figures), a **Selection** picker, a **Displace** control, barCount. Update the handler tests."
    files_touched: [src/ChordFlow.Core/Bridge/RhythmGenerateEnvelope.cs, src/ChordFlow.Core/Features/Rhythm/RhythmRequestResolver.cs, src/ChordFlow.Desktop/wwwroot/rhythm-generator.js, tests/ChordFlow.Core.Tests/Rhythm/Generation/RhythmGenerateHandlerTests.cs]
    blocked_by: [patternstrategy-patternparams-rework]
    satisfies: [IN3, IN7]
  - id: update-domain-architecture-references
    order: 5
    status: done
    description: "Ref-doc sync (CLAUDE-LOCAL): domain-model reference — replace the RhythmFamily/BarOperator entries with RhythmKind / GrooveFigures / PatternSelection / DisplaceTransform and the reworked PatternParams; architecture reference — update the Rhythm Generator page's Pattern controls (kind/selection/displace)."
    files_touched: [loom/refs/chordflow-domain-model-reference.md, loom/refs/chordflow-architecture-reference.md]
    blocked_by: [wire-rhythm-generator-page-pattern-controls]
    satisfies: []
  - id: cdp-by-ear-verification-of-kinds
    order: 6
    status: done
    description: "CDP + by-ear verification (app relaunched with the debug port): a density-2 quarter kind now yields varied bars (NOT `x x x x`); Cycle tours a kind; RandomInKind varies; Displace shifts onsets; and the named figures render/play — Rafa listens to confirm the figures sound right, adjusting any catalog cells that don't. Report results."
    files_touched: []
    blocked_by: [wire-rhythm-generator-page-pattern-controls, update-domain-architecture-references]
    satisfies: [IN3, IN7]
---
# Phase 2 rework — Pattern strategy as bar-pattern kinds

## Goal

Refactor the Pattern strategy to the revised bar-pattern model (design §3a v2, req IN3): the generation unit becomes a whole-bar pattern drawn from an enumerable KIND (generated density/placement families + a curated named-figure catalog), composed across bars by a SELECTION (Fixed / Cycle / RandomInKind / FixedPlusRotating) plus the multi-bar behaviours (RestBar / CallResponse / Sweep) and an optional Displace transform. This replaces the original per-beat operators (which collapsed quarters to `x x x x`) and makes rests intrinsic to a pattern. The onset-grid model, both projections, the count overlay, loop, reference pulse, and the Random strategy are unchanged. Closes with a CDP + by-ear verification (figures sound right — Rafa's call).

---

## Steps

| Done | # | Step | Files touched | Blocked by | Satisfies |
|---|---|---|---|---|---|
| ✅ | 1 | Bar-pattern kinds (Core): a `RhythmKind` = an ordered set of bar patterns (each an OnsetBar). Generated families enumerated by rule — density (quarter/eighth by onset count 1..4) and eighth placement (on-beat / off-beat(&) / both). A curated **named-figure catalog** (GrooveFigures) with the ~16 figures from design §3a (Four-on-floor, Downbeats, Backbeat, Straight-8ths, Offbeats, Charleston, Reverse-Charleston, Tresillo, Cinquillo, Dotted-push, Habanera, Son/Rumba/Bossa clave 2-bar). Replace RhythmFamily. Unit tests: enumeration counts (2-onset quarters = 6, etc.), figure cell placements. | src/ChordFlow.Core/Music/Rhythm/Generation/RhythmKind.cs, src/ChordFlow.Core/Music/Rhythm/Generation/GrooveFigures.cs, tests/ChordFlow.Core.Tests/Rhythm/Generation/RhythmKindTests.cs | — | IN3 |
| ✅ | 2 | Selection + transform (Core): `PatternSelection` (Fixed(index) / Cycle / RandomInKind / FixedPlusRotating) drawing bar patterns from a kind across bars; `DisplaceTransform(cells)` shifting a pattern's onsets later (wrap in-bar); rework `SequenceBehaviour` to the multi-bar layer over bar patterns (RestBar / CallResponse / Sweep). Remove the old `BarOperator` (Isolate/Mask/Accumulate/Thin become density/placement kinds; Displace becomes the transform). | src/ChordFlow.Core/Music/Rhythm/Generation/PatternSelection.cs, src/ChordFlow.Core/Music/Rhythm/Generation/DisplaceTransform.cs, src/ChordFlow.Core/Music/Rhythm/Generation/SequenceBehaviour.cs, src/ChordFlow.Core/Music/Rhythm/Generation/BarOperator.cs | rhythmkind-generated-families-figure-catalog | IN3 |
| ✅ | 3 | Rework PatternStrategy + PatternParams: `PatternParams(Kind, Selection, Behaviours, Displace?, BarCount, Seed)`; PatternStrategy draws bar patterns from the kind via the selection, layers the behaviours, applies the optional Displace → OnsetGrid; deterministic. Rework the Pattern tests in RhythmGeneratorTests (kind selection shapes, Displace, Sweep-over-index, determinism). | src/ChordFlow.Core/Music/Rhythm/Generation/PatternStrategy.cs, src/ChordFlow.Core/Music/Rhythm/Generation/PatternParams.cs, tests/ChordFlow.Core.Tests/Rhythm/Generation/RhythmGeneratorTests.cs | patternselection-displacetransform-behaviours | IN2, IN3, IN6 |
| ✅ | 4 | Wire + page controls: RhythmGenerationRequest's Pattern fields change (kind selector {source, subdivision, descriptor/figure-id} + selection + displace + barCount, replacing family/operator/behaviour); RhythmRequestResolver maps them to PatternParams (fail-loud on unknown kind/figure); the handler is unchanged. Rhythm Generator page: a grouped **Kind** picker (Density / Placement / Figures), a **Selection** picker, a **Displace** control, barCount. Update the handler tests. | src/ChordFlow.Core/Bridge/RhythmGenerateEnvelope.cs, src/ChordFlow.Core/Features/Rhythm/RhythmRequestResolver.cs, src/ChordFlow.Desktop/wwwroot/rhythm-generator.js, tests/ChordFlow.Core.Tests/Rhythm/Generation/RhythmGenerateHandlerTests.cs | patternstrategy-patternparams-rework | IN3, IN7 |
| ✅ | 5 | Ref-doc sync (CLAUDE-LOCAL): domain-model reference — replace the RhythmFamily/BarOperator entries with RhythmKind / GrooveFigures / PatternSelection / DisplaceTransform and the reworked PatternParams; architecture reference — update the Rhythm Generator page's Pattern controls (kind/selection/displace). | loom/refs/chordflow-domain-model-reference.md, loom/refs/chordflow-architecture-reference.md | wire-rhythm-generator-page-pattern-controls | — |
| ✅ | 6 | CDP + by-ear verification (app relaunched with the debug port): a density-2 quarter kind now yields varied bars (NOT `x x x x`); Cycle tours a kind; RandomInKind varies; Displace shifts onsets; and the named figures render/play — Rafa listens to confirm the figures sound right, adjusting any catalog cells that don't. Report results. | — | wire-rhythm-generator-page-pattern-controls, update-domain-architecture-references | IN3, IN7 |
---

### Legend

| Symbol | Meaning |
|--------|---------|
| ✅ | Done |
| 🔄 | In Progress |
| 🔳 | Pending |
| ❌ | Cancelled |
