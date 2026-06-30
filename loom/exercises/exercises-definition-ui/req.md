---
type: req
id: rq_01KV62KTJFKQZBGJ123S0WVT4X
title: Exercise definition & UI — the capstone over Harmony + Rhythm — Requirements
status: locked
created: 2026-06-15
updated: 2026-06-15
version: 1
design_version: 9
tags: []
parent_id: de_01KTWE8B7WKRX7M681PM4P9JFP
requires_load: []
---
# Exercise definition & UI — the capstone over Harmony + Rhythm — Requirements

### ✅ Included

- `IN1` One canonical `Exercise` record — `Song` (harmony) + `Comping` `RhythmPattern` (required) + optional `Lead` `RhythmPattern` + `KeyOverride`/`Tempo`/`Difficulty`/`Feel`. Decision (a): evolve the shipped `SongExercise` into it and **delete both** the old `Exercise(Key, Progression, …)` and the `SongExercise` name (one play-unit).
- `IN2` `Song.OfProgression` trivial lift — a bare `Progression` is wrapped into a single-section `Song` so there is **one realization path** (`SongExpander → RealizedSong → render`) with no `Progression`-vs-`Song` branching downstream. `Progression` stays a first-class reusable/CRUD entity.
- `IN3` `KeyOverride` as a global transpose — re-anchors the whole song, defaults to `Song.InitialKey`, realized via a new optional `SongExpander.Expand(startKey:)` param.
- `IN4` `ExerciseEntity` refactor — store **references** (`SongId` (was `ProgressionId`), `CompingPatternId`, `LeadPatternId?`) + param columns, plus the EF migration (drop `ProgressionId`/add columns; no data preservation needed).
- `IN5` Two-track alphaTex render — second staff emitting the `Lead` pattern as **dead/muted notes** (`x.3` / `3.3{x}`); stays single-track when `Lead` is null.

### ❌ Excluded

- `EX1` Pitched **target notes** (key scale / per-chord scale / chord-tones / guide-tones / arpeggios via `LeadTargets`) — postponed; v1 lead is dead notes only.
- `EX2` The UI layers (Definition/CRUD, Exercise-params pickers → Generate, the Play/Practice view) — live in the `ui` weave (`content-crud` done, `exercise-workbench`), not built in this thread.
- `EX3` Player settings — count-in, metronome on/off, rhythm-guitar volume, lead-guitar volume — are user prefs, not part of the Exercise definition.
- `EX4` Mode-changing key override (major song practiced in minor) — v1 is tonic transpose only.
- `EX5` Per-section rhythm and continuous `SwingPercent` — the single per-song rhythm + `Feel` enum are the v1 simplification.

### ⛓ Constraints

- `C1` **Definition vs params split** — Definition = references (`Song` + `Comping` + optional `Lead`); Params (`Key`/`Tempo`/`Difficulty`/`Feel`) are values saved as defaults, editable live before Generate.
- `C2` The `Lead` track **reuses the same `RhythmPattern` type** (rendered as dead notes in v1), forward-compatible with the deferred `LeadTargets` pitched seam.
- `C3` **Core-only** — all work lands in `ChordFlow.Core` (`Domain/` / `Persistence/` / `Rendering/`); no UI is built in this thread.