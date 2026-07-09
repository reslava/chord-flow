---
type: chat
id: ch_01KX0S9NARS9RK2XG8QX8Q7KCD
title: tempo-control Chat 001
status: active
created: 2026-07-08
version: 1
tags: []
parent_id: null
requires_load: []
last_ai_block: 3
---
# tempo-control Chat 001

## Rafa:

read the idea, it has 2 features

1. we are going to implement `api.playbackSpeed`
2. the `The score's actual tempo` has been alread done

so if it is all clear, update the idea to keep only feature 1. and delete 2.

if it is all grounded write design + req

## AI:

Read the idea and grounded it against `score-render-component.js` + the `scorer-render-params` req/design. There's a wrinkle in the "2 features" split I want to settle **before** trimming the idea, because the trim depends on the answer.

**What actually shipped for tempo (scorer-render-params).** The Tempo (BPM) control already lives in ScoreR, is seeded from `Song.DefaultTempo` (else 80), and self-applies live — but it does so through `api.playbackSpeed`, **not** by re-emitting the written `\tempo`:

```js
setTempo(bpm) { if (bpm && baseTempo) api.playbackSpeed = bpm / baseTempo; }
```

`IN5` / `EX3` there were explicit: a tempo change is a local `playbackSpeed` adjustment, **no C# re-render**, and it does **not** move the on-staff `\tempo` marking.

**So the two features aren't as separable as the idea frames them:**

- Feature 2 — "the score's actual tempo" — is only "done" in the loose sense that *a BPM control self-applies*. The **written** `\tempo` (Option B's re-emit seam) was explicitly **excluded** (EX3). What's live today is a playbackSpeed multiplier wearing a BPM label; the staff marking never changes.
- Feature 1 — a practice-speed `%` slider on `api.playbackSpeed` — wants to write the **same single** `api.playbackSpeed` property the BPM control already writes. Two controls, one knob → they stomp each other.

So before I trim the idea I need one design call: **how should the BPM field and the new % slider coexist?**

1. (Recommended) **BPM → real written tempo (re-emit); % → playbackSpeed.** Wire BPM to the `onNeedsRerender` seam (Option B as originally intended, reversing EX3) so the staff `\tempo` is accurate, and let the new % slider own `playbackSpeed` for live slow-down. Clean separation — the true Option C. Cost: a BPM edit now re-renders (scroll/cursor reset), but that's rare.
2. **BPM stays playbackSpeed; % is just another view of it.** Keep today's mechanism; BPM and % are two faces of one multiplier. No re-emit, but the written `\tempo` stays pinned at the authored value forever and "BPM" is a misnomer.
3. **Drop the BPM field, keep only %.** Written `\tempo` = the authored seed; the sole live control is practice-speed %. Simplest, but you lose typing an absolute target BPM.

My read: **1** is the correct/durable one — it makes "BPM" mean the real tempo and gives % a clean, non-colliding job. But note the consequence: if we go with 1, feature 2 is **not** actually done (the re-emit was excluded), so the trim you asked for would be wrong — the idea should keep *both* the re-emit wiring **and** the % slider.

Which way do you want to go? Once you pick, I'll trim the idea accordingly and write design + req.

## Rafa:

Sorry, your explanation is a bit confusing for me.
What if I archive this thread? I think it is useless

## AI:

No worries — let me say it plainly.

The one real value here is: **slow the playback down to practice.** And that already works today. The Tempo (BPM) box in ScoreR is already live — type a lower number and it plays slower immediately, no Generate click. Under the hood that box already drives `api.playbackSpeed`, which is the exact thing "feature 1" was going to add.

So feature 1 would mostly just be re-skinning that same live control as a "50–120%" slider instead of a BPM number. Nice-to-have, not new capability. And "feature 2" (making the number on the staff change too) is a minor accuracy detail, not a practice feature.

**So yes — I agree, archive it.** The practice payoff is already shipped. If the "% slider" affordance or the accurate on-staff tempo ever actually bothers you while playing, we reopen it then, with a clear reason.

Want me to archive the thread? (It's reversible — `loom_restore` brings it back.)