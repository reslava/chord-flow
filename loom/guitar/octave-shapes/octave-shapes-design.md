---
type: design
id: de_01KVJ7M0EAS5PXGQ0W7T67ZPKX
title: Octave shapes — the 5 CAGED root maps (engine skeleton)
status: done
created: 2026-06-20
updated: 2026-06-20
version: 4
idea_version: 7
tags: []
parent_id: id_01KV2WVWE75SPHV91HX03J4DTG
requires_load: []
---
# Octave shapes — the 5 CAGED root maps (engine skeleton)

## 0. Verdict

**Grounded to design.** The idea is `done`; chat-001 and chat-002 settled every open question: the query is **option c** (target/zone-relative), the **partition is the only authored data** (offsets, octave zone, and boxes all *derive* from the shipped `[[interval-lattice]]`), and the **CAGED-zone envelope / fingering / used-zone / candidate-selection are NOT here** — they were folded into `[[caged-system]]` (chat-002). Grounding against live code (`Fretboard`, `FretPosition`, `CagedShape`, `IntervalLattice`) leaves only small API-shape choices (§3); recommendations inline.

---

## 1. Placement (grounded against live code)

- Lives in **`Instruments/Guitar/Geometry/OctaveShape.cs`**, next to `Fretboard.cs` / `IntervalLattice.cs`. Namespace **`ChordFlow.Instruments.Guitar`** (flat, per the `instrument-boundary` convention).
- **Static class**, pure geometry — no I/O, no UI (matches `Fretboard` / `IntervalLattice`).
- **Reuses** the existing `CagedShape` enum and `FretPosition`; consumes `IntervalLattice` / `Fretboard` + Domain `PitchClass`. Sits on the allowed `Instruments → Domain` side — the `Domain ↛ Instruments` arch guard stays green.

## 2. The only authored data — the partition

| Shape | Root strings (primary first) | Primary |
|-------|------------------------------|---------|
| C | 5, 2 | 5 |
| A | 5, 3 | 5 |
| G | 6, 3, 1 | 6 |
| E | 6, 4, 1 | 6 |
| D | 4, 2 | 4 |

(alphaTab numbering, 1 = high E … 6 = low E.) Everything else derives. The idea's offset numbers (C −2, A +2, G −3, E +2, D +3, string-1 = string-6 same fret) **demote to validation examples**, never stored — they fall out of `IntervalLattice` (proved in interval-lattice's golden test).

## 3. API surface (proposed)

- `IReadOnlyList<int> RootStrings(CagedShape shape)` — the authored partition, ordered primary-first.
- **Anchor query (option c → resolved to option (a), chat-002):** `IReadOnlyList<FretPosition> AnchorsFor(PitchClass root, CagedShape shape, int minFret, int maxFret)` — **one** shape instance: the primary is anchored at its lowest occurrence ≥ `minFret` (via `Fretboard.PositionsFor`, no second neck-walk), then each later root string `k` is placed an **ascending octave** above it at `abs = primaryAbs + k·12` (frets from `Fretboard.AbsoluteSemitone`). The octave index is essential: the naive "any root position on the string in the window" returns the **wrong octave for the D shape** (the in-window str2 unison at fret 1 instead of the +3 octave-up at fret 13). "All instances in window" (option b) is a trivial loop over (a), deferred until a consumer needs it.
- **Octave zone:** `OctaveZone Zone(PitchClass root, CagedShape shape, int minFret, int maxFret)` — `[Min,Max]` fret span of the anchors. `readonly record struct OctaveZone(int MinFret, int MaxFret)`.
- **CAGED boxes:** `IReadOnlyList<CagedBox> Boxes(CagedShape shape)` — key-independent string-set partition. `readonly record struct CagedBox(int LowString, int HighString, bool IsMain)`.

→ **D1 (anchor return + window semantics) — SETTLED (chat-002):** one instance, flat `IReadOnlyList<FretPosition>`, octave-indexed anchoring (option a).
→ **D2 (box / zone carriers) — SETTLED:** small `readonly record struct`s (`CagedBox`, `OctaveZone`).

## 4. Box algorithm (derived — no data)

Sort the shape's root strings. A **partial-below** box `(6, maxRootString)` when `maxRootString < 6`; a **main** box between each consecutive root pair; a **partial-above** box `(minRootString, 1)` when `minRootString > 1`. `IsMain` only for between-roots boxes (a complete octave). Reproduces the chat table exactly — C → `6,5 · 5,2* · 2,1`; G → `6,3* · 3,1*` (no partials); D → `6,4 · 4,2* · 2,1`.

## 5. What is NOT here (folded into `caged-system`, chat-002)

The **CAGED-zone envelope** (max hand reach), **used-zone** minimization, the **anchor-finger rule** (anchor = root's rank in the placed span → Left/Right + the major/minor flip), and **candidate-selection** (the B-string unison tie-break: whole-box minimization → worst same-string stretch → total span → closest to zone center). All content placement — octave-shapes owns only the static skeleton. Recorded here so they don't drift back in.

## 6. Validation

- **Unit tests:** the golden slice — anchors reproduce the five offsets at Key C (C −2, A +2, G −3, E +2, D +3, string-1 = string-6); octave-zone spans match (E → 8–10, C → 1–3); box partitions match the table for all five shapes.
- **Dogfood (standing rule):** a fretboard UI page showing each shape's root anchors + zone — delivered in the `ui` weave (req `EX5`); this thread provides the Core query it renders.

## 7. Ref updates (same unit of work, at implementation)

Add `OctaveShape` to the `Instruments/Guitar/Geometry/` inventory in `chordflow-domain-model-reference` + `chordflow-architecture-reference`.

## 8. Out of scope (→ other threads)

Per-string fret math (`interval-lattice`) · chord-quality placement + envelope/fingering/candidate-selection (`caged-system`) · scale/arpeggio overlays · alternate tunings · the per-quality shape-pruning policy.