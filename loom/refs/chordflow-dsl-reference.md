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
description: "End-user guide to ChordFlow's text DSLs. Currently covers the Progression DSL — a Nashville-number-style notation for writing chord progressions that work in any key (bars, chord splits, qualities, and per-chord durations)."
---
# ChordFlow DSL Guide

ChordFlow lets you write musical material as short, readable text. Because it uses **scale degrees instead of letter names**, anything you write works in **every key** — pick the key in the app, and ChordFlow spells the chords for you.

> Today there is one DSL: the **Progression DSL**. More may follow (rhythm, voicings); they'll be documented here.

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
