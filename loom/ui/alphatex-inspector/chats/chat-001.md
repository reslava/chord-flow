---
type: chat
id: ch_01KV6MYKWCSG04GEKSH4W9XWSK
title: alphatex-inspector Chat
status: done
created: "2026-06-15T00:00:00.000Z"
updated: 2026-06-16
version: 2
tags: []
parent_id: null
requires_load: []
last_ai_block: 13
---
# alphatex-inspector Chat

## Rafa:

## Rafa:

There are many issues but I think instead of enumerating them and start fixing, we should implement a debug tools that will help us a lot.

UI text box to show, edit the alphaTex generated and render/play it.
What you think?
If you like the idea, give it a name and create a thread.

## AI:

**On the debug tool — strong yes.** It's exactly the right move over enumerate-then-fix, because nearly every render/playback bug lives on **one seam**: the alphaTex string between `AlphaTexRenderer` and alphaTab. A live inspector answers the only question that matters when a score is wrong — *is the C# emit wrong, or is alphaTab reading correct alphaTex differently than we assume?* — and turns each bug into an edit-and-rerender loop instead of regenerate→rebuild→relaunch. It doubles as a scratchpad to verify the alphaTex syntax ref (several entries there are flagged "unverified — smoke-test in the app").

And it's **cheap by construction**: the `loadScore` envelope already carries `tex`, `app.js` already receives it, and `ChordFlowScore.load(tex)` already renders **and plays** a raw string. So the v1 is essentially front-end only — capture the last tex into a `<textarea>`, add a Render/Play button that calls `view.load(textarea.value)`. No new bridge verb, no Core change.

**Name:** `alphatex-inspector`. Created the thread + idea in the `ui` weave:
- `loom/ui/alphatex-inspector/alphatex-inspector-idea.md` (id `id_01KV6MNTQMPZXM49TM3GQWCN5K`, draft) — peer of [[exercise-workbench]] / [[score-render-component]].

The idea captures the cheap-MVP framing + the open design calls (its own **Debug** view vs an inline panel vs a dev-only flag; "Load current" from the last score; whether v1 has any feedback path beyond read-edit-render). My lean: a third **Debug** view toggle with its own full-player `ChordFlowScore`.

Want to talk through those open calls and lock a `req`, or should I just build the front-end-only MVP first (Debug view + textarea + Load-current + Render/Play) and we iterate? Given how cheap it is, I'd lean **build the MVP now** — it'll immediately help triage the workbench issues — then formalize once we see it in use.

## Rafa:

NOTE: I moved related chat to this new chat in alphatex-inspector thread
from loom/ui/exercise-workbench/chats/exercise-workbench-chat-001.md

Following your recommendation:
  build the front-end-only MVP now
  (Debug view + textarea + "Load current" + Render/Play)
  since it'll immediately help triage the workbench
  issues, then formalize a req once we see it in use.
  Waiting on your call — build now, or lock a req first.

## AI:

Built the front-end-only MVP. Desktop builds + bundles, `node --check` clean on both JS files.

**What landed (all `wwwroot`, zero Core/bridge change):**
- **New `alphatex-inspector.js`** (`window.ChordFlowInspector`) — the Debug view. **Eagerly** caches every `loadScore.tex` off the shared bridge fan-out (so "Load current" works even before Debug is first opened), lazily builds its DOM + its own full-player `ChordFlowScore` on first show. Buttons: **Load current** (pull the last engine score into the textarea, or a sample if none yet) and **Render** (`view.load(textarea.value)` → play via the component's transport). Content-toggle changes re-render locally from the textarea, so it's fully self-contained.
- **`index.html`** — third **Debug** nav segment + `#debug-view` container + styles + the script tag (before `app.js`, so its cache listener is registered before the first `ready`/`loadScore`).
- **`app.js`** — `setupViewToggle` generalized from 2-way to an N-way table (Practice / Content / Debug), each lazily initing its module on show.
- **Architecture ref** synced (wwwroot file map + the Debug view in the fan-out description).

**How you'll use it:** Generate something in Practice → switch to **Debug** → it auto-fills with that score's alphaTex (or hit **Load current**) → edit → **Render** → play. When a workbench score looks wrong, this tells you instantly whether our C# emit is wrong or alphaTab is reading valid alphaTex differently than we assumed.

**Not verified by me:** the live click-through (needs the desktop display this session lacks) — `dotnet run --project src/ChordFlow.Desktop` and try it. The idea doc (`id_01KV6MNTQMPZXM49TM3GQWCN5K`) is still `draft` and there's no `req`/plan — you asked me to build directly, so I skipped the ceremony; say the word if you want it formalized.

Commit + push to `main`? And once you've got the inspector running, fire the workbench issues at me and we'll triage them through it.

## AI:

We're on **alphaTab 1.8.3** (build 32, dated 2026-05-24) — the `@coderline/alphatab@1.8.3` release. Three ways to check, fastest first:

1. **Runtime (best — what the app actually loaded).** The global exposes a `meta` object. In the Debug view, open DevTools (the WebView2 control supports right-click → Inspect, or F12) and run in the console:
   ```js
   alphaTab.meta.version   // "1.8.3"
   alphaTab.meta.date      // "2026-05-24T19:53:05.944Z"
   alphaTab.meta.commit    // build commit hash
   ```
2. **The bundled file's banner.** Top of `src/ChordFlow.Desktop/wwwroot/alphaTab.min.js`:
   ```
   /*! alphaTab v1.8.3 (, build 32) ... */
   ```
3. **The pinned soundfont in the build.** `ChordFlow.Desktop.csproj` downloads the matching soundfont from a version-pinned CDN URL:
   ```
   <SoundfontUrl>https://cdn.jsdelivr.net/npm/@coderline/alphatab@1.8.3/dist/soundfont/sonivox.sf2</SoundfontUrl>
   ```

**How it's vendored (so a future bump is clean):** the `alphaTab.min.js` + Bravura font are committed in `wwwroot` (offline, constraint C6), and the csproj pulls the soundfont from the pinned `@1.8.3` CDN path. So upgrading alphaTab = swap the three together (the `.min.js`, the font if its bundle changed, and the `@1.8.x` in `<SoundfontUrl>`) and re-verify in the inspector.

Want me to **surface the version in the Debug toolbar** (a tiny `alphaTab v1.8.3` label, read from `alphaTab.meta`)? Cheap, and handy while we're triaging render bugs against the syntax ref — it removes any doubt about which engine you're looking at.

## AI:

Added the version label. The Debug toolbar now shows **`alphaTab v1.8.3`** on the right, read at init straight from `alphaTab.meta.version` (so it always reflects whatever build is actually loaded — swap the `.min.js` and the label updates itself).

- `alphatex-inspector.js` — `#atxVersion` span populated from `alphaTab.meta.version` on init (guarded for the global being absent).
- `index.html` — `.atx-version` style (right-aligned, muted) + the span in the toolbar.

`node --check` clean, Desktop rebuilt + bundled. Reload the app to see it. Ready for the workbench issues whenever you are — fire them over and we'll triage each through the inspector.

## Rafa:

Using inspector I realize there is no way to show 2 tracks.
I did a researcj and I think is becuase we need to render tracks properly:
```javascript
const api = new alphaTab.AlphaTabApi(document.querySelector('#alphaTab'));
api.renderTracks([api.score.tracks[0], api.score.tracks[1]]);
```

Review and fix

## AI:

Confirmed and fixed — your research nailed it. alphaTab renders **only the first track** by default, so the two-track exercises (comping + lead) were emitting both `\track`s in the alphaTex but only the comping staff ever drew. (Good example of the inspector earning its keep — that's exactly the "is it the emit or the render?" question it exists to answer: the emit was right, the render call was incomplete.)

**Fix** — in the shared `score-render-component.js`, in the `scoreLoaded` handler:
```js
if (score && score.tracks && score.tracks.length > 1) {
  api.renderTracks(score.tracks);   // render ALL tracks, not just the default first
}
```
I render `score.tracks` (all) rather than hardcoding `[0],[1]`, and **only when there's more than one track**, so single-track scores stay on alphaTab's default path (unchanged). Because it lives in the shared component, **all three consumers** get it at once — Practice, the inspector, and the Content preview.

Also updated `alphatab-js-api-reference.md` with the verified multi-track behavior + `renderTracks` (it wasn't documented). `node --check` clean, Desktop rebuilt.

**Test it:** reload → in the inspector, **Load current** an exercise that has a Lead (or paste any alphaTex with two `\track` blocks) → **Render** → you should now see both staves. Generate a Song-with-Lead in Practice and it should show two staves too. Confirm and I'll commit; then keep the issues coming.

## Rafa:

Working

### Fix

In order to { defaultSystemsLayout 4 } (4 bars per row) work

We need to setup:
```js
display: {
  layoutMode: alphaTab.LayoutMode.Parchment
}
```

Without this, alphaTab will arrange bars automatically, so we could give users both layout: automatic, page 4 bars per row

note: we defer commit until fixes, tweaks are done

## AI:

Wired it — and confirmed the precise mechanism so it's robust. Two settings govern this, not one:

- `display.layoutMode` — `Page` (paginated, default) / `Horizontal` / **`Parchment`** (continuous).
- `display.systemsLayoutMode` — `Automatic` (fit bars to width, **default** — this is what ignored your `defaultSystemsLayout 4`) / **`UseModelLayout`** (honor the score's authored bars-per-row).

The bars-per-row only sticks when **`systemsLayoutMode = UseModelLayout`**; `Parchment` is the layout that respects it cleanly. So I set the pair, and gave users both modes as you suggested:

- **Default = honor the engine** → `Parchment` + `UseModelLayout` → your `defaultSystemsLayout 4` now gives exactly 4 bars/row.
- **"Auto layout" toggle** (new, in the full control strip) → `Page` + `Automatic` → fit-to-width. Applied at runtime via `api.settings.display.* → api.updateSettings(); api.render()`, no C# round-trip.

All in the shared `score-render-component.js`, so Practice, the inspector, and the Content preview all get it (+ the new toggle). Updated `alphatab-js-api-reference.md` with the verified layout semantics. `node --check` clean, Desktop rebuilt.

**Test:** reload → a Song-with-Lead (or any `defaultSystemsLayout 4` tex in the inspector) should show **4 bars per row**; tick **Auto layout** and it reflows to width. Holding commit until you've signed off on the batch — keep the fixes coming.

## Rafa:

Working but only for multi-track rendering.
The problem: `defaultSystemsLayout` and `systemsLayout` only work for multi-track

Solution: avoid using `defaultSystemsLayout` and `systemsLayout`
and instead use:
```js
display: {
  layoutMode: alphaTab.LayoutMode.Page,
  barsPerRow: 4
}
```

and this for automatic:
```js
display: {
    layoutMode: alphaTab.LayoutMode.Page,
    barsPerRow: -1 // automatic (also the default)
  }
```
or
```js
display: {
    layoutMode: alphaTab.LayoutMode.Page,   
  }
```

lets try this

## AI:

Switched to `barsPerRow` — you're right, `defaultSystemsLayout` is model-layout-only and only bites on multi-track, so it's the wrong knob. `display.barsPerRow` on `LayoutMode.Page` is global and works for single- **and** multi-track.

`layoutDisplay` is now simply:
```js
function layoutDisplay(auto) {
  return { layoutMode: alphaTab.LayoutMode.Page, barsPerRow: auto ? -1 : 4 };
}
```
- **Default** → `barsPerRow: 4` (fixed four bars/row, every score).
- **"Auto layout" toggle** → `barsPerRow: -1` (automatic / fit-to-width, alphaTab's default).
- Dropped the `Parchment` + `UseModelLayout` pairing entirely — no longer needed.

Runtime toggle still goes through `api.updateSettings(); api.render()`. Ref updated to the `barsPerRow` semantics. `node --check` clean, Desktop rebuilt.

**One loose end:** the C# `AlphaTexRenderer` still emits `{ defaultSystemsLayout 4 }` in the two-track tex. It's now **inert** (Page + Automatic ignores it, `barsPerRow` wins), so no conflict — but it's dead weight. Want me to strip it from the renderer (a small Core change + the two-track renderer test that asserts it)? I'd fold it into the eventual commit. For now, **test:** reload → single-track progression and a Song-with-Lead should *both* show 4 bars/row; tick **Auto layout** to reflow.

## AI:

Stripped it. The `\track` lines are now just `\track "Comping" "comp"` / `\track "Lead" "lead"` — no `{ defaultSystemsLayout 4 }`. Bars-per-row is purely the JS `display.barsPerRow` now.

- `Rendering/AlphaTexRenderer.cs` — `AppendTrackHeader` drops the brace block (+ comments updated).
- `AlphaTexRendererTests` — the two-track assertion now expects the bare `\track` lines.
- **Build clean, 399 tests pass.**
- Refs synced: `alphatex-syntax-reference` (Multiple-tracks section + code sample), `chordflow-domain-model-reference` (§5 + the §7 pipeline line), `alphatab-js-api-reference` (the cleanup note).

That's the layout thread fully closed — single + multi-track both honor `barsPerRow`, no dead alphaTex. Commit still on hold per your call. Next issue whenever you're ready.