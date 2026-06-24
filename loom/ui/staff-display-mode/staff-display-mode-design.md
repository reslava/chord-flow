---
type: design
id: de_01KVX5ZNHEZKPEHGC1DYA84MQ0
title: Tab-only staff display mode
status: done
created: 2026-06-24
updated: 2026-06-24
version: 2
tags: []
parent_id: id_01KVW7Z0VRK9K8T06CED28T8S6
requires_load: []
---
# Tab-only staff display mode

## Goal

Give the score view a **three-way staff-display control** — **tab** (default) / **standard** (notation only) /
**both** (combined) — a pure **presentation** choice over unchanged content, persisted as a global preference so it
**survives restart** (`IN2`/`IN4`/`IN6`/`IN7`).

## Decisions locked (from chat-001)

- **Option B — display-only, JS-side** (`C5`/`C6`). The profile is the two per-staff alphaTab model flags
  **`showStandardNotation` / `showTablature`**; `ScoreR` sets them and calls `api.render()`. **Zero Core change** —
  no `AlphaTexRenderer`, no `RenderOptions`, no `\staff` directive. This is the **`barsPerRow` / "Auto layout"**
  precedent: a display/layout concern handled locally, not a content-kind re-render.
  - *Rejected — Option A (emit `\staff {tabs}` from Core, content-kind re-render):* would round-trip a show/hide
    through the engine and add a `RenderOptions` field; heavier and puts a presentation choice in the UI-agnostic
    kernel. Kept here only as the rationale for `C1 ~dropped`.
- **Three states**, not a binary toggle: `tab` (default) / `standard` / `both`.
- **Global persistence** via the existing `AppSettingsStore`, over a bridge verb pair **mirroring the soundfont
  choice** (`C2`).

## Profile → flags

| Profile     | `showStandardNotation` | `showTablature` |
|-------------|------------------------|-----------------|
| `tab` (default) | false | true  |
| `standard`      | true  | false |
| `both`          | true  | true  |

`both` is today's effective render (alphaTab's default when no `\staff` is emitted), so it stays byte-identical
(`IN5`). The default flips from the implicit `both` → **`tab`** (`IN2`) — intentional, call it out so a reviewer
doesn't read the hidden standard staff as a regression.

## The change, end to end

### 1. `score-render-component.js` (`ScoreR`) — the control + apply (`IN6`/`IN7`, `C5`/`C6`)

- **Control:** a staff-profile `<select>` (Tab / Standard / Both) in the toolbar, built in `buildControls`
  alongside the existing toggles. It is a **local/display-only** option — it never enters `renderOptions` and never
  fires `onNeedsRerender` (same class as the soundfont picker and the Auto-layout toggle).
- **`applyStaffProfile(profile)`:** for every `staff` in every `api.score.tracks[*].staves[*]`, set the two flags
  per the table, then trigger a re-layout via the **same render path `scoreLoaded` uses** (`api.renderTracks(api.score.tracks)`
  for a multi-track score, else `api.render()`). No `updateSettings()` — these are score-model flags, not settings.
- **Re-assert on `scoreLoaded`:** alphaTab rebuilds the score model on every load, so the profile must be re-applied
  there (the exact pitfall already handled for `globalDisplayChordDiagramsOnTop` and per-track volumes). Set the
  flags **inside** the existing `scoreLoaded` handler, before its `renderTracks`, so load and toggle share one path
  and a freshly loaded score doesn't flash the default profile.
- **Boot:** on init `ScoreR` requests the persisted value (verb below); the reply sets the `<select>` and calls
  `applyStaffProfile`. On user change → `applyStaffProfile(value)` locally **and** send the persist verb — the exact
  `applySoundFont` + `setSoundFont` shape.

### 2. Bridge + Core persistence — mirror the soundfont pair (`C2`/`C3`)

- **Inbound verbs:** `getStaffProfile` (request the saved value) and `setStaffProfile {profile}` (persist a new
  choice) — modelled on `listSoundFonts` / `setSoundFont`.
- **Reply:** `staffProfile {profile}` — modelled on `soundFontsListed {fonts, selectedId}` (here just the one value).
- **`WebMessageRouter`:** add `GetStaffProfileRequested` / `SetStaffProfileRequested(string)` events and a
  `StaffProfile` field on the inbound envelope; add a `StaffProfileEnvelope {profile}` reply DTO.
- **`Program.cs`:** wire against the **already-constructed** `AppSettingsStore` (the same instance the soundfont
  library uses):
  - `router.GetStaffProfileRequested += () => bridge.Send(new StaffProfileEnvelope(settings.Get("staffProfile") ?? "tab"));`
  - `router.SetStaffProfileRequested += p => settings.Set("staffProfile", p);`
  - (lift the `AppSettingsStore` to a named local so both the soundfont wiring and this share it; key `"staffProfile"`,
    value ∈ `tab|standard|both`, default `tab`).

### 3. Core renderer — **unchanged**

`AlphaTexRenderer`, `RenderOptions`, and the emitted alphaTex are untouched (`C6`). The `alphatex-syntax-reference`
needs **no** `\staff` update because we never emit it — the contrast with the rejected Option A.

## Reference-doc sync (do in the implementing unit of work)

Touches **app architecture** (a new bridge verb pair + a new `AppSettingsStore` key + a new `ScoreR` display-only
option) → update **`chordflow-architecture-reference.md` §5**: add `getStaffProfile` / `setStaffProfile` /
`staffProfile` to the bridge verb list, and list the staff-profile control under `ScoreR`'s **player-kind /
display-only** options (the local, non-re-rendering bucket alongside soundfont + Auto-layout), persisted via
`AppSettingsStore`.

## What does NOT change

- **Core** — `AlphaTexRenderer` / `RenderOptions` / the alphaTex string (`C6`).
- **The exercise definition** and the generate / save / library paths (`EX4`).
- **`alphatex-syntax-reference`** — no `\staff` directive emitted.

## Risks / watch-items

- **Re-assert on every `scoreLoaded`** — the model is rebuilt per load; miss this and the profile resets to alphaTab's
  default (`both`) after any re-render. Highest-likelihood bug; mirror the existing diagrams-on-top re-assert.
- **Confirm the model path** — `api.score.tracks[i].staves[j].showStandardNotation / showTablature` are settable and
  picked up by `render()` in the bundled alphaTab (both identifiers are present in `alphaTab.min.js`; verify the exact
  staff path at implementation).
- **Default flip** (`both` → `tab`) is intentional (`IN2`) — note it so the disappearing standard staff isn't read as
  a regression.
- **`standard` over a tab exercise** — pitches derive from the `fret.string` positions; confirm the standard staff
  renders legibly for a comping voicing (visual check).

## Validation

- App boots in **tab**; switching to **standard** / **both** re-renders **instantly** (no engine round-trip);
  the choice **survives restart** (`IN2`/`IN4`).
- Single-track (comping-only) **and** two-track (comping + lead) exercises follow the profile on **every** staff
  (`IN7`).
- **both** matches today's combined render (`IN5`).
- Dogfood: confirmed on the **`score-render-component`** page (the score view *is* the surface; the guitar-weave
  fretboard dogfood rule doesn't apply — this is a score-display knob, not a fretboard/engine capability).

Related: [[staff-display-mode-idea]], `score-render-component`, the **soundfont-library** thread (persistence
precedent), the `barsPerRow` / Auto-layout display toggle (display-only precedent).