---
type: req
id: rq_01KVFCHEZ3C1ANBNG4D0FGNW3Z
title: Intervals — the theory substrate (deferred, captured) — Requirements
status: locked
created: 2026-06-19
updated: 2026-06-19
version: 1
tags: []
parent_id: id_01KTXEQADETHX82GXAV8T5A5GT
requires_load: []
---
# Intervals — the theory substrate (deferred, captured) — Requirements

Authoritative scope for the intervals theory-substrate thread, extracted from `intervals-idea.md`, `intervals-design.md`, and the decisions settled in `intervals-chat-001`.

### ✅ Included

- `IN1` A pure `Domain/IntervalSpeller` static class — the single interval-spelling authority, the interval-naming peer of `NoteSpeller`.
- `IN2` `Name(int semitone)` — the flats-only, role-free substrate vocabulary, **computed and unfolded** (not mod-12): `number = baseNumber(sem % 12) + 7 * (sem / 12)`, accidental from one 12-entry flats base table; octave-extensible (12→`8`, 14→`9`, 21→`13`, 24→`15`, …).
- `IN3` `Label(int semitone, ChordToneFunction? role)` — the chord-context authority: **role-keyed** chord-tone spelling (`R/b3/3/b5/5/#5/b7/bb7/7`), falling back for `role: null` to the **conventional** compound tensions (`b9 9 #9 11 #11 b13 13`).
- `IN4` `VoicingDiagram` delegates spelling to `IntervalSpeller.Label`; its inline `IntervalLabel` and `GenericLabel` are removed.
- `IN5` `IntervalSpellerTests` pinning both methods (Name octave-1 + octave-up series; Label every role branch + the full `role:null` tension table).
- `IN6` `chordflow-domain-model-reference.md` updated in the **same unit of work** — an `IntervalSpeller` row + the `VoicingDiagram` delegation note.

### ❌ Excluded

- `EX1` A spelling-aware `Interval` value type (P5/M3/m7…) — keep `int` semitones + the speller.
- `EX2` Refactoring `Scale` / `QualityIntervals` / triads / arpeggios to "derive from" this layer — they already own their semitone arrays; revisit only when a spelling-aware type earns its keep.
- `EX3` The fretboard interval-position lattice (where each interval sits on the neck) — owned by `interval-lattice` in the `guitar` weave.
- `EX4` Alternate tunings — a fretboard concern.
- `EX5` Flats-style diagram tensions (`b10`/`b12`) — the player-facing diagram stays conventional (`#9`/`#11`).

### ⛓ Constraints

- `C1` Pure / immutable, no I/O, in namespace `ChordFlow.Domain` (kernel purity).
- `C2` `Label` output is **byte-for-byte identical** to the current `VoicingDiagram` labels — the existing `VoicingDiagramTests` (`R`/`3`/`5`, `bb7`, `#5`, tension `9`) stay green unchanged as the regression oracle.
- `C3` `Name` is **computed** from one 12-entry base table, not a hand-written per-degree array (no literal to mis-transcribe; every octave falls out of the formula).
- `C4` Two distinct label spaces by design: `Name` indexed by **absolute** semitone (flats, octaves real); `Label` by **`(pc mod-12, role)`** (octaves folded by function — a tension reads `9` regardless of register).