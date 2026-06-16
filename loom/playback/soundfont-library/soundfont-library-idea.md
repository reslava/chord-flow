---
type: idea
id: id_01KV7MTAXG10Y0HQVYABXZ7TVA
title: SoundFont library — pick & load playback soundfonts
status: done
created: "2026-06-16T00:00:00.000Z"
updated: 2026-06-16
version: 3
tags: []
parent_id: null
requires_load: []
---
# SoundFont library — pick & load playback soundfonts

## What

Let the user choose which **SoundFont (.sf2)** drives playback, instead of being locked to the single hardcoded `soundfont/sonivox.sf2`. A small picker in the score controls strip lists the available soundfonts and switches the active one live via the alphaTab player; the choice is a **global setting** (persists across exercises and sessions), not a per-exercise property.

## Why

The bundled sonivox font is a serviceable default but thin — a guitarist practicing wants a usable electric/acoustic guitar tone, and different fonts suit different exercises. The render format (alphaTex) is unaffected; this is purely a **playback-engine / asset-loading** concern, which is why it lives in the new `playback` weave rather than `ui/` (layout) or `domain/` (the pure, I/O-free music kernel).

## Shape (proposed)

- **Catalog** — soundfonts live under `wwwroot/soundfont/`. The picker lists the `.sf2` files actually present (plus a friendly display name), so adding a font is an additive data drop — no code change. Mirrors the "content loads from importable bundles, never hardcoded" principle in the global ctx.
- **Switch live** — selecting a font calls `api.loadSoundFont(...)` (or rebuilds `player.soundFont`) on the running alphaTab instance; the cursor/playback keep working. Sits next to the existing player toggles in `score-render-component.js`.
- **Global persistence** — the chosen font is a single app-wide setting (decided: global, not per-exercise). Stored host-side (the same SQLite/settings store the app already owns) and applied as the default `player.soundFont` on every score load.

## Open question — ship vs. gitignore soundfonts

`.sf2` files range from a few MB (sonivox) to tens/hundreds of MB (full GM banks), and licenses vary. **Recommendation (hybrid):**

1. **Ship one small, license-clean default** (the existing sonivox) so a fresh clone/build plays sound out of the box and the player is never broken.
2. **Gitignore additional/large fonts** (`wwwroot/soundfont/*.sf2` except the default) to keep the repo lean and avoid bundling fonts with restrictive licenses.
3. **Provide a curated download list** (name · license · URL · target path = `wwwroot/soundfont/`) in the repo/README; the picker auto-discovers whatever the user has dropped in.

This keeps the repo small, respects font licensing, and makes the soundfont set an additive data drop — consistent with the content-pack philosophy. The alternative (ship a richer font set) trades repo bloat + licensing review for zero-setup richness; flagged for the design step.

## Out of scope (for now)

- In-app downloading/installing of soundfonts (the list points the user to download manually).
- Per-track or per-instrument soundfont assignment.
- Soundfont editing or bank/program remapping.
