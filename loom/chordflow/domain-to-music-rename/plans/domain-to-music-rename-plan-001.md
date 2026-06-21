---
type: plan
id: pl_01KVP3YRT1V27WJJ5PTVFJN3JH
title: Rename Domain → Music — Plan
status: done
created: 2026-06-21
updated: 2026-06-21
version: 1
design_version: 1
req_version: 2
tags: []
parent_id: de_01KVP2BZJANXQR4TD9DQ4DVECH
requires_load: []
target_version: 0.1.0
steps:
  - id: move-kernel-files-into-the-music
    order: 1
    status: done
    description: Move every src/ChordFlow.Core/Domain/** file into Music/{Harmony,Rhythm,Melody,Progression,Song}/ and Exercises/, rewrite each `namespace ChordFlow.Domain;` to its new namespace per the design move table, and add the intra-kernel `using`s so the moved types resolve each other. DSL parsers (ProgressionParser→Progression, RhythmPatternParser→Rhythm, SongParser/SongExpander→Song) and the IProgressionStore port→Music.Song travel with their types. Keep the ChordFlow. root prefix (C2). Build is intentionally red until step 2.
    files_touched: ["src/ChordFlow.Core/Domain/** (moved → src/ChordFlow.Core/Music/**)", src/ChordFlow.Core/Exercises/Exercise.cs, src/ChordFlow.Core/Exercises/Difficulty.cs]
    blocked_by: []
    satisfies: [IN1, IN2, IN3, IN4, C2]
  - id: rewire-all-consumers-usings-xml-doc
    order: 2
    status: done
    description: "Replace every `using ChordFlow.Domain;` and every `<see cref=\"Domain.*\"/>` cross-reference across Features/, Rendering/, Bridge/, Persistence/, the Desktop host, and tests/ with the specific new namespace(s) each file actually uses. End state: full solution builds and ALL existing tests pass."
    files_touched: ["src/ChordFlow.Core/Features/**", "src/ChordFlow.Core/Rendering/**", "src/ChordFlow.Core/Bridge/**", "src/ChordFlow.Core/Persistence/**", "src/ChordFlow.Desktop/**", "tests/ChordFlow.Core.Tests/**"]
    blocked_by: [move-kernel-files]
    satisfies: [IN5, C3]
  - id: resolve-seeddata-placement-consumer-check
    order: 3
    status: done
    description: Grep for runtime (src/) consumers of SeedData. If none remain (content now ships from the default pack), move it to a test/seed area; otherwise leave it in Music.Progression. Update any references and keep the build/tests green.
    files_touched: [src/ChordFlow.Core/Music/Progression/SeedData.cs (or moved to tests/seed area)]
    blocked_by: [rewire-consumers]
    satisfies: [IN8]
  - id: retarget-the-instrument-boundary-architecture-test
    order: 4
    status: done
    description: Retarget tests/.../Architecture/InstrumentBoundaryTests from the `ChordFlow.Domain` edge to `ChordFlow.Music` (no Music type may reference ChordFlow.Instruments). Keep Rendering→Instruments and Persistence→Instruments allowed.
    files_touched: [tests/ChordFlow.Core.Tests/Architecture/InstrumentBoundaryTests.cs]
    blocked_by: [rewire-consumers]
    satisfies: [IN6]
  - id: add-music-layering-architecture-tests
    order: 5
    status: done
    description: "Add NetArchTest rules locking the new boundaries: Music.Harmony references no sibling Music.* (it is the sink), and there are no dependency cycles among the Music.* namespaces. Set each sibling's allowed outward edges to the REAL references observed after the move (assert the observed graph, never an aspirational one — must not force any code change, EX1)."
    files_touched: [tests/ChordFlow.Core.Tests/Architecture/MusicLayeringTests.cs]
    blocked_by: [rewire-consumers, seeddata-placement]
    satisfies: [IN9]
  - id: sync-all-documentation
    order: 6
    status: done
    description: Update the three loom/refs docs (architecture, domain-model, dsl), loom/ctx.md, and README.md/CHANGELOG.md to the new namespaces, in this same unit of work (ref-sync contract).
    files_touched: [loom/refs/chordflow-architecture-reference.md, loom/refs/chordflow-domain-model-reference.md, loom/refs/chordflow-dsl-reference.md, loom/ctx.md, README.md, CHANGELOG.md]
    blocked_by: [seeddata-placement]
    satisfies: [IN7]
  - id: final-verification-isolated-commit
    order: 7
    status: done
    description: Full solution builds; all tests green (incl. the new layering tests); loom_validate clean; grep proves zero `ChordFlow.Domain` / `namespace ChordFlow.Domain` references remain in src/, tests/, the three refs, ctx.md, and README. Commit as its own isolated commit.
    files_touched: [(whole solution — verification only)]
    blocked_by: [retarget-arch-test, music-layering-tests, update-docs]
    satisfies: [C1, C2, C3, C4]
---
# Rename Domain → Music — Plan

## Goal

Execute the Domain → Music reorganization (idea scope (b)) as one isolated commit. Replace the single `ChordFlow.Domain` namespace with the flat-sibling family — `ChordFlow.Music.{Harmony,Rhythm,Melody,Progression,Song}` plus `ChordFlow.Exercises` — moving each kernel file to a folder that mirrors its new namespace (DSL parsers and the `IProgressionStore` port travel with their types), rewiring every consumer's `using`s and XML-doc cross-references, then locking the new boundaries in with architecture tests and syncing all documentation. Pure naming/structure: every type keeps its shape, behavior is unchanged (EX1), and the work lands green (build + all tests) and grep-clean of `ChordFlow.Domain`. The big mechanical move (step 1) and its rewire (step 2) are one logical unit — the build only returns green at the end of step 2; the remaining steps are additive and independently green.

---

## Steps

| Done | # | Step | Files touched | Blocked by | Satisfies |
|---|---|---|---|---|---|
| ✅ | 1 | Move every src/ChordFlow.Core/Domain/** file into Music/{Harmony,Rhythm,Melody,Progression,Song}/ and Exercises/, rewrite each `namespace ChordFlow.Domain;` to its new namespace per the design move table, and add the intra-kernel `using`s so the moved types resolve each other. DSL parsers (ProgressionParser→Progression, RhythmPatternParser→Rhythm, SongParser/SongExpander→Song) and the IProgressionStore port→Music.Song travel with their types. Keep the ChordFlow. root prefix (C2). Build is intentionally red until step 2. | src/ChordFlow.Core/Domain/** (moved → src/ChordFlow.Core/Music/**), src/ChordFlow.Core/Exercises/Exercise.cs, src/ChordFlow.Core/Exercises/Difficulty.cs | — | IN1, IN2, IN3, IN4, C2 |
| ✅ | 2 | Replace every `using ChordFlow.Domain;` and every `<see cref="Domain.*"/>` cross-reference across Features/, Rendering/, Bridge/, Persistence/, the Desktop host, and tests/ with the specific new namespace(s) each file actually uses. End state: full solution builds and ALL existing tests pass. | src/ChordFlow.Core/Features/**, src/ChordFlow.Core/Rendering/**, src/ChordFlow.Core/Bridge/**, src/ChordFlow.Core/Persistence/**, src/ChordFlow.Desktop/**, tests/ChordFlow.Core.Tests/** | move-kernel-files | IN5, C3 |
| ✅ | 3 | Grep for runtime (src/) consumers of SeedData. If none remain (content now ships from the default pack), move it to a test/seed area; otherwise leave it in Music.Progression. Update any references and keep the build/tests green. | src/ChordFlow.Core/Music/Progression/SeedData.cs (or moved to tests/seed area) | rewire-consumers | IN8 |
| ✅ | 4 | Retarget tests/.../Architecture/InstrumentBoundaryTests from the `ChordFlow.Domain` edge to `ChordFlow.Music` (no Music type may reference ChordFlow.Instruments). Keep Rendering→Instruments and Persistence→Instruments allowed. | tests/ChordFlow.Core.Tests/Architecture/InstrumentBoundaryTests.cs | rewire-consumers | IN6 |
| ✅ | 5 | Add NetArchTest rules locking the new boundaries: Music.Harmony references no sibling Music.* (it is the sink), and there are no dependency cycles among the Music.* namespaces. Set each sibling's allowed outward edges to the REAL references observed after the move (assert the observed graph, never an aspirational one — must not force any code change, EX1). | tests/ChordFlow.Core.Tests/Architecture/MusicLayeringTests.cs | rewire-consumers, seeddata-placement | IN9 |
| ✅ | 6 | Update the three loom/refs docs (architecture, domain-model, dsl), loom/ctx.md, and README.md/CHANGELOG.md to the new namespaces, in this same unit of work (ref-sync contract). | loom/refs/chordflow-architecture-reference.md, loom/refs/chordflow-domain-model-reference.md, loom/refs/chordflow-dsl-reference.md, loom/ctx.md, README.md, CHANGELOG.md | seeddata-placement | IN7 |
| ✅ | 7 | Full solution builds; all tests green (incl. the new layering tests); loom_validate clean; grep proves zero `ChordFlow.Domain` / `namespace ChordFlow.Domain` references remain in src/, tests/, the three refs, ctx.md, and README. Commit as its own isolated commit. | (whole solution — verification only) | retarget-arch-test, music-layering-tests, update-docs | C1, C2, C3, C4 |
---

### Legend

| Symbol | Meaning |
|--------|---------|
| ✅ | Done |
| 🔄 | In Progress |
| 🔳 | Pending |
| ❌ | Cancelled |

<!-- step:move-kernel-files-into-the-music -->
### Step 1 — Move kernel files into the Music/* + Exercises/ tree

Follow the design's full type→namespace move table verbatim. Folder mirrors namespace. `SeedData` is deferred to step 3 — leave it moved into Music/Progression provisionally so the build can be reasoned about, final home decided in step 3.

<!-- step:rewire-all-consumers-usings-xml-doc -->
### Step 2 — Rewire all consumers — usings + XML-doc crefs → green

Let the compiler drive: fix unresolved-type errors namespace by namespace. No behavioral edits (EX1) — only `using`/cref changes and namespace qualifiers.

<!-- step:add-music-layering-architecture-tests -->
### Step 5 — Add Music.* layering architecture tests

Determine the actual edges by inspecting the post-move `using`s (e.g. confirm whether Progression→Rhythm exists via tick durations on ChordSpan/HarmonicBar) and encode exactly those.

<!-- step:sync-all-documentation -->
### Step 6 — Sync all documentation

refs/* are gate-excluded → edit via loom_patch_doc/loom_update_doc to keep frontmatter consistent. ctx.md is a Loom doc → loom_update_doc. README/CHANGELOG are repo-root → normal Edit.
