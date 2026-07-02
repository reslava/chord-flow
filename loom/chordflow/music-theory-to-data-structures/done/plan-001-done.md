---
type: done
id: pl_01KTM0Z0RPTP75F7CWBCZC8AZ1-done
title: Done — Music-Theory Domain Model — Implementation Plan
status: done
created: "2026-06-08T00:00:00.000Z"
version: 9
tags: []
parent_id: pl_01KTM0Z0RPTP75F7CWBCZC8AZ1
requires_load: []
---
# Done — Music-Theory Domain Model — Implementation Plan

## Step 1 — Quality interval sets + ChordTone: back the Quality enum with the 8 v1 interval sets (C5) via an interval table; add a chord-relative ChordTone/interval type and a ChordTones(Chord) generator. Pure, additive. Unit-test chord-tone spelling for each quality.

**Quality interval sets + ChordTone** (additive, MVP stays green).

Files:
- `Domain/Quality.cs` — expanded enum from {Major, Minor, Dominant7} to the 8 v1 qualities (C5): added `Major7`, `Minor7`, `HalfDiminished7` (m7b5), `Diminished`, `Augmented`. Each value xml-doc'd with its interval set.
- `Domain/QualityIntervals.cs` (new) — single source of truth: `Intervals(Quality) → IReadOnlyList<int>` semitone table backing all 8 qualities, root-up ordered (1st/3rd/5th/7th). Throws for unmapped qualities.
- `Domain/ChordTone.cs` (new) — `ChordTone(int Interval, ChordToneFunction Function)` readonly record struct, chord-root-relative (distinct from key-relative `RomanDegree`). `ChordToneFunction { Root, Third, Fifth, Seventh }`. `PitchClassFor(root)` resolves to a concrete `PitchClass` only when a root is supplied (spelling stays deferred, C4).
- `Domain/ChordTones.cs` (new) — `ChordTones.Of(chord)` generator producing tagged tones from the interval set; `Classify(interval)` maps 0→Root, 3/4→Third, 6/7/8→Fifth, 10/11→Seventh (unambiguous for the v1 set). Also `PitchClassesOf(chord)` convenience. This is the Theme-A↔Theme-B bridge: "the b7 of G7" = root+10, computed not stored.

Decisions:
- Named m7b5 `HalfDiminished7` for clarity; existing names Major/Minor/Dominant7 kept so no downstream breakage.
- Function classified from interval rather than stored, keeping the interval table the lone source.

Tests: `tests/ChordTonesTests.cs` (new) — per-quality interval-set spelling (all 8), root-relative transposition (G7→G B D F), guide-tone (3 & 7) function tagging, and m7b5 diminished-5th classification. Full suite 50 passed (was 39), build clean.

## Step 2 — Scale + diatonic generator: introduce Scale(Tonic, Intervals); extract MajorOffsets/NaturalMinorOffsets from Transposer into Scale; refactor Transposer.Realize to consume a Scale; add BuildDiatonicChord(Scale, ScaleDegree). Unit-test C-major diatonic set (I Cmaj7 .. vii Bm7b5) and existing transposition tests still green.

**Scale + diatonic generator** (additive; existing transposition tests stay green).

Files:
- `Domain/Scale.cs` (new) — `Scale(PitchClass Tonic, IReadOnlyList<int> Intervals)` record. Holds `MajorIntervals {0,2,4,5,7,9,11}` and `NaturalMinorIntervals {0,2,3,5,7,8,10}` (extracted from Transposer). Factories `Major(tonic)`, `NaturalMinor(tonic)`, `ForKey(key)`; `Count`; `DegreePitchClass(degree)` resolves a 1-based degree to a `PitchClass` and throws `ArgumentOutOfRangeException` outside 1..Count.
- `Domain/ScaleDegree.cs` (new) — `ScaleDegree(int Number)` readonly record struct: pure key-relative position with **no quality** (diatonic generator derives quality). Distinct from quality-carrying `RomanDegree` and chord-relative `ChordTone` per IN4. Kept `RomanDegree` as-is (C6) so progression authoring + existing tests are untouched.
- `Domain/QualityIntervals.cs` — added `FromIntervals(IReadOnlyCollection<int>)` reverse lookup (order-independent set match) so a stack of scale thirds can be named.
- `Domain/DiatonicChord.cs` (new) — `DiatonicChord.Build(Scale, ScaleDegree)`: stacks scale thirds (positions rootIdx, +2, +4, +6, adding 12 semitones on octave wrap so the stack stays ascending), computes intervals from the chord root, and matches them to a `Quality` via `FromIntervals`. Quality is fully derived.
- `Domain/Transposer.cs` — refactored to consume a `Scale`. Added `Realize(Progression, Scale)` overload; `Realize(Progression, Key)` now delegates via `Scale.ForKey(key)`. Removed the hardcoded `MajorOffsets`/`NaturalMinorOffsets` arrays (now in Scale). Out-of-range degree still throws `ArgumentOutOfRangeException` (now from `Scale.DegreePitchClass`).

Tests: `tests/DiatonicChordTests.cs` (new) — full C-major diatonic set (I Cmaj7 .. vii Bm7b5), transposition (ii of G = Am7), out-of-range throw; plus `ScaleTests` for `ForKey` interval selection and natural-minor degree. Full suite 61 passed (was 50); all 5 prior TransposerTests still green.

## Step 3 — NoteSpeller: add pure NoteSpeller(PitchClass, Key) -> spelled name + key-signature token; promote the renderer's hardcoded MajorKeyName/MajorKeySignature arrays into the domain and have AlphaTexRenderer call the speller. Unit-test spelling across sharp keys (D, F#), flat keys (Ab, Bb), and C.

**NoteSpeller** — spelling promoted from the renderer into the domain (C2: renderer stays the only alphaTex-aware code; spelling now lives in Domain).

Files:
- `Domain/NoteSpeller.cs` (new) — pure `NoteSpeller`. `Name(PitchClass, Key)` returns the per-key spelled note name (sharp vs flat table chosen by key); `KeySignatureToken(Key)` returns the lowercase alphaTex `\ks` token (tonic spelled, e.g. `bb`, `f#`). Sharp/flat direction driven by a `MajorUsesSharps[12]` table reproducing the renderer's old name/signature choices exactly (pc1=Db flat, pc6=F# sharp, pc8=Ab flat, C defaults to naturals/flats). Minor keys resolve via their relative major (tonic+3) so the speller is general even though v1 renders major only. Spelling derived, never stored (C4).
- `Rendering/AlphaTexRenderer.cs` — deleted the hardcoded `MajorKeySignature` and `MajorKeyName` arrays; `Render` now calls `NoteSpeller.Name(key.Tonic, key)` for the title and `NoteSpeller.KeySignatureToken(key)` for `\ks`. Output is byte-identical to before.

Tests: `tests/NoteSpellerTests.cs` (new) — per-key accidental direction across sharp keys (D→C#/F#, F#), flat keys (Ab→Db, Bb→Bb/Eb), and C (naturals); key-signature token casing; and a 12-key regression asserting the token array exactly matches the renderer's old hardcoded tokens. Full suite 75 passed (was 61); all 4 AlphaTexRendererTests still green (unchanged output).

## Step 4 — Voicing diagram metadata + strategy: add optional BarreFret/FirstFret/muted-strings metadata to Voicing; define IVoicingStrategy (Chord+Difficulty -> Voicing) and make the current algorithmic shell shape the Beginner strategy; VoicingBook resolves via strategy. Keep existing VoicingBook tests green; add a metadata test.

**Voicing diagram metadata + strategy** (additive; existing VoicingBook/renderer tests stay green).

Files:
- `Domain/Voicing.cs` — added three optional diagram-metadata fields as record params with defaults: `int? BarreFret = null`, `int? FirstFret = null`, `IReadOnlyList<int>? MutedStrings = null`. `Positions` stays authoritative (IN6); metadata is presentation-only for the alphaTex `\chord (...)` directive. Existing `new Voicing(positions)` call sites unaffected by the defaults.
- `Domain/IVoicingStrategy.cs` (new) — `IVoicingStrategy` with `Difficulty Difficulty { get; }` and `Voicing Voice(Chord)`. Difficulty selects the strategy; each strategy voices a chord (IN7 / design §3.5).
- `Domain/BeginnerShellStrategy.cs` (new) — the movable dominant-7 shell shape (root + maj3 + min7 on strings 5/4/3) moved verbatim out of VoicingBook into the first strategy. Positions are byte-identical to before. Now also emits derived diagram hints: `FirstFret` = lowest fret used, `MutedStrings` = {1,2,6} (unplayed high-E/B/low-E), `BarreFret` = null. Still throws `NotSupportedException` for non-Dominant7.
- `Domain/VoicingBook.cs` — rewritten as a strategy dispatcher: a `Difficulty → IVoicingStrategy` registry (only `BeginnerShellStrategy` registered for the MVP). `Lookup(chord, difficulty)` resolves the strategy and delegates; throws `NotSupportedException` for an unregistered difficulty (keeps the Intermediate-throws test green) — the non-quality throw now comes from the strategy.

Tests: `tests/VoicingBookTests.cs` — added `Lookup_BeginnerShell_CarriesDiagramMetadata` (asserts BarreFret null, FirstFret 0 for Bb7, MutedStrings {1,2,6}) and `BeginnerShellStrategy_ReportsBeginnerDifficulty`. All 7 prior VoicingBook tests unchanged and green. Full suite 77 passed (was 75); renderer output unchanged (uses Positions only).

## Step 5 — Tick-grid rhythm types (parallel, non-breaking): add PPQ=48 constant, TimeSignature (4/4), RhythmEvent(Position, Length, Stroke, Accent), and a new tick-based RhythmPattern type ALONGSIDE the existing Beat model (no removals yet so the build stays green). Add a tick->alphaTex quantizer helper (events in tick order -> :N tokens, rests for gaps, ties across the grid) with focused unit tests. Port the 3 MVP patterns to tick definitions and model pickup/anacrusis as a leading measure.

**Tick-grid rhythm types + quantizer** (added in parallel; old Beat model untouched so the build stays green — C7).

New Domain types:
- `TickGrid.cs` — `Ppq = 48` constant (C1) + `WholeNoteTicks = 192`.
- `TimeSignature.cs` — `TimeSignature(Numerator, Denominator)` readonly record struct; `FourFour` static, `BeatTicks` (=48 in 4/4) and `BarTicks` (=192) derived from PPQ (IN9). 4/4 only (EX2).
- `Stroke.cs` (`Down/Up/Either`), `Accent.cs` (`Normal/Accented`) — composable overlay enums.
- `RhythmEvent.cs` — `RhythmEvent(int Position, int Length, Stroke, Accent)` readonly record struct (ticks); `Hit(pos,len)` factory for plain down-stroke comping hits.
- `PickupMeasure.cs` — `PickupMeasure(Events, LengthTicks)`: anacrusis modeled as its own short leading measure (IN11), not a negative position.
- `TickRhythmPattern.cs` — `TickRhythmPattern(Id, Name, Events, TimeSignature, Pickup?)`, timing-only. Named `Tick...` during the parallel phase; renamed to `RhythmPattern` in step 6 after the old one is deleted.

Quantizer (Rendering seam, C2 / IN12):
- `Rendering/RhythmSlot.cs` — `RhythmSlot(int NoteValue, bool IsRest, bool TiedToPrevious)`; `NoteValue` is the alphaTex `:N` number (1/2/4/8/16). Final `:N` formatting stays in AlphaTexRenderer.
- `Rendering/RhythmQuantizer.cs` — `Quantize(events, TimeSignature)` / `Quantize(PickupMeasure)` / core `Quantize(events, barTicks, beatTicks)`. Walks events in tick order, fills gaps with rests, **splits spans at every beat line**, and greedy-decomposes each chunk into representable note values. A note crossing a beat line yields tied continuation slots; rests are separate cells. Validates ordering/overlap/bar-overflow (throws ArgumentException) and unrepresentable remainders (NotSupportedException — tuplets/32nds out of v1 scope, EX1).

Key decision (per alphaTex ref line 49: **ties/dotted tokens are NOT verified; MVP needs only `:4`+`r`**): beat-line splitting was chosen deliberately — it reproduces the MVP `:4 (chord) r r r` exactly (a 144-tick rest → three quarter rests, not a dotted half) and matches the beat-cursor semantics, while ties are kept as slot *metadata only* and never arise for the MVP seed patterns (so no unverified tie token is ever emitted in v1 output).

Seed: ported the 3 MVP patterns to `TickBeat1`/`TickBeat1And3`/`TickQuarters` + `TickRhythmPatterns` list in `SeedData.cs`, keeping the same ids (`beat_1`/`beat_1_3`/`quarters`) so the step-6 swap keeps feature lookups working. Old `Beat`-based `RhythmPatterns` left in place.

Tests: `tests/RhythmQuantizerTests.cs` (new, 10 cases) — bar/beat tick derivation; the 3 MVP patterns quantize to the expected hit/rest slot sequences; note-across-two-beats → tied quarters; sixteenth-on-downbeat decomposition; unordered-event sorting; overlap + beyond-bar throws; pickup leading measure. Full suite 87 passed (was 77); build green with both rhythm models present.

## Step 6 — Switch to the tick model + remove the old one (atomic migration): repoint Exercise, SeedData, and AlphaTexRenderer at the tick-based RhythmPattern + quantizer; delete the old Beat/sequential RhythmPattern and the inline duration logic in the renderer; regenerate from definitions and wipe the dev SQLite DB rather than migrating persisted rows (respects EX3). Update AlphaTexRendererTests to the new model; full solution build + test pass.

**Atomic migration to the tick model; old Beat model deleted** (C7: full solution builds + all tests green).

Removals:
- Deleted `Domain/Beat.cs`, `Domain/Duration.cs`, and `Domain/TickRhythmPattern.cs`.
- Removed the renderer's inline `DurationToken`/`Beat`-iteration logic.

Swap:
- `Domain/RhythmPattern.cs` — replaced the old `RhythmPattern(Id, Name, IReadOnlyList<Beat>)` with the tick-based record `RhythmPattern(Id, Name, IReadOnlyList<RhythmEvent> Events, TimeSignature, PickupMeasure? Pickup)` (the promoted TickRhythmPattern). Same type name, so `Exercise.Rhythm` and the Features/Infrastructure call sites compiled unchanged.
- `Domain/SeedData.cs` — dropped the Beat-based patterns; the three tick patterns are now `Beat1`/`Beat1And3`/`Quarters` (+ `RhythmPatterns` list), keeping ids `beat_1`/`beat_1_3`/`quarters`. `GenerateExercise`, `ExerciseLibrary`, `WebMessageRouter` resolve by id and were untouched.
- `Rendering/AlphaTexRenderer.cs` — rewired onto `RhythmQuantizer`: per bar it quantizes `rhythm.Events` (and a leading `Pickup` measure if present, voiced with the first chord) into `RhythmSlot`s, then formats stateful `:N` + chord group / `r`. `\ts` now derives from the pattern's `TimeSignature` (`4 4` for 4/4 — identical output). Tied slots throw `NotSupportedException` (alphaTex tie token unverified; MVP never ties), keeping the renderer honest. Spelling + quantization stay outside this class (C2).

Persistence / EX3: no schema change and no row migration — the DB stores `RhythmId` (string) and regenerates alphaTex on load; the tick patterns reuse the same ids. No tracked exercise `.db` exists in the repo (only WebView2 caches), so "wipe the dev DB" is a runtime note, nothing to change in code.

Tests: existing `AlphaTexRendererTests` pass **unchanged** — the MVP seed patterns produce byte-identical alphaTex through the new path (beat-line quantizing reproduces `:4 (chord) r r r`). Updated `RhythmQuantizerTests` seed references (`Tick*` → renamed). Added two renderer tests: `\ts`-from-pattern + tick-path Quarters output, and a pickup leading-measure render (2 bars, stateful `:4` carried across). Full suite 89 passed (was 87); `dotnet build` of the App project succeeds with 0 errors.

## Step 7 — Feel / Accent / Stroke overlays: implement Feel as a playback-time timing transform (straight grid -> long-short for Swing/Shuffle/Triplet) and AccentPattern + Stroke as composable layers applied onto a pattern; ensure Feel is never stored in RhythmPattern. Unit-test that composing pattern+accent+feel yields expected timing/accents without mutating the base pattern.

**Feel / Accent / Stroke composable overlays** (pure Domain transforms; base pattern never mutated — C4 / IN10).

Files (all new, Domain):
- `Feel.cs` — `Feel { Straight, Swing, Shuffle, Triplet }`. Documented as a playback-time transform, never stored in a RhythmPattern.
- `FeelTransform.cs` — `Apply(events, feel, TimeSignature) → new event list`. `OffBeatRatio(feel)`: Straight 1/2, Swing 2/3, Shuffle 3/4, Triplet 2/3 (Triplet shares swing's ratio for v1, documented). Warps the off-beat eighth (offset == half-beat) later to the swing point and shortens it; lengthens the matching on-beat eighth — the long-short groove. Straight is identity. Returns a fresh array; inputs untouched.
- `AccentPattern.cs` — `AccentPattern(IReadOnlyList<int> AccentedBeats)` with `Backbeat` (beats 2 & 4). `Apply(events, ts)` sets `Accent.Accented` on events whose beat index is accented, leaving others as-is (additive overlay), returning a new list.
- `StrokeOverlay.cs` — `All(events, stroke)` and `AlternateDownUp(events)`; both return new lists.

Design decisions:
- Overlays operate on `IReadOnlyList<RhythmEvent>` and return new lists, so they compose by chaining (`accent → feel`) and Feel is provably not stored on the pattern. Feel is intentionally NOT applied in AlphaTexRenderer — it is a playback-time concern and the score stays notated straight.
- Accents are beat-granular: both eighths within an accented beat carry the accent (validated in tests).

Tests: `tests/RhythmOverlayTests.cs` (new, 8 cases) — Straight=identity; Swing pushes off-beats to 2/3 with long(32)/short(16); Shuffle to 3/4 (36/12); Feel is a no-op on the quarter-grid MVP patterns; Backbeat accents beats 2 & 4; alternate down/up strokes; and two composition tests proving accent+feel compose to the expected timing/accents AND that `SeedData.Beat1And3.Events` is unchanged afterward (no-mutation, NotSame). Full suite 97 passed (was 89).

## Step 8 — Lead TargetZone domain layer: add TargetZone(ChordTone, Importance); derive guide tones (3 & 7) from the interval sets and resolve TargetZones to fretboard FretPositions for a chord — domain types only, no fretboard UI (respects EX5). Unit-test ii-V-I guide-tone targets (e.g. G7 -> 3 and b7) resolve to correct pitch classes/positions.

**Lead TargetZone domain layer** (domain types only, no fretboard UI — EX5; IN14).

Files (all new, Domain):
- `Importance.cs` — `Importance { Primary, Secondary }` (guide tones are Primary sweet spots).
- `TargetZone.cs` — `TargetZone(ChordTone Tone, Importance Importance)` readonly record struct; chord-relative so it transposes with the chord, resolved to concrete pitch classes/positions late.
- `Fretboard.cs` — standard-tuning (E A D G B E) geometry. `PositionsFor(PitchClass, maxFret=12)` returns every `FretPosition` (alphaTab string numbering 1=high E..6=low E) that sounds the pitch class, ordered by string then fret. Pure, no UI.
- `LeadTargets.cs` — `GuideTones(Chord)` derives the 3rd & (if present) 7th from `ChordTones.Of` as `Primary` TargetZones — no per-chord authoring; `PitchClassOf(chord, zone)` resolves the chord-relative tone to a concrete pitch class; `Resolve(chord, zone, maxFret)` maps it to fretboard positions.

Decisions:
- Guide tones built directly off the interval-set chord tones from step 1 (Third/Seventh functions), so a ii–V–I guide-tone line falls out automatically. A triad (no 7th) yields just the 3rd.
- Fretboard kept a pure static (standard tuning only for v1); alt tunings can add an instance form later.

Tests: `tests/LeadTargetsTests.cs` (new, 6 cases incl. a 3-row Theory) — G7 guide tones = B(11) & F(5), both Primary; ii–V–I in C (Dm7→F/C, G7→B/F, Cmaj7→E/B) resolve to the expected pitch classes; resolving G7's 3rd returns only fretboard positions sounding B (incl. open B string (2,0)); a triad yields only its 3rd. Full suite 103 passed (was 97).

## Step 9 — End-to-end wiring + verification: assemble the full Exercise pipeline (resolve progression -> choose voicings/targets -> apply rhythm+feel -> quantize -> alphaTex), run the complete test suite, and do a render smoke check that a Bb 12-bar blues exercise produces valid alphaTex through the new path.

**End-to-end wiring + verification** (IN15).

Wiring:
- `Domain/Exercise.cs` — added `Feel Feel = Feel.Straight` as the unifying object's groove field (defaulted, so all existing 5-arg constructions and persistence are unaffected — no ExerciseEntity/schema change, respecting EX6). Documented as a playback-time transform, never stored on the pattern (C4).
- `Rendering/AlphaTexRenderer.cs` — completed the pipeline tail: before quantizing each bar it applies `FeelTransform.Apply(rhythm.Events, exercise.Feel, ts)` (identity for Straight), then `RhythmQuantizer.Quantize` → tokens. So the full chain is now: `Transposer.Realize` (resolve progression via Scale) → `VoicingBook` strategy (voicings) → `FeelTransform` (rhythm+feel) → `RhythmQuantizer` → alphaTex. The lead-target branch (`LeadTargets.GuideTones`/`Resolve`) is the alternate content path.

Verification:
- `tests/ExercisePipelineTests.cs` (new, 3 cases):
  - Bb 12-bar blues smoke check: renders through the new path and asserts valid alphaTex — correct header (`\title "12-Bar Blues — Bb"`, `\subtitle`, `\tempo 80`, `\ts 4 4`, `\ks bb`, lone `.`), 12 bars/pipes, stateful `:4` exactly once, I/IV/V shell voicings (Bb7 `(1.5 0.4 1.3)`, Eb7 `(6.5 5.4 6.3)`, F7 `(8.5 7.4 8.3)`), every bar ends `" |"`.
  - Feel.Straight render == no-Feel render (identity warp doesn't change the score).
  - Lead-target branch: every realized chord of the Bb blues resolves its two guide tones to non-empty fretboard positions that all sound the expected pitch class.

Full suite: **106 passed, 0 failed** (was 103). Solution builds clean. The migration is complete — interval-backed qualities, Scale + diatonic generation, key-aware spelling, strategy voicings, the 48-PPQ tick rhythm model + quantizer, feel/accent/stroke overlays, lead TargetZones, and the rewired Exercise pipeline are all in place and green.
