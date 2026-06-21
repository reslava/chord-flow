---
type: design
id: de_01KVP2BZJANXQR4TD9DQ4DVECH
title: Rename Domain → Music — Design
status: done
created: 2026-06-21
updated: 2026-06-21
version: 3
tags: []
parent_id: id_01KVGH7P05NX5W8RKTMVRGBTZ6
requires_load: []
---
# Rename Domain → Music — Design

## Goal

Replace the single grab-bag `ChordFlow.Domain` namespace / `Domain/` folder with a set of
**concept-named, flat-sibling** namespaces that say what the code *is*. This is the idea's
**scope (b)** — a real reorganization, not a pure rename — chosen because the existing
`Domain/` already mixes pure theory with progression/song structure and exercise-generation
types, and "Music" only earns its name if the non-theory types move out.

Pure structure/naming. **No behavioral change** — every type keeps its shape; only its
namespace and file location change, plus the `using`s and docs that reference them.

## Decisions (settled in `domain-to-music-rename-chat-001`)

1. **Flat siblings, never nested.** Namespace nesting means "is-part-of"; these concepts are
   peers with a one-way dependency, so they are siblings under `Music`, not nested
   (`Music.Song.Progression` was rejected — an `Exercise` uses a `Progression` with no `Song`,
   so nesting would force the wrong coupling).
2. **Keep the `ChordFlow.` root prefix** on every namespace — `ChordFlow.Music.Harmony`, not
   bare `Music.Harmony`. Consistent with the `ChordFlow.Core` assembly and `ChordFlow.Exercises`.
3. **Namespaces + folders only — single assembly.** Everything stays inside `ChordFlow.Core`;
   no new `.csproj`. Boundaries are enforced by the existing architecture test plus new
   namespace-layering assertions, not by project references.
4. **DSL parsers/writers live *with the type they parse*** — never a horizontal `Dsl/` or
   `Parsers/` bucket. A DSL is a textual surface for a concept, so the parser is cohesive with
   the type. (alphaTex is the deliberate exception: it is render *output*, stays in `Rendering/`.)
5. **An interface lives in the kernel only when the kernel depends on it** (port vs repository).
   `IProgressionStore` is a true **port** — pure-domain `SongExpander` needs it while staying
   I/O-free (constraint C3) — so it moves with its consumer into `Music.Song`. The concrete
   content stores (`ProgressionStore`/`SongStore`/`RhythmPatternStore`/`VoicingStore`) are
   repositories with no kernel consumer and correctly stay interface-free in `Persistence/`.
6. **Documentation is in-scope, as explicit plan steps** — the three `loom/refs/` docs, `ctx.md`,
   the README DSL section, XML-doc cross-references, and the architecture test, each updated in
   the same unit of work (ref-sync contract).

## Target namespace map

Flat siblings under `ChordFlow.Music`, plus the existing `Instruments.Guitar`, plus a new
top-level `ChordFlow.Exercises` for the non-theory types pulled out of the old kernel:

```
ChordFlow.Music.Harmony       static theory: pitch, chords, scales, qualities, spelling, transposition
ChordFlow.Music.Rhythm        the 48-PPQ tick-grid rhythm model + feel/accent/stroke overlays
ChordFlow.Music.Melody        lead/solo target zones (pitch-class guide tones)
ChordFlow.Music.Progression   harmony over time: progressions, harmonic bars/spans
ChordFlow.Music.Song          form over progressions: song, expansion, modulation + the IProgressionStore port
ChordFlow.Instruments.Guitar  (unchanged — already its own boundary)
ChordFlow.Exercises           the composed practice unit + its generation params
```

Dependency direction (a DAG, no cycles — each depends only on the ones above it):

```
Exercises ──► Song ──► Progression ──► Harmony
                                  ▲           ▲
              Rhythm ─────────────┘           │   (Rhythm is independent of Harmony)
              Melody ─────────────────────────┘   (Melody depends on Harmony)
Instruments.Guitar ──► Music.*   (the guitar adapter consumes theory; never the reverse)
```

## Full type → namespace move table

Every current `src/ChordFlow.Core/Domain/**` type and its new home. Folder mirrors namespace
(`Domain/` → `Music/Harmony/`, `Music/Rhythm/`, …; `Exercises/` at Core root).

| Current `Domain/` file | New namespace |
|---|---|
| PitchClass, Key, Chord, ChordSymbol, ChordTone, ChordTones, RomanDegree, Scale, ScaleDegree, DiatonicChord, Quality, QualityFormulas, QualityIntervals, NoteSpeller, IntervalSpeller, Transposer | `ChordFlow.Music.Harmony` |
| TickGrid, TimeSignature, Stroke, Accent, RhythmEvent, PickupMeasure, Feel, FeelTransform, AccentPattern, StrokeOverlay, RhythmPattern, **RhythmPatternParser** | `ChordFlow.Music.Rhythm` |
| TargetZone, Importance, LeadTargets | `ChordFlow.Music.Melody` |
| Progression, HarmonicBar, ChordSpan, **ProgressionParser** | `ChordFlow.Music.Progression` |
| Song/Song, Song/SongParser, Song/SongExpander, Song/RealizedSong, Song/Modulation, **Song/IProgressionStore** | `ChordFlow.Music.Song` |
| Exercise, Difficulty | `ChordFlow.Exercises` |

DSL parsers (bold above) move *with their type*, confirming decision 4: `ProgressionParser`
→ Progression, `RhythmPatternParser` → Rhythm, `SongParser`/`SongExpander` → Song.

The `Features/`, `Rendering/`, `Bridge/`, `Persistence/` areas keep their namespaces; they
change only by updating their `using ChordFlow.Domain;` to the specific new namespace(s) each
file actually uses.

## Open decision — where does `SeedData` go?

`SeedData` is hand-authored MVP constants (the 12-bar blues `Progression`, three
`RhythmPattern`s, the 12 keys) used by rendering and tests; persisted built-in content now
ships as the on-disk default pack, so `SeedData` is increasingly a **dev/test fixture** that
straddles Harmony + Rhythm. It fits no single `Music.*` namespace cleanly. Options:

- **(i)** keep it as one static class — pick the dominant home (it is mostly a `Progression`,
  so `Music.Progression`), accept the cross-references.
- **(ii)** split it per concept (blues progression → Progression, patterns → Rhythm, keys →
  Harmony).
- **(iii)** treat it as a fixture and move it to a test/seed area (e.g. alongside `Packs` or
  into the test project), since production content comes from packs.

Recommendation: **(iii)** if it turns out nothing in `src/` still depends on it at runtime;
otherwise **(i)**. Decide during planning after a quick consumer check — not a blocker.

## Architecture-test impact

- `tests/.../Architecture/InstrumentBoundaryTests` pins the string `ChordFlow.Domain`
  (no `ChordFlow.Domain` type may reference `ChordFlow.Instruments`). Retarget it to
  `ChordFlow.Music` (the kernel edge is now the whole `Music.*` family).
- Keep `Rendering → Instruments` and `Persistence → Instruments` allowed (unchanged).
- **Optionally add** layering assertions for the new DAG: `Music.Harmony` references no other
  `Music.*`; `Music.Progression → Harmony` only; `Music.Song → Progression`/`Harmony`;
  `Music.Rhythm` independent of Harmony. This is the durable payoff of the split — the new
  boundaries become compiler-checked, not conventions. (Flag as its own step; can be deferred
  if it balloons.)

## Documentation update scope (explicit plan steps)

Per the ref-sync contract, in the same unit of work:

- `loom/refs/chordflow-architecture-reference.md` — §2 solution shape, §3 layer descriptions,
  §7 the theory↔instrument boundary section (all `Domain/` → `Music/…`).
- `loom/refs/chordflow-domain-model-reference.md` — the kernel map (every `ChordFlow.Domain`
  reference).
- `loom/refs/chordflow-dsl-reference.md` — any namespace mention.
- `loom/ctx.md` — §2 architecture bullet (`Domain/` description).
- `README.md` / `CHANGELOG.md` — DSL section + any `ChordFlow.Domain` mention.
- XML-doc `<see cref="Domain.*"/>` cross-references throughout the source.

## Validation

- Full solution builds; **all tests green**; `loom_validate` clean.
- The architecture test passes against the retargeted `ChordFlow.Music` edge.
- Grep proves zero remaining `ChordFlow.Domain` / `namespace ChordFlow.Domain` / `Domain/`
  references in `src/`, `tests/`, the three refs, `ctx.md`, and `README`.
- Done as its **own isolated commit**, never riding along with feature work.

## Out of scope

- Any behavioral change — pure naming/structure.
- Splitting `ChordFlow.Core` into multiple assemblies (decided against; namespaces suffice).
- Moving the `Features/` exercise slices (`GenerateExercise`, `ExerciseRendering`, …) — they
  stay; only their `using`s update.

Related: [[chordflow-architecture-reference]], [[chordflow-domain-model-reference]].