---
type: design
id: de_01KWC2P5FB9R739B0QT3PC118Z
title: FretR theming + display polish
status: done
created: 2026-06-30
updated: 2026-06-30
version: 2
tags: []
parent_id: id_01KWC2NDD20PZTJ2ZY2YAX3MDF
requires_load: []
---
# FretR theming + display polish

## 1. Summary

A FretR theming + legibility pass: a **`theme: light|dark`** opt + two color tables, a **hideable per-cell theme toggle** + **`setTheme`** method, a **global Dark/Light toggle on GuitarVoicingsR** fanned out to every cell, dark-mode contrast (white fret numbers + `✕`), a larger fret-number font, and the **orientation toggle un-locked** on the standalone FretR pages. Reuses the existing per-control-visibility + fan-out patterns — no new plumbing model. Settled in `voicings-render-component-chat-001.md` (the GuitarVoicingsR dogfood pass).

## 2. Decisions

### 2.1 Theme as a color table behind a `theme` opt (default light)

FretR replaces its hardcoded color literals (nut `#222`, lines `#999`, fret numbers `#777`/`#555`, `✕` `#888`, position label) with a lookup into one of two **THEME tables** selected by `opts.theme` (`"light"` default | `"dark"`). The marker function/interval palette (`FUNCTION_COLORS` / override palette) is **unchanged** — it already reads on both backgrounds; only the chrome (lines, numbers, `✕`, nut, labels) is themed. Default `light` ⇒ every current standalone usage renders **byte-identical** (no regression).

| Chrome element | light | dark |
|---|---|---|
| Nut / string + fret lines | dark on white (current) | light gray on dark |
| Fret numbers / position label | dark gray (current) | **white** |
| Muted `✕` | gray (current) | **white** |

### 2.2 Per-cell theme toggle, hideable — parallel to orientation

FretR's toolbar gains a **Dark/Light toggle** governed by a new `controls.theme` visibility flag (default visible). Standalone diagrams show it; **inside a grid** the host passes `controls.theme:false` to hide it (the grid owns one global toggle). A **`setTheme(mode)`** method re-renders with the new theme — the fan-out hook, exactly mirroring `setOrientation`/`setLabelMode`.

### 2.3 GuitarVoicingsR owns one global theme toggle, fanned out

GuitarVoicingsR adds a **third global display control** — Dark/Light — beside its orientation + label toggles. It creates each cell with `theme: "dark"` (matching the dark cell background) + `controls.theme:false`, and on toggle fans out `setTheme(mode)` to every live FretR handle **without a re-fetch** (same mechanism as orientation/label). The grid thus defaults dark; the user can flip the whole grid to light.

### 2.4 Larger fret-number font + dark contrast

The fret-number / position-label font size is bumped (both orientations). In the dark table, fret numbers and `✕` are white; lines lighten for contrast. These are the same change as §2.1's dark table — one unit.

### 2.5 Expose orientation on the standalone pages

CAGED Chords, Scales, and the Content → Voicings preview stop passing `controls.orientation:false`, so each single diagram gets the vertical/horizontal toggle. (Scales was deliberately horizontal-only; un-locking it is intentional per the dogfood feedback.) These pages keep the **light** theme (white panel background), and now also expose the theme toggle (default on, via §2.2) — a user can dark-mode an individual diagram if they want.

## 3. Where the code lives

| Piece | Location |
|---|---|
| `theme` opt + THEME tables + `setTheme` + `controls.theme` toggle + font bump | `wwwroot/fretboard-render-component.js` |
| Global Dark/Light toggle + per-cell `theme:dark`/`controls.theme:false` + fan-out | `wwwroot/guitar-voicings-render-component.js` |
| Un-lock orientation (drop `controls.orientation:false`) | `wwwroot/caged-chords.js`, `wwwroot/scales.js`, the Content voicing preview (`content-crud.js`) |
| Sandbox fixtures (light vs dark, theme toggle) | `wwwroot/fretboard-sandbox.html` |

Dumb view (C1): theme is presentation only — no theory in JS.

## 4. Validation / dogfood

Sandbox shows a light and a dark fixture + the theme toggle; the live Voicings grid in dark reads cleanly (white fret numbers/`✕`, legible lines); the standalone pages flip orientation and (optionally) theme. The per-cell theme toggle is hidden in the grid, shown standalone.

## 5. Open / deferred

- Pages information-architecture (rename / retire / fold) → `voicings-pages-ia` thread.
- ScoreR / alphaTab theming → out of scope.
