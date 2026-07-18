---
type: done
id: pl_01KXVNCP9NN03WDBR2XQTTQA9C-done
title: Done — Fix content rhythms preview — score hidden by leftover Sheet-view collapse
status: done
created: 2026-07-18
version: 1
tags: []
parent_id: pl_01KXVNCP9NN03WDBR2XQTTQA9C
requires_load: []
---
# Done — Fix content rhythms preview — score hidden by leftover Sheet-view collapse

**Repro:** on Content, open a progression/song (sheet:true), click **Sheet** (collapses `#ccPreviewScore`), then switch the entity tab to **Rhythm** (score-only) — `ensureSurface()` disposes + recreates the composite with `sheet:false`, which builds no toggle, so nothing cleared the leftover `.view-collapsed` on the page-owned score element and the rhythm score rendered into a `max-height:0` box.

**Fix (`src/ChordFlow.Desktop/wwwroot/render-surface-component.js`):** after the `toggle` is (not) built, `if (!toggle) scoreEl.classList.remove("view-collapsed");`.

**Verification:** `node --check` clean; causation traced end-to-end against the composite's create/dispose + toggle state machine. No loom/refs change — the composite's documented score-only contract is unchanged (this makes it honor it). Rafa will confirm visually and re-capture `images/screenshots/05-content-rhythms.png` (overwrite in place) once the fix is running.
