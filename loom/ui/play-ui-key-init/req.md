---
type: req
id: rq_01KVVB3576J1F8SE2P4DASZ85C
title: Play UI — Key control seeds from the song's key — Requirements
status: locked
created: 2026-06-23
updated: 2026-06-23
version: 1
design_version: 2
tags: []
parent_id: de_01KVVAXKWG4F6SFKSPPC2P4CPG
requires_load: []
---
# Play UI — Key control seeds from the song's key — Requirements

Seed the Practice key picker from the selected harmony's key so a song plays in its authored key by default (the song's `InitialKey`), while keeping manual override working. Scope confirmed in `play-ui-key-init-chat-001` + `play-ui-key-init-design`. Root cause: `app.js` hardcodes the key picker to Bb and always sends a concrete `keyPitchClass`, so `Song.InitialKey` never applies.

### ✅ Included

- `IN1` The **song** catalog entry surfaces an **`initialKey`** pitch class (0–11), computed from the parsed song's `InitialKey` (explicit `key` token, else the C default), carried to JS over the existing harmony **`entityList`** reply. **Null/absent for progressions** (key-independent).
- `IN2` On harmony selection, `app.js` **seeds the key picker**: a **song** → its `initialKey`; a **progression** → the neutral **C** default.
- `IN3` The hardcoded **Bb** default is removed — the picker default for the key-less / progression case becomes **C** (`app.js` picker default + initial state `keyPitchClass`).
- `IN4` **Re-seed on every harmony switch**: selecting a new harmony **adopts that piece's key**; a manual key override persists **only until the next harmony switch**.
- `IN5` `generate` keeps sending the picker's (now correctly seeded) `keyPitchClass`, so a song plays in its **authored key**; **manual override still works** for the current selection.

### ❌ Excluded

- `EX1` The nullable-`keyPitchClass` / null-`KeyOverride` bridge-contract change (design Option B) — not pursued; the picker stays the single source of the sent key.
- `EX2` The other jazz-blues dogfood findings — `tie-dotted-rendering`, `chromatic-degrees`, `voicing-difficulty-bands` (their own threads).
- `EX3` Showing the key in the Practice header, and any redesign of the param strip (a follow-on the seam enables, not required here).
- `EX4` Giving a **progression** a real key concept — progressions stay key-independent.
- `EX5` Any change to the **save / library** key paths beyond not clobbering them.

### ⛓ Constraints

- `C1` Dependency direction **Desktop → Core** unchanged; the engine stays UI-agnostic.
- `C2` **Saved exercises untouched**: a loaded `Exercise`'s explicit `KeyOverride` still wins and seeds the picker on load — the fix only changes the **fresh-selection** default, it must not clobber the load path.
- `C3` Reuse the existing generic **`entity*` bridge family** / harmony `entityList` reply — surface `initialKey` on the existing song list items; **no new bridge verb**.
- `C4` **No new build step or framework** in `wwwroot` — vanilla JS over the existing virtual host.
- `C5` `initialKey` is **derived from the song's parsed `InitialKey`** (the same `KeyOverride ?? InitialKey` semantics) — not a second, duplicated key store that could drift.