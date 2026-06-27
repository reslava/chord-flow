---
type: design
id: de_01KW2G8YYBE2QE4M78H519RH2A
title: Derive CAGED 6th voicings
status: done
created: 2026-06-26
updated: 2026-06-26
version: 2
tags: []
parent_id: id_01KVYRNSY7JPFKF3TWYKJYWH6V
requires_load: []
---
# Derive CAGED 6th voicings

## Summary

Extend the CAGED **derivation engine** to cover two new qualities — **major 6** (`1 3 5 6` = `{0,4,7,9}`) and **minor 6** (`1 b3 5 6` = `{0,3,7,9}`) — and add the **E-shape voicing tweak** Rafa specified for the four "string-5-is-awkward" qualities (`m7b5`, `dim7`, `6`, `m6`): mute string 5 and let the index reach one fret behind the bass root. All grips stay engine-derived from the locked substrates; the authored grips remain the golden oracle. Designed in chat-001 (2026-06-26).

## Decisions

### D1 — `Major6`/`Minor6` are a domain precursor (mirrors dim7)

The `Quality` enum stops at `…HalfDiminished7, Diminished, Diminished7, Augmented`; neither 6th exists. So step 0 is the same precursor the dim7 work did: add `Quality.Major6` / `Quality.Minor6` to the enum and their rows to `QualityFormulas` (`"1 3 5 6"` / `"1 b3 5 6"`). `QualityIntervals` derives the semitones (`{0,4,7,9}` / `{0,3,7,9}`) automatically. Collision-safe: both sets are distinct from every existing quality, so `QualityIntervals.FromIntervals` stays unambiguous (a Major6 is the set-twin of a minor7 a 3rd away, but *root-relative* it is distinct).

### D2 — The E-shape tweak is **two coupled toggles**, not one

Rafa's grips (verified tone-by-tone, C root, string 6→1):

| quality | grip | s6 | s4 | s3 | s2 | s1 | tones |
|---|---|---|---|---|---|---|---|
| m7b5 | `8 x 8 8 7 8` | 1 | b7 | b3 | **b5** | 1 | {1 b3 b5 b7} |
| dim7 | `8 x 7 8 7 8` | 1 | bb7 | b3 | **b5** | 1 | {1 b3 b5 bb7} |
| m6 | `8 x 7 8 8 8` | 1 | 6 | b3 | 5 | 1 | {1 b3 5 6} |
| 6 | `8 x 7 9 8 8` | 1 | 6 | 3 | 5 | 1 | {1 3 5 6} |

Each grip relocates a colour tone to **fret 7 — one fret *below* the bass root (fret 8)**: the b5 onto string 2 (m7b5/dim7), the 6 onto string 4 (6/m6). The engine's reach window for an up-stacking shape (E) is forward-only `[bassFret, bassFret+3]` = `[8,11]` — *except* `Diminished7`, which already gets a behind-1 stretch-back. Hand-tracing the selector with string 5 muted:

- **dim7** already has the back-stretch → derives `8 x 7 8 7 8` from just muting string 5. ✓
- **m7b5** without the back-stretch can't reach the b5 at fret 7; the all-tones pass never voices the b5 and the grip degrades to `8 x 8 8 11 8` — **an m7, missing its defining b5**. With the back-stretch → `8 x 8 8 7 8`. ✓
- **6 / m6** put the 6 on string 4 fret 7 → need the back-stretch → derive `8 x 7 9 8 8` / `8 x 7 8 8 8`. ✓

So "skip string 5" alone is correct only for dim7. For m7b5/6/m6 it must be paired with the **behind-1 stretch-back**, or the engine produces a worse grip (and a *wrong* one for m7b5). The tweak is therefore: for `{HalfDiminished7, Diminished7, Major6, Minor6}` **in the E shape**, (1) mute string 5 and (2) grant the index's behind-1 reach.

### D3 — Mechanism (a): a small authored E-shape exception, not a derived rule

Rafa chose (a). The alternative (b) — a general "mute the interior string that only re-doubles the 5th" rule — is rejected: it does not solve the stretch-back (a separate concern), it could mis-fire on maj/min/dom7 where string 5 carries a wanted tone, and it is exactly the kind of human-hand/grip-sound judgment that resists a general rule. (a) is implemented as a small per-(quality, E-shape) condition in `CagedDerivation`.

### D4 — Scope of the tweak is **E-shape only**; broadening the stretch-back must not regress A/D

`m7b5` is also authored at the A and D shapes (`x 3 4 3 4 6`, `x x 10 11 11 11`). Granting the back-stretch globally could shift those and break their oracle. So both toggles are gated to `shape == E` (dim7 keeps its existing un-gated stretch-back, which the A/D oracle already bakes in). A/D/C/G derivations of every quality are byte-identical after this change except the two updated E grips.

### D5 — 6 / m6 derive across all five CAGED shapes; oracle is capture-after-confirm

`Major6`/`Minor6` join the **five-shape** qualities (like maj/min — full CAGED, per the caged-c-full rule; only m7b5/dim7/aug trim). The E grip is shaped by the tweak; C/A/G/D derive by normal stacking and Rafa reviews them visually on the fretboard page. m7b5/dim7 **E** oracle entries are updated in lockstep with the engine. 6/m6 are **derived-only first, then captured** into the golden oracle once visually confirmed (E grips known: `6 = 8 x 7 9 8 8`, `m6 = 8 x 7 8 8 8`; other shapes per Rafa's review) — so they gain a regression anchor rather than staying oracle-less.

### D6 — Out of scope

Progression-DSL `6`/`m6` degree suffixes (no `ProgressionParser` change — these are voicing-only here); shell-voicing derivation (`shell-voicing-derivation`); the app-source flip (`engine-derived-as-app-source`). The **voicing**-DSL parser does gain `6`/`m6` suffixes, only so the oracle fixtures can be authored.

## Affected seams

- `Music/Harmony`: `Quality`, `QualityFormulas` (→ `QualityIntervals` derived).
- `Instruments/Guitar/Caged`: `CagedDerivation` (the two-toggle E-shape exception), `CagedVoicingCatalog` (Major6/Minor6 as five-shape qualities).
- `Instruments/Guitar/Voicings`: `VoicingDslParser` (6/m6 suffixes, for fixtures).
- `Features/Voicings`: `EngineVoicingSource.DisplayNames` (new labels).
- `wwwroot/caged-chords.js`: the dogfood quality list.
- Tests: `CagedDerivationOracleTests` (update 2 E grips, add 6/m6 after capture), `CagedOracleVoicingsTests`/`OracleVoicings` fixtures, coverage/count tests.
- Ref-sync: `chordflow-domain-model-reference` (the two qualities + the E-shape derivation rule).

## Validation

- All 36 (soon 36+) oracle cells: derived == authored, including the two re-authored E grips.
- Every CAGED shape yields a no-throw, fully-spelled 6/m6 grip (coverage test).
- Dogfood: 6 and m6 render on the fretboard UI page across shapes for Rafa's visual review.
