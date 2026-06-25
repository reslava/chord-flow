---
type: idea
id: id_01KW0FM81JP0DB4AFFZ6Y4Q6VG
title: CagedDerivation.Derive throws at extreme placements (anchor-fret edge)
status: draft
created: 2026-06-25
updated: 2026-06-25
version: 2
tags: []
parent_id: null
requires_load: []
---
# CagedDerivation.Derive throws at extreme placements (anchor-fret edge)

## Goal

Root-cause and fix `CagedDerivation.Derive` throwing **`ArgumentOutOfRangeException`** ("Anchor fret is outside the realized box", from `AnchorFinger.Derive`) for some quality×CAGED-shape combos at **extreme placements** — e.g. the full neck `[0, 15]` — that the golden oracle never exercises (it derives each shape in a shape-specific window keyed to the authored frets). The engine should return a valid grip, or fail with a **clean, specific** `InvalidOperationException` ("no grip in this region"), never an internal out-of-range throw.

## Origin

Surfaced by [[engine-derived-as-app-source]] (chat-002). That thread made `Derive` the app's `automatic` voicing source: `CompingResolver` derives grips over a fret **region** and currently **catches + skips** this exception as the region filter (`CompingResolver.AutomaticCandidates`), so comping stays robust — but the engine bug is **latent** and the workaround masks it.

## Why

The engine is the app's `automatic` voicing source. At certain regions some shapes **silently drop out** of the candidate set (caught + skipped), and a **single-shape preview** of such a combo can't be derived at all (relevant once automatic voicings are previewable in the Content view). A derivation engine that throws an internal `ArgumentOutOfRangeException` at a legal-looking placement is a correctness smell regardless.

## Shape (sketch — design firms this up)

- Reproduce: loop every `CagedVoicingCatalog.Combos` at `Derive(quality, shape, C, 0, 15)` and collect the throwers.
- Investigate `AnchorFinger.Derive(anchorFret, boxMinFret, boxMaxFret)` — the anchor fret falls outside `[boxMinFret, boxMaxFret]` when the bass root's lowest occurrence in the region yields an under-/over-stacked box (likely a low/open placement the authored set never used).
- **Decision (for design):** clamp/relax the anchor to the realized box, *or* fail loud with a specific `InvalidOperationException`. Either way the 36-grip oracle must stay 36/36.

## Scope

**In:** the `AnchorFinger`/`Derive` fix + a regression test deriving every catalog combo across the full `[0,15]` neck. **Out:** the resolver's skip workaround (keep it as defense-in-depth); the catalog/coverage set (unchanged).

## Resolution (FIXED — engine-derived-as-app-source chat-002)

Root cause found + fixed immediately because it was **blocking open-position comping** (a 0–4 region couldn't comp D7). The open root sits on fret 0; `CagedDerivation` builds the anchor box from **fretted notes only** (so boxMin ≥ 1), then `AnchorFinger.Derive(anchorFret=0, boxMin=1, …)` threw because the open root fell below the box. Fix: `AnchorFinger.Derive` now treats `anchorFret <= boxMin` (root at/below the box low edge, incl. an open root) as **Index / open position** — oracle-safe (its canonical-C grips never have an open root). Regression test `CagedDeriveAnchorEdgeTests`: open D7 derives `x x 0 2 1 2`; no catalog combo throws `ArgumentOutOfRangeException` across all 12 roots at `[0,15]`; the 36-grip oracle stays 36/36. The `CompingResolver`/`AutomaticVoicingDoc` skip remains as defense-in-depth.

## Validation

- Every `CagedVoicingCatalog` combo derives a valid grip across `[0, 15]` with no `ArgumentOutOfRangeException` (a clean `InvalidOperationException` is acceptable only where genuinely no grip fits).
- `CagedDerivationOracleTests` + `CagedAnchorFingerOracleTests` stay 36/36.
- The `CompingResolver` skip no longer hides any combo at full neck.