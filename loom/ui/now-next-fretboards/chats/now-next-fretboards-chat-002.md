---
type: chat
id: ch_01KVTK54YBVQBYVYFCXJCK10V4
title: now-next-fretboards Chat
status: done
created: 2026-06-23
updated: 2026-06-23
version: 2
tags: []
parent_id: null
requires_load: []
last_ai_block: 14
---
# now-next-fretboards Chat

## Rafa:

I has beem testing app and actual scrolling is annoyinh to work with it.
`smooth` mode scroll continuously the score, even with the beat did not reach the end of the row yet, very annoying!

Let's do this:
1. Control to activate/deactivate scroll, control to show/hide Now and Next FretR
2. Try `OffScreen`:

```js
settings.player.scrollMode = alphaTab.ScrollMode.OffScreen;
settings.display.scrollOffsetY = N;
```

Being `N` = height of sticky top area

Agree?

## AI:

Agreed on the direction — both points are right. Two facts to ground it, then one fork I want you to settle before we plan.

**ScrollMode facts (this alphaTab build):** the enum is `Off=0, Continuous=1, OffScreen=2, Smooth=3`. We're currently on **`Smooth`** (`score-render-component.js:103`) — that *is* the "creep the score continuously" behaviour you're hitting; it rAF-nudges every frame regardless of whether the beat has reached the row end. **`OffScreen`** does exactly what you want: it only scrolls **when the cursor would leave the visible area**, then repositions so the active bar is back in view — a page-flip, not a creep. So your instinct is correct: `Smooth → OffScreen`.

Pair it with **`nativeBrowserSmoothScroll: true`** (we currently set it `!scroll`, i.e. *false* while playing). With OffScreen the only scrolls are the occasional jumps — letting the browser animate them makes the flip smooth instead of a hard snap. Best of both: no creep, but the jump isn't jarring.

**The fork — where does the score scroll, and what is `N`?** Your snippet (`scrollOffsetY = N = sticky-top height`) assumes the **window** scrolls and the Now/Next + transport sit `position: sticky` at the top, so `N` reserves room for them. But the code today does something different on purpose (`score-render-component.js:131-138`): it bounds the score's **own surface** to `maxHeight: 60vh` and sets `scrollElement = surface`, so the inner box scrolls and the Now/Next boards never leave the top — `scrollOffsetY` is just `-15` headroom. So:

- **Model A — inner bounded surface (what's there now).** Keep `scrollElement = surface` + `maxHeight`, just flip the mode to OffScreen. `scrollOffsetY` stays a small headroom value, *not* the sticky height. Minimal change; Now/Next stay pinned for free.
- **Model B — your snippet: window-scroll + sticky header.** Drop `scrollElement`/`maxHeight`, make the Now/Next + transport a `position: sticky` top bar, set `scrollOffsetY = N` = that bar's height. The score uses the full window height instead of a 60vh box. More layout work, but the score gets more vertical room.

I lean **Model A** — it already solves the "Now/Next scroll off the top" problem, so flipping to OffScreen is a ~2-line change and we're done. Model B is the right call only if 60vh feels too cramped and you want the score to use full height. Which one?

**The two controls are easy either way:**
1. *Scroll on/off* — there's already a `scroll` opt threaded through `buildSettings`; the toggle just sets it and calls `api.updateSettings()` (switching `scrollMode` to `Off`). No new plumbing.
2. *Show/hide Now/Next* — the boards live in one host container above the score; toggle is pure CSS (hide the container). No engine/schedule change.

Where should the two toggles live — in the transport bar next to play/tempo, or a small settings strip? My vote: transport bar, two small toggle buttons.

So: pick **Model A or B**, confirm transport-bar placement for the toggles, and I'll turn it into a plan.

## AI:

Locked: **Model A** (keep the bounded inner surface), **toggles in the transport bar**.

Concrete change set, as I see it:

**1. Scroll mode (the actual fix)** — `score-render-component.js`:
- `buildSettings`: `scroll ? ScrollMode.OffScreen : ScrollMode.Off` (was `Smooth`), and `nativeBrowserSmoothScroll: scroll` (was `!scroll`) so the page-flips animate instead of snapping.
- The `opts.scroll` block (`:131`) stays: `scrollElement = surface`, `maxHeight: 60vh`, `scrollOffsetY` headroom. Model A is untouched structurally.

**2. Scroll on/off toggle** — transport bar. Heads-up: this is *not* a plain render-`options` toggle like `Metronome`/`Chord names` (those flip `options.*` and re-render). Scroll is a **player setting**, so the toggle flips `scrollMode` between `OffScreen`/`Off` on `api.settings.player` and calls `api.updateSettings()` live — and toggling on/off also has to add/remove the `scrollElement` + `maxHeight` binding. So it needs its own little handler on `handle`, not the generic `toggle()`. Small but distinct.

**3. Now/Next show/hide toggle** — transport bar. Another heads-up: the score component **doesn't own** the Now/Next container — `app.js` mounts `ChordFlowNowNext` in a sibling container above the score. So the toggle button lives in the transport strip but its action has to reach out to that container. Cleanest: the component exposes an `onToggleNowNext(visible)` callback (like the existing `onBeat`/`onStateChange` opts), and `app.js` wires it to show/hide its container. Keeps the component from knowing about Now/Next specifically.

Net: `score-render-component.js` (mode flip + two transport toggles + one new callback opt) and `app.js` (wire the now/next-visibility callback to the container). No engine/schedule/C# changes at all — this is pure front-end.

**One question before I formalize:** the design is basically nailed down right here. Do you want the full Loom ceremony (fresh idea → design → req → plan in this thread), or — since it's a ~2-file front-end tweak with the design settled in this chat — a single compact **plan** citing these decisions? I lean the compact plan; say the word and I'll write it.

## Rafa:

a single compact plan is enough

## AI:

Compact plan written → **`now-next-fretboards-plan-002.md`** (Scroll Mode + Transport Toggles). Four tight steps, pure front-end:

1. **Scroll mode fix** — `Smooth → OffScreen` + `nativeBrowserSmoothScroll: scroll` in `buildSettings`. Kills the per-frame creep; Model A binding untouched.
2. **Scroll on/off toggle** — live `scrollMode` flip via `updateSettings()` + bind/unbind the `scrollElement`/`maxHeight`, exposed as `handle.setScroll(on)`, button in the transport strip.
3. **Now/Next show/hide toggle** — new `onToggleNowNext(visible)` callback opt; `app.js` shows/hides the container. Component stays generic.
4. **Verify on the running app** — creep gone, page-flips only, both toggles work. No architecture-ref change expected (UI/behavior only).

Files: `score-render-component.js` (1–3) and `app.js` (3). No engine/schedule/C#.

Ready to start the plan and do step 1 on your `go`.

## Rafa:

I started the plan
Do whole plan

## AI:

Steps 1–3 implemented, build green (JS syntax-checked + Desktop project compiles, only the pre-existing SQLite/WindowsBase warnings).

**What landed** (`score-render-component.js` + `app.js`):
- **Scroll mode** — `buildSettings` now uses `ScrollMode.OffScreen` (was `Smooth`) with `nativeBrowserSmoothScroll: scroll`. The per-frame creep is gone by construction; the surface only page-flips when the cursor would leave view, and the browser animates the flip.
- **`applyScroll(on)`** replaces the old inline binding block — single live manager for Model A: `on` binds the 60vh surface (`scrollElement` + `scrollOffsetY: -15`), `off` releases it (full score in normal flow). Runs once at construction from `opts.scroll`.
- **Auto-scroll toggle** in the transport (full profile) → `handle.setScroll(on)` → `applyScroll` + `updateSettings()` live.
- **Now/Next toggle** in the transport → new `onToggleNowNext(visible)` callback opt; `app.js` flips `#now-next-pane`'s `hidden`. Component never names Now/Next — stays generic. The toggle only renders when a consumer passes the callback.

**Step 4 — needs your eyes.** Verifying "creep gone, only animated page-flips, active bar lands below the boards, both toggles work live" is a watch-it-play check I can't self-serve. Run Practice, start playback, and confirm:
1. The score no longer creeps continuously — it holds, then flips a page when the cursor reaches the bottom of the visible window.
2. The active bar lands just below the Now/Next boards (not glued to the very top).
3. **Auto-scroll** toggle off → score stops following (manual scroll), on → follows again.
4. **Now/Next** toggle hides/shows the two boards.

Want me to launch it now, or will you run it? Once you confirm, I'll mark step 4 ✅ and close the plan.

## rafa:

Visual check done. Not perfect, but we can go ahead.

I noticed that some soungFonts can produce some breaks in the app, I noted the secure ones.

Curiosity, switch between scroll modes `Continuous` and `OffScreen` is just changing: `player.scrollMode`, correct?

If so, implement a control to switch between them and this way I can stress and test both and decide.

## AI:

Step 4 ✅ — plan-002 is now **done**. Thanks for the visual check.

**Your curiosity — almost.** Switching follow modes is *mostly* just `api.settings.player.scrollMode` + `api.updateSettings()`, and our Model A binding (`scrollElement` = surface, `maxHeight: 60vh`, `scrollOffsetY: -15`) is identical for both OffScreen and Continuous, so it stays put. The **one paired tweak** is `nativeBrowserSmoothScroll`:
- **OffScreen** → keep it **on** so the page-flip glides.
- **Continuous** → turn it **off**. Continuous repositions every beat; native smooth-scroll animates each of those tiny steps and rubber-bands/fights it. (That was the original "don't let the two fight" comment — it applies to Continuous.)

So: `scrollMode` is the switch, `nativeBrowserSmoothScroll` is a small feel-pairing. Both handled for you.

**Implemented the control** (you authorized it). I refactored `applyScroll(on)` → **`applyScrollMode(mode)`** with three modes — `off` / `offscreen` / `continuous` — and **replaced the Auto-scroll checkbox with a `Scroll` dropdown** in the transport (Off / OffScreen / Continuous). That one design call (a 3-way select subsumes the on/off toggle — cleaner than a checkbox + separate picker) gives you exactly the live A/B you wanted: play a piece, flip the dropdown, feel both. `handle.setScroll` became `handle.setScrollMode`. Build green, syntax-checked. Refresh Practice and the dropdown's there.

This rides on top of the now-closed plan-002 and lives in this chat as the record — say the word if you want it formalized as its own step/plan.

**Soundfont breakage** — noted, thanks for flagging the safe ones. That's a separate concern (some SF2s crash the synth/app); when you want, I can spin a small thread to either validate/guard soundfont loads or prune the picker to the known-good set. Not touching it now.

Anything feel off in either follow mode once you've stress-tested?

## Rafa:

If you want you can add a small new step in plan 002 and mark it done

I think we are ready and the app have enough features to finally move to dogfeed it with real songs, etc...

Propose a weave/thread for this important pending milestone

## AI:

**Plan-002 step** — couldn't cleanly do it: marking step 4 done auto-closed the plan to `done`, and there's no clean reopen (the `start_plan` transition only accepts draft/active, and forcing the status backward risks a duplicate done-doc). Rather than fight the state machine for a retroactive row, the scroll-mode-select is recorded here in the chat. If you want a plan-level artifact I'll drop a tiny **plan-003** for it — one step, born done. Your call; not blocking the milestone.

---

## Dogfooding milestone — proposed weave/thread

This is the direction we agreed on (2026-06-22, `meta/general-chat-006`): **stop adding engine substrate ahead of use; drive ChordFlow with real music and let it expose the gaps.** Concretely — one real **jazz blues** + one **jazz standard** end-to-end, multi-bar **lead melodies with pickups**, and the differentiator: **derived voicings** (shell 3–7, guide-tone lines, drop-2) *computed* from the interval kernel, drilled over those real progressions.

**Proposed weave: `dogfood`** — "Drive ChordFlow with real jazz; surface kernel/renderer gaps; build the derived-voicings differentiator on contact with real music."

Why a *dedicated* weave rather than scattering threads into `packages/` (content) and `guitar/` (voicings): the organizing principle here is the **milestone/goal**, and the threads are sequenced by *what the real music forces next*, not by architecture layer. The code still lands in its proper homes — definition bundles in `packages`, voicing producers in `guitar`/Core — the weave is just the workstream that tells the "we dogfooded" story.

**Initial thread (the only one I'd create now): `jazz-blues`**
- Author **one** real jazz blues end-to-end as an importable definition bundle — swing feel, ii–V turnaround, secondary dominants, multi-chord bars. Play it through Practice with the now/next boards. **Log every gap** it exposes (parser, rhythm grid, renderer, voicing).
- This *is* the dogfood: first real contact, and per our own rule, **it dictates the roadmap** — we don't pre-build the rest.

**Anticipated follow-on threads (named, NOT created yet — the blues decides if/when):**
- `jazz-standard` — one full 32-bar standard (e.g. Autumn Leaves); richer harmony, more gaps.
- `derived-voicings` — shell / guide-tone / drop-2 computed from the interval kernel, the daily-driver differentiator (leans on [[interval-derivation-engine-vision]]; hand-authored voicings are its golden oracle).
- `lead-melodies` — multi-bar melodies with pickups over the blues/standard (the lead-track side).

**The one call I want from you before I create anything:**
1. Weave name — **`dogfood`** (honest about intent) or **`repertoire`**/**`songbook`** (names what it holds)?
2. Dedicated weave (my recommendation) vs. distributing threads into existing `packages`/`guitar` weaves?
3. Start with **`jazz-blues`** as the single first thread, roadmap-by-contact — agree?

Give me the three and I'll create the weave + first thread and we kick off its idea doc.

## AI:

↑ Update: you marked the plan active, so I added **step 5 — "Scroll mode select (Off / OffScreen / Continuous)"**, marked it ✅, and the plan re-closed to `done`. No tiny plan-003 needed — plan-002 now reflects all five steps. Thanks for the reopen.

Milestone proposal above still stands — waiting on your three answers (weave name, dedicated vs distributed, start with `jazz-blues`).

## Rafa:

Dedicated weave,  Start with `jazz-blues`

I like `dogfood` (honest) and `songbook` (music related), you pick.

Draft an idea doc.

## AI:

Picked **`songbook`** — dogfooding is the *intent*, but `songbook` is the durable *place* real repertoire accretes (and it ages better than a phase-name). Created:

- **Weave** `loom/songbook/`
- **Thread + idea** → `loom/songbook/jazz-blues/jazz-blues-idea.md` (`id_01KVTQXQWTBNAH7GEKF74XF75Y`, status draft)

The idea's spine: author **one** real jazz blues as an importable bundle, play it through Practice, and the **real deliverable is a written gap/findings log** — what the real music breaks dictates the next thread (we don't pre-build jazz-standard / derived-voicings / lead-melodies; they're named as anticipated, not created). Derived voicings, when they come, likely home in `guitar` — songbook holds the *content*.

**To move it to design, I need your musical calls (the idea's open questions):**
1. **Changes** — basic jazz blues, or bird-blues (the `iii–VI–ii–V` / tritone-sub heavy variant)? I'd start **basic** and keep bird as a stretch.
2. **Key** — **Bb** or **F**? Both idiomatic; Bb is the most common jazz-blues default.
3. Gut check before we author: do you expect the **Progression DSL already expresses secondary dominants + the ii–V turnaround**, or is that likely our *first* logged gap? (Either answer is fine — it's the point of the exercise — but it tells me whether step 1 is "author" or "author *and* extend the DSL".)

Give me 1–2 (and a hunch on 3) and I'll draft the design.