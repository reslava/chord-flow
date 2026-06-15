---
type: reference
id: rf_01KTHJNV034RMM23TNY1RXF4SR
title: alphaTab JS API Reference
status: active
created: "2026-06-07T00:00:00.000Z"
updated: 2026-06-15
version: 5
tags: []
parent_id: null
requires_load: []
slug: alphatab-js-api-reference
description: Verified alphaTab JavaScript API surface for ChordFlow's WebView layer — init, player settings, playback methods, events.
---
# alphaTab JS API Reference

The JavaScript rendering + playback library ChordFlow drives from `wwwroot/app.js`. **Verified against the official docs** (API reference + player settings pages) on 2026-06-07.

## What it is / why JS build

alphaTab renders music notation and plays it back with a built-in SoundFont2/3 synthesizer. ChordFlow uses the **JS build** (not the .NET package) because it gives the synced playback cursor + beat-highlight events cheaply — the core feedback loop for a rhythm trainer.

> **No default player UI** — "alphaTab does not ship a default UI for the player." ChordFlow builds its own transport controls (play/stop/tempo) and calls the API.

## Initialization (settings object)

```javascript
const settings = {
  player: {
    enablePlayer: true,                 // default false; required for audio + cursor
    soundFont: '/soundfont/sonivox.sf2' // URL loaded automatically once player is ready
  }
};
const api = new alphaTab.AlphaTabApi(document.querySelector('#at'), settings);
```

- `player.enablePlayer` — boolean, default `false`. Must be `true` for playback/cursor.
- `player.soundFont` — `string | null`. URL loaded automatically after the player is ready. (Variants exist: `soundFontJavaScript`, `soundFontJSON`.)

## Loading content

- `api.tex(texString)` — renders the given alphaTex. **This is ChordFlow's load path** (imperative; the C# engine pushes the alphaTex string over the WebView2 bridge).
- **Multi-track rendering (verified, v1.8.3):** `api.tex(tex)` renders **only the first track** by default — a two-track score (comping + lead) shows just the comping staff. Render every track explicitly with `api.renderTracks(api.score.tracks)` (takes `Track[]` from `api.score.tracks`). ChordFlow's `score-render-component.js` does this in its `scoreLoaded` handler when `score.tracks.length > 1`; a single-track score is left on the default path (byte-identical render).
- **Bars per row (verified, v1.8.3):** the score's authored `defaultSystemsLayout N` only takes effect for **multi-track** scores (and needs `systemsLayoutMode = UseModelLayout`), so it's unreliable as the single control. Use the global **`display.barsPerRow`** on `LayoutMode.Page` instead — it governs single- AND multi-track: `4` = fixed four bars per row, `-1` = automatic (fit-to-width, the default). ChordFlow sets `barsPerRow: 4` by default with an **"Auto layout"** toggle (→ `-1`); runtime change via `api.settings.display.barsPerRow = N; api.updateSettings(); api.render();`. (The C# renderer no longer emits `defaultSystemsLayout` — bars-per-row is purely this JS setting.)

## Playback methods

- `api.playPause()` — toggle play/pause depending on current state.
- `api.stop()` — stop and reset playback position to the start.

## Events (subscribe via `api.<event>.on(handler)`)

| Event | Fires when |
|-------|-----------|
| `playerReady` | all data required for playback is loaded and ready |
| `soundFontLoaded` | the SoundFont needed for playback finished loading |
| `playerStateChanged` | playback state changed (→ map to ChordFlow `playbackFinished`) |
| `playerPositionChanged` | current playback position changed |
| `activeBeatsChanged` | the currently active beats across all tracks changed |
| `playedBeatChanged` | the played beat changed (→ ChordFlow `beatChanged` for progress/accuracy) |
| `renderFinished` | rendering of the whole sheet finished |

## ChordFlow bridge mapping (Photino ↔ alphaTab)

| Bridge envelope (C#→JS) | alphaTab call |
|--------------------------|---------------|
| `{type:"loadScore", tex}` | `api.tex(msg.tex)` |
| `{type:"play"}` / `{type:"stop"}` | `api.playPause()` / `api.stop()` |

| alphaTab event | Bridge envelope (JS→C#) |
|----------------|--------------------------|
| `playerStateChanged` (→ stopped at end) | `{type:"playbackFinished"}` |
| `playedBeatChanged` | `{type:"beatChanged", bar, beat}` |
| `playerReady` / `soundFontLoaded` | `{type:"ready"}` |

## To confirm against the installed version
- ⚠️ Exact event subscription shape (`api.playerReady.on(...)` vs `api.addEventListener`) — confirm with the bundled alphaTab version.
- ⚠️ Whether a small redistributable GM `.sf2` ships with the npm package or must be sourced separately (check license + size).

## Sources
- API reference: https://www.alphatab.net/docs/reference/api/
- Player enable: https://www.alphatab.net/docs/reference/settings/player/enableplayer
- Soundfont: https://www.alphatab.net/docs/reference/settings/player/soundfont
- Audio playback guide: https://www.alphatab.net/docs/guides/audio-playback
