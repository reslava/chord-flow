---
type: plan
id: pl_01KVZYFBK1NWG8P2VA8R8K83AE
title: Engine-derived voicings as the app's source
status: done
created: 2026-06-25
updated: 2026-06-25
version: 1
design_version: 1
req_version: 1
tags: []
parent_id: de_01KVZVJC1A52Q0GSJGRHXQJZ8B
requires_load: []
target_version: 0.1.0
steps:
  - id: chordshape-voicing-adapter
    order: 1
    status: done
    description: "`ChordShape → Voicing` adapter — `ChordShapeVoicing.ToVoicing`: non-muted strings → `FretPosition`, muted → `MutedStrings`, `FirstFret` = lowest sounding fret; `BarreFret` left null (EX5)."
    files_touched: [src/ChordFlow.Core/Instruments/Guitar/Caged/ChordShapeVoicing.cs, tests/ChordFlow.Core.Tests/ChordShapeVoicingTests.cs]
    blocked_by: []
    satisfies: [IN1]
  - id: enginevoicingsource-listing
    order: 2
    status: done
    description: "`EngineVoicingSource : IComputedContentSource` — `List(Voicing)` → 36 `automatic` `ContentItem` rows with synthetic ids `auto:{quality}:{shape}` (other kinds empty); wire as `ContentCrudHandler`'s `computed:` source in `Program.cs`. Computed, un-persisted (C3)."
    files_touched: [src/ChordFlow.Core/Features/Voicings/EngineVoicingSource.cs, src/ChordFlow.Desktop/Program.cs, tests/ChordFlow.Core.Tests/EngineVoicingSourceTests.cs]
    blocked_by: []
    satisfies: [IN2, IN3, C3]
  - id: ranking-seam-closest
    order: 3
    status: done
    description: "Ranking seam `IVoicingRanking` + default `ClosestRanking`: first chord = lowest-`FirstFret` in region; next = reuse this chord's earlier grip if seen, else minimize the full per-string fret-distance sum to the previous grip."
    files_touched: [src/ChordFlow.Core/Instruments/Guitar/Voicings/IVoicingRanking.cs, src/ChordFlow.Core/Instruments/Guitar/Voicings/ClosestRanking.cs, tests/ChordFlow.Core.Tests/ClosestRankingTests.cs]
    blocked_by: []
    satisfies: [IN7]
  - id: compingresolver-compingplan
    order: 4
    status: done
    description: "`CompingResolver` + `CompingPlan` in Features: per chord try the main source, else fall back `user > package > automatic`; automatic grips via `Derive → ChordShapeVoicing.ToVoicing`, picked by the ranking strategy. Fail-loud (C2), per-chord mixing."
    files_touched: [src/ChordFlow.Core/Features/Voicings/CompingResolver.cs, src/ChordFlow.Core/Features/Voicings/CompingPlan.cs, tests/ChordFlow.Core.Tests/CompingResolverTests.cs]
    blocked_by: []
    satisfies: [IN4, C2, C3]
  - id: voicingsource-knob
    order: 5
    status: done
    description: Replace the `VoicingStrategy` enum with structured `VoicingSource { Kind, MinFret?, MaxFret?, PackageId?, Ranking? }`; thread it through `RenderOptions`, the bridge `renderOptions.voicing`, and the Practice voicing picker. Absent ⇒ automatic/full-neck/Closest.
    files_touched: [src/ChordFlow.Core/Rendering/RenderOptions.cs, src/ChordFlow.Core/Bridge/, src/ChordFlow.Desktop/wwwroot/app.js, tests/ChordFlow.Core.Tests/RenderOptionsTests.cs]
    blocked_by: []
    satisfies: [IN6]
  - id: renderer-as-pure-formatter-wire
    order: 6
    status: done
    description: "Renderer → pure formatter: `AlphaTexRenderer` drops the `VoicingBook` ctor dependency and takes the `CompingPlan` as a `Render(...)` input; `ExerciseRendering` invokes `CompingResolver` and passes the plan; `Program.cs` rewires; the now/next chord schedule is built from the same plan."
    files_touched: [src/ChordFlow.Core/Rendering/AlphaTexRenderer.cs, src/ChordFlow.Core/Features/ExerciseRendering.cs, src/ChordFlow.Desktop/Program.cs, tests/ChordFlow.Core.Tests/AlphaTexRendererTests.cs]
    blocked_by: []
    satisfies: [IN5, IN4]
  - id: relocate-grips-coverage-gate-atomic-with
    order: 7
    status: done
    description: Relocate the 36 authored `.dsl` grips from `Content/default-pack/voicings` to a test-only fixture loaded by `CagedDerivationOracleTests` (default pack ships zero voicings); add the coverage structural test asserting every quality×shape the source offers derives a valid, fully-spelled grip, pinned to the 36-set (C5).
    files_touched: [src/ChordFlow.Core/Content/default-pack/voicings/, tests/ChordFlow.Core.Tests/fixtures/caged-oracle/, tests/ChordFlow.Core.Tests/CagedDerivationOracleTests.cs, tests/ChordFlow.Core.Tests/DefaultPackVoicingsTests.cs, tests/ChordFlow.Core.Tests/EngineVoicingCoverageTests.cs]
    blocked_by: []
    satisfies: [IN8, IN9, C1, C5]
  - id: ref-comment-sync
    order: 8
    status: done
    description: "Ref + comment updates: `chordflow-architecture-reference` (automatic = engine filling `IComputedContentSource`; main-source/fallback comping), `chordflow-domain-model-reference` §2/§5/§6 (engine automatic source; renderer-as-formatter), and fix the stale \"34\" in `CagedDerivation.cs:17` + the oracle-test comment."
    files_touched: [loom/refs/chordflow-architecture-reference.md, loom/refs/chordflow-domain-model-reference.md, src/ChordFlow.Core/Instruments/Guitar/Caged/CagedDerivation.cs, tests/ChordFlow.Core.Tests/CagedDerivationOracleTests.cs]
    blocked_by: []
    satisfies: [IN10]
  - id: dogfood-on-now-next-fret-boxes
    order: 9
    status: done
    description: "Dogfood: generate a 12-bar blues with `automatic` comping and confirm the engine-derived grips render on the now/next fret-boxes; spot-check a region-locked main source (e.g. automatic 5–12) changes the grips and a missing chord falls back."
    files_touched: [src/ChordFlow.Desktop/wwwroot/now-next-fretboards.js]
    blocked_by: []
    satisfies: [IN11]
---
# Engine-derived voicings as the app's source

## Goal

Make CagedDerivation.Derive the app's `automatic` voicing source and demote the 36 default-pack CAGED grips to a test-only oracle. Two seams: fill the content-source-model listing seam (IComputedContentSource) so automatic voicings appear in the catalog, and add a Features-layer CompingResolver→CompingPlan (decision D4=(B)) that picks a grip per chord by main-source→fallback and a Closest ranking, with the renderer demoted to a pure formatter. The relocation of the 36 grips and the comping re-wire land atomically (C1) so the app never silently regresses to BeginnerShellStrategy. Builds against the locked req (IN1–IN11, C1–C5).

---

## Steps

| Done | # | Step | Files touched | Blocked by | Satisfies |
|---|---|---|---|---|---|
| ✅ | 1 | `ChordShape → Voicing` adapter — `ChordShapeVoicing.ToVoicing`: non-muted strings → `FretPosition`, muted → `MutedStrings`, `FirstFret` = lowest sounding fret; `BarreFret` left null (EX5). | src/ChordFlow.Core/Instruments/Guitar/Caged/ChordShapeVoicing.cs, tests/ChordFlow.Core.Tests/ChordShapeVoicingTests.cs | — | IN1 |
| ✅ | 2 | `EngineVoicingSource : IComputedContentSource` — `List(Voicing)` → 36 `automatic` `ContentItem` rows with synthetic ids `auto:{quality}:{shape}` (other kinds empty); wire as `ContentCrudHandler`'s `computed:` source in `Program.cs`. Computed, un-persisted (C3). | src/ChordFlow.Core/Features/Voicings/EngineVoicingSource.cs, src/ChordFlow.Desktop/Program.cs, tests/ChordFlow.Core.Tests/EngineVoicingSourceTests.cs | — | IN2, IN3, C3 |
| ✅ | 3 | Ranking seam `IVoicingRanking` + default `ClosestRanking`: first chord = lowest-`FirstFret` in region; next = reuse this chord's earlier grip if seen, else minimize the full per-string fret-distance sum to the previous grip. | src/ChordFlow.Core/Instruments/Guitar/Voicings/IVoicingRanking.cs, src/ChordFlow.Core/Instruments/Guitar/Voicings/ClosestRanking.cs, tests/ChordFlow.Core.Tests/ClosestRankingTests.cs | — | IN7 |
| ✅ | 4 | `CompingResolver` + `CompingPlan` in Features: per chord try the main source, else fall back `user > package > automatic`; automatic grips via `Derive → ChordShapeVoicing.ToVoicing`, picked by the ranking strategy. Fail-loud (C2), per-chord mixing. | src/ChordFlow.Core/Features/Voicings/CompingResolver.cs, src/ChordFlow.Core/Features/Voicings/CompingPlan.cs, tests/ChordFlow.Core.Tests/CompingResolverTests.cs | — | IN4, C2, C3 |
| ✅ | 5 | Replace the `VoicingStrategy` enum with structured `VoicingSource { Kind, MinFret?, MaxFret?, PackageId?, Ranking? }`; thread it through `RenderOptions`, the bridge `renderOptions.voicing`, and the Practice voicing picker. Absent ⇒ automatic/full-neck/Closest. | src/ChordFlow.Core/Rendering/RenderOptions.cs, src/ChordFlow.Core/Bridge/, src/ChordFlow.Desktop/wwwroot/app.js, tests/ChordFlow.Core.Tests/RenderOptionsTests.cs | — | IN6 |
| ✅ | 6 | Renderer → pure formatter: `AlphaTexRenderer` drops the `VoicingBook` ctor dependency and takes the `CompingPlan` as a `Render(...)` input; `ExerciseRendering` invokes `CompingResolver` and passes the plan; `Program.cs` rewires; the now/next chord schedule is built from the same plan. | src/ChordFlow.Core/Rendering/AlphaTexRenderer.cs, src/ChordFlow.Core/Features/ExerciseRendering.cs, src/ChordFlow.Desktop/Program.cs, tests/ChordFlow.Core.Tests/AlphaTexRendererTests.cs | — | IN5, IN4 |
| ✅ | 7 | Relocate the 36 authored `.dsl` grips from `Content/default-pack/voicings` to a test-only fixture loaded by `CagedDerivationOracleTests` (default pack ships zero voicings); add the coverage structural test asserting every quality×shape the source offers derives a valid, fully-spelled grip, pinned to the 36-set (C5). | src/ChordFlow.Core/Content/default-pack/voicings/, tests/ChordFlow.Core.Tests/fixtures/caged-oracle/, tests/ChordFlow.Core.Tests/CagedDerivationOracleTests.cs, tests/ChordFlow.Core.Tests/DefaultPackVoicingsTests.cs, tests/ChordFlow.Core.Tests/EngineVoicingCoverageTests.cs | — | IN8, IN9, C1, C5 |
| ✅ | 8 | Ref + comment updates: `chordflow-architecture-reference` (automatic = engine filling `IComputedContentSource`; main-source/fallback comping), `chordflow-domain-model-reference` §2/§5/§6 (engine automatic source; renderer-as-formatter), and fix the stale "34" in `CagedDerivation.cs:17` + the oracle-test comment. | loom/refs/chordflow-architecture-reference.md, loom/refs/chordflow-domain-model-reference.md, src/ChordFlow.Core/Instruments/Guitar/Caged/CagedDerivation.cs, tests/ChordFlow.Core.Tests/CagedDerivationOracleTests.cs | — | IN10 |
| ✅ | 9 | Dogfood: generate a 12-bar blues with `automatic` comping and confirm the engine-derived grips render on the now/next fret-boxes; spot-check a region-locked main source (e.g. automatic 5–12) changes the grips and a missing chord falls back. | src/ChordFlow.Desktop/wwwroot/now-next-fretboards.js | — | IN11 |
---

### Legend

| Symbol | Meaning |
|--------|---------|
| ✅ | Done |
| 🔄 | In Progress |
| 🔳 | Pending |
| ❌ | Cancelled |

<!-- step:compingresolver-compingplan -->
### Step 4 — CompingResolver + CompingPlan

Needs the adapter (step 1) and the ranking seam (step 3). Leaves a per-chord override hook open for [[explicit-voicing-reference]] (EX3) but does not implement it.

<!-- step:renderer-as-pure-formatter-wire -->
### Step 6 — Renderer as pure formatter + wire

Depends on steps 4 and 5. This flips the comping path off `VoicingBook.Lookup` — must land atomically with step 7 (C1).

<!-- step:relocate-grips-coverage-gate-atomic-with -->
### Step 7 — Relocate grips + coverage gate (atomic with step 6)

C1: lands in the SAME unit of work as step 6 — relocating the grips severs the voicing seed, so the comping re-wire must be in place or the app regresses to BeginnerShellStrategy.
