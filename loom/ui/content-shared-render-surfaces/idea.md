---
type: idea
id: id_01KXT94RKDERB14BNMAE6C3FY0
title: Content page mounts the shared render surfaces (ScoreR + ChordSheetR + toggle) like Practice
status: done
created: 2026-07-18
version: 1
tags: []
parent_id: null
requires_load: [rf_01KTSAPAT132QTEY5BEPRKS3MB]
---
# Content page mounts the shared render surfaces (ScoreR + ChordSheetR + toggle) like Practice

## What

Make the **Content page's preview** present content through the **same shared render composition as the Practice page** — ScoreR + ChordSheetR behind the **Score⇄Sheet toggle**, with the page-level transport (PlayerControlsR) — instead of the bare, bespoke ScoreR-only preview it mounts today. One way to look at a piece, whichever page you're on.

## Why — the two pages diverged, and the divergence hides bugs

Both pages render the *same* content, but they compose the render surface differently:

- **Practice** (`app.js`) mounts the full shared stack: **ScoreR** (`ChordFlowScore`, `transport:false`) for the tab/notation view, **ChordSheetR** (`ChordFlowSheetView`, `chord-sheets.js`) for the Sheet view, a page-level **Score⇄Sheet segmented toggle** that survives mid-playback, page-level **PlayerControlsR** bound to the one engine, and **HarmonyControlsR** for the definition strip.
- **Content** (`content-crud.js`) mounts only a **bare ScoreR** for the score/rhythm preview (plus the fretboard for voicings) — **no Sheet view, no Score⇄Sheet toggle, no shared page transport**, and its own one-off control strip (comping picker + the new tonality control).

So you can't audition a progression/song as a **chord sheet** while authoring it, and the two surfaces drift. That drift is not theoretical: the recent **minor-preview bug** (`plan-003` in [[minor-mode-ui-threading]]) partly hid because the Content preview is its own path — the same content renders through two different setups, so a fix or a check on one doesn't cover the other. Converging them makes the render surface **one improvement path**: fix ScoreR/ChordSheetR once, both pages benefit; dogfood once, both are covered.

## The core design decisions (settle in design)

The render *surfaces* clearly should be shared. The tension is **which controls are shared vs page-specific**, and **what the reusable unit is**:

1. **What's genuinely shared vs page-specific?** The render surfaces (ScoreR + ChordSheetR + toggle + PlayerControlsR) are common. But Practice's **HarmonyControlsR** is a *performance/definition* strip (key/feel/difficulty/lead/voicing-window) — much of it is exercise-level and doesn't apply to authoring a single progression. The Content page's controls are *authoring* controls (name, DSL, tonality, comping). So the shared unit is probably the **render surface composite**, not the full control strip.
2. **Extract a composite, or reuse the pieces?** Do we extract a single **render-surface component** (ScoreR + ChordSheetR + Score⇄Sheet toggle + PlayerControlsR, wired to one engine) that both pages mount, or does Content just mount the same pieces itself? The composite is more durable (one place owns the mid-playback toggle + single-engine wiring) but is a bigger refactor.
3. **Per-entity applicability.** The Sheet view only makes sense for progression/song; **rhythm** previews as a score, **voicing** as a fretboard. So the toggle is progression/song-only — the shared surface must degrade cleanly per entity (as the current strategy split already does).

These are the design's job; the idea just fences them.

## Scope (roughly)

1. Content preview mounts **ChordSheetR** alongside ScoreR, behind the shared **Score⇄Sheet toggle** (progression/song only).
2. A shared page transport (PlayerControlsR) for the preview, one engine across the toggle — matching Practice's mid-playback behavior.
3. Factor the common wiring so ScoreR/ChordSheetR improvements land once (the composite decision above).

## Non-goals

- Not merging the two pages — Content stays an editor, Practice stays practice. Only the **render surface** converges.
- Not moving authoring controls (tonality/comping/DSL) into Practice, nor performance controls (difficulty/lead) into Content.
- No new render capability — this is consolidation, not a feature.

## Consumers / related

- Direct follow-on of [[minor-mode-ui-threading]] (whose preview-mode work exposed the divergence).
- Builds on [[score-render-component]], the ChordSheet render surface, [[player-controls-component]], and [[content-crud]].
- **Load the architecture ref before designing** (per the ref-sync rule) — this touches page/component boundaries and the one-engine wiring.

## Validation / dogfood

- Author/select a progression or song in Content → toggle **Score⇄Sheet** in the preview, mid-playback, exactly like Practice; both markers track.
- A minor progression renders correct chords + `\ks` in **both** the score and the sheet preview (the regression the divergence hid).
- Rhythm still previews as a score, voicing as a fretboard — the toggle is absent where it doesn't apply.
