---
type: idea
id: id_01KVGH7P05NX5W8RKTMVRGBTZ6
title: Rename Domain → Music (theory kernel)
status: done
created: 2026-06-19
updated: 2026-06-21
version: 3
tags: []
parent_id: null
requires_load: []
---
# Rename Domain → Music (theory kernel)

## The idea

Rename the music-theory kernel from `Domain` to **`Music`** — namespace
`ChordFlow.Domain` → `ChordFlow.Music`, folder `src/ChordFlow.Core/Domain/` →
`Music/`. Rafa leans `Music`; **no hurry, doesn't block any feature thread.** Captured so
it isn't forgotten.

## Why `Music` (the case for)

- It's what the kernel actually **is** — `ctx.md` already calls it the "music-theory-first
  kernel." "Domain" is generic DDD jargon that says nothing specific.
- It pairs cleanly with the growing `Instruments/` weave:
  **`Music` = pure theory · `Instruments` = how an instrument projects theory onto
  frets/strings.** That conceptual split is genuinely nicer than `Domain` vs `Instruments`.

## Cautions / open decision

1. **It's a large mechanical rename.** Touches: the namespace, the folder, every
   `using ChordFlow.Domain;`, XML-doc cross-references, the three `loom/refs/` reference
   docs (`chordflow-domain-model-reference`, `chordflow-architecture-reference`,
   `chordflow-dsl-reference`), `loom/ctx.md`, and the MCP-gate path globs. Do it as its
   **own isolated commit**, never riding along with feature work.

2. **Scope decision — pick before doing it.** `Domain/` today is *not* only theory: it also
   holds `Exercise`, `SeedData`, `IProgressionStore`, the Song parser, the rhythm grid.
   "Music" invites "why is `Exercise` in Music?". So choose:
   - **(a) Pure rename** of the existing grab-bag — cheap, mechanical.
   - **(b) `Music` = pure theory only** — the feature/persistence types (`Exercise`,
     `SeedData`, stores) move out to a `Features`/`Application` area. A real reorg, larger.

   Recommendation: agree on `Music`; defer until either we're already doing a theory/guitar
   reorg or there's a quiet moment, and decide (a) vs (b) then.

## In scope (when scheduled)

- The rename itself (namespace + folder + all `using`s + doc/ctx/gate references), per the
  chosen scope (a or b).

## Out of scope / deferred

- Any behavioral change — this is pure naming/structure.

## Validation

- Full solution builds; all tests green; `loom_validate` clean; the three ref docs + ctx
  updated in the same unit of work (ref-sync contract).

Related: [[chordflow-architecture-reference]], [[chordflow-domain-model-reference]], the
`core-host-split` thread (a comparable structural refactor).