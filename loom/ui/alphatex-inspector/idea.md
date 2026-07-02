---
type: idea
id: id_01KV6MNTQMPZXM49TM3GQWCN5K
title: alphaTex inspector — live edit + render/play the engine's output
status: done
created: "2026-06-15T00:00:00.000Z"
updated: 2026-06-16
version: 2
tags: []
parent_id: null
requires_load: []
---
# alphaTex inspector — live edit + render/play the engine's output

## The idea

A **debug/dev surface that shows the alphaTex the engine just emitted, lets you edit
it freely, and renders/plays the edited text** through the same `ChordFlowScore`
component. A round-trip inspector sitting directly on the **engine↔alphaTab seam** —
the one string that is both the bridge payload and the entire contract between
`AlphaTexRenderer` and alphaTab.

## Why it's worth building now

The current workbench has "many issues" (Rafa) — and almost all rendering/playback
bugs are *somewhere on the alphaTex seam*. This tool answers the only question that
matters when a score looks wrong: **is the C# emit wrong, or is alphaTab interpreting
correct alphaTex differently than we expect?**

- **Isolates the bug.** Read the emitted tex; if it's wrong, the fix is in
  `AlphaTexRenderer`. If it looks right but renders wrong, hand-edit it until it's
  right — now you know the renderer's target output and alphaTab's real behaviour.
- **Tightens the loop.** Edit-and-rerender beats regenerate-rebuild-relaunch for
  every notation/timing/two-track/chord-diagram question.
- **A scratchpad for the DSL ref.** Verify alphaTex syntax against
  `alphatex-syntax-reference.md` live before encoding it in the renderer (several
  ref entries are flagged "unverified — smoke-test in the app").

## Cheap by construction (the strong argument)

The `loadScore` envelope **already carries `tex`**, and `app.js` already receives it
(`msg.tex`). `ChordFlowScore.load(tex)` already renders **and** plays a raw alphaTex
string. So the MVP is essentially **front-end only**:

1. capture the last `loadScore.tex` into an editable `<textarea>`,
2. a **Render/Play** button that calls `view.load(textarea.value)` on a
   `ChordFlowScore` instance — bypassing the bridge generate entirely.

No new bridge verb, no Core change for v1.

## Shape (to design)

- **Where it lives:** a third **Debug** view toggle (peer of Practice/Content) vs a
  collapsible panel under the Practice score vs a dev-only affordance. (Leaning: its
  own view, so it doesn't clutter Practice.)
- **Its own `ChordFlowScore`** in full-player mode, fed straight from the textarea.
- **"Load current"** — pull the tex from the last generated/loaded score so you start
  from real engine output, not a blank box.

## Open questions

1. Debug view vs inline panel vs dev-only flag.
2. Always-on, or gated behind a dev/debug toggle (it exposes raw engine internals)?
3. Any feedback path (e.g. "copy edited tex", or a diff vs the generated tex), or is
   it strictly a read-edit-render scratchpad in v1?
4. Future: also surface the **render inputs** (the chosen Song/Comping/Lead + params)
   and/or the intermediate `RealizedSong`, not just the final tex.

## Relationship to the `ui` weave

Peer of [[exercise-workbench]] (consumes the same `loadScore` tex + `ChordFlowScore`)
and [[score-render-component]] (the render component it drives); sibling of
[[content-crud]]. A pure **diagnostic** surface — it reads/echoes the seam, it doesn't
add engine capability.

Related: `alphatex-syntax-reference.md`, [[chordflow-architecture-reference]],
[[design-philosophy-durable-over-minimal]].