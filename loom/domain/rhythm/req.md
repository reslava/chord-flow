---
type: req
id: rq_01KTXTFZFJXVRXNN5MNN7XDJ7V
title: Rhythm DSL — authoring strum patterns as a tick grid — Requirements
status: locked
created: "2026-06-12T00:00:00.000Z"
updated: 2026-06-12
version: 1
tags: []
parent_id: id_01KTVVS1K2KZH08E63QQB3PQ4V
requires_load: []
---
# Rhythm DSL — authoring strum patterns as a tick grid — Requirements

### ✅ Included

- `IN1` — Adopt the multi-bar `RhythmPattern(Bars: IReadOnlyList<PatternBar>, …)` type (with a `SingleBar(...)` helper) from the start, and refactor `FeelTransform` / `RhythmQuantizer` / `AlphaTexRenderer` / `Exercise` to iterate `Bars`.
- `IN2` — `RhythmPatternParser.Parse(id, name, dsl, ts) → RhythmPattern`: a pure, no-I/O static parser, peer of `ProgressionParser` / `SongParser`, that throws `FormatException` naming the offending cell / group / line.
- `IN3` — Onset-only glyph set `X` (attack), `.` (sustain), `-` (rest/mute), with the sustain rule: a hit's length runs to the next `X` or `-` (or bar end).
- `IN4` — Subdivision grammar `:n` — per-row (whole row) and per-beat mixed (each space-separated beat group may carry its own `:n`, default 4); `n` must divide 48; each group must contain exactly `n` cells.
- `IN5` — `|` multi-bar separator: each segment is parsed independently into its own `PatternBar`.
- `IN6` — `PICKUP:` block (may be shorter than a full bar) mapping to the existing `PickupMeasure`.
- `IN7` — Triplet rendering: a tuplet marker on `RhythmSlot`, `RhythmQuantizer` emitting tuplet slots, and `AlphaTexRenderer` emitting the verified alphaTex `{tu N}` token.
- `IN8` — Unit tests proving the three C# seeds (`Beat1`, `Beat1And3`, `Quarters`) re-expressed as DSL parse to the expected event positions.

### ❌ Excluded

- `EX1` — Any `SONG:` / arrangement / pattern-chain layer in the rhythm DSL → owned by the `domain/song` thread.
- `EX2` — `RhythmPatternEntity` persistence (EF entity, migration, DSL-re-expressed seeds, `Origin` provenance) → `rhythm` slice 2.
- `EX3` — Multi-bar *features* (section-anchored fills, richer pattern↔progression alignment, divisibility rules) → `domain/multi-bar` thread.
- `EX4` — Per-hit stroke / accent authoring glyphs → future second annotation row (icebox).
- `EX5` — `Velocity` on `RhythmEvent` and continuous `SwingPercent` → icebox.
- `EX6` — Multi-lane / percussion guide track (drum `K`/`S`/`H` notation) → icebox.
- `EX7` — Arbitrary nested tuplets, polyrhythm, and sub-÷48 subdivisions → icebox.
- `EX8` — `*` extend-sugar glyph → deferred (redundant with `.` under the sustain rule); icebox.
- `EX9` — Intra-group readability whitespace → not supported (accepted limitation; space is the beat-group separator).

### ⛓ Constraints

- `C1` — Fixed 48-PPQ tick grid: 192 ticks/bar in 4/4, 16th = 12 ticks; subdivision `n` must divide 48 (÷4 → 12t 16th, ÷3 → 16t eighth-triplet).
- `C2` — Patterns hold **timing only**; harmony, voicings, accents, strokes, and feel are separate layers applied later — the DSL never authors stroke/accent (`StrokeOverlay` / `AccentPattern` apply downstream).
- `C3` — Feel/swing and tempo live on the play unit (`Exercise` / `SongExercise`) as the `Feel` enum — never in the pattern DSL.
- `C4` — `AlphaTexRenderer` stays the only alphaTex-aware code and emits only verified tokens; ties remain unsupported and still throw — only tuplets graduate to supported.
- `C5` — All code lives in `ChordFlow.Core`; the Desktop → Core dependency direction is unchanged.
- `C6` — Every excluded/deferred item must remain **additive**: adopting it later must not require a breaking change to a shipped type (durable-over-minimal design rule).
