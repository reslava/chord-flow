---
type: plan
id: pl_01KVT08RFQN5ZES525DQKBWEWK
title: Now/Next Fretboards — Implementation
status: done
created: 2026-06-23
updated: 2026-06-23
version: 1
design_version: 1
req_version: 1
tags: []
parent_id: de_01KVSZBZQ1WZB5S31GSNS0F3QD
requires_load: []
target_version: 0.1.0
steps:
  - id: real-root-voicing-diagram-producer
    order: 1
    status: done
    description: Add RealizedVoicingDiagram.Build(chord, voicing, key) producing a real-root FretboardDiagram of a concrete Voicing, with xUnit tests.
    files_touched: [src/ChordFlow.Core/Instruments/Guitar/Diagrams/RealizedVoicingDiagram.cs, tests/ChordFlow.Core.Tests/RealizedVoicingDiagramTests.cs]
    blocked_by: []
    satisfies: [IN2, C2, C5, C7]
  - id: render-pass-emits-the-chord-schedule
    order: 2
    status: done
    description: Introduce ChordChange + make the render seam return (tex, schedule); capture a ChordChange at each chord-change boundary via RealizedVoicingDiagram. Add emission tests.
    files_touched: [src/ChordFlow.Core/Rendering/ChordChange.cs, src/ChordFlow.Core/Rendering/IScoreRenderer.cs, src/ChordFlow.Core/Rendering/AlphaTexRenderer.cs, tests/ChordFlow.Core.Tests/AlphaTexRendererTests.cs]
    blocked_by: []
    satisfies: [IN1, IN7, C1, C2]
  - id: loadscore-envelope-carries-the-schedule
    order: 3
    status: done
    description: Add the schedule field to LoadScoreEnvelope (camelCase serialization incl. the FretboardDiagram) and thread it through From + all callers.
    files_touched: [src/ChordFlow.Core/Features/GenerateExercise/GenerateExercise.cs, src/ChordFlow.Core/Features/ExerciseLibrary/ExerciseLibrary.cs, src/ChordFlow.Desktop/Program.cs]
    blocked_by: []
    satisfies: [IN3]
  - id: chordflownownext-sibling-module
    order: 4
    status: done
    description: "Create the sibling shared module now-next-fretboards.js (window.ChordFlowNowNext): two side-by-side FretR + positional beat→schedule lookup + now/next captions."
    files_touched: [src/ChordFlow.Desktop/wwwroot/now-next-fretboards.js, src/ChordFlow.Desktop/wwwroot/index.html]
    blocked_by: []
    satisfies: [IN4, IN5, C3]
  - id: mount-on-practice
    order: 5
    status: done
    description: "Wire ChordFlowNowNext into Practice (app.js): forward loadScore.schedule → setSchedule and the score component's onBeat → onBeat; reset on loadScore and stop."
    files_touched: [src/ChordFlow.Desktop/wwwroot/app.js]
    blocked_by: []
    satisfies: [IN6, IN8]
  - id: verify-sync-sync-the-architecture-ref
    order: 6
    status: done
    description: Verify beat-ordinal alignment on the running app, confirm now/next sync + voicing fidelity, and update the architecture reference.
    files_touched: [loom/refs/chordflow-architecture-reference.md]
    blocked_by: []
    satisfies: [C4, C6]
---
# Now/Next Fretboards — Implementation

## Goal

Pin two fretboards (current + next chord) above the score, synced to playback. The C# engine stays the single source of truth: the render pass emits a chord schedule (one ChordChange per chord change, carrying a real-root FretboardDiagram of the comped voicing) alongside the alphaTex; a sibling shared JS module (ChordFlowNowNext) builds a positional beat→schedule lookup and drives the two side-by-side FretR off the score component's existing activeBeatsChanged signal. Built bottom-up: the real-root diagram producer first, then the schedule-emitting render seam, then the bridge envelope field, then the JS module, then the Practice wiring, closing with the beat-ordinal verification + architecture-ref sync.

---

## Steps

| Done | # | Step | Files touched | Blocked by | Satisfies |
|---|---|---|---|---|---|
| ✅ | 1 | Add RealizedVoicingDiagram.Build(chord, voicing, key) producing a real-root FretboardDiagram of a concrete Voicing, with xUnit tests. | src/ChordFlow.Core/Instruments/Guitar/Diagrams/RealizedVoicingDiagram.cs, tests/ChordFlow.Core.Tests/RealizedVoicingDiagramTests.cs | — | IN2, C2, C5, C7 |
| ✅ | 2 | Introduce ChordChange + make the render seam return (tex, schedule); capture a ChordChange at each chord-change boundary via RealizedVoicingDiagram. Add emission tests. | src/ChordFlow.Core/Rendering/ChordChange.cs, src/ChordFlow.Core/Rendering/IScoreRenderer.cs, src/ChordFlow.Core/Rendering/AlphaTexRenderer.cs, tests/ChordFlow.Core.Tests/AlphaTexRendererTests.cs | — | IN1, IN7, C1, C2 |
| ✅ | 3 | Add the schedule field to LoadScoreEnvelope (camelCase serialization incl. the FretboardDiagram) and thread it through From + all callers. | src/ChordFlow.Core/Features/GenerateExercise/GenerateExercise.cs, src/ChordFlow.Core/Features/ExerciseLibrary/ExerciseLibrary.cs, src/ChordFlow.Desktop/Program.cs | — | IN3 |
| ✅ | 4 | Create the sibling shared module now-next-fretboards.js (window.ChordFlowNowNext): two side-by-side FretR + positional beat→schedule lookup + now/next captions. | src/ChordFlow.Desktop/wwwroot/now-next-fretboards.js, src/ChordFlow.Desktop/wwwroot/index.html | — | IN4, IN5, C3 |
| ✅ | 5 | Wire ChordFlowNowNext into Practice (app.js): forward loadScore.schedule → setSchedule and the score component's onBeat → onBeat; reset on loadScore and stop. | src/ChordFlow.Desktop/wwwroot/app.js | — | IN6, IN8 |
| ✅ | 6 | Verify beat-ordinal alignment on the running app, confirm now/next sync + voicing fidelity, and update the architecture reference. | loom/refs/chordflow-architecture-reference.md | — | C4, C6 |
---

### Legend

| Symbol | Meaning |
|--------|---------|
| ✅ | Done |
| 🔄 | In Progress |
| 🔳 | Pending |
| ❌ | Cancelled |

<!-- step:real-root-voicing-diagram-producer -->
### Step 1 — Real-root voicing diagram producer

Mirror VoicingDiagram.Build's per-string marker logic (pitch class → interval vs chord root → chord-tone function → label/spelled note → Circle marker; muted strings as chrome; FretMin from the voicing), but anchor at chord.Root instead of canonical-C and take a concrete Voicing (not a VoicingShape). Title = ChordSymbol.Format(chord, key). Lives next to its siblings under Instruments/Guitar/Diagrams/ (Rendering→Instruments edge stays allowed, Music stays instrument-agnostic — C5). Tests: real-root markers/functions for a couple of known chords (e.g. G7, C7) assert correct interval/function/fret mapping at the real root.

<!-- step:render-pass-emits-the-chord-schedule -->
### Step 2 — Render pass emits the chord schedule

Define record ChordChange(int Bar, int Beat, string Name, FretboardDiagram Diagram) in Rendering. Change IScoreRenderer.Render / AlphaTexRenderer.Render to return a result carrying (tex, IReadOnlyList<ChordChange>). Capture an entry at the existing CurrentChordName change boundary in RenderBar — reuse the already-realized concrete Voicing and the covering Chord; Bar = master-bar index, Beat = the rendered beat's ordinal within the bar (the slot index). Build each entry's diagram via RealizedVoicingDiagram (step 1). Schedule comes from the comping track only (track 0 invariant — IN7); lead never contributes. Tests: a known multi-chord exercise (incl. a bar with an interior chord change) → expected (Bar, Beat, Name) entries and the right number of changes.

<!-- step:loadscore-envelope-carries-the-schedule -->
### Step 3 — loadScore envelope carries the schedule

Extend LoadScoreEnvelope with a schedule property (ChordChange[]) serialized camelCase to match the diagram model the FretR already consumes (content-crud's msg.diagram shape). Update LoadScoreEnvelope.From to take the render result's schedule; update the GenerateExercise + ExerciseLibrary call sites and Program.cs render handler. No behavior change to tex/tempo.

<!-- step:chordflownownext-sibling-module -->
### Step 4 — ChordFlowNowNext sibling module

create(container) → { setSchedule(schedule), onBeat(bar, beat), reset(), dispose() }. setSchedule builds chordIndexByKey: Map("bar:beat" → index) and stores the array. onBeat looks up the key; if undefined/unchanged, hold; on a change set currentIndex and render now = schedule[i].diagram, next = schedule[i+1]?.diagram ?? null (next shows a blank/— state past the last). Two ChordFlowFretboard instances side-by-side, controls:{ orientation:false, fretWindow:false } (fixed vertical chord-box); chord name via diagram title; static "Now"/"Next" captions are wrapper divs (FretR stays a dumb view — C3). reset() clears to first/blank. Register the script + add the host container above the score in index.html.

<!-- step:mount-on-practice -->
### Step 5 — Mount on Practice

Instantiate ChordFlowNowNext once against its container. On the loadScore handler (app.js:282 region), call reset() then setSchedule(msg.schedule). Forward the existing score-component onBeat(bar, beatInBar) callback through to the module's onBeat. Reset on stop. No new alphaTab event subscription — reuse the activeBeatsChanged signal already emitted by score-render-component (IN6).

<!-- step:verify-sync-sync-the-architecture-ref -->
### Step 6 — Verify sync + sync the architecture ref

Run the app and confirm the renderer's (Bar, Beat) ordinals match alphaTab's post-parse beat.voice.bar.index / beat.index (rests/tuplets are the risk — C4); if they diverge, align the schedule's Beat to alphaTab's beat.index. Confirm now/next track the cursor through a multi-chord, multi-bar exercise (incl. an interior chord change), next blanks at the end, and the now FretR shows the same shape the tab comps. Update chordflow-architecture-reference.md §5 (Bridge contract: loadScore gains schedule) and §5's fretboard-component paragraph (new ChordFlowNowNext consumer) — same unit of work (C6).
