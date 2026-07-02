---
type: done
id: pl_01KVJYYX38ZX36V4901FBS0R4W-done
title: Done — Chord qualities — interval formulas as the authoritative layer — Plan
status: done
created: 2026-06-20
version: 4
tags: []
parent_id: pl_01KVJYYX38ZX36V4901FBS0R4W
requires_load: []
---
# Done — Chord qualities — interval formulas as the authoritative layer — Plan

## Step 1 — Add Domain/QualityFormulas — the 9 v1 formulas as degree+accidental strings + Formula(Quality)

Created `src/ChordFlow.Core/Domain/QualityFormulas.cs` — pure static class, the authored quality→formula table. Private `IReadOnlyDictionary<Quality,string>` holds the 9 v1 formulas verbatim in root-up token order (`Major`=`"1 3 5"` … `Augmented`=`"1 3 #5"`, `Diminished7`=`"1 b3 b5 bb7"`); `Formula(Quality)` returns the string and throws `ArgumentOutOfRangeException` for an unmapped quality (mirrors `QualityIntervals.Intervals`). The only authored chord-content data — no semitones stored. (IN1, IN2, C1, C4)

## Step 2 — Rewire QualityIntervals to derive its semitone table from QualityFormulas via IntervalSpeller.ParseSet; delete the hand-authored arrays

Rewired `src/ChordFlow.Core/Domain/QualityIntervals.cs` — deleted the hand-authored `new[]{…}` arrays; its `Table` is now built once at static init via `Enum.GetValues<Quality>().ToDictionary(q => q, q => IntervalSpeller.ParseSet(QualityFormulas.Formula(q)).ToArray())`. `Intervals`/`FromIntervals` signatures and behavior unchanged — semitones derived, parsed once, exactly one authored source per quality. Verified each formula decodes to the prior array (`bb7`→9, `#5`→8, root-up preserved). (IN3, C2, C3)

## Step 3 — Add QualityFormulasTests — assert each Formula string verbatim + Intervals(q) equals a hand-authored expected-semitone oracle for all 9 qualities; confirm existing domain tests stay green

Added `tests/ChordFlow.Core.Tests/QualityFormulasTests.cs` — a `[Theory]` over all 9 qualities asserting both `Formula(q)` verbatim and `QualityIntervals.Intervals(q)` against a hand-authored expected-semitone array (the design's Semitones column = the cross-check oracle), plus facts for non-empty-formula-per-quality and the unmapped-quality throw. Full suite: **575 passed, 0 failed** — existing `QualityIntervalsTests`/`ChordTonesTests`/`DiatonicChordTests` green unchanged (byte-for-byte parity oracle). (IN4, C3)

## Step 4 — Update chordflow-domain-model-reference §1 — add QualityFormulas row, amend QualityIntervals row to 'derived'

Updated `loom/refs/chordflow-domain-model-reference.md` §1 Harmony (via `loom_patch_doc`): added a `QualityFormulas` row (authored quality→formula table, degree+accidental strings, the only authored chord-content data) and amended the `QualityIntervals` row to "the semitone projection … derived at static init via `IntervalSpeller.ParseSet`, hand-authored arrays gone, public surface unchanged." Same unit of work as the code. (IN5)
