---
type: idea
id: id_01KWFV8SFG5EC7RYZARGHCYAQY
title: Run migrate-layout — flatten to canonical filenames
status: done
created: 2026-07-01
updated: 2026-07-01
version: 3
tags: []
parent_id: null
requires_load: []
---
# Run migrate-layout — flatten to canonical filenames

## What

Execute `loom migrate-layout` on a Loom repo to rename every doc to the canonical flat scheme introduced by the entities-crud work:

- `{thread}-idea.md` → `idea.md`, `{thread}-design.md` → `design.md`
- `{thread}-plan-NNN.md` → `plan-NNN.md`, done docs → `plan-NNN-done.md`
- `{thread}-chat-NNN.md` / `{thread}.md` → `chat-NNN.md`
- `loom/refs/*` unchanged

Rename-only (zero content rewrites), idempotent, and **collision-aware**: when a thread holds several legacy docs sharing an ordinal (e.g. multiple `{slug}-plan-001.md`, common before per-thread ordinals), each is auto-renumbered to a unique thread-local ordinal — distinct ordinals and gaps are preserved, only the colliding extras move, and done docs mirror their parent plan's new ordinal — so nothing is ever overwritten or aborted. Every renumber is recorded in an audit log at `.loom/cache/migrate-layout.log` (gitignored).

## Why

Step 1 of the entities-crud thread shipped **dual-read** (transition strategy A): writers emit the new names, readers accept old *and* new. This command normalises existing docs onto the new names so the dual-read scaffolding can eventually be removed — see the `clean-legacy-read` thread.

## Portable runbook (self-instructions for future-me)

This thread is a runbook for **future-me to execute in a FRESH session**, and it is **repo-agnostic** — `loom migrate-layout` is a `loom` CLI command available in any installed repo. Rafa will copy this thread into Chord Flow and ping me to run it there too.

Two hard rules:

1. **Fresh session only.** The migration renames this thread's own plan + chat files mid-run (expected — it's a nice self-test that the migration handles live docs). Don't run it while stepping this plan from its files.
2. **Verify thoroughly.** There are many documents; the whole point is to prove nothing was lost or corrupted.

## Success criteria

- All docs on the new canonical names; `loom/refs` untouched.
- `git diff` shows **only renames, zero content deltas**.
- The count of `.md` files under `loom/` is **unchanged** (nothing lost or duplicated).
- `loom validate` introduces **no new issues**: capture the issue count before and after and confirm it is unchanged. A real repo carries pre-existing legacy noise (`blockedBy`-prose, stale plans) that will *not* be clean — what matters is that the migration adds nothing. Specifically: no broken `parent_id`, no dangling `child_id`, no missing Steps table — the ULID cross-reference graph survived, which is the real safety net. (A rename can flip an already-broken filename-slug blocker's error category — e.g. `unknown blocker format` → `blocked by missing plan` — with no net count change; that is expected, not new breakage.)
- Any auto-renumbered collisions in the audit log look correct — distinct ordinals preserved, only true duplicates moved, done docs matched to their plan.
- `./scripts/test-all.sh` green (where the repo has it); `loom status` loads clean.