---
type: done
id: pl_01KW0FP6EG5Y4P6C6SZY4R501W-done
title: Done — Engine-derived voicings — dogfood integration fixes
status: done
created: 2026-06-25
updated: 2026-06-25
version: 2
tags: []
parent_id: pl_01KW0FP6EG5Y4P6C6SZY4R501W
requires_load: []
---
# Done — Engine-derived voicings — dogfood integration fixes

All 5 steps complete; re-dogfood confirmed by Rafa (no duplicate voicings, automatic rows open read-only with Duplicate-to-user, the min/max region control changes the comped grips). 726/726 tests green.

## Fixes
- **IN12 — pack import reconciles.** `PackImporter` deletes its own `Origin.Pack` rows (same `PackId`) no longer shipped, per kind — so emptying the pack's voicings purges the stale rows on next launch (the duplicate `package`/`automatic` voicings the dogfood found). User copies (forked, fresh ids) untouched. `PackImportReconcileTests`.
- **IN13 — computed rows read-only.** `ContentCrudHandler.Get` derives a read-only voicing DSL for an `auto:` id via `AutomaticVoicingDoc` (lowest valid placement at C, robust to the Derive edge) instead of returning "not found". The Content view's read-only + "Duplicate to user" UI already existed (content-source-model). `AutomaticVoicingDocTests`, `ContentCrudHandlerTests.Get_AutomaticVoicing…`.
- **IN14 — Practice voicing-region control.** Min/max fret inputs on the Practice builder, sent on `renderOptions.voicing = {kind:"automatic", minFret, maxFret}`; changing them re-renders. Ranking stays Closest (mode selector is voicing-ranking-strategies).

## Also fixed in this pass (found in the re-dogfood)
- **`{firstfret N}` on chord diagrams** (folded into plan-001's renderer): `AlphaTexRenderer.ChordDefinition` emits `\chord (...) {firstfret N}` for grips ≥ fret 2. alphaTex ref updated; `AlphaTexRendererTests.ChordDefinition_GripUpTheNeck…`.
- **Open-root anchor bug** ([[caged-derive-anchor-edge]]): `AnchorFinger.Derive` no longer throws when the root is on an open string (open D7 `x x 0 2 1 2`); `anchorFret <= boxMin` → Index. Unblocked open-position comping (a 0–4 region couldn't comp D7). `CagedDeriveAnchorEdgeTests`; oracle stays 36/36.

## Still open (separate thread)
- `guitar/fretboard-fret-label-clip` — the `10fr`→`0fr` SVG label clip. Idea only.