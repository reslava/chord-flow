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
3. `git log <lastTag>..HEAD` **with full commit bodies** — the source for the CHANGELOG
   prose (`git tag --sort=-creatordate | head -1` gives the last tag). Roadmap history is
   *not* a substitute: it carries no version and no per-change detail.

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
2. **Gather changes.** `git log <lastTag>..HEAD --format='===== %h %s%n%b'`. Sort the
   user-facing commits into Added / Changed / Fixed; drop pure chore/docs/roadmap commits.
3. **Draft the changelog:**
   - Promote `## [Unreleased]` (or write a fresh section) to a dated `## [X.Y.Z]` section in
     `CHANGELOG.md` (the **GitHub release body verbatim**).
   - Add the bottom link reference:
     `[X.Y.Z]: https://github.com/reslava/chord-flow/releases/tag/vX.Y.Z`.
4. **Review the docs** for accuracy at the new version: root `README.md` and the three
   `loom/refs/` docs (architecture, domain-model, DSL). Note any that need an edit before
   the release.
5. **STOP — show the proposed version + the `[X.Y.Z]` changelog section and wait for `go`.**
   The section is published verbatim as release notes, so it gets one human review.
6. On `go`:
   - Bump the `<Version>` in `src/ChordFlow.Desktop/ChordFlow.Desktop.csproj` to `X.Y.Z`.
   - `dotnet build -c Release && dotnet test -c Release` — red = stop and report; never ship
     a red build.
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
