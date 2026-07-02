---
type: design
id: de_01KVEASE6MHSWMVDER0AY0ZJPT
title: Theory / Instrument boundary + concrete Guitar adapter
status: done
created: 2026-06-18
updated: 2026-06-18
version: 8
idea_version: 3
tags: []
parent_id: id_01KVCTCBE0AXZH6FX2HJ9ZA1YH
requires_load: []
---
# Theory / Instrument boundary + concrete Guitar adapter

## 0. Verdict

The idea is **grounded enough to design and build**. The big decisions are settled (boundary not assembly · concrete `GuitarInstrument` first · `IInstrument` deferred), scope is explicit, and the invariants/sequencing are clear. Grounding the idea against the **live code** surfaced three things the idea glosses that this design pins down — they are the real content here:

1. **A naming correction** — the actual root namespace is flat `ChordFlow.*`, not `ChordFlow.Core.*` as the idea and both refs state.
2. **Two boundary-crossing seams** the move *exposes* (`LeadTargets.Resolve` and `IVoicingStrategy`) — not in the idea's in-scope list, but the architecture test will fail without resolving them.
3. **The architecture-test mechanism** — settled to `NetArchTest.Rules` (no arch-test infra exists today). §5.

---

## 1. Grounding correction — namespace is `ChordFlow.*`, not `ChordFlow.Core.*`

The assembly is `ChordFlow.Core` but its **root namespace is `ChordFlow`** — verified flat across the kernel: `ChordFlow.Domain`, `ChordFlow.Rendering`, `ChordFlow.Bridge`, `ChordFlow.Persistence`. Subfolders (`Domain/Voicings/`, `Domain/Diagrams/`, `Domain/Song/`) **do not** add sub-namespaces — every file in them is still `namespace ChordFlow.Domain;`.

Consequences for this thread:

- New area namespace = **`ChordFlow.Instruments.Guitar`** (not `ChordFlow.Core.Instruments`).
- Folders `Instruments/Guitar/{Geometry,Voicings,Diagrams}/` are **organization only** — all guitar types share the one flat `ChordFlow.Instruments.Guitar` namespace, matching the kernel's existing flat-per-area convention. (The arch test keys on the `ChordFlow.Instruments` prefix, so sub-namespaces buy nothing and would break convention.)
- The architecture test asserts: **no type in `ChordFlow.Domain` depends on `ChordFlow.Instruments`.**
- **Both refs carry the wrong namespace** (`ChordFlow.Core.Domain` / `ChordFlow.Core.Instruments`) — correcting them is part of the same-unit-of-work ref update (§6).

---

## 2. Target layout

`Domain/` keeps pure theory; guitar moves to `Instruments/Guitar/`. Verified file inventory to move (current path → new path), namespace `ChordFlow.Domain` → `ChordFlow.Instruments.Guitar`:

**Geometry** — `Instruments/Guitar/Geometry/`
- `Domain/Fretboard.cs`
- `Domain/FretPosition.cs`

**Voicings** — `Instruments/Guitar/Voicings/`
- `Domain/Voicing.cs`
- `Domain/IVoicingStrategy.cs`  *(see §3.2)*
- `Domain/BeginnerShellStrategy.cs`
- `Domain/VoicingBook.cs`
- `Domain/Voicings/VoicingShape.cs`
- `Domain/Voicings/CagedShape.cs`
- `Domain/Voicings/VoicingRealizer.cs`
- `Domain/Voicings/VoicingDslParser.cs`
- `Domain/Voicings/VoicingDslWriter.cs`
- `Domain/Voicings/VoicingDiagram.cs`

**Diagrams** — `Instruments/Guitar/Diagrams/`
- `Domain/Diagrams/FretboardDiagram.cs` (+ `FretboardMarker`, `MarkerShape` declared within)

**Stays in `Domain/` (verified pure):** PitchClass, Key, Quality, QualityIntervals, ChordTone(s), Chord, Scale, DiatonicChord, RomanDegree, ScaleDegree, ChordSpan, HarmonicBar, Progression, ProgressionParser, Transposer, NoteSpeller, ChordSymbol, the whole `Song/` family, the 48-PPQ rhythm grid (+ parser + overlays), `Importance`/`TargetZone`/`LeadTargets`, `SeedData`, `Exercise`. `SeedData` confirmed guitar-free (Progression + rhythm + keys only).

> **The `Diagrams/` carrier is guitar.** `FretboardDiagram` is named/modeled around frets, strings, and barres — it moves whole. The architecture ref's aspiration of a future *instrument-agnostic* diagram is out of scope; today it is the guitar spatial carrier and belongs in `Instruments/Guitar/`.

---

## 3. Boundary-crossers the move exposes (the real work)

The mechanical move is trivial; these two seams are why a naive move would red the architecture test on day one.

### 3.1 `LeadTargets.Resolve` reaches into the fretboard

`Domain/LeadTargets.cs` is *mostly* pure (`GuideTones` → `TargetZone`s, `PitchClassOf` → `PitchClass`) **except** `Resolve(chord, zone, maxFret)`, which calls `Fretboard.PositionsFor(...)` → `FretPosition`. That is a **Domain → guitar** edge — exactly what the test forbids, and it contradicts the idea's own "Domain output vocabulary → ChordTones / PitchClasses."

**Resolution:** `LeadTargets` stays in `Domain/`, keeping `GuideTones` + `PitchClassOf` (pure, pitch-class output). The fret-resolving `Resolve` method **moves to the guitar side** — it becomes a method on **`GuitarInstrument`** (settled — lead fret-resolution sits on the one adapter surface; today it appears used only by `LeadTargetsTests`, so the move is cheap). Signature on the guitar side: `Resolve(Chord, TargetZone, maxFret) → IReadOnlyList<FretPosition>` — guitar in, fret positions out.

### 3.2 `IVoicingStrategy` returns a guitar type

`Domain/IVoicingStrategy.Voice(chord) → Voicing`. `Voicing` is guitar, so the interface is guitar-shaped even though the idea's in-scope list names only `BeginnerShellStrategy`. **The interface moves with the voicing family** into `Instruments/Guitar/Voicings/`. (This is *not* the deferred `IInstrument` — it is the existing voicing-strategy seam, which is legitimately guitar-internal.)

These two are the difference between "the architecture test passes because the code is actually clean" and "the test is theater." Resolving them is the substantive deliverable.

---

## 4. The `GuitarInstrument` adapter surface

A deliberate public facade in `ChordFlow.Instruments.Guitar` over the moved pieces — the "first-class concrete adapter" the idea calls for, *not* a free-floating interface. Intended surface (signatures finalized in the plan):

- `Realize(Chord, Difficulty) → Voicing` — delegates to `VoicingBook.Lookup`; the guitar voicing carries its `FretPosition`s.
- `Diagram(VoicingShape) → FretboardDiagram` — the spatial twin, a passthrough to `VoicingDiagram.Build` (settled: shape-based, canonical-C — the live producer's contract; `VoicingBook` returns a realized `Voicing` and exposes no winning *shape*, and the root-picker is deferred, so a `Diagram(Chord, Difficulty)` would render misleading C-anchored frets).
- `ResolveLead(Chord, TargetZone, maxFret) → IReadOnlyList<FretPosition>` — the relocated lead-target fret resolution (§3.1).

> **Authored↔CAGED reconciliation is deferred to `caged-system` (settled in chat-001).** Authored voicings are the golden oracle the future derivation engine is *validated against* (in tests), not a runtime rival to merge here. The runtime extension point is `VoicingBook`'s shadow rule (stored shadows generated) — the derived source slots in there additively without changing this facade. The facade + design carry a forward-link note so that thread picks it up. Same discipline as deferring `IInstrument`: don't build the abstraction before its second real consumer.

**Construction seam:** `VoicingBook` is today an *instance* built from the store at the `Program.cs` seam and injected into `AlphaTexRenderer`. `GuitarInstrument` takes a `VoicingBook` (and `Fretboard` is static geometry). This is an additive facade — existing callers (`AlphaTexRenderer`, `ContentCrud`, `VoicingStore`) keep using the underlying types directly for now; nothing is forced through the facade in this thread. It exists as the deliberate surface the deferred `IInstrument` is later extracted *from*.

---

## 5. The architecture test — **settled: `NetArchTest.Rules`**

No arch-test infrastructure exists — the test project carries only xUnit + Test SDK + coverlet. The idea wants the Domain edge **provably** guarded. Two mechanisms:

- **Option A — `NetArchTest.Rules` (recommended).** Test-only NuGet, purpose-built. `Types.InNamespace("ChordFlow.Domain").ShouldNot().HaveDependencyOn("ChordFlow.Instruments")`. Does **IL-level** dependency analysis, so it catches method-body references (the `LeadTargets.Resolve → Fretboard` kind), not just public surface. One small test-only dependency; strongest real guarantee.
- **Option B — hand-rolled reflection.** Zero new dependencies, but reflection only sees the **public surface** (base types, interfaces, field/property/parameter/return types) — it would **miss a method-body reference** like §3.1. To match Option A's strength you'd add `Mono.Cecil` and scan IL yourself — i.e. reinvent NetArchTest with more code.

**Settled: Option A (`NetArchTest.Rules`)** — confirmed by Rafa. It gives the real proof the idea asks for, the project's design philosophy favors correctness over dependency-minimalism, and a test-only package never reaches production.

**Scope of the assertion (both options):** guard the **Domain edge only**. Explicitly *do not* forbid `Rendering → Instruments` or `Persistence → Instruments` — the tab renderer and the voicing store legitimately consume guitar types. Only `ChordFlow.Domain ↛ ChordFlow.Instruments` is enforced.

---

## 6. Ref updates (same unit of work)

- **`chordflow-architecture-reference`** — the "Planned: theory ↔ instrument boundary" subsection (§7) becomes **live structure**: `Instruments/Guitar/` in the solution shape (§2), the dependency arrow, the arch-test description. **Fix the namespace** (`ChordFlow.Core.Instruments` → `ChordFlow.Instruments`).
- **`chordflow-domain-model-reference`** — move the entire Voicing layer (§2) and the `Diagrams/` carrier out of the Domain map into a new Guitar-instrument section; note `LeadTargets` is now pitch-class-only and its fret resolution lives on the guitar side.

---

## 7. Ripple (mechanical)

~36 files reference the moved types. Today they reach them via `using ChordFlow.Domain;`; after the move they need `using ChordFlow.Instruments.Guitar;` added. Consumers and their (allowed) edges: `Rendering/AlphaTexRenderer` (Rendering→Instruments ✓), `Persistence/VoicingStore` + `VoicingEntity` (Persistence→Instruments ✓), `Features/ContentCrud` + `Features/Packs` (✓), `Bridge/WebMessageRouter` (✓), `Desktop/Program.cs` (✓), and the test files. None of these violate the guarded edge. Mechanical and compiler-caught — the move is "red until every `using` is added," not a logic change.

---

## 8. Out of scope (→ `instrument-rendering`)

Unchanged from the idea: the `IInstrument` interface · the notation/tab renderer fork · a `Pitch(pc, octave)` theory type · Piano or any second instrument · forcing existing callers through the `GuitarInstrument` facade.

---

## 9. Decisions (settled)

1. **Architecture-test mechanism** — **`NetArchTest.Rules`** (test-only NuGet, IL-level dependency analysis). §5.
2. **Home of relocated lead fret-resolution** — a method on **`GuitarInstrument`** (`ResolveLead`). §3.1 / §4.

Everything is settled — ready to plan.
