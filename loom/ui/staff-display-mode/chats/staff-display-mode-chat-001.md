---
type: chat
id: ch_01KVX425JA7ZV4Q7PMHE1C2RPS
title: staff-display-mode Chat
status: done
created: 2026-06-24
updated: 2026-06-24
version: 2
tags: []
parent_id: null
requires_load: []
last_ai_block: 8
---
# staff-display-mode Chat

## Rafa:

Read idea
is it all grounded?
if so, create req

## AI:

📡 MCP: loom://context/staff-display-mode-chat-001?mode=chat
📄 staff-display-mode-idea.md — loaded for context
*(no design or plan in this thread yet)*

I read the idea and traced every claim to real code. **Yes — it's all grounded.** Every seam it names exists today:

1. **`\staff {tabs}` (tab-only profile)** — verified working in-app (origin chat). It's a per-track alphaTex directive that sits right after the `\track`/header line. Today the renderer emits **no** `\staff` directive at all (`AppendHeader` / `AppendTrackHeader` in `AlphaTexRenderer.cs`), so alphaTab falls back to its default **combined standard-notation + tab** — which is exactly the "current view" the idea wants to keep as the toggle's other state. ✓
2. **The `Rendering/` header-emission seam** — `AlphaTexRenderer` is the only alphaTex-aware code; header/track metadata is already centralized in `AppendHeader` / `AppendScoreMetadata` / `AppendTrackHeader`. Adding a staff-profile directive there is a clean, localized change. ✓
3. **`score-render-component` (ScoreR) + bridge** — already has the toggle/option pattern (`toggle(...)`, `options`, content-kind re-render vs display-only `applyLayout`), so a "tab-only ↔ combined" control fits the existing shape. ✓
4. **Persistence** — `IAppSettings` / `AppSettingsStore` (Core, generic `Get/Set(key, value)`) is the established global-preference seam, wired through `Program.cs` exactly like the soundfont global choice (`ListSoundFontsRequested` / `SetSoundFontRequested`). "Survives restart" rides this seam — no new persistence infra needed. ✓

**One open design question** (for the design phase, not the req — it doesn't change *what* we want, only *how*): **where the staff toggle lives.**
- (A) **C# content-kind option** — emit `\staff {tabs}` per track, like `ShowChordNames`; flipping it re-renders. This is the route the origin verification used.
- (B) **JS display-only** — toggle the standard staff via alphaTab display settings at runtime, like the `autoLayout` toggle; no C# re-render.

Both are viable; I'll bring trade-offs when we write the design. The req below stays mechanism-neutral.

I've created the req as a **draft** — `staff-display-mode/req.md`. Want me to **lock** it (`finalize_req`) so it becomes the anchor for the design, or tweak any handle first?

## Rafa:

I locked req
Write design

## AI:

Before I write the design doc, one architectural fork needs your call — it determines the whole shape of the design, so I'm stopping here per our design-decision rule.

**What I confirmed in the bundled alphaTab + the refs:**
- The staff profile is two per-staff model flags: **`showStandardNotation`** / **`showTablature`**. `\staff {tabs}` is just parse-time sugar that sets them (`showStandardNotation=false`, `showTablature=true`). Both routes hit the *same* flags.
- alphaTab lets JS flip those flags at runtime and `api.render()` — **no Core round-trip** — exactly like the existing **`barsPerRow` / "Auto layout"** display toggle.
- Persistence precedent is the **soundfont global choice**: bridge `setSoundFont` → Core `AppSettingsStore` (`IAppSettings.Get/Set`), re-applied on boot. Our preference rides the same rails (C2).
- `\staff {tabs}` is **not yet in `alphatex-syntax-reference`** — if we emit it from Core, that ref gets updated in the same unit of work (reference-doc sync rule).

**The fork — where the toggle lives:**

**Option A — content-kind (emit `\staff {tabs}` from Core).** Add a `StaffProfile`/`TabOnly` field to `RenderOptions`; `AlphaTexRenderer` emits `\staff {tabs}` (per track) when tab-only; toggling fires `onNeedsRerender` → C# re-render, like `ShowChordNames`.
- ➕ Literally honors **C1** (`\staff {tabs}` as the mechanism); the renderer stays the single source of alphaTex truth, so the **debug panel's tex stays faithful**; consistent with the other `RenderOptions` toggles.
- ➖ A full Core round-trip + re-parse for what is purely a show/hide; touches Core, the bridge `renderOptions` bag, and `AlphaTexRenderer`. Minor risk: `\staff` placement on the **single-track** (no-`\track`) path needs verifying so the combined output stays byte-identical (IN5).

**Option B — display-only (JS, in `ScoreR`).** ScoreR owns a staff-profile setting; on `scoreLoaded` + on toggle it sets `showStandardNotation`/`showTablature` on every staff and `api.render()`. **Zero Core/renderer change.** Persist via the soundfont-style bridge→`AppSettingsStore` pair.
- ➕ Matches the **`barsPerRow` precedent** exactly — a *display/layout* concern handled locally; snappy toggle, no re-render; squares cleanly with **C5** ("display concern, not content"); Core untouched (C3).
- ➖ Uses the staff flags directly rather than the alphaTex `\staff {tabs}` string, so it **bends C1's literal wording** (though it's the same mechanism, and your "if emitted as alphaTex" clause left this open). The debug-panel tex won't *show* `\staff {tabs}` — the profile is applied post-parse.

**My recommendation: Option B.** Tab-only-vs-combined is a presentation choice over unchanged content — the same category as bars-per-row, which we already do JS-side. Content-kind/re-render is for toggles that change *what notes/labels* appear (chord names, diagrams); this changes neither. B keeps Core UI-agnostic and the toggle instant.

The counterweight is C1: you locked `\staff {tabs}` as "the only staff-profile mechanism." If you read that literally, **Option A** is the intended route and I'll design around it (RenderOptions field + AlphaTexRenderer emission + alphatex-ref update).

Which way — **A** or **B**? Then I'll write the design.

## Rafa:

I also think B is better.
I see we could offer presentation choices:
- tab (default)
- standard
- both

write design

## AI:

Option B it is. Two things done:

**1. Amended the req → v2 (it's back to `draft`).** Your three-way set added **standard-only**, which the locked req *excluded* (`EX2`) and whose mechanism (`\staff {tabs}`, `C1`/`IN1`) we just replaced. Under append-only rules I retired and re-added rather than deleting:
- `EX2 ~dropped` → notation-only is now Included.
- `IN1 ~dropped` (the `\staff {tabs}` route), `IN3 ~dropped` (binary toggle) → generalized to **`IN6`** (three-state control: tab / standard / both) + **`IN7`** (applies to every staff of every track).
- `C1 ~dropped` → **`C6`**: Option B, the JS staff-flag route, no Core change.
- Kept `IN2` (default tab), `IN4` (persist), `IN5` (both = today's render), `C2`–`C5`.

**2. Wrote the design** — `staff-display-mode-design.md`. Shape:
- **Profile → flags:** tab `{std:false, tab:true}` · standard `{std:true, tab:false}` · both `{std:true, tab:true}`. `both` is alphaTab's no-`\staff` default, so it stays byte-identical.
- **`ScoreR`:** a Tab/Standard/Both `<select>`; `applyStaffProfile()` sets the per-staff flags + renders via the same path `scoreLoaded` uses; **re-asserted on `scoreLoaded`** (the model is rebuilt each load — same pitfall as diagrams-on-top). Local/display-only, never touches `renderOptions`.
- **Persistence:** a `getStaffProfile` / `setStaffProfile` / `staffProfile` bridge trio mirroring the soundfont pair, wired to the existing `AppSettingsStore` (key `"staffProfile"`, default `tab`). Zero Core renderer change.
- **Ref sync:** `chordflow-architecture-reference §5` gets the new verbs + the display-only option, done in the implementing unit of work.
- **Top risk:** forgetting the `scoreLoaded` re-assert (profile would snap back to `both` after any re-render).

Two notes for you:
- The amend put the req back to **draft** — **re-lock it** (`finalize_req`) when the v2 scope looks right, so it's a solid anchor before I cut the plan.
- Default flips from the implicit `both` → **`tab`** — intentional, flagged in the design so it's not misread as a regression.

Want a tweak to the design, or shall I re-lock the req and write the plan next?

## AI:

Plan written — `staff-display-mode-plan-001.md` (against req v2 + the design). Built back-to-front so each step lands on a working seam:

| # | Step | Satisfies |
|---|------|-----------|
| 1 | **Persistence seam (bridge + Core)** — `StaffProfileEnvelope` reply + `getStaffProfile`/`setStaffProfile` verbs + router events + `Program.cs` wiring to `AppSettingsStore`, mirroring the soundfont pair. Router test included. | IN4, C2, C3 |
| 2 | **ScoreR control + apply + persist** — `applyStaffProfile()` sets the per-staff `showStandardNotation`/`showTablature` flags + re-renders, **re-asserted on `scoreLoaded`**; Tab/Standard/Both `<select>`; boot-request + on-change-persist. Default `tab`, display-only. | IN2, IN5, IN6, IN7, C5, C6 |
| 3 | **Architecture-ref sync** — `§5` gets the verb trio + the ScoreR display-only option. | C2, C6 |

Coverage check: every Included handle lands (IN2/IN4/IN5/IN6/IN7), all constraints cited (C2/C3/C5/C6; C4 is inherent — vanilla JS). No Core renderer / `RenderOptions` / alphatex-ref change, by design (Option B).

Step 1 touches `WebMessageRouter.cs`, a new `Bridge/StaffProfileEnvelope.cs`, `Program.cs`, and `WebMessageRouterContentTests.cs`.

Want to run `loom_verify_req` on the plan first, or say **go** and I'll start Step 1?

## Rafa:

I already checked no gaps and started the plan
Do whole plan

## AI:

Whole plan implemented and the plan auto-completed to **done**. Summary:

**Step 1 — persistence seam** ✅
- New `Bridge/StaffProfileEnvelope.cs` (`{type:"staffProfile", profile}` reply).
- `WebMessageRouter`: `GetStaffProfileRequested` / `SetStaffProfileRequested` events, `getStaffProfile` / `setStaffProfile` cases, `Profile` field on `InboundEnvelope`.
- `Program.cs`: lifted `AppSettingsStore` to a shared local; wired both events to it (key `"display.staffProfile"`, default `"tab"`).
- 3 new router tests (get fires, set carries the profile, missing-profile dropped).

**Step 2 — ScoreR control** ✅
- `STAFF_FLAGS` map + `setStaffFlags()` / `applyStaffProfile()` / `onStaffProfile()`. Flags set in `scoreLoaded` **before** the render (re-asserted per load); runtime toggle sets flags + re-renders via the same path.
- Tab/Standard/Both `<select>` (`staffProfileSelect`) shown in full **and** mini score views; `handle.setStaffProfile` applies locally + persists via the bridge.
- Boot block (both player + lite modes) requests `getStaffProfile` and applies the reply. Default `tab`; never enters `renderOptions`.

**Step 3 — ref sync** ✅
- `chordflow-architecture-reference §5`: added the verb trio + the staff-profile as a ScoreR display-only/local option persisted via `AppSettings`.

**Verified:** `dotnet build` clean (only pre-existing WindowsBase warnings); full Core suite **667/667 green** (incl. the 3 new tests). One bug caught + fixed mid-build: the inbound field was `StaffProfile` (→ camelCased to `staffProfile`) but JS sends `profile` — renamed to `Profile`.

**Not yet done — in-app dogfood** (the design's Validation): boots in tab-only, switch to standard/both re-renders instantly, choice survives restart, both single- and two-track follow it. That needs the WinForms app run, which is your side. Want me to leave the plan as-is for you to verify, or shall I close the plan (`loom_close_plan` → done doc) now?

## Rafa:

app working
commit