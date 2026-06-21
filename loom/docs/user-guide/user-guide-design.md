---
type: design
id: de_01KVNEHZVQJP675M66SV68MD2P
title: ChordFlow user guide — Design
status: done
created: 2026-06-21
updated: 2026-06-21
version: 5
tags: []
parent_id: id_01KVB4KGMYZTB1FXVB3XJ82HTG
requires_load: []
---
# ChordFlow user guide — Design

## Goal

Write an **end-user** guide for ChordFlow — for guitarists who download the Windows release zip, not for developers. It answers: how do I install and run this, how do I build & play an exercise, and how do I use my own soundfont. Authored in this thread; published as a repo doc and bundled into the release.

## Decisions (resolved in `user-guide-chat-001.md`)

1. **Home = top-level repo doc.** Publish as **`docs/user-guide.md`** at the repo root, linked from the README. **Not** a `loom/refs/` doc: `loom/refs/` is the *system mirror* (architecture / domain / DSL grammar) under bidirectional sync with code; an end-user how-to is a different genre and the idea itself frames it as "**not** a developer/architecture doc." The DSL-ref precedent (public, README-linked) holds only because the DSL ref mirrors a system artifact.
2. **Authoring vs. publishing split.** The guide is *authored* the Loom way in this thread (idea → design → req → plan → content). The published `docs/user-guide.md` is the rendered output — the Loom thread is the source of record, the repo file is the deliverable.
3. **Release-zip handoff.** The release pipeline copies the *same* `docs/user-guide.md` into the zip as **`USERGUIDE.md`** at the zip root, next to `ChordFlow.exe`. **Chosen filename: `docs/user-guide.md` in the repo → `USERGUIDE.md` in the zip** (Q4) — an all-caps top-level `USERGUIDE.md` is the convention a downloader expects to find beside the exe, while `docs/user-guide.md` is the tidy repo home. The guide joins the release **doc-accuracy review** list alongside the README + the three refs.
4. **In-app help (option C) is deferred** — a `wwwroot`-served help page is a future presentation layer over the already-written prose; out of scope here.
5. **Screenshots + app icon** (new, this chat — see "Assets" below).

## Assets (screenshots + icon)

Rafa added `images/icon.png` and `images/screenshots/*.png` to the repo. Verified present:

- `images/icon.png` — three-guitars-with-notes mark.
- `images/screenshots/` — `01-practice`, `02-content-progressions`, `03-content-songs`, `04-content-rhythms`, `05-scales`, `06-caged-chords`, `10-debug`.

Decisions:

- **Screenshots in BOTH the user guide and the README**, rendered **smaller + clickable to full size**. GitHub-markdown technique: `<a href="images/.../X.png"><img src="images/.../X.png" width="480"></a>` (HTML `<img width>` is the only reliable resize on GitHub; bare markdown can't). Use a consistent thumbnail width (~480px).
- **Screenshot → guide-section mapping** (the guide only shows end-user screens; the dogfood shots are README-tour only):
  - §3 Build & play → `01-practice.png`.
  - §4 Your own content → `02-content-progressions` (+ optionally `03-content-songs`, `04-content-rhythms`).
  - `05-scales`, `06-caged-chords`, `10-debug` → **not in the guide** (dogfood/dev pages, scoped out). The README "feature tour" may use them.
- **App icon:** use `images/icon.png` as the **application icon** of the desktop build (the WinForms window icon + the `.exe` icon), and show it in both docs' headers.
  - **`.ico` provided.** Rafa converted and committed `images/icon.ico` (multi-resolution). The plan step only **wires** it — `<ApplicationIcon>images/icon.ico</ApplicationIcon>` in `ChordFlow.Desktop.csproj` (the `.exe` icon) + `Form.Icon` on the WinForms shell (the window icon) — no conversion needed.
  - **Scope (decided):** the app-icon wiring is a `ChordFlow.Desktop` code/build change but **stays in this thread** as one isolated plan step (Rafa's call), called out so it doesn't masquerade as doc work.
- **Offline-bundling consequence:** the in-zip `USERGUIDE.md` uses relative image paths, so the release pipeline must **copy `images/screenshots/` (and the icon) into the zip** alongside `USERGUIDE.md`, or the bundled guide shows broken images. This is a new requirement on the `release-pipeline` handoff.

## Grounding corrections folded in (verified against code/README + the live UI screenshot, 2026-06-21)

- **Builder controls are not "key / rhythm / tempo."** The Practice-view builder exposes six pickers — **Harmony · Comping · Lead · Key · Difficulty · Feel** — plus **Generate · Save · Mark practiced** (confirmed in `01-practice.png`: Harmony="Blues Song Demo", Comping="Beats 1 & 3", Lead="(none)", Key="Bb", Difficulty="Beginner", Feel="Straight"). "Rhythm" ≈ **Comping + Feel**.
- **Tempo + soundfont live in the transport, not the builder.** The transport strip (shared score component) has **Play · Stop · Tempo (BPM) · Metronome · Count-in · Rhythm vol · Lead vol · Sound (soundfont picker) · Chord names · Diagrams over staff · Diagrams on top · Auto layout**. The **Sound** dropdown is where a user selects their soundfont (shows "Sonivox" by default) — §5 points here.
- **Soundfont accepts `.sf2` *and* `.sf3`** (README:87); drop into **`wwwroot/soundfont/` next to `ChordFlow.exe`** in a release (README:91–92), auto-discovered (README:39), then pick it in the transport's **Sound** dropdown. Bundled default = **Sonivox** (`sonivox.sf2`, Apache-2.0).
- **DSL is Nashville scale-degree notation**, not chord letters — the §4 inline example must use the real Progression DSL (e.g. a `1 4 5`-style degree line), not "C Am F G". (Load `chordflow-dsl-reference.md` when writing that bit.)

## Section outline (the guide's structure)

1. **What ChordFlow is** — one paragraph: a rhythm-&-progression practice tool; renders exercises as tab with synchronized playback. It *generates* exercises; it is not a tab viewer / not a DAW. (Icon in the header.)
2. **Install & first run** — unzip, run `ChordFlow.exe`; the **SmartScreen "unknown publisher"** prompt (unsigned build) → "More info → Run anyway." Windows-only. *(Screenshot placeholder: SmartScreen prompt — not yet captured.)*
3. **Build & play an exercise** (core walkthrough) — the six builder pickers → **Generate** → tab renders → **Play** (moving cursor / highlighted beat) → **Stop**, **Tempo**, optional Metronome / Count-in / volumes. Then **Save** → appears in *Saved exercises* (click to reload) → **Mark practiced**. Screenshot: `01-practice.png`.
4. **Your own content (brief)** — the **Content** view: add/edit your own progressions / comping / lead definitions in the DSL. **Link** the public DSL guide (`loom/refs/chordflow-dsl-reference.md`) **+ one tiny inline degree example** (Q3). Screenshot: `02-content-progressions.png`.
5. **Soundfonts** — what a soundfont is; bundled **Sonivox** default; add your own (`.sf2`/`.sf3` → `wwwroot/soundfont/` next to the exe → auto-discovered → pick in the **Sound** dropdown); link the README's curated download list rather than duplicating it.
6. **Known limits** — Windows-only; no audio-input accuracy detection (it won't grade your playing); offline/local, no account.

## Scope boundaries

- **In:** Practice view (build/play/save/practiced), Content view (brief), soundfonts, install/first-run, known limits, screenshots in guide+README, the app-icon wiring.
- **Out:** **Scales / CAGED / CAGED Chords / Debug** views — dogfood/dev pages, **ignored** in the guide (Q2). Architecture / bridge / build-from-source — developer territory.

## Resolved open questions

1. **Screenshots** → **yes**, in guide + README, resized (~480px) + clickable to full size. *(New SmartScreen shot still to capture for §2.)*
2. **Dogfood tabs** → **ignore** in the guide.
3. **DSL in §4** → **link the ref + one tiny inline example** (in real Nashville degree DSL).
4. **Zip filename** → repo `docs/user-guide.md` → zip **`USERGUIDE.md`**.

## Remaining open questions

- None blocking. (App-icon scope resolved: keep-here, `.ico` already provided. Release-pipeline image-bundling confirmed in scope as a handoff.)
