---
type: plan
id: pl_01KTXTT2HTACTHQDK6VSVC425D
title: Rhythm DSL — first slice
status: done
created: 2026-06-12
updated: 2026-06-12
version: 1
design_version: 1
req_version: 1
tags: []
parent_id: de_01KTVVTS9HG5X2C39TC1X1KP94
requires_load: []
target_version: 0.1.0
actual_release: 0.5.0
steps:
  - id: multi-bar-rhythmpattern-type
    order: 1
    status: done
    description: "Adopt multi-bar RhythmPattern(Bars: IReadOnlyList<PatternBar>) + PatternBar + SingleBar(...) helper; refactor FeelTransform / RhythmQuantizer / AlphaTexRenderer / Exercise to iterate Bars"
    files_touched: [src/ChordFlow.Core/Domain/RhythmPattern.cs, src/ChordFlow.Core/Domain/FeelTransform.cs, src/ChordFlow.Core/Rendering/RhythmQuantizer.cs, src/ChordFlow.Core/Rendering/AlphaTexRenderer.cs, src/ChordFlow.Core/Domain/Exercise.cs, tests/ChordFlow.Core.Tests/RhythmQuantizerTests.cs, tests/ChordFlow.Core.Tests/AlphaTexRendererTests.cs]
    blocked_by: []
    satisfies: [IN1, C1, C5, C6]
  - id: rhythmpatternparser-glyphs-sustain-subdivisions
    order: 2
    status: done
    description: "RhythmPatternParser.Parse(id, name, dsl, ts) → RhythmPattern (single bar): glyphs X/./- with the sustain rule, per-row and per-beat :n subdivisions, FormatException naming the bad cell/group"
    files_touched: [src/ChordFlow.Core/Domain/RhythmPatternParser.cs, tests/ChordFlow.Core.Tests/RhythmPatternParserTests.cs]
    blocked_by: [1]
    satisfies: [IN2, IN3, IN4, C1, C2, C5]
  - id: multi-bar-pickup-block
    order: 3
    status: done
    description: "Add the | bar separator (each segment parsed independently into a PatternBar) and the optional PICKUP: block mapping to the existing PickupMeasure"
    files_touched: [src/ChordFlow.Core/Domain/RhythmPatternParser.cs, src/ChordFlow.Core/Domain/PickupMeasure.cs, tests/ChordFlow.Core.Tests/RhythmPatternParserTests.cs]
    blocked_by: [2]
    satisfies: [IN5, IN6, C6]
  - id: triplet-rendering-tu-n
    order: 4
    status: done
    description: RhythmSlot tuplet marker, RhythmQuantizer emitting tuplet slots for triplet-grid beat groups, AlphaTexRenderer emitting the verified alphaTex {tu N} token
    files_touched: [src/ChordFlow.Core/Rendering/RhythmSlot.cs, src/ChordFlow.Core/Rendering/RhythmQuantizer.cs, src/ChordFlow.Core/Rendering/AlphaTexRenderer.cs, tests/ChordFlow.Core.Tests/RhythmQuantizerTests.cs, tests/ChordFlow.Core.Tests/AlphaTexRendererTests.cs]
    blocked_by: [1, 2]
    satisfies: [IN7, C1, C4]
  - id: seed-dsl-round-trip-tests
    order: 5
    status: done
    description: "Unit tests proving the three C# seeds (Beat1, Beat1And3, Quarters) re-expressed as DSL parse to the expected event positions — end-to-end validation without the EF layer"
    files_touched: [tests/ChordFlow.Core.Tests/RhythmSeedDslTests.cs]
    blocked_by: [2]
    satisfies: [IN8, C6]
---
# Rhythm DSL — first slice

## Goal

Implement rhythm slice 1 exactly as the locked req (rq_01KTXTFZFJ…) and design: adopt the durable multi-bar RhythmPattern type now, add a pure RhythmPatternParser peer of ProgressionParser/SongParser, and graduate triplet rendering to verified alphaTex {tu N}. Step 1 adopts RhythmPattern(Bars: PatternBar[]) + a SingleBar helper and refactors FeelTransform/RhythmQuantizer/AlphaTexRenderer/Exercise to iterate bars (mechanical today, durable later). Steps 2–3 build the parser additively: single-bar glyphs (X/./-) with the sustain rule and per-row/per-beat :n subdivisions, then the | multi-bar separator and PICKUP: block. Step 4 adds tuplet support across RhythmSlot, the quantizer, and the renderer. Step 5 proves the three built-in seeds re-expressed as DSL round-trip to the expected events — the end-to-end validation that lets persistence stay slice 2. Everything is pure ChordFlow.Core, unit-tested, Desktop→Core unchanged. RhythmPatternEntity persistence (EX2), arrangement (EX1), multi-bar features (EX3), stroke/accent glyphs (EX4), Velocity/swing (EX5), multi-lane (EX6), nested tuplets (EX7), * sugar (EX8), and intra-group whitespace (EX9) are all out of scope.

---

## Steps

| Done | # | Step | Files touched | Blocked by | Satisfies |
|---|---|---|---|---|---|
| ✅ | 1 | Adopt multi-bar RhythmPattern(Bars: IReadOnlyList<PatternBar>) + PatternBar + SingleBar(...) helper; refactor FeelTransform / RhythmQuantizer / AlphaTexRenderer / Exercise to iterate Bars | src/ChordFlow.Core/Domain/RhythmPattern.cs, src/ChordFlow.Core/Domain/FeelTransform.cs, src/ChordFlow.Core/Rendering/RhythmQuantizer.cs, src/ChordFlow.Core/Rendering/AlphaTexRenderer.cs, src/ChordFlow.Core/Domain/Exercise.cs, tests/ChordFlow.Core.Tests/RhythmQuantizerTests.cs, tests/ChordFlow.Core.Tests/AlphaTexRendererTests.cs | — | IN1, C1, C5, C6 |
| ✅ | 2 | RhythmPatternParser.Parse(id, name, dsl, ts) → RhythmPattern (single bar): glyphs X/./- with the sustain rule, per-row and per-beat :n subdivisions, FormatException naming the bad cell/group | src/ChordFlow.Core/Domain/RhythmPatternParser.cs, tests/ChordFlow.Core.Tests/RhythmPatternParserTests.cs | 1 | IN2, IN3, IN4, C1, C2, C5 |
| ✅ | 3 | Add the \| bar separator (each segment parsed independently into a PatternBar) and the optional PICKUP: block mapping to the existing PickupMeasure | src/ChordFlow.Core/Domain/RhythmPatternParser.cs, src/ChordFlow.Core/Domain/PickupMeasure.cs, tests/ChordFlow.Core.Tests/RhythmPatternParserTests.cs | 2 | IN5, IN6, C6 |
| ✅ | 4 | RhythmSlot tuplet marker, RhythmQuantizer emitting tuplet slots for triplet-grid beat groups, AlphaTexRenderer emitting the verified alphaTex {tu N} token | src/ChordFlow.Core/Rendering/RhythmSlot.cs, src/ChordFlow.Core/Rendering/RhythmQuantizer.cs, src/ChordFlow.Core/Rendering/AlphaTexRenderer.cs, tests/ChordFlow.Core.Tests/RhythmQuantizerTests.cs, tests/ChordFlow.Core.Tests/AlphaTexRendererTests.cs | 1, 2 | IN7, C1, C4 |
| ✅ | 5 | Unit tests proving the three C# seeds (Beat1, Beat1And3, Quarters) re-expressed as DSL parse to the expected event positions — end-to-end validation without the EF layer | tests/ChordFlow.Core.Tests/RhythmSeedDslTests.cs | 2 | IN8, C6 |
---

### Legend

| Symbol | Meaning |
|--------|---------|
| ✅ | Done |
| 🔄 | In Progress |
| 🔳 | Pending |
| ❌ | Cancelled |

<!-- step:multi-bar-rhythmpattern-type -->
### Step 1 — Multi-bar RhythmPattern type

Introduce `public sealed record PatternBar(IReadOnlyList<RhythmEvent> Events)` and change `RhythmPattern.Events` → `Bars: IReadOnlyList<PatternBar>`, leaving `RhythmEvent` unchanged (no Velocity — EX5). Add `static RhythmPattern.SingleBar(id, name, events, ts, pickup = null)` so today's single-bar callers and seeds stay one line. Refactor the four consumers to iterate `Bars` — mechanical with today's one-element lists: `FeelTransform` maps per bar, `RhythmQuantizer` quantizes per bar, `AlphaTexRenderer` renders bar-by-bar, `Exercise` threads bars through. Feel stays on the play unit, never on the pattern (C3). Existing quantizer/renderer tests updated to the new shape; single-bar output stays byte-identical. Pure, no I/O (C5); durable shape adopted now so multi-bar is additive later (C6).

<!-- step:rhythmpatternparser-glyphs-sustain-subdivisions -->
### Step 2 — RhythmPatternParser — glyphs, sustain, subdivisions

Pure static peer of `ProgressionParser`. Parse one bar: split on spaces → beat groups; read each group's optional `:n` suffix (default 4); validate `n` divides 48, each group has exactly `n` cells, and Σ groups == TimeSignature beats; reject glyphs ∉ {X, ., -} (EX8 — no `*`; EX9 — no intra-group whitespace, space is the group separator). Cell ticks = 48 / n. Walk cells left→right per group carrying current state — `X` starts a note whose length runs to the next `X`/`-` or bar end, `.` sustains, `-` starts a rest — emitting `RhythmEvent` at absolute ticks (C1). A leading whole-row `:n` sets the default for all groups. Returns `RhythmPattern.SingleBar(...)`. `FormatException` names the offending cell/group/line (e.g. `beat group ':3' has 2 cells, expected 3`). Timing only — stroke/accent never authored here (C2). Tests: each glyph, sustain-length math, dotted-via-sustain (`X..X....`), per-row `:3`, per-beat mixed (`XXX:3 X... X.X:3 X...`), and every validation throw.

<!-- step:multi-bar-pickup-block -->
### Step 3 — Multi-bar | + PICKUP: block

Additive on step 2's per-bar parse. Split an optional leading `PICKUP:` block from the body first, then split the body on `|` → bar segments, parsing each independently (its own per-beat subdivisions) into a `PatternBar`; assemble `RhythmPattern(id, name, bars, ts, pickup?)`. The `PICKUP:` grid may be shorter than a full bar (1..cellsPerBar cells of its subdivision); `PickupMeasure.LengthTicks = cellCount · cellTicks` (a 4-cell `:4` pickup = the last beat, 48t). Glyph/subdivision/walk rules reused unchanged from step 2 — multi-bar *features* (fills, alignment) stay EX3. Tests: two- and four-bar patterns with differing per-bar content, a `:4` and a `:3` pickup, pickup-shorter-than-bar length math, and malformed-segment throws.

<!-- step:triplet-rendering-tu-n -->
### Step 4 — Triplet rendering — {tu N}

Add `Tuplet? Tuplet = null` to `RhythmSlot` with `readonly record struct Tuplet(int Numerator, int Denominator)` (e.g. (3,2) for an eighth-triplet). `RhythmQuantizer` recognises a beat group on the triplet grid (cell ticks 16 → eighth-triplet, 8 → 16th-triplet) and emits its cells as NoteValue 8 / 16 slots tagged `Tuplet(3,2)`; straight beats and the split-at-beat-line / span-boundary logic are untouched. `AlphaTexRenderer` emits `{tu N}` on each tuplet slot — graduating tuplets from unverified to supported while ties stay unsupported and still throw (C4); the renderer remains the only alphaTex-aware code. Arbitrary nested tuplets/polyrhythm stay EX7. Tests: a `:3` beat → three `{tu 3}`-tagged eighths at 0/16/32, a mixed straight+triplet bar, a 16th-triplet beat, and that straight-only patterns render byte-identically to before.

<!-- step:seed-dsl-round-trip-tests -->
### Step 5 — Seed-DSL round-trip tests

Assert that `RhythmPatternParser.Parse` of the canonical seed DSLs — Beat1 `X...............`, Beat1And3 `X.......X.......`, Quarters `X...X...X...X...` — yields the same `RhythmEvent` positions and lengths as today's C#-authored seeds. This is the round-trip proof the design's slice-1 boundary calls for: the `RhythmPatternEntity` persistence that would seed these from DB rows is slice 2 (EX2), but the parser must demonstrably reproduce the real shipped patterns first.
