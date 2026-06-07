---
type: plan
id: pl_01KTHKHV1JHQFMRMJWBG5HRZHA
title: Phase 2 — Desktop Shell, Rendering & Playback
status: active
created: 2026-06-07
version: 1
design_version: 1
req_version: 1
tags: []
parent_id: de_01KTHJD3QTBGRVX3BBRD29PKAW
requires_load: []
target_version: 0.1.0
---
# Phase 2 — Desktop Shell, Rendering & Playback

## Goal

Get exercises on screen and playing: stand up the Photino window hosting alphaTab JS, wire the narrow C#↔JS bridge, and achieve playback with a synchronized beat cursor. Satisfies req IN6, IN7, IN8; constraints C2, C6, C7, C8. Depends on Phase 1.
---

## Steps

| Done | # | Step | Files touched | Blocked by | Satisfies |
|---|---|---|---|---|---|
| 🔳 | 1 | Stand up the Photino host (PhotinoWindow loading wwwroot/index.html); scaffold wwwroot (index.html, app.js); bundle alphaTab.min.js and a small redistributable GM soundfont (confirm license). | — | — | — |
| 🔳 | 2 | Render a hardcoded alphaTex string end-to-end in the window via api.tex with player.enablePlayer/player.soundFont — proves the alphaTab integration before any bridging. | — | — | — |
| 🔳 | 3 | Build the C#<->JS bridge: WebMessageRouter + JSON envelopes (loadScore/play/stop out; ready/playbackFinished/beatChanged in); wire the GenerateExercise slice to push a real engine-produced score. | — | — | — |
| 🔳 | 4 | Add playback: play/stop/tempo controls; map alphaTab events (playerStateChanged -> playbackFinished, playedBeatChanged -> beatChanged) and confirm the synced beat cursor highlights in time. Verify the ⚠️ alphaTab API details against the installed version. | — | — | — |
---

### Legend

| Symbol | Meaning |
|--------|---------|
| ✅ | Done |
| 🔄 | In Progress |
| 🔳 | Pending |
| ❌ | Cancelled |
