---
type: idea
id: id_01KVB4KGMYZTB1FXVB3XJ82HTG
title: ChordFlow user guide
status: done
created: 2026-06-17
updated: 2026-06-21
version: 2
tags: []
parent_id: null
requires_load: []
---
# ChordFlow user guide

## What

An end-user guide for ChordFlow — "how do I use this app" for guitarists who download the Windows build, **not** a developer/architecture doc. Stubbed out of the `release-pipeline` discussion (`release-pipeline-chat-001.md`): once releases ship a downloadable zip, downloaders need usage docs.

## Why

The release pipeline produces a self-contained Windows zip aimed at non-technical guitarists. They need: how to install/run it (incl. the SmartScreen "unknown publisher" first-run prompt), how to build & play an exercise (key / rhythm / tempo / generate / save / mark-practiced), and how to **add their own soundfont** (drop a `.sf2` into `wwwroot/soundfont/` — the catalog auto-discovers it).

## Likely scope (to refine in design)

- Install & first run (SmartScreen note).
- The builder UI walkthrough — pickers, generate, playback cursor, save/library, mark-practiced.
- The Progression DSL for the curious (link to `loom/refs/chordflow-dsl-reference.md`, the public DSL guide).
- Soundfonts: the bundled `sonivox.sf2`, the README curated download list, and how to add a bank.
- Known limits (Windows-only, no audio-input accuracy detection).

## Intersection with the release pipeline (kept deliberately thin)

- Joins the release **doc-accuracy review** list (alongside README + the three refs).
- Optionally **bundled into the release zip** (a `USERGUIDE`/`LICENSE` next to the exe) and/or linked from the GitHub release notes.

This thread owns *writing* the guide; the `release-pipeline` thread owns the machinery. Format/home (in-repo Markdown vs in-app help vs a docs site) is an open design question.

## Open questions

- Where does it live — a repo `docs/` Markdown set, in-app help, or a published page?
- Weave placement: stubbed under a new `docs` weave; move to `meta` or elsewhere if preferred.