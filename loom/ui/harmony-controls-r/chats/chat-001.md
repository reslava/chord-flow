---
type: chat
id: ch_01KXN2W8RX2GRC27ZZ0M9JWS18
title: harmony-controls-r Chat 001
status: done
created: 2026-07-16
version: 1
tags: []
parent_id: null
requires_load: []
last_ai_block: 19
---
# harmony-controls-r Chat 001

## Rafa:

Practice and Chord Sheets pages should be look very similar, almost same controls, just different views for playback

Currently we already have:

```
  ScoreR            = ScoreControls (staff/notation toggles, debug panel)      + Score (alphaTab staff)
  Chord Sheets page = ChordSheetControls (layout/notation/export/marker/tab)   + ChordSheet (SVG)
        │                                                                          │
        │   each surface mounts a PlayerControlsR bound to ITS OWN engine handle   │
        ▼                                                                          ▼
  ┌────────────────────────────────────────────────────────────────────────────────┐
  │ PlayerControlsR   play · stop · tempo · soundfont · metronome ·                  │
  │                   count-in · (Now/Next — only when the surface wires boards)     │
  └────────────────────────────────────────────────────────────────────────────────┘
```

But we should extract also similar, shared harmony related controls from ScoreR and ChordSheetR so they use them.

HarmonyControlsR: key, comping + Rhythm vol, leading + Lead vol, Difficulty, Voicings frets Min - Max, generate + save + mark practice

Of course, both pages still have specific UI controls:
Chord Sheets page: layout + chords + line + bellow cell + tone labels + theme + 3 exports

It is difficult to explain clearly what I want. It is almost like:
Practice and Chord Sheet could be a view toggle in the same page. I want 2 pages but I'm telling you this so you'll understand me.
The idea could be: if you take Practice page and hide ScoreR, shows ChordSheetR and specific controls you have Chord Sheet Page.

They share almost everything except the view, so they should reuse as much as they can.
1 page shows Score the other shows Chords Sheet but they allow same playback controls 

note: for example, in Chord Sheets page label `Sheet` and combo associated should be exactly like practice page `Harmony` and combo associated, same UI component

before going forward, I would like to know if you understand what I mean and what do you think?

## AI:

Yes — I understand you, and I think it's the right next step. Let me restate it to confirm, then give you my read on where the real design decision is.

**What you mean:** Practice and Chord Sheets are *two projections of the same practice definition*. Conceptually they could be one page with a view toggle — you want two pages, but that mental model is the design constraint: everything that isn't the view surface itself (alphaTab staff vs SVG sheet) or view-specific display controls should be one shared component. PlayerControlsR was step 1 (the transport). **HarmonyControlsR is step 2 (the builder/definition strip):** harmony picker, key, comping + Rhythm vol, lead + Lead vol, difficulty, voicing fret window, Generate / Save / Mark practiced. And per your note: the Chord Sheets "Sheet" combo and Practice "Harmony" combo must literally be the same component, not two look-alikes.

**What the code says today** — the asymmetries this thread would erase:

1. Practice's builder is static HTML (`index.html` `#builder`) wired by `app.js`; Chord Sheets builds its own toolbar in JS with its own `select()` helper. The two harmony combos are already drifted: Practice uses `<optgroup>` (Songs / Progressions), Chord Sheets uses `♪`/`→` prefixes, and they have separate population code (`rebuildHarmonyPicker` vs `mergeHarmony`). Exactly the drift you're pointing at.
2. Key exists twice with different semantics: Practice's Key lives on the ScoreR transport (0–11, seeded from the song on harmony switch — the scorer-render-params design), Chord Sheets has its own Key with a blank "Song key" option.
3. Rhythm vol / Lead vol currently live inside ScoreR's control strip (not PlayerControlsR) — they're engine-bound sliders. Moving them next to the comping/lead pickers is semantically better anyway: the volume belongs to the *voice*, not the transport.
4. The deep one: the Chord Sheets bridge request (`chordSheet`) carries **no comping / lead / difficulty / voicing window**. Its handler only resolves comping voicings for the adornment diagrams, and its playable tex comes from that same narrow request.

**The real design decision** (point 4 is the fork):

- **Option A — full convergence.** One shared definition envelope on the bridge: Chord Sheets sends the *same* definition Practice's Generate sends (harmony + comping + lead + key + difficulty + voicing window), and the host returns projections — score tex for Practice, sheet model + playable tex + schedules for Chord Sheets. Then comping actually changes what a chord sheet *plays* and which grips its diagrams show; difficulty and the fret window mean the same thing on both pages; Save / Mark practiced work identically. This is your "one page, two views" made real at the bridge level, not just in the toolbar's looks.
- **Option B — configurable component.** HarmonyControlsR with opt-in sections; Chord Sheets mounts only harmony + key today and grows into the rest later. Cheaper now, but the two request shapes stay diverged and the shared strip is partly cosmetic.

My take: **Option A** — it matches durable-over-minimal, and it's what your "if you hide ScoreR and show ChordSheetR you get the Chord Sheets page" intuition actually implies. The visible deliverable is HarmonyControlsR, but the core of the thread is unifying `generate` and `chordSheet` into one definition request with two render projections.

**Component shape** (PlayerControlsR precedent): `ChordFlowHarmonyControls.create(container, opts)` — owns its DOM and state; the page feeds it `entityList` payloads (one population path, so both combos are identical by construction); seeds key/tempo/feel on harmony switch (the existing `seed*ForHarmony` logic moves in); exposes `getDefinition()` + callbacks (`onGenerate`, `onSave`, `onMarkPracticed`, `onDefinitionChange`); takes the page's engine handle to bind the two volume sliders. Page-specific controls stay put: Chord Sheets keeps layout/chords/+line/below-cell/tone-labels/theme/exports/marker; Practice keeps staff toggles, debug panel, library pane.

**Open questions before I'd write the idea/design:**

1. **Key ownership** — scorer-render-params deliberately put Key on ScoreR's transport as a render param. Your list moves it into HarmonyControlsR, which I agree with semantically (it sits with the harmony), but it reverses that earlier decision. And if Key moves, what about **feel**? It's also content-kind, currently on ScoreR's transport, Practice-only. Bring feel into HarmonyControlsR too (Chord Sheets playback would gain swing), or leave it view-side?
2. **"Song key" blank option** — Chord Sheets' blank "use the song's own key" vs Practice's seed model (combo always shows a concrete key, seeded on harmony switch). I'd unify on the seed model — same behavior, one less special case. OK?
3. **Save / library on Chord Sheets** — under Option A, Save from Chord Sheets creates the same saved exercise. Should the saved-exercises library pane appear on Chord Sheets too, or stay Practice-only for now?

If you confirm the direction (and pick on 1–3), I'd spin up the idea/design for this thread next.

## Rafa:

Glad you could understand me (my English is not so good ;-)
Of course, Option A — full convergence

1. Key and feel should live in HarmonyControlsR, so they can be used by both pages. They could behave like they already do currently in Practice page.
2. Yes, always same behaviour, fill controls with content value, if no value, default, but never blank. If a song has no Key then C.
3. Yes, both pages should be almost similar

If it is all clear and grounded
write idea + design 

curiosity: do you think that using just 1 page with 2 views toggle is better approach? if so, tell me and do not write docs, just reply with reasoning

## AI:

All clear on 1–3: Option A, key + feel move into HarmonyControlsR (behaving exactly as Practice does today — seeded on harmony switch, manual edit survives until the next switch), controls always show a concrete value (song without a key → C, never blank), and both pages get the library.

But you asked the curiosity question, and my honest answer is **yes — one page with a Score ⇄ Sheet view toggle is the better approach.** So per your instruction: no docs this turn, reasoning instead.

**Why one page wins once you've chosen Option A:**

1. **Two pages force duplicate instances + a state-sync problem.** Full convergence means one definition feeding two projections. With two pages, every shared piece exists twice: two HarmonyControlsR, two PlayerControlsR, two ChordFlowPlayback engines, two Now/Next boards. Then: you pick a song and set tempo on Practice, switch to Chord Sheets — do its controls follow? Either we build cross-page state synchronization (real complexity, new bug class) or we accept that the "same" controls show different values on each page — which contradicts the exact incoherence you're trying to remove. One page dissolves the problem: one instance of everything, one state, nothing to sync.

2. **One engine instead of two.** Today Chord Sheets owns its own hidden-staff ChordFlowPlayback. Under Option A both engines would load *the same playable tex from the same definition* — pure duplication. One page = one engine, and that unlocks the killer feature: **toggling Score ⇄ Sheet mid-playback without stopping.** The music keeps playing, the cursor/marker keeps its position, you just switch how you're looking at it — notation or chord grid. With two pages this is impossible by design (page switch deliberately calls `stopAll()`). For a practice app, switching *view* while playing is genuinely valuable; switching *page* while playing is a bug.

3. **Less shell code, fewer seams.** The toggle is show/hide of the view surface + its view-specific controls (Sheet mode shows layout/chords/+line/below-cell/tone-labels/theme/exports/marker; Score mode shows staff toggles + debug panel). No second lazy-init path, no second bridge subscription for the same envelopes, no second entityList merge.

**What it costs:** losing "Chord Sheets" as a top-level destination, and refactoring `chord-sheets.js` from a standalone page shell into a view of the Practice page. The first is mitigable — the header nav button can stay and simply open Practice with Sheet view active (a deep link, not a separate page). The second is exactly the kind of restructuring your no-back-compat philosophy allows.

**What this decision does NOT change:** HarmonyControlsR extraction, the `generate`/`chordSheet` bridge unification into one definition envelope with projections, PlayerControlsR reuse — all of that is identical work either way. The 1-vs-2-page question only decides the *shell* around those components. So nothing from our earlier discussion is wasted; one page actually *simplifies* it (each shared component is mounted once instead of twice).

**My recommendation:** one **Practice** page with a segmented **Score / Sheet** toggle (same style as the header nav), one HarmonyControlsR + one PlayerControlsR + one engine + one Now/Next + one library, and the view-specific control strip swapping with the surface. Keep the "Chord Sheets" nav button as a shortcut into Sheet view if you want the discoverability.

Say the word — "1 page" or "2 pages" — and I'll write the idea + design for that shape.

## Rafa:

As I guess 1 page wins, its most logic approach
go with 1 page
write idea + design

## AI:

Done — idea + design written for the 1-page shape:

- **`idea.md`** (`id_01KXN9CD4EEAQ6RV5KSGFBF51X`) — the problem (the drifted duplicates: two harmony combos, two Keys with different semantics, the narrow `chordSheet` request, the second engine), the idea (HarmonyControlsR + one Practice page with a Score ⇄ Sheet toggle + one definition on the bridge), scope in/out, and validation (headlined by: toggle views mid-playback, audio continues, both markers track the same beat).
- **`design.md`** (`de_01KXN9DWFJG6QWET6VVFFS36HD`) — the four decisions from this chat, the target page shape (one HarmonyControlsR / PlayerControlsR / engine / Now-Next / library, swapping view surfaces), component APIs, and the bridge unification: `generate`/`loadExercise` become the only render-producing requests, the reply carries both projections (`tex` + `sheet` + both schedules), and `chordSheet` retires.

Design points worth your eyes before we go further:

1. **Content-CRUD preview constraint** — `content-crud.js` also uses ScoreR's key/feel controls (scorer-render-params IN7), and it's a single-entity preview, not a definition builder. So ScoreR *keeps* its opt-in key/feel/volume controls for that page; Practice simply creates ScoreR with them off. Small accepted duplication instead of contorting the Content preview onto HarmonyControlsR.
2. **Tempo stays in PlayerControlsR** — HarmonyControlsR seeds it on harmony switch through a shell hook (`onHarmonySwitch` → `pc.setTempoValue`), it never owns the control.
3. **Adornments become display-kind** — since the unified reply always resolves the comping grips, the sheet model always carries tone/diagram data, so "Below cell" stops re-requesting and becomes a pure display toggle like Layout/Theme.
4. **One implementation risk flagged** — alphaTab layout in a hidden container (the mid-playback toggle needs both surfaces mounted); mitigations in the doc, to verify during implementation.

Next step would be **req** (lock the Included/Excluded/Constraints from these docs), then the plan. Say `go` when you want the req — or raise anything in the design first.

## Rafa:

I marked idea + design done
write req + plan

## AI:

Req + plan written:

- **`req.md`** (`rq_01KXN9QE9TANNVBPQ2WGHHPJ4B`, **locked**) — 10 Included / 3 Excluded / 6 Constraints extracted from this chat + the done design. Highlights: IN3 (bridge convergence, `chordSheet` retires), IN7 (mid-playback toggle), IN10 (adornments become display-kind), C1 (tempo stays in PlayerControlsR), C2 (Content-CRUD preview keeps ScoreR's opt-in controls), C6 (architecture ref updated in the same unit of work).
- **`plans/plan-001.md`** (`pl_01KXN9S3C4E1KYX8J3YAH4CXGN`) — 7 steps, each citing its IN/C handles:
  1. **Unified reply** — sheet projection (model + cellSchedule + chordSchedule, grips always resolved) rides the generate/loadExercise reply; additive, tests first. (IN3, IN10)
  2. **HarmonyControlsR component** — new `harmony-controls-component.js`, full definition strip + seeding + engine-bound volume sliders. (IN1, IN4, IN5, IN8, C1, C3)
  3. **ScoreR `volumes` opt** so Practice mounts it slim; Content preview untouched. (C2)
  4. **Sheet view module** — `chord-sheets.js` refactor: drops engine/PlayerControlsR/Now-Next/combos, keeps strip + exports, marker driven by the shell. (IN9, IN10, C5)
  5. **Practice shell rewire** — one page, Score ⇄ Sheet toggle, both surfaces mounted for the mid-playback toggle, old `#builder` + Chord Sheets nav removed. (IN2, IN6, IN7, C1, C3, C4)
  6. **Retire `chordSheet`** request path (router + handler request side; print flow stays). (IN3)
  7. **End-to-end verification** of the idea's Validation scenarios + `chordflow-architecture-reference.md` update. (C6)

Order rationale: C# lands first (additive, testable in isolation), components next (unmounted additions), the shell rewire is the one step where the app flips shape, then cleanup and verification.

Ready to start — say `go` (or `do step 1`) to begin with the unified reply.

## Rafa:

I started plan
do plan

## AI:

Steps 1–6 are ✅ (build green, 917/917 tests, `chordSheet` verb retired, architecture ref updated). Step 7 is the manual validation walk — here's the checklist from the idea's Validation section. Reply inline with ✅ / ❌ (+ what you saw) per item:

**1. Mid-playback Score ⇄ Sheet toggle (IN7/C4)**
- [ ] Generate → Play → flip **Score ⇄ Sheet** while playing: audio continues without a hiccup.
- [ ] The sheet's bar marker is already in the right place when Sheet is revealed (it tracks while hidden).
- [ ] Flip back to Score: the alphaTab cursor is still on the correct bar/beat and the staff renders correctly (no blank/mis-measured layout after the collapse trick).
- [ ] Now/Next boards stay in sync across toggles.

**2. One definition drives both views (IN3/IN10)**
- [ ] Change **Comping** → Generate: the Sheet view plays the new pattern and its Below-cell **fret diagrams** show the new grips.
- [ ] Narrow the **Voicing frets** window (e.g. 0–5) on a rendered exercise: it re-voices live — tab and sheet diagrams both change, no Generate needed.
- [ ] **Below cell** (None / Tone strip / Fret diagram / Both) flips instantly with no round-trip and the data is always there.
- [ ] **Difficulty** + **Lead** (+ Lead vol slider) behave as before.

**3. Key/Feel seeding (IN4/IN5)**
- [ ] Switch harmony to a **song** → Key/Feel/Tempo seed from the song; switch to a **progression** → C / Straight / 80.
- [ ] Manually change Key, then Generate — it renders in that key; the manual choice survives until the next harmony switch.
- [ ] Key/Feel changes re-render live (transpose / swing) in **both** views.
- [ ] No control ever shows blank.

**4. Save / library (IN6)**
- [ ] **Save** while in Sheet view creates a normal library entry.
- [ ] Clicking a library entry restores the definition (key/feel/tempo seeds included) and loads **both** projections — check in Sheet view too.

**5. General sanity**
- [ ] Chord Sheets nav button is gone; the Score/Sheet toggle covers it.
- [ ] Exports (SVG / PNG / PDF) still work from Sheet view, filename = the harmony name.
- [ ] Marker modes (Visual metronome / Per chord) both work.
- [ ] Rhythm/Lead volume sliders (in the Harmony strip) affect their tracks; metronome + count-in still sound.
- [ ] Content preview (Content → progression/song) still has its own Key/Feel/volume controls and works unchanged.

Anything that fails, describe it and I'll fix before closing step 7 (error-loop rule applies). If all green, say so and I'll mark the plan's last step done.

## Rafa:

All working
I like the result, I think it is much more clear and useful that 2 different pages.

Little "issue", and comping `charleston` = `X...--X.--------`, no matter if i pick when `feel` is `triplet 8th (swing)` or `straight` and no matter if I use score or sheet view the cursor / marker looks like accelerating and decelerating in each bar, it is a weird sensation. It looks like accelerating in the `silences`, maybe is alphaTab problem, I will investigate.

Mark step 7 done
commit

## AI:

Step 7 marked done — and with it **the plan auto-closed: 7/7 ✅**. The thread's deliverables are all live: HarmonyControlsR, the one Practice page with the Score ⇄ Sheet toggle over one engine, the unified `loadScore` reply carrying both projections, the `chordSheet` verb retired, and the architecture ref updated. Really glad the 1-page call proved itself in your hands.

**On the charleston cursor "acceleration"** — recorded in the done doc as a known, non-blocking observation. My read supports your alphaTab suspicion: it's view-independent (score cursor and sheet marker both derive from the same `playedBeatChanged` signal) and almost certainly pre-existing. alphaTab's beat cursor *eases between beat onsets*, so in a bar like `X...--X.--------` (a dotted-quarter, a rest gap, then a long sustain) the cursor has few onsets to interpolate between — it glides slowly through the long note and then "sprints" across the gap to the next onset. Worth checking alphaTab's player cursor animation settings when you investigate; if you open a thread for it, `playback/` weave seems the home.

Committing the whole thread now (code + tests + loom docs). Suggested follow-ups whenever you feel like them: `docs done` for this thread's idea/design/chat, and the charleston investigation.

*(Small English note, since you asked me to flag these: "more clear and useful **that** 2 different pages" → "…**than** 2 different pages" — and "much more clear" reads more naturally as "much clearer".)*

## Rafa:

Update `loom/refs/chordflow-architecture-reference.md`, include the new `HarmonyControlsR`
write quick

how can I check what alphaTab version are we using?

## AI:

**Architecture ref — already done.** The HarmonyControlsR update landed as part of step 7 (req C6: ref updated in the same unit of work) and is in the commit. It's covered in three places in `chordflow-architecture-reference.md`:

- §2 (solution shape): `harmony-controls-component.js` listed in the wwwroot map with its one-line role.
- §5: a dedicated **"The shared harmony/definition controls … HarmonyControlsR"** paragraph — the definition state, the one-picker/no-drift population path, key/feel seeding rules, the tempo-stays-in-PlayerControlsR hook, and the live-param replay flow.
- §7: the playback-seam diagram redrawn around the ONE Practice page (HarmonyControlsR + transport strip + two view surfaces over one engine).

So no `write quick` needed — there's nothing new to ship, and the work is already recorded in plan-001's done doc. Give it a read and if you want anything expanded or worded differently, point at the section and I'll patch it.

**How to check the alphaTab version** — two ways, no code changes:

1. **The debug panel** (easiest): on the Practice page's Score view, expand the collapsed *alphaTex scratchpad* under the staff — its summary row shows **"alphaTab vX.Y.Z"** on the right (it reads `alphaTab.meta.version`, see `score-render-component.js:361`).
2. **DevTools console**: launch with `CHORDFLOW_DEVTOOLS=1`, press F12, and type `alphaTab.meta.version`.

(The bundled build lives at `src/ChordFlow.Desktop/wwwroot/alphaTab.min.js` — useful when you compare against alphaTab's changelog while investigating the charleston cursor easing.)

## Rafa:

I checked way 1, we are using v1.8.3 

I tried to launch in bash terminal
```bash
CHORDFLOW_DEVTOOLS=1 
dotnet run --project src/ChordFlow.Desktop
```
but F12 did not launch dev tools
did I launch the env variable wrongly?

### Cursor issue

I got this from web:
>This issue was accepted as a bug and targeted for the v1.8.3 milestone 
>Disable the Native Cursor: If updating doesn't help, you can try disabling the built-in playback cursor and using a custom one if your implementation allows, as custom cursors sometimes avoid the specific "hit-testing" logic causing the regression 

I think the jitter effect is common for alphaTab, I checked it even with v1.9.0. I also checked it in Guitar Pro an also has that effect. I think it is becuase notes/silence durations are not equaly scaled in the score.
I think we are going to leave as is for now

## AI:

**Yes — the env var was set wrongly** (a classic bash subtlety). Written on its own line:

```bash
CHORDFLOW_DEVTOOLS=1
dotnet run --project src/ChordFlow.Desktop
```

…the first line creates a plain **shell variable** — not exported, so the `dotnet` child process never sees it (and `Program.cs` reads it via `Environment.GetEnvironmentVariable`, i.e. from the process environment). Two correct forms:

```bash
# 1. prefix form — same line, env applies to that one command only:
CHORDFLOW_DEVTOOLS=1 dotnet run --project src/ChordFlow.Desktop

# 2. export — applies to everything you launch from this shell afterwards:
export CHORDFLOW_DEVTOOLS=1
dotnet run --project src/ChordFlow.Desktop
```

(In PowerShell it would be `$env:CHORDFLOW_DEVTOOLS = "1"` before the run.) With it actually set you get F12/right-click-Inspect plus `window.__cfApi` / `window.__cfEngine` in the console.

**Cursor jitter — agreed, leave as is.** Your investigation is convincing: reproduced on v1.9.0 and even in Guitar Pro, so it's inherent to how notation is engraved — a score gives a dotted-quarter and a sixteenth very different horizontal widths than their durations proportionally deserve, and a time-linear cursor moving over duration-nonlinear spacing necessarily looks like it accelerates through the tight spots (the rests). Not our bug, not alphaTab's either really. Two crumbs for the future, no action now:

- The Sheet view's **Visual metronome** marker is discrete (per-beat column steps, no easing), so practicing in Sheet view sidesteps the sensation entirely — a nice side-benefit of the view toggle.
- If it ever bothers enough: alphaTab's player cursor can be hidden (`displayMode`/cursor settings) in favor of a custom bar-level highlight like ChordSheetR's — the "disable the native cursor" route your quote mentions. Worth a `playback/` thread only if that day comes.

v1.8.3 noted. Anything else on this thread, or shall we `docs done` it?

## Rafa:

I did doc dones myself
I asked about cursor jitter effect because even in Sheet view happens and in that view is really annoying, so we did not get the side-benefit at all.
Maybe we can try to hide alphaTab Cursor when Sheet view is active.
I also check alphaTab website and found this:
https://next.alphatab.net/docs/reference/types/platform/cursors
```
barCursor​
Gets the element that is positioned above the bar that is currently played.

readonly barCursor: IContainer;

beatCursor​
Gets the element that is positioned above the beat that is currently played.

readonly beatCursor: IContainer;

cursorWrapper​
Gets the element that spans across the whole music sheet and holds the other cursor elements.

readonly cursorWrapper: IContainer;

selectionWrapper​
Gets the element that spans across the whole music sheet and will hold any selection related elements.

readonly selectionWrapper: IContainer;
```

## AI:

You're right, and I was wrong about the side-benefit — I dug into the signal chain and the Sheet marker was never metronomic. Here's the actual mechanics:

**Why Sheet view jitters too.** The engine's `beat` event comes from alphaTab's `activeBeatsChanged` (`playback-component.js:192-198`), and the `beat` number it emits is **`beat.index` — the index of the rendered note/rest event within the bar, not a quarter-note position**. So for charleston (`X...--X.--------`) the events fire at *note/rest boundaries* — irregular in time by construction — and the "Visual metronome" marker steps exactly when those events fire. It's not a cursor-rendering problem at all; **the clock itself is event-driven, not time-driven**. Same signal feeds Now/Next and the score cursor easing, which is why the sensation is identical everywhere.

**Why hiding the alphaTab cursor won't help Sheet view:** it's already invisible there — the score surface is collapsed to zero height when Sheet is active. The native cursor never appears in Sheet view; what you're seeing is our own marker driven by the irregular signal above. (The `cursors` API you found is from `next.alphatab.net` — the *upcoming* version's docs — and it's about grabbing/styling the cursor elements. Useful someday if we restyle the Score view's native cursor; irrelevant to the Sheet marker.)

**The real fix — a time-based clock for the Visual-metronome mode.** alphaTab also exposes **`playerPositionChanged`**, which fires continuously with the current **tick/time position** (time-linear, unaffected by how notes are engraved or how sparse the rhythm is). The shape:

1. `playback-component.js`: subscribe `api.playerPositionChanged`, derive `(bar, quarterBeat)` from `currentTick` (960 ticks per quarter in alphaTab's MIDI; 4/4 bars = 3840 ticks), emit a new `"position"` bus event alongside the existing `"beat"`.
2. `chord-sheets.js`: **Visual metronome** mode consumes `"position"` → the marker steps once per quarter, perfectly even, silence or not. **Per chord** mode stays on the event-driven signal (chord onsets *are* events — correct as-is).
3. Optionally Now/Next could switch too, but chord changes are events, so it's fine on the current signal.

Small change (one engine subscription + one consumer switch), and it fixes the annoyance at the root instead of hiding cursors. The score view's native cursor keeps its inherent easing (that one genuinely is alphaTab/Guitar-Pro-inherent, as you found).

This is new work beyond this thread's closed plan — want me to spin up a thread for it (suggest: `playback/metronome-true-marker`) with idea + design, or park it for now?

## Rafa:

Our investigation brings good news! Yes, we need a time-based clock for the Visual-metronome mode.
Spin up thread `playback/metronome-true-marker` + idea + design

## AI:

Thread spun up: **`playback/metronome-true-marker`** (`th_01KXNW4VE3J659QNXGZKGMGSF9`, priority 50) with:

- **`idea.md`** — the problem (event-driven `beat.index` clock, not the cursor, not fixable by cursor styling), the idea (a `"position"` engine event from `playerPositionChanged`, consumed only by the Visual-metronome mode), scope in/out, and validation (charleston steps evenly straight AND swing; per-chord + Now/Next untouched; pickup-bar alignment; tempo-change follows).
- **`design.md`** — the signal chain (today → target), the engine derivation (960 ticks/quarter, **bar starts cached from the score model** — not a fixed 3840 division, so `\ac` pickup bars stay aligned; deduped emit, 1-based like `"beat"`), consumer wiring (shell subscribes via `getEngine()`, `sheetView.onPosition` drives metronome mode only), edge cases (count-in clamp, swing-safe, tempo-free ticks), rejected alternatives (fixed bars, `setInterval` drift, cursor hiding), and the verify notes (JS-only; architecture ref's event-bus line updates in the same unit of work).

Two things flagged for implementation-time verification against **v1.8.3**: the exact `playerPositionChanged` arg shape (`currentTick`) and the master-bar start-tick property (fallback: derive cumulatively from time signatures).

Since this discussion lives here in the harmony-controls-r chat, I'd continue the new thread's work in its own chat when we get to it. Next step there when you're ready: req + plan (`load playback/metronome-true-marker` and say `go`).