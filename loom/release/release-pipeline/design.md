---
type: design
id: de_01KVB4K1PTQ437QZHX8PWX0N63
title: Release pipeline — automate ChordFlow releases (adapted from Loom)
status: done
created: 2026-06-17
updated: 2026-06-17
version: 3
tags: []
parent_id: null
requires_load: []
---
# Release pipeline — automate ChordFlow releases (adapted from Loom)

## Goal

Automate the ChordFlow release so a single command + tag push produces a consistent, reviewed release: **CHANGELOG written, docs reviewed, tests green, .NET 10 build verified, a downloadable Windows artifact attached, and a GitHub release cut** — with the Loom roadmap stamped via `loom record-release`.

Adapted from the proven Loom pipeline (`J:/src/loom/`): `.claude/commands/do-release.md`, `RELEASING.md`, `.github/workflows/release.yml`. ChordFlow shares the same Loom base, so all Loom-workflow behaviour (notably `loom record-release X.Y.Z`) carries over **unchanged**.

Source conversation: `release-pipeline-chat-001.md`. **All open decisions below are resolved** (see "Decisions").

---

## What's adapted vs dropped (vs Loom)

Loom's pipeline exists primarily to **publish to three registries** (npm + VS Code Marketplace + Open VSX) with immutable-version, idempotent, skip-if-published jobs. **ChordFlow publishes to no registry.** So:

**Dropped entirely:** the `publish-npm | publish-vsce | publish-ovsx` fan-out; all registry secrets / OIDC Trusted Publishing setup; the dual-changelog dance (root + extension); `scripts/bump-version.sh` (Loom needs it to sync 7 `package.json`s); the registry partial-failure recovery section.

**Kept (adapted):** the tag-driven spine `guard → build-test → release`; the version-sync guard; CHANGELOG-section-as-release-body (extracted verbatim, passed via env to avoid script injection); `loom record-release`; the `/do-release` command shape; the `RELEASING.md` runbook; the lightweight `workflow_dispatch` dry-run.

**Added (ChordFlow-specific):** a **Windows artifact** — `dotnet publish` self-contained build, zipped and uploaded to the GitHub release.

---

## Version source — single csproj property

The authoritative version is the single `<Version>` in `src/ChordFlow.Desktop/ChordFlow.Desktop.csproj` (currently `0.4.0`). No multi-file sync, so **no bump script** — the `/do-release` command edits that one element directly. (Core and Tests don't carry a `<Version>`; only Desktop, the shipped host, does.)

---

## The artifact — self-contained Windows build, zipped

- **Self-contained** (`--self-contained -r win-x64`) — bundles the .NET 10 runtime. The audience is guitarists, not developers; "install the .NET Desktop Runtime first" would kill download-and-run. WebView2 runtime is *not* bundled (evergreen, pre-installed on Win10/11).
- **Single-file** (`-p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true`) — collapses the runtime DLLs (and native deps like `e_sqlite3`, the WebView2 loader) into the exe.
- **Exe name: `ChordFlow.exe`** — set `<AssemblyName>ChordFlow</AssemblyName>` so the end-user exe isn't `ChordFlow.Desktop.exe`. *(Decision 1.)*
- **The artifact is a folder, not a bare exe.** The host serves `wwwroot/` from disk (`SetVirtualHostNameToFolderMapping`) and **auto-discovers soundfonts from `wwwroot/soundfont/` at runtime**, so `wwwroot/**` ships as **loose `Content` files beside the exe** (single-file does *not* embed Content — and must not, or the drop-in-your-own-soundfont feature breaks). Output = `ChordFlow.exe` + `wwwroot/`.
- **Distributed as a zip:** `ChordFlow-vX.Y.Z-win-x64.zip` (exe + `wwwroot/`). Zipping also softens the browser/SmartScreen hard-warning that a raw `.exe` download triggers.
- **No trimming** — WinForms + EF Core are reflection-heavy; trimming risks runtime breakage for marginal savings.

### SoundFont shipping — commit the default (resolved)

**`sonivox.sf2` becomes a committed, tracked asset; the build-time fetch is removed.** *(Decision: confirmed in chat.)*

- Today `FetchSoundfont` (MSBuild `DownloadFile`) pulls `sonivox.sf2` (1.35 MB, Apache-2.0) from the jsdelivr CDN on build. That makes every CI/release build depend on an external CDN + a pinned `alphatab@1.8.3` URL — a blip fails a release. **Committing the file makes the build hermetic and reproducible** (bytes shipped = bytes in repo), removes the target, and lets offline/first-clone builds work. 1.35 MB Apache-2.0 is a negligible, license-clean git blob.
- This **realigns with the `soundfont-library` plan's Step 6** ("Keep `sonivox.sf2` tracked (ships as default + fallback)") — the build-time fetch was a later drift.
- `<Content Include="wwwroot\**\*">` already copies the soundfont folder into the publish output → it's in the zip with no extra step.
- `.gitignore` keeps the broad `*.sf2`/`*.sf3` ignore (so the big local banks — Arachno/FluidR3/MuseScore/GeneralUser, 32–215 MB — never enter git or CI) **plus** a negation un-ignoring `sonivox.sf2`.
- `WwwrootSoundFontCatalog` still auto-discovers any `*.sf2` in the installed folder, so users add their own bank by dropping a file next to the exe.

---

## CI runner — `windows-latest` (forced)

`ChordFlow.Desktop` is `net10.0-windows` + WinForms; it won't build on `ubuntu-latest`. A whole-solution build/test/publish therefore runs on `windows-latest` (more Actions minutes than Loom's ubuntu jobs, but mandatory). Uses `actions/setup-dotnet` pinned to the .NET 10 SDK.

---

## Workflow — `.github/workflows/release.yml`

Trigger: `push` tags `v*.*.*`, plus `workflow_dispatch { dry_run: boolean = true }`. *(Dry-run kept — Decision 2.)*

Job graph: **`guard → build-test → release`** (no publish fan-out).

1. **`guard`** (windows-latest): parse the tag → `VERSION` (`workflow_dispatch` reads `<Version>` from the csproj instead). Assert **csproj `<Version>` == VERSION**, and assert a dated `## [VERSION]` section exists in `CHANGELOG.md` (literal/index-based match — a dynamic regex treats `[X.Y.Z]` as a character class). Fails fast before any build. Runs on dry-runs too.
2. **`build-test`** (windows-latest, needs guard): `dotnet restore` → `dotnet build -c Release` → `dotnet test -c Release` → `dotnet publish src/ChordFlow.Desktop -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true` → zip publish output to `ChordFlow-vX.Y.Z-win-x64.zip` → `upload-artifact`. (`sonivox.sf2` is committed, so the zip carries it with no network step.)
3. **`release`** (needs build-test; `if: github.event_name == 'push'` — never on dry-run): download the zip artifact; extract the `CHANGELOG.md` `[VERSION]` section (same literal awk capture as Loom); `gh release create vX.Y.Z --title vX.Y.Z --notes-file <section> <zip>`. Release notes passed via **env, not inline `${{ }}`** (script-injection safety, carried from Loom). `permissions: contents: write`.

**Dry-run:** `workflow_dispatch { dry_run: true }` exercises `guard + build-test` (build + tests + zip) against the current branch without cutting a release — used to validate the workflow before the first tag pushed under it.

---

## `/do-release X.Y.Z` command (`.claude/commands/do-release.md`)

Operational runbook task (does **not** load the `release-pipeline` thread bundle). Steps, adapted:

1. **Version required, never assumed.** Empty `$ARGUMENTS` → show candidates from current csproj `<Version>` (patch/minor/major) and STOP.
2. **Commit stray roadmap reflows** (`chore: roadmap`) so the release commit stays clean; STOP on non-roadmap `thread.md` diffs.
3. **Gather changes:** `git log <lastTag>..HEAD --format=...` with full bodies; sort into Added/Changed/Fixed; drop chore/docs/roadmap.
4. **Draft `CHANGELOG.md`** — new dated `## [X.Y.Z]` section + the bottom `[X.Y.Z]: https://github.com/reslava/chord-flow/releases/tag/vX.Y.Z` link ref.
5. **Review docs for accuracy at the new version** — root `README.md` **and the three `loom/refs/` docs** (architecture, domain-model, DSL) **+ the user guide** once it exists. (The refs are kept current per-change by the CLAUDE.md "Reference-doc sync" rule; this is a release-time sanity glance, not a regen.)
6. **STOP — show proposed version + CHANGELOG section, wait for `go`** (it's published verbatim).
7. On `go`: bump csproj `<Version>` → `dotnet build -c Release && dotnet test -c Release` (red = stop) → `loom record-release X.Y.Z` (after build) → `git commit -am "release: vX.Y.Z"` (record-release stamps land here) → `git tag -a vX.Y.Z -m "vX.Y.Z"` (**annotated** or `--follow-tags` won't push it) → `git push --follow-tags`.
8. **Monitor:** `gh run list --workflow=release.yml --limit 1` → `gh run watch <id> --exit-status`.
9. **Recovery:** a transient CI failure re-runs the **same tag** (`gh run rerun <id> --failed`). No registries to worry about.

---

## `RELEASING.md` runbook

Trust-based pre-tag checklist + gotchas, minus registry content. Gotchas to document:
- **Unsigned exe → SmartScreen "unknown publisher"** on first run (until reputation builds, or a code-signing cert is bought — future). Document "More info → Run anyway".
- **Soundfont:** only `sonivox.sf2` ships (committed); richer banks are user-added (drop `.sf2` into `wwwroot/soundfont/`); README curated list.
- **CI is windows-latest.**
- First-time: no registry secrets; only `GITHUB_TOKEN` (auto-provided).

---

## Decisions (resolved)

1. **Exe rename → `ChordFlow.exe`** via `<AssemblyName>`. ✓
2. **Keep the dry-run `workflow_dispatch`.** ✓
3. **Soundfont note in both** README (this thread) and the user guide (`docs/user-guide` thread). ✓
4. **Commit `sonivox.sf2`, remove the build-time fetch** (hermetic builds; realigns with `soundfont-library` Step 6). ✓

---

## Out of scope / deferred (additive later)
- Code signing (Authenticode cert) to clear SmartScreen.
- Store distribution: MS Store / Gumroad / itch.io (each an additive publish job, like Loom's registry jobs).
- Bundling the user guide + LICENSE into the release zip (once the `docs/user-guide` thread produces a guide).
- A macOS/Linux build (blocked until a cross-platform host exists; the Core split already makes it additive).
