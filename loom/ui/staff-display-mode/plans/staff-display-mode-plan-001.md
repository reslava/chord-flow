---
type: plan
id: pl_01KVX65268QESYZ3K5DZKHN37J
title: Staff display mode — Implementation
status: done
created: 2026-06-24
updated: 2026-06-24
version: 1
design_version: 1
req_version: 2
tags: []
parent_id: de_01KVX5ZNHEZKPEHGC1DYA84MQ0
requires_load: []
target_version: 0.1.0
actual_release: 0.12.0
steps:
  - id: persistence-seam-bridge-core
    order: 1
    status: done
    description: "Add the staff-profile persistence seam: a `staffProfile` reply envelope, the `getStaffProfile` / `setStaffProfile` inbound verbs + router events, and the `Program.cs` wiring to `AppSettingsStore` — mirroring the soundfont pair."
    files_touched: [src/ChordFlow.Core/Bridge/StaffProfileEnvelope.cs, src/ChordFlow.Core/Bridge/WebMessageRouter.cs, src/ChordFlow.Desktop/Program.cs, tests/ChordFlow.Core.Tests/WebMessageRouterContentTests.cs]
    blocked_by: []
    satisfies: [IN4, C2, C3]
  - id: scorer-control-apply-persist
    order: 2
    status: done
    description: "Add the three-state staff-profile control to `ScoreR`: an `applyStaffProfile()` that sets the per-staff `showStandardNotation`/`showTablature` flags + re-renders, re-asserted on `scoreLoaded`, plus the toolbar `<select>` and the boot-request / on-change-persist wiring. Default `tab`; local/display-only (never in `renderOptions`)."
    files_touched: [src/ChordFlow.Desktop/wwwroot/score-render-component.js]
    blocked_by: [persistence-seam]
    satisfies: [IN2, IN5, IN6, IN7, C5, C6]
  - id: architecture-ref-sync
    order: 3
    status: done
    description: "Update `chordflow-architecture-reference.md` §5: add the `getStaffProfile` / `setStaffProfile` / `staffProfile` verbs to the bridge list, and list the staff-profile control under ScoreR's display-only (local, non-re-rendering) options persisted via `AppSettingsStore`."
    files_touched: [loom/refs/chordflow-architecture-reference.md]
    blocked_by: [persistence-seam, scorer-control]
    satisfies: [C2, C6]
---
# Staff display mode — Implementation

## Goal

Add a three-way staff-display control (tab / standard / both) to the score view as a display-only choice over unchanged content, persisted as a global preference. Option B: ScoreR sets the per-staff `showStandardNotation` / `showTablature` alphaTab model flags and re-renders locally — zero Core renderer change (no `AlphaTexRenderer`, no `RenderOptions`, no `\staff` directive). Persistence reuses the existing `AppSettingsStore` over a bridge verb trio mirroring the soundfont choice. Built back-to-front: the persistence seam (bridge + Core wiring) first, then the ScoreR control + apply + boot/persist wiring, then the architecture-ref sync. The default flips from today's implicit `both` to `tab`.

---

## Steps

| Done | # | Step | Files touched | Blocked by | Satisfies |
|---|---|---|---|---|---|
| ✅ | 1 | Add the staff-profile persistence seam: a `staffProfile` reply envelope, the `getStaffProfile` / `setStaffProfile` inbound verbs + router events, and the `Program.cs` wiring to `AppSettingsStore` — mirroring the soundfont pair. | src/ChordFlow.Core/Bridge/StaffProfileEnvelope.cs, src/ChordFlow.Core/Bridge/WebMessageRouter.cs, src/ChordFlow.Desktop/Program.cs, tests/ChordFlow.Core.Tests/WebMessageRouterContentTests.cs | — | IN4, C2, C3 |
| ✅ | 2 | Add the three-state staff-profile control to `ScoreR`: an `applyStaffProfile()` that sets the per-staff `showStandardNotation`/`showTablature` flags + re-renders, re-asserted on `scoreLoaded`, plus the toolbar `<select>` and the boot-request / on-change-persist wiring. Default `tab`; local/display-only (never in `renderOptions`). | src/ChordFlow.Desktop/wwwroot/score-render-component.js | persistence-seam | IN2, IN5, IN6, IN7, C5, C6 |
| ✅ | 3 | Update `chordflow-architecture-reference.md` §5: add the `getStaffProfile` / `setStaffProfile` / `staffProfile` verbs to the bridge list, and list the staff-profile control under ScoreR's display-only (local, non-re-rendering) options persisted via `AppSettingsStore`. | loom/refs/chordflow-architecture-reference.md | persistence-seam, scorer-control | C2, C6 |
---

### Legend

| Symbol | Meaning |
|--------|---------|
| ✅ | Done |
| 🔄 | In Progress |
| 🔳 | Pending |
| ❌ | Cancelled |

<!-- step:persistence-seam-bridge-core -->
### Step 1 — Persistence seam (bridge + Core)

Mirror the soundfont choice end to end:

- **New `StaffProfileEnvelope(string Profile, string Type = "staffProfile")`** reply DTO (a sibling of `SoundFontsListedEnvelope` — new file `Bridge/StaffProfileEnvelope.cs`), serializing to `{"type":"staffProfile","profile":"…"}`.
- **`WebMessageRouter`**: add `event Action? GetStaffProfileRequested` and `event Action<string>? SetStaffProfileRequested`; add a `string? StaffProfile` field to the private `InboundEnvelope`; add two switch arms next to `listSoundFonts`/`setSoundFont` — `getStaffProfile` → `GetStaffProfileRequested?.Invoke()`, `setStaffProfile` → `if (envelope.StaffProfile is { } p) SetStaffProfileRequested?.Invoke(p)`.
- **`Program.cs`**: lift the `AppSettingsStore` currently built inline for `SoundFontLibrary` into a shared named local, then wire `router.GetStaffProfileRequested += () => bridge.Send(new StaffProfileEnvelope(appSettings.Get("staffProfile") ?? "tab"));` and `router.SetStaffProfileRequested += p => appSettings.Set("staffProfile", p);` (key `"staffProfile"`, default `"tab"`).
- **Test** (`WebMessageRouterContentTests`): a `getStaffProfile` message fires `GetStaffProfileRequested`; a `setStaffProfile` message with `profile` invokes `SetStaffProfileRequested` with that string.

<!-- step:scorer-control-apply-persist -->
### Step 2 — ScoreR control + apply + persist

- **`applyStaffProfile(profile)`**: map `tab → {std:false, tab:true}`, `standard → {std:true, tab:false}`, `both → {std:true, tab:true}`; for every `staff` in every `api.score.tracks[*].staves[*]` set `showStandardNotation` / `showTablature`, then re-render via the **same branch `scoreLoaded` uses** (`api.renderTracks(api.score.tracks)` when `tracks.length > 1`, else `api.render()`). No `updateSettings()` — these are score-model flags (`IN7`).
- **Re-assert on `scoreLoaded`**: call `applyStaffProfile(currentStaffProfile)` inside the existing handler, before its `renderTracks`, so load + toggle share one path and a freshly loaded score never flashes alphaTab's default `both` (the same re-assert pattern as `globalDisplayChordDiagramsOnTop`).
- **Control**: a Tab/Standard/Both `<select>` in `buildControls` (alongside the existing toggles), default `tab`. On change → `applyStaffProfile(value)` locally **and** `bridge` send `{type:"setStaffProfile", profile:value}`. It is **display-only** — it never enters `renderOptions` and never fires `onNeedsRerender` (same class as the soundfont picker / Auto-layout).
- **Boot**: track `let currentStaffProfile = "tab"`; on init send `{type:"getStaffProfile"}`; on the `staffProfile` reply set `currentStaffProfile`, the `<select>` value, and `applyStaffProfile`. Unknown/blank profile coalesces to `tab`.
- `both` reproduces today's render (alphaTab's no-`\staff` default), so the combined view stays byte-identical (`IN5`); the default flips to `tab` (`IN2`).

<!-- step:architecture-ref-sync -->
### Step 3 — Architecture-ref sync

Add the verb trio to the §5 envelope-`type` list (next to the soundfont pair). In the ScoreR options-split paragraph, add the staff-profile control to the **player-kind / display-only** bucket (applied locally via the per-staff flags + `api.render()`, no C# re-render — the `barsPerRow`/Auto-layout sibling), persisted globally via the `AppSettingsStore` key `"staffProfile"`. No domain-model or alphatex-syntax ref change (Core/alphaTex untouched).
