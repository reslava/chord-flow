---
type: plan
id: pl_01KY0RS6WV6GN3EGF59FYHXMTA
title: Phase 1 — Generation core
status: done
created: 2026-07-20
updated: 2026-07-20
version: 1
design_version: 1
req_version: 1
tags: []
parent_id: de_01KY0RDXS9C7X93BX8Y1HVCMC3
requires_load: []
target_version: 0.1.0
steps:
  - id: onset-grid-model-tick-arithmetic
    order: 1
    status: done
    description: "Onset-grid model: Block (one beat's onsets), OnsetBar (4 blocks in 4/4), OnsetGrid (1–4 bars), plus the cell→tick arithmetic (cell k at subdivision n on beat b → tick b*BeatTicks + k*(BeatTicks/n); n must divide 48)."
    files_touched: [src/ChordFlow.Core/Music/Rhythm/Generation/Block.cs, src/ChordFlow.Core/Music/Rhythm/Generation/OnsetBar.cs, src/ChordFlow.Core/Music/Rhythm/Generation/OnsetGrid.cs]
    blocked_by: []
    satisfies: [IN1, C1, C8]
  - id: rhythmfamily-quarter-eighth-families
    order: 2
    status: done
    description: "RhythmFamily: a named, ordered palette of non-empty Blocks at one subdivision. Author the v1 families — Quarter (subdivision 1, {[0]}) and Eighth (subdivision 2, {[0], [1], [0,1]} = on-beat / the & / both)."
    files_touched: [src/ChordFlow.Core/Music/Rhythm/Generation/RhythmFamily.cs]
    blocked_by: [onset-grid-model-tick-arithmetic]
    satisfies: [IN3]
  - id: bar-composition-operators-six
    order: 3
    status: done
    description: "BarOperator — the (family, beatIndex, rng) → Block dispatch. Implement the six operators: Uniform, Isolate(k), AnchorRotate, Mask(beats), Displace(cells), Accumulate(n)/Thin(n)."
    files_touched: [src/ChordFlow.Core/Music/Rhythm/Generation/BarOperator.cs]
    blocked_by: [onset-grid-model-tick-arithmetic, rhythmfamily-quarter-eighth-families]
    satisfies: [IN3]
  - id: sequence-behaviours-five
    order: 4
    status: done
    description: SequenceBehaviour — the (barIndex, operatorConfig, prevBar) → operatorConfig dispatch. Implement Repeat, Cycle, Sweep (bind an operator param to barIndex), RestBar (emit an empty OnsetBar), CallResponse (content bar then empty bar).
    files_touched: [src/ChordFlow.Core/Music/Rhythm/Generation/SequenceBehaviour.cs]
    blocked_by: [onset-grid-model-tick-arithmetic, bar-composition-operators-six]
    satisfies: [IN3]
  - id: pattern-strategy-patternparams
    order: 5
    status: done
    description: "Pattern strategy: PatternParams(Family, Operator, Behaviour, BarCount, Seed) and the composition loop (behaviour yields per-bar operator config → operator fills 4 beats from the family → OnsetBar → OnsetGrid)."
    files_touched: [src/ChordFlow.Core/Music/Rhythm/Generation/PatternStrategy.cs, src/ChordFlow.Core/Music/Rhythm/Generation/PatternParams.cs]
    blocked_by: [rhythmfamily-quarter-eighth-families, bar-composition-operators-six, sequence-behaviours-five]
    satisfies: [IN2, IN3]
  - id: random-strategy-randomparams
    order: 6
    status: done
    description: "Random strategy: RandomParams(ValuePalette, ContentBars, SilenceBars, Seed). Seeded fill of ContentBars from the note-value palette, then SilenceBars empty bars; produces a (ContentBars+SilenceBars)-bar OnsetGrid."
    files_touched: [src/ChordFlow.Core/Music/Rhythm/Generation/RandomStrategy.cs, src/ChordFlow.Core/Music/Rhythm/Generation/RandomParams.cs]
    blocked_by: [onset-grid-model-tick-arithmetic]
    satisfies: [IN2, IN4]
  - id: rhythmgenerator-dispatch-generationparams
    order: 7
    status: done
    description: RhythmGenerator + GenerationParams(BarCount, Ts, Seed, strategy payload). The single entry point that dispatches on strategy and returns an OnsetGrid; deterministic — same {strategy, params, seed} → same grid.
    files_touched: [src/ChordFlow.Core/Music/Rhythm/Generation/RhythmGenerator.cs, src/ChordFlow.Core/Music/Rhythm/Generation/GenerationParams.cs]
    blocked_by: [pattern-strategy-patternparams, random-strategy-randomparams]
    satisfies: [IN2, IN6, C7]
  - id: rhythmpattern-legato-projection
    order: 8
    status: done
    description: "OnsetGrid → RhythmPattern projection (legato, ring-to-next-onset): each onset → RhythmEvent(pos, nextOnset−pos); last onset of a bar rings to the barline (no cross-bar tie); empty bar → whole-bar rest. Stays within the verified :N + rest vocabulary."
    files_touched: [src/ChordFlow.Core/Music/Rhythm/Generation/OnsetGridToRhythmPattern.cs]
    blocked_by: [onset-grid-model-tick-arithmetic]
    satisfies: [IN5, C4]
  - id: drumgroove-single-lane-projection
    order: 9
    status: done
    description: "OnsetGrid → DrumGroove projection (single voice): each onset → a one-cell RhythmEvent (Hit) on one DrumLane; one DrumBar per generated bar; default voice HiHatClosed. Lives in Instruments/Drums (targets a Drums type)."
    files_touched: [src/ChordFlow.Core/Instruments/Drums/OnsetGridToDrumGroove.cs]
    blocked_by: [onset-grid-model-tick-arithmetic]
    satisfies: [IN5, C2, C3]
  - id: generation-unit-tests
    order: 10
    status: done
    description: "Unit tests: determinism (same {params, seed} → identical OnsetGrid), projection agreement (RhythmPattern event onset ticks == DrumGroove hit onset ticks for one grid), the legato projection stays inside the verified render vocabulary (renders without throwing an unverified-tie), plus per-operator / per-behaviour / per-strategy shape assertions. MusicLayeringTests confirms the Music→Instruments edge is uncrossed."
    files_touched: [tests/ChordFlow.Core.Tests/Rhythm/Generation/RhythmGeneratorTests.cs, tests/ChordFlow.Core.Tests/Rhythm/Generation/OnsetGridProjectionTests.cs]
    blocked_by: [rhythmgenerator-dispatch-generationparams, rhythmpattern-legato-projection, drumgroove-single-lane-projection]
    satisfies: [IN6, C4, C6, C1]
  - id: update-domain-model-reference
    order: 11
    status: done
    description: Update the domain-model reference doc with the new Music/Rhythm/Generation types and the two projections (the CLAUDE-LOCAL bidirectional ref-sync rule — same unit of work as the code).
    files_touched: [loom/refs/chordflow-domain-model-reference.md]
    blocked_by: [generation-unit-tests]
    satisfies: []
---
# Phase 1 — Generation core

## Goal

Build the headless Core of the rhythm generation engine: the instrument-agnostic onset-grid model, the one `RhythmGenerator` with its Pattern and Random strategies (families, the six bar operators, the five sequence behaviours), and both projections (`OnsetGrid → RhythmPattern` legato in Music; `OnsetGrid → DrumGroove` single-lane in Instruments/Drums), all seeded and unit-tested. No UI, no bridge, no page — this phase delivers the substrate every later phase sits on, verified by determinism + projection-agreement + verified-vocabulary tests. Closes with the mandated domain-model reference update.

---

## Steps

| Done | # | Step | Files touched | Blocked by | Satisfies |
|---|---|---|---|---|---|
| ✅ | 1 | Onset-grid model: Block (one beat's onsets), OnsetBar (4 blocks in 4/4), OnsetGrid (1–4 bars), plus the cell→tick arithmetic (cell k at subdivision n on beat b → tick b*BeatTicks + k*(BeatTicks/n); n must divide 48). | src/ChordFlow.Core/Music/Rhythm/Generation/Block.cs, src/ChordFlow.Core/Music/Rhythm/Generation/OnsetBar.cs, src/ChordFlow.Core/Music/Rhythm/Generation/OnsetGrid.cs | — | IN1, C1, C8 |
| ✅ | 2 | RhythmFamily: a named, ordered palette of non-empty Blocks at one subdivision. Author the v1 families — Quarter (subdivision 1, {[0]}) and Eighth (subdivision 2, {[0], [1], [0,1]} = on-beat / the & / both). | src/ChordFlow.Core/Music/Rhythm/Generation/RhythmFamily.cs | onset-grid-model-tick-arithmetic | IN3 |
| ✅ | 3 | BarOperator — the (family, beatIndex, rng) → Block dispatch. Implement the six operators: Uniform, Isolate(k), AnchorRotate, Mask(beats), Displace(cells), Accumulate(n)/Thin(n). | src/ChordFlow.Core/Music/Rhythm/Generation/BarOperator.cs | onset-grid-model-tick-arithmetic, rhythmfamily-quarter-eighth-families | IN3 |
| ✅ | 4 | SequenceBehaviour — the (barIndex, operatorConfig, prevBar) → operatorConfig dispatch. Implement Repeat, Cycle, Sweep (bind an operator param to barIndex), RestBar (emit an empty OnsetBar), CallResponse (content bar then empty bar). | src/ChordFlow.Core/Music/Rhythm/Generation/SequenceBehaviour.cs | onset-grid-model-tick-arithmetic, bar-composition-operators-six | IN3 |
| ✅ | 5 | Pattern strategy: PatternParams(Family, Operator, Behaviour, BarCount, Seed) and the composition loop (behaviour yields per-bar operator config → operator fills 4 beats from the family → OnsetBar → OnsetGrid). | src/ChordFlow.Core/Music/Rhythm/Generation/PatternStrategy.cs, src/ChordFlow.Core/Music/Rhythm/Generation/PatternParams.cs | rhythmfamily-quarter-eighth-families, bar-composition-operators-six, sequence-behaviours-five | IN2, IN3 |
| ✅ | 6 | Random strategy: RandomParams(ValuePalette, ContentBars, SilenceBars, Seed). Seeded fill of ContentBars from the note-value palette, then SilenceBars empty bars; produces a (ContentBars+SilenceBars)-bar OnsetGrid. | src/ChordFlow.Core/Music/Rhythm/Generation/RandomStrategy.cs, src/ChordFlow.Core/Music/Rhythm/Generation/RandomParams.cs | onset-grid-model-tick-arithmetic | IN2, IN4 |
| ✅ | 7 | RhythmGenerator + GenerationParams(BarCount, Ts, Seed, strategy payload). The single entry point that dispatches on strategy and returns an OnsetGrid; deterministic — same {strategy, params, seed} → same grid. | src/ChordFlow.Core/Music/Rhythm/Generation/RhythmGenerator.cs, src/ChordFlow.Core/Music/Rhythm/Generation/GenerationParams.cs | pattern-strategy-patternparams, random-strategy-randomparams | IN2, IN6, C7 |
| ✅ | 8 | OnsetGrid → RhythmPattern projection (legato, ring-to-next-onset): each onset → RhythmEvent(pos, nextOnset−pos); last onset of a bar rings to the barline (no cross-bar tie); empty bar → whole-bar rest. Stays within the verified :N + rest vocabulary. | src/ChordFlow.Core/Music/Rhythm/Generation/OnsetGridToRhythmPattern.cs | onset-grid-model-tick-arithmetic | IN5, C4 |
| ✅ | 9 | OnsetGrid → DrumGroove projection (single voice): each onset → a one-cell RhythmEvent (Hit) on one DrumLane; one DrumBar per generated bar; default voice HiHatClosed. Lives in Instruments/Drums (targets a Drums type). | src/ChordFlow.Core/Instruments/Drums/OnsetGridToDrumGroove.cs | onset-grid-model-tick-arithmetic | IN5, C2, C3 |
| ✅ | 10 | Unit tests: determinism (same {params, seed} → identical OnsetGrid), projection agreement (RhythmPattern event onset ticks == DrumGroove hit onset ticks for one grid), the legato projection stays inside the verified render vocabulary (renders without throwing an unverified-tie), plus per-operator / per-behaviour / per-strategy shape assertions. MusicLayeringTests confirms the Music→Instruments edge is uncrossed. | tests/ChordFlow.Core.Tests/Rhythm/Generation/RhythmGeneratorTests.cs, tests/ChordFlow.Core.Tests/Rhythm/Generation/OnsetGridProjectionTests.cs | rhythmgenerator-dispatch-generationparams, rhythmpattern-legato-projection, drumgroove-single-lane-projection | IN6, C4, C6, C1 |
| ✅ | 11 | Update the domain-model reference doc with the new Music/Rhythm/Generation types and the two projections (the CLAUDE-LOCAL bidirectional ref-sync rule — same unit of work as the code). | loom/refs/chordflow-domain-model-reference.md | generation-unit-tests | — |
---

### Legend

| Symbol | Meaning |
|--------|---------|
| ✅ | Done |
| 🔄 | In Progress |
| 🔳 | Pending |
| ❌ | Cancelled |

<!-- step:onset-grid-model-tick-arithmetic -->
### Step 1 — Onset-grid model + tick arithmetic

Pure/immutable records under a new `Music/Rhythm/Generation/` namespace. Block = one beat is the canonical unit; empty beat = `Block(n, [])`. Instrument-agnostic — no reference to anything under `Instruments/`. The tick helper is the single arithmetic bridge both projections reuse.
