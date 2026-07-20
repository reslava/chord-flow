---
type: plan
id: pl_01KY04HM48W11PCTPWY0BB6ARW
title: Filter UX — hierarchical facets, all/none, counts
status: done
created: 2026-07-20
updated: 2026-07-20
version: 1
design_version: 1
req_version: 1
tags: []
parent_id: de_01KY045E0MSF9W45T6TZP5JAKV
requires_load: []
target_version: 0.1.0
steps:
  - id: filterr-counts-greyed-disabled-all-none
    order: 1
    status: done
    description: "Rewrite FilterR's chip model: chips become {token,label,count?,disabled?,selected?} (selected defaults on) — render the count as a suffix, a disabled chip greyed + non-clickable + never selected; add a per-level All/None control (showAllNone, default on; All selects only available chips); onChange(enabledByKey, changedKey); drop the internal default-on/sticky-off state; clone chips so caller objects aren't mutated."
    files_touched: [src/ChordFlow.Desktop/wwwroot/filter-render-component.js]
    blocked_by: []
    satisfies: [IN1, IN2, C1, C4]
  - id: filter-cascade-js-pure-util-node
    order: 2
    status: done
    description: "Add the shared pure filter-cascade.js util (browser + node compatible): given items + ordered level defs (key/label/accessor) + a per-level selected set, compute each level's chips {count, disabled(!available), selected} from the items passing the higher levels (full stable vocabulary), the filtered item set, initialSelected (all values), and resetBelow (reset lower levels to all-available on a higher change). Cover it with a node-run unit test (counts, availability, reset-on-higher-change)."
    files_touched: [src/ChordFlow.Desktop/wwwroot/filter-cascade.js, src/ChordFlow.Desktop/wwwroot/index.html, tests/js/filter-cascade.test.js]
    blocked_by: []
    satisfies: [IN3, C5]
  - id: content-pages-cascade-counts
    order: 3
    status: done
    description: "Content pages adopt the cascade: build ordered levels (Source → Genre → Subgenre → Tags) via the util, show per-chip counts + a grand-total “N of M”, reset lower levels on a higher change; the Subgenre (and any single-value optional facet) is now visible. Replace the flat build/applyFilter."
    files_touched: [src/ChordFlow.Desktop/wwwroot/content-crud.js, src/ChordFlow.Desktop/wwwroot/index.html]
    blocked_by: [filterr-counts-greyed-disabled-all-none, filter-cascade-js-pure-util-node]
    satisfies: [IN4, IN7, C2, C3]
  - id: practice-strip-cascade-total
    order: 4
    status: done
    description: Practice strip adopts the cascade over the metadata-bearing pickers (Harmony songs+progressions + Drums) via the util, and shows the narrowed-option grand total; Comping/Lead untouched, source always all.
    files_touched: [src/ChordFlow.Desktop/wwwroot/app.js, src/ChordFlow.Desktop/wwwroot/index.html]
    blocked_by: [filterr-counts-greyed-disabled-all-none, filter-cascade-js-pure-util-node]
    satisfies: [IN5, C2, C3]
  - id: voicings-grid-all-none
    order: 5
    status: done
    description: Voicings grid gains the per-level All/None only (its chips default selected; no cascade, no counts — EX1). Pass through FilterR's new All/None; behavior otherwise unchanged (server-side voicingGrid round-trip).
    files_touched: [src/ChordFlow.Desktop/wwwroot/guitar-voicings-render-component.js]
    blocked_by: [filterr-counts-greyed-disabled-all-none]
    satisfies: [IN6]
  - id: architecture-ref-sync
    order: 6
    status: done
    description: "Sync chordflow-architecture-reference.md: add filter-cascade.js (the shared pure cascade util) beside FilterR in the wwwroot listing."
    files_touched: [loom/refs/chordflow-architecture-reference.md]
    blocked_by: [filter-cascade-js-pure-util-node]
    satisfies: []
---
# Filter UX — hierarchical facets, all/none, counts

## Goal

Add the hierarchical facet cascade, quick All/None, and result counts to the shared filter, keeping FilterR a dumb view and putting the cascade recompute in a shared pure util. Builds against the locked req IN1–IN7 / C1–C5; the Voicings-grid cascade/counts (EX1), rhythm facets (EX2), and metadata editing (EX3) stay out.

---

## Steps

| Done | # | Step | Files touched | Blocked by | Satisfies |
|---|---|---|---|---|---|
| ✅ | 1 | Rewrite FilterR's chip model: chips become {token,label,count?,disabled?,selected?} (selected defaults on) — render the count as a suffix, a disabled chip greyed + non-clickable + never selected; add a per-level All/None control (showAllNone, default on; All selects only available chips); onChange(enabledByKey, changedKey); drop the internal default-on/sticky-off state; clone chips so caller objects aren't mutated. | src/ChordFlow.Desktop/wwwroot/filter-render-component.js | — | IN1, IN2, C1, C4 |
| ✅ | 2 | Add the shared pure filter-cascade.js util (browser + node compatible): given items + ordered level defs (key/label/accessor) + a per-level selected set, compute each level's chips {count, disabled(!available), selected} from the items passing the higher levels (full stable vocabulary), the filtered item set, initialSelected (all values), and resetBelow (reset lower levels to all-available on a higher change). Cover it with a node-run unit test (counts, availability, reset-on-higher-change). | src/ChordFlow.Desktop/wwwroot/filter-cascade.js, src/ChordFlow.Desktop/wwwroot/index.html, tests/js/filter-cascade.test.js | — | IN3, C5 |
| ✅ | 3 | Content pages adopt the cascade: build ordered levels (Source → Genre → Subgenre → Tags) via the util, show per-chip counts + a grand-total “N of M”, reset lower levels on a higher change; the Subgenre (and any single-value optional facet) is now visible. Replace the flat build/applyFilter. | src/ChordFlow.Desktop/wwwroot/content-crud.js, src/ChordFlow.Desktop/wwwroot/index.html | filterr-counts-greyed-disabled-all-none, filter-cascade-js-pure-util-node | IN4, IN7, C2, C3 |
| ✅ | 4 | Practice strip adopts the cascade over the metadata-bearing pickers (Harmony songs+progressions + Drums) via the util, and shows the narrowed-option grand total; Comping/Lead untouched, source always all. | src/ChordFlow.Desktop/wwwroot/app.js, src/ChordFlow.Desktop/wwwroot/index.html | filterr-counts-greyed-disabled-all-none, filter-cascade-js-pure-util-node | IN5, C2, C3 |
| ✅ | 5 | Voicings grid gains the per-level All/None only (its chips default selected; no cascade, no counts — EX1). Pass through FilterR's new All/None; behavior otherwise unchanged (server-side voicingGrid round-trip). | src/ChordFlow.Desktop/wwwroot/guitar-voicings-render-component.js | filterr-counts-greyed-disabled-all-none | IN6 |
| ✅ | 6 | Sync chordflow-architecture-reference.md: add filter-cascade.js (the shared pure cascade util) beside FilterR in the wwwroot listing. | loom/refs/chordflow-architecture-reference.md | filter-cascade-js-pure-util-node | — |
---

### Legend

| Symbol | Meaning |
|--------|---------|
| ✅ | Done |
| 🔄 | In Progress |
| 🔳 | Pending |
| ❌ | Cancelled |
