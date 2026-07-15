---
type: chat
id: ch_01KXKAWZY18XRHYXR4ER4X19NZ
title: metronome-countin-fix Chat 001
status: done
created: 2026-07-15
version: 1
tags: []
parent_id: null
requires_load: []
last_ai_block: 23
---
# metronome-countin-fix Chat 001

## Rafa:

read idea
when metronome & count-in was implemented worked OK
but now they are not working
we need to find the cause of this regression
they are not working in ScoreR, ChordSheetR neither

## AI:

Read the idea, then hunted the cause through the code, git history, the alphaTab bundle, and the shipped soundfonts. Headline: **this is not a code regression — nothing in git that governs the metronome ever changed.** Below is what I ruled out and where that leaves us.

## The metronome mechanism (decoded from `alphaTab.min.js` v1.8.3)

The click is not a synth beep — it's a **percussion note on a drum channel**:

1. **Events** — at MIDI-sequencer load (`createStateFromFile`) alphaTab injects `AlphaTabMetronome` events on every time-signature boundary. This is **unconditional** — every score gets metronome events regardless of any enable flag. Count-in works the same way on a separate one-time MIDI.
2. **Volume gate** — when the synth dequeues an event, it plays the click only if `event.isMetronome && metronomeVolume > 0`:
   ```
   channelNoteOn(metronomeChannel, MetronomeKey, 95/127)
   ```
3. **The channel** — `metronomeVolume = 1` runs `setupMetronomeChannel(ch, 1)` → `channelSetPresetNumber(ch, 0, /*isPercussion*/true)`. So the click needs the **drum-kit preset (bank 128, preset 0)** to exist in the *currently loaded* soundfont. This is why it must be re-asserted after `soundFontLoaded` — set before the font loads, the preset lookup finds nothing.
4. **The note** — `MetronomeKey = 33`. Note 33 is the **GS "Metronome Click"**, *outside* the standard GM percussion range (35–81). So the loaded drum kit must actually map key 33.

## What I ruled OUT (all four static suspects)

- **JS wiring** — `api.metronomeVolume = on ? 1 : 0` is **byte-identical back to its first commit** (`0dcc60e`). Never changed.
- **The alphaTab bundle** — `alphaTab.min.js` has **exactly one commit ever** (`21db5ea`) and is v1.8.3. Never upgraded, never touched.
- **The default soundfont** — always `sonivox.sf2`, from day one. Never switched.
- **The soundfont itself** — I parsed `sonivox.sf2`'s SF2 chunks: it **has** the Standard drum kit (bank 128, preset 0) **and** its drum kit **does map key 33**. So even the "small font has no metronome sample" theory is dead for sonivox. `GeneralUser-GS` and `FluidR3Mono` have it too.
- **Player settings** (`useWorkers`, `enablePlayer`, …) — same block since `0dcc60e`.

So a git bisect finds **no breaking commit**. The mechanism is fully intact for the shipped default.

## Where that leaves us

Because every tracked input is unchanged and correct, the cause has to be **runtime state git can't show**, and it needs live inspection. Two hypotheses, ranked:

1. **The effective (persisted) soundfont at play time** — the `soundfont-library` thread added a host-persisted font choice (SQLite/app-data). That's the *one* thing that changed between "worked" and "now" and that isn't in git. If your stored choice is a font whose drum kit doesn't map key 33 (or fails to load), the click is silent — while play/cursor/notes still work. I can't read your local DB, so I can't see which font is active.
2. **A worker-synth ordering quirk in 1.8.3** — `metronomeVolume` posts a `setMetronomeVolume` command to the worker synth; if the metronome channel's preset ends up assigned before the soundfont is live and isn't successfully re-applied for this bundle's ordering, both metronome and count-in go silent together (which matches your "both fail, in both ScoreR and ChordSheetR").

The idea's **step 1 (enable WebView devtools)** is exactly right — but now it's a *targeted* check, not a blind one. Once devtools is on, at play time I'd verify: (a) `api.metronomeVolume` is really `1`; (b) which soundfont is actually loaded; (c) whether forcing `sonivox.sf2` makes the click return.

**Two questions before I go live:**
- Which soundfont do you currently have selected? Does the metronome fail specifically **with `sonivox.sf2` selected**, or only with a larger font?
- Want me to proceed with enabling `CoreWebView2.Settings.AreDevToolsEnabled = true` (reversible) so we can inspect points (a)–(c) directly? That's the fastest path to the real cause now that the static suspects are all eliminated.

## Rafa:

- the bug does not depend on soundfont
- enable it and tell me what to inspect

flag it to diable it when error is fixed

## AI:

Good — soundfont-independent kills hypothesis 1, so we're on the synth/wiring path. Devtools is enabled, plus I exposed a debug handle so the console can actually reach the api (it's otherwise private inside the `ChordFlowPlayback` closure). C# build is green (0 errors).

## What I changed (both flagged `TEMP-DEBUG (playback/metronome-countin-fix)` — grep that to find/remove them when fixed)

- `Program.cs` — `core.Settings.AreDevToolsEnabled = true;` → open devtools with **F12** (or right-click → Inspect).
- `score-render-component.js` — after a score loads in the Practice view, `window.__cfApi` (the alphaTab api) and `window.__cfEngine` (the ChordFlowPlayback engine) are exposed.

To pick up the JS change, rebuild+run the app (F5 / `dotnet run`) and open the **Practice** page so a score renders.

## What to inspect — do these in order

The whole point is one discriminator: **does the click depend on our toggle→api wiring, or on alphaTab's synth itself?** Steps 3 and 4 answer that.

**1. Confirm the handle is live** (after a score renders):
```js
__cfApi                     // should be the AlphaTabApi object, not undefined
__cfApi.isReadyForPlayback  // must be true — false means soundfont/synth not ready yet
```

**2. Toggle Metronome ON in the UI, then read what our wiring actually set:**
```js
__cfApi.metronomeVolume   // expect 1. If it's 0, the toggle isn't reaching the api (app bug, upstream of alphaTab)
__cfApi.countInVolume     // toggle Count-in on too, expect 1
```

**3. Bypass the UI entirely — force it straight on the api, then press Play:**
```js
__cfApi.metronomeVolume = 1;
__cfApi.countInVolume = 1;
__cfApi.playPause();
```
- **Click returns** → the bug is in our toggle→engine→api path or its *timing* (e.g. value set while `isReadyForPlayback` was still false and never re-applied). We fix it in `ChordFlowPlayback`.
- **Still silent** → the value is reaching alphaTab but the synth isn't sounding the metronome channel. That's inside alphaTab 1.8.3 (preset assignment / channel ordering) and we go a level deeper.

**4. Turn up alphaTab's own logging to catch a "preset not found" / soundfont complaint at the moment you toggle+play:**
```js
__cfApi.settings.core.logLevel = alphaTab.LogLevel.Debug;
__cfApi.updateSettings();
```
Then Play with the metronome on and copy whatever the console prints (especially anything mentioning soundfont, preset, channel, or metronome).

**5. Sanity on the synth output:**
```js
__cfApi.masterVolume      // not 0
__cfApi.playbackSpeed      // sane (≈1)
```

Paste back: the values from steps 1–2, the **result of step 3** (click or silence — this is the key one), and any step-4 console output. That tells me exactly which side of the boundary the bug is on and I'll target the fix there.

## Rafa: 

What I check so far:
1. Ok
2. both 0
3. i set __cfApi.metronomeVolume = 1; __cfApi.countInVolume = 1; both sound correct when play

(Maybe this regression occurs when we moved that UI toggles check boxes from page to ScoreR)

## AI:

Found it — and your instinct was exactly right.

## Root cause

`ScoreR.setOption()` does this on every toggle change:
```js
setOption(name, value) {
  options[name] = value;
  syncToggle(name, value);        // <-- line 281
  if (PLAYER_KIND.has(name)) {
    if (name === "metronome") engine.setMetronome(value);   // <-- line 283, never reached
    ...
```
**`syncToggle` was called but had no definition** — so every checkbox change threw `ReferenceError: syncToggle is not defined` at line 281, *before* the `engine.setMetronome(value)` call at line 283. The exception is swallowed by the DOM change-event dispatcher, so the checkbox flips visually but the engine is never told → `metronomeVolume` stays 0. Setting the value directly on the api works because it bypasses `setOption` entirely. That's a perfect match for every symptom (both toggles dead, both pages affected — ChordSheetR shares the same ScoreR-derived toggle path, direct api works).

## The exact breaking commit — your hypothesis confirmed

Git history pins it precisely:
- `ab390cc` — `syncToggle` was **defined** (a 4-line helper that mirrors a programmatic option change back into its checkbox, for the diagrams→chord-names coupling) and called.
- **`aadd147`** — *"feat(chord-sheets): playback marker over ChordSheetR + extract ChordFlowPlayback"* — the extraction refactor **deleted the `function syncToggle` definition but kept both call sites.** The diff literally shows `- function syncToggle(name, value) {` with no replacement.

That's the "moved the toggles into ScoreR" refactor you suspected — collateral damage from the ChordFlowPlayback extraction, exactly as you guessed.

## The fix

Restored the original definition verbatim (right after `reflectPlayState`):
```js
// Keep a toggle checkbox in sync when its option is set programmatically (e.g. the on-top coupling).
function syncToggle(name, value) {
  const toggle = ui.toggles[name];
  if (toggle && toggle.checked !== !!value) toggle.checked = !!value;
}
```
With the throw gone, `setOption` now falls through to `engine.setMetronome(value)` / `engine.setCountIn(value)`. JS parses clean, C# build green.

Note this also silently broke the **diagrams-on-top → auto-enable chord-names** coupling (line 296-297 also calls `syncToggle`) — that's fixed by the same restore.

## Please confirm live

Rebuild + run, open Practice, and toggle **Metronome** / **Count-in** in the UI (not the console this time) → you should hear the click / count-in on Play. `__cfApi.metronomeVolume` should now read `1` after toggling.

Once you confirm, I'll strip the two `TEMP-DEBUG (playback/metronome-countin-fix)` lines (devtools flag + `window.__cf*` handles) and record the fix. Want me to also add a tiny guard so a future missing helper can't silently swallow a toggle again (wrap the toggle handler so it logs instead of dying)? Optional — say the word.

## Rafa:

metronome & count-in working in Practice page, ScoreR
but now metronome, count-in, now-next FretR chords togles are not showing in Chord Sheets page, ChordSheetR

we must ensure these components behaviours are correct:
ScoreR and ChordSheetR renders and both use `ChordFlowPlayback` for time events
also both renders component can integrate all related controls
correct?

side issue: in Chord Sheets page, `Show tab` shows tabs over a dark background, not possible to read at all

note: if you consider useful for future bug fixes, we could live the debug lines, but deactivated

## AI:

Did the two isolated fixes; the toggle-parity one is a genuine design decision, so I'm stopping there for your call.

## Side issue — dark "Show tab" background: fixed

Root cause: ScoreR's alphaTab surface carries `class="cf-score-surface"` → `background:#fff;color:#000` (`index.html:103`), so notation is black-on-white. The chord-sheets page created its surface with **no class** (`chord-sheets.js:252`), so alphaTab's dark notation rendered on the dark page → unreadable. Fix = one line, reusing the same CSS:
```js
surface.className = "cf-score-surface";
```

## Debug lines — kept, but deactivated (your suggestion)

Converted the two `TEMP-DEBUG` lines into a **default-off** facility, activated by the `CHORDFLOW_DEVTOOLS` env var (no rebuild to flip):
- `Program.cs` — `AreDevToolsEnabled` now follows the env var, and when on it injects `window.__cfDebug = true`.
- `score-render-component.js` — `if (player && window.__cfDebug) { window.__cfApi = api; … }`.

So a normal run is inert; `set CHORDFLOW_DEVTOOLS=1` before launch re-arms F12 + `window.__cfApi`/`__cfEngine`. C# compiled clean (the only build errors were a file lock — your app is still running; close it for a fresh exe).

## The architecture question — the honest answer

Your model isn't quite what's built today. The facts:

- **Time events:** both *pages* use `ChordFlowPlayback` ✓ — but each owns its **own** engine instance (ScoreR internally; chord-sheets.js in `setupEngine`). There's no shared transport — that was a deliberate "option a, no cross-page transport" call in the chord-sheets-playback thread.
- **Controls:** the two render components are **not symmetric**:
  - **ScoreR** = a *fat player-widget*: owns the engine **and** a full control strip (transport, tempo, soundfont, **metronome, count-in**, now-next, staff-profile, display toggles).
  - **ChordSheetR** = a *pure SVG drawer* (by design — C1, no theory, one SVG for screen+export parity): **no engine, no controls**. It only draws + `highlight`/`highlightBeat`. The chord-sheets **page** owns the engine and hand-rolls a **partial** transport (play/stop/tempo/sound/marker/Show-tab).

So "metronome / count-in / now-next not showing on Chord Sheets" is **not a regression** — those controls were simply never added to the chord-sheets transport. The engine supports `setMetronome`/`setCountIn` fully; the page just never exposes them, and it never instantiates the now-next boards (`ChordFlowNowNext`).

That means "both components integrate all related controls" is a **goal, not the current state** — and getting there is a design choice:

**Option A — extract a shared player-controls strip (recommended).** Pull the *player-kind* controls (play/stop/tempo/soundfont/**metronome**/**count-in**/now-next) out of ScoreR into a small module that binds to a `ChordFlowPlayback` handle. Both ScoreR and the chord-sheets page mount it. ChordSheetR stays a pure drawer. One place to add a control → no drift (this is exactly the class of bug the `syncToggle` drop was). Display-only toggles (chord-names/diagrams/auto-layout/staff-profile) stay ScoreR-specific; marker-mode stays chord-sheets-specific. *Cost: a real refactor of ScoreR's `buildControls`.*

**Option B — just add the missing toggles to `chord-sheets.js` `buildTransport`.** Fast unblock: three checkboxes wired to `engine.setMetronome/setCountIn` + a now-next board. *Cost: duplicates the wiring in two places — the drift risk that just bit us.*

**Option C — make ChordSheetR own the engine + controls like ScoreR.** Rejected: it breaks ChordSheetR's "dumb drawer / export-parity" purpose.

My recommendation is **A** (matches durable-over-minimal and kills the drift class), with **B** as a stopgap only if you want metronome/count-in on chord sheets *today* and defer the extraction.

**Which way do you want to go — A, or B-now-A-later?** Once you pick, I'll plan it out. (I haven't touched the controls yet.)

## Rafa:

### Debug lines — kept, but deactivated 

I suggest this, but do you agree?
If so take note if it in `loom/ctx.md` to use it when needed it, agree?

### The architecture question 

I thought my model was your model also.
Of course, option A, always the durable, scalable, reusable approach

ScoreR (ScoreControls + Score)--------------------| 
                                                  |----> PlayerControlsR <-> `ChordFlowPlayback` 
ChordSheetR (ChordSheetControls + ChordSheet) ----| 

correct?

I think we need to spin up a thread + idea for this architecture refactor, agree?
If so, do sping up, explain well the details in the idea

## AI:

Agreed on all three. Done:

### Debug lines — agreed + noted in `loom/ctx.md`

Yes, I agree — a default-off, env-activatable facility is the right call. Added a bullet to `ctx.md` §4 so every future session knows to reach for it instead of re-adding temp lines:

> **Live WebView debugging (`CHORDFLOW_DEVTOOLS`):** set the env var before launching to enable WebView2 devtools (F12) + expose `window.__cfApi` / `window.__cfEngine` for live playback/synth inspection. Inert in normal runs; wired in `Program.cs` + `score-render-component.js`.

### Architecture — Option A, your diagram is correct

```
ScoreR      = ScoreControls      + Score       ─┐
                                                 ├─→ PlayerControlsR ⇄ ChordFlowPlayback
ChordSheetR = ChordSheetControls + ChordSheet  ─┘
```

Two refinements to keep in mind (both captured in the idea):
- **PlayerControlsR binds to a `ChordFlowPlayback` *handle* — it doesn't own the engine.** Each page keeps owning its own engine instance, so the deliberate "no cross-page transport" decision stays intact. The `⇄` is a handle, not ownership.
- **now-next** is slightly special: the current/next fretboards are a separate component (`ChordFlowNowNext`) each page mounts; PlayerControlsR just exposes the *toggle* that shows/hides them (Practice already has this; Chord Sheets would newly mount the boards).

### Thread + idea — spun up

- Thread: `loom/ui/player-controls-component/` (`th_01KXKSW2R4NMTVRFZKSM8W2D67`, priority 55) — placed in the **ui** weave alongside the other render-component threads.
- Idea: `id_01KXKSWXMZG7Y6K5S25042XVZZ` — "Shared PlayerControlsR — extract the player transport from ScoreR so both render surfaces mount it." It details the What / Why / boundaries (what moves to PlayerControlsR vs. what stays in ScoreControls/ChordSheetControls) / invariants (handle-not-owner, ChordSheetR stays a pure drawer, log-not-die toggle handlers) / validation, plus the required `chordflow-architecture-reference.md` sync when it lands, and links back to this chat as the origin.

This thread — `metronome-countin-fix` — is now effectively **done**: the regression is fixed (`syncToggle` restored), the dark-tab bug fixed, and the debug facility kept-but-deactivated. The controls-parity work carries forward in the new `player-controls-component` thread. Want me to `read ui/player-controls-component/idea` so we can refine the idea → design there, or leave it parked for now? And shall I commit the fixes from this thread (syncToggle restore, dark-tab class, debug gate)?

## Rafa:

lets close this thread
do quick
commit

then 
read `loom/ui/player-controls-component/chats/chat-001.md`

## AI:

Closing this thread.

- **do quick** → recorded plan `pl_01KXKTH18XHXZV1X39353WAH7W` (4 done steps: root-cause + bisect, `syncToggle` restore, chord-sheet dark-tab fix, `CHORDFLOW_DEVTOOLS` facility).
- Marking idea + this chat **done**.
- Committing the metronome-thread work (code fixes + the done record + ctx note + the follow-up thread/idea) as the last action.

Control-parity work continues in `ui/player-controls-component`. See you there.