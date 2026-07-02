---
type: design
id: de_01KVW7YEZ70AXE0NNAE82DJWTX
title: Rhythm DSL — Accurate-Notation Redesign — Design
status: done
created: 2026-06-24
updated: 2026-06-24
version: 4
idea_version: 3
tags: []
parent_id: id_01KVVCJ200HK15X7GABK2P2NKT
requires_load: []
---
# Rhythm DSL — Accurate-Notation Redesign — Design

> Load before reasoning here: `loom/refs/chordflow-dsl-reference.md` (Rhythm DSL) and `loom/refs/chordflow-domain-model-reference.md` (Rhythm kernel + Rendering seam). Both must be **updated** when this lands.

## 1. The decision (from chat-001)

Notation and sustain are **separate concerns**. A note's *written value* is the duration shown on the score; *sustain* (a string ringing) is a **playback** overlay ("Let Ring"), not a longer written value. Today's `.` conflates them, so any ring across a syncopation forces a tie/dot — which the renderer throws on.

This design makes the grammar describe **notated durations only**, has the engine **emit dots and ties faithfully**, and defers sustain to a later playback feature. The Charleston (`:2 X.-X-...`) already renders without dots/ties; this thread exists for *accurate notation of genuinely dotted/syncopated rhythms*, with ties as the universal escape hatch.

## 2. Grammar specification

### 2.1 Tokens

| Token | Meaning |
|------|------|
| `X` | **attack** — starts a note that lasts itself + each immediately following `.` |
| `.` | **sustain** the currently sounding note by one cell. **Invalid** (FormatException) when nothing is sounding — i.e. at the start of a bar, immediately after `-`, or immediately after `_`. (`.` ≡ sound.) |
| `-` | one cell of **silence**. Repeat for longer rests. (`-` ≡ silence.) |
| `_` | **tie**: the sounding note continues into the next group with **no re-attack**. Must be immediately followed by `X` (tie within the bar) or be the **last cell of a bar** whose next bar begins with a note (tie across the barline). |
| `\|` | bar separator (each bar parsed independently). |
| `:n` | subdivision = cells per beat (default 4); leading token sets the row default, a `:n` suffix overrides per run. **Unchanged.** |
| space | **insignificant** — readability only; retains its current narrow role separating subdivision runs and attaching per-run `:n`. |

### 2.2 The note-group rule (the heart)

A **note-group** is an `X` plus its trailing `.` cells. Its cell count, times the cell length, is a duration that **must equal exactly one representable note value**:

- a **base** value — 16th/8th/quarter/half/whole (cells = `n`·{0.25,0.5,1,2,4} per beat), or
- a **single-dotted** value — 1.5× a base (e.g. 6 cells at `:4` = dotted quarter; 3 cells at `:2` = dotted quarter).

Any other count (a 5-cell group, a double-dotted 7-cell group, a syncopated run that isn't a single value) is a **FormatException** that names the group and says *"ambiguous duration — tie it with `_`."* The engine never silently decomposes a note. This is what removes guessing and makes the score 1:1 with the DSL.

### 2.3 Ties

`_` is the only source of ties. Two placements:

- **Within a bar:** `X..._X.` — a dotted-quarter's worth (1.5 beats) **notated as quarter tied to eighth** (the author chose a tie over the dot; `X.....` would be the dotted-quarter spelling of the same sound).
- **Across the barline:** `…X._ | X… ` — the trailing `_` ties into the next bar's first note (anticipation / the "and-of-4 push").

Validation:
- `_` not followed by `X` and not bar-final → error ("tie must continue into a note").
- **Bar-final `_` on the last bar, or before a bar that starts with `-`** → **dangling tie** error.
- A `_` whose tie would cross a **chord-span boundary** (harmonic layer) → error (loud over silent); the harmonic quantizer re-attacks at chord changes and cannot honor a tie across them. (Plain timing-only patterns have no chord spans, so this only bites in the harmonic path.)

### 2.4 Rests

Silence is `-` runs. The quantizer decomposes a rest run greedily into representable rest values (largest-first); a rest may render as multiple cells or a dotted rest, and **rests are never tied**. No author burden — rest grouping is a rendering detail, unlike notes.

### 2.5 Worked examples (`:4` unless noted)

| Intent | DSL |
|------|------|
| whole note | `X...............` |
| half note + half rest | `X.......--------` |
| quarter + quarter rest + half rest | `X...----` `--------` |
| dotted quarter + eighth | `X.....` `X.` |
| dotted half + quarter | `X...........` `X...` |
| quarter tied to eighth (tie spelling) | `X..._X.` |
| Charleston (clean) `:2` | `X.-X-...` |
| Charleston (dotted-quarter spelling) `:2` | `X..X.-..` → dotted quarter + eighth + eighth rest |
| anticipation across barline `:2` | `X...... X._ \| X. X. X...` |

## 3. Coverage

Complete for the trainer: every base value, single-dotted values directly, and **any other duration via `_` tie chains** (double-dots, 5/7-cell durations, cross-beat and cross-bar syncopation). Rests at every value. Triplets via `:n` as today. Deliberately out: accents/strokes/swing (play-time overlays — DSL stays timing-only), quintuplets/32nds (out of v1 grid), grace notes/fermata (not rhythmically essential), auto double-dot (use ties), pitch (chords supply it).

## 4. Implementation

Dependency direction unchanged: `Music/Rhythm` (parser, model) is a kernel sink; `Rendering` (quantizer, renderer) sits above it; nothing in `Music` learns about alphaTex.

### 4.1 `Music/Rhythm` — model + parser

- **`RhythmEvent`**: add `bool TiedToNext` (default false). `Hit(...)` stays the unaccented down-stroke default; tie is set by the parser.
- **`RhythmPatternParser.Walk`**:
  - `case '.'`: error if `openNoteStart is null` (nothing sounding) — enforces `.` ≡ sound and kills `.`-on-rest.
  - `case '-'`: close any open note, then advance as silence (as today). A `.` after this errors.
  - new `case '_'`: set a `pendingTie` flag; do **not** advance a cell (a `_` is a boundary marker, zero width) — *decision point, see §5*. The next `X` closes the current note with `TiedToNext = true` and opens the tied continuation. A `_` followed by anything but `X`, or bar-final, is validated per §2.3.
  - bar-final `_`: mark the bar's last note `TiedToNext`; cross-bar validation happens once all bars are parsed (need the next bar's first event).
  - Reject a note-group whose duration is not a single representable value **at parse time** (the parser knows cell counts and `n`), with the §2.2 message.

### 4.2 `Rendering` — quantizer + slot + renderer

- **`RhythmSlot`**: add `bool Dotted` (default false). `TiedToPrevious` stays.
- **`RhythmQuantizer`**: notes no longer beat-line-split or coalesce. Each `RhythmEvent` maps to **one** `RhythmSlot` (its single value; `Dotted` when 1.5×); `TiedToPrevious` is set when the *previous* event had `TiedToNext`. Rests keep largest-fit decomposition (multiple untied rest slots). The harmonic overload still re-attacks at chord boundaries (and rejects a tie that would cross one). `LargestAlignedFit`/the note-coalescing path is **removed**; `LargestFit` (rests) and `LargestFitTuplet` (triplets) remain.
- **`AlphaTexRenderer`**: replace the two `throw` sites with emission. A `Dotted` slot emits its base `:N` + chord group + **`{d}`** beat property (double-dot `{dd}` reserved). A `TiedToPrevious` slot re-states each voiced string of the held chord with the **`-.{string}`** tie fret (e.g. `(-.4 -.3 -.2 -.1)`); alphaTab ties to the previous note on that string (`{t}` note-effect is the documented alternative — we use `-.string`, terser, no fret re-derivation). Tuplet `{tu N}` suffix unchanged. Tokens verified against alphaTab (chat-001); update `alphatex-syntax-reference.md` in this step.

### 4.3 Content + refs

- Migrate every seed `.dsl` rhythm to the new grammar (breaking change — acceptable per project policy). Enumerate as a plan step; re-add `charleston.dsl`.
- Update `chordflow-dsl-reference.md` (Rhythm DSL section: tokens, the note-group rule, `_`, examples) and `chordflow-domain-model-reference.md` (RhythmEvent.TiedToNext, RhythmSlot.Dotted, quantizer simplification) in the same unit of work.

## 5. Open implementation decisions (resolve in the plan, not blocking the grammar)

1. **alphaTab dot + tie + let-ring tokens — RESOLVED** (verified from alphaTab docs, chat-001):
   - **Dot:** `{d}` beat property; double-dot `{dd}`. Dotted slot → base `:N` + group + `{d}` (dotted quarter = `:4 (…){d}`).
   - **Tie:** `-.{string}` fret form — a tied continuation re-states each voiced string with fret `-`; alphaTab ties to the prior note on that string. (`{t}` note-effect is the alternative.)
   - **Let Ring:** `{lr}` note effect — confirmed for the deferred follow-on.
   `alphatex-syntax-reference.md` gets these (mark tie/dot verified) in the renderer step.
2. **`_` width** — modeled as a zero-width boundary (above). Alternative: `_` occupies a cell. Zero-width is cleaner (it sits *between* groups) and keeps cell-count math intact; confirm no parser ambiguity with bar-final `_`.
3. **Let Ring** — out of scope here; capture as a follow-on. Token verified (`{lr}`, item 1).

## 6. Test plan

- Parser: `.`-after-rest errors; `_` placement rules (mid-bar ok, bar-final-with-next-note ok, dangling errors); non-representable group errors with the right message.
- Quantizer: dotted-quarter group → one `Dotted` slot (not quarter+tied-eighth); `_` chain → `TiedToPrevious` slots; rest run → untied rest slots; chord-boundary re-attack + cross-boundary-tie rejection (harmonic path).
- Renderer: dotted + tied slots emit verified alphaTex (golden strings); triplet suffix preserved.
- End-to-end: Charleston + a dotted comp render and play; every migrated seed pattern round-trips.