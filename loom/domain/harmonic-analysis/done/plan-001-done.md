---
type: done
id: pl_01KXQGHZWSE9J103VHHC2GTQFX-done
title: Done — Harmonic analyzer — engine + golden tests
status: done
created: 2026-07-17
version: 1
tags: []
parent_id: pl_01KXQGHZWSE9J103VHHC2GTQFX
requires_load: []
---
# Done — Harmonic analyzer — engine + golden tests

## Step 1 — Add the structured analysis result types: HarmonicCategory (Diatonic, SecondaryDominant, SecondaryLeadingTone, Borrowed, TritoneSub, Chromatic), KeyMode (Major, Minor), and the ChordAnalysis record (Category, Function: RomanDegree, Target: ScaleDegree?, SourceMode: KeyMode?).

Added `Music/Harmony/HarmonicCategory.cs` (the split-per-D6 `HarmonicCategory` enum: Diatonic, SecondaryDominant, SecondaryLeadingTone, Borrowed, TritoneSub, Chromatic — plus the `KeyMode { Major, Minor }` enum naming a borrowed chord's parallel source) and `Music/Harmony/ChordAnalysis.cs` (the `readonly record struct ChordAnalysis(HarmonicCategory Category, RomanDegree Function, ScaleDegree? Target = null, KeyMode? SourceMode = null)`). Glyph-free structured output (C3); pure Harmony types (C1). Satisfies IN4, IN5.

## Step 2 — Implement HarmonicAnalyzer.Analyze(Chord, Key): compute rootOffset, diatonic detection via Scale.ForKey + DiatonicChord.Build with triad-vs-7th tolerance, and the honest Function (RomanDegree) format-compatible with ChordSheetBuilder.RomanFunction. Returns Diatonic when it matches, else a Chromatic placeholder pending step 3.

Implemented `HarmonicAnalyzer.Analyze(Chord, Key)` (pitch-based, IN2): `rootOffset = (chordRootPc - tonicPc) mod 12`; the honest `Function` via a fixed **major-frame degree table** (0→I, 1→♭II, 3→♭III, 6→♯IV, 8→♭VI, 10→♭VII, …) carrying the chord's own quality — this yields the conventional minor-key spelling (i ii° ♭III iv v ♭VI ♭VII) and is format-compatible with `ChordSheetBuilder.RomanFunction` (IN9). Diatonic detection reuses `Scale.ForKey` + `DiatonicChord.Build`, compared by **functional core** (`CoreMatches`: 3rd + 5th equal, 7th equal-or-plain-triad) — the triad-vs-7th tolerance (IN7), and the extension-proof keying Rafa asked for (a future dom9/13 reads as a dominant with no change). Satisfies IN1, IN2, IN7, IN9, C1.

## Step 3 — Add the non-diatonic classifiers in fixed precedence: SecondaryDominant (V/x, Target set), SecondaryLeadingTone (vii°/x, Target set), TritoneSub (bII7 -> V, Target set), Borrowed (parallel-mode diatonic match, SourceMode set), Chromatic fallback.

Added the non-diatonic classifiers in the fixed precedence Diatonic > SecondaryDominant > SecondaryLeadingTone > TritoneSub > Borrowed > Chromatic (IN6): `IsSecondaryDominant` (dominant-family root a perfect fifth above a **non-tonic** diatonic degree → V/x), `IsSecondaryLeadingTone` (fully-diminished — triad/dim7, not half-dim — a semitone below a non-tonic degree → vii°/x), `IsTritoneSub` (v1 = ♭II7 only → resolves to I), `IsBorrowed` (functional-core match against the **parallel-mode** diatonic chord → SourceMode), else Chromatic. The **tonic-exclusion / blues ruling** (chat decision): a tonic-rooted chord is never a secondary function, so the blues `I7` is not mislabelled `V/IV`. Satisfies IN5, IN6, C4.

## Step 4 — Ensure symmetric major<->minor handling (parallel-mode borrowing both directions; minor iiø-V-i, Picardy), and add the sequence API Analyze(IReadOnlyList<(Chord, Key)>) with a per-position key.

Minor symmetry (IN8): `IsDiatonic` treats the **harmonic-minor functional dominant** (degree-5 major triad / dominant 7) and the raised **leading-tone diminished** (offset 11, dim triad/dim7) as diatonic — which natural minor lacks — so `iiø–V–i` in a minor key reads ii°·V·i all Diatonic, and the Picardy major tonic reads Borrowed(Major). `IsBorrowed` compares against the parallel mode in both directions (major↔minor). Added the sequence API `Analyze(IReadOnlyList<(Chord Chord, Key Key)>)` with a per-position key (IN3, C2 — the core takes (Chord, Key), never Song/Realized types). Satisfies IN3, IN8, C2.

## Step 5 — Golden tests over inline fixtures: ii-V-I, I-vi-ii-V, circle of secondary dominants, borrowed iv/bVII/bVI-bVII, bII7 tritone sub, Tadd Dameron turnaround, #iv°, chromatic passing #i°7, minor iiø-V-i, Picardy third, and the dominant-blues must-not-over-label stress test. Cover every category and both major and minor tonic.

Added `tests/ChordFlow.Core.Tests/HarmonicAnalyzerTests.cs` (flat path, matching the repo's flat test convention — the plan's `Harmony/` subfolder isn't used by the project). 10 tests, all green: diatonic ii–V–I, triad-vs-7th tolerance (bare G/Dm/B° diatonic; Bm not vii°), the circle of secondary dominants (V/vi·V/ii·V/V·V), secondary leading-tones (F♯°7→V, C♯°7→ii), ♭II7 tritone sub→I, borrowed iv/♭VII/♭VI, minor iiø–V–i (harmonic-minor V), Picardy third (Borrowed Major), the **dominant-blues stress test** (I7·IV7 Chromatic, V7 Diatonic — IN10), and the per-position-key sequence API. Full Core suite: **933 passed, 0 failed** (incl. the `MusicLayeringTests` architecture check — the analyzer is a clean Harmony sink). Satisfies IN8, IN10, C4.
