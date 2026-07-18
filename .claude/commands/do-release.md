---
description: Ship a ChordFlow release end-to-end — changelog, version bump, build/test, record-release, tag, push, and monitor the release workflow.
argument-hint: "<X.Y.Z>  (required — omit and it shows candidates then stops; never assumed)"
---

# do-release

Ship ChordFlow version **$ARGUMENTS**. The version is **required and never assumed** — if
`$ARGUMENTS` is empty, do not pick one: run pre-flight **A** below (show the candidates,
STOP, and require an explicit re-run with `X.Y.Z`).

This is an **operational runbook task**, not pipeline design work. Load only the minimal
context below — do **not** read `loom://state` or the `release-pipeline` thread bundle
(idea/design/plan). Those describe how the pipeline is *built*; this task only *runs* it.

## Context to load (and nothing more)

1. `RELEASING.md` — the authoritative checklist + gotchas + recovery.
2. The single `<Version>` in `src/ChordFlow.Desktop/ChordFlow.Desktop.csproj` — the only
   authoritative version source.
3. **`loom report release-notes`** — the command that drafts the changelog from the doc graph:
   Unreleased selection (`actual_release` null), done-body enrichment, and the empty-set guard
   all live in the command. This repo runs it; you do not hand-read the graph. *(Requires a Loom
   version carrying the enriched `loom report release-notes` — upgrade `@reslava/loom` if the
   installed command predates it.)*
4. `git log <lastTag>..HEAD --oneline` (`git tag --sort=-creatordate | head -1` gives the last
   tag) — the coverage net + a "work not recorded" tell (step 2). Not the changelog source.

## Pre-flight (before any release work)

**A. Version is required — never assumed.** If `$ARGUMENTS` is empty, do **not** pick a
version. Read the current csproj `<Version>` and show the three candidates as a hint — e.g.
`patch → 0.4.1 · minor → 0.5.0 · major → 1.0.0` — with one line noting which applies
depends on what shipped (bugfix-only = patch, new features = minor, breaking = major). Then
**STOP** and require the user to re-run `/do-release X.Y.Z`. Proceed past here only when a
version was given.

**B. Commit stray roadmap reflows first (keep the release commit clean).** Check
`git status` for uncommitted `thread.md` files so they don't get swept into the
`release: vX.Y.Z` commit:
- **Modified, tracked `thread.md` whose diff touches only `priority` / `depends_on`** →
  commit just those as `chore: roadmap` before continuing.
- **New / untracked `thread.md`, or any `thread.md` with a non-roadmap diff** → do **not**
  auto-commit. **STOP and report** them, and let the user commit them by hand.

## Steps

1. **Confirm version.** Pre-flight A guarantees a version was supplied. Sanity-check it's a
   clean bump above the current csproj `<Version>` and state it.
2. **Draft the changelog with `loom report release-notes`.** Selection (the Unreleased set),
   done-body enrichment, and the doc-graph empty-set guard live in the command now — this repo
   runs it and reviews:
   - Run **`loom report release-notes`** (`--titles-only` for a fast, low-token draft) and
     synthesize its brief **in-session** (you are the AI — no shell-out, no API key). The brief
     is the Unreleased plans (`actual_release` null) with their done-doc detail, framed
     Highlights → **Added / Changed / Fixed** in a benefit voice.
   - **Empty-set guard (built in):** if the command returns the **"NOTHING UNRELEASED"**
     stop-signal, do **not** draft — STOP and report it (it names any threads still
     `implementing`). Cross-check the git tells: a dirty tree (`git status`) or commits in
     `git log <lastTag>..HEAD` mean work shipped but was never closed/quick-shipped/committed —
     have the user record it, then re-run.
   - **Coverage net (release-side):** `git log <lastTag>..HEAD --oneline`; list any user-facing
     commit **not represented** by an Unreleased done plan as a **"Not covered by a done plan"**
     appendix for the human to fold in or dismiss.
   - **Stale-leak (release-side):** flag any Unreleased done doc dated **before the previous tag**
     — a prior release may have failed to stamp its plans; the human decides. Non-blocking.
3. **Write the changelog from the step-2 draft:**
   - Put the curated Added / Changed / Fixed (+ Highlights) entries into a dated `## [X.Y.Z]`
     section in `CHANGELOG.md` (the **GitHub release body verbatim**).
   - Add the bottom link reference:
     `[X.Y.Z]: https://github.com/reslava/chord-flow/releases/tag/vX.Y.Z`.
4. **Update the README + review the refs.** Bring root `README.md` current with what shipped —
   this is a required edit, not just a glance (the `loom/refs/` sync rule does *not* cover the
   README's user-facing snapshot, so it silently drifts otherwise). Update: the **Status** line
   (version + one-line framing), the **`## Features (vX.Y.Z)`** heading and its bullets (add the
   newly shipped user-facing features — the section tracks the *latest release*, not a running
   log), the **test count**, the **Project layout** if structure changed, and any
   download/usage detail that changed. Then sanity-glance the three `loom/refs/` docs
   (architecture, domain-model, DSL) — kept current per code-change, so this is just a check.
   The README edits are part of the release commit (step 6 `git commit -am` picks them up).
5. **STOP — show the proposed version + the `[X.Y.Z]` changelog section and wait for `go`.**
   The section is published verbatim as release notes, so it gets one human review.
6. On `go`:
   - Bump the `<Version>` in `src/ChordFlow.Desktop/ChordFlow.Desktop.csproj` to `X.Y.Z`.
   - `dotnet build -c Release && dotnet test -c Release` — red = stop and report; never ship
     a red build.
   - `dotnet list package --vulnerable --include-transitive` — if any advisory shows, **STOP and
     report it** (resolve by bumping or pinning/overriding the offending package before tagging);
     don't let a known-vulnerable dependency ship or first surface mid-release. A transitive
     advisory is cleared by a top-level `PackageReference` to the patched package (see
     `loom/chordflow/sqlite-security-bump/` for the pattern).
   - `loom record-release X.Y.Z` — stamp this release's done plans with `actual_release` so
     the roadmap owns "what shipped in vX.Y.Z" (idempotent; no-op if nothing is unstamped).
     Run it **after** the build. Its plan-file edits are part of the release commit below.
   - `git commit -am "release: vX.Y.Z"` (the `record-release` stamps land in this commit).
   - `git tag -a vX.Y.Z -m "vX.Y.Z"` — **annotated**, or `--follow-tags` silently leaves the
     tag local and the workflow never fires.
   - `git push --follow-tags`.
7. **Monitor.** `gh run list --workflow=release.yml --limit 1` → `gh run watch <id> --exit-status`.
   Job graph: `guard → build-test → release`.
8. **Recovery.** A transient failure is recovered by re-running the **same tag**:
   `gh run rerun <id> --failed` — `guard`, `build-test`, and `gh release create` are all
   re-runnable (no immutable registry publishes). Only if the released content is wrong do
   you roll forward to the next patch. If a job fails twice consecutively, stop and diagnose.

## Done when

The GitHub release for the tag is published with the `ChordFlow-vX.Y.Z-win-x64.zip` asset
attached (`gh release view vX.Y.Z`).
