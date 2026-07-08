---
type: done
id: pl_01KX0J0W78NGPRB7TMH8TM1P6S-done
title: Done — Explicit per-chord voicing references in the DSL
status: done
created: 2026-07-08
version: 10
tags: []
parent_id: pl_01KX0J0W78NGPRB7TMH8TM1P6S
requires_load: []
---
# Done — Explicit per-chord voicing references in the DSL

## Step 1 — Add a `VoicingSpec` value type (a literal grip with an optional `root:<string>[@<fret>]` anchor clause, OR a source-qualified reference `<source>:<id>` with source ∈ u|a|<package>) and a small shared parser + writer. Reuse the existing fret-token parsing; add the `root:` clause (voiced `root:6`, phantom `root:6@8`). Bare grip == `c:` grip; a reference is anything carrying a non-`c` `source:` word.

Added the shared voicing-spec value grammar.

- **New `VoicingSpec.cs`** — abstract `VoicingSpec` with `GripSpec(Positions, MutedStrings, Anchor?)` and `ReferenceSpec(Source, Id)`, plus `GripAnchor(String, Fret?)` (`Fret == null` = voiced root; set = phantom root on a muted string). Purely syntactic — authored frets kept verbatim; normalize-to-C is deferred to realization (step 5).
- **`VoicingDslParser.ParseSpec`** — reuses the existing `ParseFrets`. Discriminates by the first token's `<word>:` prefix: `c` → literal grip (bare grip is sugar for `c:`), `root` → anchor clause, anything else with a colon → reference. `root:<string>[@<fret>]` parsed by `ParseGripAnchor`. Reference id may be spaced (`u: C6`) or glued (`swing:C6`).
- **`VoicingDslWriter.SpecToDsl`** — canonical inner text: bare grip (+ `root:` clause) or `<source>: <id>`.
- Tests in `VoicingDslParserTests.cs`: bare/`c:` grip, voiced + phantom root, references, writer round-trip (asserts writer idempotence — record equality can't compare the `List<>` members), and malformed-input throws. 43/43 green.

Note: `c` and `root` are reserved as spec prefixes (a package literally named `c`/`root` can't be referenced) — acceptable edge, will document in step 9.

## Step 2 — Lex a trailing `{ voicing-spec }` token that binds to the immediately preceding chord (whitespace-optional; `{` can't start a bar/chord) into an optional annotation field on the parsed chord/span. Purity guard: accept annotations only in inline-in-Song parse context; a standalone/stored progression parse rejects them (fail-loud). Errors: orphan `{`, annotation on a stored progression.

ProgressionParser now lexes the per-chord `{…}` annotation and guards purity.

- **`ChordSpan`** gained `string? VoicingAnnotation = null` — the raw inner text of `{…}`, opaque (design D9). Backward-compatible (3rd positional param defaults null; existing 2-arg constructions + equality unaffected).
- **`TokenizeBars`** replaces the naive `Split(' ')`: a brace-depth scanner keeps `{…}` atomic (grips have internal spaces) and folds an annotation-only token (`{…}`) onto the preceding chord token, so `17 {…}` and `17{…}` are one bar either way. Orphan `{`, unmatched/unclosed braces → throw.
- **`ParseChord`** peels a trailing `{…}` off the chord token before the `:slots` split (a grip's `root:6@8` carries a colon), stores the trimmed inner text, and rejects it when `allowVoicingAnnotations` is false (the purity guard, IN7). Malformed/empty/double annotation → throw.
- **`Parse`** gained `bool allowVoicingAnnotations = false`; stored/standalone callers keep the default (reject), Song inline-parse will pass true (step 3).
- Tests: purity rejection, raw-storage of grip + reference, binding to the right chord (multi-chord bar, after slots), no extra bar, and malformed cases. 56/56 green.

Deferred: the spec *value* syntax isn't validated here (Music can't call the Instruments parser) — it fails loud at realization (step 5/6).

## Step 3 — Parse `voice <selector> = <voicing-spec>` in the Song definitions section into a selector→spec map on the Song. Selectors: quality-scoped `*<quality>` (`*7`, `*m7`, `*` = major triad) and degree-scoped `[accidental]<degree><quality>` (`17`, `#4dim7`, `2-7`). Errors: duplicate selector ('defines voicing for … more than once'). Coexist with the `feel`/`tempo` space-keywords; canonical minor-7 token `6-7`.

Song `voice` directive + inline annotations wired.

- **New `VoiceSelector.cs`** (Music.Songs) — `record VoiceSelector(RomanDegree? Degree, Quality Quality)` with `ForDegree`/`ForQuality`; value-equality is the dup-key and the resolver's most-specific lookup key.
- **`Song`** gained `IReadOnlyDictionary<VoiceSelector, string> Voices` (selector → **raw** spec text, opaque per D9); `FromSections` takes an optional `voices` (defaults empty).
- **`ProgressionParser`** exposed two public helpers so the selector grammar *is* a chord token, single-sourced: `ParseChordSymbol` (degree-scoped) and `ParseQualitySuffix` (`*<quality>`).
- **`SongParser`**: `IsVoiceDirectiveLine` intercepts `voice ` before `TrySplitDefinition` (it uses `=` like a definition); `ParseVoiceDefault`/`ParseVoiceSelector` build the map (`*<quality>` wildcard incl. bare `*`=Major, or a degree chord symbol), duplicate selector → throw (C6). Inline progressions now parse with `allowVoicingAnnotations: true` (IN1/IN7). `voice` added to reserved part names.
- **`StripComment` fix**: `#` is now a comment only when *not* immediately followed by a digit — so `voice #4dim7` (and, as a bonus, inline `#4dim7` in a song progression, a latent bug) survive. Existing `# text` comments (space after `#`) unaffected.
- Tests: quality/degree/`#4`/bare-`*` selectors, reference spec verbatim, duplicate throw, malformed selectors, `voiceleading` is a part not a directive, inline annotation reaches the InlineProgression's span. **Full suite 857/857 green** (InstrumentBoundary intact).

## Step 4 — Carry the per-chord annotation onto `RealizedSpan` and the song `voice` map onto `RealizedSong` via `SongExpander`. Ensure each realized span exposes its **scale degree** (for degree-scoped matching) alongside the transposed `Chord`, since the concrete post-transpose chord no longer carries its degree.

Annotation + voice-map + degree now flow through realization.

- **`RealizedSpan`** gained `RomanDegree Degree = default` and `string? VoicingAnnotation = null` (appended, defaulted — the only constructor is `Transposer`, and the legacy render paths ignore them). Degree is preserved because the post-transpose concrete `Chord` has lost its scale degree, which degree-scoped `voice` matching needs.
- **`Transposer.RealizeBars`** now passes `span.Degree` and `span.VoicingAnnotation` into each `RealizedSpan`.
- **`RealizedSong`** gained `Voices { get; init; }` (defaults to a shared empty dict) so existing `new RealizedSong(sections)` sites are unchanged; **`SongExpander.Expand`** sets `{ Voices = song.Voices }`.
- Tests: voice map reaches `RealizedSong.Voices`; per-chord annotation reaches the right `RealizedSpan` (and a plain chord stays null); degree survives transposition (I7 in key A → root A=9 but Degree still 1/Dominant7). **Full suite 860/860** — renderer unaffected (it reads only Chord/StartTick/DurationTicks).

## Step 5 — Realize a grip `VoicingSpec` to a movable `Voicing`: (1) harmony-fixed anchor (per-chord / degree-scoped — root from degree+key), (2) inferred anchor (quality-scoped voiced — spell the shape), (3) declared `root:` / phantom `@fret` for rootless or ambiguous shapes. Canonicalize-to-C and transpose to the sounding root. First-class rootless (`@fret`) support. Error when a muted root lacks `@<fret>` or an ambiguous grip has no `root:`.

`VoicingRealizer.RealizeGrip(GripSpec, PitchClass targetRoot)` → `Voicing?`.

- **Anchor**: explicit `root:` wins — `root:S@F` is a phantom root (S may be muted, the rootless case), `root:S` reads the fret off the grip on S (muted → `FormatException` telling the author to use `@`). No clause → **bass inference**: the lowest-pitched sounded string (scan 6→1) is the root; correct for root-position grips, inversions/rootless must declare `root:` (documented contract).
- **Transpose**: pivot the anchor's pitch class (`Fretboard.PitchClassAt`) onto `targetRoot` (0..11 up-shift), then octave-fold to the lowest non-negative placement — byte-identical math to the authored-voicing `Realize`. So a grip authored WYSIWYG for a chord stays verbatim at that root (shift 0) and slides as a whole under any key change. Returns `null` when the shape can't fit the 0..15 window.
- Reuses `Fretboard`; needs only `targetRoot` (quality not required with bass inference).
- Tests: verbatim-at-authored-root, shift-to-another-root, explicit-voiced-root == bass-inference, rootless phantom root transposes without sounding the root, voiced-root-on-muted-string throws. 11/11 in the realizer suite.

## Step 6 — Add a new `IVoicingReferenceSource` port (Features) that resolves a source-qualified reference to a `Voicing` at the chord's root by id: `u:` = an origin-aware user-row lookup, `<packageId>:` = that pack's row, `a:` = the engine `auto:shell:dom7:E` id (`AutomaticVoicingId`) derived on the fly. Add `VoicingStore.FindBySource(id, source, packageId)` (origin-strict, unlike the tier-collapsing `Find`). Return null on a miss/filtered-out source so the cascade (step 7) can fail loud (IN6). The `a:` id format is the engine's structured `auto:<family>:<quality>:<shape>`.

Reference resolution built (option A — full references, no half-feature).

- **New `IVoicingReferenceSource`** (Features) — `Voicing? Resolve(source, id, chord)`; null on miss so the cascade fails loud (IN6).
- **New `VoicingReferenceSource`** — origin-strict over id-tagged rows: `u:` → user row, `<packageId>:` → that pack's row (source AND pack must match — a `u:` id never matches a package row, IN6), `a:` → parse the engine `auto:<family>:<quality>:<shape>` id and derive the grip at the chord's root (miss/unplaceable → null). Stored shapes realized via `VoicingShape.Realize`. Built from a pure row list (`From(store)` / `Empty`), so it unit-tests with no DB.
- **`VoicingStore.LoadShapesWithIds()`** — the id-carrying, source-tagged, no-collapse peer of `LoadShapesBySource` (the tier-collapsing `Find` couldn't distinguish sources).
- `a:` id format confirmed = the engine's `auto:shell:dom7:E` (`AutomaticVoicingId`), per Rafa.
- Tests (DB-free): user resolve+realize, unknown id → null, user-id-that-is-only-package → null (origin-strict), package source+pack match/mismatch, automatic derive, malformed auto id → null. 6/6 green.
- The `CompingResolver` wiring (new dependency from `ExerciseRendering`) is Step 7, where the resolver is modified anyway.

## Step 7 — In `CompingResolver.Resolve`, before the ranking fill, apply per span the most-specific-wins cascade: per-chord `{}` annotation (steps 5/6) › degree-scoped `voice <deg><qual>` › quality-scoped `voice *<qual>` › existing candidate/ranking fill. Override resolution keys per-span (not the by-`Chord` candidate cache, since two identical chords may differ in annotation). The fill path is unchanged — pure addition in front of it.

The most-specific-wins cascade, wired end-to-end.

- **`CompingResolver.Resolve`** gained `IVoicingReferenceSource? references = null` and now runs, per span: (1) a per-chord `{…}` annotation → a **per-occurrence** override; (2) degree-scoped `voice` default; (3) quality-scoped `voice` default; (4) the existing candidate/ranking fill (factored into `Fill`). Explicit tiers go through `ResolveSpec` (parse the opaque spec → `RealizeGrip` for a grip / `references.Resolve` for a reference) and **fail loud** (IN6) on a malformed spec or unresolvable reference. `context.PreviousGrip` updates for explicit grips too, so the fill's voice-leading stays coherent.
- **Per-occurrence keying** (the crux): `CompingPlan` now carries a `Chord`-keyed map (defaults + fill, unchanged) **plus** a `RealizedSpan`-keyed override map for annotations; `For(RealizedSpan)` checks the override first. So annotating one occurrence never leaks to the other identical chords. Kept `For(Chord)` + the single-arg ctor, so existing call sites/tests are untouched.
- **`RealizedBar.SpanCovering(tick)`** added; **`AlphaTexRenderer`** threads the span (not just the chord) into `RenderBar`/`RecordChordChange` and looks up `plan.For(span)` — the one renderer change, behavior-identical when there are no annotations.
- **Wiring**: `references` threaded through `ExerciseRendering` / `LoadScoreEnvelope.From`, and every composition site that builds `StoredVoicingSource.From(new VoicingStore(db))` now also builds `VoicingReferenceSource.From(...)` (Program.cs, ExerciseLibrary, GenerateExercise, ContentCrud) — so `u:`/`pkg:` references resolve in the real app.
- Tests: per-occurrence override, degree default (all occurrences), quality default (transposed per root, C7 fret6=8 → F7 fret6=1), degree-beats-quality, annotation-beats-default, fail-loud on bad reference / malformed grip. **Solution builds; full suite 878/878.**

## Step 8 — Serialize per-chord `{}` annotations and `voice` directives back through the Song/Progression writers so parse → serialize → parse is a fixed point. Bare-grip is the canonical grip output; references serialize as `{source: id}` / `voice sel = source: id`; `root:` clause preserved.

Round-trip (IN8) — no new emitter needed; verified the two real surfaces.

- There is **no structural Progression/Song DSL writer** (confirmed by grep) — those DSLs are stored **verbatim** and re-parsed, so an annotated Song survives store→reload by construction. The only structural serializer is `VoicingDslWriter.SpecToDsl`, which is the actual serialization surface for a `{…}` annotation.
- Tests: `AnnotatedSong_SurvivesTextualRoundTrip` (voice map + inline `{…}` preserved through a re-parse of the same text, mirroring the `feel` round-trip test) and `InlineAnnotationSpec_RoundTripsThroughTheWriter` (the stored raw annotation → `ParseSpec` → `SpecToDsl` is byte-identical, for grip+voiced-anchor, rootless phantom anchor, and reference). The `VoicingSpec` writer round-trip itself was already locked in Step 1. 52/52 in SongParserTests.

## Step 9 — Document both placements in `chordflow-dsl-reference`: the shared voicing-spec grammar (grip, `root:`/phantom anchor, rootless, references), the per-chord `{}` annotation with the purity rule, the `voice` default with `*` wildcard + degree selectors, the resolution cascade, and the new error messages. Use canonical tokens (`6-7`).

Reference docs updated (both, per CLAUDE-LOCAL always-update).

- **`chordflow-dsl-reference`** (end-user): new "Pinning voicings — per-chord `{…}` and the `voice` default" subsection in the Song DSL section — the shared voicing-value table (grip, `c:`, `root:`/phantom rootless anchor, `u:`/`a:`/`pkg:` references), both placements, the most-specific-wins order, and the Song-only purity rule; plus three new entries in the Song "Common errors" list.
- **`chordflow-domain-model-reference`** (kernel): `ChordSpan` row updated with the opaque `VoicingAnnotation`; the realization-flow line updated with the cascade order; a new "Explicit voicings" bullet covering `Song.Voices`/`VoiceSelector`, `RealizedSpan` (degree + annotation), the Features-layer parse/realize/reference pieces, and `CompingPlan`'s per-occurrence override map.

## Step 10 — An annotated 12-bar blues — mixing a per-chord `{}` pin, a `voice *7` default, a `voice #4dim7`, and one rootless grip (`root:…@…`) — renders the pinned grips on the now/next fret-boxes of the fretboard UI page. Visual confirmation each cascade tier resolves to the intended shape (guitar-weave dogfood rule).

Dogfood — the annotated blues comps its pinned grips into the now/next-fretboard feed.

- **New default-pack song `songs/explicit_voicings_demo.dsl`** (data drop, auto-imported by `PackReader`): a 12-bar blues exercising every tier — `voice *7` (movable A-shape dom7), `voice #4dim7 = a: auto:caged:dim7:A` (engine reference), bar 1's I7 pinned to an E-shape grip `{8 10 8 9 8 8}`, and bar 11's I7 a **rootless** shell `{x x 2 3 x x root:6@8}` (needs the phantom anchor since the bass isn't the root). Selectable in the app for a manual visual run.
- **End-to-end test** `Generate_ExplicitVoicingsDemo_CompsThePinnedGripsIntoTheSchedule` (EngineComping-style, real SQLite + DefaultPack): generates the song and asserts the **chord schedule** (the exact data the now/next fret-boxes draw) carries the pinned E-shape (low E @ fret 8) for bar 1's C7, the `voice *7` default (low E muted, root string 5 @ 3) for a later non-annotated C7 — proving per-occurrence — and a voiced #IVdim7 from the `a:` reference. All grips resolve through the full pipeline (SongParser → SongExpander → cascade → renderer → schedule).
- **Full suite 883/883.**
