---
type: plan
id: pl_01KTY962NNT74YZ51BA87CFJJB
title: Rhythm DSL — slice 2 (persistence + seed migration)
status: done
created: 2026-06-12
updated: 2026-06-12
version: 1
design_version: 14
req_version: 3
tags: []
parent_id: de_01KTVVTS9HG5X2C39TC1X1KP94
requires_load: []
target_version: 0.1.0
actual_release: 0.5.0
steps:
  - id: rhythmpatternentity-built-in-definitions
    order: 1
    status: done
    description: Add RhythmPatternEntity (Persistence/Entities) mirroring ProgressionEntity, and SeedData.RhythmPatternDefinition + BuiltInRhythmPatterns (the three seeds as sustain-literal DSL)
    files_touched: [src/ChordFlow.Core/Persistence/Entities/RhythmPatternEntity.cs, src/ChordFlow.Core/Domain/SeedData.cs]
    blocked_by: []
    satisfies: [IN1, IN3, C1]
  - id: dbset-ef-migration
    order: 2
    status: done
    description: Add ChordFlowDbContext.RhythmPatterns DbSet + OnModelCreating config (string PK, Origin HasConversion<string>) and generate the AddRhythmPatterns EF migration
    files_touched: [src/ChordFlow.Core/Persistence/ChordFlowDbContext.cs, src/ChordFlow.Core/Persistence/Migrations/]
    blocked_by: [1]
    satisfies: [IN2]
  - id: idempotent-seeding-program-cs-wiring
    order: 3
    status: done
    description: ChordFlowDbContext.SeedBuiltInRhythmPatterns() (idempotent, insert-missing-by-Id, Origin.BuiltIn) wired into Program.cs after Migrate(); idempotency test
    files_touched: [src/ChordFlow.Core/Persistence/ChordFlowDbContext.cs, src/ChordFlow.Desktop/Program.cs, tests/ChordFlow.Core.Tests/RhythmPatternSeedTests.cs]
    blocked_by: [2]
    satisfies: [IN4, C3]
  - id: rhythmpatternstore-load-round-trip
    order: 4
    status: done
    description: RhythmPatternStore.Find(id) reconstructs a RhythmPattern by parsing the stored Dsl with the row's TimeSignature; persistence round-trip test
    files_touched: [src/ChordFlow.Core/Persistence/RhythmPatternStore.cs, tests/ChordFlow.Core.Tests/RhythmPatternPersistenceTests.cs]
    blocked_by: [2]
    satisfies: [IN5, C1, C2]
  - id: migrate-in-memory-seeds-to-dsl
    order: 5
    status: done
    description: Make SeedData.Beat1/Beat1And3/Quarters DSL-derived via the parser so they ring (Beat 1 = whole bar, Beats 1 & 3 = two halves); flip the slice-1 guard test and update the rippled renderer/quantizer/overlay expectations
    files_touched: [src/ChordFlow.Core/Domain/SeedData.cs, tests/ChordFlow.Core.Tests/RhythmSeedDslTests.cs, tests/ChordFlow.Core.Tests/AlphaTexRendererTests.cs, tests/ChordFlow.Core.Tests/RhythmQuantizerTests.cs, tests/ChordFlow.Core.Tests/RhythmOverlayTests.cs]
    blocked_by: [1]
    satisfies: [IN6, C4, C5]
---
# Rhythm DSL — slice 2 (persistence + seed migration)

## Goal

Implement rhythm slice 2 exactly as the locked req (rq_01KTXTFZFJ…): persist rhythm patterns as first-class content (mirroring the existing progression/song persistence) and then migrate the three built-in seeds from hand-built RhythmEvent[] to DSL-derived sustain-literal patterns. The slice is deliberately sequenced in two halves. Steps 1–4 are pure plumbing with no behavior change — a RhythmPatternEntity + DbSet + EF migration, idempotent first-run seeding, and a load round-trip through RhythmPatternParser — so all 263 existing tests stay green throughout. Step 5 is the one intended behavior change (EX2 / slice-1 deferral): SeedData.Beat1/Beat1And3/Quarters become DSL-derived single-source-of-truth, so Beat 1 rings the whole bar and Beats 1 & 3 become two half notes (guitar rings, not staccato — slice-1 decision 1); the slice-1 guard test flips and the affected renderer/quantizer/overlay expectations are updated rather than preserved (C4). Dsl is the only persisted form — alphaTex and the parsed grid are regenerated on load, never stored (C1); the entity + seeding live in Persistence/, SeedData stays I/O-free (C2). Authoring/selection UI (EX1), a full CRUD library feature (EX2), catalog metadata on patterns (EX3), the pack import pipeline (EX4), and non-4/4 meters + new DSL grammar (EX5) are all out of scope.

---

## Steps

| Done | # | Step | Files touched | Blocked by | Satisfies |
|---|---|---|---|---|---|
| ✅ | 1 | Add RhythmPatternEntity (Persistence/Entities) mirroring ProgressionEntity, and SeedData.RhythmPatternDefinition + BuiltInRhythmPatterns (the three seeds as sustain-literal DSL) | src/ChordFlow.Core/Persistence/Entities/RhythmPatternEntity.cs, src/ChordFlow.Core/Domain/SeedData.cs | — | IN1, IN3, C1 |
| ✅ | 2 | Add ChordFlowDbContext.RhythmPatterns DbSet + OnModelCreating config (string PK, Origin HasConversion<string>) and generate the AddRhythmPatterns EF migration | src/ChordFlow.Core/Persistence/ChordFlowDbContext.cs, src/ChordFlow.Core/Persistence/Migrations/ | 1 | IN2 |
| ✅ | 3 | ChordFlowDbContext.SeedBuiltInRhythmPatterns() (idempotent, insert-missing-by-Id, Origin.BuiltIn) wired into Program.cs after Migrate(); idempotency test | src/ChordFlow.Core/Persistence/ChordFlowDbContext.cs, src/ChordFlow.Desktop/Program.cs, tests/ChordFlow.Core.Tests/RhythmPatternSeedTests.cs | 2 | IN4, C3 |
| ✅ | 4 | RhythmPatternStore.Find(id) reconstructs a RhythmPattern by parsing the stored Dsl with the row's TimeSignature; persistence round-trip test | src/ChordFlow.Core/Persistence/RhythmPatternStore.cs, tests/ChordFlow.Core.Tests/RhythmPatternPersistenceTests.cs | 2 | IN5, C1, C2 |
| ✅ | 5 | Make SeedData.Beat1/Beat1And3/Quarters DSL-derived via the parser so they ring (Beat 1 = whole bar, Beats 1 & 3 = two halves); flip the slice-1 guard test and update the rippled renderer/quantizer/overlay expectations | src/ChordFlow.Core/Domain/SeedData.cs, tests/ChordFlow.Core.Tests/RhythmSeedDslTests.cs, tests/ChordFlow.Core.Tests/AlphaTexRendererTests.cs, tests/ChordFlow.Core.Tests/RhythmQuantizerTests.cs, tests/ChordFlow.Core.Tests/RhythmOverlayTests.cs | 1 | IN6, C4, C5 |
---

### Legend

| Symbol | Meaning |
|--------|---------|
| ✅ | Done |
| 🔄 | In Progress |
| 🔳 | Pending |
| ❌ | Cancelled |

<!-- step:rhythmpatternentity-built-in-definitions -->
### Step 1 — RhythmPatternEntity + built-in definitions

New `RhythmPatternEntity : IOriginated` with `Id` (string PK — slug for built-ins, GUID for user), `Name`, `Dsl` (canonical Rhythm-DSL string — the only persisted form, C1), `TsNumerator`/`TsDenominator` (default 4/4, stored so non-4/4 is additive), `Origin` + nullable `PackId`, `CreatedUtc`. Unlike `ProgressionEntity` it carries **no** catalog metadata columns (Genre/Subgenre/Tags) — patterns aren't genre-filtered (EX3). In `Domain/SeedData.cs` add `public sealed record RhythmPatternDefinition(string Id, string Name, string Dsl)` (analog of `ProgressionDefinition`) and `BuiltInRhythmPatterns`: Beat 1 `X...............`, Beats 1 & 3 `X.......X.......`, Quarters `X...X...X...X...` (ids `beat_1`/`beat_1_3`/`quarters`). Pure, no I/O (C2). No DbContext/seeding wiring yet — that's steps 2–3.

<!-- step:dbset-ef-migration -->
### Step 2 — DbSet + EF migration

Add `public DbSet<RhythmPatternEntity> RhythmPatterns => Set<RhythmPatternEntity>();` and an `OnModelCreating` block mirroring the Progression/Song config: `HasKey(x => x.Id)` and `Property(x => x.Origin).HasConversion<string>()` (no Tags default — no catalog columns). Generate the migration with `dotnet ef migrations add AddRhythmPatterns` (the design-time `ChordFlowDbContextFactory` supports this), which writes the `*_AddRhythmPatterns.cs` + `.Designer.cs` and updates `ChordFlowDbContextModelSnapshot.cs`. Verify the migration creates the `RhythmPatterns` table and applies cleanly via `Database.Migrate()`.

<!-- step:idempotent-seeding-program-cs-wiring -->
### Step 3 — Idempotent seeding + Program.cs wiring

Add `SeedBuiltInRhythmPatterns()` mirroring `SeedBuiltInProgressions`/`SeedBuiltInSongs`: collect existing ids, insert any `BuiltInRhythmPatterns` missing by `Id` with `Origin.BuiltIn` (no catalog-header parse — patterns have none), `SaveChanges()` only when something was added, return the count. Idempotent and provenance-safe — never touches existing or user rows (C3). Wire `db.SeedBuiltInRhythmPatterns();` into `Desktop/Program.cs` right after the `Migrate()` + existing seed calls. Test (`RhythmPatternSeedTests`, mirroring `ProgressionSeedTests`): first run inserts 3, second run inserts 0 and leaves rows untouched.

<!-- step:rhythmpatternstore-load-round-trip -->
### Step 4 — RhythmPatternStore load round-trip

New `RhythmPatternStore` (concrete, in `Persistence/` — no Domain interface yet; nothing in Domain resolves patterns by id today, so YAGNI per C2): `Find(string id)` reads the row `AsNoTracking()`, builds `TimeSignature(row.TsNumerator, row.TsDenominator)`, and returns `RhythmPatternParser.Parse(row.Id, row.Name, row.Dsl, ts)`. No catalog-header strip (patterns have none). The grid/events are regenerated on load, never stored (C1) — the same 'store the definition, regenerate on load' pattern as `ProgressionStore`. Test (`RhythmPatternPersistenceTests`, mirroring `ProgressionPersistenceTests`): migrate → insert a known pattern row → `Find` → assert the parsed `Bars`/`Events` match the expected sustain-literal shape, and that round-tripping a multi-bar/pickup DSL reconstructs both.

<!-- step:migrate-in-memory-seeds-to-dsl -->
### Step 5 — Migrate in-memory seeds to DSL-derived (behavior change)

Rebuild the three `RhythmPattern` constants from their `BuiltInRhythmPatterns` DSL via `RhythmPatternParser.Parse` (single source of truth), so `Beat1` becomes one whole-bar ring `Hit(0,192)` and `Beat1And3` two halves `Hit(0,96)+Hit(96,96)`; `Quarters` is unchanged (it already round-trips). The constants stay (only their event content changes), so all existing consumers keep compiling (C5). This is the intended musical correction (C4), so **update** expectations rather than preserve staccato: in `RhythmSeedDslTests` flip the `SustainLiteralSeeds_DivergeFromTheStaccatoLiveSeeds_UntilSlice2` guard (now equal) and the Beat1/Beat1And3 round-trip asserts to live==DSL; in `AlphaTexRendererTests` update the Beat 1 (`:4 (c) r r r` → whole-bar ring) and Beats 1 & 3 (→ `:2 (c) (c)`) expectations and the pickup test's main-bar line; in `RhythmQuantizerTests` update `Quantize_Beat1`/`Quantize_Beat1And3`; in `RhythmOverlayTests` update any assertion keyed to the old staccato Beat1/Beat1And3 events. Run the full suite green to confirm the ripple is fully chased.
