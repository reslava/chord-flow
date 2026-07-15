---
type: chat
id: ch_01KXKB243T7QT1QSHS6H3KDFQB
title: chord-sheets-playback update doc Chat 002
status: done
created: 2026-07-15
updated: 2026-07-15
version: 1
tags: []
parent_id: null
requires_load: []
last_ai_block: 2
---
# chord-sheets-playback update doc Chat 002

## Rafa:

1. update `loom/refs/chordflow-architecture-reference.md` to include new
`ChordFlowPlayback`  
`ChordSheetR`

and `ScoreR` (become thinner)

2. include `Chord Sheets` feature in main README

agree?
if so
write quick

## AI:

Agree. Quick note: the architecture ref already gained the `ChordFlowPlayback` seam paragraph + the `chordSheetResult`/marker note during the build — so this is topping it off: add **`chord-sheet-render-component` (ChordSheetR)** to the UI dumb-views box (it's currently missing from the list) and make the **ScoreR-becomes-thinner** framing explicit there, plus a **Chord Sheets** feature line in the README. Doing it now (`write quick` — docs only, read-through is the check).

(Also: sorry for the mix-up — I replied to the *maker's* chat-002 by mistake a moment ago. Harmless closing note, but say if you want it removed.)

## AI:

Done (`write quick`, recorded as plan-005):

- **Architecture ref** — added `chord-sheet-render-component` (ChordSheetR) to the UI dumb-views box (it was missing), and made the **ScoreR → thinner notation-only layer** framing explicit in the `ChordFlowPlayback` seam paragraph. (`ChordFlowPlayback` + the marker/`chordSheetResult` notes were already added during the build.)
- **README** — the **Chord Sheets** feature is retitled "print *and play along with* your songs" and gains a play-along sentence: a marker follows the music in time (**Visual metronome** / **Per chord**), driven by the same synchronized playback engine as the tablature view.

Docs-only, so no build/test — read-through checks out. Not committed (you didn't ask); say the word and I'll commit these along with anything else pending.