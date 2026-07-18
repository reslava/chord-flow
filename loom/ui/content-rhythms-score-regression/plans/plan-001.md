---
type: plan
id: pl_01KXVNCP9NN03WDBR2XQTTQA9C
title: Fix content rhythms preview — score hidden by leftover Sheet-view collapse
status: done
created: 2026-07-18
updated: 2026-07-18
version: 1
design_version: 1
tags: []
parent_id: null
requires_load: []
target_version: 0.1.0
steps:
  - id: root-caused-the-content-rhythms-preview
    order: 1
    status: done
    description: "Root-caused the content rhythms-preview regression from the content-shared-render-surfaces work: the shared ChordFlowRenderSurface's score-only mode never established scoreEl visibility, so a leftover `.view-collapsed` class (left on the page-owned #ccPreviewScore by a prior Sheet view) collapsed the rhythm score to max-height:0 — ScoreR appeared hidden."
    files_touched: []
    blocked_by: []
    satisfies: []
  - id: fixed-render-surface-component-js-in
    order: 2
    status: done
    description: "Fixed render-surface-component.js: in score-only mode (no Score⇄Sheet toggle), remove `.view-collapsed` from scoreEl on create so the score always renders visible; harmless no-op when the class is absent, and the sheet→progression path already self-heals via the toggle's show('score')."
    files_touched: []
    blocked_by: []
    satisfies: []
---
# Fix content rhythms preview — score hidden by leftover Sheet-view collapse

## Goal

Quick-ship record of 2 completed changes.

---

## Steps

| Done | # | Step | Files touched | Blocked by | Satisfies |
|---|---|---|---|---|---|
| ✅ | 1 | Root-caused the content rhythms-preview regression from the content-shared-render-surfaces work: the shared ChordFlowRenderSurface's score-only mode never established scoreEl visibility, so a leftover `.view-collapsed` class (left on the page-owned #ccPreviewScore by a prior Sheet view) collapsed the rhythm score to max-height:0 — ScoreR appeared hidden. | — | — | — |
| ✅ | 2 | Fixed render-surface-component.js: in score-only mode (no Score⇄Sheet toggle), remove `.view-collapsed` from scoreEl on create so the score always renders visible; harmless no-op when the class is absent, and the sheet→progression path already self-heals via the toggle's show('score'). | — | — | — |
---

### Legend

| Symbol | Meaning |
|--------|---------|
| ✅ | Done |
| 🔄 | In Progress |
| 🔳 | Pending |
| ❌ | Cancelled |
