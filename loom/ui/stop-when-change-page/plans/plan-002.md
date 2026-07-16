---
type: plan
id: pl_01KXMZD01HKZN8YCYBFKMNHQCE
title: Stop all playback on window blur / close
status: done
created: 2026-07-16
updated: 2026-07-16
version: 1
design_version: 1
tags: []
parent_id: null
requires_load: []
target_version: 0.1.0
steps:
  - id: wired-app-js-init-to-call
    order: 1
    status: done
    description: Wired app.js init() to call ChordFlowPlayback.stopAll() on window `blur` (app loses focus) and `pagehide` (closing / navigating away), so playback is silenced when ChordFlow goes to the background or shuts down — reusing the same registry-wide stopAll() as the page toggle.
    files_touched: []
    blocked_by: []
    satisfies: []
---
# Stop all playback on window blur / close

## Goal

Wired app.js init() to call ChordFlowPlayback.stopAll() on window `blur` (app loses focus) and `pagehide` (closing / navigating away), so playback is silenced when ChordFlow goes to the background or shuts down — reusing the same registry-wide stopAll() as the page toggle.

---

## Steps

| Done | # | Step | Files touched | Blocked by | Satisfies |
|---|---|---|---|---|---|
| ✅ | 1 | Wired app.js init() to call ChordFlowPlayback.stopAll() on window `blur` (app loses focus) and `pagehide` (closing / navigating away), so playback is silenced when ChordFlow goes to the background or shuts down — reusing the same registry-wide stopAll() as the page toggle. | — | — | — |
---

### Legend

| Symbol | Meaning |
|--------|---------|
| ✅ | Done |
| 🔄 | In Progress |
| 🔳 | Pending |
| ❌ | Cancelled |
