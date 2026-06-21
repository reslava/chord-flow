---
type: idea
id: id_01KVNGQP5KEXJPNPVNFDE7DY7F
title: Bundle the user guide + images into the release zip
status: done
created: 2026-06-21
updated: 2026-06-21
version: 2
tags: []
parent_id: null
requires_load: []
---
# Bundle the user guide + images into the release zip

## What

Make the release artifact ship the **end-user guide** and its images. The release pipeline copies the repo's `docs/user-guide.md` into the zip as **`USERGUIDE.md`** at the zip root (beside `ChordFlow.exe`), and copies **`images/screenshots/` + the app icon** into the zip so the guide's relative image paths resolve offline.

## Why

Spun out of the `docs/user-guide` thread (`req.md` `C5`). That thread *authors* the guide and references the screenshots/icon by relative path; this thread owns the **release machinery** that gets them into the downloadable zip. The `release-pipeline` thread currently **excludes** the guide (`release-pipeline/req.md` `EX6` — "Writing the end-user guide … only the README soundfont/download note is in scope here"), so this work **amends** that scope: writing the guide stays in `docs/user-guide`, but *bundling* it is a release concern.

## Scope

- Copy `docs/user-guide.md` → `USERGUIDE.md` (zip root) in the release build/zip step.
- Copy `images/screenshots/*` + `images/icon.{png,ico}` into the zip so relative `<img src="images/…">` links resolve in the bundled `USERGUIDE.md`.
- **Amend `release-pipeline/req.md`**: retire/relax `EX6` and add IN handle(s) for guide + image bundling, so the pipeline's spec matches reality.
- Decide the in-zip image layout (mirror `images/…` paths, or flatten next to `USERGUIDE.md`) so the guide's `src` paths are written to match.

## Depends on

- `docs/user-guide` thread (`th_01KVB4KGN24YTN8MT3YNTN6C3T`) — the guide file + image set must exist first.

## Open questions

- In-zip image path: mirror the repo `images/screenshots/` tree, or flatten? (The guide's `<img src>` must be authored to match whichever this thread picks.)
- Amend `release-pipeline/req.md` in place, or treat this thread's req as the authority and cross-link?
