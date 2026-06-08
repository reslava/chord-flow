---
type: chat
id: ch_01KTKD945E6G31Q2Q9S10GXF51
title: Phase 3 Persistence & UI
status: active
created: "2026-06-08T00:00:00.000Z"
updated: 2026-06-08
version: 1
tags: []
parent_id: null
requires_load: []
---
# Phase 3 Persistence & UI

## Rafa:

Do whole `loom/chordflow/mvp/plans/mvp-plan-003.md`
I think not need any update after migration Photino -> winforms


## AI:

## Plan-003 (Persistence & UI) — confirmed, no migration update needed, proceeding through all 4 steps

I read the plan against the post-migration codebase. **You're right — it needs no update.** Every file it touches is host-agnostic and already in the WinForms world:

- `Infrastructure/WebMessageRouter.cs` was explicitly *unchanged* across Photino→WebView2 (it only parses envelopes). The new save/list/load/markPracticed envelopes slot into the same router.
- `wwwroot/index.html` + `app.js` already speak `chrome.webview`; new UI controls just post more envelope types.
- Steps 1–3 (EF Core, ExerciseLibrary, Progress) are pure Domain + Infrastructure, untouched by the host swap.

### One design choice I'm making (flagging, not blocking)

Plan step 4 says *"each control posts an envelope that routes to its slice through the bridge"* — including **Play/Stop/Tempo**. Today those buttons call alphaTab **directly in JS** (local), while the C# `PracticeSessionHandler` slice (which exists and tracks `IsPlaying`/cursor) is idle. To honor the plan and give the C# `Progress`/future-accuracy slices real playback state, I'm routing the transport buttons **through** `PracticeSession`: button → `{type:"play"}` → `PracticeSession.Play()` → echoes `{type:"play"}` → JS `api.playPause()`. Single path, one `playPause` per press, no loop. If you'd rather keep transport JS-local, say so and I'll revert just that.

### What I'm building (all 4 steps, explicit multi-step authorization)

1. **EF Core + SQLite** — `ChordFlowDbContext` (`Exercises`, `PracticeRecords`), entities, initial migration, design-time factory. DB at `%LOCALAPPDATA%\ChordFlow\chordflow.db`. Stores **definitions only**, never alphaTex.
2. **ExerciseLibrary slice** — save / list / load (regenerating alphaTex via `AlphaTexRenderer` on load); wired through the router.
3. **Progress slice** — `markPracticed` writes a `PracticeRecord` for the active saved exercise. Event only, no scoring.
4. **UI** — key picker (12), rhythm picker (3), tempo, Generate, Play/Stop, Save, Mark-practiced, saved-exercise list — each posts an envelope.

Building now; I'll mark each step ✅ as it lands and report at the end.


## AI:

## ✅ Plan-003 implemented (all 4 steps) — app is launching, please verify live

Build clean (0 errors), **26/26 tests green**, `node --check app.js` clean. The app is starting now via `dotnet run`.

### What landed

**Step 1 — EF Core + SQLite** (`IN9`, `C2`)
- `ChordFlowDbContext` + `ExerciseEntity` / `PracticeRecordEntity`; `Difficulty` stored by name; cascade delete on records.
- Initial migration `InitialCreate` — tables match the plan exactly: `Exercises(Id, Key, ProgressionId, RhythmId, Tempo, Difficulty, CreatedUtc)`, `PracticeRecords(Id, ExerciseId, PracticedUtc)`.
- DB at `%LOCALAPPDATA%\ChordFlow\chordflow.db`; `Database.Migrate()` on startup. `Design` package is build-only (`PrivateAssets=all`). Added a design-time factory for the EF tooling. **Definitions only — alphaTex never stored.**

**Step 2 — ExerciseLibrary slice** (`IN9/IN10`, `C1/C3`)
- `Save` / `List` / `Load`; load reconstructs the `Exercise` from seed data and **regenerates alphaTex** via `AlphaTexRenderer`. Refactored a shared `LoadScoreEnvelope.From(exercise, renderer)` so generate + load build the score one way.
- Routed `save` / `listExercises` / `loadExercise` through `WebMessageRouter`. List labels are built in JS (it already has the key/rhythm name maps) — no naming duplicated in C#.

**Step 3 — Progress slice** (`IN10`, `C1/C3`)
- `markPracticed` writes one `PracticeRecord` (event + timestamp only — **no scoring**, EX1). Returns the running count for feedback.

**Step 4 — UI** (`IN10`)
- Builder bar (Key ×12, Rhythm ×3, Generate, Save, Mark practiced), transport bar (Play/Stop/Tempo), and a clickable **Saved exercises** list. Each control posts an envelope to its slice.

### Two choices I made (both flagged earlier / minor)
1. **Transport routed through `PracticeSession`** — Play/Stop/Tempo buttons post envelopes; the slice echoes the command back to alphaTab. Single path, no double-trigger. (C#'s `IsPlaying` can read stale on *pause* since pause isn't echoed back — cosmetic, unused.)
2. **Mark-practiced auto-saves an unsaved exercise first** — marking something practiced implies keeping it, so it persists then records. Avoids a "save first" dead-end.

### Please verify in the running app
1. **Boots** with the Bb · Beats 1 & 3 score, tablature + "ready · soundfont loaded"?
2. **Generate** — pick a different Key/Rhythm → Generate redraws the score?
3. **Play/Stop/Tempo** still work (audio + synced cursor)?
4. **Save** — the exercise appears in the Saved list (right panel)?
5. **Click a saved item** — it reloads (score redraws)?
6. **Mark practiced** — status shows `practiced ✓ — recorded N×` (and an unsaved one gets saved first)?

Tell me what you see. Nothing committed yet (working tree only, per your solo-dev/main workflow) — once you confirm, I can commit Phase 3.

## Rafa:

1. I changed Rhythm, clicked generate and updated the tablature
2. I saved it, appeared in `Saved exercises`
3. I marked it practiced (should add a mark ✅ also in Saved exercises)

then I tried changing key, generate button do nothing, I am able to save, but tablature render is wrong cause did not generate it
load saved exercise do not render 

## AI:

## Root cause found — the engine only supports **Bb** today

Great testing. Two real bugs + one feature request, and they share one root cause.

### Why generate/load "do nothing" off Bb

`VoicingBook` authors exactly **three** voicings — `Bb7`, `Eb7`, `F7` — the I/IV/V of the **Bb** 12-bar blues. For any other key, `Transposer` realizes a chord with no authored voicing, so `VoicingBook.Lookup` **throws** `KeyNotFoundException`. That throw happens inside `AlphaTexRenderer.Render` → `LoadScoreEnvelope.From` → the WebView2 message handler, where it's swallowed — so C# sends **no `loadScore`**, and the UI just sits there. Then "Save" persisted the new (unrenderable) key as the active definition while the old tablature stayed on screen — exactly the "render is wrong" / "load doesn't render" you saw.

I checked: **only Bb fully renders** right now. F-blues needs C7, Eb-blues needs Ab7 — neither authored. So the 12-key picker is writing a cheque the engine can't cash.

### Two fixes I'll bundle regardless of your choice below
- **Fail loudly, not silently** — wrap the generate/load/save handlers so an unsupported render surfaces a clear status (e.g. *"no voicing for Ab7 yet"*) instead of doing nothing, and **don't let Save persist an exercise that doesn't render**.
- **✅ practiced marker in the Saved list** — add a practiced count to the list payload so practiced exercises show a ✓ (your point 3).

### The decision: how do we want keys to work? (this is the fork)

| | Approach | Delivers | Cost / tension |
|---|---|---|---|
| **A** | **Restrict the picker to Bb** for now | Honest to current data; matches design's *"UI exposes Bb first"* | Key picker becomes ~1 option; defers the idea doc's *"all 12 keys"* |
| **B** ⭐ | **Computed movable shell voicings** — generalize the dom7 shell to any root | **All 12 keys immediately**; realizes idea doc item #1 (*"transposable to all 12 keys — proves the harmony engine"*) | Departs from design §6's *"added as authored rows, not code changes"* note → needs a small design update; a few keys land at higher frets |
| **C** | **Author 12 dom7 rows** by hand | All 12 keys, stays "authored data" | You hand-verify ~9 more voicings now; slower |

**I recommend B.** Shell voicings *are* movable shapes — and I verified the three authored ones are just three positions of one formula: on strings A/D/G, `fret = root` / `root+4 (3rd)` / `root+10 (♭7)` relative to each open string. That formula **reproduces `Bb7`/`Eb7`/`F7` exactly**, so `VoicingBookTests` stays green, and it's a small, pure-Domain change that finally proves the transposition engine the whole MVP is built around. The only honest cost is it turns "12 authored rows" into "1 authored shape" — worth a one-line design-doc amendment.

Which way — **A**, **B**, or **C**? (I'll also do the two bundled fixes either way.)

## Rafa:

Do B



## AI:

## Option B implemented — all 12 keys now render. Relaunched, please verify.

Build clean (0 errors), **39/39 tests** (was 26 — the voicing suite now covers all 12 roots), `app.js` valid.

### The fix — `VoicingBook` is now one movable shape
Replaced the 3-row hand-authored table with the computed dom7 **shell shape** on A/D/G: with the root at fret `R` on the A string, the voicing is `(s5:R, s4:R-1, s3:R)`. `R` is kept in 1..12 so the shape stays contiguous and never needs a negative fret (the lowest, A7, sits an octave up at frets 11–12). I unit-checked that it reproduces the old **Bb7 `(1,0,1)` / Eb7 `(6,5,6)` / F7 `(8,7,8)`** exactly, and that every root spells root/+4/♭7 — so the Bb rendering and the renderer tests are unchanged, and the other 11 keys just work now.

### Bundled fixes (as promised)
- **Fail-loud:** generate/load/boot now catch a render failure and push a `status` error to the UI instead of silently no-opping; an exercise only becomes "current" (and thus savable) **if it rendered**. So Save can't persist a broken one anymore.
- **✅ practiced marker:** the Saved list now shows `✅ N` on practiced exercises (practice count flows in the list payload; the list refreshes after Mark-practiced).

### Please re-test the things that broke
1. **Change Key → Generate** redraws the score (try a few keys — C, F, A, etc.)?
2. **Save** then **click the saved item** → it reloads/redraws?
3. **Mark practiced** → the Saved list entry shows **✅ 1** (and increments on repeat)?
4. Audio + synced cursor still good on the non-Bb keys?

One bookkeeping note: this contradicts design §6's *"voicings added as authored rows, not code changes,"* so once you confirm it's working I'll **amend the design doc** (§2 + §6: computed movable shell, all 12 keys) to record the decision. Holding that edit until you verify.

## Rafa:

Working correctly
ammend the design, but this mvp is just the beginning to have a starting point to implement chord progressions and rhythmic patterns (for rhythmic and lead guitar). we will chat about app features after...
release 0.2.0
update all README, CHANGELOG, etc
commit, push, tag


## AI:

## 🚀 v0.2.0 shipped

**Release:** https://github.com/reslava/chord-flow/releases/tag/v0.2.0

- **Design amended** (`mvp-design.md` v3) — §2 + §6 now describe the **computed movable dom7 shell** (all 12 keys), §7 records the voicing-coverage resolution, §4/§5 note the Phase-3 bridge vocabulary + slices. The earlier "authored rows, not code changes" note is explicitly superseded.
- **Version → 0.2.0** (`ChordFlow.App.csproj`).
- **README + CHANGELOG** updated (features, 39 tests, project layout, full 0.2.0 entry).
- **Commit** `7b3c266` on `main` (no attribution), pushed. **Tag** `v0.2.0` (annotated), pushed. **GitHub release** created.
- 25 files, 39/39 tests green; `.sf2` still git-ignored (fetched at build), no `.db` in the tree.

That closes Phase 3: persistence + library + progress + the builder UI, plus the all-12-keys voicing fix you found and verified.

Noted your framing — **this MVP is the foundation**, not the destination: a starting point for real chord-progression + rhythm training across rhythm *and* lead guitar. When you're ready we'll open a new chat to scope those features (more progressions, lead/scale content, additional voicing sets, difficulty progression, etc.).

Say `close` to close this chat.
