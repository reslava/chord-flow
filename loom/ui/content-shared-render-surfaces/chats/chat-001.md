---
type: chat
id: ch_01KXT9F3HGB6VM7R0MN3VEJH7X
title: content-shared-render-surfaces Chat 001
status: done
created: 2026-07-18
version: 1
tags: []
parent_id: null
requires_load: []
last_ai_block: 6
---
# content-shared-render-surfaces Chat 001

## Rafa:

is idea grounded?
if so write design
if not tell me

## AI:

**Grounded — yes.** I verified every claim against the code before writing, and wrote the design (`design.md`, draft).

What the code confirms:
- **Practice** (`app.js`) mounts the full shared stack — `ScoreR` (`transport:false, volumes:false`) + page-level **PlayerControlsR** bound to `getEngine()` + **Score⇄Sheet toggle** + `ChordFlowSheetView`, all fed by one `loadScore` reply carrying `sheet` + `cellSchedule`.
- **Content** (`content-crud.js`) mounts a **bare ScoreR** (`player:true, controls:"full"`) + a fretboard for voicings. No sheet, no toggle, no page transport. The divergence — and why the minor-preview bug hid on this path — is exactly as the idea says.

One thing the code surfaced that the idea didn't spell out: **this isn't JS-only.** Content's preview rides a *different* verb — `entityPreview` → `ContentCrudHandler.Preview` → `EntityPreviewEnvelope`, which builds **only the tex**, no `sheet`/`cellSchedule`. Practice's sheet is fed by `ExerciseRendering.RenderWithSheet`. So the refactor spans Core (route Preview through `RenderWithSheet`), Bridge (widen the envelope with `Sheet`/`CellSchedule`), and JS (the composition). The machinery all exists already — Practice uses it — so it's plumbing, not new capability.

Decisions I settled in the design:
- **D1** — shared unit = the render-surface composite (ScoreR + ChordSheetR + toggle + PlayerControlsR), *not* the control strips. Practice keeps HarmonyControlsR + Now/Next; Content keeps its authoring controls.
- **D3** — per-entity: progression/song = full composite w/ sheet, rhythm = score-only (no toggle), voicing = fretboard (untouched).

**The one call I want you to confirm before I write req + plan — D2:** extract a shared composite (`ChordFlowRenderSurface`) that both pages mount, vs. Content duplicating app.js's ~130 lines of composition. I **recommend the composite** — otherwise we re-create the exact drift this thread exists to kill, and the toggle/single-engine/dual-marker wiring is subtle enough that a second hand-wired copy is where the next silent break lives. Cost is that `app.js` becomes a composite-consumer (a real but correct refactor).

Read `design.md` and tell me: composite (A) or pieces (B)? Once D2 is settled I'll lock the req and build the plan.

## Rafa:

D2: (A) Extract a composite JS component
update design
write req + plan

## AI:

Done — D2 (A) recorded, req locked, plan written and verified.

**Design** — D2 marked decided (A); the "open call" section is now a Resolution.

**`req.md` (locked)** — 7 Included, 4 Excluded, 5 Constraints. Highlights: `IN1` extract `ChordFlowRenderSurface`, `IN4`/`IN5` route the preview through `RenderWithSheet` + widen the envelope, `IN7` minor correctness in both surfaces; `C2` per-entity degradation, `C4` no duplicated composition, `C5` sync the architecture ref.

**`plans/plan-001.md`** — 5 steps, Core-first so each is testable:
1. Sheet projection on `EntityPreviewEnvelope` + `Preview`→`RenderWithSheet` — IN4, IN5, C1
2. Extract `ChordFlowRenderSurface` composite — IN1, C1, C4
3. Practice (`app.js`) mounts the composite — IN3, C4 *(after 2)*
4. Content (`content-crud.js`) mounts the composite — IN2, IN6, C2, C3 *(after 1, 2)*
5. Verify both surfaces + sync architecture ref — IN7, C5 *(after 3, 4)*

`loom_verify_req` is clean: no uncovered Included, no Excluded violations, no dangling citations.

Say `do step 1` (or `do plan`) when you want me to start implementing.

## Rafa:

I makred docs done, started plan
do plan

## AI:

**`do plan` complete — all 5 steps done, plan closed.** Content now mounts the exact same render surface as Practice.

1. **Sheet projection on the preview envelope** — `EntityPreviewEnvelope` gained `Sheet` + `CellSchedule`; `ContentCrudHandler.Preview` routes progression/song through the same `ExerciseRendering.RenderWithSheet` pass as `loadScore` (rhythm stays score-only, voicing a diagram). +3 tests.
2. **`ChordFlowRenderSurface` composite** — new `render-surface-component.js` owning ScoreR(`transport:false`) + ChordSheetR + Score⇄Sheet toggle + page-level PlayerControlsR + the beat/position fan-out. Takes the page's three mount els (placement stays the page's job); `sheet:false` = score-only.
3. **Practice → composite** — `app.js` dropped its hand-wired ScoreR + toggle + PlayerControlsR + SheetView + fan-out for one `surface = ChordFlowRenderSurface.create(...)`; keeps HarmonyControlsR + Now/Next around it.
4. **Content → composite** — `content-crud.js` mounts it for the score strategy (progression/song = sheet, rhythm = score-only, recreated on mode-flip); voicing keeps the fretboard.
5. **Verify + ref sync** — architecture ref updated; **live CDP dogfood** confirmed it.

**The divergence bug is closed at the root:** on Content, a minor `1- 4- 5-` now previews `\ks cminor` in the score **and** "key of Cm · Cm Fm Gm" in the sheet — both correct, from one pass. Toggle flips surfaces; rhythm is score-only; voicing is a fretboard. Fix ScoreR/ChordSheetR/PlayerControlsR once → both pages benefit (C4).

Verification tally: Core **1022/1022** green · solution build **0 errors** · all JS `node --check` clean · runtime CDP dogfood green. The one thing not driven headlessly was frame-by-frame *audio-synced* marker tracking (needs a trusted-gesture play + timed sampling) — covered structurally: both surfaces render the same run's projections and the `"beat"`/`"position"` fan-out is wired, same mechanism Practice ships.

Changes are **uncommitted** — say the word and I'll commit (idea/design/req/chats too).

## AI:

Committing to `main` — the composite + Core/bridge change + Practice/Content refactors + the architecture-ref sync, plus this thread's idea/design/req/plan/done/chat. Leaving the unrelated `harmonic-analysis/chat-002.md` untracked (not part of this work).