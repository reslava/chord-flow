---
type: reference
id: rf_01KTM41K36DYJ0CE44FE7TMCGH
title: ChordFlow Domain Model
status: active
created: "2026-06-08T00:00:00.000Z"
updated: 2026-06-10
version: 2
tags: []
parent_id: null
requires_load: []
slug: chordflow-domain-model
description: Map of the ChordFlow music kernel — harmony, rhythm (48-PPQ tick grid), voicings, feel/accent/stroke overlays, lead targets, the quantizer/render seam, and the Exercise pipeline. Load when designing/implementing features or touching the domain.
---
# ChordFlow Domain Model

A map of the ChordFlow **music kernel** as it stands after the
`multi-chord-per-bar progressions` thread (v0.4.0). Use this when designing or
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
| `RomanDegree(int Degree, Quality Quality)` | Key-relative degree **carrying an explicit quality** — for authored progressions. **Timing-free** (C1 of the harmonic-rhythm thread: timing lives on `ChordSpan`, never on the degree). |
| `ScaleDegree(int Number)` | Key-relative position **with no quality** — the diatonic generator derives the quality. (Two distinct degree frames; do not conflate.) |
| `Scale(PitchClass Tonic, IReadOnlyList<int> Intervals)` | First-class scale. `Major`/`NaturalMinor`/`ForKey` factories; `DegreePitchClass(n)`. Owns the offset arrays (moved out of `Transposer`). |
| `DiatonicChord` | `Build(scale, scaleDegree)` → a 7th chord by stacking scale thirds; quality derived via `QualityIntervals.FromIntervals`. C major ⇒ I maj7 … vii m7b5. |
| `ChordSpan(RomanDegree Degree, int DurationTicks)` | **v0.4.0.** A chord + its tick duration on the 48-PPQ grid. Duration ∈ {48, 96, 144, 192} (quarter-aligned, v1); summing across all spans in a bar must equal `BarTicks` (192 in 4/4). |
| `HarmonicBar(IReadOnlyList<ChordSpan> Spans)` | **v0.4.0.** One bar's harmonic rhythm — 1–4 spans in order. `SpanCovering(int tick)` → the span whose `[start, start+dur)` interval contains `tick`. `TotalTicks` helper. |
| `Progression(string Id, string Name, IReadOnlyList<HarmonicBar> Bars)` | **v0.4.0.** Key-independent progression as a sequence of `HarmonicBar`s. Constructed via the guarded factory `Progression.FromBars(id, name, bars, ts)` which validates per bar: spans sum to `ts.BarTicks`, each `DurationTicks > 0`, each a multiple of `ts.BeatTicks`. A single-chord bar = one `ChordSpan(degree, 192)` — backward-compatible with v0.3. |
| `ProgressionParser` | **v0.4.0.** Pure static `Parse(id, name, dsl, ts) → Progression`. M1 Nashville DSL: space = bar separator, `_` = chord separator. Token = `<degree><quality?>[:<slots>]`. Even-split (n∈{1,2,4}); explicit-slots suffix (`:N` quarters, all-or-nothing per bar, must sum to 4). Throws `FormatException` naming the bad token. Peer of `NoteSpeller`. |
| `Transposer` | `Realize(progression, key)` → `IReadOnlyList<RealizedBar>` where each `RealizedBar` holds ordered `(Chord chord, int DurationTicks)` pairs — preserving the harmonic-rhythm structure through to the renderer. |
| `NoteSpeller` | `Name(pc, key)` → per-key sharp/flat spelling; `KeySignatureToken(key)` → alphaTex `\ks`. Promoted out of the renderer. |

### Nashville DSL quick reference (M1)

| Suffix | Quality | Example |
|--------|---------|---------|
| *(none)* | Major | `1` |
| `-` / `m` | Minor | `6-` |
| `7` | Dominant7 | `57` |
| `-7` / `m7` | Minor7 | `2-7` |
| `maj7` / `^7` | Major7 | `1^7` |
| `°` / `dim` | Diminished | `7°` |
| `ø` / `m7b5` | HalfDiminished7 | `7ø` |
| `+` / `aug` | Augmented | `5+` |

Even split: `17_67` → I7 half · VI7 half. Explicit slots: `17:2_67:1_27:1` → I7 (96) · VI7 (48) · ii7 (48). 3-chord bars require explicit slots.

---

## 2. Voicing layer (`Domain/`)

| Type | Role |
|------|------|
| `FretPosition(int String, int Fret)` | alphaTab string numbering (1=high E .. 6=low E), fret 0=open. |
| `Voicing(Positions, BarreFret?, FirstFret?, MutedStrings?)` | A list of fret positions + optional **diagram metadata** (presentation hints for `\chord (...)`; positions stay authoritative). |
| `IVoicingStrategy` | `Difficulty Difficulty`, `Voice(chord) → Voicing`. Selection is a strategy, not a table. |
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
| `PickupMeasure(Events, LengthTicks)` | Anacrusis as its own short **leading measure**, not a negative position. |
| **Seed patterns** (`SeedData`) | `Beat1`, `Beat1And3`, `Quarters` (ids `beat_1`/`beat_1_3`/`quarters`). `BuiltInProgressions` — `IReadOnlyList<ProgressionDefinition>` with `Id`, `Name`, `Dsl`; includes `12-Bar Blues` and `Jazz-Blues Turnaround`. Seeded as `BuiltIn` rows on first run. |

### Composable overlays (never mutate the base; return new event lists)

| Type | Role |
|------|------|
| `Feel` (enum) + `FeelTransform` | Playback-time **long-short warp** (C4 — never stored). `Apply(events, feel, ts)`. Off-beat ratios: Straight 1/2, Swing 2/3, Shuffle 3/4, Triplet 2/3. Straight = identity. |
| `AccentPattern(AccentedBeats)` | `Backbeat` (beats 2 & 4). `Apply(events, ts)` accents events on those beats (additive). |
| `StrokeOverlay` | `All(events, stroke)`, `AlternateDownUp(events)`. |

---

## 4. Lead-training layer (`Domain/`) — domain only, no UI

| Type | Role |
|------|------|
| `Importance` (enum) | Primary (guide tones) / Secondary. |
| `TargetZone(ChordTone Tone, Importance)` | A chord-relative "sweet spot"; resolves to pitch classes / frets late. |
| `LeadTargets` | `GuideTones(chord)` → the 3rd & 7th as Primary; `PitchClassOf(chord, zone)`; `Resolve(chord, zone, maxFret)` → fret positions. ii–V–I guide-tone lines fall out of the interval sets — no per-chord authoring. |

---

## 5. Rendering seam (`Rendering/`) — the **only** alphaTex-aware code

| Type | Role |
|------|------|
| `RhythmSlot(int NoteValue, bool IsRest, bool TiedToPrevious, int StartTick)` | One quantized note/rest cell. `NoteValue` = alphaTex `:N` (1/2/4/8/16). **`StartTick`** (v0.4.0) = bar-relative tick of the slot's onset; used by the renderer to look up which `ChordSpan` covers this slot. |
| `RhythmQuantizer` | tick grid → sequential slots. Walks events in order, fills gaps with rests, **splits spans at beat lines and at `ChordSpan` boundaries** (v0.4.0). At a chord boundary: a sounding note **re-attacks** (`TiedToPrevious = false`); a rest stays a rest (no phantom strum). `Quantize(events, ts, spanBoundaries)`. |
| `AlphaTexRenderer` | `Render(exercise)` → alphaTex. Per realized bar, quantizes with that bar's span boundaries, then for each slot picks the chord via `HarmonicBar.SpanCovering(slot.StartTick)` (v0.4.0). Header (`\title \subtitle \tempo \ts \ks .`) then bars of stateful `:N` + `( )` chord groups / `r`. Calls `NoteSpeller` + `RhythmQuantizer`; applies `Feel` pre-quantize. |
| `IScoreRenderer` | Seam for future MIDI / GuitarPro / MusicXML exporters. |

> ⚠️ **Ties/dotted alphaTex tokens are unverified** (see `alphatex-syntax-reference.md`). The quantizer models ties as slot metadata but the MVP patterns never produce them; the renderer **throws** if a tie slot ever reaches it rather than emit an unverified token.

---

## 6. Persistence (`Infrastructure/`)

| Type | Role |
|------|------|
| `ProgressionOrigin` (enum) | `BuiltIn` (ships with the app) / `UserDefined` (pro tier — enforcement is a Features/licensing concern, not domain). |
| `ProgressionEntity` | EF entity: `Id` (string PK — slug for built-ins, GUID for user), `Name`, `Dsl` (canonical Nashville string — v1 serialization), `Origin` (stored by name), `CreatedUtc`. |
| `ChordFlowDbContext.Progressions` | `DbSet<ProgressionEntity>`; `Origin` stored `HasConversion<string>()`. |
| `ChordFlowDbContext.SeedBuiltInProgressions()` | Idempotent first-run seeding: inserts `SeedData.BuiltInProgressions` rows missing by `Id`; never touches existing or user rows. Called from `Program.cs` after `Migrate()`. |
| Round-trip | Load row → `ProgressionParser.Parse(Dsl)` → `Progression` → `Transposer.Realize` → `AlphaTexRenderer.Render`. `Dsl` is the only persisted form; alphaTex is never stored. |

---

## 7. The unifying object & pipeline

`Exercise(Key, Progression, RhythmPattern Rhythm, int Tempo, Difficulty, Feel = Straight)` — the persisted definition (SQLite stores definition fields only; alphaTex is regenerated on load, never stored).

```
Exercise
  → Transposer.Realize (progression → IReadOnlyList<RealizedBar> — chord + ticks per span)
  → VoicingBook strategy (voicings)   OR  LeadTargets (solo targets → fretboard)
  → FeelTransform (apply rhythm + feel; identity for Straight)
  → RhythmQuantizer (→ slots, split at beat lines AND chord-span boundaries)
  → AlphaTexRenderer (per slot: SpanCovering(StartTick) → chord group → alphaTex)
  → alphaTab
```

---

## 8. Invariants worth remembering

- **C1** PPQ fixed at 48. **C2** only `AlphaTexRenderer` knows alphaTex; quantizer + spelling live in the Domain/Rendering seam. **C3** domain kernel pure + unit-tested. **C4** Feel never stored in a pattern; spelling never derived — both derived. **C5** the 8 quality interval sets.
- **Timing/harmony separation:** `RomanDegree` is always timing-free. Chord-change timing lives exclusively on `ChordSpan.DurationTicks` on the 48-PPQ grid.
- **Two degree frames:** `RomanDegree`/`ScaleDegree` (key-relative) vs `ChordTone` (chord-relative). Don't conflate.
- **v1 render constraint:** only quarter-aligned span boundaries (durations ∈ {48, 96, 144, 192} in 4/4). Sub-quarter and off-beat (syncopated) boundaries are domain-legal but deferred.
- **4/4 only** for v1; no tuplets; no sub-quarter equal divisions; no accuracy detection; no lead-training UI.

Related: [[alphatex-syntax-reference]], [[alphatab-js-api-reference]].