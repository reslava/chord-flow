---
type: idea
id: id_01KVZ4P949RB1RHJ0469SESEJS
title: Voicing ranking strategies (selectable comping-grip selection)
status: draft
created: 2026-06-25
version: 1
tags: []
parent_id: null
requires_load: []
---
# Voicing ranking strategies (selectable comping-grip selection)

## Goal

Make the within-`automatic` comping-grip selection a **pluggable, user-selectable ranking strategy** with several named modes, each serving a different practice goal. Builds on [[engine-derived-as-app-source]], which defines the ranking **seam** and ships **one default**; this thread adds the alternative modes and the selection knob.

## Origin

chat-001 of `guitar/engine-derived-as-app-source`. Rafa: the within-`automatic` ordering isn't one rule — it's several distinct strategies, and which one the player wants depends on the goal (learn shapes vs comp comfortably vs voice-lead).

## Why

When the `automatic` source comps a whole progression, *which* grip it picks per chord — across the progression — is a stateful choice (each pick can depend on prior picks). There's no single right rule; there are several, each musically/pedagogically meaningful. Folding only one into the engine undersells it; the engine vision wants this as a seam with selectable modes.

## The modes

1. **All CAGED shapes (variety)** — 1st chord: lowest-fret grip in the region; next: closest to the previous chord **among shapes not yet used**, relaxing the not-yet-used constraint once every shape has been used. Forces the player through every CAGED shape. A *learning* mode.
2. **Closest (consistent)** — 1st chord: lowest fret; next: if this chord already appeared, **reuse its earlier grip** (muscle memory / consistency); else the grip closest to the previous chord. Minimal movement. **The default** (shipped by [[engine-derived-as-app-source]]).
3. **Voice leading (guide tones) — future** — 1st chord: lowest fret; next: voice the **3rd/7th on the same string**, closest to the previous chord's 3rd/7th. Smooth guide-tone motion. Needs guide-tone (shell) derivation — depends on [[shell-voicing-derivation]].

## Shape (sketch — design firms this up)

- A **ranking-strategy seam** (interface) the `automatic` source consumes, **stateful across the progression** (a fold — each pick may depend on prior picks).
- The selected strategy is a **generate-time practice knob** (alongside the main-source knob), flowing through the `generate` envelope.
- Mode 2 is the default (from [[engine-derived-as-app-source]]); modes 1 and 3 are additive.

## Scope

**In:** modes 1 and 2 (3 deferred), the selection knob + Practice UI, the per-progression stateful selection, the seam if not already shipped by the sibling thread.
**Out:** the seam's existence + the default impl (in [[engine-derived-as-app-source]]); guide-tone derivation (mode 3 depends on [[shell-voicing-derivation]]).

## Open design questions (for design)

1. Does "used" track the **CAGED shape** or the **exact grip**?
2. Where the selection knob sits in the Practice UI; persisted (AppSettings) or transient per generate.
3. Mode 3's sequencing — after [[shell-voicing-derivation]] lands the guide-tone substrate?

## Validation

- Switching mode changes the comped grips across a progression in the defined way.
- **Dogfood:** render the same 12-bar blues under each mode on the fretboard UI page (now/next fret-boxes); visually confirm variety vs consistency.
