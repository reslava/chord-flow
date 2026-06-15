---
type: done
id: pl_01KV6482DNFNKGX8R1MPMV6DXG-done
title: Done — Exercise keystone — record merge, two-track render, persistence
status: done
created: "2026-06-15T00:00:00.000Z"
version: 5
tags: []
parent_id: pl_01KV6482DNFNKGX8R1MPMV6DXG
requires_load: []
---
# Done — Exercise keystone — record merge, two-track render, persistence

## Step 1 — Rewrite `Exercise` to the merged shape, add `Song.OfProgression`, add optional `startKey` to `SongExpander.Expand`, delete `SongExercise`

**Canonical `Exercise` record + `Song.OfProgression` + `SongExpander.startKey`; deleted `SongExercise`.**

- `Domain/Exercise.cs` — rewritten to `record Exercise(Song Song, RhythmPattern Comping, RhythmPattern? Lead, Key? KeyOverride, int Tempo, Difficulty Difficulty, Feel Feel = Straight)`. Supersedes the old `Exercise(Key, Progression, …)` and the `song` thread's `SongExercise` (decision (a)).
- `Domain/Song/Song.cs` — added `static Song OfProgression(Progression p, Key initialKey)`: one inline part named after the progression (so the rendered title reads "{prog.Name} — {key}", preserving the old `Render(Exercise)` title), played once. Docstring no longer points at the deleted `SongExercise`.
- `Domain/Song/SongExpander.cs` — `Expand` gained optional `Key? startKey = null`; seeds the modulation fold from `startKey ?? song.InitialKey` (additive; relative mods still accumulate, an `AbsoluteKey` still overrides mid-arrangement). This is how `KeyOverride` transposes the whole song.
- `Domain/Song/SongExercise.cs` — **deleted** (it had no consumers beyond its own file + one docstring).

IN1 / IN2 / IN3. Won't compile alone — old-`Exercise` callers are repointed in Step 2 (same compile unit).

## Step 2 — Move Song-expansion into Features; remove `Render(Exercise)`; add `lead` param to the RealizedSong overload; repoint all old-Exercise callers so the solution is green

**Renderer merge per decision (A) — Features expand, `Render(Exercise)` dropped, all callers repointed. Build + 388 tests green.**

- `Rendering/IScoreRenderer.cs` — removed `Render(Exercise)`; the sole entry point is now `Render(RealizedSong, rhythm, tempo, difficulty, feel, RhythmPattern? lead = null, RenderOptions? options = null)`. Docstring states the renderer is pure/store-free.
- `Rendering/AlphaTexRenderer.cs` — deleted the `Render(Exercise)` method; **ported its pickup/anacrusis handling** into the `RealizedSong` path (voiced with the first chord of the first section); added the `lead` param (rendered in Step 3 — accepted/ignored for now). All shared private helpers stay used.
- `Features/ExerciseRendering.cs` (new) — the single expand+render primitive `RenderToTex(Exercise, IProgressionStore, IScoreRenderer, RenderOptions?)`: computes `baseKey = KeyOverride ?? Song.InitialKey`, `SongExpander.Expand(Song, store, startKey: baseKey)`, then `Render(RealizedSong, Comping, …, lead: Lead, …)`. This is the one I/O seam (decision (A)) — renderer never touches the store.
- `Features/GenerateExercise/GenerateExercise.cs` — `LoadScoreEnvelope.From` now takes an `IProgressionStore` and routes through `ExerciseRendering`. `GenerateExerciseHandler` ctor gains `DbContextOptions`; `Build` returns the new `Exercise` shape via `Song.OfProgression`; `Generate` opens a short-lived context for the store.
- `Features/ExerciseLibrary/ExerciseLibrary.cs` — `Save` maps the new shape onto the still-old entity (`Song.Id`→ProgressionId, `Comping.Id`→RhythmId, `KeyOverride ?? Song.InitialKey`→Key); `ToExercise` rebuilds via `Song.OfProgression`; `Load` expands via `new ProgressionStore(db)`.
- `Features/ContentCrud/ContentCrudHandler.cs` — progression/rhythm previews build the new `Exercise` via `Song.OfProgression` and render through `ExerciseRendering` against the live db's store; `ScorePreview` takes the `db`.
- `Desktop/WebHost/SwappableRenderer.cs` + `Desktop/Program.cs` — dropped the `Render(Exercise)` delegation; `GenerateExerciseHandler(dbOptions, renderer)`; `TrySendScore` opens a short-lived context + `ProgressionStore`.
- **Tests** — new `RenderTestHelpers.RenderProgression` extension (single-section RealizedSong labelled with the prog name → byte-identical to old `Render(Exercise)`); migrated `AlphaTexRendererTests`, `ExercisePipelineTests`, `ProgressionSeedTests`; rewrote the `SongRenderTests` parity test to assert `Song.OfProgression` lift == manual single-section song body. The minor-key rejection test now builds a minor `RealizedSection` directly.

IN1 / C1 / C3. Single-track output is byte-identical (all prior assertions pass unchanged).

## Step 3 — Render the optional Lead pattern as a second `\track` of dead notes; single-track byte-identical when null; document `\track` in the ref

**Two-track lead staff (dead notes) + alphaTex ref update. 391 tests green (388 + 3 new).** Multi-track skeleton confirmed by Rafa against the structural-metadata / document-structure docs.

- `Rendering/AlphaTexRenderer.cs` — `Render(RealizedSong, …, lead)` now branches:
  - **`lead == null`** → single track, **byte-identical** to the pre-lead output (`\ts`/`\ks` in the header, no `\track`). All prior string assertions pass unchanged.
  - **`lead != null`** → two tracks. Score metadata (`\title`/`\subtitle`/`\tempo` + chord directives) + the lone `.` at top, then a `\track "Comping" "comp" { defaultSystemsLayout 4 }` and a `\track "Lead" "lead" { defaultSystemsLayout 4 }`, each carrying its own `\ts`/`\ks` (bar metadata). 4 bars per row.
  - Extracted `BuildCompingBars` (pickup + section walk, shared) and `BuildLeadBars` (lead body). Split `AppendHeader` into `AppendScoreMetadata` + `AppendTrackHeader` + shared `AppendChordDirectives`.
  - `BuildLeadBars` renders the `Lead` pattern as dead notes via new `RenderLeadBar`: each hit → `x.3` (string 3, rhythm only), each rest → `r`, with the same stateful `:N` + `{tu N}` rules and a **fresh RenderState** (alphaTex duration state does not carry across tracks). The lead tiles per-section exactly as the comping pattern; a comping pickup is mirrored as a leading **rest** bar so the staves stay master-bar aligned.
- `loom/refs/alphatex-syntax-reference.md` — added a verified **Multiple tracks (two staves)** section (the `\track "Name" "short" { defaultSystemsLayout N }` skeleton, score-vs-bar metadata placement) and a **Dead / muted notes** section (`x.3`, `3.3{x}`).
- Tests (`AlphaTexRendererTests`): two-track emits both `\track`s with per-track `\ts`/`\ks`, one `.` terminator, comping unchanged, lead as `:4 x.3 x.3 x.3 x.3 |`; single-track has no `\track`; pickup is mirrored as `:4 r |` on the lead.

IN5 / C2.

## Step 4 — Move the entity from Key/ProgressionId/RhythmId to references + param columns; add the EF migration

**`ExerciseEntity` refactor + EF migration. Full solution builds; 394 tests green (391 + 3 new ExerciseLibrary round-trip tests).**

- `Persistence/Entities/ExerciseEntity.cs` — refactored from `(Key int, ProgressionId, RhythmId)` to references + params: `SongId`, `CompingPatternId`, `LeadPatternId?`, `KeyOverride?` (key token), `Tempo`, `Difficulty`, `Feel`, `CreatedUtc`. Kept the `int Id` surrogate PK + PracticeRecords FK (req IN4 = "drop ProgressionId / add columns", not change the PK).
- `Persistence/ChordFlowDbContext.cs` — `Feel` stored by name (`HasConversion<string>`), alongside `Difficulty`.
- `Persistence/Migrations/20260615185159_RefactorExerciseToSongRefs.cs` — EF scaffolded positional **renames** that mismapped data (`RhythmId`→`SongId`, `ProgressionId`→`Feel`); rewrote `Up`/`Down` as a clean **drop-and-add** (no data preservation — no users, IN4).
- `Domain/NoteSpeller.cs` — added `KeyFromSignatureToken` (inverse of `KeySignatureToken`) so the `KeyOverride` token round-trips to a major `Key`.
- `Features/ExerciseLibrary/ExerciseLibrary.cs` — `Save` writes the new columns (`Lead?.Id`, key token via `KeySignatureToken`, `Feel`); `ToExercise` rebuilds from them (parses the `KeyOverride` token, resolves comping/lead patterns from `SeedData`, lifts the seed progression). `ExerciseSummary`/`List` reshaped to the new reference columns (+ `Feel`).
- `Features/GenerateExercise/GenerateExercise.cs` — `Build` now sets `KeyOverride = key`: for a lifted bare progression the Song isn't persisted (only its id), so `KeyOverride` is the chosen practice key's only persistent home.
- Tests: new `ExerciseLibraryTests` (Save→Load round-trips key/comping/lead/feel + two-track score when a lead is saved; single-track when not; `List` projects the new columns).

**Flag (UI, out of scope — `ui/exercise-workbench`):** `ExerciseSummary` changed shape, so `wwwroot/app.js`'s saved-exercise list (which reads the old `Key`/`RhythmId` fields) needs a rewire in the workbench thread (EX2 — no UI in this thread).

IN4.

## Step 5 — Update domain-model + architecture refs and write the thread done doc

**Ref-doc sync + done doc. Plan complete.**

- `loom/refs/chordflow-domain-model-reference.md` — §7 rewritten to the canonical `Exercise(Song, Comping, Lead?, KeyOverride?, …)` shape + the Features-expand pipeline (renderer pure, expansion is the one I/O seam, no `Render(Exercise)`) + the two-track/dead-note render; §5 renderer rows (`AlphaTexRenderer`, `IScoreRenderer`) corrected to the single `Render(RealizedSong, …, lead?, options?)` entry point; §8 invariant updated for the dead-note lead track.
- `loom/refs/chordflow-architecture-reference.md` — Rendering-layer line now says the renderer maps `RealizedSong → string` and is pure/store-free (expansion in `ExerciseRendering`, decision (a)); the §6 data-flow shows the Features expand step.
- `loom/refs/alphatex-syntax-reference.md` — already updated in Step 3 (Multiple-tracks + dead-note sections).
- This `done` doc (steps 1–5) is the thread's implementation record.

**Final state:** full solution builds; **394 tests green**. The whole plan (record merge → Features-expand renderer → two-track lead → entity refactor + migration → ref sync) is shipped.

**Open follow-up (out of scope — `ui/exercise-workbench`):** `wwwroot/app.js` reads the old `ExerciseSummary` fields (`Key`/`RhythmId`); the saved-exercise list needs a UI rewire to the new reference columns (EX2 — no UI in this thread).
