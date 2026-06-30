---
type: plan
id: pl_01KVTNA4KM6MG9TQSEKB4QJS6P
title: Scroll Mode + Transport Toggles
status: done
created: 2026-06-23
updated: 2026-06-23
version: 2
design_version: 2
req_version: 1
tags: []
parent_id: de_01KVSZBZQ1WZB5S31GSNS0F3QD
requires_load: []
target_version: 0.1.0
actual_release: 0.12.0
steps:
  - id: scroll-mode-fix-the-actual-annoyance
    order: 1
    status: done
    description: "Switch the playing surface from ScrollMode.Smooth to ScrollMode.OffScreen and flip nativeBrowserSmoothScroll to follow `scroll` (animate the page-flips). Model A binding (scrollElement + maxHeight:60vh + scrollOffsetY headroom) is unchanged."
    files_touched: [src/ChordFlow.Desktop/wwwroot/score-render-component.js]
    blocked_by: []
    satisfies: []
  - id: scroll-on-off-transport-toggle
    order: 2
    status: done
    description: "Add a scroll auto-follow on/off toggle to the transport strip. Not a render-`options` toggle: it flips api.settings.player.scrollMode between OffScreen and Off live via updateSettings(), and adds/removes the scrollElement + maxHeight binding so turning it off releases the bounded surface."
    files_touched: [src/ChordFlow.Desktop/wwwroot/score-render-component.js]
    blocked_by: []
    satisfies: []
  - id: now-next-show-hide-transport-toggle
    order: 3
    status: done
    description: Add a Now/Next show/hide toggle to the transport strip plus an onToggleNowNext(visible) callback opt; wire app.js to show/hide the ChordFlowNowNext container. Component stays generic — it never names Now/Next, just fires the callback.
    files_touched: [src/ChordFlow.Desktop/wwwroot/score-render-component.js, src/ChordFlow.Desktop/wwwroot/app.js]
    blocked_by: []
    satisfies: []
  - id: verify-on-running-app
    order: 4
    status: done
    description: "Verify on the running app: OffScreen no longer creeps (only page-flips at row/page boundaries, animated), the active bar lands below the Now/Next boards, the scroll toggle stops/starts auto-follow live, and the Now/Next toggle shows/hides the boards. No architecture-ref change expected (behavior/UI only) — confirm."
    files_touched: []
    blocked_by: []
    satisfies: []
  - id: scroll-mode-select-off-offscreen-continuous
    order: 5
    status: done
    description: Add a 3-way Scroll mode select (Off / OffScreen / Continuous) to the transport, replacing the auto-scroll on/off toggle — refactor applyScroll(on) → applyScrollMode(mode), pairing nativeBrowserSmoothScroll per mode (on for OffScreen to animate the flip, off for Continuous to avoid rubber-banding), exposed as handle.setScrollMode, so both follow modes can be A/B-tested live.
    files_touched: [src/ChordFlow.Desktop/wwwroot/score-render-component.js]
    blocked_by: []
    satisfies: []
---
# Scroll Mode + Transport Toggles

## Goal

Fix the annoying continuous score-scroll and add two transport-bar toggles. The current `ScrollMode.Smooth` creeps the score every frame regardless of whether the active beat has reached the row end; switch the playing surface to `ScrollMode.OffScreen` so it only page-flips when the cursor would leave view, animating the flip via the native browser smooth-scroll. Keep Model A — the score still scrolls its own bounded inner surface (`scrollElement` + `maxHeight: 60vh`), so the Now/Next boards stay pinned for free. Then add two toggles to the transport strip: (1) scroll auto-follow on/off (a live player-setting flip, distinct from the render-`options` toggles), and (2) Now/Next fretboards show/hide (the score component doesn't own that container, so it exposes a callback and app.js shows/hides it). Pure front-end — no engine, schedule, or C# changes.

---

## Steps

| Done | # | Step | Files touched | Blocked by | Satisfies |
|---|---|---|---|---|---|
| ✅ | 1 | Switch the playing surface from ScrollMode.Smooth to ScrollMode.OffScreen and flip nativeBrowserSmoothScroll to follow `scroll` (animate the page-flips). Model A binding (scrollElement + maxHeight:60vh + scrollOffsetY headroom) is unchanged. | src/ChordFlow.Desktop/wwwroot/score-render-component.js | — | — |
| ✅ | 2 | Add a scroll auto-follow on/off toggle to the transport strip. Not a render-`options` toggle: it flips api.settings.player.scrollMode between OffScreen and Off live via updateSettings(), and adds/removes the scrollElement + maxHeight binding so turning it off releases the bounded surface. | src/ChordFlow.Desktop/wwwroot/score-render-component.js | — | — |
| ✅ | 3 | Add a Now/Next show/hide toggle to the transport strip plus an onToggleNowNext(visible) callback opt; wire app.js to show/hide the ChordFlowNowNext container. Component stays generic — it never names Now/Next, just fires the callback. | src/ChordFlow.Desktop/wwwroot/score-render-component.js, src/ChordFlow.Desktop/wwwroot/app.js | — | — |
| ✅ | 4 | Verify on the running app: OffScreen no longer creeps (only page-flips at row/page boundaries, animated), the active bar lands below the Now/Next boards, the scroll toggle stops/starts auto-follow live, and the Now/Next toggle shows/hides the boards. No architecture-ref change expected (behavior/UI only) — confirm. | — | — | — |
| ✅ | 5 | Add a 3-way Scroll mode select (Off / OffScreen / Continuous) to the transport, replacing the auto-scroll on/off toggle — refactor applyScroll(on) → applyScrollMode(mode), pairing nativeBrowserSmoothScroll per mode (on for OffScreen to animate the flip, off for Continuous to avoid rubber-banding), exposed as handle.setScrollMode, so both follow modes can be A/B-tested live. | src/ChordFlow.Desktop/wwwroot/score-render-component.js | — | — |
---

### Legend

| Symbol | Meaning |
|--------|---------|
| ✅ | Done |
| 🔄 | In Progress |
| 🔳 | Pending |
| ❌ | Cancelled |

<!-- step:scroll-mode-fix-the-actual-annoyance -->
### Step 1 — Scroll mode fix (the actual annoyance)

In buildSettings: `scrollMode: scroll ? alphaTab.ScrollMode.OffScreen : alphaTab.ScrollMode.Off` (was Smooth) and `nativeBrowserSmoothScroll: scroll` (was !scroll). Update the now-stale comment (lines ~100-104) explaining why OffScreen + native smooth-scroll replaces the per-frame rAF creep. Leave the `opts.scroll` block at :131 (scrollElement = surface, maxHeight 60vh, scrollOffsetY) intact — Model A.

<!-- step:scroll-on-off-transport-toggle -->
### Step 2 — Scroll on/off transport toggle

Add a `setScroll(on)` method on `handle` that, on the live api, sets scrollMode (OffScreen/Off), nativeBrowserSmoothScroll, and binds/unbinds scrollElement + surface.style.maxHeight, then api.updateSettings(). Render a toggle button in buildControls (player + full/mini) reflecting the current state; initial state from opts.scroll. Keep it visually consistent with the existing transport buttons.

<!-- step:now-next-show-hide-transport-toggle -->
### Step 3 — Now/Next show/hide transport toggle

Add cb.onToggleNowNext = opts.onToggleNowNext || noop (alongside onBeat/onStateChange). Render a toggle button in buildControls (full profile) that tracks visible state and calls cb.onToggleNowNext(visible). In app.js, pass onToggleNowNext that toggles the Now/Next host container's hidden/display. Default visible = true.
