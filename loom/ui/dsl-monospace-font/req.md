---
type: req
id: rq_01KVX1SY9W3Q3RG843R4J8GWR9
title: Monospace font for DSL editors — Requirements
status: locked
created: 2026-06-24
updated: 2026-06-24
version: 2
tags: []
parent_id: id_01KVW7YV4V2XFDSSKNPT653N4F
requires_load: []
---
# Monospace font for DSL editors — Requirements

### ✅ Included

- `IN1` A single shared CSS class (`.dsl-input`) carrying the monospace font stack, applied to every DSL text input in the app (one class, no per-editor duplication).
- `IN2` Apply `.dsl-input` to the content CRUD DSL textarea (`#ccDsl` in `content-crud.js`) — the one editor that serves all four entity types (progression / song / rhythm / voicing).
- `IN3` Apply `.dsl-input` to the Scales view interval-set input (`#scaleIntervals` in `index.html`), which is currently proportional.
- `IN4` ~dropped~ (superseded by `IN5`) — was: full monospace stack `ui-monospace, "Cascadia Code", Consolas, "Courier New", monospace`. Dropped because `ui-monospace`/`Cascadia Code` resolve to a ligature font whose contextual alternates make character cells "dance" while typing — defeating the cell-alignment goal.
- `IN5` Monospace stack leads with a Windows-guaranteed, ligature-free font: `Consolas, "Cascadia Mono", ui-monospace, "Courier New", monospace` (Cascadia **Mono** = the no-ligature variant; generic `monospace` as final fallback).
- `IN6` Disable ligatures / contextual alternates on every DSL editor (`font-variant-ligatures: none` + `font-feature-settings: "liga" 0, "calt" 0`), so cells stay fixed-advance regardless of which font resolves — no `=>` / `..` / `==` glyph merging.

### ❌ Excluded

- `EX1` The score/tab engraving font — alphaTab renders with its own engraving font; untouched.
- `EX2` Syntax highlighting / a full code editor — a separate, later concern.
- `EX3` The alphaTex debug scratchpad (`.cf-debug-tex`) — alphaTex tooling, not an end-user DSL editor (and already monospace); out of scope.

### ⛓ Constraints

- `C1` No JS behavior/logic change — JS markup gains only the `class="dsl-input"` attribute; CSS gains only the shared rule.
- `C2` Consolidate, don't accrete: fold the existing ad-hoc `font-family` monospace declaration on `.cc-editor textarea` (index.html) into the shared `.dsl-input` class rather than adding a third declaration.
- `C3` Preserve the existing dark-theme styling of those inputs (background, border, padding, sizing) — only the font changes.
- `C4` `.dsl-input` must out-specify the `.cc-editor … { font: inherit }` reset (which is `(0,1,1)`): the rule is element-qualified (`textarea.dsl-input, input.dsl-input`) so it ties and wins on source order. A plain `.dsl-input` class `(0,1,0)` silently loses.