---
type: req
id: rq_01KY2PAM36XG40CWCSEE256F7V
title: Generated rhythms in Practice — Requirements
status: locked
created: 2026-07-21
updated: 2026-07-21
version: 1
design_version: 1
tags: []
parent_id: de_01KY2P9JQTNM8TKH6NVSEFY8CY
requires_load: []
---
# Generated rhythms in Practice — Requirements

Requirements for wiring the rhythm generator into the Practice/Exercise page. Derived from `generated-rhythms-for-practice/chat-001`, this thread's `idea.md`, and `design.md`.

### ✅ Included

- `IN1` A generated rhythm usable as a **Practice track** in **four roles**: **Comping / Lead / Drums / Extra**, over a song/progression.
- `IN2` Role → projection: **Comping / Lead** use the legato `OnsetGrid → RhythmPattern`; **Drums / Extra** use the single-voice `OnsetGrid → DrumGroove`.
- `IN3` The generation **source is the Rhythm Generator page's current configuration** (full param fidelity), referenced by Practice via a small shared **generated-rhythm registry** keyed by role — **no duplicate param picker** in HarmonyControlsR.
- `IN4` A **"Use in Practice as → role"** handoff on the Rhythm Generator page that stashes the current `{strategy, params, seed}` under the chosen role.
- `IN5` A new **`ExtraPart`** `InstrumentPart` arm — a **second percussion track** (default **closed hi-hat**) with its **own volume** — an additive time-keeper layer.
- `IN6` **Legato safety (snap-and-rest):** a legato ring is emitted as the largest single notatable value ≤ the gap, remainder → rest, so **any** generated (syncopated) bar renders notatably for comping/lead.
- `IN7` Practice **Generate** resolves each *generated* role through the **same `RhythmRequestResolver` + `RhythmGenerator`** as the Rhythm Gen handler → `OnsetGrid` → the role's projection → the `InstrumentPart`.
- `IN8` **HarmonyControlsR**: a per-track **source toggle (Catalog | Generated)** for comping/lead/drums, plus an **Extra track control + volume**, reading the registry.
- `IN9` **Ephemeral** in this phase — the current generation is used on the fly, not persisted.
- `IN10` **Drums / Extra land first** (no legato dependency); **comping / lead** follow once legato-safety (`IN6`) is in.

### ❌ Excluded

- `EX1` **Persisting** a generation into a saved exercise — deferred to [[save-generation-into-exercise]] (Phase 5).
- `EX2` A **pitched "metronome click"** instrument voice for the Extra track — later additive; v1 Extra is a drum voice (default closed hi-hat).
- `EX3` **Multi-lane** generated drums — single voice per role (as the generator produces).
- `EX4` A **verified-tie** legato variant — v1 is snap-and-rest (`IN6`).
- `EX5` **Reference pulse in Practice** — off (the song is the reference).
- `EX6` A **full param picker duplicated in HarmonyControlsR** — the Rhythm Gen page is the authoring surface (`IN3`).

### ⛓ Constraints

- `C1` **One resolve path** — a generated role resolves via the *same* `RhythmRequestResolver` + `RhythmGenerator` as the Rhythm Gen handler (no drift between the page preview and the Practice track).
- `C2` **Additive `InstrumentPart` seam** — an absent `ExtraPart` renders byte-identical to today (non-breaking, the seam that added `DrumPart`).
- `C3` **No new alphaTex-aware code** — reuse `AlphaTexRenderer` / `DrumGrooveRenderer`.
- `C4` The legato snap-and-rest output stays within the **verified render vocabulary** — a unit test proves *every* kind / figure / random grid quantizes without an unverified token.
- `C5` **The generator core is unchanged** (pure, seeded) — this phase is integration + the legato-safety projection change only.
- `C6` **Reuse existing seams** — the multi-track renderer, the `InstrumentPart` union, `HarmonyControlsR`, `GenerateExercise` / `ExerciseRefs`; no new architecture.
