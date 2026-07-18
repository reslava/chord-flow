---
type: plan
id: pl_01KXTWQRT3DS5H0XYC6349AHTJ
title: Seeded-catalog golden oracle — authored reference doc + catalog-driven analyzer test
status: done
created: 2026-07-18
updated: 2026-07-18
version: 1
design_version: 3
req_version: 3
tags: []
parent_id: de_01KXQFJYNF0D10R9B5VR3JP930
requires_load: []
target_version: 0.1.0
steps:
  - id: author-the-golden-oracle-reference-doc
    order: 1
    status: done
    description: "Author the golden-oracle reference doc loom/refs/harmonic-analysis-oracle-reference.md: for every seeded default-pack progression across BOTH sets (major-frame: ii_v_i, major_turnaround, secondary_dominant_turnaround, circle_secondary_dominants, tritone_sub_ii_v_i, tadd_dameron_turnaround, borrowed_iv, mixolydian_bvii, aeolian_cadence, chromatic_passing_dim, 12bar_blues, jazz_blues_turnaround, jazz_blues_standard; minor-home: minor_ii_v_i, andalusian_cadence, natural_minor_i_iv_v, harmonic_minor_i_iv_v, minor_turnaround, aeolian_loop, picardy_cadence, minor_12bar_blues), a per-progression section with a stable machine-readable table (row per chord: degree, realized chord, expected Category, Target, SourceMode) reasoned from theory. Resolve explicitly the theory judgments the hand-built fixtures never touched — e.g. andalusian_cadence's final `5` (major Phrygian-dominant V vs natural-minor v after realization) and the dominant-blues must-not-over-label rows (I7/IV7 = Chromatic, V7 = Diatonic)."
    files_touched: [loom/refs/harmonic-analysis-oracle-reference.md]
    blocked_by: []
    satisfies: [IN11]
  - id: catalog-driven-golden-test-completeness-guard
    order: 2
    status: done
    description: "Add the catalog-driven golden test (tests/ChordFlow.Core.Tests/HarmonicAnalyzerCatalogTests.cs): a test-side helper loads each seeded progression .dsl, reads its tonality: header, and realizes it via Transposer into IReadOnlyList<(Chord, Key)> in the pinned key (major-frame → C major, minor-home → A minor); the test parses the step-1 reference doc (located by walking up to the repo root) as the single source of expected sequences and asserts HarmonicAnalyzer.Analyze reproduces each (Category, Target, SourceMode) exactly for every chord — covering both sets incl. the dominant-blues must-not-over-label rows, minor iiø-V-i, harmonic-minor V, and Picardy. Include a completeness guard that enumerates the default-pack progressions folder and fails if any .dsl lacks an oracle entry. Full Core suite green."
    files_touched: [tests/ChordFlow.Core.Tests/HarmonicAnalyzerCatalogTests.cs]
    blocked_by: [author-the-golden-oracle-reference-doc]
    satisfies: [IN12, IN8, IN10, C2]
---
# Seeded-catalog golden oracle — authored reference doc + catalog-driven analyzer test

## Goal

Deliver the seeded-catalog golden oracle for `HarmonicAnalyzer` — the piece `IN10` anticipated, now unblocked because both the major-frame set (plan-002) and the minor-home set (`domain/minor-progressions`) exist as default-pack content. This is a **test + docs increment only**: the analyzer itself already handles minor natively (design D4 — minor from day one: harmonic-minor V, leading-tone vii°, Picardy, borrowing into minor, all covered by hand-built C-minor tests), so there is **no analyzer change**. Step 1 authors a human-reasoned golden-oracle **reference doc** (IN11) giving the expected `(Category, Target/SourceMode)` sequence for every seeded progression in both sets — the single authored source of truth, reasoned from theory rather than snapshotted from code. Step 2 adds a catalog-driven golden test (IN12) that realizes each progression via `Transposer` into `(Chord, Key)` — the major-frame set in C major, the minor-home set in A minor (matching the existing `MinorProgression_RealizesToExpectedChordsInAMinor` precedent) — and asserts the analyzer reproduces the oracle exactly, with a completeness guard so no seeded `.dsl` escapes coverage. Out of scope (thread 3, EX6/EX7): any consumer/overlay wiring and the `IN9` subsumption verification / `ChordSheetBuilder.RomanFunction` retirement. The realize adapter is a test-side helper (C2).

---

## Steps

| Done | # | Step | Files touched | Blocked by | Satisfies |
|---|---|---|---|---|---|
| ✅ | 1 | Author the golden-oracle reference doc loom/refs/harmonic-analysis-oracle-reference.md: for every seeded default-pack progression across BOTH sets (major-frame: ii_v_i, major_turnaround, secondary_dominant_turnaround, circle_secondary_dominants, tritone_sub_ii_v_i, tadd_dameron_turnaround, borrowed_iv, mixolydian_bvii, aeolian_cadence, chromatic_passing_dim, 12bar_blues, jazz_blues_turnaround, jazz_blues_standard; minor-home: minor_ii_v_i, andalusian_cadence, natural_minor_i_iv_v, harmonic_minor_i_iv_v, minor_turnaround, aeolian_loop, picardy_cadence, minor_12bar_blues), a per-progression section with a stable machine-readable table (row per chord: degree, realized chord, expected Category, Target, SourceMode) reasoned from theory. Resolve explicitly the theory judgments the hand-built fixtures never touched — e.g. andalusian_cadence's final `5` (major Phrygian-dominant V vs natural-minor v after realization) and the dominant-blues must-not-over-label rows (I7/IV7 = Chromatic, V7 = Diatonic). | loom/refs/harmonic-analysis-oracle-reference.md | — | IN11 |
| ✅ | 2 | Add the catalog-driven golden test (tests/ChordFlow.Core.Tests/HarmonicAnalyzerCatalogTests.cs): a test-side helper loads each seeded progression .dsl, reads its tonality: header, and realizes it via Transposer into IReadOnlyList<(Chord, Key)> in the pinned key (major-frame → C major, minor-home → A minor); the test parses the step-1 reference doc (located by walking up to the repo root) as the single source of expected sequences and asserts HarmonicAnalyzer.Analyze reproduces each (Category, Target, SourceMode) exactly for every chord — covering both sets incl. the dominant-blues must-not-over-label rows, minor iiø-V-i, harmonic-minor V, and Picardy. Include a completeness guard that enumerates the default-pack progressions folder and fails if any .dsl lacks an oracle entry. Full Core suite green. | tests/ChordFlow.Core.Tests/HarmonicAnalyzerCatalogTests.cs | author-the-golden-oracle-reference-doc | IN12, IN8, IN10, C2 |
---

### Legend

| Symbol | Meaning |
|--------|---------|
| ✅ | Done |
| 🔄 | In Progress |
| 🔳 | Pending |
| ❌ | Cancelled |

<!-- step:author-the-golden-oracle-reference-doc -->
### Step 1 — Author the golden-oracle reference doc

The oracle is HUMAN-REASONED, not `HarmonicAnalyzer` output pasted back — that is what makes it a golden oracle rather than a snapshot. Pin the realize keys in the doc: major-frame set → C major, minor-home set → A minor. Use a stable per-progression table format (e.g. `| # | degree | chord | Category | Target | SourceMode |`) so step 2 can parse it as the single source of the expectations. Created via loom_create_reference (loom/refs is gate-excluded but still authored through MCP).

<!-- step:catalog-driven-golden-test-completeness-guard -->
### Step 2 — Catalog-driven golden test + completeness guard

The realize adapter is test-side per C2 — no Music-layer dependency on Song/Realized types. Parsing the ref doc keeps the oracle single-sourced (the doc drives the assertions); if markdown parsing proves brittle, the fallback is to mirror the oracle as a C# table guarded by the same completeness check, but parse is preferred. No analyzer change is expected — a red row means either an oracle theory error (fix step 1) or a genuine analyzer gap (surface it, do not paper over it).
