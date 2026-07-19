---
type: idea
id: id_01KXWJV6XD6K54AZP4D7K8NXVR
title: basic-drums idea
status: done
created: 2026-07-19
version: 1
tags: []
parent_id: null
requires_load: [rf_01KTSAPAT132QTEY5BEPRKS3MB, rf_01KTM41K36DYJ0CE44FE7TMCGH, rf_01KTSAQ6990GY3J4CZ7HPVPW6K]
---
# basic-drums idea

In **alphaTex**, drums are represented by **articulation names** instead of pitches or frets. To write a drum track we need to:

1. Set the instrument to percussion.
2. Register the default drum articulations.
3. Use articulation names (such as `KickHit` or `SnareHit`) as the notes. ([alphaTab][1])

### Minimal example

```alphatex
\instrument percussion
\articulation defaults
\ts 4 4

:8
HiHatClosed HiHatClosed HiHatClosed HiHatClosed
HiHatClosed HiHatClosed HiHatClosed HiHatClosed

:4
KickHit
SnareHit
KickHit
SnareHit
```

### A basic rock beat

This places eighth-note hi-hats over quarter-note kick/snare:

```alphatex
\instrument percussion
\articulation defaults

|
:8
(HiHatClosed KickHit)
HiHatClosed
(HiHatClosed SnareHit)
HiHatClosed
(HiHatClosed KickHit)
HiHatClosed
(HiHatClosed SnareHit)
HiHatClosed
|
```

Notes in parentheses are played simultaneously (a chord), so this creates:

* Beat 1: Kick + Closed Hi-Hat
* Beat 2: Snare + Closed Hi-Hat
* Beat 3: Kick + Closed Hi-Hat
* Beat 4: Snare + Closed Hi-Hat

### Common articulations

After `\articulation defaults`, we can use many standard names such as:

| Instrument    | alphaTex articulation |
| ------------- | --------------------- |
| Kick          | `KickHit`             |
| Snare         | `SnareHit`            |
| Closed Hi-Hat | `HiHatClosed`         |
| Open Hi-Hat   | `HiHatOpen`           |
| Pedal Hi-Hat  | `HiHatPedal`          |
| Crash         | `CrashHit`            |
| Ride          | `RideHit`             |
| Ride Bell     | `RideBell`            |
| High Tom      | `HighTomHit`          |
| Mid Tom       | `MidTomHit`           |
| Floor Tom     | `LowFloorTomHit`      |

The default articulation set includes many more drum and percussion sounds. ([alphaTab][2])

### Simultaneous hits

Use parentheses:

```alphatex
(KickHit CrashHit)
```

or

```alphatex
(KickHit HiHatClosed)
```

### Rests

Use `r`:

```alphatex
:8
KickHit
r
SnareHit
r
```

### Changing note durations

```alphatex
:4 KickHit SnareHit
:8 HiHatClosed HiHatClosed HiHatClosed HiHatClosed
:16 KickHit KickHit KickHit KickHit
```

The duration (`:4`, `:8`, `:16`, etc.) stays in effect until we change it. ([alphaTab][1])

[1]: https://alphatab.net/docs/alphatex/document-structure?utm_source=chatgpt.com "Document Structure | alphaTab"
[2]: https://next.alphatab.net/docs/alphatex/staff-metadata?utm_source=chatgpt.com "Staff metadata | alphaTab"

## Basic drums grooves for main Genres blues, rock, funk, jazz

Below are simple one-bar drum grooves in **alphaTex** using the default percussion articulations. They are meant as "starter grooves" that we can loop.

Start each track with:

```alphatex
\instrument percussion
\articulation defaults
\ts 4 4
```

---

# 1. Rock (Straight 8ths)

### Rock #1 – Basic 8ths

```alphatex
|
:8
(HiHatClosed KickHit)
HiHatClosed
(HiHatClosed SnareHit)
HiHatClosed
(HiHatClosed KickHit)
HiHatClosed
(HiHatClosed SnareHit)
HiHatClosed
|
```

Pattern:

```
HH x x x x x x x x
SD . . o . . . o .
BD o . . . o . . .
```

---

### Rock #2 – Kick Variation

```alphatex
|
:8
(HiHatClosed KickHit)
HiHatClosed
(HiHatClosed SnareHit)
(HiHatClosed KickHit)
HiHatClosed
(HiHatClosed KickHit)
(HiHatClosed SnareHit)
HiHatClosed
|
```

---

# 2. Blues Shuffle (Triplet Feel)

Think:

```
1-trip-let 2-trip-let ...
play: X . X
```

### Shuffle #1

```alphatex
|
:12
(HiHatClosed KickHit)
r
HiHatClosed

(HiHatClosed SnareHit)
r
HiHatClosed

(HiHatClosed KickHit)
r
HiHatClosed

(HiHatClosed SnareHit)
r
HiHatClosed
|
```

---

### Shuffle #2 – Walking Kick

```alphatex
|
:12
(HiHatClosed KickHit)
r
HiHatClosed

(HiHatClosed SnareHit)
r
(HiHatClosed KickHit)

HiHatClosed
r
(HiHatClosed KickHit)

(HiHatClosed SnareHit)
r
HiHatClosed
|
```

---

# 3. Jazz Swing

Usually played on the ride.

### Jazz #1

```alphatex
|
:12
(RideHit KickHit)
r
RideHit

(RideHit SnareHit)
r
RideHit

(RideHit KickHit)
r
RideHit

(RideHit SnareHit)
r
RideHit
|
```

---

### Jazz #2 – Ride + Hi-hat Foot

```alphatex
|
:12
(RideHit KickHit)
r
RideHit

(RideHit SnareHit HiHatPedal)
r
RideHit

(RideHit KickHit)
r
RideHit

(RideHit SnareHit HiHatPedal)
r
RideHit
|
```

Hi-hat pedal occurs on beats **2** and **4**.

---

# 4. Funk (Straight 16ths)

### Funk #1

```alphatex
|
:16
(HiHatClosed KickHit)
HiHatClosed
HiHatClosed
HiHatClosed

(HiHatClosed SnareHit)
HiHatClosed
HiHatClosed
HiHatClosed

(HiHatClosed KickHit)
HiHatClosed
HiHatClosed
HiHatClosed

(HiHatClosed SnareHit)
HiHatClosed
HiHatClosed
HiHatClosed
|
```

---

### Funk #2 – Syncopated Kick

```alphatex
|
:16
(HiHatClosed KickHit)
HiHatClosed
HiHatClosed
(HiHatClosed KickHit)

(HiHatClosed SnareHit)
HiHatClosed
(HiHatClosed KickHit)
HiHatClosed

HiHatClosed
HiHatClosed
(HiHatClosed KickHit)
HiHatClosed

(HiHatClosed SnareHit)
HiHatClosed
HiHatClosed
HiHatClosed
|
```

---

# 5. Straight 8ths Pop/Country

### Groove #1

```alphatex
|
:8
(HiHatClosed KickHit)
HiHatClosed
(HiHatClosed SnareHit)
HiHatClosed
HiHatClosed
(HiHatClosed KickHit)
(HiHatClosed SnareHit)
HiHatClosed
|
```

---

### Groove #2

```alphatex
|
:8
(HiHatClosed KickHit)
HiHatClosed
(HiHatClosed SnareHit)
HiHatClosed
(HiHatClosed KickHit)
(HiHatClosed KickHit)
(HiHatClosed SnareHit)
HiHatClosed
|
```

---

# Summary

| Genre       | Subdivision            | Cymbal        |
| ----------- | ---------------------- | ------------- |
| Rock        | Straight 8ths          | Closed Hi-hat |
| Blues       | Shuffle (triplet 8ths) | Closed Hi-hat |
| Jazz        | Swing (triplet 8ths)   | Ride          |
| Funk        | Straight 16ths         | Closed Hi-hat |
| Pop/Country | Straight 8ths          | Closed Hi-hat |

These grooves intentionally use only the essentials—kick, snare, hi-hat, ride, and hi-hat pedal—making them easy to expand. From here we can add crashes at phrase starts, ghost notes on the snare, open hi-hats, tom fills, and more syncopated kick patterns.

## Applying all to our Rhythm Engine

I would like to create/write new grooves using a kind of rhythm grid: 
HH x x x x x x x x 
SD . . o . . . o . 
BD o . . . o . . . 

Our app already use a 48-PPQ tick-grid model and a DSL to write rhythm like this:

## Using the 48-PPQ + DSL expanded for the drums

Our approach it's much more expressive than a typical drum grid because it can represent any instrument while still being easy to read.

For drums, though, we would actually **not** use the `X...` duration semantics. Drum hits are almost always instantaneous, so the grid becomes much easier to author if every occupied cell is simply an attack and empty cells are silence.

For example, instead of

```text
HH X-X-X-X-
```

we'd write

```text
HH x x x x x x x x
SD . . o . . . o .
BD o . . . o . . .
```

where

* `x` = hi-hat hit
* `o` = snare/kick hit
* `.` = no hit

Our parser could translate every hit into a one-cell note automatically.

Lets reuse our DSL unchanged (which I think is a good idea), here's how we'd map common grooves.

---

# Rock 1

16ths (`:4`)

```text
HH :4 X.X.X.X.X.X.X.X.
SD :4 ----X-------X---
BD :4 X-------X-------
```

Equivalent drum grid

```text
HH x x x x x x x x
SD . . o . . . o .
BD o . . . o . . .
```

---

# Rock 2

```text
HH :4 X.X.X.X.X.X.X.X.
SD :4 ----X-------X---
BD :4 X-------X.X-----
```

Grid

```text
HH x x x x x x x x
SD . . o . . . o .
BD o . . . o o . .
```

---

# Four-on-the-floor

```text
HH :4 X.X.X.X.X.X.X.X.
SD :4 ----X-------X---
BD :4 X...X...X...X...
```

Grid

```text
HH x x x x x x x x
SD . . o . . . o .
BD o . o . o . o .
```

---

# Funk 1

16th hats

```text
HH :4 XXXX XXXX XXXX XXXX
SD :4 ----X-------X---
BD :4 X-----X---X-----
```

Grid

```text
HH xxxxxxxxxxxxxxxx
SD ....o.......o...
BD o.....o...o.....
```

---

# Funk 2

```text
HH :4 XXXX XXXX XXXX XXXX
SD :4 ----X-------X---
BD :4 X---X-----X-X---
```

Grid

```text
HH xxxxxxxxxxxxxxxx
SD ....o.......o...
BD o...o.....o.o...
```

---

# Shuffle / Blues

Here I'd actually use `:3`.

```text
HH :3 X.X X.X X.X X.X
SD :3 --- X.. --- X..
BD :3 X.. --- X.. ---
```

Grid

```text
HH x . x  x . x  x . x  x . x
SD . . .  o . .  . . .  o . .
BD o . .  . . .  o . .  . . .
```

---

# Jazz Ride

```text
RD :3 X.X X.X X.X X.X
HH :3 --- X.. --- X..
SD :3 ----X-------X---
BD :3 X.. --- --- ---
```

Conceptually

```text
Ride x . x  x . x  x . x  x . x
HHft . . .  o . .  . . .  o . .
BD   o . .  . . .  . . .  . . .
```

---

# Jazz Comping

```text
RD :3 X.X X.X X.X X.X
HH :3 --- X.. --- X..
SD :3 --- X-- --X ---
BD :3 X-- --- X-- ---
```

---

## Why I would use `:3` instead of a swing flag

Our design:

- **Swing** is performance.
- **Triplets** are notation.

So

```text
:4 X.X.X.X.
```

should mean

> straight eighths

and

```text
:3 X.X X.X X.X X.X
```

should mean

> literal triplet rhythm

If the user later selects a **Swing 8ths** feel, your engine can perform

```text
:4 X.X.X.X.
```

*as if* it were

```text
:3 X.X X.X X.X X.X
```

without changing the written rhythm. That's exactly how most notation software (and real musicians) treat swing.

## One thing I'd consider

For percussion only, I'd add an alternate "hit grid" syntax that omits durations entirely:

```text
HH | x x x x x x x x
SD | . . o . . . o .
BD | o . . . o . . .
```

or even

```text
HH xxxxxxxx
SD ..o...o.
BD o...o...
```

Internally, each `x`/`o` simply becomes a one-cell `X` in our rhythm engine. This gives drummers the notation they're used to while letting your underlying DSL remain unified for all instruments. I think that separation—**notation optimized for the user, common rhythm model underneath**—would make the authoring experience much nicer.
