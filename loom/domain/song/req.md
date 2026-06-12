---
type: req
id: rq_01KTXQ815FXKF6N974RNKJWRD2
title: Song — an arrangement layer over Progressions — Requirements
status: locked
created: "2026-06-12T00:00:00.000Z"
updated: 2026-06-12
version: 1
tags: []
parent_id: id_01KTVTKHDVQYNXYB33HXH0HXAS
requires_load: []
---
# Song — an arrangement layer over Progressions — Requirements

### ✅ Included

- `IN1` Song domain model — `Song` plus `Part` (`ProgressionReference` / `InlineProgression`), `ArrangementItem` (`PartPlay` / `RelativeMod` / `AbsoluteKey`), and `Modulation` — with a single guarded `Song.FromSections(...)` factory paralleling `Progression.FromBars` (validates part references resolve, `Repeat >= 1`, at least one `PartPlay`).
- `IN2` `SongExpander.Expand(song, store)` — resolves part references (local-first, then store) and folds modulations left-to-right over a running key into a `RealizedSong` of labelled, keyed `RealizedSection`s built via `Transposer.Realize`.
- `IN3` `SongParser` (peer of `ProgressionParser`) for the Song DSL — `key` line, inline `NAME = <prog-dsl>` definitions (RHS parsed by `ProgressionParser`), `NAME: <stored-id>` references, stream `NAME`/`NAME x<n>`, `mod <spec>` (relative), and `key <token>` (absolute reset).
- `IN4` `SongEntity` persistence with parity to `ProgressionEntity` — `Dsl` as the only stored form, idempotent first-run seeding by `Id`, `DbContext` wiring, and the catalog-metadata + `Origin` provenance model adopted from the `packages` thread.
- `IN5` `SongExercise` play-target model (`Song` + `RhythmPattern` + `Tempo` + `Difficulty` + `Feel`) and a single section-aware renderer entry point `Render(RealizedSong, rhythm, tempo, difficulty, feel)` that owns the whole walk (one header, inline `\ks` only on key change, `currentDuration` flowing across section seams, shared private `RenderBars(...)`).
- `IN6` One seeded example song plus a public-facing Song DSL reference doc.

### ❌ Excluded

- `EX1` Progression transforms (transpose, dominantize, jazzify, take/skip/reverse, turnaround injection, …) — deferred to their own `domain/transforms` thread; the Song DSL only reserves the `@op` slot.
- `EX2` Repeat endings (1st/2nd), D.C./D.S. al coda, and coda jumps.
- `EX3` Per-section time signatures / multi-meter songs — v1 inherits the single 4/4 time signature.
- `EX4` Per-section rhythm / voicing / tempo / feel overrides — these attach at play time via `SongExercise`, never inside the Song.
- `EX5` Wiring `SongExercise` into the UI / exercise library — a follow-up once the harmony layer is proven.

### ⛓ Constraints

- `C1` A Song composes **references only** — it never holds bars or chords directly; harmony stays in the Progression. `SongExpander` slots in above `Transposer`; nothing in `Domain/`, `Rendering/`, or the bridge below it changes.
- `C2` Modulation lives **only at the arrangement layer** as a stateful fold of the running key; the Progression is never mutated. Relative (`mod V` / `mod +2`) is the default and **accumulates**; absolute (`key G`) is the reset / escape hatch. `RealizedSection.Key` is an output of the fold, never an input.
- `C3` `Domain/` stays I/O-free — stored-progression lookup goes through the `IProgressionStore` interface; the renderer stays I/O-free and `AlphaTexRenderer` remains the only alphaTex-aware code (the Features orchestrator runs `Expand` only).
- `C4` `Dsl` is the only persisted form; `RealizedSong` / alphaTex are never stored (regenerated on load). Referential integrity is enforced loud at resolution time (`reference 'x' not found`), not via a DB-level FK.
- `C5` `x<n>` is the only section-repeat syntax (`@repeat` is reserved for the future bar-expansion transform); `mod` is a stream token between parts; locals shadow stored names; `Repeat` expands at realization, never in the parser.
- `C6` `InitialKey` defaults to **C major** when the `key` line is omitted.