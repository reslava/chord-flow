---
type: plan
id: pl_01KVVB4YFN3HFMFAS9T1F7HVJ6
title: play-ui-key-init Plan
status: done
created: 2026-06-23
updated: 2026-06-23
version: 1
design_version: 1
req_version: 1
tags: []
parent_id: de_01KVVAXKWG4F6SFKSPPC2P4CPG
requires_load: []
target_version: 0.1.0
steps:
  - id: core-expose-song-initialkey-on-the
    order: 1
    status: done
    description: Surface a nullable song initialKey on the entityList reply items, derived from the parsed Song.InitialKey
    files_touched: [src/ChordFlow.Core/Persistence/ContentSummary.cs, src/ChordFlow.Core/Persistence/SongStore.cs, src/ChordFlow.Core/Features/ContentCrud/ContentCrudHandler.cs]
    blocked_by: []
    satisfies: [IN1, C3, C5]
  - id: frontend-seed-the-key-picker
    order: 2
    status: done
    description: Seed the Practice key picker from the selected harmony; default C for progressions; re-seed on harmony switch
    files_touched: [src/ChordFlow.Desktop/wwwroot/app.js]
    blocked_by: [1]
    satisfies: [IN2, IN3, IN4, IN5, C2, C4]
  - id: tests
    order: 3
    status: done
    description: Tests — the song list surfaces initialKey (F for jazz_blues_f, C for a key-less song); non-song items are null
    files_touched: [tests/ChordFlow.Core.Tests/ContentCrudStoreTests.cs]
    blocked_by: [1]
    satisfies: [IN1, C5]
  - id: validate-ref-sync
    order: 4
    status: done
    description: Validate in-app + sync the architecture reference's entityList contract note
    files_touched: [loom/refs/chordflow-architecture-reference.md]
    blocked_by: [2, 3]
    satisfies: [IN2, IN4, C2]
---
# play-ui-key-init Plan

## Goal

Make the Practice key picker seed from the selected harmony's key so a song plays in its authored key by default, while keeping manual override working. The song's InitialKey is surfaced to JS as a nullable initialKey pitch class on the existing entityList reply items (null for key-independent progressions); app.js seeds the key picker on harmony selection (song → its initialKey, progression → the neutral C default), replacing the hardcoded Bb. Re-seeding happens on every harmony switch; the saved-exercise load path (explicit KeyOverride) is untouched. No new bridge verb, no new build step, dependency direction Desktop → Core unchanged.

---

## Steps

| Done | # | Step | Files touched | Blocked by | Satisfies |
|---|---|---|---|---|---|
| ✅ | 1 | Surface a nullable song initialKey on the entityList reply items, derived from the parsed Song.InitialKey | src/ChordFlow.Core/Persistence/ContentSummary.cs, src/ChordFlow.Core/Persistence/SongStore.cs, src/ChordFlow.Core/Features/ContentCrud/ContentCrudHandler.cs | — | IN1, C3, C5 |
| ✅ | 2 | Seed the Practice key picker from the selected harmony; default C for progressions; re-seed on harmony switch | src/ChordFlow.Desktop/wwwroot/app.js | 1 | IN2, IN3, IN4, IN5, C2, C4 |
| ✅ | 3 | Tests — the song list surfaces initialKey (F for jazz_blues_f, C for a key-less song); non-song items are null | tests/ChordFlow.Core.Tests/ContentCrudStoreTests.cs | 1 | IN1, C5 |
| ✅ | 4 | Validate in-app + sync the architecture reference's entityList contract note | loom/refs/chordflow-architecture-reference.md | 2, 3 | IN2, IN4, C2 |
---

### Legend

| Symbol | Meaning |
|--------|---------|
| ✅ | Done |
| 🔄 | In Progress |
| 🔳 | Pending |
| ❌ | Cancelled |

<!-- step:core-expose-song-initialkey-on-the -->
### Step 1 — Core — expose song initialKey on the list

The harmony picker is filled from the entityList reply, whose items are ContentSummary(Id, Name, Origin, HasLowerTier). Add a nullable `int? InitialKey` (tonic pitch class 0-11) to ContentSummary, populated **only for songs**: SongStore.List parses each song body (SongParser) and reads Song.InitialKey.Tonic.Value (explicit `key` token, else the C=0 default per the Song DSL). Progression/Rhythm/Voicing stores leave it null (key-independent — IN1). This is derived from the song's own InitialKey, never a second stored key (C5). Reuses the existing entity* list reply — no new verb (C3). Confirm exact field placement (ContentSummary vs a song-only item) and the parse-cost during implementation; songs are few, parsing the list is cheap. Verify the actual file paths for ContentSummary / SongStore before editing.

<!-- step:frontend-seed-the-key-picker -->
### Step 2 — Frontend — seed the key picker

Carry initialKey on the catalog items app.js builds from the entityList reply. Change the hardcoded Bb default to C: populateStaticPickers (line ~93) default `"10"` → `"0"`, and the initial state (line ~35) `keyPitchClass: 10` → `0` (IN3). Add a seedKeyForHarmony() that, on harmony `<select>` change, looks up the selected item: a song with a non-null initialKey → set $("key").value to it; a progression → set to `"0"` (C) (IN2). Wire it to the harmony select's change event so every switch re-seeds and adopts the new piece's key (IN4). generate still reads $("key").value (line ~142), so the seeded value flows through and manual override for the current selection still works (IN5). Crucially, do NOT call seedKeyForHarmony on the saved-exercise load path — loadExercise sets the picker from the stored KeyOverride and must keep winning (C2). Vanilla JS only (C4).

<!-- step:tests -->
### Step 3 — Tests

Against an in-memory db seeded with the default pack: SongStore.List (or the ContentCrud list path) surfaces InitialKey = 5 (F) for jazz_blues_f, and = 0 (C) for a song with no explicit `key` line (e.g. blues_song_demo, which defaults to C). A progression list item has null InitialKey. Place alongside the existing ContentCrud store/handler tests.

<!-- step:validate-ref-sync -->
### Step 4 — Validate + ref sync

Dogfood on the Practice surface: (a) load Jazz Blues in F → picker shows F, plays in F with no manual change; (b) select the 12-bar blues progression → picker shows C; (c) manually override the key, then switch harmony → the new piece's key wins (IN4); (d) load a saved exercise with a stored key → that key still applies (C2). Then update chordflow-architecture-reference.md where it documents the entityList reply to note the song items now carry initialKey (bridge-contract change → ref sync). The fretboard dogfood rule does not apply (a play-UI param knob, not a fretboard/engine capability).
