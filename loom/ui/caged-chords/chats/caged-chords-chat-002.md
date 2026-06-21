---
type: chat
id: ch_01KVNDZF2GMB97MB3D9S4TRKC5
title: caged-chord Fix
status: done
created: 2026-06-21
updated: 2026-06-21
version: 2
tags: []
parent_id: null
requires_load: []
last_ai_block: 13
---
# caged-chord Fix

## Rafa:

I ran the app to do the feature visual check.

I found that in some combinations when:
- chord land outside the fretboard active zone
- chord do not find in 0-12 some notes are draw as X and missing

examples that produces wrong diagrams:

| shape | quality | root | fretboard zone showed | missing strings |
|---|---|---|---|---|
| C |maj7 |A |1-4 | 2,3,4|    
| G |maj7 |E |1-4 | 3,4,5|    

these diagrams should be placed around fret 9-12 chords should be 

## AI:

Reproduced both cases analytically — your two examples match the mechanism exactly (C·maj7·A → strings 2,3,4 muted; G·maj7·E → strings 3,4,5 muted). Good catch; this is a real engine bug the dogfood page exposed.

## Root cause

The auto-region derives at `[minFret 0, maxFret 15]` (`CagedChordHandler`), so the engine anchors the shape at the **lowest** root occurrence. For C-shape A that's the **open A string (fret 0)**; for G-shape E it's the **open low-E (fret 0)**. Two things then break, both in `OctaveShape.AnchorsFor` / the engine's window logic — not in the page:

1. **Secondary octave anchors go negative.** `AnchorsFor` places each later root string one octave *up* from the primary: `fret = (primaryAbs + k·12) − openAbs(string)`, with no floor. With the primary at fret 0:
   - C-shape A: string-2 anchor = `(45+12) − 59 = −2`
   - G-shape E: string-3 anchor = `(40+12) − 55 = −3`
   
   So the octave skeleton physically falls off the nut, and `Zone` is computed from negative frets.

2. **The reach window collapses to a single fret.** Because the bass root sits at fret 0 while a secondary anchor is negative, `stacksUp` resolves to *false* (down-stacking), so `window = [max(0, bassFret − reachBehind), bassFret] = [0, 0]`. Candidate enumeration is then confined to **fret 0 only** — the open strings. The only interior strings that can sound are open strings that happen to be chord tones; everything else finds no candidate and is muted. That is precisely strings 2,3,4 (C·maj7·A) and 3,4,5 (G·maj7·E).

The correct placement is the **next octave up** (root A at A-string fret 12, root E at low-E fret 12), where the whole skeleton fits and the box forms normally — your "around 9–12". The C and G shapes are the exposed ones because their box extends *toward the nut* from the bass root; E/A/D stack upward and are fine open.

This is why the 36/36 oracle still passes: the authored voicings are real, playable grips, so none of them is a negative-fret placement. The bug only surfaces when a caller asks for an auto-region from fret 0 on a down-stacking shape.

## Fix options

**A — Engine: anchor at the lowest placement where the whole skeleton fits (recommended).** In `AnchorsFor`, advance the primary to the lowest occurrence such that *every* derived octave anchor is `≥ minFret` (≥ 0). C-shape A skips fret 0 → lands fret 12; E-shape E keeps fret 0 (all anchors already ≥ 0). This fixes the root defect (an octave skeleton with negative frets is invalid for *any* caller, not just this page) and is a small, local change. Touches the engine — crosses this thread's **EX4 ("no engine changes")**, so it's your call.

**B — Handler: smarter auto-region, engine untouched.** Walk candidate placements upward and pick the first whose derived grip is *complete* (no in-box string muted). Keeps EX4 intact, but leaves the latent negative-fret/window-collapse bug in the engine for the next caller, and needs a "grip complete?" predicate that duplicates engine knowledge.

**C — Floor `minFret` at 1 in the handler.** Rejected: wrong. It would push the open E/A/D shapes (open E major, open A, open D) up an octave needlessly — they're correct at fret 0.

My recommendation is **A**: it's the durable fix (corrects the engine's invariant — anchors never go below the nut), it matches your 9–12 expectation, and I'll re-run the 36/36 oracle plus `ChordShapeDiagramTests` to confirm no regression before/after. It does mean amending the req (EX4) for this thread, since the dogfood page did its job and found an engine defect.

Which approach do you want? If A, I'll also update `chordflow-domain-model-reference` (the `AnchorsFor` semantics) in the same change.

## Rafa:

A

## AI:

Done — approach A implemented.

**The fix** (`OctaveShape.AnchorsFor`): instead of taking the primary string's *first* root occurrence ≥ minFret, it now walks the occurrences low→high and takes the first whose **whole octave skeleton fits on the neck** — every derived anchor at fret ≥ 0. A too-low primary (open-string root on a down-stacking C/G shape) whose higher-octave anchor would fall below the nut is skipped to the next octave up. Octave-skeleton geometry extracted into a small `SkeletonAt` helper; behaviour for every in-bounds case is unchanged.

**Why it can't regress the oracle:** the floor is `≥ 0` (never below the nut), not `≥ minFret`, so any placement that already fit is untouched. All existing tests pin root C, where the first occurrence already fits.

**Verification:**
- New regression test `AnchorsFor_SkipsTooLowPrimary_WhenSkeletonFallsBelowTheNut` — asserts C·A → primary fret **12** and G·E → primary fret **12**, with no sub-nut anchor. ✅
- `CagedDerivationOracleTests` still **36/36**; `CagedAnchorFingerOracleTests` still 36/36.
- Full Core suite: **587/587 green**.

Your two cases now resolve to the next octave up (zone ≈ 9–12), the box forms, and the spurious muting on strings 2,3,4 (C·maj7·A) / 3,4,5 (G·maj7·E) is gone. Note this also fixes the **octave-shapes** dogfood page, which calls the same `AnchorsFor` and had the identical latent bug for non-C roots.

**Bookkeeping done:** req amended to v2 (`IN7` records the fix, `EX4` relaxed for this one defect) and re-locked; `chordflow-domain-model-reference` updated with the new `AnchorsFor` semantics.

Two notes for you:
1. This is a unit-verified fix. The dogfood rule wants a **visual re-check in the running app** as the final sign-off — want me to walk you through launching it, or will you re-run the visual check yourself?
2. The design doc (§4) still phrases the auto-region as "the lowest placement" — now technically "the lowest *playable* placement." I left the done design untouched; say the word if you want me to patch that line for accuracy.

Not committed yet — say `go` (or `commit`) when you want it on main.

## Rafa:

diagram component tweak 
fret min max stablished to call parameters, do not let to cut the zone

## AI:

Done — diagram tweak applied.

**The bug:** `ChordShapeDiagram.Build` was passing `FretMax: null` (and `FretMin` = lowest fretted marker). The JS view then auto-fits the window to the top marker, so whenever the octave zone extends above/below the markers, `effectiveWindow` clips the band (`zMax = Math.min(zoneFretMax, topFret + rows − 1)`).

**The fix:** the producer now sets an **explicit window over the union of the fretted markers and the zone** — `FretMin = min(lowestFretted, zoneMin)`, `FretMax = max(highestFretted, zoneMax)` (floored at 0). So the call now carries a real `fretMin`/`fretMax` and the band can never be cut. This matches the sibling `CagedShapeDiagram`, which already frames an explicit window around its zone.

**Tests:**
- New `Build_FretWindowContainsTheWholeZone_SoTheBandIsNeverClipped` — asserts `FretMin ≤ ZoneFretMin`, `FretMax ≥ ZoneFretMax`, and every fretted marker is inside the window. ✅
- Existing `Build_EShapeMajor…` still asserts `FretMin == 8` (unchanged — for that grip the zone and markers share the low fret). ✅
- Full Core suite: **590/590 green**.

One choice for you: I framed the window **tight** to markers∪zone (no extra context frets). The sibling adds a 2-fret margin so the band "reads within the neck rather than filling edge-to-edge." For a chord the grip itself gives context, so I left it tight — but say the word if you want the same ±2 margin for visual consistency across the dogfood pages.

Still uncommitted — this builds on the `AnchorsFor` fix above. Say `go`/`commit` to put both on main.

## Rafa:

I ran the app to check, fret min max do not load any value, also do not stablish any limit
Need to rebuild?

## AI:

**Yes — rebuild and relaunch.** Both fixes live in **C# Core** (`OctaveShape.AnchorsFor` and `ChordShapeDiagram.Build`), compiled into the Core/Desktop DLLs. The running app keeps using the old build until you rebuild — that's why nothing changed. I just built `ChordFlow.Desktop` here: **0 errors** (only the pre-existing WindowsBase version warnings). So:

```
dotnet build src/ChordFlow.Desktop/ChordFlow.Desktop.csproj
```

then relaunch the app. (No JS changed, so nothing to refresh on the web side — the window now arrives from Core.)

**About the empty "Frets: [min] [max]" toolbar fields — that's expected, not the bug.** Those two inputs are *manual overrides*: blank = "auto / honor the Core-computed window." They are bound to the user-override state (`userFretMin`/`userFretMax`), which starts null, so they always render empty — they never display the model's window. The fix works through the model's `fretMin`/`fretMax` that Core now sends; `effectiveWindow()` already prefers the model value when the override is blank, so after the rebuild the zone band will be framed correctly even with those boxes empty.

So: after rebuild, the C·maj7·A / G·maj7·E grips should sit at ≈9–12 with the full zone band visible and no stray X's. If they still look wrong, tell me exactly what you see and I'll dig further.

**Optional UX I can add:** make those two input boxes *pre-fill* with the current effective window (so you can see the limits and nudge them), instead of starting blank. Small change in `fretboard-render-component.js`. Want it? It's separate from the bug fix.

## Rafa:

I though you were going to implement also the Optional UX, plus making de component able to limiting fret min max to the zone 

## AI:

Fair — you'd lumped both into the tweak; I'd split them out as a question. Done now, both in `fretboard-render-component.js`:

**1. Component guarantees the window contains the zone (the durable guard).** `computeWindow()` now grows the drawn window to include `zoneFretMin/zoneFretMax` after the model/override math:

```js
if (model.zoneFretMin != null) windowMin = Math.min(windowMin, model.zoneFretMin);
if (model.zoneFretMax != null) windowMax = Math.max(windowMax, model.zoneFretMax);
```

So **neither the model window nor a user min/max override can clip the band** — this protects every producer (caged-chords, caged-shapes, scales…), not just this page. A too-tight entry visibly snaps back to the zone on render.

**2. Min/max boxes show the current window.** The inputs now load the *actual drawn window* (`shownWindow()` = `[topFret, topFret + fretCount − 1]`) instead of starting blank. Editing one sets an override + re-renders; clearing it reverts to auto. Because of #1, if you type a max below the zone it snaps back up to the zone on render.

This is a **shared-component** change, so the other fretboard pages now also pre-fill their fret boxes — consistent, and an improvement everywhere.

Built `ChordFlow.Desktop`: **Build succeeded, 0 errors**, and `wwwroot` is copied to the output (`CopyToOutputDirectory=PreserveNewest`), so the JS ships with the build. **Rebuild + relaunch** to see it (the JS is served from `bin/.../wwwroot`, not the source tree, so the rebuild is what copies it).

Note I did **not** add the ±2 context margin — the window is still framed tight to markers∪zone. Say so if you want the margin too.

Still uncommitted (now three pieces: `AnchorsFor`, `ChordShapeDiagram` window, and this component change). `go`/`commit` when you're happy after the visual check.

## Rafa:

Nice you expanded to other fretboard pages, thanks!
Implement the margin.


## AI:

Margin added — at the **component** level so every page gets it:

```js
const ZONE_MARGIN = 2; // frets of context each side of the zone band
…
if (model.zoneFretMin != null) windowMin = Math.min(windowMin, model.zoneFretMin - ZONE_MARGIN);
if (model.zoneFretMax != null) windowMax = Math.max(windowMax, model.zoneFretMax + ZONE_MARGIN);
```

So the drawn window keeps ~2 frets of neck on each side of the zone band (the band no longer fills edge-to-edge). The `showNut` rule still clamps the low side to the nut, so a low zone just shows from fret 1 — no phantom negative frets. The sibling `CagedShapeDiagram` already bakes its own ±2 into the model window; `Math.min`/`Math.max` make that idempotent, so no double margin there.

Built `ChordFlow.Desktop`: **succeeded, 0 errors**, wwwroot copied to output. Rebuild + relaunch to see it.

Full set of changes now uncommitted:
- `OctaveShape.AnchorsFor` — skip sub-nut placements (the 9–12 fix) + regression test
- `ChordShapeDiagram.Build` — explicit window over markers∪zone + test
- `fretboard-render-component.js` — window always contains the zone (+margin), and the min/max boxes pre-fill with the current window
- ref `chordflow-domain-model-reference` synced; req amended to v2 (IN7)

`go`/`commit` when the visual check looks right.

## Rafa:

Create small plan 1 step done with the fixes/tweaks implemented in order to appear in roadmap

commit