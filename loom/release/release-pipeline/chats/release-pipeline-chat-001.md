---
type: chat
id: ch_01KVB1Y63DJ6GBRYZVRVEZ868Z
title: release-pipeline Chat
status: active
created: 2026-06-17
version: 1
tags: []
parent_id: null
requires_load: []
last_ai_block: 20
---
# release-pipeline Chat

## Rafa:

### Goal

automatize repo / app release

### How

- claude commands
- github actions
- runbook

### Based on / copy and adapt

I think best approach is copy and adapt the pipeline I already built with in in Loom project `J:/src/loom/`

- Claude command: `J:/src/loom/.claude/commands/do-release.md`
- Runbok: `J:/src/loom/RELEASING.md`
- GitHub actions: `J:/src/loom/.github/workflows/release.yml`

### Adapt

#### Diferences

Loom is different app an publish artifacts:
- vscode extension in 2 markets 
- npm

Guitar Flow currently do not publish any artifact. In the future, maybe in Gumroad/itch/MS Store. 
Actually guitar flow needs to take care of: changelog, github release, update readmes, pass tests, .NET 10 build ok...


Instead of using:
- Root `package.json` `version` — the current version (the only authoritative source).
in Guitar Flow we use:
- `src/ChordFlow.Desktop/ChordFlow.Desktop.csproj`:
```
  <PropertyGroup>
    ... 
    <Version>0.4.0</Version>
  </PropertyGroup>
```   

But we can ommit these part and extract the rest.

#### Shared

Guitar Flow is develper using Loom
and Loom also uses Loom to be developed 
so both projects share same Loom base,

All loom based behaviour should be the same, for example:

- In Claude command is important to call `loom record-release X.Y.Z`

### TODO

Read & review the files to copy and adapt from release pipeline of Loom.
Tell me if you have any question.

## AI:

Read all three Loom files (`do-release.md`, `RELEASING.md`, `release.yml`) and surveyed the ChordFlow repo. Here's what maps over cleanly, what drops, and the open questions before I'd design anything.

## What ChordFlow actually is (release-relevant facts)

- **One .NET solution**, three projects: `ChordFlow.Core` (`net10.0`), `ChordFlow.Desktop` (`net10.0-windows`, WinForms+WebView2), `tests/ChordFlow.Core.Tests` (`net10.0`).
- **Single version source:** `<Version>0.4.0</Version>` in `src/ChordFlow.Desktop/ChordFlow.Desktop.csproj`. Just one place, not Loom's 7 `package.json`s.
- **CHANGELOG.md** already in Keep-a-Changelog format (currently top section is `[0.4.0]`, with `[X.Y.Z]: https://github.com/...` link refs maintained by hand at the bottom).
- **One README.md** (root). No CLI/extension READMEs.
- Tags `v0.1.0`–`v0.4.0` exist and **GitHub releases were already cut by hand** for each.
- **No `scripts/`, no `.github/workflows/`, no `RELEASING.md`, no `.claude/commands/`** yet — all net-new.
- Build/test = `dotnet build` / `dotnet test` (not npm).

## The big structural difference

Loom's whole reason for a CI pipeline is **publishing to 3 registries** (npm + 2 marketplaces) with immutable-version, idempotent, skip-if-published jobs. ChordFlow publishes **nothing** today. So the entire `publish-npm | publish-vsce | publish-ovsx` fan-out, all the registry-secret/OIDC/Trusted-Publishing setup, the dual-changelog dance, and the partial-failure-recovery section **all drop**.

What's left is the spine you actually asked for:

> changelog · GitHub release · update README · pass tests · .NET 10 build ok

So the adapted shape is roughly:

- **`/do-release X.Y.Z` command** — version required (never assumed); commit stray roadmap reflows; gather `git log <lastTag>..HEAD`; draft the single root `CHANGELOG.md` `[X.Y.Z]` section + bottom link ref; review README; STOP for `go`; then bump csproj `<Version>`, `dotnet build` + `dotnet test`, `loom record-release X.Y.Z`, commit `release: vX.Y.Z`, annotated tag, `git push --follow-tags`, monitor the workflow.
- **`RELEASING.md`** — the trust-based checklist + gotchas, minus everything registry-related.
- **`release.yml`** — `guard` (tag → version; assert csproj `<Version>` == tag; assert `CHANGELOG.md` `[X.Y.Z]` section exists) → `build-test` → `release` (cut GitHub release with the CHANGELOG section as the body, verbatim, via the safe env-passing pattern Loom uses).

The `loom record-release X.Y.Z` step carries over **unchanged** — same shared Loom base.

## Things that are forced (not really decisions, just flagging)

1. **CI must run on `windows-latest`.** `ChordFlow.Desktop` is `net10.0-windows` + WinForms — it won't build on `ubuntu-latest`. Core+Tests alone could build on Linux, but a whole-solution build/test needs Windows. (Costs more Actions minutes than Loom's ubuntu jobs, but it's mandatory.)
2. **No bump script needed.** Loom's `bump-version.sh` exists to keep 7 files in sync. For one csproj value, the command edits it directly — a script would be ceremony.

## Open questions (your call before I design)

1. **Does the GitHub release attach a downloadable build, or stay notes-only (tag + changelog) like today?** This is the biggest fork. Options:
   - **(a) Notes-only now** — matches "no artifact today"; simplest; release page = notes + auto source zip. I lean here for v1, *designing the workflow so adding an artifact step later is purely additive*.
   - **(b) Attach a Windows build now** — a `dotnet publish` self-contained single-file `.zip` (or just the `.exe`) uploaded to the release, so people can actually download + run ChordFlow. Given the future Gumroad/itch/MS Store idea, a downloadable Windows build is the natural in-between and makes the GitHub release genuinely useful rather than dev-only.
   - If (b): self-contained (no .NET runtime needed, ~big) vs framework-dependent (small, needs .NET 10 installed)? And single-file `.exe` vs zipped publish folder?

2. **Dry-run trigger?** Loom keeps a `workflow_dispatch { dry_run }` to rehearse the pipeline before the first real tag. With no publishing, "dry-run" basically = guard + build-test (cheap, useful to validate the workflow on a branch before pushing your first tag under it). Keep it, or skip since there's nothing to *not*-publish?

3. **README scope** — just the root `README.md` to review for accuracy each release (vs Loom's 3), correct?

My recommendation: **1(a) notes-only**, **2 keep a lightweight dry-run**, **3 yes root README only** — that's the minimal correct spine, and the artifact upload becomes a clean additive step the day you want downloads. But the artifact question (1) is genuinely yours — if you'd rather ship a downloadable Windows build from day one, that changes the `build-test`/`release` jobs and the runbook. How do you want to play it?

## Rafa:

I forgot to mention these docs:
loom/refs/chordflow-architecture-reference.md
loom/refs/chordflow-domain-model-reference.md
loom/refs/chordflow-dsl-reference.md
But I think I told you already to include a rule for them in CLAUDE.md, confirm pls

Maybe we should add a `user guide`

1. (b) tell me your recommendations to pick between (I never published EXE artifacts in github before):
   1. self-contained  vs framework-dependent 
   2. single-file `.exe` vs zipped

## AI:

Three things in order.

## 1. The refs rule — confirmed, it's already in CLAUDE.md

Yes. `.loom/CLAUDE.md` has a **"Reference-doc sync (required)"** section that already governs exactly those three docs, bidirectionally:

| Area changed | Ref to UPDATE same change | Ref to LOAD before reasoning |
|---|---|---|
| core DSL | `chordflow-dsl-reference.md` | same |
| domain/kernel | `chordflow-domain-model-reference.md` | same |
| app architecture | `chordflow-architecture-reference.md` | same |

Key point: that rule fires **per code-change**, not per-release — the refs are kept current continuously, so the release doesn't need to *regenerate* them. What the release pipeline *should* do is add them to the **doc-accuracy review step** alongside the README (they're public-facing — the DSL ref is even linked from the README). So the `/do-release` "review docs" step becomes: **root README + the three `loom/refs/` docs** (+ user guide, see below) glanced for accuracy at the new version. No new rule needed — just widening the release checklist's review list to point at docs the CLAUDE.md rule already keeps fresh.

## 2. User guide — good idea, but it's its own deliverable

A real end-user guide makes a lot of sense the moment you ship a downloadable build (point 3 below) — guitarists downloading a `.zip` need a "how do I use this" page. But it's a **content deliverable**, not part of the release *machinery*. I'd treat it as its own thread/doc (an idea → the guide doc), and let it intersect the release pipeline in just two spots:
- it joins the doc-review list above, and
- optionally it gets **bundled into the release `.zip`** (a `USERGUIDE.md`/`.pdf` next to the `.exe`) and/or linked from the release notes.

So: I'd **not** fold writing the guide into this release-pipeline thread — keep this thread about the pipeline, spin the guide separately. Want me to stub a `user-guide` idea later, or park it?

## 3. Windows EXE artifact — recommendations (you picked (b))

Short answer: **self-contained, single-file `.exe`, shipped inside a `.zip`.** Reasoning for each fork:

### 3.1 Self-contained vs framework-dependent → **self-contained**

- **Framework-dependent** = tiny (~5 MB) but the user must already have the **.NET 10 Desktop Runtime** installed. Your audience is **guitarists, not developers** — most won't have it, and "download this, then also go install a Microsoft runtime first" kills the download-and-run promise. Dealbreaker for end users.
- **Self-contained** bundles the .NET runtime, so it just runs. Cost is size (~**100–180 MB** for a WinForms app). That's totally fine for a GitHub release asset.
- **WebView2 runtime** you do *not* need to bundle either way — it's evergreen and pre-installed on Windows 11 and virtually all Win10. Good.
- Note: **don't enable trimming.** WinForms + EF Core are reflection-heavy; trimming risks runtime breakage for marginal size savings. Ship untrimmed.

### 3.2 single-file `.exe` vs zipped → **both: single-file, inside a zip**

These aren't really opposites — the cleanest answer combines them:
- Build with `PublishSingleFile=true` so the output is **one `.exe`** (native deps like `e_sqlite3` for EF Core SQLite and the WebView2 loader get embedded and self-extract at runtime — needs `IncludeNativeLibrariesForSelfExtract=true`).
- Then **zip that single `.exe`** (later: + `USERGUIDE`/`LICENSE`) and upload the `.zip` as the release asset.

Why zip a single file:
- A **raw `.exe` download** trips browser/Windows "this file may harm your computer" hard-warnings much harder than a `.zip` does. The zip softens the download path.
- One clean asset on the release page; room to add the user guide/license later without changing the pipeline.

Concretely the build step would be:
```
dotnet publish src/ChordFlow.Desktop -c Release -r win-x64 --self-contained \
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
```
→ zip the publish output → `gh release` uploads `ChordFlow-vX.Y.Z-win-x64.zip`.

### One gotcha to bake into RELEASING.md

The `.exe` will be **unsigned**, so Windows **SmartScreen** shows an "unknown publisher" warning on first run until enough downloads build reputation (or you buy a code-signing cert — a future cost, not now). I'll document the warning + "More info → Run anyway" in the runbook so it's expected, not a surprise bug report.

---

Net: I'll design around **(b) self-contained single-file `.exe` zipped + uploaded to the GitHub release**, doc-review widened to README + 3 refs (+ user guide once it exists), and dry-run kept lightweight. That makes `build-test` produce the zip artifact and `release` upload it. Good to proceed to a design doc on that basis — and do you want the user-guide stubbed as a separate thread now or parked?

## Rafa:

### user-guide 

stub it as a separate thread

### SoundFont

Currently Guitar Flow download sonivox.sf2 through MSBuild, maybe we need to pack this in the .ZIP, correct?

### TODO

Create design

## AI:

All three — and the soundfont question turned up something important that **corrects my earlier "single .exe inside a zip"** answer. Read this part carefully.

## SoundFont — yes it ships, but only `sonivox.sf2`, and it forces the artifact to be a folder

I dug into the build. Here's the real picture:

- `FetchSoundfont` MSBuild target (`BeforeBuild`) downloads **`sonivox.sf2`** (1.35 MB, Apache-2.0) from the alphaTab CDN into `wwwroot/soundfont/` if absent.
- `wwwroot/**` is `<Content CopyToOutputDirectory=PreserveNewest>`, so **`dotnet publish` already copies the whole `wwwroot` tree (sonivox included) into the publish folder.** → it lands in the zip **automatically**. No extra "pack the soundfont" step needed.
- `.gitignore` excludes **all** `*.sf2`/`*.sf3`. So the four big fonts you have locally — Arachno (155 MB), FluidR3 (148 MB), MuseScore (215 MB), GeneralUser (32 MB) — are **local-only, never in git, never downloaded by CI**. Good: they must **not** ship (500+ MB + their own licenses).
- `WwwrootSoundFontCatalog` **auto-discovers** any `*.sf2` in the folder at runtime. So a user who wants a richer bank drops their own `.sf2` into the installed `wwwroot/soundfont/` next to the exe and it appears in the picker — no reinstall.

**So: the CI-built zip ships exactly one soundfont, `sonivox.sf2`, automatically. Correct and intended.** The README's curated download list covers the rest, and the user guide should explain "drop a `.sf2` here to add instruments."

### The correction this forces on the artifact shape

Because the app **serves `wwwroot/` from disk** (`SetVirtualHostNameToFolderMapping`) and **discovers soundfonts from that folder at runtime**, the published output is inherently a **folder = `ChordFlow.Desktop.exe` + `wwwroot/`**, not a bare single `.exe`. `PublishSingleFile=true` only collapses the **.NET runtime DLLs** into the exe; `Content` files (the whole `wwwroot` tree — `index.html`, `app.js`, `alphaTab.min.js`, Bravura font, `soundfont/sonivox.sf2`) are emitted as **loose files beside the exe**, and that's exactly what we want — embedding `wwwroot` would *break* the drop-in-your-own-soundfont feature.

So the artifact is: **self-contained single-file `ChordFlow.Desktop.exe` + the `wwwroot/` folder, zipped together** → `ChordFlow-vX.Y.Z-win-x64.zip`. Same zip, same self-contained decision — just be clear it's "exe + wwwroot folder," not one file.

(Minor optional polish: set `<AssemblyName>ChordFlow</AssemblyName>` so the exe is `ChordFlow.exe` rather than `ChordFlow.Desktop.exe` for end users. Not blocking — flagging for the design's "nice-to-have" list.)

## user-guide — stubbing as a separate thread now

Creating it as a new thread with a short idea stub. There's no `docs` weave today, so I'm placing it at **`loom/docs/user-guide/`** (a new `docs` weave — the natural home for future user-facing docs like FAQ/tutorials too). Say the word if you'd rather it live under `meta` or elsewhere and I'll move it.

## Creating the design now

Writing `release-pipeline-design.md` capturing: the Loom-adapted spine (guard → build-test → release), single csproj version source, the **(b) self-contained exe + wwwroot zip** artifact with the soundfont reality above, `windows-latest` runner, the widened doc-review (README + 3 refs + user guide), lightweight dry-run, the `/do-release` command shape, and the `RELEASING.md` runbook outline — with the unsigned-exe/SmartScreen and soundfont gotchas baked in. I'll drop a couple of open decisions at the end for sign-off before any plan.

## AI:

Done — both created:

- **Design:** `loom/release/release-pipeline/release-pipeline-design.md` (`de_01KVB4K1PTQ437QZHX8PWX0N63`) — the full adapted pipeline (guard → build-test → release, single csproj version, self-contained exe + `wwwroot` zip with the soundfont reality, windows-latest, widened doc review, `/do-release` shape, `RELEASING.md` outline, gotchas).
- **User-guide stub:** new `docs` weave → `loom/docs/user-guide/user-guide-idea.md` (`id_01KVB4KGMYZTB1FXVB3XJ82HTG`).

The design ends with **3 open decisions** for your sign-off before I write a plan:
1. Rename the exe to `ChordFlow.exe` (`<AssemblyName>`) as part of this, or leave `ChordFlow.Desktop.exe`?
2. Keep the lightweight dry-run `workflow_dispatch`? (I recommend yes.)
3. README soundfont note — fold into this thread, or leave it to `docs/user-guide`?

**STOP — review the design and answer the three, then I'll write the implementation plan.**

## Rafa:

1. Rename to `ChordFlow.exe`
2. yes
3. soundFont note in both, agree?

### Question

Maybe is better approach for soundFont
- exclude `src/ChordFlow.Desktop/wwwroot/soundfont/sonivox.sf2` from `.gitignore` and publish it
- no need to download it with MSBuild 
- include in the .ZIP

What you think?

### TODO

Create plan when all is grounded

## AI:

Decisions locked: **1** rename to `ChordFlow.exe` ✓ · **2** keep dry-run ✓ · **3** soundfont note in both (README here + the `docs/user-guide` thread) ✓.

## On the soundfont approach — strongly agree, it's the better design

Commit `sonivox.sf2`, drop the MSBuild fetch, let it ride to the zip as a plain asset. Why it's clearly right for a *release* pipeline:

- **Hermetic, reproducible builds.** Today's `FetchSoundfont` makes every CI build depend on the jsdelivr CDN and the pinned `@coderline/alphatab@1.8.3` URL staying alive. A CDN blip or a yanked version would fail a *release* build — the worst time. Committing removes that external dependency entirely; the bytes shipped are the bytes in the repo.
- **Simpler:** the `FetchSoundfont` target + `DownloadFile` disappear; offline/first-clone builds just work.
- **Negligible cost:** 1.35 MB, **Apache-2.0** — trivial git footprint and license-clean to redistribute in-repo and in the zip.
- **It realigns with the documented intent.** The `soundfont-library` plan's own **Step 6** said *"Keep `sonivox.sf2` tracked (ships as default + fallback)."* The build-time fetch was a later drift from that plan — your proposal puts it back.

### Mechanics (folded into the plan)
- `.gitignore`: add a negation `!src/ChordFlow.Desktop/wwwroot/soundfont/sonivox.sf2` **after** the broad `*.sf2` ignore (the big banks stay ignored), and fix the comment.
- `git add` the file once; remove the `FetchSoundfont` target + its comment from `ChordFlow.Desktop.csproj`. The existing `<Content Include="wwwroot\**\*">` already copies it to publish → zip, so nothing else changes there.
- **Doc-sync:** README says *"fetched at build time"* in **3 spots** (lines ~51/57/69), plus the architecture ref and the `.gitignore`/csproj comments — all flip to *"bundled (committed)."*

Updating the design to reflect all of the above, then writing the implementation plan.

## AI:

Design updated and the plan is written.

**Plan:** `loom/release/release-pipeline/plans/release-pipeline-plan-001.md` (`pl_01KVB53F79XSMPYDHNY6KEP72K`) — 8 steps:

1. Commit `sonivox.sf2`, drop `FetchSoundfont` (hermetic builds)
2. `<AssemblyName>ChordFlow</AssemblyName>` → `ChordFlow.exe`
3. `release.yml` — guard → build-test → release, windows-latest, self-contained single-file publish + zip, gh release, dry-run *(blocked by 1, 2)*
4. `RELEASING.md` runbook *(blocked by 3)*
5. `/do-release` command *(blocked by 3)*
6. README — Download/Install section + flip soundfont docs to "bundled" *(blocked by 1)*
7. Ref-sync — architecture reference *(blocked by 1)*
8. Dry-run validation before the first real tag *(blocked by 1, 2, 3)*

**STOP** — review the plan. Say `go` (or "do step 1", "do all") and I'll start at step 1.

## Rafa:

We forgot to generate requirements
- generate them
- ammend the plan
- start plan
- do whole plan