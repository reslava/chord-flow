---
type: req
id: rq_01KY0RF3C74YD1Z401KKD3PRVX
title: Generated Rhythms for Practice — Requirements
status: locked
created: 2026-07-20
updated: 2026-07-21
version: 4
design_version: 2
tags: []
parent_id: de_01KY0RDXS9C7X93BX8Y1HVCMC3
requires_load: []
---
# Generated Rhythms for Practice — Requirements

Requirements for the rhythm **generation engine** and its pedagogy layer — the authoritative scope anchor for the phase plans. Derived from `chat-001`, `idea.md`, and `design.md`.

### ✅ Included

- `IN1` An instrument-agnostic **onset-grid** generation model: `Block` (one beat's onset pattern), `OnsetBar` (4 blocks in 4/4), `OnsetGrid` (1–4 bars) — attack positions only, no durations/pitch/instrument.
- `IN2` One `RhythmGenerator` with **two selectable strategies** — `Pattern` (pedagogical) and `Random` (free fill) — over the shared onset-grid substrate.
- `IN3` The **Pattern strategy** pedagogy, built on **whole-bar patterns** (revised chat-001 — supersedes the original per-beat operators; "block = a bar pattern"): a **kind** vocabulary of generated families (quarter/eighth **density** families by onset count 1–4; eighth **placement** families on-beat / off-beat(`&`) / both) plus a curated **named-figure** catalog (Backbeat, Downbeats, Charleston, Tresillo, Cinquillo, clave, … — cheap data, grown freely); a **selection** of how bars are drawn from a kind (Fixed / Cycle / RandomInKind / FixedPlusRotating); the multi-bar behaviours **RestBar / CallResponse / Sweep**; and an optional **Displace** transform (shift onsets later → offbeat/pushed variants).
- `IN4` The **Random strategy**: fill 1–4 content bars from a note-value palette, plus a silence-bar fill, tiled across the progression. (Refined by `IN12` — within-content-bar rests.)
- `IN5` Two **projections** from one `OnsetGrid`: `→ RhythmPattern` (comping/lead, **ring-to-next-onset legato**) and `→ DrumGroove` (drums, **single voice**, onsets 1:1).
- `IN6` **Determinism**: a generation is fully described by `{strategy, params, seed}` and re-running reproduces the same grid.
- `IN7` A **Rhythm Generator dogfood page**: strategy/preset/param controls → generate → visualize on the reused **DrumsR** with a **count/emphasis overlay** (`1 e & a`) + audible playback, via a `rhythmGenerate` bridge verb.
- `IN8` A **reference pulse** option (`Off | Beat1 | Quarters`) sounded under the generated figure. (The `Beat1` variant lands first on the Rhythm Generator page — an implicit, *visible* reference row, distinct voice, added post-generation, never part of the generated pattern.)
- `IN9` **Named trainer presets** (pinned param tuples) — v1: Find the Beat, The Backbeat, On the &, Leave Space. (The named-figure kinds of `IN3` double as presets.)
- `IN10` **Practice integration**: the generator as an on-the-fly (ephemeral) **comping / lead / drums (single-voice)** rhythm source in the Generate flow (may also be an extra track layered over a Song).
- `IN11` **Phased delivery** — 1 idea + 1 design + N plans, one plan per phase, each independently shippable and dogfood-verifiable.
- `IN12` The **Random strategy within-content-bar rests**: a **rest-probability** control intersperses rests among the onsets, each rest taking the length of the note value drawn at that step (quarter / eighth / 16th rests). Beat 1 is **not** forced to sound — the generator stays free; any downbeat reference is the `IN8` reference pulse, not a generator onset.
- `IN13` A **Loop / cycling** playback toggle — added to **ScoreR** (shared, so every render surface can loop) and surfaced on the Rhythm Generator page, **default on**.

### ❌ Excluded

- `EX1` **Persistence of generations** in v1 — they are ephemeral; saving a `{strategy, params, seed}` into an exercise is deferred (Phase 5).
- `EX2` **Multi-lane drum generation** — v1 projects to a single user-picked drum voice only. (The `IN8`/`IN12` reference pulse is a fixed non-generated layer, not multi-lane generation.)
- `EX3` **Triplet & 16th families** in v1 — quarter + eighth only; richer subdivisions deferred (Phase 5).
- `EX4` **Cross-bar sustain / syncopated ties** in the legato projection — the last onset of a bar rings only to the barline in v1.
- `EX5` **Time signatures other than 4/4.**
- `EX6` **Stroke / accent / feel written into the generated grid** — the grid is timing-only; those stay play-time overlays.
- `EX7` **A stab/staccato sustain policy** — v1 comping/lead is fixed legato (ring-to-next-onset), no toggle.
- `EX8` **Ramp behaviour** in v1 — deferred (Phase 5). (`RandomInKind` is **no longer excluded** — it is now a Pattern-strategy selection under the revised `IN3`.)

### ⛓ Constraints

- `C1` The generator **core** lives in `Music/Rhythm/Generation/`, is pure/immutable with no I/O, and is **instrument-agnostic** — it must not cross the guarded `Music → Instruments` edge.
- `C2` The `OnsetGrid → DrumGroove` projection lives in `Instruments/Drums/` (it targets a Drums type; `Instruments → Music` is the legal direction).
- `C3` **No new alphaTex-aware code** — the drums projection reuses `DrumGrooveRenderer`, the comping/lead projection reuses `AlphaTexRenderer`.
- `C4` The legato projection must stay within the **verified render vocabulary** (`:N` + rests, `( )` groups) — it must never emit an unverified tie/dotted token.
- `C5` The dogfood renderer **reuses DrumsR**; the count/emphasis overlay is **display-only** (no change to the rhythm model or DSL).
- `C6` **Projection agreement** — for one `OnsetGrid`, the `RhythmPattern` event onset ticks equal the `DrumGroove` hit onset ticks (unit-test invariant).
- `C7` The **seed** is part of the params from day one, even while output is ephemeral.
- `C8` **Block = one beat** is the canonical *cell-grouping* unit (a bar = 4 blocks in 4/4); the Pattern strategy's *vocabulary* unit is a whole-bar pattern built from them (`IN3`).
