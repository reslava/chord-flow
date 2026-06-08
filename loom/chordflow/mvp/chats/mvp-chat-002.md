---
type: chat
id: ch_01KTK6S4QXCZFV64ZG4RCGR420
title: Phase 2 WebView2/Photino black-window diagnosis
status: active
created: 2026-06-08
version: 1
tags: []
parent_id: null
requires_load: []
---
# Phase 2 WebView2/Photino black-window diagnosis

## Rafa:


## AI:

## Diagnosis — Phase 2 renders, but the WebView2 client area is blank

Moving our debugging here so it persists. Phase 2 (steps 1–4) is **code-complete and tested** — `dotnet build` 0/0, 26/26 tests, `node --check app.js` clean. The blocker is entirely in the **Photino/WebView2 display layer on this machine**, not in our code.

### What we proved (facts, not guesses)

| Fact | Rules out |
|------|-----------|
| Native window works (title, size, center applied) | Photino native load / P/Invoke |
| **6 `msedgewebview2.exe` processes spawn every run** | WebView2 engine *does* initialize & load content |
| Photino **4.0.16** → client area **black** (file:// *and* `LoadRawString`) | file-path / alphaTex / app.js (a raw red page also failed) |
| Photino **3.2.3** → client area **white/blank** (file:// *and* raw string) | same — even a hardcoded red page won't show |
| `--disable-gpu` and `--disable-features=CalculateNativeWinOcclusion` → no change | the usual occlusion bug; GPU-disable alone |
| WebView2 runtime **149.x** (2026 build), RTX 3060, genuine console session, no Parsec/Sunshine/AnyDesk/VNC/RDP | remote-streaming compositing; missing runtime |

### The signature

Engine runs (6 processes) and the window paints a *default* background (black on 4.x, white on 3.x), but the **navigated DOM never displays**. Our `index.html` body is `#1e1e1e` (dark) — a blank **white** client area means the page body never painted, not that it rendered dark. This is a WebView2 **compositing/surface** failure: Chromium renders to a surface that never reaches the visible window. The black↔white difference between Photino versions is just their surface clear-color.

### Leading hypothesis

The installed **WebView2 Evergreen runtime 149.x (2026) is newer than these Photino native builds expect** (3.2.3 / 4.0.16 ship ~2024–25 WebView2 SDK/loader contracts). Both Photino majors failing *differently* against one very-new runtime points at the host↔runtime contract, not anything machine-specific.

### Decisive, free next test

`app.js` already falls back to rendering `SAMPLE_TEX` when there's no Photino host (`window.external` absent). So open the built page directly in Edge:

```
J:\src\chord-flow\src\ChordFlow.App\bin\Debug\net10.0\wwwroot\index.html
```

- **Renders in Edge** → our code is correct; the issue is *purely* Photino's embedded presentation → fix the host.
- **Blank in Edge too** → system-level WebView2/GPU problem affecting all WebView2 → machine-level fix (driver/runtime).

### Options on the table

1. **Edge decoupling test** (recommended first — 30s, free, decisive).
2. **Capture WebView2 logs** — relaunch with `--enable-logging --v=1` to a writable user-data folder, read the actual GPU/compositing errors.
3. **Swap the desktop host** — alternative WebView host (Photino 2.x LTS, or WinForms/WPF `WebView2` control directly). Touches `Infrastructure/` only; engine/renderer/bridge are host-agnostic and unaffected.
4. **Pause & write up** — Phase 2 code stands complete regardless.

Note: `ChordFlow.App.csproj` is currently pinned to **Photino 3.2.3** (changed from 4.0.16 during testing) — we'll settle the final version once we know the root cause.

## Rafa:

Read https://docs.tryphotino.io/ and create a reference doc with the important info
Do 1


## AI:

## ✅ Decoupling test result — case cracked

You opened the **exact same** `file:///J:/.../bin/Debug/net10.0/wwwroot/index.html` in Edge and got **tablature + Play/Stop/tempo**. That's the decisive datapoint.

**Conclusion:** our entire stack — engine → `AlphaTexRenderer` → `app.js` → alphaTab → render → transport UI — is **100% correct**. Same WebView2 engine (149.x), same `file://` page: it renders in Edge, blank when embedded by Photino. So the failure is **isolated to Photino's native embedding of the WebView2 control** — specifically its controller→host-window compositing against the very-new runtime 149. Nothing in our code needs to change to fix this.

This also means the fix is **`Infrastructure/` only**. The engine/renderer/bridge are host-agnostic; whatever shell we land on, `Domain/`, `Rendering/`, `Features/`, and the bridge contract stay put.

(Also done: created the Photino reference doc → `loom/refs/photino-net-desktop-host-reference.md` (`rf_01KTK7ATYGHWBRR8Z6M76976ET`), capturing the verified API surface, the bridge, the `file://` gotchas, browser-args, and this investigation.)

### Fix paths, ranked

1. **Pin a fixed-version WebView2 runtime** (recommended first). Download a WebView2 *fixed-version* runtime from Photino 4.0.16's era (~WebView2 120–131) and point `WEBVIEW2_BROWSER_EXECUTABLE_FOLDER` at it. The WebView2 loader honors that env var, so Photino would use the older, compatible runtime instead of Evergreen 149 — **no code change**, just a test. If it composites, we've confirmed the root cause *and* have a shippable workaround (apps can bundle a fixed runtime).
2. **Swap host → official WinForms/WPF `WebView2` control** (`Microsoft.Web.WebView2`). Microsoft's own host, tracks the current runtime, definitely composites. Most robust long-term; a contained `Infrastructure/` rewrite of the window + bridge (the JS `window.external` shim becomes `chrome.webview.postMessage`/`addEventListener`).
3. **Try a Photino prerelease / different line** (2.x LTS or a 5.x preview) on the off chance a build already targets a 149-compatible SDK. Cheap to try, low confidence.

My instinct: **try #1 first** (proves the hypothesis, possibly fixes it with zero code), and if we'd rather not ship a bundled runtime, move to **#2** as the durable answer.

Which way do you want to go?


## AI:

## Second issue (separate from the blank window): `alphaTab error — see console`

That status text comes from our own `api.error.on(...)` handler in `app.js` — so alphaTab raised an error *after* the tablature drew. Two things are notable:

- **The tablature still rendered** → layout + the Bravura font are fine (those load via `@font-face` CSS, not a network fetch).
- The thing that *does* fetch is the **soundfont**. alphaTab loads `soundfont/sonivox.sf2` via an XHR/`fetch`, and **Chromium blocks `fetch()` of `file://` subresources from a `file://` page** (null origin → "cross-origin requests are only supported for http/https"). That's almost certainly the error.

So this is the **same root theme as the worker issue: `file://` is a crippled origin.** It affects the **real Photino app too** (Photino also serves over `file://`), so even once the window composites, **audio/playback would fail** until we fix content origin.

**The clean unifying fix:** stop using raw `file://` and serve `wwwroot` over a **custom URL scheme** (`app://…`) via Photino's scheme handler — that gives a real origin, which (a) un-blocks the soundfont fetch and workers, and (b) is just better architecture. Alternatively, hand alphaTab the soundfont as bytes (`soundFont` accepts an ArrayBuffer / base64) so nothing is fetched. The custom scheme is the more general fix; embedding bytes is the quick one.

**To confirm before I act:** in that Edge window press **F12 → Console** and paste the red error line. I expect something like *"Failed to load … sonivox.sf2 … Cross origin requests are only supported for protocol schemes: http, https…"* or a soundfont/Web Audio error. That pins it exactly rather than me inferring.

### Where this leaves us — two host-layer items, one remedy

| # | Problem | Root | Likely fix |
|---|---------|------|-----------|
| A | Blank/black client area in Photino | Photino embedding ↔ WebView2 149 compositing | fixed-version runtime, or swap host (WinForms/WPF WebView2) |
| B | alphaTab error (no audio) | soundfont fetch blocked on `file://` null origin | custom `app://` scheme **or** embed soundfont as bytes |

Both are `Infrastructure/`-only; our engine/renderer/bridge/UI are proven good. Paste the console line and tell me which fix path for **A** you want, and I'll proceed.

## Rafa:

This is Edge browser consoler errors:

```
Access to XMLHttpRequest at 'file:///J:/src/chord-flow/src/ChordFlow.App/bin/Debug/net10.0/wwwroot/soundfont/sonivox.sf2' from origin 'null' has been blocked by CORS policy: Cross origin requests are only supported for protocol schemes: chrome-extension, chrome-untrusted, data, edge, http, https, isolated-app.
alphaTab.min.js:51 [AlphaTab][AlphaSynth] Loading failed: undefined
error @ alphaTab.min.js:51
alphaTab.min.js:51 [AlphaTab][API] An unexpected error occurred Qs
    at i.onerror (alphaTab.min.js:51:737963)
error @ alphaTab.min.js:51
app.js:206 alphaTab error: Qs
    at i.onerror (alphaTab.min.js:51:737963)
(anonymous) @ app.js:206
soundfont/sonivox.sf2:1  Failed to load resource: net::ERR_FAILED
index.html:1 Unsafe attempt to load URL file:///J:/src/chord-flow/src/ChordFlow.App/bin/Debug/net10.0/wwwroot/index.html from frame with URL file:///J:/src/chord-flow/src/ChordFlow.App/bin/Debug/net10.0/wwwroot/index.html. 'file:' URLs are treated as unique security origins.
```


## AI:

## Issue B confirmed — and it points at the cleanest fix for *both*

```
Access to XMLHttpRequest at 'file:///…/soundfont/sonivox.sf2' from origin 'null'
blocked by CORS: Cross origin requests are only supported for protocol schemes:
chrome-extension, chrome-untrusted, data, edge, http, https, isolated-app.
[AlphaTab][AlphaSynth] Loading failed
```

Exactly the `file://` null-origin block. Note the **allow-list**: `http, https, data, …` — i.e. the fix is simply *"don't serve the app from `file://`."* Get a real origin and the soundfont fetch (and workers) just work.

### The realization

The official **WebView2 control has `SetVirtualHostNameToFolderMapping`** — it maps a virtual host like `https://chordflow.local/` onto our local `wwwroot` folder. The page then loads from an **`https` origin**, so the soundfont XHR is allowed. That **solves B outright** — and it comes from Microsoft's own WebView2 host, which tracks the Evergreen runtime and **composites correctly** (so it also solves **A**, the blank window).

So the two problems collapse into **one move**.

### Two coherent plans

**P1 — Keep Photino.** Fix **A** by pinning a fixed-version WebView2 runtime (`WEBVIEW2_BROWSER_EXECUTABLE_FOLDER` → an older runtime). Fix **B** with a Photino custom `app://` scheme handler (real origin) or by handing alphaTab the soundfont as bytes. *Pros:* keeps Photino's small footprint + cross-platform path. *Cons:* A's fix is unproven and bundling a fixed runtime adds ~100–150 MB; two separate fixes; we're fighting a Photino/runtime regression.

**P2 — Swap to the official WinForms (or WPF) `WebView2` host.** `Microsoft.Web.WebView2`, with `SetVirtualHostNameToFolderMapping("chordflow.local", wwwrootPath, …)` and `Navigate("https://chordflow.local/index.html")`. Fixes **A and B together**, MS-maintained, highest confidence. *Cons:* Windows-only (but the MVP is **Windows-first by design — EX8**), and the JS bridge shim changes from `window.external.{sendMessage,receiveMessage}` to `chrome.webview.postMessage` / `chrome.webview.addEventListener('message', …)`. **Infrastructure-only** — `PhotinoBridge`/`WebMessageRouter`/host get rewritten; `Domain`, `Rendering`, `Features`, `AlphaTexRenderer`, and the *envelope contract* are untouched. `app.js`'s `Bridge` module gets a small adapter (it already feature-detects, so the fallback path stays).

### My recommendation

**P2.** It's the durable answer: one move fixes both, it's Microsoft-supported, and the Windows-only trade-off is already our stated MVP scope (EX8 excludes mac/Linux packaging; the engine stays UI-agnostic so a Photino/web front-end remains a *future additive* option, not lost). P1 keeps us debugging a regression with a heavyweight workaround.

If you agree, I'll write it up as a short amendment/new plan (host swap is an architecture change, so it deserves a recorded decision) and then implement. Your call: **P2**, or stick with **P1**?

## Rafa:

Lets analyze differents paths....

Could we get more information about the error `Access to XMLHttpRequest...`?

If we go P2 we lose all Photine effort, should we try to solve with P1 B first?
At the end using thrid part libraries and depend on them always a source of problems
If we go P2 the migration is big, correct? only window? better alternative?

## AI:

Let's slow down and analyze properly — your instincts here are reasonable, and a couple of my framings need correcting.

### Q1 — Deeper on `Access to XMLHttpRequest … from origin 'null'`

What's actually happening, mechanically:
- A page loaded from `file://` is given an **opaque origin that serializes to `null`** — Chromium does this deliberately: `file://` has no trustworthy origin (any local file could be anything), so it can't be allowed to act as a normal web origin.
- `XMLHttpRequest`/`fetch` enforce CORS, and **Chromium flatly refuses XHR/fetch for the `file://` scheme** — it's not in the allowed list (`http, https, data, chrome-extension, edge, …`). It's not that the soundfont is in the "wrong" folder; **even a same-folder `file://` fetch is refused.** The scheme itself is the problem.
- Why the **tablature still rendered**: the things that loaded successfully — the Bravura font (`@font-face`), and any `<img>/<script>/<link>` — are *HTML/CSS subresource* loads, which `file://` **does** allow. Only **programmatic `fetch`/`XHR`** is blocked, and alphaTab's synth fetches the `.sf2` that way → "Loading failed".
- **This is not Edge-specific and not test-only — it applies inside Photino too** (Photino also serves `file://`). So **B is real for the actual app regardless of host**, *unless* we either (a) give the page a real origin, or (b) never fetch — hand alphaTab the soundfont as bytes.

The browser even tells us the fix in that allow-list: get onto `http/https`, a custom standard scheme, or `data:` — or bypass the fetch.

### Q2 — "Should we solve P1's B first, keep Photino?"

Here's the ordering problem: **B is moot until A is fixed.** We only *saw* B because we opened the page in Edge. In the real Photino app the window is blank (A), so there's nothing to hear anyway. So "fix B first" doesn't unblock us — **A is the actual blocker.**

So if we want to save the Photino work, the honest next step is to **decisively test A's fix**: pin a *fixed-version* WebView2 runtime (an older build from Photino's era) via `WEBVIEW2_BROWSER_EXECUTABLE_FOLDER` and see if Photino then composites. **That single experiment decides whether Photino is salvageable** — zero code change. If it renders → Photino lives, and B is then a 20-minute fix (ship the soundfont as bytes over the bridge → `api.loadSoundFont(...)`, no fetch). If it stays blank → we have proof, not a guess, that it's P2.

I'd rather run that test than abandon Photino on a hunch. **Want me to run the fixed-runtime test now?**

### Q3 — "With P2 we lose all the Photino effort"

This framing is the one I most want to correct, because it's steering the decision and it's not accurate. Tally of what P2 actually touches:

**Lost (Photino-specific glue, ~70 lines total):** the `PhotinoWindow` setup in `Program.cs`, `Infrastructure/PhotinoBridge.cs`, and the Photino package ref.

**Kept (everything of substance):**
- All of `Domain/` (the engine — the actual product) ✅
- `Rendering/AlphaTexRenderer` ✅
- All of `Features/` — `GenerateExercise`, `PracticeSession` ✅
- `Infrastructure/WebMessageRouter` (envelope parsing — host-agnostic) ✅
- The **entire `wwwroot/`** — `index.html`, `app.js`, alphaTab, Bravura, soundfont ✅
- The bridge **envelope contract** (`loadScore`/`play`/`stop`/`ready`/…) ✅
- All **26 tests** ✅

So it's not "lose all the effort" — it's "swap ~70 lines of window-and-bridge glue." The Photino *learning* is captured in the reference doc and stays useful if we ever re-host. Phases 1–2 were 95% host-agnostic *on purpose* (the design's "engine kept UI-agnostic" principle) — this is exactly the situation that design choice was protecting us from.

### Q4 — Third-party dependency risk

You're right in general — but look at *which* dependencies, because P2 actually **reduces** third-party risk:
- **alphaTab** — third-party, but it *is* the core value (notation + playback + synced cursor). Irreplaceable for the MVP, and proven working in Edge.
- **WebView2 runtime** — **Microsoft, ships with Windows.** Depending on it ≈ depending on the OS. Lowest possible risk.
- **Photino** — third-party, thin wrapper, and *the thing currently failing.*
- **`Microsoft.Web.WebView2` (WinForms/WPF control)** — **Microsoft first-party.**

So P2 *replaces a struggling third-party wrapper (Photino) with Microsoft's own first-party host* over the same Microsoft runtime. That's **fewer and safer** third-party deps, not more. Your concern actually argues *for* P2.

### Q4b — Migration size, Windows-only, better alternatives?

- **Size:** Infrastructure-only, modest. New host window (~60 lines WinForms/WPF), a WebView2 bridge replacing PhotinoBridge (~40 lines), and ~15 lines in `app.js`'s `Bridge` module (`window.external` → `chrome.webview.postMessage`/`addEventListener`). Everything else untouched. Not a big migration.
- **Windows-only:** yes — WinForms/WPF are Windows-only. But the MVP is **Windows-first by explicit scope (EX8)**, and the engine stays UI-agnostic, so a cross-platform/web front-end later is *additive*, not a rewrite.
- **Alternatives, honestly ranked:**
  1. **Official WebView2 in WinForms/WPF (P2)** — Windows-only, first-party, `SetVirtualHostNameToFolderMapping` gives an `https://chordflow.local/` origin that fixes B, composites fine (A). Best for a Windows MVP.
  2. **Photino + fixed-version runtime (P1)** — keeps cross-platform potential, but A's fix is unproven and bundling a fixed runtime adds ~100–150 MB.
  3. **Avalonia + WebView** — cross-platform, but its WebView support is immature/third-party → *more* risk.
  4. **Local Kestrel server on `http://localhost`** — would fix B trivially and work under *any* webview… but **constraint C2 forbids a localhost port**, so it's out. (Worth noting: P2's virtual-host mapping gives a real `https` origin via an *in-process intercept* — **no actual port/server** — so it satisfies C2 where a real localhost server would violate it.)
  5. **Tauri/Electron** — abandons the C# host entirely; contradicts the "C# everywhere" principle. No.

### Where I land

Two clean ways forward, and I don't think we should pick blind:
- **First, run the fixed-runtime test for A** (cheap, decisive, saves Photino if it works).
- If that fixes it → **P1**, keep Photino, then the small B-as-bytes fix.
- If it doesn't → **P2** with proof, knowing it costs ~70 lines and *reduces* third-party risk.

Shall I run the fixed-runtime test now so we're deciding on evidence rather than preference?

## Rafa:

go ahead and inform with results


## AI:

## Web-search results — hypothesis reshaped

I searched the known-issue landscape before committing to the fixed-runtime download. Results:

- **Photino #258 "Black Photino Window"** → **Linux Mint only**; fix is `LIBGL_ALWAYS_SOFTWARE=1`. The report even says *"Windows working without issues."* Not our case.
- **Registry `…\Avalon.Graphics\DisableHWAcceleration=1`** (WebView2Feedback #5281 — composition control renders blank) → **checked this machine: not set.** Ruled out.
- The hits that *do* match our signature: **WebView2Feedback #1077 "blank when created in the background"** and **#2983 "blank when a non-activated/hidden window is resized"**. WebView2's **composition controller** (the DComp visual-hosting path Photino uses) can come up **blank when the host window isn't shown/activated at the moment the controller is created**. Edge uses a plain *windowed* controller, not composition — which is exactly why **Edge renders and Photino doesn't**, and why *both* Photino 3.x and 4.x fail.

**Implication:** the *fixed-version-runtime* test was aimed at "runtime 149 too new" — but this evidence says the cause is more likely the **composition-controller-created-while-hidden** bug, which a different runtime wouldn't fix. So I'm running the cheaper, more on-target test instead.

## Please do this 10-second test

The app is **running now** (Photino 4.0.16, black window). With that black ChordFlow window:

1. **Grab a window edge/corner and resize it** (make it noticeably bigger or smaller), or maximize then restore it.
2. Optionally click into it / alt-tab away and back to force re-activation.

**Does the tablature + transport bar suddenly appear after the resize/reactivate?**

- **Yes, it paints after resizing** → confirmed: it's the "WebView2 composition surface created while the window wasn't shown" bug. That's a *known* Photino-on-Windows weakness — fixable, but only with a hacky post-show resize nudge (Photino has no clean API for it, since `WaitForClose` blocks the thread).
- **Still black after resizing** → composition path is broken regardless; the fixed-runtime download is the only remaining P1 lever, and frankly P2 starts looking like the responsible call.

Either outcome is decision-grade. Tell me what the resize does.

(Note: this is also itself a data point about Photino — its Windows rendering shows up across *several* known-issue threads with finicky, version-specific workarounds. The official WebView2 control doesn't carry that baggage.)

## Rafa:

Always black
should we consider kestrel or electron better than WPF?


## AI:

## Resize didn't fix it → composition path is just broken here

So it's not the "created-while-hidden" timing variant — Photino's **composition controller can't present on this machine at all**, and a resize/reactivate doesn't recover it. Combined with everything else, **P1 (keep Photino) is effectively dead.** Let's pick the replacement properly.

## Kestrel vs Electron vs the WebView2 control — measured against *our* constraints

**Kestrel (local ASP.NET server) — ❌ wrong tool here.** Two problems: (1) it **violates C2** outright ("no external web server, no localhost port"); (2) it only gives a real origin (fixes **B**) — it is **not a display host.** You'd *still* need a window/webview to show the page, so it doesn't solve **A** (the blank window) at all. It solves the one problem we already have a port-free fix for, while breaking a constraint. No.

**Electron — ❌ wrong stack.** Its one real advantage is that it **bundles its own Chromium**, so it's immune to the system-WebView2 composition bug. But it **abandons the C# host** — Electron is Node/JS. Our entire value is the **C# engine** (Domain/Rendering/Features); under Electron we'd have to run that as a sidecar process and bridge it over IPC/stdio, or rewrite it in JS. That contradicts **C1** ("C# wherever possible"), balloons the app (~150 MB Chromium), and is far more than a solo-dev MVP should carry. (Electron.NET exists but just hosts ASP.NET over a localhost port → straight back into the C2 problem.) No.

**Official WebView2 control in C# — ✅ the right answer, with one important refinement.** Same engine, but Microsoft's *reference host* that handles the controller lifecycle the bugs Photino trips on. And here's the key detail I should have led with:

> The **WinForms** `WebView2` control uses a **windowed controller** (HWND-hosted) — the *same* presentation path **Edge uses, which we proved renders on this machine.** The **WPF** control uses the **composition controller** — the *same* path Photino uses, which is failing here.

So **WinForms + WebView2 is specifically the variant most likely to render on this box**, because it sidesteps the exact composition path that's broken. That's a meaningful de-risk over WPF. It also keeps everything: C# engine, renderer, features, all of `wwwroot`, the envelope contract, 26 tests; `SetVirtualHostNameToFolderMapping` gives an `https://chordflow.local/` origin (fixes **B**, no port → C2-safe).

### Honest ranking for our case

| Option | Renders here? | C# engine kept | C2 (no port) | Weight | Verdict |
|--------|---------------|----------------|--------------|--------|---------|
| **WinForms + WebView2 (windowed)** | **very likely** (Edge-style path) | ✅ | ✅ (virtual host) | light | **recommended** |
| WPF + WebView2 (composition) | risky (Photino's failing path) | ✅ | ✅ | light | fallback only |
| Kestrel + a webview | doesn't address A | ✅ | ❌ violates C2 | medium | no |
| Electron (+ C# sidecar) | yes (own Chromium) | ✗ (JS host) | ❌ (IPC/port) | heavy | no |

### Proposed next step — a 15-minute de-risking spike

Before committing the migration, let me build a **throwaway ~40-line WinForms + WebView2 app** that loads our existing `wwwroot` via virtual-host mapping. One of two outcomes:
- **It renders the tablature + plays** → P2 confirmed *and* we've basically written the new host; I proceed to wire the bridge (`window.external` → `chrome.webview`) and we're back on track.
- **It's also black** → then it's a deeper machine/runtime problem affecting *all* embedded WebView2, and Electron's bundled-Chromium independence genuinely becomes worth the weight — we'd reconsider with that proof.

This spike is the cheapest way to *know* rather than argue. **Want me to run it?**


## AI:

## "Why does it fail here when so many devs use Photino fine?"

Fair question — and worth taking seriously rather than waving away. Here's the honest answer.

**First, what we've ruled out — it isn't us misusing it.**
- Our code is correct: the *exact same* page renders fully in Edge.
- Our Photino usage is correct: even Photino's own `LoadRawString` of a bare red page is black. There's no "enable rendering" flag we skipped — the native window, title, size, and 6 WebView2 processes all come up; only the **composition surface** presents black. We used the documented quickstart API (verified against the actual DLL).

So it's not a skill/config gap. That leaves: **our stack is newer than what Photino 4.0.16 was built and tested against.** Three specifics:

1. **WebView2 runtime 149.x (a 2026 build).** Photino 4.0.16 shipped ~early 2025 against a much earlier runtime. WebView2's **composition-controller** rendering behavior shifts across Chromium versions; a regression on 149 would hit *anyone* on 149 — but very few are on it yet. Most "works fine" users are on older, settled runtimes.
2. **.NET 10.** Photino.NET 4.0.16's package targets **net8.0 / net9.0** (I read its nuspec) — **not net10.** It runs on net10 via roll-forward, but we're off its tested path.
3. **This machine's GPU/driver + the composition path.** The issue threads we found are full of WebView2-composition + specific-driver black-screen reports. Many devs never trip it because their runtime/driver/GPU combo differs.

**And Photino's Windows rendering genuinely is a known soft spot** — we found *multiple* "black Photino window" issue threads (Linux `LIBGL_ALWAYS_SOFTWARE`, Windows GPU/composition). That's the tell: it's not that we're uniquely wrong, it's that Photino's composition path is the fragile part, and we're on the newest possible stack that exposes it. The people for whom it "just works" are mostly on net8/9 + an older runtime + different hardware — and the ones who *do* hit it are why those issue threads exist.

**The honest bottom line:** "many devs use it fine" and "it's black on this exact net10 + WebView2-149 + RTX-3060 box" are both true at once. It's a **version/environment incompatibility on a bleeding-edge stack**, not a usage error.

**The WinForms spike settles it for certain.** WinForms WebView2 uses the *windowed* controller (Edge's proven-working path), not composition. So:
- If the spike **renders** → it confirms the culprit is *specifically Photino's composition path on this stack* — and we have our host.
- If the spike is **also black** → then *all* embedded WebView2 is broken on this machine (a runtime/driver problem, still not our code — Edge's own windowed process works), and that's the real story.

Either way we stop speculating and *know why*. Shall I run it?

## Rafa:

yes, run it.
briefly show pros & cons between winforms and wpf


## AI:

## Spike is running — what do you see?

A window titled **"WebView2 Spike — ChordFlow"** is open (official WinForms WebView2, serving our real `wwwroot` over `https://chordflow.local/`). Tell me:

1. **Tablature + transport bar visible?** (the rendering / issue-A test)
2. **Status line top-right** — does it reach *"ready · soundfont loaded"* now (no alphaTab error), since the soundfont is same-origin over https? (the issue-B test)
3. If it rendered: click **▶ Play** — audio + moving cursor?

(It uses the `SAMPLE_TEX` fallback since there's no Photino `window.external` here — that's expected; this spike only tests the host, not the bridge.)

## WinForms vs WPF for the WebView2 host — brief

| | **WinForms + WebView2** | **WPF + WebView2** |
|---|---|---|
| Controller | **Windowed (HWND)** — *Edge's path, proven to render on this box* | **Composition (DComp)** — *same path Photino is failing on here* |
| Risk on this machine | **Low** | Higher (could repeat the black window) |
| Ceremony | Minimal — one Form, dock-fill the control | More — XAML, App.xaml, HwndHost plumbing |
| Strength | Dead-simple "one full-window webview" host | Rich native UI, binding, theming, DPI niceties |
| Relevance to us | We need exactly a full-window webview — no native chrome | We'd use ~none of WPF's extra power |
| Windows-only | Yes | Yes |

**Verdict:** for ChordFlow the **window *is* the web app** — there's no native UI to build — so WPF's advantages don't apply, and its composition controller is the very thing breaking here. **WinForms is the lower-risk, simpler, better fit.** (If we ever want a fancy native shell around the WebView later, WPF is the upgrade path — but that's not the MVP.)

Waiting on what the spike window shows.


## AI:

## ✅ Spike renders perfectly — root cause confirmed, decision settled

That's conclusive:
- **Issue A solved.** The official **WinForms WebView2 (windowed controller)** renders our exact `wwwroot` on this machine. So the culprit was *specifically Photino's composition-controller path on this net10 + WebView2-149 stack* — not our code, not the runtime broadly (Edge's windowed path and now this windowed control both work).
- **Issue B solved too** (please just confirm): served over `https://chordflow.local/`, the soundfont is same-origin — so the status line should now reach **"ready · soundfont loaded"** with no alphaTab error, and **▶ Play** should give audio + a moving cursor. Can you confirm the audio/cursor work?

**Decision: P2 with WinForms.** We migrate the host from Photino → official WinForms + WebView2.

### Migration — `Infrastructure/` only, contained

1. **`ChordFlow.App.csproj`**: `TargetFramework` → `net10.0-windows`, `UseWindowsForms=true`, `OutputType=WinExe`; drop `Photino.NET`, add `Microsoft.Web.WebView2`. (Keep the wwwroot copy.)
2. **`Program.cs`**: a WinForms `Form` with a dock-filled `WebView2`; `EnsureCoreWebView2Async` → `SetVirtualHostNameToFolderMapping("chordflow.local", wwwroot, Allow)` → `Navigate("https://chordflow.local/index.html")`.
3. **`Infrastructure/PhotinoBridge.cs` → `WebView2Bridge.cs`**: C#→JS via `CoreWebView2.PostWebMessageAsString(json)`; JS→C# via `CoreWebView2.WebMessageReceived`. **`WebMessageRouter` unchanged** (still parses the same JSON envelopes).
4. **`wwwroot/app.js`** `Bridge` module: swap `window.external.{sendMessage,receiveMessage}` → `window.chrome.webview.postMessage(...)` / `chrome.webview.addEventListener('message', e => …)`. The no-host fallback stays. (And since we now have a real origin, workers could be re-enabled — minor, optional.)

**Untouched:** `Domain/`, `Rendering/AlphaTexRenderer`, `Features/` (GenerateExercise, PracticeSession), `WebMessageRouter`, the envelope contract, all 26 tests, and 95% of `wwwroot`.

### Loom bookkeeping

This changes the architecture baseline (design §4 says "desktop-first Photino"). I'd: (a) **amend the design** — host = WinForms + WebView2, bridge over `chrome.webview`, virtual-host origin; note Photino retired with the reason; (b) **create a short migration plan** (Phase 2b — host swap) with the 4 steps above; then implement. The Photino reference doc stays (still accurate; I'll add a "why we moved off it" note).

**Green light to do the design amendment + migration plan, then implement?** Or want to keep the spike up and poke at it more first?

## Rafa:

Go ahead with all, update design, refine reqs (all should be covered), migration plan, do whole plan


## AI:

## Migration implemented — please confirm the running app

All the bookkeeping + code is done:

**Docs (consistent set):**
- **Requirements refined** (`req.md`) — `IN6` → WinForms + WebView2 + virtual-host; `C2` clarified (virtual-host = in-process origin, no port); `IN8` notes `chrome.webview` transport; `EX5/EX8` note Windows-only host but portable engine; `C8` covers the WebView2 surface. *(Did req first, then design, so they stay in sync — re your note.)*
- **Design updated** (`mvp-design.md` v2) — host-decision banner, §1 layout, §4 host & bridge (WebView2 + virtual host + `chrome.webview`), §7 risks marked resolved, §8 adds the Phase-2b step.
- **Photino ref doc** — added a ✅ RESOLUTION section (root cause = composition vs windowed controller; decision to migrate).
- **Migration plan** `mvp-plan-004` created + driven.

**Code (plan-004, steps 1–3 done, ✅ build/tests):**
- csproj → `net10.0-windows` / WinForms / WebView2 (Photino removed); test csproj realigned.
- `Program.cs` → WinForms `Form` + dock-filled `WebView2`; `SetVirtualHostNameToFolderMapping` + `Navigate("https://chordflow.local/index.html")`.
- New `WebView2Bridge` (+ small `IBridge` seam so slices don't bind to the host); `PhotinoBridge` deleted; `PracticeSession` now takes `IBridge`.
- `app.js` Bridge → `window.chrome.webview`; `useWorkers` re-enabled (real origin).
- **`dotnet test` 26/26 green** on net10.0-windows; spike deleted; no Photino leftovers in output.

**The app is running now (the real bridge path, not the dev fallback).** Please confirm:
1. **Tablature renders?** (12-bar Bb blues, pushed by the C# engine over the bridge)
2. **Status top-right reaches "ready · soundfont loaded"** (no alphaTab error — issue B fixed by the https origin)?
3. **▶ Play → audio + the beat cursor highlighting in time?** Stop resets; tempo changes speed?

If all three are good, I'll mark step 4 ✅ (plan-004 complete) and we're fully migrated. If anything's off, tell me what and I'll fix it. (The one thing I'd watch: `useWorkers:true` — if the tab doesn't render, that's the first thing I revert to the known-good `false`.)


## AI:

## Rendering ✅ — fixing the missing cursor

Good news first: **tablature renders** under the WinForms WebView2 host → issue A is fully solved, migration is working.

**No cursor:** that's an alphaTab config gap, not a host problem. In alphaTab 1.8.3 the beat/bar cursor is **not** automatically on just because the player is enabled — it needs explicit flags. I verified the settings exist in the bundle (`enableCursor`, `enableAnimatedBeatCursor`, `enableElementHighlighting`, `at-cursor`) and added them:

```js
player: {
  enablePlayer: true,
  enableCursor: true,              // ← show the bar/beat cursor
  enableAnimatedBeatCursor: true,  // ← animate it between beats
  enableElementHighlighting: true, // ← highlight the active beat's notes
  soundFont: "soundfont/sonivox.sf2",
  scrollMode: alphaTab.ScrollMode.Off,
}
```

Rebuilt + **relaunched**. Please press **▶ Play** and tell me:
1. **Does the beat cursor now appear and track in time?** (a highlighted bar + a moving beat line)
2. **Is there audio?** (and does the status top-right read **"ready · soundfont loaded"**?)

If the cursor shows but you want it more visible, I can tune its styling. If it still doesn't appear, the next likely culprit is the cursor overlay needing `position: relative` on the score container — I'll add that. And if there's no audio / the status didn't reach "soundfont loaded", that's a separate thread to pull. What do you see?

## Rafa:

1. no cursor yet
2. audio is OK

## AI:

## Audio ✅ — cursor fix #2: it needed CSS

Audio works, so playback + soundfont (issue B) are fully good. The cursor was *enabled* but **invisible** — the classic alphaTab gotcha: alphaTab creates and positions the cursor elements but **does not color them**; you have to supply the cursor CSS or they render transparent. Added the standard rules to `index.html`:

```css
#score { position: relative; }                 /* anchor the cursor overlay */
.at-cursor-bar  { background: rgba(255,242,0,0.25); }       /* current-bar highlight */
.at-cursor-beat { background: rgba(64,64,255,0.75); width:3px; } /* moving beat line */
.at-highlight * { fill:#2867f5; stroke:#2867f5; }           /* active beat's notes */
```

Rebuilt + **relaunched** (I stopped the old instance you closed). Press **▶ Play**:

- You should now see a **pale-yellow highlight on the current bar** and a **blue beat line that steps in time** with the audio, with the active beat's notes turning blue.

Does the cursor show and track now? If yes, that's IN7's synced cursor satisfied and I'll close out step 4 → plan-004 done. If it's still missing, I'll enable WebView2 DevTools so we can inspect whether the `.at-cursor-*` elements are in the DOM.

## Rafa:

Cursos showed up
Current bar highlighted
Current notes highlighted
Let me click on any spot and move cursor


## AI:

🎉 **That's the whole Phase-2 goal working:** tablature renders, audio plays, and the **synced beat cursor + bar/note highlighting track in time** — req **IN7** satisfied, on the new WinForms + WebView2 host.

On your click test: clicking a note/beat in the notation is alphaTab's built-in **seek** — it moves the playback position (and the cursor) to that beat; pressing Play resumes from there. That's a free alphaTab behavior with the player enabled — no code from us. So go ahead and click around; the cursor should jump to wherever you click.

Once you're happy with the click-to-seek behavior, say the word and I'll **mark step 4 ✅ → plan-004 (host migration) complete**. The full picture then:
- **Phase 2** (plan-002): engine → render → bridge → playback — done.
- **Phase 2b** (plan-004): Photino → WinForms + WebView2, virtual-host origin, `chrome.webview` bridge — done, with rendering + audio + synced cursor all verified live.

Nothing's committed yet (working tree only, per your solo-dev/main workflow). When we close step 4 I can also offer to commit the migration. What does the click-seek do?

## Rafa:

mark step 4 done
this is v0.1.0 , create CHANGELOG, update README, etc
commit, push, tag, release



## AI:

## 🚀 v0.1.0 shipped

**Release:** https://github.com/reslava/chord-flow/releases/tag/v0.1.0

- **Step 4 ✅ → plan-004 (host migration) done.** Phase 2 + Phase 2b complete; render + audio + synced cursor verified live.
- **Commit** `7831a2d` on `main` (no attribution), pushed.
- **Tag** `v0.1.0` (annotated), pushed.
- **GitHub release** created with notes.

**Repo housekeeping:**
- `README.md` + `CHANGELOG.md` added; `<Version>0.1.0</Version>` set.
- **Soundfont handled per the `.gitignore` intent** ("bundled at build time, not committed"): added an MSBuild `DownloadFile` target that fetches `sonivox.sf2` on first build if missing — verified by deleting it and rebuilding (re-downloaded, 1.29 MB). The Apache-2.0 `LICENSE`/`README` for it *are* committed; the binary isn't.
- 27 files committed; **no `.sf2`** in the tree (sanity-checked); 26 tests green.

That closes the black-window saga: root-caused (Photino composition controller vs WinForms windowed controller on the net10 + WebView2-149 stack), migrated, documented (design + reqs + `photino-net-desktop-host-reference.md`), and released.

Next phase whenever you want it: SQLite persistence + the on-screen pickers (key/rhythm/tempo/generate/save) — `IN9`/`IN10`. Say `close` if you want to close this chat.