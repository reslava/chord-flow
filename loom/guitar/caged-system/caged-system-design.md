---
type: design
id: de_01KVK5JEFP67KM8213ZPZGGSSC
title: CAGED system — the derivation engine (subsumes authored voicings)
status: done
created: 2026-06-20
updated: 2026-06-21
version: 3
tags: []
parent_id: id_01KV2WWWZ6PYT4Y4VCT4A5KD8N
requires_load: []
---
# CAGED system — the derivation engine (subsumes authored voicings)

Synthesizes the idea + the four locked substrates ([[intervals]], [[interval-lattice]],
[[octave-shapes]], [[chord-qualities]]) and the four grounding decisions resolved in
`chats/caged-system-chat-001.md` (2026-06-20). The selection algorithm was then refined to
its **locked T4 form** across `chats/caged-system-chat-002.md` (2026-06-21) — see §9. See
[[chordflow-domain-model-reference]] / [[chordflow-architecture-reference]] for placement.

## 1. Goal

One function, computed not authored:

```
derive(quality, shape, root, neckRegion) → ChordShape
```

where `ChordShape` carries, per string, a fret (or muted) **plus** the derived **anchor
finger** — and a flag for whether the realized box is **main** (2 roots) or **partial**
(1 root). This subsumes the authored voicings: at root C, each authored (quality, shape)
must reproduce exactly (frets) and its anchor finger must match (the second oracle).
**Status: the frets oracle is green at 36/36** (§6, §9).

## 2. Where it lives

`ChordFlow.Core`, guitar code area `Instruments/Guitar/Caged/` (alongside the
interval-lattice and octave-shape skeleton, per [[instrument-boundary]]). Pure, no UI/host
refs. Consumes Domain (`PitchClass`, the [[chord-qualities]] formula layer /
`QualityIntervals`) and the guitar geometry (`Fretboard`, the [[interval-lattice]]
coordinate primitive, the [[octave-shapes]] anchor query). Dependency direction stays
Domain ← Instruments/Guitar.

## 3. Inputs (all from locked substrates — nothing authored here)

| Input | Source | Shape |
|-------|--------|-------|
| quality formula | [[chord-qualities]] → `QualityIntervals` | degrees+accidentals → semitone set, e.g. maj `{0,4,7}` |
| CAGED partition | [[octave-shapes]] | shape → ordered root strings + primary string |
| anchor query | [[octave-shapes]] | `(root pc, shape, neckRegion) → octave anchors (string,fret)` + octave zone `[min,max]` |
| interval positions | [[interval-lattice]] | `abs(string,fret)=STRING_OFFSET[string]+fret`; `distance(origin,target)` |
| reach table | **this thread** (one global ergonomic table) | finger → `(behind, ahead)` reach in frets |

## 4. Algorithm (locked — the **bass-up greedy stacker**, T4)

The original design proposed a *whole-box joint minimization*; experiment (§9) showed a
**bass-up greedy stack** is cleaner, less brute-force, and reproduces the pack exactly. The
realized algorithm:

1. **Place anchors.** Query [[octave-shapes]] for the root octave anchors + octave zone in
   the region. The **bass root** = the lowest-pitch root string; mute everything below it.
2. **Stacking direction.** Is the bass root the lowest octave anchor (index-anchored — box
   stacks **up**) or the highest (pinky-anchored — box stacks **down**)? Derived, not authored.
3. **Reach window.** From the bass root, extend only in the stacking direction by the anchor
   finger's reach (index `+ahead`, pinky `−behind`), clamped so the grip stays within the
   **width-4 cap** (§5). For up-stacked **dim7** only, the index's **behind-1** adds one
   "stretch-back" fret below the bass root.
4. **Enumerate candidates.** Per played string (bass→treble), every quality tone whose
   [[interval-lattice]] position lands on that string inside the window. The bass string is
   filtered to the root (root-position). The str3→2 = 4-semitone gap can put one interval on
   two strings — both become candidates.
5. **Stack, bass → treble** (`CandidateSelector`):
   - **Uncovered first** — pick the highest-[weight](§5) chord tone *not yet voiced* (the
     stretch-back fret may voice an uncovered tone only).
   - **Fill** (all tones voiced) — this string only doubles: prefer the most **compact**
     double (least stretch from the placed box); a **root on a root string** beats a tighter
     non-root double (preserves the CAGED skeleton); the stretch-back fret may not double.
   - **Width-4 cap** gates every pick (the realized grip spans ≤ 4 frets).
6. **Anchor finger.** `AnchorFinger.Derive` from the root's rank in the realized box (lowest
   fret → index, highest → pinky, interior → middle/ring).
7. **Box kind.** Main box (2 roots) shows all the quality's tones; partial box (1 root) the
   subset — the derived usable-subset/chunk signal (IN5). *(Treble-mute trim still deferred.)*
8. **Emit.** Per string: fret or muted; plus the anchor finger and the box kind.

## 5. Tone weights, the reach table, and the width cap

- **Tone weights** (`ChordToneWeight`) for the *uncovered* pick and the fill tiebreak:
  root 100 > 3rd 70 > 7th (bb7/b7/7 = semitone 9/10/11) 50 > 5th 30; tensions 0 (kept once
  by the all-tones pass, never doubled for a bonus).
- **Reach table** (`HandReach`, one global table, Rafa's values, chat-001):
  ```
  index  : behind 1, ahead 3
  middle : behind 1, ahead 1
  ring   : behind 1, ahead 1   (placeholder — no shape anchors on the ring yet)
  pinky  : behind 4, ahead 0
  ```
- **Width-4 cap** (chat-002): a chord grip spans at most **4 frets** (`MaxChordWidth`, the
  4-finger hand), enforced on the *realized* grip in `CandidateSelector`. It supersedes the
  pinky's behind-4 reach for chords (the fuller C/G stretch is a diagram/chunk concern). The
  reach table stays the full envelope for scales/arpeggios.
- **behind-1, dim7 only** (chat-002): dim7 is the one fully-symmetric quality whose nearest
  7th lands a fret *below* the bass root, so up-stacked dim7 gets the index's behind-1 reach,
  usable only to grab an uncovered tone. A mild [[C1]] tension (quality-scoped), justified by
  the symmetry, not a per-shape fret table.

## 6. Validation — golden oracles

- **Frets oracle (IN6/C5) — GREEN at 36/36.** `CagedDerivationOracleTests` derives every
  authored (quality, shape) at C and asserts fret-equality against the pack. Where the engine
  produced a better/more-standard grip, the **authored pack was revised to the engine** (the
  "derive, don't author" call): min·C, m7·C, dom7·C, dom7·G, m7·G, m7b5·A/E, dim7·A/E,
  aug·A/E/D were updated, and aug·C/G were added (aug now ships all 5 CAGED shapes → **36
  voicings**).
- **Anchor-finger oracle (IN7) — step 6, in progress.** Annotate each authored voicing with
  one anchor-finger token and assert the derived anchor matches. Anchor only (fingering is
  non-unique). The engine's core rule, made falsifiable.
- **Dogfood (IN8).** A fretboard UI page renders the derived shape (frets + anchor + box kind)
  on the [[fretboard-render-component]].

## 7. Boundaries

- Engine **complements** the authored pipeline (generate → optionally persist as authored);
  it does not delete the DSL/pack path.
- Out: scales & arpeggios overlays (same skeleton, next), extended/altered qualities beyond
  the [[chord-qualities]] table, alternate tunings.

## 8. Still open / deferred

- **Treble-mute / partial-box trim** — the engine voices the fuller box; the author's compact
  muting (the dim7 treble strings before they were adopted) is the IN5 chunk signal, deferred.
- **Barre & 4-finger playability scoring** — builds on the anchor finger (step 6).
- The `neckRegion` convention at C: region containing the authored frets (lowest occurrence on
  the primary string).

## 9. Evolution — the tries log (chat-002, 2026-06-21)

The selection rule was tuned empirically against the pack; each try is tagged for reference:

| tag | rules (delta) | vs pack |
|-----|---------------|---------|
| T0 | bass-up tone stacker (fill by pure weight) | 17/34 |
| T1 | + compactness wins after all tones voiced (fill tiebreak) | 21/34 |
| T2 | + max width 4 + 7th-ins | 18/34 — ✗ abandoned (7th-ins over-reaches for the b7) |
| T3 | T1 + max width 4 | 33/36 |
| **T4** | T3 + roots prefer root-strings + behind-1 (dim7 only) | **36/36 — locked** |

(34 → 36 voicings once aug gained its full CAGED set.) The locked rule-set is §4 steps 5 +
§5's width cap, weights, and the dim7 behind-1.