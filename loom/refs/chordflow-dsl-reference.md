---
type: reference
id: rf_01KTSAQ6990GY3J4CZ7HPVPW6K
title: ChordFlow DSL
status: active
created: 2026-06-10
updated: 2026-07-08
version: 29
tags: []
parent_id: null
requires_load: []
slug: chordflow-dsl
description: "End-user guide to ChordFlow's text DSLs: the Progression DSL (Nashville-number notation for key-independent chord progressions — bars, chord splits, qualities, durations) and the Song DSL (arranging progressions into a full piece with definitions, repeats, and modulation)."
---
# ChordFlow DSL

ChordFlow lets you write musical material as short, readable text. Because it uses **scale degrees instead of letter names**, anything you write works in **every key** — pick the key in the app, and ChordFlow spells the chords for you.

> Four DSLs today: the **Progression DSL** (chords in any key), the **Song DSL** (arranging progressions into a piece), the **Rhythm DSL** (strum/timing patterns on a tick grid), and the **Voicing DSL** (authored, movable chord shapes). The last two are *engine-internal today* — no end-user editor yet; see the final sections.

---

## Progression DSL

A progression is a sequence of **bars**. Each bar holds one or more **chords**, written by their **scale degree** (1–7) instead of a letter — so `1 4 5` means "the I, IV, and V chords of whatever key you choose."

### The two separators

| Symbol | Meaning |
|--------|---------|
| space | **next bar** |
| `_` (underscore) | **next chord in the same bar** |

```
1 4 1 5
```
Four bars, one chord each: I, IV, I, V.

```
1_4 5
```
Two bars: the first splits between I and IV; the second is all V.

### Chord qualities

By default a degree is a **major** chord. Add a suffix to change it:

| Suffix | Quality | Example |
|--------|---------|---------|
| *(none)* | Major | `1` |
| `-` or `m` | Minor | `6-` · `6m` |
| `7` | Dominant 7th | `5 7` |
| `-7` or `m7` | Minor 7th | `2-7` · `2m7` |
| `maj7` or `^7` | Major 7th | `1maj7` · `1^7` |
| `°` or `dim` | Diminished | `7°` · `7dim` |
| `°7` or `dim7` | Diminished 7th | `7°7` · `7dim7` |
| `ø` or `m7b5` | Half-diminished 7th | `2ø` · `2m7b5` |
| `+` or `aug` | Augmented | `5+` · `5aug` |

The degree is a **single digit 1–7** — optionally preceded by one accidental (see below) — and everything after it is the quality, so `17` is "degree 1, dominant 7th," not "degree 17."

**12-bar blues**, all dominant 7th chords:
```
17 17 17 17 47 47 17 17 57 47 17 57
```

### Chromatic degrees (`#` / `b`)

A degree may carry **one** leading accidental to raise (`#`) or lower (`b`) its root by a semitone — for the passing and substitute chords that live between the diatonic degrees:

| Token | Meaning | In F |
|-------|---------|------|
| `#4dim7` | `#IVdim7` — the chromatic passing diminished chord (e.g. blues bar 6) | `Bdim7` |
| `b27` | `bII7` — the tritone substitute for V7 | `Gb7` |

The accidental **combines** with whatever the degree already is in the key: in F the 4th degree is already B♭, so `#4` raises it to **B natural**. Spelling follows the **written degree**, not the key, and never collapses enharmonically — `b4` in C is `Fb` (not E), `#7` in C is `B#` (not C). Only **one** accidental is allowed in the input; double accidentals (`##4`, `#b4`) are rejected.

### Chord durations within a bar

You have two ways to share a bar between chords. **Pick one per bar — don't mix them in the same bar.**

**1. Even split (the easy default).** Write the chords and they divide the bar equally:
```
1_4
```
Two chords → half a bar each. In 4/4 you can evenly split a bar into **1, 2, or 4** chords. (Three even chords don't line up to quarter notes — for that, use explicit slots.)

**2. Explicit slots (`:n`).** Give every chord in the bar a slot count — how many **quarter-note beats** it lasts. The counts must add up to the bar (4 in 4/4):
```
1:2_4:1_5:1
```
I for two beats, IV for one, V for one. Use this for uneven layouts like three chords in a bar (`1:2_4:1_5:1`).

Rule of thumb: **no `:n` anywhere in a bar = even split; `:n` on every chord = explicit.** Mixing the two inside one bar is an error.

---

## Worked examples

| Progression | DSL |
|-------------|-----|
| I–IV–V (three bars) | `1 4 5` |
| 12-bar blues (dominant 7ths) | `17 17 17 17 47 47 17 17 57 47 17 57` |
| ii–V–I (jazz) | `2-7 57 1maj7` |
| 50s doo-wop (I–vi–IV–V) | `1 6- 4 5` |
| Two chords per bar | `1_5 6-_4` |
| Uneven bar (I 2 beats, IV + V 1 each) | `1:2_4:1_5:1` |
| Chromatic passing dim7 (blues bar 6) | `#4dim7` |
| Tritone sub for V7 | `b27` |

---

## Common errors (and what they mean)

ChordFlow validates as it parses and tells you which token is wrong:

- **"missing a scale degree"** — a chord didn't start with a digit 1–7 (e.g. you wrote `m7` with no degree, or a lone `#`/`b` with no degree after it).
- **"degree N outside 1..7"** — degrees are 1 through 7 only (the accidental, if any, attaches to a valid degree — `#8` is still out of range).
- **"unknown quality suffix"** — the suffix after the degree isn't in the table above.
- **"cannot be split evenly into N … chords"** — an even-split bar that doesn't divide into quarter notes; switch that bar to explicit `:n` slots.
- **"`:slots` sum to X, expected 4"** — the explicit slots in a bar don't add up to the bar length.
- **"mixes explicit `:slots` with even-split chords"** — one bar used both modes; pick one.

---

## Notes

- Degrees are **key-independent** — `1 4 5` is C–F–G in C, or G–C–D in G. Choose the key in the app.
- Whitespace is flexible: extra spaces between bars are fine.
- Time signature affects how slots add up (4/4 → 4 beats per bar); the default exercises are 4/4.

---

## Song DSL

A **Song** arranges progressions into a full piece — intro, verses, choruses — with repeats and key changes. A Song never contains chords directly; it **references progressions** and says how to order, repeat, and modulate them. Harmony stays in the progression (the DSL above); the Song only composes.

A Song has two parts: **definitions** (name your parts) and the **arrangement stream** (play them in order).

### Definitions

Two ways to name a part:

| Form | Meaning |
|------|---------|
| `NAME = <progression DSL>` | **inline** — define a progression right here, using the Progression DSL above |
| `NAME: <stored-id>` | **reference** — point at a saved progression by its id |

```
intro = 17 47 17 17        # inline: a 4-bar progression
verse: 12bar_blues         # reference: the saved 12-bar blues
chorus = 67 27 57 17
```

Names are yours to choose (`A`, `verse`, `chorus`). A local definition **shadows** a saved progression of the same name. `#` starts a comment to the end of the line.

### The arrangement stream

After the definitions, list the parts in playing order — one instruction per line:

| Line | Meaning |
|------|---------|
| `NAME` | play that part once |
| `NAME x<n>` | play it **n** times (`verse x2`) |
| `NAME @op(args)` | apply a **progression transform** to that play (e.g. `verse @take(4)`) — see *Progression transforms* below |
| `key <note>` | set or reset the key (e.g. `key C`, `key Eb`, `key Am`) |
| `mod <spec>` | **modulate** — shift the key from here onward |
| `feel <token>` | the song's **default triplet feel** (swing) — `feel none`, `feel triplet8th`, `feel triplet16th` |
| `tempo <bpm>` | the song's **default tempo** (BPM, 40–240) — `tempo 120`; pre-selects the Tempo control when you pick the song |

```
key C
feel triplet8th   # a swing tune — the Feel control pre-selects Triplet8th when you pick this song
tempo 120         # the Tempo control pre-selects 120 BPM when you pick this song

intro
verse x2
mod V        # up a fifth — everything after this is now in G
chorus
verse
```

- A `key` line **before** the stream sets the **starting key** (defaults to **C major** if omitted).
- A `key` line **inside** the stream is an absolute **reset** — the escape hatch to return home.
- `mod` is **relative and accumulates**: two `mod V` in a row move up two fifths.
- `feel` is a **whole-song default** (position-independent, at most once) — it only **pre-selects** the play-time Feel control when you pick the song; you can still change the feel in the transport, and the swing itself still happens at play time (it never changes the notated rhythm). Omitting `feel` means "no preference" (the control stays straight); an explicit `feel none` says "this is a straight tune." It is a **Song-only** directive — progressions are pure chords/bars and carry no `feel` (or `key`). Uses a space keyword (`feel triplet8th`), **not** a colon (`feel:` is reserved for a stored-progression reference).
- `tempo` is the exact **peer of `feel`**: a whole-song default (position-independent, at most once, BPM in 40–240) that only **pre-selects** the play-time Tempo control — you can still change the tempo in the transport. Omitting `tempo` means "no preference" (the control seeds the ChordFlow default **80**). Also **Song-only**, space-keyword (`tempo 120`).

### Modulation specs

| Spec | Move |
|------|------|
| `+n` / `-n` | up / down **n** semitones (`+2`, `-3`) |
| `V` | up a fifth (+7) |
| `IV` | up a fourth (+5) |
| `bIII` | up a minor third (+3) — a leading `b`/`#` lowers/raises the degree |
| `vi` | relative minor (+9 **and** switch to minor) — a **lowercase** numeral flips the mode |

### Progression transforms — `@op(args)`

A play can **rewrite** its progression before it's realized, with one or more `@op(args)` transforms on the play line:

```
key F
head = 17 47 17 17 47 47 17 67 2-7 57 17 57   # a 12-bar jazz blues
head x2          # play the full head twice
head @take(4)    # then drill just the first 4 bars
```

| Transform | Effect |
|-----------|--------|
| `@take(n)` | keep only the **first n bars** of the progression (drill the head / a section) |

- **Composition:** list several and they apply **left-to-right** — `@take(8) @take(4)` takes 8 then 4. They are **not commutative** (the reverse can even be out of range).
- **With `x<n>`:** a play may carry both, in **either order** (`head @take(4) x2` ≡ `head x2 @take(4)`): the transform rewrites the progression, then the section repeats.
- `@take(n)` requires `1 ≤ n ≤ the progression's bar count` — out of range is an error.

### Repeats: `x` vs `@repeat`

`verse x2` plays the verse **twice, as two sections** (rehearsal-style). That is different from a progression transform `@repeat(2)` — which would make **one** progression twice as long — so `@repeat` stays **reserved/unimplemented**: it would only duplicate what `x<n>` already does. Use `x` for repetition.

### A full example

```
genre: Blues
subgenre: Shuffle
tags: [12-bar, demo]
intro = 17 47 17 17
verse: 12bar_blues
chorus = 67 27 57 17

intro
verse x2
mod V
chorus
verse
```

Intro, two verses (the 12-bar blues), then up a fifth for the chorus and a final verse — all from one reusable definition. The optional `genre:`/`subgenre:`/`tags:` header at the top is catalog metadata for filtering; it isn't part of the arrangement.

### Pinning voicings — per-chord `{…}` and the `voice` default

By default ChordFlow **derives** the fretboard grip for each chord (lowest, most-common shape). You can override that and **pin a specific voicing** — a grip you fingered yourself, or a listed one — in two places. Both are **Song-level** (harmony stays pure — a stored progression on its own carries no voicings).

**The voicing value** (the same in both places) is either a **literal grip** or a **reference**:

| Value | Meaning |
|-------|---------|
| `8 x 7 9 8 x` | a **literal grip** — six frets low-E→high-E (`x` = muted, `0` = open). Optionally written `c: 8 x 7 9 8 x`. |
| `8 x 7 9 8 x root:6` | a grip with an explicit **root anchor** — string 6 sounds the root. Needed only when the root isn't the lowest sounded string. |
| `x 3 2 3 1 x root:6@8` | a **rootless** grip — the root (low E) isn't played; `root:6@8` names where it *would* be (string 6, fret 8) so the shape still transposes. |
| `u: C6` | a **reference** to your **user** voicing with id `C6`. |
| `a: auto:shell:dom7:E` | a reference to an **engine** voicing (the `auto:…` catalog id). |
| `swing: C6` | a reference to voicing `C6` from the **package** `swing`. |

A grip is a **movable shape**: you write it once (as it looks at that chord) and ChordFlow slides it to the actual root when you change key or modulate — so a pinned voicing never breaks on transposition. A missing/filtered reference **fails loud** when the song is realized (never a silent fallback).

**1. Per-chord `{…}` — pin one chord.** Attach a `{value}` to a chord inside an **inline** progression:

```
head = 17 {8 x 7 9 8 x} 47 17 67 2-7 {u: C6} 57 17
```

The annotation binds to the chord just before it (a space is optional: `17{…}` works too). It overrides **only that occurrence** — other `17`s keep the automatic fill.

**2. `voice <selector> = <value>` — a whole-song default.** A definition-section line (a peer of `key`/`feel`/`tempo`) that pins **every** matching chord:

```
voice *7   = 3 3 2 3 1 x      # every dominant-7 chord, transposed to each root
voice 17   = 8 x 7 9 8 x      # …but the I7 specifically uses this
voice #4dim7 = 8 x 7 8 7 x    # the chromatic passing dim7
voice *m7  = u: my-m7-shell   # every minor-7 chord uses a user voicing
```

- `*<quality>` matches **any degree** of that quality (`*7`, `*m7`, `*maj7`, bare `*` = every major triad).
- `<degree><quality>` (a chord symbol — `17`, `#4dim7`, `2-7`) matches just that degree.

**Which one wins** (most specific first): a per-chord `{…}` › a degree `voice 17` › a quality `voice *7` › the automatic fill.

### Common errors

- **"plays undefined part"** — a stream line names a part you never defined (add a `NAME =` or `NAME:` line).
- **"defines part … more than once"** — two definitions share a name.
- **"repeat … must be a positive integer"** — `x<n>` needs n ≥ 1 (e.g. `verse x2`).
- **"unknown note letter" / "unknown roman numeral"** — a `key` or `mod` token isn't recognized.
- **"Unknown progression transform"** — an `@op` name isn't a known transform (only `@take` today).
- **"must look like @name(args)"** / **"requires a positive integer argument"** — a malformed transform token (missing parens) or a bad `@take` argument.
- **"more than one repeat token"** — a play line has two `x<n>` repeats.
- **"voicing annotations are a Song-level concern"** — a `{…}` appeared in a **stored** progression; pin voicings inside a Song (an inline part or a `voice` line) instead.
- **"defines voicing for … more than once"** — two `voice` lines share a selector.
- **"could not be resolved"** — a `{…}`/`voice` reference names a voicing that doesn't exist in that source (or a grip that doesn't fit the neck) — a fail-loud realization error.

### Notes

- A Song is pure harmony + arrangement. Rhythm, tempo, difficulty, and **triplet feel (swing)** are chosen at **play** time (the same way a progression becomes a practice exercise) — so one Song works across many rhythm settings. A Song *may* carry `feel`/`tempo` directives, but only as **play-time-control default seeds** (they pre-select the transport control, overridable there), never as content: swing is still a play-time render setting (it becomes alphaTab's `\tf`) and tempo a play-time speed, neither written into the realized notes. Progressions and Rhythms carry **no** `feel`/`tempo`/`key` at all.
- Modulations never change the underlying progression; they only change the **key it's realized in** from that point onward.

---

## Rhythm DSL

> **Engine-internal today** — the Rhythm DSL is how strum/timing patterns are authored in code and tests; there is no end-user rhythm editor yet. It's documented here because it's a core DSL and the built-in seed patterns are expressed in it.

A **rhythm pattern** is a bar (or several) of pure **timing** — when you strike and how long each strum rings. It carries no chords, no up/down stroke, and no accent; those are layered on at play time. You write a bar as a row of **cells**, each one **subdivision** of a beat.

### Glyphs

| Glyph | Meaning |
|-------|---------|
| `X` | **attack** — start a note; it lasts itself plus each following `.` |
| `.` | **sustain** — extend the currently **sounding** note by one cell (illegal where nothing sounds — at a bar start, after `-`, or after `_`: `.` means *sound*) |
| `-` | **rest** — one cell of silence (repeat for longer rests) |
| `_` | **tie** — a tied note: like `X` it occupies cells and extends with `.`, but ties to the previous note (no re-attack). A **leading** `_` ties the bar's first note into the previous bar |

A note lasts **exactly its drawn cells** — its `X` plus the `.`s after it — and silence is written with `-` (never `.`). So `X...............` is a **whole-bar** note, `X...X...X...X...` is **four quarter notes**, and silence is spelled out:

```text
X...X...X...X...   # four quarters
X.......X.......   # two half notes (beats 1 and 3)
X...----X...----   # quarter, quarter rest, quarter, quarter rest
```

**Durations are notated, not sustained.** Each note-group must be **one notatable value** — a base value (whole/half/quarter/8th/16th) or a single-**dotted** value (1.5×). A duration that isn't a single value (a syncopation, a double dot) is written by **tying** with `_`:

```text
:2 X..X----        # Charleston: dotted quarter + eighth + rest
:2 X.....X.        # dotted half + quarter
X..._...X...X...   # a quarter tied to the next quarter, then two quarters (_ opens the tied note)
X...............|_...------------   # a chord rung across the barline (leading _ ties into bar 1)
```

(Guitar *sustain* — letting strings ring past their written value — is a play-time "let ring" setting, not part of the written rhythm.)

### Subdivision — `:n`

`n` = **cells per beat** (default **4** = sixteenths); it must divide a beat evenly. Cell length = one beat ÷ n.

| `:n` | Subdivision | Cells / 4-4 bar |
|-----:|-------------|----------------:|
| `:1` | quarters | 4 |
| `:2` | eighths | 8 |
| `:3` | eighth-triplets | 12 |
| `:4` | sixteenths (default) | 16 |
| `:6` | 16th-triplets | 24 |

A **leading** `:n` sets the whole row's subdivision:

```text
:3 XXX XXX XXX XXX   # twelve eighth-note triplets
```

### Subdivision runs (mixing straight and triplet beats)

A bar is a sequence of **runs** separated by spaces. A run's cells split into consecutive beats **by count**, so a same-`n` run may omit inner spaces — `X...X...X...X...` and `X... X... X... X...` are identical. A space is only needed to **switch subdivision** or attach a per-run `:n` suffix — which lets straight and triplet beats live in one bar:

```text
XXX:3 X... X.X:3 X...
#  └ beat 1 triplet · beat 2 straight 16ths · beat 3 triplet (with a sustain) · beat 4 straight
```

Each run's cell count must be a whole multiple of its `n`, and the per-beat counts must sum to a full bar.

### Bars and pickups

- `|` separates **bars**: `X...X...X...X... | X.......X.......` is a two-bar pattern.
- A leading `PICKUP: <grid> |` is a short **anacrusis** (lead-in) before the first bar — it may be **shorter than a bar** and need not fill whole beats:

```text
PICKUP: ...........X | X...X...X...X...
#       └ the last sixteenth rings into bar 1
```

Newlines are insignificant (they collapse to spaces), so a multi-bar pattern can be laid out over several lines as long as the `|`s are present.

### Triplets

Triplet beats (`:3`, `:6`) render as proper tuplets — the notation shows the `3` bracket. A sustained triplet note is written with `.` like any other (`X.X:3` = a note held across two of the three triplet cells) and renders as a single longer tuplet note, not as tied cells.

### What you can't do yet

- **No accent or stroke** inside the grid — those are overlays applied at play time (a pattern is timing only).
- **No feel / swing** in the grid — triplet feel is a **play-time** setting (it becomes alphaTab's `\tf`), never written into a pattern; a straight pattern *plays* swung when you choose a swing feel. (For an explicit triplet *figure* — three attacks, or an attack on the middle slot — use a `:3` triplet beat; the swing pair `:3 X.X` is what a feel produces for you.)
- **`*`** (a "hold/extend" sugar glyph) is reserved but not implemented — use `.` sustains.
- **Double-dots** aren't auto-emitted (single dot only) — write a double-dotted value by tying with `_`.
- A tie **over a chord change** (within a bar or across the barline) **holds the previous chord** — rhythm wins over harmony; you keep ringing what you struck, whatever the progression does underneath.

---

## Voicing DSL

> **Engine-internal today** — like the Rhythm DSL, the Voicing DSL is how chord shapes are authored in code and content packs; the end-user voicing editor is still to come. Documented here because it's a core DSL.

A **voicing** is one authored fingering of a chord. You write it **once at a canonical anchor** (convention: **C**) and ChordFlow makes it **movable** — sliding the shape to any of the 12 roots for you. There's no separate "open" vs "barre" form: open strings simply become a barre when a shape moves up the neck (and a barre opens up moving down).

### The line

```text
voicing <Chord>  shape:<C|A|G|E|D>  root:<6..1>  [anchor:<i|m|r|p>]  frets: <s6 s5 s4 s3 s2 s1>
```

| Field | Meaning |
|-------|---------|
| `<Chord>` | the **anchor chord** — a note name + quality suffix (`Cmaj`, `C7`, `Ebm7`, …). Convention is C; any anchor is accepted and normalized to C. The **quality** is what the app matches; the root pitch is the transpose anchor. |
| `shape:` | the **CAGED family** (`C`, `A`, `G`, `E`, `D`) — diagram label + the order shapes are offered. |
| `root:` | the string (6 = low E … 1 = high E) that sounds the root. |
| `anchor:` | **optional** — the finger that anchors the shape's root: `i` index · `m` middle · `r` ring · `p` pinky. The CAGED derivation engine *derives* this (root's rank in the grip), and the token is the golden oracle that checks the derivation. Omit it on chords whose fingering is idiosyncratic (literal open-position chords). |
| `frets:` | six fret numbers, **low-E → high-E** (strings 6 → 1): `x` = muted, `0` = open, a number = that fret. |

Quality suffixes are the same as the Progression DSL (`maj`/`m`/`7`/`m7`/`maj7`/`m7b5`/`dim`/`dim7`/`aug`, …; `dim` = diminished triad, `dim7` = symmetric diminished 7th). A trailing `# comment` is ignored.

### Examples

| Voicing | DSL |
|---------|-----|
| Open C (C-shape) | `voicing Cmaj shape:C root:5 frets: x 3 2 0 1 0` |
| E-shape C major | `voicing Cmaj shape:E root:6 frets: 8 10 10 9 8 8` |
| G-shape C major | `voicing Cmaj shape:G root:6 frets: 8 7 5 5 5 8` |
| A-shape C minor | `voicing Cmin shape:A root:5 frets: x 3 1 0 1 3` |

Because shapes are movable, the open-C line above also gives D major (a barre) at the 2nd fret, E major at the 4th, and so on — you author one shape, not twelve.

### How it's used

ChordFlow keeps a **library** of authored voicings. When it needs a chord it offers the stored shapes of that quality, **ranked** by neck position (lowest first) then by how common the CAGED shape is, and falls back to a built-in generated shape if you haven't authored one. Authored voicings **shadow** the generated default.

### Canonical storage

Whatever anchor you type, the voicing is stored **normalized to C** (its lowest non-negative placement), so the same shape written as `Gmaj` or `Dmaj` collapses to one canonical record — no duplicate rows per key.

### Common errors

- **"missing the 'frets:' clause"** / **"needs 6 fret values"** — `frets:` must list exactly six entries, low-E→high-E.
- **"unknown quality suffix"** — the suffix after the note name isn't a known quality.
- **"invalid shape"** — `shape:` must be one of `C A G E D`.
- **"root string … outside 1..6"** — `root:` is a string number 1–6.

---

## Content packs

> **Engine-internal today** — there is no in-app pack authoring/import UI yet. A
> pack is a **folder of plain text** you (or, later, a content seller) drop in;
> the engine imports it at startup. Documented here because the file format is a
> stable public contract.

A **content pack** is the unit ChordFlow ships and (later) sells content in — the
open-core model: the engine + a free **default pack** are open; curated genre /
song / voicing packs are the optional paid layer. A pack is **data only** — it
adds rows, never code; importing one needs zero engine change.

### Bundle layout

```
my-pack/
  manifest.json
  progressions/*.dsl     # one Progression DSL definition per file
  songs/*.dsl            # one Song DSL definition per file
  rhythms/*.dsl          # one Rhythm DSL definition per file
  voicings/*.dsl         # one Voicing DSL definition per file
```

Each kind folder is optional — a pack carries any mix (a real genre pack ships
progressions + songs + rhythms + voicings together). A **definition's kind comes
from the folder it sits in**, not from anything inside the file.

### `manifest.json`

```json
{
  "id": "blues-essentials",
  "name": "Blues Essentials",
  "version": "1.0.0",
  "kind": "content",
  "provenance": "ChordFlow",
  "requires": []
}
```

| Field | Meaning |
|-------|---------|
| `id` | stable pack id — stamped onto every imported definition as its source pack |
| `name` | display name |
| `version` | pack version (semver string; dependency resolution is not enforced yet) |
| `kind` | coarse **pack-type** discriminator — `content` today (future: `soundfont`, `theme`, …) |
| `provenance` | free-text author/source label (e.g. `ChordFlow`) |
| `requires` | other pack ids this one depends on — recorded, not yet resolved |

### How each `.dsl` file carries its identity

Every definition needs a stable **Id** and a display **Name**. In a pack:

- **The filename stem is the `Id`** — `progressions/12bar_blues.dsl` → id
  `12bar_blues`. Human-navigable, and it makes cross-references readable: a
  song's `verse: 12bar_blues` points at a file you can find on disk.
- **An optional leading `name:` line** is the display name. Omit it and the Id is
  title-cased into one (`12bar_blues` → "12bar Blues").
- `genre:` / `subgenre:` / `tags:` follow as usual (the catalog header); then the
  entity's own grammar is the body. Rhythm files carry no catalog metadata.

```
progressions/12bar_blues.dsl
─────────────────────────────
name: 12-Bar Blues
genre: Blues
tags: [12-bar]
17 17 17 17 47 47 17 17 57 47 17 57
```

### Import

- **Idempotent by Id.** Importing a pack upserts each definition **by its Id** —
  re-importing the same pack changes nothing; an updated definition replaces the
  prior row of the same Id. The same mechanism that seeds the built-in defaults.
- **Provenance + shadowing.** Imported definitions are stamped `Pack` with the
  manifest's id. A locally-edited copy of the same Id still wins
  (`UserDefined > Pack > BuiltIn`), non-destructively — remove your local edit and
  the pack copy takes over again.
- **Fail-loud references.** A pack's song may reference a progression (by id) from
  the same pack, another pack, or the built-ins. A reference to a definition that
  exists nowhere fails loudly when the song is realized — never a silently dropped
  section.

### The default pack

The free starter content ships as the **default pack** and is imported on first
run through this same path — no special-case seeding code. Its curated content
(the starter progressions, songs, rhythms, and authored voicings) is maintained
as a normal bundle.
