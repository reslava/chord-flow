---
type: idea
id: id_01KXJQHWZJ36XZCEBA2HTW1EBZ
title: Metronome & count-in produce no sound (pre-existing regression)
status: done
created: 2026-07-15
version: 1
tags: []
parent_id: null
requires_load: []
---
# Metronome & count-in produce no sound (pre-existing regression)

## What

The Practice transport's **Metronome** and **Count-in** toggles produce no audio: toggling on + pressing play gives no click track and no count-in lead-in bar. Everything else in playback (play/pause/stop, tempo, cursor, per-track volume, soundfont) works.

## Why (status)

Surfaced during the `chord-sheets/chord-sheets-playback` Plan 1 parity check (the ChordFlowPlayback extraction). **Not caused by that refactor** — the code path is byte-identical to the committed original and `alphaTab.min.js` was untouched. Rafa confirms metronome/count-in **worked when first implemented** but broke silently in a later thread's plan and weren't re-checked since. So this is a real, pre-existing regression to hunt down and fix.

## What's known / already tried

- The wiring is `api.metronomeVolume = on ? 1 : 0` / `api.countInVolume = on ? 1 : 0` on the alphaTab api (now in `ChordFlowPlayback`, `playback-component.js`) — the same call the feature has always used.
- alphaTab bundle inspection: `AlphaTabApi.metronomeVolume` forwards to `this.Mw` (the player); the synth-side setter only pushes to the live output (`this.aw && (this.aw.metronomeVolume = t)`) if the output already exists.
- **Tried (didn't fix):** re-asserting the desired metronome/count-in state on `soundFontLoaded` + every `scoreLoaded` (in `ChordFlowPlayback`). So the cause is likely deeper in the alphaTab metronome enablement for this bundle version, not a timing miss.

## Next steps (for the fix session)

- The app's WebView has **no devtools** — first add `CoreWebView2.Settings.AreDevToolsEnabled = true` (reversible) so the console + `api.metronomeVolume` behaviour can be inspected live.
- Confirm the correct metronome enablement for this alphaTab version (settings.player flag vs runtime property; whether the shipped soundfont provides the metronome sample; whether a percussion/metronome channel needs setup).
- Bisect against git history to find the thread/plan that broke it (it worked at implementation).

## Validation

- Toggle Metronome on + play → audible click on each beat. Toggle Count-in on + play → an audible count-in bar before playback. Both in the Practice view.

## Reference material

- Origin: `loom/chord-sheets/chord-sheets-playback/chats/chat-001.md` (the diagnosis) and that thread's Plan 1 done doc.
- Code: `src/ChordFlow.Desktop/wwwroot/playback-component.js` (`ChordFlowPlayback`, the metronome/count-in setters + re-assert), `score-render-component.js` (the toggles).