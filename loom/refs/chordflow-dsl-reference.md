---
type: reference
id: rf_01KTSAQ6990GY3J4CZ7HPVPW6K
title: ChordFlow DSL
status: active
created: "2026-06-10T00:00:00.000Z"
updated: 2026-06-12
version: 3
tags: []
parent_id: null
requires_load: []
slug: chordflow-dsl
description: "End-user guide to ChordFlow's text DSLs: the Progression DSL (Nashville-number notation for key-independent chord progressions — bars, chord splits, qualities, durations) and the Song DSL (arranging progressions into a full piece with definitions, repeats, and modulation)."
---
# ChordFlow DSL

ChordFlow lets you write musical material as short, readable text. Because it uses **scale degrees instead of letter names**, anything you write works in **every key** — pick the key in the app, and ChordFlow spells the chords for you.

> Three DSLs today: the **Progression DSL** (chords in any key), the **Song DSL** (arranging progressions into a piece), and the **Rhythm DSL** (strum/timing patterns on a tick grid — *engine-internal today*; see the last section). Voicings may follow; they'll be documented here.

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
| `ø` or `m7b5` | Half-diminished 7th | `2ø` · `2m7b5` |
| `+` or `aug` | Augmented | `5+` · `5aug` |

The degree is always the **first character** (a single digit 1–7); everything after it is the quality — so `17` is "degree 1, dominant 7th," not "degree 17."

**12-bar blues**, all dominant 7th chords:
```
17 17 17 17 47 47 17 17 57 47 17 57
```

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

---

## Common errors (and what they mean)

ChordFlow validates as it parses and tells you which token is wrong:

- **"missing a scale degree"** — a chord didn't start with a digit 1–7 (e.g. you wrote `m7` with no degree).
- **"degree N outside 1..7"** — degrees are 1 through 7 only.
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
| `key <note>` | set or reset the key (e.g. `key C`, `key Eb`, `key Am`) |
| `mod <spec>` | **modulate** — shift the key from here onward |

```
key C

intro
verse x2
mod V        # up a fifth — everything after this is now in G
chorus
verse
```

- A `key` line **before** the stream sets the **starting key** (defaults to **C major** if omitted).
- A `key` line **inside** the stream is an absolute **reset** — the escape hatch to return home.
- `mod` is **relative and accumulates**: two `mod V` in a row move up two fifths.

### Modulation specs

| Spec | Move |
|------|------|
| `+n` / `-n` | up / down **n** semitones (`+2`, `-3`) |
| `V` | up a fifth (+7) |
| `IV` | up a fourth (+5) |
| `bIII` | up a minor third (+3) — a leading `b`/`#` lowers/raises the degree |
| `vi` | relative minor (+9 **and** switch to minor) — a **lowercase** numeral flips the mode |

### Repeats: `x` vs `@repeat`

`verse x2` plays the verse **twice, as two sections** (rehearsal-style). That is different from a future progression transform `@repeat(2)`, which would make **one** progression twice as long. The Song layer uses **`x` only**; `@repeat` is reserved.

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

### Common errors

- **"plays undefined part"** — a stream line names a part you never defined (add a `NAME =` or `NAME:` line).
- **"defines part … more than once"** — two definitions share a name.
- **"repeat … must be a positive integer"** — `x<n>` needs n ≥ 1 (e.g. `verse x2`).
- **"unknown note letter" / "unknown roman numeral"** — a `key` or `mod` token isn't recognized.

### Notes

- A Song is pure harmony + arrangement. Rhythm, tempo, difficulty, and feel are chosen at **play** time (the same way a progression becomes a practice exercise) — so one Song works across many rhythm settings.
- Modulations never change the underlying progression; they only change the **key it's realized in** from that point onward.

---

## Rhythm DSL

> **Engine-internal today** — the Rhythm DSL is how strum/timing patterns are authored in code and tests; there is no end-user rhythm editor yet. It's documented here because it's a core DSL and the built-in seed patterns are expressed in it.

A **rhythm pattern** is a bar (or several) of pure **timing** — when you strike and how long each strum rings. It carries no chords, no up/down stroke, and no accent; those are layered on at play time. You write a bar as a row of **cells**, each one **subdivision** of a beat.

### Glyphs

| Glyph | Meaning |
|-------|---------|
| `X` | **attack** — strike here (start a new note) |
| `.` | **sustain** — let the current note (or rest) keep ringing through this cell |
| `-` | **rest / mute** — stop the note; silence from here |

**The sustain rule:** a struck note rings until the **next `X` or `-`, or the bar end** — guitar strums ring; they are never automatically staccato. So `X...............` (one strike, then sustains) is a **whole-bar** note, and `X...X...X...X...` is **four quarter notes** (each rings to the next strike).

```text
X...X...X...X...   # four quarters
X.......X.......   # two half notes (beats 1 and 3)
X...-...X...-...   # quarter, cut to silence, quarter, silence
```

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
- **`*`** (a "hold/extend" sugar glyph) is reserved but not implemented — use `.` sustains.
- **Ties and dotted-note tokens** beyond what the sustain rule yields are not emitted; a pattern that would require an explicit tie is rejected.
