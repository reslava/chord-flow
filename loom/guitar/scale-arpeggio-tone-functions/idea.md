---
type: idea
id: id_01KW4093JAEMVR7EGATY5B2AQA
title: Scale & arpeggio tone functions
status: draft
created: 2026-06-27
version: 1
tags: []
parent_id: null
requires_load: []
---
# Scale & arpeggio tone functions

## Goal

Decide how **scales and arpeggios** model tone function, spelling, and colour on the fretboard — and, as the concrete first step, make `IntervalSetDiagram`'s default colour bucket **degree-aware** (consistent with the chord-tone classification now driven by the formula degree).

## Origin

Spun off from `guitar/caged-sixth-voicings` (chat-001, 2026-06-27). That thread made chord-tone **function derive from the formula degree** (new `ChordToneFunction.Sixth`; semitone 9 resolves to `6` for 6/m6 vs `bb7` for dim7) and fixed both chord-diagram producers. The scale/arpeggio producer (`IntervalSetDiagram`) was deliberately left out of scope — Rafa wants to think more deeply about scales/arpeggios before changing them.

## Why

`IntervalSetDiagram` still classifies its default colour bucket by **semitone band** (`FunctionFor`: 9/10/11 → "seventh"), the last place the old band-based classifier survives. Today it's invisible — the Scales page uses an override root/rest palette and the note **label is the user's typed token** (`6` stays `6`), so nothing is wrong on screen. But it's an inconsistency: the diagram already *has* the user's token, so it could bucket by the token's **degree** (`IntervalSpeller.Degree`) exactly like the chord path. Folding it in removes the last band-based classifier and keeps a future bare-render (no override palette) musically correct.

More broadly, scales/arpeggios differ from chords: they have **real octaves** (9/11/13 are meaningful), multiple notes per string, and no single "quality formula" — so the chord model (one formula → one function per degree) may not map cleanly. This thread is the place to think that through before committing to a model.

## Scope

**In (the concrete tweak):**
- Make `IntervalSetDiagram.FunctionFor` degree-aware: bucket by the token's degree (`6` → a `sixth`/amber bucket, `bb7` → `seventh`), so the dormant default palette matches the chord path.

**To think about (open, may split into follow-ups):**
- How should an arpeggio (a chord spelled as a scale) carry function — reuse `ChordToneFunction`, or a scale-degree model?
- Do scales need their own tension/extension vocabulary (9/11/13, #11, b13) as first-class, vs the chord `tension` catch-all?
- Should the Scales page expose function colouring at all, or stay token/root-only?

**Out:**
- Any chord-diagram change (done in `caged-sixth-voicings`).

## Open design questions (for design)

1. Is "function" even the right frame for a scale, or is it a scale-degree role (tonic/supertonic/…)? 
2. If `FunctionFor` becomes degree-aware, does `ChordToneFunction` stay the shared vocabulary, or do scales get their own?
3. Arpeggios sit between chords and scales — which model wins?

## Validation

- `IntervalSetDiagram` default-palette render of `1 b3 b5 6` colours the 6 as a sixth (not a seventh); `1 b3 b5 bb7` stays a seventh.
- dogfood: render scales/arpeggios on the fretboard UI page (Scales page) and confirm labels + colours read musically.
