---
type: design
id: de_01KTXERD54E8GFPPNE19GMCPB1
title: Voicings — the fourth content pillar (authored, stored, movable)
status: draft
created: 2026-06-12
version: 1
tags: []
parent_id: id_01KTXEQ5F1R316J3RCF5CMBDTW
requires_load: []
---
# Voicings — the fourth content pillar (authored, stored, movable)

Design for the authored-voicing content pillar: the DSL, `VoicingEntity`, the
`Realize` transpose, and stored-first `VoicingBook` integration. Builds on
[[chordflow-domain-model-reference]] (the existing `Voicing` / `FretPosition` /
`Fretboard` / strategy). Decisions in `exercises-definition-ui-chat-001`.

> **Stance** ([[design-philosophy-durable-over-minimal]]): one canonical authored
> form (movable by default), reusing the existing `Voicing` value type as
> `Realize`'s output — no new downstream types.

---

## 1. DSL

```
voicing <Chord>  shape:<C|A|G|E|D|…>  root:<6..1>  frets: <s6 s5 s4 s3 s2 s1>  [fixed]
```

- `<Chord>` = a canonical-anchor chord (convention: **C**), e.g. `Cmaj`, `Cmin`,
  `Cmaj7`, `C7`. The **quality** is what `Lookup` matches; the root pitch (C) is
  the transpose anchor.
- `frets` = absolute frets at the anchor, strings 6→1; `x` = muted, `0` = open.
- `shape:` = the CAGED family (metadata: diagram labelling + author legibility).
- `root:` = the string sounding the root — for chord-diagram root marking + the
  octave-window heuristic.
- `fixed` = authored position only; never transposed (open/ringing voicings).

Parses onto the existing
`Voicing(IReadOnlyList<FretPosition> Positions, int? BarreFret, int? FirstFret, IReadOnlySet<int> MutedStrings)`.

## 2. Realize — movable transpose

```csharp
Voicing? Realize(VoicingShape entry, PitchClass targetRoot)
{
    int semis = PitchClass.Interval(from: entry.AnchorRoot /*C*/, to: targetRoot); // 0..11
    // add semis to every fretted string; x stays muted
    // octave-fold (±12) so the lowest fretted note lands in the 0..15 window
    // → null if no octave placement fits
    // BarreFret / FirstFret derived from the lowest fretted fret
}
```

- **0–15 fret guard**; octave-fold to find a placement; return null if none.
- `fixed` entries skip transpose — returned only when `targetRoot == AnchorRoot`.

## 3. `VoicingBook.Lookup(chord, difficulty)` — stored-first

```
1. stored entries whose quality == chord.Quality
     → Realize(entry, chord.Root) for each
     → keep playable (fit 0–15), order by position
     → return up to 2 (lowest fit + next region/octave)
2. else strategy fallback (BeginnerShellStrategy) — as today.
```

Stored authored voicings **shadow** generated ones for the same chord (same rule
as song's locals-shadow-stored). Difficulty narrows the candidate set / position
band (refinement deferred).

## 4. Persistence — `VoicingEntity`

```csharp
record VoicingEntity(
    string Id,
    string Name,
    string Dsl,           // canonical
    string Origin,        // BuiltIn / UserDefined / Pack:<id>  (packages thread)
    string? Genre,        // catalog metadata (packages thread)
    DateTime CreatedUtc);
```

DSL-only (frets regenerated from DSL on load) — mirrors `ProgressionEntity`.
Adopts catalog metadata + `Origin` from the `packages` thread. New `Voicings`
table + EF migration.

## 5. UI

A **CRUD** screen uniform with `Progression` / `Song` / `RhythmPattern`: edit DSL
+ name, live preview (rendered chord diagram from `Voicing` metadata) +
parse-error surface.

## 6. Placement & dependency direction

- DSL parser + `Realize` + `VoicingBook` integration → **`ChordFlow.Core/Domain/`**
  (voicings) — pure, reuses `PitchClass` + `Fretboard`.
- `VoicingEntity` + migration → **`ChordFlow.Core/Persistence/`**.
- CRUD screen → **`ChordFlow.Desktop`** (`wwwroot`). Desktop → Core unchanged.

## 7. Explicitly deferred (additive)

- Difficulty-band selection heuristics beyond "lowest fit + next".
- Alternate tunings (fixed-tuning `Fretboard` in v1).
- Pitched target-note voicings (`domain/intervals` + LeadTargets).
- Movable-shape *abstraction* refinement once `domain/intervals` lands (intervals
  could re-express shapes as interval stacks).

## 8. Open implementation questions (decide at plan time)

1. Canonical anchor — hard-fix to C, or allow any declared anchor chord (engine
   normalizes)? Leaning **allow any**, C as convention.
2. "Up to 2" position selection — lowest + octave, or two distinct CAGED
   families? Leaning lowest fit + next playable region.
3. Quality matching granularity (does `maj7` fall back to `maj`?) — leaning
   exact-quality, strategy covers the gap.

Related: [[chordflow-domain-model-reference]], [[design-philosophy-durable-over-minimal]], the `packages` thread, `domain/intervals`.