---
type: chat
id: ch_01KV7N19WZS1MM8JCHR9ZN69KT
title: soundfont-library Chat
status: done
created: 2026-06-16
updated: 2026-06-16
version: 2
tags: []
parent_id: null
requires_load: []
last_ai_block: 10
---
# soundfont-library Chat

## Rafa:

I am thinkingh in a new soundFont related feature, also with UI to load different soundFonts
I already created playback/soundfont-library

## AI:

**2. Noted** — `playback/soundfont-library` exists. Want me to start its idea doc? I'd capture: a soundfont catalog served from `wwwroot/soundfont/`, a picker UI in the score controls strip wired to `player.soundFont` + `api.loadSoundFont(...)`, and the open question of whether the chosen font persists (per-exercise vs. global setting). Say go and I'll draft it.

## Rafa:

2. global setting. 
- should we gitignore the soundfonts becuase of file sizes and just give the links to download them and what path to place them? or should we ship them as part of the app?

go, create the idea

## AI:

Created the idea: `playback/soundfont-library/soundfont-library-idea.md` (`id_01KV7MTAXG10Y0HQVYABXZ7TVA`, status draft). Baked in **global setting** as decided.

**On ship-vs-gitignore — I went with a hybrid (my recommendation):**

1. **Ship one small, license-clean default** (the existing `sonivox.sf2`, a few MB) so a fresh clone/build plays out of the box — the player is never broken on first run.
2. **Gitignore everything else** (`wwwroot/soundfont/*.sf2` except the default) so big GM banks (tens–hundreds of MB) and license-iffy fonts never bloat the repo.
3. **Curated download list** in the README (name · license · URL · target path = `wwwroot/soundfont/`); the picker auto-discovers whatever `.sf2` files are present, so adding one is a pure data drop — no code change.

Why not just ship a rich set? Repo bloat + per-font license review for marginal gain, when the picker discovering a user-dropped folder gives the same UX with none of that. I noted the "ship richer set" alternative in the idea for the design step in case you want to revisit.

Next: finalize the idea (draft → active), then a design doc, then a plan. Want me to finalize and move to design?

## Rafa:

idea marked done
create design
note: this is active chat now


## AI:

Design created: `soundfont-library-design.md` (`de_01KV7NET48R277ZA3EWC9ZR6ZY`, draft). It covers four moving parts (discovery, persistence, live switch, UI) + the hybrid shipping policy.

Two decisions are architectural, so I stopped short of a plan and want your call:

**Decision 1 — how fonts are discovered.** JS can't list a virtual-host directory, so auto-discovery needs server-side enumeration of `wwwroot/soundfont/`.
- **Recommended:** `ISoundFontCatalog` in Core + a `SoundFontLibrary` feature slice, implemented by Desktop (scans the folder). New bridge verb `listSoundFonts` → `soundFontsListed`. Keeps the feature in Core (the grain); a web host plugs in its own catalog.
- **Alt:** host answers the verb directly — simpler, but a feature living in the host, re-implemented per host.

**Decision 2 — where the global choice persists.** It's an app setting, not content.
- **Recommended:** a small `AppSettings` key/value table in Core (EF Core + migration) via `IAppSettings`; the font is one key, future global prefs reuse it.
- **Alt:** a host-side JSON file — less code, but fragments persistence out of Core.

Decisions 3 (live switch via `updateSettings`/`loadSoundFont`) and 4 (a `<select>` in the controls strip) are implementation details — no sign-off needed.

Sign off on 1 and 2 (or pick the alternatives) and I'll write the plan. My recommendation is the Core-side option for both — it's the durable choice and keeps the web-host path additive.

## Rafa:

Core-side for both

## AI:

Plan created: `soundfont-library-plan-001.md` (`pl_01KV7NP49XN0CE351Q7YRAVJM3`, draft). Both decisions locked to the Core option. Seven steps:

1. **Core `AppSettings` store** — `AppSetting` entity + `IAppSettings` (Get/Set) + EF config + migration. (no blockers)
2. **Core catalog + feature** — `ISoundFontCatalog`/`SoundFontInfo` + `SoundFontLibrary` (lists fonts, reads/writes selected id, sonivox fallback). *(blocked by 1)*
3. **Bridge contract** — `soundFontsListed` envelope + router wiring for `listSoundFonts`/`setSoundFont`. *(blocked by 2)*
4. **Desktop** — `WwwrootSoundFontCatalog` (scans `wwwroot/soundfont/*.sf2`), DI, verb handlers. *(blocked by 3)*
5. **wwwroot** — `bridge.js` fan-out + the picker/live-switch in `score-render-component.js`, replacing the hardcoded path. *(blocked by 3)*
6. **Shipping** — gitignore non-default `.sf2`, README download list. (no blockers)
7. **Ref-sync** — update `chordflow-architecture-reference.md` (§3 + §5). *(blocked by 1–5)*

Steps 4 and 5 both unblock after 3, so the Desktop and JS work can go in either order. Want me to start the plan (→ implementing) and take **Step 1**? I'll do Step 1, then stop for `go` per the usual cadence.

## Rafa:

We forgot generate requirements
Now we should generate requirements and ammend plan

## AI:

Done — req generated, locked, and the plan amended to cite it. Clean `verify_req` (no uncovered Included, no excluded violations, no dangling citations).

**`req.md`** (`rq_01KV7P09B3S4G6SZP6KDD2M6NM`, locked):
- **Included** — `IN1` picker UI · `IN2` auto-discovery (data drop, no code change) · `IN3` global persisted choice · `IN4` live switch · `IN5` ship sonivox default/fallback.
- **Excluded** — `EX1` in-app downloading · `EX2` per-track fonts · `EX3` font editing/remapping · `EX4` bundling large banks in the repo · `EX5` per-exercise choice.
- **Constraints** — `C1` no Domain/renderer/alphaTex change · `C2` discovery seam in Core · `C3` persistence in Core · `C4` extends the narrow envelope protocol · `C5` `score-render-component` stays the sole alphaTab owner.

**Plan citations** (step → satisfies):
1 → IN3, C3 · 2 → IN1, IN2, IN3, C2 · 3 → IN1, IN2, C4 · 4 → IN2, IN3, C2 · 5 → IN1, IN4, C5 · 6 → IN5 · 7 → C2, C3, C4.

One verify note worth recording: I'd initially cited `EX4` on the gitignore step — `verify_req` correctly flagged it, since `satisfies` is for Included/Constraints a step *advances*, not exclusions it *enforces*. Removed; re-verify is clean.

Plan unchanged structurally (still 7 steps, `implementing`). Ready to resume the build — I was mid-exploration on **Step 1** (Core `AppSettings` store) when you said stop. Want me to continue the whole plan from Step 1?

## AI:

✅ **Whole plan done** (all 7 steps; plan auto-closed to `done`). Solution builds, 399/399 Core tests pass.

**What landed**
- **Step 1** — Core `AppSettings` key/value store: `AppSettingEntity`, `IAppSettings`, `AppSettingsStore` (app-lifetime singleton over `DbContextOptions`, short-lived context per access), DbSet + `HasKey`, EF migration `AddAppSettings`.
- **Step 2** — `ISoundFontCatalog`/`SoundFontInfo` (Bridge) + `SoundFontLibrary` feature (lists fonts + persisted selection with layered fallback; `SetSelected`).
- **Step 3** — `SoundFontsListedEnvelope`/`SoundFontDto` + router `listSoundFonts`/`setSoundFont` verbs (`SoundFontId` field).
- **Step 4** — `WwwrootSoundFontCatalog` (scans `wwwroot/soundfont/*.sf2`, friendly names) + `Program.cs` wiring.
- **Step 5** — picker `<select>` + live switch in `score-render-component.js` (`loadSoundFontFromUrl`), requests the list on init, persists on change, replaced the hardcoded path.
- **Step 7** — architecture ref updated (§3 AppSettings + SoundFontLibrary/ISoundFontCatalog, §5 the verbs + player-kind picker).

**⚠️ One deviation worth your eyes — Step 6 (shipping).** The plan assumed sonivox.sf2 was a committed asset to keep tracked. It isn't: `ChordFlow.Desktop.csproj` has a `FetchSoundfont` MSBuild target that downloads it from the alphaTab CDN at build time, and `.gitignore` already ignores **all** `*.sf2`. That existing design already satisfies IN5 (default present in the built app) + EX4 (no banks in the repo) — more cleanly than committing the font. So instead of changing the ignore rule, I:
- kept the `*.sf2` ignore, expanded its comment to explain the fetch + auto-discovery;
- added a **Soundfonts** section to the README (how to drop a font in + pick it, that added fonts are git-ignored & auto-discovered, plus a curated table: Sonivox/Apache-2.0, FluidR3 GM/MIT, GeneralUser GS/free).

If you'd rather I *also* commit a default font (track sonivox and drop the build-time fetch), say so — but I think the fetch design is the better one and left it intact.

**Can't fully verify here:** the live font swap (`loadSoundFontFromUrl`) and the picker UI need a run of the desktop app — I confirmed the method exists in the bundled alphaTab and the C#/build/tests are green, but haven't clicked it. Worth a manual smoke test: pick a second font, confirm it switches and survives a restart.

req citations all clean (`verify_req` green). Want me to `git add`/commit this, or leave it for your review first?