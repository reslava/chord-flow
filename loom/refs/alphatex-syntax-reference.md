---
type: reference
id: rf_01KTHJN829FMW964FTNCFSS2GM
title: alphaTex Syntax Reference
status: active
created: 2026-06-07
updated: 2026-06-25
version: 12
tags: []
parent_id: null
requires_load: []
slug: alphatex-syntax-reference
description: Verified alphaTex notation syntax for ChordFlow's AlphaTexRenderer — metadata, notes, durations, chords, rests, bars.
---
# alphaTex Syntax Reference

The text DSL ChordFlow's `AlphaTexRenderer` emits. **Verified against the official docs** (alphaTex introduction + bar-metadata pages) on 2026-06-07. Items marked ⚠️ are unverified and must be confirmed in the [playground](https://www.alphatab.net/docs/playground) before use.

## Document structure

```
<metadata directives>
.                       ← a lone dot ends the metadata block, begins music
<bar> | <bar> | ...     ← bars separated by pipes
```

## Multiple tracks (two staves)

Verified 2026-06-15 against the [structural-metadata](https://www.alphatab.net/docs/alphatex/structural-metadata) + [document-structure](https://www.alphatab.net/docs/alphatex/document-structure) docs. A `\track "Name" "short"` directive (Structural Metadata) starts a new track; the bars after it belong to that track. **Score metadata** (`\title` / `\subtitle` / `\tempo` + the chord directives) stays at the top, terminated by the lone `.`; **bar metadata** (`\ts` / `\ks`) moves *into each track* (it is bar / master-bar level). (Bars-per-row is **not** set in alphaTex — `{ defaultSystemsLayout N }` is multi-track-only and unreliable; ChordFlow controls it JS-side via `display.barsPerRow`, see `alphatab-js-api-reference.md`. ChordFlow no longer emits the `{ … }` block.)

```
\title "…"
\subtitle "…"
\tempo 80
.
\track "Comping" "comp"
\ts 4 4
\ks bb
:4 (1.5 0.4 1.3) … |          ← rhythm-guitar bars
\track "Lead" "lead"
\ts 4 4
\ks bb
:4 x.3 r x.3 r … |           ← lead bars as dead notes
```

ChordFlow emits two tracks **only** when an `Exercise.Lead` pattern is present; with no lead it stays single-track (no `\track` wrapper, `\ts`/`\ks` in the header — byte-identical to the pre-lead output, design §7.4).

## Dead / muted notes

- **Dead note:** `x.3` — a muted/dead note on string 3 (no pitch). The v1 lead track renders each hit as `x.3` (rhythm only; pitched `LeadTargets` are the deferred swap-in), each rest as `r`. Also `3.3{x}` (a fretted note with the dead-note effect). Verified 2026-06-15.

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

- **Dotted notes** — `{d}` is a **beat property** in braces after the beat: `:4 (3.4 3.3){d}` = a dotted quarter chord. Double dot is `{dd}`. The note value stays the base (`:4`); `{d}` adds the augmentation dot. Verified against alphaTab (2026-06-24).
- **Ties** — re-state the note with the **`-` fret**: `3.3 -.3` ties the second note to the first on string 3. For a chord, re-state each voiced string: `(3.4 3.3 3.2) (-.4 -.3 -.2)`. (`{t}` is an equivalent note-effect spelling.) ChordFlow emits the `-.string` form. Verified against alphaTab (2026-06-24).
- **Let ring** (deferred) — `{lr}` note effect (`3.4{lr}`) sustains a note past its written value at **playback** without changing the notation. Not emitted yet; reserved for the let-ring follow-on. Verified token (2026-06-24).

> The early-exploration notation `(...)h.2` / `h.2` was **wrong — do not use**.

## Anacrusis (pickup bars)

**`\ac`** — bar metadata marking a bar as an **anacrusis** (pickup). Anacrusis bars do **not** follow the strict time-signature timing: the bar's length is defined by the **actual beats/notes in it**, so a pickup is *not* padded to a full bar and is *not* counted as bar 1. It is emitted at the **start of the bar's content**, before the stateful `:N` and the beats (like `\ts`/`\ks`, it is bar-level). Verified 2026-06-22 — the bundled alphaTab supports anacrusis (`isAnacrusis`).

> **Limitation:** `\ac` does **not** suppress the bar number — alphaTab still numbers the anacrusis as **bar 1** (the first full bar then shows as bar 2). alphaTex exposes no directive to renumber; only bar-number *visibility* (show/hide) is controllable JS-side. Musically a pickup is unnumbered, so this is a known, accepted gap.

```
\ks D \ts 24 16 \ac r.16 6.3 7.3 9.3 7.3 6.3 |   ← incomplete pickup bar, then full bars follow
```

ChordFlow emits `\ac` as a prefix on the leading **pickup bar** only (when a `PickupMeasure` is present) — on both the comping and the lead track so the staves stay aligned. The pickup remains a single bar (`\ac` is a prefix, not a new bar):

```
\ac :4 (1.5 0.4 1.3) |     ← comping pickup, voiced with the first chord
\ac :4 r |                 ← lead pickup, rests (the lead doesn't play during the anacrusis in v1)
```

## Triplet feel (swing) — `\tf`

**`\tf <value>`** — bar metadata that sets the **triplet feel** (aka. swing) play style for the bar **and every bar after it, until the next `\tf` or the song end**. It swings both **rendering and playback** natively (alphaTab owns the long-short groove), so straight 8ths written in the score read and play swung — no need to hand-author the warp. Verified 2026-06-22 against the [bar-metadata `#tf`](https://www.alphatab.net/docs/alphatex/bar-metadata#tf) docs.

Values (`Ident | Number`). ChordFlow emits the lowercase ident; wired today are the first three:

| Value | Meaning |
|-------|---------|
| `none` | no triplet feel (even) — ChordFlow emits **no** `\tf` for this |
| `triplet8th` | triplet-8th swing — a straight 8th pair plays as 2/3 + 1/3 of the beat (≡ a `:3 X.X` triplet) |
| `triplet16th` | triplet-16th swing — the same shape at the 16th level |
| `dotted8th` / `dotted16th` / `scottish8th` / `scottish16th` | reserved (defined in the engine, not yet emitted) |

- Placed at the **start of the bar's content**, before the stateful `:N`/beats (bar-level, like `\ts`/`\ks`/`\ac`). For a whole-song feel ChordFlow emits it **once on the first bar of each track**.
- **Composes with `{tu}` beat tuplets — not mutually exclusive.** `\tf` only reshapes straight 8th/16th *pairs*; an explicit `:3` triplet beat is already a tuplet (no straight pair to warp), so `\tf` leaves it alone. In one bar, plain-8th beats swing while `:3` beats render as authored — no double-swing.

```
\tf triplet8th :2 (1.5 0.4 1.3) (1.5 0.4 1.3) |   ← whole-song swing on the first bar; plain 8ths play swung
```

ChordFlow emits `\tf` only when the chosen `TripletFeel` ≠ `None` (a straight song emits no directive — byte-identical to the un-swung output).

## Chord names & diagrams

Verified 2026-06-15 against the alphaTab docs ([score-metadata](https://www.alphatab.net/docs/alphatex/score-metadata#chorddiagramsinscore), [Chord model](https://www.alphatab.net/docs/reference/types/model/chord/)) and confirmed in the running app. **`\chordDiagramsInScore` and `\chord` are score-metadata directives — they go in the header, before the lone `.`, NOT inline in the music** (inline `\chord` is silently ignored — names show, diagrams don't). Canonical shape:

```
\chordDiagramsInScore
\chord ("E" 0 0 1 2 2 0)
.
(0.1 0.2 1.3 2.4 2.5 0.6){ch "E"}
```

- **Attach a chord label to a beat:** the `{ch "Name"}` beat effect — e.g. `(1.5 0.4 1.3){ch "Bb7"}`. The name renders above the staff (works on its own, no `\chord` needed). Beat effects combine in one brace group: `{ch "Bb7" tu 3}`.
- **Define a chord diagram (header):** `\chord ("Name" f1 f2 f3 f4 f5 f6)` — exactly six fret values **ordered string 1 (high E) → string 6 (low E)** (cross-checked: notes `0.1 0.2 1.3 2.4 2.5 0.6` ⇒ `0 0 1 2 2 0`). An unplayed string is `x`. One definition per distinct chord, emitted in the metadata header; the body references it by name with `{ch "Name"}`. **A grip up the neck needs a `{firstfret N}` suffix** — `\chord ("D#" 6 8 8 8 6 x) {firstfret 6}` — or alphaTab draws the box from the nut with the dots floating in the air; ChordFlow emits it when the voicing's lowest fret is ≥ 2 (omitted for open/nut grips). (engine-derived-as-app-source)
- **Two diagram-display modes:**
  - **On top** (a chord-diagram list above the score) — shown automatically for any chord that is **defined** (`\chord …`) and **used** (`{ch …}`). There is **no alphaTex directive** for it; visibility is the score stylesheet flag `globalDisplayChordDiagramsOnTop` (default shown), set in JS (`score.stylesheet.globalDisplayChordDiagramsOnTop`) when it needs suppressing.
  - **Over the staff** (inline boxes at each chord) — the `\chordDiagramsInScore` directive: bare = show, `\chordDiagramsInScore false` = hide. The **only** chord-diagram alphaTex directive.
- ⚠️ `\chordDiagramsOnTop` is **not** a valid alphaTex directive (only a settings/stylesheet key) — emitting it breaks the parse. Use the stylesheet flag, not a directive.
- ChordFlow's `RenderOptions` maps: `ShowChordNames`→`{ch}` · `ShowChordDiagramsOverStaff`→`\chordDiagramsInScore` + `\chord` defs · `ShowChordDiagramsOnTop`→`\chord` defs (+ JS stylesheet flag). The directive is omitted entirely when no chord toggle is on (default render byte-identical).

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
| `RenderOptions.ShowChordNames` | `{ch "Name"}` at each chord change + `\chordDiagramsInScore false` |
| `RenderOptions.ShowChordDiagrams` | `\chordDiagramsInScore true` + inline `\chord ("Name" f1…f6)` (once) + `{ch "Name"}` |
| `PickupMeasure` (anacrusis) | `\ac` prefixing the leading pickup bar (both tracks) |
| `Exercise.TripletFeel` (≠ `None`) | `\tf <ident>` on the first bar of each track (whole-song swing) |

## Sources
- https://www.alphatab.net/docs/alphatex/introduction
- https://www.alphatab.net/docs/alphatex/bar-metadata
- Playground (verify ⚠️ items): https://www.alphatab.net/docs/playground
