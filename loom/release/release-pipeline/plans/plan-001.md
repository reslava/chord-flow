---
type: plan
id: pl_01KVB53F79XSMPYDHNY6KEP72K
title: Release pipeline — implementation
status: done
created: 2026-06-17
updated: 2026-06-17
version: 1
design_version: 3
tags: []
parent_id: de_01KVB4K1PTQ437QZHX8PWX0N63
requires_load: []
target_version: 0.1.0
actual_release: 0.5.0
steps:
  - id: commit-sonivox-sf2-drop-fetchsoundfont
    order: 1
    status: done
    description: Commit the default soundfont and remove the build-time fetch (hermetic builds)
    files_touched: [.gitignore, src/ChordFlow.Desktop/ChordFlow.Desktop.csproj, src/ChordFlow.Desktop/wwwroot/soundfont/sonivox.sf2]
    blocked_by: []
    satisfies: [IN10, C5, C6]
  - id: assemblyname-chordflow
    order: 2
    status: done
    description: Rename the shipped executable to ChordFlow.exe via <AssemblyName>
    files_touched: [src/ChordFlow.Desktop/ChordFlow.Desktop.csproj]
    blocked_by: []
    satisfies: [IN9]
  - id: release-yml
    order: 3
    status: done
    description: Add the tag-driven release workflow (guard → build-test → release) on windows-latest
    files_touched: [.github/workflows/release.yml]
    blocked_by: [1, 2]
    satisfies: [IN2, IN3, IN4, IN5, IN6, C2, C3, C4, C5, C7]
  - id: releasing-md
    order: 4
    status: done
    description: Write RELEASING.md runbook (pre-tag checklist + gotchas)
    files_touched: [RELEASING.md]
    blocked_by: [3]
    satisfies: [IN8]
  - id: do-release-command
    order: 5
    status: done
    description: Add the /do-release X.Y.Z slash command adapted from Loom
    files_touched: [.claude/commands/do-release.md]
    blocked_by: [3]
    satisfies: [IN1, IN7]
  - id: readme-download-soundfont-note
    order: 6
    status: done
    description: "Update README: add a Download/Install section and flip the soundfont docs from 'fetched at build' to 'bundled'"
    files_touched: [README.md]
    blocked_by: [1]
    satisfies: [IN3, IN10]
  - id: ref-sync-architecture
    order: 7
    status: done
    description: "Doc/ref sync: update the architecture reference (and any doc) describing the soundfont fetch"
    files_touched: [loom/refs/chordflow-architecture-reference.md]
    blocked_by: [1]
    satisfies: [C8]
  - id: dry-run-validation
    order: 8
    status: done
    description: Validate end-to-end with a dry-run before the first real tag
    files_touched: []
    blocked_by: [1, 2, 3]
    satisfies: [IN6, C6]
---
# Release pipeline — implementation

## Goal

Implement the ChordFlow release pipeline per the design: a tag-driven GitHub Actions workflow (guard → build-test → release) on windows-latest that builds + tests .NET 10, publishes a self-contained single-file ChordFlow.exe + wwwroot zip, and cuts a GitHub release with the CHANGELOG section as notes; a /do-release command and RELEASING.md runbook driving it; the exe renamed to ChordFlow.exe; and the default sonivox.sf2 soundfont committed (build-time fetch removed) so release builds are hermetic. Adapted from Loom's pipeline, minus all registry-publishing. No registry secrets — only the auto-provided GITHUB_TOKEN.

---

## Steps

| Done | # | Step | Files touched | Blocked by | Satisfies |
|---|---|---|---|---|---|
| ✅ | 1 | Commit the default soundfont and remove the build-time fetch (hermetic builds) | .gitignore, src/ChordFlow.Desktop/ChordFlow.Desktop.csproj, src/ChordFlow.Desktop/wwwroot/soundfont/sonivox.sf2 | — | IN10, C5, C6 |
| ✅ | 2 | Rename the shipped executable to ChordFlow.exe via <AssemblyName> | src/ChordFlow.Desktop/ChordFlow.Desktop.csproj | — | IN9 |
| ✅ | 3 | Add the tag-driven release workflow (guard → build-test → release) on windows-latest | .github/workflows/release.yml | 1, 2 | IN2, IN3, IN4, IN5, IN6, C2, C3, C4, C5, C7 |
| ✅ | 4 | Write RELEASING.md runbook (pre-tag checklist + gotchas) | RELEASING.md | 3 | IN8 |
| ✅ | 5 | Add the /do-release X.Y.Z slash command adapted from Loom | .claude/commands/do-release.md | 3 | IN1, IN7 |
| ✅ | 6 | Update README: add a Download/Install section and flip the soundfont docs from 'fetched at build' to 'bundled' | README.md | 1 | IN3, IN10 |
| ✅ | 7 | Doc/ref sync: update the architecture reference (and any doc) describing the soundfont fetch | loom/refs/chordflow-architecture-reference.md | 1 | C8 |
| ✅ | 8 | Validate end-to-end with a dry-run before the first real tag | — | 1, 2, 3 | IN6, C6 |
---

### Legend

| Symbol | Meaning |
|--------|---------|
| ✅ | Done |
| 🔄 | In Progress |
| 🔳 | Pending |
| ❌ | Cancelled |

<!-- step:commit-sonivox-sf2-drop-fetchsoundfont -->
### Step 1 — Commit sonivox.sf2, drop FetchSoundfont

Add a `.gitignore` negation `!src/ChordFlow.Desktop/wwwroot/soundfont/sonivox.sf2` AFTER the broad `*.sf2`/`*.sf3` ignore (big banks stay ignored), and fix the now-stale comment. `git add` the 1.35 MB Apache-2.0 file. Remove the `FetchSoundfont` MSBuild target + its comment from the csproj. The existing `<Content Include="wwwroot\**\*">` already copies the soundfont folder to the publish output, so no other build change. Verify `dotnet build` still places sonivox.sf2 in the output and playback works offline. Realigns with the soundfont-library plan's Step 6.

<!-- step:assemblyname-chordflow -->
### Step 2 — AssemblyName → ChordFlow

Add `<AssemblyName>ChordFlow</AssemblyName>` to the Desktop csproj PropertyGroup. Grep the repo/docs for any hardcoded `ChordFlow.Desktop.exe` reference and update. Confirm `dotnet build`/`dotnet run` still launch and the WebView2 virtual host still resolves wwwroot.

<!-- step:release-yml -->
### Step 3 — release.yml

Trigger on push tags `v*.*.*` + `workflow_dispatch { dry_run: boolean = true }`. guard (windows-latest): parse tag→VERSION (dispatch reads csproj <Version>), assert csproj <Version>==VERSION and a literal `## [VERSION]` section exists in CHANGELOG.md (index-based awk match). build-test: actions/setup-dotnet (net10 SDK) → dotnet restore → build -c Release → test -c Release → publish src/ChordFlow.Desktop -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true → zip publish output → upload-artifact (ChordFlow-vX.Y.Z-win-x64.zip). release (if push only): download artifact, extract CHANGELOG section, `gh release create` with notes passed via env (not inline ${{ }}) + attach the zip; permissions contents: write. Dry-run runs guard+build-test only.

<!-- step:releasing-md -->
### Step 4 — RELEASING.md

Tag-driven checklist adapted from Loom, minus registries: record CHANGELOG [Unreleased]→[X.Y.Z], review docs, bump csproj <Version>, build+test green, loom record-release, commit/annotated-tag/push --follow-tags. Gotchas: unsigned-exe SmartScreen 'unknown publisher' (More info → Run anyway); only sonivox.sf2 ships (committed) + drop-your-own-.sf2; windows-latest runner; no registry secrets (only auto GITHUB_TOKEN). Document the dry-run dispatch.

<!-- step:do-release-command -->
### Step 5 — do-release command

Operational runbook command. Version required/never assumed (empty → show patch/minor/major candidates from csproj <Version> and STOP). Commit stray roadmap reflows. Gather git log <lastTag>..HEAD with bodies → Added/Changed/Fixed. Draft CHANGELOG [X.Y.Z] + bottom link ref. Review README + 3 refs (+ user guide). STOP for go. Then bump csproj <Version>, dotnet build+test, loom record-release X.Y.Z, commit 'release: vX.Y.Z', annotated tag, push --follow-tags, monitor gh run. Recovery = rerun same tag.

<!-- step:readme-download-soundfont-note -->
### Step 6 — README download + soundfont note

Add a 'Download & install' section pointing at the GitHub release zip (incl. the SmartScreen 'unknown publisher' first-run note). Flip the three 'fetched at build time' mentions (~lines 51/57/69) to 'bundled (committed)'. Keep/refresh the curated add-your-own-soundfont download list + the 'drop a .sf2 into wwwroot/soundfont/' note (Decision 3 — README half; the user-guide half lives in docs/user-guide).

<!-- step:ref-sync-architecture -->
### Step 7 — Ref-sync architecture

Per the CLAUDE.md Reference-doc sync rule: update chordflow-architecture-reference.md where it describes the build-time soundfont fetch to reflect the committed asset. Sweep for any other doc referencing FetchSoundfont/DownloadFile.

<!-- step:dry-run-validation -->
### Step 8 — Dry-run validation

Push the branch and trigger `workflow_dispatch { dry_run: true }`; confirm guard + build-test are green and the zip artifact is produced (exe + wwwroot incl. sonivox.sf2) before cutting the first real vX.Y.Z tag under the new workflow.
