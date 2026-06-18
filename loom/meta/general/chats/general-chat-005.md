---
type: chat
id: ch_01KVCPZHPD5FBMENZTFRH4FD0J
title: Music theory - Instruments adapters - UI components
status: active
created: 2026-06-18
version: 1
tags: []
parent_id: null
requires_load: []
last_ai_block: 23
---
# Music theory - Instruments adapters - UI components

## Rafa:

We just launched 0.6.0 (Fretboard render component shipped) and before continue with current roadmap next feature intervals I am thinking about separation of concerns:
- pure music theory
- instruments

Chord Flow is mainly for guitar players, but If its well designed could be instrument agnostic, robust, expandable for new instruments opt-in

This is the darft architecture proposal:

Music Theory Domain (instrument agnostic, C#)
  Intervals
  Octaves
  Chord qualities
  Voicings
  Rhythm
  Progression
  Song

↓

Instrument adapters (C#)
  Guitar: Guitar Octave shapes, Guitar Voicings, CAGED System
  Piano: Piano Otaves, Piano Voicings

↓

alphaTex/DTOs

↓

UI (js)
  Score render component (all instruments) -> alphaTex → alphaTab notation/playback
  Guitar instrument: Fretboard render component -> fretboard/SVG spatial

### TODO

Analyze and let me know what you think
Is it worth the effort? Suggestions? Improvements?

## AI:

**Short version:** the *conceptual* split is correct, and it's exactly where your next feature already lives — but I'd not turn it into a third C# project yet. Make it a **namespace boundary inside Core now**, guarded by an architecture test; promote it to a separate assembly when a second instrument (piano) is actually real.

## 1. Most of this already exists — the job is to *name* what's implicitly true

Your `Domain/` is described as "music-theory-first," but it's already two things wearing one coat:

- **Pure / agnostic:** PitchClass, Key, Quality, QualityIntervals, ChordTone, Chord, Scale, DiatonicChord, RomanDegree/ScaleDegree, ChordSpan, HarmonicBar, Progression, Transposer, NoteSpeller, ChordSymbol, the whole 48-PPQ rhythm grid (TickGrid/RhythmPattern/Feel/…), LeadTargets.
- **Guitar-specific** (standard tuning, frets, strings, CAGED): FretPosition, Fretboard, Voicing, VoicingBook, BeginnerShellStrategy, VoicingShape, CagedShape, VoicingRealizer, VoicingDiagram, FretboardDiagram/Marker.

And the UI layer you drew is *already* split the way you want it: `score-render-component.js` (notation, any instrument) vs `fretboard-render-component.js` (guitar spatial). So this isn't a rewrite — it's making an existing, implicit boundary explicit and load-bearing.

## 2. The seam you're drawing is exactly where the interval-derivation engine lands

Your derivation-engine vision is "derive CAGED shapes from intervals + octave shapes + quality formulas." *That is the theory→guitar adapter.* Theory hands you chord tones / intervals (root-relative, agnostic); the guitar adapter realizes them onto a tuning as fret positions / CAGED shapes / a FretboardDiagram. So naming this boundary now isn't speculative overhead — it's the scaffolding for the literal next thing you're building. That's the strongest argument *for* doing it, and it's why I'd time the split to ride in **with** the interval work, not as a standalone refactor.

## 3. Where I'd push back: a boundary, not a third project (yet)

The Core/Desktop split earns its own assembly because there's a real second consumer (the host) and a compile guarantee worth having ("Core physically cannot call WinForms"). For instruments you have **one** real instrument. Piano is hypothetical, and an adapter interface designed against a hypothetical is usually the wrong shape — you discover the right abstraction from the *second* concrete case, not the first.

A separate assembly also costs more than the diagram suggests: the renderer emits guitar tab (`fret.string`) so Rendering depends on guitar voicings, and VoicingDiagram depends on theory (ChordSymbol/QualityIntervals). You'd be reshuffling a real dependency graph for zero second consumer.

So — get the boundary, skip the assembly:

1. Regroup inside Core: `Domain/Theory/*` (pure) and `Domain/Guitar/*` (instrument) — or `Domain/` + a sibling `Instruments/Guitar/`.
2. Add **one architecture test** in Core.Tests asserting no type under the Theory namespace references the Guitar namespace. That's the cheap version of the compile guarantee — it enforces "guitar derives from theory, never the reverse" *today*, and turns the eventual project split into a mechanical move rather than a redesign.

## 4. One correction to the layer diagram

alphaTex isn't cleanly "below all instruments." The current renderer emits guitar tab, so it already consumes guitar fret positions — it sits *beside/after* the guitar adapter, not under a shared instrument layer. Honest picture:

- **Notation / standard staff** (alphaTex) — mostly agnostic, sharable with piano.
- **Tab staff + fretboard SVG** — guitar, fed by the guitar adapter.

Piano would reuse the notation path but needs its own realization (hand/octave assignment), not fret positions. So it's less a linear stack and more: `theory → { guitar adapter → tab/fretboard · piano adapter → grand staff } → renderer(s)`.

## Verdict

Worth it — but as a **boundary**, not a **project**, and timed to land alongside the interval/derivation work. The payoff isn't "instrument-agnostic for its own sake"; it's that pinning theory as provably-pure is the precondition for deriving guitar shapes *from* it cleanly.

If you want, I can sketch the minimal `IInstrument` / adapter seam derived purely from guitar + the theory you have today, so we can sanity-check it against the derivation engine before committing to anything.

## Rafa:

I never mentioned a new project / assembly, I don't know why you think I was proposing that.

### Boundary

I lean to regroup this way `Domain/` + a sibling `Instruments/Guitar/`, agree? other suggestions?
I liked one architecture test in Core.Tests asserting no type under the Theory namespace references the Guitar namespace

### TODO

- sketch the IInstrument adapter seam
- draw the whole diagram with detailed annotations

## AI:

Fair — you said "instrument adapters (C#)" and I pattern-matched it onto the Core/Desktop precedent (separate assembly) and argued against a strawman you didn't raise. Scratch §3's framing; the real question is just *where the folders go and how rich the seam is*. We agree on the substance.

### Boundary — agreed, with two pins

`Domain/` (pure theory) + sibling `Instruments/Guitar/` is the right shape: it parallels the existing top-level Core seams (`Domain/ Rendering/ Features/ Bridge/ Persistence/`) instead of nesting a `Theory` sub-folder. Two things to pin so it doesn't rot:

1. **The architecture test is namespace-based:** no type in `ChordFlow.Core.Domain` may reference `ChordFlow.Core.Instruments`. That's the whole guarantee — theory stays derivable-from, never derives-toward.
2. **Rendering depends on `Instruments/Guitar`, and that's fine** — the dependency direction is `Domain ← Instruments ← Rendering`. `AlphaTexRenderer` emits guitar tab (`fret.string`), so it legitimately consumes guitar fret positions. The test guards the *Domain* edge only; it must NOT forbid Rendering→Instruments or you'd be stuck.

What physically moves into `Instruments/Guitar/`: `Fretboard`, `FretPosition`, `Voicing`, `VoicingBook`, `BeginnerShellStrategy`, `VoicingShape`, `CagedShape`, `VoicingRealizer`, plus `Diagrams/` (`FretboardDiagram`, `FretboardMarker`, `MarkerShape`, `VoicingDiagram`). Everything else in `Domain/` stays.

---

### TODO 1 — the `IInstrument` seam (sketch + honest annotation)

The thing I want to be candid about first: **with one instrument there is no polymorphic call site**, so an `IInstrument` interface today would be an interface with exactly one implementer and nobody dispatching over it — the same speculative-abstraction trap, just smaller. So I'd **document the seam now, build the interface when piano is real.** Here's the shape it converges to, which is what lets us sanity-check the boundary:

```csharp
namespace ChordFlow.Core.Instruments;

public interface IInstrument
{
    string Id { get; }            // "guitar"
    string DisplayName { get; }   // "Guitar (standard tuning)"

    // Agnostic contract: harmony -> concrete sounding pitches.
    // Pitches (pc + octave) are theory-typed, so the NOTATION renderer
    // and playback can consume any instrument without knowing which.
    InstrumentRealization Realize(Chord chord, Difficulty difficulty);
}

// What every renderer can consume — agnostic.
public record InstrumentRealization(IReadOnlyList<Pitch> Pitches);

// Guitar ADDS positions. A guitar-aware tab renderer / fretboard
// producer use these; a piano grand-staff renderer ignores them.
public sealed record GuitarVoicing(
    IReadOnlyList<Pitch> Pitches,
    Voicing Positions) : InstrumentRealization(Pitches);
```

**The one real design tension this surfaces** (worth deciding consciously, even if we defer building it): a *fully* instrument-agnostic renderer is a fiction for **tab**. Tab needs frets, not just pitches. The clean resolution:

- **Notation / standard staff** = agnostic, consumes `Pitch`es → one shared path.
- **Tab + fretboard SVG** = instrument-aware, consumes `GuitarVoicing.Positions`.

So `IScoreRenderer` eventually forks into "agnostic notation" + "instrument-specific tab" — but that's a piano-era decision. **Today, the single guitar-bound `AlphaTexRenderer` is correct**; we just write down that the fork is the planned extension point.

Net: the input vocabulary the guitar adapter consumes from theory is the **already-existing `ChordTones` / `PitchClasses`** ("b7 of G7" = root+10, computed not stored) — no new theory type needed for the seam. The derivation engine slots in here: it's a `VoicingShape` *producer* inside `Instruments/Guitar/`, fed by interval sets from `QualityIntervals`.

---

### TODO 2 — the whole diagram, annotated

```
┌──────────────────────────────────────────────────────────────────┐
│ Domain/   PURE MUSIC THEORY — instrument-agnostic                  │
│   harmony  PitchClass · Quality/QualityIntervals · ChordTone ·     │
│            Chord · Scale · DiatonicChord · RomanDegree/ScaleDegree· │
│            Progression · Transposer · NoteSpeller · ChordSymbol     │
│   rhythm   TickGrid(48ppq) · RhythmPattern · Feel/Accent/Stroke     │
│   lead     TargetZone · LeadTargets                                 │
│   ── output vocabulary → ChordTones / PitchClasses (root-relative   │
│                          + concrete), Pitch(pc+octave)              │
│   RULE  references nothing below.  ◄── architecture test guards     │
└───────────────┬────────────────────────────────────────────────────┘
                │  theory in;  never referenced back by Domain
                ▼
┌──────────────────────────────────────────────────────────────────┐
│ Instruments/                                                       │
│   IInstrument  (thin: Id · DisplayName · Realize → pitches)        │
│                          [interface deferred until 2nd instrument] │
│   Guitar/   ← the only real adapter today                          │
│     geometry  StandardTuning · Fretboard                           │
│     realize   Voicing · FretPosition · VoicingBook ·               │
│               BeginnerShellStrategy · VoicingShape · CagedShape ·  │
│               VoicingRealizer   (+ future: derivation engine)      │
│     diagram   FretboardDiagram · FretboardMarker · MarkerShape ·   │
│               VoicingDiagram                                       │
│   « Piano/ — extension point, NOT built »                          │
└───────────────┬────────────────────────────────────────────────────┘
                │  agnostic Pitches  +  guitar FretPositions
                ▼
┌──────────────────────────────────────────────────────────────────┐
│ Rendering/   export seam (IScoreRenderer)                          │
│   AlphaTexRenderer → guitar tab (fret.string) TODAY                │
│       · staff/notation part = agnostic                             │
│       · tab part            = consumes Guitar positions            │
│       « future fork: agnostic-notation ∥ instrument-tab renderers »│
│   RhythmQuantizer  (agnostic)                                      │
└───────────────┬────────────────────────────────────────────────────┘
                ▼  alphaTex string   +   FretboardDiagram model
                │  (Bridge envelope DTOs — host-agnostic)
                ▼
┌──────────────────────────────────────────────────────────────────┐
│ UI (JS) — dumb views, zero theory                                  │
│   score-render-component.js     → notation + playback (any instr.) │
│   fretboard-render-component.js → guitar spatial SVG (guitar)      │
└──────────────────────────────────────────────────────────────────┘
```

Dependency arrows all point **up**: `UI → Bridge → Rendering → Instruments → Domain`. The only enforced edge is the bottom one (the test); the rest are conventions the project layout already follows.

---

So, concretely, what I'd actually *do* when we act on this:
1. Create `Instruments/Guitar/` and move the guitar types (mechanical; namespace churn + `using` updates).
2. Add the namespace architecture test.
3. Update `chordflow-architecture-reference` + `chordflow-domain-model-reference` to show the Theory/Guitar boundary.
4. **Leave** `IInstrument`, the renderer fork, and `Pitch`-based realization as documented extension points — don't build them yet.

Does that "build the move, defer the interface" line match how you want it? And do you want this to land as its own thread now, or ride in with the interval-derivation work (where the boundary actually starts paying off)? I'll hold here for your call before anything moves.

## Rafa:

### Tablature

Tablature will be only available to be showed for guitar tracks in score.

### About defer `IInstrument`

I think if this is grounded, should be implemented now, so future develoing fit into it.
Probably Piano will never come out, probably Chord Flow will dissapear in the clud, no users, no feedback, I just like designing projects as well as I can.
What you think?

## AI:

### Tablature

Good — "tab only on guitar tracks" locks the fork cleanly: a track is shown as **standard notation** (agnostic) and/or **tab** (guitar-only). Piano tracks render to a grand staff, never tab. That's not a constraint we're tolerating, it's the actual shape of the boundary, and it tells us *exactly* where the agnostic/instrument line falls inside the renderer.

### On building `IInstrument` now

Let me drop the YAGNI framing entirely — it's the wrong argument for you, and I agree with your premise: no users, no deadline, the point is to design it as well as it can be designed. So the question isn't "is it worth the effort," it's strictly **"is an interface built now the most *correct* design, or a less-correct one than the alternative?"** That's the only axis that matters here, and on that axis I land on: *build the boundary and the concrete adapter now; let the interface be born with its first caller — which can be this same thread.* Here's the reasoning, not the reflex.

**An interface is a contract about how callers vary over implementations. With one implementation and zero polymorphic callers, there's nothing to vary — so the interface isn't expressing a discovered truth, it's asserting a guess.** The danger isn't wasted work (you'd happily rewrite it — breaking changes are free for you). The danger is *subtler and worse for design quality*: an unvalidated interface tends to be **wrong in a way you can't see**, because nothing is pushing back on its shape. And the specific place it'd be wrong is concrete here:

The agnostic output (sounding pitches) is **entangled** with a guitar-specific decision (which voicing? at what difficulty? one or a ranked set?). `Realize(Chord) → Pitches` is underdetermined — the pitches *come from* a chosen voicing, and voicing-selection is the most guitar-shaped thing in the whole kernel (CAGED, fret reachability, difficulty). So any `IInstrument.Realize` I write today has to bake in a guess about how selection generalizes across instruments — and that's precisely the part piano would rewrite. The clean-looking thin interface isn't cleanly *extractable* from one case; it only *looks* clean because guitar is the only thing pressing on it.

So what I'd build now is the part that's **grounded with zero guessing**:

1. `Instruments/Guitar/` + the namespace test + ref updates (agreed).
2. Give guitar a **deliberate first-class adapter surface** — a concrete `GuitarInstrument` (or a crisp public facade over `VoicingBook`/`Fretboard`/`VoicingDiagram`) with the exact methods a caller would want: realize a chord → a guitar voicing carrying both its `Pitch`es and its `FretPosition`s; produce a `FretboardDiagram`. This *is* "designing it in from the start" — a named adapter with an intentional public shape — without freezing a cross-instrument contract I can't yet verify.

And then the honest path to a *real* `IInstrument`: it should be **born the moment a second caller dispatches over it** — and that caller already has a name: the **notation/tab renderer fork** the tablature rule just defined. A renderer that emits standard-notation-from-pitches (agnostic) vs tab-from-positions (guitar) is the first place code genuinely asks "give me the agnostic view, I don't care which instrument." Extract `IInstrument` *there*, against that caller, and it'll be right because something real shaped it.

The collaboration contract says don't commit below 95% confidence. I'm ~95% on the *boundary* and the *concrete adapter*. I'm well under 95% on the cross-instrument method contract (difficulty? candidates vs one? realization shape?). So I'd build the first and hold the second — not as a compromise, but because that split is where my actual confidence is.

**So my recommendation, and the decision for you:** the most-correct `IInstrument` is one born with a consumer. So —

- **(A)** Pull the **notation/tab renderer fork** into *this* work, so `IInstrument` is introduced with a real caller and validated immediately. Bigger scope, but the interface is grounded the day it exists. This is the "design it fully now, correctly" option.
- **(B)** Ship the **boundary + concrete `GuitarInstrument` adapter** now; `IInstrument` arrives with the renderer fork as a near-future thread. Smaller, and nothing built is throwaway.

I lean **B** only because the renderer fork is a meaty piece of real design in its own right and deserves its own room — but **A** is the more complete answer to "design it in from the start," and if that's the spirit you want, I'm in. Which one?

## Rafa:

You convinced me for doing this refactoring in Phases and when the time/need comes:
1. Spin up a weave/thread with an idea for `notation/tab renderer fork` and `IInstrument`, this way is never, showed in roadmap
2. Spin up a weave/thread with an idea for **boundary + concrete `GuitarInstrument` adapter**
3. We will do 2. before intervals....

Before proceed, let me know what names for weave/threads you suggest and let me review them

## AI:

Phased + capture-now-build-later is exactly right. One structural note first: these are **threads under the existing `chordflow` weave**, not new weaves — they sit beside `core-host-split`, `voicings`, `content-crud`, `caged-system`, etc. (a weave is the project; these are workstreams in it). So both live at `loom/chordflow/{thread-id}/`.

Here's what I'd name them and what each idea doc would scope. Ordering: **#2 is built first (before intervals); #1 is created now as an idea so it shows on the roadmap, but parked until the need arises.**

### Thread #2 — build first (the structural move)

| | |
|---|---|
| **thread-id (primary)** | `instrument-boundary` |
| alternates | `theory-guitar-split` · `guitar-adapter` |
| idea title | "Theory / Instrument boundary + concrete Guitar adapter" |
| scope | Move guitar types into `Instruments/Guitar/` (Fretboard, Voicing, FretPosition, VoicingBook, BeginnerShellStrategy, VoicingShape, CagedShape, VoicingRealizer, Diagrams/*). Add the namespace architecture test (`Domain` may not reference `Instruments`). Give guitar a deliberate first-class `GuitarInstrument` adapter surface. Update `chordflow-architecture-reference` + `chordflow-domain-model-reference`. **No `IInstrument` interface, no renderer change.** |

### Thread #1 — capture now, build when needed

| | |
|---|---|
| **thread-id (primary)** | `instrument-rendering` |
| alternates | `notation-tab-fork` · `iinstrument-seam` |
| idea title | "Instrument-aware rendering fork + IInstrument seam" |
| scope | Fork the score path into **agnostic notation (from pitches)** vs **guitar tab (from fret positions)**; tab only on guitar tracks. Extract `IInstrument` *here*, born with that real polymorphic caller. Likely introduces a `Pitch(pc, octave)` theory type. Depends on #2. |

My leans: **`instrument-boundary`** for #2 (it names the structural essence and reads cleanly in the roadmap), and **`instrument-rendering`** for #1 — though if you'd rather the roadmap entry foreground the interface, **`iinstrument-seam`** is the more pointed label.

Pick/adjust the two thread-ids (and tweak the idea titles if you want), and I'll spin them up — #2 with a fleshed-out idea ready to move on, #1 as a parked idea doc. I'll hold until you confirm the names.

## Rafa:

Agree on weave `chordflow`
Liked threads names
Spin up them with the ideas, mentioning this chat as origin

## AI:

Both threads are up under the `chordflow` weave, each idea citing this chat as origin:

- **`instrument-boundary`** — `th_01KVCTCBFPMRJHQPDQBVEXDV8P` · idea `id_01KVCTCBE0AXZH6FX2HJ9ZA1YH` → `loom/chordflow/instrument-boundary/instrument-boundary-idea.md`. The structural move (boundary + namespace test + concrete `GuitarInstrument` adapter + ref updates). Built first, before intervals.
- **`instrument-rendering`** — `th_01KVCTCN9STSHNPK02KKP1GASA` · idea `id_01KVCTCN9M2NMG56CQ0BSW9SQ3` → `loom/chordflow/instrument-rendering/instrument-rendering-idea.md`. Parked. The renderer fork + `IInstrument` born with its first real caller. Marked **depends_on `instrument-boundary`** on the roadmap.

Both ideas are at `status: draft`. When you want to move on `instrument-boundary`, the next step is finalizing its idea, then a design/plan — say the word and I'll start, but I'll hold here since nothing's scheduled yet.

## Rafa:

Maybe loom weave threads ideas need a small refactor:

Some threads under domain weave `loom/domain/` should be moved to a new weave:
`instrumment-guitar`: `caged-system` `octave-shapes`

And thread `voicings` and its idea splitted into
`domain/voicings`
`instrumment-guitar/guitar-voicings`

and some ideas reviewed and updated.

Agree? Something missing? Better approach names?

## TODO

About weave thread CRUD operation just give me a table and I will do it.
About reviewing, updating ideas, give me a list of what need the udpdate.


## AI:

First, a correction to *my* earlier framing: your weaves are already **architectural areas** (`domain`, `ui`, `packages`, `playback`, `release`, `exercises`, `docs`, `meta`, and `chordflow` as the foundational/cross-cutting one). So I was wrong to drop `instrument-boundary`/`instrument-rendering` into `chordflow` as if it were a flat catch-all. Realigning the weaves to the theory/instrument boundary is exactly consistent with how you're already organizing — **agree with the direction.**

And reading the cluster, the split is **cleaner and slightly bigger** than the three threads you named. The whole derivation engine partitions along the theory/guitar line:

| Thread | Theory or Guitar? | Why |
|---|---|---|
| `chord-qualities` | **Theory** — stays in `domain` | quality → interval *formula* (`1 b3 5 b7`). Zero fretboard. |
| `intervals` | **Both — split it** | vocabulary (degree→semitones+spelling) is pure theory; the **fretboard interval-position lattice** ("where each interval sits on the neck") is pure guitar geometry. |
| `octave-shapes` | **Guitar** → move | the 5 CAGED root maps = string/fret offsets. |
| `caged-system` | **Guitar** → move | emits fret shapes (`quality intervals × octave shape → fret shape`). |
| `voicings` | **~all Guitar** → move+rename | the whole thing is fret-chart DSL, CAGED shapes, `Fretboard`/`FretPosition`. |

### Something missing (beyond your list)

1. **`intervals` also has a guitar half.** Its idea explicitly has two sections: the interval *vocabulary* (theory) and the *fretboard interval lattice* (guitar — "the real deliverable," 2 octaves L/R per string). I'd split it: vocabulary stays `domain/intervals`; the lattice becomes a new guitar thread **`interval-lattice`** (alt: `fretboard-intervals`). It's the generalization of `octave-shapes` to every degree, so it sits naturally in the guitar weave between `octave-shapes` and `caged-system`.
2. **`instrument-boundary` should move too** — it *founds* the guitar code area, so it belongs in the new weave, not `chordflow`. My mistake to put it there.
3. **`instrument-rendering` I'd leave in `chordflow`** — it births `IInstrument` (an abstraction *over* instruments, not guitar-specific) and forks the Core renderer; that's a cross-cutting seam like `core-host-split`. Your call, but I lean keep-in-chordflow.

### On splitting `voicings` — honest pushback

Here's where I'd diverge from your proposal. **As built, `voicings` is essentially all guitar** — fret-chart DSL, canonical-C CAGED shapes, `VoicingBook`/`VoicingRealizer`. There is no implemented *theory* voicing layer. A real `domain/voicings` would be the **abstract** notion (chord-tone selection + register/octave + inversion/drop → pitches, instrument-agnostic) — genuine music theory, but **not built and not required by the current roadmap** (the derivation engine goes `quality × octave-shape → frets` directly, never through an abstract voicing).

So creating `domain/voicings` now is the *same speculative-abstraction move* we just agreed to avoid with `IInstrument`: an empty theory layer with no consumer. My recommendation:

- **Move `voicings` wholesale → `guitar/guitar-voicings`.** Don't split it.
- Capture the abstract-voicing concept as a one-line "future substrate" note inside the `guitar-voicings` idea (or a parked `domain/voicings` stub **only if** you want it visible on the roadmap — but I lean defer, for consistency with the principle).

If you *do* see abstract voicings as a near-term substrate (e.g. you want it before piano-that-probably-never-comes), say so and I'll treat `domain/voicings` as a real parked thread.

### Weave name

I'd name it **`guitar`** — shortest, and `domain` (theory) / `guitar` (instrument) mirrors the code's `Domain/` ↔ `Instruments/Guitar/` pairing cleanly, matching your single-word sibling weaves. Alt: **`instrument-guitar`** (your original, mirrors the code path more literally). Pick whichever; table below assumes `guitar`.

---

### TODO 1 — Weave/thread CRUD ops (you execute)

Thread `th_` ULIDs survive moves and deps reference ULIDs (not paths), so cross-weave dependency edges stay valid — no re-wiring needed except the new `interval-lattice` edges.

| # | Operation | Source → Target | Tool |
|---|---|---|---|
| ✅1 | Create weave | — → `guitar` | `loom_create_weave` |
| ✅2 | Move thread | `domain/octave-shapes` → `guitar/octave-shapes` | `loom_rename` |
| ✅3 | Move thread | `domain/caged-system` → `guitar/caged-system` | `loom_rename` |
| ✅4 | Move + rename | `domain/voicings` → `guitar/guitar-voicings` | `loom_rename` |
| ✅5 | Move thread | `chordflow/instrument-boundary` → `guitar/instrument-boundary` | `loom_rename` |
| 6 | ✅Create thread + 🔳idea | — → `guitar/interval-lattice` (the lattice split out of `intervals`) | `loom_create_idea` (auto-scaffolds thread) |
| 7 | 🔳Set deps | `guitar/interval-lattice` depends_on `domain/intervals` (`th_…Z08`) + `guitar/octave-shapes` (`th_…0K…BMY17`) | `loom_set_thread_deps` |
| 8 | 🔳Update deps | `guitar/caged-system`: repoint the intervals-lattice dependency to `interval-lattice` (keep `chord-qualities`) | `loom_set_thread_deps` |
| 9 | (decision) | `instrument-rendering` — ✅ leave in `chordflow` (my lean) or move to `guitar` | `loom_rename` if moving |
| 10 | (decision) | `domain/voicings` abstract stub — ✅**skip** (my lean) or create as parked | `loom_create_idea` if creating |

> ⚠️ If `loom_rename` doesn't support cross-*weave* moves (only in-weave slug/title changes), the fallback per row is recreate-in-target + `loom_archive` the old — you'll know which your tooling supports. Flag it back to me if rename refuses and I'll adjust.

### TODO 2 — Ideas needing review/update

| Idea | Update |
|---|---|
| `domain/intervals-idea` | **Trim to vocabulary only** — remove the "Fretboard interval positions" section (moves to `interval-lattice`). Fix `[[octave-shapes]]`/`[[caged-system]]` links to note they're now guitar. |
| `guitar/interval-lattice-idea` *(new)* | The fretboard interval-position lattice section pulled out of `intervals` — 2-octave L/R per-string lattice, the generalization of `octave-shapes`. Links: `[[intervals]]` (vocab), `[[octave-shapes]]`. |
| `guitar/voicings → guitar-voicings-idea` | Reframe: drop the "fourth content pillar **in domain**" framing; it's the guitar adapter's authored-voicing layer. Add the one-line abstract-voicing future note. Update `domain/intervals` references. |
| `guitar/caged-system-idea` | Repoint `[[intervals]]` (the lattice references) → `[[interval-lattice]]`; note it now lives in `guitar`. |
| `guitar/octave-shapes-idea` | Link hygiene: `[[intervals]]` lattice mentions → `[[interval-lattice]]`. No content change. |
| `domain/chord-qualities-idea` | Link hygiene only — confirm its `[[intervals]]` refs mean the *vocabulary* (they do). Stays in `domain`. |
| `guitar/instrument-boundary-idea` | Note it founds the `guitar` weave's code area (origin chat ref unchanged). |
| `chordflow/instrument-rendering-idea` | Only if you move it; ULID dep on `instrument-boundary` stays valid regardless. |

That's the whole picture. Confirm the weave name and the two decisions (rows 9–10), and tell me if `loom_rename` balks at cross-weave moves — then you run the CRUD and I'll do the idea-doc edits (TODO 2) whenever you want them. **Holding here.**

## Rafa:

### TODO 1: i marked already done ✅ and pending 🔳
| # | Operation | Source → Target | Tool |
|---|---|---|---|
| ✅1 | Create weave | — → `guitar` | `loom_create_weave` |
| ✅2 | Move thread | `domain/octave-shapes` → `guitar/octave-shapes` | `loom_rename` |
| ✅3 | Move thread | `domain/caged-system` → `guitar/caged-system` | `loom_rename` |
| ✅4 | Move + rename | `domain/voicings` → `guitar/guitar-voicings` | `loom_rename` |
| ✅5 | Move thread | `chordflow/instrument-boundary` → `guitar/instrument-boundary` | `loom_rename` |
| 6 | ✅Create thread + ✅idea | — → `guitar/interval-lattice` (the lattice split out of `intervals`) | `loom_create_idea` (auto-scaffolds thread) |
| 7 | ✅Set deps | `guitar/interval-lattice` depends_on `domain/intervals` (`th_…Z08`) + `guitar/octave-shapes` (`th_…0K…BMY17`) | `loom_set_thread_deps` |
| 8 | ✅Update deps | `guitar/caged-system`: repoint the intervals-lattice dependency to `interval-lattice` (keep `chord-qualities`) | `loom_set_thread_deps` |
| 9 | (decision) | `instrument-rendering` — ✅ leave in `chordflow` (my lean) or move to `guitar` | `loom_rename` if moving |
| 10 | (decision) | `domain/voicings` abstract stub — ✅**skip** (my lean) or create as parked | `loom_create_idea` if creating |

> `loom_rename` doesn't support cross-*weave* moves 

I did all weave / threads CRUD manually and if more CRUD operations are needed I prefer you tell me and I will do it


## AI:

Structure confirmed and **row 6 done** — `interval-lattice` idea authored: `id_01KVDEEY1959RD07H63R5PFMVZ` → `loom/guitar/interval-lattice/interval-lattice-idea.md` (the fretboard-lattice deliverable pulled out of `intervals`, framed as the guitar projection).

### Rows 7 & 8 — exact dep ops (CRUD, you run)

ULIDs I looked up: interval-lattice `th_01KVD1S8MQ3H8D5R823KF3PC7A` · domain/intervals `th_01KV3X9Z08WBZ7M4S9WXNCYWV7` · octave-shapes `th_01KV3X9Z0KFRM2R2NYFG4BMY17` · chord-qualities `th_01KV3X9YY4WJ5KPW9W7401NJVS` · caged-system `th_01KV3X9YXYMDFMHPDJCH96K265`.

- **Row 7** — `interval-lattice` depends_on:
  `loom_set_thread_deps(threadUlid="th_01KVD1S8MQ3H8D5R823KF3PC7A", dependsOn=["th_01KV3X9Z08WBZ7M4S9WXNCYWV7","th_01KV3X9Z0KFRM2R2NYFG4BMY17"])`  *(domain/intervals + octave-shapes)*

- **Row 8 — correction:** `caged-system`'s current deps are `[octave-shapes, chord-qualities]` — there's **no `intervals` edge to repoint**, so this is an **ADD** of `interval-lattice`, not a repoint:
  `loom_set_thread_deps(threadUlid="th_01KV3X9YXYMDFMHPDJCH96K265", dependsOn=["th_01KV3X9Z0KFRM2R2NYFG4BMY17","th_01KV3X9YY4WJ5KPW9W7401NJVS","th_01KVD1S8MQ3H8D5R823KF3PC7A"])`  *(octave-shapes + chord-qualities + interval-lattice)*

### Two optional cleanups I noticed (your call, CRUD)

1. `guitar/guitar-voicings/` kept its **inner filenames** (`voicings-idea.md`, `voicings-design.md`, `voicings-plan-001.md`) — the folder renamed but the docs didn't. Cosmetic only (doc IDs/links are unaffected); rename if you want them to match the slug.
2. `interval-lattice`'s `thread.md` title is the bare slug `interval-lattice` — you may want to set it to the idea's title for the roadmap display.

### Remaining: TODO 2 idea edits

These are content edits (my job), not yet started. The **priority** one is trimming `domain/intervals-idea` — right now the "Fretboard interval positions" section is **duplicated** between it and the new `interval-lattice` idea, so that should be reconciled soon. The rest are reframes/link-hygiene (`guitar-voicings` idea, link repoints in `caged-system`/`octave-shapes`/`chord-qualities`, the `instrument-boundary` note).

Want me to proceed with the TODO 2 idea edits now (starting with the `intervals` trim), or hold? **Holding for your go.**

## Rafa:

### Rows 7 & 8 

done. 

I did them manually becuase I got an error trying to run 
loom_set_thread_deps(threadUlid="th_01KVD1S8MQ3H8D5R823KF3PC7A", dependsOn=["th_01KV3X9Z08WBZ7M4S9WXNCYWV7","th_01KV3X9Z0KFRM2R2NYFG4BMY17"])
bash: syntax error near unexpected token `threadUlid="th_01KVD1S8MQ3H8D5R823KF3PC7A",'

### Two optional cleanups I noticed

done

### TODO

If no more TODO 1 pending
Start TODO 2


## AI:

**TODO 2 done** — all idea docs reconciled to the theory/guitar split:

| Idea | What changed |
|---|---|
| `domain/intervals` | Trimmed to **vocabulary only** — the "Fretboard interval positions" section removed (no more duplication); now points to `[[interval-lattice]]` as its guitar projection. |
| `guitar/caged-system` | Step 3 + Dependencies repointed `[[intervals]]`→`[[interval-lattice]]`; deps now read "four substrates"; `voicings`→`guitar-voicings`. |
| `guitar/octave-shapes` | Lattice mentions + validation repointed to `[[interval-lattice]]`; related links updated. |
| `domain/chord-qualities` | The *fretboard-lattice* reference repointed to `[[interval-lattice]]`; kept "Spelling follows `[[intervals]]`" (that's genuinely the vocabulary). Stays in `domain`. |
| `guitar/guitar-voicings` | Reframed as the **guitar adapter's authored-voicing layer**; added the deferred **abstract-voicing** note (the `domain/voicings`-someday substrate); links updated. |
| `guitar/instrument-boundary` | Added the note that it **founds the `guitar` weave's code area**, with the derivation threads as its consumers. |

`chordflow/instrument-rendering` needed no edit (stayed put; its ULID dep is intact).

Two leftover **optional** title touch-ups (frontmatter titles can't be set via the content tools — they'd need a `loom_rename`, your domain):
- `guitar-voicings` idea title still reads *"Voicings — the fourth content pillar"* — you may want *"Guitar voicings — …"*.
- `interval-lattice` thread title is the bare slug.

The whole instrument-separation reorg — code-boundary threads, the derivation cluster moved to `guitar`, the `intervals` split, and all the idea docs — is now consistent. Nothing pending on my side. Anything else, or shall we call this chat done?

## Rafa:

### Documentation

After all reorganizing done we should update:
- loom/refs/chordflow-architecture-reference.md
- loom/refs/chordflow-domain-model-reference.md

Review and include diagram lines 166-215

### Roadmap

I reorder roadmap, this is the part related:

🗺️  Roadmap   current release: v0.6.0

ROADMAP  (present + future — dependency + priority order)
  ⚪ guitar/instrument-boundary (p10) Theory / Instrument boundary + concrete Guitar adapter
  🔴 chordflow/instrument-rendering (p20) Instrument-aware rendering fork + IInstrument seam  ⛔ blocked on → guitar/instrument-boundary
  ⚪ domain/intervals (p30) Intervals — the theory substrate (deferred, captured)
  🔴 guitar/octave-shapes (p40) Octave shapes — the 5 CAGED root maps (engine skeleton)  ⛔ blocked on → domain/intervals
  🔴 guitar/interval-lattice (p50) Interval lattice — fretboard interval positions (guitar projection of intervals)  ⛔ blocked on → domain/intervals, guitar/octave-shapes
  🔴 domain/chord-qualities (p60) Chord qualities — the interval formulas (engine input)  ⛔ blocked on → domain/intervals
  🔴 guitar/guitar-voicings (p70) Voicings — the fourth content pillar (authored, stored, movable)  ⛔ blocked on → domain/intervals
  🔴 guitar/caged-system (p80) CAGED system — the derivation engine (subsumes authored voicings)  ⛔ blocked on → guitar/octave-shapes, domain/chord-qualities, guitar/interval-lattice

Is correct, any suggestion?

note: I am finding myself using Loom and all its features extremely useful

## AI:

### Documentation — one timing caveat before I touch the refs

The two refs are bound by the contract's hard rule: *"the three `loom/refs/` docs mirror the **live system**… a code change that lands without its ref update is incomplete."* Right now the boundary is **designed but not built** — the guitar types still physically live in `Domain/`, and `instrument-boundary` is `⚪` (not started). If I rewrite the refs now to show `Instruments/Guitar/` as the structure, they'd describe code that doesn't exist yet — the refs would *lie about the current tree*, which is exactly what that rule guards against. And `instrument-boundary`'s own idea already lists "update both refs in the same unit of work" — so the **full structural rewrite belongs to that thread's execution**, not now.

What I'd do instead, now, without breaking the mirror:

- **Architecture ref** — extend §7 "Why this is built to evolve" with a clearly-labelled **"Planned: theory ↔ instrument boundary"** subsection: the decision, the `guitar` weave, and the reviewed diagram (chat lines 166–215) as the *target* shape. Documents the direction; doesn't claim it's built.
- **Domain-model ref** — a one-line forward note under the voicings/diagram types: "slated to move to `Instruments/Guitar/` — see `guitar/instrument-boundary`."
- Then the **full rewrite** (move the structural description, promote the diagram to the live map, the namespace test, `GuitarInstrument`) lands **with `instrument-boundary`**.

So: **(A)** forward-looking note now + full rewrite at code-time *(my recommendation — keeps the refs honest)*, or **(B)** you want the target documented as-of-record now and accept the refs lead the code. Tell me which and I'll do it immediately.

### Roadmap — mostly right, three suggestions

The dependency edges check out (boundary→rendering; intervals→{octave-shapes, lattice, chord-qualities}; lattice→+octave-shapes; caged→{octave-shapes, chord-qualities, lattice}). Three things:

1. **`instrument-rendering` at p20 is too eager.** It's the **parked** thread ("build when the need arises"). At p20 it ranks *ahead of the entire derivation cluster* (intervals p30 … caged p80) — but the derivation work is what you actually want next, and rendering is deferred. Its hard dep on `instrument-boundary` is right, but priority-wise it should sit **last** — bump it to ~**p90** so it doesn't jump the queue the moment boundary lands.

2. **`guitar-voicings` should NOT depend on `domain/intervals`.** Its own idea says it plainly — *"Realize math reuses `PitchClass` + `Fretboard` — no first-class `Interval` type needed."* That edge is spurious; remove it (`loom_set_thread_deps` → `[]`). Two follow-ons: it'd then go `⚪` ready, and — it already has a **done plan** (`done/pl_01KTZWZ…`), so is it actually pending future work, or should the thread read further-along than "🔴 blocked future"? Worth confirming its real status.

3. **Optional:** the three *new-code* guitar threads (`octave-shapes`, `interval-lattice`, `caged-system`) add code to the area `instrument-boundary` founds — so strictly they depend on it. Priority already puts boundary first (p10), so this is cosmetic-honesty, not a blocker; add `instrument-boundary` to their `depends_on` if you want the graph to *say* it, or leave it to priority. (`guitar-voicings` is excluded — it's already-built code that'll just move.)

(And noted on Loom — glad it's earning its keep. The reorg we just did is a decent stress-test of the weave/thread/dep model and it held up.)

Tell me **A or B** on the refs and I'll write them; the three roadmap tweaks are CRUD on your side whenever you agree. **Holding.**

## Rafa:

### Documentation

(A)

### Roadmap

1. `instrument-rendering` is now P80 after all important base threads
2. `guitar-voicings` was splitted into to slices:
slice 1: the authored-voicing content pillar end to end, done, shipped in v0.5.0
slice 2: difficulty bands / simplifier, deferred

I think we can close `guitar-voicings` marking all done, correct?
3. `octave-shapes`, `interval-lattice`, `caged-system` now depend on `instrument-boundary`

## TODO

when all is done
if no pending tasks
commit, push