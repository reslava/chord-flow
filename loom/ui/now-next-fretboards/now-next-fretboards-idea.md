---
type: idea
id: id_01KVSYWCXFGDW3EX0Q02MR8895
title: Now/Next Fretboards
status: done
created: 2026-06-23
updated: 2026-06-23
version: 3
tags: []
parent_id: null
requires_load: []
---
# Now/Next Fretboards

## Goal

Pin **two FretR** above the ScoreR — **current chord** (now) and **next chord coming** (next) — synchronized with score playback. As the cursor moves through Practice / Progressions / Songs, the two fretboards update to show the chord being played and the one approaching.

This is the first slice of a broader "live theory overlay" on the score: the same "what chord is active right now" signal later drives guide tones, scales, and arpeggios on the same fretboards.

## Motivation

A rhythm/progression trainer should make the harmony *legible while it plays* — not just notation on a staff. Seeing the current shape and pre-reading the next one trains anticipation, which is exactly the skill comping demands.

## Source of truth (key decision)

The C# **engine is the single source of truth for harmony.** It already knows the full chord schedule because it generated the exercise from a Progression/Song. The engine therefore emits a **`ChordChange[]`** alongside the alphaTex in the load envelope:

```
interface ChordChange { bar: number; beat: number; chord: Chord; }
```

JS **never re-derives chords from the rendered notation.** It only maps the engine's `ChordChange[]` onto alphaTab beats and reacts to playback. This touches the **Bridge contract** (a new field next to the alphaTex string) and the **renderer** (emit the schedule), so the design step must define that envelope shape.

## Approach

1. **Engine emits the schedule** — `ChordChange[]` (bar/beat → chord, plus the comped voicing for each) ships in the load envelope with the alphaTex.
2. **Build a beat→chord lookup** on score load: walk `api.score.tracks → staves → bars → voices → beats`, matching `(barIndex, beatInBar)` against the `ChordChange[]`, storing `chordIndexByBeat`.
   - **Key choice (empirical):** prefer `beat.id` if ids are stable across re-renders; otherwise key on the positional `(barIndex, beatOrdinalInBar)` counter. Decide once via the Validation probe, then hardcode the winner.
3. **Drive from `activeBeatsChanged`** — on each event, take the **comping track's** active beat (`e.activeBeats[0]`; comping/rhythm guitar is the invariant track 0), look up its chord index, and if it changed, update the now FretR with `chordChanges[i]` and the next FretR with `chordChanges[i+1] ?? null`.
4. **FretR title slot** — FretR gains a general **label slot** (renders the chord name now; reused later for "now"/"next" tags and guide-tone labels). Designed as a generic label, not a hardcoded chord-name field.
5. **Voicing shown** — the **comped voicing** from the schedule (what the score actually plays). Canonical/CAGED shapes are a later toggle.
6. **Layout** — two FretR instances stacked/side-by-side above the ScoreR, reusing `fretboard-render-component`.

## Scope

**In:**
- Engine `ChordChange[]` emission in the load envelope (Bridge + renderer seam).
- Two FretR above ScoreR, synced via `activeBeatsChanged`.
- FretR title/label slot.
- Comped-voicing display; next = next *distinct* change, blank/"—" at end of piece.
- Works for mono- and multi-track scores (comping track = index 0).

**Out (future):**
- Guide tones / scales / arpeggios overlays (this slice only proves the "now/next chord" signal).
- Canonical / CAGED shape toggle.
- Any harmony re-derivation in JS.

## Open questions

- Exact **load-envelope shape** for `ChordChange[]` (sibling field vs nested) — decide in design alongside the Bridge contract.
- FretR layout: stacked vs side-by-side, and behavior on narrow widths.

## Validation

- **beat.id stability probe** — confirm whether alphaTab `beat.id`s survive a re-render (layout toggle, bars-per-row change, resize). Result selects the lookup key (id vs positional). One-time empirical check.
- **Sync correctness** — now/next FretR track the cursor through a multi-chord exercise; next blanks at the end.
- **Dogfood** — render on the fretboard UI page: the two FretR + a playback timeline *is* the dogfood surface for this feature (per the guitar-weave dogfood rule).
