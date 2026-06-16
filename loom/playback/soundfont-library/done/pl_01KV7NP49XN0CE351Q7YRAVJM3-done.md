---
type: done
id: pl_01KV7NP49XN0CE351Q7YRAVJM3-done
title: Done — SoundFont library — implementation
status: done
created: "2026-06-16T00:00:00.000Z"
version: 7
tags: []
parent_id: pl_01KV7NP49XN0CE351Q7YRAVJM3
requires_load: []
---
# Done — SoundFont library — implementation

## Step 1 — Add a Core AppSettings key/value store: AppSetting entity, IAppSettings (Get/Set), EF Core config + migration

Added the Core AppSettings key/value store.

- `Persistence/Entities/AppSettingEntity.cs` — `{ Key, Value }` (placed in Entities/ to match the existing entity convention rather than flat in Persistence/ as the plan sketched).
- `Persistence/IAppSettings.cs` — `Get(key)` / `Set(key, value)`.
- `Persistence/AppSettingsStore.cs` — implements `IAppSettings`. **Design note:** unlike the per-request content stores (which take a live `ChordFlowDbContext`), this takes `DbContextOptions<ChordFlowDbContext>` and opens a short-lived context per call, so the app-lifetime singleton never holds a long-lived tracking context. Access is infrequent (read on boot, write on change).
- `Persistence/ChordFlowDbContext.cs` — added `DbSet<AppSettingEntity> AppSettings` + `HasKey(x => x.Key)` (plain string PK, no Origin tiering — it's app prefs, not content).
- Migration `20260616074544_AddAppSettings` generated via `dotnet ef`; creates `AppSettings(Key TEXT PK, Value TEXT)`. Applies on startup via the existing `db.Database.Migrate()`. Build succeeded.

Satisfies IN3 (persisted global choice), C3 (persistence in Core).

## Step 2 — Add ISoundFontCatalog + SoundFontInfo in Core and a SoundFontLibrary feature slice that lists fonts and reads/writes the selected id

Added the Core soundfont catalog seam + feature.

- `Bridge/ISoundFontCatalog.cs` — `ISoundFontCatalog { IReadOnlyList<SoundFontInfo> List(); }` + `SoundFontInfo(Id, DisplayName)`. Seam in Core (C2); host implements it.
- `Features/SoundFontLibrary.cs` — composes `ISoundFontCatalog` + `IAppSettings`. `ListWithSelection()` returns the fonts + the id to load, with layered fallback: persisted choice if it still names an available font → else `DefaultFont` (sonivox.sf2) if present → else first available → else the default id (never empty). `SetSelected(id)` persists under `SelectedKey = "playback.soundFont"`.

Satisfies IN1, IN2, IN3, C2. Core builds clean.

## Step 3 — Add bridge envelopes + WebMessageRouter wiring for listSoundFonts/setSoundFont (inbound) and soundFontsListed (outbound)

Added the bridge envelope + router wiring.

- `Bridge/SoundFontEnvelopes.cs` — `SoundFontsListedEnvelope(Fonts, SelectedId, Type="soundFontsListed")` + `SoundFontDto(Id, Name)`. (Placed in its own file matching the per-area envelope convention, e.g. ContentCrudEnvelopes.cs — the plan's `Bridge/Envelopes.cs` doesn't exist in this codebase.)
- `Bridge/WebMessageRouter.cs` — new events `ListSoundFontsRequested` / `SetSoundFontRequested(id)`; dispatch cases for `listSoundFonts` / `setSoundFont`; added `SoundFontId` string field to the inbound envelope (distinct from the int `Id` and string `EntityId`). No renderOptions change — font isn't a render input (C4).

Satisfies IN1, IN2, C4. Core builds clean.

## Step 4 — Desktop: implement ISoundFontCatalog (scan wwwroot/soundfont/*.sf2), wire DI, and handle the new verbs (reply soundFontsListed, persist on setSoundFont)

Desktop: catalog implementation + wiring.

- `WebHost/WwwrootSoundFontCatalog.cs` — `ISoundFontCatalog` that enumerates `*.sf2` in the served folder (ordered, case-insensitive), id = file name, display name derived (`fluidr3_gm.sf2` → "Fluidr3 Gm" via `_`/`-` → space + title-case). Returns empty when the folder is missing.
- `Program.cs` — added `using ChordFlow.Features;`; constructed `SoundFontLibrary` (singleton) over `WwwrootSoundFontCatalog(Path.Combine(wwwroot, "soundfont"))` + `AppSettingsStore(dbOptions)`. Wired `router.ListSoundFontsRequested → bridge.Send(soundFonts.ListWithSelection())` and `router.SetSoundFontRequested → soundFonts.SetSelected(id)`.

Full solution builds (one pre-existing, unrelated WindowsBase version-conflict warning). 399/399 Core tests pass. Satisfies IN2, IN3, C2.

## Step 5 — wwwroot: route soundFontsListed in bridge.js and add the soundfont picker + live switch to score-render-component.js, replacing the hardcoded soundFont path

wwwroot: picker + live switch in `score-render-component.js`.

- Added `DEFAULT_SOUNDFONT = "sonivox.sf2"` + `fontUrl(id)`; `buildSettings` boot default now uses `fontUrl(DEFAULT_SOUNDFONT)` instead of the hardcoded string.
- In `create()`: `currentSoundFont`/`disposed` state + a feature-detected `bridge` ref. `applySoundFont(id)` swaps the synth font live via `api.loadSoundFontFromUrl(url, false)` (the method the bundled alphaTab actually exposes — verified in alphaTab.min.js) and mirrors `settings.player.soundFont`; no re-render, no persist. `onSoundFontsListed(msg)` fills the picker and applies the persisted selection (skipping a redundant reload when it already matches the boot default).
- On init (player + host present) the component sends `{type:"listSoundFonts"}` and registers an `onReceive` parser for `soundFontsListed`. `dispose()` sets `disposed` so a late fan-out can't touch a destroyed api.
- `handle.setSoundFont(id)` (select `change`) applies live + sends `{type:"setSoundFont", id}` to persist.
- `soundFontPicker(handle, ui)` builder adds a `<select class="cf-soundfont">` to the full player controls strip; empty until the host reply (so a plain-browser run shows no options and keeps the boot default).

Satisfies IN1, IN4, C5 (player-kind, local control, no C# re-render).

## Step 6 — Shipping policy: gitignore wwwroot/soundfont/*.sf2 except sonivox.sf2; add curated download list to README

Shipping policy — **deviation from the plan, documented here.**

Discovered the default soundfont is **not** committed: `ChordFlow.Desktop.csproj` has a `FetchSoundfont` MSBuild target that `DownloadFile`s `sonivox.sf2` from the alphaTab CDN at build time, and `.gitignore:52` already ignores **all** `*.sf2`. So the plan's "track sonivox.sf2, gitignore the rest" was based on a wrong assumption — the existing build-time-fetch design already satisfies IN5 (default present in the built app) and EX4 (no banks in the repo), more cleanly than committing the font.

So instead of changing the ignore rule:
- `.gitignore` — kept the `*.sf2`/`*.sf3` ignore; expanded the comment to note the default is fetched by `FetchSoundfont` and any user-dropped font is auto-discovered by the picker and intentionally stays out of the repo.
- `README.md` — added a **Soundfonts** subsection: how to drop a `.sf2` into `wwwroot/soundfont/` and pick it (global, remembered), that added fonts are git-ignored + auto-discovered, and a curated table (Sonivox/Apache-2.0 default, FluidR3 GM/MIT, GeneralUser GS/free) with where to get them.

Satisfies IN5; enforces EX4.

## Step 7 — Ref-sync: update chordflow-architecture-reference with the new bridge verbs (§5) and the AppSettings store + SoundFontLibrary feature (§3)

Ref-sync — updated `chordflow-architecture-reference.md` (via loom_patch_doc):
- §3 Persistence — documented the `AppSettings` key/value table (`IAppSettings`/`AppSettingsStore`, app-lifetime singleton over `DbContextOptions`, short-lived context per access).
- §3 Features — added the `SoundFontLibrary` slice + the `ISoundFontCatalog` Core discovery seam (host scans `wwwroot/soundfont`).
- §5 bridge contract — added the `listSoundFonts`/`setSoundFont` verbs + `soundFontsListed` reply, noting font isn't a render input (no renderOptions, no re-render).
- §5 render-component paragraph — added the soundfont picker to the player-kind options (live `loadSoundFontFromUrl` + persist via `setSoundFont`).

Satisfies C2, C3, C4.
