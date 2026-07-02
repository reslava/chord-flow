---
type: plan
id: pl_01KVQCV5MFQEFGG56QX6JE5A3J
title: Anacrusis rendering — emit \ac on the pickup bar
status: done
created: 2026-06-22
updated: 2026-06-22
version: 1
design_version: 3
req_version: 1
tags: []
parent_id: de_01KVQ4HM9ZV5SSRR86P1TGZ9H2
requires_load: []
target_version: 0.1.0
actual_release: 0.10.0
steps:
  - id: emit-ac-on-both-pickup-bars
    order: 1
    status: done
    description: Prepend "\ac " to the rendered pickup bar in BuildCompingBars and BuildLeadBars (the two pickup call-sites)
    files_touched: [src/ChordFlow.Core/Rendering/AlphaTexRenderer.cs]
    blocked_by: []
    satisfies: [IN1, IN2, IN3, C1, C2, C3]
  - id: update-pickup-tests-non-pickup-guard
    order: 2
    status: done
    description: Update the two pickup test assertions to expect the \ac token and add a guard that non-pickup bars carry no \ac
    files_touched: [tests/ChordFlow.Core.Tests/AlphaTexRendererTests.cs]
    blocked_by: [1]
    satisfies: [IN4, C4]
  - id: document-ac-in-the-alphatex-reference
    order: 3
    status: done
    description: Add \ac to alphatex-syntax-reference.md (bar metadata; length = actual beats; emitted before the bar's beats)
    files_touched: [loom/refs/alphatex-syntax-reference.md]
    blocked_by: []
    satisfies: [IN5]
  - id: build-green-visual-verify
    order: 4
    status: done
    description: Confirm the solution builds and all tests pass, then visually verify a pickup renders as a true anacrusis in the running app
    files_touched: []
    blocked_by: [1, 2, 3]
    satisfies: [IN6, C4, C5]
---
# Anacrusis rendering — emit \ac on the pickup bar

## Goal

Implement the locked req (rq_01KVQCKZRG4NHSC57V4BW78PD1) exactly: make the existing PickupMeasure render as a true alphaTex anacrusis by prefixing its bar with `\ac`. This is a pure rendering change confined to AlphaTexRenderer (C1) — the PickupMeasure type, authoring, and parsing are untouched (EX1), and non-pickup bars stay byte-identical (EX4). The pickup is the first bar of each track, so all `\ts`/`\ks` bar metadata already precedes it and `\ac` slots in immediately before the bar's stateful `:N` and beats (IN2). Step 1 makes the emission change at the two existing pickup call-sites by prepending the constant `"\ac "`, keeping RenderBar/RenderLeadBar pure formatters (IN3); the pickup stays one bar so pipe counts are unchanged (C2) and it is emitted only when rhythm.Pickup exists (C3). Step 2 updates the two pickup tests to expect the token and adds a guard that non-pickup bars carry no `\ac` (IN4, C4). Step 3 documents `\ac` in the alphaTex syntax reference in the same unit of work (IN5, ref-sync). Step 4 confirms the build is green and visually verifies in the running app that alphaTab renders a real pickup — a string assertion alone is not acceptance (IN6, C5). Pickup-into-section / multi-bar alignment (EX2) and a self-authored compensating final bar (EX3) are out of scope.

---

## Steps

| Done | # | Step | Files touched | Blocked by | Satisfies |
|---|---|---|---|---|---|
| ✅ | 1 | Prepend "\ac " to the rendered pickup bar in BuildCompingBars and BuildLeadBars (the two pickup call-sites) | src/ChordFlow.Core/Rendering/AlphaTexRenderer.cs | — | IN1, IN2, IN3, C1, C2, C3 |
| ✅ | 2 | Update the two pickup test assertions to expect the \ac token and add a guard that non-pickup bars carry no \ac | tests/ChordFlow.Core.Tests/AlphaTexRendererTests.cs | 1 | IN4, C4 |
| ✅ | 3 | Add \ac to alphatex-syntax-reference.md (bar metadata; length = actual beats; emitted before the bar's beats) | loom/refs/alphatex-syntax-reference.md | — | IN5 |
| ✅ | 4 | Confirm the solution builds and all tests pass, then visually verify a pickup renders as a true anacrusis in the running app | — | 1, 2, 3 | IN6, C4, C5 |
---

### Legend

| Symbol | Meaning |
|--------|---------|
| ✅ | Done |
| 🔄 | In Progress |
| 🔳 | Pending |
| ❌ | Cancelled |

<!-- step:emit-ac-on-both-pickup-bars -->
### Step 1 — Emit \ac on both pickup bars

In `BuildCompingBars`, the pickup branch (`if (rhythm.Pickup is { } pickup …)`) currently does `barLines.Add(RenderBar(pickupSlots, _ => firstChord, …))`. Change to `barLines.Add("\\ac " + RenderBar(...))`. In `BuildLeadBars`, the matching pickup branch does `barLines.Add(RenderLeadBar(pickupSlots, state, allRests: true))` — change to `barLines.Add("\\ac " + RenderLeadBar(...))`. `\ac` lands at the very start of the bar string, before the stateful `:N` and beats (IN2), e.g. `\ac :4 (1.5 0.4 1.3) |` / `\ac :4 r |`. RenderBar/RenderLeadBar are left untouched — they stay pure per-bar formatters with no pickup awareness (IN3); rejected adding a `bool anacrusis` param for a constant prefix the other bars never use. Emitted only inside the existing `Pickup is { }` guards (C3); the pickup is still one bar so pipe counts are unchanged (C2).

<!-- step:update-pickup-tests-non-pickup-guard -->
### Step 2 — Update pickup tests + non-pickup guard

`Render_Pickup_EmitsLeadingMeasureBeforeBars`: change `EndsWith(":4 (1.5 0.4 1.3) |\n:1 …")` to `EndsWith("\\ac :4 (1.5 0.4 1.3) |\n:1 …")`; the `Equal(2, pipe count)` assertion stays (the pickup is still one bar). `Render_WithLeadAndPickup_MirrorsPickupAsRestsOnLeadTrack`: change `Contains(":4 r |")` to `Contains("\\ac :4 r |")`; the `Equal(4, pipe count)` stays. Add a guard (extend an existing no-pickup render test, e.g. the plain progression render) asserting the output `DoesNotContain("\\ac")` so a regular section bar never emits the token (C3/IN4). Run the suite green (C4).

<!-- step:document-ac-in-the-alphatex-reference -->
### Step 3 — Document \ac in the alphaTex reference

Add `\ac` as a verified directive: bar metadata marking an anacrusis (pickup) bar; the bar's length follows its actual beats/notes rather than the time signature; emitted at the start of the bar before its beats (e.g. `\ac :4 (1.5 0.4 1.3) |`). Now verified — our bundled alphaTab supports anacrusis (`isAnacrusis`) and we have a working example. Edit via `loom_patch_doc`/`loom_update_doc` (refs are gate-excluded but versioned). Land in the same unit of work as step 1 (ref-sync contract).

<!-- step:build-green-visual-verify -->
### Step 4 — Build green + visual verify

Full solution builds; all tests green (C4). Then run the app on a pattern with a pickup and confirm alphaTab renders a real pickup — correct bar numbering / pickup display — not a generic short first bar (IN6). A passing string assertion is explicitly not sufficient acceptance (C5).
