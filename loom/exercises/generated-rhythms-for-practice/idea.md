---
type: idea
id: id_01KY0R4KJ4ZKWFVWQJ6T5MCJFR
title: Generated Rhythms for Practice
status: done
created: 2026-07-20
version: 1
tags: []
parent_id: null
requires_load: []
---
# Generated Rhythms for Practice

## Goal

Help the user internalize **groove and time-feel** — where beat 1, 2, 3, 4, the `&`s (and at 16ths the `e`/`a`) actually *are* — by **generating rhythm material on the fly** and playing it back over a progression. The core is a rhythm **generation engine**, not a fixed pattern library: it manufactures timing from parameters, so a practicer gets endless, controllable drills instead of a frozen set.

Generated rhythms are **ephemeral** (produced per play, not stored) in v1; because a generation is fully described by `{ strategy, params, seed }`, saving one *into an exercise* later is a small additive step (store the definition, regenerate — exactly how the rest of the app already works). Nothing is baked into the notes.

## Shape: one engine, two strategies

A single `RhythmGenerator` (Core, `Music/Rhythm`, pure `params → domain object`, no I/O, unit-testable) with two selectable strategies:

1. **Pattern strategy** — pedagogical/curated. Builds bars from *blocks* drawn from named *families* via composition operators + sequence behaviours. The teaching tool (the heart of this thread).
2. **Random strategy** — free fill: tile 1–4 bars from a value palette (quarter / 8th / 16th / triplets + rests), plus a silence-bar fill (e.g. 2 content bars + 2 rest bars), looped across the progression. It is the Pattern strategy with the family opened up and the behaviour set to "random" — same substrate, not a second subsystem.

### The load-bearing model: instrument-agnostic onset grid + projection

The generator core produces an **onset grid** (which cells carry an onset, at subdivision `:n`) — instrument-agnostic, no durations, no sustain decisions. A **projection** step renders it to the target:

```
params ─► [generator core] ─► onset grid ─► project ─┬─► RhythmPattern    (comping / lead) — sustain policy
                                                     └─► DrumGroove lane  (drums, single voice) — onsets 1:1
```

This is the existing "two DSLs, one 48-PPQ model" seam. It resolves the notation mismatch in the original draft (drums hit-grid `x`/`.` = hit/no-hit vs. Rhythm DSL `X`/`.`/`-` = attack/sustain/rest): the onset grid is neutral; only the projection knows durations.

**Decisions locked (chat-001):**
- **Sustain policy:** comping/lead onsets **ring to the next onset** (legato), fixed for v1.
- **Canonical output** is a structured domain object (`RhythmPattern` / `DrumGroove`), not a string. The DSL string is a projection for display / debug / future save only.
- **Drums:** single user-picked voice (HH closed default) for v1; multi-lane is later.
- **Block = one beat** (consistent unit); a bar = 4 blocks in 4/4.
- **Families v1:** quarter + eighth only. Triplets/16ths after the model is proven.

## Pattern strategy — the pedagogy

Every good drill isolates **one of two axes**:

- **Axis A — which beats sound** (bar-level): 1 / 2 / 3 / 4. Quarters live here (a `:1` block is just onset-or-rest). Teaches the pulse and the backbeat.
- **Axis B — where inside a beat** (block-level, `:2`+): on-beat vs the `&` (and `e`/`a` at `:4`). Eighths/16ths live here. Teaches syncopation and the offbeat.

### Bar-composition operators — `(family, beatIndex) → block`

1. **Uniform** — same block every beat; the steady-pulse reference.
2. **Isolate(k)** — only beat *k* sounds; the single-onset "where is beat 3?" trainer.
3. **Anchor + Rotate** — beat 1 fixed to a strong `X` lighthouse; beats 2–4 rotate through the family.
4. **Mask(beats)** — onsets only on chosen beats: `Mask(2,4)` backbeat, `Mask(1,3)` the "boom" pulse.
5. **Displace(cells)** — slide a block's onset later; at `:2`, `Displace(1)` turns on-beat `X.` into the `&` `.X` (the offbeat maker).
6. **Accumulate(n) / Thin(n)** — add/drop one onset from beat 1 outward; density as a dial.

### Sequence behaviours — `(barIndex, prevBar) → bar`

1. **Repeat** — identical every bar; internalize before varying.
2. **Cycle** — bar *N* = next entry in the family's ordered list; a guided tour of one family.
3. **Sweep** — bind an operator param to the bar index (`Isolate(barIndex)` walks 1→2→3→4; `Displace(barIndex)` walks a figure through every subdivision). The signature drill: the same shape felt against every metric position.
4. **Rest-bar** — insert empty bars between content bars (`content, rest`; `content, content, rest, rest`); teaches holding time through silence.
5. **Call-and-response** — a content bar, then an empty "your turn" bar to echo it.
6. **Random-in-family** — each bar random within a fixed family (difficulty stays bounded).
7. **Ramp** — progressively grow density/subdivision across the loop (quarters → add `&`s → add `e`/`a`); a tiny curriculum in one generation.

### Two force-multipliers (not operators, but they make the drills *teach*)

1. **Reference pulse** — optionally sound the quarter pulse (or just beat 1) *under* the generated figure so a syncopated onset has an audible "ground" to lock against. Toggle: `referencePulse: off | beat1 | quarters`. In drums mode it's a click/HH lane; for comping/lead it rides the transport's existing metronome/count-in — mostly wiring.
2. **Emphasis + count overlay on the rhythm renderer** — highlight the trained beat / all downbeats and print `1 e & a` under the grid. A *display overlay* (pattern stays timing-only, no DSL change), like the harmonic overlay on ChordSheetR. The most direct hit on the goal — the user *sees* "this onset is the `&` of 2."

### Named trainers (presets — pick intent, not knobs)

Each preset pins operator + behaviour + family; they cost nothing (saved param-tuples over the one engine) and double as the dogfood page's "load an example" menu:

- **Find the Beat** — Isolate + Sweep, quarters, count labels on.
- **The Backbeat** — Mask(2,4), quarters, reference pulse = quarters.
- **On the &** — eighth family, offbeats only (Displace(1)); the offbeat trainer.
- **Fill It In** — Accumulate + Ramp; density grows bar by bar.
- **Leave Space** — content + Rest-bar; hold time through silence.
- **Echo** — Call-and-response + reference pulse; teacher bar, your bar.

## Consumption & the renderer (rhythm-generatorR)

The generated pattern feeds **comping**, **lead**, or **drums** (single voice) — the user picks. The visual/audible "dumb renderer with active beats + animated cursor" already exists: **DrumsR** (pure-SVG hit grid, animated off the engine's time-linear `position` clock) and ChordSheetR's visual-metronome marker. A single-lane onset grid *is* a DrumsR with one row — **reuse DrumsR** (or a trimmed sibling) plus the count-label/emphasis overlay rather than author a new component.

## Phasing (design will split into N plans, one per phase)

This thread carries **1 idea + 1 design + N plans**, one plan per phase, so it all lands incrementally but completely.

- **Phase 1 — dogfood page first (mandated by the dogfood rule).** The generation engine (onset grid + projection + Pattern & Random strategies) + a Rhythm Generator page: pick strategy/preset/params → see + hear generated bars on the reused DrumsR with count labels. De-risks the whole onset→projection model cheaply.
- **Phase 2 — wire into Practice** as a comping/lead/drums source (plumb the existing generator into the existing Generate flow).
- Later phases (folded in by design, sequenced): reference pulse, emphasis/count overlay, full preset set, then triplet/16th families, Random-in-family, Ramp, and eventually "save a generation into an exercise" (`{strategy, params, seed}`).

Exact phase boundaries are a **design decision** (deferred to `design.md`), not fixed here.

## Validation

- **Dogfood:** the Phase-1 Rhythm Generator page renders + plays every strategy/preset on the reused DrumsR — visual + audible confirmation before Practice integration.
- **Determinism:** the same `{strategy, params, seed}` reproduces the same bars (unit-tested); Random takes an explicit seed from day one.
- **Projection correctness:** the same onset grid projects to a legato `RhythmPattern` (comping/lead) and a single-lane `DrumGroove` (drums) that agree on onset placement (unit-tested).
- **Purity:** generator lives in `Music/Rhythm`, no I/O, instrument-agnostic — the guarded `Music → Instruments` edge is not crossed (the drums projection touches only the shared rhythm model).

## Open (for design, not blockers)

- Exact phase split and per-phase plan boundaries.
- Whether the preset set ships fully in one phase or trickles in.
- UI shape of the generator controls (strategy/preset selector + param knobs) on the dogfood page.
