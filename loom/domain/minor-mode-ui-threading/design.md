---
type: design
id: de_01KXT0K179D9EVX7CY1EE40WYN
title: Minor key mode — thread through content preview, list seeding & loadExercise
status: done
created: 2026-07-18
version: 1
idea_version: 1
tags: []
parent_id: id_01KXSW3V26WPTK3D59E5TJC94D
requires_load: []
---
# Minor key mode — thread through content preview, list seeding & loadExercise

## 0. Correction since the idea (read this first)

The idea framed the core work as *"promote `Home` from an informal tag to a first-class field."* Reading the code corrected that premise:

- The **`tonality:` catalog-header field already exists and is fully wired.** `CatalogHeader` parses/serializes it (fail-loud `major`/`minor`), the minor default pack authors `tonality: minor` tonic-relative (`1- 4- 5-`), and `ProgressionStore.Find` threads it into realization as the parser's `Home`. So minor progressions already realize correctly at the model layer.
- The **fork/edit-drops-`tonality:` correctness bug** the investigation surfaced is **already fixed** (plan-001, done): `Save` now re-attaches the source's catalog header (via a `sourceId`) instead of stripping it.

So this design is **not "build a field"** — it is **surface the existing tonality through the CRUD / list / preview / reload surfaces that still ignore it**, so a minor key is first-class everywhere, not just on the Practice generate path.

## 1. Decisions

### 1a. A tonality control in the content editor — seeded from content, drives preview + save

This supersedes the earlier chat "(a) ScoreR toggle vs (b) derive from content" framing. The clean answer once you see that **tonality is a *content* property** (a progression's `Home`), not a render knob:

- The **content editor** (`content-crud.js`), not ScoreR, gains a **major/minor tonality control** — shown for progressions (songs get mode from the Song `key`/`mod` stream; rhythm/voicing have no tonality).
- It **seeds** from the loaded item's `tonality` (so opening a minor progression shows minor).
- It **drives the live preview** — `requestPreview` sends `keyIsMinor` = the control's value → `\ks Aminor` (fixes the reported `\ks A`).
- It is **written on save** — `entitySave` carries an explicit `tonality`; the store serializes it into the header (this is what finally makes **authoring a new minor progression** possible). Absent ⇒ preserve the source header (the plan-001 behavior).

ScoreR stays mode-free (a pure render surface); the editor owns the content property. EX3's "no raw header text in the box" is preserved — tonality is a first-class control, not an exposed `tonality:` line.

### 1b. List payload surfaces tonality → the harmony controls auto-pick minor

`ContentSummary` gains `InitialKeyIsMinor` (peer of the existing `InitialKey`/`DefaultFeel`/`DefaultTempo`), set from `CatalogMetadata.Tonality` for a progression, and from the Song's own key mode for a song. `harmony-controls-component.js` **already reads** `item.initialKeyIsMinor` (line ~222) — this is the missing source. Selecting a minor song/progression on Practice then auto-seeds minor mode.

### 1c. `loadExercise` reply carries the exercise's key mode

`Exercise.KeyOverride` already round-trips `IsMinor` (first-class-minor-keys step 3). The `loadExercise` reply should carry it, and the load path should seed it via `hc.seedKeyMode` (already exists) so a saved minor exercise reopens minor, and re-keying keeps mode.

## 2. Threading details

- **Save (extend plan-001):** `entitySave` gains an optional `tonality` (`"major"`/`"minor"`); `IContentStore.Save` / `ProgressionStore` use it when present (serialize into the header, overriding the preserved source), else keep the preserve-source behavior. Only `ProgressionStore` acts on it in v1.
- **Preview:** `content-crud.js` `requestPreview` adds `keyIsMinor` from the tonality control; the bridge `entityPreview` → `ContentCrudHandler.Preview(keyIsMinor)` path already exists (step 8a) — the JS caller is the only gap.
- **List:** `ContentSummaries.Build` / each store's `List` projection surfaces `InitialKeyIsMinor`; the `entityList` payload carries it; `app.js`/`content-crud.js` pass it into the seeds the harmony controls read.
- **loadExercise:** the reply envelope carries `keyIsMinor`; `app.js` calls `hc.seedKeyMode(msg.keyIsMinor)` alongside the existing `hc.seedKey(msg.key)`.

## 3. Non-goals

- No kernel/realization change — the C frame + converter is settled and untouched.
- Not the other diatonic modes (Major + Minor only, per first-class-minor-keys EX3).
- No metadata editor beyond the tonality control (genre/tags/description stay preserve-only, EX3).
- Songs don't get a `tonality:` header — their mode is the `key`/`mod` stream; a multi-section song in several keys/modes is already expressible there (worth a golden, not new modeling).

## 4. Validation / dogfood

- Author a new minor progression in Content (toggle → minor) → preview emits `\ks Aminor`, spells from the relative-major table; Save persists `tonality: minor`; reload shows the toggle at minor.
- Edit an existing minor progression → toggle seeds minor; a major↔minor flip re-realizes live.
- Select a minor song/progression on Practice → the Key control auto-picks minor from the list payload.
- Save a minor exercise, reload → it reopens minor (key + mode); re-key keeps mode.
- Regression: every major flow is byte-identical (absent tonality ⇒ no header ⇒ verbatim body).
