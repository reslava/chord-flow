---
type: req
id: rq_01KWC2PP01RQV5C2BZSY526HYP
title: FretR theming + display polish — Requirements
status: locked
created: 2026-06-30
updated: 2026-06-30
version: 2
tags: []
parent_id: id_01KWC2NDD20PZTJ2ZY2YAX3MDF
requires_load: []
---
# FretR theming + display polish — Requirements

### ✅ Included

- `IN1` A **`theme: "light" | "dark"`** opt on FretR selecting one of two **surface** color tables — the render-area **background** + toolbar/legend foreground + buttons/inputs + SVG chrome (nut, lines, fret numbers, position label, muted `✕`). Default **`light`** (white surface, dark contrast) preserves the existing dark-on-white diagram.
- `IN2` A **per-cell Dark/Light toggle** in FretR's toolbar, governed by a new **`controls.theme`** visibility flag (default visible; hidden inside a grid).
- `IN3` A **`setTheme(mode)`** method on FretR that re-renders with the new theme — the fan-out hook (parallel to `setOrientation`/`setLabelMode`).
- `IN4` A **single global Dark/Light toggle on GuitarVoicingsR** that fans out `setTheme` to every cell **without a re-fetch**; grid cells are created **`theme:"dark"` + `controls.theme:false`** (grid defaults dark).
- `IN5` **Dark-mode contrast**: white fret numbers + white muted `✕`, and line/nut colors legible on a dark background.
- `IN6` A **larger fret-number / position-label font** in both orientations.
- `IN7` **Expose the orientation toggle** on the standalone FretR pages — the **CAGED octave-shapes** page, CAGED Chords, Scales, and the Content → Voicings preview stop passing `controls.orientation:false`.

### ❌ Excluded

- `EX1` The **pages information-architecture** decision (rename CAGED → Octave shapes, retire CAGED Chords, fold Content↔Voicings) — owned by the `voicings-pages-ia` thread.
- `EX2` Theming **ScoreR** / alphaTab — this pass is FretR only.
- `EX3` Changing the **marker function/interval palette** — it already reads on both backgrounds; the theme covers the surface (background + chrome + toolbar/legend), not the marker colors.

### ⛓ Constraints

- `C1` **Dumb view** — theme is presentation only; no music theory in JS.
- `C2` **No light-mode regression** — `light` (the default) preserves the existing **dark-on-white diagram**; the component now **owns its themed render surface** (background + foreground), so the toolbar/legend chrome adapts to that surface (it was previously dark-styled regardless of the host background) rather than rendering pixel-identical.
- `C3` **Reuse the existing patterns** — the per-control-visibility flag (`controls.theme`) and the live-handle fan-out (`setTheme`) mirror orientation/label; no parallel plumbing.
