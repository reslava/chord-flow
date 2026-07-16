---
type: chat
id: ch_01KXPA55AT7FEBJ3JQVABNNYKE
title: sheet-pickup-bar Chat 001
status: done
created: 2026-07-16
version: 1
tags: []
parent_id: null
requires_load: []
last_ai_block: 7
---
# sheet-pickup-bar Chat 001

## Rafa:

is idea grounded for a design?

## AI:

**Short answer: yes — grounded enough to design, with one factual correction and three decisions the design must pin.** I verified the idea against the code (`ChordSheetBuilder`, `ChordSheet` model, `ExerciseRendering.RenderWithSheet`, `chord-sheets.js`, `playback-component.js`) and the metronome-true-marker done doc.

### What checks out

- **The gap is exactly where the idea says.** `ChordSheetBuilder.Build` walks `realized.Sections` only — the pickup never produces a cell (`ChordSheetBuilder.cs:72-117`). The score side renders it fine (`AlphaTexRenderer.cs:101-109`, `\ac` + first chord).
- **The seams the idea needs already exist.** `ChordSheetCell` carries `BarTicks` (a natural width datum for a narrow lead-in cell); ChordSheetR already splits cells proportionally by ticks; the sheet + cellSchedule ride the unified `loadScore` reply, so a new cell flag flows to JS with no envelope work; exports render the same SVG — free.
- **The playback side is proven.** The clock counts the pickup as master bar 1 (1-based) with its real quarter count (test B in the metronome-true-marker done doc), so a lead-in cell at schedule bar 0 gets stepped correctly with zero clock changes.

### One correction — the marker is NOT "correctly silent" during the pickup

The idea (inherited from the origin chat) claims the marker stays silent during the pickup. The code says otherwise, and I think it matches what you actually saw ("Sheet almost start at bar 2"):

- The builder's `globalBar` counts **full bars from 0** — the comment says it "lines up with the AlphaTexRenderer's BarIndex", but that only holds **without a pickup**: the renderer's `state.BarIndex` counts the `\ac` bar as bar 0 (`RenderBar` increments on it), and alphaTab's master bars do too.
- So with a pickup, during the anacrusis the clock reports bar 1 → `chord-sheets.js` does `scheduleByBar.get(0)` → that's the **first full bar's cell** → the marker highlights it early, and **every bar of the song highlights one cell ahead**. Same off-by-one hits `OverlaySchedule` (mid-bar chord onsets attach one bar late) and Per-chord mode.

This *strengthens* the idea: the lead-in cell isn't cosmetic — emitting the pickup as **builder bar 0** restores the builder ↔ renderer ↔ alphaTab bar alignment by construction, fixing the marker, the overlay, and Per-chord mode in one move, with no JS offset hacks. (Worth a 60-second visual/CDP confirm of the off-by-one before we build on it.) The idea's Problem paragraph should be amended once confirmed.

### Decisions the design must pin

1. **Bar-index contract** — recommended: the lead-in cell is schedule bar 0; `globalBar` starts at the pickup. No-pickup songs are byte-identical to today.
2. **Input threading** — the pickup lives on the **RhythmPattern** (`exercise.Comping`), not on `Song`/`RealizedSong`; `Build` never sees rhythm today. Thread the pickup (or the pattern) into `Build` — `RenderWithSheet` already holds `exercise.Comping`, so it's a parameter, not a restructure. Lead-in cell = first chord of the first section (same rule as the renderer), `BarTicks = pickup.LengthTicks`.
3. **Model shape + placement** — an `IsPickup` flag on `ChordSheetCell` (my lean), and where it sits: prepended to section 0 / row 0 (that row becomes barsPerRow + 1 cells) vs. its own short row. Affects both layouts; also the simile rule — the first full bar must never compare against the pickup cell (no `%` of a lead-in).
4. **Drawing rule** — width proportional to `BarTicks` vs. fixed narrow width + a "pickup" annotation; Layout A vs. B treatment.

Point 1 I'd call settled (it's the correctness fix); points 3–4 are genuine taste/layout calls I'd like your read on before drafting. Want me to draft `design.md` with these baked in?

## Rafa:

Agree
write design + req

## AI:

Design + req written and the req is **locked** (`design.md` v1, `req.md` v1). I loaded the domain-model ref first (per the ref-sync rule) and grounded the drawing seam in `chord-sheet-render-component.js` (`BAR_W = 138`, per-beat highlight columns already exist — the pickup cell just needs its own count).

**Design decisions (D1–D5):**

1. **D1 — bar-index contract (the correctness core):** the lead-in cell is schedule **bar 0**; `globalBar` counts the pickup. `OverlaySchedule` and `chord-sheets.js` need **zero changes** — the existing `get(bar - 1)` becomes correct by construction. Rejected the JS-offset alternative.
2. **D2 — threading:** `Build` gains a `PickupMeasure? pickup` param (not the whole `RhythmPattern` — the builder only needs `LengthTicks`); `RenderWithSheet` passes `exercise.Comping.Pickup`.
3. **D3 — model/placement (my call, per your Agree):** `IsPickup` trailing-optional flag on `ChordSheetCell`; lead-in **prepended to section 0 / row 0** (that row holds barsPerRow + 1 cells) — a real leadsheet shows the pickup at the start of the first line; a dedicated row would waste a printed line on a 1-beat bar. Emitted before the section walk so the first full bar can never be a `%` of it (C3).
4. **D4 — drawing (my call):** width `max(0.4, barTicks/fullBarTicks) × BAR_W` — proportional so it reads short, floored so the chord symbol fits — plus a muted "pickup" label; beat columns `ceil(barTicks/48)` so the Visual-metronome marker steps the pickup's **real** quarters. Both layouts; exports free.
5. **D5 — doc sync:** the `globalBar` comment, `CellScheduleEntry` XML doc, and the domain-model ref's Chord-sheet sections all still state the wrong alignment claim — updated in the same unit of work (IN7).

The req (IN1–IN7 / EX1–EX3 / C1–C3) captures the off-by-one fix as first-class scope (IN2), not a side effect — with-pickup tests plus a no-pickup byte-identical regression (IN6/C1).

**Veto window:** D3 placement and D4 width rule were the two taste calls — say the word if you'd rather have a dedicated pickup row or a fixed narrow width.

Next step: `loom_create_plan` (goal + structured steps citing the req handles). Say `go` and I'll draft it.

## AI:

Plan created — `plan-001` (`pl_01KXPGP8DSQEQX2T987AYG7AA0`), 5 steps, all req handles covered:

1. **Model + builder** — `IsPickup` on `ChordSheetCell`, `PickupMeasure?` param on `Build`, lead-in cell + bar-0 schedule entry, `globalBar` counts the pickup, `RenderWithSheet` passes `exercise.Comping.Pickup`, comment/XML-doc fixes → IN1 IN2 IN3 IN5 C2 C3
2. **C# tests** — pickup projection, schedule alignment (+1 shift), simile guard, overlay attachment, no-pickup byte-identical regression → IN6 C1 C3 *(blocked by 1)*
3. **ChordSheetR** — floored-proportional lead-in cell + "pickup" annotation, both layouts, `ceil(barTicks/48)` beat columns → IN4 *(blocked by 1)*
4. **Ref sync** — domain-model ref's Chord-sheet sections updated to the new bar-index contract → IN7 *(blocked by 1)*
5. **Verify** — build + tests + the validation walk (marker no longer one cell ahead; both modes; mid-bar splits; no-pickup regression; exports) → C1 IN6 *(blocked by 2, 3)*

Steps 1→2 are the correctness core; 3 is the visible feature; 4–5 close the loop. Ready to implement — say `do step 1` (or `do plan` to run it through).

## AI:

`do plan` ran through. **Steps 1–4 ✅, step 5 half-done** — the automated verification passed everywhere; the visual walk is yours (step 5 stays ⬜ until then).

**What landed:**

1. **Model + builder** — `ChordSheetCell.IsPickup`; `Build(…, PickupMeasure? pickup)`; the lead-in cell (first chord, real `BarTicks`, prepended to section 0/row 0, outside simile detection) emitted as **schedule bar 0** with `globalBar` counting it; `RenderWithSheet` passes `exercise.Comping.Pickup`; the wrong alignment comments fixed. `OverlaySchedule` and `chord-sheets.js` untouched — correct by construction now.
2. **Tests** — 5 new builder facts (lead-in shape, bar-0 schedule + shifted indices, simile guard, no-pickup regression, first-ever `OverlaySchedule` test proving the mid-bar onset lands on the right bar).
3. **ChordSheetR** — per-cell widths (cumulative x): lead-in at `max(0.4, ticks/192)×BAR_W` + italic "pickup" tag; beat columns = its real quarters; Layout A re-wraps full bars only then prepends the lead-in (marker addressing stays schedule-aligned); sheet width = widest row in pixels. Exports share the SVG.
4. **Ref sync** — domain-model ref: `IsPickup` in the cell signature, the lead-in rule, and the explicit bar-index contract (+ the off-by-one it repairs).

**Verification so far:** full suite **923/923**, including a new renderer fact `Render_Schedule_WithPickup_CountsTheAnacrusisAsBarZero` — first numeric proof from the renderer side that the `\ac` bar consumes bar 0 (`(0,0,Bb7),(2,0,Eb7)` for `"17 47"` + 1-quarter pickup). Plus a headless DOM harness over the real component file (scratchpad, not committed): **20/20 geometry checks** across both layouts, floor + proportional widths, beat-column counts, re-wrap parity, and a byte-identical no-pickup baseline.

**Your walk (the default pack ships no pickup rhythm, so author one first):**
1. Create a rhythm like `PICKUP: X... | X...X...X...X...` (1-quarter pickup + quarters bar) and use it as comping.
2. Sheet view, Layout A and B — narrow annotated lead-in before bar 1.
3. Play — Visual-metronome: marker steps the lead-in's quarter(s), then bar 1 on the downbeat, **no more one-cell-ahead drift**; Per-chord: lead-in lights during the anacrusis.
4. A no-pickup song — identical to before. 5. An export — lead-in included.

**Two honest notes:** (a) I fat-fingered `loom_complete_step(4)` while completing step 3 — no un-complete tool exists, so I did step 4's work immediately after to make the mark truthful; recorded in the done doc. (b) The new renderer test is in `AlphaTexRendererTests.cs`, slightly beyond step 2's declared files — added during verify because nothing pinned the renderer's half of the contract.

Say the word after the walk and I'll mark step 5 ✅ (and `docs done` when you're happy).

## Rafa:

Worked nicely, I tried pickups of 1,2 and 3 bars and showed well.
mark step 5 done
commit

## AI:

Step 5 ✅ — your walk recorded in the done doc (nice touch trying 1-, 2-, and 3-quarter pickups: that exercised the width rule across its range — the 0.4 floor at 1 quarter, proportional 0.5 and 0.75 above it). Completing the last step auto-closed the plan: **plan-001 is done**, thread delivered.

**What shipped:** the pickup renders as a narrow annotated lead-in cell in both layouts (exports included), and — the correctness core — the sheet schedule now shares the renderer/alphaTab bar axis (lead-in = bar 0), which killed the one-cell-ahead marker drift and the late mid-bar onsets on every pickup song. 923/923 tests, two new contract facts pinning both sides of the axis.

**Committing now** (the last action of this turn): the four source files (`ChordSheet.cs`, `ChordSheetBuilder.cs`, `ExerciseRendering.cs`, `chord-sheet-render-component.js`), the two test files, the domain-model ref sync, and this thread's full doc set (idea/design/req/plan/done/chat).

Whenever you're ready: `docs done` to close out the thread's docs.