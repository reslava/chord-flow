---
type: idea
id: id_01KVZYCRYEXGAAAH2QQ27ZCSYF
title: Explicit per-chord voicing references in the DSL
status: draft
created: 2026-06-25
version: 1
tags: []
parent_id: null
requires_load: []
---
# Explicit per-chord voicing references in the DSL

## Goal

Let an author pin a **specific voicing to a specific chord** in the Song/Progression DSL — overriding the automatic ranking fill for that chord only. Two annotation forms on a chord token:

- **Source-qualified reference** — names a listed voicing by source + id: `{u: C6}` (user), `{a: shell-C6}` (automatic), `{swing: C6}` (package *Swing*).
- **Literal custom grip (no source)** — a bare fret string, low-E→high-E: `{c: 8 x 7 9 8 x}` (`x` = muted).

So `2m7_V7 I6 {u: C6}` and `2m7_V7 I6 {c: 8 x 7 9 8 x}` are two explicit voicing versions of the same progression.

## Origin

Spun off from [[engine-derived-as-app-source]] (chat-002), which surfaced this while deciding the comping-resolution architecture. **Depends on [[engine-derived-as-app-source]]** — its `CompingResolver` (the Features-layer voicing resolution, decision D4=(B)) is the seam this rides on: per chord, an explicit annotation **overrides** the ranking fill. Also leans on [[content-source-model]] source tags for the `{u:}`/`{a:}`/`{package:}` vocabulary.

## Why

Real arranging is per-chord: a player wants *this* C6 grip here, a stretch voicing there. The automatic ranking is the bulk default; explicit references are the manual override that makes a progression a finished arrangement, and they let one progression carry several authored voicing treatments. (B) was chosen in the sibling thread **specifically** so this is an additive override path, not a rewrite.

## Shape (sketch — design firms this up)

- **DSL grammar.** A per-chord `{…}` annotation token on a chord in the Progression/Song DSL: `{u: id}` / `{a: id}` / `{<package>: id}` (source-qualified) and `{c: <fret string>}` (literal). Parsed by `ProgressionParser`/`SongParser`; the parsed chord carries an optional voicing annotation (timing-free, like the chord itself).
- **Resolver override.** `CompingResolver` checks each chord for an annotation **before** the ranking fill: a reference resolves the exact source-qualified voicing from the stores / engine; a literal `{c:}` parses straight to a `Voicing`. No annotation ⇒ today's ranking fill.
- **Round-trip + canonicalization.** Decide how the annotation serializes back (the literal grip may canonicalize like authored voicings do).

## Scope

**In:** the per-chord DSL annotation grammar (both forms), parser support, the `CompingResolver` override path, the DSL-reference update, round-trip.
**Out:** the automatic ranking fill + main-source/fallback ([[engine-derived-as-app-source]]); the selectable ranking modes ([[voicing-ranking-strategies]]); a UI voicing-picker that writes these annotations (additive, later).

## Open design questions (for design)

1. Token placement + escaping in the DSL (does `{…}` collide with anything? per-chord vs per-span).
2. The literal `{c:}` grip's exact syntax (fret string only, or barre/finger hints?) and whether it canonicalizes.
3. Reference resolution semantics when the named id is missing or its source is filtered out (fail loud vs fall back).
4. Whether annotations live on the Progression DSL, the Song DSL, or both.

## Validation

- A chord with `{u: C6}` comps that exact user voicing; the rest of the progression uses the automatic fill.
- A chord with `{c: 8 x 7 9 8 x}` comps that literal grip.
- The annotated DSL round-trips through parse → serialize unchanged.
- **Dogfood:** an annotated 12-bar blues renders the pinned grips on the now/next fret-boxes.