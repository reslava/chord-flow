---
type: design
id: de_01KY2P9JQTNM8TKH6NVSEFY8CY
title: Generated rhythms in Practice
status: draft
created: 2026-07-21
version: 1
idea_version: 1
tags: []
parent_id: id_01KY2NM0ENXKAYFQB6ZE8C3AZG
requires_load: []
---
# Generated rhythms in Practice

Design for wiring the rhythm generator into the Practice/Exercise page as a real track over harmony (see `idea.md`). Ephemeral in this phase; persistence is [[save-generation-into-exercise]] (Phase 5). Decisions below were agreed in `generated-rhythms-for-practice/chat-001`.

---

## 1. The four track roles

A generated `OnsetGrid` feeds a track through the **existing projections**:

| Role | Projection | InstrumentPart |
|------|-----------|----------------|
| **Comping** | `OnsetGrid → RhythmPattern` (legato) | `CompingPart` (its `RhythmPattern`) |
| **Lead** | `OnsetGrid → RhythmPattern` (legato) | `LeadPart` |
| **Drums** | `OnsetGrid → DrumGroove` (single voice) | `DrumPart` |
| **Extra** | `OnsetGrid → DrumGroove` (single voice) | **`ExtraPart`** (new arm) |

Comping/lead need **legato safety** (§3); drums/extra are already hit+rest safe, so **they land first**.

## 2. Decision — the generation source is the Rhythm Gen page (agreed #3)

Rather than duplicate a param picker in HarmonyControlsR, **the Rhythm Generator page is the authoring surface** (full fidelity — every strategy/kind/selection/behaviour). Practice **references the current generation**:

- The Rhythm Gen page holds a **current generation request** (`{strategy, params, seed}` — already what it sends). A **"Use in Practice as → Comping / Lead / Drums / Extra"** action stashes that request under the chosen **role** in a small shared **generated-rhythm registry** (JS module both views read; ephemeral).
- Practice's **Generate** includes, per role marked *generated*, that role's stashed request. The bridge `generate` verb carries the generation request(s); **`GenerateExercise` resolves each via the same `RhythmRequestResolver` + `RhythmGenerator`** the Rhythm Gen handler uses (one resolve path, can't drift) → `OnsetGrid` → the role's projection → the `InstrumentPart`.
- A per-role registry lets each track carry a **different** full-param rhythm (an off-beat comping + a straight hi-hat extra, say). *(v1 alternative if simpler: a single current generation assignable to one role at a time — decide at plan time; the registry is the durable shape.)*
- **Persisted** generations (Phase 5) slot into the same resolve path — the registry entry becomes a saved `{strategy, params, seed}` on the exercise.

## 3. Prerequisite — legato safety (agreed #1: snap-and-rest)

The legato `OnsetGrid → RhythmPattern` ring-to-barline can yield a **non-notatable** length for arbitrary syncopated bars (plan-004 finding). Policy: **snap-and-rest** — a ring is emitted as the **largest single notatable value ≤ the gap**, and the remainder becomes a **rest** (never an unverified tie). Guarantees renderable output for *any* generated bar. (A verified-tie variant, more faithful, is a later option.) This is a change to (or a safe variant of) `OnsetGridToRhythmPattern`, with a unit test that **every** kind/figure/random grid quantizes cleanly. Drums/extra are unaffected.

## 4. The Extra track (agreed #2)

A new **`ExtraPart(DrumGroove Groove, DrumVoice Voice, double Volume, bool Muted)`** arm of the `InstrumentPart` union — a **second percussion track** (reuses `DrumGrooveRenderer` → another `\track`), **default closed hi-hat**, its own **volume slider**. A time-keeper layer under comping/lead/drums. *(When a pitched "metronome click" voice exists, it becomes the default — a small additive follow-up, tracked.)* Non-breaking: an absent `ExtraPart` renders exactly as today (same seam that added `DrumPart`).

## 5. Integration seams (all already in place)

- **`InstrumentPart` union / `Exercise`** — add the `ExtraPart` arm (additive).
- **`AlphaTexRenderer`** — already multi-track; the extra part is another percussion `\track` riding the shared `\tf`.
- **`HarmonyControlsR`** — per-track a **source toggle (Catalog | Generated)** for comping/lead/drums; an **Extra track control + volume**; and it reads the generated-rhythm registry. Reference pulse is **off** here (agreed #4 — the song is the reference).
- **`GenerateExercise` / `ExerciseRefs`** — resolve a *generated* role's request through `RhythmRequestResolver` (the one seam), exactly as it resolves catalog references today.
- **Bridge `generate` verb** — carries the per-role generation request(s) alongside the existing `compingPatternId` / `leadPatternId` / `drumGrooveId`.

## 6. Suggested plan split (for the fresh session)

1. **Legato-safety** (Core) — snap-and-rest in the RhythmPattern projection + the all-grids test. *(Prerequisite for comping/lead; unblocks nothing for drums/extra.)*
2. **`ExtraPart` arm** (Core + renderer) — the union arm + `AlphaTexRenderer` extra `\track` + tests.
3. **Generated-rhythm registry + resolve path** (JS shared state + bridge `generate` fields + `GenerateExercise`/`ExerciseRefs` resolving a generated role).
4. **Rhythm Gen "Use in Practice as → role"** action (page).
5. **HarmonyControlsR** — per-track source toggle + Extra control/volume, wired to the registry.
6. **End-to-end dogfood** — hear all four roles over a song (drums/extra first, then comping/lead).

## 7. Open (resolve at plan time)

- Per-role registry vs. single-current-generation (§2).
- Exact HarmonyControlsR layout for the source toggle + Extra track.
- Whether `ExtraPart` is truly a new arm or a tagged second `DrumPart` (leaning new arm for clarity).

## 8. Validation / dogfood

Generate a rhythm on the Rhythm Gen page, send it to a role, and hear it **comp / lead / drum / extra over a real song** in Practice — with a working per-track volume. Drums + extra path first (no legato dependency), comping/lead after §3.
