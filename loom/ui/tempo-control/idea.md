---
type: idea
id: id_01KVWRPRSWYC6ND48JBNBSFK7A
title: Tempo control self-applies (no Generate click)
status: draft
created: 2026-06-24
version: 1
tags: []
parent_id: null
requires_load: []
---
# Tempo control self-applies (no Generate click)

## Goal

Changing the tempo in `ChordFlowScore` (ScoreR) should take effect **immediately**, not only after clicking the page's `Generate`. The control rightly lives in ScoreR (like the triplet-feel control); it just doesn't self-apply yet.

## Origin

`domain/rhythm-notation` chat-001 — surfaced while dogfooding the Charleston comp. Tempo is in ScoreR but editing it does nothing until a page-level `Generate`, which feels awkward.

## The alphaTab constraint (the fork)

Two different things, only one of which is live:

- **`api.playbackSpeed`** — a **percentage multiplier** (1.0 = 100%, 0.5 = 50%). Changes playback speed **live, no re-render**. But it is *relative* — it does **not** change the score's written tempo (the `\tempo` BPM marking stays).
- **The score's actual tempo** (the `\tempo N` directive / on-staff BPM) **requires re-emitting the score** (a regenerate). alphaTab can't mutate the written tempo live.

So "make tempo live" splits into two distinct designs.

## Options

- **(A) Live practice-speed (%) via `playbackSpeed`.** A slider (e.g. 50–120%) applied instantly, no re-render, no scroll/cursor reset. High value for a practice trainer (slow it down to learn). But it's a *speed multiplier*, not the written BPM — the staff marking won't reflect it.
- **(B) Absolute tempo (BPM), wired to the re-render seam.** Reuse the `onNeedsRerender` → host-replay path the **feel** control uses (triplet-feel IN7) so a BPM change self-applies without `Generate`. Accurate written tempo, but it re-emits + re-renders (scroll/cursor reset) for a number change.
- **(C) Both (the practice-tool pattern).** Keep an absolute **tempo (BPM)** — set at generate, re-rendered if changed (rare, via the seam) — *and* add a live **practice-speed (%)** slider (`playbackSpeed`) for everyday slow-down/speed-up.

## Recommendation

Lean **(C)**, MVP-able as **(A) first**: the live `playbackSpeed` slider is the responsive, high-value control and is cheap (no re-render). Keep the BPM control as-is but, if we want BPM edits to self-apply too, wire it to the existing feel re-render seam (B). Decide A-only vs A+B in design.

## Scope

**In:** a live practice-speed control on ScoreR via `api.playbackSpeed`; optionally wiring the BPM control to the existing re-render seam.
**Out:** persisting practice-speed as an exercise field (it's a transient view setting); metronome; tap-tempo.

## Validation

- Dragging practice-speed changes playback speed instantly with **no re-render** (scroll position + play cursor preserved).
- (If B) editing BPM updates the staff tempo without a `Generate` click.
- Dogfood: confirmed on the Practice + Content preview ScoreR.