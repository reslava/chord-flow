---
type: req
id: rq_01KVJYY9QK6XW9RSTT690YQQDV
title: Chord qualities — the interval formulas (engine input) — Requirements
status: locked
created: 2026-06-20
updated: 2026-06-20
version: 1
tags: []
parent_id: id_01KV2WWAZDDC1XWFF5AGTJX197
requires_load: []
---
# Chord qualities — the interval formulas (engine input) — Requirements

Authoritative scope for the chord-qualities thread, extracted from `chord-qualities-idea.md`, `chord-qualities-design.md`, and the decisions settled in `chord-qualities-chat-001` (Option A; formula-only stored, semitones as test oracle).

### ✅ Included

- `IN1` A pure `Domain/QualityFormulas` static class — the authored quality→interval-formula table, the single source of truth for chord content. `Formula(Quality)` → the degree+accidental spelling string; throws `ArgumentOutOfRangeException` for an unmapped quality.
- `IN2` The 9 v1 formulas authored verbatim as strings: `Major`=`1 3 5`, `Minor`=`1 b3 5`, `Major7`=`1 3 5 7`, `Dominant7`=`1 3 5 b7`, `Minor7`=`1 b3 5 b7`, `HalfDiminished7`=`1 b3 b5 b7`, `Diminished`=`1 b3 b5`, `Diminished7`=`1 b3 b5 bb7`, `Augmented`=`1 3 #5`.
- `IN3` `QualityIntervals` derives its semitone table from `QualityFormulas` via `IntervalSpeller.ParseSet` (parsed once at static init); the hand-authored `new[] { … }` arrays are deleted; the public `Intervals(q)` / `FromIntervals(set)` surface is unchanged.
- `IN4` `QualityFormulasTests` — assert each `Formula(q)` string verbatim **and** assert `QualityIntervals.Intervals(q)` equals a hand-authored expected-semitone oracle table (the design's Semitones column) for all 9 qualities.
- `IN5` `chordflow-domain-model-reference.md` §1 Harmony updated in the **same unit of work** — a `QualityFormulas` row + the `QualityIntervals` row amended to "derives from `QualityFormulas` via `IntervalSpeller.ParseSet`".

### ❌ Excluded

- `EX1` Extended / altered qualities themselves (6, 9, 11, 13, sus, alt) — the formula form keeps the door open additively, but none are implemented here.
- `EX2` A chord-symbol parser for arbitrary text — the DSL suffix tables stay as they are.
- `EX3` A structured `IntervalFormula` (degree, accidental) value type — Option B, rejected; the formula is a string parsed by `IntervalSpeller` (Option A).
- `EX4` Storing semitones as runtime data alongside the formula — semitones are derived and pinned only by the test oracle (`IN4`), never a second stored source.
- `EX5` Degree-driven function classification in `ChordTones` — `Classify` stays the semitone-band switch; deferred (was Option B's appeal).
- `EX6` Reproducing the 34 hand-authored CAGED voicings — the downstream end-to-end golden oracle via `caged-system`, not this thread.

### ⛓ Constraints

- `C1` Pure / immutable, no I/O, namespace `ChordFlow.Domain` (kernel purity).
- `C2` The formula is the **only** authored chord-content data; semitones are **always** derived — exactly one source of truth per quality, with no stored semitone duplication.
- `C3` Byte-for-byte parity — `QualityIntervals.Intervals` / `FromIntervals` output identical to today's hand-authored arrays; the existing `QualityIntervalsTests` / `ChordTonesTests` / `DiatonicChordTests` stay green **unchanged** as the regression oracle.
- `C4` Formula token order is root-up so the parsed semitones stay root-up — the position property (index *i* = 1st/3rd/5th/7th) that `ChordTones` relies on.