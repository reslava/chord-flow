---
type: done
id: pl_01KXMZD01HKZN8YCYBFKMNHQCE-done
title: Done — Stop all playback on window blur / close
status: done
created: 2026-07-16
version: 1
tags: []
parent_id: pl_01KXMZD01HKZN8YCYBFKMNHQCE
requires_load: []
---
# Done — Stop all playback on window blur / close

Follow-up to plan-001 (stop on page change). JS-only in wwwroot; solution builds clean, C# suite unaffected (no C# change). `blur` = focus loss, `pagehide` = reliable close/navigate signal. Best smoke-tested live: play a score → Alt-Tab away / close → sound stops.
