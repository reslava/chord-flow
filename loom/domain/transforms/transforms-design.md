---
type: design
id: de_01KVQ23HY2X7VM6JY2S51F0NCH
title: Progression Transforms — base + take (slice 1)
status: done
created: 2026-06-22
updated: 2026-06-22
version: 2
idea_version: 2
tags: []
parent_id: id_01KTVTM1797WBJ8TF9K7B4VPTR
requires_load: []
---
# Progression Transforms — base + take (slice 1)

## Scope

The **first, minimal slice** of the Progression Transforms idea: the reusable
**base** (the transform contract + composition + the Song-DSL `@op` hook) plus
**one proof transform — `take`** — to prove the seam end-to-end. Everything else
in the idea's priority set (`skip`, `reverse`, `transpose`, `dominantize`,
`triadsToSevenths`, `turnaround`, …) is deferred; this slice exists to lay the
substrate down cleanly and validate it against the real pipeline with one
genuinely useful drill transform.

Grounded against the live kernel (2026-06-22): `Progression` /
`HarmonicBar` / `ChordSpan` / `RomanDegree` / `Transposer` all exist as the idea
assumed; `IProgressionTransform` does not exist yet (true greenfield). The Song
DSL reserves the `@`-token (`SongParser`: "`@repeat` is reserved for the future
transform and is not parsed here") but the `@op` slot and a per-play transform
list still have to be **built here** — they did not arrive with Song.

## The pipeline seam

```
PartPlay (name + transforms)
  → Resolve(name) → Progression                 [SongExpander, unchanged]
  → apply transforms left-to-right → Progression  ← NEW (the only new step)
  → Transposer.RealizeBars(progression, key)     [unchanged]
  → RealizedSection
```

The transform step slots into `SongExpander.Expand`'s `PartPlay` case, **between**
`Resolve(...)` and `RealizeBars(...)`. Nothing below `Transposer` changes — the
renderer, quantizer, and bridge never know transforms exist. This is the
"additive layer above Transposer" the idea promised, and it is literally a
2-line insertion at `SongExpander.cs:47`.

## 1. The transform contract

```csharp
namespace ChordFlow.Music.Progressions.Transforms;

public interface IProgressionTransform
{
    Progression Apply(Progression progression);
}
```

Pure, key-independent, `Progression → Progression`. `take` (and most of the
idea's priority set) operates on `RomanDegree`s / bars and never needs the key.

**Decision D1 — key-aware variant deferred.** The idea anticipates a small number
of *key-aware* transforms taking `Apply(Progression, Key)`. `take` does not need
it, so we **do not introduce it now**. When the first key-aware transform lands
(likely `transpose` if it ever needs key context — though degree-transpose does
not), add a sibling `IKeyAwareProgressionTransform { Progression Apply(Progression, Key); }`
and have `SongExpander` dispatch on type (it has the running key in hand at the
seam). Defining one interface now keeps the slice honest (YAGNI) without closing
the door — the dispatch point is already key-aware.

## 2. Composition

Transforms **compose left-to-right** and are **not commutative** (idea contract).
A `PartPlay` carries an ordered `IReadOnlyList<IProgressionTransform>`; the
expander folds them:

```csharp
Progression p = Resolve(play.PartName, song, store);
foreach (IProgressionTransform t in play.Transforms)
    p = t.Apply(p);
```

No dedicated `CompositeTransform` type — the ordered list *is* the composition,
applied in arrangement order. (A `Compose(...)` helper can come later if a caller
ever needs a first-class composite; nothing in this slice does.)

## 3. Song model change — where transforms attach

**Decision D2 — attach to `PartPlay`, not `Part`.** Transforms are an
*application-site* choice: the same defined part may be played plain in one spot
and `@take(8)` in another. That mirrors how `x<n>` (repeat) already lives on the
play, not the definition. So:

```csharp
public sealed record PartPlay(
    string PartName,
    int Repeat,
    IReadOnlyList<IProgressionTransform> Transforms) : ArrangementItem;
```

- Default to an **empty list** for the no-transform case. `Song.OfProgression`'s
  `new PartPlay("A", 1)` and every existing construction get `[]`
  (a `static readonly` empty array or a convenience overload), so all current
  Songs stay byte-identical through realization.
- `Song.FromSections` validation is unchanged (transforms need no structural
  check at the Song layer — a bad `take(n)` fails inside the transform).

*Alternative considered:* attach to the `Part` definition. Rejected — it would
bind a transform to every play of the part and break the "same part, different
drill" use that makes `take`/`skip` worth having.

## 4. The `@op` DSL hook

Extend the **play-line grammar** in `SongParser` (pass 2). Today a play line is
`NAME` or `NAME x<n>` and `tokens.Length > 2` throws. New grammar:

```
play-line := NAME ( x<n> | @op )*
@op       := '@' name '(' args ')'      e.g. @take(8)
```

- `NAME` first (unchanged); then any mix of one `x<n>` and zero-or-more `@op`
  tokens. `x<n>` parses to `Repeat` (still at most one); each `@op` parses to a
  transform appended in **written order**.
- **Decision D3 — token order is lexical, semantics fixed.** Transforms always
  apply to the *progression*, then the (transformed) section repeats `Repeat`
  times — so `A @take(8) x2` and `A x2 @take(8)` mean the same thing. We allow
  both orders rather than legislate one; recommend authors write `NAME @op… x<n>`.
- **Lexing stays in `SongParser`** (it owns the play line). The **name→transform
  mapping** lives in a `ProgressionTransform.Parse(name, args)` factory in
  `Music.Progressions.Transforms` (the transform catalog's home). `SongParser`
  recognizes the `@name(args)` shape, splits args, and delegates construction —
  it never hard-codes `take`. Unknown `@name` → `FormatException` naming the
  token (house convention).
- `@repeat` stays reserved/unimplemented in this slice (it is the *least*
  valuable transform — it duplicates `x<n>`; see the chat). The factory simply
  does not register it yet.

## 5. The `take` transform

```csharp
public sealed record TakeTransform(int Count) : IProgressionTransform
{
    public Progression Apply(Progression p) =>
        Progression.FromBars(p.Id, p.Name, p.Bars.Take(Count).ToArray(), TimeSignature.FourFour);
}
```

- **Semantics:** keep the **first `Count` bars** of the progression, drop the
  rest. Bars are retained **whole**, so every per-bar invariant
  (`spans sum to BarTicks`, quarter-alignment) is preserved untouched — the
  guarded `FromBars` re-validation passes trivially. v1 is 4/4-only, so
  `TimeSignature.FourFour` is correct here (matches `ToSingleSpanBars`).
- **Decision D4 — out-of-range fails loud.** `Count < 1` or `Count > Bars.Count`
  throws (`FormatException` at parse for a literal, `ArgumentException` at
  construct) naming the offending value — consistent with `Progression.FromBars`
  / `Song.FromSections`. *Alternative:* clamp `Count` to `[1, Bars.Count]`.
  Rejected for the house fail-loud style; asking for 8 bars of a 4-bar
  progression is an authoring error worth surfacing, not silently absorbing.
- **Bar-granular only** (v1): `take` counts whole `HarmonicBar`s, never spans
  within a bar. Sub-bar slicing is out of scope.

## 6. What this is NOT (deferred)

- Every other transform in the idea's priority set. They register into the same
  factory additively when demand (from dogfooding real songs) picks them.
- The key-aware interface (D1).
- A first-class `CompositeTransform` / `Compose` helper.
- Any change to the renderer, quantizer, bridge, persistence, or the standalone
  `Progression`/`Transposer` paths. The whole slice is confined to
  `Music.Progressions.Transforms` (+ the `PartPlay`/`SongParser`/`SongExpander`
  touch points in `Music.Songs`).

## 7. Validation

- Unit: `TakeTransform` keeps first N bars; preserves multi-span bars; throws on
  `0`, negative, and `> count`.
- Unit: `SongParser` parses `@take(N)`, composes `@a @b` left-to-right, accepts
  `x<n>` + `@op` in either order, throws on unknown `@name` and malformed args.
- Integration: a Song with `blues @take(4)` realizes to a 4-bar `RealizedSong`;
  a Song with **no** transforms is byte-identical to today's render (regression).
- **Dogfood:** author one real multi-section tune in the Song DSL and use
  `@take` to drill a section on its dedicated page — the slice earns its keep
  only if it makes drilling a real standard easier.

## 8. Reference-doc sync (on implementation)

- `chordflow-domain-model-reference.md` — add `IProgressionTransform` /
  `TakeTransform` / `ProgressionTransform.Parse` to the Progressions section, and
  note the `PartPlay.Transforms` field + the `SongExpander` transform seam.
- `chordflow-dsl-reference.md` — document the play-line `@op` syntax and `@take(N)`
  in the Song DSL section.

## Open decisions for sign-off

D1 (defer key-aware interface), D2 (attach to `PartPlay`), D3 (lexical token
order, fixed semantics), D4 (out-of-range `take` fails loud) — all carry a
recommendation above. Confirm or redirect before req + plan.

Related: [[chordflow-domain-model-reference]], [[chordflow-dsl-reference]], the `song` thread.
