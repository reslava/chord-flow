# Releasing ChordFlow

ChordFlow ships as **one artifact**: a downloadable, self-contained Windows build
(`ChordFlow.exe` + its `wwwroot/`, zipped) attached to a **GitHub release**. There is
**no registry/store publish** today (npm / MS Store / Gumroad / itch.io are future,
additive options).

Releases are **tag-driven**. You bump the version + write notes locally and push a
`vX.Y.Z` tag; the [`release` workflow](.github/workflows/release.yml) reacts:
`guard → build-test → release`. CI never pushes to `main` and never owns the version bump.

The easiest path is the **`/do-release X.Y.Z`** Claude command, which walks every step
below and stops for your review before tagging. This file is the authoritative checklist
behind it.

---

## Pre-tag checklist

Run through this **before** pushing the tag. It is trust-based — CI machine-checks only the
two items noted. Skipping a step ships a broken or inconsistent release.

- [ ] **Record changes under `## [Unreleased]`** in [`CHANGELOG.md`](CHANGELOG.md)
      (*Keep a Changelog* format) as you go. On release, promote them to a dated
      `## [X.Y.Z]` section — this becomes the **GitHub release body verbatim**.
      *(Machine-checked: `guard` fails before any build if no `## [X.Y.Z]` section exists.)*
- [ ] **Add the bottom link reference:** `[X.Y.Z]: https://github.com/reslava/chord-flow/releases/tag/vX.Y.Z`.
- [ ] **Bump the version:** edit the single `<Version>` in
      [`src/ChordFlow.Desktop/ChordFlow.Desktop.csproj`](src/ChordFlow.Desktop/ChordFlow.Desktop.csproj)
      — the **only** authoritative version source (no other file carries it).
      *(Machine-checked: `guard` asserts the csproj `<Version>` equals the tag.)*
- [ ] **Update the README, review the refs.** Bring root [`README.md`](README.md) current with
      what shipped — a **required edit**: the **Status** line, the **`## Features (vX.Y.Z)`**
      section (a latest-release snapshot, not a running log), the **test count**, and the
      **Project layout** if it changed. Then sanity-glance the three `loom/refs/` docs
      (architecture, domain-model, DSL) — kept current per code-change by the CLAUDE.md
      "Reference-doc sync" rule, so the README is the one doc that needs the release-time edit.
      Also confirm the **[User Guide](docs/user-guide.md)** still matches the app — it joins this
      doc-accuracy review set alongside the README + the three refs.
      CI does **not** verify docs; this is human judgment.
- [ ] **Build + test locally green:** `dotnet build -c Release && dotnet test -c Release`.
- [ ] **Record the release in Loom:** `loom record-release X.Y.Z` — stamps `actual_release`
      onto this release's done plans so the roadmap owns "what shipped in vX.Y.Z" (idempotent;
      run after the build). The plan-file edits are included in the release commit below.
- [ ] **Commit, tag, push:**
      ```bash
      git commit -am "release: vX.Y.Z"
      git tag -a vX.Y.Z -m "vX.Y.Z"
      git push --follow-tags
      ```
      The tag **must be annotated** (`-a`) — `git push --follow-tags` only pushes annotated
      tags, so a lightweight `git tag vX.Y.Z` would push the branch but silently leave the
      tag local, and the release workflow would never fire.

---

## What the workflow does

On a `vX.Y.Z` tag push (all jobs on **`windows-latest`** — `ChordFlow.Desktop` is
`net10.0-windows`/WinForms and only builds on Windows):

1. **`guard`** — resolves the version, asserts the csproj `<Version>` matches the tag, and
   asserts a `## [X.Y.Z]` section exists in `CHANGELOG.md`. Fails fast before any build.
2. **`build-test`** — `dotnet restore → build -c Release → test -c Release`, then
   `dotnet publish` (self-contained, single-file, `win-x64`), **bundles the end-user
   guide** (a link-rewritten `USERGUIDE.md` + its `images/`, copied into the publish
   folder — see the *artifact* gotcha below), zips the output to
   `ChordFlow-vX.Y.Z-win-x64.zip`, and uploads it as an artifact.
3. **`release`** — extracts the `CHANGELOG.md` `[X.Y.Z]` section and runs `gh release create`
   with those notes, attaching the zip. (Skipped on dry-runs.)

**No secrets to configure** — the release uses the auto-provided `GITHUB_TOKEN`.

---

## Dry-run before the first real tag

The workflow has a manual trigger that exercises everything **without cutting a release**:

**Actions → release → Run workflow → `dry_run: true`** (the default).

It runs `guard → build-test` against the current branch (version read from the csproj since
there's no tag): the build + tests run and the zip artifact is produced, but the GitHub
release is **not** cut. Use it to confirm the pipeline is green before pushing your first
`vX.Y.Z`.

---

## If something fails

1. **Re-run the same tag's workflow** (`gh run rerun <id> --failed`). `guard`, `build-test`,
   and the `gh release create` step are all safe to re-run on the same tag — there are no
   immutable registry publishes to worry about.
2. If the **build/test** itself is red, fix forward: never ship a red build.
3. If the **released content** is wrong, roll forward to the next **patch** (bump the csproj
   `<Version>`, re-tag, push) — don't try to reuse a tag.

---

## Known risks / gotchas

- **The exe is unsigned**, so on first run Windows **SmartScreen** shows an "unknown
  publisher" warning (**More info → Run anyway**). This clears as the download builds
  reputation, or permanently once an Authenticode code-signing certificate is added (a
  future cost, out of scope today). Set expectations in the release notes / user guide so
  it isn't reported as a bug.
- **Soundfont:** only the small default `sonivox.sf2` (Apache-2.0) is **committed** and
  ships in the zip — the build is hermetic (no build-time download). Larger banks are **not**
  bundled (size + licensing); a user adds their own by dropping a `.sf2` into
  `wwwroot/soundfont/` next to the exe — the in-app picker auto-discovers it. The README
  carries a curated download list.
- **windows-latest only** — `ChordFlow.Desktop` can't build on Linux runners. A future
  cross-platform host would add its own build matrix; the `ChordFlow.Core` split keeps that
  additive.
- **The artifact is a folder, not a bare exe** — `ChordFlow.exe` + `wwwroot/`, plus the
  bundled **`USERGUIDE.md`** + **`images/`** (the end-user guide). Single-file publish embeds
  only the .NET runtime; `wwwroot/**` (served from disk + scanned for soundfonts at runtime)
  ships as loose files beside the exe, which is required. The guide is a *link-rewritten copy*
  of `docs/user-guide.md` (image paths made zip-root-relative; sibling-doc links pointed at the
  online repo) — the repo file is never mutated by CI.
