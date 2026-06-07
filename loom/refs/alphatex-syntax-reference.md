---
type: reference
id: rf_01KTHJN829FMW964FTNCFSS2GM
title: "alphaTex Syntax Reference"
status: active
created: 2026-06-07
version: 1
tags: []
parent_id: null
child_ids: []
requires_load: []
slug: alphatex-syntax-reference
description: "Verified alphaTex notation syntax for ChordFlow's AlphaTexRenderer — metadata, notes, durations, chords, rests, bars."
---
# alphaTex Syntax Reference

The text DSL ChordFlow's `AlphaTexRenderer` emits. **Verified against the official docs** (alphaTex introduction + bar-metadata pages) on 2026-06-07. Items marked ⚠️ are unverified and must be confirmed in the [playground](https://www.alphatab.net/docs/playground) before use.

## Document structure

```
<metadata directives>
.                       ← a lone dot ends the metadata block, begins music
<bar> | <bar> | ...     ← bars separated by pipes
```

## Metadata directives (header)

| Directive | Syntax | Notes |
|-----------|--------|-------|
| Title | `\title "12 Bar Blues — Bb"` | quoted string |
| Subtitle | `\subtitle "..."` | |
| Tempo | `\tempo 80` | BPM; also `\tempo (120 "Moderate")`, `\tempo (60 "" 0.5 hide)` |
| Time signature | `\ts 4 4` | also `\ts common` (= 4/4 with C symbol), `\ts 6 8`, `\ts 3 4` |
| Key signature | `\ks bb` | flat keys: `cb gb db ab eb bb f`; sharp/natural: `c g d a e b f# c#`; also `bbmajor`, `aminor`, etc. Docs show **lowercase** flats. |
| Clef | `\clef g2` | `g2 f4 c3 c4 n treble bass tenor alto neutral` |

Bar-level metadata can also appear at the start of any bar, before notes: `\ts 3 4 | \ks C | ... |`.

## Notes, durations, chords, rests

- **Note:** `fret.string` — e.g. `3.4` = fret 3 on string 4. alphaTab string numbering: **1 = highest-pitched string**.
- **Duration (stateful):** a leading `:N` token sets the duration for **all following beats until changed** — persists across bars.
  - `:1` whole · `:2` half · `:4` quarter · `:8` eighth · `:16` sixteenth · `:32` …
- **Chord / simultaneous notes:** group in parentheses — `(3.4 3.3 3.2)`.
- **Rest:** `r`.
- **Bar separator:** `|`.

⚠️ **Dotted notes and ties** — exact token NOT yet verified. The early-exploration notation `(...)h.2` / `h.2` is **wrong — do not use**. MVP rhythms (beat-1, beat-1+3, quarters) need only `:4` + `r`, so this is not required for v1.

## Worked example — 12-bar blues in Bb, beats 1 & 3

A "beats 1 & 3" bar in 4/4 = four quarters: chord, rest, chord, rest. Frets below are placeholders (`x`) — real values come from ChordFlow's `VoicingBook`, not hardcoded in alphaTex.

```alphatex
\title "12 Bar Blues — Bb"
\subtitle "Beginner — Beats 1 & 3"
\tempo 80
\ts 4 4
\ks bb
.
:4 (x.4 x.3 x.2) r (x.4 x.3 x.2) r |
...
```

## ChordFlow renderer mapping

| Domain concept | alphaTex |
|----------------|----------|
| `Key` | `\ks <flat-or-sharp>` |
| `Exercise.Tempo` | `\tempo N` |
| 4/4 | `\ts 4 4` |
| `Duration.Quarter` | `:4` (emit once, it persists) |
| `Beat.IsHit == true` + `Voicing` | `(fret.string …)` |
| `Beat.IsHit == false` | `r` |
| bar boundary | `|` |

## Sources
- https://www.alphatab.net/docs/alphatex/introduction
- https://www.alphatab.net/docs/alphatex/bar-metadata
- Playground (verify ⚠️ items): https://www.alphatab.net/docs/playground
