---
type: chat
id: ch_01KXMY87GS4FAKMNX7CG13HQ6M
title: stop-when-change-page Chat 001
status: done
created: 2026-07-16
version: 1
tags: []
parent_id: null
requires_load: []
last_ai_block: 9
---
# stop-when-change-page Chat 001

## Rafa:

We need to stop sound when user change page

## AI:

Agreed — right now switching pages leaves audio playing. Let me lay out what's actually happening and the cleanest fix.

## What's going on

Page switching is `setupViewToggle().show(viewName)` in `app.js`. It just flips `hidden` on each view's element and calls the incoming view's `onShow` — it never touches the outgoing view's playback. So an alphaTab synth keeps running after you navigate away.

There isn't one player either — there are **three independent player engines**, each its own `ChordFlowPlayback.create(..., { player: true })` (its own alphaTab api + synth):

1. **Practice** — `app.js` ScoreR
2. **Content preview** — `content-crud.js` ScoreR (full transport, makes sound)
3. **Chord Sheets** — `chord-sheets.js`

Chord Sheets already `engine.stop()`s on a *content change*, but nothing stops playback on a *page change* — for any of the three.

## The design question

We could bolt a stop onto the view toggle by hand, but that means every current and future sound surface has to remember to wire itself in. Given the "durable foundation" bias, I'd rather make the engine layer own this so it's automatic.

**Recommended — a tiny registry inside `ChordFlowPlayback`:**

- Every `create()` adds its engine to a module-level live set; `dispose()` removes it.
- Expose `ChordFlowPlayback.stopAll()` that calls `.stop()` on each live player engine.
- `show()` in the view toggle calls `ChordFlowPlayback.stopAll()` at the top, before switching.

Result: any sound surface — the three today, and anything added later — stops on page change with zero per-view wiring, because the engine self-registers. It's ~15 lines in `playback-component.js` plus one call in `app.js`.

**Alternative — per-view `onHide` hooks:** add an `onHide` to the `views` map and have each module expose a `stop()`. More wiring, and it's the thing you have to remember for every new page. I don't think it's worth it over the registry.

**One scope check:** the ask is "when user changes page." I'd read that as the in-app nav switch (the `show()` path). Do you also want the same stop-all on the desktop window losing focus / closing (a `visibilitychange` / `beforeunload` hook)? That's a cheap add on top of the same `stopAll()`, but it's a separate trigger — say the word if you want it in scope.

If the registry + `stopAll()` on nav sounds right, I'll write it up (idea → design → req → plan, per our flow). Want me to spin those up?

## Rafa:

I like your recommendation
I think this could be done via `code quick`, if you agree:
code quick

## AI:

Done — shipped via `code quick`.

**What changed:**
- `playback-component.js` — added a live player-engine registry: each player-mode engine self-registers on `create()`, drops out on `dispose()`, and a new `ChordFlowPlayback.stopAll()` stops every registered engine. Lite render-only previews (no synth) don't register.
- `app.js` — `show()` (the view toggle) calls `ChordFlowPlayback.stopAll()` before switching views.

So changing pages now silences whatever was playing on the page you left — Practice, Content preview, Chord Sheets, and any sound surface added later, for free.

**Verified:** solution builds clean, full 922-test suite green, both JS files pass `node --check`. The change is wwwroot-only, so the C# suite doesn't exercise it directly — the audio-stop itself is best smoke-tested live (play a score → click another nav tab → sound cuts). Recorded as `plan-001` (done) in this thread.

Left out of scope (per your go on just the recommendation): stopping on the desktop window losing focus / closing — a one-liner on the same `stopAll()` whenever you want it.

## Rafa:

working
sorry I missed:
stopping on the desktop window losing focus / closing
do it

## AI:

Done — window blur / close now stops playback too.

Added to `app.js` `init()`: `stopAll()` on `window` `blur` (app loses focus, e.g. Alt-Tab / minimize / clicking another app) and `pagehide` (closing or navigating away). Same registry-wide `stopAll()`, so it covers every sound surface.

**Verified:** builds clean; C# suite unaffected (no C# touched); `app.js` passes `node --check`. Smoke-test live: play a score → Alt-Tab away or close the window → sound cuts. Recorded as `plan-002` (done).

One thing to know for the live test: `blur` also fires if focus jumps to the WebView DevTools (F12) — so in a `CHORDFLOW_DEVTOOLS` session, opening the inspector will stop playback. Harmless, but don't let it surprise you while debugging.

## Rafa:

Working
commit

## AI:

Committing now — the code (`app.js` + `playback-component.js`), the two done plans, and this chat, all in one commit on `main`.