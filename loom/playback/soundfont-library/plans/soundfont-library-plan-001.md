---
type: plan
id: pl_01KV7NP49XN0CE351Q7YRAVJM3
title: SoundFont library — implementation
status: done
created: 2026-06-16
updated: 2026-06-16
version: 1
design_version: 1
tags: []
parent_id: de_01KV7NET48R277ZA3EWC9ZR6ZY
requires_load: []
target_version: 0.1.0
actual_release: 0.5.0
steps:
  - id: add-a-core-appsettings-key-value
    order: 1
    status: done
    description: "Add a Core AppSettings key/value store: AppSetting entity, IAppSettings (Get/Set), EF Core config + migration"
    files_touched: [src/ChordFlow.Core/Persistence/AppSetting.cs, src/ChordFlow.Core/Persistence/IAppSettings.cs, src/ChordFlow.Core/Persistence/AppSettingsStore.cs, src/ChordFlow.Core/Persistence/ChordFlowDbContext.cs, src/ChordFlow.Core/Persistence/Migrations/]
    blocked_by: []
    satisfies: [IN3, C3]
  - id: add-isoundfontcatalog-soundfontinfo-in-core-and
    order: 2
    status: done
    description: Add ISoundFontCatalog + SoundFontInfo in Core and a SoundFontLibrary feature slice that lists fonts and reads/writes the selected id
    files_touched: [src/ChordFlow.Core/Bridge/ISoundFontCatalog.cs, src/ChordFlow.Core/Features/SoundFontLibrary.cs]
    blocked_by: [1]
    satisfies: [IN1, IN2, IN3, C2]
  - id: add-bridge-envelopes-webmessagerouter-wiring-for
    order: 3
    status: done
    description: Add bridge envelopes + WebMessageRouter wiring for listSoundFonts/setSoundFont (inbound) and soundFontsListed (outbound)
    files_touched: [src/ChordFlow.Core/Bridge/Envelopes.cs, src/ChordFlow.Core/Bridge/WebMessageRouter.cs]
    blocked_by: [2]
    satisfies: [IN1, IN2, C4]
  - id: desktop-implement-isoundfontcatalog-scan-wwwroot-soundfont
    order: 4
    status: done
    description: "Desktop: implement ISoundFontCatalog (scan wwwroot/soundfont/*.sf2), wire DI, and handle the new verbs (reply soundFontsListed, persist on setSoundFont)"
    files_touched: [src/ChordFlow.Desktop/WebHost/WwwrootSoundFontCatalog.cs, src/ChordFlow.Desktop/Program.cs]
    blocked_by: [3]
    satisfies: [IN2, IN3, C2]
  - id: wwwroot-route-soundfontslisted-in-bridge-js
    order: 5
    status: done
    description: "wwwroot: route soundFontsListed in bridge.js and add the soundfont picker + live switch to score-render-component.js, replacing the hardcoded soundFont path"
    files_touched: [src/ChordFlow.Desktop/wwwroot/bridge.js, src/ChordFlow.Desktop/wwwroot/score-render-component.js]
    blocked_by: [3]
    satisfies: [IN1, IN4, C5]
  - id: shipping-policy-gitignore-wwwroot-soundfont-sf2
    order: 6
    status: done
    description: "Shipping policy: gitignore wwwroot/soundfont/*.sf2 except sonivox.sf2; add curated download list to README"
    files_touched: [.gitignore, README.md]
    blocked_by: []
    satisfies: [IN5]
  - id: ref-sync-update-chordflow-architecture-reference
    order: 7
    status: done
    description: "Ref-sync: update chordflow-architecture-reference with the new bridge verbs (§5) and the AppSettings store + SoundFontLibrary feature (§3)"
    files_touched: [loom/refs/chordflow-architecture-reference.md]
    blocked_by: [1, 2, 3, 4, 5]
    satisfies: [C2, C3, C4]
---
# SoundFont library — implementation

## Goal

Implement the user-pickable, auto-discovered soundfont library per the design (Core option for both decisions). Add a Core AppSettings key/value store for the global font choice, an ISoundFontCatalog feature in Core implemented by the Desktop host (scanning wwwroot/soundfont), two new bridge verbs (listSoundFonts/setSoundFont) with the soundFontsListed reply, and a picker in the shared score-render-component that lists fonts, switches live, and persists the global choice — replacing the hardcoded sonivox path. Ship the small sonivox default, gitignore other .sf2 banks, and document a curated download list. No Domain/renderer/alphaTex change.

---

## Steps

| Done | # | Step | Files touched | Blocked by | Satisfies |
|---|---|---|---|---|---|
| ✅ | 1 | Add a Core AppSettings key/value store: AppSetting entity, IAppSettings (Get/Set), EF Core config + migration | src/ChordFlow.Core/Persistence/AppSetting.cs, src/ChordFlow.Core/Persistence/IAppSettings.cs, src/ChordFlow.Core/Persistence/AppSettingsStore.cs, src/ChordFlow.Core/Persistence/ChordFlowDbContext.cs, src/ChordFlow.Core/Persistence/Migrations/ | — | IN3, C3 |
| ✅ | 2 | Add ISoundFontCatalog + SoundFontInfo in Core and a SoundFontLibrary feature slice that lists fonts and reads/writes the selected id | src/ChordFlow.Core/Bridge/ISoundFontCatalog.cs, src/ChordFlow.Core/Features/SoundFontLibrary.cs | 1 | IN1, IN2, IN3, C2 |
| ✅ | 3 | Add bridge envelopes + WebMessageRouter wiring for listSoundFonts/setSoundFont (inbound) and soundFontsListed (outbound) | src/ChordFlow.Core/Bridge/Envelopes.cs, src/ChordFlow.Core/Bridge/WebMessageRouter.cs | 2 | IN1, IN2, C4 |
| ✅ | 4 | Desktop: implement ISoundFontCatalog (scan wwwroot/soundfont/*.sf2), wire DI, and handle the new verbs (reply soundFontsListed, persist on setSoundFont) | src/ChordFlow.Desktop/WebHost/WwwrootSoundFontCatalog.cs, src/ChordFlow.Desktop/Program.cs | 3 | IN2, IN3, C2 |
| ✅ | 5 | wwwroot: route soundFontsListed in bridge.js and add the soundfont picker + live switch to score-render-component.js, replacing the hardcoded soundFont path | src/ChordFlow.Desktop/wwwroot/bridge.js, src/ChordFlow.Desktop/wwwroot/score-render-component.js | 3 | IN1, IN4, C5 |
| ✅ | 6 | Shipping policy: gitignore wwwroot/soundfont/*.sf2 except sonivox.sf2; add curated download list to README | .gitignore, README.md | — | IN5 |
| ✅ | 7 | Ref-sync: update chordflow-architecture-reference with the new bridge verbs (§5) and the AppSettings store + SoundFontLibrary feature (§3) | loom/refs/chordflow-architecture-reference.md | 1, 2, 3, 4, 5 | C2, C3, C4 |
---

### Legend

| Symbol | Meaning |
|--------|---------|
| ✅ | Done |
| 🔄 | In Progress |
| 🔳 | Pending |
| ❌ | Cancelled |

<!-- step:add-a-core-appsettings-key-value -->
### Step 1 — Add a Core AppSettings key/value store: AppSetting entity, IAppSettings (Get/Set), EF Core config + migration

New table AppSettings(Key TEXT PK, Value TEXT). IAppSettings { string? Get(string key); void Set(string key, string value); }. Migration applies on startup like the others. Reusable for future global prefs; the font choice is one key "playback.soundFont".

<!-- step:add-isoundfontcatalog-soundfontinfo-in-core-and -->
### Step 2 — Add ISoundFontCatalog + SoundFontInfo in Core and a SoundFontLibrary feature slice that lists fonts and reads/writes the selected id

ISoundFontCatalog { IReadOnlyList<SoundFontInfo> List(); } with SoundFontInfo { string Id; string DisplayName; } (Id = file name). SoundFontLibrary composes the catalog + IAppSettings: ListWithSelection() returns the fonts + the persisted selectedId (falling back to sonivox.sf2 when unset), and SetSelected(id) persists via IAppSettings.

<!-- step:add-bridge-envelopes-webmessagerouter-wiring-for -->
### Step 3 — Add bridge envelopes + WebMessageRouter wiring for listSoundFonts/setSoundFont (inbound) and soundFontsListed (outbound)

SoundFontsListedEnvelope { fonts: [{id, name}], selectedId }. Router parses listSoundFonts and setSoundFont{id} and raises typed events the host handles. Follows the existing inbound-verb pattern; no renderOptions change (font is not a render input).

<!-- step:desktop-implement-isoundfontcatalog-scan-wwwroot-soundfont -->
### Step 4 — Desktop: implement ISoundFontCatalog (scan wwwroot/soundfont/*.sf2), wire DI, and handle the new verbs (reply soundFontsListed, persist on setSoundFont)

Host implementation scans the served wwwroot/soundfont folder and derives friendly names from file names. Register it + SoundFontLibrary in DI. On listSoundFonts → send SoundFontsListedEnvelope via IBridge; on setSoundFont → SoundFontLibrary.SetSelected(id).

<!-- step:wwwroot-route-soundfontslisted-in-bridge-js -->
### Step 5 — wwwroot: route soundFontsListed in bridge.js and add the soundfont picker + live switch to score-render-component.js, replacing the hardcoded soundFont path

On init the component sends listSoundFonts; the soundFontsListed reply fills a <select> in the player controls strip and sets the default font (replacing soundFont: 'soundfont/sonivox.sf2' in buildSettings). On change: apply live (set api.settings.player.soundFont + updateSettings(); fallback fetch→api.loadSoundFont — verify exact call against the bundled alphaTab) and send setSoundFont{id} to persist. Re-assert the active font on scoreLoaded, like trackVolumes. Player-kind control: local, no C# re-render.

<!-- step:shipping-policy-gitignore-wwwroot-soundfont-sf2 -->
### Step 6 — Shipping policy: gitignore wwwroot/soundfont/*.sf2 except sonivox.sf2; add curated download list to README

Keep sonivox.sf2 tracked (ships as default + fallback). Ignore other .sf2 banks. README: curated table (name · license · download URL · target path wwwroot/soundfont/); the catalog auto-discovers whatever is present.

<!-- step:ref-sync-update-chordflow-architecture-reference -->
### Step 7 — Ref-sync: update chordflow-architecture-reference with the new bridge verbs (§5) and the AppSettings store + SoundFontLibrary feature (§3)

Mandatory same-unit ref update: document listSoundFonts/setSoundFont/soundFontsListed in the envelope contract and the new persistence/feature areas.
