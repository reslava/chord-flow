---
type: done
id: pl_01KY2MDR3TB8ZNVKQSHPRWRCJ1-done
title: Done — Phase 2 tweak — figure neutral defaults + Reset button
status: done
created: 2026-07-21
version: 2
tags: []
parent_id: pl_01KY2MDR3TB8ZNVKQSHPRWRCJ1
requires_load: []
---
# Done — Phase 2 tweak — figure neutral defaults + Reset button

## Step 1 — rhythm-generator.js: add a `resetModifiers()` that sets the neutral defaults (selection=cycle, index=0, rotIdx=0, displace=0, sweep/restBar/callResponse off, content=1, rest=0, barCount=4); a **Reset** button that calls it + generates; and a strategy-select change listener that calls it when switching to Figure (so a figure lands natural). Surprise-me sets its randoms directly and is unaffected. Default the initial barCount to 4.

**`rhythm-generator.js`**: added `resetModifiers()` (selection=cycle, index/rotIdx/displace=0, sweep/restBar/callResponse off, content=1, rest=0, barCount=4 — Cycle(0) is neutral for 1-bar figures *and* 2-bar claves); a **Reset** button (resetModifiers + generate); a strategy-select change listener that calls `resetModifiers()` when switching to **Figure** (so a figure lands natural). Surprise-me sets its randoms programmatically (no change event), so it's unaffected. Initial `barCount` default raised to 4. JS `node --check` clean; hot-deployed to the running app's wwwroot.

## Step 2 — CDP verify (app relaunched with the debug port): switching Strategy → Figure resets the modifiers to defaults; the Reset button restores defaults; a clave figure plays both bars (Cycle default); Surprise-me still varies the modifiers. Report results.

CDP verification (`verify-plan006.mjs`), all green: tweaking modifiers under Pattern (fixedPlusRotating/displace 5/bars 8) then switching **Strategy → Figure** resets them to cycle/0/4; **son-clave-32** renders `x. .x .. x. | .. x. x. .. | …` (bar0 ≠ bar1 — the clave plays both bars via the Cycle default); the **Reset** button restores defaults after a change; **Surprise me** still varies (randomInKind/2/7). Rafa also confirmed it looks/works OK by hand.
