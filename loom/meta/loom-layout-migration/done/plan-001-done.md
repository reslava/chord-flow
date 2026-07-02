---
type: done
id: pl_01KWHQS0CPSP0BC84FRW1XN66T-done
title: Done — Run migrate-layout on ChordFlow
status: done
created: 2026-07-02
version: 1
tags: []
parent_id: pl_01KWHQS0CPSP0BC84FRW1XN66T
requires_load: []
---
# Done — Run migrate-layout on ChordFlow

## Step 1 — Capture pre-migration baseline: clean git status, count of *.md files under loom/, and loom validate issue count.

Baseline captured: git clean except the new untracked thread. `.md` under `loom/` = **389**. `loom validate --all` = **26 issues** (chordflow 9, guitar 11, ui 6), all pre-existing legacy noise: "unknown blocker format" prose blockers + 3 stale plans in ui. Detail saved for before/after diff.

## Step 2 — Run `loom migrate-layout --dry-run` and eyeball the rename list and audit log for correct collision renumbering.

`loom migrate-layout --dry-run` = **269 renames**. Verified: 0 `loom/refs` touched; 0 touches to this thread (its docs are already canonical); done docs map `pl_{ULID}-done.md` → `plan-NNN-done.md` matched to parent-plan ordinals with gaps preserved (e.g. mvp → plan-001/002/004). No ordinal collisions requiring renumbering in this repo.

## Step 3 — Run `loom migrate-layout` to perform the renames.

`loom migrate-layout` ran, exit 0. `.md` count unchanged at **389**.

## Step 4 — Verify: git diff = pure renames (zero content deltas), unchanged .md count, no NEW loom validate issues, ./scripts/test-all.sh green (if present), loom status clean.

Verified: `git diff --cached -M` = **269 R100** (100% similarity, zero content deltas) + 4 new-thread additions; no M/D on existing files. `.md` count unchanged (389). `loom validate --all` after = identical **26 issues**; category *and* full detail diff before→after = **IDENTICAL** (even blocker-format errors did not flip). `loom status` loads clean (exit 0). `dotnet test tests/ChordFlow.Core.Tests` = **767 passed, 0 failed** (migration touched only markdown, so code is unaffected, but confirmed).

## Step 5 — Commit the migration (renames only) directly to main.

Committed the 269 renames + the new migration thread (idea/thread/plan/chat) directly to main. The audit log `.loom/cache/migrate-layout.log` was left untracked/uncommitted (per Rafa: no need to gitignore it); no `.gitignore` change made.
