---
type: plan
id: pl_01KY2MDR3TB8ZNVKQSHPRWRCJ1
title: Phase 2 tweak — figure neutral defaults + Reset button
status: done
created: 2026-07-21
updated: 2026-07-21
version: 1
design_version: 3
req_version: 5
tags: []
parent_id: de_01KY0RDXS9C7X93BX8Y1HVCMC3
requires_load: []
target_version: 0.1.0
steps:
  - id: neutral-defaults-reset-auto-reset-on
    order: 1
    status: done
    description: "rhythm-generator.js: add a `resetModifiers()` that sets the neutral defaults (selection=cycle, index=0, rotIdx=0, displace=0, sweep/restBar/callResponse off, content=1, rest=0, barCount=4); a **Reset** button that calls it + generates; and a strategy-select change listener that calls it when switching to Figure (so a figure lands natural). Surprise-me sets its randoms directly and is unaffected. Default the initial barCount to 4."
    files_touched: [src/ChordFlow.Desktop/wwwroot/rhythm-generator.js]
    blocked_by: []
    satisfies: [IN7]
  - id: cdp-verification
    order: 2
    status: done
    description: "CDP verify (app relaunched with the debug port): switching Strategy → Figure resets the modifiers to defaults; the Reset button restores defaults; a clave figure plays both bars (Cycle default); Surprise-me still varies the modifiers. Report results."
    files_touched: []
    blocked_by: [neutral-defaults-reset-auto-reset-on]
    satisfies: [IN7]
---
# Phase 2 tweak — figure neutral defaults + Reset button

## Goal

Page-only UX: a figure should play its natural self. Define neutral modifier defaults (selection = Cycle(0) — neutral for both 1-bar figures and 2-bar claves, unlike Fixed; index 0, displace 0, sweep/restBar/callResponse off, content 1, rest 0, bars 4); auto-apply them when the user switches Strategy → Figure; add a Reset button that restores them anytime; and keep Surprise-me wild (it sets its randoms without triggering the reset).

---

## Steps

| Done | # | Step | Files touched | Blocked by | Satisfies |
|---|---|---|---|---|---|
| ✅ | 1 | rhythm-generator.js: add a `resetModifiers()` that sets the neutral defaults (selection=cycle, index=0, rotIdx=0, displace=0, sweep/restBar/callResponse off, content=1, rest=0, barCount=4); a **Reset** button that calls it + generates; and a strategy-select change listener that calls it when switching to Figure (so a figure lands natural). Surprise-me sets its randoms directly and is unaffected. Default the initial barCount to 4. | src/ChordFlow.Desktop/wwwroot/rhythm-generator.js | — | IN7 |
| ✅ | 2 | CDP verify (app relaunched with the debug port): switching Strategy → Figure resets the modifiers to defaults; the Reset button restores defaults; a clave figure plays both bars (Cycle default); Surprise-me still varies the modifiers. Report results. | — | neutral-defaults-reset-auto-reset-on | IN7 |
---

### Legend

| Symbol | Meaning |
|--------|---------|
| ✅ | Done |
| 🔄 | In Progress |
| 🔳 | Pending |
| ❌ | Cancelled |
