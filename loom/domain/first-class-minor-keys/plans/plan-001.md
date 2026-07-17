---
type: plan
id: pl_01KXQWH3EHRG7JYEG7D5XREV8T
title: First-class minor keys — Implementation
status: done
created: 2026-07-17
updated: 2026-07-17
version: 2
design_version: 1
req_version: 1
tags: []
parent_id: de_01KXQW333KYNQCH1P71VB9E35P
requires_load: []
target_version: 0.1.0
steps:
  - id: transposer-realizes-minor-keys-via-major
    order: 1
    status: done
    description: Resolve degree roots through Scale.Major(key.Tonic) always; leave Scale.ForKey untouched.
    files_touched: [src/ChordFlow.Core/Music/Progressions/Transposer.cs, loom/refs/chordflow-domain-model-reference.md, loom/refs/chordflow-dsl-reference.md]
    blocked_by: []
    satisfies: [IN1, IN2, C1, C2, C3]
  - id: realization-golden-tests-for-minor-keys
    order: 2
    status: done
    description: "Golden tests: natural-minor i–iv–v and iiø–V–i realized in a minor key; a major progression unchanged."
    files_touched: [tests/ChordFlow.Core.Tests/TransposerTests.cs]
    blocked_by: [transposer-realizes-minor-keys-via-major]
    satisfies: [IN7, C1, C2]
  - id: minor-ks-token-round-trip-inverse
    order: 3
    status: done
    description: KeySignatureToken emits {tonic}minor; KeyFromSignatureToken parses the minor suffix; confirm minor note spelling via UsesSharps.
    files_touched: [src/ChordFlow.Core/Music/Harmony/NoteSpeller.cs, tests/ChordFlow.Core.Tests/NoteSpellerTests.cs, loom/refs/alphatex-syntax-reference.md, loom/refs/chordflow-domain-model-reference.md]
    blocked_by: []
    satisfies: [IN3, IN4, IN6, C4]
  - id: c-pivot-parent-major-realization-converter
    order: 4
    status: done
    description: "Pivot realization to C: parent-major realization + Progression.Home + the ToParent/ToAuthor converter (reverts step 1's A1 change)."
    files_touched: [src/ChordFlow.Core/Music/Progressions/Transposer.cs, src/ChordFlow.Core/Music/Progressions/Progression.cs, src/ChordFlow.Core/Music/Progressions/DegreeFrameConverter.cs, loom/refs/chordflow-domain-model-reference.md]
    blocked_by: []
    satisfies: [IN1, IN2, IN8, IN9, C1, C2, C3]
  - id: parser-applies-the-converter
    order: 5
    status: done
    description: ProgressionParser applies ToParent at parse (given Home); the .dsl stays author-frame, Bars become parent-major.
    files_touched: [src/ChordFlow.Core/Music/Progressions/ProgressionParser.cs, src/ChordFlow.Core/Music/Songs/SongParser.cs, tests/ChordFlow.Core.Tests/ProgressionParserTests.cs, loom/refs/chordflow-dsl-reference.md]
    blocked_by: [c-pivot-parent-major-realization-converter]
    satisfies: [IN10, IN1]
  - id: c-realization-spelling-goldens
    order: 6
    status: done
    description: "C goldens: converter round-trip; natural-minor i–iv–v & iiø–V–i; harmonic-minor vii°7→G♯, melodic-minor vi°→F♯ (replaces the A1 goldens from step 2)."
    files_touched: [tests/ChordFlow.Core.Tests/DegreeFrameConverterTests.cs, tests/ChordFlow.Core.Tests/TransposerTests.cs, tests/ChordFlow.Core.Tests/ChordSymbolTests.cs]
    blocked_by: [c-pivot-parent-major-realization-converter, parser-applies-the-converter]
    satisfies: [IN7, IN4, C4]
  - id: renderer-spelling-golden-for-a-minor
    order: 7
    status: done
    description: "AlphaTexRenderer golden: a minor tune emits \\ks {tonic}minor + relative-major spelling; major render byte-identical."
    files_touched: [tests/ChordFlow.Core.Tests/AlphaTexRendererTests.cs]
    blocked_by: [c-pivot-parent-major-realization-converter, minor-ks-token-round-trip-inverse]
    satisfies: [IN7, IN4, C1]
  - id: 8a-bridge-features-mode-threading
    order: 8
    status: done
    description: "8a — bridge + Features mode threading: GenerateRequest/envelope carry keyIsMinor; GenerateExercise.Build + ContentCrud.Preview build new Key(pc, isMinor)."
    files_touched: [src/ChordFlow.Core/Bridge/WebMessageRouter.cs, src/ChordFlow.Core/Features/GenerateExercise/GenerateExercise.cs, src/ChordFlow.Core/Features/ContentCrud/ContentCrudHandler.cs, src/ChordFlow.Desktop/Program.cs, tests/ChordFlow.Core.Tests/GenerateExerciseTests.cs, tests/ChordFlow.Core.Tests/ContentCrudHandlerTests.cs]
    blocked_by: [c-pivot-parent-major-realization-converter]
    satisfies: [IN5]
  - id: ui-key-picker-offers-minor-keys
    order: 9
    status: done
    description: harmony-controls offers minor keys; carry isMinor through bridge → Features so a minor key realizes on Score and Sheet.
    files_touched: [src/ChordFlow.Desktop/wwwroot/harmony-controls-component.js, src/ChordFlow.Desktop/Program.cs, src/ChordFlow.Core/Bridge/, src/ChordFlow.Core/Features/GenerateExercise/GenerateExercise.cs, src/ChordFlow.Core/Features/ContentCrud/ContentCrudHandler.cs]
    blocked_by: [c-pivot-parent-major-realization-converter, minor-ks-token-round-trip-inverse]
    satisfies: [IN5]
---
# First-class minor keys — Implementation

## Goal

Make minor keys a coherent, first-class citizen end-to-end under the **C** frame (chosen in chat-001, superseding the initial A1 pick): every progression is stored in one absolute **parent-major** frame, and minor is an authoring lens applied by a small pure **converter** at the DSL edges — so the kernel never branches on mode and the design generalizes to any scale/mode. `Key(A, minor)` stays honest, driving both the parent major (for realization) and the spelling. Work: pivot `Transposer` to realize against the key's parent major, add `Progression.Home` + the `ToParent`/`ToAuthor` converter, wire the parser to convert at parse (the `.dsl` stays author-frame), keep the native `\ks {tonic}minor` token, and thread the mode through the UI → bridge → Features chain. **Steps 1–2 implemented the earlier A1 frame and are reverted by the C-pivot step; step 3 (`\ks`) and the renderer guard removal survive.** Reference docs are updated in the same steps that change their areas.

---

## Steps

| Done | # | Step | Files touched | Blocked by | Satisfies |
|---|---|---|---|---|---|
| ✅ | 1 | Resolve degree roots through Scale.Major(key.Tonic) always; leave Scale.ForKey untouched. | src/ChordFlow.Core/Music/Progressions/Transposer.cs, loom/refs/chordflow-domain-model-reference.md, loom/refs/chordflow-dsl-reference.md | — | IN1, IN2, C1, C2, C3 |
| ✅ | 2 | Golden tests: natural-minor i–iv–v and iiø–V–i realized in a minor key; a major progression unchanged. | tests/ChordFlow.Core.Tests/TransposerTests.cs | transposer-realizes-minor-keys-via-major | IN7, C1, C2 |
| ✅ | 3 | KeySignatureToken emits {tonic}minor; KeyFromSignatureToken parses the minor suffix; confirm minor note spelling via UsesSharps. | src/ChordFlow.Core/Music/Harmony/NoteSpeller.cs, tests/ChordFlow.Core.Tests/NoteSpellerTests.cs, loom/refs/alphatex-syntax-reference.md, loom/refs/chordflow-domain-model-reference.md | — | IN3, IN4, IN6, C4 |
| ✅ | 4 | Pivot realization to C: parent-major realization + Progression.Home + the ToParent/ToAuthor converter (reverts step 1's A1 change). | src/ChordFlow.Core/Music/Progressions/Transposer.cs, src/ChordFlow.Core/Music/Progressions/Progression.cs, src/ChordFlow.Core/Music/Progressions/DegreeFrameConverter.cs, loom/refs/chordflow-domain-model-reference.md | — | IN1, IN2, IN8, IN9, C1, C2, C3 |
| ✅ | 5 | ProgressionParser applies ToParent at parse (given Home); the .dsl stays author-frame, Bars become parent-major. | src/ChordFlow.Core/Music/Progressions/ProgressionParser.cs, src/ChordFlow.Core/Music/Songs/SongParser.cs, tests/ChordFlow.Core.Tests/ProgressionParserTests.cs, loom/refs/chordflow-dsl-reference.md | c-pivot-parent-major-realization-converter | IN10, IN1 |
| ✅ | 6 | C goldens: converter round-trip; natural-minor i–iv–v & iiø–V–i; harmonic-minor vii°7→G♯, melodic-minor vi°→F♯ (replaces the A1 goldens from step 2). | tests/ChordFlow.Core.Tests/DegreeFrameConverterTests.cs, tests/ChordFlow.Core.Tests/TransposerTests.cs, tests/ChordFlow.Core.Tests/ChordSymbolTests.cs | c-pivot-parent-major-realization-converter, parser-applies-the-converter | IN7, IN4, C4 |
| ✅ | 7 | AlphaTexRenderer golden: a minor tune emits \ks {tonic}minor + relative-major spelling; major render byte-identical. | tests/ChordFlow.Core.Tests/AlphaTexRendererTests.cs | c-pivot-parent-major-realization-converter, minor-ks-token-round-trip-inverse | IN7, IN4, C1 |
| ✅ | 8 | 8a — bridge + Features mode threading: GenerateRequest/envelope carry keyIsMinor; GenerateExercise.Build + ContentCrud.Preview build new Key(pc, isMinor). | src/ChordFlow.Core/Bridge/WebMessageRouter.cs, src/ChordFlow.Core/Features/GenerateExercise/GenerateExercise.cs, src/ChordFlow.Core/Features/ContentCrud/ContentCrudHandler.cs, src/ChordFlow.Desktop/Program.cs, tests/ChordFlow.Core.Tests/GenerateExerciseTests.cs, tests/ChordFlow.Core.Tests/ContentCrudHandlerTests.cs | c-pivot-parent-major-realization-converter | IN5 |
| ✅ | 9 | harmony-controls offers minor keys; carry isMinor through bridge → Features so a minor key realizes on Score and Sheet. | src/ChordFlow.Desktop/wwwroot/harmony-controls-component.js, src/ChordFlow.Desktop/Program.cs, src/ChordFlow.Core/Bridge/, src/ChordFlow.Core/Features/GenerateExercise/GenerateExercise.cs, src/ChordFlow.Core/Features/ContentCrud/ContentCrudHandler.cs | c-pivot-parent-major-realization-converter, minor-ks-token-round-trip-inverse | IN5 |
---

### Legend

| Symbol | Meaning |
|--------|---------|
| ✅ | Done |
| 🔄 | In Progress |
| 🔳 | Pending |
| ❌ | Cancelled |

<!-- step:transposer-realizes-minor-keys-via-major -->
### Step 1 — Transposer realizes minor keys via major offsets (A1 frame)

Change the two key-taking entry points — `Realize(Progression, Key)` (line 67) and `RealizeBars(Progression, Key)` (line 97) — to pass `Scale.Major(key.Tonic)` instead of `Scale.ForKey(key)`; the `key` still flows into `ChordFor` for `RootSpelling`, and the scale-only overloads are unchanged. For a **major** key `Scale.ForKey` already returns `Major`, so output is byte-identical (C1). For a **minor** key this is the A1 fix: roots come from the major frame, the minor color rides the quality (`1-`), and the raised 7 / major V ride the quality too — no harmonic-minor scale (C2). `Scale.ForKey` itself is deliberately left alone so `HarmonicAnalyzer` keeps its natural-minor classification (C3). Update the Transposer entry in the domain-model ref and add a minor-key authoring note (home `1-`, `b3 b6 b7` explicit) to the DSL ref, in this same unit of work.

<!-- step:realization-golden-tests-for-minor-keys -->
### Step 2 — Realization golden tests for minor keys

Assert A1 realization in A minor: `1- 4- 5-` → Am, Dm, Em; `2ø 57 1-` → Bm7b5, E7, Am (the E7 major-V third G♯ demonstrates the leading tone riding the quality, C2). Add a regression assertion that a representative existing major progression (e.g. the 12-bar blues degrees) realizes to the same `Chord[]` as before (C1).

<!-- step:minor-ks-token-round-trip-inverse -->
### Step 3 — Minor \ks token + round-trip inverse; verify relative-major spelling

`KeySignatureToken(key)` appends `minor` for a minor key (alphaTab-native `\ks Aminor`; keep the existing lowercase-tonic form for major). `KeyFromSignatureToken` learns the `minor` suffix so a persisted minor `Exercise.KeyOverride` round-trips (currently hardcodes `IsMinor: false`). IN4 (diatonic notes spelled from the relative-major table) is **already** delivered by `UsesSharps` (tonic+3 → relative major) — add a confirming test (A minor spells all-naturals, no code change). Chromatic roots keep `RootSpelling` untouched (C4). Update the alphatex ref to document the `{Note}minor` `\ks` form (IN6) and the NoteSpeller entry in the domain-model ref, in this same unit of work.

<!-- step:c-pivot-parent-major-realization-converter -->
### Step 4 — C pivot — parent-major realization + converter

Replace step 1's A1 change (`Scale.Major(key.Tonic)`) with parent-major realization: `Scale.Major(ParentTonic(key))` where `ParentTonic` = `key.Tonic` for major, `key.Tonic + 3` (relative major) for minor. Add `enum Tonality { Major, Minor }` and `Progression.Home` (default `Major`, so existing progressions are unchanged — IN8). Add the pure `DegreeFrameConverter.ToParent`/`ToAuthor` (IN9): a fixed degree rotation (`1→6 2→7 3→1 4→2 5→3 6→4 7→5` for minor) with the accidental carried through unchanged. `Scale.ForKey` stays untouched (C3); major realization byte-identical (C1); no per-mode kernel scale table (C2). Update the `Transposer` + add `Progression`/converter rows in the domain-model ref (rewriting the earlier A1 note to C).

<!-- step:parser-applies-the-converter -->
### Step 5 — Parser applies the converter

A minor-home progression's degrees are converted via `DegreeFrameConverter.ToParent` as they are parsed, so `Progression.Bars` end up parent-major while the stored `.dsl` text stays tonic-relative (author-frame). **Sub-decision to settle here:** how a progression declares its `Home` — from the Song's `key Am` context, and/or an explicit tag in the progression/catalog definition (a standalone content-pack progression must carry its mode). Update the DSL ref: rewrite the earlier A1 "explicit flats" note to the C authoring model (minor tunes author bare `3 6 7`, raised tones `#6`/`#7`; the converter maps to storage).

<!-- step:c-realization-spelling-goldens -->
### Step 6 — C realization + spelling goldens

Revert/replace the A1 minor goldens added in step 2 with the C set: (1) converter round-trip `ToAuthor(ToParent(d, Minor), Minor) == d` for all degrees × accidentals; (2) a minor-home progression realizes correctly — natural-minor `1- 4- 5-` → Am/Dm/Em and `2ø 57 1-` → Bm7♭5/E7/Am (through the parse→ToParent→parent-major path); (3) the payoff C unlocks — harmonic-minor vii°7 (authored `#7dim7` → stored `#5dim7`) root spells **G♯**, and melodic-minor vi° (authored `#6ø` → `#4ø`) root spells **F♯**, via `RootSpelling` (IN4, C4). Keep the C1 major-unchanged assertion.

<!-- step:renderer-spelling-golden-for-a-minor -->
### Step 7 — Renderer + spelling golden for a minor tune

Render a short A-minor tune end-to-end: assert the header carries `\ks aminor` (or the documented casing), diatonic notes spell from C's table, and a `#7dim7` chord root spells `G♯` via `RootSpelling`. Assert an existing major render is byte-identical (C1). The renderer already calls `NoteSpeller.KeySignatureToken`, so the minor `\ks` falls out of step 3 — this step is the end-to-end proof.

<!-- step:8a-bridge-features-mode-threading -->
### Step 8 — 8a — bridge + Features mode threading

Thread the key's mode from the definition through the bridge into the Features layer, defaulting `false` so every existing call compiles and every major flow is byte-identical. `GenerateRequest` + the inbound envelope gain `KeyIsMinor`; the router's `generate` and `entityPreview` cases pass it (the `EntityPreviewRequested` event grows a bool). `GenerateExercise.Build` (all overloads) and `ContentCrudHandler.Preview` take `bool keyIsMinor = false` and build `new Key(pc, keyIsMinor)`. `Program.cs` handlers forward `req.KeyIsMinor` / the preview flag. Unit-test that a minor request builds a minor `KeyOverride` / realizes a minor-key preview. Deferred to 8b: the JS toggle and seeding the mode from a song's key; loadExercise re-key mode is a separate follow-up.

<!-- step:ui-key-picker-offers-minor-keys -->
### Step 9 — UI key picker offers minor keys — thread isMinor end-to-end

Today the key is a bare `keyPitchClass` (int?) turned into `new Key(pc, false)` everywhere. Offer minor keys in `harmony-controls-component.js` (a major/minor mode toggle beside the Key select, or 24 entries) and emit `isMinor` from `getDefinition`. Carry `isMinor` through the bridge request DTOs + `Program.cs` router wiring into `GenerateExerciseHandler` and `ContentCrudHandler.Preview`, building `new Key(pc, isMinor)`. Absent ⇒ `false`, so existing major flows are unchanged. Dogfood: pick a minor key in the app → correct realized chords + spelling on Score and Sheet.
