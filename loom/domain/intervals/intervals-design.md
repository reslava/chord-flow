---
type: design
id: de_01KVF92S3CBV2XT4B8N9SF3038
title: Intervals — the theory substrate — Design
status: done
created: 2026-06-19
updated: 2026-06-19
version: 9
tags: []
parent_id: id_01KTXEQADETHX82GXAV8T5A5GT
requires_load: []
---
# Intervals — the theory substrate — Design

## Goal

Establish a first-class **interval-spelling authority** in the `Domain/` kernel: the single
place that turns a semitone distance into its theory name. It owns **two label spaces** —
the plain octave-degree vocabulary (`1 b2 2 b3 3 4 b5 5 b6 6 b7 7`) and the chord-context
labels (role-aware chord tones + compound tensions) — and **centralizes the spelling logic
already implemented ad-hoc** in `VoicingDiagram.IntervalLabel` / `VoicingDiagram.GenericLabel`.

This is the theory substrate the [[caged-system]] derivation engine will read. The fretboard
*projection* of intervals is out of scope (split to [[interval-lattice]] in the `guitar` weave).

Grounded against: `Domain/QualityIntervals`, `Domain/Scale`, `Domain/ChordTone` +
`ChordToneFunction`, `Domain/NoteSpeller` (the naming peer), and
`Instruments/Guitar/Voicings/VoicingDiagram` (the de-facto current spec + first consumer).

---

## 1. The type — `Domain/IntervalSpeller`

A pure static class, the interval peer of `NoteSpeller` (which spells pitch classes per key;
this spells semitone intervals). Lives in `ChordFlow.Domain`, no I/O, unit-tested. **No new
value type** — the idea defers a spelling-aware `Interval` struct (P5/M3/m7…) as out of scope;
a static speller over `int` semitones is the minimal correct surface.

```csharp
namespace ChordFlow.Domain;

public static class IntervalSpeller
{
    /// Plain interval-degree name — the substrate vocabulary, role-free, flats-only,
    /// computed over ANY distance (NOT folded): the second octave yields 9/10/11/13…,
    /// so 0→"1" … 11→"7", 12→"8", 14→"9", 17→"11", 21→"13", 24→"15", and on up.
    /// number = baseNumber(sem % 12) + 7 * (sem / 12); accidental from the 12-entry flats table.
    public static string Name(int semitone);

    /// Chord-context label. When `role` is a chord-tone function, spell by function
    /// (role-keyed, so a shared pitch class spells correctly); when `role` is null
    /// (a note outside the quality), fall back to the compound tension name.
    public static string Label(int semitone, ChordToneFunction? role);
}
```

### `Name` — the substrate vocabulary, **computed and unfolded**

The flats-only interval degrees, role-free, over *any* distance — not mod-12-folded. A tension
is just a chord tone an octave up, so the series is **derived by formula**, not a hand-written
table (one 12-entry base table is the only data — every octave falls out for free, and there's
no literal array to mis-transcribe):

```
number     = baseNumber(sem % 12) + 7 * (sem / 12)   // +7 scale-steps per octave
accidental = flatsTable[sem % 12]                     // the only data
```

Root glyph is **`1`** (a scale degree), distinct from `Label`'s **`R`** (a chord root) —
intentional: scales count degrees, chords name a root.

| semitone | 0 | 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8 | 9 | 10 | 11 |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| base | 1 | b2 | 2 | b3 | 3 | 4 | b5 | 5 | b6 | 6 | b7 | 7 |

Octave 2 then reads `8 b9 9 b10 10 11 b12 12 b13 13 b14 14`, octave 3 `15 …`, ad infinitum.
This is the **substrate** spelling (scales/arpeggios, which have real octaves) — flats-regular,
deliberately *not* the conventional `#9/#11` the diagram uses (see `Label`).

### `Label` — chord-context, role-keyed

Spelling is a **function of (semitone, role)**, not a flat `semitone → name` map — the central
finding from the review. The same pitch class spells differently by role:

| role | rule | examples |
|---|---|---|
| `Root` | always `R` | 0 → `R` |
| `Third` | 3 → `b3`, else `3` | 3 → `b3`, 4 → `3` |
| `Fifth` | 6 → `b5`, 8 → `#5`, else `5` | 6 → `b5`, 7 → `5`, 8 → `#5` |
| `Seventh` | 9 → `bb7`, 11 → `7`, else `b7` | 9 → `bb7`, 10 → `b7`, 11 → `7` |
| `null` (tension) | compound tension table ↓ | 1 → `b9`, 8 → `b13` |

Compound tension table (the `null` fallback — a note outside the quality):

| semitone | 0 | 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8 | 9 | 10 | 11 |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| tension | R | b9 | 9 | #9 | 3 | 11 | #11 | 5 | b13 | 13 | b7 | 7 |

These two tables are lifted **verbatim** from the current `VoicingDiagram.IntervalLabel`
(role branch) and `GenericLabel` (tension fallback) — same output, new home.

> **Why the tension space differs from `Name`.** `Name` uses simple octave forms
> (`b2 4 b6 6`); the chord-context tension fallback uses compound forms (`b9 11 b13 13`),
> because a non-chord tone *over a chord* is conventionally a tension/extension, not a scale
> step. Both are correct in their frame — that is precisely why the layer owns **both**.

---

## 2. Consumer migration — `VoicingDiagram` delegates

`VoicingDiagram` keeps deciding **which** semitone is the chord's 3rd/5th/7th
(`RoleByInterval(quality)`, a tertian-position concern that stays put) and keeps
`FunctionName` (role → colour-key string — a presentation concern, not interval spelling).
Only the **spelling** moves:

- **Delete** `VoicingDiagram.IntervalLabel(semitone, role)` and `VoicingDiagram.GenericLabel(semitone)`.
- **Replace** the call site `IntervalLabel(semitone, role)` with `IntervalSpeller.Label(semitone, role)`.

Net: `VoicingDiagram` shrinks; the spelling lives in one authority. No behavior change —
guaranteed by the oracle below.

---

## 3. Tests & oracle

**Immediate oracle (byte-for-byte): the existing `VoicingDiagramTests` stay green, unchanged.**
They already pin the live contract — `R`/`3`/`5` (open C major), `bb7` (dim7), `#5` (aug),
and a `tension` `9` (non-chord tone). If those four pass after delegation, the move is correct.

New `IntervalSpellerTests` (the unit-level spec for the extracted authority):
- `Name` — assert the full octave-1 table (0..11); assert the octave-up series via the formula
  (12 → `8`, 14 → `9`, 17 → `11`, 21 → `13`, 24 → `15`) so the unfolded computation is pinned.
- `Label` — assert every (semitone, role) branch: the four role rules above **plus** the full
  compound tension table for `role: null`. This pins the de-facto spec independently of the
  diagram, so a future edit to either table is a deliberate, tested change.

---

## 4. Scope & decisions

### Decisions (settled — `intervals-chat-001`)

- **Ship both `Name` and `Label` now (A).** The substrate vocabulary *is* this thread's headline
  deliverable; it's a tiny pure formula fully pinned by a unit test (its own golden oracle), and
  capturing it is the point of "intervals — the theory substrate." Aligns with the approved idea
  ("owns both label spaces") and [[design-philosophy-durable-over-minimal]].
- **Diagram tensions stay conventional (`#9/#11/b13`), not flats (`b10/b12`).** The player-facing
  surface uses the names lead-sheet readers expect; the flats-regular series is the substrate's
  (`Name`). This is *why* the two label spaces don't collapse: `Name` is indexed by absolute
  semitone (octaves real), `Label` by `(pc mod-12, role)` (octaves folded by function — a tension
  reads `9` regardless of register). Different questions, by design.

### Explicitly out of scope (deferred, unchanged from the idea)

- A spelling-aware `Interval` value type (P5/M3/m7…) — keep `int` semitones + the speller.
- Refactoring `Scale` / `QualityIntervals` / triads / arpeggios to "derive from" this layer —
  they already own their own semitone arrays; no real call-site changes here. Revisit only when
  a spelling-aware type earns its keep.
- The fretboard interval-position lattice → [[interval-lattice]] (`guitar` weave).
- Alternate tunings → a fretboard concern.

### End-to-end validation (downstream, not in this thread)

Through [[caged-system]], this vocabulary + [[interval-lattice]] + [[octave-shapes]] +
[[chord-qualities]] must reproduce the 34 hand-authored CAGED voicings exactly — the golden
oracle. This thread's slice ends at the byte-for-byte `VoicingDiagram` parity above.

---

## 5. Reference-doc sync (same unit of work)

This adds a `Domain/` kernel type → **update `chordflow-domain-model-reference.md`** §1 Harmony
in the same change: a new `IntervalSpeller` row (the interval-naming peer of `NoteSpeller`,
owns the octave vocabulary + role-keyed chord-context labels), and a note on
`VoicingDiagram` that its inline `IntervalLabel`/`GenericLabel` are gone, delegated to the speller.

---

## Summary

| | |
|---|---|
| **New** | `Domain/IntervalSpeller` — `Name(semitone)` (computed, unfolded flats vocabulary) + `Label(semitone, role)` (chord-context, role-keyed, conventional) |
| **Changed** | `VoicingDiagram` delegates spelling; drops `IntervalLabel`/`GenericLabel` |
| **Tests** | new `IntervalSpellerTests`; existing `VoicingDiagramTests` unchanged (byte-for-byte oracle) |
| **Ref** | `chordflow-domain-model-reference` §1 — add `IntervalSpeller`, note `VoicingDiagram` delegation |
| **Decided** | ship `Name` now (A); diagram tensions conventional `#9/#11` (substrate stays flats) |
