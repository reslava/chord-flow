---
type: design
id: de_01KVQ4HM9ZV5SSRR86P1TGZ9H2
title: Anacrusis rendering — emit PickupMeasure as a true \ac pickup bar
status: done
created: 2026-06-22
updated: 2026-06-22
version: 3
tags: []
parent_id: id_01KVQ3RMZXEPH63QJQD2M3MVE2
requires_load: []
---
# Anacrusis rendering — emit PickupMeasure as a true \ac pickup bar

## Goal

Emit the existing `PickupMeasure` as a **true anacrusis** by prefixing its rendered bar with alphaTex
`\ac`. Pure rendering change — no `PickupMeasure` type change, no behavioral change to anything but the
emitted string for the pickup bar.

## Grounding (all confirmed — nothing open)

- **`\ac` is bar metadata**, emitted at the **start of the bar's content**, before its beats — exactly
  like `\ts`/`\ks`. Confirmed by example:
  ```text
  \ks D \ts 24 16 \ac r.16 6.3 7.3 9.3 7.3 6.3 | r.16 5.4 7.4 9.4 7.4 5.4 6.3.4{d} …
  ```
  `\ac r.16 6.3 7.3 9.3 7.3 6.3` is the incomplete pickup bar.
- Bundled `alphaTab.min.js` supports anacrusis (`isAnacrusis`).
- A pickup is **not** padded to a full bar — its length is the actual beats. So our current short-bar
  emission shape is already correct; we are *only* prepending the `\ac ` token. No fill/pad logic.

## Current emission (what we change)

The pickup is the **first bar** of each track, so all bar-level `\ts`/`\ks` metadata already sits in the
header / track header *before* it — `\ac` slots in immediately ahead of the pickup's beats.

- **Comping** (`AlphaTexRenderer.BuildCompingBars`): `RenderBar(pickupSlots, _ => firstChord, …)` →
  emits e.g. `:4 (1.5 0.4 1.3) |`. Becomes **`\ac :4 (1.5 0.4 1.3) |`**.
- **Lead** (`BuildLeadBars`): `RenderLeadBar(pickupSlots, state, allRests: true)` → emits e.g. `:4 r |`.
  Becomes **`\ac :4 r |`**.

Both pickup emissions are already guarded by `rhythm.Pickup is { } pickup`, so `\ac` is emitted **only**
when a pickup exists. The main/section bars are untouched.

## Decision — how to attach the token

`\ac` belongs at the very start of the bar string, *before* the stateful `:N` and the beats, so the
returned `"<:N> <beats> |"` just needs the constant prefix `"\\ac "`.

**Chosen: prepend in the two pickup call-sites** — `"\\ac " + RenderBar(...)` / `"\\ac " + RenderLeadBar(...)`.

- Keeps `RenderBar`/`RenderLeadBar` as pure per-bar formatters with no "am I a pickup?" knowledge.
- The "this bar is the anacrusis" fact already lives in the two pickup branches — the prefix lives there too.
- Rejected: adding a `bool anacrusis` param to both renderers — more surface for a constant prefix the
  formatter would otherwise never need; the formatter shouldn't grow a flag the rest of the bars ignore.

(`state.CurrentDuration` is unaffected — `\ac` carries no duration; the `:N` after it sets state as today.)

## Tests (update the two that pin the pickup line)

- `Render_Pickup_EmitsLeadingMeasureBeforeBars` —
  `EndsWith(":4 (1.5 0.4 1.3) |\n:1 …")` → `EndsWith("\\ac :4 (1.5 0.4 1.3) |\n:1 …")`.
  Pipe count (= 2 bars) is unchanged — `\ac` is a prefix, not a new bar.
- `Render_WithLeadAndPickup_MirrorsPickupAsRestsOnLeadTrack` —
  `Contains(":4 r |")` → `Contains("\\ac :4 r |")`; pipe count (4) unchanged.
- Add/extend one assertion that the **non-pickup** bars carry **no** `\ac` (guard against over-emission).

## Reference sync (same unit of work)

- `loom/refs/alphatex-syntax-reference.md` — add `\ac`: bar metadata marking an anacrusis (pickup); the
  bar's length follows its actual beats rather than the time signature; emitted at the start of the bar
  before its beats. (Now a *verified* directive — our bundled alphaTab supports it and we have a working
  example.)

## Validation

- Solution builds; all tests green (the two updated pickup tests assert the `\ac` token).
- alphaTex ref documents `\ac`.
- **Visual verify** — run the app on a pickup pattern and confirm alphaTab renders a real pickup
  (correct bar numbering / pickup display), not a generic short bar. A string assertion alone does not
  prove the rendered result.

## Known limitation (accepted — `C6`)

alphaTab numbers the anacrusis bar as **bar 1** (so the first *full* bar displays as bar 2). `\ac` makes
the short leading bar legal and renders it as a true pickup (incomplete, not padded — verified), but it
does **not** suppress the bar number, and alphaTex exposes **no directive** to renumber — only bar-number
*visibility* (show/hide) is controllable, JS-side. Musically a pickup is unnumbered, so this is a real but
**accepted** gap (confirmed against the alphaTex docs — no fix available); revisit only if alphaTab adds
support or a JS display workaround proves viable.

## Out of scope

- `PickupMeasure` type, authoring, parsing — all unchanged.
- **Pickup-into-section** alignment (a pickup leading into repeated/multi-bar sections, interaction with
  section-anchored fills) — owned by the deferred [[multi-bar-idea]], which `depends_on` this thread.

Related: [[anacrusis-idea]], [[multi-bar-idea]], `chordflow-dsl-reference`.