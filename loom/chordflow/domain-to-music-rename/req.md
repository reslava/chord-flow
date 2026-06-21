---
type: req
id: rq_01KVP330W2MF7TK9KZ539B4B0G
title: Rename Domain → Music (theory kernel) — Requirements
status: locked
created: 2026-06-21
updated: 2026-06-21
version: 2
tags: []
parent_id: id_01KVGH7P05NX5W8RKTMVRGBTZ6
requires_load: []
---
# Rename Domain → Music (theory kernel) — Requirements

Authoritative scope for the Domain → Music reorganization (idea scope **(b)**). Anchors the
plan; built against `domain-to-music-rename-design.md`.

### ✅ Included

- `IN1` Replace the `ChordFlow.Domain` namespace with the flat-sibling family from the design:
  `ChordFlow.Music.Harmony`, `ChordFlow.Music.Rhythm`, `ChordFlow.Music.Melody`,
  `ChordFlow.Music.Progression`, `ChordFlow.Music.Song`, and `ChordFlow.Exercises`.
- `IN2` Move every `src/ChordFlow.Core/Domain/**` file to a folder mirroring its new namespace
  (`Music/Harmony/`, `Music/Rhythm/`, `Music/Melody/`, `Music/Progression/`, `Music/Song/`,
  `Exercises/`), per the design's full type→namespace move table.
- `IN3` DSL parsers/writers move *with their type*: `ProgressionParser` → `Music.Progression`,
  `RhythmPatternParser` → `Music.Rhythm`, `SongParser`/`SongExpander` → `Music.Song`.
- `IN4` The `IProgressionStore` **port** moves into `Music.Song` (co-located with its consumer
  `SongExpander`); the concrete content stores stay interface-free in `Persistence/`.
- `IN5` Update every `using ChordFlow.Domain;` and every `<see cref="Domain.*"/>` XML-doc
  cross-reference across `src/` and `tests/` to the specific new namespace(s) each file uses.
- `IN6` Retarget the architecture test (`tests/.../Architecture/InstrumentBoundaryTests`) from
  the `ChordFlow.Domain` edge to `ChordFlow.Music`; keep `Rendering → Instruments` and
  `Persistence → Instruments` allowed.
- `IN7` Update documentation in the same unit of work (ref-sync contract): the three
  `loom/refs/` docs (architecture, domain-model, dsl), `loom/ctx.md`, and `README.md` /
  `CHANGELOG.md`.
- `IN8` Resolve the `SeedData` placement (design open decision) via a quick consumer check:
  if no `src/` runtime consumer remains, move it to a test/seed area; else park it in
  `Music.Progression`.
- `IN9` Add `Music.*` **layering architecture tests** (NetArchTest, beside the retargeted
  `InstrumentBoundaryTests`) that lock in the new boundaries as the durable payoff of the split:
  `Music.Harmony` references no sibling `Music.*`, and there are no dependency cycles among the
  `Music.*` namespaces. Each sibling's allowed outward edges are set to the **real references
  observed after the move** (expected: `Progression → Harmony` — plus `Rhythm` if spans carry
  tick durations; `Song → Progression`/`Harmony`; `Melody → Harmony`; `Rhythm` self-contained).
  The allow-list encodes *observed* edges, never an aspirational DAG — so it confirms the
  structure without forcing any code change (`EX1`).

### ❌ Excluded

- `EX1` Any behavioral change — this is pure naming/structure; every type keeps its shape.
- `EX2` Splitting `ChordFlow.Core` into multiple assemblies — namespaces + folders only.
- `EX3` Moving `src/ChordFlow.Core/Content/default-pack` out of the Core project — a separate
  concern, not part of this thread (runtime resolves it via `AppContext.BaseDirectory`, and the
  on-disk data stays cohesive beside the `Packs`/`DefaultPack` code that imports it).
- `EX4` Moving the `Features/` exercise slices (`GenerateExercise`, `ExerciseRendering`, …) —
  they stay; only their `using`s update.

### ⛓ Constraints

- `C1` Done as its **own isolated commit**, never riding along with feature work.
- `C2` Every namespace keeps the `ChordFlow.` root prefix.
- `C3` Full solution builds; **all tests green** (including the new `IN9` layering tests);
  `loom_validate` clean.
- `C4` Zero remaining `ChordFlow.Domain` / `namespace ChordFlow.Domain` references in `src/`,
  `tests/`, the three refs, `ctx.md`, and `README` after the change (grep-proven).