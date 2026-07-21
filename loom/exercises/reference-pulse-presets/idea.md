---
type: idea
id: id_01KY2NJKPFNJ3B15ZHBR2D8WPF
title: Quarters reference pulse + named trainer presets
status: draft
created: 2026-07-21
version: 1
tags: []
parent_id: null
requires_load: []
---
# Quarters reference pulse + named trainer presets

**The Phase-3 remainder** (most of Phase 3 already shipped: the **Beat-1** reference in plan-003, and the **named figures double as presets** since plan-004). Two small items left; deferred, low priority.

## 1. `Quarters` reference pulse

The design's reference pulse is `Off | Beat1 | Quarters` (req IN8). **Beat1 shipped** (a non-generated reference lane hitting beat 1 of each bar, distinct voice). **`Quarters`** is the same mechanism but hitting **all four beats** — a full quarter-note click under the figure, for locking to the pulse. Trivial extension of `WithBeat1Reference` (hit every beat, not just tick 0) + a `referencePulse` option on the page.

## 2. Named trainer presets

The design named a few **drill presets** (Find the Beat / The Backbeat / On the & / Leave Space) — one-click bundles of *kind + selection + behaviour + referencePulse*. Post-refactor these re-express as e.g.:
- **Find the Beat** — `Placement(quarter, all, 1)` + Cycle (walks the single onset 1→2→3→4).
- **The Backbeat** — figure `backbeat` + Quarters reference pulse.
- **On the &** — `Placement(eighth, offbeat, …)`.
- **Leave Space** — any kind + `RestBar`.

Largely covered by the figure catalog already; the remaining value is a small curated **preset menu** (a dropdown/buttons on the page) that sets the controls to a named drill. Optional — reassess whether it's worth it after Practice integration.
