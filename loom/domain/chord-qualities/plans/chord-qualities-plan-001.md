---
type: plan
id: pl_01KVJYYX38ZX36V4901FBS0R4W
title: Chord qualities — interval formulas as the authoritative layer — Plan
status: done
created: 2026-06-20
updated: 2026-06-20
version: 1
design_version: 1
req_version: 1
tags: []
parent_id: de_01KVJYPHF2G4CHGNEN9ZMFPGAY
requires_load: []
target_version: 0.1.0
actual_release: 0.9.0
steps:
  - id: qualityformulas-authority
    order: 1
    status: done
    description: Add Domain/QualityFormulas — the 9 v1 formulas as degree+accidental strings + Formula(Quality)
    files_touched: [src/ChordFlow.Core/Domain/QualityFormulas.cs]
    blocked_by: []
    satisfies: [IN1, IN2, C1, C4]
  - id: qualityintervals-derives
    order: 2
    status: done
    description: Rewire QualityIntervals to derive its semitone table from QualityFormulas via IntervalSpeller.ParseSet; delete the hand-authored arrays
    files_touched: [src/ChordFlow.Core/Domain/QualityIntervals.cs]
    blocked_by: []
    satisfies: [IN3, C2, C3]
  - id: formula-golden-oracle
    order: 3
    status: done
    description: Add QualityFormulasTests — assert each Formula string verbatim + Intervals(q) equals a hand-authored expected-semitone oracle for all 9 qualities; confirm existing domain tests stay green
    files_touched: [tests/ChordFlow.Core.Tests/QualityFormulasTests.cs]
    blocked_by: []
    satisfies: [IN4, C3]
  - id: ref-sync-same-unit-of-work
    order: 4
    status: done
    description: Update chordflow-domain-model-reference §1 — add QualityFormulas row, amend QualityIntervals row to 'derived'
    files_touched: [loom/refs/chordflow-domain-model-reference.md]
    blocked_by: []
    satisfies: [IN5]
---
# Chord qualities — interval formulas as the authoritative layer — Plan

## Goal

Make the interval formula the authoritative form of every chord quality and the semitone set a derived projection. Introduce `Domain/QualityFormulas` (the 9 v1 formulas authored as degree+accidental strings — the single source of truth for chord content), rewire `QualityIntervals` to derive its semitone table from those formulas via the already-shipped `IntervalSpeller.ParseSet` (deleting the hand-authored arrays, public surface unchanged), and pin the derivation with a `QualityFormulasTests` golden oracle (hand-authored expected semitones) while the existing `QualityIntervals`/`ChordTones`/`Diatonic` tests stay green byte-for-byte. Per the chat-settled Option A: formula-only stored, semitones as the test oracle (no second runtime source). Ref-sync to `chordflow-domain-model-reference` is part of the same unit of work.

---

## Steps

| Done | # | Step | Files touched | Blocked by | Satisfies |
|---|---|---|---|---|---|
| ✅ | 1 | Add Domain/QualityFormulas — the 9 v1 formulas as degree+accidental strings + Formula(Quality) | src/ChordFlow.Core/Domain/QualityFormulas.cs | — | IN1, IN2, C1, C4 |
| ✅ | 2 | Rewire QualityIntervals to derive its semitone table from QualityFormulas via IntervalSpeller.ParseSet; delete the hand-authored arrays | src/ChordFlow.Core/Domain/QualityIntervals.cs | — | IN3, C2, C3 |
| ✅ | 3 | Add QualityFormulasTests — assert each Formula string verbatim + Intervals(q) equals a hand-authored expected-semitone oracle for all 9 qualities; confirm existing domain tests stay green | tests/ChordFlow.Core.Tests/QualityFormulasTests.cs | — | IN4, C3 |
| ✅ | 4 | Update chordflow-domain-model-reference §1 — add QualityFormulas row, amend QualityIntervals row to 'derived' | loom/refs/chordflow-domain-model-reference.md | — | IN5 |
---

### Legend

| Symbol | Meaning |
|--------|---------|
| ✅ | Done |
| 🔄 | In Progress |
| 🔳 | Pending |
| ❌ | Cancelled |

<!-- step:qualityformulas-authority -->
### Step 1 — QualityFormulas authority

Pure static class in `ChordFlow.Domain`. A private `IReadOnlyDictionary<Quality, string>` table holding the 9 formulas verbatim (`Major`=`"1 3 5"` … `Augmented`=`"1 3 #5"`), root-up token order. `Formula(Quality quality)` returns the string, throwing `ArgumentOutOfRangeException` for an unmapped quality (mirrors `QualityIntervals.Intervals`). The ONLY authored chord-content data — no semitones stored.

<!-- step:qualityintervals-derives -->
### Step 2 — QualityIntervals derives

Replace the literal `new[] { 0, 4, 7 }` dictionary with one built once at static init: `Enum.GetValues<Quality>().ToDictionary(q => q, q => IntervalSpeller.ParseSet(QualityFormulas.Formula(q)).ToArray())`. `Intervals(q)` and `FromIntervals(set)` signatures and behavior unchanged — semitones now derived, parsed once, exactly one authored source per quality.

<!-- step:formula-golden-oracle -->
### Step 3 — Formula golden oracle

Two assertions per quality: (a) `QualityFormulas.Formula(q)` equals the authored string; (b) `QualityIntervals.Intervals(q)` equals a hand-written expected-semitone array (the design's Semitones column — authored independently in the test, the cross-check that keeps the derivation honest). Run the suite: existing `QualityIntervalsTests`/`ChordTonesTests`/`DiatonicChordTests` must pass unchanged (byte-for-byte parity oracle).

<!-- step:ref-sync-same-unit-of-work -->
### Step 4 — Ref sync (same unit of work)

§1 Harmony: add a `QualityFormulas` row (authored quality→formula table, degree+accidental strings, single source of truth for chord content). Amend the `QualityIntervals` row: it no longer hand-authors semitone arrays — it derives them from `QualityFormulas` via `IntervalSpeller.ParseSet` (still the public `Intervals`/`FromIntervals` semitone API). Edit via loom_patch_doc/loom_update_doc (refs are gate-excluded).
