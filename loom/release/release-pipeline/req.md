---
type: req
id: rq_01KVB5721BQNCT4VJDHM0QY8FS
title: release-pipeline Requirements
status: locked
created: 2026-06-17
updated: 2026-06-21
version: 2
tags: []
parent_id: null
requires_load: []
---
# release-pipeline Requirements

### ✅ Included

- `IN1` A single command (`/do-release X.Y.Z`) drives the whole release: changelog draft, doc review, version bump, build + test, `loom record-release`, commit, annotated tag, push, and monitor.
- `IN2` A tag-driven CI workflow reacts to a `vX.Y.Z` push with the spine `guard → build-test → release`.
- `IN3` The release attaches a downloadable Windows artifact: a self-contained, single-file `ChordFlow.exe` plus its `wwwroot/`, zipped (`ChordFlow-vX.Y.Z-win-x64.zip`).
- `IN4` The GitHub release body is the `CHANGELOG.md` section for that version, verbatim.
- `IN5` The `guard` fails fast (before any build) if the csproj `<Version>` does not match the tag, or `CHANGELOG.md` has no dated `## [X.Y.Z]` section.
- `IN6` A manual dry-run (`workflow_dispatch`) exercises `guard + build-test` without publishing or cutting a release.
- `IN7` The release stamps the Loom roadmap via `loom record-release X.Y.Z`.
- `IN8` A `RELEASING.md` runbook documents the pre-tag checklist and the gotchas (SmartScreen, soundfont, windows runner).
- `IN9` The shipped executable is named `ChordFlow.exe`.
- `IN10` The default soundfont (`sonivox.sf2`) is committed and ships inside the artifact — no build-time download.
- `IN11` The artifact also carries the **end-user guide** — `USERGUIDE.md` (a link-rewritten copy of `docs/user-guide.md`) at the zip root, plus its `images/` — bundled by an additive `build-test` step. Owned by the `guide-zip-bundle` thread (`th_01KVNGQA9CNXD7KY1ZWNW308TW`).

### ❌ Excluded

- `EX1` Publishing to any registry or store (npm, VS Code Marketplace, Open VSX, MS Store, Gumroad, itch.io) — future, additive jobs.
- `EX2` Code signing / Authenticode certificate — the SmartScreen "unknown publisher" warning is accepted for now and documented.
- `EX3` macOS / Linux builds — blocked until a cross-platform host exists (the Core split keeps it additive).
- `EX4` A version-bump script — the single csproj `<Version>` is edited directly by the command.
- `EX5` Bundling large soundfont banks — only `sonivox.sf2` ships; users add their own by dropping a `.sf2` into `wwwroot/soundfont/`.
- `EX6` ~relaxed~ — *Writing* the end-user guide still lives in the `docs/user-guide` thread, but **bundling** it into the artifact is now in scope (delivered via `guide-zip-bundle`, `IN11`). Originally: "Writing the end-user guide … only the README soundfont/download note is in scope here."

### ⛓ Constraints

- `C1` Single authoritative version source: `<Version>` in `src/ChordFlow.Desktop/ChordFlow.Desktop.csproj`.
- `C2` CI build/test/publish runs on `windows-latest` (`ChordFlow.Desktop` is `net10.0-windows` + WinForms).
- `C3` No registry/publish secrets — only the auto-provided `GITHUB_TOKEN`.
- `C4` Release notes are passed to the release step via env, never inline `${{ }}` (script-injection safety).
- `C5` `wwwroot/` ships as loose files beside the exe (single-file embeds only the runtime), preserving runtime soundfont auto-discovery.
- `C6` Hermetic build — no external network dependency for bundled assets at build time.
- `C7` The CHANGELOG `## [X.Y.Z]` section is matched literally (index-based), not via a dynamic regex.
- `C8` Reference-doc sync: a change to the soundfont mechanism updates `chordflow-architecture-reference.md` in the same unit of work (CLAUDE.md rule).
