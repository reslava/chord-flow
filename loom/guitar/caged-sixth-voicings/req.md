---
type: req
id: rq_01KW2G9V1TYQB3PVKYAEFE5ZFG
title: Derive CAGED 6th voicings — Requirements
status: locked
created: 2026-06-26
updated: 2026-06-27
version: 2
tags: []
parent_id: id_01KVYRNSY7JPFKF3TWYKJYWH6V
requires_load: []
---
# Derive CAGED 6th voicings — Requirements

### ✅ Included

- `IN1` Add `Quality.Major6` and `Quality.Minor6` to the enum, plus their `QualityFormulas` rows `"1 3 5 6"` / `"1 b3 5 6"`. Mirrors the `Diminished7` precursor pattern.
- `IN2` `QualityIntervals` derives `{0,4,7,9}` / `{0,3,7,9}` from those formulas (no separately-authored semitones); `FromIntervals` stays unambiguous (both sets distinct from every existing quality).
- `IN3` `CagedDerivation` gains a single E-shape exception for the set `{HalfDiminished7, Diminished7, Major6, Minor6}`: when `shape == E`, (1) **mute string 5** (it is left out of candidate building) and (2) grant the **index's behind-1 stretch-back** (window low edge = `bassFret − 1`, the stretch-back fret may only voice an uncovered tone). `Diminished7` keeps its existing un-gated stretch-back.
- `IN4` The m7b5 and dim7 **E** golden-oracle grips are updated in lockstep to `8 x 8 8 7 8` and `8 x 7 8 7 8`; `CagedDerivationOracleTests.Authored` and the `fixtures/caged-oracle/*.dsl` files are updated; derived == authored for all oracle cells.
- `IN5` `Major6`/`Minor6` join `CagedVoicingCatalog` as **five-shape** qualities (all of C/A/G/E/D, like maj/min); the coverage test verifies every shape derives a no-throw, fully-spelled grip across all 12 roots within the fret window.
- `IN6` `VoicingDslParser` accepts the quality suffixes `6` → `Major6` and `m6`/`-6` → `Minor6` (so the oracle fixtures can be authored).
- `IN7` `EngineVoicingSource.DisplayNames` and the `caged-chords.js` `QUALITIES` list gain `Major6` ("6") and `Minor6` ("m6") so the families list and render on the fretboard dogfood page.
- `IN8` **Capture-after-confirm**: once Rafa visually confirms the derived 6/m6 grips, they are captured into the golden oracle (E grips `6 = 8 x 7 9 8 8`, `m6 = 8 x 7 8 8 8`; other shapes per his review) — new `fixtures/caged-oracle/*.dsl`, added `CagedDerivationOracleTests` rows, bumped fixture/coverage counts.
- `IN9` Ref-sync `chordflow-domain-model-reference.md` in the same unit of work: the two new qualities and the E-shape skip-string-5 + behind-1 stretch-back derivation rule.

> Added at the chat-001 dogfood review (2026-06-27), after the engine work above was confirmed:

- `IN10` **Quality-resolved chord-tone labels.** Semitone 9 is enharmonically ambiguous (the `bb7` of `Diminished7` vs the `6` of `Major6`/`Minor6`); chord-tone function and interval label must resolve it **by quality**, not by a semitone band. Classify each chord tone from its `QualityFormulas` **degree** (degree 6 → a new `ChordToneFunction.Sixth`, degree 7 → `Seventh`), so the derived label reads `6` for 6/m6 and `bb7` for dim7. `IntervalSpeller.Label` maps a `Sixth` role → `"6"`; the `fretboard-render-component.js` palette + legend gain a `sixth` colour (amber `#f59e0b`). `Diminished7` and every existing quality are byte-identical (dim7 still reads `bb7`).
- `IN11` **E-shape behind-1 anchor finger.** When an E-shape grip actually reaches the behind-1 stretch-back fret (its low edge), the index is committed to that fret, so the derived `AnchorFinger` is the **middle** finger (root one fret above the stretch). Gated to the E-shape behind-1 case: A/D `Diminished7`, which also stretch back, keep their `Index` anchor. The m7b5 E and dim7 E fixtures carry `anchor:m`.

### ❌ Excluded

- `EX1` Progression-DSL `6`/`m6` chord degrees — no `ProgressionParser` change; these are voicing-only here.
- `EX2` Shell-voicing derivation — that is `shell-voicing-derivation`.
- `EX3` The engine-derived-as-app-source flip — that is `engine-derived-as-app-source`.
- `EX4` A `BeginnerShellStrategy` 6/m6 arm — not needed (6/m6 never reach a progression in this thread).

### ⛓ Constraints

- `C1` Both toggles are **E-shape-gated**; C/A/G/D derivations of every quality are byte-identical after the change, so the m7b5 A/D oracle grips do not regress.
- `C2` Grips stay **engine-derived from the locked substrates** — the exception is a per-(quality, shape) condition in the pipeline, not a reintroduced authored fret table.
- `C3` `QualityFormulas` remains the **only authored chord-content data**; `QualityIntervals` is derived, never stored alongside.
- `C4` `Major6`/`Minor6` are **five-shape** qualities (full CAGED); only m7b5/dim7/aug trim to E/A/D — the caged-c-full rule.
- `C5` The 6/m6 oracle is **capture-after-visual-confirm**, never pre-authored ahead of the dogfood check.
- `C6` Chord-tone **function is derived from `QualityFormulas`** (the degree spelling), never a separate per-quality role table — the same single-source-of-truth rule as C3, extended from intervals to roles.
