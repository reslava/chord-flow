---
type: idea
id: id_01KY2NM0ENXKAYFQB6ZE8C3AZG
title: Generated rhythms in Practice
status: draft
created: 2026-07-21
version: 1
tags: []
parent_id: null
requires_load: []
---
# Generated rhythms in Practice

## Goal

Make the rhythm generator **actually useful** — a generated rhythm becomes a **real track over a song/progression** in the Practice/Exercise page, not just a sandbox output. This is the payoff of the whole `generated-rhythms-for-practice` line and the app's north star: practice groove/time-feel *in context*, over harmony.

**On-the-fly / ephemeral (req IN10):** the generation is chosen in the Practice controls and used for the current exercise; **persisting** it (`{strategy, params, seed}` into a saved exercise) is [[save-generation-into-exercise]] (Phase 5). This thread just wires the generator output into the Generate flow.

## The four ways a generated rhythm plugs in (Rafa, chat-001)

1. **Comping track** — the generated rhythm becomes the **comping** `RhythmPattern` (replaces the picked comping pattern).
2. **Lead track** — becomes the **lead** `RhythmPattern`.
3. **Drums track** — the single-voice `DrumGroove` becomes the **drum part**.
4. **Extra track** — a **new** rhythm layer on its own instrument voice, **with its own volume slider**. Default an **extra drums track on closed hi-hat** (a time-keeper under everything); optionally a **metronome-like instrument** (a click/woodblock voice). Additive to whatever comping/lead/drums are already playing.

All four ride the same generated `OnsetGrid` via the existing projections (comping/lead ← `RhythmPattern`; drums/extra ← `DrumGroove`).

## Integration seams (already in place)

- **`InstrumentPart` union** — `Exercise(Song, Parts[], …)` with `CompingPart`/`LeadPart`/`DrumPart` (each carrying `Volume`/`Muted`). The **extra track is a new arm** (a `GeneratedPart`/`ExtraPart` on a chosen voice) — a non-breaking additive seam, exactly how `DrumPart` was added.
- **`HarmonyControlsR`** — the definition strip (comping/lead/drums pickers + volumes). Gains a way to set a track's **rhythm source = "generated"** (with a compact generator control + params) instead of a catalog id, plus the **extra-track control + volume**.
- **`GenerateExercise` / `ExerciseRefs`** — resolve the chosen references into the `Exercise`. A generated track resolves the `{strategy, params, seed}` → `OnsetGrid` → the projection for that part.
- **`AlphaTexRenderer`** — already multi-track (comping + lead + percussion `\track`s); the extra track is another `\track`.

## Prerequisite — legato safety (the Phase-4 finding)

Comping/lead use the **legato** `OnsetGrid → RhythmPattern` projection, whose ring-to-barline can produce a **non-notatable** length for arbitrary syncopated bars (surfaced in plan-004; `design.md §8` of the generator thread). Before generated *syncopated* rhythms can comp/lead, that projection needs a **notatable-safe policy** — snap the ring to the largest notatable value + rest the remainder, or a verified tie. The **drums + extra paths are already safe** (hit + rest), so **drums/extra into Practice can land first**, with comping/lead following once legato-safety is done.

## Open design decisions (for `design.md`)

1. **Legato-safety policy** — snap-and-rest vs. a verified tie (and its render-verification).
2. **Extra-track instrument** — a drum voice (default closed hi-hat) vs. a pitched "metronome" click; is it a new `InstrumentPart` arm or a second `DrumPart` with a role tag?
3. **How generation params reach Practice** — a compact inline generator control in HarmonyControlsR vs. "pick a preset/figure" vs. a link to the full Rhythm Generator page; and the bridge `generate` verb's shape for a generated track.
4. **Reference pulse in Practice** — off by default here (the song is the reference), per IN8.

## Validation / dogfood

- Generate a rhythm and hear it **comp / lead / drum over a real song** in Practice.
- The **extra closed-hi-hat track** as an audible time-keeper under a progression, with a working volume slider.
- Drums/extra path first (no legato dependency), then comping/lead after legato-safety.
