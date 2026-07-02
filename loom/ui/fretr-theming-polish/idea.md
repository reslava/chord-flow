---
type: idea
id: id_01KWC2NDD20PZTJ2ZY2YAX3MDF
title: FretR theming + display polish
status: done
created: 2026-06-30
updated: 2026-06-30
version: 2
tags: []
parent_id: null
requires_load: []
---
# FretR theming + display polish

## Problem / motivation

The shared fretboard render component **FretR** (`fretboard-render-component.js`) hardcodes a single **light-mode** color scheme (dark lines on white) that was right when it only rendered into the white `.cc-preview` panels (Scales, CAGED Chords, Content → Voicings preview). Now that **GuitarVoicingsR** renders dozens of FretR cells inside **dark** grid cells, the same colors read poorly — the fret numbers, the `✕` muted-string marks, and the nut/string lines lack contrast on a dark background. Surfaced during the GuitarVoicingsR dogfood pass (`voicings-render-component-chat-001.md`).

Three smaller papercuts came up in the same pass:
- The standalone FretR pages (CAGED Chords, Scales) **lock the orientation toggle off**, so the user can't flip a single diagram between vertical chord-box and horizontal neck — even though FretR already supports both.
- The **fret-number font is too small** to read comfortably.
- Dark-mode **contrast** generally needs work (white fret numbers + white `✕`).

## Concept

A focused **FretR theming + legibility** pass, reusing the exact patterns GuitarVoicingsR already established:

- **`theme: "light" | "dark"`** opt on FretR with two color tables (nut, string/fret lines, fret numbers, muted-`✕`, position label). Defaults to **light** so every existing standalone usage renders byte-identical.
- A **per-cell dark/light toggle** in FretR's toolbar, hideable via `controls.theme` — shown on standalone diagrams, **hidden inside a grid** (exactly like the orientation toggle).
- A **`setTheme(mode)`** method so a host can drive it — the fan-out hook.
- **GuitarVoicingsR** gains **one global Dark/Light toggle** that fans out to every cell via `setTheme` (no re-fetch), and creates its cells **dark by default** (matching the cell background) with `controls.theme:false`. Same shape as the orientation/label fan-out.
- **Dark-mode contrast**: white fret numbers + white `✕`, legible line colors on dark.
- **Bigger fret-number font.**
- **Expose the orientation toggle** on the standalone FretR pages (CAGED Chords, Scales, Content voicing preview) — stop passing `controls.orientation:false`.

## Scope

**In (v1):** the `theme` opt + the two color tables; the FretR theme toggle (hideable) + `setTheme`; GuitarVoicingsR's global theme toggle fanned out (grid defaults dark); dark-mode contrast (white fret numbers/`✕`); a larger fret-number font; un-locking the orientation toggle on the standalone pages.

**Out (deferred):**
- The **pages information-architecture** decision (rename CAGED → Octave shapes, retire CAGED Chords, fold Content↔Voicings) — its own design-first thread (`voicings-pages-ia`).
- Theming **ScoreR** / alphaTab — this pass is FretR only.

## Validation

- **Dogfood:** the GuitarVoicingsR grid in dark mode reads cleanly (fret numbers, `✕`, lines all legible); the standalone pages can flip orientation; the per-cell theme toggle is hidden inside the grid and shown standalone. Exercised via `fretboard-sandbox.html` + the live pages (guitar-weave dogfood rule).
