---
type: plan
id: pl_01KV6482DNFNKGX8R1MPMV6DXG
title: Exercise keystone — record merge, two-track render, persistence
status: done
created: 2026-06-15
updated: 2026-06-15
version: 1
design_version: 1
req_version: 1
tags: []
parent_id: de_01KTWE8B7WKRX7M681PM4P9JFP
requires_load: []
target_version: 0.1.0
actual_release: 0.5.0
steps:
  - id: canonical-exercise-record-song-ofprogression-songexpander
    order: 1
    status: done
    description: Rewrite `Exercise` to the merged shape, add `Song.OfProgression`, add optional `startKey` to `SongExpander.Expand`, delete `SongExercise`
    files_touched: [src/ChordFlow.Core/Domain/Exercise.cs, src/ChordFlow.Core/Domain/Song/Song.cs, src/ChordFlow.Core/Domain/Song/SongExercise.cs, src/ChordFlow.Core/Domain/Song/SongExpander.cs]
    blocked_by: []
    satisfies: [IN1, IN2, IN3]
  - id: renderer-merge-per-decision-a-features
    order: 2
    status: done
    description: Move Song-expansion into Features; remove `Render(Exercise)`; add `lead` param to the RealizedSong overload; repoint all old-Exercise callers so the solution is green
    files_touched: [src/ChordFlow.Core/Rendering/IScoreRenderer.cs, src/ChordFlow.Core/Rendering/AlphaTexRenderer.cs, src/ChordFlow.Desktop/WebHost/SwappableRenderer.cs, src/ChordFlow.Core/Features/GenerateExercise/GenerateExercise.cs, src/ChordFlow.Core/Features/ContentCrud/ContentCrudHandler.cs, src/ChordFlow.Core/Features/ExerciseLibrary/ExerciseLibrary.cs, tests/ChordFlow.Core.Tests/]
    blocked_by: []
    satisfies: [IN1, C1, C3]
  - id: two-track-lead-staff-dead-notes
    order: 3
    status: done
    description: Render the optional Lead pattern as a second `\track` of dead notes; single-track byte-identical when null; document `\track` in the ref
    files_touched: [src/ChordFlow.Core/Rendering/AlphaTexRenderer.cs, loom/refs/alphatex-syntax-reference.md, tests/ChordFlow.Core.Tests/AlphaTexRendererTests.cs]
    blocked_by: []
    satisfies: [IN5, C2]
  - id: exerciseentity-refactor-ef-migration
    order: 4
    status: done
    description: Move the entity from Key/ProgressionId/RhythmId to references + param columns; add the EF migration
    files_touched: [src/ChordFlow.Core/Persistence/Entities/ExerciseEntity.cs, src/ChordFlow.Core/Persistence/ChordFlowDbContext.cs, src/ChordFlow.Core/Features/ExerciseLibrary/ExerciseLibrary.cs, src/ChordFlow.Core/Persistence/Migrations/, tests/ChordFlow.Core.Tests/]
    blocked_by: []
    satisfies: [IN4]
  - id: ref-doc-sync-done-doc
    order: 5
    status: done
    description: Update domain-model + architecture refs and write the thread done doc
    files_touched: [loom/refs/chordflow-domain-model-reference.md, loom/refs/chordflow-architecture-reference.md, loom/exercises/exercises-definition-ui/done/]
    blocked_by: []
    satisfies: []
---
# Exercise keystone — record merge, two-track render, persistence

## Goal

Implement the Exercise keystone over the locked req: collapse the old `Exercise(Key, Progression, …)` and the unused `SongExercise` into one canonical `Exercise(Song, Comping, Lead?, KeyOverride?, Tempo, Difficulty, Feel)` (decision (a)); realize every exercise through the single `SongExpander → RealizedSong → Render(RealizedSong, …)` path with `Song.OfProgression` lifting bare progressions; render the optional `Lead` pattern as a second `\track` of dead notes; and refactor `ExerciseEntity` to references + param columns with an EF migration. Per renderer fork decision (A): the renderer stays pure/store-free — the one I/O seam (Song expansion against `IProgressionStore`) lives in the Features layer, `Render(Exercise)` is dropped, and the lead staff rides the `Render(RealizedSong, …)` overload. Core-only (C3): all work lands in `ChordFlow.Core` Domain/Rendering/Persistence; no UI. Refs (`chordflow-domain-model-reference`, `alphatex-syntax-reference`) are updated in the same work.

---

## Steps

| Done | # | Step | Files touched | Blocked by | Satisfies |
|---|---|---|---|---|---|
| ✅ | 1 | Rewrite `Exercise` to the merged shape, add `Song.OfProgression`, add optional `startKey` to `SongExpander.Expand`, delete `SongExercise` | src/ChordFlow.Core/Domain/Exercise.cs, src/ChordFlow.Core/Domain/Song/Song.cs, src/ChordFlow.Core/Domain/Song/SongExercise.cs, src/ChordFlow.Core/Domain/Song/SongExpander.cs | — | IN1, IN2, IN3 |
| ✅ | 2 | Move Song-expansion into Features; remove `Render(Exercise)`; add `lead` param to the RealizedSong overload; repoint all old-Exercise callers so the solution is green | src/ChordFlow.Core/Rendering/IScoreRenderer.cs, src/ChordFlow.Core/Rendering/AlphaTexRenderer.cs, src/ChordFlow.Desktop/WebHost/SwappableRenderer.cs, src/ChordFlow.Core/Features/GenerateExercise/GenerateExercise.cs, src/ChordFlow.Core/Features/ContentCrud/ContentCrudHandler.cs, src/ChordFlow.Core/Features/ExerciseLibrary/ExerciseLibrary.cs, tests/ChordFlow.Core.Tests/ | — | IN1, C1, C3 |
| ✅ | 3 | Render the optional Lead pattern as a second `\track` of dead notes; single-track byte-identical when null; document `\track` in the ref | src/ChordFlow.Core/Rendering/AlphaTexRenderer.cs, loom/refs/alphatex-syntax-reference.md, tests/ChordFlow.Core.Tests/AlphaTexRendererTests.cs | — | IN5, C2 |
| ✅ | 4 | Move the entity from Key/ProgressionId/RhythmId to references + param columns; add the EF migration | src/ChordFlow.Core/Persistence/Entities/ExerciseEntity.cs, src/ChordFlow.Core/Persistence/ChordFlowDbContext.cs, src/ChordFlow.Core/Features/ExerciseLibrary/ExerciseLibrary.cs, src/ChordFlow.Core/Persistence/Migrations/, tests/ChordFlow.Core.Tests/ | — | IN4 |
| ✅ | 5 | Update domain-model + architecture refs and write the thread done doc | loom/refs/chordflow-domain-model-reference.md, loom/refs/chordflow-architecture-reference.md, loom/exercises/exercises-definition-ui/done/ | — | — |
---

### Legend

| Symbol | Meaning |
|--------|---------|
| ✅ | Done |
| 🔄 | In Progress |
| 🔳 | Pending |
| ❌ | Cancelled |

<!-- step:canonical-exercise-record-song-ofprogression-songexpander -->
### Step 1 — Canonical Exercise record + Song.OfProgression + SongExpander.startKey

Rewrite `Exercise.cs` to `record Exercise(Song Song, RhythmPattern Comping, RhythmPattern? Lead, Key? KeyOverride, int Tempo, Difficulty Difficulty, Feel Feel = Feel.Straight)`. Add `static Song OfProgression(Progression p, Key initialKey)` on `Song` building a one-section inline Song (`FromSections` with `{["A"]=InlineProgression("A",p)}`, items `[PartPlay("A",1)]`). Delete `SongExercise.cs` (it has no consumers) and fix the `Song` docstring that points to it. Add optional `Key? startKey = null` to `SongExpander.Expand` — seed the fold from `startKey ?? song.InitialKey` (additive; modulations still accumulate from the seed). NOTE: this breaks every old-`Exercise` caller — they are repointed in Step 2 within the same build.

<!-- step:renderer-merge-per-decision-a-features -->
### Step 2 — Renderer merge per decision (A) — Features expand, drop Render(Exercise), repoint callers green

Per decision (A): remove `Render(Exercise, options)` from `IScoreRenderer`, `AlphaTexRenderer`, and `SwappableRenderer`; add a `RhythmPattern? lead = null` param to the `Render(RealizedSong, …)` overload (lead not yet rendered — Step 3). Move the one I/O seam into Features: `LoadScoreEnvelope.From(Exercise, IProgressionStore, IScoreRenderer, options)` computes `baseKey = KeyOverride ?? Song.InitialKey`, calls `SongExpander.Expand(Song, store, startKey: baseKey)`, then `Render(RealizedSong, Comping, Tempo, Difficulty, Feel, lead: Lead, options)` — the single shared expand+render place so GenerateExercise/ExerciseLibrary don't duplicate it. `GenerateExerciseHandler` gains an `IProgressionStore`; `ExerciseLibraryHandler` already has `DbContextOptions` to build a `ProgressionStore`. Repoint their `new Exercise(...)` builders and the two `ContentCrudHandler` previews to `Song.OfProgression(...)` + Comping. Fix all affected tests. Single-track output must stay byte-identical; solution compiles green.

<!-- step:two-track-lead-staff-dead-notes -->
### Step 3 — Two-track lead staff (dead notes) + alphaTex ref update

First update `alphatex-syntax-reference.md` with the confirmed structural-metadata syntax `\track "name" "short"` (source: alphatab.net/docs/alphatex/structural-metadata) — the ref currently documents no multi-track syntax. Then extend the `Render(RealizedSong, …, lead)` path: when `lead != null`, emit two `\track` blocks sharing one header — track 1 = comping bars (current output), track 2 = the `Lead` pattern quantized (reuse `WarpBars`/`RhythmQuantizer`) and rendered as dead notes (`x.3`) per slot instead of voiced chords. When `lead == null`, emit today's single-track output unchanged (no `\track` wrapper) so existing tests stay byte-identical (design §7.4). Add tests: two-track dead-note output, single-track-when-null parity.

<!-- step:exerciseentity-refactor-ef-migration -->
### Step 4 — ExerciseEntity refactor + EF migration

Refactor `ExerciseEntity` from `Key`(int) + `ProgressionId` + `RhythmId` to: `SongId`, `CompingPatternId`, `LeadPatternId?` (nullable), `KeyOverride` (string key token; null → song key), `Tempo`, `Difficulty` (by name), `Feel` (by name), `CreatedUtc`. Update `ExerciseLibraryHandler.Save`/`ToExercise`/`Load` and `ExerciseSummary` to the reference shape — resolve `SongId`/pattern ids against the stores and fail loud if a referenced row is missing (design §3, same rule as Song→Progression refs). Update `ChordFlowDbContext.OnModelCreating` (add `Feel` `HasConversion<string>`; keep the PracticeRecords FK). Add an EF migration dropping `Key`/`ProgressionId`/`RhythmId` and adding the new columns — no data preservation (no users); re-express any seed exercises against Songs.

<!-- step:ref-doc-sync-done-doc -->
### Step 5 — Ref-doc sync + done doc

Update `chordflow-domain-model-reference.md`: the canonical `Exercise` shape, the Features-expand pipeline (renderer stays store-free, expansion is the Features I/O seam — decision (A)), and the two-track/dead-note render. Check `chordflow-architecture-reference.md` is still accurate and add a line if the expand-in-Features seam needs one. Write the thread `done` doc summarizing what shipped (record merge, OfProgression, startKey, two-track lead, entity refactor + migration).
