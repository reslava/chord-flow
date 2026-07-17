---
type: plan
id: pl_01KXQGHZWSE9J103VHHC2GTQFX
title: Harmonic analyzer — engine + golden tests
status: done
created: 2026-07-17
updated: 2026-07-17
version: 1
design_version: 2
req_version: 2
tags: []
parent_id: de_01KXQFJYNF0D10R9B5VR3JP930
requires_load: []
target_version: 0.1.0
steps:
  - id: output-types
    order: 1
    status: done
    description: "Add the structured analysis result types: HarmonicCategory (Diatonic, SecondaryDominant, SecondaryLeadingTone, Borrowed, TritoneSub, Chromatic), KeyMode (Major, Minor), and the ChordAnalysis record (Category, Function: RomanDegree, Target: ScaleDegree?, SourceMode: KeyMode?)."
    files_touched: [src/ChordFlow.Core/Music/Harmony/HarmonicCategory.cs, src/ChordFlow.Core/Music/Harmony/ChordAnalysis.cs]
    blocked_by: []
    satisfies: [IN4, IN5, C1, C3]
  - id: diatonic-classification-honest-function
    order: 2
    status: done
    description: "Implement HarmonicAnalyzer.Analyze(Chord, Key): compute rootOffset, diatonic detection via Scale.ForKey + DiatonicChord.Build with triad-vs-7th tolerance, and the honest Function (RomanDegree) format-compatible with ChordSheetBuilder.RomanFunction. Returns Diatonic when it matches, else a Chromatic placeholder pending step 3."
    files_touched: [src/ChordFlow.Core/Music/Harmony/HarmonicAnalyzer.cs]
    blocked_by: [output-types]
    satisfies: [IN1, IN2, IN7, IN9, C1]
  - id: non-diatonic-classifiers-precedence
    order: 3
    status: done
    description: "Add the non-diatonic classifiers in fixed precedence: SecondaryDominant (V/x, Target set), SecondaryLeadingTone (vii°/x, Target set), TritoneSub (bII7 -> V, Target set), Borrowed (parallel-mode diatonic match, SourceMode set), Chromatic fallback."
    files_touched: [src/ChordFlow.Core/Music/Harmony/HarmonicAnalyzer.cs]
    blocked_by: [diatonic-classification-honest-function]
    satisfies: [IN5, IN6, C4]
  - id: minor-tonic-sequence-api
    order: 4
    status: done
    description: Ensure symmetric major<->minor handling (parallel-mode borrowing both directions; minor iiø-V-i, Picardy), and add the sequence API Analyze(IReadOnlyList<(Chord, Key)>) with a per-position key.
    files_touched: [src/ChordFlow.Core/Music/Harmony/HarmonicAnalyzer.cs]
    blocked_by: [non-diatonic-classifiers-precedence]
    satisfies: [IN3, IN8, C2]
  - id: golden-oracle-tests
    order: 5
    status: done
    description: "Golden tests over inline fixtures: ii-V-I, I-vi-ii-V, circle of secondary dominants, borrowed iv/bVII/bVI-bVII, bII7 tritone sub, Tadd Dameron turnaround, #iv°, chromatic passing #i°7, minor iiø-V-i, Picardy third, and the dominant-blues must-not-over-label stress test. Cover every category and both major and minor tonic."
    files_touched: [tests/ChordFlow.Core.Tests/Harmony/HarmonicAnalyzerTests.cs]
    blocked_by: [minor-tonic-sequence-api]
    satisfies: [IN8, IN10, C4]
---
# Harmonic analyzer — engine + golden tests

## Goal

Implement the pure harmonic-analysis pass per the design/req: the structured ChordAnalysis output types (with the split SecondaryDominant / SecondaryLeadingTone categories), the pitch-based HarmonicAnalyzer (diatonic detection reusing DiatonicChord/Scale.ForKey with triad-vs-7th tolerance; the secondary-dominant, secondary-leading-tone, tritone-sub, borrowed, and chromatic classifiers in fixed precedence; symmetric major & minor tonic), the per-position-keyed sequence API, and golden-oracle tests over inline fixtures including the dominant-blues must-not-over-label case. A Harmony sink — no I/O, immutable, frozen by MusicLayeringTests. The RealizedSong→sequence adapter (thread 3) and any minor-key DSL/UI (thread 2) are out of scope; the default-pack progression content + description field are a separate deliverable pending the metadata decision.

---

## Steps

| Done | # | Step | Files touched | Blocked by | Satisfies |
|---|---|---|---|---|---|
| ✅ | 1 | Add the structured analysis result types: HarmonicCategory (Diatonic, SecondaryDominant, SecondaryLeadingTone, Borrowed, TritoneSub, Chromatic), KeyMode (Major, Minor), and the ChordAnalysis record (Category, Function: RomanDegree, Target: ScaleDegree?, SourceMode: KeyMode?). | src/ChordFlow.Core/Music/Harmony/HarmonicCategory.cs, src/ChordFlow.Core/Music/Harmony/ChordAnalysis.cs | — | IN4, IN5, C1, C3 |
| ✅ | 2 | Implement HarmonicAnalyzer.Analyze(Chord, Key): compute rootOffset, diatonic detection via Scale.ForKey + DiatonicChord.Build with triad-vs-7th tolerance, and the honest Function (RomanDegree) format-compatible with ChordSheetBuilder.RomanFunction. Returns Diatonic when it matches, else a Chromatic placeholder pending step 3. | src/ChordFlow.Core/Music/Harmony/HarmonicAnalyzer.cs | output-types | IN1, IN2, IN7, IN9, C1 |
| ✅ | 3 | Add the non-diatonic classifiers in fixed precedence: SecondaryDominant (V/x, Target set), SecondaryLeadingTone (vii°/x, Target set), TritoneSub (bII7 -> V, Target set), Borrowed (parallel-mode diatonic match, SourceMode set), Chromatic fallback. | src/ChordFlow.Core/Music/Harmony/HarmonicAnalyzer.cs | diatonic-classification-honest-function | IN5, IN6, C4 |
| ✅ | 4 | Ensure symmetric major<->minor handling (parallel-mode borrowing both directions; minor iiø-V-i, Picardy), and add the sequence API Analyze(IReadOnlyList<(Chord, Key)>) with a per-position key. | src/ChordFlow.Core/Music/Harmony/HarmonicAnalyzer.cs | non-diatonic-classifiers-precedence | IN3, IN8, C2 |
| ✅ | 5 | Golden tests over inline fixtures: ii-V-I, I-vi-ii-V, circle of secondary dominants, borrowed iv/bVII/bVI-bVII, bII7 tritone sub, Tadd Dameron turnaround, #iv°, chromatic passing #i°7, minor iiø-V-i, Picardy third, and the dominant-blues must-not-over-label stress test. Cover every category and both major and minor tonic. | tests/ChordFlow.Core.Tests/Harmony/HarmonicAnalyzerTests.cs | minor-tonic-sequence-api | IN8, IN10, C4 |
---

### Legend

| Symbol | Meaning |
|--------|---------|
| ✅ | Done |
| 🔄 | In Progress |
| 🔳 | Pending |
| ❌ | Cancelled |
