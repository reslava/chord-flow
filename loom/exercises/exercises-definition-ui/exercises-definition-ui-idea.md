---
type: idea
id: id_01KTWE71HWW2GWFHQQ0QMR84K6
title: Exercise definition & UI — the capstone over Harmony + Rhythm
status: done
created: "2026-06-11T00:00:00.000Z"
updated: 2026-06-15
version: 2
tags: []
parent_id: null
requires_load: []
---
# Exercise definition & UI — the capstone over Harmony + Rhythm

## The idea

With the two pillars refined — **Harmony** (`Progression` → `Song`) and
**Rhythm** (`RhythmPattern` DSL) — `Exercise` is the keystone that composes them
into something the user practices. This thread refines **what an Exercise is** and
the **UI** to define and play one.

> **Principle:** an Exercise is a small bundle of **references** (a Song, a
> comping pattern, an optional lead pattern) plus **playback params** with saved
> defaults. Definition is references; params are values. Everything realizes
> through the one pipeline we already have.

## Locked decisions (from `exercises-definition-ui-chat-001`)

- **Harmony slot = `Song` only.** A bare progression is **trivially lifted** to a
  single-section Song (`Song.OfProgression(prog)`), so simple drills stay simple
  *and* there's **one realization path** (`SongExpander → RealizedSong → render`)
  with no `Progression`-vs-`Song` branching. `Progression` stays a first-class
  reusable/CRUD entity that Songs reference.
- **Two fixed tracks, not a list:** `Comping` (rhythm guitar, required) +
  `Lead` (optional). Maps 1:1 onto the two-track play view. Per-section rhythm is
  **deferred** (consistent with `song` decision D — one rhythm per song);
  multi-bar patterns already give bar-to-bar variation within a track.
- **Definition vs params split.** Definition = Song + Comping + Lead. Params =
  Key / Tempo / Difficulty / Feel — saved as defaults, editable live before
  **Generate**.
- **Key = a transpose override, not a second key.** The Song owns harmonic key
  (`InitialKey` + modulations); Exercise's Key param re-anchors the whole song and
  **defaults to `Song.InitialKey`**. Avoids two sources of truth.
- **Target notes postponed; lead = dead notes now.** v1 lead track is
  dead/muted notes on the `Lead` `RhythmPattern` grid (rhythm only, no pitch),
  reusing the **same** `RhythmPattern` type. Pitched targets later via the
  existing `LeadTargets` seam (scale / chord-tones / guide-tones / arpeggios).
- **Player settings are user prefs, not exercise definition** — count-in,
  metronome on/off, rhythm-volume, lead-volume are alphaTab *player* config (how
  you listen), not part of what the Exercise *is*.

## Exercise shape

```csharp
Exercise(
    Song Song,                  // harmony + arrangement
    RhythmPattern Comping,      // rhythm-guitar track
    RhythmPattern? Lead,        // lead-guitar track (v1 = dead notes)
    Key? KeyOverride,           // null → Song.InitialKey; else global transpose
    int Tempo,
    Difficulty Difficulty,
    Feel Feel = Straight);      // + Swing% later
```

`ExerciseEntity` stores **references** (`SongId`, `CompingPatternId`,
`LeadPatternId?`) + param columns — today's `ProgressionId` becomes `SongId`.

## UI — three distinct layers

1. **Definition / CRUD** — `Progression`, `Song`, `RhythmPattern` (each "edit a
   DSL + name"; Target notes postponed).
2. **Exercise params** — Key / Tempo / Difficulty / Feel(+Swing%), saved defaults,
   editable live → **Generate** → alphaTex.
3. **Player settings** (user prefs) — count-in, metronome, per-track volumes.

**Play view:** two tracks — rhythm guitar (chord names on change + chord diagrams
above, from `Voicing` metadata) and lead guitar (when `Lead` is defined).

## In scope (first slice)

- `Exercise` refactor to the shape above + `Song.OfProgression` lift.
- `ExerciseEntity` refactor (`ProgressionId → SongId`, add `Comping`/`Lead`
  pattern refs, param columns) + EF migration.
- **2-track alphaTex** rendering; **dead-note** lead track (alphaTex `x.3` /
  `3.3{x}` — confirmed).
- The three UI layers / screens.

## Out of scope (deferred — additive)

- Pitched **target notes** (scale / chord-tones / guide-tones / arpeggios) via
  `LeadTargets`.
- Per-section rhythm; continuous `SwingPercent` (enum is the v1 simplification).
- Anything song-arrangement or transform-related (their own threads).

Related: [[chordflow-domain-model-reference]], [[chordflow-architecture-reference]], [[design-philosophy-durable-over-minimal]], the `song` & `rhythm` threads.