---
type: idea
id: id_01KVW7YV4V2XFDSSKNPT653N4F
title: Monospace font for DSL editors
status: done
created: 2026-06-24
updated: 2026-06-24
version: 2
tags: []
parent_id: null
requires_load: []
---
# Monospace font for DSL editors

## Goal

Render **every DSL text input** in a **monospace** font so cells/columns line up under the grid — Rhythm DSL especially (where each cell must align), and consistently across Progression, Song, and Voicing editors too.

## Origin

`domain/tie-dotted-rendering` chat-001: the rhythm content editor uses a proportional font, which makes writing cell-aligned rhythms painful.

## Shape

- A monospace stack on all DSL textareas/editors: `font-family: ui-monospace, "Cascadia Code", Consolas, "Courier New", monospace`.
- A `wwwroot` CSS change; isolate a single class (e.g. `.dsl-input`) applied to every DSL editor.

## Scope

**In:** monospace styling for all DSL text inputs in the app.
**Out:** the score/tab font (alphaTab uses its own engraving font); a full editor with syntax highlighting (separate, later).

## Validation

- Rhythm cells visually align column-by-column in the editor.
- Dogfood: confirmed on the content/exercise UI pages.