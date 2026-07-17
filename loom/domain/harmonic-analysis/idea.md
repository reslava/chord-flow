---
type: idea
id: id_01KXGQGTSHN6WAK5KZBE4G4CC9
title: Harmonic analysis — functional labels, secondary dominants, borrowed chords
status: done
created: 2026-07-14
version: 1
tags: []
parent_id: null
requires_load: [rf_01KTSAPAT132QTEY5BEPRKS3MB, rf_01KTM41K36DYJ0CE44FE7TMCGH, rf_01KTSAQ6990GY3J4CZ7HPVPW6K]
---
# Harmonic analysis — functional labels, secondary dominants, borrowed chords

## What

A pure **harmonic-analysis pass** in the `ChordFlow.Music.Harmony` namespace that, given a chord (or a chord sequence) plus a **key/scale context**, labels each chord's **harmonic function**. It is an *engine capability*, not a UI feature: it returns a structured, introspectable analysis that **any render component** (ChordSheetR, ScoreR, FretR, future ones) can consume to *explain* the harmony rather than just display it.

This is the reasoner north star applied to *function*: the same way the Voicings Engine derives voicings from theory, this derives **roles** from theory.

## Why

- ChordSheetR wants to annotate chords with their function and flag non-diatonic colour (that `Fm` in the Layout-B reference is a **borrowed iv**, the `A7` in a C blues is a **secondary dominant V/ii**). v1 of the chord sheet will show only the honest diatonic degree; the *interesting* labels need this pass.
- The label must be **computed once, in Core, and shared** — every render surface asks the same engine the same question and gets the same answer. No per-component re-derivation, no UI owning music logic.
- It unlocks later reasoner overlays across the app: voice-leading hints, scale/mode suggestions, tonicization spans.

## Scope (capabilities, roughly in build order)

1. **Diatonic function** — map a chord to its degree/Roman function within a key (`I`, `ii`, `V7`, …). For diatonic chords the Nashville degree already *is* this; formalize it as an analysis result with a `Diatonic` category.
2. **Secondary dominants** — detect `V/x` and `vii°/x`: a dominant-quality chord whose root is a fifth above a diatonic target (resolving or not), labelled against that target.
3. **Borrowed / modal mixture** — chords drawn from the parallel mode (minor borrowing into major and vice-versa): `iv`, `♭VI`, `♭VII`, `♭III`, `ii°`, Picardy, etc., with the source mode recorded.
4. **Tritone substitution** — recognize a dominant a tritone from the expected `V` (e.g. `♭II7` for `V7`).
5. *(later)* **Tonicization / modulation spans** — group runs that imply a temporary key.

## Output shape (sketch — settle in design)

Per chord, a structured annotation, e.g.
`ChordAnalysis { functionLabel, category: Diatonic | SecondaryDominant | Borrowed | TritoneSub | Chromatic, target?, sourceMode? }`
returned for a whole progression as an ordered list aligned to the input chords. Immutable, no I/O, no UI, no allocation surprises — a Harmony sink like the rest of the kernel.

## Non-goals

- No rendering, no glyphs, no colour — consumers decide presentation.
- No key *detection* in v1 — the key/scale context is an input (the Song already carries it).
- Not a full Schenkerian/tonal-hierarchy analysis — pragmatic labels a practising guitarist recognizes.

## Consumers

- **ChordSheetR** (`chord-sheets` weave) — function labels (v1: diatonic only) and non-diatonic markers (v2, this pass).
- Any future render component that wants to explain harmony.

## Validation / dogfood

- Golden tests over known progressions: `ii–V–I`, a C blues with the Herb Ellis substitutions from the reference sheet (`V/ii`, `♭II` tritone sub, `#iv°`), the borrowed `iv` (`Fm`) case, Picardy third.
- Dogfood through the ChordSheetR analysis overlay once that lands — the sheet visibly labels the reference songs correctly.

## Related

- Consumed by [[chord-sheets-maker]] (its v2 analysis overlay).
- Builds on the existing `Progressions` / Nashville degree model and `chromatic-degrees` work.
