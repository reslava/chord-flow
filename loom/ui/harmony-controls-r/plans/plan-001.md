---
type: plan
id: pl_01KXN9S3C4E1KYX8J3YAH4CXGN
title: HarmonyControlsR + one Practice page — implementation
status: done
created: 2026-07-16
updated: 2026-07-16
version: 1
design_version: 1
req_version: 1
tags: []
parent_id: de_01KXN9DWFJG6QWET6VVFFS36HD
requires_load: []
target_version: 0.1.0
steps:
  - id: unified-reply-sheet-projection-rides-the
    order: 1
    status: done
    description: "Extend the generate/loadExercise reply to carry the chord-sheet projection over the SAME Exercise: sheet model (always including resolved comping-grip tone + diagram data), cellSchedule, and chordSchedule alongside tex/key/tempo/tripletFeel. ChordSheetBuilder is invoked from the generate/load pipeline as a projection (no separate resolve). Unit tests for the combined reply. Additive — the old chordSheet path still works until step 6."
    files_touched: [src/ChordFlow.Core/Features/GenerateExercise/GenerateExercise.cs, src/ChordFlow.Core/Features/ChordSheets/ChordSheetBuilder.cs, src/ChordFlow.Core/Features/ChordSheets/ChordSheetEnvelopes.cs, src/ChordFlow.Core/Features/ExerciseRendering.cs, tests/]
    blocked_by: []
    satisfies: [IN3, IN10]
  - id: harmonycontrolsr-component
    order: 2
    status: done
    description: "New harmony-controls-component.js: builds its own DOM (PlayerControlsR precedent) — harmony picker (optgroups, one population path via setCatalog(entityList payloads)), Key, Feel, Comping + Rhythm vol, Lead + Lead vol, Difficulty, voicing-fret min–max, Generate/Save/Mark practiced. Owns the definition state (getDefinition()); seeds key/feel on harmony switch (song values, else C/Straight — never blank) with manual edits surviving until the next switch; seedKey/seedTripletFeel for the load path; onHarmonySwitch hook for the shell to seed tempo into PlayerControlsR; volume sliders bound to the page engine (setTrackVolume). Plus index.html script tag + .cf-harmony-controls CSS."
    files_touched: [src/ChordFlow.Desktop/wwwroot/harmony-controls-component.js, src/ChordFlow.Desktop/wwwroot/index.html]
    blocked_by: []
    satisfies: [IN1, IN4, IN5, IN8, C1, C3]
  - id: scorer-slims-for-practice-volumes-opt
    order: 3
    status: done
    description: Add a volumes opt to ScoreR (default true) so the Practice page can mount it without the Rhythm/Lead sliders; key/tripletFeel opts already exist and simply stay off for Practice. Content-CRUD preview keeps its current opts untouched.
    files_touched: [src/ChordFlow.Desktop/wwwroot/score-render-component.js]
    blocked_by: []
    satisfies: [C2]
  - id: sheet-view-module-chord-sheets-js
    order: 4
    status: done
    description: "Refactor chord-sheets.js from a standalone page shell into a Sheet VIEW module: create(container) mounting ChordSheetR + the sheet-specific strip (Layout, Chords, + line, Below cell, Tone labels, Theme, Marker mode) + the three exports and the #chord-sheet-print PDF flow; render(sheet)/setSchedules(cellSchedule)/onBeat(bar,beat) driven by the shell. Drops: its own engine, PlayerControlsR, Now/Next, Sheet/Key combos, entityList merging, Show tab. Below cell becomes a pure display toggle (the model always carries tone/diagram data)."
    files_touched: [src/ChordFlow.Desktop/wwwroot/chord-sheets.js]
    blocked_by: [unified-reply-sheet-projection-rides-the]
    satisfies: [IN9, IN10, C5]
  - id: practice-shell-rewire-one-page-score
    order: 5
    status: done
    description: "Rework app.js + index.html into the single-page shape: remove the static #builder HTML and the Chord Sheets nav/view; mount HarmonyControlsR (fed entityList payloads, wired to sendDefinition/save/markPracticed and the tempo-seed hook) + the Score ⇄ Sheet segmented toggle at the head of the transport strip; ScoreR mounted with key/feel/volumes off; one reply handler feeds ScoreR.load(tex), sheetView.render(sheet), schedules, and the seeds (key/feel → HarmonyControlsR, tempo → PlayerControlsR, override wins on load); beat fan-out drives Now/Next + sheet marker in both views; both surfaces stay mounted so the toggle works mid-playback (visibility/collapsed-overflow hiding, re-render() on reveal if needed)."
    files_touched: [src/ChordFlow.Desktop/wwwroot/app.js, src/ChordFlow.Desktop/wwwroot/index.html]
    blocked_by: [unified-reply-sheet-projection-rides-the, harmonycontrolsr-component, scorer-slims-for-practice-volumes-opt, sheet-view-module-chord-sheets-js]
    satisfies: [IN2, IN6, IN7, C1, C3, C4]
  - id: retire-the-chordsheet-request-path
    order: 6
    status: done
    description: Remove the chordSheet request routing and its request-side handler/envelopes from Core + Program.cs wiring (the builder stays as the projection invoked by step 1; exportChordSheet/chordSheetPdfDone print flow stays). Remove any dead JS references. Build + full test suite green.
    files_touched: [src/ChordFlow.Core/Features/ChordSheets/ChordSheetHandler.cs, src/ChordFlow.Core/Features/ChordSheets/ChordSheetEnvelopes.cs, src/ChordFlow.Core/Bridge/WebMessageRouter.cs, src/ChordFlow.Desktop/Program.cs]
    blocked_by: [practice-shell-rewire-one-page-score]
    satisfies: [IN3]
  - id: end-to-end-verification-architecture-ref
    order: 7
    status: done
    description: "Run the app and walk the idea's Validation scenarios: mid-playback Score ⇄ Sheet toggle (audio continues, cursor + marker track the same beat, Now/Next in sync); comping/difficulty/voicing-window changes reflected in the sheet's playback and below-cell diagrams; key/feel seeding + survive-until-switch in both views; Save from Sheet view + library load restoring the definition. Update loom/refs/chordflow-architecture-reference.md (page structure + bridge envelope changes) in the same unit of work."
    files_touched: [loom/refs/chordflow-architecture-reference.md]
    blocked_by: [retire-the-chordsheet-request-path]
    satisfies: [C6]
---
# HarmonyControlsR + one Practice page — implementation

## Goal

Implement the 1-page convergence designed in design.md: extract the shared HarmonyControlsR definition strip (harmony/key/feel/comping+vol/lead+vol/difficulty/voicing window/actions), merge the Chord Sheets page into the Practice page as a Score ⇄ Sheet view toggle over one engine/PlayerControlsR/Now-Next/library, and unify the bridge so generate/loadExercise replies carry both projections (score alphaTex + chord-sheet model + shared schedules), retiring the chordSheet request. C# projection work lands first (additive), then the JS components, then the shell rewire, then cleanup + end-to-end verification with the architecture ref updated.

---

## Steps

| Done | # | Step | Files touched | Blocked by | Satisfies |
|---|---|---|---|---|---|
| ✅ | 1 | Extend the generate/loadExercise reply to carry the chord-sheet projection over the SAME Exercise: sheet model (always including resolved comping-grip tone + diagram data), cellSchedule, and chordSchedule alongside tex/key/tempo/tripletFeel. ChordSheetBuilder is invoked from the generate/load pipeline as a projection (no separate resolve). Unit tests for the combined reply. Additive — the old chordSheet path still works until step 6. | src/ChordFlow.Core/Features/GenerateExercise/GenerateExercise.cs, src/ChordFlow.Core/Features/ChordSheets/ChordSheetBuilder.cs, src/ChordFlow.Core/Features/ChordSheets/ChordSheetEnvelopes.cs, src/ChordFlow.Core/Features/ExerciseRendering.cs, tests/ | — | IN3, IN10 |
| ✅ | 2 | New harmony-controls-component.js: builds its own DOM (PlayerControlsR precedent) — harmony picker (optgroups, one population path via setCatalog(entityList payloads)), Key, Feel, Comping + Rhythm vol, Lead + Lead vol, Difficulty, voicing-fret min–max, Generate/Save/Mark practiced. Owns the definition state (getDefinition()); seeds key/feel on harmony switch (song values, else C/Straight — never blank) with manual edits surviving until the next switch; seedKey/seedTripletFeel for the load path; onHarmonySwitch hook for the shell to seed tempo into PlayerControlsR; volume sliders bound to the page engine (setTrackVolume). Plus index.html script tag + .cf-harmony-controls CSS. | src/ChordFlow.Desktop/wwwroot/harmony-controls-component.js, src/ChordFlow.Desktop/wwwroot/index.html | — | IN1, IN4, IN5, IN8, C1, C3 |
| ✅ | 3 | Add a volumes opt to ScoreR (default true) so the Practice page can mount it without the Rhythm/Lead sliders; key/tripletFeel opts already exist and simply stay off for Practice. Content-CRUD preview keeps its current opts untouched. | src/ChordFlow.Desktop/wwwroot/score-render-component.js | — | C2 |
| ✅ | 4 | Refactor chord-sheets.js from a standalone page shell into a Sheet VIEW module: create(container) mounting ChordSheetR + the sheet-specific strip (Layout, Chords, + line, Below cell, Tone labels, Theme, Marker mode) + the three exports and the #chord-sheet-print PDF flow; render(sheet)/setSchedules(cellSchedule)/onBeat(bar,beat) driven by the shell. Drops: its own engine, PlayerControlsR, Now/Next, Sheet/Key combos, entityList merging, Show tab. Below cell becomes a pure display toggle (the model always carries tone/diagram data). | src/ChordFlow.Desktop/wwwroot/chord-sheets.js | unified-reply-sheet-projection-rides-the | IN9, IN10, C5 |
| ✅ | 5 | Rework app.js + index.html into the single-page shape: remove the static #builder HTML and the Chord Sheets nav/view; mount HarmonyControlsR (fed entityList payloads, wired to sendDefinition/save/markPracticed and the tempo-seed hook) + the Score ⇄ Sheet segmented toggle at the head of the transport strip; ScoreR mounted with key/feel/volumes off; one reply handler feeds ScoreR.load(tex), sheetView.render(sheet), schedules, and the seeds (key/feel → HarmonyControlsR, tempo → PlayerControlsR, override wins on load); beat fan-out drives Now/Next + sheet marker in both views; both surfaces stay mounted so the toggle works mid-playback (visibility/collapsed-overflow hiding, re-render() on reveal if needed). | src/ChordFlow.Desktop/wwwroot/app.js, src/ChordFlow.Desktop/wwwroot/index.html | unified-reply-sheet-projection-rides-the, harmonycontrolsr-component, scorer-slims-for-practice-volumes-opt, sheet-view-module-chord-sheets-js | IN2, IN6, IN7, C1, C3, C4 |
| ✅ | 6 | Remove the chordSheet request routing and its request-side handler/envelopes from Core + Program.cs wiring (the builder stays as the projection invoked by step 1; exportChordSheet/chordSheetPdfDone print flow stays). Remove any dead JS references. Build + full test suite green. | src/ChordFlow.Core/Features/ChordSheets/ChordSheetHandler.cs, src/ChordFlow.Core/Features/ChordSheets/ChordSheetEnvelopes.cs, src/ChordFlow.Core/Bridge/WebMessageRouter.cs, src/ChordFlow.Desktop/Program.cs | practice-shell-rewire-one-page-score | IN3 |
| ✅ | 7 | Run the app and walk the idea's Validation scenarios: mid-playback Score ⇄ Sheet toggle (audio continues, cursor + marker track the same beat, Now/Next in sync); comping/difficulty/voicing-window changes reflected in the sheet's playback and below-cell diagrams; key/feel seeding + survive-until-switch in both views; Save from Sheet view + library load restoring the definition. Update loom/refs/chordflow-architecture-reference.md (page structure + bridge envelope changes) in the same unit of work. | loom/refs/chordflow-architecture-reference.md | retire-the-chordsheet-request-path | C6 |
---

### Legend

| Symbol | Meaning |
|--------|---------|
| ✅ | Done |
| 🔄 | In Progress |
| 🔳 | Pending |
| ❌ | Cancelled |
