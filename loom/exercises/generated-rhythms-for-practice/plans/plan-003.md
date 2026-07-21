---
type: plan
id: pl_01KY1SNH2YWBR1JPFYADHBCSDP
title: Phase 2 polish — Random rests + Beat-1 reference + Loop
status: done
created: 2026-07-21
updated: 2026-07-21
version: 1
design_version: 1
req_version: 3
tags: []
parent_id: de_01KY0RDXS9C7X93BX8Y1HVCMC3
requires_load: []
target_version: 0.1.0
steps:
  - id: random-rests-restprobability
    order: 1
    status: done
    description: "Random strategy rests: add `RestProbability` (0..1) to RandomParams; in the walk, roll onset-vs-rest per step (a rest advances the drawn value's duration with no onset, so it reads as a quarter/eighth/16th rest); remove the forced beat-1 onset (cell 0 may now rest). Unit tests: rest density trends with probability (0 = solid fill, 1 = empty), determinism with a seed, and the all-rest edge."
    files_touched: [src/ChordFlow.Core/Music/Rhythm/Generation/RandomParams.cs, src/ChordFlow.Core/Music/Rhythm/Generation/RandomStrategy.cs, tests/ChordFlow.Core.Tests/Rhythm/Generation/RhythmGeneratorTests.cs]
    blocked_by: []
    satisfies: [IN4, IN12, IN6]
  - id: wire-rests-beat-1-reference-lane
    order: 2
    status: done
    description: "Wire rests + Beat-1 reference: add `RestProbability` and `ReferencePulse` (\"off\"/\"beat1\") to RhythmGenerationRequest; RhythmRequestResolver maps restProbability onto RandomParams; RhythmGenerateHandler, when referencePulse==beat1, adds a NON-generated reference lane (a hit on beat 1 of each bar) in a distinct voice (Kick, or HiHatPedal if the generated voice is Kick) to the preview DrumGroove — so it shows as its own DrumsR row and plays. Update the handler test (rest density in the reply; the ref row present with hits only on beat 1)."
    files_touched: [src/ChordFlow.Core/Bridge/RhythmGenerateEnvelope.cs, src/ChordFlow.Core/Features/Rhythm/RhythmRequestResolver.cs, src/ChordFlow.Core/Features/Rhythm/RhythmGenerateHandler.cs, tests/ChordFlow.Core.Tests/Rhythm/Generation/RhythmGenerateHandlerTests.cs]
    blocked_by: [random-rests-restprobability]
    satisfies: [IN8, IN12, IN7]
  - id: loop-toggle-on-scorer-playercontrolsr-default
    order: 3
    status: done
    description: "Loop support in the shared transport: playback-component.js gains `setLooping(on)` (sets `api.isLooping`), re-asserted per loaded score; PlayerControlsR gets a Loop toggle button reflecting/So-toggling it, defaulting ON (applied on ready). ScoreR passes a `loop` create-opt (default true) through to the engine. Default-on affects every render surface (per the request); per-page override stays possible."
    files_touched: [src/ChordFlow.Desktop/wwwroot/playback-component.js, src/ChordFlow.Desktop/wwwroot/player-controls-component.js, src/ChordFlow.Desktop/wwwroot/score-render-component.js]
    blocked_by: []
    satisfies: [IN13]
  - id: page-rest-slider-reference-pulse-loop
    order: 4
    status: done
    description: "Rhythm Generator page: add a Rest % slider to the Random controls (sends `restProbability`); always send `referencePulse:\"beat1\"` so the visible reference row appears; confirm the Loop toggle (from ScoreR, default on) shows on the page. The beat-1 ref row renders automatically as an extra DrumsR lane."
    files_touched: [src/ChordFlow.Desktop/wwwroot/rhythm-generator.js]
    blocked_by: [wire-rests-beat-1-reference-lane, loop-toggle-on-scorer-playercontrolsr-default]
    satisfies: [IN7, IN12, IN8]
  - id: update-domain-architecture-references
    order: 5
    status: done
    description: "Ref-doc sync (CLAUDE-LOCAL): domain-model reference gets RandomParams.RestProbability; architecture reference gets the Beat-1 reference lane in RhythmGenerateHandler, the shared Loop toggle on ScoreR/PlayerControlsR, and the Rest % control."
    files_touched: [loom/refs/chordflow-domain-model-reference.md, loom/refs/chordflow-architecture-reference.md]
    blocked_by: [page-rest-slider-reference-pulse-loop]
    satisfies: []
  - id: cdp-verification-pass
    order: 6
    status: done
    description: "CDP verification (run once the app is relaunched with the debug port): the harness asserts rests appear (grid sparser than solid at a mid probability), the Beat-1 reference row is present with hits only on beat 1, and loop playback repeats continuously. Report results; no code unless a defect surfaces."
    files_touched: []
    blocked_by: [page-rest-slider-reference-pulse-loop, update-domain-architecture-references]
    satisfies: [IN7, IN12, IN13, IN8]
---
# Phase 2 polish — Random rests + Beat-1 reference + Loop

## Goal

Dogfooding fixes/enhancements on the Rhythm Generator page: (1) the Random strategy interleaves real rests via a rest-probability control — each rest the length of the note value drawn (quarter/eighth/16th rests) — and stops forcing beat 1 to sound (the generator stays free); (2) the page gains an implicit, visible Beat-1 reference pulse (the Beat1 slice of the reference-pulse feature pulled forward from Phase 3) — a distinct non-generated reference row/click so the user hears where 1 is when the rhythm is the only sound; (3) a shared Loop/cycling toggle on ScoreR (default on), surfaced on the page, so a short pattern loops continuously (also side-stepping the alphaTab short-score replay glitch for normal use). Satisfies IN12, IN8, IN13; refines the Phase-2 dogfood page. Closes with the ref-doc sync + a CDP verification pass.

---

## Steps

| Done | # | Step | Files touched | Blocked by | Satisfies |
|---|---|---|---|---|---|
| ✅ | 1 | Random strategy rests: add `RestProbability` (0..1) to RandomParams; in the walk, roll onset-vs-rest per step (a rest advances the drawn value's duration with no onset, so it reads as a quarter/eighth/16th rest); remove the forced beat-1 onset (cell 0 may now rest). Unit tests: rest density trends with probability (0 = solid fill, 1 = empty), determinism with a seed, and the all-rest edge. | src/ChordFlow.Core/Music/Rhythm/Generation/RandomParams.cs, src/ChordFlow.Core/Music/Rhythm/Generation/RandomStrategy.cs, tests/ChordFlow.Core.Tests/Rhythm/Generation/RhythmGeneratorTests.cs | — | IN4, IN12, IN6 |
| ✅ | 2 | Wire rests + Beat-1 reference: add `RestProbability` and `ReferencePulse` ("off"/"beat1") to RhythmGenerationRequest; RhythmRequestResolver maps restProbability onto RandomParams; RhythmGenerateHandler, when referencePulse==beat1, adds a NON-generated reference lane (a hit on beat 1 of each bar) in a distinct voice (Kick, or HiHatPedal if the generated voice is Kick) to the preview DrumGroove — so it shows as its own DrumsR row and plays. Update the handler test (rest density in the reply; the ref row present with hits only on beat 1). | src/ChordFlow.Core/Bridge/RhythmGenerateEnvelope.cs, src/ChordFlow.Core/Features/Rhythm/RhythmRequestResolver.cs, src/ChordFlow.Core/Features/Rhythm/RhythmGenerateHandler.cs, tests/ChordFlow.Core.Tests/Rhythm/Generation/RhythmGenerateHandlerTests.cs | random-rests-restprobability | IN8, IN12, IN7 |
| ✅ | 3 | Loop support in the shared transport: playback-component.js gains `setLooping(on)` (sets `api.isLooping`), re-asserted per loaded score; PlayerControlsR gets a Loop toggle button reflecting/So-toggling it, defaulting ON (applied on ready). ScoreR passes a `loop` create-opt (default true) through to the engine. Default-on affects every render surface (per the request); per-page override stays possible. | src/ChordFlow.Desktop/wwwroot/playback-component.js, src/ChordFlow.Desktop/wwwroot/player-controls-component.js, src/ChordFlow.Desktop/wwwroot/score-render-component.js | — | IN13 |
| ✅ | 4 | Rhythm Generator page: add a Rest % slider to the Random controls (sends `restProbability`); always send `referencePulse:"beat1"` so the visible reference row appears; confirm the Loop toggle (from ScoreR, default on) shows on the page. The beat-1 ref row renders automatically as an extra DrumsR lane. | src/ChordFlow.Desktop/wwwroot/rhythm-generator.js | wire-rests-beat-1-reference-lane, loop-toggle-on-scorer-playercontrolsr-default | IN7, IN12, IN8 |
| ✅ | 5 | Ref-doc sync (CLAUDE-LOCAL): domain-model reference gets RandomParams.RestProbability; architecture reference gets the Beat-1 reference lane in RhythmGenerateHandler, the shared Loop toggle on ScoreR/PlayerControlsR, and the Rest % control. | loom/refs/chordflow-domain-model-reference.md, loom/refs/chordflow-architecture-reference.md | page-rest-slider-reference-pulse-loop | — |
| ✅ | 6 | CDP verification (run once the app is relaunched with the debug port): the harness asserts rests appear (grid sparser than solid at a mid probability), the Beat-1 reference row is present with hits only on beat 1, and loop playback repeats continuously. Report results; no code unless a defect surfaces. | — | page-rest-slider-reference-pulse-loop, update-domain-architecture-references | IN7, IN12, IN13, IN8 |
---

### Legend

| Symbol | Meaning |
|--------|---------|
| ✅ | Done |
| 🔄 | In Progress |
| 🔳 | Pending |
| ❌ | Cancelled |
