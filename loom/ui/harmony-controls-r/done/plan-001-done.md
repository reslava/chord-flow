---
type: done
id: pl_01KXN9S3C4E1KYX8J3YAH4CXGN-done
title: Done — HarmonyControlsR + one Practice page — implementation
status: done
created: 2026-07-16
version: 7
tags: []
parent_id: pl_01KXN9S3C4E1KYX8J3YAH4CXGN
requires_load: []
---
# Done — HarmonyControlsR + one Practice page — implementation

## Step 1 — Extend the generate/loadExercise reply to carry the chord-sheet projection over the SAME Exercise: sheet model (always including resolved comping-grip tone + diagram data), cellSchedule, and chordSchedule alongside tex/key/tempo/tripletFeel. ChordSheetBuilder is invoked from the generate/load pipeline as a projection (no separate resolve). Unit tests for the combined reply. Additive — the old chordSheet path still works until step 6.

**Unified reply — the sheet projection rides the score reply (IN3/IN10).**

- `ExerciseRendering.cs` — restructured around one private `RenderCore` (base key → expansion → `CompingPlan` → render) so every projection derives from the same pass. New `ExerciseProjections` record (`Render`, `Sheet`, `CellSchedule`) + public `RenderWithSheet(...)`: builds the `ChordSheet` via `ChordSheetBuilder.Build` with the comping plan passed **unconditionally** (IN10 — the model always carries tone + diagram data) and overlays the cellSchedule from the same `render.Schedule`. `Render`/`RenderToTex` unchanged for existing callers (Content preview never pays for a sheet build). `SheetBarsPerRow = 4` is the fixed request-side constant.
- `ChordSheetBuilder.cs` — the handler's private `BuildCellSchedule` moved here as public `OverlaySchedule(barSchedule, renderSchedule)` (pure walk, arg-checked), shared by the unified reply and the legacy handler.
- `ChordSheetHandler.cs` — now delegates to `ChordSheetBuilder.OverlaySchedule` (dedupe; otherwise untouched — the `chordSheet` verb keeps working until step 6).
- `GenerateExercise.cs` — `LoadScoreEnvelope` grew `Sheet` + `CellSchedule`; `From(...)` routes through `RenderWithSheet`. Wire shape now `{type:"loadScore", tex, tempo, key, tripletFeel, schedule, sheet, cellSchedule}`. `ExerciseLibrary.Load` inherits the projection for free (it builds through the same `From`).
- Tests: new `ExerciseProjectionsTests.cs` — both projections on one reply (12 downbeat cell entries for the blues), sheet always carries tones + diagrams with plain default options, and a key override (Bb) reaching the seed key + sheet header + comped schedule alike.
- Build green; **925/925 tests pass** (3 new).

## Step 2 — New harmony-controls-component.js: builds its own DOM (PlayerControlsR precedent) — harmony picker (optgroups, one population path via setCatalog(entityList payloads)), Key, Feel, Comping + Rhythm vol, Lead + Lead vol, Difficulty, voicing-fret min–max, Generate/Save/Mark practiced. Owns the definition state (getDefinition()); seeds key/feel on harmony switch (song values, else C/Straight — never blank) with manual edits surviving until the next switch; seedKey/seedTripletFeel for the load path; onHarmonySwitch hook for the shell to seed tempo into PlayerControlsR; volume sliders bound to the page engine (setTrackVolume). Plus index.html script tag + .cf-harmony-controls CSS.

**HarmonyControlsR component (IN1/IN4/IN5/IN8, C1/C3).**

- New `src/ChordFlow.Desktop/wwwroot/harmony-controls-component.js` (`window.ChordFlowHarmonyControls`), PlayerControlsR precedent throughout: builds its own DOM, `guard()`-wrapped handlers, `create(container, opts) → { el, getDefinition, setCatalog, seedKey, seedTripletFeel, dispose }`.
- Controls in order: Harmony (optgroups Songs/Progressions, boot default `progression:12bar_blues`, selection preserved on rebuild) · Key (concrete 0–11, never blank) · Feel (None/Triplet8th/Triplet16th) · Comping + Rhythm vol · Lead ("(none)" option) + Lead vol · Difficulty · Voicing frets min–max (clamped 0–15, auto-swap when inverted) · Generate / Save / Mark practiced.
- **Seeding (IN4/IN5)**: harmony switch seeds Key from the song's `initialKey` (else C) and Feel from `defaultFeel` (else Straight) — silent control-value sets, Generate applies them; manual edits survive until the next switch. `seedKey`/`seedTripletFeel` are the loadExercise-path seeds (override wins, C3).
- **Tempo never owned (C1)**: `onHarmonySwitch(item)` hands the full catalog item to the shell, which seeds PlayerControlsR from `item.defaultTempo`.
- **Live re-render params**: Key/Feel/voicing-window changes fire `onDefinitionChange(def, "key"|"tripletFeel"|"voicing")`; comping/lead/difficulty apply only on Generate (today's behavior preserved).
- **Volume sliders**: bound to `opts.engine.setTrackVolume("rhythm"|"lead", v)`; hidden when no engine supplied.
- `setCatalog(entity, items)` takes raw `entityList` payloads — the single population path (IN8).
- `index.html`: `.cf-harmony-controls` CSS block + script tag (after player-controls, before score-render). `node --check` clean.

## Step 3 — Add a volumes opt to ScoreR (default true) so the Practice page can mount it without the Rhythm/Lead sliders; key/tripletFeel opts already exist and simply stay off for Practice. Content-CRUD preview keeps its current opts untouched.

**ScoreR `volumes` opt (C2).**

- `score-render-component.js`: new `volumes` create-opt (default **true** — existing consumers unchanged). When false, `buildControls` skips the Rhythm/Lead `volumeSlider` pair in the full profile; the scroll-mode select stays. Opt documented in the create-opts header comment; `keyEnabled` comment updated (Key select is now the Content-preview affordance — Practice gets key/feel from HarmonyControlsR).
- Content-CRUD preview untouched (it passes no `volumes`, so it keeps its sliders + key/feel).
- `node --check` clean.

## Step 4 — Refactor chord-sheets.js from a standalone page shell into a Sheet VIEW module: create(container) mounting ChordSheetR + the sheet-specific strip (Layout, Chords, + line, Below cell, Tone labels, Theme, Marker mode) + the three exports and the #chord-sheet-print PDF flow; render(sheet)/setSchedules(cellSchedule)/onBeat(bar,beat) driven by the shell. Drops: its own engine, PlayerControlsR, Now/Next, Sheet/Key combos, entityList merging, Show tab. Below cell becomes a pure display toggle (the model always carries tone/diagram data).

**Sheet view module (IN9/IN10, C5) — `chord-sheets.js` rewritten.**

- Global renamed `ChordFlowChordSheets` → **`ChordFlowSheetView`**, now `create(container) → { render(sheet, name), setSchedule(cellSchedule), onBeat(bar, beat), clearMarker, dispose }` — a view, not a page.
- **Keeps** (sheet-specific, IN9): the display strip — Layout, Chords, + line, Below cell, Tone labels, Theme, Marker mode (moved in from the old transport strip) — with exports right-aligned; the ChordSheetR mount (created once, reused so the marker survives toggles); the marker logic (metronome/per-chord modes, 1-based→0-based step-down, redundant-highlight skip); the three exports + `#chord-sheet-print` PDF flow with the `chordSheetPdfDone` teardown as its only remaining bridge subscription (EX3).
- **Drops**: its own ChordFlowPlayback engine + hidden staff + "Show tab", PlayerControlsR mount, Now/Next mount, the Sheet/Key combos, entityList merging, `requestSheet`/`chordSheetResult` handling — all now shell/unified-reply concerns.
- **Below cell is pure display (IN10)**: flips `view.setAdornments` only — no re-request (the unified reply's model always carries tones + diagrams).
- `onBeat` runs even while hidden so a mid-playback toggle reveals the marker already in place (IN7). Export filename base comes from the shell via `render(sheet, name)`.
- `node --check` clean. (The shell rewire that consumes this API is step 5.)

## Step 5 — Rework app.js + index.html into the single-page shape: remove the static #builder HTML and the Chord Sheets nav/view; mount HarmonyControlsR (fed entityList payloads, wired to sendDefinition/save/markPracticed and the tempo-seed hook) + the Score ⇄ Sheet segmented toggle at the head of the transport strip; ScoreR mounted with key/feel/volumes off; one reply handler feeds ScoreR.load(tex), sheetView.render(sheet), schedules, and the seeds (key/feel → HarmonyControlsR, tempo → PlayerControlsR, override wins on load); beat fan-out drives Now/Next + sheet marker in both views; both surfaces stay mounted so the toggle works mid-playback (visibility/collapsed-overflow hiding, re-render() on reveal if needed).

**Practice shell rewire — one page, Score ⇄ Sheet toggle (IN2/IN6/IN7, C1/C3/C4).**

- **ScoreR gained two shell seams** (beyond step 3's `volumes`): `transport: false` (skips the in-strip PlayerControlsR; all internal `pc` uses were already null-guarded) and `handle.getEngine()` — so the page can bind page-level controls to the ONE engine. Documented in the create-opts header.
- **`index.html`**: static `#builder` removed (HarmonyControlsR builds its own DOM); Practice view is now `#harmony-controls` + `#transport-strip` + `#now-next-pane` + `#view-pane` (`#score-pane` + `#sheet-pane`) + library. Chord Sheets nav button + `#chord-sheets-view` removed; `#chord-sheet-print` stays. New CSS: `.view-collapsed` (max-height:0 + overflow hidden — the proven "Show tab" collapse, keeps width so alphaTab never re-measures while hidden, C4) and `.view-toggle` segmented chrome.
- **`app.js`** rewritten as the one-page shell: ScoreR mounted with `transport:false, volumes:false` (no key/feel opts); page-level transport strip = Score/Sheet toggle + PlayerControlsR on `view.getEngine()`; HarmonyControlsR mounted with the same engine (volume sliders) and wired — `onGenerate` (tempo from `pc.getTempo()`, C1), `onSave`/`onMarkPracticed`, `onDefinitionChange` → `replayScoreRequest()` (the single re-render path: key transpose, \tf, voicing window; also serves ScoreR's `onNeedsRerender`), `onHarmonySwitch` → `view.seedTempo` + `pc.setTempoValue`.
- **One reply, both views**: `loadScore` handler feeds `view.load(tex)`, `pc.setTempoValue`, `hc.seedKey/seedTripletFeel` (override wins on load, C3), `nowNext.setSchedule(schedule)`, `sheetView.render(sheet, harmonyName)` + `setSchedule(cellSchedule)`. Beat fan-out drives Now/Next + the sheet marker in both views (markers track while hidden — IN7); `onFinished` resets both.
- The Score ⇄ Sheet toggle deliberately does NOT `stopAll()` (same page, same run); the top-level nav switch still does. Old picker/seed functions deleted (moved into HarmonyControlsR).
- `node --check` clean on all touched JS; zero stale `ChordFlowChordSheets`/`navChordSheets` references.

## Step 6 — Remove the chordSheet request routing and its request-side handler/envelopes from Core + Program.cs wiring (the builder stays as the projection invoked by step 1; exportChordSheet/chordSheetPdfDone print flow stays). Remove any dead JS references. Build + full test suite green.

**`chordSheet` request path retired (IN3).**

- Deleted `ChordSheetHandler.cs` (the request-side handler; `ChordSheetBuilder` stays as the projection invoked by `ExerciseRendering.RenderWithSheet`) and `ChordSheetHandlerTests.cs`.
- `ChordSheetEnvelopes.cs`: `ChordSheetResultEnvelope` + `ChordSheetErrorEnvelope` removed; only `ChordSheetPdfDoneEnvelope` (the print round-trip) remains, with a header comment pointing at the unified reply.
- `WebMessageRouter.cs`: `ChordSheetRequest` record, `ChordSheetRequested` event, `case "chordSheet"`, and the `BarsPerRow`/`Adornment` envelope fields removed. `exportChordSheet` stays.
- `Program.cs`: handler instantiation + the `ChordSheetRequested` subscription removed; the PDF-export block stays (with a pointer comment).
- Coverage ported: the retired handler tests' split-bar case now lives in `ExerciseProjectionsTests.Generate_SplitBar_GetsSubChordOnsetEntry` (seeded `17_47` progression → downbeat + mid-bar cellSchedule entries, C7/F7 in the chord schedule).
- Zero remaining references (`ChordSheetHandler|ChordSheetRequest|ChordSheetResultEnvelope|ChordSheetErrorEnvelope` → no hits). Build green; **917/917 tests pass** (925 − 9 retired + 1 ported).

## Step 7 — Run the app and walk the idea's Validation scenarios: mid-playback Score ⇄ Sheet toggle (audio continues, cursor + marker track the same beat, Now/Next in sync); comping/difficulty/voicing-window changes reflected in the sheet's playback and below-cell diagrams; key/feel seeding + survive-until-switch in both views; Save from Sheet view + library load restoring the definition. Update loom/refs/chordflow-architecture-reference.md (page structure + bridge envelope changes) in the same unit of work.

**End-to-end validation + architecture ref (C6).**

- **Manual validation walk (Rafa, in-app): all green** across the full checklist — mid-playback Score ⇄ Sheet toggle (audio continues, marker pre-positioned, alphaTab cursor/layout intact after the collapse-swap, Now/Next in sync), one definition driving both views (comping/voicing-window/Below-cell/difficulty/lead), key/feel seeding + survive-until-switch (both views, never blank), Save from Sheet view + library restore (both projections), nav button gone, exports + marker modes + volume sliders + metronome/count-in + Content preview all working. Verdict: "much clearer and more useful than 2 different pages."
- **Known non-blocking observation** (Rafa investigating, view-independent and pre-existing): with comping `charleston` (`X...--X.--------`) the cursor/marker appears to accelerate/decelerate within each bar regardless of feel or view — likely alphaTab's per-beat cursor easing across long notes/rests, not introduced by this thread (both surfaces consume the same `playedBeatChanged`).
- **`loom/refs/chordflow-architecture-reference.md` updated in the same unit of work** (7 surgical patches): §2 wwwroot map (harmony-controls-component.js added; app.js/chord-sheets.js re-described), §5 ScoreR (transport/volumes opts + getEngine), new HarmonyControlsR component paragraph, ChordSheetR paragraph (verbs retired → unified reply), §6 data flow (one reply, all projections), §7 PlayerControlsR mount + the one-page engine diagram + chord-sheet playback paragraph.
