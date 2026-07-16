---
type: done
id: pl_01KXMZ37JDP9V055BEPJ4P0P1H-done
title: Done — Stop all playback on page change
status: done
created: 2026-07-16
version: 1
tags: []
parent_id: pl_01KXMZ37JDP9V055BEPJ4P0P1H
requires_load: []
---
# Done — Stop all playback on page change

JS-only change in wwwroot. Design: registry inside the engine layer (self-registering) beats per-view onHide hooks because a new sound surface needs no wiring to be covered. Runtime audio-stop confirmed by inspection (registry add/remove + stopAll iterating .stop()); best smoke-tested live in the app (play a score → switch page → sound stops). Scope kept to the in-app nav path; a window blur/close (visibilitychange/beforeunload) stop-all remains an easy follow-up on the same stopAll() if wanted.
