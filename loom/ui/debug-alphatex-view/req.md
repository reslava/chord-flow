---
type: req
id: rq_01KVQK5GW5ADNDBJ8AB07BR762
title: Show alphaTex — always-visible DSL debug view — Requirements
status: locked
created: 2026-06-22
updated: 2026-06-22
version: 1
design_version: 2
tags: []
parent_id: de_01KVQK4VV6C4YHS2N04PMGFSC6
requires_load: []
---
# Show alphaTex — always-visible DSL debug view — Requirements

An **editable alphaTex debug panel built into the shared `ChordFlowScore` render component**, opt-in per instance and collapsed by default, available on every screen that renders a score. Relocates and **retires** the standalone Debug view (`alphatex-inspector.js`). Pure front-end diagnostic — no Core/bridge/DSL change. Scope confirmed in `debug-alphatex-view-chat-001` + `debug-alphatex-view-design`.

### ✅ Included

- `IN1` A new **`debugPanel` boolean option** on `ChordFlowScore.create(container, opts)` (orthogonal to `player`/`controls`); default off. When on, the component renders a collapsible alphaTex panel below the score surface.
- `IN2` The panel is **collapsed by default** (`<details>`-style) — present with zero clutter until expanded.
- `IN3` A **textarea** in the panel, prefilled with the alphaTex the component is currently rendering (the last string passed to `load(tex)`).
- `IN4` A **`Render from alphaTex`** button that renders the textarea content to *this* component's staff via the existing alphaTab path (`api.tex(value)`), bypassing C# generation entirely.
- `IN5` A **`Reload from engine`** button that discards edits, copies the last host tex back into the textarea, renders it, and clears the dirty state.
- `IN6` **`load(tex)` retains the string** (`lastHostTex`) — currently it calls `api.tex(tex)` without keeping it; the panel needs the captured value for prefill and reload.
- `IN7` **Dirty-state rule:** while the user has edited the textarea, host `load(tex)` calls still render to the staff (contract unchanged) but **do not overwrite the textarea**; the panel shows an *"engine output changed — Reload from engine"* hint. `Reload from engine` clears dirty. (Approved: dirty-state + explicit reload.)
- `IN8` The **alphaTab version label** (`alphaTab.meta.version`, guarded for absence) shown in the panel — carried over from the retired inspector.
- `IN9` **Retire the standalone Debug view:** remove the Debug nav segment + `#debug-view` container from `index.html`, drop the Debug branch from the `app.js` view toggle (Practice/Content/Scales remain), and stop loading the inspector.
- `IN10` **Delete `alphatex-inspector.js`**; fold its `SAMPLE_TEX` empty-box fallback + version label into the component panel.
- `IN11` **Opt Practice (`app.js`) and the Content-CRUD preview (`content-crud.js`) into `debugPanel: true`** so both score-rendering pages get the panel.
- `IN12` A small **CSS block** for the panel (collapsible container, monospace textarea, the two buttons) alongside the existing `.cf-controls` styles.
- `IN13` **Reference-doc update in the same unit of work:** `chordflow-architecture-reference.md` §2 (`wwwroot` inventory — inspector removed) and §5 (Debug view retired; edit→render scratchpad now an opt-in `debugPanel` on the shared component).

### ❌ Excluded

- `EX1` **Saving edited tex as a custom entity** / a debug surface that loads any page (the idea's "future direction") — deferred; this is a read-edit-render scratchpad, not an authoring surface.
- `EX2` **Any Core / bridge / DSL change** — no new envelope, no `AlphaTexRenderer` or `RenderOptions` change. The tex is already in hand via `load(tex)`.
- `EX3` A **diff view** (edited vs. engine tex) — possibly later; v1 ships only the dirty hint.
- `EX4` **Surfacing render inputs / intermediate `RealizedSong`** (the inspector's old open question #4) — not in scope.

### ⛓ Constraints

- `C1` alphaTex is **never built in JS** — the panel only edits/echoes a string and feeds it to `api.tex(...)`; generation stays in C# `AlphaTexRenderer` (architecture rule).
- `C2` The existing `load(tex)` behavior stays **byte-identical** for every current consumer when `debugPanel` is off or the panel is not dirty.
- `C3` `debugPanel` is **orthogonal to `controls`** — it renders independently of the control-strip profile (full/mini/none).
- `C4` **No new build step or framework** in `wwwroot` — vanilla JS over the existing virtual host.
- `C5` Dependency direction **Desktop → Core** unchanged; pure `wwwroot` work, the engine is untouched.
