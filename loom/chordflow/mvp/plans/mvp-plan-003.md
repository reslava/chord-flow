---
type: plan
id: pl_01KTHKJ10P2215FZ5RP9VC1H27
title: Phase 3 — Persistence & UI
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
# Phase 3 — Persistence & UI

## Goal

Make it a usable trainer: persist exercise definitions in SQLite, add the library/progress slices, and wire the minimal UI controls. Satisfies req IN9, IN10; excludes EX1 (no accuracy detection). Depends on Phase 2.
---

## Steps

| Done | # | Step | Files touched | Blocked by | Satisfies |
|---|---|---|---|---|---|
| 🔳 | 1 | Add SQLite via EF Core: Exercises(Id, Key, ProgressionId, RhythmId, Tempo, Difficulty, CreatedUtc) and PracticeRecords(Id, ExerciseId, PracticedUtc) + initial migration. Store definitions only, never alphaTex. | — | — | — |
| 🔳 | 2 | Implement the ExerciseLibrary slice: save an Exercise definition, list saved exercises, reload one (regenerate alphaTex on load via AlphaTexRenderer). | — | — | — |
| 🔳 | 3 | Implement the Progress slice: mark an exercise practiced -> write a PracticeRecord. | — | — | — |
| 🔳 | 4 | Wire the minimal UI: key picker, rhythm picker, tempo, Generate, Play/Stop, Save, and the saved-exercise list — each control calls its slice through the bridge. | — | — | — |
---

### Legend

| Symbol | Meaning |
|--------|---------|
| ✅ | Done |
| 🔄 | In Progress |
| 🔳 | Pending |
| ❌ | Cancelled |
