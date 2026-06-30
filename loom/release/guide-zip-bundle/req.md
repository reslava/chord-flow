---
type: req
id: rq_01KVNHX9D6XJSER70TKFAMZPNS
title: Bundle the user guide + images into the release zip — Requirements
status: locked
created: 2026-06-21
updated: 2026-06-21
version: 1
design_version: 2
tags: []
parent_id: de_01KVNHWEWNXJJ4RNCKZ32BRY12
requires_load: []
---
# Bundle the user guide + images into the release zip — Requirements

### ✅ Included

- `IN1` The release zip (`ChordFlow-vX.Y.Z-win-x64.zip`) contains the end-user guide as **`USERGUIDE.md`** at the **zip root**, beside `ChordFlow.exe`.
- `IN2` The zip contains the guide's images under **`images/`** (the repo `images/screenshots/` + `images/icon.png`), so the bundled guide's image links resolve **offline**.
- `IN3` A `build-test` step generates `publish/USERGUIDE.md` from `docs/user-guide.md` with two link rewrites: `../images/` → `images/` (Decision 1) and repo-relative doc links (`../README.md`, `../loom/refs/…`) → **absolute GitHub URLs** (Decision 2). The repo file is never mutated.
- `IN4` Bundling runs inside `build-test`, so the `workflow_dispatch` **dry-run** exercises it without cutting a release.
- `IN5` `release-pipeline/req.md` is **amended** — `EX6` retired/relaxed and an `IN` handle added so the pipeline spec covers guide + image bundling.
- `IN6` `RELEASING.md` documents that the zip now carries `USERGUIDE.md` + `images/`.

### ❌ Excluded

- `EX1` Writing or maintaining the **guide content** — owned by the `docs/user-guide` thread.
- `EX2` Bundling the **README** or the `loom/refs/` docs into the zip — only the guide + its images ship; sibling-doc links point back to GitHub (Decision 2).
- `EX3` Changing the **artifact name, the `release` job, or the zip mechanism** (`Compress-Archive`) — bundling is additive into `publish/`.
- `EX4` Code signing / any SmartScreen change.

### ⛓ Constraints

- `C1` Bundling is **additive**: copy into `publish/` before the existing `Compress-Archive -Path publish/*` step; no zip-logic change.
- `C2` `docs/user-guide.md` is the source of record; CI transforms a **copy** (`publish/USERGUIDE.md`), never edits the repo file.
- `C3` Depends on the `docs/user-guide` thread (`th_01KVB4KGN24YTN8MT3YNTN6C3T`) — the guide + images must exist (they now do).
- `C4` Rewrite rules target the `../images/` and repo-relative `../` prefixes **generically**, not per-file hardcoding, so the transform survives as the guide's links evolve.
