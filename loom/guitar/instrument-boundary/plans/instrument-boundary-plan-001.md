---
type: plan
id: pl_01KVEBP4AN05RVNZPY599JQ3KR
title: Theory / Instrument boundary — structural move + GuitarInstrument adapter
status: done
created: 2026-06-18
updated: 2026-06-18
version: 1
design_version: 1
req_version: 1
tags: []
parent_id: de_01KVEASE6MHSWMVDER0AY0ZJPT
requires_load: []
target_version: 0.1.0
actual_release: 0.7.0
steps:
  - id: structural-move-renamespace
    order: 1
    status: done
    description: Move guitar geometry, voicings & diagrams to `Instruments/Guitar/`; renamespace to `ChordFlow.Instruments.Guitar`; fix every consumer `using`
    files_touched: [src/ChordFlow.Core/Instruments/Guitar/Geometry/Fretboard.cs, src/ChordFlow.Core/Instruments/Guitar/Geometry/FretPosition.cs, src/ChordFlow.Core/Instruments/Guitar/Voicings/Voicing.cs, src/ChordFlow.Core/Instruments/Guitar/Voicings/IVoicingStrategy.cs, src/ChordFlow.Core/Instruments/Guitar/Voicings/BeginnerShellStrategy.cs, src/ChordFlow.Core/Instruments/Guitar/Voicings/VoicingBook.cs, src/ChordFlow.Core/Instruments/Guitar/Voicings/VoicingShape.cs, src/ChordFlow.Core/Instruments/Guitar/Voicings/CagedShape.cs, src/ChordFlow.Core/Instruments/Guitar/Voicings/VoicingRealizer.cs, src/ChordFlow.Core/Instruments/Guitar/Voicings/VoicingDslParser.cs, src/ChordFlow.Core/Instruments/Guitar/Voicings/VoicingDslWriter.cs, src/ChordFlow.Core/Instruments/Guitar/Voicings/VoicingDiagram.cs, src/ChordFlow.Core/Instruments/Guitar/Diagrams/FretboardDiagram.cs, src/ChordFlow.Core/Rendering/AlphaTexRenderer.cs, src/ChordFlow.Core/Rendering/RenderOptions.cs, src/ChordFlow.Core/Bridge/WebMessageRouter.cs, src/ChordFlow.Core/Persistence/VoicingStore.cs, src/ChordFlow.Core/Persistence/Entities/VoicingEntity.cs, src/ChordFlow.Core/Features/ContentCrud/ContentCrudHandler.cs, src/ChordFlow.Core/Features/ContentCrud/ContentEntity.cs, src/ChordFlow.Core/Features/Packs/PackImporter.cs, src/ChordFlow.Core/Features/Packs/ContentKind.cs, src/ChordFlow.Desktop/Program.cs, tests/ChordFlow.Core.Tests/]
    blocked_by: []
    satisfies: [IN1, IN2, IN3, IN4, IN5, C6]
  - id: guitarinstrument-adapter-leadtargets-split
    order: 2
    status: done
    description: Add `GuitarInstrument` adapter facade (`Realize`/`Diagram`/`ResolveLead`); trim `LeadTargets` in Domain to pitch-class output only
    files_touched: [src/ChordFlow.Core/Instruments/Guitar/GuitarInstrument.cs, src/ChordFlow.Core/Domain/LeadTargets.cs, tests/ChordFlow.Core.Tests/Instruments/GuitarInstrumentTests.cs, tests/ChordFlow.Core.Tests/LeadTargetsTests.cs]
    blocked_by: []
    satisfies: [IN6, IN7, C1, C4]
  - id: boundary-architecture-test
    order: 3
    status: done
    description: "Architecture test (`NetArchTest.Rules`): assert no `ChordFlow.Domain` type depends on `ChordFlow.Instruments`"
    files_touched: [tests/ChordFlow.Core.Tests/ChordFlow.Core.Tests.csproj, tests/ChordFlow.Core.Tests/Architecture/InstrumentBoundaryTests.cs]
    blocked_by: [1, 2]
    satisfies: [IN8, C1, C3]
  - id: reference-doc-sync
    order: 4
    status: done
    description: Ref-sync — promote the boundary to live structure in the architecture + domain-model refs; correct the `ChordFlow.Core.*` → `ChordFlow.*` namespace
    files_touched: [loom/refs/chordflow-architecture-reference.md, loom/refs/chordflow-domain-model-reference.md]
    blocked_by: [1, 2, 3]
    satisfies: [IN9, C2]
---
# Theory / Instrument boundary — structural move + GuitarInstrument adapter

## Goal

Separate pure music theory from guitar realization inside ChordFlow.Core, turning guitar into an opt-in adapter. Move every guitar-specific type (geometry: Fretboard/FretPosition; realize: Voicing/IVoicingStrategy/BeginnerShellStrategy/VoicingBook/VoicingShape/CagedShape/VoicingRealizer/VoicingDslParser/VoicingDslWriter/VoicingDiagram; diagrams: FretboardDiagram/FretboardMarker/MarkerShape) out of Domain/ into a new Instruments/Guitar/ area under the flat namespace ChordFlow.Instruments.Guitar, fixing every consumer's using. Resolve the two boundary-crossers the move exposes: trim LeadTargets in Domain to pitch-class output only and relocate its fret-resolution to a ResolveLead method on a new concrete GuitarInstrument adapter facade (over VoicingBook/Fretboard/VoicingDiagram); IVoicingStrategy moves with the voicing family. Prove the boundary with a NetArchTest.Rules architecture test asserting no ChordFlow.Domain type depends on ChordFlow.Instruments (Rendering→Instruments and Persistence→Instruments stay allowed). Update the architecture + domain-model refs in the same unit of work, correcting the ChordFlow.Core.* → ChordFlow.* namespace error. Out of scope (→ instrument-rendering): the IInstrument interface, the renderer fork, a Pitch type, any second instrument, and forcing existing callers through the facade. Anchored on the locked req.

---

## Steps

| Done | # | Step | Files touched | Blocked by | Satisfies |
|---|---|---|---|---|---|
| ✅ | 1 | Move guitar geometry, voicings & diagrams to `Instruments/Guitar/`; renamespace to `ChordFlow.Instruments.Guitar`; fix every consumer `using` | src/ChordFlow.Core/Instruments/Guitar/Geometry/Fretboard.cs, src/ChordFlow.Core/Instruments/Guitar/Geometry/FretPosition.cs, src/ChordFlow.Core/Instruments/Guitar/Voicings/Voicing.cs, src/ChordFlow.Core/Instruments/Guitar/Voicings/IVoicingStrategy.cs, src/ChordFlow.Core/Instruments/Guitar/Voicings/BeginnerShellStrategy.cs, src/ChordFlow.Core/Instruments/Guitar/Voicings/VoicingBook.cs, src/ChordFlow.Core/Instruments/Guitar/Voicings/VoicingShape.cs, src/ChordFlow.Core/Instruments/Guitar/Voicings/CagedShape.cs, src/ChordFlow.Core/Instruments/Guitar/Voicings/VoicingRealizer.cs, src/ChordFlow.Core/Instruments/Guitar/Voicings/VoicingDslParser.cs, src/ChordFlow.Core/Instruments/Guitar/Voicings/VoicingDslWriter.cs, src/ChordFlow.Core/Instruments/Guitar/Voicings/VoicingDiagram.cs, src/ChordFlow.Core/Instruments/Guitar/Diagrams/FretboardDiagram.cs, src/ChordFlow.Core/Rendering/AlphaTexRenderer.cs, src/ChordFlow.Core/Rendering/RenderOptions.cs, src/ChordFlow.Core/Bridge/WebMessageRouter.cs, src/ChordFlow.Core/Persistence/VoicingStore.cs, src/ChordFlow.Core/Persistence/Entities/VoicingEntity.cs, src/ChordFlow.Core/Features/ContentCrud/ContentCrudHandler.cs, src/ChordFlow.Core/Features/ContentCrud/ContentEntity.cs, src/ChordFlow.Core/Features/Packs/PackImporter.cs, src/ChordFlow.Core/Features/Packs/ContentKind.cs, src/ChordFlow.Desktop/Program.cs, tests/ChordFlow.Core.Tests/ | — | IN1, IN2, IN3, IN4, IN5, C6 |
| ✅ | 2 | Add `GuitarInstrument` adapter facade (`Realize`/`Diagram`/`ResolveLead`); trim `LeadTargets` in Domain to pitch-class output only | src/ChordFlow.Core/Instruments/Guitar/GuitarInstrument.cs, src/ChordFlow.Core/Domain/LeadTargets.cs, tests/ChordFlow.Core.Tests/Instruments/GuitarInstrumentTests.cs, tests/ChordFlow.Core.Tests/LeadTargetsTests.cs | — | IN6, IN7, C1, C4 |
| ✅ | 3 | Architecture test (`NetArchTest.Rules`): assert no `ChordFlow.Domain` type depends on `ChordFlow.Instruments` | tests/ChordFlow.Core.Tests/ChordFlow.Core.Tests.csproj, tests/ChordFlow.Core.Tests/Architecture/InstrumentBoundaryTests.cs | 1, 2 | IN8, C1, C3 |
| ✅ | 4 | Ref-sync — promote the boundary to live structure in the architecture + domain-model refs; correct the `ChordFlow.Core.*` → `ChordFlow.*` namespace | loom/refs/chordflow-architecture-reference.md, loom/refs/chordflow-domain-model-reference.md | 1, 2, 3 | IN9, C2 |
---

### Legend

| Symbol | Meaning |
|--------|---------|
| ✅ | Done |
| 🔄 | In Progress |
| 🔳 | Pending |
| ❌ | Cancelled |

<!-- step:structural-move-renamespace -->
### Step 1 — Structural move + renamespace

Pure mechanical relocation — no logic change. Move the 13 guitar files into `Instruments/Guitar/{Geometry,Voicings,Diagrams}/` (folders organizational only) and change each from `namespace ChordFlow.Domain;` to `namespace ChordFlow.Instruments.Guitar;`. Then add `using ChordFlow.Instruments.Guitar;` to every consumer that referenced a moved type — across Rendering, Persistence, Features (ContentCrud/Packs), Bridge, Desktop, and the test project (~36 referencing files total; the file list names the production consumers). Ends **compile-green**. `LeadTargets.Resolve` (Domain) still references the now-moved `Fretboard` — a temporary Domain→Instruments reference that compiles (same assembly) and is removed in Step 2 before the arch test lands. Verify the full test suite stays green (behavior unchanged).

<!-- step:guitarinstrument-adapter-leadtargets-split -->
### Step 2 — GuitarInstrument adapter + LeadTargets split

New `GuitarInstrument` facade in `ChordFlow.Instruments.Guitar` over the moved pieces, constructed with a `VoicingBook` (the instance already built at the `Program.cs` seam): `Realize(Chord, Difficulty) → Voicing` (delegates `VoicingBook.Lookup`), `Diagram(Chord, Difficulty) → FretboardDiagram` (via the realized shape + `VoicingDiagram.Build`), and `ResolveLead(Chord, TargetZone, maxFret) → IReadOnlyList<FretPosition>`. Remove the fret-resolving `Resolve` method from `Domain/LeadTargets.cs`, leaving `GuideTones` + `PitchClassOf` (pure, pitch-class output) — Domain no longer references any guitar type. Move the lead fret-resolution assertions out of `LeadTargetsTests` into `GuitarInstrumentTests`. Additive facade: existing callers keep using the moved types directly (EX5).

<!-- step:boundary-architecture-test -->
### Step 3 — Boundary architecture test

Add the test-only `NetArchTest.Rules` PackageReference. New test: `Types.InNamespace("ChordFlow.Domain").ShouldNot().HaveDependencyOn("ChordFlow.Instruments")` resolves with no failing types — green only because Steps 1–2 cleaned the `LeadTargets` and `IVoicingStrategy` edges. Guard the **Domain edge only**: add explicit assertions (or a comment + omission) confirming `Rendering → Instruments` and `Persistence → Instruments` remain allowed, so the test never over-constrains. This is the `C4` 'arch test green at end' gate.

<!-- step:reference-doc-sync -->
### Step 4 — Reference-doc sync

Per the same-unit-of-work ref rule. In `chordflow-architecture-reference`: replace the 'Planned: theory ↔ instrument boundary' subsection with live structure — `Instruments/Guitar/` in the solution shape, the enforced dependency arrow, the `NetArchTest.Rules` test — and fix `ChordFlow.Core.Instruments` → `ChordFlow.Instruments`. In `chordflow-domain-model-reference`: move the Voicing layer (§2) and the `Diagrams/` carrier out of the Domain map into a new Guitar-instrument section, and note `LeadTargets` is now pitch-class-only with fret resolution on `GuitarInstrument.ResolveLead`.
