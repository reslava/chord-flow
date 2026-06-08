---
type: design
id: de_01KTM0DRF3Q7F4X35RMCBX6DDT
title: Music-Theory Domain Model — Design
status: draft
created: 2026-06-08
version: 1
tags: []
parent_id: null
requires_load: []
---
# Music-Theory Domain Model — Design

## 1. Purpose & scope

Replace the MVP's thin, partly-hardcoded domain with a **music-theory-first** kernel so that transposition, diatonic generation, scales/modes, chord voicings, rhythm patterns, swing/shuffle, and lead "sweet-spot" targets are all *derived*, never hand-authored per case.

Two themes, modeled as **separate layers** that meet only at render time:

- **(A) Harmony** — pitch, scales, chords, progressions, voicings.
- **(B) Rhythm** — pure time: positional events on a tick grid, with feel/accent/stroke as composable overlays.

**Guiding principle (agreed):**

```
Theory  →  Voicing  →  Rendering
```

never "rendered chord name → reverse-engineer later". And, the keystone decision for Theme B:

> **Rhythm patterns contain only timing. Chords, scales, voicings, and solo targets are separate layers applied onto the grid.**

This is what lets one rhythm library serve comping, blues, funk, pentatonic target drills, arpeggio studies, and guide-tone training without duplicating patterns.

### In scope
- The domain type model for harmony + rhythm (this doc).
- Migration of the existing sequential rhythm model to a tick grid (decided: **do it now**, while surface area is tiny).
- A grid → alphaTex **quantizer** as a new responsibility of the renderer.

### Out of scope (later threads)
- Accuracy/pitch detection.
- Persistence schema changes (will follow once the model settles).
- Full UI for the lead-training fretboard view.

---

## 2. What already exists (baseline)

The MVP (`src/ChordFlow.App/Domain/*.cs`) already implements the harmony spine correctly and is the starting point — not a rewrite:

| Concept | Current type | Verdict |
|---|---|---|
| Pitch class | `PitchClass(int Value)` 0..11, spelling deferred | **Keep** — correct |
| Quality | `enum Quality { Major, Minor, Dominant7 }` | Keep enum, **back it with intervals** |
| Chord | `Chord(PitchClass Root, Quality Quality)` | Keep |
| Degree | `RomanDegree(int Degree, Quality Quality)` | Keep (rename concept clarity) |
| Progression | `Progression(Id, Name, IReadOnlyList<RomanDegree>)` | Keep |
| Transpose | `Transposer.Realize(prog, key)` | Keep; extract `Scale` |
| Voicing | `Voicing(IReadOnlyList<FretPosition>)` | **Keep list**, add barre/mute metadata |
| Voicing source | `VoicingBook` — one algorithmic movable shell shape | Keep; generalize to a strategy |
| Rhythm | `RhythmPattern(Id, Name, IReadOnlyList<Beat>)`, `Beat(Duration, IsHit)` | **Migrate** to tick grid |
| Render | `AlphaTexRenderer` (only alphaTex-aware code) | Keep seam; add quantizer + spelling |

---

## 3. Theme A — Harmony model

### 3.1 Pitch & spelling
- `PitchClass` stays an `int 0..11`. **Spelling is never baked into the type.** PC 1 is `C#` in D major but `Db` in Ab major.
- **New:** a `NoteSpeller` (pure function) `(PitchClass, Key) → string` producing the correctly-spelled note name and accidental for the active key. This also feeds the alphaTex key signature (today hardcoded as a flat-only array inside the renderer — promote it to the domain).

### 3.2 Scale (first-class)
- Promote the interval arrays currently buried in `Transposer` into a real type:
  - `Scale(PitchClass Tonic, int[] Intervals)` — Major `{0,2,4,5,7,9,11}`, Natural minor `{0,2,3,5,7,8,10}`, modes/pentatonic later.
- `Transposer` consumes a `Scale` rather than owning the offsets.

### 3.3 Quality as interval set (correction)
- `Quality` stays a friendly enum **but is backed by an interval table** from the root:
  - `Maj7 = {0,4,7,11}`, `Dom7 = {0,4,7,10}`, `Min7 = {0,3,7,10}`, `m7b5 = {0,3,6,10}`, …
- This single source generates: chord tones, **guide tones (3 & 7)**, and lead **target tones**. "The b7 of G7" = `root + 10` — computable, not stored. **This is the bridge between Theme A and Theme B.**

### 3.4 Degrees — two distinct reference frames (correction)
Do **not** reuse one `ScaleDegree` for everything:
- **`ScaleDegree`** — relative to the **key tonic** (I–vii). Used by progressions. (Current `RomanDegree` fills this role.)
- **`ChordTone` / interval** — relative to the **chord root** (R, 3, 5, b7). Used by voicings and lead targets.

Conflating them breaks as soon as lead targets arrive.

### 3.5 Voicing (correction: stay a list)
- Keep `Voicing(IReadOnlyList<FretPosition>)` — flexible for partial voicings, drop/alt tunings, 7-string later. Reject the fixed `String6..String1` shape.
- **Add optional diagram metadata** for the alphaTex `\chord (...)` directive: `BarreFret?`, muted strings, `FirstFret`. These are presentation hints, not positional fields.
- **Voicing selection is a strategy, not a table:** `Difficulty → shape chooser` (Beginner = open/shell, Intermediate = shell, Advanced = inversions). `VoicingBook`'s single algorithmic shape is the first strategy.

### 3.6 Diatonic generator
- `BuildDiatonicChord(Scale, ScaleDegree) → Chord` falls out of §3.2–3.4, producing `I Cmaj7 / ii Dm7 / … / vii Bm7b5` automatically.

---

## 4. Theme B — Rhythm model (the migration)

### 4.1 Decision: positional tick grid (replaces sequential `Beat`)
- **Migrate now.** Current `Beat(Duration, IsHit)` is sequential and cannot express syncopation ("the *a* of 2"), ties, accents, or polyrhythm.
- New unit:
  - `RhythmEvent(int Position, int Length, Stroke Stroke, Accent Accent)` — `Position`/`Length` in **ticks**.
  - `RhythmPattern(string Name, IReadOnlyList<RhythmEvent> Events, TimeSignature Ts)`.

### 4.2 One fixed PPQ tick base (not a per-pattern `GridResolution`)
- Reject a per-pattern `GridResolution` enum — it makes a 16th groove uncomposable with a triplet fill.
- Use **one tick base = 48 PPQ** (pulses per quarter): divisible by 4 → sixteenth = 12 ticks, and by 3 → eighth-triplet = 16 ticks. All common subdivisions coexist in one grid, so **pattern composition** works.
- Bar length in ticks derives from `TimeSignature` × PPQ.

### 4.3 Feel, accent, stroke as composable overlays
- `Feel { Straight, Swing, Shuffle, Triplet }` is a **playback-time transform**, never baked into the pattern. The renderer/player warps timing (long-short) from a straight grid.
- `AccentPattern` (e.g. backbeat) and `Stroke` (Down/Up/Either) are separate layers composed onto the timing grid.
- `Pattern + AccentPattern + Feel → final exercise pattern`.

### 4.4 Pickup / anacrusis
- Model a pickup as its own short **leading measure** with its own length — *not* `Position: -2`. Negative positions complicate bar math and rendering.

### 4.5 Quantizer (new renderer responsibility)
- alphaTex consumes **sequential durations**, so the renderer gains a **tick-grid → duration-token compiler**: walk the bar's events in tick order, emit `:N` tokens (and rests for gaps, ties where a note crosses the grid). This is the main new piece of work the migration buys us, and it stays isolated inside the `Rendering/` seam.

---

## 5. The unifying object

```
Exercise
  Key
  Progression        (Theme A — harmony)
  RhythmPattern      (Theme B — pure timing)
  Content            ChordVoicings | SoloTargets   (applied onto the grid)
  Feel
  Tempo, Difficulty
```

Engine pipeline:

```
Exercise
  → resolve progression (Transposer + Scale)
  → choose voicings (Difficulty strategy)  OR  resolve solo targets to fretboard
  → apply rhythm grid + feel/accent
  → quantize → alphaTex
  → alphaTab
```

---

## 6. Lead training (Theme B, content layer)

- `TargetZone(ChordTone Tone, Importance Importance)` — chord-**relative** (see §3.4), resolved to fretboard positions late.
- For ii–V–I, guide-tone targets (3 & 7 of each chord) derive directly from the §3.2 interval sets — no per-chord authoring.
- Renders as a sparse "sweet-spot" fret map (`o` = target, `x` = other scale notes) rather than full notation.

---

## 7. Migration plan (shape, not steps)

1. Harmony additions that don't break MVP: `Scale` type, `Quality`→interval table, `NoteSpeller`, split `ScaleDegree`/`ChordTone`, voicing diagram metadata + selection strategy.
2. Rhythm migration: introduce tick-grid `RhythmEvent`/`RhythmPattern` + `TimeSignature`; port the three MVP patterns (beat-1, beat-1+3, quarters) to ticks.
3. Renderer: add the quantizer + domain-driven spelling; keep `AlphaTexRenderer` the only alphaTex-aware code.
4. Feel/accent/stroke overlays + lead `TargetZone` once 1–3 are green.

Persistence and UI changes follow in their own threads once the model is stable.

---

## 8. Open questions

- **Q1 — PPQ value:** 48 covers 16ths + triplets; do we want 32nd-note or quintuplet support (would push to 96/240)? Default 48 unless a use case demands more.
- **Q2 — Time signature scope:** support compound (6/8) and odd (5/4, 7/8) meters in v1 of the rhythm model, or 4/4 only and generalize later?
- **Q3 — Migration boundary:** do persisted MVP exercises (sequential beats) need a one-time data migration, or do we regenerate from definitions (the ctx says SQLite stores *definitions* and regenerates alphaTex on load — likely no migration needed)?
- **Q4 — Quality coverage for v1:** which qualities ship first (Maj/Min/Dom7/Maj7/Min7/m7b5/dim/aug)?
