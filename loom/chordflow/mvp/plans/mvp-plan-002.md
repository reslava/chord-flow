---
type: plan
id: pl_01KTHKHV1JHQFMRMJWBG5HRZHA
title: Phase 2 — Desktop Shell, Rendering & Playback
status: done
created: "2026-06-07T00:00:00.000Z"
updated: "2026-06-08T00:00:00.000Z"
version: 2
design_version: 1
req_version: 1
tags: []
parent_id: de_01KTHJD3QTBGRVX3BBRD29PKAW
requires_load: []
target_version: 0.1.0
steps:
  - id: stand-up-the-photino-host-photinowindow
    order: 1
    status: done
    description: Stand up the Photino host (PhotinoWindow loading wwwroot/index.html); scaffold wwwroot (index.html, app.js); bundle alphaTab.min.js and a small redistributable GM soundfont (confirm license).
    files_touched: [src/ChordFlow.App/Program.cs, src/ChordFlow.App/wwwroot/index.html, src/ChordFlow.App/wwwroot/app.js, src/ChordFlow.App/wwwroot/alphaTab.min.js, "src/ChordFlow.App/wwwroot/soundfont/*.sf2"]
    blocked_by: []
    satisfies: [IN6, C2, C6, C7]
  - id: render-a-hardcoded-alphatex-string-end
    order: 2
    status: done
    description: Render a hardcoded alphaTex string end-to-end in the window via api.tex with player.enablePlayer/player.soundFont — proves the alphaTab integration before any bridging.
    files_touched: [src/ChordFlow.App/wwwroot/app.js, src/ChordFlow.App/wwwroot/index.html]
    blocked_by: [1]
    satisfies: [IN7, C6, C8]
  - id: build-the-c-js-bridge-webmessagerouter
    order: 3
    status: done
    description: "Build the C#<->JS bridge: WebMessageRouter + JSON envelopes (loadScore/play/stop out; ready/playbackFinished/beatChanged in); wire the GenerateExercise slice to push a real engine-produced score."
    files_touched: [src/ChordFlow.App/Infrastructure/WebMessageRouter.cs, src/ChordFlow.App/Infrastructure/PhotinoBridge.cs, src/ChordFlow.App/Features/GenerateExercise/GenerateExercise.cs, src/ChordFlow.App/wwwroot/app.js]
    blocked_by: [2]
    satisfies: [IN7, IN8]
  - id: add-playback-play-stop-tempo-controls
    order: 4
    status: done
    description: "Add playback: play/stop/tempo controls; map alphaTab events (playerStateChanged -> playbackFinished, activeBeatsChanged -> beatChanged) and confirm the synced beat cursor highlights in time. Verify the ⚠️ alphaTab API details against the installed version."
    files_touched: [src/ChordFlow.App/wwwroot/app.js, src/ChordFlow.App/Features/PracticeSession/PracticeSession.cs, src/ChordFlow.App/Infrastructure/WebMessageRouter.cs]
    blocked_by: [3]
    satisfies: [IN7, IN8, C8]
---
# Phase 2 — Desktop Shell, Rendering & Playback

## Goal

Get exercises on screen and playing: stand up the Photino window hosting alphaTab JS, wire the narrow C#↔JS bridge, and achieve playback with a synchronized beat cursor. Satisfies req IN6, IN7, IN8; constraints C2, C6, C7, C8. Depends on Phase 1.
---

## Steps

| Done | # | Step | Files touched | Blocked by | Satisfies |
|---|---|---|---|---|---|
| ✅ | 1 | Stand up the Photino host (PhotinoWindow loading wwwroot/index.html); scaffold wwwroot (index.html, app.js); bundle alphaTab.min.js and a small redistributable GM soundfont (confirm license). | src/ChordFlow.App/Program.cs, src/ChordFlow.App/wwwroot/index.html, src/ChordFlow.App/wwwroot/app.js, src/ChordFlow.App/wwwroot/alphaTab.min.js, src/ChordFlow.App/wwwroot/soundfont/*.sf2 | — | IN6, C2, C6, C7 |
| ✅ | 2 | Render a hardcoded alphaTex string end-to-end in the window via api.tex with player.enablePlayer/player.soundFont — proves the alphaTab integration before any bridging. | src/ChordFlow.App/wwwroot/app.js, src/ChordFlow.App/wwwroot/index.html | 1 | IN7, C6, C8 |
| ✅ | 3 | Build the C#<->JS bridge: WebMessageRouter + JSON envelopes (loadScore/play/stop out; ready/playbackFinished/beatChanged in); wire the GenerateExercise slice to push a real engine-produced score. | src/ChordFlow.App/Infrastructure/WebMessageRouter.cs, src/ChordFlow.App/Infrastructure/PhotinoBridge.cs, src/ChordFlow.App/Features/GenerateExercise/GenerateExercise.cs, src/ChordFlow.App/wwwroot/app.js | 2 | IN7, IN8 |
| ✅ | 4 | Add playback: play/stop/tempo controls; map alphaTab events (playerStateChanged -> playbackFinished, activeBeatsChanged -> beatChanged) and confirm the synced beat cursor highlights in time. Verify the ⚠️ alphaTab API details against the installed version. | src/ChordFlow.App/wwwroot/app.js, src/ChordFlow.App/Features/PracticeSession/PracticeSession.cs, src/ChordFlow.App/Infrastructure/WebMessageRouter.cs | 3 | IN7, IN8, C8 |
---

### Legend

| Symbol | Meaning |
|--------|---------|
| ✅ | Done |
| 🔄 | In Progress |
| 🔳 | Pending |
| ❌ | Cancelled |