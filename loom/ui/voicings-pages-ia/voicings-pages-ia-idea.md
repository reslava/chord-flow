---
type: idea
id: id_01KWC2S9272C3FF4CJH3J9MRR9
title: Voicings pages — information architecture
status: draft
created: 2026-06-30
version: 1
tags: []
parent_id: null
requires_load: []
---
# Voicings pages — information architecture

> **Design-first thread.** This is a *draft idea* to anchor a design conversation — the decisions below are deliberately left **open**, not settled. No plan until the design is discussed and agreed.

## Problem / motivation

Now that **GuitarVoicingsR** (the Voicings page) renders many realized voicings at once, several chord/voicing-related pages overlap, and a **future Voicings Engine page** is coming — so the app's page/nav **information architecture** needs a deliberate pass rather than piecemeal tweaks. Raised in `voicings-render-component-chat-001.md` after the GuitarVoicingsR dogfood.

Current chord/voicing surfaces:
- **CAGED** — renders the octave shapes (the CAGED octave anchors).
- **CAGED Chords** — derives a **single** voicing for a (family, quality, shape, root) and shows it with the **octave-zone band + anchor finger** (the engine-*inspector* detail).
- **Content → Voicings** — CRUD for authored voicings.
- **Voicings** — the new multi-voicing grid (GuitarVoicingsR), read-only.
- **(future) Voicings Engine** — the inspector/playground that drives the engine (operator/quality/root/params) and renders its output *through GuitarVoicingsR*.

## Open questions (for the design conversation)

1. **Rename "CAGED" → "Octave shapes".** It literally renders the octave shapes; the name is misleading. *Leaning: yes — trivial, low-risk.*
2. **Is "CAGED Chords" now redundant?** The Voicings grid supersedes its *viewing* role, but CAGED Chords uniquely shows the **octave-zone band + anchor finger** — the inspector detail the grid cells deliberately omit. That role is exactly what the **Voicings Engine page** will own. *Leaning: keep CAGED Chords until the Voicings Engine page lands, then retire it — don't drop the inspector view before its replacement exists.*
3. **Fold Content → Voicings (CRUD) into a "Voicings" hub?** The Voicings page is read-only by design. Merging CRUD would make "Voicings" a hub with *view + edit* modes — a real UX change to design deliberately, not a quick merge. *Open.*
4. **Where does the Voicings Engine page sit, and how do these pages relate to it?** It is the anchor: it consumes GuitarVoicingsR as its output surface, so the whole layout should be designed around it. *Open.*

## Scope (tentative — to be set in design)

**Likely in:** the nav/page structure for the chord/voicing surfaces — the rename, the CAGED-Chords retirement *timing*, the Content↔Voicings relationship, and how the future Voicings Engine page fits.

**Likely out:** building the Voicings Engine page itself (its own thread); the FretR theming/polish (the `fretr-theming-polish` thread).

## Relationships

- **Depends on / coordinates with** the future `voicings-engine` page thread (the anchor for retirement decisions).
- **Sibling:** `fretr-theming-polish` (the FretR display pass) — independent.

## Validation

Each page-structure change ships with the guitar-weave dogfood: the affected pages render and navigate correctly in the app.
