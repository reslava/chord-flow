---
type: plan
id: pl_01KTP2EG1HXQXR7DKM0G8QCHTP
title: Multi-chord-per-bar progressions — Implementation Plan
status: done
created: 2026-06-09
updated: 2026-06-09
version: 2
design_version: 1
req_version: 2
tags: []
parent_id: de_01KTP11T7JCSDK6PN2FEXDR5CW
requires_load: []
target_version: 0.1.0
actual_release: 0.4.0
steps:
  - id: domain-model-guarded-progression
    order: 1
    status: done
    description: "Domain model `ChordSpan`/`HarmonicBar` + guarded `Progression.FromBars` (per-bar validation: spans sum to BarTicks, >0, multiple of 48); adapt `Transposer` (→ realized bars), `SeedData` (blues as single-span bars) and `AlphaTexRenderer` (single-span fast path) to keep build + golden tests green."
    files_touched: [src/ChordFlow.App/Domain/ChordSpan.cs, src/ChordFlow.App/Domain/HarmonicBar.cs, src/ChordFlow.App/Domain/Progression.cs, src/ChordFlow.App/Domain/Transposer.cs, src/ChordFlow.App/Domain/SeedData.cs, src/ChordFlow.App/Rendering/AlphaTexRenderer.cs]
    blocked_by: []
    satisfies: [IN1, IN2, IN3, IN4, C1, C2, C3, C4]
  - id: m1-dsl-quality-suffix-table-bar
    order: 2
    status: done
    description: "`ProgressionParser` (M1 DSL): quality-suffix table, bar/chord split, even-split n∈{1,2,4}, `:slots` quarter-count suffix (all-or-nothing per bar), `FormatException` naming bad tokens; delegates validation to `Progression.FromBars`. + unit tests (turnaround, every quality, every error, blues round-trip)."
    files_touched: [src/ChordFlow.App/Domain/ProgressionParser.cs, tests/ChordFlow.Tests/ProgressionParserTests.cs]
    blocked_by: [1]
    satisfies: [IN5, IN6]
  - id: rhythmslot
    order: 3
    status: done
    description: "`RhythmSlot.StartTick` + `RhythmQuantizer` splits at `ChordSpan` boundaries as well as beat lines; note across a chord boundary re-attacks (`TiedToPrevious=false`), rest stays a rest. + unit tests."
    files_touched: [src/ChordFlow.App/Rendering/RhythmSlot.cs, src/ChordFlow.App/Rendering/RhythmQuantizer.cs, tests/ChordFlow.Tests/RhythmQuantizerTests.cs]
    blocked_by: [1]
    satisfies: [IN7, IN8, C2]
  - id: multi-chord-harmonicbar
    order: 4
    status: done
    description: "`AlphaTexRenderer` multi-chord: `HarmonicBar.SpanCovering(tick)`, RenderBar picks chord per `slot.StartTick`; replace the single-span fast path. + render tests for 2/3/4-chord bars; 12-bar-blues golden output stays byte-identical."
    files_touched: [src/ChordFlow.App/Domain/HarmonicBar.cs, src/ChordFlow.App/Rendering/AlphaTexRenderer.cs, tests/ChordFlow.Tests/AlphaTexRendererTests.cs]
    blocked_by: [3]
    satisfies: [IN7, C3, C6]
  - id: persistence-id-name-dsl-origin-createdutc
    order: 5
    status: done
    description: "Persistence: `ProgressionEntity` (Id/Name/Dsl/Origin/CreatedUtc) + `ProgressionOrigin` enum; `ChordFlowDbContext` DbSet + `HasConversion<string>()`; `ExerciseEntity.ProgressionId` references a row; EF migration adds the `Progressions` table. + round-trip test."
    files_touched: [src/ChordFlow.App/Domain/ProgressionOrigin.cs, src/ChordFlow.App/Infrastructure/Entities/ProgressionEntity.cs, src/ChordFlow.App/Infrastructure/ChordFlowDbContext.cs, src/ChordFlow.App/Infrastructure/Entities/ExerciseEntity.cs, "src/ChordFlow.App/Infrastructure/Migrations/*"]
    blocked_by: [1]
    satisfies: [IN9, IN10, IN12, C5]
  - id: seeding-example-progressions-blues-jazz-blues
    order: 6
    status: done
    description: "Seeding: `SeedData` example progressions (blues + jazz-blues turnaround) as DSL with `Origin=BuiltIn`; idempotent first-run seeding of missing built-ins by `Id`. + DSL→model→render round-trip test per seeded progression."
    files_touched: [src/ChordFlow.App/Domain/SeedData.cs, src/ChordFlow.App/Infrastructure/ChordFlowDbContext.cs, tests/ChordFlow.Tests/ProgressionSeedTests.cs]
    blocked_by: [2, 5]
    satisfies: [IN11, C4]
---
# Multi-chord-per-bar progressions — Implementation Plan

## Goal

Implement the harmonic-rhythm layer from `req.md`: a `Progression` of `HarmonicBar`s of `ChordSpan`s (quarter-aligned, 1–4 chords/bar), an M1 Nashville `ProgressionParser`, a renderer that maps each rhythm slot to its covering chord, and SQLite persistence with a built-in/user `Origin`. Each step leaves the solution **building and green**. Satisfies IN1–IN12; constraints C1–C6.
---

## Steps

| Done | # | Step | Files touched | Blocked by | Satisfies |
|---|---|---|---|---|---|
| ✅ | 1 | Domain model `ChordSpan`/`HarmonicBar` + guarded `Progression.FromBars` (per-bar validation: spans sum to BarTicks, >0, multiple of 48); adapt `Transposer` (→ realized bars), `SeedData` (blues as single-span bars) and `AlphaTexRenderer` (single-span fast path) to keep build + golden tests green. | src/ChordFlow.App/Domain/ChordSpan.cs, src/ChordFlow.App/Domain/HarmonicBar.cs, src/ChordFlow.App/Domain/Progression.cs, src/ChordFlow.App/Domain/Transposer.cs, src/ChordFlow.App/Domain/SeedData.cs, src/ChordFlow.App/Rendering/AlphaTexRenderer.cs | — | IN1, IN2, IN3, IN4, C1, C2, C3, C4 |
| ✅ | 2 | `ProgressionParser` (M1 DSL): quality-suffix table, bar/chord split, even-split n∈{1,2,4}, `:slots` quarter-count suffix (all-or-nothing per bar), `FormatException` naming bad tokens; delegates validation to `Progression.FromBars`. + unit tests (turnaround, every quality, every error, blues round-trip). | src/ChordFlow.App/Domain/ProgressionParser.cs, tests/ChordFlow.Tests/ProgressionParserTests.cs | 1 | IN5, IN6 |
| ✅ | 3 | `RhythmSlot.StartTick` + `RhythmQuantizer` splits at `ChordSpan` boundaries as well as beat lines; note across a chord boundary re-attacks (`TiedToPrevious=false`), rest stays a rest. + unit tests. | src/ChordFlow.App/Rendering/RhythmSlot.cs, src/ChordFlow.App/Rendering/RhythmQuantizer.cs, tests/ChordFlow.Tests/RhythmQuantizerTests.cs | 1 | IN7, IN8, C2 |
| ✅ | 4 | `AlphaTexRenderer` multi-chord: `HarmonicBar.SpanCovering(tick)`, RenderBar picks chord per `slot.StartTick`; replace the single-span fast path. + render tests for 2/3/4-chord bars; 12-bar-blues golden output stays byte-identical. | src/ChordFlow.App/Domain/HarmonicBar.cs, src/ChordFlow.App/Rendering/AlphaTexRenderer.cs, tests/ChordFlow.Tests/AlphaTexRendererTests.cs | 3 | IN7, C3, C6 |
| ✅ | 5 | Persistence: `ProgressionEntity` (Id/Name/Dsl/Origin/CreatedUtc) + `ProgressionOrigin` enum; `ChordFlowDbContext` DbSet + `HasConversion<string>()`; `ExerciseEntity.ProgressionId` references a row; EF migration adds the `Progressions` table. + round-trip test. | src/ChordFlow.App/Domain/ProgressionOrigin.cs, src/ChordFlow.App/Infrastructure/Entities/ProgressionEntity.cs, src/ChordFlow.App/Infrastructure/ChordFlowDbContext.cs, src/ChordFlow.App/Infrastructure/Entities/ExerciseEntity.cs, src/ChordFlow.App/Infrastructure/Migrations/* | 1 | IN9, IN10, IN12, C5 |
| ✅ | 6 | Seeding: `SeedData` example progressions (blues + jazz-blues turnaround) as DSL with `Origin=BuiltIn`; idempotent first-run seeding of missing built-ins by `Id`. + DSL→model→render round-trip test per seeded progression. | src/ChordFlow.App/Domain/SeedData.cs, src/ChordFlow.App/Infrastructure/ChordFlowDbContext.cs, tests/ChordFlow.Tests/ProgressionSeedTests.cs | 2, 5 | IN11, C4 |
---

### Legend

| Symbol | Meaning |
|--------|---------|
| ✅ | Done |
| 🔄 | In Progress |
| 🔳 | Pending |
| ❌ | Cancelled |

---

<!-- step:domain-model-guarded-progression -->
### Step 1 — Domain model + guarded factory + keep green

- New `Domain/ChordSpan.cs` — `readonly record struct ChordSpan(RomanDegree Degree, int DurationTicks)`.
- New `Domain/HarmonicBar.cs` — `record HarmonicBar(IReadOnlyList<ChordSpan> Spans)` with `SpanCovering(int tick)` (returns the span whose `[start, start+dur)` contains `tick`); a `TotalTicks` helper.
- `Domain/Progression.cs` — change `Degrees` → `IReadOnlyList<HarmonicBar> Bars`; add static guarded factory `Progression.FromBars(id, name, bars, TimeSignature ts)` validating per bar: spans sum to `ts.BarTicks`, each `DurationTicks > 0`, each a multiple of `ts.BeatTicks` (v1 quarter-aligned). Throws `ArgumentException` naming the bad bar (Q2).
- `Domain/Transposer.cs` — `Realize` now returns realized **bars**: `IReadOnlyList<RealizedBar>` where `RealizedBar` carries ordered `(Chord, int DurationTicks)`.
- `Domain/SeedData.cs` — rebuild `TwelveBarBlues` as 12 single-span bars (`ChordSpan(degree, 192)` each), semantically identical.
- `Rendering/AlphaTexRenderer.cs` — iterate realized bars; **single-span fast path** reproduces today's output (one chord group for the whole bar). Multi-span rendering lands in Step 4.
- **Done when:** solution builds; existing renderer/transposer/seed tests pass unchanged (golden alphaTex for 12-bar blues identical).

<!-- step:m1-dsl-quality-suffix-table-bar -->
### Step 2 — `ProgressionParser` (M1 DSL)

- New `Domain/ProgressionParser.cs` — pure `static Progression Parse(string id, string name, string dsl, TimeSignature ts)`.
- Tokenize: bars by space, chords by `_`. Per token parse `<degree><quality?>[:<slots>]`.
- Quality suffix table (design §3): none→Major, `-`/`m`→Minor, `7`→Dominant7, `-7`/`m7`→Minor7, `maj7`/`^7`→Major7, `°`/`dim`→Diminished, `ø`/`m7b5`→HalfDiminished7, `+`/`aug`→Augmented.
- Duration: all-or-nothing per bar. No `:slots` → even split, valid for n∈{1,2,4} (n=3 → error). All `:slots` → quarters×48, must sum to 4. Mixed → error.
- Build bars → hand off to `Progression.FromBars` (re-uses Step 1 validation).
- Errors: `FormatException` naming the offending token.
- **Tests:** `17_67`→Half/Half; `17:2_67:1_27:1`→[96,48,48]; `2-7 57 17_67 2-7_57` turnaround; every quality suffix; each error case; blues string round-trips to `SeedData.TwelveBarBlues`.

<!-- step:rhythmslot -->
### Step 3 — Quantizer: `StartTick` + span-boundary splitting

- `Rendering/RhythmSlot.cs` — add `int StartTick` (bar-relative tick of the slot's onset).
- `Rendering/RhythmQuantizer.cs` — populate `StartTick` (already tracked as `p`/`q` in `EmitSpan`). Add a param taking the bar's span boundaries; split at those as well as beat lines. At a chord boundary a continuing note re-attacks (`TiedToPrevious=false`); a rest stays a rest.
- **Tests:** boundaries at 96 (2-chord) and 48/96/144 (4-chord) produce correctly split slots with right `StartTick`; note across a chord boundary re-attacks, not ties; rest across a boundary stays one rest; single-span bar unchanged.

<!-- step:multi-chord-harmonicbar -->
### Step 4 — Renderer multi-chord

- `Domain/HarmonicBar.cs` — confirm `SpanCovering` maps the realized bar too.
- `Rendering/AlphaTexRenderer.cs` — replace the single-span fast path: per realized bar, quantize with that bar's span boundaries (Step 3), then for each slot pick the chord via covering-span lookup on `slot.StartTick`; format that chord group (`r` for rests). Header/feel/pickup logic unchanged.
- **Tests:** 2-chord (`17_67` + Quarters), 3-chord `[96,48,48]`, 4-chord (`17_27_37_47`); 12-bar-blues golden output identical; audibility behaviour asserted.

<!-- step:persistence-id-name-dsl-origin-createdutc -->
### Step 5 — Persistence

- New `Infrastructure/Entities/ProgressionEntity.cs` — `Id` (string PK), `Name`, `Dsl`, `Origin`, `CreatedUtc`.
- New `Domain/ProgressionOrigin.cs` — `enum { BuiltIn, UserDefined }` (Q1: GUID ids for user, slug for built-in).
- `Infrastructure/ChordFlowDbContext.cs` — `DbSet<ProgressionEntity> Progressions`; `Origin` `HasConversion<string>()`; key on `Id`.
- `Infrastructure/Entities/ExerciseEntity.cs` — `ProgressionId` references a `ProgressionEntity.Id` (string ref; FK optional for MVP).
- EF migration adding the `Progressions` table.
- **Tests:** context creates the table; save/load round-trips `Dsl` + `Origin`.

<!-- step:seeding-example-progressions-blues-jazz-blues -->
### Step 6 — Seeding + round-trip

- `Domain/SeedData.cs` — add example progressions `(Id, Name, Dsl, Origin=BuiltIn)`, including blues `"17 17 17 17 47 47 17 17 57 47 17 57"` and turnaround `2-7 57 17_67 2-7_57`.
- First-run seeding: on DB init insert missing `BuiltIn` rows from `SeedData` (idempotent by `Id`).
- **Tests:** DSL→parser→transposer→renderer round-trip for each seeded progression renders without error; built-ins seed once and are idempotent.

## Notes

- Deferred (per `req.md` EX1–EX5): syncopation, tuplets / sub-quarter equal divisions, non-quarter-aligned durations, non-4/4 meters, minor keys, paywall enforcement.
- Build stays green after every step; renderer golden output for the existing 12-bar blues must remain byte-identical through Steps 1–4.
