---
type: plan
id: pl_01KVQKA0GZ2W4J8ZYQ46ZZF5YF
title: alphaTex debug panel in the shared score component
status: done
created: 2026-06-22
updated: 2026-06-22
version: 1
design_version: 1
req_version: 1
tags: []
parent_id: de_01KVQK4VV6C4YHS2N04PMGFSC6
requires_load: []
target_version: 0.1.0
actual_release: 0.10.0
steps:
  - id: debugpanel-in-score-render-component-js
    order: 1
    status: done
    description: "Component: add the debugPanel option, capture lastHostTex in load(), build the collapsed panel (textarea + Render from alphaTex + Reload from engine + alphaTab version label), and implement the dirty-state rule."
    files_touched: [src/ChordFlow.Desktop/wwwroot/score-render-component.js]
    blocked_by: []
    satisfies: [IN1, IN2, IN3, IN4, IN5, IN6, IN7, IN8, C1, C2, C3]
  - id: debug-panel-styles
    order: 2
    status: done
    description: "Panel CSS: styles for the collapsible debug panel, monospace textarea, and its buttons, alongside the existing .cf-controls block."
    files_touched: [src/ChordFlow.Desktop/wwwroot/index.html]
    blocked_by: []
    satisfies: [IN12, C4]
  - id: retire-inspector-opt-consumers-in
    order: 3
    status: done
    description: "Retire the standalone Debug view and opt the score-rendering consumers into debugPanel: remove the Debug nav segment + #debug-view container, drop the Debug branch from the app.js view toggle, pass debugPanel:true in Practice and the Content-CRUD preview, and delete alphatex-inspector.js."
    files_touched: [src/ChordFlow.Desktop/wwwroot/index.html, src/ChordFlow.Desktop/wwwroot/app.js, src/ChordFlow.Desktop/wwwroot/content-crud.js, src/ChordFlow.Desktop/wwwroot/alphatex-inspector.js]
    blocked_by: []
    satisfies: [IN9, IN10, IN11]
  - id: arch-ref-sync
    order: 4
    status: done
    description: "Reference-doc sync: update chordflow-architecture-reference.md §2 wwwroot inventory and §5 fan-out note — inspector removed, Debug view retired, edit→render scratchpad now an opt-in debugPanel on the shared component."
    files_touched: [loom/refs/chordflow-architecture-reference.md]
    blocked_by: []
    satisfies: [IN13]
---
# alphaTex debug panel in the shared score component

## Goal

Build an editable alphaTex debug panel into the shared ChordFlowScore render component (score-render-component.js) — opt-in via a new `debugPanel` option, collapsed by default — so every score-rendering page (Practice, Content preview) gets a live edit→render scratchpad on the engine↔alphaTab seam, prefilled with the tex that staff is rendering. Retire and delete the standalone Debug view (alphatex-inspector.js), folding its scratch-start + version label into the panel. Pure front-end work in wwwroot; no Core/bridge/DSL change (the tex is already in hand via load(tex)). Implements req IN1–IN13 under constraints C1–C5.

---

## Steps

| Done | # | Step | Files touched | Blocked by | Satisfies |
|---|---|---|---|---|---|
| ✅ | 1 | Component: add the debugPanel option, capture lastHostTex in load(), build the collapsed panel (textarea + Render from alphaTex + Reload from engine + alphaTab version label), and implement the dirty-state rule. | src/ChordFlow.Desktop/wwwroot/score-render-component.js | — | IN1, IN2, IN3, IN4, IN5, IN6, IN7, IN8, C1, C2, C3 |
| ✅ | 2 | Panel CSS: styles for the collapsible debug panel, monospace textarea, and its buttons, alongside the existing .cf-controls block. | src/ChordFlow.Desktop/wwwroot/index.html | — | IN12, C4 |
| ✅ | 3 | Retire the standalone Debug view and opt the score-rendering consumers into debugPanel: remove the Debug nav segment + #debug-view container, drop the Debug branch from the app.js view toggle, pass debugPanel:true in Practice and the Content-CRUD preview, and delete alphatex-inspector.js. | src/ChordFlow.Desktop/wwwroot/index.html, src/ChordFlow.Desktop/wwwroot/app.js, src/ChordFlow.Desktop/wwwroot/content-crud.js, src/ChordFlow.Desktop/wwwroot/alphatex-inspector.js | — | IN9, IN10, IN11 |
| ✅ | 4 | Reference-doc sync: update chordflow-architecture-reference.md §2 wwwroot inventory and §5 fan-out note — inspector removed, Debug view retired, edit→render scratchpad now an opt-in debugPanel on the shared component. | loom/refs/chordflow-architecture-reference.md | — | IN13 |
---

### Legend

| Symbol | Meaning |
|--------|---------|
| ✅ | Done |
| 🔄 | In Progress |
| 🔳 | Pending |
| ❌ | Cancelled |

<!-- step:debugpanel-in-score-render-component-js -->
### Step 1 — debugPanel in score-render-component.js

In `create`, read `opts.debugPanel` (default false). In `handle.load(tex, o)` stash `lastHostTex = tex` (it currently calls `api.tex(tex)` without keeping the string); if the panel exists and is not dirty, mirror `tex` into the textarea. Add a `buildDebugPanel(handle)` builder appended after `surface`: a `<details>` collapsed container holding a monospace `<textarea>`, a **Render from alphaTex** button (`api.tex(textarea.value)`), a **Reload from engine** button (textarea ← lastHostTex, render, clear dirty), and a version span (`alphaTab.meta.version`, guarded). Wire dirty: textarea `input` sets dirty=true; while dirty a host `load()` does not overwrite the textarea but shows a small 'engine output changed — Reload from engine' hint; Reload clears dirty. Carry the inspector's `SAMPLE_TEX` as the empty-box fallback. `dispose()`'s `container.innerHTML = ''` already tears the panel down.

<!-- step:debug-panel-styles -->
### Step 2 — Debug-panel styles

Add a small style block for the panel (collapsible summary, full-width monospace textarea, button row, the dirty hint). Verify during impl where .cf-controls is defined (index.html <style> vs. a stylesheet) and colocate there. Vanilla CSS — no build step.

<!-- step:retire-inspector-opt-consumers-in -->
### Step 3 — Retire inspector + opt consumers in

index.html: remove the Debug nav segment + `#debug-view` container + the inspector <script> tag (Practice/Content/Scales segments stay — toggle remains N-way). app.js: drop the Debug branch from the view toggle, stop initializing ChordFlowInspector, pass `debugPanel: true` to Practice's ChordFlowScore.create. content-crud.js: pass `debugPanel: true` to the preview's create. Delete alphatex-inspector.js (its SAMPLE_TEX + version label already moved into the component in Step 1).
