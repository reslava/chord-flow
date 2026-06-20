---
type: design
id: de_01KVJYPHF2G4CHGNEN9ZMFPGAY
title: Chord qualities — the interval formulas — Design
status: done
created: 2026-06-20
updated: 2026-06-20
version: 2
tags: []
parent_id: id_01KV2WWAZDDC1XWFF5AGTJX197
requires_load: []
---
# Chord qualities — the interval formulas — Design

## Goal

Make the **interval formula** the authoritative form of every chord quality, and the
semitone set a **derived projection** of it. Today `QualityIntervals` hand-authors the
semitone arrays (`new[] { 0, 3, 6, 9 }`); this thread replaces that with a small authored
**formula table** in degree+accidental spelling (`"1 b3 b5 bb7"`) and computes the semitones
from it via the already-shipped [[intervals|`IntervalSpeller`]]. One authored source of truth
for "what notes a quality contains," expressed the way a musician writes it.

This is the data layer the [[caged-system]] derivation engine reads: pick a quality → its
formula → its intervals → lay them onto an [[octave-shapes|octave shape]] via the
[[interval-lattice]] → a CAGED shape.

Grounded against (verified, `chord-qualities-chat-001`): `Domain/Quality` (9 members),
`Domain/QualityIntervals` (the semitone arrays this replaces), `Domain/ChordTones` (the
classifier that reads it), `Domain/IntervalSpeller` (`ParseSet(tokens) → semitones`, accepts
`b`/`#`/`bb`), and the 34 hand-authored CAGED voicings in `Content/default-pack/voicings/`
(the downstream golden oracle).

---

## 1. The interval formulas table

The authored data — Rafa's table. The **Formula** column is the only thing stored;
**Semitones** is shown here (and in the ref) as documentation and becomes the **unit-test
oracle** (§3), never stored runtime data.

| Quality (`enum`) | Formula | Semitones (derived / oracle) |
|---|---|---|
| `Major` | `1 3 5` | 0 4 7 |
| `Minor` | `1 b3 5` | 0 3 7 |
| `Major7` | `1 3 5 7` | 0 4 7 11 |
| `Dominant7` | `1 3 5 b7` | 0 4 7 10 |
| `Minor7` | `1 b3 5 b7` | 0 3 7 10 |
| `HalfDiminished7` (m7b5) | `1 b3 b5 b7` | 0 3 6 10 |
| `Diminished` (triad) | `1 b3 b5` | 0 3 6 |
| `Diminished7` | `1 b3 b5 bb7` | 0 3 6 9 |
| `Augmented` | `1 3 #5` | 0 4 8 |

Spelling is canonical per degree (`#5` not `b6` for aug; `bb7` not `6` for dim7) — exactly
what `IntervalSpeller` round-trips. Token order is root-up, so the parsed semitones stay
root-up — the property `ChordTones` relies on to read position *i* as 1st/3rd/5th/7th.

---

## 2. The type — `Domain/QualityFormulas`

A pure static class in `ChordFlow.Domain`, no I/O, unit-tested — the authored peer that
`QualityIntervals` becomes a *projection* of. Names pair deliberately:
**`QualityFormulas`** (authored spelling) → **`QualityIntervals`** (derived semitones).

```csharp
namespace ChordFlow.Domain;

public static class QualityFormulas
{
    // The ONLY authored chord-content data: each quality's formula in degree+accidental
    // spelling. Semitones are NOT stored — derived via IntervalSpeller.ParseSet.
    private static readonly IReadOnlyDictionary<Quality, string> Table = new Dictionary<Quality, string>
    {
        [Quality.Major]           = "1 3 5",
        [Quality.Minor]           = "1 b3 5",
        [Quality.Major7]          = "1 3 5 7",
        [Quality.Dominant7]       = "1 3 5 b7",
        [Quality.Minor7]          = "1 b3 5 b7",
        [Quality.HalfDiminished7] = "1 b3 b5 b7",
        [Quality.Diminished]      = "1 b3 b5",
        [Quality.Diminished7]     = "1 b3 b5 bb7",
        [Quality.Augmented]       = "1 3 #5",
    };

    /// <summary>The authored interval formula (degree+accidental spelling) of a quality.</summary>
    public static string Formula(Quality quality);   // throws ArgumentOutOfRangeException if absent
}
```

### `QualityIntervals` becomes derived (public surface unchanged)

`QualityIntervals` stays the consumer-facing **semitone** API — `ChordTones`,
`DiatonicChord`, and `VoicingDiagram` keep calling `Intervals(q)` / `FromIntervals(set)`
untouched. Only its internals change: the hand-written `new[] { … }` arrays are **deleted**
and the lookup table is built once, at static init, by parsing the formulas:

```csharp
private static readonly IReadOnlyDictionary<Quality, int[]> Table =
    Enum.GetValues<Quality>().ToDictionary(
        q => q,
        q => IntervalSpeller.ParseSet(QualityFormulas.Formula(q)).ToArray());
```

So semitones are parsed **once** (not per call — no perf cost) yet remain a pure derivation:
there is exactly one authored value per quality, and the semitones can never drift from it.
`Intervals` / `FromIntervals` signatures and behavior are **byte-for-byte identical** — the
parsed sets equal today's hard-coded arrays (the parity oracle, §3).

### `ChordTones` classifier — unchanged

`ChordTones.Classify` keeps mapping semitone → `ChordToneFunction` by band
(`0→Root · 3/4→Third · 6/7/8→Fifth · 9/10/11→Seventh`). Deriving function from the *degree
number* instead was the appeal of a structured formula type (Option B in chat); under the
chosen **Option A** the formula is a string and the degree number isn't retained past
`ParseSet`, so the band classifier stays. No change, no regression — flagged here only so the
deferral is explicit (revisit if/when an extended-quality classifier needs degree identity).

---

## 3. Tests & oracle

**Parity oracle (byte-for-byte): existing tests stay green, unchanged.**
`QualityIntervalsTests`, `ChordTonesTests`, `DiatonicChordTests` already pin the live semitone
sets and the diatonic C-major derivation (I maj7 … vii m7b5). If they pass after the
derivation swap, the move is correct — the formulas decode to exactly the old arrays.

**New `QualityFormulasTests` — the formula layer's own golden oracle (this is where the
"Semitones" column lives, per the chat decision):**
- Assert each quality's `Formula(q)` string verbatim (pins the authored spelling).
- Assert `QualityIntervals.Intervals(q)` equals a **hand-written expected-semitone table**
  (Rafa's third column, authored independently in the test) for all 9 qualities. This is the
  cross-check that keeps the derivation honest: a formula typo, or a future `IntervalSpeller`
  change, breaks this test against a human-authored constant — without a second source of
  truth at runtime. Same pattern the intervals thread used (formula's oracle = a unit test).

---

## 4. Scope & decisions

### Decisions (settled — `chord-qualities-chat-001`)

- **Option A — formula authored as a string, parsed via `IntervalSpeller` (not a structured
  type).** Reuses the just-shipped degree+accidental authority; no parallel value type. The
  authored form *is* the spelling; semitones are the derived projection — exactly the idea's
  framing. ([[design-philosophy-durable-over-minimal]] cuts toward reuse.)
- **Formula-only stored; semitones are the unit-test oracle, never stored data.** Storing the
  semitone column next to the formula would reintroduce two sources of truth for one fact,
  free to drift — the very duplication this thread removes (ctx C4: everything derived). Its
  value as a cross-check is fully captured in the test (§3).
- **`QualityIntervals` stays the public semitone API, now derived.** Keeps the many call sites
  untouched; the hand-written arrays are deleted, not relocated.
- **`ChordTones` classifier stays semitone-band** (degree-driven classification was Option B's
  win; deferred — A doesn't need it).

### Explicitly out of scope (deferred, from the idea)

- **Extended / altered qualities** (6, 9, 11, 13, sus, alt) themselves. The formula form keeps
  the door open *additively*: a new quality = one `enum` member + one formula string, and
  `IntervalSpeller.ParseSet` already unfolds compound degrees (`"1 3 5 7 9"` → `0 4 7 11 14`).
  The deferred part is downstream support for tones above the 7th (the band classifier and
  voicing layer stop at `11` today) — not the formula data.
- **A chord-symbol parser** for arbitrary text — the DSL suffix tables stay as they are.

### End-to-end validation (downstream, not in this thread)

Through [[caged-system]], these formulas + [[octave-shapes]] + [[interval-lattice]] must
reproduce the **34 hand-authored CAGED voicings** (`Content/default-pack/voicings/`) exactly —
the golden oracle ([[interval-derivation-engine-vision]]). This thread's slice ends at
`QualityIntervals` derived from `QualityFormulas` with byte-for-byte parity.

---

## 5. Reference-doc sync (same unit of work)

This adds a `Domain/` kernel type → **update `chordflow-domain-model-reference.md`** §1
Harmony in the same change:
- New `QualityFormulas` row — the authored quality→formula table (degree+accidental strings),
  the single source of truth for chord content.
- Amend the `QualityIntervals` row — it no longer hand-authors semitone arrays; it **derives**
  them from `QualityFormulas` via `IntervalSpeller.ParseSet` (single authored source = the
  formula strings; still the public `Intervals`/`FromIntervals` semitone API).

---

## Summary

| | |
|---|---|
| **New** | `Domain/QualityFormulas` — `Formula(Quality)` → authored degree+accidental string; the only authored chord-content data |
| **Changed** | `QualityIntervals` — internal table now derived via `IntervalSpeller.ParseSet`; hand-written arrays deleted; public surface unchanged |
| **Unchanged** | `ChordTones.Classify` (semitone-band; degree-driven deferred) |
| **Tests** | new `QualityFormulasTests` (formula strings + hand-authored semitone oracle); existing `QualityIntervals`/`ChordTones`/`Diatonic` tests stay green (parity) |
| **Ref** | `chordflow-domain-model-reference` §1 — add `QualityFormulas`, amend `QualityIntervals` to "derived" |
| **Decided** | A (string formula via `IntervalSpeller`); formula-only stored, semitones as test oracle; `QualityIntervals` stays public + derived; classifier unchanged |
