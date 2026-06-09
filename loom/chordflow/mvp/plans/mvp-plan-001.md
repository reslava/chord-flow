---
type: plan
id: pl_01KTHKHSCQE2D3CZFRHQG6KVPD
title: Phase 1 — Engine & Renderer
status: done
created: "2026-06-07T00:00:00.000Z"
updated: "2026-06-08T00:00:00.000Z"
version: 2
design_version: 1
req_version: 1
tags: []
parent_id: de_01KTHJD3QTBGRVX3BBRD29PKAW
requires_load: []
target_version: 0.1.0
steps:
  - id: scaffold-the-solution-chordflow
    order: 1
    status: done
    description: "Scaffold the solution: ChordFlow.App host project + ChordFlow.Tests (xUnit), targeting net9.0; create the Domain/ Rendering/ Features/ Infrastructure/ wwwroot/ folders; add .gitignore."
    files_touched: [ChordFlow.sln, src/ChordFlow.App/ChordFlow.App.csproj, tests/ChordFlow.Tests/ChordFlow.Tests.csproj, .gitignore]
    blocked_by: []
    satisfies: [C1, C3]
  - id: define-the-domain-kernel-types-as
    order: 2
    status: done
    description: "Define the Domain kernel types as immutable records: PitchClass, Key, Quality, Chord, Progression, RomanDegree, RhythmPattern, Beat, Duration, Voicing, FretPosition, Difficulty, Exercise."
    files_touched: ["src/ChordFlow.App/Domain/*.cs"]
    blocked_by: [1]
    satisfies: [IN1, C4]
  - id: implement-the-transposer-progression-key-chord
    order: 3
    status: done
    description: "Implement the Transposer (Progression+Key -> Chord[]); add seed data: the 12-bar blues progression and the three rhythm patterns (beat-1, beat-1+3, quarters). Unit-test transposition across all 12 keys."
    files_touched: [src/ChordFlow.App/Domain/Transposer.cs, src/ChordFlow.App/Domain/SeedData.cs, tests/ChordFlow.Tests/TransposerTests.cs]
    blocked_by: [2]
    satisfies: [IN1, IN2, IN3, IN11, C4]
  - id: implement-voicingbook
    order: 4
    status: done
    description: Implement VoicingBook.Lookup(Chord, Difficulty) with a hand-authored beginner shell-voicing table (Bb, Eb, F at minimum); unit-test lookups.
    files_touched: [src/ChordFlow.App/Domain/VoicingBook.cs, tests/ChordFlow.Tests/VoicingBookTests.cs]
    blocked_by: [2]
    satisfies: [IN1, IN4, IN11, C4]
  - id: implement-alphatexrenderer-exercise-alphatex-string-following
    order: 5
    status: done
    description: "Implement AlphaTexRenderer (Exercise -> alphaTex string) following loom/refs/alphatex-syntax-reference.md (stateful :N durations, ( ) chord groups, r rests, \\ts/\\ks/\\tempo). Unit-test: known Exercise -> expected alphaTex string."
    files_touched: [src/ChordFlow.App/Rendering/IScoreRenderer.cs, src/ChordFlow.App/Rendering/AlphaTexRenderer.cs, tests/ChordFlow.Tests/AlphaTexRendererTests.cs]
    blocked_by: [3, 4]
    satisfies: [IN5, IN11, C5]
---
# Phase 1 — Engine & Renderer

## Goal

Deliver the pure, fully unit-tested C# core: the Domain kernel, transposition, seed data, and the Exercise→alphaTex renderer. No UI, no I/O — every step is testable in isolation. Satisfies req IN1–IN5, IN11; constraints C1, C3, C4, C5.
---

## Steps

| Done | # | Step | Files touched | Blocked by | Satisfies |
|---|---|---|---|---|---|
| ✅ | 1 | Scaffold the solution: ChordFlow.App host project + ChordFlow.Tests (xUnit), targeting net9.0; create the Domain/ Rendering/ Features/ Infrastructure/ wwwroot/ folders; add .gitignore. | ChordFlow.sln, src/ChordFlow.App/ChordFlow.App.csproj, tests/ChordFlow.Tests/ChordFlow.Tests.csproj, .gitignore | — | C1, C3 |
| ✅ | 2 | Define the Domain kernel types as immutable records: PitchClass, Key, Quality, Chord, Progression, RomanDegree, RhythmPattern, Beat, Duration, Voicing, FretPosition, Difficulty, Exercise. | src/ChordFlow.App/Domain/*.cs | 1 | IN1, C4 |
| ✅ | 3 | Implement the Transposer (Progression+Key -> Chord[]); add seed data: the 12-bar blues progression and the three rhythm patterns (beat-1, beat-1+3, quarters). Unit-test transposition across all 12 keys. | src/ChordFlow.App/Domain/Transposer.cs, src/ChordFlow.App/Domain/SeedData.cs, tests/ChordFlow.Tests/TransposerTests.cs | 2 | IN1, IN2, IN3, IN11, C4 |
| ✅ | 4 | Implement VoicingBook.Lookup(Chord, Difficulty) with a hand-authored beginner shell-voicing table (Bb, Eb, F at minimum); unit-test lookups. | src/ChordFlow.App/Domain/VoicingBook.cs, tests/ChordFlow.Tests/VoicingBookTests.cs | 2 | IN1, IN4, IN11, C4 |
| ✅ | 5 | Implement AlphaTexRenderer (Exercise -> alphaTex string) following loom/refs/alphatex-syntax-reference.md (stateful :N durations, ( ) chord groups, r rests, \ts/\ks/\tempo). Unit-test: known Exercise -> expected alphaTex string. | src/ChordFlow.App/Rendering/IScoreRenderer.cs, src/ChordFlow.App/Rendering/AlphaTexRenderer.cs, tests/ChordFlow.Tests/AlphaTexRendererTests.cs | 3, 4 | IN5, IN11, C5 |
---

### Legend

| Symbol | Meaning |
|--------|---------|
| ✅ | Done |
| 🔄 | In Progress |
| 🔳 | Pending |
| ❌ | Cancelled |