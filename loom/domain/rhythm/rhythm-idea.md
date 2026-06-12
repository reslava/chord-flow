---
type: idea
id: id_01KTVVS1K2KZH08E63QQB3PQ4V
title: Rhythm DSL — authoring strum patterns as a tick grid
status: draft
created: "2026-06-11T00:00:00.000Z"
updated: 2026-06-11
version: 4
tags: []
parent_id: null
requires_load: []
---
# Rhythm DSL — authoring strum patterns as a tick grid

## The idea

Today's `RhythmPattern`s exist only as **C#-authored seed data** (`Beat1`,
`Beat1And3`, `Quarters`). This thread adds a tiny **text DSL** so users (and
content packs) can author strum patterns as a character grid — the rhythmic
analog of what the Nashville DSL did for progressions.

```text
X...X...X...X...
```

16 cells × 12 ticks = 192 = one 4/4 bar, mapped 1:1 onto the existing
`TickGrid` (16th = 12 ticks). **No new timing concepts** — it's a new *front
door* to the rhythm model we already have.

> **Principle inherited:** rhythm patterns hold **timing only**; harmony,
> voicings, accents, strokes, and feel are separate layers applied later
> ([[chordflow-domain-model-reference]] §3).

## Why we want it

- **Authoring without code** — patterns become data, like progressions.
- **Content packs** — a starter rhythm set ships as a definition bundle, not C#.
- **Consistency** — a third parser peer (`ProgressionParser` → `SongParser` →
  `RhythmPatternParser`) all producing existing domain types from text.

## The grid (concept — detail in the design)

One lane. Glyphs `X` (attack) / `.` (sustain) / `-` (rest/mute); a hit rings
**until the next `X` or `-`**, so `X...X...X...X...` is four ringing quarter-note
strums and `X...............` is one strum held the whole bar.

The grid's **subdivision is declarable**, defaulting to 16ths — the 48-PPQ grid
makes both `÷4` (16th = 12t) and `÷3` (triplet = 16t) integer:

- **per row** — `:3 XXX XXX XXX XXX` = eighth-note triplets;
- **per beat (mixed)** — `XXX:3 X... X.X:3 X...` = a triplet beat beside straight
  16ths, common in lead playing;
- **multi-bar** — bars separated by `|`;
- **dotted** falls out of the sustain rule (`X..` = a dotted eighth); `X*` is
  optional sugar.

An optional `PICKUP:` block (a shorter grid) maps to the existing `PickupMeasure`.

## Locked decisions (from `rhythm-chat-001`)

1. **`.` = sustain, not silence.** A hit's length extends to the next onset; `-`
   is the dedicated rest/mute glyph. (The naive "every hit = one 16th" makes
   guitar strums staccato — wrong.)
2. **Single lane** for v1. Drum-machine `K`/`S`/`H` multi-lane notation isn't
   needed — ChordFlow renders one guitar voice.
3. **Onset only in the glyph** (`X`/`.`/`-`). Stroke and accent stay **overlays**
   (`StrokeOverlay`, `AccentPattern` — already built), not per-character flags.
4. **`RhythmPattern` is multi-bar-shaped from the start** (`Bars: PatternBar[]`) —
   we adopt the durable type **now** (a single-bar pattern is one element) so
   multi-bar is later an *additive* feature, never a breaking refactor (per
   [[design-philosophy-durable-over-minimal]]). The `domain/multi-bar` thread owns
   the *features* (fills, alignment), not a type change.
5. **`Velocity` deferred.** `Accent` covers the v1 need; records make it trivial
   to add later.
6. **Triplets & mixed subdivisions are in-scope** (common for lead). The model is
   already positional so they need no event change; the quantizer/renderer gain
   tuplet support emitting verified alphaTex `{tu N}`.
- **No arrangement here.** A `SONG:`/pattern-chain belongs to the harmonic
  `song` thread — keeping it out avoids a second, competing timeline.
- **Feel/swing stays an overlay** carried by the play unit (`Exercise` /
  `SongExercise`) as the `Feel` enum — never in the pattern DSL.

## In scope (first slice)

- **Adopt the multi-bar `RhythmPattern(Bars: PatternBar[])` type** (durable shape;
  refactor `FeelTransform`/`RhythmQuantizer`/`AlphaTexRenderer`/`Exercise` to
  iterate bars — additive, trivial today).
- `RhythmPatternParser.Parse(id, name, dsl, ts) → RhythmPattern` — peer of
  `ProgressionParser`/`SongParser`: glyphs `X`/`.`/`-`, per-row `:n` and per-beat
  mixed subdivisions, `|` multi-bar, optional `PICKUP:`. `FormatException` naming
  the bad cell/group.
- **Triplet rendering** — `RhythmSlot` tuplet marker, quantizer tuplet slots,
  renderer emits verified alphaTex `{tu N}`.
- Persistence parity: `RhythmPatternEntity` (Dsl-only) mirroring
  `ProgressionEntity`/`SongEntity`; the C# seeds re-expressed as DSL.

## Out of scope (deferred — each still additive)

- **Multi-bar *features*** — `domain/multi-bar` thread: richer pattern↔progression
  alignment, fills, divisibility rules (the *type* + `|` parsing ship here; v1
  defaults to cyclic tiling).
- Per-hit stroke/accent glyphs (a future second annotation row).
- Multi-lane / percussion guide track.
- `Velocity` / continuous `SwingPercent`.
- Arbitrary nested tuplets / polyrhythm (rare; still positional if ever needed).
- Any song-level arrangement (`song` thread).

Related: [[chordflow-domain-model-reference]], the `song` thread, the `progression` thread.