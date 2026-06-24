---
type: idea
id: id_01KVVCJ200HK15X7GABK2P2NKT
title: Rhythm DSL — Accurate-Notation Redesign
status: done
created: 2026-06-23
updated: 2026-06-24
version: 3
tags: []
parent_id: null
requires_load: []
---
# Rhythm DSL — Accurate-Notation Redesign

## Goal

Make the Rhythm DSL produce **accurate scores** — the rendered notation always shows the true note/rest values the author wrote, including **dotted notes and ties** — so syncopated and dotted comping (the Charleston, anticipations, the "and-of-4" push) renders and plays instead of throwing.

## Origin

`songbook/jazz-blues` dogfood — **Finding 3**: the Charleston comp threw (*alphaTex tie rendering not supported in v1*). The investigation (this thread's chat-001) reframed the problem: the throw was a symptom, not the disease.

## Root cause (reframed)

The real issue is that `.` **conflated notated duration with guitar sustain**. Because a note rings to the next attack, any ring that crosses a syncopation must be notated with a tie or a dot — and the renderer *throws* on both. Standard notation (and Guitar Pro) keep these separate: a note's **written value** is what it is; "let it ring" is a **playback** overlay ("Let Ring"), not a longer notated value. The fix is to make the grammar describe **notated durations only**, emit dots/ties faithfully, and treat sustain as a later playback concern.

The Charleston itself turned out to need **neither** a dot nor a tie when written as a clean quarter + eighth (`:2 X.-X-...`, verified in the app). So tie/dotted support is no longer *required by the Charleston* — but it **is** required for accurate notation of genuinely dotted/syncopated rhythms, and that is what this thread delivers.

## The grammar (locked — see design for full spec)

| Token | Meaning |
|------|------|
| `X` | attack — a note lasting itself **+ each following `.`** |
| `.` | extend the currently **sounding** note (error if nothing sounds) |
| `-` | one cell of **silence** (repeat for longer rests) |
| `_` | **tie** into the next group — must be followed by `X` or be bar-final (with a tied next bar) |
| `\|` · `:n` | bar · subdivision (unchanged) |
| space | insignificant (readability; current narrow role at subdivision switches) |

- A note-group's cell count must equal **one representable value** — a base (1/2/4/8/16) or a single-**dotted** value (1.5×). Anything else is an **error**: the author ties it with `_` (so the engine never guesses).
- `_` is the universal escape hatch: any duration not expressible as one value (5-cell, double-dotted, cross-beat, cross-bar) is a tie chain.
- Rests are `-` runs; the quantizer decomposes them into representable rest values; rests never tie.

## Deltas from today

1. `.` may no longer extend a **rest** — silence is `-`-only; `.`-after-nothing-sounding is an error (`.` ≡ sound, `-` ≡ silence).
2. Add `_` (authored ties + the author's dot-vs-tie choice + cross-bar anticipation).
3. Renderer/quantizer **emit** dotted + tied values instead of throwing — and, with each note-group constrained to one representable value, the quantizer drops its beat-line splitting/coalescing for notes (ties come only from `_`). A `RhythmSlot.Dotted` flag is added; `TiedToPrevious` survives.

## Scope

**In:** the grammar refinement (parser), dotted + tied emission (quantizer + renderer), verified alphaTex dot/tie tokens (update `alphatex-syntax-reference.md`), seed-pattern migration to the new grammar, ref updates (DSL + domain).
**Out:** Let Ring (playback sustain) — a follow-on (verify alphaTab support first); quintuplets/32nds; accents/strokes/swing (play-time overlays); auto double-dot.

## Validation

- Charleston (`:2 X.-X-...`) and a genuinely **dotted** comp (`:2 X..X.-..`-style) both render + play.
- A unit test: a dotted/tied pattern quantizes to verified dotted/tied alphaTex instead of throwing; a non-representable group without `_` errors with a clear message.
- **Re-add `charleston.dsl`** to the default pack.
- Dogfood: render on the fretboard/score UI page.

## Spun-off siblings

Two display concerns surfaced in chat-001 are **separate threads** (not this one): `ui/dsl-monospace-font` (monospace for all DSL editors) and `ui/staff-display-mode` (tab-only `\staff{tabs}` default + toggle to combined score+tab).