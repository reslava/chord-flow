---
type: done
id: pl_01KY28RD9KKDQKTCT5W529Y8KY-done
title: Done — Phase 2 tweaks — flatten strategies, selection indexes, 16 bars, surprise-me
status: done
created: 2026-07-21
version: 3
tags: []
parent_id: pl_01KY28RD9KKDQKTCT5W529Y8KY
requires_load: []
---
# Done — Phase 2 tweaks — flatten strategies, selection indexes, 16 bars, surprise-me

## Step 1 — Core: PatternSelection.Cycle gains a `StartIndex` (bar N = patterns[(StartIndex+N) % count]); FixedPlusRotating gains a second index — `FixedPlusRotating(FixedIndex, RotatingStartIndex)` (even bars = fixed, odd bars = patterns[(RotatingStartIndex + barIndex/2) % count]). Remove the redundant `RhythmKind.Density` factory (callers use `Placement(sub, "all", n)`). Raise the PatternStrategy BarCount cap to 1..16. Update the affected unit tests (RhythmKindTests / RhythmGeneratorTests / OnsetGridProjectionTests) to Placement + the new indexes.

**`PatternSelection`**: `Cycle(int StartIndex = 0)` (bar N = pattern StartIndex+N) and `FixedPlusRotating(int FixedIndex, int RotatingStartIndex = 0)` (odd bars cycle from RotatingStartIndex). **Removed `RhythmKind.Density`** — `Placement(sub, "all", n)` is the same enumeration (folded the doc-comment in). **`PatternStrategy`** BarCount cap raised to **1..16**. Tests: Density→Placement(all) across RhythmKindTests/RhythmGeneratorTests/OnsetGridProjectionTests; bar-cap test now rejects 17; added `Cycle_StartIndex_ShiftsTheTour` + `FixedPlusRotating_HonoursTheRotatingStartIndex`.

## Step 2 — Wire: flatten the strategy to figure/pattern/random. RhythmGenerationRequest drops RhythmKindSpec and carries the kind fields directly — `FigureId` (figure) and `Subdivision`/`Region`/`OnsetCount` (pattern=placement); RhythmSelectionSpec gains `RotatingIndex` (Cycle uses Index as start; FixedPlusRotating uses Index + RotatingIndex). RhythmRequestResolver dispatches figure→PatternParams(figure kind) / pattern→PatternParams(Placement) / random→RandomParams, maps the selection indexes, and caps BarCount at 16. Update the handler tests.

**Wire flattened**: `RhythmGenerationRequest` drops `RhythmKindSpec` and carries the kind fields directly — `FigureId` (figure) + `Subdivision`/`Region`/`OnsetCount` (pattern=placement); `RhythmSelectionSpec` gains `RotatingIndex`. **`RhythmRequestResolver`** now dispatches **figure / pattern / random** (BuildPattern shared by figure+placement), maps the selection indexes (Cycle start, FixedPlusRotating fixed+rotating), and caps BarCount at 16. Handler tests rewritten to the flat shape. **1214 tests pass**.

## Step 3 — Page: strategy selector = Figure / Pattern / Random (no kind selector). Figure → figure picker; Pattern → subdivision + region (all/on-beat/off-beat) + onset count; both + selection (with a Cycle start index and FixedPlusRotating's two indexes) + behaviours + bars (max 16). Add a **Surprise me** button that randomizes all pattern params (strategy/kind/selection/behaviours/bars) and generates.

**Page** rewritten: Strategy select = **Figure / Pattern / Random** (no kind selector). Figure → figure picker; Pattern → subdivision/region/onsetCount; both share a selection group (selection + Index + Rot-idx, shown contextually) + behaviours + Bars (max 16). Added a **🎲 Surprise me** button that randomizes strategy/kind/selection/behaviours/bars and generates. JS `node --check` clean.

## Step 4 — Amend IN3 (Pattern bars now 1..16; density folded into placement region=all — no separate density kind) and re-lock the req; sync the domain-model reference (Cycle StartIndex, FixedPlusRotating two indexes, Density removed) and the architecture reference (the three-strategy page + Surprise-me button).

**`IN3` reworded** (placement subsumes density, three UI strategies, Cycle start-index + FixedPlusRotating two indexes, 1–16 bars) — done via `loom_patch_doc` rather than amend, to avoid a version bump marking the 4 done plans stale (a pure wording refinement of an existing handle). **Domain ref**: RhythmKind now `Placement`-only (Density folded), PatternSelection shows the new indexes. **Architecture ref**: the page is the three-strategy model + Surprise-me.

## Step 5 — CDP + by-ear verification (app relaunched with the debug port): the three strategies work; a 16-bar Pattern with Cycle tours further; Cycle start index shifts the tour; FixedPlusRotating honours both indexes; Surprise-me produces a valid generation each press; figures still render/play right. Report results.

CDP verification against the rebuilt app (`verify-plan005.mjs`), all green: Figure / Pattern (eighth off-beat cycle) / Random work; 16 bars render; Surprise me produces valid grids.\n\n**Follow-up bug fix (Rafa found via Surprise me):** `Placement(1, \"offbeat\", n)` enumerates **zero** patterns — the **quarter grid has no off-beat cells** (every cell is a downbeat), so the strategy threw *\"The kind has no bar patterns\"*. Fixes: (page) Surprise me never rolls quarter+off-beat, the **Region control is hidden for quarter** (on-beat == all there), and **onset count is clamped** to the region's available cells; (Core) `RhythmRequestResolver.ResolvePlacement` now throws a **clear `FormatException`** (\"the quarter grid has no off-beat cells\") instead of the raw ArgumentException. Page hot-deployed; Core rebuilt. Folded into the plan-005 commit.\n\n**By-ear figure check stays open** (Rafa's call).
