---
type: plan
id: pl_01KVNGT653V1GCF27E4Q4NX3HH
title: ChordFlow user guide — Plan
status: done
created: 2026-06-21
updated: 2026-06-21
version: 1
design_version: 1
req_version: 2
tags: []
parent_id: de_01KVNEHZVQJP675M66SV68MD2P
requires_load: []
target_version: 0.1.0
actual_release: 0.9.0
steps:
  - id: author-the-guide-prose
    order: 1
    status: done
    description: Write docs/user-guide.md — the six sections (what it is · install & first run · build & play · your own content · soundfonts · known limits)
    files_touched: [docs/user-guide.md]
    blocked_by: []
    satisfies: [IN1, IN2, IN3, IN4, IN5, C1, C4, C6]
  - id: screenshots-in-the-guide
    order: 2
    status: done
    description: Embed screenshots in the guide as ~480px thumbnails clickable to full size
    files_touched: [docs/user-guide.md]
    blocked_by: [1]
    satisfies: [IN6, C2]
  - id: readme-guide-link-tour
    order: 3
    status: done
    description: "README: link the guide + add a screenshot feature tour + show the app icon"
    files_touched: [README.md]
    blocked_by: [1]
    satisfies: [IN1, IN6, C2]
  - id: app-icon-wiring
    order: 4
    status: done
    description: Wire images/icon.ico as the desktop application icon (.exe + window) and show it in both docs' headers
    files_touched: [src/ChordFlow.Desktop/ChordFlow.Desktop.csproj, src/ChordFlow.Desktop/wwwroot/.. (WinForms shell .cs), docs/user-guide.md, README.md]
    blocked_by: []
    satisfies: [IN7, C3]
  - id: doc-review-registration
    order: 5
    status: done
    description: Register the guide in the release doc-accuracy review list
    files_touched: [RELEASING.md]
    blocked_by: []
    satisfies: [IN9]
---
# ChordFlow user guide — Plan

## Goal

Write and publish the end-user guide as `docs/user-guide.md`, link it from the README, embed the existing screenshots in both docs as clickable thumbnails, wire `images/icon.ico` as the desktop application icon, and register the guide in the release doc-accuracy review list. Grounded against the live UI + README per the locked req (`rq_01KVNG7ZYZ2XYYQ0M2E94TJ8DY`). The in-zip bundling of `USERGUIDE.md` + images (`IN8`) is out of scope here — it is owned by the `guide-zip-bundle` thread (`C5`); the SmartScreen screenshot is dropped (`C6`, text-only coverage remains).

---

## Steps

| Done | # | Step | Files touched | Blocked by | Satisfies |
|---|---|---|---|---|---|
| ✅ | 1 | Write docs/user-guide.md — the six sections (what it is · install & first run · build & play · your own content · soundfonts · known limits) | docs/user-guide.md | — | IN1, IN2, IN3, IN4, IN5, C1, C4, C6 |
| ✅ | 2 | Embed screenshots in the guide as ~480px thumbnails clickable to full size | docs/user-guide.md | 1 | IN6, C2 |
| ✅ | 3 | README: link the guide + add a screenshot feature tour + show the app icon | README.md | 1 | IN1, IN6, C2 |
| ✅ | 4 | Wire images/icon.ico as the desktop application icon (.exe + window) and show it in both docs' headers | src/ChordFlow.Desktop/ChordFlow.Desktop.csproj, src/ChordFlow.Desktop/wwwroot/.. (WinForms shell .cs), docs/user-guide.md, README.md | — | IN7, C3 |
| ✅ | 5 | Register the guide in the release doc-accuracy review list | RELEASING.md | — | IN9 |
---

### Legend

| Symbol | Meaning |
|--------|---------|
| ✅ | Done |
| 🔄 | In Progress |
| 🔳 | Pending |
| ❌ | Cancelled |

<!-- step:author-the-guide-prose -->
### Step 1 — Author the guide prose

Six sections per IN2. Ground every control name against the live UI (IN3/C4): builder pickers **Harmony · Comping · Lead · Key · Difficulty · Feel** + **Generate · Save · Mark practiced**; transport **Play · Stop · Tempo** (+ Metronome / Count-in / Rhythm vol / Lead vol / Sound / diagram toggles); the *Saved exercises* list. §2 covers the SmartScreen 'unknown publisher' prompt in **text only** — no screenshot (C6). §4 covers the Content view, **links** `loom/refs/chordflow-dsl-reference.md`, and includes **one tiny inline example in real Nashville scale-degree DSL** (load the DSL ref first, C4). §5: bundled **Sonivox** default, add your own `.sf2`/`.sf3` into `wwwroot/soundfont/` next to the exe → auto-discovered → pick in the **Sound** dropdown, and **link** (not duplicate) the README download list. No screenshots yet — that's step 2.

<!-- step:screenshots-in-the-guide -->
### Step 2 — Screenshots in the guide

Use `<a href="images/screenshots/X.png"><img src="images/screenshots/X.png" width="480"></a>` (C2). §3 → `01-practice.png`; §4 → `02-content-progressions.png` (optionally `03-content-songs`, `04-content-rhythms`). The dogfood shots (`05-scales`, `06-caged-chords`, `10-debug`) stay out of the guide (EX2). NB: in-zip image paths are settled by the `guide-zip-bundle` thread — keep `src` paths as repo-relative `images/screenshots/...` for now.

<!-- step:readme-guide-link-tour -->
### Step 3 — README guide link + tour

Add a prominent link to `docs/user-guide.md`. Add a screenshot feature tour using the same clickable-thumbnail technique (C2); the README tour may include the dogfood shots (`05-scales`, `06-caged-chords`, `10-debug`) that the guide omits. Show `images/icon.png` in the header.

<!-- step:app-icon-wiring -->
### Step 4 — App-icon wiring

Isolated Desktop code/build step (kept in this thread by Rafa's call). Add `<ApplicationIcon>images/icon.ico</ApplicationIcon>` (path relative to the csproj — adjust to `..\..\images\icon.ico` or copy into the project) for the `.exe` icon, and set `Form.Icon` on the WinForms shell for the window icon. Confirm both the taskbar/exe and the window title show the mark. icon.ico is already committed (C3) — wire only, no conversion. Also embed the icon in the docs' headers.

<!-- step:doc-review-registration -->
### Step 5 — Doc-review registration

Add `docs/user-guide.md` to the release doc-accuracy review checklist (alongside README + the three refs). Locate the list in `RELEASING.md` or the `/do-release` flow. This is a one-line checklist add, not pipeline machinery (the bundling itself is `guide-zip-bundle`, C5).
