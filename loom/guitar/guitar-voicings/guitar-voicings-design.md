---
type: design
id: de_01KTXERD54E8GFPPNE19GMCPB1
title: Guitar voicings — the fourth content pillar (authored, stored, movable)
status: done
created: 2026-06-12
updated: 2026-06-18
version: 11
tags: []
parent_id: id_01KTXEQ5F1R316J3RCF5CMBDTW
requires_load: []
---
# Guitar voicings — the fourth content pillar (authored, stored, movable)

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
voicing <Chord>  shape:<C|A|G|E|D|…>  root:<6..1>  frets: <s6 s5 s4 s3 s2 s1>
```

- `<Chord>` = the anchor chord (authoring convention: **C**), e.g. `Cmaj`, `Cmin`,
  `Cmaj7`, `C7` — any anchor is accepted and normalized to C on save. The
  **quality** is what `Lookup` matches; the root pitch is the transpose anchor.
- `frets` = absolute frets at the anchor, strings 6→1; `x` = muted, `0` = open.
- `shape:` = the CAGED family (metadata: diagram labelling + author legibility).
- `root:` = the string sounding the root — for chord-diagram root marking + the
  octave-window heuristic.
- **Stored canonical-C:** whatever anchor is declared, the parser normalizes the
  voicing to its lowest non-negative **C** placement before save (dedup key =
  `(quality, shape)`); `Realize` transposes on demand. "Open" is just where a
  shape lands — no `fixed` concept.

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
- **Normalize-on-save** is the mirror of this: store at the *lowest non-negative
  C placement* (some shapes — e.g. the D-shape — sit below the nut at C and must
  octave-fold up), so each `(quality, shape)` has one canonical record.

## 3. `VoicingBook.Lookup(chord, difficulty)` — stored-first

```
1. stored entries whose quality == chord.Quality
     → Realize(entry, chord.Root) for each
     → keep playable (fit 0–15)
     → return the full ranked list (sort: neck position; tiebreak: CAGED
       familiarity rank E A G C D, pack-overridable metadata)
2. else strategy fallback (BeginnerShellStrategy) — as today.
```

Stored authored voicings **shadow** generated ones for the same chord (same rule
as song's locals-shadow-stored). Matching is **exact-quality** — `maj7` never
silently returns `maj` (simplification is the separate opt-in `QualitySimplifier`,
§7). The engine returns the ranked list; the consumer (exercise / UI / difficulty
band) takes N — "up to 2" is an edge-side filter, not an engine cap.

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

DSL-only (frets regenerated from DSL on load) — mirrors `ProgressionEntity`. The
stored DSL is the **canonical-C** form (normalized on save; dedup key
`(quality, shape)`), so `Dmaj`/`Emaj`/… never appear in the table. Adopts catalog
metadata + `Origin` from the `packages` thread. New `Voicings` table + EF migration.

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

- Difficulty-band heuristics over the ranked shape list (which / how many to surface).
- `QualitySimplifier` — opt-in `maj13→maj7→maj` reduction applied to the *chord*
  upstream of `Lookup` (the "level / simplify chords" feature); keeps `Lookup`
  exact and makes simplification a reusable, intentional transform. Reserved seam.
- Open drone/pedal voicings (open strings holding pitch under transpose) — overlaps
  alternate tunings.
- Alternate tunings (fixed-tuning `Fretboard` in v1).
- Pitched target-note voicings (`domain/intervals` + LeadTargets).
- Movable-shape *abstraction* refinement once `domain/intervals` lands (intervals
  could re-express shapes as interval stacks).

## 8. Resolved decisions (voicings-chat-001)

1. **Canonical anchor — any anchor accepted, normalized to C on save.** Authoring
   convention is C; the engine normalizes whatever anchor is declared to the
   lowest non-negative C placement and dedups on `(quality, shape)`. No duplicate
   `Dmaj`/`Emaj` records — `Realize` transposes at render.
2. **No `fixed` flag — every voicing is movable.** "Open" is just where a shape
   lands (open ↔ barre under transpose), not a separate form. Open drone/pedal
   tones (the one true non-movable case) are deferred (§7).
3. **Selection returns the full ranked list, not a hard cap.** Sort by neck
   position; tiebreak by CAGED familiarity rank (E A G C D, pack-overridable
   metadata). "Up to N" is a consumer-side filter; difficulty bands narrow the
   list (deferred).
4. **Quality matching is exact.** `maj7` never silently returns `maj`;
   simplification is the separate opt-in `QualitySimplifier` (§7), not baked into
   `Lookup`.

Related: [[chordflow-domain-model-reference]], [[design-philosophy-durable-over-minimal]], the `packages` thread, `domain/intervals`.