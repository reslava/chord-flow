---
type: chat
id: ch_01KVEAE6AQ28RA5711GYRKRMCY
title: instrument-boundary Chat
status: active
created: 2026-06-18
version: 1
tags: []
parent_id: null
requires_load: []
last_ai_block: 10
---
# instrument-boundary Chat

## Rafa:

read the idea
is all grounded to create a design?
id so, create it

note: next thread will be domain/intervals by roadmap

## AI:

Read the idea, and grounded it against the live code before answering.

**Yes — it's grounded enough to design and build, and I created the design** (`instrument-boundary-design.md`, draft). The idea's big calls are settled (boundary not assembly · concrete `GuitarInstrument` first · `IInstrument` deferred), scope is explicit, sequencing is clear.

What grounding against the actual code added — three things the idea glosses, now pinned in the design:

1. **Namespace correction.** The idea and *both refs* say `ChordFlow.Core.Domain` / `ChordFlow.Core.Instruments`. The real root namespace is flat **`ChordFlow.*`** (`ChordFlow.Domain`, `ChordFlow.Rendering`, …) — subfolders don't add sub-namespaces. So the new area is **`ChordFlow.Instruments.Guitar`** and the arch test guards `ChordFlow.Domain ↛ ChordFlow.Instruments`. The ref fix rides along.

2. **Two boundary-crossers the move *exposes*** (not in the idea's scope list, but the arch test fails without them):
   - `LeadTargets.Resolve(...)` (in `Domain/`) calls `Fretboard` → a Domain→guitar edge. Fix: `LeadTargets` stays pure (guide tones + pitch classes); the fret-resolving method moves to the guitar side.
   - `IVoicingStrategy.Voice → Voicing` is guitar-shaped, so the interface moves with the voicing family (it's *not* the deferred `IInstrument`).

3. **One open decision before the plan: the architecture-test mechanism.** No arch-test infra exists today. I recommend **`NetArchTest.Rules`** (test-only NuGet, IL-level — actually catches the `LeadTargets.Resolve` kind of method-body reference) over a hand-rolled reflection scan (misses method bodies unless you add Mono.Cecil and reinvent it). Flagging rather than baking it in since test-mechanism is a design decision.

Two small things to confirm (design §9): the arch-test mechanism above, and where the relocated lead fret-resolution lands (I lean a method on `GuitarInstrument`).

**Noted for next:** per the roadmap the next thread is `domain/intervals` (the derivation engine this boundary is the precondition for).

Want me to lock these two decisions and move to a plan, or talk through anything in the design first?

## Rafa:

arch-test mechanism: NetArchTest.Rules
fret-resolution: method on `GuitarInstrument`

settle decisions in design

generate requirements

## AI:

Done both.

**Decisions settled in `instrument-boundary-design.md`:**
- §5 — architecture test mechanism is now `NetArchTest.Rules` (no longer an open decision).
- §3.1 / §4 — relocated lead fret-resolution is now a `ResolveLead` method on `GuitarInstrument`.
- §0 and §9 updated to match — §9 is now "Decisions (settled)", nothing open.

**Requirements generated → `req.md`** (draft, faithful extraction from the idea + settled design):
- **9 Included** (`IN1`–`IN9`) — the three move groups (geometry / realize / diagrams), the `ChordFlow.Instruments.Guitar` namespace, `Domain/` stays pure, the `LeadTargets` split, the `GuitarInstrument` facade, the `NetArchTest.Rules` arch test, and the same-unit-of-work ref updates.
- **6 Excluded** (`EX1`–`EX6`) — `IInstrument`, renderer fork, `Pitch` type, second instrument, forcing callers through the facade, separate assembly.
- **6 Constraints** (`C1`–`C6`) — dependency direction (only the Domain edge enforced; Rendering/Persistence→Instruments stay allowed), the flat-namespace fact, `NetArchTest.Rules`, the "arch test green at end" rule, no-throwaway, and sequencing.

The req is **draft**. Next steps when you're ready: lock the req (`loom_finalize_req` — the anchor the plan verifies against), finalize the design, then draft the plan. Want me to lock the req and move to the plan?

## Rafa:

reqs locked
create plan

## AI:

Locked the req, finalized the design (active), and created the plan → **`instrument-boundary-plan-001.md`**. Four steps, every req handle cited:

| # | Step | Satisfies |
|---|------|-----------|
| 1 | **Structural move + renamespace** — move the 13 guitar files to `Instruments/Guitar/{Geometry,Voicings,Diagrams}/`, change `ChordFlow.Domain` → `ChordFlow.Instruments.Guitar`, fix every consumer `using`. Ends compile-green. | IN1–IN5, C6 |
| 2 | **`GuitarInstrument` adapter + `LeadTargets` split** — new facade (`Realize`/`Diagram`/`ResolveLead`); trim `LeadTargets` to pitch-class-only so Domain stops referencing any guitar type. | IN6, IN7, C1, C4 |
| 3 | **Boundary architecture test** — add `NetArchTest.Rules`; assert `ChordFlow.Domain ↛ ChordFlow.Instruments`; green only because steps 1–2 cleaned the edges. Keeps `Rendering`/`Persistence → Instruments` allowed. | IN8, C1, C3 |
| 4 | **Ref-sync** — promote the boundary to live structure in both refs + fix the `ChordFlow.Core.*` → `ChordFlow.*` namespace. | IN9, C2 |

**Sequencing note:** Step 1 deliberately leaves one temporary `LeadTargets.Resolve → Fretboard` cross-reference (compiles, same assembly); Step 2 removes it; Step 3's arch test lands only after that edge is clean — so the test is green by construction, not theater. Steps 3 and 4 are blocked-by the earlier steps accordingly.

Plan is ready. Per the session contract I'll **stop here and wait for `go`** before implementing Step 1 (the structural move). Say `go` for step 1, or `do the whole plan` if you want me to run straight through.

## Rafa:

Plan started
Do whole plan

## AI:

**Step 1 ✅ — structural move done.** All 13 guitar files moved to `Instruments/Guitar/` under `ChordFlow.Instruments.Guitar`, every consumer `using` fixed, **build green, 399/399 tests pass** (behavior unchanged). Details in the done doc.

**Stopping on a design decision before Step 2** (stop-rule 3 — API shape — interrupts even under "do whole plan").

The `GuitarInstrument` facade has three intended methods. Two are unambiguous:
- `Realize(Chord, Difficulty) → Voicing` — delegates `VoicingBook.Lookup`. ✅
- `ResolveLead(Chord, TargetZone, maxFret) → IReadOnlyList<FretPosition>` — the relocated lead resolution. ✅

The third — **`Diagram(...)`** — is the snag. The plan sketched `Diagram(Chord, Difficulty)`, but the live code doesn't support it cleanly: `VoicingDiagram.Build` takes a **`VoicingShape`** and renders at the canonical-**C** anchor (root-picker is deferred — EX2 of the voicings work), while `VoicingBook.Lookup`/`Candidates` return a **realized `Voicing`** at the chord's real root and never expose the winning *shape*. There's no `(Chord, Difficulty) → VoicingShape` path today.

Options:

- **A — `Diagram(VoicingShape) → FretboardDiagram` (recommended).** A faithful passthrough to `VoicingDiagram.Build` — honest about today's canonical-C reality, matches how the Content/Voicings preview actually calls it, zero scope expansion. Satisfies IN7's "produce a `FretboardDiagram`" via the facade. Slightly thin (it's a 1-line delegate), but correct.
- **B — `Diagram(Chord, Difficulty) → FretboardDiagram`.** Requires adding a "winning shape" accessor to `VoicingBook`, *and* the diagram would still render at canonical-C — so a `G7` diagram would show C-shape frets. Misleading until the deferred root-aware diagram lands. Couples the facade to incomplete capability.
- **C — drop `Diagram` from the facade this thread.** Keep it `Realize` + `ResolveLead` only; leave `VoicingDiagram.Build` as the direct call until root-aware diagrams exist. Smallest, but skips an IN7 sub-goal.

I recommend **A**. Which do you want?

## Rafa:

I think we can do A for now for authored guitar chord voices
In roadmap we have guitar/caged-system derivation engine
I do not care about Difficulty param for now
But I think we could rethink to match the authored guitar voicings with the future CAGED derived, or mayb defer this design for the time we implement CAGED derived engine

What you think?


## AI:

Agree with A — and I'd **defer the authored↔CAGED reconciliation to the `caged-system` thread**, not design it now. Reasoning:

**There isn't really a "match" problem to solve at the facade.** Per the derivation-engine vision, authored voicings are the **golden oracle** — the ground truth the future CAGED-derived shapes are *validated against* (derived must reproduce authored). So authored and derived aren't two runtime rivals that need reconciling in `GuitarInstrument`; they're "spec" vs "implementation," and the check lives in the derivation engine's **tests**, not in a runtime merge we'd design today.

**The runtime extension point already exists** — `VoicingBook`'s shadow rule (*stored authored shadows generated strategy*). When CAGED-derived lands, it slots in there as another candidate source (derived ranked alongside/below authored), which is an **additive change at that one seam** — the `GuitarInstrument` facade and its `Realize`/`Diagram` signatures don't need to change. So building A now locks us into nothing.

**Designing the reconciliation now would be guessing** — the derivation engine's output shape is exactly what `caged-system` will discover. This is the same discipline that made us defer `IInstrument`: don't build the abstraction before its second real consumer exists. Doing it now risks a wrong guess we'd rip out.

So my recommendation for this thread:
- **A** — `Diagram(VoicingShape) → FretboardDiagram` (thin, honest passthrough). ✅
- **Difficulty** — keep it as a straight pass-through param on `Realize` (it's what `VoicingBook.Lookup` takes; reserved for the deferred difficulty-band narrowing). Not over-designing it, just forwarding — dropping it would mean inventing a default and diverging from the book's contract.
- **Explicitly note** in the facade + design that authored↔CAGED reconciliation is **owned by `caged-system`**, with `VoicingBook`'s shadow seam as the named extension point — a forward-link so that thread picks it up.

Net: A is forward-safe; the interesting design lands *with* CAGED when we actually have the derived shapes to reconcile.

Good to proceed with Step 2 on that basis? (facade = `Realize` + `Diagram(VoicingShape)` + `ResolveLead`, plus the forward-link note.)

## AI:

**Whole plan complete — all 4 steps ✅, plan auto-closed to `done`. Final: build green, 404/404 tests pass.**

- **Step 2 ✅** — `GuitarInstrument` facade (`Realize` / `Diagram(VoicingShape)` / `ResolveLead`) under `Instruments/Guitar/`; `LeadTargets` trimmed to pitch-class-only so `Domain/` stops referencing any guitar type; lead-resolution tests relocated; `ExercisePipelineTests` migrated. The facade carries the forward-link note (authored↔CAGED reconciliation → `caged-system`, via `VoicingBook`'s shadow seam).
- **Step 3 ✅** — the boundary is now **proven**: `NetArchTest.Rules` (IL-level) asserts `ChordFlow.Domain ↛ ChordFlow.Instruments`, plus an anti-vacuous guard. `Rendering`/`Persistence → Instruments` left allowed by design.
- **Step 4 ✅** — both refs synced to live structure; the `ChordFlow.Core.*` → `ChordFlow.*` namespace error is fixed (zero wrong-namespace strings remain).

Net result: the kernel is provably instrument-agnostic, guitar is an opt-in adapter behind `GuitarInstrument`, and the `instrument-boundary` precondition for the derivation-engine threads is in place. Per the roadmap note in your first message, the next thread is **`domain/intervals`** (then the guitar derivation threads — `octave-shapes`, `interval-lattice`, `caged-system`, `guitar-voicings` — all target the `Instruments/Guitar/` area this founded).

**Not committed** — I leave git to you (you commit to `main` directly). Working tree has the move (git-tracked renames), the new `GuitarInstrument` + arch test, the `NetArchTest.Rules` package, and the ref updates. Want me to commit it, or anything to review first?