---
type: done
id: pl_01KXT6N3XRQ2ARA4GPWA375QB7-done
title: "Done — Fix: content preview honors the tonality control as parse Home (minor chords)"
status: done
created: 2026-07-18
version: 1
tags: []
parent_id: pl_01KXT6N3XRQ2ARA4GPWA375QB7
requires_load: []
---
# Done — Fix: content preview honors the tonality control as parse Home (minor chords)

Quick-shipped — recorded already-completed work:

1. Fixed the content-editor preview realizing minor progressions with the wrong chords (Cm/Fm/Gm instead of Am/Dm/Em) even though `\ks` was correct. `ContentCrudHandler.ProgressionPreview` derived the parse `Home` from the DSL's `tonality:` header, but the editor strips the header (EX3) and sends the mode via the tonality control (carried on the preview key). It now takes `Home` from `liftKey.IsMinor` (the control), falling back to the header only when the key has no minor opinion. Added a golden asserting the header-stripped body + keyIsMinor renders identically to the headered version (`Preview_MinorProgression_HeaderStripped_StillRendersMinor`). Full solution builds; Core suite green at 1019.
