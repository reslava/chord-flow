---
type: chat
id: ch_01KXMR432SEQVBR687BJEH6CCE
title: player-controls-component now-next boards on Chord Sheets Chat 002
status: done
created: 2026-07-16
updated: 2026-07-16
version: 1
tags: []
parent_id: null
requires_load: []
last_ai_block: 3
---
# player-controls-component now-next boards on Chord Sheets Chat 002

## Rafa:

about the deferred fast-follow — **now-next boards on Chord Sheets** (`EX1`)
from chat-001:
>- **now-next on Chord Sheets.** The idea lists the now-next *toggle* in PlayerControlsR (easy) **and** "Chord Sheets would newly mount `ChordFlowNowNext`." That second part is not a free extraction — the now/next fretboards need a *current/next-chord feed*. Practice gets it from the exercise beat schedule; Chord Sheets would have to derive it from the `cellSchedule`. That's a real feature layered on top of the control refactor.

could we derive *current/next-chord feed* once place and be available for both consumers ScoreR and ChordSheetR?

## AI:

Yes — and it *should* be one place, because the current/next-chord feed isn't a property of the presentation (tab vs sheet). It's a property of the **RealizedSong + the comping plan**: "at bar/beat X the sounding chord is C7, and here's its comped grip." Both surfaces expand the *same* RealizedSong, so both can emit the *same* feed.

Good news: Practice already has it. The `loadScore` envelope carries a **chord schedule** today — one `ChordChange` per chord change: `{ bar, beat (0-based), FretboardDiagram of the comped voicing }`, produced as a by-product of the render pass so it can't drift from the tab; `ChordFlowNowNext` drives the now/next fretboards off it. So the feed shape already exists; it's just only wired on one path.

**The unification (three moves, mostly reuse):**

1. **One Core producer.** Lift "walk the RealizedSong + CompingPlan → `ChordChange[]`" into a shared Core producer (it's currently a by-product of the exercise render). Both the exercise path and `ChordSheetBuilder`/`ChordSheetHandler` call it — the handler already holds the RealizedSong and resolves comping grips (for the diagram adornment), so it has everything it needs.
2. **Emit the same DTO on `chordSheetResult`.** Add the `ChordChange[]` schedule alongside the existing `{ sheet, cellSchedule, tex }`. (One caveat: emit the grip diagrams for now-next **independent of** the sheet's below-cell adornment toggle — now-next should show grips even when the sheet hides its per-cell diagrams.)
3. **One JS consumer, on both pages.** The existing `ChordFlowNowNext` component consumes that DTO unchanged, synced via the **`engine.on("beat")` bus we just built**. Chord Sheets mounts it and wires the **optional now-next toggle we already added to PlayerControlsR** — so no new JS component and no new event plumbing.

**One distinction worth keeping:** the sheet's `cellSchedule` (beat → *cell/chord*, for the SVG marker) stays separate from the chord/now-next schedule (beat → *chord + grip*). They're different projections — cell-addressing is sheet-specific — but both fall out of the *same* realized-song walk, so "one pass, two projections," not two independent derivations.

Net: the real new work is a small **shared Core `ChordSchedule` producer** + emitting it on the chord-sheet envelope + mounting `ChordFlowNowNext` on the sheet page. The JS side is almost free because the event bus + the optional toggle already landed.

Want me to spin this into the fast-follow's idea/design (its own thread, or continue in this one)? And one open question for the design: should now-next on Chord Sheets follow the **per-chord** granularity (like Practice's ChordChange) even when the sheet's marker is in **visual-metronome** mode — i.e. the boards advance per chord while the beat column ticks? I'd say yes (now/next is inherently per-chord), but it's worth pinning.

## Rafa:

Yes, now-next on Chord Sheets follow the **per-chord** granularity
Spin up a new thread + idea + design + req
then we can move to a new fresh session to write plan and implement, agree?

## AI:

Agreed — per-chord granularity locked in, and a fresh session for plan + implement is the right call (the plan wants a clean read of `now-next-fretboards.js` + the current `ChordChange` producer, which a fresh context serves best).

Spun up the new thread + the three docs:

- **Thread:** `ui/chord-sheets-now-next` (`th_01KXMS2D23BF8HQCF449JGC9BM`, depends_on `player-controls-component`).
- **Idea:** `id_01KXMS31BX5B4EABZKYSTP0SQQ` — now/next on Chord Sheets from one shared feed.
- **Design:** `de_01KXMS3Z1MTACNXG5R4NXKA5TR` — the shared `ChordScheduleBuilder` (RealizedSong + CompingPlan → `ChordChange[]`), the additive `chordSchedule` field on `chordSheetResult`, and mounting the existing `ChordFlowNowNext` off the `engine.on("beat")` bus + the PlayerControlsR toggle.
- **Req:** `rq_01KXMS4EVN3ABZAG3ERTAF11YN` — `IN1–7 / EX1–5 / C1–4`. **Status: draft** — review it, then the fresh session **locks it → writes the plan → implements**.

**One design decision flagged for your review** (in the design): reuse the existing `ChordChange` DTO + producer shape verbatim (Practice is the oracle) rather than a new schedule type — which makes the Practice-side change a pure extraction, verified by Practice's now/next staying unchanged. I recommend it; say the word if you'd rather shape it differently.

**Two things the plan session must confirm from code** (noted in the design's open list): where the current `ChordChange` by-product is produced (Rendering vs Features) → where the shared producer lives, and `ChordFlowNowNext`'s exact create/load/update/visibility API.

Committing these planning docs now so the fresh session opens on a clean tree. When you start it: `load ui/chord-sheets-now-next`, lock the req, then `generate-plan` / write the plan.