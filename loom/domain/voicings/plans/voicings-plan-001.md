---
type: plan
id: pl_01KTZWZPKHS2BHNAR7RABZPDDX
title: Voicings — authored content pillar (slice 1)
status: done
created: 2026-06-13
updated: 2026-06-13
version: 1
design_version: 1
req_version: 1
tags: []
parent_id: de_01KTXERD54E8GFPPNE19GMCPB1
requires_load: []
target_version: 0.1.0
actual_release: 0.5.0
steps:
  - id: dsl-parser-canonical-c-normalize
    order: 1
    status: done
    description: Voicing DSL + parser + `VoicingShape` entry type + canonical-C normalizer
    files_touched: [src/ChordFlow.Core/Domain/Voicings/VoicingShape.cs, src/ChordFlow.Core/Domain/Voicings/VoicingDslParser.cs, tests/ChordFlow.Core.Tests/Voicings/VoicingDslParserTests.cs]
    blocked_by: []
    satisfies: [IN1, IN2, C1, C3]
  - id: realize-transpose
    order: 2
    status: done
    description: "`Realize(shape, targetRoot)` — movable transpose, octave-fold, 0–15 guard"
    files_touched: [src/ChordFlow.Core/Domain/Voicings/VoicingRealizer.cs, tests/ChordFlow.Core.Tests/Voicings/VoicingRealizerTests.cs]
    blocked_by: []
    satisfies: [IN3, C1, C4]
  - id: voicingbook-lookup-ranked
    order: 3
    status: done
    description: "`CagedShape` familiarity rank + `VoicingBook.Lookup` (exact-quality, ranked, stored-first over a supplied entry set, strategy fallback)"
    files_touched: [src/ChordFlow.Core/Domain/Voicings/CagedShape.cs, src/ChordFlow.Core/Domain/Voicings/VoicingBook.cs, tests/ChordFlow.Core.Tests/Voicings/VoicingBookTests.cs]
    blocked_by: []
    satisfies: [IN4, IN5, C1]
  - id: persistence-migration
    order: 4
    status: done
    description: "`VoicingEntity` + `Voicings` EF table + migration + repository"
    files_touched: [src/ChordFlow.Core/Persistence/VoicingEntity.cs, src/ChordFlow.Core/Persistence/ChordFlowDbContext.cs, src/ChordFlow.Core/Persistence/Migrations/, src/ChordFlow.Core/Persistence/VoicingRepository.cs]
    blocked_by: []
    satisfies: [IN6, C2, C3, C5]
  - id: stored-first-integration
    order: 5
    status: done
    description: Wire repository → `VoicingBook` (stored-first end-to-end, stored-shadows-strategy)
    files_touched: [src/ChordFlow.Core/Domain/Voicings/VoicingBook.cs, src/ChordFlow.Core/Features/, tests/ChordFlow.Core.Tests/Voicings/VoicingBookIntegrationTests.cs]
    blocked_by: []
    satisfies: [IN4, C2]
  - id: reference-doc-sync
    order: 6
    status: done
    description: Ref-sync — update `chordflow-domain-model-reference.md` (+ DSL ref if the public surface changed)
    files_touched: [loom/refs/chordflow-domain-model-reference.md, loom/refs/chordflow-dsl-reference.md]
    blocked_by: []
    satisfies: [IN8]
---
# Voicings — authored content pillar (slice 1)

## Goal

Implement the authored-voicing content pillar end to end: a fixed-fret chord-chart DSL parsed into the existing `Voicing` value type, normalized to a canonical-C anchor on save; a `Realize` transpose that slides a canonical-C shape to any of the 12 roots within the 0–15 window; a stored-first, exact-quality `VoicingBook.Lookup` returning a neck-position-ranked list (CAGED-familiarity tiebreak) with `BeginnerShellStrategy` as fallback; `VoicingEntity` + `Voicings` EF table + migration; and a CRUD screen uniform with the other DSL-backed entities. Pure domain (parser/normalizer/Realize/VoicingBook) lives in `ChordFlow.Core/Domain/` reusing `PitchClass` + `Fretboard` (no first-class `Interval` type); persistence in `ChordFlow.Core/Persistence/`; UI in `ChordFlow.Desktop`. Anchored on the locked req; `fixed` flag, drone/pedal voicings, alternate tunings, `QualitySimplifier`, and difficulty-band heuristics are explicitly out of scope (deferred, additive).

---

## Steps

| Done | # | Step | Files touched | Blocked by | Satisfies |
|---|---|---|---|---|---|
| ✅ | 1 | Voicing DSL + parser + `VoicingShape` entry type + canonical-C normalizer | src/ChordFlow.Core/Domain/Voicings/VoicingShape.cs, src/ChordFlow.Core/Domain/Voicings/VoicingDslParser.cs, tests/ChordFlow.Core.Tests/Voicings/VoicingDslParserTests.cs | — | IN1, IN2, C1, C3 |
| ✅ | 2 | `Realize(shape, targetRoot)` — movable transpose, octave-fold, 0–15 guard | src/ChordFlow.Core/Domain/Voicings/VoicingRealizer.cs, tests/ChordFlow.Core.Tests/Voicings/VoicingRealizerTests.cs | — | IN3, C1, C4 |
| ✅ | 3 | `CagedShape` familiarity rank + `VoicingBook.Lookup` (exact-quality, ranked, stored-first over a supplied entry set, strategy fallback) | src/ChordFlow.Core/Domain/Voicings/CagedShape.cs, src/ChordFlow.Core/Domain/Voicings/VoicingBook.cs, tests/ChordFlow.Core.Tests/Voicings/VoicingBookTests.cs | — | IN4, IN5, C1 |
| ✅ | 4 | `VoicingEntity` + `Voicings` EF table + migration + repository | src/ChordFlow.Core/Persistence/VoicingEntity.cs, src/ChordFlow.Core/Persistence/ChordFlowDbContext.cs, src/ChordFlow.Core/Persistence/Migrations/, src/ChordFlow.Core/Persistence/VoicingRepository.cs | — | IN6, C2, C3, C5 |
| ✅ | 5 | Wire repository → `VoicingBook` (stored-first end-to-end, stored-shadows-strategy) | src/ChordFlow.Core/Domain/Voicings/VoicingBook.cs, src/ChordFlow.Core/Features/, tests/ChordFlow.Core.Tests/Voicings/VoicingBookIntegrationTests.cs | — | IN4, C2 |
| ✅ | 6 | Ref-sync — update `chordflow-domain-model-reference.md` (+ DSL ref if the public surface changed) | loom/refs/chordflow-domain-model-reference.md, loom/refs/chordflow-dsl-reference.md | — | IN8 |
---

### Legend

| Symbol | Meaning |
|--------|---------|
| ✅ | Done |
| 🔄 | In Progress |
| 🔳 | Pending |
| ❌ | Cancelled |

<!-- step:dsl-parser-canonical-c-normalize -->
### Step 1 — DSL, parser & canonical-C normalize

Parse `voicing <Chord>  shape:<C|A|G|E|D|…>  root:<6..1>  frets: <s6 … s1>` (x=muted, 0=open) onto `Voicing(Positions, BarreFret?, FirstFret?, MutedStrings?)` + `FretPosition`. Capture `shape` (CAGED family) + `root` (string sounding the root) as metadata on `VoicingShape`. Normalize any declared anchor to its **lowest non-negative C placement** (octave-fold up where a shape — e.g. the D-shape — sits below the nut at C) so each `(quality, shape)` has one canonical form; reuse `PitchClass`. Parse-error surface for the UI. Unit tests: round-trip, x/0 handling, below-nut octave-fold, dedup key.

<!-- step:realize-transpose -->
### Step 2 — Realize transpose

Add `semis = PitchClass.Interval(C → targetRoot)` to every fretted string (x stays muted); octave-fold (±12) so the lowest fretted note lands in the **0–15** window; return `null` if no placement fits. Derive `BarreFret`/`FirstFret` from the lowest fretted fret. Output reuses the existing `Voicing` value type — no new downstream type. Tests: open↔barre (`x32010` C-shape Cmaj → `x53232` Dmaj; `875558` G-shape Cmaj → `320003` Gmaj), out-of-window → null, barre derivation.

<!-- step:voicingbook-lookup-ranked -->
### Step 3 — VoicingBook.Lookup ranked

Exact-quality match (quality == chord.Quality — `maj7` never returns `maj`), `Realize` each candidate to `chord.Root`, keep playable (0–15), return the **full ranked list**: sort by neck position, tiebreak by CAGED familiarity rank (pack-overridable metadata, seed **E A G C D**). `BeginnerShellStrategy` fallback when nothing stored; stored authored voicings **shadow** generated ones. Lookup is fed a parsed entry set (persistence wiring is step 5). Tests: exact-quality (no maj fallback), ranked order, familiarity tiebreak, shadow rule, empty→strategy.

<!-- step:persistence-migration -->
### Step 4 — Persistence + migration

`VoicingEntity(Id, Name, Dsl, Origin, Genre?, CreatedUtc)` — DSL-only (stored DSL is the canonical-C form; frets regenerated on load), mirrors `ProgressionEntity`. New `Voicings` table + EF migration. `Origin`/`Genre` adopt catalog metadata + provenance from the `packages` thread. Repository for CRUD + load.

<!-- step:stored-first-integration -->
### Step 5 — Stored-first integration

Feed `VoicingBook.Lookup` the parsed stored entries from `VoicingRepository` (parse DSL → `VoicingShape`), so a stored authored voicing for a quality shadows the generated one in the real pipeline. Keep `VoicingBook` itself pure (Core/Domain); the repository read happens at the feature seam. Integration test over a seeded entry.

<!-- step:reference-doc-sync -->
### Step 6 — Reference-doc sync

Map the new voicings DSL, `VoicingShape`, `Realize`, canonical-C normalize, and stored-first ranked `VoicingBook.Lookup` into the domain-model ref; add the voicing DSL to the public DSL ref. Per the contract this lands in the same unit of work as the domain code — listed as a discrete step so it is not skipped.
