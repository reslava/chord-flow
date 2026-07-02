---
type: done
id: pl_01KVX1TEWVSZC2KSWTPEZP2CQ0-done
title: Done — dsl-monospace-font Plan
status: done
created: 2026-06-24
version: 3
tags: []
parent_id: pl_01KVX1TEWVSZC2KSWTPEZP2CQ0
requires_load: []
---
# Done — dsl-monospace-font Plan

## Step 1 — Add a shared `.dsl-input` rule with the full monospace stack to the index.html <style>, and remove the now-redundant font-family from `.cc-editor textarea`.

**File:** `src/ChordFlow.Desktop/wwwroot/index.html` — added the shared `.dsl-input` rule and removed the ad-hoc `font-family` from `.cc-editor textarea` (consolidation, C2).

This step took two iterations against the live app — both worth recording:

**Bug 1 — specificity loss.** The first cut was a plain `.dsl-input { font-family: … }`. It rendered proportional. Cause: `.cc-editor textarea` carries `font: inherit` (a shorthand that *also* sets `font-family: inherit`) at specificity **(0,1,1)**; a plain class is **(0,1,0)** and loses. Removing the old line-149 `font-family` actually made it worse — that was the only (0,1,1) declaration beating `font: inherit`. Fix: element-qualify so the shared class ties at (0,1,1) and wins on source order:

```css
textarea.dsl-input,
input.dsl-input { … }
```

(recorded as `C4`).

**Bug 2 — ligature "dancing".** Once monospace, characters shifted while typing (`=>`, `..`, `==` merging). Cause: the original stack (`ui-monospace`, `"Cascadia Code"`) resolves to a font with OpenType ligatures / contextual alternates (`liga`/`calt`) on — fine for code, wrong for a cell-aligned DSL grid. Fix: lead with **Consolas** (ships on every Windows, ligature-free, more readable than Courier New), swap Cascadia **Code** → Cascadia **Mono**, and disable ligatures outright so it holds regardless of which font wins:

```css
font-family: Consolas, "Cascadia Mono", ui-monospace, "Courier New", monospace;
font-variant-ligatures: none;
font-feature-settings: "liga" 0, "calt" 0;
```

The req was amended to match (`IN4` ~dropped → `IN5` Consolas-first stack + `IN6` ligatures-off; `C4` specificity), re-locked at v2. Satisfies: IN1, IN5, IN6, C2, C3, C4.

## Step 2 — Apply `class="dsl-input"` to the content CRUD DSL textarea (#ccDsl, content-crud.js template) and the Scales interval input (#scaleIntervals, index.html).

Applied `class="dsl-input"` to both DSL editors:
- `#ccDsl` `<textarea>` in `src/ChordFlow.Desktop/wwwroot/content-crud.js` (the one editor serving progression / song / rhythm / voicing).
- `#scaleIntervals` `<input>` in `src/ChordFlow.Desktop/wwwroot/index.html` (now monospace for the first time).

No JS logic touched — markup gained only the class attribute (C1). Satisfies: IN2, IN3, C1.

## Step 3 — Dogfood: run the app; confirm rhythm cells align column-by-column in the content editor and the Scales interval input renders monospace.

Dogfooded in the running app (Rafa built + ran). Outcomes:
- Rhythm DSL editor (`#ccDsl`, Rhythms tab) now renders monospace — confirmed the earlier proportional look was the specificity loss (Bug 1), not a stale-asset cache.
- After the ligature fix (Bug 2), `X....` and similar holds rock-steady column-by-column — no glyph dancing.
- `#scaleIntervals` (Scales view) renders monospace.

Satisfies: IN2, IN3.
