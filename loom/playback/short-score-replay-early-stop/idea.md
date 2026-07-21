---
type: idea
id: id_01KY1QJYDHJEWVNGMXA5WPJVQA
title: Short-score replay stops early (alphaTab)
status: draft
created: 2026-07-21
version: 1
tags: []
parent_id: null
requires_load: []
---
# Short-score replay stops early (alphaTab)

## Symptom

On the **Rhythm Generator** page (`exercises/generated-rhythms-for-practice`, Phase 2), pressing **Play** a second (and other alternating) time replays the short percussion score **only partway** and stops early — audible and reproducible. Surfaced during dogfooding; the user confirmed it by ear.

## Reproduction (CDP)

App launched with `CHORDFLOW_DEVTOOLS=1` + `WEBVIEW2_ADDITIONAL_BROWSER_ARGUMENTS=--remote-debugging-port=9223`; driven from a Node CDP harness (scratchpad `repro-finish3.mjs` / `repro-events.mjs`) that clicks the real Play button (`Input.dispatchMouseEvent` — a trusted gesture) and samples `window.__cfApi.tickPosition` / `playerStateChanged`.

Default generation (eighth family, AnchorRotate, Cycle, 2 bars) → a 2-bar percussion score. Observed max tick per consecutive play:

```
play#1: 7267   (full — score end is 7680)
play#2: 4636   (early — ~60%)
play#3: 4636 / 7058   (varies)
play#4: 7154   (full)
```

The early-stop tick recurs at **~4636** — deterministic, not a random cutoff. Roughly alternating full / short.

## Ruled out

- **Not the Play/Pause toggle.** Replacing the shared engine's blind `api.playPause()` with an explicit `playerState`-checked `api.play()`/`api.pause()` did **not** change the behavior (fix confirmed loaded in-page via `fetch("playback-component.js")`). Reverted the change.
- **Not a short/truncated score.** `api.score` reports **2 masterBars, endTick 7680, 1 track** — the full 2 bars.
- **Not a stale playhead.** After each finish/stop the playhead resets to tick ~0/1; `play#2` starts from 0 and still stops at ~4636.
- **Not our page's reload path.** No `rhythmGenerate`/`load()` fires between consecutive Play clicks (no control change), so the score isn't being reloaded mid-sequence.

## Working hypothesis

An **alphaTab-internal replay issue** with rapid re-play of a short percussion score (synth/midi-render state not fully reset between plays) — matches the user's suspicion. Not ChordFlow wiring.

## Next steps (when picked up)

- Check whether the same alternating early-stop reproduces on the **Drums page** (identical shared engine, similar short percussion tex) — isolates page-specific vs engine-wide.
- Capture whether alphaTab emits a `playerStateChanged(stopped)` at tick ~4636 on a bad play (engine deciding to stop) vs the synth simply going silent.
- Try a **page-scoped** workaround that does NOT touch shared pause/resume semantics: e.g. reload the score (`load(tex)`) or `api.stop()`+seek immediately before a fresh Play **only when stopped-at-end** — verify via the CDP harness.
- Review the vendored **alphaTab 1.8.3** changelog / known issues for short-score / percussion replay; consider a version bump if fixed upstream.

## Notes

- Does **not** block the generation feature — generate / grid / count-overlay / first-play all work.
- Harness + `cdp.mjs` driver preserved in the session scratchpad; the CDP pattern is the ctx §Rules "Scripted debugging via CDP" recipe.
