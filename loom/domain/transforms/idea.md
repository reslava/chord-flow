---
type: idea
id: id_01KTVTM1797WBJ8TF9K7B4VPTR
title: Progression Transforms — functional rewrites of Progressions
status: done
created: 2026-06-11
updated: 2026-06-22
version: 2
tags: []
parent_id: null
requires_load: []
---
# Progression Transforms — functional rewrites of Progressions

## The idea

A **transform** is a pure, functional operation on a Progression — the harmonic
analog of how `FeelTransform` / `AccentPattern` / `StrokeOverlay` operate on
rhythm: **never mutate the base, return a new value.**

```text
Progression
    ↓  Transform(s)
Progression
    ↓  Transposer.Realize(key)
RealizedBar[]
```

A transform never renders, never knows about voicings, never knows about
alphaTex. The contract:

```csharp
interface IProgressionTransform
{
    Progression Apply(Progression progression);
}
```

A small number of transforms are genuinely **key-aware** (they resolve relative
to the realization key); those take an overload `Apply(Progression, Key)`. Most
operate on `RomanDegree`s and are **key-independent**, so the pure signature is
the default. Transforms **compose left-to-right** and are **not commutative**
(`transpose` then `dominantize` ≠ the reverse), so application order is part of
the DSL contract.

## Why a separate thread

This was carved out of the `song` thread deliberately: get **Song** realizing
correctly first (references + repetition + modulation fold → `RealizedSong`),
then add transforms as a clean **additive** layer. They slot into the Song DSL's
`@op` slot and into a `SongPart`'s transform list **without reworking the
timeline**. Keeping them separate keeps each slice small and shippable.

## The key insight — three different buckets

The raw brainstorm (in `song-chat-001`) conflated three kinds of operation.
Naming the split is the core of this idea — only bucket 1 is actually a
`IProgressionTransform`:

### 1. True harmonic rewrites — `Progression → Progression` (this thread)

transpose · dominantize · jazzify · triads↔sevenths · simplify · minorize ·
relative-minor · tritone-sub · reverse · take / skip / loop · double / halve
harmonic rhythm (operates on `ChordSpan` durations) · turnaround injection ·
sequence · cycle-of-fifths · walk-up / walk-down.

### 2. Arrangement ops — Song layer, **not** transforms

repeat (section) · modulate. These operate on the *timeline*, not a progression,
and already live in the `song` thread.

### 3. Practice-representation generators — lead / voicing layer, output is **not** a Progression

guide-tone version · chord-tone focus · shell-voicing version · dominant-only.
These change *what the student plays*, not the harmony — they belong with
`LeadTargets` / `VoicingBook`, not `IProgressionTransform`.

## Priority set (first transform slice, when we get here)

Eight transforms that give a lot of musical value while staying fully compatible
with the existing immutable `Progression → RealizedBar` pipeline:

```
repeat   take   skip   reverse   transpose   dominantize   triadsToSevenths   turnaround
```

All bucket-1 (`@repeat` here is the bar-expansion transform, distinct from Song's
section-level `x4`). The more advanced ideas (sequence, tritone-sub, jazzify) come
later as rule engines on top of the same infrastructure.

## DSL sketch

```text
blues @repeat(2)
ii-v-i @transpose(2)
turnaround @dominantize
rhythm_changes @take(8)
```

Composable, applied left-to-right:

```text
ii-v-i @transpose(2) @repeat(4) @dominantize
```

## Status

Idea only — captured so the taxonomy isn't lost. No design/plan until the `song`
thread lands and we choose to start it.

Related: [[chordflow-domain-model-reference]], the `song` thread, the `progression` thread.