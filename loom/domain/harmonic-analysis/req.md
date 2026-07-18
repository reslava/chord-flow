---
type: req
id: rq_01KXQFKNWPC5HX4189JVYYF7RN
title: Harmonic analysis — functional labels, secondary dominants, borrowed chords — Requirements
status: locked
created: 2026-07-17
updated: 2026-07-18
version: 3
design_version: 3
tags: []
parent_id: de_01KXQFJYNF0D10R9B5VR3JP930
requires_load: []
---
# Harmonic analysis — functional labels, secondary dominants, borrowed chords — Requirements

### ✅ Included

- `IN1` A pure **`HarmonicAnalyzer`** in `Music/Harmony` — a Harmony sink (no I/O, immutable, unit-tested) — that labels a chord's harmonic function given a key context.
- `IN2` **Pitch-based input**: a concrete `Chord` (root pitch class + quality) + a `Key`; function computed from `(chordRootPc − tonicPc, mode)`, never from a DSL `RomanDegree`.
- `IN3` **Sequence API with a per-position key**: `Analyze(IReadOnlyList<(Chord, Key)>) → IReadOnlyList<ChordAnalysis>`, plus a single-chord convenience overload. The key may vary per position (accommodates modulation / multi-key).
- `IN4` **Structured output** `ChordAnalysis`: a `Category` + the honest base `Function` (degree+quality+accidental of the chord in the key) + an optional `Target` degree + an optional `SourceMode`. No rendered glyph strings.
- `IN5` **Categories** (split per chat decision): `Diatonic`, `SecondaryDominant` (`V/x`), `SecondaryLeadingTone` (`vii°/x` — the applied leading-tone diminished chord), `Borrowed` (modal mixture), `TritoneSub`, `Chromatic`.
- `IN6` **Deterministic category precedence**: `Diatonic > SecondaryDominant > SecondaryLeadingTone > TritoneSub > Borrowed > Chromatic`.
- `IN7` **Diatonic detection tolerates triad vs 7th** forms of the diatonic chord (a plain `Dm` triad is still diatonic `ii` in C).
- `IN8` Handles **both major and minor tonic** (`Key.IsMinor`) natively in Core — incl. minor `iiø–V–i`, Picardy third, and borrowing into minor — with no relative-major shortcut.
- `IN9` The `Diatonic`-category `Function` output is **format-compatible with** the honest Roman label `ChordSheetBuilder.RomanFunction` produces today (so thread 3 can retire the inline method — one function source).
- `IN10` **Golden-oracle tests** over the seeded progression catalog + hand-built minor fixtures, including the **dominant-blues must-not-over-label** case (`I7 IV7 V7` all dominant read as the blues idiom, not secondary dominants).
- `IN11` **Authored golden-oracle reference doc** — a `loom/refs/` reference listing, per seeded default-pack progression across **both** the major-frame set (plan-002) and the minor-home set (`domain/minor-progressions`), the expected `(Category, Target/SourceMode)` label sequence. It is the **human-reasoned** oracle (derived from theory, not a snapshot of analyzer output) that the catalog test asserts against, and it is the single authored source those expectations come from.
- `IN12` **Catalog-driven golden test** — a test that realizes each seeded progression into concrete `(Chord, Key)` via `Transposer` (the major-frame set in a major key, the minor-home set in a minor key) and asserts `HarmonicAnalyzer` reproduces the `IN11` oracle sequence **exactly**, with a completeness guard so every seeded progression `.dsl` has an oracle entry. Completes the "seeded progression catalog" half of `IN10`, now unblocked by the existence of the minor set.

### ❌ Excluded

- `EX1` **No key detection** — the key/scale context is an input.
- `EX2` **No rendering, glyphs, or colour** — consumers own presentation.
- `EX3` **No tonicization / modulation spans** (grouping runs into a temporary key) in v1.
- `EX4` **No minor-key DSL / realization / UI / spelling** work — that is `domain/first-class-minor-keys` (thread 2); this thread only consumes a `Key(IsMinor)` it is handed.
- `EX5` **No resolution-based (sequence-aware) disambiguation** in v1 — labeling is context-free per chord; the sequence is accepted for the future, not yet used.
- `EX6` **No chord-sheet overlay / consumer wiring** — that is `chord-sheets/harmonic-overlay` (thread 3).
- `EX7` **No `IN9` subsumption verification / `ChordSheetBuilder.RomanFunction` retirement** in this thread — asserting the analyzer's `Function` equals the builder's inline label, and deleting that method, is done in thread 3 where the retirement lands.

### ⛓ Constraints

- `C1` **Pure Harmony sink**: references only `Music.Harmony`; no I/O; immutable records; frozen by `MusicLayeringTests`.
- `C2` The **`RealizedSong → (Chord, Key)` adapter stays outside `Music`** (a Features/consumer concern) so the analyzer never depends on Song/Realized types. The `IN12` catalog adapter is a **test-side** helper, which satisfies this by construction.
- `C3` **Structured output only** — no glyph strings (per `EX2`).
- `C4` **Deterministic, order-independent per-chord labeling** in v1 (context-free, per `IN6` / `EX5`).