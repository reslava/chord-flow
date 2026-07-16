---
type: design
id: de_01KXN9DWFJG6QWET6VVFFS36HD
title: HarmonyControlsR + one Practice page — design
status: done
created: 2026-07-16
version: 1
idea_version: 1
tags: []
parent_id: id_01KXN9CD4EEAQ6RV5KSGFBF51X
requires_load: []
---
# HarmonyControlsR + one Practice page — design

## Decisions (from chat-001)

1. **Option A — full convergence**: one definition envelope on the bridge; the sheet is a projection of the same generated exercise, not a separate narrower request.
2. **One page**: Practice gains a Score ⇄ Sheet segmented view toggle; the standalone Chord Sheets page + nav button are removed. One instance of every shared piece; view toggle works mid-playback.
3. **Key + Feel live in HarmonyControlsR**, behaving exactly as Practice today: seeded on harmony switch, manual edit survives until the next switch, always concrete (song without key → C, feel default Straight), never blank.
4. **Volumes sit with their voice**: Rhythm vol next to Comping, Lead vol next to Lead — inside HarmonyControlsR, bound to the page engine.

## Target shape

```
Practice page (app.js — the only shell)
├── HarmonyControlsR   harmony · key · feel · comping+RhythmVol · lead+LeadVol ·
│                      difficulty · voicing frets min–max · Generate/Save/Practiced
├── transport strip    [Score ⇄ Sheet toggle] + PlayerControlsR (play/stop/tempo/
│                      soundfont/metronome/count-in/Now-Next toggle)
├── Now/Next boards    one ChordFlowNowNext, fed chordSchedule, view-independent
├── view surface       ┌ Score view: ScoreR (staff toggles + debug panel + alphaTab)
│   (swaps)            └ Sheet view: sheet strip (layout/chords/+line/below-cell/
│                        tone-labels/theme/marker-mode/exports) + ChordSheetR (SVG)
└── library pane       saved exercises — page-level, visible in both views
```

**One engine.** ScoreR keeps owning the page's `ChordFlowPlayback` (as today). The Sheet view has **no engine of its own** — its bar marker consumes the same `onBeat` signal the shell already fans out to Now/Next. `chord-sheets.js`'s engine + hidden staff + "Show tab" checkbox + own PlayerControlsR + own Now/Next all disappear.

## Components

### HarmonyControlsR (`harmony-controls-component.js`, new)

`window.ChordFlowHarmonyControls.create(container, opts)` — PlayerControlsR precedent: builds its own DOM, self-styled, page feeds it data.

- **State owned**: the full definition — `{ harmonyEntity, harmonyId, compingPatternId, leadPatternId, keyPitchClass, tripletFeel, difficulty, voicingMinFret, voicingMaxFret }`.
- **API**: `setCatalog(entity, items)` (fed raw `entityList` payloads — the single population path, so the combo is identical everywhere by construction; optgroups Songs/Progressions, boot default `progression:12bar_blues`); `getDefinition()`; `seedKey(pc)` / `seedTripletFeel(v)` (used by the `loadExercise` reply path, where the stored override wins).
- **Callbacks**: `onGenerate` / `onSave` / `onMarkPracticed`; `onDefinitionChange(def)` for the live re-render params (key/feel/voicing-window changes replay the last request — today's `onNeedsRerender` + voicing-input behavior); `onHarmonySwitch(item)` so the **shell** seeds tempo into PlayerControlsR (`pc.setTempoValue`) — tempo stays a PlayerControlsR param, HarmonyControlsR never owns it.
- **Engine binding**: `opts.engine` — the two volume sliders call `engine.setTrackVolume("rhythm"|"lead", v)` directly (player-kind, no round-trip), replacing ScoreR's `volumeSlider`s on this page.
- **Seeding** (moves in from `app.js` `seed{Key,Tempo,Feel}ForHarmony`): on harmony *switch* only — song → `initialKey`/`defaultTempo`/`defaultFeel`, else C / 80 / Straight; manual edits survive until the next switch; controls always show a concrete value.

### ScoreR (slims on Practice; unchanged elsewhere)

Practice creates it with `key: false, tripletFeel: false` and a new `volumes: false` opt. The opt-in key/feel/volume controls **stay in the component** — the Content-CRUD preview still uses them (`content-crud.js` creates ScoreR with `key: true, tripletFeel: true`; it previews single entities and is not a definition builder). Accepted trade-off: a small key/feel-control duplication between ScoreR and HarmonyControlsR, rather than contorting the Content preview onto a builder component.

### Sheet view (`chord-sheets.js` refactors into a view module)

Keeps: `ChordSheetR` mount + display strip (Layout, Chords, + line, Below cell, Tone labels, Theme — all pure-JS setters on the live view), Marker mode (metronome/chord), the three exports + the `#chord-sheet-print` PDF flow, `buildSchedule`/`onBeat` marker logic (now driven by the shell's beat fan-out).
Drops: engine, PlayerControlsR, Now/Next, harmony/Sheet + Key combos, entityList merging, "Show tab".
**Adornments become display-kind**: the unified reply always carries the cell tone/diagram data (grips are resolved by the definition anyway), so Below cell no longer re-requests — it flips `setAdornments` like every other display toggle.

### View toggle

A page-level segmented control (Score | Sheet) at the head of the transport strip. Toggling swaps the surface + view strip only — engine, definition, schedules, Now/Next, library untouched, so it works mid-playback. **Both surfaces stay mounted**; the inactive one hides via `visibility`/off-screen rather than `display:none` where needed — alphaTab must keep valid layout for its cursor (re-`render()` on reveal as fallback — implementation risk, see below).

## Bridge unification (C# seam)

- **Requests**: `generate` (from `getDefinition()` + `renderOptions` incl. the voicing window, as today) and `loadExercise` are the only render-producing envelopes. **`chordSheet` request + `chordSheetResult`/`chordSheetError` retire.** `exportChordSheet`/`chordSheetPdfDone` (print flow) stay.
- **Reply**: the score reply grows the sheet projection — `{ tex, key, tempo, tripletFeel, chordSchedule, sheet, cellSchedule }`. One handler path: definition → Exercise (existing generate pipeline) → projections: `AlphaTexRenderer` (tex) + the ChordSheet builder (`Features/ChordSheets/ChordSheetHandler.cs` logic becomes a projection over the same Exercise, not a separate resolve). Sheet model always includes tones + diagram data (see adornments above).
- `barsPerRow` stays a fixed request-side constant (4) as today; sheet layout remains a JS display concern.
- Schedules: `chordSchedule` (Now/Next, shared) + `cellSchedule` (sheet marker) both derive from the same Exercise — one timing source, no drift.

## Flows

- **Generate / library load / boot** → one reply → `ScoreR.load(tex)`, sheet view `render(sheet)` + `buildSchedule(cellSchedule)`, `nowNext.setSchedule(chordSchedule)`, seeds (key/feel → HarmonyControlsR, tempo → PlayerControlsR).
- **Beat** → shell fans out: score cursor (alphaTab internal), `nowNext.onBeat`, `sheetView.onBeat` (marker) — both views track even while hidden, so a mid-playback toggle is seamless.
- **Content-kind change** (key, feel, chordNames/diagrams toggles, voicing window) → replay last request with current definition — the existing `onNeedsRerender` path, now sourced from HarmonyControlsR.
- **Player-kind change** (tempo, vols, metronome, count-in, soundfont) → local, no round-trip (unchanged).

## Risks / notes

- **alphaTab hidden-surface layout**: alphaTab may mis-measure in a hidden container. Mitigation: hide via `visibility:hidden`/zero-height-overflow rather than `display:none`, or call `api.render()` on reveal. Verify during implementation.
- **Reply size**: the sheet model rides on every score reply. Local desktop bridge, small JSON — acceptable; if it ever matters, a `projections` request flag is an additive tweak.
- **`Program.cs` / `WebMessageRouter`**: `chordSheet` routing is removed; the ChordSheet builder is invoked from the generate/load path instead.
- Practice's static `#builder` HTML in `index.html` is removed (HarmonyControlsR builds its own DOM, like PlayerControlsR).