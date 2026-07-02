---
type: done
id: pl_01KTP2EG1HXQXR7DKM0G8QCHTP-done
title: Done — Multi-chord-per-bar progressions — Implementation Plan
status: done
created: "2026-06-09T00:00:00.000Z"
version: 6
tags: []
parent_id: pl_01KTP2EG1HXQXR7DKM0G8QCHTP
requires_load: []
---
# Done — Multi-chord-per-bar progressions — Implementation Plan

## Step 1 — Domain model `ChordSpan`/`HarmonicBar` + guarded `Progression.FromBars` (per-bar validation: spans sum to BarTicks, >0, multiple of 48); adapt `Transposer` (→ realized bars), `SeedData` (blues as single-span bars) and `AlphaTexRenderer` (single-span fast path) to keep build + golden tests green.

**Domain model + guarded factory — done. Build green, all 106 tests pass unchanged.**

Files:
- **`Domain/ChordSpan.cs`** (new) — `readonly record struct ChordSpan(RomanDegree Degree, int DurationTicks)`. Harmony stays pure (C1); timing lives only here.
- **`Domain/HarmonicBar.cs`** (new) — `sealed record HarmonicBar(IReadOnlyList<ChordSpan> Spans)` with `TotalTicks` and `SpanCovering(int tick)` (half-open `[start, start+dur)` lookup; throws `ArgumentOutOfRangeException` if uncovered).
- **`Domain/Progression.cs`** (rewrite) — now bars-based: `IReadOnlyList<HarmonicBar> Bars`. Construction funnels through a **private** full ctor, so a malformed progression is unconstructable (IN4/Q2):
  - Guarded factory `FromBars(id, name, bars, TimeSignature ts)` — per-bar validation: spans non-empty, each `DurationTicks > 0`, each a multiple of `ts.BeatTicks` (v1 quarter-aligned, C3), summing to `ts.BarTicks`; throws `ArgumentException` naming the 1-based bar.
  - **Backward-compat** public ctor `Progression(id, name, IReadOnlyList<RomanDegree>)` → one full-bar (192-tick) single span per degree (C4). Kept so the existing renderer/transposer/seed tests construct progressions exactly as before.
  - `Degrees` computed property (each bar's first span) retained for degree-oriented callers/tests.
- **`Domain/Transposer.cs`** (rewrite) — kept `Realize(...) → Chord[]` (legacy one-chord-per-bar view, exact for single-span bars) so existing tests pass unchanged; added `RealizeBars(...) → IReadOnlyList<RealizedBar>` plus new `RealizedSpan(Chord, StartTick, DurationTicks)` and `RealizedBar` (with `ChordCovering(tick)`) — the per-span realization the multi-chord renderer (Step 4) will consume.
- **`Domain/SeedData.cs`** — `TwelveBarBlues` unchanged structurally (still the degree ctor ⇒ 12 single-span bars); only the doc comment clarifies the single-span/C4 mapping so the rendered alphaTex stays byte-identical.
- **`Rendering/AlphaTexRenderer.cs`** — **no code change needed** in Step 1: it already renders one chord group per bar via `Transposer.Realize → Chord[]`, which is exactly the single-span fast path. The multi-chord replacement lands in Step 4 (listed here only because the original plan assumed `Realize`'s return type would change; it didn't, preserving the golden output).

Decision: rather than break the API and edit the test files (Step 1's done-criterion is "existing tests pass unchanged" and test files are not in its files-touched set), the bars model is introduced behind backward-compatible shims (degree ctor + `Degrees` + `Realize→Chord[]`). The 12-bar-blues golden alphaTex is therefore byte-identical. Satisfies IN1–IN4, C1–C4.

## Step 2 — `ProgressionParser` (M1 DSL): quality-suffix table, bar/chord split, even-split n∈{1,2,4}, `:slots` quarter-count suffix (all-or-nothing per bar), `FormatException` naming bad tokens; delegates validation to `Progression.FromBars`. + unit tests (turnaround, every quality, every error, blues round-trip).

**`ProgressionParser` (M1 DSL) — done. 32 new tests, full suite 138 green.**

Files:
- **`Domain/ProgressionParser.cs`** (new) — pure `static Progression Parse(string id, string name, string dsl, TimeSignature ts)`:
  - Bars split on space (`RemoveEmptyEntries`, tolerates extra whitespace), chords split on `_`.
  - **Quality-suffix table** (exact full-suffix `Dictionary`): `""`→Major, `-`/`m`→Minor, `7`→Dominant7, `-7`/`m7`→Minor7, `maj7`/`^7`→Major7, `°`/`dim`→Diminished, `ø`/`m7b5`→HalfDiminished7, `+`/`aug`→Augmented.
  - **Degree** = exactly one leading digit (1..7); the rest is the quality suffix. (Key subtlety, caught by a test failure: degrees are single-digit, so `17` is degree 1 + suffix `7` (Dominant7), *not* degree 17 — the parser must not greedily swallow the digit-bearing suffixes like `7`/`-7`/`m7b5`.)
  - **Duration, all-or-nothing per bar:** no `:slots` → even split, valid only when `BarTicks/n` is a whole multiple of `BeatTicks` (n∈{1,2,4} in 4/4; n=3 → error). All `:slots` → each `slots × BeatTicks`, slots ∈ 1..beatsPerBar, summing to beatsPerBar (4). Mixed modes → error.
  - **Errors** → `FormatException` naming the offending token/bar: empty DSL, empty chord (stray `_`), missing degree, degree out of 1..7, unknown suffix, n=3 even split, slots out of range, slots not summing, mixed mode, multiple `:`, non-numeric slots.
  - Delegates structural validation to `Progression.FromBars` (Step 1) after building bars.
- **`tests/ChordFlow.Tests/ProgressionParserTests.cs`** (new) — blues round-trip (12 single-span Dominant7 bars; `Degrees` equals `SeedData.TwelveBarBlues.Degrees`), single Major chord = full-bar span, jazz-blues turnaround `2-7 57 17_67 2-7_57` (asserts every bar's spans incl. the two half-bar bars), 3-chord explicit `17:2_67:1_27:1`→[96,48,48], 4-chord even split→four 48-tick spans, a Theory over all 14 quality suffix spellings, and a Theory over 13 malformed inputs each asserting `FormatException`.

Satisfies IN5, IN6.

## Step 3 — `RhythmSlot.StartTick` + `RhythmQuantizer` splits at `ChordSpan` boundaries as well as beat lines; note across a chord boundary re-attacks (`TiedToPrevious=false`), rest stays a rest. + unit tests.

**Quantizer `StartTick` + chord-boundary re-attack — done. 5 new tests, full suite 143 green.**

Files:
- **`Rendering/RhythmSlot.cs`** — added `int StartTick` (4th positional field): the slot's bar-relative onset tick, so the renderer can look up the covering `ChordSpan` via `slot.StartTick`. Existing consumers/tests that project `(NoteValue, IsRest, TiedToPrevious)` are unaffected.
- **`Rendering/RhythmQuantizer.cs`**:
  - New `chordBoundaries` parameter (bar-relative interior boundary ticks, exclusive of 0 and bar end) on a new `Quantize(events, TimeSignature, IReadOnlyList<int>)` overload and the core `Quantize(events, barTicks, beatTicks, chordBoundaries)`. The old `Quantize(events, ts)` and `Quantize(pickup)` signatures are kept (delegate with empty boundaries), so all existing callers/tests are unchanged.
  - `EmitSpan` now records `StartTick = q` on every slot, and for a **note** additionally stops each chunk at the nearest interior chord boundary so the continuation **re-attacks**. Tie rule: `tied = isNote && !firstSlotOfSpan && startTick ∉ boundaries` — beat-line splits within one chord still tie; a split landing on a chord boundary does not (re-attack). **Rests ignore chord boundaries** (a rest has no attack to re-trigger — the chord changes silently and is first heard at the next attack), so a rest spanning a boundary produces no phantom strike.
  - Boundaries materialized into a `HashSet<int>` once per quantize; `null` when empty (zero overhead for single-span bars).
- **`tests/ChordFlow.Tests/RhythmQuantizerTests.cs`** (+5): `StartTick` populated at each onset (Quarters → 0/48/96/144); note across a boundary at 48 re-attacks (both quarters not tied) vs. the same note with no boundary still tying; sustained whole note across the 4-chord boundaries {48,96,144} → four un-tied attacks with correct start ticks; rest spanning a boundary stays a rest (exactly one attack in the bar, no phantom strike).

Design note: since all v1 boundaries are quarter-aligned (= beat lines) and MVP patterns hit on quarters, no tie is ever produced in practice — the re-attack logic is correct and future-proof but a no-op for current output, keeping the 12-bar-blues golden bytes identical. Satisfies IN7, IN8, C2.

## Step 4 — `AlphaTexRenderer` multi-chord: `HarmonicBar.SpanCovering(tick)`, RenderBar picks chord per `slot.StartTick`; replace the single-span fast path. + render tests for 2/3/4-chord bars; 12-bar-blues golden output stays byte-identical.

**Multi-chord renderer — done. 4 new render tests, full suite 147 green; 12-bar-blues golden output byte-identical.**

Files:
- **`Domain/HarmonicBar.cs`** — no change needed: `SpanCovering(tick)` from Step 1 already covers the requirement; the renderer uses the realized counterpart `RealizedBar.ChordCovering(tick)` (Step 1). (Listed in the plan but already satisfied.)
- **`Rendering/AlphaTexRenderer.cs`** (rewrite of the bar loop) — replaced the single-span fast path:
  - Now realizes via `Transposer.RealizeBars(progression, key)` → `IReadOnlyList<RealizedBar>`.
  - Per bar, computes `InteriorBoundaries(bar)` (cumulative span-start ticks, exclusive of 0/bar-end; empty for single-chord bars), quantizes the felt rhythm against those boundaries (`RhythmQuantizer.Quantize(feltEvents, ts, boundaries)`), then voices each slot with `bar.ChordCovering(slot.StartTick)`.
  - `RenderBar` now takes a `Func<int, Chord> chordForTick` instead of a single chord, calling it per non-rest slot; rests stay `r`. Pickup voiced with `bars[0].Spans[0].Chord` (first chord of first bar). Header/feel/pickup/stateful-`:N`/tie-guard logic unchanged.
  - A single-chord bar has no interior boundaries → identical quantization and one chord group for the whole bar, so today's output is reproduced exactly (C4/C6).
- **`tests/ChordFlow.Tests/AlphaTexRendererTests.cs`** (+4): 2-chord `17_67`+Quarters voices [I7,I7,VI7,VI7] (chord changes at the boundary); 3-chord explicit `17:2_67:1_27:1` voices [I7,I7,VI7,ii7]; 4-chord `17_27_37_47` → four distinct groups (audibility: every chord struck under Quarters); and a byte-identical check that the blues rendered from `SeedData.TwelveBarBlues` equals the blues rendered from the parsed DSL `"17 17 17 17 47 47 17 17 57 47 17 57"`. Added `LastBar`/`ChordGroups` regex helpers.

Satisfies IN7, C3, C6.

## Step 5 — Persistence: `ProgressionEntity` (Id/Name/Dsl/Origin/CreatedUtc) + `ProgressionOrigin` enum; `ChordFlowDbContext` DbSet + `HasConversion<string>()`; `ExerciseEntity.ProgressionId` references a row; EF migration adds the `Progressions` table. + round-trip test.

**Persistence (`Progressions` table) — done. EF migration generated, round-trip test, full suite 148 green.**

Files:
- **`Domain/ProgressionOrigin.cs`** (new) — `enum ProgressionOrigin { BuiltIn, UserDefined }` with doc on the id strategy (slug for built-ins, GUID for user) and that tier *enforcement* is out of scope (EX4).
- **`Infrastructure/Entities/ProgressionEntity.cs`** (new) — `Id` (string PK), `Name`, `Dsl` (canonical Nashville string = v1 serialization, C5), `Origin`, `CreatedUtc`. Mirrors the `ExerciseEntity` store-definition/regenerate-on-load pattern.
- **`Infrastructure/ChordFlowDbContext.cs`** — added `DbSet<ProgressionEntity> Progressions`; `OnModelCreating` configures `HasKey(Id)` and `Origin` `HasConversion<string>()` (stored by name, matching the `Difficulty` convention).
- **`Infrastructure/Entities/ExerciseEntity.cs`** — `ProgressionId` doc updated to note it references a `ProgressionEntity.Id` row (string ref; FK left implicit for MVP, IN12).
- **`Migrations/20260609120358_AddProgressions.cs` (+ `.Designer.cs` + updated `ChordFlowDbContextModelSnapshot.cs`)** — generated via `dotnet ef migrations add AddProgressions` (EF tools 10.0.8). Creates the `Progressions` table: `Id` TEXT PK, `Name`/`Dsl`/`Origin` TEXT NOT NULL, `CreatedUtc` TEXT. Existing `Exercises`/`PracticeRecords` tables untouched. (Migrations live in `src/ChordFlow.App/Migrations/`, the actual location — the plan's `Infrastructure/Migrations/*` path was approximate.)
- **`tests/ChordFlow.Tests/ProgressionPersistenceTests.cs`** (new) — in-memory SQLite (`DataSource=:memory:`, connection kept open across two contexts); `db.Database.Migrate()` builds the schema (proving the migration adds the table); saves a `BuiltIn` blues row + a `UserDefined` row, reloads in a fresh context and asserts `Dsl`/`Origin` round-trip; a raw `SELECT Origin` asserts it's stored as the name `"BuiltIn"`, not an integer.

Note: `Program.cs` already calls `db.Database.Migrate()` on startup, so the new table is created on existing installs automatically. Satisfies IN9, IN10, IN12, C5.

## Step 6 — Seeding: `SeedData` example progressions (blues + jazz-blues turnaround) as DSL with `Origin=BuiltIn`; idempotent first-run seeding of missing built-ins by `Id`. + DSL→model→render round-trip test per seeded progression.

**Seeding + first-run wiring — done. Full suite 163 green.**

Files:
- **`Domain/SeedData.cs`** — new pure `ProgressionDefinition(Id, Name, Dsl)` record + `BuiltInProgressions` list: `12bar_blues` ("17 17 17 17 47 47 17 17 57 47 17 57") and `jazz_blues_turnaround` ("2-7 57 17_67 2-7_57"). Kept in Domain (no I/O) so defaults are testable without a DB.
- **`Infrastructure/ChordFlowDbContext.cs`** — `SeedBuiltInProgressions()`: inserts any `SeedData.BuiltInProgressions` not already present (matched by `Id`) with `Origin=BuiltIn`; idempotent (only adds missing rows, never touches existing/user rows), returns the count inserted. Added `using ChordFlow.Domain`.
- **`Program.cs`** — calls `db.SeedBuiltInProgressions()` right after `db.Database.Migrate()` on startup, so built-ins seed on first run (and any newly-added built-ins on later runs). (Not in the plan's file list, but the seeding has to be wired to a startup hook to actually run; one line.)
- **`tests/ChordFlow.Tests/ProgressionSeedTests.cs`** (new) — `[MemberData]` over every `BuiltInProgressions` entry asserting DSL→parser→transposer→renderer round-trips (title + bar lines) in Bb; plus an in-memory-SQLite test that `SeedBuiltInProgressions()` seeds all built-ins as `BuiltIn` once and a second call adds 0 / leaves the row count unchanged.

**Scope note — resolved blocker (user-approved):** the seeded jazz turnaround uses `Minor7` chords, but the MVP voicing book (`BeginnerShellStrategy`) covered `Dominant7` only, so the render round-trip threw `NotSupportedException` (and the seed would crash the renderer in-app, not just in tests). I stopped and asked; the user chose **"Add Minor7 voicings"**. Extra (authorized, beyond step 6's listed files):
- **`Domain/BeginnerShellStrategy.cs`** — generalized the movable shell to a `thirdOffset` switch: `Dominant7` → −1 (major 3rd), `Minor7` → −2 (minor 3rd); shared minor-7th on the G string; root lifted an octave only when the 3rd would need a negative fret. The `Dominant7` path is byte-identical to before (offset −1, same octave-lift threshold) so all existing blues/render golden tests stay green. Other qualities still throw.
- **`tests/ChordFlow.Tests/VoicingBookTests.cs`** — added a 12-root `Minor7` theory (spells root + ♭3 + ♭7, non-negative frets) and updated the now-stale "dom7-only" comment.

Satisfies IN11, C4.
