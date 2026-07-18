<img src="../images/icon.png" width="96" align="right" alt="ChordFlow app icon">

# ChordFlow — DSL Guide

ChordFlow lets you write your own musical material as short, readable **text**. This
guide teaches the four small languages you'll use, one example at a time:

1. **[Progressions](#1-progressions--chords-in-any-key)** — chords, in any key
2. **[Rhythms](#2-rhythms--strum--timing-patterns)** — strum / timing patterns
3. **[Voicings](#3-voicings--movable-chord-shapes)** — movable chord shapes
4. **[Songs](#4-songs--arrange-progressions-into-a-piece)** — arrange progressions into a full piece

> **New to the app?** Read the **[User Guide](user-guide.md)** first — it covers
> installing, building an exercise, and where the **Content** tab (the editor for all of
> this) lives. This guide is about *what you type* once you're there.

The one idea behind all of it: you write music with **scale degrees, not letter names**,
so everything you author works in **every key** — you pick the key later, in the app, and
ChordFlow spells the chords for you.

---

## 1. Progressions — chords in any key

A **progression** is a sequence of **bars**. Each bar holds one or more **chords**, written
by their **scale degree** `1`–`7` instead of a letter. So `1 4 5` means "the I, IV, and V
chords of whatever key you choose" — C–F–G in C, or G–C–D in G.

### Bars and splits

Two separators are all you need:

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
Two bars: the first splits between I and IV, the second is all V.

### Chord qualities

A bare degree is a **major** chord. Add a suffix to change its quality:

| Suffix | Quality | Example |
|--------|---------|---------|
| *(none)* | Major | `1` |
| `-` or `m` | Minor | `6-` · `6m` |
| `7` | Dominant 7th | `57` |
| `-7` or `m7` | Minor 7th | `2-7` · `2m7` |
| `maj7` or `^7` | Major 7th | `1maj7` · `1^7` |
| `°` or `dim` | Diminished | `7°` · `7dim` |
| `°7` or `dim7` | Diminished 7th | `7°7` · `7dim7` |
| `ø` or `m7b5` | Half-diminished 7th | `2ø` · `2m7b5` |
| `+` or `aug` | Augmented | `5+` · `5aug` |

The degree is a **single digit** `1`–`7`; everything after it is the quality. So `17` is
"degree 1, dominant 7th" — **not** "degree 17".

A **12-bar blues** (all dominant chords) is just:
```
17 17 17 17 47 47 17 17 57 47 17 57
```
and a jazz **ii–V–I**:
```
2-7 57 1maj7
```

### Chromatic degrees (`#` / `b`)

A degree may carry **one** leading accidental to raise (`#`) or lower (`b`) its root by a
semitone — for the passing and substitute chords that live between the diatonic degrees:

| Token | Meaning | In C |
|-------|---------|------|
| `#4dim7` | `#IVdim7` — the chromatic passing diminished chord | `F#dim7` |
| `b27` | `bII7` — the tritone substitute for V7 | `Db7` |

Only one accidental is allowed (`##4` is rejected), and the spelling follows the **written
degree**, never collapsing enharmonically.

### Sharing a bar between chords

You have two ways to give chords different lengths inside a bar — **pick one per bar**:

**1. Even split** (the easy default) — just write the chords and they divide the bar equally:
```
1_4
```
Two chords → half a bar each. In 4/4 a bar splits evenly into **1, 2, or 4** chords.

**2. Explicit slots** `:n` — give every chord a slot count in **quarter-note beats**; they
must add up to the bar (4 in 4/4):
```
1:2_4:1_5:1
```
I for two beats, IV for one, V for one — the way to fit an uneven three-chord bar.

> Rule of thumb: **no `:n` in a bar = even split; `:n` on every chord = explicit.** Mixing
> the two inside one bar is an error.

### Minor progressions

Minor progressions are authored **tonic-relative** — the minor tonic is `1-`, and the
natural-minor scale reads with **bare** degrees: a natural-minor i–ii°–III–iv–v–VI–VII is
```
1- 2° 3 4- 5- 6 7
```
The raised tones of harmonic/melodic minor are just accidentals: a dominant V is `5`/`57`,
the vii°7 is `#7dim7`. You think in the minor scale; the **song** later picks the actual
minor key (`key Am`). In the Content editor you set this with a **major / minor** control
rather than typing a header line.

### A few worked progressions

| Progression | DSL |
|-------------|-----|
| I–IV–V | `1 4 5` |
| 50s doo-wop (I–vi–IV–V) | `1 6- 4 5` |
| ii–V–I (jazz) | `2-7 57 1maj7` |
| Minor ii–V–i | `2ø 57 1-` |
| Andalusian cadence (minor) | `1- 7 6 5` |
| Uneven bar (I 2 beats, IV + V 1 each) | `1:2_4:1_5:1` |

---

## 2. Rhythms — strum / timing patterns

A **rhythm pattern** is a bar (or several) of pure **timing** — when you strike and how long
each strum rings. It carries no chords: those are supplied by the progression, and the up/down
stroke, accent, and swing are added at play time. You write a bar as a row of **cells**, each
one **subdivision** of a beat.

### The four glyphs

| Glyph | Meaning |
|-------|---------|
| `X` | **attack** — start a note; it lasts itself plus each following `.` |
| `.` | **sustain** — extend the currently sounding note by one cell |
| `-` | **rest** — one cell of silence |
| `_` | **tie** — hold the previous note without re-attacking (a leading `_` ties into the previous bar) |

A note lasts **exactly its drawn cells**, and silence is always written with `-` (never `.`):

```text
X...X...X...X...   # four quarter notes
X.......X.......   # two half notes (beats 1 and 3)
X...----X...----   # quarter, quarter-rest, quarter, quarter-rest
```

Each note-group must be **one notatable value** (whole / half / quarter / 8th / 16th, or a
single-**dotted** value). For anything else — a syncopation or a double dot — **tie** two
values with `_`:

```text
X..._...X...X...   # a quarter tied to the next quarter, then two quarters
```

### Subdivision — `:n`

`n` is the number of **cells per beat** (default `4` = sixteenths). A leading `:n` sets the
row:

| `:n` | Subdivision | Cells per 4/4 bar |
|-----:|-------------|------------------:|
| `:1` | quarters | 4 |
| `:2` | eighths | 8 |
| `:3` | eighth-triplets | 12 |
| `:4` | sixteenths (default) | 16 |

```text
:2 X..X----          # Charleston: dotted-quarter + eighth + rest
:1 X X X X           # four plain quarters
```

### Bars and pickups

- `|` separates **bars**: `X...X...X...X... | X.......X.......` is a two-bar pattern.
- A leading `PICKUP: <grid> |` is a short **anacrusis** (lead-in) before the first bar, and
  may be shorter than a full bar:

```text
PICKUP: ...........X | X...X...X...X...
#       └ the last sixteenth rings into bar 1
```

> **Feel is not in the grid.** Swing (triplet feel) is a **play-time** setting — a straight
> pattern *plays* swung when you choose a swing feel. For an explicit triplet *figure*, use a
> `:3` triplet beat.

---

## 3. Voicings — movable chord shapes

A **voicing** is one authored fingering of a chord. You write it **once at a canonical anchor**
(convention: **C**) and ChordFlow makes it **movable** — sliding the shape to any of the 12
roots for you. There's no separate "open" vs "barre" form: open strings simply become a barre
as the shape moves up the neck.

### The line

```text
voicing <Chord>  shape:<C|A|G|E|D>  root:<6..1>  [anchor:<i|m|r|p>]  frets: <s6 s5 s4 s3 s2 s1>
```

| Field | Meaning |
|-------|---------|
| `<Chord>` | the **anchor chord** — a note name + quality (`Cmaj`, `C7`, `Ebm7`, …). The **quality** is what the app matches; the root is the transpose anchor. |
| `shape:` | the **CAGED family** (`C`, `A`, `G`, `E`, `D`) — the diagram label and the order shapes are offered. |
| `root:` | the string (6 = low E … 1 = high E) that sounds the root. |
| `anchor:` | *optional* — the finger on the root (`i` index · `m` middle · `r` ring · `p` pinky). |
| `frets:` | six fret numbers, **low-E → high-E**: `x` = muted, `0` = open, a number = that fret. |

### Examples

| Voicing | DSL |
|---------|-----|
| Open C (C-shape) | `voicing Cmaj shape:C root:5 frets: x 3 2 0 1 0` |
| E-shape C major | `voicing Cmaj shape:E root:6 frets: 8 10 10 9 8 8` |
| A-shape C minor | `voicing Cmin shape:A root:5 frets: x 3 1 0 1 3` |

Because shapes are movable, that open-C line also gives D major (a barre) at the 2nd fret, E
major at the 4th, and so on — you author **one shape, not twelve**.

> ChordFlow already **derives** a full library of grips from theory, so you only author a
> voicing when you want a specific fingering that differs from the engine's pick. Authored
> voicings shadow the generated default.

---

## 4. Songs — arrange progressions into a piece

A **Song** arranges progressions into a full piece — intro, verses, choruses — with repeats
and key changes. A Song never contains chords directly: it **references progressions** and says
how to order, repeat, and modulate them. It has two parts — **definitions** (name your parts)
and the **arrangement stream** (play them in order).

### Definitions

| Form | Meaning |
|------|---------|
| `NAME = <progression DSL>` | **inline** — define a progression right here |
| `NAME: <stored-id>` | **reference** — point at a saved progression by its id |

```
intro = 17 47 17 17        # inline: a 4-bar progression
verse: 12bar_blues         # reference: the saved 12-bar blues
chorus = 67 27 57 17
```

Names are yours to choose. A `#` starts a comment to the end of the line.

### The arrangement stream

After the definitions, list the parts in playing order — one instruction per line:

| Line | Meaning |
|------|---------|
| `NAME` | play that part once |
| `NAME x<n>` | play it **n** times (`verse x2`) |
| `NAME @take(n)` | keep only the first **n** bars of that play (drill a head) |
| `key <note>` | set or reset the key (`key C`, `key Eb`, `key Am`) |
| `mod <spec>` | **modulate** — shift the key from here onward |
| `feel <token>` | the song's default **swing** — `feel none` / `feel triplet8th` / `feel triplet16th` |
| `tempo <bpm>` | the song's default **tempo** (40–240) — `tempo 120` |
| `capo <fret>` | the song's **capo** fret (1–12) — shown on the chord sheet, does **not** transpose the harmony |

```
key C
tempo 120
feel triplet8th

intro
verse x2
mod V        # up a fifth — everything after this is now in G
chorus
verse
```

- A `key` line **before** the stream sets the **starting key** (defaults to C major).
- A `key` line **inside** the stream is an absolute **reset** — the escape hatch home.
- `mod` is **relative and accumulates** — two `mod V` in a row move up two fifths.

#### `feel`, `tempo`, `capo` — the play-time seeds

These three are **whole-song defaults** (each used at most once, position-independent):

- `feel` and `tempo` only **pre-select** the transport controls when you pick the song — you
  can still change them at play time. Omitting `feel` leaves the control straight; an explicit
  `feel none` says "this is a straight tune." Swing always happens at play time (it never
  changes the written rhythm).
- `capo` is **presentational**: it records that the song is played with a capo so the **chord
  sheet** can show it. It does **not** transpose the written chords.

> All three use a **space** keyword (`feel triplet8th`, `tempo 120`, `capo 3`) — never a colon
> (`feel:` is reserved for a stored-progression reference).

### Modulation specs

| Spec | Move |
|------|------|
| `+n` / `-n` | up / down **n** semitones |
| `V` | up a fifth (+7) |
| `IV` | up a fourth (+5) |
| `bIII` | up a minor third — a leading `b`/`#` lowers/raises |
| `vi` | relative minor (+9 **and** switch to minor) — a **lowercase** numeral flips the mode |

That last one is how a tune moves between a major key and its relative minor. In **Falling
Leaves** (a shipped example), a major ii–V–I is answered by the relative-minor ii–V–i:

```
major: ii_v_i
minor: minor_ii_v_i

key G
major
mod vi        # G major → E minor
minor
```

### Pinning voicings — per-chord `{…}` and the `voice` default

By default ChordFlow **derives** the grip for each chord. To pin a specific one — a fingering
you chose, or a listed one — use either place (both are Song-level):

**The voicing value** is a **literal grip** or a **reference**:

| Value | Meaning |
|-------|---------|
| `8 x 7 9 8 x` | a **literal grip** — six frets low-E→high-E (`x` = muted, `0` = open) |
| `x 3 2 3 1 x root:6@8` | a **rootless** grip — `root:6@8` names where the root *would* be so it still transposes |
| `u: C6` | a reference to your **user** voicing `C6` |
| `a: auto:shell:dom7:E` | a reference to an **engine** voicing |

**1. Per-chord `{…}`** — pin one chord inside an inline progression (binds to the chord just
before it, overriding only that occurrence):

```
head = 17 {8 x 7 9 8 x} 47 17 67 2-7 {u: C6} 57 17
```

**2. `voice <selector> = <value>`** — a whole-song default that pins **every** matching chord:

```
voice *7   = 3 3 2 3 1 x      # every dominant-7 chord
voice 17   = 8 x 7 9 8 x      # …but the I7 specifically uses this
```

- `*<quality>` matches any degree of that quality (`*7`, `*m7`, `*maj7`, bare `*` = every major triad).
- `<degree><quality>` (`17`, `#4dim7`, `2-7`) matches just that degree.

**Which one wins** (most specific first): a per-chord `{…}` › a degree `voice 17` › a quality
`voice *7` › the automatic fill.

### A full example

```
name: Blues Song Demo
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

Intro, two verses (the 12-bar blues), up a fifth for the chorus, and a final verse — all from
one reusable definition. The `name:` / `genre:` / `subgenre:` / `tags:` / `description:` header
at the top is catalog metadata (used for filtering and display), not part of the arrangement.

---

## Where things live

The **Content** tab is the editor for everything above — progressions, songs, rhythms, and
voicings — with a live score preview so you can see and hear what you typed. ChordFlow also
ships a growing **starter library** (a set of progressions and 15 example songs) you can open,
play, and use as templates for your own.

*Building or contributing to ChordFlow itself? The developer reference docs live in
`loom/refs/` — they mirror the engine internals and are aimed at collaborators, not end users.*
