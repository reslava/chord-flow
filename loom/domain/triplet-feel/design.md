---
type: design
id: de_01KVR89QNHC6NE2XHTJ6EM9MDQ
title: Triplet Feel — delegate whole-song swing to alphaTex \tf
status: done
created: 2026-06-22
updated: 2026-06-22
version: 2
idea_version: 2
tags: []
parent_id: id_01KVQGK9R3CNMJZRZVN3V65SJB
requires_load: []
---
# Triplet Feel — delegate whole-song swing to alphaTex \tf

## Goal

Replace our self-computed playback swing (`FeelTransform` tick-warp) with alphaTab's native **`\tf`**
directive for a **whole-song, play-time** feel. Two wins: the **score reads correctly** (swung notation
instead of straight-looking 8ths that merely *play* swung), and **authoring stays simple** — write plain
8ths and let `\tf` swing them. Rename the `Feel` model to alphaTab's `TripletFeel` vocabulary, keep feel a
play-time choice (**C4 intact — no new grammar**), and move the control into the shared `ChordFlowScore`
component.

## Grounding (confirmed in chat — nothing musical is open)

- **The combo is already wired** end-to-end today: `app.js $("feel")` → `WebMessageRouter` →
  `Exercise.Feel` → `AlphaTexRenderer.Render(…, feel)` → `WarpBars` → `FeelTransform.Apply`. It only
  *felt* dead because (a) it warps **playback only** — the notation stays straight — and (b)
  `FeelTransform` only swings the **off-beat 8th**, so the default quarter-note comping (`beat_1_3`) shows
  no audible difference for any Feel.
- **`\tf triplet8th` on a straight 8th pair ≡ `3: X.X`** (first note 2/3 of the beat, second 1/3). So
  `3: X.X` becomes redundant under `\tf`; explicit `:3` triplets are still needed only for figures `\tf`
  can't synthesize from a 2-note pair: `3: XXX`, `3: .XX`, `3: XX.`, and `3: X.X` **when the global feel
  is `none`**.
- **Coexistence:** `\tf` reshapes only straight **8th/16th pairs**; an explicit `:3` triplet beat is
  already a 3-note tuplet with no straight pair to warp, so `\tf` leaves it alone. `\tf` and `{tu 3}` nest
  in one bar with no double-swing — confirming the idea-doc note that they aren't mutually exclusive.

## Decisions

### 1. Delegate to `\tf`; retire the warp from the alphaTex path

`AlphaTexRenderer` emits a `\tf <value>` directive and **stops calling `FeelTransform`** (`WarpBars`
becomes identity / is removed from the comping + lead bar builders). alphaTab owns both render and
playback swing. Keeping *both* paths would double-swing, so this is a replacement, not a coexistence.

### 2. `Feel` → `TripletFeel` (alphaTab vocabulary)

Replace the `Feel { Straight, Swing, Shuffle, Triplet }` enum with **`TripletFeel`** mirroring alphaTab's
`TripletFeel` members. **Wire now:** `None`, `Triplet8th`, `Triplet16th`. **Define-but-don't-offer:**
`Dotted8th` (+ the Scottish/Dotted16th members) — present in the enum for a clean later add, not in the
UI list. One vocabulary, no lossy `Swing→?` mapping. This is a **breaking rename** across `Exercise`,
`ExerciseEntity`, the bridge parse, and `app.js` `FEELS` — accepted (no back-compat contortion).

### 3. Whole-song, play-time only — C4 preserved

Feel is a **single choice applied to the whole song at play time**, never written into a
Progression / Song / Rhythm. So **no new grammar token** in any DSL — the original "new DSL in
Progression/Song/Rhythm" TODO dissolves. C4 ("feel never baked into content; chosen at play time") stays
literally true; per-section feel (the only thing that would force feel into the grammar) is **out of
scope** and, if ever needed, becomes its own thread with its own C4 conversation.

### 4. The control moves into `ChordFlowScore` — tripletFeel is "tempo's twin"

Once feel is a pure render directive it's the same *kind* of knob as tempo, which already lives in the
component. Move the `Feel` select out of the page builder toolbar (`index.html` / `app.js`) into the
component's transport. tripletFeel becomes a **component-owned value** with a `getTripletFeel()` accessor
(parallel to `getTempo()`); the page reads it into the generate/save request just as it reads
`getTempo()`. The dividing line: **component = render/playback knobs** (tempo, render options,
tripletFeel); **page = content-selection knobs** (harmony, key, difficulty — they change which notes
exist).

**One difference from tempo:** changing tripletFeel changes the alphaTex string (the `\tf` line), so it is
**content-kind** — a change must trigger a cheap **re-render** (re-emit with the new `\tf`, harmony
unchanged), reusing the existing `onNeedsRerender` → host-replay seam (the same path chord-diagram toggles
already use). Tempo, by contrast, is applied locally (`api.playbackSpeed`) with no re-render. Keep
`RenderOptions` itself **view-only/unpersisted** as today — tripletFeel rides as a first-class request
field (like tempo), not folded into `RenderOptions`.

### 5. tripletFeel stays a **persisted Exercise param** (recommended — confirm)

Today `Feel` sits beside `Tempo`/`Difficulty` on `Exercise` / `ExerciseEntity` as a "saved default,
applied render-time" param. Keep that: rename in place to `TripletFeel`, still persisted. A saved swung
blues remembers it's swung, exactly as it remembers its tempo. The control's *location* moving to the
component doesn't change whether the *value* is saved.

- **Rejected alternative:** make tripletFeel a transient render-only preference and **drop**
  `ExerciseEntity.Feel` (+ a drop-column migration). Cleaner "component owns it" story, but it's a real
  behavior change (saved exercises forget their swing) and breaks parity with tempo/difficulty. Not worth
  it — but this is the **one decision to sign off** before code.

## Emission detail (verify before/at implementation)

- **Placement:** `\tf` is **bar metadata** (like `\ts`/`\ks`/`\ac`) at the **start of the first bar's
  content**. For whole-song feel, emit it once on the **first bar of each track** (comping and lead) so
  both staves swing — *verify* `\tf` is per-track and that one emission covers the whole track. Emit
  **only when value ≠ None** (a `None` song emits no `\tf`, byte-identical to today's Straight output).
- **Value spelling:** `\tf` is **not yet in `alphatex-syntax-reference.md`** and its accepted ident
  spelling (`none` / `triplet8th` / `triplet16th` vs the enum-cased members vs the numeric form) must be
  **verified against the bundled `alphaTab.min.js`**. Prefer the readable **ident** if confirmed; the
  **numeric** form (`\tf 0/2/1`) is the guaranteed fallback. Document whichever we emit in the ref.

## `FeelTransform` — kept, but unused by the alphaTex path

Don't delete it. It's pure and unit-tested, and the `IScoreRenderer` seam has a **real future consumer**:
a MIDI / GuitarPro exporter has no alphaTab to swing playback and would need to bake the groove into ticks
itself. `AlphaTexRenderer` simply stops calling it. (Keep-vs-delete is reversible; keeping costs nothing.)

## Reference sync (same unit of work — mandatory)

- **`alphatex-syntax-reference.md`** — add `\tf` (bar metadata; the values we emit; applies until the next
  `\tf` or song end; per-track placement) — now a *verified* directive.
- **`chordflow-domain-model-reference.md`** — `Feel` → `TripletFeel` (new members + which are wired);
  `FeelTransform` is no longer in the alphaTex render path (only the future export seam); C4 wording stays
  but now realized via `\tf`.
- **`chordflow-dsl-reference.md`** — feel terminology note; state explicitly that there is **no** feel
  token in the Progression/Song/Rhythm grammar (feel is a play-time param).

## Tests

- Update the render tests that assert `FeelTransform` warping to instead assert the emitted **`\tf`** line
  (and that a `None` song emits **no** `\tf`, byte-identical to today's straight output).
- `FeelTransform`'s own unit tests stay green (the class is unchanged) — just no longer exercised via the
  renderer.
- Bridge/parse test: an envelope feel string parses to the new `TripletFeel` members.

## Validation

- Solution builds; all tests green.
- alphaTex ref documents `\tf`; domain + DSL refs updated in the same unit of work.
- **Visual dogfood (required):** run the app, pick `Triplet8th`, confirm alphaTab renders **swung
  notation** (not straight 8ths) and plays swung — a string assertion alone doesn't prove the rendered
  result. Confirm flipping the control **re-renders** without a full regenerate.

## Out of scope

- `Dotted8th` + Scottish/`Dotted16th` feels (defined in the enum, **not** wired/offered yet).
- **Per-section / per-bar** feel and any feel-authoring grammar in Progression/Song/Rhythm.
- Any change to `{tu}` / `:3` triplet behavior (a distinct axis — unchanged).
- **Removing** `FeelTransform`.
- The separately-reported "`{tu}` triplets not rendering in the app" bug — chase as its own small bug.

Related: [[triplet-feel-idea]], `chordflow-dsl-reference`, [[chordflow-domain-model-reference]],
`alphatex-syntax-reference`.