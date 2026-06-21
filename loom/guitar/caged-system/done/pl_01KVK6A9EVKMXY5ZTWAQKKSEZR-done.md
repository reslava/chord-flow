---
type: done
id: pl_01KVK6A9EVKMXY5ZTWAQKKSEZR-done
title: Done — CAGED derivation engine — derive shapes from theory
status: done
created: 2026-06-20
version: 2
tags: []
parent_id: pl_01KVK6A9EVKMXY5ZTWAQKKSEZR
requires_load: []
---
# Done — CAGED derivation engine — derive shapes from theory

## Step 1 — Add the Finger enum and the single global anchor-relative reach table (index 1/3, middle 1/1, ring 1/1 placeholder, pinky 4/0) plus the envelope computation: given an anchor finger + the OctaveShape octave zone, the [min,max] fret window the box may occupy past the zone. One global table, never per-shape; pure geometry beside IntervalLattice/OctaveShape.

**Hand-reach model + envelope** — `src/ChordFlow.Core/Instruments/Guitar/Caged/HandReach.cs` (+ `tests/ChordFlow.Core.Tests/HandReachTests.cs`, 7 passing).

- `Finger` enum (Index=1 … Pinky=4, ordered by natural fret position).
- `FretWindow(MinFret, MaxFret)` value type.
- `HandReach` — the single global reach table (Rafa's values: index 1/3, middle 1/1, ring 1/1 placeholder, pinky 4/0) + `Envelope(anchor, OctaveZone)` = `[zone.Min − behind, zone.Max + ahead]`, low edge clamped to 0.

Envelope verified against real grips: E-maj zone [8,10] + index → [7,13]; C-maj zone [1,3] + pinky → [0,3]. The pinky `behind 4` is what admits the stretchy C/G shapes. Satisfies IN3, C4, C1.

## Step 4 — Wire OctaveShape.AnchorsFor/Zone/Boxes + the QualityIntervals formula + candidate selection (3) + anchor finger (2) + envelope (1) into derive(quality, shape, root, neckRegion) → ChordShape (per-string fret/muted + anchor finger + box kind). Main box (2 roots) keeps all the quality's intervals; partial box (1 root) keeps only the rule-satisfying subset (the derived usable-subset signal).

**Engine built (steps 2–4).** Files in `src/ChordFlow.Core/Instruments/Guitar/Caged/`: `AnchorFinger.cs` (step 2 — root's rank in the box → finger), `CandidateSelector.cs` (step 3 — whole-box joint min: voice every distinct tone, minimize span then fret-sum, deterministic), `CagedDerivation.cs` + `ChordShape.cs` (step 4 — the `Derive(quality, shape, root, region)` integrator).

Key derived rules that fell out of calibrating against the authored grips (no per-quality authoring):
- Mute strings below the bass root; play the rest.
- Bass-most played string = the root at its octave anchor (root-position).
- **Directional stacking:** the box extends from the bass root toward the anchor finger's reach side — index-anchored shapes (E/A/D, bass root is the lowest octave anchor) stack UP; pinky-anchored (C/G, bass root is the highest anchor) stack DOWN. Derived from `OctaveShape.AnchorsFor`, replaces an earlier too-strict zone-containment rule.

Oracle harness `CagedDerivationOracleTests` (calibration tool): **23/34 exact**. 21/25 full qualities match; the 4 full misses are cases where the engine finds a *more compact valid* voicing than the authored grip; the 7 remaining are the trimmed symmetric/half-dim grips (engine plays the full box, authored is compacted). Step 5 (the oracle's definition) is the open decision — pending Rafa.
