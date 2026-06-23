---
type: idea
id: id_01KVVCJ200HK15X7GABK2P2NKT
title: alphaTex tie / dotted-note rendering
status: draft
created: 2026-06-23
version: 1
tags: []
parent_id: null
requires_load: []
---
# alphaTex tie / dotted-note rendering

## Goal

Render **syncopated and dotted** rhythms so off-beat comping plays instead of erroring — the Charleston, anticipations, the "and-of-4" push: the heart of jazz comping.

## Origin

`songbook/jazz-blues` dogfood — **Finding 3** (priority 1 of the four follow-ons). The Charleston comp (`:2 X..X....`) threw in the app: *alphaTex tie rendering not supported in v1 (tie token unverified)*.

## Root cause

The v1 `RhythmQuantizer` / `AlphaTexRenderer` refuse tie/dotted alphaTex tokens (flagged *unverified* in the domain + alphaTex refs). Beat-aligned rings coalesce into whole/half notes, but a genuinely **syncopated or dotted** ring needs a tie or a dotted value — so the renderer **throws** rather than emit an unverified token. The Charleston's hit on the "and of 2" is exactly such an off-beat attack.

## Shape

- **Verify** the alphaTex tie (`-`/tie) and dotted (`{d}` / dotted-value) tokens against alphaTab (update `alphatex-syntax-reference.md`).
- Emit them from the quantizer/renderer for syncopated/dotted rings, replacing the throw.
- The **Charleston is the golden test** — `:2 X..X....` must render + play swung.

## Scope

**In:** tie + dotted note emission in the Rendering seam; verified token reference; the Charleston as the proof.
**Out:** new Rhythm DSL features beyond ties/dots; 32nds.

## Validation

- The Charleston comp plays end-to-end in Practice (swing on). 
- **Re-add `charleston.dsl`** to the default pack (it was pulled in the jazz-blues thread precisely because it couldn't render).
- A unit test: a syncopated pattern quantizes to verified tie/dotted alphaTex instead of throwing.