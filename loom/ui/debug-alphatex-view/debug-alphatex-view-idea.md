---
type: idea
id: id_01KVQGJXY8VJMKMR25N337ASK6
title: Show alphaTex — always-visible DSL debug view
status: draft
created: 2026-06-22
version: 1
tags: []
parent_id: null
requires_load: []
---
# Show alphaTex — always-visible DSL debug view

## The idea

When debugging the app, the **raw alphaTex text** is the ground truth for what the renderer emitted —
but today it's not visible in the UI, so diagnosing a render issue means reading code or tests. Add a
**"show alphaTex"** affordance to the score-render component so the emitted alphaTex is viewable alongside
the rendered score.

Surfaced while dogfooding the `anacrusis` thread — confirming the `\ac` pickup render would have been
trivial with the alphaTex visible in-app.

## What this adds

- A **toggle / panel in the score-render component** that shows the current alphaTex string the renderer
  produced for the loaded exercise.
- Read-only first (just *see* it); the edit/save path below is a later layer.

## Future direction (not v1)

- A **Debug surface that can load any page** (not just exercises), let the user **edit the DSL** live, and
  **save it as a custom entity** — a fast authoring/experimentation loop on top of the same render path.

## Notes / open questions

- Where it lives: a dev-only panel vs. a first-class "source view" toggle. Likely behind a debug flag at first.
- It reads the alphaTex the C# renderer already produces (the bridge payload is the alphaTex string), so
  this is mostly a UI/bridge surfacing task — no renderer change.

Related: `ui/score-render-component`, `ui/alphatex-inspector` (overlap — check before designing),
[[chordflow-architecture-reference]].