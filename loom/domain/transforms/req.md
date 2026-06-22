---
type: req
id: rq_01KVQ2TRN6SJB8RWSV1XHAA651
title: Progression Transforms — functional rewrites of Progressions — Requirements
status: locked
created: 2026-06-22
updated: 2026-06-22
version: 1
tags: []
parent_id: id_01KTVTM1797WBJ8TF9K7B4VPTR
requires_load: []
---
# Progression Transforms — functional rewrites of Progressions — Requirements

### ✅ Included

- `IN1` `IProgressionTransform` contract — a pure `Progression Apply(Progression)` interface in `Music.Progressions.Transforms`.
- `IN2` Left-to-right transform composition — an ordered list of transforms applied in sequence (no first-class composite type).
- `IN3` Song-DSL `@op` hook — extend the play-line grammar to `NAME ( x<n> | @op )*` where `@op = @name(args)`; `SongParser` lexes the `@name(args)` shape and delegates construction to a `ProgressionTransform.Parse(name, args)` factory in `Music.Progressions.Transforms`; an unknown `@name` throws `FormatException` naming the token.
- `IN4` `PartPlay.Transforms` field — `PartPlay` gains an ordered `IReadOnlyList<IProgressionTransform>` (defaults to empty for the no-transform case).
- `IN5` `SongExpander` transform seam — in the `PartPlay` case, apply the play's transforms to the resolved `Progression` (folded left-to-right) **between** `Resolve(...)` and `Transposer.RealizeBars(...)`.
- `IN6` `TakeTransform` — the one proof transform: keep the **first `Count` whole bars** of a progression, drop the rest; registered in the factory under `take`.
- `IN7` Validation — unit tests for `TakeTransform`, the `@op`/composition parsing, and the transform-free regression; plus a dogfood: author one real multi-section tune and use `@take` to drill a section.

### ❌ Excluded

- `EX1` The key-aware transform interface (`IKeyAwareProgressionTransform` / `Apply(Progression, Key)`) — deferred until a transform actually needs the key (D1).
- `EX2` Every other transform in the idea's priority set (`skip`, `reverse`, `transpose`, `dominantize`, `triadsToSevenths`, `turnaround`, sequence, tritone-sub, jazzify, …) **and** `@repeat` — `@repeat` stays reserved/unbuilt this slice (it duplicates `x<n>`).
- `EX3` A first-class `CompositeTransform` type or `Compose(...)` helper — the ordered list is the composition.
- `EX4` Sub-bar / span-level `take` granularity — `take` counts whole `HarmonicBar`s only.
- `EX5` Any change to the renderer, quantizer, bridge, persistence, or the standalone `Progression`/`Transposer` code paths — the slice is confined to `Music.Progressions.Transforms` + the `PartPlay`/`SongParser`/`SongExpander` touch points.

### ⛓ Constraints

- `C1` Transforms attach to `PartPlay` (the application site), never to the `Part` definition (D2) — so the same part can be played plain or transformed in different spots, mirroring `x<n>`.
- `C2` On a play line `@op` and `x<n>` are accepted in **either** token order; semantics are fixed — transforms apply to the progression, then the section repeats `Repeat` times (D3).
- `C3` Out-of-range `take` fails loud: `Count < 1` or `Count > Bars.Count` throws (`FormatException` at parse / `ArgumentException` at construct) naming the value — no clamping (D4).
- `C4` Transforms are pure and key-independent in this slice; `Music` kernel purity (no I/O) is preserved; `take` retains whole bars so every per-bar `Progression.FromBars` invariant holds untouched (4/4-only v1).
- `C5` A transform-free Song realizes **byte-identical** to today's render (`PartPlay.Transforms` empty ⇒ no behavior change) — a regression guard.
- `C6` Reference-doc sync on implementation: update `chordflow-domain-model-reference.md` (the transform types + `PartPlay.Transforms` + the expander seam) and `chordflow-dsl-reference.md` (the play-line `@op` / `@take(N)` syntax) in the same unit of work.
