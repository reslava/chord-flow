---
type: idea
id: id_01KV05B8D5VP4T5R37BSJ1NQRR
title: Exercise workbench UI — generator + practice/player
status: draft
created: 2026-06-13
version: 1
tags: []
parent_id: null
requires_load: []
---
# Exercise workbench UI — generator + practice/player

## The idea

The **exercise-facing UI**: generate an exercise, render its tab, play it with the
synced cursor, save it, and revisit the saved library. This is today's MVP
`wwwroot/` (`index.html` + `app.js`) — captured here in the `ui` weave so all
front-end/UX work is grouped, and given a home for its future evolution.

## What exists today (MVP)

- Key picker + the three seed rhythms; **Generate** → engine-produced alphaTex.
- **Play / Stop / Tempo**, the alphaTab cursor, **Save**, **Mark-practiced**, and
  the saved-exercise list — all over the narrow C#↔JS envelope bridge.
- Hard-wired to the 12-bar blues progression and Beginner difficulty.

## Where it goes (to design)

- Choose **any** stored `Progression` / `Song` (not just the blues) — needs the
  content-list bridge from [[content-crud]].
- **Difficulty / Feel** pickers; multi-bar rhythm patterns.
- **Voicing selection:** surface `VoicingBook.Candidates` (the ranked CAGED list)
  so the player can pick which shape/region to practice — the consumer the
  voicings engine was built for.
- Richer practice loop (looping, count-in, tempo ramps) — overlaps the
  `Progress` / `PracticeSession` features.

## Relationship to the rest of the `ui` weave

Peer of [[content-crud]] (authoring) — this thread is the **consumption** side
(practice/play). Both sit on the same bridge contract and `wwwroot` shell.

Related: [[content-crud]], the `domain/voicings` thread (Candidates ranked list),
the `chordflow/mvp` thread (origin of the current UI).