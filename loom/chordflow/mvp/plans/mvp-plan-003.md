---
type: plan
id: pl_01KTHKJ10P2215FZ5RP9VC1H27
title: Phase 3 — Persistence & UI
status: done
created: "2026-06-07T00:00:00.000Z"
updated: 2026-06-08
version: 2
design_version: 1
req_version: 1
tags: []
parent_id: de_01KTHJD3QTBGRVX3BBRD29PKAW
requires_load: []
target_version: 0.1.0
---
# Phase 3 — Persistence & UI

## Goal

Make it a usable trainer: persist exercise **definitions** in SQLite, add the ExerciseLibrary/Progress slices, and wire the minimal UI controls. Satisfies req IN9, IN10; honours constraints C2 (fully offline, no server), C1/C3 (UI-agnostic vertical slices, no MediatR). Hard boundary: EX1 — no audio-input accuracy detection or scoring; Progress records *that* an exercise was practiced, nothing more. Depends on Phase 2.
---

## Steps

| Done | # | Step | Files touched | Blocked by | Satisfies |
|---|---|---|---|---|---|
| ✅ | 1 | Add SQLite via EF Core: ChordFlowDbContext with Exercises(Id, Key, ProgressionId, RhythmId, Tempo, Difficulty, CreatedUtc) and PracticeRecords(Id, ExerciseId, PracticedUtc) + initial migration. Store the **definition** fields only, never alphaTex. Local file DB — no server, no network. | src/ChordFlow.App/Infrastructure/ChordFlowDbContext.cs, src/ChordFlow.App/Infrastructure/Entities/ExerciseEntity.cs, src/ChordFlow.App/Infrastructure/Entities/PracticeRecordEntity.cs, src/ChordFlow.App/Migrations/*, src/ChordFlow.App/ChordFlow.App.csproj | — | IN9, C2 |
| ✅ | 2 | Implement the ExerciseLibrary slice: save an Exercise definition, list saved exercises, reload one — regenerating alphaTex on load via AlphaTexRenderer (never persisting it). Wire save/list/load envelopes through WebMessageRouter. | src/ChordFlow.App/Features/ExerciseLibrary/ExerciseLibrary.cs, src/ChordFlow.App/Infrastructure/WebMessageRouter.cs | 1 | IN9, IN10, C1, C3 |
| ✅ | 3 | Implement the Progress slice: on "mark practiced" write a PracticeRecord for the active exercise via the bridge. Records the practice event only — no accuracy/scoring. | src/ChordFlow.App/Features/Progress/Progress.cs, src/ChordFlow.App/Infrastructure/WebMessageRouter.cs | 1 | IN10, C1, C3 |
| ✅ | 4 | Wire the minimal UI: key picker, rhythm picker, tempo, Generate, Play/Stop, Save, Mark-practiced, and the saved-exercise list — each control posts an envelope that routes to its slice through the bridge. | src/ChordFlow.App/wwwroot/index.html, src/ChordFlow.App/wwwroot/app.js, src/ChordFlow.App/Infrastructure/WebMessageRouter.cs | 2, 3 | IN10 |
---

### Legend

| Symbol | Meaning |
|--------|---------|
| ✅ | Done |
| 🔄 | In Progress |
| 🔳 | Pending |
| ❌ | Cancelled |
