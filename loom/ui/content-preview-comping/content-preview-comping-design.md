---
type: design
id: de_01KVSQ96H93G8GNAM6DHTBPYD4
title: Comping picker in the Content preview
status: done
created: 2026-06-23
updated: 2026-06-23
version: 2
tags: []
parent_id: id_01KVRK4WVV6QT6CGS397SRWNBN
requires_load: []
---
# Comping picker in the Content preview

## Goal

In **Content → progression / song**, let the user pick the **comping rhythm** used for the live
preview, replacing the hard-wired `SeedData.Quarters` in `ContentCrudHandler`. So you can hear/see a
progression or song with a real strum, not just block quarters.

## Decisions locked (from chat-001)

- **Transient** — the picker resets to the default on each page load; no persistence, no settings round-trip.
- **Comping-only** — the lead pattern does **not** get a picker here. Fold it in later if wanted.
- **Default `beat_1_3`** — the app's default comping. This **changes** the preview default from
  `SeedData.Quarters` → `beat_1_3` in both the progression and song preview paths.
- **Page-level, not `ScoreR`** — the picker lives on the Content page (`content-crud.js`), never in the
  content-agnostic `score-render-component.js`. (Settled in the idea; comping is a content-selection knob that
  *regenerates*, and its options are dynamic catalog content — the opposite of the fixed-enum `\tf` feel knob.)

## The change, end to end (5 touch points)

The whole feature is plumbing an id through an existing pipe — no new engine capability.

**1. `content-crud.js` — the picker + the envelope.**
   - Add a small **preview toolbar** (a new `cc-preview-toolbar` row above the score) holding a comping
     `<label>` + `<select>`. Shown **only** for `progression` / `song`; hidden for `rhythm` (the rhythm *is*
     the content under test) and `voicing` (a diagram).
   - `requestPreview()` adds `compingPatternId` (the picker's value, default `beat_1_3`) to the `entityPreview`
     envelope when the picker is visible; omitted otherwise (harmless if sent — the handler ignores it for
     rhythm/voicing).
   - On `<select>` change → call `requestPreview()` (same path `ScoreR`'s `onNeedsRerender` already uses for
     toggle/feel changes).
   - **Transient state** = just the `<select>`'s current value. No storage. Resets to `beat_1_3` each load.

**2. `content-crud.js` — populating the picker from the rhythm catalog.**
   - On entering `progression` / `song` (`selectEntity`), send `{type:"entityList", entity:"rhythm"}` to fetch
     the rhythm catalog for the picker (mirrors how `app.js` builds its workbench comping `<select>` from
     `catalog.rhythm`).
   - Needs a **carve-out in `onMessage`**: today `if (msg.entity !== current.key) return;` drops the rhythm
     list while the active entity is progression/song. Add, *before* that guard:
     ```js
     if (msg.type === "entityList" && msg.entity === "rhythm" && current.key !== "rhythm") {
       populateCompingOptions(msg.items || []); return;
     }
     ```
   - Populate options as `{value: it.id, label: it.name}`, default-select `beat_1_3` (fall through to first if
     absent — shouldn't happen with seed data).

**3. `WebMessageRouter` — carry the id.**
   - `InboundEnvelope` **already has `CompingPatternId`** (used by `generate`) — no new field.
   - Extend the `EntityPreviewRequested` event from `Action<string,string,RenderOptions,TripletFeel>` →
     `Action<string,string,RenderOptions,TripletFeel,string?>`, and pass `envelope.CompingPatternId` in the
     `entityPreview` dispatch arm.

**4. `Program.cs` — pass it through.**
   - `router.EntityPreviewRequested += (entity, dsl, renderOptions, tripletFeel, compingPatternId) => …
     contentCrud.Preview(entity, dsl, renderOptions, tripletFeel, compingPatternId) …` — one extra arg.

**5. `ContentCrudHandler.Preview` — resolve + feed.**
   - New optional param: `Preview(string entity, string dsl, RenderOptions? options = null, TripletFeel
     tripletFeel = TripletFeel.None, string? compingPatternId = null)`.
   - Resolve once inside the `using db` block via the existing seam:
     `RhythmPattern comping = ExerciseRefs.ResolvePattern(string.IsNullOrWhiteSpace(compingPatternId) ?
     "beat_1_3" : compingPatternId, db);`
   - Thread `comping` into `ProgressionPreview` and `SongPreview` in place of `SeedData.Quarters` (both become
     instance-or-parameterized to accept the pattern). **`RhythmPreview` is untouched** — it previews the bare
     rhythm on a single I chord, no comping. `VoicingPreview` untouched.

## The resolve step (the one new mechanic)

`compingPatternId` (a catalog id) → `RhythmPattern`, via **`ExerciseRefs.ResolvePattern(id, db)`** — the same
seam `generate` and the library-load path already use.

- **Blank / missing id** → fall back to `"beat_1_3"` (the locked default).
- **Non-blank id that doesn't resolve** → `ResolvePattern` throws `InvalidOperationException`, which
  `Preview`'s outer catch maps to a `FormatException` → `entityParseError` (the existing IN3 surface).
  **Fail-loud, consistent with generate/load.** This is an edge case only reachable by a deleted-rhythm race,
  since the picker is populated from the live catalog.

## What does NOT change

- **`score-render-component.js` (`ScoreR`)** — stays content-agnostic. The picker is outside it.
- **`RhythmPreview` / `VoicingPreview`** — no comping.
- **The `generate` / save / library paths** — already carry `CompingPatternId`; this only touches the
  *preview* path, which was the one place still hard-wiring `SeedData.Quarters`.

## Risks / watch-items

- **The `onMessage` carve-out** is the only subtle bit: a targeted exception to the "ignore other entities'
  messages" rule, scoped narrowly to the rhythm catalog feeding the comping picker. Keep it to that one case.
- **Default flip** (`Quarters` → `beat_1_3`) is intentional and aligns the preview with the app default — note
  it so a reviewer doesn't read it as a regression. No test asserts the old `Quarters` preview default (the
  preview builders are private), but confirm during implementation.

## Validation

- Manual: Content → Progression, type `17 47 17 57`, pick a few comping patterns from the picker → the preview
  re-renders and **plays** with the chosen strum. Same for a Song.
- Rhythm and Voicing tabs show **no** comping picker.
- An unknown/deleted comping id surfaces as an inline parse error, not a crash.
- *(The guitar-weave fretboard dogfood rule does not apply — this is a score-preview UI knob, not a new
  fretboard/engine capability.)*

Related: [[content-preview-comping-idea]], [[triplet-feel-chat-001]] (where comping-vs-ScoreR was scoped),
`content-crud`, `score-render-component`.