---
type: plan
id: pl_01KVFCBHR461J9CQ6B1NFKY52Y
title: Intervals — the theory substrate — Plan
status: done
created: 2026-06-19
updated: 2026-06-19
version: 1
design_version: 1
tags: []
parent_id: de_01KVF92S3CBV2XT4B8N9SF3038
requires_load: []
target_version: 0.1.0
actual_release: 0.7.0
steps:
  - id: intervalspeller-tests
    order: 1
    status: done
    description: Add Domain/IntervalSpeller (Name computed/unfolded + Label role-keyed/conventional) with its unit tests
    files_touched: [src/ChordFlow.Core/Domain/IntervalSpeller.cs, tests/ChordFlow.Core.Tests/IntervalSpellerTests.cs]
    blocked_by: []
    satisfies: [IN1, IN2, IN3, IN5, C1, C2, C3, C4]
  - id: voicingdiagram-delegates
    order: 2
    status: done
    description: Migrate VoicingDiagram to delegate spelling to IntervalSpeller.Label; delete its inline IntervalLabel/GenericLabel; confirm VoicingDiagramTests stay green
    files_touched: [src/ChordFlow.Core/Instruments/Guitar/Voicings/VoicingDiagram.cs]
    blocked_by: []
    satisfies: [IN4, C2]
  - id: reference-doc-sync
    order: 3
    status: done
    description: Update chordflow-domain-model-reference §1 — add IntervalSpeller row + note VoicingDiagram delegation
    files_touched: [loom/refs/chordflow-domain-model-reference.md]
    blocked_by: []
    satisfies: [IN6]
---
# Intervals — the theory substrate — Plan

## Goal

Stand up `Domain/IntervalSpeller` as the single interval-spelling authority and migrate the one current consumer onto it. `Name(semitone)` is the computed, unfolded, flats-only substrate vocabulary (number = baseNumber(sem%12) + 7*(sem/12); accidental from a 12-entry flats table — octave-extensible for free). `Label(semitone, role)` is the chord-context authority: role-keyed chord-tone spelling (`R/b3/3/b5/5/#5/b7/bb7/7`) falling back to the conventional compound tensions (`#9/#11/b13`) for out-of-chord notes — both tables lifted verbatim from `VoicingDiagram`. `VoicingDiagram` then delegates and drops its inline `IntervalLabel`/`GenericLabel`. Correctness is pinned two ways: a new `IntervalSpellerTests` for the extracted spec, and the existing `VoicingDiagramTests` staying byte-for-byte green as the regression oracle. The domain-model reference is updated in the same unit of work.

---

## Steps

| Done | # | Step | Files touched | Blocked by | Satisfies |
|---|---|---|---|---|---|
| ✅ | 1 | Add Domain/IntervalSpeller (Name computed/unfolded + Label role-keyed/conventional) with its unit tests | src/ChordFlow.Core/Domain/IntervalSpeller.cs, tests/ChordFlow.Core.Tests/IntervalSpellerTests.cs | — | IN1, IN2, IN3, IN5, C1, C2, C3, C4 |
| ✅ | 2 | Migrate VoicingDiagram to delegate spelling to IntervalSpeller.Label; delete its inline IntervalLabel/GenericLabel; confirm VoicingDiagramTests stay green | src/ChordFlow.Core/Instruments/Guitar/Voicings/VoicingDiagram.cs | — | IN4, C2 |
| ✅ | 3 | Update chordflow-domain-model-reference §1 — add IntervalSpeller row + note VoicingDiagram delegation | loom/refs/chordflow-domain-model-reference.md | — | IN6 |
---

### Legend

| Symbol | Meaning |
|--------|---------|
| ✅ | Done |
| 🔄 | In Progress |
| 🔳 | Pending |
| ❌ | Cancelled |

<!-- step:intervalspeller-tests -->
### Step 1 — IntervalSpeller + tests

New pure static class in `ChordFlow.Domain`, peer of `NoteSpeller`.

**`Name(int semitone)`** — flats-only, role-free, **computed and unfolded** (not mod-12):
- `number = baseNumber(sem % 12) + 7 * (sem / 12)`; `accidental = flatsTable[sem % 12]`.
- 12-entry base table: `0→("",1) 1→("b",2) 2→("",2) 3→("b",3) 4→("",3) 5→("",4) 6→("b",5) 7→("",5) 8→("b",6) 9→("",6) 10→("b",7) 11→("",7)`.
- So 0→`1` … 11→`7`, 12→`8`, 14→`9`, 17→`11`, 21→`13`, 24→`15`, ad infinitum. Negative input: clamp/guard or document non-negative (v1: assume ≥0).

**`Label(int semitone, ChordToneFunction? role)`** — chord-context, `semitone` reduced mod-12 defensively:
- `Root`→`R`; `Third`: 3→`b3` else `3`; `Fifth`: 6→`b5`,8→`#5`,else`5`; `Seventh`: 9→`bb7`,11→`7`,else`b7`.
- `null` (tension) → conventional compound table: `0→R 1→b9 2→9 3→#9 4→3 5→11 6→#11 7→5 8→b13 9→13 10→b7 11→7`.
- Both tables are the verbatim current `VoicingDiagram.IntervalLabel`/`GenericLabel` logic.

**`IntervalSpellerTests`:** `Name` octave-1 table (0..11) + the formula's octave-up series (12→`8`, 14→`9`, 17→`11`, 21→`13`, 24→`15`); `Label` every role branch + the full `role:null` tension table.

<!-- step:voicingdiagram-delegates -->
### Step 2 — VoicingDiagram delegates

Replace the `IntervalLabel(semitone, role)` call site with `IntervalSpeller.Label(semitone, role)` and **delete** the private `IntervalLabel` and `GenericLabel` methods. Keep `RoleByInterval` (which tertian position is 3rd/5th/7th) and `FunctionName` (role → colour-key string) — those are not interval spelling. No behavior change: the existing `VoicingDiagramTests` (`R`/`3`/`5`, `bb7`, `#5`, tension `9`) must pass **unchanged** — that is the byte-for-byte regression oracle for this step.

<!-- step:reference-doc-sync -->
### Step 3 — Reference-doc sync

Same unit of work (a `Domain/` kernel type changed). In §1 Harmony, add an `IntervalSpeller` row: the interval-naming peer of `NoteSpeller` — `Name(semitone)` the computed/unfolded flats substrate vocabulary, `Label(semitone, role)` the role-keyed chord-context labels with conventional tensions. Update the §2 `VoicingDiagram` row to note `IntervalLabel`/`GenericLabel` are gone, delegated to `IntervalSpeller`. Edit via `loom_patch_doc` (refs are gate-excluded but stay versioned).
