---
type: done
id: pl_01KXTWQRT3DS5H0XYC6349AHTJ-done
title: Done — Seeded-catalog golden oracle — authored reference doc + catalog-driven analyzer test
status: done
created: 2026-07-18
version: 2
tags: []
parent_id: pl_01KXTWQRT3DS5H0XYC6349AHTJ
requires_load: []
---
# Done — Seeded-catalog golden oracle — authored reference doc + catalog-driven analyzer test

## Step 1 — Author the golden-oracle reference doc loom/refs/harmonic-analysis-oracle-reference.md: for every seeded default-pack progression across BOTH sets (major-frame: ii_v_i, major_turnaround, secondary_dominant_turnaround, circle_secondary_dominants, tritone_sub_ii_v_i, tadd_dameron_turnaround, borrowed_iv, mixolydian_bvii, aeolian_cadence, chromatic_passing_dim, 12bar_blues, jazz_blues_turnaround, jazz_blues_standard; minor-home: minor_ii_v_i, andalusian_cadence, natural_minor_i_iv_v, harmonic_minor_i_iv_v, minor_turnaround, aeolian_loop, picardy_cadence, minor_12bar_blues), a per-progression section with a stable machine-readable table (row per chord: degree, realized chord, expected Category, Target, SourceMode) reasoned from theory. Resolve explicitly the theory judgments the hand-built fixtures never touched — e.g. andalusian_cadence's final `5` (major Phrygian-dominant V vs natural-minor v after realization) and the dominant-blues must-not-over-label rows (I7/IV7 = Chromatic, V7 = Diatonic).

Authored `loom/refs/harmonic-analysis-oracle-reference.md` (id `rf_01KXTXCBDA7Q6355VZBH0WCFE5`) — the hand-reasoned golden oracle for all 21 seeded progressions.

**Grounding (not guessed):** minor-set realized chords come from the existing `ProgressionSeedTests.MinorProgression_RealizesToExpectedChordsInAMinor` assertions (e.g. `andalusian_cadence → Am G F E`); major-set chords from the degree+quality DSL tokens realized in C major under the DSL's "a bare degree is a **major** chord" default (confirmed in `chordflow-dsl-reference.md` and cross-checked by the seed test: harmonic_minor's bare `5` → E major vs natural_minor's explicit `5-` → Em).

**Pinned realize keys:** major-frame set → C major, minor-home set → A minor.

**Resolved theory judgments (the ones hand-built fixtures never touched):**
- `andalusian_cadence` final `5` → E **major** (Phrygian-dominant / harmonic-minor V) ⇒ Diatonic, not Borrowed.
- `picardy_cadence` final `1` → A **major** in A minor ⇒ Borrowed(Major) — the Picardy third.
- Major `12bar_blues`/`jazz_blues_*`: `I7`/`IV7` ⇒ Chromatic (blues ruling — tonic never V/IV), only `V7` Diatonic; the must-not-over-label case.
- Minor `12bar_blues`: `i7`/`iv7` are genuine diatonic minor sevenths ⇒ all-Diatonic (native minor, no relative-major shortcut).
- `tadd_dameron_turnaround`: under v1's context-free rules only `♭II7→I` is TritoneSub; `Eb7`(♭III7) reads Chromatic, `Abmaj7` reads Borrowed(Minor) — noted as a later resolution-aware refinement (EX5).
- `chromatic_passing_dim` `#i°7`: Category SecondaryLeadingTone / target ii; the analyzer's honest `Function` spells it `♭IIdim7` (pitch-based degree table) — documented enharmonic note. The catalog test asserts Category/Target/SourceMode, not Function.

Includes a machine-parse contract section (id-backticked `###` headings + the first pipe table; `—` = null; parser keys on the `Category`/`Target`/`SourceMode` header names) so step 2 can read this doc as the single source of expectations, and a sync-obligation note.

## Step 2 — Add the catalog-driven golden test (tests/ChordFlow.Core.Tests/HarmonicAnalyzerCatalogTests.cs): a test-side helper loads each seeded progression .dsl, reads its tonality: header, and realizes it via Transposer into IReadOnlyList<(Chord, Key)> in the pinned key (major-frame → C major, minor-home → A minor); the test parses the step-1 reference doc (located by walking up to the repo root) as the single source of expected sequences and asserts HarmonicAnalyzer.Analyze reproduces each (Category, Target, SourceMode) exactly for every chord — covering both sets incl. the dominant-blues must-not-over-label rows, minor iiø-V-i, harmonic-minor V, and Picardy. Include a completeness guard that enumerates the default-pack progressions folder and fails if any .dsl lacks an oracle entry. Full Core suite green.

Added `tests/ChordFlow.Core.Tests/HarmonicAnalyzerCatalogTests.cs`.

- **Realize adapter (test-side, C2):** `CatalogHeader.Parse` → `ProgressionParser.Parse(…, home: meta.Tonality)` → `Transposer.RealizeBars(prog, PinnedKey)` flattened over `bar.Spans` → concrete `(Chord, Key)`. Uses `RealizeBars` (not the one-chord-per-bar `Transposer.Realize`) so multi-chord bars like `17_67` contribute **every** chord — matching the oracle's flat per-chord sequence. Pinned keys: major-frame → C major, minor-home → A minor.
- **Golden assertion** (`SeededProgression_AnalyzesToTheOracle`, `[Theory]` over all seeded progressions): parses the step-1 reference doc (located by walking up from `AppContext.BaseDirectory` to the repo `loom/refs/…`) as the single source of expected `(Category, Target, SourceMode)` and asserts `HarmonicAnalyzer.Analyze` reproduces each row; row-count guard ensures one oracle row per realized chord.
- **Completeness guard** (`OracleAndCatalog_AreInLockstep_NoOrphansEitherWay`): every seeded progression has an oracle section and vice-versa — a new `.dsl` can't silently escape analysis, and a stale oracle entry is caught.
- **Engine dump** (`EmitActualEngineOutput_ForReview`): writes the actual analyzer output per chord to `harmonic-analysis-oracle.actual.md` in the test output; that output was appended into the reference doc's "Engine output (actual — verified)" section for Rafa's independent review (`####`/unbackticked headings so the parser ignores it).

**Oracle-parser bug found & fixed during the run:** the parser only reset the current section on `### ` headings, so the appended `####`-heading engine tables were consumed as extra rows of the last oracle section (`Enum.Parse` crash on a mis-indexed cell). Fixed to treat **any** heading (`#`-prefixed) as a section boundary; only a backtick-bearing `### ` starts a new oracle section.

**Result:** full Core suite green — **1045/1045** (23 in the new catalog class: 21 progression theories + completeness guard + engine dump). The analyzer reproduces the hand-reasoned oracle exactly across both sets, including the dominant-blues must-not-over-label case and the minor-tonic cases (harmonic-minor V, iiø7, Picardy, borrowing) — validating minor end-to-end over real seeded content.
