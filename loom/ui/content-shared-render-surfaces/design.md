---
type: design
id: de_01KXT9SEKXG87T69JPW7AQ018T
title: Content page mounts the shared render surfaces (ScoreR + ChordSheetR + toggle) like Practice
status: done
created: 2026-07-18
updated: 2026-07-18
version: 4
idea_version: 1
tags: []
parent_id: id_01KXT94RKDERB14BNMAE6C3FY0
requires_load: []
---
# Content page mounts the shared render surfaces (ScoreR + ChordSheetR + toggle) like Practice

## Grounding — verified against the code

The idea is **grounded**. The divergence it describes is real, and I confirmed every claim against source:

- **Practice** (`wwwroot/app.js`) mounts the full shared stack: `ScoreR` created with `transport:false, volumes:false`; a **page-level PlayerControlsR** bound to `view.getEngine()`; a **Score⇄Sheet toggle** (`buildViewToggle`, collapse-swap so it survives mid-playback); `ChordFlowSheetView` (the Sheet surface); and one `loadScore` reply that feeds **both** projections (`view.load(tex)` + `sheetView.render(sheet)` + `sheetView.setSchedule(cellSchedule)`), with the engine's `beat`/`position` signals fanned to both markers.
- **Content** (`wwwroot/content-crud.js`) mounts a **bare ScoreR** in full in-strip player mode (`player:true, controls:"full"`) for progression/song/rhythm, and a `ChordFlowFretboard` for voicing. **No Sheet view, no toggle, no page-level transport.**

So you genuinely cannot audition a progression/song as a chord sheet while authoring it, and the two surfaces drift — exactly as the idea states, and consistent with why the minor-preview bug ([[minor-mode-ui-threading]]) hid on the Content path.

### The non-obvious constraint the code revealed

This is **not** a JS-only wiring job. Practice's Sheet view is fed by the `sheet` + `cellSchedule` projections that ride the **`loadScore`** reply, built by `ExerciseRendering.RenderWithSheet` → `ChordSheetBuilder`. But Content's preview travels a **different bridge verb**: `entityPreview` → `ContentCrudHandler.Preview()` → `EntityPreviewEnvelope(entity, "score", tex, tempo)` — which calls `RenderToTex` / `SongPreview` and builds **only the alphaTex string**, no sheet, no cellSchedule.

The good news: the machinery already exists (`RenderWithSheet`, `ChordSheetBuilder`, `ChordFlowSheetView`). The work is to route the Content preview through it and widen the envelope. So the refactor spans **three layers**: Core (preview path), Bridge (envelope), JS (composition).

---

## Decisions settled

### D1 — What is shared vs page-specific (idea decision 1)

**Shared render surface** = `ScoreR` (notation) + `ChordSheetR` (sheet) + the **Score⇄Sheet toggle** + a **PlayerControlsR** transport bound to the one engine, plus the beat/position fan-out that keeps both markers tracking across the toggle.

**Page-specific, NOT shared:**
- Practice keeps **HarmonyControlsR** (performance/definition strip: key/feel/difficulty/lead/voicing-window/Generate/Save) and **Now/Next** boards.
- Content keeps its **authoring controls** (name, DSL textarea, tonality, comping picker, source filter/list).

The shared unit is the **render-surface composite**, never the full control strip — the authoring strip and the performance strip stay divergent by design (they are genuinely different jobs).

### D2 — The reusable unit: extract a composite (idea decision 2) — **DECIDED: (A) extract the composite**

Two options:

- **(A) Extract a composite JS component** — `window.ChordFlowRenderSurface` — that owns `{ ScoreR(transport:false) + ChordSheetR + Score⇄Sheet toggle + PlayerControlsR + the one engine + beat/position fan-out }`. Both `app.js` and `content-crud.js` mount it and wrap it with their page-specific controls.
- **(B) Content mounts the same pieces itself**, duplicating app.js's ~130 lines of composition (toggle build, PlayerControlsR bind, SheetView mount, the beat→sheet/position→sheet fan-out, the dual-surface load handler).

**Recommendation: (A), extract the composite.** Rationale:
- The whole point of the thread is to make the render surface **one improvement path**. Option (B) re-creates the exact divergence we're removing — a second hand-wired copy of the mid-playback toggle + single-engine + dual-marker fan-out that can silently break on one page (the class of bug that motivated [[player-controls-component]] and `metronome-countin-fix`).
- The composition logic is subtle (collapse-swap so alphaTab keeps its width; `position` vs `beat` routing; feeding both projections from one reply). Subtle + duplicated = drift. Owning it once is the durable choice.
- Cost is a bigger refactor of `app.js` (it becomes a composite-consumer), but that is the correct altitude — Practice stops hand-wiring what every page needs.

Proposed composite shape:
```
ChordFlowRenderSurface.create(mountEl, opts) → handle
  opts: { scoreOpts, sheet: true|false, nowNext?, onBeat?, onFinished?, onNeedsRerender? }
  handle.load({ tex, tempo, sheet, cellSchedule, key?, keyIsMinor?, tripletFeel? })  // feeds BOTH projections
  handle.getEngine()          // the one ChordFlowPlayback, for volume binds etc.
  handle.getRenderParams()    // key/tempo/feel for the preview round-trip (Content)
  handle.dispose()
```
It builds ScoreR (`transport:false`), the toggle, PlayerControlsR (bound to `getEngine()`), and SheetView internally; wires `engine.on("position")`/`onBeat` to the sheet marker. Now/Next stays a Practice-only add-on the shell wires from the same engine (out of the composite — it is not part of "the render surface" and Content has no now/next).

**Decision (Rafa): (A).** The composite is the unit both pages mount.

### D3 — Per-entity degradation (idea decision 3)

The composite must degrade cleanly per entity:
- **progression / song** → full composite: Score⇄Sheet toggle present, both surfaces live.
- **rhythm** → **score-only** (`sheet:false`): the toggle is hidden; a bare rhythm on a single I chord has no meaningful chord sheet.
- **voicing** → **not this composite at all**: stays on the existing `ChordFlowFretboard` fret-box path (the diagram strategy in content-crud.js is untouched).

The composite's `sheet` opt is the switch; the Content editor already selects its preview strategy per entity, so it picks composite-with-sheet / composite-score-only / fretboard.

---

## The Core + Bridge change (what makes Content's Sheet real)

1. **`ContentCrudHandler.Preview`** — for **progression + song**, route through `ExerciseRendering.RenderWithSheet` (the same pass Practice uses) instead of `RenderToTex` / the bespoke `SongPreview` render. This yields `{ Render (tex+schedule), Sheet, CellSchedule }` from one realized-song + CompingPlan pass — so the Content sheet and score cannot drift, and the minor-preview correctness the divergence hid is covered by construction. Rhythm keeps its score-only path; voicing keeps its diagram path.
2. **`EntityPreviewEnvelope`** — widen to carry the sheet projection for `Kind == "score"`:
   ```
   EntityPreviewEnvelope(Entity, Kind, Tex?, Tempo?, Diagram?, Sheet?, CellSchedule?, Type)
   ```
   `Sheet`/`CellSchedule` are populated for progression/song, null for rhythm (score-only) and voicing (diagram).
3. **`content-crud.js`** — mount `ChordFlowRenderSurface` in place of the bare ScoreR for the score strategy; on `entityPreview`, call `handle.load({ tex, tempo, sheet, cellSchedule, ... })` so both surfaces render. The comping picker, tonality control, and seed plumbing (`pendingSeeds` → `handle`) stay; they now feed the composite's ScoreR.

Content has **no page-level HarmonyControlsR**, so ScoreR inside the composite keeps its opt-in **key/feel** pickers (`scoreOpts: { key:true, tripletFeel:true }`) — that is where Content's live transpose/feel lives (Practice passes those `false` because HarmonyControlsR owns them). Tempo stays a PlayerControlsR param on both pages.

---

## Scope

**In:**
- Extract `ChordFlowRenderSurface` (composite: ScoreR + ChordSheetR + toggle + PlayerControlsR + fan-out).
- Refactor `app.js` to mount the composite (Practice keeps HarmonyControlsR + Now/Next + library around it).
- Route Content's progression/song preview through `RenderWithSheet`; widen `EntityPreviewEnvelope`.
- Mount the composite in `content-crud.js` for the score strategy (progression/song = with sheet, rhythm = score-only); voicing untouched.

**Out (non-goals, per the idea):**
- Not merging the pages; not moving authoring controls into Practice or performance controls into Content.
- No new render capability — consolidation only.
- Now/Next is not added to Content (it is a Practice performance affordance, not part of the render surface).

## Validation / dogfood

- In Content, select/author a progression or song → toggle **Score⇄Sheet** in the preview **mid-playback**, exactly like Practice; both markers track.
- A **minor** progression renders correct chords + `\ks` in **both** the score and the sheet preview (the regression the divergence hid).
- **Rhythm** still previews score-only (no toggle); **voicing** still previews as a fretboard.
- Fix ScoreR/ChordSheetR/PlayerControlsR once → both pages benefit (the "one improvement path" test).

## Resolution

- **D2 settled: (A) extract the composite** (`ChordFlowRenderSurface`). Both pages mount it; `app.js` becomes a composite-consumer.
- D1, D3, and the Core/Bridge/JS change follow from it. Requirements locked in `req.md`; plan in `plans/plan-001.md`.
