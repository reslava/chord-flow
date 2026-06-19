---
type: idea
id: id_01KVDEEY1959RD07H63R5PFMVZ
title: Interval lattice — fretboard interval positions (guitar projection of intervals)
status: done
created: 2026-06-18
updated: 2026-06-19
version: 3
tags: []
parent_id: null
requires_load: []
---
# Interval lattice — fretboard interval positions (guitar projection of intervals)

## Origin

Split out of the `domain/intervals` idea during the theory/guitar weave realignment (chat `loom/meta/general/chats/general-chat-005.md`, id `ch_01KVCPZHPD5FBMENZTFRH4FD0J`). The interval **vocabulary** (semitone → label, with the aug/dim7 spelling overrides) stays pure theory in `[[intervals]]` (`domain`) — it already exists as `IntervalSpeller.Name`. **This** thread is its **guitar projection**: where each interval physically sits on the fretboard.

> **Sequencing (corrected 2026-06-19):** this lattice is the **base guitar primitive**, not a consumer of `[[octave-shapes]]`. All five octave-shape root offsets (C −2, A +2, G −3, E +2, D +3, string-1 = string-6 same fret) fall out of this lattice's tuning table + mod 12 — so octave-shapes is the *unison/octave special case* that builds **on** this, not the reverse. (Earlier the dependency was wired backwards.)

## The idea

For a root anywhere on the neck, **where does each interval sit** — at any reach in either direction. The same-note (octave/unison) positions are exactly the `[[octave-shapes]]` root maps; this lattice **generalizes that to every degree**, and octave-shapes consumes it. The `[[caged-system]]` engine queries this lattice to place a quality's tones onto an octave shape.

### How it works (Rafa's research)

Standard tuning as **cumulative semitone offsets from string 6** (octave-preserving — the non-mod-12 version of what `Fretboard` already encodes as E A D G B E):

```
STRING_OFFSET = [0, 5, 10, 15, 19, 24]   // strings 6,5,4,3,2,1
```

Then everything is integer arithmetic:

```
abs(string, fret) = STRING_OFFSET[string] + fret      // absolute semitone coordinate
distance          = abs(target) − abs(origin)         // the canonical, signed value
```

Interval **labels are two thin views over that one canonical distance** (the domain owns the vocabulary — call `[[intervals]]`'s `IntervalSpeller.Name`, do **not** re-author the table here):

- **Pitch-class label** ("what interval is this vs. the root", direction-free): `Name(((distance % 12) + 12) % 12)` → `1…7`.
- **Unfolded + octave** (scales/arpeggios, which have real octaves): `Name(|distance|)` → `8/9/11/15…`, plus a direction flag from `sign(distance)`.

Both views ship — the caller picks per purpose. The single irregularity to respect everywhere is **string 3→2 = 4 semitones** (the B string); `STRING_OFFSET` already encodes it, so no code may assume a uniform +5 per string.

## In scope (when scheduled)

- The absolute-coordinate primitive (`STRING_OFFSET` + `abs(string, fret)`) — guitar geometry next to `Fretboard`, not in `domain`.
- An **on-demand** `distance(origin, target)` / "where does interval D sit near this origin" query — no pre-enumeration of "2 octaves L/R per string" (the absolute-coordinate math makes range bookkeeping unnecessary).
- Both label views (pitch-class and unfolded+octave) as projections over the canonical signed distance, via `IntervalSpeller.Name`.
- Reuses `PitchClass` + `Fretboard`; consumes the interval vocabulary from `[[intervals]]`. Lives in the `guitar` weave's code area (`Instruments/Guitar/`, per `[[instrument-boundary]]`).

## Out of scope / deferred

- The interval **vocabulary** itself (semitone → label, spelling, aug/dim7 overrides) — `[[intervals]]` in `domain` already owns it (`IntervalSpeller`). This thread does not change the domain.
- A reverse `label → semitone` parser or a first-class `Interval` value type — not needed by the lattice; build only if a consumer asks.
- Alternate tunings (`Fretboard` is fixed-tuning in v1) — though the semitone-integer model makes them cheap later.
- Scale / arpeggio overlays (same lattice, additive once chords work).

## Dependencies

`[[intervals]]` (the semitone→label vocabulary, `domain`) + `[[instrument-boundary]]` / `Fretboard` (guitar geometry). **No longer depends on `[[octave-shapes]]`** — that relationship is inverted (octave-shapes is the special case that consumes this). Consumed by `[[octave-shapes]]`, `[[caged-system]]`, and fed degrees by `[[chord-qualities]]`.

## Validation

Through `[[caged-system]]`, this lattice + `[[octave-shapes]]` + `[[chord-qualities]]` must reproduce the 34 hand-authored CAGED voicings (`packages/default-pack`) exactly — the golden oracle.

**Dogfood (standing guitar-weave rule):** ship a fretboard UI page that lights up every interval around a chosen root on the neck — fast visual confirmation the lattice math is right *before* building chord-qualities / caged on top. Built on the `[[fretboard-render-component]]`.

Related: `[[caged-system]]`, `[[intervals]]`, `[[octave-shapes]]`, `[[chord-qualities]]`, `[[interval-derivation-engine-vision]]`, `[[fretboard-render-component]]`, `[[chordflow-domain-model-reference]]`.