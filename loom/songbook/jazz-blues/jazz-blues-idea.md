---
type: idea
id: id_01KVTQXQWTBNAH7GEKF74XF75Y
title: Jazz Blues — First Real Song
status: done
created: 2026-06-23
updated: 2026-06-23
version: 2
tags: []
parent_id: null
requires_load: []
---
# Jazz Blues — First Real Song

## Goal

Author **one real jazz blues** (12-bar) end-to-end as an importable content definition bundle, then drive it through the app (Practice + the now/next boards) to **surface where the kernel, renderer, and DSL bend or break under real music**. This is the first deliberate dogfood: real content pulling on the engine, not new substrate built ahead of use.

The deliverable is two things: (a) a playable jazz-blues bundle, and (b) a **written gap/findings log** — the list of everything real music exposes. That log, not a pre-drawn plan, dictates the next thread.

## Why a jazz blues first

A jazz blues is the **smallest real form** that exercises the harmony the MVP hand-waved:

- **swing feel** (not straight eighths),
- a **ii–V (–I) turnaround** in the last bars,
- **secondary / substitute dominants** (e.g. `VI7`, the `#IVdim`, the tritone sub),
- **quick changes** and **multi-chord bars**.

It is idiomatic, finite (12 bars), and universally known — so any gap is obvious **by ear**. It is the honest stress test of the architecture before we invest in the derived-voicings differentiator.

## Shape (grounded in the existing architecture)

- **Content as a definition bundle.** Authored content loads via the existing **importable-bundle path** (Persistence seeds from bundles — *never hardcoded*), so this song is an **additive data drop**, not a code change.
- **Progression** in the Progression DSL (Nashville scale-degree notation). The exact changes (basic jazz blues vs. bird-blues) and key are a **design decision**, not pre-decided here.
- **Rhythm** = a swing comping pattern over the 48-PPQ tick grid (feel/accent/stroke overlays).
- **Voicing** = the comped voicings the renderer already realizes at the chosen difficulty.

## Approach

1. **Pick the changes + key** (basic jazz blues first; bird-blues noted as a stretch). Settle in design.
2. **Author the bundle** — progression + rhythm + key + voicing difficulty.
3. **Load + play in Practice** — watch the score, cursor, swing feel, and the now/next boards.
4. **Log every gap** in a findings list: parser can't express X · rhythm grid rounds Y · renderer mis-spells Z · voicing picks the wrong shape. *This list is the real deliverable.*
5. **Triage** — fix small in-thread gaps; spin follow-on threads for the big ones (a gap that needs DSL or kernel work becomes its own thread).

## Scope

**In:**
- One jazz-blues content bundle (progression + swing rhythm + key + voicing).
- Play-through on Practice with synced now/next boards.
- A concrete **gap/findings log**, each item tagged *fix-now* vs. *new-thread*.

**Out (future / other threads):**
- The jazz **standard** (`songbook/jazz-standard`).
- **Derived voicings** — shell (3–7), guide-tone lines, drop-2 — the differentiator (its own thread, likely homed in `guitar`).
- **Lead melodies** with pickups (`songbook/lead-melodies`).
- **Bird-blues** changes (stretch — only if the basic blues lands clean).
- Any engine rewrite — gaps are triaged out into their own threads, not solved inline here.

## Open questions

- **Changes**: basic jazz blues vs. bird-blues; **key**: Bb vs. F (idiomatic jazz-blues keys).
- Does the Progression DSL already express **secondary dominants** and the **turnaround**, or is that the first gap?
- Is the current **triplet-feel** overlay enough for real swing, or does it need more (accent/stroke nuance)?
- Does the comping voicer pick **idiomatic jazz shapes** for these chords, or does it expose the need for derived voicings sooner than expected?

## Validation

- The blues **plays end-to-end** in Practice with correct chords, audible swing feel, and now/next boards tracking the cursor — **dogfood: render/play on the app's Practice surface** (per the guitar-weave dogfood rule).
- A written **findings log** exists, each gap tagged *fix-now* or *spin-a-thread* — proving the dogfood did its job: real music dictating the roadmap.
