---
type: plan
id: pl_01KVX1TEWVSZC2KSWTPEZP2CQ0
title: dsl-monospace-font Plan
status: done
created: 2026-06-24
updated: 2026-06-24
version: 1
design_version: 1
req_version: 1
tags: []
parent_id: null
requires_load: []
target_version: 0.1.0
steps:
  - id: define-the-shared-dsl-input-class
    order: 1
    status: done
    description: Add a shared `.dsl-input` rule with the full monospace stack to the index.html <style>, and remove the now-redundant font-family from `.cc-editor textarea`.
    files_touched: [src/ChordFlow.Desktop/wwwroot/index.html]
    blocked_by: []
    satisfies: [IN1, IN5, IN6, C2, C3, C4]
  - id: apply-the-class-to-every-dsl
    order: 2
    status: done
    description: "Apply `class=\"dsl-input\"` to the content CRUD DSL textarea (#ccDsl, content-crud.js template) and the Scales interval input (#scaleIntervals, index.html)."
    files_touched: [src/ChordFlow.Desktop/wwwroot/content-crud.js, src/ChordFlow.Desktop/wwwroot/index.html]
    blocked_by: []
    satisfies: [IN2, IN3, C1]
  - id: dogfood-on-the-content-scales-ui
    order: 3
    status: done
    description: "Dogfood: run the app; confirm rhythm cells align column-by-column in the content editor and the Scales interval input renders monospace."
    files_touched: []
    blocked_by: []
    satisfies: [IN2, IN3]
---
# dsl-monospace-font Plan

## Goal

Render every DSL text input in a monospace font so cells/columns line up — Rhythm DSL especially, and consistently across the progression/song/rhythm/voicing CRUD editor and the Scales interval input. The codebase already applies monospace ad-hoc in one spot (`.cc-editor textarea`) with an incomplete stack, while `#scaleIntervals` is still proportional; this plan consolidates both into one shared `.dsl-input` class with the full font stack and applies it to every DSL editor. CSS + minimal markup only — no JS behavior change.

---

## Steps

| Done | # | Step | Files touched | Blocked by | Satisfies |
|---|---|---|---|---|---|
| ✅ | 1 | Add a shared `.dsl-input` rule with the full monospace stack to the index.html <style>, and remove the now-redundant font-family from `.cc-editor textarea`. | src/ChordFlow.Desktop/wwwroot/index.html | — | IN1, IN5, IN6, C2, C3, C4 |
| ✅ | 2 | Apply `class="dsl-input"` to the content CRUD DSL textarea (#ccDsl, content-crud.js template) and the Scales interval input (#scaleIntervals, index.html). | src/ChordFlow.Desktop/wwwroot/content-crud.js, src/ChordFlow.Desktop/wwwroot/index.html | — | IN2, IN3, C1 |
| ✅ | 3 | Dogfood: run the app; confirm rhythm cells align column-by-column in the content editor and the Scales interval input renders monospace. | — | — | IN2, IN3 |
---

### Legend

| Symbol | Meaning |
|--------|---------|
| ✅ | Done |
| 🔄 | In Progress |
| 🔳 | Pending |
| ❌ | Cancelled |

<!-- step:define-the-shared-dsl-input-class -->
### Step 1 — Define the shared .dsl-input class

Add `.dsl-input { font-family: ui-monospace, "Cascadia Code", Consolas, "Courier New", monospace; }` to the <style> block. Drop the `font-family: ui-monospace, "Cascadia Code", monospace` from the `.cc-editor textarea` rule (index.html:149) so monospace lives in exactly one place; keep its other declarations (min-height, resize).

<!-- step:apply-the-class-to-every-dsl -->
### Step 2 — Apply the class to every DSL editor

In content-crud.js, add `class="dsl-input"` to the `#ccDsl` <textarea> in the editor template string. In index.html, add `class="dsl-input"` to the `#scaleIntervals` <input>. No logic changes.

<!-- step:dogfood-on-the-content-scales-ui -->
### Step 3 — Dogfood on the content + scales UI pages

Per the guitar-weave dogfood rule and the idea's Validation: open the Content view, select Rhythms, type a multi-cell rhythm and confirm columns line up; open the Scales view and confirm `#scaleIntervals` is monospace.
