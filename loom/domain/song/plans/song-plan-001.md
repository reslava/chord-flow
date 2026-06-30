---
type: plan
id: pl_01KTXQD6EYEV6FVZJET4K7E4FB
title: Song — first slice
status: done
created: 2026-06-12
updated: 2026-06-12
version: 1
design_version: 8
req_version: 1
tags: []
parent_id: de_01KTVTNZPYS36K23R5Z9MYDB54
requires_load: []
target_version: 0.1.0
actual_release: 0.5.0
steps:
  - id: domain-model
    order: 1
    status: done
    description: Song domain model + output types + guarded Song.FromSections factory
    files_touched: [src/ChordFlow.Core/Domain/Song/Song.cs, src/ChordFlow.Core/Domain/Song/Modulation.cs, src/ChordFlow.Core/Domain/Song/RealizedSong.cs, tests/ChordFlow.Core.Tests/SongModelTests.cs]
    blocked_by: []
    satisfies: [IN1, C1, C6]
  - id: songexpander
    order: 2
    status: done
    description: SongExpander.Expand + IProgressionStore seam (reference resolution + modulation fold)
    files_touched: [src/ChordFlow.Core/Domain/Song/IProgressionStore.cs, src/ChordFlow.Core/Domain/Song/SongExpander.cs, tests/ChordFlow.Core.Tests/SongExpanderTests.cs]
    blocked_by: [1]
    satisfies: [IN2, C1, C2, C3]
  - id: songparser
    order: 3
    status: done
    description: SongParser (peer of ProgressionParser) for the Song DSL
    files_touched: [src/ChordFlow.Core/Domain/Song/SongParser.cs, tests/ChordFlow.Core.Tests/SongParserTests.cs]
    blocked_by: [1]
    satisfies: [IN3, C5, C6]
  - id: section-aware-render
    order: 4
    status: done
    description: Section-aware renderer entry point + RenderBars extraction + SongExercise model
    files_touched: [src/ChordFlow.Core/Domain/Song/SongExercise.cs, src/ChordFlow.Core/Rendering/IScoreRenderer.cs, src/ChordFlow.Core/Rendering/AlphaTexRenderer.cs, tests/ChordFlow.Core.Tests/SongRenderTests.cs]
    blocked_by: [1]
    satisfies: [IN5, C3]
  - id: persistence
    order: 5
    status: done
    description: SongEntity persistence parity, DbContext wiring, concrete IProgressionStore, built-in seeding
    files_touched: [src/ChordFlow.Core/Persistence/Entities/SongEntity.cs, src/ChordFlow.Core/Persistence/ChordFlowDbContext.cs, src/ChordFlow.Core/Persistence/ProgressionStore.cs, tests/ChordFlow.Core.Tests/SongPersistenceTests.cs]
    blocked_by: [2, 3]
    satisfies: [IN4, C4]
  - id: example-dsl-ref
    order: 6
    status: done
    description: Seeded example song + public Song DSL reference doc
    files_touched: [src/ChordFlow.Core/Domain/SeedData.cs, tests/ChordFlow.Core.Tests/SongSeedTests.cs, loom/refs/chordflow-dsl-reference.md]
    blocked_by: [3, 5]
    satisfies: [IN6]
---
# Song — first slice

## Goal

Implement the first Song slice exactly as the locked req (rq_01KTXQ81…) and design scope: a pure arrangement layer over Progressions that composes references, folds modulations over a running key into a RealizedSong, parses a small Song DSL, persists by Dsl with parity to ProgressionEntity, renders section-aware through the existing AlphaTexRenderer, and ships one seeded example plus a public DSL reference. The SongExpander slots in above Transposer; nothing in Domain/ harmony, Rendering/ bar logic, or the bridge below it changes. Every step is unit-tested in ChordFlow.Core.Tests, mirroring the progression work. Progression transforms, repeat endings/D.C./D.S., multi-meter, per-section rhythm overrides, and UI/library wiring are out of scope (EX1–EX5).

---

## Steps

| Done | # | Step | Files touched | Blocked by | Satisfies |
|---|---|---|---|---|---|
| ✅ | 1 | Song domain model + output types + guarded Song.FromSections factory | src/ChordFlow.Core/Domain/Song/Song.cs, src/ChordFlow.Core/Domain/Song/Modulation.cs, src/ChordFlow.Core/Domain/Song/RealizedSong.cs, tests/ChordFlow.Core.Tests/SongModelTests.cs | — | IN1, C1, C6 |
| ✅ | 2 | SongExpander.Expand + IProgressionStore seam (reference resolution + modulation fold) | src/ChordFlow.Core/Domain/Song/IProgressionStore.cs, src/ChordFlow.Core/Domain/Song/SongExpander.cs, tests/ChordFlow.Core.Tests/SongExpanderTests.cs | 1 | IN2, C1, C2, C3 |
| ✅ | 3 | SongParser (peer of ProgressionParser) for the Song DSL | src/ChordFlow.Core/Domain/Song/SongParser.cs, tests/ChordFlow.Core.Tests/SongParserTests.cs | 1 | IN3, C5, C6 |
| ✅ | 4 | Section-aware renderer entry point + RenderBars extraction + SongExercise model | src/ChordFlow.Core/Domain/Song/SongExercise.cs, src/ChordFlow.Core/Rendering/IScoreRenderer.cs, src/ChordFlow.Core/Rendering/AlphaTexRenderer.cs, tests/ChordFlow.Core.Tests/SongRenderTests.cs | 1 | IN5, C3 |
| ✅ | 5 | SongEntity persistence parity, DbContext wiring, concrete IProgressionStore, built-in seeding | src/ChordFlow.Core/Persistence/Entities/SongEntity.cs, src/ChordFlow.Core/Persistence/ChordFlowDbContext.cs, src/ChordFlow.Core/Persistence/ProgressionStore.cs, tests/ChordFlow.Core.Tests/SongPersistenceTests.cs | 2, 3 | IN4, C4 |
| ✅ | 6 | Seeded example song + public Song DSL reference doc | src/ChordFlow.Core/Domain/SeedData.cs, tests/ChordFlow.Core.Tests/SongSeedTests.cs, loom/refs/chordflow-dsl-reference.md | 3, 5 | IN6 |
---

### Legend

| Symbol | Meaning |
|--------|---------|
| ✅ | Done |
| 🔄 | In Progress |
| 🔳 | Pending |
| ❌ | Cancelled |

<!-- step:domain-model -->
### Step 1 — Domain model

Add `Song`, `Part` (`ProgressionReference` / `InlineProgression`), `ArrangementItem` (`PartPlay` / `RelativeMod` / `AbsoluteKey`), and `Modulation(int Semitones, bool? ModeChange)` with `Key Apply(Key current)` (shift tonic by Semitones mod 12, flip `IsMinor` when `ModeChange` is set). Output types `RealizedSong(IReadOnlyList<RealizedSection> Sections)` and `RealizedSection(string Label, Key Key, IReadOnlyList<RealizedBar> Bars)` live in `Domain/Song/` and reuse the existing `RealizedBar` (decision Q2 — pure keyed data, no alphaTex). `Song.FromSections(id, name, initialKey, parts, items)` is the only constructor, paralleling `Progression.FromBars`: validates every `PartPlay.PartName` resolves in `Parts`, `ProgressionReference.Name` non-empty (store resolution deferred to the expander), `Repeat >= 1`, and at least one `PartPlay`; throws `FormatException`/`ArgumentException` naming the offending item. `InitialKey` defaults to C major when unspecified by callers (C6 is enforced at the parser in step 3). Tests cover happy path + each guard.

<!-- step:songexpander -->
### Step 2 — SongExpander

`IProgressionStore { Progression? Find(string id); }` is the I/O-free lookup seam in `Domain/Song/` (keeps Domain/ I/O-free, C3). `SongExpander.Expand(Song song, IProgressionStore store) -> RealizedSong` runs the left-to-right fold carrying a running key: `AbsoluteKey` resets it, `RelativeMod` accumulates via `Modulation.Apply`, `PartPlay` resolves the part (local-first then store), then appends `Repeat` copies of `new RealizedSection(name, key, Transposer.Realize-bars(prog, key))`. `RealizedSection.Key` is an output of the fold, never an input (decision E). Unresolved reference -> clear domain error `reference 'x' not found` (fail loud, C4 resolution-time integrity). Tests: modulation accumulation, AbsoluteKey reset/return-home, local-shadows-store, Repeat expansion count, unresolved-reference throw.

<!-- step:songparser -->
### Step 3 — SongParser

`SongParser.Parse(id, name, dsl, ts) -> Song`, pure static, no I/O. Two regions: definitions (order-free) then the order-significant stream. Grammar: `key <token>` (definitions region, first) sets `InitialKey`, defaulting to C major when omitted (C6); `NAME = <prog-dsl>` -> `InlineProgression` with RHS handed verbatim to `ProgressionParser.Parse`; `NAME: <stored-id>` -> `ProgressionReference`; stream `NAME` / `NAME x<n>` -> `PartPlay(NAME, n)` with n defaulting to 1; `mod <spec>` -> `RelativeMod` (spec `+n`/`-n` and plain roman `V`/`IV`/`bIII` mapped to semitones per design §3; lowercase mode-flip form may land later); stream `key <token>` -> `AbsoluteKey` reset. `x<n>` is the only section-repeat syntax; `@repeat` is reserved (not parsed) for the future transform; `mod` is a stream token, never a section attribute (C5). Unknown stream name -> `FormatException` naming it. Tests cover each grammar form, the mod-spec table, key-omitted default, and malformed-input throws.

<!-- step:section-aware-render -->
### Step 4 — Section-aware render

`SongExercise(Song Song, RhythmPattern Rhythm, int Tempo, Difficulty Difficulty, Feel Feel = Feel.Straight)` — the play-target analog of `Exercise`. Extract today's per-bar body loop (`AlphaTexRenderer.cs:50-75`) into a private `RenderBars(IReadOnlyList<RealizedBar> bars, Key key, RhythmPattern rhythm, Difficulty difficulty, ref string? currentDuration)` shared by both entry points — per-bar logic genuinely untouched. Add `string Render(RealizedSong song, RhythmPattern rhythm, int tempo, Difficulty difficulty, Feel feel = Feel.Straight)` to `IScoreRenderer` + `AlphaTexRenderer`: one header seeded from the first section's key, then per section an inline `\ks` ONLY on key change + optional `Label` marker + `RenderBars(...)` with `currentDuration` flowing across section seams (decision Q3; `\ks` confirmed legal mid-score). `Render(Exercise)` is refactored to call `RenderBars` for its single section — output stays byte-identical. AlphaTexRenderer remains the only alphaTex-aware code (C3). Tests: single-section parity with `Render(Exercise)`, multi-section concatenation, `\ks` emitted only on key change, duration state carried across the seam.

<!-- step:persistence -->
### Step 5 — Persistence

`SongEntity` mirrors `ProgressionEntity` (Id, Name, Dsl, Origin, PackId, Genre, Subgenre, Tags, CreatedUtc; `IOriginated`) — `Dsl` is the only stored form, `RealizedSong`/alphaTex never persisted (C4). Add `DbSet<SongEntity> Songs`, `OnModelCreating` config (string PK, `Origin` `HasConversion<string>()`, `Tags` default `[]`), and `SeedBuiltInSongs()` — idempotent first-run insert by `Id` mirroring `SeedBuiltInProgressions()`, denormalizing the catalog header. Concrete `ProgressionStore : IProgressionStore` over `ChordFlowDbContext` (the DB-backed seam for step 2's interface; lives in Persistence/, Domain/ stays I/O-free). Referential integrity stays at resolution time, no DB FK (C4). Tests: round-trip row -> `SongParser.Parse(Dsl)` -> `SongExpander.Expand` over the store -> render; idempotent reseed; store Find hit/miss.

<!-- step:example-dsl-ref -->
### Step 6 — Example + DSL ref

Add a `SongDefinition(Id, Name, Dsl)` record + a `BuiltInSongs` set with one curated example song (e.g. intro/verse/chorus referencing the seeded blues + an inline part + one `mod`) to `SeedData`, wired into `SeedBuiltInSongs()`. Author the public-facing Song DSL reference at `loom/refs/` via `loom_create_reference` (peer of `chordflow-dsl-reference`): key line, inline `NAME =` / reference `NAME:`, stream `NAME x<n>`, `mod <spec>` table, `key` reset, and the `x` vs future `@repeat` distinction — link it from the README alongside the progression DSL ref. Tests: the example song parses, expands, and renders end-to-end without error; built-in seed is present and idempotent.
