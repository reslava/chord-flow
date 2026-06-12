---
type: reference
id: rf_01KTSAQ6990GY3J4CZ7HPVPW6K
title: "ChordFlow DSL"
status: active
created: 2026-06-10
version: 1
tags: []
parent_id: null
child_ids: []
requires_load: []
slug: chordflow-dsl
description: "End-user guide to ChordFlow's text DSLs: the Progression DSL (Nashville-number notation for key-independent chord progressions — bars, chord splits, qualities, durations) and the Song DSL (arranging progressions into a full piece with definitions, repeats, and modulation)."
---
# ChordFlow DSL Guide

ChordFlow lets you write musical material as short, readable text. Because it uses **scale degrees instead of letter names**, anything you write works in **every key** — pick the key in the app, and ChordFlow spells the chords for you.

> Two DSLs today: the **Progression DSL** (chords in any key) and the **Song DSL** (arranging progressions into a piece). More may follow (rhythm, voicings); they'll be documented here.

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
