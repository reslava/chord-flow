---
type: chat
id: ch_01KXNWANHR6GKMDTC17QV1ETJH
title: metronome-true-marker Chat 001
status: done
created: 2026-07-16
version: 1
tags: []
parent_id: null
requires_load: []
last_ai_block: 9
---
# metronome-true-marker Chat 001

## Rafa:

read thread context

is all clear & grounded for req + plan?

we should design this "time clock" as special component of the architecture, so could be useful for future features, like a metronome

we started talking about this idea in `loom/ui/harmony-controls-r/chats/chat-001.md`, here an extract:
>`playback-component.js`: subscribe `api.playerPositionChanged`, derive `(bar, quarterBeat)` from `currentTick` (960 ticks per quarter in alphaTab's MIDI; 4/4 bars = 3840 ticks), emit a new `"position"` bus event alongside the existing `"beat"`.

We could call this component something like `PlaybackPositionPublisher` or similar.

## AI:

Loaded idea + design + the architecture ref. Overall: yes — the thread is clear and grounded for req + plan, with **one open design decision** (the one you just raised). Answering both parts:

### 1. Grounded for req + plan?

The idea and design are solid and mutually consistent. The two "verify at implementation" flags in the design (exact `playerPositionChanged` arg shape on v1.8.3; the master-bar start-tick property) are fine as implementation-time checks — they don't block the req. Note the design already fixed the one wrong assumption from the harmony-controls-r extract you quoted: no fixed 3840-tick bars — bar starts come from the score model, which is what makes pickup (`\ac`) bars work.

So the only thing standing between us and req → plan is deciding the component shape below.

### 2. The "time clock" as an architecture component

Agree with the instinct — the tick → (bar, quarter) derivation + bar-start cache + dedupe latch is a coherent unit with a name, not loose code inside an event handler. Two shapes for it:

**Option A — named internal component, composed by the engine (recommended).**
A `PlaybackClock` class inside `playback-component.js`, owned by `ChordFlowPlayback`: the engine feeds it `api.playerPositionChanged` + the score model on `load()`, and the clock's output is emitted on the engine's existing multi-subscriber bus as the `"position"` event.

- The **reuse contract is the bus event, not the file layout**: a future metronome widget (visual flasher, pendulum, or a custom click) just does `engine.on("position", …)` — exactly how PlayerControlsR consumes `stateChange` today. Zero new wiring per consumer.
- The clock has no UI and no independent lifecycle — its only inputs (the api, the score, the load/reset moments) are things `ChordFlowPlayback` already owns. Giving it its own file + `window.*` global would be component ceremony without a consumer that needs it.
- It still gets named in the architecture ref as part of the playback-engine seam (`ChordFlowPlayback` composes `PlaybackClock`; emits `beat · position · …`), so it's a first-class citizen of the map.

**Option B — standalone module (`playback-clock-component.js`, own global) binding to an engine handle**, PlayerControlsR-style. More visible in the architecture, and the right shape **if/when** the clock grows real independent behavior — sub-beat subdivisions, look-ahead scheduling for a sample-accurate audio click, count-in ticks. Today that's speculative; extraction later is additive (the consumers only know the bus event, so nothing downstream changes).

My call: **A now, B only when a consumer demands it.** Durable ≠ heavy — the durability is in the named class + the stable `"position"` contract.

**Naming:** I'd prefer **`PlaybackClock`** over `PlaybackPositionPublisher`. "Publisher" describes the mechanism (and the bus already is the publisher); "clock" is what the thing *is* — the design doc literally calls it "a time-based playback clock", and "the synth's own position is the only honest clock" is the design's core argument. Future-feature sentences also read naturally: *the metronome consumes the clock*.

**One future-proofing touch worth baking in now (cheap):** make the quarter divisor a parameter of the class (`ticksPerStep = 960`, default quarter) rather than a hardcoded literal. A future metronome wanting eighth-note subdivision becomes a constructor arg, not a refactor. I would NOT add per-consumer resolutions or count-in ticks now — no consumer, out of scope.

If you're good with A + `PlaybackClock` + the divisor param, next actions are: patch the design doc's Engine section to name the component (small `loom_patch_doc`-style update via `loom_update_doc`), then req → lock → plan.

**STOP — waiting for your call on A/B and the name.**

## Rafa:

Agree all, A + `PlaybackClock` + the divisor param
update design
then req → lock → plan

## AI:

Done — design updated, req created + locked, plan created.

**Design (`design.md`, patched in 3 spots):**
- Decision now names the component: the clock is **`PlaybackClock`**, composed inside `ChordFlowPlayback` (decided here).
- Engine section leads with the component bullet: what it owns (bar-start cache, tick → (bar, quarterBeat), dedupe latch), why it's internal not a standalone module (no UI, no independent lifecycle; the reuse contract is the bus event), and the `ticksPerStep = 960` constructor param.
- Test/verify note extends the architecture-ref sync to name the composed `PlaybackClock`.

**Req (`req.md`, locked):** IN1–IN8 (PlaybackClock class, model-derived bar starts, divisor param, dedupe, `"position"` bus event, shell fan-out, marker mode split, arch-ref sync) · EX1–EX5 (native cursor, Now/Next/bridge echo, C#/bridge, alphaTab upgrade, sub-beat/count-in ticks) · C1–C3 (JS-only + validation walk, 1-based payload convention, out-of-range/count-in clamping).

**Plan (`plans/plan-001.md` — 4 steps, every step citing its IN/C handles):**

1. **PlaybackClock + `"position"` engine event** — the class + engine wiring in `playback-component.js` (IN1–IN5, C2, C3)
2. **Shell fan-out + Sheet marker mode split** — `app.js` + `chord-sheets.js` (IN6, IN7)
3. **Validation walk + devtools check** — charleston straight/swing, pickup song, tempo change, mode switch, 4-steps-per-bar console check (C1)
4. **Architecture ref sync** — `ChordFlowPlayback` composes `PlaybackClock`, bus emits `beat · position · …` (IN8)

`loom_verify_req` ran clean: no uncovered Included, no Excluded citations, no dangling handles (the semantic pass I judged myself — sampling is blocked in this host — no step strays into EX territory).

Next: `loom_start_plan` + Step 1 (`playback-component.js`) on your go.

## Rafa:

I started plan
do plan

## AI:

`do plan` run: **Steps 1, 2, 4 ✅ — Step 3 (the validation walk) is yours**, since "the marker steps evenly" is a visual check by design (C1). The app is built and **running now with `CHORDFLOW_DEVTOOLS` enabled**.

**Step 1 — `PlaybackClock` + `"position"` event (`playback-component.js`)** ✅
The design's "verify at implementation" flag caught a real trap: on alphaTab v1.8.3 the score model's `masterBar.start` **zero-widths a pickup bar** (`start += prev.isAnacrusis ? 0 : duration`) while the sequencer accumulates real durations — so the model timeline drifts one pickup-length off the synth for any `\ac` score. The clock therefore caches bar starts from **`api.tickCache.masterBars`** (verified in the shipped bundle: `masterBar`/`start`/`tickDuration`) — the playback timeline itself, anacrusis-correct by construction. Cache identity-keys on the tickCache object (self-rebuilds once per load, no event coordination); dedupe latch resets on `scoreLoaded`; `ticksPerStep` constructor param (default 960 = quarter); binary-search lookup; out-of-range ticks keep the last marker, count-in ticks clamp to the first bar. One extra guard beyond the design: emission is gated on `isPlaying`, because `stop()` seeks back to tick 0 and would otherwise re-highlight bar 1 beat 1 after the marker cleared.

**Step 2 — consumers** ✅
`chord-sheets.js`: new `onPosition(bar, quarterBeat)` drives the marker only in `metronome` mode (same downbeat-cell lookup + `highlightBeat`, shared `lastMarkerKey`); `onBeat` narrowed to `chord` mode only. `app.js`: page-level `view.getEngine().on("position", …)` fan-out next to the beat fan-out — no ScoreR passthrough. Now/Next + `beatChanged` echo untouched (EX2). `node --check` passes on all three files.

**Step 4 — architecture ref** ✅
6 patches: the playback-engine seam names the composed `PlaybackClock` (incl. the tickCache-not-model-start rationale), the bus lists `position`, the Practice-page diagram + chord-sheet-playback section updated.

**Step 3 — your walk** (app is running):
1. Charleston (`X...--X.--------`) in Sheet view, Visual metronome, straight AND swing → marker steps evenly, 4 per 4/4 bar, through silences and sustains.
2. Per chord mode + Now/Next → exactly as before.
3. A song with a pickup (`\ac`) bar → marker aligned after the anacrusis.
4. Tempo change mid-session → step rate follows.
5. Mode switch mid-playback → clean.

Devtools console check (F12 → paste, then play a bar or two and stop):
```js
(() => {
  const e = window.__cfEngine;
  if (!e) return "no __cfEngine — launch with CHORDFLOW_DEVTOOLS";
  const counts = {};
  e.on("position", (bar, q) => { counts[bar] = (counts[bar] || 0) + 1; console.log("position", bar, q); });
  e.on("finished", () => console.table(counts));
  return "counting position steps per bar — play, stop, read the table (expect 4 per 4/4 bar)";
})();
```

Say the word when the walk passes and I'll mark Step 3 ✅ and close the plan.

## Rafa:

All working good. Except: pickup, sheet did not show the pickup bar. We know alphaTab start numbering \ac bar as bar 1. Sheet almost start at bar 2. This could be hard to fix.
I ran the js script, showed the msg: counting position steps per bar — play, stop, read the table (expect 4 per 4/4 bar)

I played, but i couldn't see any counting...
I don't know how to handle the console very well

## AI:

Good news: I automated the console check myself, and it found (and fixed) two real bugs before passing everything. **Plan closed — all 4 steps ✅.**

**The automated check** — I attached to the running app through the WebView2 debugging port, dispatched a *trusted* click (that's the key: Chromium's autoplay policy blocks the synth's audio without a user gesture — a purely programmatic `play()` reports "Playing" but time never advances), fed test scores straight to `window.__cfEngine`, played them for real, and asserted the emitted `"position"` sequences:

- 2 bars of 4/4 **whole notes** → `1:1 1:2 1:3 1:4 2:1 2:2 2:3 2:4` — 4 steps per bar through sustains (the event-driven signal would fire once per bar). **PASS**
- **`\ac` pickup** (1 quarter) + 2 full bars → `1:1  2:1..2:4  3:1..3:4` — the anacrusis gets exactly its real length, bars aligned after it. **PASS**
- **Replay** of the same score → both runs start at `1:1`. **PASS**

**Two bugs the harness caught (both fixed in `playback-component.js`, rebuilt, re-verified):**
1. v1.8.3's `MasterBarTickLookup` has **no `tickDuration`** — my bar-end was `NaN`, silently disabling the past-the-end guard. Now uses the lookup's plain `end` property (runtime-probed).
2. At natural end, alphaTab **seeks back to tick 0 before the "stopped" state-change arrives**, so a trailing `1:1` slipped through — and left the dedupe latch holding `"1:1"`, which would have swallowed the first step of your next replay. Now `isSeek` reports are skipped and the latch resets on stop.

The design doc's tick-source bullet is updated to the resolved facts, and the done doc records all of it.

**Your console mystery** — the bus itself is fine (my harness counts through the very same `__cfEngine.on("position", …)` path). Two likely culprits: (1) if you had visited the **Content** page in that session, its preview player overwrites `window.__cfEngine`, so your subscription landed on the wrong engine — run the snippet fresh on the Practice page; (2) the console's *Filter* box / log level can hide `console.log` lines — check "Default levels" includes *Info*. Also remember `console.table` only prints after you press Stop.

**The pickup-bar gap you spotted** — the sheet not *showing* the `\ac` bar is a `ChordSheetBuilder` (C#) model gap, deliberately out of this thread (EX3: no C# change; the marker stays correctly silent during the pickup and aligns from bar 2, as test B proves numerically). It deserves its own thread — probably `chord-sheets` weave, "ChordSheet model renders the pickup bar as a lead-in cell". Want me to spin up the idea?

The app is still running (my test tex is loaded in it — hit Generate to get back to real content). Remaining housekeeping when you're happy: `docs done` for the thread.

## Rafa:

I already marked this thread docs done. I stopped the app.
I was impressed watching you debug the app! I saw you were using some browser debugging tools to execute javascript, awesome!

I read your findings
Create new weave `chord-sheets` + thread + idea for "ChordSheet model renders the pickup bar as a lead-in cell"
commit

## AI:

Thanks! That was CDP (Chrome DevTools Protocol) — the same wire your F12 devtools speak; the `CHORDFLOW_DEVTOOLS` facility plus a debugging port made the app fully scriptable from outside. A nice trick to keep for future playback hunts.

Done, both things:

**New thread** — the `chord-sheets` **weave already existed** (chord-sheets-maker, chord-sheets-playback), so I created just the thread inside it: `chord-sheets/sheet-pickup-bar` — *"ChordSheet model renders the pickup bar as a lead-in cell"* — with the idea drafted: problem (ChordSheetBuilder emits no cell for the `\ac` bar; found in this thread's validation), the lead-in-cell approach (model flag → ChordSheetR distinct drawing in both layouts → `cellSchedule` covering the pickup so both marker modes track it), scope In/Out (playback engine explicitly Out — it's already anacrusis-correct), and a validation walk.

**Commit** — going in now as the last action of this turn: the three JS files (`playback-component.js` with `PlaybackClock` + the `"position"` event, `app.js` fan-out, `chord-sheets.js` marker mode split), the architecture ref sync, this thread's full doc set (idea/design/req/plan/done/chat), and the new `sheet-pickup-bar` thread + idea.