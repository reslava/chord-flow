---
type: plan
id: pl_01KY28RD9KKDQKTCT5W529Y8KY
title: Phase 2 tweaks — flatten strategies, selection indexes, 16 bars, surprise-me
status: done
created: 2026-07-21
updated: 2026-07-21
version: 1
design_version: 3
req_version: 4
tags: []
parent_id: de_01KY0RDXS9C7X93BX8Y1HVCMC3
requires_load: []
target_version: 0.1.0
steps:
  - id: core-selection-indexes-drop-density-16
    order: 1
    status: done
    description: "Core: PatternSelection.Cycle gains a `StartIndex` (bar N = patterns[(StartIndex+N) % count]); FixedPlusRotating gains a second index — `FixedPlusRotating(FixedIndex, RotatingStartIndex)` (even bars = fixed, odd bars = patterns[(RotatingStartIndex + barIndex/2) % count]). Remove the redundant `RhythmKind.Density` factory (callers use `Placement(sub, \"all\", n)`). Raise the PatternStrategy BarCount cap to 1..16. Update the affected unit tests (RhythmKindTests / RhythmGeneratorTests / OnsetGridProjectionTests) to Placement + the new indexes."
    files_touched: [src/ChordFlow.Core/Music/Rhythm/Generation/PatternSelection.cs, src/ChordFlow.Core/Music/Rhythm/Generation/RhythmKind.cs, src/ChordFlow.Core/Music/Rhythm/Generation/PatternStrategy.cs, tests/ChordFlow.Core.Tests/Rhythm/Generation/RhythmKindTests.cs, tests/ChordFlow.Core.Tests/Rhythm/Generation/RhythmGeneratorTests.cs, tests/ChordFlow.Core.Tests/Rhythm/Generation/OnsetGridProjectionTests.cs]
    blocked_by: []
    satisfies: [IN3]
  - id: wire-3-strategies-selection-indexes
    order: 2
    status: done
    description: "Wire: flatten the strategy to figure/pattern/random. RhythmGenerationRequest drops RhythmKindSpec and carries the kind fields directly — `FigureId` (figure) and `Subdivision`/`Region`/`OnsetCount` (pattern=placement); RhythmSelectionSpec gains `RotatingIndex` (Cycle uses Index as start; FixedPlusRotating uses Index + RotatingIndex). RhythmRequestResolver dispatches figure→PatternParams(figure kind) / pattern→PatternParams(Placement) / random→RandomParams, maps the selection indexes, and caps BarCount at 16. Update the handler tests."
    files_touched: [src/ChordFlow.Core/Bridge/RhythmGenerateEnvelope.cs, src/ChordFlow.Core/Features/Rhythm/RhythmRequestResolver.cs, tests/ChordFlow.Core.Tests/Rhythm/Generation/RhythmGenerateHandlerTests.cs]
    blocked_by: [core-selection-indexes-drop-density-16]
    satisfies: [IN3, IN7]
  - id: page-flattened-strategies-indexes-surprise-me
    order: 3
    status: done
    description: "Page: strategy selector = Figure / Pattern / Random (no kind selector). Figure → figure picker; Pattern → subdivision + region (all/on-beat/off-beat) + onset count; both + selection (with a Cycle start index and FixedPlusRotating's two indexes) + behaviours + bars (max 16). Add a **Surprise me** button that randomizes all pattern params (strategy/kind/selection/behaviours/bars) and generates."
    files_touched: [src/ChordFlow.Desktop/wwwroot/rhythm-generator.js]
    blocked_by: [wire-3-strategies-selection-indexes]
    satisfies: [IN7, IN3]
  - id: req-in3-note-ref-sync
    order: 4
    status: done
    description: Amend IN3 (Pattern bars now 1..16; density folded into placement region=all — no separate density kind) and re-lock the req; sync the domain-model reference (Cycle StartIndex, FixedPlusRotating two indexes, Density removed) and the architecture reference (the three-strategy page + Surprise-me button).
    files_touched: [loom/exercises/generated-rhythms-for-practice/req.md, loom/refs/chordflow-domain-model-reference.md, loom/refs/chordflow-architecture-reference.md]
    blocked_by: [page-flattened-strategies-indexes-surprise-me]
    satisfies: [IN3]
  - id: cdp-by-ear-verification
    order: 5
    status: done
    description: "CDP + by-ear verification (app relaunched with the debug port): the three strategies work; a 16-bar Pattern with Cycle tours further; Cycle start index shifts the tour; FixedPlusRotating honours both indexes; Surprise-me produces a valid generation each press; figures still render/play right. Report results."
    files_touched: []
    blocked_by: [page-flattened-strategies-indexes-surprise-me, req-in3-note-ref-sync]
    satisfies: [IN3, IN7]
---
# Phase 2 tweaks — flatten strategies, selection indexes, 16 bars, surprise-me

## Goal

Dogfooding tweaks on the Rhythm Generator (chat-001): drop the redundant Density factory (it equals Placement region=all) and flatten the UI to three top-level strategies — Figure / Pattern (placement family) / Random (no kind selector); raise the Pattern bar cap to 16 (Random content/silence stay 1–4) so Cycle/FixedPlusRotating are interesting; give Cycle a start index and FixedPlusRotating two indexes (fixed + rotating start); and add a "Surprise me" button that randomizes all pattern params. Small Core/wire touches + a page rework; onset model, projections, figures, loop, reference all unchanged.

---

## Steps

| Done | # | Step | Files touched | Blocked by | Satisfies |
|---|---|---|---|---|---|
| ✅ | 1 | Core: PatternSelection.Cycle gains a `StartIndex` (bar N = patterns[(StartIndex+N) % count]); FixedPlusRotating gains a second index — `FixedPlusRotating(FixedIndex, RotatingStartIndex)` (even bars = fixed, odd bars = patterns[(RotatingStartIndex + barIndex/2) % count]). Remove the redundant `RhythmKind.Density` factory (callers use `Placement(sub, "all", n)`). Raise the PatternStrategy BarCount cap to 1..16. Update the affected unit tests (RhythmKindTests / RhythmGeneratorTests / OnsetGridProjectionTests) to Placement + the new indexes. | src/ChordFlow.Core/Music/Rhythm/Generation/PatternSelection.cs, src/ChordFlow.Core/Music/Rhythm/Generation/RhythmKind.cs, src/ChordFlow.Core/Music/Rhythm/Generation/PatternStrategy.cs, tests/ChordFlow.Core.Tests/Rhythm/Generation/RhythmKindTests.cs, tests/ChordFlow.Core.Tests/Rhythm/Generation/RhythmGeneratorTests.cs, tests/ChordFlow.Core.Tests/Rhythm/Generation/OnsetGridProjectionTests.cs | — | IN3 |
| ✅ | 2 | Wire: flatten the strategy to figure/pattern/random. RhythmGenerationRequest drops RhythmKindSpec and carries the kind fields directly — `FigureId` (figure) and `Subdivision`/`Region`/`OnsetCount` (pattern=placement); RhythmSelectionSpec gains `RotatingIndex` (Cycle uses Index as start; FixedPlusRotating uses Index + RotatingIndex). RhythmRequestResolver dispatches figure→PatternParams(figure kind) / pattern→PatternParams(Placement) / random→RandomParams, maps the selection indexes, and caps BarCount at 16. Update the handler tests. | src/ChordFlow.Core/Bridge/RhythmGenerateEnvelope.cs, src/ChordFlow.Core/Features/Rhythm/RhythmRequestResolver.cs, tests/ChordFlow.Core.Tests/Rhythm/Generation/RhythmGenerateHandlerTests.cs | core-selection-indexes-drop-density-16 | IN3, IN7 |
| ✅ | 3 | Page: strategy selector = Figure / Pattern / Random (no kind selector). Figure → figure picker; Pattern → subdivision + region (all/on-beat/off-beat) + onset count; both + selection (with a Cycle start index and FixedPlusRotating's two indexes) + behaviours + bars (max 16). Add a **Surprise me** button that randomizes all pattern params (strategy/kind/selection/behaviours/bars) and generates. | src/ChordFlow.Desktop/wwwroot/rhythm-generator.js | wire-3-strategies-selection-indexes | IN7, IN3 |
| ✅ | 4 | Amend IN3 (Pattern bars now 1..16; density folded into placement region=all — no separate density kind) and re-lock the req; sync the domain-model reference (Cycle StartIndex, FixedPlusRotating two indexes, Density removed) and the architecture reference (the three-strategy page + Surprise-me button). | loom/exercises/generated-rhythms-for-practice/req.md, loom/refs/chordflow-domain-model-reference.md, loom/refs/chordflow-architecture-reference.md | page-flattened-strategies-indexes-surprise-me | IN3 |
| ✅ | 5 | CDP + by-ear verification (app relaunched with the debug port): the three strategies work; a 16-bar Pattern with Cycle tours further; Cycle start index shifts the tour; FixedPlusRotating honours both indexes; Surprise-me produces a valid generation each press; figures still render/play right. Report results. | — | page-flattened-strategies-indexes-surprise-me, req-in3-note-ref-sync | IN3, IN7 |
---

### Legend

| Symbol | Meaning |
|--------|---------|
| ✅ | Done |
| 🔄 | In Progress |
| 🔳 | Pending |
| ❌ | Cancelled |
