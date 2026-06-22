---
type: plan
id: pl_01KVQ2WH0K3H9WQYDEWMK48R5Z
title: Progression Transforms — base + take (slice 1)
status: done
created: 2026-06-22
updated: 2026-06-22
version: 1
design_version: 1
req_version: 1
tags: []
parent_id: de_01KVQ23HY2X7VM6JY2S51F0NCH
requires_load: []
target_version: 0.1.0
steps:
  - id: transform-contract-take
    order: 1
    status: done
    description: Add the `IProgressionTransform` contract and `TakeTransform`
    files_touched: [src/ChordFlow.Core/Music/Progressions/Transforms/IProgressionTransform.cs, src/ChordFlow.Core/Music/Progressions/Transforms/TakeTransform.cs]
    blocked_by: []
    satisfies: [IN1, IN6, C3, C4]
  - id: transform-factory
    order: 2
    status: done
    description: Add the `ProgressionTransform.Parse(name, args)` factory registering only `take`
    files_touched: [src/ChordFlow.Core/Music/Progressions/Transforms/ProgressionTransform.cs]
    blocked_by: []
    satisfies: [IN3]
  - id: partplay-transforms
    order: 3
    status: done
    description: Add `Transforms` (ordered, default-empty) to `PartPlay` and fix construction sites
    files_touched: [src/ChordFlow.Core/Music/Songs/Song.cs]
    blocked_by: []
    satisfies: [IN4, C1]
  - id: op-dsl-hook
    order: 4
    status: done
    description: Extend the Song play-line grammar to lex `@op` tokens and build transforms
    files_touched: [src/ChordFlow.Core/Music/Songs/SongParser.cs]
    blocked_by: []
    satisfies: [IN3, C2]
  - id: expander-seam
    order: 5
    status: done
    description: Apply a play's transforms in `SongExpander` before realization
    files_touched: [src/ChordFlow.Core/Music/Songs/SongExpander.cs]
    blocked_by: []
    satisfies: [IN2, IN5, C5]
  - id: tests
    order: 6
    status: done
    description: "Tests: TakeTransform, @op parsing/composition, transform-free regression"
    files_touched: [tests/ChordFlow.Core.Tests/Music/Progressions/Transforms/TakeTransformTests.cs, tests/ChordFlow.Core.Tests/Music/Songs/SongParserTransformTests.cs]
    blocked_by: []
    satisfies: [IN7, C5]
  - id: dogfood
    order: 7
    status: done
    description: "Dogfood: author a real multi-section tune and drill a section with `@take`"
    files_touched: []
    blocked_by: []
    satisfies: [IN7]
  - id: ref-sync
    order: 8
    status: done
    description: "Reference-doc sync: domain-model + DSL references"
    files_touched: [loom/refs/chordflow-domain-model-reference.md, loom/refs/chordflow-dsl-reference.md]
    blocked_by: []
    satisfies: [C6]
---
# Progression Transforms — base + take (slice 1)

## Goal

Lay down the reusable transform substrate and prove it with one transform. Introduce a pure `IProgressionTransform` contract, register `take` (keep the first N whole bars) in a `ProgressionTransform.Parse` factory, attach an ordered transform list to `PartPlay`, lex the `@op` token in the Song play-line grammar, and apply transforms in `SongExpander` between part resolution and `Transposer.RealizeBars` — so nothing below `Transposer` changes and a transform-free Song stays byte-identical. Confined to `Music.Progressions.Transforms` plus the `PartPlay`/`SongParser`/`SongExpander` touch points; everything else in the idea's priority set is deferred.

---

## Steps

| Done | # | Step | Files touched | Blocked by | Satisfies |
|---|---|---|---|---|---|
| ✅ | 1 | Add the `IProgressionTransform` contract and `TakeTransform` | src/ChordFlow.Core/Music/Progressions/Transforms/IProgressionTransform.cs, src/ChordFlow.Core/Music/Progressions/Transforms/TakeTransform.cs | — | IN1, IN6, C3, C4 |
| ✅ | 2 | Add the `ProgressionTransform.Parse(name, args)` factory registering only `take` | src/ChordFlow.Core/Music/Progressions/Transforms/ProgressionTransform.cs | — | IN3 |
| ✅ | 3 | Add `Transforms` (ordered, default-empty) to `PartPlay` and fix construction sites | src/ChordFlow.Core/Music/Songs/Song.cs | — | IN4, C1 |
| ✅ | 4 | Extend the Song play-line grammar to lex `@op` tokens and build transforms | src/ChordFlow.Core/Music/Songs/SongParser.cs | — | IN3, C2 |
| ✅ | 5 | Apply a play's transforms in `SongExpander` before realization | src/ChordFlow.Core/Music/Songs/SongExpander.cs | — | IN2, IN5, C5 |
| ✅ | 6 | Tests: TakeTransform, @op parsing/composition, transform-free regression | tests/ChordFlow.Core.Tests/Music/Progressions/Transforms/TakeTransformTests.cs, tests/ChordFlow.Core.Tests/Music/Songs/SongParserTransformTests.cs | — | IN7, C5 |
| ✅ | 7 | Dogfood: author a real multi-section tune and drill a section with `@take` | — | — | IN7 |
| ✅ | 8 | Reference-doc sync: domain-model + DSL references | loom/refs/chordflow-domain-model-reference.md, loom/refs/chordflow-dsl-reference.md | — | C6 |
---

### Legend

| Symbol | Meaning |
|--------|---------|
| ✅ | Done |
| 🔄 | In Progress |
| 🔳 | Pending |
| ❌ | Cancelled |

<!-- step:transform-contract-take -->
### Step 1 — Transform contract + take

New `Music/Progressions/Transforms/IProgressionTransform.cs` (pure `Progression Apply(Progression)`) and `TakeTransform.cs` (`record TakeTransform(int Count)`). `Apply` keeps the first `Count` whole bars via `Progression.FromBars(p.Id, p.Name, p.Bars.Take(Count).ToArray(), TimeSignature.FourFour)`. Whole bars retained ⇒ per-bar invariants hold untouched. Throw on `Count < 1` or `Count > Bars.Count` (ArgumentException naming the value). No key-aware interface (deferred).

<!-- step:transform-factory -->
### Step 2 — Transform factory

New `Music/Progressions/Transforms/ProgressionTransform.cs` static factory: maps a transform name + parsed args to an `IProgressionTransform`. Registers `take` → `new TakeTransform(int.Parse(arg))`. Unknown name or malformed args throw `FormatException` naming the token. `@repeat` and the rest of the priority set are intentionally not registered.

<!-- step:partplay-transforms -->
### Step 3 — PartPlay.Transforms

Extend `PartPlay` to `record PartPlay(string PartName, int Repeat, IReadOnlyList<IProgressionTransform> Transforms)`. Default to a shared empty list. Update `Song.OfProgression`'s `new PartPlay("A", 1)` and any other construction site to pass `[]`. `Song.FromSections` validation unchanged.

<!-- step:op-dsl-hook -->
### Step 4 — @op DSL hook

In `SongParser` pass 2, change the play branch from `NAME [x<n>]` to `NAME ( x<n> | @op )*`: accept one `x<n>` (Repeat) and zero-or-more `@name(args)` tokens in either order; lex the `@name(args)` shape, split args, and delegate construction to `ProgressionTransform.Parse`. Append transforms in written order. Unknown `@name` / bad args surface the factory's `FormatException`. Update the class-doc comment that currently says `@repeat` is unparsed.

<!-- step:expander-seam -->
### Step 5 — Expander seam

In `SongExpander.Expand`'s `PartPlay` case, fold `play.Transforms` left-to-right onto the resolved `Progression` (between `Resolve(...)` and `Transposer.RealizeBars(...)`). Empty list ⇒ no change ⇒ byte-identical render (regression). Nothing below `Transposer` touched.

<!-- step:tests -->
### Step 6 — Tests

Unit: `TakeTransform` keeps first N bars, preserves multi-span bars, throws on 0/negative/`>count`. Unit: `SongParser` parses `@take(N)`, composes `@a @b` left-to-right, accepts `x<n>`+`@op` in either order, throws on unknown `@name`/bad args. Integration: `blues @take(4)` realizes to a 4-bar RealizedSong; a transform-free Song is byte-identical to today.

<!-- step:dogfood -->
### Step 7 — Dogfood

Author one real standard/blues in the Song DSL and use `@take(N)` to drill a section on its dogfood page — confirm the slice actually eases drilling real content.

<!-- step:ref-sync -->
### Step 8 — Ref sync

Update `chordflow-domain-model-reference.md` (the transform types, `ProgressionTransform.Parse`, `PartPlay.Transforms`, the `SongExpander` seam) and `chordflow-dsl-reference.md` (the play-line `@op` / `@take(N)` syntax) in the same unit of work.
