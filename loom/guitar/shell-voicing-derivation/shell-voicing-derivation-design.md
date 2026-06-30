---
type: design
id: de_01KW481X6B93Y3QPX8G2E7XMP2
title: Derive shell voicings from CAGED chords
status: done
created: 2026-06-27
updated: 2026-06-27
version: 8
idea_version: 3
tags: []
parent_id: id_01KVYQ3DY08RT6KGK50X0PPEGR
requires_load: []
---
# Derive shell voicings from CAGED chords

## Summary

Shells are **derived as their own compact voicings**, in **2 canonical forms**, verified by an **authored golden-oracle table** — *not* a reduction of the CAGED chords. Three `automatic` voicing families:

| family | what it is | derivation | shapes | oracle |
|--------|-----------|------------|--------|--------|
| `caged` | full chord | `CagedDerivation` (exists) | 5 CAGED | authored CAGED (exists) |
| `doubled-shell` (`dshell`) | chord **minus the 5th** (doubled root) — curated common set | `ShellReduction` over the derived `ChordShape` | **C only**, dom7/dim7/6/m6 | inherits the CAGED oracle (structural) |
| `shell` | compact 3-note guide-tone shell | **new `ShellDerivation`** (2 forms) | 2 (`C`=5th-root, `E`=6th-root) | **authored shell table** (12 grips) |

Settled in chat-001 (Rafa approved the 3-family / 2-form model and reusing `CagedShape` `C`/`E` for the form). This **supersedes** the earlier "pure reduction" design — see the reframing below.

## Why shells are derived, not reduced (the reframing)

The original premise was "shell = CAGED chord minus the 5th + doublings." Validated against the real engine output (the `CagedDerivationOracleTests` table), that is **false for the textbook shell**: a real shell *re-voices* the guide tones onto the **D+G strings (s4+s3)**, but the full CAGED chord voices them wherever its box puts them (e.g. C-shape **maj7** voices the maj7 on the open B string, not the G string). You cannot get the canonical shell by *filtering* the chord — you must *derive* it. The reduction is still useful, but as a different product: a fuller **chord-minus-5th** voicing (`doubled-shell`).

Rafa's authored shells reveal the structure: shells are **2 forms**, guide tones always on **s4+s3**, only the root string moving:

- **5th-string-root (`C`):** root s5; **s4 = 3rd, s3 = 7th|6th**.
- **6th-string-root (`E`):** root s6 (s5 muted); **s4 = 7th|6th, s3 = 3rd**.

## The shell golden oracle (authored fixture, root C)

The 12 grips `ShellDerivation` must reproduce (frets low-E→high-E). This is the shell's spec exactly as authored CAGED chords are the spec for `CagedDerivation`:

| | dom7 | min7 | maj7 | dim7 | 6 | m6 |
|---|---|---|---|---|---|---|
| **C** (5th-root) | `x 3 2 3 x x` | `x 3 1 3 x x` | `x 3 2 4 x x` | `x 3 1 2 x x` | `x 3 2 2 x x` | `x 3 1 2 x x` |
| **E** (6th-root) | `8 x 8 9 x x` | `8 x 8 8 x x` | `8 x 9 9 x x` | `8 x 7 8 x x` | `8 x 7 9 x x` | `8 x 7 8 x x` |

Note m6 ≡ dim7 grip (both root–♭3–[9]). `m7♭5` is also shell-eligible; its shell equals the **min7** grip (the ♭5 is the dropped fifth) — derived, validated structurally, no separate oracle row.

## The derivation (ShellDerivation)

`Derive(quality, CagedShape form /* C|E */, root, region) → ChordShape`, pure:
1. `rootString` = `C`→5, `E`→6; anchor the root at its lowest **compact** placement `R` in the region (an open-string root whose guide tones would land ~12 frets away is pushed an octave up — A maj7 C-form → `x 12 11 13`, not `x 0 11 1`). No authored frets.
2. guide intervals from `QualityFormulas`: `third` (degree 3) and `seventhOrSixth` (degree 6/7); the `fifth` (degree 5) is never voiced.
3. assign by form — `C`: (s4=third, s3=seventhOrSixth); `E`: (s4=seventhOrSixth, s3=third).
4. each guide tone's fret = the occurrence on its string **nearest `R`** (picks the right octave; this reproduces the "forward-1" maj7 and "behind-1" dim7/6/m6 placements automatically — they fall out, they are not special-cased).
5. emit a `ChordShape` (root + 2 guide strings sounded, the rest muted) → `ChordShapeVoicing.ToVoicing`.

Traced against all 12 oracle grips by hand — reproduces them exactly.

## Decisions (revised, supersede prior D1–D9)

- **D1** Three families: `caged` / `doubled-shell` (`dshell`) / `shell`.
- **D2** `shell` & `doubled-shell` apply only to **7th/6th** qualities (`Dominant7, Major7, Minor7, HalfDiminished7, Diminished7, Major6, Minor6`); `caged` covers all incl. triads.
- **D3** Identity `auto:{family}:{token}:{shape}` (4-segment, breaking). `shell` shape ∈ {`C`,`E`} (forms, reusing `CagedShape`); `caged`/`doubled-shell` shape ∈ the CAGED set.
- **D4** `doubled-shell` = `ShellReduction` over the derived `ChordShape`: mute the strings whose function is the **fifth** (via `QualityFormulas`/`ChordTones`), keep doublings; mute, never repack. Inherits the CAGED oracle.
- **D5** `shell` = `ShellDerivation` (the 2-form deriver above).
- **D6** Shell golden oracle = the 12 authored grips; the **only** authored shell data, test-only (never a runtime table). `m7♭5` derived (= min7 grip), validated structurally.
- **D7** `VoicingSource.Family` (default `caged` ⇒ no regression). `CompingResolver` dispatches per family and **falls back to `caged`** for a chord whose quality has no shell, before the source fallback chain.
- **D8** Retire `BeginnerShellStrategy`/`IVoicingStrategy`/`VoicingBook` (superseded; the shell oracle replaces the old 3-grip regression).
- **D9** `common`/`extended` classification **dropped** from scope (no consumer yet; derive it from the root string when a reader appears).

## Architecture / components

- **`ShellDerivation`** (new, `Instruments/Guitar/Caged/`) — the 2-form compact-shell deriver.
- **`ShellReduction`** (new) — `MuteFifth(ChordShape)` for `doubled-shell`.
- **`VoicingFamily`** enum (`Caged, DoubledShell, Shell`) + tokens.
- **`AutomaticVoicingId`** — 4-segment id.
- **`CagedVoicingCatalog`** — `(VoicingFamily, Quality, CagedShape)`, **64 combos**; `caged`×all (46), `doubled-shell`×(C only, dom7/dim7/6/m6 — curated, 4), `shell`×(7th/6th × {C,E}, 14).
- **`CompingResolver`** — family dispatch (`caged`→Derive · `doubled-shell`→Derive+MuteFifth · `shell`→ShellDerivation) + `Family` knob + `caged` fallback.
- **`EngineVoicingSource`** — family-qualified listing rows.

## Picking pipeline / reference-doc sync / validation / deferred

Unchanged from the prior design except: ref-sync adds `ShellDerivation` + `ShellReduction` + `VoicingFamily` to `chordflow-domain-model-reference.md`; validation's shell oracle is the 12-grip table (not `BeginnerShellStrategy`); `common`/`extended` is out. Picking pipeline (explicit override → difficulty/family filter → ranking) and the sibling-thread boundaries are as before.
