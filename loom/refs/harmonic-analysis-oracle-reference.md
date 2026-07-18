---
type: reference
id: rf_01KXTXCBDA7Q6355VZBH0WCFE5
title: Harmonic Analysis Oracle
status: active
created: 2026-07-18
updated: 2026-07-18
version: 2
tags: []
parent_id: null
requires_load: []
slug: harmonic-analysis-oracle
description: "Human-reasoned golden oracle: the expected HarmonicAnalyzer (Category, Target, SourceMode) label for every chord of every seeded default-pack progression (both the major-frame and minor-home sets). The single authored source the catalog golden test asserts against."
---
# Harmonic Analysis Oracle

**Golden oracle for `HarmonicAnalyzer`.** For every seeded default-pack progression — the major-frame set (`domain/harmonic-analysis` plan-002) and the minor-home set (`domain/minor-progressions`) — this doc records the **expected** `(Category, Target, SourceMode)` label the analyzer should produce for each chord. It is **hand-reasoned from theory**, not a snapshot of analyzer output — that is what makes it an *oracle* rather than a regression capture. `HarmonicAnalyzerCatalogTests` (`IN12`) realizes each progression and asserts the engine reproduces this table exactly; a completeness guard fails if a seeded `.dsl` has no section here.

## How to read it

- Each progression is realized in a **pinned key**: the **major-frame set in C major**, the **minor-home set in A minor** (matching the `MinorProgression_RealizesToExpectedChordsInAMinor` precedent). The categories are tonic-relative, so the key choice is illustrative — it fixes the concrete `Chord` column, not the labels.
- **Category** ∈ `Diatonic`, `SecondaryDominant`, `SecondaryLeadingTone`, `Borrowed`, `TritoneSub`, `Chromatic` (the precedence order, `IN6`).
- **Target** = the tonicized scale degree (1–7) for `SecondaryDominant` / `SecondaryLeadingTone` / `TritoneSub`; `—` otherwise.
- **SourceMode** = the parallel mode a `Borrowed` chord is drawn from (`Major` / `Minor`); `—` otherwise.
- **Function** is the analyzer's honest key-relative degree (documentation only — the catalog test asserts *Category/Target/SourceMode*, not Function, so its fixed enharmonic spelling below is not a failure).

## How the catalog test reads this doc (machine contract)

A progression section is any `###` heading whose text contains the **backticked progression id** (e.g. `` `andalusian_cadence` ``). The **first pipe table** after that heading is the oracle: one data row per chord, columns `# · Degree · Chord · Function · Category · Target · SourceMode`. `Target`/`SourceMode` cell `—` parses as null. Keep the id backticked and the column order stable — the parser keys on the header names `Category`/`Target`/`SourceMode`.

---

## Major-frame set — realized in C major

### `ii_v_i` — ii-V-I

| # | Degree | Chord | Function | Category | Target | SourceMode |
|---|--------|-------|----------|----------|--------|------------|
| 1 | 2-7 | Dm7 | ii7 | Diatonic | — | — |
| 2 | 57 | G7 | V7 | Diatonic | — | — |
| 3 | 1maj7 | Cmaj7 | Imaj7 | Diatonic | — | — |

### `major_turnaround` — I-vi-ii-V

| # | Degree | Chord | Function | Category | Target | SourceMode |
|---|--------|-------|----------|----------|--------|------------|
| 1 | 1maj7 | Cmaj7 | Imaj7 | Diatonic | — | — |
| 2 | 6-7 | Am7 | vi7 | Diatonic | — | — |
| 3 | 2-7 | Dm7 | ii7 | Diatonic | — | — |
| 4 | 57 | G7 | V7 | Diatonic | — | — |

### `secondary_dominant_turnaround` — I-VI7-ii-V

| # | Degree | Chord | Function | Category | Target | SourceMode |
|---|--------|-------|----------|----------|--------|------------|
| 1 | 1maj7 | Cmaj7 | Imaj7 | Diatonic | — | — |
| 2 | 67 | A7 | VI7 | SecondaryDominant | 2 | — |
| 3 | 2-7 | Dm7 | ii7 | Diatonic | — | — |
| 4 | 57 | G7 | V7 | Diatonic | — | — |

### `circle_secondary_dominants` — III7-VI7-II7-V7-I

| # | Degree | Chord | Function | Category | Target | SourceMode |
|---|--------|-------|----------|----------|--------|------------|
| 1 | 37 | E7 | III7 | SecondaryDominant | 6 | — |
| 2 | 67 | A7 | VI7 | SecondaryDominant | 2 | — |
| 3 | 27 | D7 | II7 | SecondaryDominant | 5 | — |
| 4 | 57 | G7 | V7 | Diatonic | — | — |
| 5 | 1 | C | I | Diatonic | — | — |

### `tritone_sub_ii_v_i` — ii-♭II7-I

| # | Degree | Chord | Function | Category | Target | SourceMode |
|---|--------|-------|----------|----------|--------|------------|
| 1 | 2-7 | Dm7 | ii7 | Diatonic | — | — |
| 2 | b27 | Db7 | ♭II7 | TritoneSub | 1 | — |
| 3 | 1maj7 | Cmaj7 | Imaj7 | Diatonic | — | — |

### `tadd_dameron_turnaround` — I-♭III7-♭VImaj7-♭II7

| # | Degree | Chord | Function | Category | Target | SourceMode |
|---|--------|-------|----------|----------|--------|------------|
| 1 | 1maj7 | Cmaj7 | Imaj7 | Diatonic | — | — |
| 2 | b37 | Eb7 | ♭III7 | Chromatic | — | — |
| 3 | b6maj7 | Abmaj7 | ♭VImaj7 | Borrowed | — | Minor |
| 4 | b27 | Db7 | ♭II7 | TritoneSub | 1 | — |

> **v1 note:** the Tadd Dameron chain is a run of tritone substitutes. Under v1's *context-free* rules (`EX5`), only `♭II7→I` is recognized as `TritoneSub`; `Eb7` (`♭III7`) is a tritone sub of the applied dominant of `♭VI`, which needs resolution context, so it honestly reads **Chromatic**, and `Abmaj7` matches the parallel-minor `♭VI` so it reads **Borrowed**. A later resolution-aware pass could refine both.

### `borrowed_iv` — I-iv-I

| # | Degree | Chord | Function | Category | Target | SourceMode |
|---|--------|-------|----------|----------|--------|------------|
| 1 | 1 | C | I | Diatonic | — | — |
| 2 | 4- | Fm | iv | Borrowed | — | Minor |
| 3 | 1 | C | I | Diatonic | — | — |

### `mixolydian_bvii` — I-♭VII-IV

| # | Degree | Chord | Function | Category | Target | SourceMode |
|---|--------|-------|----------|----------|--------|------------|
| 1 | 1 | C | I | Diatonic | — | — |
| 2 | b7 | Bb | ♭VII | Borrowed | — | Minor |
| 3 | 4 | F | IV | Diatonic | — | — |

### `aeolian_cadence` — I-♭VI-♭VII-I

| # | Degree | Chord | Function | Category | Target | SourceMode |
|---|--------|-------|----------|----------|--------|------------|
| 1 | 1 | C | I | Diatonic | — | — |
| 2 | b6 | Ab | ♭VI | Borrowed | — | Minor |
| 3 | b7 | Bb | ♭VII | Borrowed | — | Minor |
| 4 | 1 | C | I | Diatonic | — | — |

### `chromatic_passing_dim` — I-#i°7-ii

| # | Degree | Chord | Function | Category | Target | SourceMode |
|---|--------|-------|----------|----------|--------|------------|
| 1 | 1 | C | I | Diatonic | — | — |
| 2 | #1dim7 | C#dim7 | ♭IIdim7 | SecondaryLeadingTone | 2 | — |
| 3 | 2-7 | Dm7 | ii7 | Diatonic | — | — |

> **Enharmonic note:** the chord is conventionally `#i°7` (leading-tone of `ii`), but the analyzer's pitch-based degree table spells root-offset 1 as `♭II`, so its honest `Function` reads `♭IIdim7`. The *interpretation* (`SecondaryLeadingTone`, target `ii`) is what carries meaning and what the test asserts.

### `12bar_blues` — dominant blues (the must-not-over-label case)

| # | Degree | Chord | Function | Category | Target | SourceMode |
|---|--------|-------|----------|----------|--------|------------|
| 1 | 17 | C7 | I7 | Chromatic | — | — |
| 2 | 17 | C7 | I7 | Chromatic | — | — |
| 3 | 17 | C7 | I7 | Chromatic | — | — |
| 4 | 17 | C7 | I7 | Chromatic | — | — |
| 5 | 47 | F7 | IV7 | Chromatic | — | — |
| 6 | 47 | F7 | IV7 | Chromatic | — | — |
| 7 | 17 | C7 | I7 | Chromatic | — | — |
| 8 | 17 | C7 | I7 | Chromatic | — | — |
| 9 | 57 | G7 | V7 | Diatonic | — | — |
| 10 | 47 | F7 | IV7 | Chromatic | — | — |
| 11 | 17 | C7 | I7 | Chromatic | — | — |
| 12 | 57 | G7 | V7 | Diatonic | — | — |

> **The blues ruling (design):** `I7` and `IV7` carry dominant colour on a diatonic root but are **not** secondary dominants (a tonic is never `V/IV`); they read **Chromatic** with their honest `Function` preserved, and only `V7` is `Diatonic`. Folding `I7 IV7 V7` back to all-diatonic is an inherently sequence-aware blues detector, deferred (`EX5`).

### `jazz_blues_turnaround` — ii-V with a I7 and V/ii

Degrees `2-7 57 17_67 2-7_57` → Dm7 G7 C7 A7 Dm7 G7.

| # | Degree | Chord | Function | Category | Target | SourceMode |
|---|--------|-------|----------|----------|--------|------------|
| 1 | 2-7 | Dm7 | ii7 | Diatonic | — | — |
| 2 | 57 | G7 | V7 | Diatonic | — | — |
| 3 | 17 | C7 | I7 | Chromatic | — | — |
| 4 | 67 | A7 | VI7 | SecondaryDominant | 2 | — |
| 5 | 2-7 | Dm7 | ii7 | Diatonic | — | — |
| 6 | 57 | G7 | V7 | Diatonic | — | — |

### `jazz_blues_standard` — standard jazz blues

Degrees `17 47 17 17 47 #4dim7 17 67 2-7 57 17_67 2-7_57` → C7 F7 C7 C7 F7 F#dim7 C7 A7 Dm7 G7 C7 A7 Dm7 G7.

| # | Degree | Chord | Function | Category | Target | SourceMode |
|---|--------|-------|----------|----------|--------|------------|
| 1 | 17 | C7 | I7 | Chromatic | — | — |
| 2 | 47 | F7 | IV7 | Chromatic | — | — |
| 3 | 17 | C7 | I7 | Chromatic | — | — |
| 4 | 17 | C7 | I7 | Chromatic | — | — |
| 5 | 47 | F7 | IV7 | Chromatic | — | — |
| 6 | #4dim7 | F#dim7 | #IVdim7 | SecondaryLeadingTone | 5 | — |
| 7 | 17 | C7 | I7 | Chromatic | — | — |
| 8 | 67 | A7 | VI7 | SecondaryDominant | 2 | — |
| 9 | 2-7 | Dm7 | ii7 | Diatonic | — | — |
| 10 | 57 | G7 | V7 | Diatonic | — | — |
| 11 | 17 | C7 | I7 | Chromatic | — | — |
| 12 | 67 | A7 | VI7 | SecondaryDominant | 2 | — |
| 13 | 2-7 | Dm7 | ii7 | Diatonic | — | — |
| 14 | 57 | G7 | V7 | Diatonic | — | — |

---

## Minor-home set — realized in A minor

### `minor_ii_v_i` — iiø7-V7-i

| # | Degree | Chord | Function | Category | Target | SourceMode |
|---|--------|-------|----------|----------|--------|------------|
| 1 | 2ø | Bm7b5 | iiø7 | Diatonic | — | — |
| 2 | 57 | E7 | V7 | Diatonic | — | — |
| 3 | 1- | Am | i | Diatonic | — | — |

> `E7` is the **harmonic-minor V7** (raised leading tone) — treated as diatonic in a minor key, not a secondary dominant.

### `andalusian_cadence` — i-♭VII-♭VI-V

| # | Degree | Chord | Function | Category | Target | SourceMode |
|---|--------|-------|----------|----------|--------|------------|
| 1 | 1- | Am | i | Diatonic | — | — |
| 2 | 7 | G | ♭VII | Diatonic | — | — |
| 3 | 6 | F | ♭VI | Diatonic | — | — |
| 4 | 5 | E | V | Diatonic | — | — |

> **The andalusian judgment (resolved):** the bare final `5` realizes to **E major** (the seed test asserts `Am G F E`), i.e. the Phrygian-dominant / harmonic-minor **major V** — so it reads **Diatonic**, not Borrowed. `♭VII` (G) and `♭VI` (F) are the natural-minor major-triad degrees, also diatonic.

### `natural_minor_i_iv_v` — i-iv-v

| # | Degree | Chord | Function | Category | Target | SourceMode |
|---|--------|-------|----------|----------|--------|------------|
| 1 | 1- | Am | i | Diatonic | — | — |
| 2 | 4- | Dm | iv | Diatonic | — | — |
| 3 | 5- | Em | v | Diatonic | — | — |

> Contrast with the andalusian: here the explicit `5-` gives the **natural-minor minor v** (Em), also diatonic — the analyzer accepts both the natural `v` and the harmonic `V`.

### `harmonic_minor_i_iv_v` — i-iv-V

| # | Degree | Chord | Function | Category | Target | SourceMode |
|---|--------|-------|----------|----------|--------|------------|
| 1 | 1- | Am | i | Diatonic | — | — |
| 2 | 4- | Dm | iv | Diatonic | — | — |
| 3 | 5 | E | V | Diatonic | — | — |

### `minor_turnaround` — i-♭VI-iiø7-V

| # | Degree | Chord | Function | Category | Target | SourceMode |
|---|--------|-------|----------|----------|--------|------------|
| 1 | 1- | Am | i | Diatonic | — | — |
| 2 | 6 | F | ♭VI | Diatonic | — | — |
| 3 | 2ø | Bm7b5 | iiø7 | Diatonic | — | — |
| 4 | 5 | E | V | Diatonic | — | — |

### `aeolian_loop` — i-♭VI-♭VII-i

| # | Degree | Chord | Function | Category | Target | SourceMode |
|---|--------|-------|----------|----------|--------|------------|
| 1 | 1- | Am | i | Diatonic | — | — |
| 2 | 6 | F | ♭VI | Diatonic | — | — |
| 3 | 7 | G | ♭VII | Diatonic | — | — |
| 4 | 1- | Am | i | Diatonic | — | — |

### `picardy_cadence` — i-iv-V-I (Picardy third)

| # | Degree | Chord | Function | Category | Target | SourceMode |
|---|--------|-------|----------|----------|--------|------------|
| 1 | 1- | Am | i | Diatonic | — | — |
| 2 | 4- | Dm | iv | Diatonic | — | — |
| 3 | 5 | E | V | Diatonic | — | — |
| 4 | 1 | A | I | Borrowed | — | Major |

> The final bare `1` realizes to **A major** (the seed test asserts `Am Dm E A`) — a major tonic in a minor key: the **Picardy third**, borrowed from the parallel **major**.

### `minor_12bar_blues` — minor blues (i7 / iv7 are diatonic)

Degrees `1-7 4-7 1-7 1-7 4-7 4-7 1-7 1-7 57 4-7 1-7 57` → Am7 Dm7 Am7 Am7 Dm7 Dm7 Am7 Am7 E7 Dm7 Am7 E7.

| # | Degree | Chord | Function | Category | Target | SourceMode |
|---|--------|-------|----------|----------|--------|------------|
| 1 | 1-7 | Am7 | i7 | Diatonic | — | — |
| 2 | 4-7 | Dm7 | iv7 | Diatonic | — | — |
| 3 | 1-7 | Am7 | i7 | Diatonic | — | — |
| 4 | 1-7 | Am7 | i7 | Diatonic | — | — |
| 5 | 4-7 | Dm7 | iv7 | Diatonic | — | — |
| 6 | 4-7 | Dm7 | iv7 | Diatonic | — | — |
| 7 | 1-7 | Am7 | i7 | Diatonic | — | — |
| 8 | 1-7 | Am7 | i7 | Diatonic | — | — |
| 9 | 57 | E7 | V7 | Diatonic | — | — |
| 10 | 4-7 | Dm7 | iv7 | Diatonic | — | — |
| 11 | 1-7 | Am7 | i7 | Diatonic | — | — |
| 12 | 57 | E7 | V7 | Diatonic | — | — |

> **Minor vs major blues:** unlike the major `12bar_blues` (where `I7`/`IV7` are Chromatic dominants), the minor blues `i7`/`iv7` are genuine **diatonic minor sevenths** (Am7, Dm7), and the `V7` is the harmonic-minor dominant — so the whole minor blues reads **all-Diatonic**. A clean demonstration that the analyzer treats minor natively rather than via a relative-major shortcut.

---

## Sync obligation

This oracle mirrors the seeded progression catalog (`Content/default-pack/progressions/*.dsl`). When a progression is **added, removed, or its chords change**, update the matching section here in the same unit of work — `HarmonicAnalyzerCatalogTests`' completeness guard fails the build if a seeded `.dsl` has no section, and its assertions fail if a row's expected label drifts from the analyzer.

## Engine output (actual — verified)

Emitted by `HarmonicAnalyzerCatalogTests.EmitActualEngineOutput_ForReview` and pasted here for independent review: the **actual** `HarmonicAnalyzer` output for every realized chord. `SeededProgression_AnalyzesToTheOracle` asserts every row below equals the expected table above — this whole section matched on the run that produced it (33/33 harmonic-analysis tests green). Headings use `####` and unbackticked ids so the oracle parser ignores this section.

#### 12bar_blues — engine (C major)

| # | Chord | Category | Target | SourceMode |
|---|-------|----------|--------|------------|
| 1 | C7 | Chromatic | — | — |
| 2 | C7 | Chromatic | — | — |
| 3 | C7 | Chromatic | — | — |
| 4 | C7 | Chromatic | — | — |
| 5 | F7 | Chromatic | — | — |
| 6 | F7 | Chromatic | — | — |
| 7 | C7 | Chromatic | — | — |
| 8 | C7 | Chromatic | — | — |
| 9 | G7 | Diatonic | — | — |
| 10 | F7 | Chromatic | — | — |
| 11 | C7 | Chromatic | — | — |
| 12 | G7 | Diatonic | — | — |

#### aeolian_cadence — engine (C major)

| # | Chord | Category | Target | SourceMode |
|---|-------|----------|--------|------------|
| 1 | C | Diatonic | — | — |
| 2 | Ab | Borrowed | — | Minor |
| 3 | Bb | Borrowed | — | Minor |
| 4 | C | Diatonic | — | — |

#### borrowed_iv — engine (C major)

| # | Chord | Category | Target | SourceMode |
|---|-------|----------|--------|------------|
| 1 | C | Diatonic | — | — |
| 2 | Fm | Borrowed | — | Minor |
| 3 | C | Diatonic | — | — |

#### chromatic_passing_dim — engine (C major)

| # | Chord | Category | Target | SourceMode |
|---|-------|----------|--------|------------|
| 1 | C | Diatonic | — | — |
| 2 | C#dim7 | SecondaryLeadingTone | 2 | — |
| 3 | Dm7 | Diatonic | — | — |

#### circle_secondary_dominants — engine (C major)

| # | Chord | Category | Target | SourceMode |
|---|-------|----------|--------|------------|
| 1 | E7 | SecondaryDominant | 6 | — |
| 2 | A7 | SecondaryDominant | 2 | — |
| 3 | D7 | SecondaryDominant | 5 | — |
| 4 | G7 | Diatonic | — | — |
| 5 | C | Diatonic | — | — |

#### ii_v_i — engine (C major)

| # | Chord | Category | Target | SourceMode |
|---|-------|----------|--------|------------|
| 1 | Dm7 | Diatonic | — | — |
| 2 | G7 | Diatonic | — | — |
| 3 | Cmaj7 | Diatonic | — | — |

#### jazz_blues_standard — engine (C major)

| # | Chord | Category | Target | SourceMode |
|---|-------|----------|--------|------------|
| 1 | C7 | Chromatic | — | — |
| 2 | F7 | Chromatic | — | — |
| 3 | C7 | Chromatic | — | — |
| 4 | C7 | Chromatic | — | — |
| 5 | F7 | Chromatic | — | — |
| 6 | F#dim7 | SecondaryLeadingTone | 5 | — |
| 7 | C7 | Chromatic | — | — |
| 8 | A7 | SecondaryDominant | 2 | — |
| 9 | Dm7 | Diatonic | — | — |
| 10 | G7 | Diatonic | — | — |
| 11 | C7 | Chromatic | — | — |
| 12 | A7 | SecondaryDominant | 2 | — |
| 13 | Dm7 | Diatonic | — | — |
| 14 | G7 | Diatonic | — | — |

#### jazz_blues_turnaround — engine (C major)

| # | Chord | Category | Target | SourceMode |
|---|-------|----------|--------|------------|
| 1 | Dm7 | Diatonic | — | — |
| 2 | G7 | Diatonic | — | — |
| 3 | C7 | Chromatic | — | — |
| 4 | A7 | SecondaryDominant | 2 | — |
| 5 | Dm7 | Diatonic | — | — |
| 6 | G7 | Diatonic | — | — |

#### major_turnaround — engine (C major)

| # | Chord | Category | Target | SourceMode |
|---|-------|----------|--------|------------|
| 1 | Cmaj7 | Diatonic | — | — |
| 2 | Am7 | Diatonic | — | — |
| 3 | Dm7 | Diatonic | — | — |
| 4 | G7 | Diatonic | — | — |

#### mixolydian_bvii — engine (C major)

| # | Chord | Category | Target | SourceMode |
|---|-------|----------|--------|------------|
| 1 | C | Diatonic | — | — |
| 2 | Bb | Borrowed | — | Minor |
| 3 | F | Diatonic | — | — |

#### secondary_dominant_turnaround — engine (C major)

| # | Chord | Category | Target | SourceMode |
|---|-------|----------|--------|------------|
| 1 | Cmaj7 | Diatonic | — | — |
| 2 | A7 | SecondaryDominant | 2 | — |
| 3 | Dm7 | Diatonic | — | — |
| 4 | G7 | Diatonic | — | — |

#### tadd_dameron_turnaround — engine (C major)

| # | Chord | Category | Target | SourceMode |
|---|-------|----------|--------|------------|
| 1 | Cmaj7 | Diatonic | — | — |
| 2 | Eb7 | Chromatic | — | — |
| 3 | Abmaj7 | Borrowed | — | Minor |
| 4 | Db7 | TritoneSub | 1 | — |

#### tritone_sub_ii_v_i — engine (C major)

| # | Chord | Category | Target | SourceMode |
|---|-------|----------|--------|------------|
| 1 | Dm7 | Diatonic | — | — |
| 2 | Db7 | TritoneSub | 1 | — |
| 3 | Cmaj7 | Diatonic | — | — |

#### aeolian_loop — engine (A minor)

| # | Chord | Category | Target | SourceMode |
|---|-------|----------|--------|------------|
| 1 | Am | Diatonic | — | — |
| 2 | F | Diatonic | — | — |
| 3 | G | Diatonic | — | — |
| 4 | Am | Diatonic | — | — |

#### andalusian_cadence — engine (A minor)

| # | Chord | Category | Target | SourceMode |
|---|-------|----------|--------|------------|
| 1 | Am | Diatonic | — | — |
| 2 | G | Diatonic | — | — |
| 3 | F | Diatonic | — | — |
| 4 | E | Diatonic | — | — |

#### harmonic_minor_i_iv_v — engine (A minor)

| # | Chord | Category | Target | SourceMode |
|---|-------|----------|--------|------------|
| 1 | Am | Diatonic | — | — |
| 2 | Dm | Diatonic | — | — |
| 3 | E | Diatonic | — | — |

#### minor_12bar_blues — engine (A minor)

| # | Chord | Category | Target | SourceMode |
|---|-------|----------|--------|------------|
| 1 | Am7 | Diatonic | — | — |
| 2 | Dm7 | Diatonic | — | — |
| 3 | Am7 | Diatonic | — | — |
| 4 | Am7 | Diatonic | — | — |
| 5 | Dm7 | Diatonic | — | — |
| 6 | Dm7 | Diatonic | — | — |
| 7 | Am7 | Diatonic | — | — |
| 8 | Am7 | Diatonic | — | — |
| 9 | E7 | Diatonic | — | — |
| 10 | Dm7 | Diatonic | — | — |
| 11 | Am7 | Diatonic | — | — |
| 12 | E7 | Diatonic | — | — |

#### minor_ii_v_i — engine (A minor)

| # | Chord | Category | Target | SourceMode |
|---|-------|----------|--------|------------|
| 1 | Bm7b5 | Diatonic | — | — |
| 2 | E7 | Diatonic | — | — |
| 3 | Am | Diatonic | — | — |

#### minor_turnaround — engine (A minor)

| # | Chord | Category | Target | SourceMode |
|---|-------|----------|--------|------------|
| 1 | Am | Diatonic | — | — |
| 2 | F | Diatonic | — | — |
| 3 | Bm7b5 | Diatonic | — | — |
| 4 | E | Diatonic | — | — |

#### natural_minor_i_iv_v — engine (A minor)

| # | Chord | Category | Target | SourceMode |
|---|-------|----------|--------|------------|
| 1 | Am | Diatonic | — | — |
| 2 | Dm | Diatonic | — | — |
| 3 | Em | Diatonic | — | — |

#### picardy_cadence — engine (A minor)

| # | Chord | Category | Target | SourceMode |
|---|-------|----------|--------|------------|
| 1 | Am | Diatonic | — | — |
| 2 | Dm | Diatonic | — | — |
| 3 | E | Diatonic | — | — |
| 4 | A | Borrowed | — | Major |
