---
type: design
id: de_01KVVAXKWG4F6SFKSPPC2P4CPG
title: Play UI — Key control seeds from the song's key (Design)
status: done
created: 2026-06-23
updated: 2026-06-23
version: 2
tags: []
parent_id: id_01KVVADMV8C65C52YN8E8C0CEZ
requires_load: []
---
# Play UI — Key control seeds from the song's key (Design)

> Design for Finding 4 of the `songbook/jazz-blues` dogfood. Root cause confirmed in code; approach + sub-decisions settled in `chats/play-ui-key-init-chat-001.md`.

## Problem (confirmed root cause)

Loading a song into Practice ignores its authored key — *Jazz Blues in F* played in **Bb**. In `wwwroot/app.js`:

- `populateStaticPickers()` (line 93) hardcodes the key picker to `"10"` (Bb).
- default state (line 35) sets `keyPitchClass: 10`.
- the `generate` envelope (line 142) **always** sends a concrete `keyPitchClass` (`… || 0`), never null.

`keyPitchClass` → `Exercise.KeyOverride`; `ExerciseRendering` resolves `KeyOverride ?? InitialKey`. The override is never null, so `Song.InitialKey` never applies, and the picker is never re-seeded on song selection. The Bb is a leftover default from the key-less 12-bar blues.

## Crux

The picker can neither display nor honor the song's key because **JS doesn't know it**: the harmony picker is filled from `entityList`, whose `ContentSummary` is `(Id, Name, Origin, HasLowerTier)` — no key. And a **progression is key-independent** (no `InitialKey`); only a **song** has one (explicit `key` line, else the C default). So the fix needs the song's initial key **surfaced across the bridge**.

## Decision — Option A (surface `initialKey` on the song catalog entry)

Chosen over a nullable-`keyPitchClass` contract (which fixes playback but not the picker's *display*, so it's only half a fix). A fixes both, and is the seam that later lets the Practice header show "Jazz Blues in F" honestly.

**Mechanism:**

- **Core / bridge:** the song's catalog entry (surfaced via `entityList`) gains an **`initialKey`** pitch class (0–11), computed from the parsed song's `InitialKey` (explicit `key` token, else C default). **Null for progressions** (no key). Exact DTO shape (extend `ContentSummary` vs a song-specific field on the list reply) is a plan-level detail; the seam is "the harmony list tells JS each song's key."
- **JS (`app.js`):** on harmony selection, **seed the key picker**:
  - selected harmony is a **song** → set the picker to its `initialKey`;
  - selected harmony is a **progression** → set the picker to the neutral **C** default (was Bb).
- `generate` keeps sending the picker's value (now correctly seeded), so a song plays in its authored key; **manual override still works**.

## Settled sub-decisions

1. **Progression (no key) default = C** (was Bb — Bb was only ever the blues demo's key).
2. **Re-seed on harmony switch:** selecting a new harmony **adopts that piece's key**; a manual key override persists **only until the next harmony switch**, then the newly selected piece's key wins.
3. **Saved exercises unaffected:** a loaded `Exercise` carries an explicit `KeyOverride` token — it still wins and seeds the picker on load. The fix must **not clobber** that path; it only changes the *fresh-selection* default.

## Scope

**In:** surface song `initialKey` through the harmony list; seed the Practice key picker from the selected harmony (song → its key, progression → C); update the hardcoded Bb default. 

**Out:** the other dogfood findings (their own threads); any redesign of the param strip; showing the key in the Practice header (a nice follow-on the seam enables, not required here).

## Validation

- Load **Jazz Blues in F** → picker shows **F** and it plays in **F**, no manual change. 
- Select the **12-bar blues** progression → picker shows **C**. 
- Manually override the key, then switch harmony → the new piece's key wins. 
- Load a **saved exercise** with a stored key → that key still applies.
- Dogfood on the Practice surface (the guitar-weave dogfood rule): confirm picker + rendered/played key agree with the selected content.
