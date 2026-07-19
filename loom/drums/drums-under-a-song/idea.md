---
type: idea
id: id_01KXWNH0AD17BA9B918QT6HDM6
title: Drums under a song — the drum track (phase 2)
status: draft
created: 2026-07-19
version: 1
tags: []
parent_id: null
requires_load: []
---
# Drums under a song — the drum track (phase 2)

Phase 2 of drums, deferred out of `drums/basic-drums` (MVP = a standalone drum groove as its own content kind, no harmony). This thread makes a groove **accompany a harmonic exercise** — drums *under* a 12-bar blues, not drums alone.

## The core problem this thread must solve

**A drum groove has no harmony, but the current `Exercise` requires a `Song`.** MVP sidesteps this by keeping grooves standalone. Phase 2 cannot — a drum track plays *alongside* a progression/song, so the play-unit must hold both a harmonic part and a percussion part. This is the deliberate `Exercise`/`Song` remodel we chose **not** to do by accident in MVP.

## Known foundation (from `basic-drums` chat-001)

- **The renderer already emits multiple `\track` staves** — `AlphaTexRenderer` produces 2 today (comping + a dead-note lead). A drum track is a **3rd `\track`** with `\instrument percussion` / `\articulation defaults`, its notes the groove's articulation-name hits.
- **The groove domain already exists** after `basic-drums`: `DrumGroove` (multi-lane over the 48-PPQ tick grid) in `Instruments/Drums/`, compiled from the hit-grid DSL. This thread *consumes* it — no new groove model needed.
- **`IInstrument` / renderer fork** is tracked separately in `chordflow/instrument-rendering`; the multi-track percussion render likely leans on whatever seam that thread lands.

## Open design questions (for this thread's design, not now)

- **Play-unit shape:** does `Exercise` gain an optional `Drums` (a `DrumGroove`), the way it has an optional `Lead`? Or a more general "instrument parts" list so a 4th instrument is additive? (Prefer the durable one — decide then.)
- **Tiling / length:** a groove is a 1–2 bar loop; the progression can be 12+ bars. The groove **tiles cyclically** onto the progression's bars, same as multi-bar rhythm patterns already do (`progression bar i → pattern bar i % m`).
- **UI:** where does the drum groove get *selected* for an exercise — a new picker in HarmonyControlsR alongside comping/lead? And does the Practice render surface show the drum staff, DrumsR, or both?
- **Feel/swing:** a swung groove and a swung comp must agree — both ride the same play-time `\tf`, so no per-track feel.

## Dogfood

Drums audible under a real progression on the Practice page, tiling across the full form, staying in sync with the comp through the shared playback beat/position bus.
