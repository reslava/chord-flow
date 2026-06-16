---
type: req
id: rq_01KV7P09B3S4G6SZP6KDD2M6NM
title: SoundFont library — pick & load playback soundfonts — Requirements
status: locked
created: "2026-06-16T00:00:00.000Z"
updated: 2026-06-16
version: 1
tags: []
parent_id: id_01KV7MTAXG10Y0HQVYABXZ7TVA
requires_load: []
---
# SoundFont library — pick & load playback soundfonts — Requirements

### ✅ Included

- `IN1` A UI control (picker) in the score controls strip to choose/load which SoundFont drives playback.
- `IN2` Available soundfonts are auto-discovered from the soundfont folder — adding one is a data drop, no code change.
- `IN3` The chosen soundfont is a **global** setting that persists across exercises and sessions.
- `IN4` Selecting a soundfont switches the active font on the running player (applies live).
- `IN5` Ship one small default soundfont (sonivox) so playback works out of the box and as the fallback when no choice is stored.

### ❌ Excluded

- `EX1` In-app downloading/installing of soundfonts (users download manually from a documented list).
- `EX2` Per-track / per-instrument soundfont assignment.
- `EX3` Soundfont editing or bank/program remapping.
- `EX4` Bundling large soundfont banks in the repo — non-default `.sf2` files are gitignored; the repo documents where to get them and where to place them.
- `EX5` Per-exercise soundfont choice (the setting is global only).

### ⛓ Constraints

- `C1` No Domain / renderer / alphaTex change — this is a playback-engine + asset + persistence concern only.
- `C2` Discovery seam lives in Core (`ISoundFontCatalog`), implemented by the host; Core stays UI/host-agnostic so a future web host plugs in its own catalog.
- `C3` The global choice persists in Core (an `AppSettings` key/value store), not host-side, so a future host reuses it.
- `C4` Soundfont verbs extend the existing narrow JSON-envelope bridge protocol; font is not a `renderOptions` input.
- `C5` `score-render-component.js` remains the single owner of the alphaTab integration; the picker is a player-kind (local) control with no C# re-render.
