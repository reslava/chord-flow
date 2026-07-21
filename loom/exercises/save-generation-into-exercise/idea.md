---
type: idea
id: id_01KY2NJTXAM9AW3ERPK0JS1V1A
title: Save a generation into an exercise
status: draft
created: 2026-07-21
version: 1
tags: []
parent_id: null
requires_load: []
---
# Save a generation into an exercise

**Deferred (req EX1, Phase 5).** Depends on **Practice integration** (`practice-integration`) — an exercise must first *carry* a generated track before we can persist one.

## Idea

A generation is fully described by **`{strategy, params, seed}`** (req IN6 — deterministic). So persisting one is the app's standard **store-the-definition-regenerate-the-output** pattern (never store the rendered tex/grid): a saved `Exercise` that uses a generated rhythm stores the generation spec for that track, and on load the engine **regenerates** the identical `OnsetGrid` and projects it.

## Shape when picked up

- `ExerciseEntity` / the definition gains a **generation spec** per generated track (the `{strategy, params, seed}` tuple), alongside the existing comping/lead/drums references.
- The Practice **Save** path serializes the spec; the **load** path resolves it through the same generator (via `ExerciseRefs` / `GenerateExercise`) — exactly how progressions/rhythms/voicings already round-trip.
- No engine change to the generator itself (it's already pure + seeded); this is a persistence + resolve-path feature.

Sequence **after** Practice integration lands (that thread defines how a generated track is carried in the exercise, which this then persists).
