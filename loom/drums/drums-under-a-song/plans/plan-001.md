---
type: plan
id: pl_01KXXR7D3WKTN5SGPREPCRWMHA
title: Drums under a song — parts-union remodel + drum track
status: done
created: 2026-07-19
updated: 2026-07-19
version: 1
design_version: 1
req_version: 1
tags: []
parent_id: de_01KXXQ9ECKBQZP1AN8WYM7KRNT
requires_load: []
target_version: 0.1.0
steps:
  - id: domain-the-instrument-parts-union
    order: 1
    status: done
    description: Introduce the typed InstrumentPart union and remodel Exercise to hold Parts, behavior-preserving (full suite green, no drums rendered yet).
    files_touched: [src/ChordFlow.Core/Exercises/Exercise.cs, src/ChordFlow.Core/Exercises/InstrumentPart.cs, tests/ChordFlow.Core.Tests/ExercisePipelineTests.cs, tests/ChordFlow.Core.Tests/ExerciseProjectionsTests.cs]
    blocked_by: []
    satisfies: [IN1, C4, C5]
  - id: renderer-the-drum-track-concrete-tiled
    order: 2
    status: done
    description: AlphaTexRenderer emits a 3rd \track percussion staff when a DrumPart is present, composing the concrete DrumGrooveRenderer with cyclic per-bar tiling; ExerciseRendering extracts and passes the optional drum part.
    files_touched: [src/ChordFlow.Core/Rendering/AlphaTexRenderer.cs, src/ChordFlow.Core/Rendering/DrumGrooveRenderer.cs, src/ChordFlow.Core/Features/ExerciseRendering.cs, tests/ChordFlow.Core.Tests/Rendering/AlphaTexRendererTests.cs]
    blocked_by: []
    satisfies: [IN2, IN3, IN6, C1, C2, C3, C6]
  - id: features-resolve-generate-wiring
    order: 3
    status: done
    description: ExerciseRefs.ResolveDrumGroove resolves the optional groove id; GenerateExercise.Build appends a DrumPart from drumGrooveId + drumVolume.
    files_touched: [src/ChordFlow.Core/Features/ExerciseRefs.cs, src/ChordFlow.Core/Features/GenerateExercise/GenerateExercise.cs, tests/ChordFlow.Core.Tests/ExerciseLibraryTests.cs]
    blocked_by: [domain-the-instrument-parts-union]
    satisfies: [IN8]
  - id: persistence-entity-column-migration-flat-mapper
    order: 4
    status: done
    description: ExerciseEntity gains a nullable DrumGrooveId + per-part volume/mute columns via a flat Exercise↔Entity mapper + an EF migration; the saved-exercise load path resolves the groove.
    files_touched: [src/ChordFlow.Core/Persistence/Entities/ExerciseEntity.cs, src/ChordFlow.Core/Persistence/ChordFlowDbContext.cs, src/ChordFlow.Core/Features/ExerciseLibrary/ExerciseLibrary.cs, src/ChordFlow.Core/Persistence/Migrations, tests/ChordFlow.Core.Tests/ExerciseLibraryTests.cs]
    blocked_by: [features-resolve-generate-wiring]
    satisfies: [IN7, IN8, C7]
  - id: ui-harmonycontrolsr-drums-picker-volume-staff
    order: 5
    status: done
    description: "The generate verb carries drumGrooveId + drumVolume; HarmonyControlsR gains a Drums picker (entity:\"drums\") + volume slider; a display-only drum-staff show/hide toggle."
    files_touched: [src/ChordFlow.Desktop/wwwroot/harmony-controls-component.js, src/ChordFlow.Desktop/wwwroot/score-render-component.js, src/ChordFlow.Desktop/wwwroot/app.js, src/ChordFlow.Core/Bridge, src/ChordFlow.Core/Features/GenerateExercise/GenerateExercise.cs]
    blocked_by: [persistence-entity-column-migration-flat-mapper]
    satisfies: [IN4, IN5]
  - id: reference-doc-sync-end-to-end
    order: 6
    status: done
    description: Update the domain-model + architecture refs for the parts-union remodel and the drum-track render path; run the full slice live (pick groove → audible under a 12-bar blues → save/reload → toggle staff).
    files_touched: [loom/refs/chordflow-domain-model-reference.md, loom/refs/chordflow-architecture-reference.md]
    blocked_by: [ui-harmonycontrolsr-drums-picker-volume-staff]
    satisfies: [IN9]
---
# Drums under a song — parts-union remodel + drum track

## Goal

Layer a `DrumGroove` beneath a harmonic exercise by remodeling the play-unit into a typed instrument-parts union and adding a percussion track to the render path. Built on design.md D1–D5 and the locked req (IN1–IN9 / C1–C7). The order is a clean vertical slice: first a behavior-preserving domain remodel (Exercise → Parts, suite stays green), then the renderer's drum track (concrete DrumGrooveRenderer, cyclic tiling, shared \tf), then the Features resolve + generate wiring, then flat persistence + migration + the load path, then the HarmonyControlsR picker + staff toggle, and finally reference-doc sync + a live end-to-end pass. The drum track composes the concrete DrumGrooveRenderer — this thread does not depend on chordflow/instrument-rendering (C3); Song stays instrument-agnostic (C5); persistence stays flat behind a non-breaking seam (C7).

---

## Steps

| Done | # | Step | Files touched | Blocked by | Satisfies |
|---|---|---|---|---|---|
| ✅ | 1 | Introduce the typed InstrumentPart union and remodel Exercise to hold Parts, behavior-preserving (full suite green, no drums rendered yet). | src/ChordFlow.Core/Exercises/Exercise.cs, src/ChordFlow.Core/Exercises/InstrumentPart.cs, tests/ChordFlow.Core.Tests/ExercisePipelineTests.cs, tests/ChordFlow.Core.Tests/ExerciseProjectionsTests.cs | — | IN1, C4, C5 |
| ✅ | 2 | AlphaTexRenderer emits a 3rd \track percussion staff when a DrumPart is present, composing the concrete DrumGrooveRenderer with cyclic per-bar tiling; ExerciseRendering extracts and passes the optional drum part. | src/ChordFlow.Core/Rendering/AlphaTexRenderer.cs, src/ChordFlow.Core/Rendering/DrumGrooveRenderer.cs, src/ChordFlow.Core/Features/ExerciseRendering.cs, tests/ChordFlow.Core.Tests/Rendering/AlphaTexRendererTests.cs | — | IN2, IN3, IN6, C1, C2, C3, C6 |
| ✅ | 3 | ExerciseRefs.ResolveDrumGroove resolves the optional groove id; GenerateExercise.Build appends a DrumPart from drumGrooveId + drumVolume. | src/ChordFlow.Core/Features/ExerciseRefs.cs, src/ChordFlow.Core/Features/GenerateExercise/GenerateExercise.cs, tests/ChordFlow.Core.Tests/ExerciseLibraryTests.cs | domain-the-instrument-parts-union | IN8 |
| ✅ | 4 | ExerciseEntity gains a nullable DrumGrooveId + per-part volume/mute columns via a flat Exercise↔Entity mapper + an EF migration; the saved-exercise load path resolves the groove. | src/ChordFlow.Core/Persistence/Entities/ExerciseEntity.cs, src/ChordFlow.Core/Persistence/ChordFlowDbContext.cs, src/ChordFlow.Core/Features/ExerciseLibrary/ExerciseLibrary.cs, src/ChordFlow.Core/Persistence/Migrations, tests/ChordFlow.Core.Tests/ExerciseLibraryTests.cs | features-resolve-generate-wiring | IN7, IN8, C7 |
| ✅ | 5 | The generate verb carries drumGrooveId + drumVolume; HarmonyControlsR gains a Drums picker (entity:"drums") + volume slider; a display-only drum-staff show/hide toggle. | src/ChordFlow.Desktop/wwwroot/harmony-controls-component.js, src/ChordFlow.Desktop/wwwroot/score-render-component.js, src/ChordFlow.Desktop/wwwroot/app.js, src/ChordFlow.Core/Bridge, src/ChordFlow.Core/Features/GenerateExercise/GenerateExercise.cs | persistence-entity-column-migration-flat-mapper | IN4, IN5 |
| ✅ | 6 | Update the domain-model + architecture refs for the parts-union remodel and the drum-track render path; run the full slice live (pick groove → audible under a 12-bar blues → save/reload → toggle staff). | loom/refs/chordflow-domain-model-reference.md, loom/refs/chordflow-architecture-reference.md | ui-harmonycontrolsr-drums-picker-volume-staff | IN9 |
---

### Legend

| Symbol | Meaning |
|--------|---------|
| ✅ | Done |
| 🔄 | In Progress |
| 🔳 | Pending |
| ❌ | Cancelled |

<!-- step:domain-the-instrument-parts-union -->
### Step 1 — Domain — the instrument-parts union

Add `abstract record InstrumentPart { double Volume = 1.0; bool Muted }` with arms `CompingPart(RhythmPattern)` / `LeadPart(RhythmPattern)` / `DrumPart(DrumGroove)`. Remodel `Exercise` to `(Song, IReadOnlyList<InstrumentPart> Parts, Key? KeyOverride, int Tempo, Difficulty, TripletFeel)`. Add intent accessors — `Comping` (the required CompingPart's pattern), `Lead` (optional), `Drums` (optional DrumGroove) — so existing readers (AlphaTexRenderer, ExerciseRendering) keep calling `exercise.Comping`/`exercise.Lead` unchanged. Enforce construction invariants: exactly one CompingPart; at most one LeadPart and one DrumPart (C4). Update every constructor site (GenerateExercise, ExerciseRefs callers, tests) to build `Parts`. `DrumPart` exists but is not yet rendered. Song is untouched — the groove lives on the part, never on Song (C5). Full Core suite green.

<!-- step:renderer-the-drum-track-concrete-tiled -->
### Step 2 — Renderer — the drum track (concrete, tiled, shared feel)

When `exercise.Drums` is present, emit a 3rd `\track` percussion staff by composing the existing concrete `DrumGrooveRenderer` (C3 — no IInstrument, no dependency on instrument-rendering). Tile the groove cyclically across the RealizedSong's bar count: song bar i → groove bar (i mod m) (IN3); the groove and comping pattern tile independently. The drum track rides the same song-level `\tf` as the other tracks (IN6). Per-part Volume → the alphaTab track volume; Muted → volume 0 (staff still emitted). 4/4 only (C6). Renderer stays pure/store-free — the drum part is handed in extracted, the union is not passed into the renderer (C2); the drum-track emission is the allowed Rendering → Instruments edge (C1). ExerciseRendering.RenderCore extracts the optional DrumPart and passes it alongside comping + lead; the chord schedule + chord-sheet projection are unchanged. Tests: a fixture Exercise with a DrumPart renders a tiled percussion track; a 12-bar song over a 2-bar groove tiles 6×; \tf shared.

<!-- step:features-resolve-generate-wiring -->
### Step 3 — Features — resolve + generate wiring

Add `ExerciseRefs.ResolveDrumGroove(string? id, ChordFlowDbContext db)` via `DrumGrooveStore.Find` — optional, blank/null ⇒ null (mirrors ResolveOptionalPattern). `GenerateExercise.Build` resolves the optional `drumGrooveId` + `drumVolume` and appends a `DrumPart` to `Parts` (absent ⇒ no drum part). Tests: generate with a groove id yields an Exercise carrying a DrumPart; blank yields none; a missing id fails loud.

<!-- step:persistence-entity-column-migration-flat-mapper -->
### Step 4 — Persistence — entity column, migration, flat mapper, load path

Add nullable `DrumGrooveId` + per-part volume/mute columns to `ExerciseEntity`; EF migration on the `Exercises` table (applied on startup via Migrate()). A flat `Exercise ↔ ExerciseEntity` mapper translates the fixed v1 part set ↔ columns (C7 — the domain union is durable; only the mapping is provisional behind a non-breaking internal seam). The load path resolves the stored `DrumGrooveId` via `ExerciseRefs.ResolveDrumGroove` into a DrumPart (IN8, load side). Tests: save an exercise with a DrumPart → reload restores the groove + volumes; save without → no drum part on reload.

<!-- step:ui-harmonycontrolsr-drums-picker-volume-staff -->
### Step 5 — UI — HarmonyControlsR Drums picker, volume, staff toggle

HarmonyControlsR gains a Drums picker populated from `entityList entity:"drums"` (same pattern as comping/lead) + a volume slider bound to the page engine's setTrackVolume; the drum part enters getDefinition(); blank selection ⇒ no drum part. The `generate` verb carries the optional `drumGrooveId` + `drumVolume` (Bridge DTO + WebMessageRouter → GenerateExercise). The drum-staff show/hide is a display-only toggle (flips staff visibility via api.render(), no C# re-render — the staffProfile sibling; IN5); audio always emitted. Placement of the toggle (ScoreR display strip vs HarmonyControlsR) decided here per Rafa. Verified live via CDP: pick a groove → drums audible under the comp; toggle hides/shows the staff without a re-render.

<!-- step:reference-doc-sync-end-to-end -->
### Step 6 — Reference-doc sync + end-to-end

Update `chordflow-domain-model-reference.md` (the Exercise pipeline now over a typed InstrumentPart union; the drum track in the render path; tiling) and `chordflow-architecture-reference.md` (the play-unit remodel, the drum track, the HarmonyControlsR Drums picker + staff toggle, the generate-verb fields) — same unit of work (IN9). Final e2e via the CDP harness: pick a groove in HarmonyControlsR under a real 12-bar blues, generate → drums audible tiling across the full form in sync with the comp; a swung song swings comp + drums together with no double-swing; save → reload restores the groove; the staff toggle shows/hides without a re-render. Full Core suite green, Music → Instruments architecture test green.
