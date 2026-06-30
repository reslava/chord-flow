---
type: design
id: de_01KTP11T7JCSDK6PN2FEXDR5CW
title: Multi-chord-per-bar progressions — Design
status: done
created: 2026-06-09
updated: 2026-06-12
version: 4
idea_version: 2
tags: []
parent_id: id_01KTP0SG7G3YR8CRQKZA3YPK6G
requires_load: []
---
# Multi-chord-per-bar progressions — Design

## 1. Decision (locked)

Option **B**: chord-change timing lives on the **tick grid**, not as an enum on `RomanDegree`. A new **harmonic-rhythm layer** sits beside the existing strum `RhythmPattern`. `RomanDegree` stays pure harmony.

## 2. Domain model

```csharp
public sealed record Progression(string Id, string Name, IReadOnlyList<HarmonicBar> Bars);
public sealed record HarmonicBar(IReadOnlyList<ChordSpan> Spans);     // spans sum to BarTicks, in order
public readonly record struct ChordSpan(RomanDegree Degree, int DurationTicks);
```

- `RomanDegree(int Degree, Quality Quality)` — **unchanged**. No `BarPart`.
- `DurationTicks` is on the existing 48-PPQ grid (`TickGrid`, `TimeSignature.BarTicks = 192` in 4/4).
- **Validation (local, per bar):** `sum(span.DurationTicks) == ts.BarTicks`, every `DurationTicks > 0`, and **for v1 each `DurationTicks` is a multiple of `ts.BeatTicks` (48)** — i.e. quarter-aligned (see §5). Enforced by a guarded factory so a malformed `Progression` is unconstructable; bar count = `Bars.Count`.
- **Quarter-slot model (per Rafa):** the bar is conceptually 4 quarter slots of 48 ticks. A chord occupies one or more contiguous slots, so each span duration ∈ {48, 96, 144, 192} and they sum to 192. This yields **1, 2, 3 or 4 chords/bar**:
  - 1 → `[192]`
  - 2 → `[96,96]` (or `[48,144]` / `[144,48]`)
  - 3 → `[96,48,48]`, `[48,96,48]`, `[48,48,96]`
  - 4 → `[48,48,48,48]`
- **Backward compatibility:** a single-chord bar is one `ChordSpan(degree, 192)`. The existing 12-bar blues becomes 12 single-span bars — semantically identical.

`BarPart {Whole, Half, Quarter}` exists **only** in the DSL/UI layer as sugar mapping to ticks (`192 / 96 / 48`); never a field on a domain type.

## 3. Input DSL (Nashville-style) — `ProgressionParser`

Pure static parser in `Domain/` (peer of `NoteSpeller`): `string -> Progression`.

**Grammar**
- ` ` (space) = bar separator.
- `_` = chord separator within a bar.
- token = `<degree:int><quality?>[:<slots>]`. Quality suffixes onto the existing 8-value `Quality`:

  | suffix | Quality | example |
  |---|---|---|
  | *(none)* | Major | `1` |
  | `-` / `m` | Minor | `6-` |
  | `7` | Dominant7 | `57` |
  | `-7` / `m7` | Minor7 | `2-7` |
  | `maj7` / `^7` | Major7 | `1^7` |
  | `°` / `dim` | Diminished | `7°` |
  | `ø` / `m7b5` | HalfDiminished7 | `7ø` |
  | `+` / `aug` | Augmented | `5+` |

**Duration within a bar (the quarter-slot rule):**
- **Even split (no `:slots` suffix):** the `n` chords share the bar evenly → `192 / n` ticks each. Valid only when that is quarter-aligned, i.e. **n ∈ {1, 2, 4}**. So `17_67` = I7·VI7 Half/Half (matches Rafa's original example).
- **Explicit slots (`:<slots>` suffix, slots ∈ 1..4 quarters):** every chord in the bar carries `:slots`, the slots are `× 48` ticks and must **sum to 4**. This is how **3-chord bars** (and any uneven quarter-aligned layout) are written: `17:2_67:1_27:1` → I7 (half) · VI7 (quarter) · ii7 (quarter).
- A bar mixes the two modes by an all-or-nothing rule: either *no* token has `:slots` (even split) or *every* token does (explicit). Mixed → parse error.

- Example `jazz blues turnaround`: `2-7 57 17_67 2-7_57` → `ii-7 | V7 | I7·VI7 | ii-7·V7`.

**Errors:** unknown suffix, degree out of 1..7, empty bar, even-split count whose `192/n` is not quarter-aligned (n = 3), explicit slots not summing to 4, or a `slots` value outside 1..4 → `FormatException` naming the offending token.

> **Resolved (Q3 → M1):** the even-split + `:slots` syntax above (**M1**) is locked. The literal 4-slot step-sequencer alternative (M2, repeat-to-hold) was rejected because it would break the `17_67` = Half/Half shorthand from the original examples.

## 4. Renderer change (`AlphaTexRenderer` + `RhythmQuantizer`)

Chords are no longer 1:1 with bars. Two concrete changes:

1. **`RhythmSlot` gains `int StartTick`** — the bar-relative tick where the slot begins. The quantizer already walks the bar in tick order (`p`/`q` in `EmitSpan`); it just records the start. Existing consumers ignore it; no behavior change for current patterns.
2. **`RhythmQuantizer` splits at `ChordSpan` boundaries** in addition to beat lines. When a **note** span is split by a chord boundary, the continuation slot is **re-attacked** (`TiedToPrevious = false`) — you cannot tie one chord into a different chord. When a **rest** spans a boundary, it stays a rest (no phantom strum; the chord changes silently and is first heard at the next attack). This keeps the strum layer and harmonic layer independent. (Since all v1 boundaries are quarter-aligned and MVP patterns hit on quarters, no slot is finer than a quarter and no tie is produced.)

`RenderBar` becomes: for each slot, `chord = bar.SpanCovering(slot.StartTick)` (the span whose `[start, start+dur)` contains the tick), format that chord group. The slot→span lookup is the exact primitive a future syncopation feature reuses.

**Audibility note:** a chord is only *struck* where the rhythm has an onset at/after its span boundary. To guarantee every chord in a multi-chord bar sounds, the exercise generator should pair such bars with a rhythm that has an onset at each boundary (e.g. `Quarters` for a 4-chord bar). That is a generation concern, not a renderer concern.

## 5. v1 render constraint (quarter-aligned)

The model stores any tick boundary, but the **v1 renderer supports only quarter-aligned boundaries** — span durations that are multiples of `BeatTicks` (48), given the MVP rhythms and the no-tuplet quantizer. This covers **all** of Rafa's 1/2/3/4-chord quarter-slot layouts (durations ∈ {48,96,144,192}). What remains deferred:

- **Sub-quarter / non-aligned divisions** — e.g. three *equal* `64`-tick spans, which need tuplets (`RhythmQuantizer.LargestFit` throws). Not requested; the quarter-slot 3-chord layouts (`[96,48,48]` etc.) are fully supported instead.
- **Off-beat starts (syncopation)** — a boundary that is not on a beat line.

This mirrors how the renderer already restricts ties/tuplets — the domain model is general, the Rendering seam supports a v1 subset.

## 6. Persistence (SQLite / EF Core)

New entity + table; mirrors the existing `ExerciseEntity` "store the definition, regenerate alphaTex on load" pattern.

```csharp
public sealed class ProgressionEntity
{
    public string Id { get; set; } = "";          // stable id (e.g. "12bar_blues"); PK
    public string Name { get; set; } = "";
    public string Dsl  { get; set; } = "";          // canonical Nashville string — v1 serialization
    public ProgressionOrigin Origin { get; set; }   // BuiltIn | UserDefined  (stored by name)
    public DateTime CreatedUtc { get; set; }
}

public enum ProgressionOrigin { BuiltIn, UserDefined }
```

- `ChordFlowDbContext` gains `DbSet<ProgressionEntity> Progressions`; `Origin` stored `HasConversion<string>()` (matching the `Difficulty` convention).
- **`ExerciseEntity.ProgressionId`** keeps its meaning but now references a `ProgressionEntity.Id` row (was a hard-coded seed id). FK optional for MVP (string id lookup is enough).
- **Seeding:** the built-in default set (starting with `12-Bar Blues`, plus the new example progressions) is seeded on first run with `Origin = BuiltIn`. Seeding reads from `SeedData` so the defaults stay code-authored and testable.
- **Round-trip:** load row → `ProgressionParser.Parse(Dsl)` → `Progression` → realize/render. A future syncopated/uneven form the simple DSL can't express upgrades the column (`spans_json`) or normalizes to `bars`/`spans` tables; the schema is designed to allow that without losing the v1 string form.
- **Migration:** one EF migration adds the `Progressions` table. The existing `12bar_blues` definition is re-expressed as a `BuiltIn` row with `Dsl = "17 17 17 17 47 47 17 17 57 47 17 57"`.

## 7. Tiers

`Origin` is the only tier-relevant field in this thread: `BuiltIn` ships with the app, `UserDefined` is what a pro user creates. **Paywall enforcement** (can this user create/save a `UserDefined` progression?) is **out of scope** — it belongs to a Features/licensing layer that reads `Origin`. The domain + persistence here only *record* origin.

## 8. Out of scope (deferred)

- Syncopation / off-beat & bar-crossing anticipations (pushes).
- Tuplet rendering ⇒ sub-quarter equal divisions (e.g. three equal 64-tick spans). The quarter-slot 3-chord layouts are **in** scope.
- Non-4/4 meters (ctx EX2); minor keys (renderer is major-only today).
- Paywall enforcement / licensing.

## 9. Resolved decisions

- **Q1 — id strategy for user progressions:** GUID for `UserDefined`, stable human slugs for `BuiltIn`.
- **Q2 — guarded factory:** `Progression.FromBars` throws on malformed bars (an invalid `Progression` is unconstructable).
- **Q3 — DSL syntax for uneven bars: M1** (even-split + `:slots` suffix). M2 (4-slot step-sequencer) rejected.
