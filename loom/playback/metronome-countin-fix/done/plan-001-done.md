---
type: done
id: pl_01KXKTH18XHXZV1X39353WAH7W-done
title: Done — Metronome/count-in regression fix + chord-sheet dark-tab fix + CHORDFLOW_DEVTOOLS debug facility
status: done
created: 2026-07-15
version: 1
tags: []
parent_id: pl_01KXKTH18XHXZV1X39353WAH7W
requires_load: []
---
# Done — Metronome/count-in regression fix + chord-sheet dark-tab fix + CHORDFLOW_DEVTOOLS debug facility

Quick-shipped — recorded already-completed work:

1. Root-caused the metronome/count-in silence to a swallowed `ReferenceError: syncToggle is not defined` in ScoreR.setOption (score-render-component.js): every toggle change threw before reaching engine.setMetronome/setCountIn. Bisected the drop to commit aadd147 (the ChordFlowPlayback extraction, which deleted the helper but kept both call sites); ruled out the JS wiring, the alphaTab v1.8.3 bundle, the sonivox.sf2 soundfont (verified it maps the drum kit + metronome key 33), and the player settings as all unchanged.
2. Restored the dropped `syncToggle` helper verbatim in score-render-component.js, so setOption falls through to engine.setMetronome/setCountIn again — also repairs the diagrams-on-top -> auto-enable chord-names coupling that shared the helper.
3. Fixed the unreadable 'Show tab' view on the Chord Sheets page by giving its alphaTab surface the `cf-score-surface` class (white bg / black ink), matching ScoreR — previously alphaTab's dark notation rendered on the dark page.
4. Added a default-off live-WebView debug facility gated by the CHORDFLOW_DEVTOOLS env var (Program.cs enables WebView2 devtools + injects window.__cfDebug; score-render-component.js gates window.__cfApi/__cfEngine), and documented it in loom/ctx.md so future sessions use it instead of re-adding temp debug lines.
