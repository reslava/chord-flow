---
type: plan
id: pl_01KVNJ67PM2YTDW9XKX5D88S76
title: Bundle the user guide + images into the release zip — Plan
status: done
created: 2026-06-21
updated: 2026-06-21
version: 1
design_version: 1
req_version: 1
tags: []
parent_id: de_01KVNHWEWNXJJ4RNCKZ32BRY12
requires_load: []
target_version: 0.1.0
actual_release: 0.9.0
steps:
  - id: workflow-bundling-step
    order: 1
    status: done
    description: Add a 'Bundle guide + images' step to build-test (release.yml) before the zip step
    files_touched: [.github/workflows/release.yml]
    blocked_by: []
    satisfies: [IN1, IN2, IN3, IN4, C1, C2, C4]
  - id: amend-release-pipeline-req
    order: 2
    status: done
    description: Amend release-pipeline/req.md — relax EX6 and add an IN handle for guide+image bundling
    files_touched: [loom/release/release-pipeline/req.md]
    blocked_by: []
    satisfies: [IN5]
  - id: releasing-md-runbook
    order: 3
    status: done
    description: Update RELEASING.md to document the zip now carries USERGUIDE.md + images/
    files_touched: [RELEASING.md]
    blocked_by: []
    satisfies: [IN6]
---
# Bundle the user guide + images into the release zip — Plan

## Goal

Bundle the end-user guide + its images into the release zip, offline-resolvable, and bring the release-pipeline spec + runbook in line. Per the locked req (rq_01KVNHX9D6XJSER70TKFAMZPNS) and design decisions 1A/2A/copy-whole: add an additive `build-test` step that copies images into `publish/` and generates a link-rewritten `publish/USERGUIDE.md` (the repo file is never mutated), then amend `release-pipeline/req.md` EX6 and document the new zip contents in RELEASING.md.

---

## Steps

| Done | # | Step | Files touched | Blocked by | Satisfies |
|---|---|---|---|---|---|
| ✅ | 1 | Add a 'Bundle guide + images' step to build-test (release.yml) before the zip step | .github/workflows/release.yml | — | IN1, IN2, IN3, IN4, C1, C2, C4 |
| ✅ | 2 | Amend release-pipeline/req.md — relax EX6 and add an IN handle for guide+image bundling | loom/release/release-pipeline/req.md | — | IN5 |
| ✅ | 3 | Update RELEASING.md to document the zip now carries USERGUIDE.md + images/ | RELEASING.md | — | IN6 |
---

### Legend

| Symbol | Meaning |
|--------|---------|
| ✅ | Done |
| 🔄 | In Progress |
| 🔳 | Pending |
| ❌ | Cancelled |

<!-- step:workflow-bundling-step -->
### Step 1 — Workflow bundling step

New `pwsh` step in the `build-test` job, **after** *Publish self-contained …* and **before** *Package the release zip*. It: (1) copies `images/screenshots/` + `images/icon.png` into `publish/images/`; (2) generates `publish/USERGUIDE.md` from `docs/user-guide.md` with two generic link rewrites — `../images/` → `images/` (Decision 1A) and repo-relative doc links `../README.md…` / `../loom/refs/…` → absolute `https://github.com/reslava/chord-flow/blob/main/…` URLs (Decision 2A); (3) **fail-loud assert** that `publish/USERGUIDE.md` and at least one `publish/images/...` file exist before zipping (mirrors the `guard` philosophy). The repo `docs/user-guide.md` is never edited (C2). The existing `Compress-Archive -Path publish/*` then sweeps them in unchanged (C1, IN1/IN2). Lands in `build-test`, so the dry-run exercises it (IN4). Verify locally by running the rewrite on a copy and eyeballing the link transforms before pushing.

<!-- step:amend-release-pipeline-req -->
### Step 2 — Amend release-pipeline req

Via `loom_amend_req` (append-only): mark `release-pipeline` `EX6` `~relaxed~`/`~dropped~` (the guide's *bundling* is now in scope, via this thread) and append a new `IN` handle stating the artifact carries `USERGUIDE.md` + `images/`. Keep `EX6`'s point that *writing* the guide stays in `docs/user-guide`. Re-lock the release-pipeline req afterward.

<!-- step:releasing-md-runbook -->
### Step 3 — RELEASING.md runbook

Update the 'What the workflow does' section (the `build-test` description) and the 'artifact is a folder' gotcha to note the zip now contains `USERGUIDE.md` (link-rewritten) + `images/` beside `ChordFlow.exe` + `wwwroot/`.
