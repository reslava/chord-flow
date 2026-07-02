---
type: plan
id: pl_01KV7G516WXBHC3ZACT70ZGBEH
title: alphaTex inspector — Debug view + diagnostics-driven render fixes
status: done
created: 2026-06-16
updated: 2026-06-16
version: 1
design_version: 1
tags: []
parent_id: id_01KV6MNTQMPZXM49TM3GQWCN5K
requires_load: []
target_version: 0.1.0
actual_release: 0.5.0
steps:
  - id: debug-view-mvp
    order: 1
    status: done
    description: "Debug view MVP: new alphatex-inspector.js (ChordFlowInspector) — eagerly cache every loadScore.tex off the bridge fan-out, lazily build DOM + an own full-player ChordFlowScore on first show; Load-current + Render buttons; generalize app.js view toggle from 2-way to N-way (Practice/Content/Debug); third Debug nav segment + container in index.html. No Core/bridge change."
    files_touched: [src/ChordFlow.Desktop/wwwroot/alphatex-inspector.js, src/ChordFlow.Desktop/wwwroot/index.html, src/ChordFlow.Desktop/wwwroot/app.js]
    blocked_by: []
    satisfies: []
  - id: version-label
    order: 2
    status: done
    description: "alphaTab version label: read alphaTab.meta.version at init and show it in the Debug toolbar (guarded for the global being absent) so triage never doubts which engine build is loaded."
    files_touched: [src/ChordFlow.Desktop/wwwroot/alphatex-inspector.js, src/ChordFlow.Desktop/wwwroot/index.html]
    blocked_by: []
    satisfies: []
  - id: two-track-render-fix
    order: 3
    status: done
    description: "Two-track render fix (inspector-surfaced): alphaTab renders only the first track by default, so two-track (comping+lead) exercises drew one staff. In the shared score-render-component scoreLoaded handler, call api.renderTracks(score.tracks) when tracks.length > 1; single-track stays on the default path. Fix lives in the shared component so Practice, inspector, and Content preview all get it. alphatab-js-api-reference updated."
    files_touched: [src/ChordFlow.Desktop/wwwroot/score-render-component.js]
    blocked_by: []
    satisfies: []
  - id: bars-per-row-layout-fix
    order: 4
    status: done
    description: "Bars-per-row layout fix + cleanup: defaultSystemsLayout/systemsLayout only bind on multi-track, so switch to display.barsPerRow on LayoutMode.Page (barsPerRow:4 default, -1 for the new Auto-layout toggle) — works single- and multi-track. Strip the now-inert { defaultSystemsLayout 4 } from AlphaTexRenderer and update the two-track renderer test. Refs synced (alphatex-syntax, domain-model, alphatab-js-api)."
    files_touched: [src/ChordFlow.Desktop/wwwroot/score-render-component.js, src/ChordFlow.Core/Rendering/AlphaTexRenderer.cs, tests/ChordFlow.Core.Tests/AlphaTexRendererTests.cs]
    blocked_by: []
    satisfies: []
---
# alphaTex inspector — Debug view + diagnostics-driven render fixes

## Goal

Retroactive record of the alphaTex-inspector work shipped in commit 3fd5dd8 (built front-end-first by explicit request, so it lands in plan→done history). A Debug view that captures the engine's last emitted alphaTex into an editable textarea and renders/plays it through the shared ChordFlowScore — the round-trip diagnostic on the AlphaTexRenderer↔alphaTab seam. Building it immediately paid off: it surfaced and isolated two real render bugs (multi-track and bars-per-row layout), which were fixed in the shared score component during the same unit of work. Front-end-only for the inspector itself; the two fixes touched the shared component and a small renderer cleanup.

---

## Steps

| Done | # | Step | Files touched | Blocked by | Satisfies |
|---|---|---|---|---|---|
| ✅ | 1 | Debug view MVP: new alphatex-inspector.js (ChordFlowInspector) — eagerly cache every loadScore.tex off the bridge fan-out, lazily build DOM + an own full-player ChordFlowScore on first show; Load-current + Render buttons; generalize app.js view toggle from 2-way to N-way (Practice/Content/Debug); third Debug nav segment + container in index.html. No Core/bridge change. | src/ChordFlow.Desktop/wwwroot/alphatex-inspector.js, src/ChordFlow.Desktop/wwwroot/index.html, src/ChordFlow.Desktop/wwwroot/app.js | — | — |
| ✅ | 2 | alphaTab version label: read alphaTab.meta.version at init and show it in the Debug toolbar (guarded for the global being absent) so triage never doubts which engine build is loaded. | src/ChordFlow.Desktop/wwwroot/alphatex-inspector.js, src/ChordFlow.Desktop/wwwroot/index.html | — | — |
| ✅ | 3 | Two-track render fix (inspector-surfaced): alphaTab renders only the first track by default, so two-track (comping+lead) exercises drew one staff. In the shared score-render-component scoreLoaded handler, call api.renderTracks(score.tracks) when tracks.length > 1; single-track stays on the default path. Fix lives in the shared component so Practice, inspector, and Content preview all get it. alphatab-js-api-reference updated. | src/ChordFlow.Desktop/wwwroot/score-render-component.js | — | — |
| ✅ | 4 | Bars-per-row layout fix + cleanup: defaultSystemsLayout/systemsLayout only bind on multi-track, so switch to display.barsPerRow on LayoutMode.Page (barsPerRow:4 default, -1 for the new Auto-layout toggle) — works single- and multi-track. Strip the now-inert { defaultSystemsLayout 4 } from AlphaTexRenderer and update the two-track renderer test. Refs synced (alphatex-syntax, domain-model, alphatab-js-api). | src/ChordFlow.Desktop/wwwroot/score-render-component.js, src/ChordFlow.Core/Rendering/AlphaTexRenderer.cs, tests/ChordFlow.Core.Tests/AlphaTexRendererTests.cs | — | — |
---

### Legend

| Symbol | Meaning |
|--------|---------|
| ✅ | Done |
| 🔄 | In Progress |
| 🔳 | Pending |
| ❌ | Cancelled |
