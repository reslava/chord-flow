---
type: plan
id: pl_01KTM0Z0RPTP75F7CWBCZC8AZ1
title: Music-Theory Domain Model — Implementation Plan
status: done
created: "2026-06-08T00:00:00.000Z"
updated: "2026-06-08T00:00:00.000Z"
version: 3
design_version: 1
tags: []
parent_id: de_01KTM0DRF3Q7F4X35RMCBX6DDT
requires_load: []
target_version: 0.1.0
steps:
  - id: quality-interval-sets-chordtone-back-the
    order: 1
    status: done
    description: "Quality interval sets + ChordTone: back the Quality enum with the 8 v1 interval sets (C5) via an interval table; add a chord-relative ChordTone/interval type and a ChordTones(Chord) generator. Pure, additive. Unit-test chord-tone spelling for each quality."
    files_touched: []
    blocked_by: []
    satisfies: [IN3, IN4, C5]
  - id: scale-diatonic-generator-introduce-scale-tonic
    order: 2
    status: done
    description: "Scale + diatonic generator: introduce Scale(Tonic, Intervals); extract MajorOffsets/NaturalMinorOffsets from Transposer into Scale; refactor Transposer.Realize to consume a Scale; add BuildDiatonicChord(Scale, ScaleDegree). Unit-test C-major diatonic set (I Cmaj7 .. vii Bm7b5) and existing transposition tests still green."
    files_touched: []
    blocked_by: []
    satisfies: [IN2, IN5]
  - id: notespeller-add-pure-notespeller-pitchclass-key
    order: 3
    status: done
    description: "NoteSpeller: add pure NoteSpeller(PitchClass, Key) -> spelled name + key-signature token; promote the renderer's hardcoded MajorKeyName/MajorKeySignature arrays into the domain and have AlphaTexRenderer call the speller. Unit-test spelling across sharp keys (D, F#), flat keys (Ab, Bb), and C."
    files_touched: []
    blocked_by: []
    satisfies: [IN1, C2]
  - id: voicing-diagram-metadata-strategy-add-optional
    order: 4
    status: done
    description: "Voicing diagram metadata + strategy: add optional BarreFret/FirstFret/muted-strings metadata to Voicing; define IVoicingStrategy (Chord+Difficulty -> Voicing) and make the current algorithmic shell shape the Beginner strategy; VoicingBook resolves via strategy. Keep existing VoicingBook tests green; add a metadata test."
    files_touched: []
    blocked_by: []
    satisfies: [IN6, IN7]
  - id: tick-grid-rhythm-types-parallel-non
    order: 5
    status: done
    description: "Tick-grid rhythm types (parallel, non-breaking): add PPQ=48 constant, TimeSignature (4/4), RhythmEvent(Position, Length, Stroke, Accent), and a new tick-based RhythmPattern type ALONGSIDE the existing Beat model (no removals yet so the build stays green). Add a tick->alphaTex quantizer helper (events in tick order -> :N tokens, rests for gaps, ties across the grid) with focused unit tests. Port the 3 MVP patterns to tick definitions and model pickup/anacrusis as a leading measure."
    files_touched: []
    blocked_by: []
    satisfies: [IN8, IN9, IN11, IN12, IN13, C1, C7]
  - id: switch-to-the-tick-model-remove
    order: 6
    status: done
    description: "Switch to the tick model + remove the old one (atomic migration): repoint Exercise, SeedData, and AlphaTexRenderer at the tick-based RhythmPattern + quantizer; delete the old Beat/sequential RhythmPattern and the inline duration logic in the renderer; regenerate from definitions and wipe the dev SQLite DB rather than migrating persisted rows (respects EX3). Update AlphaTexRendererTests to the new model; full solution build + test pass."
    files_touched: []
    blocked_by: []
    satisfies: [IN12, IN13, IN15, C2, C7]
  - id: feel-accent-stroke-overlays-implement-feel
    order: 7
    status: done
    description: "Feel / Accent / Stroke overlays: implement Feel as a playback-time timing transform (straight grid -> long-short for Swing/Shuffle/Triplet) and AccentPattern + Stroke as composable layers applied onto a pattern; ensure Feel is never stored in RhythmPattern. Unit-test that composing pattern+accent+feel yields expected timing/accents without mutating the base pattern."
    files_touched: []
    blocked_by: []
    satisfies: [IN10, C4]
  - id: lead-targetzone-domain-layer-add-targetzone
    order: 8
    status: done
    description: "Lead TargetZone domain layer: add TargetZone(ChordTone, Importance); derive guide tones (3 & 7) from the interval sets and resolve TargetZones to fretboard FretPositions for a chord — domain types only, no fretboard UI (respects EX5). Unit-test ii-V-I guide-tone targets (e.g. G7 -> 3 and b7) resolve to correct pitch classes/positions."
    files_touched: []
    blocked_by: []
    satisfies: [IN14]
  - id: end-to-end-wiring-verification-assemble
    order: 9
    status: done
    description: "End-to-end wiring + verification: assemble the full Exercise pipeline (resolve progression -> choose voicings/targets -> apply rhythm+feel -> quantize -> alphaTex), run the complete test suite, and do a render smoke check that a Bb 12-bar blues exercise produces valid alphaTex through the new path."
    files_touched: []
    blocked_by: []
    satisfies: [IN15]
---
# Music-Theory Domain Model — Implementation Plan

## Goal

Migrate the ChordFlow domain kernel to a music-theory-first model: interval-backed qualities, first-class Scale + diatonic generation, key-aware spelling, strategy-based voicings, and a positional 48-PPQ tick-grid rhythm model with a grid→alphaTex quantizer, then rewire Exercise and add a lead TargetZone layer. Steps 1–4 are additive (MVP stays green); steps 5–6 perform the rhythm migration atomically; 7–9 complete overlays, lead targets, and end-to-end wiring. Satisfies req rq_01KTM0Y9B6JMQNJDA8THQG4WCR.
---

## Steps

| Done | # | Step | Files touched | Blocked by | Satisfies |
|---|---|---|---|---|---|
| ✅ | 1 | Quality interval sets + ChordTone: back the Quality enum with the 8 v1 interval sets (C5) via an interval table; add a chord-relative ChordTone/interval type and a ChordTones(Chord) generator. Pure, additive. Unit-test chord-tone spelling for each quality. | — | — | IN3, IN4, C5 |
| ✅ | 2 | Scale + diatonic generator: introduce Scale(Tonic, Intervals); extract MajorOffsets/NaturalMinorOffsets from Transposer into Scale; refactor Transposer.Realize to consume a Scale; add BuildDiatonicChord(Scale, ScaleDegree). Unit-test C-major diatonic set (I Cmaj7 .. vii Bm7b5) and existing transposition tests still green. | — | — | IN2, IN5 |
| ✅ | 3 | NoteSpeller: add pure NoteSpeller(PitchClass, Key) -> spelled name + key-signature token; promote the renderer's hardcoded MajorKeyName/MajorKeySignature arrays into the domain and have AlphaTexRenderer call the speller. Unit-test spelling across sharp keys (D, F#), flat keys (Ab, Bb), and C. | — | — | IN1, C2 |
| ✅ | 4 | Voicing diagram metadata + strategy: add optional BarreFret/FirstFret/muted-strings metadata to Voicing; define IVoicingStrategy (Chord+Difficulty -> Voicing) and make the current algorithmic shell shape the Beginner strategy; VoicingBook resolves via strategy. Keep existing VoicingBook tests green; add a metadata test. | — | — | IN6, IN7 |
| ✅ | 5 | Tick-grid rhythm types (parallel, non-breaking): add PPQ=48 constant, TimeSignature (4/4), RhythmEvent(Position, Length, Stroke, Accent), and a new tick-based RhythmPattern type ALONGSIDE the existing Beat model (no removals yet so the build stays green). Add a tick->alphaTex quantizer helper (events in tick order -> :N tokens, rests for gaps, ties across the grid) with focused unit tests. Port the 3 MVP patterns to tick definitions and model pickup/anacrusis as a leading measure. | — | — | IN8, IN9, IN11, IN12, IN13, C1, C7 |
| ✅ | 6 | Switch to the tick model + remove the old one (atomic migration): repoint Exercise, SeedData, and AlphaTexRenderer at the tick-based RhythmPattern + quantizer; delete the old Beat/sequential RhythmPattern and the inline duration logic in the renderer; regenerate from definitions and wipe the dev SQLite DB rather than migrating persisted rows (respects EX3). Update AlphaTexRendererTests to the new model; full solution build + test pass. | — | — | IN12, IN13, IN15, C2, C7 |
| ✅ | 7 | Feel / Accent / Stroke overlays: implement Feel as a playback-time timing transform (straight grid -> long-short for Swing/Shuffle/Triplet) and AccentPattern + Stroke as composable layers applied onto a pattern; ensure Feel is never stored in RhythmPattern. Unit-test that composing pattern+accent+feel yields expected timing/accents without mutating the base pattern. | — | — | IN10, C4 |
| ✅ | 8 | Lead TargetZone domain layer: add TargetZone(ChordTone, Importance); derive guide tones (3 & 7) from the interval sets and resolve TargetZones to fretboard FretPositions for a chord — domain types only, no fretboard UI (respects EX5). Unit-test ii-V-I guide-tone targets (e.g. G7 -> 3 and b7) resolve to correct pitch classes/positions. | — | — | IN14 |
| ✅ | 9 | End-to-end wiring + verification: assemble the full Exercise pipeline (resolve progression -> choose voicings/targets -> apply rhythm+feel -> quantize -> alphaTex), run the complete test suite, and do a render smoke check that a Bb 12-bar blues exercise produces valid alphaTex through the new path. | — | — | IN15 |
---

### Legend

| Symbol | Meaning |
|--------|---------|
| ✅ | Done |
| 🔄 | In Progress |
| 🔳 | Pending |
| ❌ | Cancelled |
