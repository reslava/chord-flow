---
type: done
id: pl_01KXQWH3EHRG7JYEG7D5XREV8T-done
title: Done — First-class minor keys — Implementation
status: done
created: 2026-07-17
version: 9
tags: []
parent_id: pl_01KXQWH3EHRG7JYEG7D5XREV8T
requires_load: []
---
# Done — First-class minor keys — Implementation

## Step 1 — Resolve degree roots through Scale.Major(key.Tonic) always; leave Scale.ForKey untouched.

Changed `Transposer`'s two key-taking entry points — `Realize(Progression, Key)` and `RealizeBars(Progression, Key)` — to resolve degree roots through `Scale.Major(key.Tonic)` instead of `Scale.ForKey(key)`. The `key` still flows into `ChordFor` for `RootSpelling`; the scale-only overloads and `Scale.ForKey` itself are untouched (`HarmonicAnalyzer` keeps its natural-minor classification). This is the A1 frame made real: for a major key `ForKey` already returned the major scale, so output is byte-identical; for a minor key roots stop double-shifting.

Added a `<para>` to the `Transposer` class XML doc documenting the A1 frame invariant. `dotnet build ChordFlow.Core` succeeds, 0 warnings.

Ref sync (same unit of work): updated the `Transposer` row in `chordflow-domain-model-reference.md` with the A1 realization note, and added a "Minor keys use one absolute frame" bullet to `chordflow-dsl-reference.md`'s Progression DSL Notes (home `1-`; `b3 b6 b7` explicit; major V = `5`/`57`).

## Step 2 — Golden tests: natural-minor i–iv–v and iiø–V–i realized in a minor key; a major progression unchanged.

Added A1 realization goldens to `TransposerTests.cs`:
- `Realize_MinorKey_A1_NaturalMinorTonicSubdominantDominant` — `1- 4- 5-` in A minor → Am, Dm, Em (roots 9,2,4).
- `Realize_MinorKey_A1_MinorTwoFiveOne` — `2ø 57 1-` in A minor → Bm7b5, E7, Am (roots 11,4,9); the E7 major-V demonstrates the raised leading tone riding the Dominant7 quality (C2).
- `Realize_MinorKey_A1_FlatDegreesAreTheNaturalMinorThirdSixthSeventh` — `b3 b6 b7` in A minor → C, F, G (roots 0,5,7).
- **Replaced** the stale `Realize_MinorKey_UsesNaturalMinorScaleDegrees` (which asserted the old pre-A1 double-shift: bare `3` in A minor → C) with `Realize_MinorKey_A1_ResolvesRootsThroughMajorFrame` asserting the A1 truth: bare `3` → C# (major frame), `b3` → C (explicit ♭III). This test encoded the very behavior A1 removes, so updating it is intended, not a regression.

C1 (major unchanged) is covered by the pre-existing `Realize_TwelveBarBlues_ProducesCorrectRootsInEveryKey` theory across all 12 major keys, still passing.

Verified: `dotnet test` TransposerTests 25/25, then the **full Core suite 947/947 pass** — step 1's frame change broke no other minor-key test (SongParser, ChordSheet, HarmonicAnalyzer, the existing A-minor AlphaTexRenderer test all green).

## Step 3 — KeySignatureToken emits {tonic}minor; KeyFromSignatureToken parses the minor suffix; confirm minor note spelling via UsesSharps.

`NoteSpeller.cs`:
- `KeySignatureToken(key)` now appends `minor` for a minor key (`aminor`, `c#minor`); a major key stays a bare note (byte-identical, C1). alphaTab accepts this natively (`\ks Aminor`).
- `KeyFromSignatureToken(token)` learns the `minor` suffix (strips it, sets `IsMinor: true`); major parsing unchanged. Previously hardcoded `IsMinor: false`.
- Confirmed IN4 needs **no new code**: `UsesSharps` already maps a minor key to its relative major (`tonic + 3`), so diatonic notes already spell correctly.

`NoteSpellerTests.cs` — added `KeySignatureToken_MinorKey_AppendsMinorSuffix` (Am→`aminor`, Cm→`cminor`, C#m→`c#minor`, Em→`eminor`), `KeyFromSignatureToken_ParsesModeAndTonic`, `KeySignatureToken_RoundTripsThroughKeyFromSignatureToken` (all 24 keys), and `Name_MinorKey_SpellsFromRelativeMajorTable` (IN4 confirmation). NoteSpellerTests 30/30 pass.

Ref sync: `alphatex-syntax-reference.md` now records that ChordFlow **emits** `{tonic}minor` for a minor key (IN6, two spots); the `NoteSpeller` row in `chordflow-domain-model-reference.md` documents the minor spelling + token + inverse.

Note for step 4: found the renderer's `EnsureMajorSupported` guard (`AlphaTexRenderer.cs`, throws `NotSupportedException` on a minor section key, 2 call sites) and the `Render_MinorKey_Throws` test that pins it — both must be removed/rewritten in step 4 for minor keys to render.

## Step 4 — Pivot realization to C: parent-major realization + Progression.Home + the ToParent/ToAuthor converter (reverts step 1's A1 change).

The C pivot (kernel). `git restore`d the A1 Transposer to a clean base first, then:
- **`DegreeFrameConverter.cs`** (new) — `enum Tonality { Major, Minor }` + the pure converter: `ParentTonic(key)` (tonic for major, +3/relative major for minor), `ToParent(degree, home)` (rotation `1→6 2→7 3→1 4→2 5→3 6→4 7→5` for minor, accidental carried through), `ToAuthor` (exact inverse). No per-mode scale table (C2).
- **`Progression.cs`** — added `Home : Tonality { get; init; } = Tonality.Major` (IN8); default keeps every existing progression unchanged.
- **`Transposer.cs`** — `Realize`/`RealizeBars` now realize against `Scale.Major(DegreeFrameConverter.ParentTonic(key))` instead of `Scale.ForKey(key)` (IN1/IN2); `Scale.ForKey` untouched (C3). Crucially `SpellRoot` now counts letters from the **parent** tonic (`scale.Tonic`), not the key tonic — so `#5dim7` in A minor spells **G♯**, not A♭ (the payoff of the parent-major frame, C4). Major realization is byte-identical (C1: `ParentTonic` = the tonic for a major key).

Tests: replaced the A1 goldens in `TransposerTests.cs` with C ones (natural minor via `RealizeMinor` helper that runs author→`ToParent`→realize; bare `3 6 7` → C/F/G; iiø–V–i; **harmonic vii°7 `#7dim7`→G♯, melodic vi° `#6ø`→F♯** spelled letter-pure). Added **`DegreeFrameConverterTests.cs`** (ParentTonic, rotations, accidental pass-through, exact round-trip).

Ref sync: rewrote the `Transposer` note in the domain-model ref from A1 → C; added the `DegreeFrameConverter`/`Tonality` row and the `Progression.Home` note.

Verified: `dotnet build` clean; **full Core suite 981/981 green** — no other path relied on the old minor realization (major content byte-identical, C1). The converter is not yet wired to the parser (that's step 5).

## Step 5 — ProgressionParser applies ToParent at parse (given Home); the .dsl stays author-frame, Bars become parent-major.

Wired the converter into the parse path, driven by a `tonality:` catalog-header key (Option 1, Rafa's pick).
- **`CatalogMetadata`** — added `Tonality Tonality = Tonality.Major`; `IsEmpty` now also requires Major. Rides in the stored DSL header (no DB column/migration — same pattern as `description`).
- **`CatalogHeader`** — recognizes/parses/serializes `tonality:` (major/minor; unknown value fails loud). Round-trips 1:1.
- **`ProgressionParser.Parse(..., Tonality home = Major)`** — applies `DegreeFrameConverter.ToParent` to every degree at parse and sets `Progression.Home`. The `.dsl` stays author-frame; `Bars` become parent-major (IN10). Major home = identity, so all existing progressions are byte-identical.
- **`ProgressionStore.Find`** — passes the header's `meta.Tonality` into the parser, so a pack/stored minor progression realizes correctly.
- **`ContentCrudHandler.ProgressionPreview`** — strips the header and passes the tonality, so a minor progression previews with correct chords (dogfood surface).

Tests: `ProgressionParserTests` (minor-home `1- 4- 5-`→parent `6- 2- 3-`, Home=Minor, realizes Am/Dm/Em; `#7dim7`→G♯; major unchanged); `CatalogHeaderTests` (`tonality: minor` extract + round-trip + unknown-throws + major emits no line). Full Core suite **988/988 green**. Ref: rewrote the DSL-ref minor note from the A1 model (explicit flats) to the C model (bare `3 6 7`, `#6`/`#7`, `tonality: minor`, song picks the key).

**Deferred (noted, out of this step):** (1) inline progressions authored *inside* a Song don't yet inherit the song's mode — needs song-`key`-ordering handling and no minor songs exist yet; a referenced *stored* minor progression works fully. (2) user-authored minor via the CRUD `Save` (it strips the header per EX3). Both are follow-ups, not blockers for pack-based minor content + preview.

## Step 6 — C goldens: converter round-trip; natural-minor i–iv–v & iiø–V–i; harmonic-minor vii°7→G♯, melodic-minor vi°→F♯ (replaces the A1 goldens from step 2).

The C goldens. Most of this step's content landed alongside the code it proves (steps 4–5), so the remaining piece here is the user-visible chord-symbol spelling:
- **`ChordSymbolTests.Format_MinorProgression_SpellsNaturalAndRaisedChordsCorrectly`** — full pipeline (parse `1- 2ø 3 4- 5- 6 7 #7dim7 #6ø` with `tonality: minor` → realize in A minor → `ChordSymbol.Format`) yields the display symbols **Am · Bm7b5 · C · Dm · Em · F · G · G#dim7 · F#m7b5** — natural-minor diatonics spelled from the relative major, and the harmonic vii°7 / melodic vi° raised roots letter-pure (G♯/F♯, never A♭/G♭). This is the end-user-visible proof of IN4/C4.

Already covering step 6's other goldens (recorded under steps 4–5): **converter round-trip** `ToAuthor∘ToParent == id` for all degrees×accidentals (`DegreeFrameConverterTests`); **natural-minor i–iv–v & iiø–V–i** realization + **harmonic vii°7→G♯ / melodic vi°→F♯** RootSpelling (`TransposerTests`, `ProgressionParserTests`).

Full Core suite **989/989 green**.

## Step 7 — AlphaTexRenderer golden: a minor tune emits \ks {tonic}minor + relative-major spelling; major render byte-identical.

Renderer golden. Replaced the weak placeholder minor-render test with a C end-to-end one: `Render_MinorKey_EmitsMinorKeySignatureAndSpelledChordNames` parses `1- 4- 5- #7dim7` (tonality minor), renders in A minor with `ShowChordNames`, and asserts `\ks aminor` plus the `{ch "…"}` labels **Am / Dm / Em / G#dim7** — proving the renderer emits the minor key signature and spells the harmonic vii°7 raised root letter-pure (G♯, not A♭) all the way to the alphaTex label. `AlphaTexRendererTests` 27/27.

C1 (major render byte-identical) is guarded by the pre-existing exact-byte major goldens (`Render_KnownExercise_ProducesExpectedAlphaTex`, etc.), all still passing — the C pivot changed no major output. The `\ks aminor` token and the removal of the `EnsureMajorSupported` guard landed in steps 3–4; this step is the end-to-end proof.

## Step 8 — 8a — bridge + Features mode threading: GenerateRequest/envelope carry keyIsMinor; GenerateExercise.Build + ContentCrud.Preview build new Key(pc, isMinor).

Threaded the key's mode from the definition through the bridge into the Features layer, `false`-defaulted so every existing call compiles and every major flow is byte-identical.
- **`GenerateRequest`** gains `bool KeyIsMinor = false`; the inbound envelope gains `bool? KeyIsMinor`; the router's `generate` case passes `envelope.KeyIsMinor ?? false`.
- **`EntityPreviewRequested`** event grows a `bool` (now 8-arg); the `entityPreview` case passes `envelope.KeyIsMinor ?? false`.
- **`GenerateExercise.Generate`/`Build`** (all 3 overloads) take `bool keyIsMinor = false` → `new Key(pc, keyIsMinor)`.
- **`ContentCrudHandler.Preview`** takes `bool keyIsMinor = false` → `new Key(pc, IsMinor: keyIsMinor)`.
- **`Program.cs`** handlers forward `req.KeyIsMinor` / the preview flag.

Fixed the 7 existing `EntityPreviewRequested` subscriber lambdas in `WebMessageRouterContentTests` to the new 8-arg shape. Tests: `Build_MinorKey_CarriesMinorKeyOverride` (A-minor `KeyOverride`), `Preview_MinorKey_RendersInMinor` (`tonality: minor` + `keyIsMinor` → `\ks aminor`), `EntityPreview_CarriesKeyIsMinor` (envelope parse). Core suite **1008/1008 green**; Desktop builds clean.

Deferred to **8b**: the JS mode toggle in `harmony-controls-component.js` + seeding the mode from a song's key. `loadExercise` re-key mode is a separate follow-up.

## Step 9 — harmony-controls offers minor keys; carry isMinor through bridge → Features so a minor key realizes on Score and Sheet.

8b — the UI. `harmony-controls-component.js`: added a **major/minor mode `<select>`** grouped beside the Key tonic control; a mode change fires the same live re-render as a tonic change. `getDefinition()` now emits `keyIsMinor` (`keyModeSel.value === "minor"`). Exposed `seedKeyMode(isMinor)` and seeded the toggle on a harmony switch from `item.initialKeyIsMinor` (defaults major) — forward-compatible with a minor song once the content-list payload carries the mode.

`app.js`: `onGenerate` and `replayScoreRequest` (the live key/feel re-render) both include `keyIsMinor: def.keyIsMinor` in the envelope, so the mode reaches the bridge → 8a threading → `new Key(pc, isMinor)`.

Verified: `node --check` on both JS files passes; Desktop builds clean; the C# path is covered by 8a's tests (1008/1008). End-to-end in-app confirmation (pick a minor key + a minor progression → play) is a manual dogfood — the whole wire is connected and each seam is tested.

**Deferred follow-ups (noted):** (1) the content-list payload gaining `initialKeyIsMinor` (+ a progression's `tonality`) so a harmony switch *auto*-selects minor mode — the JS seed already reads it; (2) the Content-editor preview (`content-crud.js`) minor mode; (3) `loadExercise` re-key mode. None block picking a minor key on the Practice page.
