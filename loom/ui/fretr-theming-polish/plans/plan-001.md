---
type: plan
id: pl_01KWC2QFRF718R4ASGCCK3JSXX
title: FretR theming + display polish
status: done
created: 2026-06-30
updated: 2026-06-30
version: 1
design_version: 2
req_version: 1
tags: []
parent_id: de_01KWC2P5FB9R739B0QT3PC118Z
requires_load: []
target_version: 0.1.0
actual_release: 0.13.0
steps:
  - id: fretr-theme-tables-settheme-larger-font
    order: 1
    status: done
    description: Replace FretR's hardcoded chrome color literals with a lookup into one of two THEME tables selected by opts.theme (light default | dark); add the setTheme(mode) method; bump the fret-number/position-label font. Dark table uses white fret numbers + white ✕ and legible line/nut colors. The marker palette is untouched.
    files_touched: [src/ChordFlow.Desktop/wwwroot/fretboard-render-component.js]
    blocked_by: []
    satisfies: [IN1, IN3, IN5, IN6, C1, C2]
  - id: fretr-per-cell-theme-toggle-controls
    order: 2
    status: done
    description: "Add a Dark/Light toggle button to FretR's toolbar governed by a new controls.theme visibility flag (default visible); clicking calls setTheme. Hidden when the host passes controls.theme:false (inside a grid)."
    files_touched: [src/ChordFlow.Desktop/wwwroot/fretboard-render-component.js]
    blocked_by: [fretr-theme-tables-settheme-larger-font]
    satisfies: [IN2, C3]
  - id: guitarvoicingsr-global-theme-toggle-fan-out
    order: 3
    status: done
    description: "Add a global Dark/Light toggle beside the orientation/label toggles; create each cell with theme:'dark' + controls.theme:false (grid defaults dark); on toggle fan setTheme out to every live FretR handle with no re-fetch."
    files_touched: [src/ChordFlow.Desktop/wwwroot/guitar-voicings-render-component.js]
    blocked_by: [fretr-per-cell-theme-toggle-controls]
    satisfies: [IN4, C3]
  - id: expose-orientation-on-the-standalone-pages
    order: 4
    status: done
    description: "Stop passing controls.orientation:false on the standalone FretR usages (CAGED octave shapes, CAGED Chords, Scales, the Content → Voicings preview) so each single diagram gets the vertical/horizontal toggle; they keep their default orientation and now also expose the theme toggle."
    files_touched: [src/ChordFlow.Desktop/wwwroot/caged-shapes.js, src/ChordFlow.Desktop/wwwroot/caged-chords.js, src/ChordFlow.Desktop/wwwroot/scales.js, src/ChordFlow.Desktop/wwwroot/content-crud.js]
    blocked_by: [fretr-per-cell-theme-toggle-controls]
    satisfies: [IN7]
  - id: sandbox-fixtures-dogfood-pass-ref-sync
    order: 5
    status: done
    description: Add light/dark + theme-toggle fixtures to fretboard-sandbox.html; visually confirm the Voicings grid in dark reads cleanly and the standalone pages flip orientation/theme; update the architecture ref §5 FretR/GuitarVoicingsR paragraphs with the theme opt + global toggle.
    files_touched: [src/ChordFlow.Desktop/wwwroot/fretboard-sandbox.html, loom/refs/chordflow-architecture-reference.md]
    blocked_by: [guitarvoicingsr-global-theme-toggle-fan-out, expose-orientation-on-the-standalone-pages]
    satisfies: [IN5, IN6]
---
# FretR theming + display polish

## Goal

Add a light/dark theme to the shared FretR fretboard component and wire it into GuitarVoicingsR, plus the small legibility/usability fixes from the GuitarVoicingsR dogfood pass. Reuse the existing per-control-visibility + live-handle fan-out patterns (no new plumbing): a `theme` opt + two chrome color tables defaulting to light (byte-identical standalone render), a hideable per-cell theme toggle + `setTheme` method, one global Dark/Light toggle on GuitarVoicingsR fanned out to every cell with grid cells defaulting dark, dark-mode contrast (white fret numbers + ✕), a larger fret-number font, and the orientation toggle un-locked on the standalone FretR pages.

---

## Steps

| Done | # | Step | Files touched | Blocked by | Satisfies |
|---|---|---|---|---|---|
| ✅ | 1 | Replace FretR's hardcoded chrome color literals with a lookup into one of two THEME tables selected by opts.theme (light default \| dark); add the setTheme(mode) method; bump the fret-number/position-label font. Dark table uses white fret numbers + white ✕ and legible line/nut colors. The marker palette is untouched. | src/ChordFlow.Desktop/wwwroot/fretboard-render-component.js | — | IN1, IN3, IN5, IN6, C1, C2 |
| ✅ | 2 | Add a Dark/Light toggle button to FretR's toolbar governed by a new controls.theme visibility flag (default visible); clicking calls setTheme. Hidden when the host passes controls.theme:false (inside a grid). | src/ChordFlow.Desktop/wwwroot/fretboard-render-component.js | fretr-theme-tables-settheme-larger-font | IN2, C3 |
| ✅ | 3 | Add a global Dark/Light toggle beside the orientation/label toggles; create each cell with theme:'dark' + controls.theme:false (grid defaults dark); on toggle fan setTheme out to every live FretR handle with no re-fetch. | src/ChordFlow.Desktop/wwwroot/guitar-voicings-render-component.js | fretr-per-cell-theme-toggle-controls | IN4, C3 |
| ✅ | 4 | Stop passing controls.orientation:false on the standalone FretR usages (CAGED octave shapes, CAGED Chords, Scales, the Content → Voicings preview) so each single diagram gets the vertical/horizontal toggle; they keep their default orientation and now also expose the theme toggle. | src/ChordFlow.Desktop/wwwroot/caged-shapes.js, src/ChordFlow.Desktop/wwwroot/caged-chords.js, src/ChordFlow.Desktop/wwwroot/scales.js, src/ChordFlow.Desktop/wwwroot/content-crud.js | fretr-per-cell-theme-toggle-controls | IN7 |
| ✅ | 5 | Add light/dark + theme-toggle fixtures to fretboard-sandbox.html; visually confirm the Voicings grid in dark reads cleanly and the standalone pages flip orientation/theme; update the architecture ref §5 FretR/GuitarVoicingsR paragraphs with the theme opt + global toggle. | src/ChordFlow.Desktop/wwwroot/fretboard-sandbox.html, loom/refs/chordflow-architecture-reference.md | guitarvoicingsr-global-theme-toggle-fan-out, expose-orientation-on-the-standalone-pages | IN5, IN6 |
---

### Legend

| Symbol | Meaning |
|--------|---------|
| ✅ | Done |
| 🔄 | In Progress |
| 🔳 | Pending |
| ❌ | Cancelled |
