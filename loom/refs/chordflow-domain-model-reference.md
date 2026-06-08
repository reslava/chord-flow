---
type: reference
id: rf_01KTM41K36DYJ0CE44FE7TMCGH
title: "ChordFlow Domain Model"
status: active
created: 2026-06-08
version: 1
tags: []
parent_id: null
child_ids: []
requires_load: []
slug: chordflow-domain-model
description: "Map of the ChordFlow music kernel — harmony, rhythm (48-PPQ tick grid), voicings, feel/accent/stroke overlays, lead targets, the quantizer/render seam, and the Exercise pipeline. Load when designing/implementing features or touching the domain."
---
A map of the ChordFlow **music kernel** as it stands after the
`music-theory-to-data-structures` thread (v0.3.0). Use this when designing or
implementing new features, or before touching `Domain/` or `Rendering/`. It
records *what each type is for and how the layers connect* — the source files
are the detail.

> **Guiding principle:** `Theory → Voicing → Rendering`. Everything is *derived*,
> never hand-authored per case. **Rhythm patterns hold only timing**; chords,
> voicings, and lead targets are separate layers applied onto the grid.

All `Domain/` types are **pure and immutable** (records / readonly record structs),
no I/O (C3). Spelling and `Feel` are **never stored** — always derived (C4).

---

## 1. Harmony layer (`Domain/`)

| Type | Role |
|------|------|
| `PitchClass(int Value)` | 0..11 (0=C). **Spelling deferred** — PC 1 is C# in D, Db in Ab. |
| `Key(PitchClass Tonic, bool IsMinor)` | Tonic + mode. |
| `Quality` (enum) | The 8 v1 qualities: Major, Minor, Dominant7, Major7, Minor7, HalfDiminished7 (m7b5), Diminished, Augmented. |
| `QualityIntervals` | **Single source of truth** for what notes a quality contains (C5). `Intervals(q)` → semitones; `FromIntervals(set)` → reverse match. |
| `ChordTone(int Interval, ChordToneFunction Function)` | A tone **relative to the chord root** (R/3/5/7). `PitchClassFor(root)` resolves it late. |
| `ChordToneFunction` (enum) | Root, Third, Fifth, Seventh — classified from the interval (0→Root, 3/4→Third, 6/7/8→Fifth, 10/11→Seventh). |
| `ChordTones` | `Of(chord)` → the chord's tones; `PitchClassesOf(chord)`. The **Theme A↔B bridge**: "b7 of G7" = root+10, computed not stored. |
| `Chord(PitchClass Root, Quality Quality)` | A concrete chord. |
| `RomanDegree(int Degree, Quality Quality)` | Key-relative degree **carrying an explicit quality** — for authored progressions (e.g. all-Dom7 blues). |
| `ScaleDegree(int Number)` | Key-relative position **with no quality** — the diatonic generator derives the quality. (IN4: two distinct degree frames; do not conflate.) |
| `Scale(PitchClass Tonic, IReadOnlyList<int> Intervals)` | First-class scale. `Major`/`NaturalMinor`/`ForKey` factories; `DegreePitchClass(n)`. Owns the offset arrays (moved out of `Transposer`). |
| `DiatonicChord` | `Build(scale, scaleDegree)` → a 7th chord by stacking scale thirds; quality derived via `QualityIntervals.FromIntervals`. C major ⇒ I maj7 … vii m7b5. |
| `Transposer` | `Realize(progression, key)` / `Realize(progression, scale)` → concrete `Chord[]`. |
| `Progression(Id, Name, IReadOnlyList<RomanDegree>)` | Key-independent progression. |
| `NoteSpeller` | `Name(pc, key)` → per-key sharp/flat spelling; `KeySignatureToken(key)` → alphaTex `\ks`. Promoted out of the renderer (C2). |

---

## 2. Voicing layer (`Domain/`)

| Type | Role |
|------|------|
| `FretPosition(int String, int Fret)` | alphaTab string numbering (1=high E .. 6=low E), fret 0=open. |
| `Voicing(Positions, BarreFret?, FirstFret?, MutedStrings?)` | A list of fret positions + optional **diagram metadata** (presentation hints for `\chord (...)`; positions stay authoritative). |
| `IVoicingStrategy` | `Difficulty Difficulty`, `Voice(chord) → Voicing`. Selection is a strategy, not a table (IN7). |
| `BeginnerShellStrategy` | Movable dom7 shell (root + maj3 + min7 on strings 5/4/3); covers all 12 roots; emits FirstFret + muted {1,2,6}. |
| `VoicingBook` | `Lookup(chord, difficulty)` — dispatches to the registered strategy. New tiers register a strategy; call sites unchanged. |
| `Fretboard` | Standard-tuning geometry. `PositionsFor(pc, maxFret=12)` → every fret that sounds a pitch class. |

---

## 3. Rhythm layer — 48-PPQ tick grid (`Domain/`)

The old sequential `Beat(Duration, IsHit)` model was **removed**; rhythm is now positional on a tick grid.

| Type | Role |
|------|------|
| `TickGrid` | `Ppq = 48` (C1; ÷4 → 16th=12 ticks, ÷3 → eighth-triplet=16 ticks). `WholeNoteTicks = 192`. One fixed grid — no per-pattern resolution, so subdivisions compose. |
| `TimeSignature(Numerator, Denominator)` | `FourFour` (4/4 only, EX2). `BeatTicks` (=48), `BarTicks` (=192) derived from PPQ. |
| `RhythmEvent(int Position, int Length, Stroke, Accent)` | A positional note/strum (ticks). `Hit(pos, len)` = plain down-stroke. Expresses syncopation/ties/accents. |
| `Stroke` (enum) | Down / Up / Either. |
| `Accent` (enum) | Normal / Accented. |
| `RhythmPattern(Id, Name, Events, TimeSignature, Pickup?)` | **One bar of timing only.** No chords/voicings/feel baked in. |
| `PickupMeasure(Events, LengthTicks)` | Anacrusis as its own short **leading measure** (IN11), not a negative position. |
| **Seed patterns** (`SeedData`) | `Beat1`, `Beat1And3`, `Quarters` (ids `beat_1`/`beat_1_3`/`quarters`), `TwelveBarBlues`, `AllMajorKeys`. |

### Composable overlays (never mutate the base; return new event lists)

| Type | Role |
|------|------|
| `Feel` (enum) + `FeelTransform` | Playback-time **long-short warp** (C4 — never stored). `Apply(events, feel, ts)`. Off-beat ratios: Straight 1/2, Swing 2/3, Shuffle 3/4, Triplet 2/3. Straight = identity. |
| `AccentPattern(AccentedBeats)` | `Backbeat` (beats 2 & 4). `Apply(events, ts)` accents events on those beats (additive). |
| `StrokeOverlay` | `All(events, stroke)`, `AlternateDownUp(events)`. |

---

## 4. Lead-training layer (`Domain/`) — domain only, no UI (EX5)

| Type | Role |
|------|------|
| `Importance` (enum) | Primary (guide tones) / Secondary. |
| `TargetZone(ChordTone Tone, Importance)` | A chord-relative "sweet spot"; resolves to pitch classes / frets late. |
| `LeadTargets` | `GuideTones(chord)` → the 3rd & 7th as Primary; `PitchClassOf(chord, zone)`; `Resolve(chord, zone, maxFret)` → fret positions. ii–V–I guide-tone lines fall out of the interval sets — no per-chord authoring. |

---

## 5. Rendering seam (`Rendering/`) — the **only** alphaTex-aware code (C2)

| Type | Role |
|------|------|
| `RhythmSlot(int NoteValue, bool IsRest, bool TiedToPrevious)` | One quantized note/rest cell; `NoteValue` = alphaTex `:N` (1/2/4/8/16). |
| `RhythmQuantizer` | tick grid → sequential slots (IN12). Walks events in order, fills gaps with rests, **splits spans at beat lines**, greedy-decomposes each chunk. A note crossing a beat line → tied continuation slots; rests are separate. `Quantize(events, ts)` / `Quantize(pickup)`. |
| `AlphaTexRenderer` | `Render(exercise)` → alphaTex. Header (`\title \subtitle \tempo \ts \ks .`) then bars of stateful `:N` + `( )` chord groups / `r`. Calls `NoteSpeller` + `RhythmQuantizer`; applies `Feel` pre-quantize. |
| `IScoreRenderer` | Seam for future MIDI / GuitarPro / MusicXML exporters. |

> ⚠️ **Ties/dotted alphaTex tokens are unverified** (see `alphatex-syntax-reference.md`). The quantizer models ties as slot metadata but the MVP patterns never produce them; the renderer **throws** if a tie slot ever reaches it rather than emit an unverified token.

---

## 6. The unifying object & pipeline

`Exercise(Key, Progression, RhythmPattern Rhythm, int Tempo, Difficulty, Feel = Straight)` — the persisted definition (SQLite stores definition fields only, by `RhythmId`; alphaTex is regenerated on load, never stored — EX3/EX6).

```
Exercise
  → Transposer.Realize (resolve progression via Scale)
  → VoicingBook strategy (voicings)   OR  LeadTargets (solo targets → fretboard)
  → FeelTransform (apply rhythm + feel; identity for Straight)
  → RhythmQuantizer (→ slots)
  → AlphaTexRenderer (→ alphaTex)
  → alphaTab
```

---

## 7. Invariants worth remembering

- **C1** PPQ fixed at 48. **C2** only `AlphaTexRenderer` knows alphaTex; quantizer + spelling live in the Domain/Rendering seam. **C3** domain kernel pure + unit-tested. **C4** Feel never stored in a pattern; spelling never stored — both derived. **C5** the 8 quality interval sets. **C7** the build/tests stayed green at every migration step.
- **Two degree frames** (IN4): `RomanDegree`/`ScaleDegree` (key-relative) vs `ChordTone` (chord-relative). Don't conflate.
- **4/4 only** for v1 (EX2); no 32nds/quintuplets (EX1); no accuracy detection (EX4); no lead-training UI (EX5).

Related: [[alphatex-syntax-reference]], [[alphatab-js-api-reference]].
