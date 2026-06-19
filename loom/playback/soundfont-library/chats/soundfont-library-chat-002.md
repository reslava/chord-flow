---
type: chat
id: ch_01KVFJVHVPJYT6HRPP0VKAMK81
title: sf3 libraries
status: done
created: 2026-06-19
updated: 2026-06-19
version: 2
tags: []
parent_id: null
requires_load: []
last_ai_block: 3
---
# sf3 libraries

## Rafa:

I think alphaTab is compatible with .sf3 libraries.

Please update src/ChordFlow.Desktop/WebHost/WwwrootSoundFontCatalog.cs
line 30 to allow .sf2 and .sf3 libraries.


## AI:

Done — `WwwrootSoundFontCatalog` now discovers both `.sf2` and `.sf3`.

Your instinct is right, and the project already half-anticipated this: the `.gitignore` (lines 59–61) ignores `wwwroot/soundfont/*.sf2` **and** `*.sf3`, with a comment that "any extra .sf2/.sf3 a user drops in is auto-discovered." The catalog enumeration was the one spot still hardcoded to `.sf2` only — so this just brings the discovery code in line with the policy that was already written.

**alphaTab compatibility** — confirmed. alphaTab's synth loads SoundFont2 (`.sf2`) and its Ogg-Vorbis-compressed variant (`.sf3`) interchangeably, and our JS loader is already extension-agnostic: `score-render-component.js` builds the URL from the file id (`fontUrl(id)`) and calls `api.loadSoundFontFromUrl(...)` — it never inspects the extension. So nothing on the JS side needs to change; the C# catalog was the only gate.

**What changed** (`src/ChordFlow.Desktop/WebHost/WwwrootSoundFontCatalog.cs`):
- `Directory.EnumerateFiles(_folder, "*.sf2")` → enumerate all files and filter against a `{ ".sf2", ".sf3" }` extension set (case-insensitive). `EnumerateFiles` only takes a single glob, so a set filter is the clean way to match both.
- Updated the class doc-comment (`*.sf2` → `*.sf2` / `*.sf3`).
- `FriendlyName` already strips the extension, so `.sf3` names render the same way — no change needed there.

Builds clean. Nothing in the design/req docs needed touching — `.sf2` there is illustrative, not a constraint (the seam is format-agnostic). Drop an `.sf3` into `wwwroot/soundfont/` and it'll show up in the picker.

## Rafa:

I ran the app and tested, all OK
Update README to mention app allow .sf2, .sf3 
Also add thin link to download soundFonts libraries
https://musescore.org/en/handbook/3/soundfonts-and-sfz-files#list

commit push

## AI:

All done — README updated, committed, and pushed to `main` (`00ebc03`).

**README (`### Soundfonts`):**
- Opening line now reads "**SoundFont (`.sf2` or `.sf3`)** — alphaTab loads SoundFont2 and its Ogg-compressed `.sf3` variant interchangeably."
- Step 1 ("Drop any `.sf2` / `.sf3` file…") and the closing "extract the `.sf2` / `.sf3`" line updated to cover both.
- Added a thin link under the font table: "More to download: the [MuseScore soundfont list](https://musescore.org/en/handbook/3/soundfonts-and-sfz-files#list)" (your `#list` anchor). The existing FluidR3 GM row already points at the same handbook page, so this just gives the broader list a direct entry point.

**Commit** `00ebc03` — `feat(playback): discover .sf3 soundfonts alongside .sf2` — bundles the catalog change + README in one unit. Pushed to `origin/main`.