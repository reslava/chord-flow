---
type: done
id: pl_01KVSR3075H7FYRHH52533Y2F6-done
title: Done — content-preview-comping Plan
status: done
created: 2026-06-23
version: 5
tags: []
parent_id: pl_01KVSR3075H7FYRHH52533Y2F6
requires_load: []
---
# Done — content-preview-comping Plan

## Step 1 — Resolve a comping id in ContentCrudHandler.Preview and feed it to the progression/song builders

`ContentCrudHandler.Preview` gained a trailing optional `string? compingPatternId = null`. A private `ResolveComping(compingPatternId, db)` resolves via `ExerciseRefs.ResolvePattern(blank ? "beat_1_3" : id, db)`; its result is threaded into `ProgressionPreview` (now takes a `RhythmPattern`) and `SongPreview` (now takes a `RhythmPattern`) in place of the hard-wired `SeedData.Quarters`. `RhythmPreview`/`VoicingPreview` untouched. Default flips Quarters → beat_1_3 (intentional, aligns preview with the app default). An unknown non-blank id throws from `ResolvePattern` → caught by `Preview`'s outer catch → `FormatException` → `entityParseError` (IN6).

## Step 2 — Widen EntityPreviewRequested and pass envelope.CompingPatternId through the router and Program wiring

`WebMessageRouter.EntityPreviewRequested` widened to `Action<string,string,RenderOptions,TripletFeel,string?>`; the `entityPreview` dispatch arm now passes `envelope.CompingPatternId` (the field already existed for `generate` — no new envelope field, C3). `Program.cs` subscriber takes the extra `compingPatternId` arg and forwards it to `contentCrud.Preview(...)`. XML-doc tuple comment updated.

## Step 3 — Add the comping picker to content-crud.js: toolbar select, catalog fetch, envelope field, re-preview on change

`content-crud.js`: `comping: true` flag added to the progression/song ENTITIES configs. New `cc-preview-toolbar` row (label + `#ccComping` select) above the preview, shown only when `current.comping`. `selectEntity` fetches the rhythm catalog (`entityList entity=rhythm`) when comping is supported. `onMessage` carve-out captures a rhythm `entityList` (when the active entity isn't rhythm) → `populateCompingOptions`, which keeps the prior pick if still present else defaults to `beat_1_3` else first. `requestPreview` adds `compingPatternId` (select value, default `beat_1_3`) when the picker is visible; the select's `change` re-previews. Transient = the select value only, no persistence. CSS for `.cc-preview-toolbar` went into `index.html` (where the other `cc-` styles live), not `styles.css` as the plan tentatively listed.

## Step 4 — Tests — entityPreview carries the id; Preview resolves it, falls back to beat_1_3, and fails loud on a bad id

Router tests (`WebMessageRouterContentTests`): the 3 existing `EntityPreviewRequested` subscriptions updated to the 5-arg arity; added `EntityPreview_CarriesCompingPatternId` and `EntityPreview_AbsentCompingPatternId_IsNull`. Handler tests (`ContentCrudHandlerTests`, default-pack db): `Preview_Progression_UsesChosenComping` + `Preview_Song_UsesChosenComping` (beat_1_3 vs quarters render differently), `Preview_BlankComping_DefaultsToBeat1And3` (IN5), `Preview_UnknownComping_ThrowsFormatException` (IN6). Full suite green: 641 passed.

## Step 5 — Manual dogfood + sync the bridge contract note in the architecture reference

**Ref-sync done:** `chordflow-architecture-reference.md` §5 now documents that `entityPreview` carries an optional `compingPatternId` (Content-page picker, populated from the rhythm `entityList`, resolved via `ExerciseRefs.ResolvePattern`, blank ⇒ beat_1_3, unknown ⇒ entityParseError) replacing the old hard-wired `SeedData.Quarters`, and that the picker is transient. Desktop host + full test suite build clean. **Manual dogfood PENDING Rafa:** open the app → Content → Progression (`17 47 17 57`) and a Song, pick several comping patterns → preview re-renders and *plays* the chosen strum; confirm rhythm/voicing show no picker and an unknown/deleted id surfaces inline (not a crash). Step left open until that human playback check passes. (Fretboard dogfood rule N/A — score-preview knob, not a fretboard capability.)
