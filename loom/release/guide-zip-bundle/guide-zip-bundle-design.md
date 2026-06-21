---
type: design
id: de_01KVNHWEWNXJJ4RNCKZ32BRY12
title: Bundle the user guide + images into the release zip — Design
status: done
created: 2026-06-21
updated: 2026-06-21
version: 2
tags: []
parent_id: id_01KVNGQP5KEXJPNPVNFDE7DY7F
requires_load: []
---
# Bundle the user guide + images into the release zip — Design

## Goal

Make the downloadable release zip carry the end-user guide and its images, offline-resolvable, and bring `release-pipeline`'s spec in line (it currently *excludes* the guide). Authoring the guide stays in `docs/user-guide`; this thread owns only the **release machinery**.

## Grounding (verified against `.github/workflows/release.yml` + `RELEASING.md`)

- The `build-test` job runs `dotnet publish "$CSPROJ" -c Release -r win-x64 --self-contained ... -o publish` → `publish/` holds `ChordFlow.exe` + loose `wwwroot/`. Then **`Compress-Archive -Path publish/* -DestinationPath artifacts/ChordFlow-v$version-win-x64.zip`**.
- So bundling is purely additive: **copy files into `publish/` before the `Compress-Archive` step** and the existing `publish/*` glob sweeps them into the zip. No change to the zip logic, the artifact name, or the `release` job.
- `build-test` runs on `workflow_dispatch` (dry-run) as well as tag pushes, so the dry-run exercises the bundling without cutting a release.
- `release-pipeline/req.md` `EX6` ("Writing the end-user guide … only the README soundfont/download note is in scope here") is the spec line this thread amends.

## The crux: links break at the zip root

`docs/user-guide.md` is authored with **repo-relative** links that do **not** resolve once the file sits alone at the zip root as `USERGUIDE.md`:

- Images — `../images/screenshots/*.png`, `../images/icon.png`.
- Sibling docs not in the zip — `../README.md#soundfonts`, `../loom/refs/chordflow-dsl-reference.md`, `../loom/refs/chordflow-architecture-reference.md`.

The bundled copy must be **transformed**, never the repo file. A new workflow step generates `publish/USERGUIDE.md` from `docs/user-guide.md` with two rewrites:

### Decision 1 — images (recommend A)

- **A (recommend):** copy repo `images/` → `publish/images/`, and rewrite the guide's `../images/` → `images/` so links are zip-root-relative. Keeps the guide at the zip root beside the exe (per `user-guide` req `C1`).
- **B (reject):** mirror the repo tree in the zip (`publish/docs/USERGUIDE.md`) so `../images/` resolves unchanged — rejected: the guide should sit at the zip root beside `ChordFlow.exe`, not in a `docs/` subfolder.

### Decision 2 — sibling-doc links (recommend A)

- **A (recommend):** rewrite repo-relative doc links (`../README.md…`, `../loom/refs/…`) → **absolute GitHub URLs** (`https://github.com/reslava/chord-flow/blob/main/…`) in the bundled copy, so an offline reader's links open the online source.
- **B (reject):** strip them to plain text — loses the references (DSL guide, soundfont list).

### Open sub-question

- Copy the **whole `images/`** tree, or just the guide-referenced subset (`01–04` + `icon.png`)? Recommend copying `images/screenshots/` + `images/icon.png` (tiny; simpler than tracking which shots the guide cites).

## Plan shape (preview — for the plan doc)

1. **New `Bundle guide + images` step** in `build-test`, after *Publish* and before *Package the release zip*: copy `images/` into `publish/images/`; generate `publish/USERGUIDE.md` from `docs/user-guide.md` with the two rewrites (Decisions 1 & 2). `pwsh` (the packaging step is already `pwsh`) or `sed`.
2. **Amend `release-pipeline/req.md`** — relax/retire `EX6`, add an IN handle for guide + image bundling so the pipeline spec matches reality.
3. **Update `RELEASING.md`** — the "What the workflow does" + zip-contents notes now mention `USERGUIDE.md` + `images/`.
4. **(Optional) sanity check** — assert `USERGUIDE.md` + an image exist in `publish/` before zipping (fail-loud, mirrors the `guard` philosophy).

## Notes

- No `loom/refs/` ref-sync needed: this touches CI packaging, not a core DSL / the domain / the app architecture.
- The repo `docs/user-guide.md` is never mutated by CI — the transform writes a separate `publish/USERGUIDE.md`.

## To confirm before locking the req

Decisions 1 & 2 (recommend A/A) and the whole-`images/` sub-question.
