---
type: design
id: de_01KV7NET48R277ZA3EWC9ZR6ZY
title: SoundFont library — pick & load playback soundfonts
status: done
created: "2026-06-16T00:00:00.000Z"
updated: 2026-06-16
version: 2
tags: []
parent_id: id_01KV7MTAXG10Y0HQVYABXZ7TVA
requires_load: []
---
# SoundFont library — pick & load playback soundfonts

## Goal

Replace the single hardcoded `soundfont/sonivox.sf2` with a **user-pickable, auto-discovered soundfont library**. The chosen font is a **global app setting** that persists across sessions and applies to every score. No alphaTex / Domain / renderer change — this is entirely a **playback-engine + asset + persistence** concern.

Anchored on the architecture ref (`rf_01KTSAPAT132QTEY5BEPRKS3MB`): Core stays UI/host-agnostic, the bridge stays a narrow JSON-envelope protocol, persistence lives in Core, and `score-render-component.js` remains the single owner of the alphaTab integration.

---

## The four moving parts

1. **Discovery** — what `.sf2` files exist, surfaced to JS.
2. **Persistence** — remembering the global choice.
3. **Live switch** — swapping the active font in a running alphaTab instance.
4. **UI** — the picker in the controls strip.

Plus the **shipping policy** for the `.sf2` assets themselves.

---

## Decision 1 — Discovery seam  ⚠️ needs sign-off

Soundfonts are served as static assets under the WebView2 virtual host (`https://chordflow.local/soundfont/*.sf2`). JS **cannot enumerate** a virtual-host directory, so "auto-discover whatever the user dropped in" requires server-side enumeration. The folder (`wwwroot/soundfont/`) is a **host asset** — Core has no path to it.

**Recommended — `ISoundFontCatalog` in Core, implemented by the host.**
- Core defines `ISoundFontCatalog` (`IReadOnlyList<SoundFontInfo> List()` where `SoundFontInfo = { Id, DisplayName }`, `Id` = the file name) and a thin `SoundFontLibrary` feature slice that the `WebMessageRouter` calls.
- Desktop implements it by scanning `wwwroot/soundfont/*.sf2` (deriving a friendly name from the file name).
- New bridge verb **`listSoundFonts`** → reply **`soundFontsListed`** carrying `{ fonts: [{id, name}], selectedId }`.
- *Why:* keeps the feature in Core (the grain — "Features live in Core, only transport differs per host"), and a future web host supplies its own catalog implementation. Matches the existing `IContentStore` / `IBridge` seam pattern.

**Alternative — host answers the verb directly** (no Core feature, Desktop handles `listSoundFonts` itself). Simpler, fewer types, but puts a feature in the host and breaks the dependency grain; a web host would re-implement from scratch.

---

## Decision 2 — Persistence of the global choice  ⚠️ needs sign-off

The selected font is an **app-wide setting**, not exercise content — so it doesn't belong in any of the four content stores.

**Recommended — a tiny `AppSettings` key/value store in Core.**
- New EF Core table `AppSettings (Key TEXT PK, Value TEXT)` + migration, exposed via `IAppSettings { string? Get(string key); void Set(string key, string value); }`.
- The selected soundfont is one key (`"playback.soundFont"`). Future global prefs (default tempo, last-used view, etc.) reuse the same store — no new table per setting.
- *Why:* persistence lives in Core (per the architecture rule, so a web host reuses it), and it's a reusable home for the global-settings category we'll keep needing.

**Alternative — host-side JSON file** next to the db. Less code now, but fragments persistence out of Core and the web host can't reuse it. Rejected unless you want to keep Core untouched for this slice.

---

## Decision 3 — Live switch (implementation detail, no sign-off)

In `score-render-component.js`, switching font = set `api.settings.player.soundFont = "soundfont/<id>"` then `api.updateSettings()`. Whether `updateSettings()` re-fetches the font is version-dependent (the alphaTab API ref flags soundfont loading as "confirm against installed version"); fallback is `fetch(url) → api.loadSoundFont(buffer)`. Exact call verified at implementation time against the bundled alphaTab. Re-asserted on `scoreLoaded` (like the existing `trackVolumes` pattern), since alphaTab rebuilds player state per load. This is a **player-kind** option (local API, no C# re-render) and slots next to the existing metronome/count-in handling.

## Decision 4 — UI (implementation detail, no sign-off)

A `<select>` in the player controls strip, populated from `soundFontsListed`, with the persisted `selectedId` pre-selected. On change: apply live (Decision 3) **and** send a small `setSoundFont` envelope (JS→C#) so the host persists the new global choice (Decision 2). On startup the component requests `listSoundFonts`; the reply both fills the dropdown and tells it which font to load as default — replacing today's hardcoded `soundFont: "soundfont/sonivox.sf2"` in `buildSettings`.

---

## Shipping policy (confirmed in the idea — hybrid)

- **Ship** the small `sonivox.sf2` default → app plays out of the box; it's also the fallback `selectedId` when no setting is stored.
- **Gitignore** `wwwroot/soundfont/*.sf2` **except** `sonivox.sf2`, so large/license-iffy banks never enter the repo.
- **README** carries a curated download list (name · license · URL · target path `wwwroot/soundfont/`); the catalog auto-discovers whatever is present.

---

## Bridge contract additions (summary)

| Verb (JS→C#) | Reply (C#→JS) | Payload |
|---|---|---|
| `listSoundFonts` | `soundFontsListed` | `{ fonts: [{id, name}], selectedId }` |
| `setSoundFont` | *(none / `status`)* | `{ id }` — persists the global choice |

These extend the narrow envelope protocol; no `renderOptions` change (font is not a render input).

---

## Ref-sync obligation

Implementation touches the **bridge contract** and adds a **persistence area**, so `chordflow-architecture-reference.md` must be updated in the same unit of work (new envelope verbs in §5, the `AppSettings` store + `SoundFontLibrary` feature in §3).

---

## Out of scope

In-app font downloading; per-track/per-instrument fonts; bank/program remapping. (Carried from the idea.)

---

## Open decisions to confirm before planning

1. **Discovery seam** — `ISoundFontCatalog` in Core (recommended) vs. host-only.
2. **Persistence** — Core `AppSettings` key/value store (recommended) vs. host-side JSON.

Sign off on these two and I'll write the implementation plan.
