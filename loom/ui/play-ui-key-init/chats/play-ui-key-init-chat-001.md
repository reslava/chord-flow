---
type: chat
id: ch_01KVVAJQ18GGXJ3QW5X71B0XF2
title: play-ui-key-init Chat
status: done
created: 2026-06-23
updated: 2026-06-23
version: 2
tags: []
parent_id: null
requires_load: []
last_ai_block: 13
---
# play-ui-key-init Chat

## Rafa:

## AI:

Starting the design for Finding 4 (from the `songbook/jazz-blues` dogfood). I read the architecture ref and the actual Practice code, so the root cause is now **confirmed, not hypothesized**.

## Root cause (confirmed in code)

In `wwwroot/app.js`:

- **`populateStaticPickers()` (line 93):** `fillSelect($("key"), …, "10"); // Bb default` — the key picker is **hardcoded** to pitch class 10 (Bb).
- **default state (line 35):** `keyPitchClass: 10`.
- **`generate` envelope (line 142):** `keyPitchClass: parseInt($("key").value, 10) || 0` — it **always** sends a concrete pitch class, never null.

That `keyPitchClass` becomes `Exercise.KeyOverride`, and `ExerciseRendering` resolves `KeyOverride ?? InitialKey`. Since the override is never null, **`Song.InitialKey` (the `key F`) never gets a chance** — and the picker is **never re-seeded** when you select a song. So *every* song plays in Bb until you move the control. Open question 1 answered: the Bb is just a hardcoded UI default left over from the key-less 12-bar blues.

## The crux

The picker can't show or honor the song's key because **JS doesn't know it**. `InitialKey` is a Core concept; the harmony picker is filled from `entityList`, whose `ContentSummary` is `(Id, Name, Origin, HasLowerTier)` — **no key**. Also note a **progression has no key** (it's key-independent) — only a **song** has an `InitialKey` (explicit `key` line, or the C default). So the fix isn't purely JS; it needs the **song's initial key surfaced across the bridge**.

## Design options (the real decision)

**A — Surface `initialKey` on the song catalog entry (recommend).**
Add an `initialKey` pitch class to the song's list/summary (null for progressions). In `app.js`, when the selected harmony is a song, **seed the key picker from its `initialKey`**; for a progression, fall back to a neutral default. `generate` keeps sending the picker value — now correctly seeded — so the song plays in its authored key, and manual override still works.
- *Cost:* extend the song summary (Core) + one seed-on-selection hook (JS). Picker stays the single source of truth for display.

**B — Make `keyPitchClass` nullable; send null unless the user picked a key.**
Core already does `KeyOverride ?? InitialKey`, so a null override Just Works for playback. But the picker still couldn't *display* the actual key (it'd show blank/Bb), which is a worse UX — and you'd still want A to show the right key. So B alone is half a fix.

**My lean: A.** It fixes both the *playback* and the *display*, and it's the seam that later lets the Practice header show "Jazz Blues in F" honestly.

## Smaller decisions inside A

1. **Progression (no key) default** — keep Bb, or switch to a neutral **C**? (Bb was only ever the blues demo's key.)
2. **Re-seed on harmony switch vs keep a manual override** — when you select a new song, re-seed the key to *its* key (my lean), or preserve whatever key you'd manually set? I think selecting a new piece should adopt that piece's key; manual override lasts until you switch pieces.
3. **Loaded saved exercises** — a saved `Exercise` has an explicit `KeyOverride` token; that should still win and seed the picker on load (no change, just don't clobber it).

Before I touch anything: does **A** sound right, and what's your call on the progression default (1) and the re-seed behavior (2)?

## Rafa:
A sound right
(1) C
(2) your lean

## AI:

Locked. Recorded the design → `play-ui-key-init-design.md`:

- **Option A** — the song catalog entry surfaces an `initialKey` pitch class (null for progressions); `app.js` seeds the key picker on harmony selection.
- **(1)** progression default → **C**.
- **(2)** harmony switch **adopts the new piece's key**; a manual override lasts only until the next switch.
- **(3)** saved exercises unchanged — their stored `KeyOverride` still wins and seeds the picker.

**Next step — the req.** Per our flow (idea → design → **req** → plan), I'll write `req.md` with the IN/C handles (the bridge `initialKey` seam, the seed-on-selection behavior, the C-default change, the don't-clobber-saved-exercises constraint), lock it, then build the plan citing those handles.

**STOP** — say go and I'll draft + lock the req.

## Rafa:

I marked idea, design done
Create req, lock it
Create plan

## AI:

Done — req **locked** (`req.md`, 5 IN · 5 EX · 5 C) and **plan created** (`plans/play-ui-key-init-plan-001.md`), 4 steps:

| # | Step | Files | Satisfies |
|--:|------|-------|-----------|
| 1 | **Core** — surface a nullable song `initialKey` on the `entityList` items, derived from `Song.InitialKey` | `ContentSummary`, `SongStore`, `ContentCrudHandler` | IN1, C3, C5 |
| 2 | **Frontend** — `app.js` seeds the key picker (song→its key, progression→C), drop the Bb default, re-seed on switch; don't clobber saved-exercise load | `app.js` | IN2–IN5, C2, C4 |
| 3 | **Tests** — list surfaces `initialKey` = F for jazz_blues_f, C for a key-less song, null for progressions | `ContentCrudStoreTests` | IN1, C5 |
| 4 | **Validate + ref sync** — dogfood the 4 scenarios; update the architecture ref's `entityList` contract note | `chordflow-architecture-reference.md` | IN2, IN4, C2 |

Step 1 touches Core: add `int? InitialKey` to `ContentSummary`, populated only for songs (parse the body, read `Song.InitialKey.Tonic.Value` — explicit `key` or the C default), null elsewhere. I'll verify the real file paths before editing.

**STOP** — say go and I'll do Step 1.

## Rafa:

I started the plan
Do whole plan

## AI:

Implemented Steps 1–3 (✅ in the plan); Step 4 is done except your in-app dogfood.

**Step 1 — Core**
- `ContentSummary` + `ContentItem` gained `int? InitialKey` (null by default).
- `SongStore.List` derives it from the **winning tier's** parsed `Song.InitialKey.Tonic.Value` (explicit `key`, else C); guarded so a malformed song still lists. Other stores leave it null (key-independent).
- `ContentCrudHandler.List` maps it onto the `entityList` items.

**Step 2 — Frontend + host boot**
- `app.js`: new `seedKeyForHarmony()` fires on the **harmony `change`** event — song → its `initialKey`, progression → **C**. Picker default flipped Bb → **C** (picker + `BOOT_REQUEST`). Saved-exercise load path untouched (it never calls this), so a stored `KeyOverride` still wins (C2).
- `Program.cs`: the boot default `keyPitchClass` 10 → **0**, so the boot 12-bar blues is consistent with the new C default (it's the same Bb-legacy — flipping it avoids a boot where the score is Bb but the picker says C).

**Step 3 — Tests**
- Two new tests: the song list surfaces `InitialKey` = 5 (F) for a `key F` song, 0 (C) for a key-less one; a progression item is null.
- Full suite **84/84 green**; solution builds (Core + Desktop).

**Step 4 — Validate + ref sync**
- Architecture ref's `entityList` note updated (song items carry `initialKey`).
- **Your turn — the in-app dogfood.** Please run and confirm:
  1. Load **Jazz Blues in F** → picker shows **F**, plays in **F**, no manual change.
  2. Select the **12-bar blues** progression → picker shows **C**.
  3. Manually change the key, then switch harmony → the **new piece's key wins**.
  4. Load a **saved exercise** that had a stored key → that key still applies.

If all four hold, I'll mark Step 4 ✅ and close the plan. If anything's off, tell me what you see.

## Rafa:

All working

## AI:

🎉 Step 4 ✅ — **plan done and closed** (`done/pl_01KVVB4YFN3HFMFAS9T1F7HVJ6-done.md`). `play-ui-key-init` is complete: a song now plays in its authored key, dogfood-confirmed.

That clears **Finding 4** from the jazz-blues dogfood. Roadmap status:

| Thread | Finding | Status |
|--------|---------|--------|
| `play-ui-key-init` | 4 | ✅ done |
| `tie-dotted-rendering` | 3 | next |
| `chromatic-degrees` | 1 | queued |
| `voicing-difficulty-bands` | 2, 5, 6 | queued |

Next in the agreed order is **`tie-dotted-rendering`** — alphaTex ties/dotted notes, which unlocks real syncopated comping (the Charleston, and the heart of jazz rhythm). Want to spin it up now, or pause here?

## Rafa:

commit
i save pending roadmap here:
Roadmap progress:
  - ✅ play-ui-key-init (Finding 4)
  - ▶️  tie-dotted-rendering (Finding 3) — next: alphaTex ties/dotted
  notes, unlocks real syncopated comping
  - ⏳ chromatic-degrees (Finding 1)
  - ⏳ voicing-difficulty-bands (Findings 2, 5, 6)

spin up all pending threads / ideas to continue tomorrow in a new session  