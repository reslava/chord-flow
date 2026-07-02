---
type: idea
id: id_01KVVADMV8C65C52YN8E8C0CEZ
title: Play UI — Key control seeds from the song's key
status: done
created: 2026-06-23
updated: 2026-06-23
version: 2
tags: []
parent_id: null
requires_load: []
---
# Play UI — Key control seeds from the song's key

## Goal

When a song is loaded into the play UI, it should play in **the song's authored key** by default. Loading **"Jazz Blues in F"** must play in **F** — without the user touching the Key control — while still letting them override the key afterward.

## The problem (observed)

Surfaced as **Finding 4** of the `songbook/jazz-blues` dogfood: loading *Jazz Blues in F* played the blues in **Bb**, not F. The user had to manually move the Key control to F before it played in the authored key.

## Root cause (hypothesis — confirm in design)

The play unit resolves key as `KeyOverride ?? Song.InitialKey` (domain model §7): a null override falls back to the song's `InitialKey`; a non-null override wins. The play UI's **Key control always emits an explicit `KeyOverride`** equal to whatever it currently shows — and its default shows **Bb** — so the override is never null and `Song.InitialKey` (the `key F` in the song DSL) never gets a chance. The control is **not seeded from the loaded song**.

## Shape

- On **song load**, seed the Key control from the resolved `Song.InitialKey` so the displayed key matches the authored key.
- Decide how "play in the authored key" is expressed across the bridge: send **no override** (null `KeyOverride`, let Core fall back) vs send an explicit override **equal to** `InitialKey`. Lean: the control mirrors `InitialKey` and only sends an override once the user *changes* it.
- Keep manual override fully working (the control is still the escape hatch).

## Scope

**In:** seed/sync the play-UI Key control from the loaded song's key; correct the load-time default so the authored key wins.

**Out:** the other dogfood findings (their own threads — `tie-dotted-rendering`, `chromatic-degrees`, `voicing-difficulty-bands`); any broader play-UI redesign.

## Open questions

- Where does the **Bb default** come from (a hardcoded control default, last-used key, persisted state)?
- After a song is loaded and the user manually overrides the key, what happens when they **switch to another song** — re-seed from the new song, or keep the manual key?
- Bridge contract: null `KeyOverride` vs explicit `InitialKey` — which is cleaner given the existing `renderOptions`/exercise envelope?

## Validation

- Load **Jazz Blues in F** → it plays in **F** with no manual key change. Override to another key still works.
- Dogfood on the actual play UI surface (load the song, confirm the key control + the rendered/played key).

## Origin

`songbook/jazz-blues` dogfood — Finding 4 (see `jazz-blues-design.md` → Outcome). First of four follow-on threads in the agreed priority order.