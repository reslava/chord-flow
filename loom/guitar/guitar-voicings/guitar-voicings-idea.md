---
type: idea
id: id_01KTXEQ5F1R316J3RCF5CMBDTW
title: Guitar voicings — the fourth content pillar (authored, stored, movable)
status: done
created: 2026-06-12
updated: 2026-06-18
version: 8
tags: []
parent_id: null
requires_load: []
---
# Guitar voicings — the fourth content pillar (authored, stored, movable)

## The idea

This is the **guitar instrument adapter's authored-voicing layer** — it lives in the
`guitar` weave, and its code moves to `Instruments/Guitar/` per [[instrument-boundary]].
A "voicing" here is a concrete **fret shape**, not an instrument-agnostic concept (see the
deferred abstract-voicing note in *Out of scope*).

Today voicings are **strategy-generated** (`BeginnerShellStrategy`), not authored.
Adding **CRUD** makes voicings **data** — exactly like `Progression` / `Song` /
`RhythmPattern`. This is the **fourth content pillar**, same pattern: a DSL, an
entity, stored-first lookup, pack-distributable.

> **Principle:** a voicing is authored **once at a canonical C anchor** and is
> **inherently movable** — the engine transposes it to all 12 roots. `Dmaj`,
> `Emaj`, … are never authored; they're `Cmaj` slid up the neck. **"Open" is not
> a separate kind of voicing** — it's just where a movable shape lands: open
> strings become a barre when slid up (`x32010` C-shape Cmaj → `x53232` Dmaj),
> and a barre shape slid down opens up (`875558` G-shape Cmaj → `320003` Gmaj).

## Locked decisions (from `exercises-definition-ui-chat-001`)

- **Single C-anchored, inherently-movable model.** Author the CAGED *variants* of
  each quality at C (C-shape, E-shape, G-shape…) with concrete frets read
  straight off a chord box; realize transposes to any root.
- **Every voicing is movable — no `fixed` flag.** "Open" is a *position*, not a
  kind: sliding a shape up turns open strings into a barre (the correct chord),
  and sliding down opens it up. The one genuine non-movable case — open
  drone/pedal tones that hold pitch under transpose — overlaps with alternate
  tunings and is deferred (additive metadata, not a v1 concept).
- **`VoicingBook.Lookup` = stored-first, strategy-fallback.** A stored authored
  voicing for the chord's quality (realized at its root) **shadows** the
  generated one — same "stored shadows generated" rule as song's
  locals-shadow-stored.
- **DSL = standard chord-chart fret notation**, mapping onto the existing
  `Voicing(Positions, BarreFret?, FirstFret?, MutedStrings?)` + `FretPosition` +
  `Fretboard`.
- **Realize math reuses `PitchClass` + `Fretboard`** — no first-class `Interval`
  type needed (that's the deferred `domain/intervals` thread).

## Voicing entry shape (DSL)

```
voicing Cmaj  shape:C  root:5  frets: x 3 2 0 1 0
voicing Cmaj  shape:E  root:6  frets: 8 10 10 9 8 8
voicing Cmaj  shape:G  root:6  frets: 8 7 5 5 5 8
voicing Cmin  shape:A  root:5  frets: x 3 1 0 1 3
```

`VoicingEntity(Id, Name, Dsl, Origin, Genre?, CreatedUtc)` — DSL-only, mirrors
`ProgressionEntity`; adopts catalog metadata + provenance from the `packages` thread.

## In scope (first slice)

- The voicing DSL + parser (fixed-fret chord-chart notation, `shape`/`root`
  metadata), normalized to a canonical C anchor on save.
- `VoicingEntity` + CRUD UI (edit DSL + name, live preview/parse-error surface —
  uniform with the other DSL-backed entities).
- `Realize(entry, targetRoot)` + `VoicingBook` stored-first integration.
- Enables the **"extended voicing books"** pack type.

## Out of scope (deferred — additive)

- Difficulty-band heuristics over the ranked shape list (which/how many to
  surface beyond "lowest fit + next region").
- `QualitySimplifier` (`maj13→maj7→maj` reduction) as an opt-in "level / simplify
  chords" transform upstream of `Lookup` — keeps `Lookup` exact-quality. Reserved seam.
- Open drone/pedal voicings (open strings that hold pitch under transpose) —
  overlaps alternate tunings.
- Alternate tunings (the `Fretboard` is fixed-tuning in v1).
- Pitched lead/target-note voicings (the `domain/intervals` + LeadTargets work).
- **Abstract (instrument-agnostic) voicings** — chord-tone selection + register/octave/
  inversion → pitches, independent of frets. Not modeled today and not required by the
  current roadmap; would become a `domain/voicings` substrate only when a second
  instrument or the agnostic-notation path needs it (origin: chat `general-chat-005`).
  Deferred per the no-speculative-abstraction principle.

Related: [[instrument-boundary]], [[chordflow-domain-model-reference]], [[design-philosophy-durable-over-minimal]], the `packages` thread, `domain/intervals`.
