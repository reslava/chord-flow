---
type: design
id: de_01KTVVTS9HG5X2C39TC1X1KP94
title: Rhythm DSL — Design
status: draft
created: "2026-06-11T00:00:00.000Z"
updated: 2026-06-12
version: 4
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

### 2.2 Per-beat mixed subdivision (common in lead)

A bar is a sequence of **beat groups** separated by spaces; each group may carry
its own `:n` suffix (default 4), so straight and triplet beats mix in one bar:

```text
XXX:3 X... X.X:3 X...
```

= beat 1 triplet (0/16/32), beat 2 straight 16ths (48…), beat 3 triplet
(96/112/128), beat 4 straight. Each group must contain exactly `n` cells.
Validation: `Σ groups = beats`, each group's cell count == its `n`.

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
- **`RhythmQuantizer`** recognises a beat group on the triplet grid and emits its
  cells as `NoteValue = 8` slots tagged `Tuplet(3,2)` (and 16th-triplet → `:16`
  tagged `(3,2)`, etc.). Straight beats are unchanged. The split-at-beat-line and
  span-boundary logic is untouched.
- **`AlphaTexRenderer`** emits the verified alphaTex tuplet token **`{tu N}`** on
  each tuplet slot. (Ties remain unsupported and still throw — unchanged; only
  tuplets graduate from "unverified" to supported, per Rafa's alphaTex check.)

> This keeps the renderer the **only** alphaTex-aware code (C2) and graduates one
> deferred render constraint (tuplets) while leaving the others (ties/dotted
> tokens) as-is.

---

## 4. Pickup — `PICKUP:` block

```text
PICKUP:
............X           # last 16th  (12 cells; LengthTicks = 12·12 = 144? — see note)

A:
X...X...X...X...
```

- The pickup grid may be **shorter than a bar** (1..cellsPerBar cells of its
  subdivision); `PickupMeasure.LengthTicks = cellCount · cellTicks`. A 4-cell
  `:4` pickup = the last beat (48 ticks).
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
3. Per bar: split on spaces → beat groups; read each group's `:n` (default 4);
   validate cell counts (`group == n`, `Σ groups == beats`); reject glyphs ∉
   `{X, ., -, *}`.
4. Walk → `RhythmEvent`s per group; assemble `PatternBar`s.
5. Build `RhythmPattern(id, name, bars, ts, pickup?)`.

**Errors:** `FormatException` naming the offending cell/group/line (e.g.
`beat group ':3' has 2 cells, expected 3`) — same convention as the other
parsers.

---

## 6. Persistence parity (mirrors `ProgressionEntity`)

```csharp
record RhythmPatternEntity(string Id, string Name, string Dsl, Origin Origin,
                           string? Genre, string? Subgenre, string Tags, DateTime CreatedUtc);
```

- `Dsl` (the canonical grid string) is the **only** persisted form; re-parsed on
  load. **Adopts the catalog-metadata + provenance model from the `packages`
  thread** — `Origin` extends `ProgressionOrigin` with `Pack{PackId}`;
  `Genre`/`Subgenre`/`Tags` denormalized from the DSL header for filtering.
  `DbSet` + `HasConversion<string>()`.
- The C# seeds (`Beat1` `X...............`, `Beat1And3` `X.......X.......`,
  `Quarters` `X...X...X...X...`) re-expressed as DSL, seeded `BuiltIn` by `Id`,
  idempotently — same as `SeedBuiltInProgressions()`.
- Round-trip: row → `RhythmPatternParser.Parse(Dsl)` → `RhythmPattern` → pipeline.

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

---

## 10. Open implementation questions (non-blocking, decide at plan time)

1. **`*` sugar** — ship in this slice or defer (dotted already works via sustain)?
2. **Whitespace inside a beat group** — spaces are the group separator, so
   intra-group readability spacing isn't available; is that acceptable? (Leaning
   yes — groups are short.)
3. **`RhythmPatternEntity` timing** — same slice as the parser, or a follow-up?

---

## Design conversation

Origin, the five locked decisions, the durable-design pivot (multi-bar type now +
triplets in-scope), and the triplet/`{tu N}` analysis are in
`loom/domain/rhythm/chats/rhythm-chat-001.md`.

Related: [[chordflow-domain-model-reference]], [[chordflow-architecture-reference]], [[design-philosophy-durable-over-minimal]].