---
type: done
id: pl_01KVEBP4AN05RVNZPY599JQ3KR-done
title: Done — Theory / Instrument boundary — structural move + GuitarInstrument adapter
status: done
created: 2026-06-18
version: 4
tags: []
parent_id: pl_01KVEBP4AN05RVNZPY599JQ3KR
requires_load: []
---
# Done — Theory / Instrument boundary — structural move + GuitarInstrument adapter

## Step 1 — Move guitar geometry, voicings & diagrams to `Instruments/Guitar/`; renamespace to `ChordFlow.Instruments.Guitar`; fix every consumer `using`

**Structural move + renamespace — done.**

- `git mv`'d all 13 guitar files into `src/ChordFlow.Core/Instruments/Guitar/{Geometry,Voicings,Diagrams}/` (history preserved); removed the now-empty `Domain/Voicings` + `Domain/Diagrams` folders.
- Changed each moved file from `namespace ChordFlow.Domain;` → `namespace ChordFlow.Instruments.Guitar;` and added `using ChordFlow.Domain;` (they still consume theory types).
- Added `using ChordFlow.Instruments.Guitar;` to every consumer the compiler flagged: **Core** — `Rendering/AlphaTexRenderer.cs`, `Persistence/VoicingStore.cs`, `Features/ContentCrud/ContentCrudEnvelopes.cs` + `ContentCrudHandler.cs`, `Domain/LeadTargets.cs` (temporary — removed in Step 2); **Desktop** — `Program.cs`; **Tests** — `DefaultPackVoicingsTests`, `ExercisePipelineTests`, `VoicingBookTests`, `VoicingDiagramTests`, `VoicingDslParserTests`, `VoicingPersistenceTests`, `VoicingRealizerTests`, `ContentCrudHandlerTests`, `ContentCrudStoreTests`, `LeadTargetsTests`, `VoicingBookIntegrationTests`, `VoicingDslWriterTests`.
- **Build: green. Tests: 399 passed, 0 failed** — behavior unchanged, as intended for a pure relocation.
- Known temporary edge: `Domain/LeadTargets.Resolve` still references the moved `Fretboard`/`FretPosition` (compiles, same assembly) — removed in Step 2 before the arch test lands in Step 3.

## Step 2 — Add `GuitarInstrument` adapter facade (`Realize`/`Diagram`/`ResolveLead`); trim `LeadTargets` in Domain to pitch-class output only

**`GuitarInstrument` adapter + `LeadTargets` split — done.**

- New `src/ChordFlow.Core/Instruments/Guitar/GuitarInstrument.cs` — concrete facade over the moved pieces, constructed with a `VoicingBook`:
  - `Realize(Chord, Difficulty) → Voicing` (delegates `VoicingBook.Lookup`; `Difficulty` kept as a straight pass-through per chat).
  - `Diagram(VoicingShape) → FretboardDiagram` (passthrough to `VoicingDiagram.Build`; **Option A** — shape-based/canonical-C, settled in chat-001).
  - `ResolveLead(Chord, TargetZone, maxFret) → IReadOnlyList<FretPosition>` (the relocated lead fret-resolution).
  - Class doc carries the **forward-link**: authored↔CAGED reconciliation is owned by `caged-system`, extension point = `VoicingBook`'s shadow rule.
- `Domain/LeadTargets.cs` trimmed to `GuideTones` + `PitchClassOf` (pitch-class output only); removed `Resolve` and the temporary `using ChordFlow.Instruments.Guitar;`. **`Domain/` now has zero code reference to `Instruments`** (verified — only an explanatory doc-comment mentions it).
- Tests: new `Instruments/GuitarInstrumentTests.cs` (Realize delegation + stored-shadows-generated, Diagram delegation, relocated `ResolveLead` B-sounding-positions). Removed the `Resolve` test from `LeadTargetsTests` and migrated `ExercisePipelineTests`' lead branch to `GuitarInstrument.ResolveLead`.
- **Build: green. Tests: 402 passed, 0 failed** (399 − 1 relocated + 4 new).

## Step 3 — Architecture test (`NetArchTest.Rules`): assert no `ChordFlow.Domain` type depends on `ChordFlow.Instruments`

**Boundary architecture test — done.**

- Added test-only `NetArchTest.Rules` 1.3.2 PackageReference to `ChordFlow.Core.Tests.csproj`.
- New `tests/ChordFlow.Core.Tests/Architecture/InstrumentBoundaryTests.cs`:
  - `Domain_DoesNotDependOn_Instruments` — `Types.InAssembly(Core).That().ResideInNamespace("ChordFlow.Domain").ShouldNot().HaveDependencyOn("ChordFlow.Instruments")`; IL-level so method-body refs are caught (the failure message lists offending types).
  - `Domain_NamespaceFilter_MatchesTypes` — anti-vacuous guard: asserts the `ChordFlow.Domain` filter actually matches >0 types, so the rule can't pass for the wrong reason after a rename/typo.
  - Doc comment records the deliberate **Domain-edge-only** scope (`Rendering`/`Persistence → Instruments` intentionally unconstrained).
- **Tests: 404 passed, 0 failed** — the boundary holds green by construction (Steps 1–2 cleaned the `LeadTargets`/`IVoicingStrategy` edges).

## Step 4 — Ref-sync — promote the boundary to live structure in the architecture + domain-model refs; correct the `ChordFlow.Core.*` → `ChordFlow.*` namespace

**Reference-doc sync — done.** Both refs updated (gate-excluded, via `loom_patch_doc`).

- **`chordflow-architecture-reference`**: added `Instruments/Guitar/` to the §2 solution tree; rewrote the §3 `Domain/` blurb as instrument-agnostic + added a new `### Instruments/Guitar/` layer section; promoted the §7 "Planned: theory ↔ instrument boundary" subsection to **live** ("Theory ↔ instrument boundary (live)") describing the `GuitarInstrument` facade, the namespace boundary, and the `NetArchTest.Rules` Domain-edge guard; updated the closing line (move landed, `IInstrument`/renderer fork still ahead in `instrument-rendering`). **Fixed the namespace error** `ChordFlow.Core.Instruments` → `ChordFlow.Instruments` (verified zero `ChordFlow.Core.{Domain,Instruments}` strings remain).
- **`chordflow-domain-model-reference`**: §2 heading + note now read as the live guitar adapter under `Instruments/Guitar/` (namespace `ChordFlow.Instruments.Guitar`) with the `GuitarInstrument` surface; fixed the `Domain/Voicings/` (×2) and `Domain/Diagrams/` path tokens to `Instruments/Guitar/...`; updated the §4 `LeadTargets` row to pitch-class-only with fret resolution on `GuitarInstrument.ResolveLead`.
