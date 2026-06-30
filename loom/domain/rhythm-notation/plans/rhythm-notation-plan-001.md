---
type: plan
id: pl_01KVWA3WWN0R5FBW6N8G5SZATF
title: Rhythm DSL — Accurate-Notation Redesign — Plan
status: done
created: 2026-06-24
updated: 2026-06-24
version: 1
design_version: 4
req_version: 1
tags: []
parent_id: de_01KVW7YEZ70AXE0NNAE82DJWTX
requires_load: []
target_version: 0.1.0
actual_release: 0.12.0
steps:
  - id: add-model-fields-rhythmevent-tiedtonext-and
    order: 1
    status: done
    description: "Add model fields: RhythmEvent.TiedToNext and RhythmSlot.Dotted (both default false)"
    files_touched: [src/ChordFlow.Core/Music/Rhythm/RhythmEvent.cs, src/ChordFlow.Core/Rendering/RhythmSlot.cs]
    blocked_by: []
    satisfies: [IN5]
  - id: parser-sound-only-tie-token-note
    order: 2
    status: done
    description: "Parser: `.`-sound-only, `_` tie token, note-group single-value validation + placement rules; parser tests"
    files_touched: [src/ChordFlow.Core/Music/Rhythm/RhythmPatternParser.cs, tests/ChordFlow.Core.Tests/RhythmPatternParserTests.cs]
    blocked_by: [1]
    satisfies: [IN1, IN2, IN3, IN10]
  - id: quantizer-one-slot-per-note-dotted
    order: 3
    status: done
    description: "Quantizer: one slot per note (Dotted when 1.5×), ties from TiedToNext, drop note coalescing, keep rest/triplet decomposition, harmonic re-attack + cross-boundary tie rejection; quantizer tests"
    files_touched: [src/ChordFlow.Core/Rendering/RhythmQuantizer.cs, tests/ChordFlow.Core.Tests/RhythmQuantizerTests.cs]
    blocked_by: [1, 2]
    satisfies: [IN6, IN4, IN10]
  - id: renderer-emit-for-dotted-for-ties
    order: 4
    status: done
    description: "Renderer: emit `{d}` for dotted, `-.{string}` for ties (remove both throws); golden renderer tests"
    files_touched: [src/ChordFlow.Core/Rendering/AlphaTexRenderer.cs, tests/ChordFlow.Core.Tests/AlphaTexRendererTests.cs]
    blocked_by: [3]
    satisfies: [IN7, IN10]
  - id: migrate-seed-rhythm-patterns-to-the
    order: 5
    status: done
    description: Migrate seed rhythm patterns to the new grammar; re-add charleston.dsl to the default pack
    files_touched: [src/ChordFlow.Core/Content/default-pack/rhythms/beat_1.dsl, src/ChordFlow.Core/Content/default-pack/rhythms/beat_1_3.dsl, src/ChordFlow.Core/Content/default-pack/rhythms/quarters.dsl, src/ChordFlow.Core/Content/default-pack/rhythms/charleston.dsl]
    blocked_by: [4]
    satisfies: [IN8, C4]
  - id: reference-doc-sync-alphatex-syntax-tie
    order: 6
    status: done
    description: "Reference-doc sync: alphaTex syntax (tie/dot/let-ring verified), DSL ref (Rhythm DSL grammar), domain-model ref"
    files_touched: [loom/refs/alphatex-syntax-reference.md, loom/refs/chordflow-dsl-reference.md, loom/refs/chordflow-domain-model-reference.md]
    blocked_by: [5]
    satisfies: [IN9]
  - id: redesign-as-a-tied-note-within
    order: 7
    status: done
    description: Redesign `_` as a tied note (within + cross-bar), rhythm-wins-over-harmony tie holding, and aligned rest coalescing
    files_touched: [src/ChordFlow.Core/Music/Rhythm/RhythmPattern.cs, src/ChordFlow.Core/Music/Rhythm/RhythmPatternParser.cs, src/ChordFlow.Core/Rendering/RhythmQuantizer.cs, src/ChordFlow.Core/Rendering/AlphaTexRenderer.cs, src/ChordFlow.Desktop/wwwroot/content-crud.js, tests/ChordFlow.Core.Tests/RhythmPatternParserTests.cs, tests/ChordFlow.Core.Tests/RhythmQuantizerTests.cs, tests/ChordFlow.Core.Tests/AlphaTexRendererTests.cs]
    blocked_by: []
    satisfies: [IN3, IN4, IN12]
  - id: visual-end-to-end-verify-in
    order: 8
    status: done
    description: Visual end-to-end verify in the running app (Charleston + a dotted comp render and play)
    files_touched: []
    blocked_by: [5]
    satisfies: [IN11, C5]
---
# Rhythm DSL — Accurate-Notation Redesign — Plan

## Goal

Implement the accurate-notation redesign of the Rhythm DSL: make the grammar describe notated durations only, emit dotted notes and authored ties faithfully, and stop throwing on syncopated/dotted rhythms. Bottom-up — model fields first, then parser, quantizer, renderer, then content migration, ref sync, and a visual end-to-end check. Each code step ships with its tests; the build and full suite stay green throughout (C6). Tie/dot/let-ring tokens are already verified (design §5); no spike needed.

---

## Steps

| Done | # | Step | Files touched | Blocked by | Satisfies |
|---|---|---|---|---|---|
| ✅ | 1 | Add model fields: RhythmEvent.TiedToNext and RhythmSlot.Dotted (both default false) | src/ChordFlow.Core/Music/Rhythm/RhythmEvent.cs, src/ChordFlow.Core/Rendering/RhythmSlot.cs | — | IN5 |
| ✅ | 2 | Parser: `.`-sound-only, `_` tie token, note-group single-value validation + placement rules; parser tests | src/ChordFlow.Core/Music/Rhythm/RhythmPatternParser.cs, tests/ChordFlow.Core.Tests/RhythmPatternParserTests.cs | 1 | IN1, IN2, IN3, IN10 |
| ✅ | 3 | Quantizer: one slot per note (Dotted when 1.5×), ties from TiedToNext, drop note coalescing, keep rest/triplet decomposition, harmonic re-attack + cross-boundary tie rejection; quantizer tests | src/ChordFlow.Core/Rendering/RhythmQuantizer.cs, tests/ChordFlow.Core.Tests/RhythmQuantizerTests.cs | 1, 2 | IN6, IN4, IN10 |
| ✅ | 4 | Renderer: emit `{d}` for dotted, `-.{string}` for ties (remove both throws); golden renderer tests | src/ChordFlow.Core/Rendering/AlphaTexRenderer.cs, tests/ChordFlow.Core.Tests/AlphaTexRendererTests.cs | 3 | IN7, IN10 |
| ✅ | 5 | Migrate seed rhythm patterns to the new grammar; re-add charleston.dsl to the default pack | src/ChordFlow.Core/Content/default-pack/rhythms/beat_1.dsl, src/ChordFlow.Core/Content/default-pack/rhythms/beat_1_3.dsl, src/ChordFlow.Core/Content/default-pack/rhythms/quarters.dsl, src/ChordFlow.Core/Content/default-pack/rhythms/charleston.dsl | 4 | IN8, C4 |
| ✅ | 6 | Reference-doc sync: alphaTex syntax (tie/dot/let-ring verified), DSL ref (Rhythm DSL grammar), domain-model ref | loom/refs/alphatex-syntax-reference.md, loom/refs/chordflow-dsl-reference.md, loom/refs/chordflow-domain-model-reference.md | 5 | IN9 |
| ✅ | 7 | Redesign `_` as a tied note (within + cross-bar), rhythm-wins-over-harmony tie holding, and aligned rest coalescing | src/ChordFlow.Core/Music/Rhythm/RhythmPattern.cs, src/ChordFlow.Core/Music/Rhythm/RhythmPatternParser.cs, src/ChordFlow.Core/Rendering/RhythmQuantizer.cs, src/ChordFlow.Core/Rendering/AlphaTexRenderer.cs, src/ChordFlow.Desktop/wwwroot/content-crud.js, tests/ChordFlow.Core.Tests/RhythmPatternParserTests.cs, tests/ChordFlow.Core.Tests/RhythmQuantizerTests.cs, tests/ChordFlow.Core.Tests/AlphaTexRendererTests.cs | — | IN3, IN4, IN12 |
| ✅ | 8 | Visual end-to-end verify in the running app (Charleston + a dotted comp render and play) | — | 5 | IN11, C5 |
---

### Legend

| Symbol | Meaning |
|--------|---------|
| ✅ | Done |
| 🔄 | In Progress |
| 🔳 | Pending |
| ❌ | Cancelled |

<!-- step:add-model-fields-rhythmevent-tiedtonext-and -->
### Step 1 — Add model fields: RhythmEvent.TiedToNext and RhythmSlot.Dotted (both default false)

Add `bool TiedToNext = false` to the `RhythmEvent` record (keep `Hit(...)` as the unaccented down-stroke default). Add `bool Dotted = false` to the `RhythmSlot` record. Pure additive field changes; no behavior yet. Build green.

<!-- step:parser-sound-only-tie-token-note -->
### Step 2 — Parser: `.`-sound-only, `_` tie token, note-group single-value validation + placement rules; parser tests

In `Walk`: `case '.'` errors when nothing is sounding (bar start, after `-`, after `_`). New `case '_'`: zero-width boundary that sets a pendingTie flag; the next `X` closes the current note with `TiedToNext=true` and opens the tied continuation; validate `_` is followed by `X` or is bar-final. Enforce the note-group rule at parse time — each `X`+`.` group's cell-duration must equal one representable value (base or 1.5× dotted), else FormatException naming the group. Bar-final `_` cross-bar validation after all bars parse (needs next bar's first event); dangling tie (last bar / next bar starts with `-`) errors. Tests cover each error and the happy paths (dotted group, tie chain, cross-bar tie).

<!-- step:quantizer-one-slot-per-note-dotted -->
### Step 3 — Quantizer: one slot per note (Dotted when 1.5×), ties from TiedToNext, drop note coalescing, keep rest/triplet decomposition, harmonic re-attack + cross-boundary tie rejection; quantizer tests

Each note `RhythmEvent` → one `RhythmSlot` (its single value; `Dotted=true` when 1.5×); set `TiedToPrevious` from the prior event's `TiedToNext`. Remove the note beat-line splitting/coalescing path (`LargestAlignedFit`). Rests keep `LargestFit` decomposition (multiple, untied); triplets keep `LargestFitTuplet`. In the harmonic (chordBoundaries) overload, still re-attack at chord boundaries and reject a `_` tie that would cross one (loud error). Tests: dotted group → one Dotted slot (not quarter+tied-eighth); `_` chain → TiedToPrevious; rest run untied; chord-boundary re-attack + cross-boundary tie rejection.

<!-- step:renderer-emit-for-dotted-for-ties -->
### Step 4 — Renderer: emit `{d}` for dotted, `-.{string}` for ties (remove both throws); golden renderer tests

Replace the two `TiedToPrevious` throw sites with emission. A `Dotted` slot emits base `:N` + chord group + `{d}`. A `TiedToPrevious` slot re-states each voiced string of the held chord with `-.{string}` (e.g. `(-.4 -.3 -.2 -.1)`). Tuplet `{tu N}` suffix unchanged. Golden-string tests for dotted + tied output (both comping and lead render paths) and that tuplets still render.

<!-- step:migrate-seed-rhythm-patterns-to-the -->
### Step 5 — Migrate seed rhythm patterns to the new grammar; re-add charleston.dsl to the default pack

Rewrite each existing seed pattern to the new grammar (explicit `-` rests, `_` ties / `.` dotted where applicable) so it still parses and renders. Add `charleston.dsl` (`:2 X.-X-...` quarter + eighth, or the dotted-quarter spelling — pick the clearest). Confirm the pack imports clean.

<!-- step:reference-doc-sync-alphatex-syntax-tie -->
### Step 6 — Reference-doc sync: alphaTex syntax (tie/dot/let-ring verified), DSL ref (Rhythm DSL grammar), domain-model ref

alphaTex ref: mark `-`/`{t}` tie and `{d}`/`{dd}` dot as verified, record `{lr}` for the Let-Ring follow-on. DSL ref: rewrite the Rhythm DSL section (tokens table, the note-group rule, `_`, worked examples). Domain-model ref: `RhythmEvent.TiedToNext`, `RhythmSlot.Dotted`, the quantizer simplification. Edit these with loom_patch_doc / loom_update_doc (refs are gate-excluded but versioned).

<!-- step:redesign-as-a-tied-note-within -->
### Step 7 — Redesign `_` as a tied note (within + cross-bar), rhythm-wins-over-harmony ti...

Post-design refinement (chat). `_` becomes a **tied note** (occupies cells, extends with `.`, ties to the previous note) with `PatternBar.StartsTied` for a leading-`_` cross-bar tie. **Rhythm wins over harmony**: a tied note is one held slot re-stating the last sounding voicing's strings (`-.string`) — the cross-boundary tie rejection is removed; the renderer tracks `RenderState.LastVoicing`. Rests coalesce to the largest metrically-aligned value (`LargestAlignedFit`) — a half rest is one `:2 r`. Updated the UI rhythm help text. Req amended (IN3/IN4) + IN12 added; refs synced. 664 tests green.

<!-- step:visual-end-to-end-verify-in -->
### Step 8 — Visual end-to-end verify in the running app (Charleston + a dotted comp render and play)

Run the app: the Charleston and a genuinely dotted comp both render (correct notation) and play. Dogfood on the score / fretboard UI page. A passing string assertion is not sufficient (C5) — confirm visually + audibly.
