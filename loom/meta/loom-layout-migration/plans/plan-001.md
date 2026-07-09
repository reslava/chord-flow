---
type: plan
id: pl_01KWHQS0CPSP0BC84FRW1XN66T
title: Run migrate-layout on ChordFlow
status: done
created: 2026-07-02
updated: 2026-07-02
version: 1
design_version: 1
tags: []
parent_id: null
requires_load: []
target_version: 0.1.0
actual_release: 0.13.0
steps:
  - id: capture-baseline
    order: 1
    status: done
    description: "Capture pre-migration baseline: clean git status, count of *.md files under loom/, and loom validate issue count."
    files_touched: []
    blocked_by: []
    satisfies: []
  - id: dry-run-review
    order: 2
    status: done
    description: Run `loom migrate-layout --dry-run` and eyeball the rename list and audit log for correct collision renumbering.
    files_touched: []
    blocked_by: []
    satisfies: []
  - id: run-migration
    order: 3
    status: done
    description: Run `loom migrate-layout` to perform the renames.
    files_touched: []
    blocked_by: []
    satisfies: []
  - id: verify
    order: 4
    status: done
    description: "Verify: git diff = pure renames (zero content deltas), unchanged .md count, no NEW loom validate issues, ./scripts/test-all.sh green (if present), loom status clean."
    files_touched: []
    blocked_by: []
    satisfies: []
  - id: commit
    order: 5
    status: done
    description: Commit the migration (renames only) directly to main.
    files_touched: []
    blocked_by: []
    satisfies: []
---
# Run migrate-layout on ChordFlow

## Goal

Execute `loom migrate-layout` on the ChordFlow repo to normalise ~270 legacy slug-prefixed doc filenames onto the canonical flat scheme (idea.md / design.md / plan-NNN.md / plan-NNN-done.md / chat-NNN.md); loom/refs untouched. The migration is rename-only, idempotent, and collision-safe; the substance of this plan is rigorous before/after verification proving nothing was lost or corrupted (unchanged .md count, git diff = pure renames, no new validate issues, tests green). This thread's own docs are already canonical so the migration will not touch them — no fresh-session self-rename concern in this repo.

---

## Steps

| Done | # | Step | Files touched | Blocked by | Satisfies |
|---|---|---|---|---|---|
| ✅ | 1 | Capture pre-migration baseline: clean git status, count of *.md files under loom/, and loom validate issue count. | — | — | — |
| ✅ | 2 | Run `loom migrate-layout --dry-run` and eyeball the rename list and audit log for correct collision renumbering. | — | — | — |
| ✅ | 3 | Run `loom migrate-layout` to perform the renames. | — | — | — |
| ✅ | 4 | Verify: git diff = pure renames (zero content deltas), unchanged .md count, no NEW loom validate issues, ./scripts/test-all.sh green (if present), loom status clean. | — | — | — |
| ✅ | 5 | Commit the migration (renames only) directly to main. | — | — | — |
---

### Legend

| Symbol | Meaning |
|--------|---------|
| ✅ | Done |
| 🔄 | In Progress |
| 🔳 | Pending |
| ❌ | Cancelled |

<!-- step:capture-baseline -->
### Step 1 — Capture baseline

Confirm `git status` is clean (or only this thread's plan/chat dirty). Record `find loom -name '*.md' | wc -l` and the `loom validate` issue count to compare against post-run. Baseline is captured immediately before the run so any docs written this session are already counted.

<!-- step:dry-run-review -->
### Step 2 — Dry-run review

Confirm renames only touch legacy names, loom/refs is absent, done docs map pl_{ULID}-done.md → plan-NNN-done.md matched to their parent plan's ordinal, and any auto-renumbered ordinal collisions look correct (distinct ordinals/gaps preserved, only true duplicates moved). Check `.loom/cache/migrate-layout.log`.

<!-- step:run-migration -->
### Step 3 — Run migration

Rename-only execution. No content should change.

<!-- step:verify -->
### Step 4 — Verify

Use `git status`/`git diff --stat` to confirm renames only. Compare .md count and validate issue count against the Step 1 baseline — the count must be unchanged and no new issue categories (a rename may flip an already-broken blocker's error category with no net change; that is expected, not new breakage). Confirm the ULID cross-reference graph survived (no broken parent_id, dangling child_id, or missing Steps table).

<!-- step:commit -->
### Step 5 — Commit

Stage all renames plus this thread's idea/plan/chat, commit with a clear message describing the flatten. Record the outcome (rename count, collision renumbers, before/after issue counts) in the done doc via loom_append_done.
