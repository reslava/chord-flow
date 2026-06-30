---
type: req
id: rq_01KTXTFZFJXVRXNN5MNN7XDJ7V
title: Rhythm DSL — authoring strum patterns as a tick grid — Requirements
status: locked
created: 2026-06-12
updated: 2026-06-12
version: 3
design_version: 14
tags: []
parent_id: de_01KTVVTS9HG5X2C39TC1X1KP94
requires_load: []
---
# Rhythm DSL — authoring strum patterns as a tick grid — Requirements

### ✅ Included

- `IN1` — `RhythmPatternEntity` (EF), mirror of `ProgressionEntity`: `Id` (string PK — slug for built-ins, GUID for user), `Name`, `Dsl` (canonical Rhythm-DSL string — the only persisted form), `TimeSignature` (Numerator/Denominator; 4/4 only today but stored so non-4/4 is additive), `Origin` (shared `BuiltIn`/`UserDefined`/`Pack`) + nullable `PackId`, `CreatedUtc`.
- `IN2` — DbContext wiring: `ChordFlowDbContext.RhythmPatterns` `DbSet` + entity config (`Origin` `HasConversion<string>()`, etc.) + an EF migration.
- `IN3` — `RhythmPatternDefinition(Id, Name, Dsl)` + `SeedData.BuiltInRhythmPatterns`: the three seeds as sustain-literal DSL — Beat 1 `X...............`, Beats 1 & 3 `X.......X.......`, Quarters `X...X...X...X...`. Analog of `BuiltInProgressions`.
- `IN4` — `SeedBuiltInRhythmPatterns()`: idempotent first-run seeding (insert missing by `Id`, never touch existing/user rows), called from `Program.cs` after `Migrate()`. Mirror of `SeedBuiltInProgressions`.
- `IN5` — Load round-trip: row → `RhythmPatternParser.Parse(dsl, ts)` → `RhythmPattern`; the grid/events are regenerated on load, never stored (`Dsl` is the single persisted form).
- `IN6` — Migrate the in-memory seeds (the intended behavior change): `SeedData.Beat1`/`Beat1And3`/`Quarters` become DSL-derived via the parser, so Beat 1 rings the whole bar and Beats 1 & 3 = two half notes. Flip the slice-1 guard test (was: sustain-literal seeds diverge from the staccato live seeds → now equal) and update the renderer/quantizer tests whose expected alphaTex changes.

### ❌ Excluded

- `EX1` — Rhythm-pattern authoring/editor UI (and pattern selection in the exercise UI) → separate thread.
- `EX2` — A full RhythmPattern library CRUD feature (save/list/delete, à la `ExerciseLibrary`) → additive later.
- `EX3` — Catalog metadata (genre/subgenre/tags header) on rhythm patterns → add additively only if packs need it.
- `EX4` — Pack import pipeline for rhythm patterns → the `Pack` origin column exists, but importing bundles belongs to the content-pack thread.
- `EX5` — Non-4/4 meters and new DSL grammar (`*` sugar, intra-group whitespace) → stay deferred (icebox).

### ⛓ Constraints

- `C1` — `Dsl` is the single persisted form; alphaTex and the parsed grid are never stored (regenerated on load) — exactly like progressions.
- `C2` — Domain stays I/O-free: the entity + seeding live in `Persistence/`; `SeedData` stays pure (it may parse DSL at init via the Domain parser, but never touches a DB).
- `C3` — Idempotent, provenance-safe seeding: never overwrites user/existing rows; `Origin` is the guard.
- `C4` — The seed-migration rendering change is intended (guitar rings, not staccato — slice-1 decision 1): expect and update the affected test expectations rather than preserve the old staccato output.
- `C5` — Backward-compatible: existing consumers of `SeedData.Beat1` etc. keep compiling — the constants stay; only their event content changes.