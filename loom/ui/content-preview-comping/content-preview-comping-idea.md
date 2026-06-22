---
type: idea
id: id_01KVRK4WVV6QT6CGS397SRWNBN
title: Comping picker in the Content preview
status: draft
created: 2026-06-22
version: 1
tags: []
parent_id: null
requires_load: []
---
# Comping picker in the Content preview

## The idea

In **Content → progression / song**, let the user **pick the comping rhythm** used for the live preview.
Today the preview is hard-wired to a single pattern (`SeedData.Quarters` in `ContentCrudHandler`), so you
can't hear/see a progression or song with a real strum — the comping picker fixes that.

## Decided up front — page-level, NOT the shared score component

This came out of the triplet-feel discussion. Comping is **not** the same kind of knob as tempo / feel:

- It's a **content-selection** knob (it changes *which notes play*, and changing it **regenerates** — it
  is not a cheap render-only re-emit like `\tf` feel).
- Its options are **dynamic catalog content** (the rhythm library, fetched over the bridge `entityList`),
  not a fixed enum.

So it must **not** go into the shared `ScoreR` (`score-render-component.js`) — that component is
deliberately content-agnostic ("alphaTex in → notation out, no catalog knowledge"). The picker belongs on
the **Content page** (`content-crud.js`), which already reaches the rhythm catalog, sent on the
`entityPreview` request and forwarded through `ContentCrudHandler.Preview`. (Compare: feel *did* fit ScoreR
because it's a fixed-enum render directive — comping is the opposite on both counts.)

## Scope

- **Where:** `content-crud.js` preview toolbar — a comping `<select>` populated from the rhythm catalog,
  shown for **progression** and **song** previews. (Rhythm preview already *is* the rhythm under test, so no
  comping picker there; voicing is a diagram.)
- **Backend:** `entityPreview` carries the chosen `compingPatternId` → `WebMessageRouter` →
  `ContentCrudHandler.Preview`, which uses it instead of the hard-wired `SeedData.Quarters` for the
  progression/song preview Exercises.
- **Default:** `beat_1_3` (the app's default comping). Likely a **transient preview preference** (not
  persisted) — confirm in design.

## Open questions

- Does the chosen preview comping persist per session, or reset each time? (Lean transient.)
- Should the lead pattern eventually get the same treatment, or is comping enough for now?

Related: [[triplet-feel-chat-001]] (where this was scoped), `score-render-component`, content-crud.