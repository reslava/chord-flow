---
type: chat
id: ch_01KVNHPH994GFQDQ21D7H340H0
title: guide-zip-bundle Chat
status: active
created: 2026-06-21
version: 1
tags: []
parent_id: null
requires_load: []
last_ai_block: 8
---
# guide-zip-bundle Chat

## Rafa:

I ran the app, icon working.

Idea is grounded?
If so create design, req


## AI:

Glad the icon works — that closes the visual check on the user-guide thread.

**Yes, the idea is grounded.** I checked it against `.github/workflows/release.yml`:

- `build-test` does `dotnet publish … -o publish` (→ `ChordFlow.exe` + loose `wwwroot/`), then `Compress-Archive -Path publish/* → ChordFlow-vX.Y.Z-win-x64.zip`. So bundling is **purely additive** — copy files into `publish/` before that zip step and the existing glob sweeps them in. No zip-logic change, no artifact-name change.
- The dry-run (`workflow_dispatch`) runs `build-test`, so it exercises the bundling without cutting a release.
- `release-pipeline` `EX6` does exclude the guide — confirmed.

**Created:** design (`de_01KVNHWEWNXJJ4RNCKZ32BRY12`) + req (`rq_01KVNHX9D6XJSER70TKFAMZPNS`, **draft — I'm holding the lock**) because grounding surfaced **two real decisions** I want your nod on first (the idea only half-anticipated these). The repo guide uses repo-relative links that break once it's alone at the zip root:

**Decision 1 — images.** Copy `images/` → `publish/images/` and rewrite the guide's `../images/` → `images/` (zip-root relative), keeping `USERGUIDE.md` at the zip root beside the exe. *(Alt: nest it in a `docs/` subfolder so `../images/` resolves unchanged — rejected, it shouldn't be buried.)*

**Decision 2 — sibling-doc links.** The guide also links `../README.md#soundfonts` and the `../loom/refs/…` DSL/architecture docs, which aren't in the zip. Rewrite those → **absolute GitHub URLs** in the bundled copy, so an offline reader's links open the online source. *(Alt: strip to plain text — loses the references.)*

Both rewrites happen in a new `build-test` step that generates `publish/USERGUIDE.md` from the repo file — **the repo `docs/user-guide.md` is never touched.**

Plus one sub-question: copy the **whole `images/`** tree, or just the four shots + icon the guide cites? I lean whole `images/screenshots/` + `icon.png` (tiny, simpler).

My recommendation is **1A / 2A / copy-whole**. Confirm (or adjust) and I'll lock the req and write the plan. Holding here per the design-decision stop rule.

## Rafa:

Agree with your recommendation

## AI:

Locked in **1A / 2A / copy-whole**.

- **Req locked** (`rq_01KVNHX9D6XJSER70TKFAMZPNS`), idea + design → **done**.
- **Plan created** → `guide-zip-bundle-plan-001.md` (`pl_01KVNJ67PM2YTDW9XKX5D88S76`), 3 steps, and `loom_verify_req` is **clean** (every IN covered, no violations):

1. **Workflow bundling step** — a new `pwsh` step in `build-test` (after *Publish*, before *Package the zip*): copy `images/` → `publish/images/`, generate `publish/USERGUIDE.md` from the repo guide with the two link rewrites, and a fail-loud assert that both landed before zipping. `.github/workflows/release.yml`. (IN1–4, C1, C2, C4)
2. **Amend `release-pipeline/req.md`** — relax `EX6` + add an IN for guide/image bundling, re-lock. (IN5)
3. **`RELEASING.md`** — document the zip now carries `USERGUIDE.md` + `images/`. (IN6)

Plan's not started. **Step 1** touches `.github/workflows/release.yml`. Say `go` for step 1, or "do the whole plan" to run all three.

## Rafa:

I started the plan
Do whole plan

## AI:

**Whole plan done — all 3 steps ✅, plan closed** (done doc written).

1. **`release.yml`** — new `Bundle the user guide + images` step in `build-test` (between *Publish* and *Package*): copies `images/screenshots/` + `icon.png` into `publish/images/`, writes a link-rewritten `publish/USERGUIDE.md`, and fail-loud asserts both exist before the zip. I **verified the rewrites locally** against the real guide — images go zip-root-relative, and the architecture / DSL / README#soundfonts links become absolute GitHub URLs (fragments preserved); nothing else touched. The repo guide is never mutated.
2. **`release-pipeline/req.md`** (v2, re-locked) — `EX6` marked `~relaxed~` and new `IN11` added for the bundling.
3. **`RELEASING.md`** — the workflow description + the artifact-contents gotcha now list `USERGUIDE.md` + `images/`.

**One thing I could only verify halfway:** I tested the rewrite *logic* locally, but the **full CI path** (copy + rewrite + zip on `windows-latest`) hasn't run. Best confirmed with a **`workflow_dispatch` dry-run** — it runs `build-test` end-to-end (build, tests, and the zip) without cutting a release. Worth doing before the next real tag; I can't trigger it myself, but you can from **Actions → release → Run workflow** (or `gh workflow run release.yml`).

That completes the guide-zip-bundle thread. Nothing else queued.

## Rafa:

do a test dry-run

if all good then
do-release 0.9.0


## AI:

Plan: **commit + push → dry-run → (if green) `do-release 0.9.0`.** The push is required because a `workflow_dispatch` dry-run runs the workflow **as it exists on the remote** — so the new bundling step has to be on `main` first.

Committing the release work as one cohesive change (both threads ship together in 0.9.0): `docs/user-guide.md`, `images/`, `README.md`, `RELEASING.md`, `.github/workflows/release.yml`, the `.csproj` + `Program.cs` icon wiring, and all the Loom docs for both threads + the `release-pipeline` `EX6` amend.

**Leaving two files out** — `loom/ui/caged-chords/chats/caged-chords-chat-001.md` and `-002.md` are modified in the working tree but predate this session and are unrelated to this work. I won't sweep them into the release commit; they're yours to handle separately.

The dry-run will run on the current csproj version (0.8.0) — that's fine, it exercises `build-test` (incl. the new bundling) without cutting a release. `do-release 0.9.0` comes after, only if the dry-run is green.

## AI:

**Dry-run green and verified.** Run `27911688608`: `guard` ✓, `build-test` ✓ (incl. the new **Bundle the user guide + images** step), `release` job correctly skipped (dry-run). I downloaded the artifact and confirmed the real zip (`ChordFlow-v0.8.0-win-x64.zip`):

- `USERGUIDE.md` at the zip root ✓
- `images/icon.png` + all 7 `images/screenshots/*.png` ✓
- Link rewrites applied **in the shipped copy**: `images/...` (zip-root-relative) and `…/blob/main/…` GitHub URLs for the architecture/DSL/README links ✓

All good → proceeding to **`do-release 0.9.0`** as you asked. (The release command stops for your review before it tags/pushes.)