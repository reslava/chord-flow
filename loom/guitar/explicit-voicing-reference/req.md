---
type: req
id: rq_01KWZAFJ3NSWGZNTDSJFWSAP6K
title: Explicit per-chord voicing references in the DSL — Requirements
status: locked
created: 2026-07-07
updated: 2026-07-08
version: 2
design_version: 5
tags: []
parent_id: de_01KWZAEXXHM8K1PB5J71G3T800
requires_load: []
---
# Explicit per-chord voicing references in the DSL — Requirements

### ✅ Included

- `IN1` A **per-chord voicing annotation** token `{ … }` bound to a chord in the (inline) Progression DSL, in both value forms (literal grip and source-qualified reference).
- `IN2` **Source-qualified reference** forms inside the annotation: `u:` (user), `a:` (automatic / engine-derived), `<package>:` (e.g. `swing:`) — resolving a listed voicing by source + id.
- `IN3` **Literal custom grip** value: six fret tokens low-E→high-E (`x`=mute, `0`=open, `N`=fret), optionally prefixed `c:`, treated as a **movable shape normalized to C**.
- `IN4` A **Song-level `voice` default** directive `voice <selector> = <voicing-spec>`, with **degree-scoped** selectors (`voice 17`, `voice #4dim7`) and **quality-scoped** wildcard selectors (`voice *7`, `voice *m7`). RHS is the same voicing-spec (grip or reference).
- `IN5` **Most-specific-wins resolution cascade** in `CompingResolver`: per-chord `{}` › degree-scoped `voice` › quality-scoped `voice` › automatic ranking fill.
- `IN6` **Fail-loud** when a referenced id is missing or its source is filtered out.
- `IN7` **Progression purity guard**: `ProgressionParser` lexes `{}` but a standalone/stored progression rejects annotations; only an inline-in-Song progression honors them.
- `IN8` **Round-trip**: annotated DSL parses → serializes → parses unchanged.
- `IN9` **DSL-reference update**: `chordflow-dsl-reference` documents both placements and the cascade (same unit of work).
- `IN10` **Dogfood**: an annotated 12-bar blues renders the pinned grips on the now/next fret-boxes of the fretboard UI page.
- `IN11` **Rootless voicings** are first-class: a grip may declare an anchor with an optional `root:<string>[@<fret>]` clause, where the `@<fret>` form is a **phantom root** on a muted string (enabling rootless jazz shells / drop-2s). Required only when the root is unsounded or shape-inference is ambiguous; the engine infers otherwise.

### ❌ Excluded

- `EX1` The automatic ranking fill and main-source/fallback resolution ([[engine-derived-as-app-source]]).
- `EX2` Selectable ranking strategies/modes ([[voicing-ranking-strategies]]).
- `EX3` A UI voicing-picker that *writes* these annotations.
- `EX4` A **per-span (per-bar)** annotation form — per-chord and per-song only.
- `EX5` **Barre / finger hints** inside the literal grip (fret string only).
- `EX6` The `\` **sigil** syntax for song defaults (rejected for the `voice` keyword).
- `EX7` Voicing annotations as **content on stored Progressions / Rhythms** (they stay pure harmony/timing).

### ⛓ Constraints

- `C1` Song defaults use the **`voice` keyword** (space-keyword style, peer of `key`/`feel`/`tempo`), not a sigil.
- `C2` **One voicing-spec grammar, uniform sugar**: bare grip accepted everywhere, `c:` optional, references always carry their `source:` word; identical in the brace and after `=`.
- `C3` Grips are **movable, canonicalized to C** exactly like the Voicing DSL. Per-chord and degree-scoped grips anchor on harmony (degree + key); quality-scoped grips anchor via engine shape-inference, with an optional `root:` hint and references as the movable alternative.
- `C4` **`*` wildcard-degree** marks quality-scoped selectors; it must not collide with existing Song/Progression DSL tokens.
- `C5` The feature is an **additive override** on the `CompingResolver` seam from [[engine-derived-as-app-source]] (D4 = (B)) — no change to the ranking fill.
- `C6` At most **one `voice` per distinct selector** (duplicate is an error).
- `C7` House canonical minor-7 token is **`6-7`**.
- `C8` Thread **depends on** [[engine-derived-as-app-source]] (its `CompingResolver` is the seam this rides).
- `C9` The `root:` anchor clause is **optional** and uses `root:<string 6..1>` (voiced, fret read from the grip) or `root:<string>@<fret>` (phantom root on a muted string). `@<fret>` is required when the named root string is muted; the `@` separator is used (not `-`).
