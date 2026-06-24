---
type: req
id: rq_01KVX4A4NQ0RRWGBWCMJ459SKY
title: Tab-only staff display mode — Requirements
status: locked
created: 2026-06-24
updated: 2026-06-24
version: 2
tags: []
parent_id: id_01KVW7Z0VRK9K8T06CED28T8S6
requires_load: []
---
# Tab-only staff display mode — Requirements

A **three-way** staff-display control — **tab** (default) / **standard** (notation only) / **both** (combined
standard + tab) — offered in the score view as a **display-only** choice over unchanged content, the choice
**persisted** as a global user preference. Scope confirmed in `staff-display-mode-idea` + `staff-display-mode-chat-001`;
mechanism settled as **Option B** (JS per-staff flags, no Core change). **v2 amendment:** the binary tab↔both toggle
(`IN1`/`IN3`) is generalized to the three-way profile (`IN6`/`IN7`), and notation-only is now **Included** (`EX2 ~dropped`);
the `\staff {tabs}` alphaTex mechanism is replaced by the JS staff-flag route (`C1 ~dropped` → `C6`).

### ✅ Included

- `IN1` ~dropped — superseded by `IN6`/`IN7` (was: a tab-only profile selected via alphaTab `\staff {tabs}`; the `\staff` mechanism is dropped under Option B).
- `IN2` **Tab is the default** display mode at app start.
- `IN3` ~dropped — superseded by `IN6` (was: a binary toggle tab-only ↔ combined; now a three-state control).
- `IN4` The chosen mode is **persisted as a global user preference** and **applied on the next launch** (survives restart).
- `IN5` The **both** mode reproduces today's standard-notation + tab output (the no-`\staff`-directive default) — the control only *adds* the tab-only and standard-only states; it does not alter the existing combined render.
- `IN6` A **three-state staff-profile control** in the score view (`score-render-component`, `ScoreR`): **tab** (default) / **standard** (notation only) / **both** (combined), switchable at runtime and back.
- `IN7` The selected profile applies to **every staff of every track** the score emits — single-track (comping-only) and two-track (comping + lead) alike.

### ❌ Excluded

- `EX1` **Per-exercise** display-mode overrides — the preference is **global/app-wide**, not stored per exercise.
- `EX2` ~dropped — notation-only (standard staff, no tab) is now **Included** as the `standard` state of `IN6`.
- `EX3` **Print / export** staff styling.
- `EX4` Any change to the **engine's musical content** — this is a display profile only; the notes, rhythm, voicings, and chord schedule are unchanged.

### ⛓ Constraints

- `C1` ~dropped — superseded by `C6` (was: `\staff {tabs}` as the only staff-profile mechanism, emitted in `AlphaTexRenderer`).
- `C2` Persistence **reuses the existing `IAppSettings` / `AppSettingsStore`** key/value seam, wired through `Program.cs` and the bridge **mirroring the global soundfont-choice pattern** (`listSoundFonts` / `setSoundFont` → `soundFontsListed`) — no new persistence store or schema.
- `C3` Dependency direction **Desktop → Core** unchanged; `ChordFlow.Core` stays UI/host-agnostic.
- `C4` **No new build step or framework** in `wwwroot` — vanilla JS modules over the existing virtual host.
- `C5` The control is a **score-view (`ScoreR`) display concern**, not a content-selection knob — it does not regenerate the exercise definition and does not re-render via Core.
- `C6` **Option B (display-only):** the profile is applied by setting the per-staff **`showStandardNotation` / `showTablature`** model flags in `ScoreR` and calling `api.render()` (re-asserted on `scoreLoaded`) — the **`barsPerRow` / autoLayout** precedent. **No `AlphaTexRenderer` / `RenderOptions` / alphaTex change** and **no `\staff` directive emitted**.
