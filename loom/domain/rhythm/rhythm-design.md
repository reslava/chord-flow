---
type: design
id: de_01KTVVTS9HG5X2C39TC1X1KP94
title: Rhythm DSL — Design
status: done
created: "2026-06-11T00:00:00.000Z"
updated: 2026-06-12
version: 14
tags: []
parent_id: id_01KTVVS1K2KZH08E63QQB3PQ4V
requires_load: []
---
# Rhythm DSL — Design

Design for the **Rhythm DSL** and its supporting domain: a text front-door for
authoring strum/lead rhythm patterns. Builds on the existing rhythm layer
([[chordflow-domain-model-reference]] §3). Decisions settled in `rhythm-chat-001`.

> **Stance (per [[design-philosophy-durable-over-minimal]]):** we adopt the
> **durable shape now** and implement in additive slices. Concretely: the pattern
> type becomes **multi-bar** from the start (so we never break it later), the DSL
> grammar is **designed in full** (subdivisions, mixed beats, multi-bar, dotted),
> and **triplet rendering is in-scope** because it's common for lead and the
> domain already supports it. Nothing here forces a future breaking change.

---

## 1. Domain shape — multi-bar from the start

The rhythm model is **positional**: `RhythmEvent(Position, Length, Stroke,
Accent)` stores **absolute ticks**, not grid cells. A triplet eighth is just
`RhythmEvent(16, 16, …)`. The 48-PPQ grid (C1) makes `÷3` (triplet = 16t) and
`÷4` (16th = 12t) both integer — so triplets, mixed subdivisions and dotted
values need **no event-level change**.

The one structural change we make **now** (cheap while only the progression
thread depends on it) is to give `RhythmPattern` a multi-bar shape, so multi-bar
is later an *additive* feature, never a breaking refactor:

```csharp
public sealed record PatternBar(IReadOnlyList<RhythmEvent> Events);

public sealed record RhythmPattern(
    string Id,
    string Name,
    IReadOnlyList<PatternBar> Bars,        // was: IReadOnlyList<RhythmEvent> Events
    TimeSignature TimeSignature,
    PickupMeasure? Pickup = null)
{
    public static RhythmPattern SingleBar(string id, string name,
        IReadOnlyList<RhythmEvent> events, TimeSignature ts, PickupMeasure? pickup = null)
        => new(id, name, [ new PatternBar(events) ], ts, pickup);
}
```

`RhythmEvent` is unchanged (no `Velocity` — deferred). `Stroke`/`Accent` keep
their defaults; the DSL does not author them (decision 3) — `StrokeOverlay` /
`AccentPattern` apply them downstream.

### Ripple (all additive, done in this slice)

`FeelTransform`, `RhythmQuantizer`, `AlphaTexRenderer`, and `Exercise` are
refactored to **iterate `Bars`**. With today's one-element lists this is
mechanical; signatures stay event/bar-shaped. This is the clean refactor we do
up front rather than shipping a single-bar type and breaking it.

---

## 2. The grid

Each bar is divided into **cells**. A bar (or a single beat — see §2.2) declares
its **subdivision `n`** = cells per beat; **cell ticks = `BeatTicks(48) / n`**.
`n` must divide 48:

| `n` | Subdivision | Cell ticks | Cells / 4-4 bar |
|----:|-------------|-----------:|----------------:|
| 1 | quarter | 48 | 4 |
| 2 | eighth | 24 | 8 |
| **3** | **eighth-triplet** | **16** | **12** |
| **4** | **16th (default)** | **12** | **16** |
| 6 | 16th-triplet / sextuplet | 8 | 24 |
| 8 | 32nd | 6 | 32 |
| 12, 16, 24, 48 | finer | 4 / 3 / 2 / 1 | … |

Default is `4` (16ths) — a bare row needs no marker.

### 2.1 Per-row subdivision — `:n`

A leading `:n` sets the whole row's subdivision:

```text
:3 XXX XXX XXX XXX     # eighth-note triplets (12 cells)
   X...X...X...X...    # default :4 (16 cells)
```

### 2.2 Subdivision runs & per-beat mixed subdivision (model B — settled `rhythm-chat-002`)

A bar is a sequence of **subdivision-runs** separated by spaces. A run's cells
split into consecutive beats **by count**, so a same-`n` run may omit inner
spaces (`X...X...X...X...` = four beats). A space is only needed to **switch
subdivision** or attach a per-beat `:n` — so straight and triplet beats mix in
one bar:

```text
XXX:3 X... X.X:3 X...
```

= beat 1 triplet (0/16/32), beat 2 straight 16ths (48…), beat 3 triplet
(96/112/128), beat 4 straight. Validation: each run's cell count is a **whole
multiple** of its `n`, and `Σ (cells ÷ n) = beats`. (Model A — one space-
delimited group per beat — was rejected: it contradicted every example. The
"`XXX:3X...` ambiguity" is avoided by requiring a space before a subdivision
change, not by forbidding inner spaces in a same-`n` run.)

### 2.3 Glyphs & the sustain rule

| Glyph | Meaning |
|-------|---------|
| `X` | **attack** — start a new note at this cell's tick |
| `.` | **sustain** — extend current state (note keeps ringing, or rest continues) |
| `-` | **rest / mute** — start silence at this cell's tick |

Walk cells left→right per beat group (using that group's cell-tick size),
carrying current state; a note's `Length` runs to the next `X` or `-` (or bar
end). So `X...X...X...X...` → `0:48 48:48 96:48 144:48`, and `X...............`
→ `0:192`.

### 2.4 Dotted is free; `*` is optional sugar

Dotted/uneven values fall out of the sustain rule on the 16th grid:
`X..X....` → `0:36` (dotted eighth) + `36:…`. A `*` glyph (`X*` = extend the
previous attack by one cell) is **reserved as sugar** for readability — it adds
no capability and can land whenever convenient.

### 2.5 Multi-bar — `|`

Bars are separated by `|`; each segment is parsed independently into a
`PatternBar` (its own per-beat subdivisions):

```text
X...X...X...X... | X...X...X...XX.. | X...X...X...X... | X...X...X.X.XX..
```

---

## 3. Triplet rendering (in-scope)

Because the events are positional, the only work to *render* tuplets is in the
seam:

- **`RhythmSlot`** gains an optional tuplet marker:
  ```csharp
  RhythmSlot(int NoteValue, bool IsRest, bool TiedToPrevious, int StartTick, Tuplet? Tuplet = null);
  readonly record struct Tuplet(int Numerator, int Denominator);   // e.g. (3,2) for an eighth triplet
  ```
- **`RhythmQuantizer`** classifies each beat by its events' edge ticks (the
  subdivision is gone by quantize time — events carry only absolute ticks): a beat
  whose interior edges all fall on the triplet grid (multiples of 8t/16t) but off
  the straight 16th grid (12t) is a **triplet beat**. A triplet beat decomposes
  against the straight duration table scaled by 3/2 and tags each slot
  `Tuplet(3,2)`: a one-cell eighth-triplet → `:8`, a one-cell 16th-triplet →
  `:16`, and a **sustained** multi-cell triplet note → the next-larger tuplet
  value (e.g. a 2-cell eighth-triplet note `X.X` → `:4{tu 3}`, 32t) rather than
  tied cells — so the no-tie constraint (C4) never trips inside a triplet. Straight
  beats and the split-at-beat-line / span-boundary logic are unchanged.
- **`AlphaTexRenderer`** emits the verified alphaTex tuplet token **`{tu N}`** on
  each tuplet slot. (Ties remain unsupported and still throw — unchanged; only
  tuplets graduate from "unverified" to supported, per Rafa's alphaTex check.)

> This keeps the renderer the **only** alphaTex-aware code (C2) and graduates one
> deferred render constraint (tuplets) while leaving the others (ties/dotted
> tokens) as-is.

---

## 4. Pickup — `PICKUP:` block

```text
PICKUP: ...........X | X...X...X...X...
#       └ 11 sustains + a final attack = 12 cells (LengthTicks = 12·12 = 144);
#         the note opens on the last cell → Hit(132, 12)
#       └ a required `|` separates the pickup from the first bar
```

- The leading `PICKUP:` keyword owns the grid up to the **first `|`**; that `|`
  is required (it is the same bar separator used between bars). Newlines are
  insignificant — they collapse to spaces, so the pickup and its bars may be
  laid out over multiple lines as long as the `|` is present.
- The pickup grid may be **shorter than a bar** (1..cellsPerBar cells of its
  subdivision); `PickupMeasure.LengthTicks = cellCount · cellTicks`. A 4-cell
  `:4` pickup = the last beat (48 ticks). Unlike a bar, a pickup need **not**
  hold whole beats (e.g. `...X..X` = 7 sixteenths = 84 ticks is legal).
- Same glyph/subdivision/walk rules; maps to the existing
  `PickupMeasure(Events, LengthTicks)` → `RhythmPattern.Pickup`.

---

## 5. The parser — `RhythmPatternParser`

```csharp
static RhythmPattern Parse(string id, string name, string dsl, TimeSignature ts);
```

Pure static, no I/O, peer of `ProgressionParser` / `SongParser`. Steps:

1. Split optional `PICKUP:` block from the body.
2. Split the body on `|` → bar segments.
3. Per bar: split on spaces → subdivision runs; read each run's `:n` (default
   4); validate cell counts (model B: a run's cells are a whole multiple of `n`,
   `cells % n == 0`, splitting into `cells / n` beats; `Σ beats == barBeats`);
   reject glyphs ∉ `{X, ., -}` (`*` is deferred sugar, EX8).
4. Walk → `RhythmEvent`s per group; assemble `PatternBar`s.
5. Build `RhythmPattern(id, name, bars, ts, pickup?)`.

**Errors:** `FormatException` naming the offending cell/group/line (e.g.
`beat group ':3' has 2 cells, expected 3`) — same convention as the other
parsers.

---

## 6. Persistence parity (mirrors `ProgressionEntity`)

> **Delivered in slice 2** (`rhythm-plan-002`, req rq_01KTXTFZFJ…). This
> persistence layer depends solely on the stabilised `RhythmPattern` type and is
> purely additive. As-built, two refinements to the original sketch:

```csharp
// As built (Persistence/Entities/RhythmPatternEntity.cs):
class RhythmPatternEntity : IOriginated   // Id, Name, Dsl,
{   int TsNumerator; int TsDenominator;   // meter, 4/4 today (additive for non-4/4)
    Origin Origin; string? PackId; DateTime CreatedUtc; }
```

- `Dsl` (the canonical grid string) is the **only** persisted form; re-parsed on
  load with the row's meter (`RhythmPatternStore.Find`). `DbSet` + `Origin`
  `HasConversion<string>()`.
- **No catalog metadata** (`Genre`/`Subgenre`/`Tags`) — unlike `ProgressionEntity`,
  rhythm patterns aren't genre-filtered (req EX3). The meter is stored instead as
  the `TsNumerator`/`TsDenominator` pair.
- The C# seeds (`Beat1` `X...............`, `Beat1And3` `X.......X.......`,
  `Quarters` `X...X...X...X...`) are now **DSL-derived** (`SeedData` parses these
  same strings — single source of truth) and seeded `BuiltIn` by `Id`,
  idempotently — same as `SeedBuiltInProgressions()`.
- Round-trip: row → `RhythmPatternParser.Parse(Dsl, ts)` → `RhythmPattern` → pipeline.

### 6.1 Quantizer note-coalescing (slice-2 decision — settled `rhythm-chat-003`)

The seed migration exposed a gap: the sustain-literal seeds ring across beats
(Beat 1 = a whole bar), but the quantizer split **every** note at beat lines into
*tied* continuations, and the renderer throws on ties (C4) — so the rings wouldn't
render. **Decision (Option A):** the quantizer now **coalesces a beat-aligned
straight note into a single note value** (`RhythmQuantizer.LargestAlignedFit`:
the largest value whose ticks divide the onset tick) — a whole note across the
bar, a half note on beat 1/3 — instead of tied quarters. Rests and triplet beats
still chunk per beat. A genuinely syncopated/dotted ring (e.g. a note from beat
2→4) still tie-splits and remains unsupported (ties/dotted stay deferred — C4); no
built-in seed hits that. Beat 1 → `:1`, Beats 1 & 3 → `:2 :2`, Quarters unchanged.

---

## 7. Pattern ↔ progression alignment (default now; refined in `multi-bar`)

With multi-bar patterns possible, an *m*-bar pattern must map onto an *n*-bar
progression. **v1 default: cyclic tiling** — progression bar *i* uses
`pattern.Bars[i % m]`. Defined and reasonable (a 4-bar pattern over 12 bars
repeats 3×). The `multi-bar` thread owns the *richer* semantics (fills bound to a
section's last bar, divisibility validation, Song-section interaction) — those
are additive on top of this default, not a redefinition.

---

## 8. Placement & cross-thread consistency

- All of the above lives in **`ChordFlow.Core`** (`Domain/` for the model +
  parser, `Rendering/` for the quantizer/renderer changes, `Infrastructure/` for
  the entity). Desktop → Core unchanged.
- **No arrangement in this DSL** — pattern chains are the harmonic `song`
  thread's job (decision D there: one `RhythmPattern` per song).
- **Feel/swing & tempo** live on the play unit (`Exercise` / `SongExercise`) as
  the `Feel` enum — never in the pattern DSL.

---

## 9. Explicitly deferred (each still additive — verified)

- **Multi-bar *features*** — `domain/multi-bar` thread: richer pattern↔progression
  alignment, fills, divisibility rules. (The *type* and `|` parsing ship here; the
  *semantics* refine there.)
- **Per-hit stroke/accent authoring** — a future *second annotation row*, kept
  orthogonal to the onset row (never an overloaded glyph).
- **`Velocity`** on `RhythmEvent`; continuous `SwingPercent`.
- **Multi-lane / percussion guide track.**
- **Arbitrary nested tuplets, polyrhythm, sub-`÷48` divisions** — rare; still
  expressible as positional events if ever needed.
- **`*` extend-sugar** — redundant with `.` under the sustain rule; reserved in
  §2.4, deferred here (decision in §10). Additive whenever wanted.

---

## 10. Resolved implementation decisions (settled in `rhythm-chat-002`)

1. **`*` sugar — deferred.** Redundant with `.` under the sustain rule (`X..`
   already yields a dotted eighth) and adds no capability, so it stays reserved
   (§2.4) and moves to the icebox (§9). Adding it later is purely additive — zero
   breaking risk.
2. **Grammar = model B (space separates subdivision-runs).** A space-delimited
   token is a maximal same-`n` run whose cells split into beats by count; spaces
   are needed only to switch subdivision or attach a per-beat `:n` (§2.2). So
   `X...X...X...X...` (one run) and `X... X... X... X...` (four runs) are
   equivalent. Intra-run readability spacing is still unavailable (a space starts
   a new run), but that's the only limitation — accepted, since runs are short.
   The `*` glyph stays deferred (EX8).
3. **`RhythmPatternEntity` → follow-up slice (slice 2).** Persistence (entity,
   migration, DSL-re-expressed seeds, Origin provenance) depends only on the
   stabilised `RhythmPattern` type and ships as a clean additive follow-up. Slice
   1 still includes a parser unit test that the three seed DSLs parse to the
   expected events, for end-to-end round-trip validation without the EF layer.

**Slice split:** **slice 1** = multi-bar `RhythmPattern` type refactor (ripple
through `FeelTransform`/`RhythmQuantizer`/`AlphaTexRenderer`/`Exercise`) + parser
+ triplet rendering + seed-DSL parse tests; **slice 2** = `RhythmPatternEntity` +
migration + seeding + provenance.

---

## Design conversation

Origin, the five locked decisions, the durable-design pivot (multi-bar type now +
triplets in-scope), and the triplet/`{tu N}` analysis are in
`loom/domain/rhythm/chats/rhythm-chat-001.md`.

Related: [[chordflow-domain-model-reference]], [[chordflow-architecture-reference]], [[design-philosophy-durable-over-minimal]].