---
type: req
id: rq_01KXT0S9CD5F9HEMG62CQQ1GJY
title: Minor key mode — thread through content preview, list seeding & loadExercise — Requirements
status: locked
created: 2026-07-18
updated: 2026-07-18
version: 1
design_version: 1
tags: []
parent_id: de_01KXT0K179D9EVX7CY1EE40WYN
requires_load: []
---
# Minor key mode — thread through content preview, list seeding & loadExercise — Requirements

### ✅ Included

- `IN1` **Tonality control in the content editor.** `content-crud.js` gains a **major/minor** control (progressions only) that **seeds** from the loaded item's `tonality`, so opening a minor progression shows minor. It is the single source of the editor's mode — ScoreR stays mode-free.
- `IN2` **Preview threads mode.** `requestPreview` sends `keyIsMinor` from the tonality control on the `entityPreview` message; the bridge → `ContentCrudHandler.Preview(bool keyIsMinor)` path already exists (first-class-minor-keys step 8a), so a minor preview emits `\ks {tonic}minor` (fixes the reported `\ks A`).
- `IN3` **Save persists tonality.** `entitySave` carries an optional explicit `tonality` (`"major"`/`"minor"`); `IContentStore.Save`/`ProgressionStore` serialize it into the stored catalog header when present — so **authoring a new minor progression** works and an in-editor major↔minor flip is written. Absent ⇒ the existing preserve-source-header behavior (the shipped `plan-001` fix) is unchanged.
- `IN4` **List payload surfaces mode.** `ContentSummary` gains `InitialKeyIsMinor` (peer of `InitialKey`/`DefaultFeel`/`DefaultTempo`), set from `CatalogMetadata.Tonality` for a progression and the Song key mode for a song; the `entityList` payload carries it. `harmony-controls-component.js` already reads `item.initialKeyIsMinor` — selecting a minor item auto-seeds minor mode.
- `IN5` **`loadExercise` carries mode.** The `loadExercise` reply carries `keyIsMinor` from `Exercise.KeyOverride` (which already round-trips `IsMinor`); the load path seeds it via the existing `hc.seedKeyMode`, so a saved minor exercise reopens minor and re-keying keeps mode.
- `IN6` **Goldens.** A store/handler test that authoring/editing a progression with the tonality control persists `tonality:` and reloads at the same mode; a test that the `entityList`/`loadExercise` payloads carry the mode; and a confirming golden that a multi-section Song in several keys/modes (via `key`/`mod`) realizes each section correctly.
- `IN7` **Ref sync.** Update `chordflow-dsl-reference.md` (the tonality control / `tonality:` authoring), `chordflow-domain-model-reference.md` (`ContentSummary.InitialKeyIsMinor`, the explicit-tonality save path), and the architecture ref if a bridge contract changes — each in the same unit of work as the code.

### ❌ Excluded

- `EX1` **Kernel / realization changes.** The C parent-major frame + `DegreeFrameConverter` from [[first-class-minor-keys]] is settled and untouched.
- `EX2` **Other diatonic modes.** Major + Minor only (per first-class-minor-keys EX3); Dorian…Locrian stay the growth path.
- `EX3` **A general metadata editor.** Only `tonality` gets a control; genre/subgenre/tags/description stay **preserve-only** (not user-edited in the CRUD), consistent with the content-crud thread's original EX3.
- `EX4` **A Song `tonality:` header.** A song's mode is its `key`/`mod` stream, not a header; a multi-key/multi-mode song is already expressible there (IN6 only *confirms* it).
- `EX5` **A ScoreR mode toggle.** Mode is a content-editor property, not a render-surface knob; ScoreR gains no major/minor control.

### ⛓ Constraints

- `C1` **Major regression invariant.** Every existing major-authored flow is byte-identical: an absent/`major` tonality serializes no header, storing the body verbatim; existing renders + tests unchanged.
- `C2` **No raw header text in the editor.** Tonality is edited only through the control — the `tonality:`/`genre:` lines never appear as editable text in the DSL box (upholds EX3's editor contract).
- `C3` **Preserve-source fallback intact.** When `entitySave` carries no explicit tonality, `Save` keeps the `plan-001` behavior (re-attach the in-place row's own header, else the fork-from source's via `sourceId`).
- `C4` **Progression-scoped acting.** Only `ProgressionStore` acts on an explicit tonality in v1; the other stores accept the param inertly (no behavior change).
- `C5` **Reuse existing seams.** Build on the delivered `keyIsMinor` bridge/`Preview` path (step 8a), `hc.seedKeyMode`, and the `item.initialKeyIsMinor` reader — do not rebuild them.
