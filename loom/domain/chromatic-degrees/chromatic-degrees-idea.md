---
type: idea
id: id_01KVVCJAS88SGJ0QHXTGFB5T4S
title: "Progression DSL — chromatic (#/b) chord degrees"
status: draft
created: 2026-06-23
version: 1
tags: []
parent_id: null
requires_load: []
---
# Progression DSL — chromatic (#/b) chord degrees

## Goal

Let the Progression DSL express **chromatic chord roots** — `#IVdim7` (the jazz-blues bar-6 passing chord) and `bII7` (the tritone sub) — so idiomatic jazz blues can be written without a stand-in.

## Origin

`songbook/jazz-blues` dogfood — **Finding 1** (priority 3). Authoring the standard jazz blues hit the wall at bar 6's `#IVdim7`; `jazz_blues_standard.dsl` currently uses a `47` stand-in.

## Root cause

`ProgressionParser`'s token is `<degree><quality?>[:slots]`, where **degree is a single digit 1–7** — no accidental prefix. So `#4dim7` fails as "missing a scale degree." The vocabulary already exists elsewhere: the **Song DSL's `mod` accepts `#`/`b`** (`bIII`, `#…`) — borrow it.

## Shape

- Accept an optional leading `#`/`b` on a degree token in `ProgressionParser` (e.g. `#4dim7`, `b2 7`), resolving to the chromatic pitch class relative to the key.
- Update `chordflow-dsl-reference.md` (DSL change → ref sync) and `chordflow-domain-model-reference.md`.
- Upgrade **bar 6 of `jazz_blues_standard.dsl`** from the `47` stand-in to `#4dim7`.

## Scope

**In:** `#`/`b` accidental prefix on a progression degree; parser + refs; correct the jazz-blues bundle.
**Out:** automated tritone-sub / secondary-dominant *transforms* (a later `IProgressionTransform` thread — the north star in `jazz-blues-design`).

## Validation

- `#4dim7` and `bII7` parse and render with correctly spelled chords. 
- The standard jazz blues plays with the real diminished passing chord in bar 6.
- Parser tests for accidental degrees (incl. error cases).