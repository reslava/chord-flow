---
type: req
id: rq_01KVR8ADB19EGZ0JY0B4AZ8S26
title: Triplet Feel (\tf) — span/song-level swing — Requirements
status: locked
created: 2026-06-22
updated: 2026-06-22
version: 2
design_version: 2
tags: []
parent_id: de_01KVR89QNHC6NE2XHTJ6EM9MDQ
requires_load: []
---
# Triplet Feel (\tf) — span/song-level swing — Requirements

### ✅ Included

- `IN1` — Replace the `Feel` enum with **`TripletFeel`**, mirroring alphaTab's `TripletFeel` members. **Wire/offer** `None`, `Triplet8th`, `Triplet16th`; **define-but-don't-offer** `Dotted8th` (+ Scottish/`Dotted16th`) for a later add.
- `IN2` — `AlphaTexRenderer` emits a single whole-song **`\tf <value>`** as bar metadata on the **first bar of each track** (comping + lead), and **only when value ≠ `None`** (a `None` song emits no `\tf`).
- `IN3` — `AlphaTexRenderer` **stops calling `FeelTransform`** for swing (the warp leaves the alphaTex path); alphaTab owns render + playback swing.
- `IN4` — Keep the `FeelTransform` class (unchanged, unit-tested) for the future `IScoreRenderer` export seam; it is simply no longer invoked by `AlphaTexRenderer`.
- `IN5` — Rename the param through the stack: `Exercise.TripletFeel`, `ExerciseEntity.TripletFeel`, `GenerateRequest`, the bridge `ParseEnum` in `WebMessageRouter`, and the `app.js` `FEELS` list — all to the new vocabulary.
- `IN6` — Move the feel **control** out of the page builder (`index.html` / `app.js`) **into the `ChordFlowScore` component**, exposed as `getTripletFeel()` (parallel to `getTempo()`); the page reads it into the generate/save request.
- `IN7` — Changing tripletFeel triggers a **re-render** (content-kind) via the existing `onNeedsRerender` → host-replay seam — re-emit with the new `\tf`, harmony unchanged (no full regenerate).
- `IN8` — Verify `\tf`'s accepted **value spelling** (ident vs numeric) against the bundled `alphaTab.min.js`; emit the readable ident if confirmed, numeric otherwise.
- `IN9` — **Ref sync in the same unit of work:** add `\tf` to `alphatex-syntax-reference.md`; update `chordflow-domain-model-reference.md` (`Feel`→`TripletFeel`, `FeelTransform` out of the alphaTex path) and `chordflow-dsl-reference.md` (feel terminology + "no feel token in the grammar").
- `IN10` — Update the render tests that pin `FeelTransform` warping to instead assert the `\tf` line (and that `None` emits no `\tf`, byte-identical to today's straight output); add a bridge parse test for the new members.
- `IN11` — **Visually verify** in the running app: `Triplet8th` renders **swung notation** (not straight 8ths) and plays swung; flipping the control re-renders without a full regenerate.
- `IN12` — The feel control is **also available in the Content view's preview** (progression / song / rhythm): enable `ChordFlowScore`'s `tripletFeel` option in `content-crud.js` and thread the chosen feel through the `entityPreview` request → `WebMessageRouter.EntityPreviewRequested` → `ContentCrudHandler.Preview` → the renderer, so changing it re-previews swung. (Added after the Practice move — the relocated control should work wherever `ChordFlowScore` is used.)

### ❌ Excluded

- `EX1` — `Dotted8th`, `Dotted16th`, and the Scottish feels — defined in the enum but **not** wired or offered in the UI.
- `EX2` — **Per-section / per-bar** feel and any feel-authoring **grammar** in the Progression / Song / Rhythm DSL (would break `C1`).
- `EX3` — Any change to `{tu}` / `:3` **triplet** behavior — a distinct axis, left untouched.
- `EX4` — **Removing** `FeelTransform`.
- `EX5` — The separately-reported "`{tu}` triplets not rendering in the app" bug — its own small thread.

### ⛓ Constraints

- `C1` — **C4 preserved:** feel is a whole-song, **play-time** choice, never baked into a Progression / Song / Rhythm; no new grammar token.
- `C2` — `AlphaTexRenderer` stays the **only** alphaTex-aware code; `\tf` is emitted there, nowhere else.
- `C3` — `RenderOptions` stays **view-only / unpersisted**; tripletFeel rides as a first-class request field (like tempo), not folded into `RenderOptions`.
- `C4` — **Decision to confirm before code:** tripletFeel stays a **persisted** `Exercise` param (recommended — parity with tempo/difficulty). The rejected alternative is render-only + a drop-column migration.
- `C5` — A passing string assertion is **not** sufficient acceptance — swung rendering must be confirmed visually (`IN11`).
- `C6` — Solution builds and **all tests stay green**.
