---
type: idea
id: id_01KW0HVVBWNZ9J9JKDWE9KY1TX
title: Two-digit fret-position label clipped in the SVG fret-box
status: draft
created: 2026-06-25
version: 1
tags: []
parent_id: null
requires_load: []
---
# Two-digit fret-position label clipped in the SVG fret-box

## Goal

Fix the **fret-position label clipping** in the shared `fretboard-render-component.js` SVG fret-box: a two-digit position label (e.g. `10fr`) renders as `0fr` — the leading `1` falls outside the SVG canvas / viewBox. Single-digit labels (`5fr`) are fine.

## Origin

Found in [[engine-derived-as-app-source]] (chat-002) re-dogfood: the voicing `voicing C7 shape:D root:4 frets: x x 10 12 11 12` shows its `10fr` marker as `0fr`. Now common because engine-derived `automatic` voicings sit up the neck (CAGED shapes past fret 9), where single-digit authored shell voicings never did. Not engine-derived-as-app-source's bug — it's the renderer component.

## Why

The fret-position label tells the player which region the box sits in; clipping it to `0fr` is actively misleading (reads as open position).

## Shape (sketch — design firms this up)

- Locate the position-label `<text>` in `fretboard-render-component.js` — likely left of the nut/first fret with a fixed x-offset sized for one glyph.
- Fix: widen the SVG canvas/viewBox left margin (or right-anchor the label, or measure its width) so a two-digit label fits. Check both orientations (vertical chord-box + horizontal neck) and the Scales page.

## Scope

**In:** the label-positioning / canvas-margin fix in `fretboard-render-component.js` + a check across its consumers (voicing fret-box, now/next fretboards, Scales). **Out:** any engine/voicing logic — purely a JS SVG layout fix.

## Validation

- A grip at fret ≥ 10 shows its full `10fr` / `12fr` label, both orientations.
- Single-digit labels unchanged.
- Dogfood: the `D`-shape voicings (canonical C at fret 10+) render their position label correctly on the Content fret-box.