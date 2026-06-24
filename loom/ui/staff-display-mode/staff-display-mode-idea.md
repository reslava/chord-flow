---
type: idea
id: id_01KVW7Z0VRK9K8T06CED28T8S6
title: Tab-only staff display mode
status: draft
created: 2026-06-24
version: 1
tags: []
parent_id: null
requires_load: []
---
# Tab-only staff display mode

## Goal

Offer a **tab-only** score view (guitar tablature **with rhythm stems**, via alphaTab `\staff {tabs}`) as the **default**, with a **toggle** to the current combined **standard-notation + tab** view. Tab-only is clearer and more compact for non-musician guitarists.

## Origin

`domain/tie-dotted-rendering` chat-001: Rafa verified `\staff {tabs}` works in the app and wants it as the clearer default.

## Shape

- The renderer (or the alphaTex header it emits) selects the staff profile: `\staff {tabs}` (tab-only) vs the combined default.
- A UI toggle, with the choice **persisted** as a user preference.
- Touches the `Rendering/` seam (header emission), the C#↔JS bridge / `score-render-component`, and persistence.

## Scope

**In:** tab-only default + toggle to combined; persisted preference.
**Out:** per-exercise overrides; notation-only (no tab) mode; print/export styling.

## Validation

- App opens in tab-only by default; toggle switches to score+tab and back; preference survives restart.
- Dogfood: confirmed on the score-render-component page.