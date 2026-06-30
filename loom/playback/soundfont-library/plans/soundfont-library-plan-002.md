---
type: plan
id: pl_01KVFN4ASF6C768EEQSW4W1B54
title: Support .sf3 soundfonts
status: done
created: 2026-06-19
updated: 2026-06-19
version: 1
design_version: 2
req_version: 1
tags: []
parent_id: de_01KV7NET48R277ZA3EWC9ZR6ZY
requires_load: []
target_version: 0.1.0
actual_release: 0.7.0
steps:
  - id: discover-sf3-alongside-sf2-document
    order: 1
    status: done
    description: Discover .sf3 soundfonts alongside .sf2 in the catalog, and document the supported formats + download link
    files_touched: [src/ChordFlow.Desktop/WebHost/WwwrootSoundFontCatalog.cs, README.md]
    blocked_by: []
    satisfies: [IN2, EX4]
---
# Support .sf3 soundfonts

## Goal

A small follow-up to the soundfont-library feature: extend catalog discovery to recognize alphaTab's Ogg-compressed `.sf3` SoundFont variant alongside `.sf2`, and document the supported formats. The discovery seam is format-agnostic by design, so no Domain/renderer/req/design change is needed — only the host catalog's enumeration was still hardcoded to `.sf2`. Shipped in commit `00ebc03`.

---

## Steps

| Done | # | Step | Files touched | Blocked by | Satisfies |
|---|---|---|---|---|---|
| ✅ | 1 | Discover .sf3 soundfonts alongside .sf2 in the catalog, and document the supported formats + download link | src/ChordFlow.Desktop/WebHost/WwwrootSoundFontCatalog.cs, README.md | — | IN2, EX4 |
---

### Legend

| Symbol | Meaning |
|--------|---------|
| ✅ | Done |
| 🔄 | In Progress |
| 🔳 | Pending |
| ❌ | Cancelled |

<!-- step:discover-sf3-alongside-sf2-document -->
### Step 1 — Discover .sf3 alongside .sf2 + document

Host catalog: `Directory.EnumerateFiles(_folder, "*.sf2")` → enumerate all files and filter against a case-insensitive `{ ".sf2", ".sf3" }` extension set (EnumerateFiles takes a single glob, so a set filter matches both); class doc-comment updated. The JS loader (`score-render-component.js`) is already extension-agnostic — it builds the URL from the file id via `fontUrl(id)` and calls `api.loadSoundFontFromUrl(...)`, never inspecting the extension — so no JS change. `.gitignore` already ignored `*.sf2`/`*.sf3`, so the policy was pre-written; this just brought discovery in line. README: opening line notes SoundFont `.sf2`/`.sf3` are loaded interchangeably, the drop-in/extract steps cover both, and a thin link to the MuseScore soundfont list was added. Shipped in commit `00ebc03`.
