---
type: design
id: de_01KVGJ0P3FC7P36EQ0ZN6FJGEB
title: Interval lattice — fretboard interval positions (guitar projection of intervals)
status: done
created: 2026-06-19
updated: 2026-06-19
version: 5
tags: []
parent_id: id_01KVDEEY1959RD07H63R5PFMVZ
requires_load: []
---
# Interval lattice — fretboard interval positions (guitar projection of intervals)

## 0. Verdict

**Grounded to design.** The big decisions are settled in chat-001: the lattice is the **base guitar primitive** (octave-shapes is its special case), it **consumes `IntervalSpeller.Name`** rather than re-authoring the vocabulary, it ships **both label views** over one canonical signed distance, the query is **on-demand** (no "2 octaves L/R" enumeration), and it **no longer depends on octave-shapes**. Grounding against the live code (`Fretboard`, `FretPosition`, `IntervalSpeller`) surfaces three API-shape choices — collected in §6 **Decisions to confirm**. I recommend a default for each; the req stays **draft** until you nod, then I lock it and plan.

---

## 1. Placement (grounded against live code)

- Lives in **`Instruments/Guitar/Geometry/IntervalLattice.cs`**, next to `Fretboard.cs`. Namespace **`ChordFlow.Instruments.Guitar`** (flat — sub-folders are organization only, per the `instrument-boundary` convention).
- **Pure geometry** — no I/O, no UI (matches `Fretboard`).
- Consumes Domain (`IntervalSpeller`, `PitchClass`) + guitar geometry (`FretPosition`, `Fretboard`). The arch test allows `Instruments → Domain` and forbids `Domain → Instruments`; the lattice sits on the allowed side.
- Shape: a **static class** `IntervalLattice`, mirroring `Fretboard`'s static-geometry style.

## 2. The tuning — single-source the absolute coordinate

Standard tuning as an **octave-preserving cumulative semitone offset from low E**, indexed by alphaTab string number (1 = high E … 6 = low E), index 0 unused like `Fretboard.OpenPitchClass`:

```
StringBase = { _, 24, 19, 15, 10, 5, 0 }   // [1]=high E … [6]=low E
Absolute(string, fret) = StringBase[string] + fret
```

This is the **octave-preserving twin** of `Fretboard.OpenPitchClass`: for every position, `(4 + Absolute) % 12 == Fretboard.PitchClassAt(string, fret).Value` (4 = PC of open low E). It encodes the one irregularity — string 3→2 = 4 semitones (the B string) — in the +4 step (`15 → 19`); no code may assume a uniform +5.

→ **Decision D1 (tuning source) — SETTLED: single-source, base lives in `Fretboard`.** Today there is only one tuning source, `Fretboard.OpenPitchClass` (mod-12). The lattice needs the octave-preserving form, which carries strictly more info (`OpenPitchClass[s] = (4 + base[s]) % 12`; the reverse can't recover the octave). So the **absolute base becomes the single source of truth, authored in `Fretboard`** (e.g. `StringSemitoneBase` + `AbsoluteSemitone(string, fret)`), and `PitchClassAt`/`OpenPitchClass` are *derived* from it. `IntervalLattice` authors no tuning — it consumes `Fretboard`'s absolute coordinate. (`IntervalLattice.Absolute` is a thin delegate.)

## 3. API surface (proposed)

**Core (canonical integers):**
- `int Absolute(FretPosition pos)` — the octave-preserving semitone coordinate.
- `int Distance(FretPosition origin, FretPosition target)` — `Absolute(target) − Absolute(origin)`, **signed** (the canonical value; everything else is a view).

**Labels — two views over the canonical distance, both via `IntervalSpeller.Name` (no re-authored table):**
- `string PitchClassLabel(int distance)` → `Name(((d % 12) + 12) % 12)` → `1…7` (direction-free fretboard label).
- `LatticeInterval Describe(int distance)` → unfolded + octave + direction, for scales/arpeggios and the dogfood UI.
- `readonly record struct LatticeInterval(int Semitones, string Label, int Octaves, int Direction)` — `Label = Name(|sem|)` (`8/9/11/15…`), `Octaves = |sem|/12`, `Direction = sign(sem)`.

**The consumer query (the idea's stated query — what `caged-system` calls):**
- `IReadOnlyList<FretPosition> PositionsOfInterval(FretPosition root, int semitones, int minFret, int maxFret)` — every position in the fret window whose `Distance` from `root` ≡ `semitones` (mod 12). "Where does interval D sit near this root," **windowed** — composes with octave-shapes' option-c and caged zone placement.
- `LatticeInterval LabelAt(FretPosition root, FretPosition target)` — convenience for the dogfood UI (light up every fret's interval vs. a chosen root).

→ **Decision D2 (`PositionsOfInterval` match):** **pitch-class match within the window** (all octaves of the degree in range) — recommend — vs. exact-octave match. Pitch-class matches how a chord's tones get placed in a zone.

→ **Decision D3 (`LatticeInterval` now or later):** ship the rich record now (recommend — cheap, the dogfood UI will want it) vs. start with raw `int` + the two label helpers and add the record when the UI needs it.

## 4. Relationship to `Fretboard` (reuse, no duplication)

`Fretboard` stays the **pitch-class** geometry (`PositionsFor`, `PitchClassAt`); `IntervalLattice` adds the **position-relative, octave-aware** interval layer. `PositionsOfInterval(root, semitones, …)` is implemented **on top of** `Fretboard.PositionsFor(rootPc + semitones)` + a window filter — not a second neck-walk. The lattice's distinct contribution is the *signed, octave-preserving* distance that `Fretboard`'s mod-12 view can't express.

## 5. Domain touch — none required (confirmed)

`IntervalSpeller.Name` already **is** the vocabulary the idea asks for (`0→"1" … 12→"8"`, unfolding to `9/11/15…`). No domain change. The only ref-sync action is a one-line note (below) so nobody re-authors the table guitar-side. The reverse `label → semitone` parser and a first-class `Interval` type stay out of scope (no consumer).

## 6. Decisions to confirm (await go)

| # | Decision | Recommendation |
|---|----------|----------------|
| **D1** | Tuning source | ✅ Settled — single-source; absolute base authored in `Fretboard`, `OpenPitchClass`/`PitchClassAt` derived from it; lattice consumes it |
| **D2** | `PositionsOfInterval` match | ✅ Settled — pitch-class + fret window |
| **D3** | `LatticeInterval` record | ✅ Settled — ship it now |

## 7. Ref updates (same unit of work, at implementation)

- **`chordflow-domain-model-reference`** — one line: `IntervalSpeller.Name` is the vocabulary the guitar `IntervalLattice` projects.
- **`chordflow-architecture-reference` / domain-model** — add `IntervalLattice` to the `Instruments/Guitar/Geometry/` inventory.

## 8. Validation

- **Unit tests:** absolute-coordinate ↔ `Fretboard` consistency for every (string, fret); signed `Distance` correctness **including across the B string** (3→2 = 4, not 5); both label views; and the **first golden check** — `PositionsOfInterval` reproduces the five octave-shape offsets (the unison/octave special case: C −2, A +2, G −3, E +2, D +3, string-1 = string-6).
- **Dogfood (standing rule):** a fretboard UI page lighting up every interval around a chosen root, built on the `fretboard-render-component`.

## 9. Out of scope (→ later threads)

Reverse `label → semitone` parser / first-class `Interval` type · alternate tunings · scale/arpeggio overlays · the octave-shapes CAGED partition (consumes this) · chord-quality placement (`caged-system`).