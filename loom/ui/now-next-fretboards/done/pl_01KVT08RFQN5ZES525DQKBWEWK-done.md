---
type: done
id: pl_01KVT08RFQN5ZES525DQKBWEWK-done
title: Done — Now/Next Fretboards — Implementation
status: done
created: 2026-06-23
version: 6
tags: []
parent_id: pl_01KVT08RFQN5ZES525DQKBWEWK
requires_load: []
---
# Done — Now/Next Fretboards — Implementation

## Step 1 — Add RealizedVoicingDiagram.Build(chord, voicing, key) producing a real-root FretboardDiagram of a concrete Voicing, with xUnit tests.

**Real-root voicing diagram producer.**

- Added `RealizedVoicingDiagram.Build(Chord chord, Voicing voicing, Key key) → FretboardDiagram` (`Instruments/Guitar/Diagrams/RealizedVoicingDiagram.cs`): same per-string marker logic as the old `VoicingDiagram`, but intervals are computed as `(pc - chord.Root) mod 12` and note names spelled against the passed `key`, so a chord renders at its **real root** instead of canonical-C. Title = `ChordSymbol.Format(chord, key)`.
- **Refactor (beyond the listed file):** `VoicingDiagram.Build(shape)` now **delegates** to `RealizedVoicingDiagram.Build(new Chord(C, shape.Quality), shape.Canonical, CAnchor)` — the canonical-C producer is just the C-anchored special case of the general one. One marker-building path, no drift (C2). Output is byte-identical: the 4 existing `VoicingDiagramTests` still pass unchanged.
- Tests: `RealizedVoicingDiagramTests` — open G7 (full interval/function mapping incl. the `b7` label) and open D major (proves the root offset ≠ C: open D reads as `R`, not the `b3` it would be at a C anchor; muted low strings as chrome).
- `dotnet test` filtered to both diagram suites: **6/6 pass**.

Satisfies IN2, C2, C5, C7.

## Step 2 — Introduce ChordChange + make the render seam return (tex, schedule); capture a ChordChange at each chord-change boundary via RealizedVoicingDiagram. Add emission tests.

**Render pass emits the chord schedule.**

- New `Rendering/ChordChange.cs`: `record ChordChange(int Bar, int Beat, string Name, FretboardDiagram Diagram)` + `record RenderResult(string Tex, IReadOnlyList<ChordChange> Schedule)`.
- `IScoreRenderer.Render` now returns `RenderResult` (was `string`) — design D1, approved. `AlphaTexRenderer.Render` builds the schedule during the **existing** comping walk and returns `(tex, schedule)` at both the single-track and two-track return points.
- `AlphaTexRenderer` capture: `RenderState` gained `BarIndex` (advanced once per `RenderBar` — `\ks`/`\tf` lines aren't bars, so they don't count), `ScheduleChordName`, and `Schedule`. `RenderBar` now computes the covering chord for every slot, tracks a per-bar `beatOrdinal`, and `RecordChordChange` pushes one entry the first time a chord appears — built from the **same voicing the tab comps** (reused for sounding beats; only realized fresh if a change lands on a rest), so the FretR can't drift from the tab (C2). Lead track untouched (comping = track 0 only, IN7).
- Callers threaded: `ExerciseRendering` gained `Render(...) → RenderResult` (loadScore path); `RenderToTex` kept as `.Tex` for tex-only callers (Content preview `ContentCrudHandler`). `SwappableRenderer` return type updated. ~12 test call sites updated to `.Render(...).Tex`.
- Tests (`AlphaTexRendererTests`): multi-bar — 12-bar Bb blues → 7 entries `(0,0,Bb7)…(11,0,F7)`, first diagram = the `(1.5 0.4 1.3)` Bb7 shell the tab plays (fidelity + real-root); interior change — `17_67` on a quarters grid → `(0,0,Bb7),(0,2,G7)` (proves `Beat=2`).
- **Full Core suite: 645/645 pass** (no regression from the seam change).

Satisfies IN1, IN7, C1, C2.

## Step 3 — Add the schedule field to LoadScoreEnvelope (camelCase serialization incl. the FretboardDiagram) and thread it through From + all callers.

**loadScore envelope carries the schedule.**

- `LoadScoreEnvelope` gained `IReadOnlyList<ChordChange> Schedule`; serializes to `{"type":"loadScore","tex":…,"tempo":N,"schedule":[…]}`.
- `LoadScoreEnvelope.From` now calls `ExerciseRendering.Render(...)` (the `RenderResult` variant) and forwards `result.Schedule`. `From`'s signature is unchanged, so both call sites (`GenerateExercise.Generate`, `ExerciseLibrary`, `Program.cs` render handler) compile untouched.
- Serialization: outbound bridge uses `JsonSerializerDefaults.Web` (camelCase; enums as their int ordinal), so `schedule[i]` = `{bar, beat, name, diagram:{title, markers:[{string,fret,note,interval,function,shape}], mutedStrings, barreFret, fretMin, fretMax,…}}` — the exact `FretboardDiagram` shape the FretR already consumes (`MarkerShape` as the 0-3 ordinal). No converter needed.
- Full solution builds clean (0 errors).

Satisfies IN3.

## Step 4 — Create the sibling shared module now-next-fretboards.js (window.ChordFlowNowNext): two side-by-side FretR + positional beat→schedule lookup + now/next captions.

**ChordFlowNowNext sibling module.**

- New `wwwroot/now-next-fretboards.js` (`window.ChordFlowNowNext`): `create(container) → { setSchedule, onBeat, reset, dispose }`. Builds two captioned ("Now"/"Next") `ChordFlowFretboard` instances side-by-side (`orientation:"vertical"`, `controls:{ orientation:false, fretWindow:false }` — fixed chord-box per IN5). `setSchedule` builds a `"bar:beat" → index` map and primes the now box to the first chord; `onBeat(bar,beat)` (0-based, the schedule convention) looks up the change index, holds if unchanged, else renders now=`schedule[i]`, next=`schedule[i+1]` (a blank "—" diagram past the last). Dumb assembler — chord name rides the diagram `title`; no theory in JS (C3).
- **Design refinement:** `reset()` re-primes to the first chord but **keeps** the schedule (dropping it on stop would leave replay blank until another loadScore). The "Now/Next" captions are wrapper divs, not a component change.
- `index.html`: `#now-next-pane` placed above `<main>`/`#score-pane` in the Practice view; `<script src="now-next-fretboards.js">` loaded after `fretboard-render-component.js`, before `app.js`.
- Verified `ChordFlowFretboard.render` handles the empty-markers blank model (draws a 4-fret empty neck, no throw). `node --check` clean.

Satisfies IN4, IN5, C3.

## Step 5 — Wire ChordFlowNowNext into Practice (app.js): forward loadScore.schedule → setSchedule and the score component's onBeat → onBeat; reset on loadScore and stop.

**Mount on Practice (app.js).**

- Added `let nowNext = null;` and create it in `init()` (`window.ChordFlowNowNext.create($("now-next-pane"))`, feature-guarded).
- `loadScore` handler now calls `nowNext.setSchedule(msg.schedule)` alongside `view.load(...)`.
- The score component's existing `onBeat(bar, beat)` callback forwards to `nowNext.onBeat(bar - 1, beat - 1)` — the **off-by-one bridge**: the score component reports 1-based (bar, beat), the schedule is 0-based (alphaTab raw). No new alphaTab subscription (reuses `activeBeatsChanged`, IN6).
- `onFinished` (fires on stop and natural end) calls `nowNext.reset()` → back to the first chord, schedule kept for replay.
- `node --check` clean; full solution builds.

Satisfies IN6, IN8.

## Step 6 — Verify beat-ordinal alignment on the running app, confirm now/next sync + voicing fidelity, and update the architecture reference.

**Verify + architecture-ref sync.**

- **Architecture ref synced (C6):** `chordflow-architecture-reference.md` §5 (fretboard-component paragraph) now documents `RealizedVoicingDiagram.Build` (real-root producer; `VoicingDiagram` is its canonical-C special case) and the `ChordFlowNowNext` consumer; §6 data-flow notes `loadScore` carrying the chord `schedule`. (Domain-model ref doesn't enumerate diagram producers → no change there.)
- **Headless verification done:**
  - C# schedule emission unit-tested (step 2) — full Core suite 645/645.
  - JS: `node --check` clean on the module + app.js.
  - **Module state-machine harness** (Node, stubbed fretboard, known schedule + beat sequence): setSchedule primes to first (Bb7/Eb7); non-change beats hold; change beats advance now/next (beat(4,0)→Eb7/Bb7); next blanks "—" past the last (beat(6,0)→Bb7/—); reset returns to first keeping the schedule. All as specified.
- **Live check pending (C4):** whether alphaTab's runtime `beat.voice.bar.index` / `beat.index` line up with the schedule's 0-based (Bar, Beat) — rests/tuplets the only risk — needs the desktop GUI running with eyes on the fretboards (can't screenshot a WinForms window from the agent). For the default boot score (12-bar blues, beat_1_3, no pickup/tuplets) alignment is expected exact. **Dogfood hand-off:** run the app, play, confirm now/next track the cursor and the now box matches the comped shape. If a chord lands one beat early/late, the fix is localized to the schedule's `Beat` ordinal vs alphaTab's `beat.index`.

Satisfies C4 (headless + ref), C6.
