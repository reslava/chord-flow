---
type: chat
id: ch_01KXMSF2V5KG0MBTECCETWEHF3
title: chord-sheets-now-next Chat 001
status: done
created: 2026-07-16
version: 1
tags: []
parent_id: null
requires_load: []
last_ai_block: 11
---
# chord-sheets-now-next Chat 001

## Rafa:

review idea + design + req
write plan

## AI:

Reviewed idea + design + req against the live code, then wrote **plan-001** (3 steps). The review turned up one thing worth your eyes before implementing — the scope is meaningfully **smaller** than the docs imply, and it's the exact "TBD during planning" the design handed to this session.

## The headline: IN1's "shared producer" already exists

The design says *"extract the RealizedSong + CompingPlan → chord schedule by-product into one named Core producer — e.g. `ChordScheduleBuilder` … TBD during planning by where the by-product code sits."*

Where it sits: **`AlphaTexRenderer.Render(realized, …, comping, …)` already returns `RenderResult(Tex, Schedule)`**, where `Schedule` is the `ChordChange[]` (one entry per chord change, each carrying the comped `FretboardDiagram`). And **`ChordSheetHandler.Build` already calls that exact renderer** (`ChordSheetHandler.cs:76-80`) — the same one Practice uses. It even computes `render.Schedule` today and then **throws it away** at line 82 when building the envelope.

So:
- **IN1** (one shared producer, both paths call it) — already true. No `ChordScheduleBuilder`, no new named type, **no Practice-side change** (nothing to extract → nothing to break, so C3 parity is free).
- **IN2** (grips resolved unconditionally) — already true: `ChordSheetHandler.cs:59-66` resolves the `CompingPlan` always; the adornment toggle only gates whether the *sheet* draws diagrams, not the schedule.
- **IN6** (per-chord granularity) — free: `RecordChordChange` dedups by chord label, so even with the `Quarters` comp the schedule has one entry *per chord change*, not per beat.

The whole Core task collapses to: **stop discarding `render.Schedule` — surface it as an additive `chordSchedule` field on `chordSheetResult`** (reusing `ChordChange`, EX2/EX4).

## Two JS-API corrections vs. the design's assumptions

Checked `ChordFlowNowNext` and PlayerControlsR directly:
- **`ChordFlowNowNext` has no `setVisible`** — its API is `{ setSchedule, onBeat, reset, dispose }`. The design floated `nowNext.setVisible(visible)`; the real move is to hide the **page container** in `onToggleNowNext`, exactly like `app.js` flips its `now-next-pane`. (Design already allowed "or show/hide the container".)
- **`ChordFlowNowNext.onBeat` is 0-based**; this page's engine `onBeat` is **1-based**. Feed it `nowNext.onBeat(bar-1, beat-1)` from the *existing* `onBeat` callback — that callback **is** the page's per-beat signal, so no separate `engine.on("beat")` subscription is needed (still satisfies C4's "reuse the beat bus").

## The plan

1. **Surface the schedule** — add `chordSchedule` (`ChordChange[]`) to `ChordSheetResultEnvelope`, pass `render.Schedule` in the handler, extend `ChordSheetHandlerTests` (assert it's populated, carries grips even at adornment `none`, and is distinct from `cellSchedule`). *(IN1, IN2, IN3, C1, C3, EX2, EX4)*
2. **Mount + feed + sync + toggle** `ChordFlowNowNext` in `chord-sheets.js` — container above the sheet, `setSchedule(msg.chordSchedule)`, `nowNext.onBeat(bar-1, beat-1)` in the existing `onBeat`, `reset()` on finish, `onToggleNowNext` → hide the container. *(IN4, IN5, IN6, C2, C4, EX1, EX3)*
3. **Ref sync** — `chordflow-architecture-reference.md` §6 + now-next seam; **no** domain-model-ref change (no new named type). *(IN7)*

**Decision for you:** I planned against reality — surface the existing `RenderResult.Schedule` rather than build the `ChordScheduleBuilder` the design named as one option. This is faithful to IN1's intent (one shared feed, reuse `ChordChange`) and is what the design's "TBD during planning" invited, but it does deviate from the literal `ChordScheduleBuilder`. If you actually want a named `ChordScheduleBuilder` wrapper around the renderer's schedule (e.g. for a future non-alphaTex caller), say so and I'll amend step 1. Otherwise: `do step 1` / `do plan` when ready.

## Rafa:

I agree with you findinds and solutions.
ammend req if needed
I started plan
do plan

## AI:

Done — req amended + re-locked, then the whole plan implemented and verified. **922 tests pass**, Desktop builds clean.

## Req amend (v2, re-locked)
Reworded the handles that implied work that isn't there, keeping every `IN`/`EX`/`C` id:
- **IN1** — "extracted from the by-product" → the producer **already exists** (`AlphaTexRenderer.Render → RenderResult.Schedule`), both paths already call it; the work is to **surface** the discarded result.
- **IN2 / IN6** — annotated as already-true (comping unconditional; per-chord dedup inherent).
- **IN4 / IN5 / C4** — corrected to the real API: `onBeat` is 0-based, no `setVisible`, the toggle hides the page container, driven by the page's `onBeat` callback (the shared beat signal).
- **C3** — "the producer extraction" → "no extraction; Practice already consumes it".

## What landed (plan-001, 3 steps ✅)
1. **Core/bridge** — `ChordSheetResultEnvelope` gains `chordSchedule` (`ChordChange[]`); handler passes the `render.Schedule` it already computes. Two new handler tests: grips present even at adornment `none` (IN2), labels `C7,F7,C7` proving per-chord dedup (IN6), and `chordSchedule` distinct from `cellSchedule` (C1).
2. **JS** (`chord-sheets.js`) — `ChordFlowNowNext` mounted above the sheet, fed `msg.chordSchedule`, advanced via `nowNext.onBeat(bar-1, beat-1)` in the existing `onBeat`, `reset()` on finish, and `onToggleNowNext` hiding the container. Reused as-is; ChordSheetR/SVG untouched.
3. **Ref sync** — `chordflow-architecture-reference.md` §5 (ChordSheetR verb shape + FretR now-next-on-both-surfaces) and §6 (shared-schedule data flow). No domain-model-ref change — no new named type.

## Not automated (needs your eyes)
The `wwwroot` side has no JS test harness (EX5), so the **visual dogfood is manual**: launch with `CHORDFLOW_DEVTOOLS=1`, open Chord Sheets, pick a song, play — the Now/Next boards should advance per chord in sync, and the PlayerControlsR **Now/Next** checkbox should show/hide them.

I did **not** commit (you didn't ask). Say the word and I'll stage + commit; the trigger chat re-dirties on the next turn as usual.

## Rafa:

now / next working in chord sheets
curiosity: before you ammed req showed 4 gaps, after you ammed req still shows 4 gaps
commit

## AI:

Glad it's working on the page.

## The "4 gaps" — my mistake, and why the amend didn't move them

I ran `loom_verify_req` to be sure. The 4 aren't coverage holes — `uncovered: []` (every IN is covered) and `unknownCitations: []`. They're **excludedViolations**:

| Step | Cited in `satisfies` |
|------|----------------------|
| 1 | `EX2`, `EX4` |
| 2 | `EX1`, `EX3` |

A step's `satisfies` is meant for **Included (IN) + Constraint (C)** handles the step *advances*. I also listed the **Excluded** handles the step respects (EX1–EX4) — that reads nicely ("this reuses as-is, adds no new DTO/verb") but it's wrong modeling: you can't *satisfy* an exclusion, so `verify_req` flags each one. That's your 4.

**Why the amend didn't change the count:** the gap set is computed from the **plan's citations vs the req**, not from the req's wording. I only reworded req prose (IN1/IN4/C3/…) and kept every handle — I never touched the plan's `satisfies`. Same 4 citations before and after → same 4 flags.

**Why I can't just strip them now:** the plan is `done`, and done steps are immutable history (`loom_update_step` only amends *pending* steps). So the clean fix would have been to not cite EX at plan-creation. It's cosmetic and harmless — the exclusions *are* respected (no new DTO, no new verb, ChordFlowNowNext/SVG untouched); the flag only says I over-cited. I've noted the rule so future plans cite IN/C only and I'll `verify_req` before closing while steps are still editable. If you'd rather have it clean, I can reopen the plan and strip the EX citations — but I'd recommend leaving it as-is (real history, no scope impact).

Committing now.