---
type: done
id: pl_01KTXQD6EYEV6FVZJET4K7E4FB-done
title: Done — Song — first slice
status: done
created: "2026-06-12T00:00:00.000Z"
version: 6
tags: []
parent_id: pl_01KTXQD6EYEV6FVZJET4K7E4FB
requires_load: []
---
# Done — Song — first slice

## Step 1 — Song domain model + output types + guarded Song.FromSections factory

**Domain model (IN1, C1, C6).** Added under `src/ChordFlow.Core/Domain/Song/` (kept in `namespace ChordFlow.Domain` — flat, to avoid a `ChordFlow.Domain.Song.Song` namespace/type collision):

- `Modulation.cs` — `readonly record struct Modulation(int Semitones, bool? ModeChange)` + `Key Apply(Key)` (tonic shift folded mod 12; `IsMinor` flips only when `ModeChange` set; relative shifts accumulate when folded).
- `RealizedSong.cs` — `RealizedSection(string Label, Key Key, IReadOnlyList<RealizedBar> Bars)` and `RealizedSong(IReadOnlyList<RealizedSection>)`; reuse the existing `RealizedBar`, no alphaTex (design §8.2). `Key` is a fold output, never an input.
- `Song.cs` — `Part`/`ProgressionReference`/`InlineProgression`, `ArrangementItem`/`PartPlay`/`RelativeMod`/`AbsoluteKey`, and `Song` with the guarded `Song.FromSections(...)` factory (the only constructor). Guards: every `PartPlay.PartName` ∈ `Parts`, `ProgressionReference.ProgressionId` non-empty, `Repeat >= 1`, at least one `PartPlay`. Throws `ArgumentException` naming the offending item (parser layer will raise `FormatException` for grammar — mirrors `Progression.FromBars`).

Tests: `SongModelTests.cs` — 9 passing (Apply fifth/wrap/mode-flip/accumulate + factory happy path + 4 guards).

## Step 2 — SongExpander.Expand + IProgressionStore seam (reference resolution + modulation fold)

**SongExpander + IProgressionStore (IN2, C1, C2, C3).**

- `IProgressionStore.cs` — `Progression? Find(string id)`, the I/O-free lookup seam in `Domain/`; concrete DB impl deferred to step 5.
- `SongExpander.cs` — `Expand(Song, IProgressionStore) -> RealizedSong`. Left-to-right fold of a running key: `AbsoluteKey` resets, `RelativeMod` accumulates via `Modulation.Apply`, `PartPlay` appends `Repeat` copies of `Transposer.RealizeBars(prog, key)` as `RealizedSection`s. Uses `RealizeBars` (not the legacy `Realize`) so sections carry full `RealizedBar` data the renderer consumes. `Resolve` is local-first via `Song.Parts` (inline shadows stored — the parser builds the dict local-first); a stored `ProgressionReference` hits the store and throws `reference 'x' not found` when the row is gone (fail loud, C4).

Tests: `SongExpanderTests.cs` — 6 passing (modulation accumulation, absolute reset/return-home, repeat→N sections w/ label+key, local-shadows-store, stored reference resolves, unresolved reference fails loud).

## Step 3 — SongParser (peer of ProgressionParser) for the Song DSL

**SongParser (IN3, C5, C6).** `SongParser.Parse(id, name, dsl, ts) -> Song`, pure, no I/O. Two passes: pass 1 pulls definitions into `Parts` (order-free), pass 2 walks the order-significant stream.

Grammar: `NAME = <prog-dsl>` → `InlineProgression` (RHS via `ProgressionParser.Parse`); `NAME: <stored-id>` → `ProgressionReference`; leading `key <tok>` → `InitialKey`, later `key <tok>` → `AbsoluteKey` reset; `NAME`/`NAME x<n>` → `PartPlay` (n default 1); `mod <spec>` → `RelativeMod`. `#` line comments stripped. `x<n>` is the only repeat syntax; `@repeat` not parsed (reserved, C5). Default `InitialKey` = C major (C6).

Decisions: `=` discriminates inline before `:` (so an inline RHS may carry `:slots`). **Stream play names must be defined locally** — undefined → `FormatException` naming it (design §3); no auto-referencing of bare names (declare `name: storedId` to use a stored progression). `key`/`mod` reserved. Mod spec: `+n`/`-n`, roman `I..VII` with optional `b`/`#` accidental, lowercase numeral → mode-flip to minor. Key spec: note letter + `#`/`b` + optional `m`/`min`.

Tests: `SongParserTests.cs` — 15 passing (full sketch round-trip, default key, reference def, absolute-reset mid-stream, 6-row mod-spec theory, repeat default, + 4 grammar-error cases).

## Step 4 — Section-aware renderer entry point + RenderBars extraction + SongExercise model

**Section-aware renderer + RenderBars extraction + SongExercise (IN5, C3).**

- `SongExercise.cs` — `record SongExercise(Song, RhythmPattern, int Tempo, Difficulty, Feel = Straight)`, the play analog of `Exercise` (decision D).
- `IScoreRenderer` — added `string Render(RealizedSong, RhythmPattern, int tempo, Difficulty, Feel = Straight)`.
- `AlphaTexRenderer` — extracted the per-bar body loop into `private RenderBars(bars, feltEvents, ts, difficulty, barLines, ref currentDuration)`, shared verbatim by both entry points. `Render(Exercise)` refactored onto it + a shared `AppendHeader(...)` and `EnsureMajorSupported(...)`; output byte-identical (full suite 217/217 still green). New `Render(RealizedSong, …)`: one header seeded from the first section's key, then per section an inline `\ks` **only on key change**, with `currentDuration` threaded across section seams. AlphaTexRenderer stays the only alphaTex-aware code (C3).

Decisions: `RenderBars` takes pre-realized `RealizedBar`s + felt events (key omitted from the design's nominal signature — bars are already key-resolved, so the key is unused for bar rendering; cleaner than carrying a dead param). **No `\section` marker emitted** — `\section` is not in the verified alphaTex reference and the renderer refuses unverified tokens; `RealizedSection.Label` is retained in the domain for the future play cursor (UI wiring is EX5). Header title for a Song = first section's `Label — keyName` (the RealizedSong carries no song name; the signature is the design-pinned one).

Tests: `SongRenderTests.cs` — 5 passing (single-section bar-body parity with `Render(Exercise)`, two-section 24-bar concat with no `\ks`, exactly one inline `\ks` on a key change, `:N` duration carried across the seam, empty-song throws).

## Step 5 — SongEntity persistence parity, DbContext wiring, concrete IProgressionStore, built-in seeding

**Persistence + concrete store + seeding (IN4, C4).**

- `Entities/SongEntity.cs` — field-for-field parity with `ProgressionEntity` (`Id`/`Name`/`Dsl`/`Origin`/`PackId`/`Genre`/`Subgenre`/`Tags`/`CreatedUtc`, `IOriginated`). `Dsl` is the only stored form; `RealizedSong`/alphaTex never persisted (C4).
- `ChordFlowDbContext` — added `DbSet<SongEntity> Songs`, `OnModelCreating` config (string PK, `Origin` `HasConversion<string>()`, `Tags` default `[]`), and `SeedBuiltInSongs()` (idempotent insert-by-Id, denormalizes the catalog header — mirrors `SeedBuiltInProgressions()`).
- `ProgressionStore.cs` — concrete `IProgressionStore` over the context; `Find(id)` reads the row, strips the catalog header, re-parses via `ProgressionParser` (store-the-definition / regenerate-on-load). Lives in `Persistence/`; `Domain/` only sees the interface (C3).
- `SeedData.cs` — `SongDefinition` record + `BuiltInSongs` with one demo (`blues_song_demo`) exercising inline parts, a stored `verse: 12bar_blues` reference, `verse x2`, and `mod V`, under a `genre/subgenre/tags` header.
- EF migration `20260612111742_AddSongs` — creates the `Songs` table (verified: string PK, `Tags` default `[]`, nullable `PackId`/`Genre`/`Subgenre`).

Tests: `SongPersistenceTests.cs` — 4 passing (migrate + seed + full DSL→parse→expand-over-store→render round-trip with denormalized genre/subgenre/tags; idempotent reseed; Origin stored by name; store Find hit/miss).

## Step 6 — Seeded example song + public Song DSL reference doc

**Seeded example + DSL reference (IN6).**

- Seed example (`blues_song_demo`) authored in `SeedData.BuiltInSongs` during Step 5 (needed for `SeedBuiltInSongs` to compile); covered end-to-end here.
- `loom/refs/chordflow-dsl-reference.md` — **folded the Song DSL into the existing reference** (per Rafa's call to keep all DSL together) rather than a new doc: new **Song DSL** section (definitions `NAME =` / `NAME:`, the `key`/`mod`/`x<n>` stream, the mod-spec table, `x` vs reserved `@repeat`, a full worked example, common errors, notes). Updated the frontmatter `description` and intro note to cover both DSLs. README "Documentation" link relabelled Progression DSL → DSL guide covering both. (refs/README are gate-excluded → direct edits.)
- Plan Step 6 `files_touched` updated to point at the existing reference doc.

Tests: `SongSeedTests.cs` — 2 passing (every `BuiltInSongs` def parses→expands→renders against a seed-progression store; `blues_song_demo` has 5 sections incl. a 12-bar verse and a G (mod V) section). DB seeding path covered separately by `SongPersistenceTests`.

**Full suite: 223/223 green.**
