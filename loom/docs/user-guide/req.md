---
type: req
id: rq_01KVNG7ZYZ2XYYQ0M2E94TJ8DY
title: ChordFlow user guide — Requirements
status: locked
created: 2026-06-21
updated: 2026-06-21
version: 3
design_version: 5
tags: []
parent_id: de_01KVNEHZVQJP675M66SV68MD2P
requires_load: []
---
# ChordFlow user guide — Requirements

### ✅ Included

- `IN1` An **end-user** guide for downloaders (not a developer/architecture doc), published as **`docs/user-guide.md`** at the repo root and linked from the README.
- `IN2` The guide covers, in order: (1) what ChordFlow is, (2) install & first run incl. the **SmartScreen "unknown publisher"** prompt (text only — see `C6`), (3) build & play an exercise, (4) your own content, (5) soundfonts, (6) known limits.
- `IN3` The build-&-play walkthrough names the **real UI controls**: builder pickers **Harmony · Comping · Lead · Key · Difficulty · Feel** + **Generate · Save · Mark practiced**; transport **Play · Stop · Tempo** (+ Metronome / Count-in / volumes); the *Saved exercises* list (click to reload).
- `IN4` The soundfonts section documents the bundled **Sonivox** default, adding your own (`.sf2`/`.sf3` into **`wwwroot/soundfont/` next to `ChordFlow.exe`** → auto-discovered → selected in the transport **Sound** dropdown), and **links** the README's curated download list (no duplication).
- `IN5` The "your own content" section covers the **Content** view and **links the DSL reference** (`chordflow-dsl-reference.md`) **plus one tiny inline example in real Nashville scale-degree DSL**.
- `IN6` **Screenshots** from `images/screenshots/` appear in **both the guide and the README**, rendered as **~480px thumbnails clickable to full size**. Guide uses `01-practice` (build & play) and the `02/03/04-content-*` shots (content); the README feature tour may use the rest.
- `IN7` The app icon **`images/icon.ico`** is wired as the desktop **application icon** — `<ApplicationIcon>` in `ChordFlow.Desktop.csproj` (the `.exe`) **and** `Form.Icon` on the WinForms shell (the window) — and the icon appears in both docs' headers. (One isolated plan step.)
- `IN8` ~relocated~ — the in-zip bundling (`USERGUIDE.md` + `images/` into the artifact) is **owned by the `guide-zip-bundle` thread** (`th_01KVNGQA9CNXD7KY1ZWNW308TW`), not this thread. Retired here so this thread's scope verifies clean; the requirement lives in that thread (see `C5`).
- `IN9` The guide joins the release **doc-accuracy review** list (alongside README + the three refs).

### ❌ Excluded

- `EX1` An **in-app help page** (`wwwroot`-served) — deferred (option C), revisited after the prose exists.
- `EX2` Documenting the **Scales / CAGED / CAGED Chords / Debug** views — dogfood/dev pages, not end-user features.
- `EX3` Developer/architecture content — the bridge, project structure, build-from-source.
- `EX4` **Code signing** to remove the SmartScreen warning — accepted and documented, not fixed here (mirrors `release-pipeline` `EX2`).

### ⛓ Constraints

- `C1` Single home: repo **`docs/user-guide.md`** → zip **`USERGUIDE.md`**; the Loom thread is the source of record, the repo file the deliverable.
- `C2` Screenshots are resized only via HTML `<a href="full"><img width="480"></a>` (the GitHub-markdown-safe resize), at a consistent thumbnail width.
- `C3` The app icon must be a Windows multi-resolution **`.ico`** (`images/icon.ico`, already committed) — wired, not converted.
- `C4` Grounding: control names, soundfont paths, and the bundled-font name match the **live UI + README**; the DSL example uses real Nashville-degree notation (load `chordflow-dsl-reference.md` before writing it).
- `C5` The in-zip image-bundling (`IN8`) is implemented by a **dedicated thread, `guide-zip-bundle`** (`th_01KVNGQA9CNXD7KY1ZWNW308TW`, in the `release` weave, `depends_on` this thread), which also **amends `release-pipeline/req.md` `EX6`**. No `release-pipeline` machinery is built in *this* thread — it is a named downstream dependency only.
- `C6` ~dropped~ — the §2 SmartScreen **screenshot is deferred completely** (Rafa's call): no screenshot and no placeholder. §2 still describes the unknown-publisher prompt in **text** (per `IN2`); only the image is dropped.
