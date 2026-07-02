---
type: design
id: de_01KVSZBZQ1WZB5S31GSNS0F3QD
title: Now/Next Fretboards
status: done
created: 2026-06-23
updated: 2026-06-23
version: 2
idea_version: 3
tags: []
parent_id: id_01KVSYWCXFGDW3EX0Q02MR8895
requires_load: []
---
# Now/Next Fretboards

## 1. Summary

Two FretR above the ScoreR show the **current** and **next** chord as a real fretboard voicing, synced to playback. The engine is the single source of truth: it emits a **chord schedule** (one entry per chord change — bar/beat position + chord name + a `FretboardDiagram` of the comped voicing) alongside the alphaTex in the `loadScore` envelope. JS builds a beat→schedule lookup on load and updates the two FretR from the **already-wired** `activeBeatsChanged` handler.

This design is grounded in the existing code; §2 lists what we reuse so the new surface stays small.

## 2. What already exists (reuse map)

- **`activeBeatsChanged` is already handled** — `score-render-component.js:382` already subscribes and computes the active beat's `(bar, beatInBar)` (both 1-based) from `beats[0]`, calling `cb.onBeat(bar, beatInBar)`. The now/next lookup hangs off this exact handler — **no new alphaTab event wiring**.
- **The renderer already realizes the concrete comped voicing per chord** — `AlphaTexRenderer.RenderBar` calls `Voice(chord, difficulty, options)` to get a real `Voicing` (concrete frets at the real root) for the chord covering each slot's onset tick, and already detects **chord-change boundaries** via `RenderState.CurrentChordName`. The schedule is a by-product of this same walk (§4).
- **FretR already renders a title** — `fretboard-render-component.js:158` draws `model.title`; `FretboardDiagram` carries `Title`. So the chord name shows for free; only the "Now/Next" role framing is additive (§6).
- **FretR already consumes a `FretboardDiagram` over the bridge** — content-crud's voicing preview (`content-crud.js:323`) does `ChordFlowFretboard.create(...).render(msg.diagram)`. Same model shape carries each schedule entry's diagram.

## 3. The chord schedule (data shape)

Per chord *change* (not per beat), the engine emits:

```
record ChordChange(int Bar, int Beat, string Name, FretboardDiagram Diagram);
```

- `Bar` / `Beat` — **0-based positional key matching alphaTab's indexing**: `Bar` = master-bar index, `Beat` = the rendered beat's ordinal **within that bar's voice** (the slot index, counting rendered beats incl. rests, excluding nothing the renderer emits as a beat). This must line up with what `activeBeatsChanged` reports (`beat.voice.bar.index`, `beat.index`) — see §5 for the keying decision and the validation probe.
- `Name` — the chord label (e.g. `G7`), already computed by the renderer as `ChordSymbol.Format(chord, key)`. Becomes the FretR `title`.
- `Diagram` — a `FretboardDiagram` of the **actual comped voicing** at its **real root** (§4).

Entries are emitted **only at chord changes** (the boundaries the renderer already tracks). Beats between changes aren't keyed; JS holds the current chord until the next keyed beat. "Next" = `schedule[i+1]` (the next distinct change); blank/"—" past the last entry.

> **Multi-track invariant:** the comping (rhythm-guitar) track is always track 0, so `e.activeBeats.beats[0]` drives the chord. The schedule is built from the comping track only; the lead track never contributes a chord change. This invariant is now load-bearing — document it where the two-track render is assembled.

## 4. Producing the schedule (the key decision)

**Recommended — Option A: schedule as a by-product of the render pass.** The render walk already (a) resolves the chord covering each slot, (b) realizes its concrete `Voicing`, and (c) fires exactly at chord-change boundaries. We capture a `ChordChange` at each of those boundaries — reusing the same change detection that drives `{ch "…"}` — and turn the concrete `Voicing` into a `FretboardDiagram` via a new producer (§4.1). The render seam then returns **`(tex, schedule)`** instead of a bare string.

- *Pro:* one source of truth — the diagram, name, and position are exactly what the tab comps and plays; zero drift.
- *Con:* `IScoreRenderer.Render` / `LoadScoreEnvelope.From` gain a second output. This is the **API-shape decision that needs sign-off** (§9, D1).

**Rejected — Option B: a sibling builder** that re-walks `RealizedSong` + `GuitarInstrument` independently. It duplicates chord-covering + voicing-resolution logic, so the FretR could show a different voicing than the tab. Drift risk kills it.

### 4.1 New diagram producer — concrete `Voicing` at real root

`VoicingDiagram.Build` exists but is **canonical-C only** (works from a `VoicingShape`, anchors to C; "movability is a later add"). The FretR needs each chord at its **real** root with intervals/functions relative to that root. New producer:

```
FretboardDiagram RealizedVoicingDiagram.Build(Chord chord, Voicing voicing, Key key)
```

Same marker logic as `VoicingDiagram` (per sounding string: pitch class → interval vs the chord root → chord-tone function → label/spelled note → Circle marker; muted strings as chrome; `FretMin` from the voicing), but anchored at `chord.Root` instead of C, `Title = ChordSymbol.Format(chord, key)`. Lives in `Instruments/Guitar/Diagrams/` next to its siblings. (`Rendering → Instruments` is an allowed edge, so the renderer producing diagrams is within architecture.)

## 5. Beat keying (positional — already computed)

`beat.id` stability across re-renders is unverified, so we key on the **positional** `(bar.index, beat.index)` — which the component **already extracts** in the `activeBeatsChanged` handler. The schedule's `(Bar, Beat)` uses the same ordinals. The one risk is a mismatch between the renderer's notion of "beat ordinal in bar" and alphaTab's `beat.index` (e.g. how rests or tuplet slots count). This is the **one empirical thing to verify** (§10) — if they diverge, the fix is to align the schedule's `Beat` to alphaTab's post-parse `beat.index`, not to invent ids.

## 6. JS wiring

1. **On `loadScore`** (carrying `schedule`): build `chordIndexByKey: Map<"bar:beat", number>` from the schedule. Store the schedule array.
2. **In the existing `activeBeatsChanged` handler**: after computing `(bar, beatInBar)`, look up `chordIndexByKey.get(key)`. If undefined or unchanged, do nothing (hold current). On a change, set `currentIndex`, and update the two FretR: now = `schedule[i].diagram`, next = `schedule[i+1]?.diagram ?? null` (next renders a blank/"—" state).
3. **Two FretR instances** above the ScoreR, created via `ChordFlowFretboard.create`, `controls:{ orientation:false, fretWindow:false }` (fixed vertical chord-box, no per-box toolbar noise), each rendering its diagram.
4. **Now/Next framing**: the chord name already shows as the diagram `title`. The "Now"/"Next" role label is a small static caption the consumer places above each box (a wrapper `<div>`, not a component change) — keeps the component generic.
5. **Reset** on new `loadScore` and on `stop` (clear to the first chord or blank).

Where this lives: the now/next pair is a small consumer module mounted alongside the ScoreR on the Practice / Progressions / Songs views (it observes the same score component). Exact host element + whether it's folded into `score-render-component`'s consumer or a sibling module is a layout detail (§9, D2).

## 7. Scope

**In:** engine schedule emission (render-seam change + `RealizedVoicingDiagram`); `loadScore` envelope `schedule` field + camelCase serialization; beat→schedule lookup; two FretR off the existing `activeBeatsChanged`; now/next captions; reset on load/stop; mono + multi-track (comping = track 0).

**Out (future):** guide tones / scales / arpeggios overlays (this slice proves the now/next-chord signal only); canonical/CAGED-shape toggle; any harmony re-derivation in JS; movable-root **voicing-preview** unification (this design adds a real-root *diagram producer*; retrofitting the Content voicing preview onto it is separate).

## 8. Data flow (delta over architecture §6)

```
… AlphaTexRenderer.Render(RealizedSong, …)  →  (tex, schedule)     ← NEW second output
  schedule[i] = ChordChange(bar, beat, name, RealizedVoicingDiagram.Build(chord, voicing, key))
  → LoadScoreEnvelope { type:"loadScore", tex, tempo, schedule }   ← NEW field
  → JS: build chordIndexByKey from schedule
  → activeBeatsChanged (existing) → lookup → update now/next FretR
```

## 9. Decisions needing sign-off

- **D1 — render-seam shape (API).** Approve Option A: `IScoreRenderer.Render` (and `LoadScoreEnvelope.From`) return `(tex, ChordChange[] schedule)` rather than a bare `string`. Alternative is Option B (sibling builder) — rejected for drift. *This changes a Core public signature, so it needs your explicit OK.*
- **D2 — layout & mount.** Two FretR stacked vs side-by-side, and whether the now/next pair is a sibling JS module or folded into the score component's consumer. Leaning side-by-side, sibling module. Low-stakes; can settle at plan time.

## 10. Validation

- **Beat-ordinal alignment probe** — confirm the renderer's `(Bar, Beat)` ordinals match alphaTab's `beat.voice.bar.index` / `beat.index` after parse (rests/tuplets are the risk). One-time; selects nothing else if they already agree.
- **Sync correctness** — now/next track the cursor through a multi-chord, multi-bar exercise (incl. a bar with an interior chord change); next blanks at the end.
- **Voicing fidelity** — the now FretR shows the *same* fret shape the tab comps for that chord (Option A guarantees this by construction; assert on one known chord).
- **Dogfood** — render on the fretboard UI page: the two FretR + a playback timeline is itself the dogfood surface (guitar-weave dogfood rule).
