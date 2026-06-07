---
type: plan
id: pl_01KTHKHSCQE2D3CZFRHQG6KVPD
title: Phase 1 — Engine & Renderer
status: active
created: 2026-06-07
version: 1
design_version: 1
req_version: 1
tags: []
parent_id: de_01KTHJD3QTBGRVX3BBRD29PKAW
requires_load: []
target_version: 0.1.0
---
# Phase 1 — Engine & Renderer

## Goal

Deliver the pure, fully unit-tested C# core: the Domain kernel, transposition, seed data, and the Exercise→alphaTex renderer. No UI, no I/O — every step is testable in isolation. Satisfies req IN1–IN5, IN11; constraints C1, C3, C4, C5.
---

## Steps

| Done | # | Step | Files touched | Blocked by | Satisfies |
|---|---|---|---|---|---|
| 🔳 | 1 | Scaffold the solution: ChordFlow.App host project + ChordFlow.Tests (xUnit), targeting net9.0; create the Domain/ Rendering/ Features/ Infrastructure/ wwwroot/ folders; add .gitignore. | — | — | — |
| 🔳 | 2 | Define the Domain kernel types as immutable records: PitchClass, Key, Quality, Chord, Progression, RomanDegree, RhythmPattern, Beat, Duration, Voicing, FretPosition, Difficulty, Exercise. | — | — | — |
| 🔳 | 3 | Implement the Transposer (Progression+Key -> Chord[]); add seed data: the 12-bar blues progression and the three rhythm patterns (beat-1, beat-1+3, quarters). Unit-test transposition across all 12 keys. | — | — | — |
| 🔳 | 4 | Implement VoicingBook.Lookup(Chord, Difficulty) with a hand-authored beginner shell-voicing table (Bb, Eb, F at minimum); unit-test lookups. | — | — | — |
| 🔳 | 5 | Implement AlphaTexRenderer (Exercise -> alphaTex string) following loom/refs/alphatex-syntax-reference.md (stateful :N durations, ( ) chord groups, r rests, \ts/\ks/\tempo). Unit-test: known Exercise -> expected alphaTex string. | — | — | — |
---

### Legend

| Symbol | Meaning |
|--------|---------|
| ✅ | Done |
| 🔄 | In Progress |
| 🔳 | Pending |
| ❌ | Cancelled |
