---
type: plan
id: pl_01KW0FP6EG5Y4P6C6SZY4R501W
title: Engine-derived voicings — dogfood integration fixes
status: done
created: 2026-06-25
updated: 2026-06-25
version: 1
design_version: 1
req_version: 2
tags: []
parent_id: de_01KVZVJC1A52Q0GSJGRHXQJZ8B
requires_load: []
target_version: 0.1.0
actual_release: 0.12.0
steps:
  - id: pack-import-reconcile
    order: 1
    status: done
    description: "Pack import **reconciles** (fix A): after upserting a pack's definitions, `PackImporter` deletes the `Origin.Pack` rows it owns (same `PackId`) whose id is no longer shipped, per content kind — so emptying the pack's voicings purges the stale rows on next run. User copies (fresh ids) untouched."
    files_touched: [src/ChordFlow.Core/Features/Packs/PackImporter.cs, tests/ChordFlow.Core.Tests/PackImportReconcileTests.cs]
    blocked_by: []
    satisfies: [IN12]
  - id: resolve-auto-id-dsl
    order: 2
    status: done
    description: "Resolve an `auto:` voicing id to a grip in Core (fix B core): a helper derives the family's lowest valid placement at canonical C → a canonical voicing DSL, so the existing preview/duplicate path can consume a computed row. Robust to the Derive edge (scan up for the lowest placement that derives)."
    files_touched: [src/ChordFlow.Core/Features/Voicings/AutomaticVoicingDoc.cs, tests/ChordFlow.Core.Tests/AutomaticVoicingDocTests.cs]
    blocked_by: []
    satisfies: [IN13]
  - id: read-only-computed-rows-duplicate-to
    order: 3
    status: done
    description: "Content view treats computed/package voicings as **read-only** (fix B UI): `ContentCrudHandler.Get` returns the derived read-only DSL for an `auto:` id (instead of a DB miss); `content-crud.js` shows automatic/package rows read-only with a **\"Duplicate to user\"** action that mints an editable `user` copy from the derived DSL."
    files_touched: [src/ChordFlow.Core/Features/ContentCrud/ContentCrudHandler.cs, src/ChordFlow.Desktop/wwwroot/content-crud.js]
    blocked_by: []
    satisfies: [IN13]
  - id: practice-min-max-region-control
    order: 4
    status: done
    description: "Practice **voicing-source control** (fix C): a small Practice-page control with `MinFret`/`MaxFret` inputs for the `automatic` region, sent on `renderOptions.voicing` ({kind:'automatic', minFret, maxFret}); a re-render is requested on change. Ranking stays Closest (no mode selector — EX2)."
    files_touched: [src/ChordFlow.Desktop/wwwroot/score-render-component.js, src/ChordFlow.Desktop/wwwroot/app.js, src/ChordFlow.Desktop/wwwroot/index.html]
    blocked_by: []
    satisfies: [IN14]
  - id: dogfood-re-run-manual
    order: 5
    status: done
    description: "Manual dogfood re-run after a clean DB: no duplicate voicings; an automatic row opens read-only with its grip + Duplicate-to-user; the min/max region control changes the comped grips on the now/next fret-boxes."
    files_touched: []
    blocked_by: []
    satisfies: [IN12, IN13, IN14]
---
# Engine-derived voicings — dogfood integration fixes

## Goal

The plan-001 code landed green, but running the app surfaced three integration gaps (IN12–IN14): the persistent DB kept stale package voicings (pack import never purges), clicking an automatic row errored (computed rows have no DB doc), and the voicing-source knob had no Practice UI. This plan fixes all three — pack-import reconciliation, read-only derived preview + "Duplicate to user" for computed rows, and a Practice min/max region control. The Derive extreme-placement throw is split to its own thread (caged-derive-anchor-edge, EX6).

---

## Steps

| Done | # | Step | Files touched | Blocked by | Satisfies |
|---|---|---|---|---|---|
| ✅ | 1 | Pack import **reconciles** (fix A): after upserting a pack's definitions, `PackImporter` deletes the `Origin.Pack` rows it owns (same `PackId`) whose id is no longer shipped, per content kind — so emptying the pack's voicings purges the stale rows on next run. User copies (fresh ids) untouched. | src/ChordFlow.Core/Features/Packs/PackImporter.cs, tests/ChordFlow.Core.Tests/PackImportReconcileTests.cs | — | IN12 |
| ✅ | 2 | Resolve an `auto:` voicing id to a grip in Core (fix B core): a helper derives the family's lowest valid placement at canonical C → a canonical voicing DSL, so the existing preview/duplicate path can consume a computed row. Robust to the Derive edge (scan up for the lowest placement that derives). | src/ChordFlow.Core/Features/Voicings/AutomaticVoicingDoc.cs, tests/ChordFlow.Core.Tests/AutomaticVoicingDocTests.cs | — | IN13 |
| ✅ | 3 | Content view treats computed/package voicings as **read-only** (fix B UI): `ContentCrudHandler.Get` returns the derived read-only DSL for an `auto:` id (instead of a DB miss); `content-crud.js` shows automatic/package rows read-only with a **"Duplicate to user"** action that mints an editable `user` copy from the derived DSL. | src/ChordFlow.Core/Features/ContentCrud/ContentCrudHandler.cs, src/ChordFlow.Desktop/wwwroot/content-crud.js | — | IN13 |
| ✅ | 4 | Practice **voicing-source control** (fix C): a small Practice-page control with `MinFret`/`MaxFret` inputs for the `automatic` region, sent on `renderOptions.voicing` ({kind:'automatic', minFret, maxFret}); a re-render is requested on change. Ranking stays Closest (no mode selector — EX2). | src/ChordFlow.Desktop/wwwroot/score-render-component.js, src/ChordFlow.Desktop/wwwroot/app.js, src/ChordFlow.Desktop/wwwroot/index.html | — | IN14 |
| ✅ | 5 | Manual dogfood re-run after a clean DB: no duplicate voicings; an automatic row opens read-only with its grip + Duplicate-to-user; the min/max region control changes the comped grips on the now/next fret-boxes. | — | — | IN12, IN13, IN14 |
---

### Legend

| Symbol | Meaning |
|--------|---------|
| ✅ | Done |
| 🔄 | In Progress |
| 🔳 | Pending |
| ❌ | Cancelled |

<!-- step:read-only-computed-rows-duplicate-to -->
### Step 3 — Read-only computed rows + Duplicate to user

Depends on step 2 (the auto: → DSL resolver). The derived DSL drives both the read-only preview and the duplicate.
