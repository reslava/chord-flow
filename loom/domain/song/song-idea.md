---
type: idea
id: id_01KTVTKHDVQYNXYB33HXH0HXAS
title: Song — an arrangement layer over Progressions
status: done
created: "2026-06-11T00:00:00.000Z"
updated: 2026-06-12
version: 2
tags: []
parent_id: null
requires_load: []
---
# Song — an arrangement layer over Progressions

## The idea

We have **Progressions** (key-independent, reusable, timing-aware *within* bars). A
**Song** is the next layer up: an **arrangement/composition** of progressions —
*not* a bigger progression and *not* a container of bars/chords.

> A Song is a **graph of references to Progressions plus arrangement instructions**
> (repetition, modulation, section order). Harmony stays in the Progression;
> the Song only composes.

This preserves the layering we already established:

```
Song        -> arrangement (order, repeats, key changes)
Progression -> harmony (key-independent)
RhythmPattern -> timing
Voicing     -> guitar realization
Renderer    -> notation / audio
```

## Why we want it

A real practice piece isn't one progression looped — it's `intro · verse ×2 ·
chorus ×2 · bridge · chorus ×3`, sometimes with a key change. Without a Song layer
we'd have to duplicate bars and bake keys in, which destroys reuse. With it:

- **Reuse** — one `blues`/`ii-V-I` progression referenced from many sections and songs.
- **Structure** — labelled sections (Verse/Chorus/Bridge) that the play cursor can surface.
- **Repetition** — first-class (`verse x2`), not copy-paste.
- **Modulation** — explicit, musical key changes between sections.

## Shape (concept level — detail in the design)

A Song is an ordered stream of **arrangement items**:

- a **part reference** (a named progression — stored-by-id *or* inline-defined) with an optional repeat count, and
- a **modulation** instruction between parts.

Realization is a left-to-right fold that carries a *running key*; each part is
realized with `Transposer.Realize(progression, currentKey)`. The output is a
**`RealizedSong`** — a list of labelled, keyed `RealizedSection`s, each a list of
`RealizedBar`s — which the existing renderer consumes section-by-section. A new
`SongExpander` slots in **above** `Transposer`; nothing below it changes.

## DSL sketch

```text
key C

A = 17 17 47 17        # inline local progression
B = 2-7 57 1maj7
C = 67 27 57 17

A x2
B x2
mod V                   # modulate to the dominant (relative)
C
B x3
```

`A`/`B`/`C` may also be references to **stored** progressions (`verse: blues`).

## Locked design decisions (from `song-chat-001`)

- **A — modulation lives at the arrangement layer only.** It changes the
  *realization key* going forward (stateful fold); the Progression is never
  mutated. A degree-rewriting `transpose` is a **future transform**, not part of Song.
- **C — modulation is relative + absolute.** Relative (`mod V`, `mod +2`,
  `mod bIII`) is the musical default; absolute (`key G`) is the reset / escape
  hatch (relative-only can't cleanly "return home" because a pure fold accumulates).
- **D — Song stays pure harmony + arrangement.** Rhythm / voicing / tempo / feel
  are **not** part of a Song; they attach at play time via a `SongExercise`
  (`Song + RhythmPattern + Difficulty + Feel + Tempo`) — the direct analog of
  today's `Exercise`. This keeps a Song reusable across rhythm settings.
- **`x4` is the only section-repeat syntax**; `@repeat(n)` is reserved for the
  (future) bar-expansion transform — they produce different structures.
- **`mod` is a stream token** between parts, not a section attribute;
  `RealizedSection.Key` is an *output* of the fold, never an input.
- **Locals shadow stored names** — a `bare` name resolves local-first, then store.

## In scope (first slice)

- `Song` domain model + guarded `Song.FromSections(...)` factory.
- Reference resolution (stored-by-id + inline) and the modulation fold → `RealizedSong`.
- `SongParser` (peer of `ProgressionParser`) for the Song DSL.
- `SongEntity` persistence parity with `ProgressionEntity` (Dsl is the only stored form).
- A seeded example song + a public DSL reference doc.

## Out of scope (deferred)

- **Progression transforms** (transpose, dominantize, jazzify, …) — their own
  `domain/transforms` thread. The Song DSL leaves an `@op` slot for them.
- Repeat endings (1st/2nd), D.C./D.S. al coda, per-section time signatures.
- Multi-meter songs (v1 inherits the single 4/4 time signature).

Related: [[chordflow-domain-model-reference]], the `progression` thread.