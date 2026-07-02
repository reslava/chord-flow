---
type: done
id: pl_01KVFCBHR461J9CQ6B1NFKY52Y-done
title: Done — Intervals — the theory substrate — Plan
status: done
created: 2026-06-19
version: 3
tags: []
parent_id: pl_01KVFCBHR461J9CQ6B1NFKY52Y
requires_load: []
---
# Done — Intervals — the theory substrate — Plan

## Step 1 — Add Domain/IntervalSpeller (Name computed/unfolded + Label role-keyed/conventional) with its unit tests

Added `src/ChordFlow.Core/Domain/IntervalSpeller.cs` (namespace `ChordFlow.Domain`, peer of `NoteSpeller`):
- **`Name(int semitone)`** — computed/unfolded flats vocabulary. One 12-entry `FlatsBase` table of `(accidental, baseNumber)`; `number = baseNumber + 7*(semitone/12)`. Throws `ArgumentOutOfRangeException` on negative input.
- **`Label(int semitone, ChordToneFunction? role)`** — reduces semitone mod-12, then role-keyed: Root→`R`, Third `b3`/`3`, Fifth `b5`/`5`/`#5`, Seventh `bb7`/`7`/`b7`, `null`→conventional `Tension[]` table (`b9 9 #9 11 #11 b13 13`). Logic verbatim from `VoicingDiagram.IntervalLabel`/`GenericLabel`.

Added `tests/ChordFlow.Core.Tests/IntervalSpellerTests.cs` — `Name` octave-1 (0..11) + unfolded octave-up series (12→`8` … 24→`15`) + negative guard; `Label` every role branch + full `role:null` tension table + mod-12 reduction. **50 tests pass.** (IN1, IN2, IN3, IN5, C1, C2, C3, C4)

## Step 2 — Migrate VoicingDiagram to delegate spelling to IntervalSpeller.Label; delete its inline IntervalLabel/GenericLabel; confirm VoicingDiagramTests stay green

`src/ChordFlow.Core/Instruments/Guitar/Voicings/VoicingDiagram.cs`: replaced the `IntervalLabel(semitone, role)` call site with `IntervalSpeller.Label(semitone, role)` and **deleted** the private `IntervalLabel` and `GenericLabel` methods. Kept `RoleByInterval` (tertian position → role) and `FunctionName` (role → colour-key string) — not interval spelling. No behavior change: the full Core suite is green — **454 passed, 0 failed** — including the unchanged `VoicingDiagramTests` (`R`/`3`/`5`, `bb7`, `#5`, tension `9`), the byte-for-byte regression oracle. (IN4, C2)

## Step 3 — Update chordflow-domain-model-reference §1 — add IntervalSpeller row + note VoicingDiagram delegation

`loom/refs/chordflow-domain-model-reference.md` (via `loom_patch_doc`): added an **`IntervalSpeller` row** to §1 Harmony (the interval-naming peer of `NoteSpeller`; both label spaces — computed/unfolded `Name`, role-keyed conventional `Label`); updated the §2 `VoicingDiagram` row to note interval-label spelling is now **delegated to `IntervalSpeller.Label`** and the inline `IntervalLabel`/`GenericLabel` were removed. (IN6)
