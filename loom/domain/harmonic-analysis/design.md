---
type: design
id: de_01KXQFJYNF0D10R9B5VR3JP930
title: Harmonic analysis — functional labels, secondary dominants, borrowed chords
status: done
created: 2026-07-17
updated: 2026-07-17
version: 3
idea_version: 1
tags: []
parent_id: id_01KXGQGTSHN6WAK5KZBE4G4CC9
requires_load: []
---
# Harmonic analysis — functional labels, secondary dominants, borrowed chords

## Goal

A pure **harmonic-analysis pass** in `Music/Harmony` that, given a chord (or a sequence of chords) plus a key context, labels each chord's **harmonic function** as a structured, introspectable result. It is the reasoner north star applied to *function*: the same way the Voicings Engine derives voicings from theory, this derives **roles** from theory. Consumed by [[harmonic-overlay]] (thread 3); it **subsumes** the honest-diatonic label `ChordSheetBuilder.RomanFunction` computes today (that method's own comment defers secondary-dominant/borrowing labels to "the harmonic-analysis pass").

This design log records the decisions reached in `chats/chat-001.md`.

## Settled decisions (from chat-001)

- **D1 — Pitch-based, context-free-per-chord labeling.** Input is a concrete `Chord` (root pitch class + quality) + a `Key`, never a DSL `RomanDegree`. Function is computed from `(chordRootPc − tonicPc, mode)`. A chord is labeled from key + quality *alone* (a dominant a fifth above a diatonic degree is `V/x` whether or not it resolves); actual resolution is a later *tonicization* concern. The pass is therefore an order-independent map — but the **API still accepts the whole sequence** so sequence-aware labeling is additive (D5, EX5).
- **D2 — Deterministic category precedence.** `Diatonic > SecondaryDominant > SecondaryLeadingTone > TritoneSub > Borrowed > Chromatic`. When resolution context eventually exists it breaks the TritoneSub-vs-Borrowed tie (the genuinely ambiguous `♭VII7` = borrowed vs `♭II7`-of-something case); v1 uses the fixed precedence.
- **D3 — Structured output, not rendered strings.** No glyph strings in Music (`V7/ii`, `♭II7` are a consumer/formatter concern). The result is a `Category` enum + the honest base function + an optional target degree + an optional source mode.
- **D4 — Minor in Core from day one (chat decision 4A).** The analyzer handles `Key.IsMinor` natively (the logic is symmetric; minor `iiø–V–i`, Picardy, borrowing *into* minor are core cases). It never uses the relative-major shortcut — analysis is *tonic-relative* (in A minor `Am` is `i`, not `vi`). Only the ability to *drive* minor analysis through the app UI waits on [[first-class-minor-keys]] (thread 2); the engine does not.
- **D5 — Per-position-keyed sequence API.** Input is an ordered sequence in which the key may vary per position (`IReadOnlyList<(Chord, Key)>`) plus a single-chord convenience. Multi-key is already real at the song level (`RealizedSong` sections are each keyed via `mod`/`key`), so a modulating song hands us per-region keys for free; and future resolution-based labeling is a later pass over the same held sequence — no API change.
- **D6 — Category granularity: SPLIT (chat decision).** `SecondaryDominant` (`V/x`) and `SecondaryLeadingTone` (`vii°/x`, the applied leading-tone diminished chord) are **separate categories** — more accurate and clearer for teaching/learning; the classifier picks between them by the chord's own quality. Not architecturally load-bearing (identical algorithm), but the enum names the two cases distinctly.

## Output shape

```
enum HarmonicCategory { Diatonic, SecondaryDominant, SecondaryLeadingTone, Borrowed, TritoneSub, Chromatic }

readonly record struct ChordAnalysis(
    HarmonicCategory Category,
    RomanDegree      Function,     // the honest degree+quality+accidental of THIS chord in the key
                                   //   — the field that subsumes ChordSheetBuilder.RomanFunction
    ScaleDegree?     Target,       // the tonicized degree, for SecondaryDominant / SecondaryLeadingTone / TritoneSub
    KeyMode?         SourceMode);  // the parallel mode borrowed from, for Borrowed
```

- `Function` is always populated (every chord has an honest degree). For a `Diatonic` chord that is the whole story; for the others it is the literal spelling (`A7` in C → the honest `VI7`) while `Category`/`Target` carry the *interpretation* (`SecondaryDominant`, target `ii`).
- `KeyMode` is a small `{ Major, Minor }` enum (naming the parallel mode a borrowed chord is drawn from — clearer than a bare `bool`).

## Algorithm (per chord, given its key)

1. `rootOffset = (chord.Root − key.Tonic) mod 12`.
2. **Diatonic check** — build the diatonic chord at the degree whose pitch matches `rootOffset` (`Scale.ForKey(key)` + `DiatonicChord.Build`) and compare **functional cores** (below), **tolerating triad vs 7th** (a plain `Dm` triad is still diatonic `ii` in C, matching the diatonic `Dm7`). Match ⇒ `Diatonic`, `Function` = that degree.
3. Otherwise classify by precedence (D2) — the **tonic degree (1) is excluded** from every secondary function (a tonic is never `V/IV` etc.; that is the blues ruling, below):
   - **SecondaryDominant** — a dominant-family chord whose root is a perfect fifth above a *non-tonic* diatonic degree's root ⇒ `V/that-degree` (`Target` set).
   - **SecondaryLeadingTone** — a dim / dim7 a semitone below a *non-tonic* diatonic degree's root ⇒ `vii°/that-degree` (`Target` set).
   - **TritoneSub** — a dominant a tritone from the expected `V` of a diatonic target (`♭II7` → `V`), `Target` set.
   - **Borrowed** — the chord matches a diatonic chord of the **parallel** mode (major↔minor); `SourceMode` = the parallel mode.
   - **Chromatic** — none of the above (incl. the blues `I7`/`IV7`, whose honest `Function` is still carried).
4. `Function` (honest degree, incl. its accidental) is computed for every chord regardless of category — this is the value that lets thread 3 retire the builder's inline `RomanFunction`.

## Type placement

- New in `Music/Harmony/`: **`HarmonicAnalyzer`** (static, pure), **`ChordAnalysis`** (record), **`HarmonicCategory`** + **`KeyMode`** (enums). Harmony **sink** — references only `Music.Harmony` (Scale, DiatonicChord, Chord, Key, RomanDegree, ScaleDegree, QualityFacets/ChordTones); no I/O; immutable. Frozen by the existing `MusicLayeringTests`.
- **C2 — the `RealizedSong → (Chord, Key)` adapter stays OUTSIDE Music** (a Features/consumer concern), so the analyzer never depends on Song/Realized types and stays a clean Harmony sink. The analyzer's core signature is `Analyze(IReadOnlyList<(Chord, Key)>)`; the consumer (thread 3's `ChordSheetBuilder`) walks its `RealizedSong` into that shape.

## Subsumption of `ChordSheetBuilder.RomanFunction`

The analyzer's `Function` (with `Category == Diatonic`) is **format-compatible** with today's honest Roman label. Thread 3 replaces the builder's inline `RomanFunction` with the analyzer's output — one function source, engine and sheet agree by construction. This design does **not** do that refactor (it's thread 3), but it commits the analyzer to producing a compatible honest degree.

## Validation / golden oracles

Golden tests over inline fixtures — the **progression catalog** (major-frame, parsed in-test) plus **hand-built minor fixtures** (concrete chords + `Key(minor)` — no `Transposer` needed, so thread 1 needs nothing from thread 2):

- `ii–V–I`, `I–vi–ii–V` (diatonic)
- circle of secondary dominants `III7 VI7 II7 V7 I` (V/vi → V/ii → V/V → V → I)
- borrowed `iv` (`Fm` in C), `♭VII`, `♭VI–♭VII`
- `♭II7` tritone sub, the Tadd Dameron turnaround
- `#iv°`, chromatic passing `#i°7`
- minor `iiø–V–i`, Picardy third
- **the dominant-blues stress test** (`I7 IV7 V7` all dominant) — must read `I7·IV7 (Chromatic) · V7 (Diatonic)`, **not** secondary-dominants-of-nothing.

## Non-goals (v1)

- **No key detection** — the key is an input (the Song carries it).
- **No rendering / glyphs / colour** — consumers format presentation (thread 3).
- **No tonicization / modulation spans** — grouping runs into a temporary key is later.
- **No minor-key DSL / realization / UI** — that's [[first-class-minor-keys]] (thread 2); this thread only consumes a `Key(IsMinor)` it is handed.
- **No resolution-based (sequence-aware) disambiguation** — v1 labels context-free; the sequence is accepted for the future, not yet used.

## Dependencies

- Depends on **nothing** (pitch-based; minor fixtures are hand-built). Feeds [[harmonic-overlay]] (thread 3).
- Parallel content deliverables (not blockers, and NOT in plan-001): the `description:` catalog-header field (chat decision 1A) and the major-frame progression catalog added to the default pack, tagged with a harmonic-concept + difficulty **tag vocabulary** (chat decision (a) — no new schema; use `tags`). Their content unit is separate; the minor-tonic progressions wait on thread 2.

## Resolved sub-decisions

- **Category granularity** — **split** into `SecondaryDominant` + `SecondaryLeadingTone` (chat decision: more accurate + better for teaching). The classifier picks by chord quality; the algorithm is identical either way.
- **Blues / diatonic-root dominants (chat decision: option 1 + tonic-exclusion).** The **tonic is never a secondary function** (no `I7 = V/IV`). A dominant-quality chord on a diatonic root that is neither the diatonic quality nor a functional secondary dominant / tritone-sub / borrowed (the blues `I7`, `IV7`) is **`Chromatic`**, with its honest `Function` (`I7`, `IV7`) preserved — truthful, no new category. So a dominant blues reads `I7·IV7 (Chromatic) · V7 (Diatonic)`. **Deferred (EX5 / v2):** a *blues/dominant-tonality* detector that, on seeing `I7` **and** `V7` in the sequence, folds `IV7` (and `I7`) back to diatonic (`I7·IV7·V7` all diatonic) — inherently sequence-aware, so out of v1's context-free scope; additive over the same sequence API.
- **Classify by functional core, not exact `Quality`.** Function is determined by a chord's **third + seventh (guide tones)**, not its extensions: dominant `9`/`11`/`13`/`7#9`/`7♭9` all function as a dominant 7, `maj9` as `maj7`, `m9`/`m11` as `m7`. The classifier keys on the functional core (via `QualityFacets`/`ChordTones`), so when **extended/altered qualities** are added to the domain later (they are absent from the 11-quality enum today — a future domain capability) they analyze correctly with no change. In v1 this is behaviour-neutral (base qualities only).
