---
type: design
id: de_01KVQK4VV6C4YHS2N04PMGFSC6
title: alphaTex debug panel in the shared score component
status: done
created: 2026-06-22
updated: 2026-06-22
version: 2
idea_version: 2
tags: []
parent_id: id_01KVQGJXY8VJMKMR25N337ASK6
requires_load: []
---
# alphaTex debug panel in the shared score component

An **editable alphaTex panel built into the shared `ChordFlowScore` component** (`score-render-component.js`), opt-in per instance and **collapsed by default**, so every screen that renders a score gains a live edit→render scratchpad on the engine↔alphaTab seam. This **relocates and supersedes the standalone Debug view** (`alphatex-inspector.js`), which is retired in the same unit of work.

> Origin discussion: `chats/debug-alphatex-view-chat-001.md`. Idea: `debug-alphatex-view-idea.md`.

---

## 1. Why (and how it differs from what shipped)

The `alphatex-inspector` thread already shipped a Debug **view** that does the edit→render round-trip — but it's a *separate tab* fed only by "Load current", isolated from the score you're actually looking at. The limitation Rafa hit: **it only reflects the exercises (Practice) page**, and you must switch views and click "Load current" to use it.

Moving the capability **into the shared component** fixes both: the panel sits directly under the staff it describes, on **every page that uses `ChordFlowScore`** (Practice and the Content-CRUD preview today), pre-filled with the exact tex that staff is rendering. Edit it, hit **Render from alphaTex**, and the same component re-renders the edited string — the tightest possible loop for "is the C# emit wrong, or is alphaTab interpreting correct tex differently than we expect?"

This is a pure diagnostic surface. It reads/echoes the seam; it adds no engine capability and touches no Core/bridge/DSL code (see `chordflow-architecture-reference` §5 — the render component is the JS display layer; alphaTex is generated only in C# `AlphaTexRenderer`).

---

## 2. Shape

A new opt-in option on the existing factory:

```js
const view = ChordFlowScore.create(containerEl, {
  player: true, controls: "full",
  debugPanel: true,        // NEW — render a collapsed alphaTex panel under the staff
  …
});
```

When `debugPanel` is on, the component renders a `<details>`-style collapsible panel **below the score surface** containing:

- A **textarea** prefilled with the alphaTex currently rendered (the last string passed to `load(tex)`).
- **Render from alphaTex** — renders the textarea content to *this* component's staff (`api.tex(textarea.value)`), bypassing C# entirely.
- **Reload from engine** — discards edits, copies the last host tex back into the textarea, renders it, clears the dirty flag.
- The **alphaTab version label** (`alphaTab.meta.version`), carried over from the inspector, so triage never doubts which engine build is loaded.

Collapsed by default (zero clutter); a consumer that wants it open can expand it. No new toolbar profile — `debugPanel` is orthogonal to `controls`, so any consumer (full/mini/none) can flip it on.

---

## 3. The dirty-state rule (the one real behavior decision — approved)

The component renders from two sources: the **host** (each consumer `load(tex)` call — boot score, generate, content-toggle re-render via `onNeedsRerender`) and the **user** (Render from alphaTex). They conflict when the user has hand-edited tex and a host re-render arrives. Rule (Rafa-approved: *dirty-state + explicit reload*):

- **Not dirty:** every host `load(tex)` mirrors the new tex into the textarea (the panel tracks live engine output).
- **User edits the textarea** → panel goes **dirty**.
- **While dirty:** host `load(tex)` still renders to the staff as today (the component's contract is unchanged — `load()` is never suppressed), but it **does not overwrite the textarea**, and the panel shows a small *"engine output changed — Reload from engine"* hint so the divergence is visible, not silent.
- **Render from alphaTex** renders the textarea to the staff; stays dirty.
- **Reload from engine** syncs textarea ← last host tex, renders it, clears dirty.

This keeps the existing `load()` behavior byte-identical for every current consumer and adds only a textarea mirror + a dirty guard.

---

## 4. Implementation surface (all in `wwwroot`)

**`score-render-component.js`** (the substance):
- Stash the last tex: `handle.load(tex, o)` currently calls `api.tex(tex)` **without retaining the string** — add `lastHostTex = tex`, and if `debugPanel` is on and not dirty, mirror it into the textarea.
- `buildControls`/`create` gain a `debugPanel` branch that builds the collapsible panel (textarea + two buttons + version label) appended after `surface`.
- `dispose()` already does `container.innerHTML = ""`, so the panel tears down with the rest.

**`index.html`** — remove the **Debug** nav segment and the `#debug-view` container. Practice/Content/Scales segments stay (the view toggle remains N-way, just one fewer entry).

**`app.js`** — drop the Debug branch from the view toggle; pass `debugPanel: true` to Practice's `ChordFlowScore.create(...)`. Stop loading/initializing the inspector.

**`content-crud.js`** — pass `debugPanel: true` to the preview's `ChordFlowScore.create(...)` so Content previews get the panel too.

**`alphatex-inspector.js`** — **deleted.** Its two carried-over pieces (the `SAMPLE_TEX` scratch-start fallback for an empty box, the alphaTab version label) move into the component panel.

---

## 5. Out of scope / deferred

- **Save edited tex as a custom entity / "load any page"** (the idea's "future direction") — explicitly deferred; this is a read-edit-render scratchpad, not an authoring surface. Persisting/authoring belongs to `content-crud` or a future thread.
- **Any Core / bridge / DSL change** — none. No new envelope, no `AlphaTexRenderer` change, no `RenderOptions` change. The component already receives the full tex in `load(tex)`.
- **A diff view** (edited vs. engine tex) — possible later; v1 is just the dirty hint.

---

## 6. Reference-doc updates (same unit of work)

- `chordflow-architecture-reference.md` — update the §2 `wwwroot` inventory and §5 fan-out note: `alphatex-inspector.js` is removed and the Debug view retired; the alphaTex edit→render scratchpad is now an opt-in `debugPanel` on the shared `score-render-component.js`. (No bridge-protocol change.)
- Domain-model and DSL refs — untouched (no Core/DSL change).

---

## 7. Open questions / risks

1. **Panel placement vs. `controls:"none"` consumers** — none today use the score component without controls, but the panel must render independently of the control strip (it's gated by `debugPanel`, not `controls`). Confirmed in the contract above.
2. **CSS** — the panel needs a small stylesheet block (collapsible, monospace textarea) alongside the existing `.cf-controls` styles.
