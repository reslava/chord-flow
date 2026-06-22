---
type: req
id: rq_01KVQCKZRG4NHSC57V4BW78PD1
title: Anacrusis rendering — emit PickupMeasure as a true \ac pickup bar — Requirements
status: locked
created: 2026-06-22
updated: 2026-06-22
version: 2
tags: []
parent_id: id_01KVQ3RMZXEPH63QJQD2M3MVE2
requires_load: []
---
# Anacrusis rendering — emit PickupMeasure as a true \ac pickup bar — Requirements

### ✅ Included

- `IN1` — Emit alphaTex `\ac` on the rendered **pickup bar**, on **both** tracks — the voiced comping pickup (`BuildCompingBars`) and the all-rests lead pickup (`BuildLeadBars`) — and only when `rhythm.Pickup` is present.
- `IN2` — `\ac` is placed as **bar metadata at the start of the pickup bar's content**, before its stateful `:N` and beats: e.g. `\ac :4 (1.5 0.4 1.3) |` (comping) and `\ac :4 r |` (lead).
- `IN3` — Attach the token by **prepending `"\ac "` at the two pickup call-sites**; `RenderBar` / `RenderLeadBar` remain pure per-bar formatters with no pickup awareness.
- `IN4` — Update the two pickup tests (`Render_Pickup_EmitsLeadingMeasureBeforeBars`, `Render_WithLeadAndPickup_MirrorsPickupAsRestsOnLeadTrack`) to expect the `\ac` token, and add a guard asserting **non-pickup bars carry no `\ac`**.
- `IN5` — Document `\ac` in `loom/refs/alphatex-syntax-reference.md` (bar metadata; length follows actual beats, not the time signature; emitted before the bar's beats) in the **same unit of work**.
- `IN6` — **Visually verify** in the running app that alphaTab renders a true pickup — a short, **incomplete leading bar** that is *not* padded to a full bar. **(Met.)** The "correct bar **numbering**" part of this is constrained by `C6` — see the known limitation.

### ❌ Excluded

- `EX1` — Any change to the `PickupMeasure` **type, authoring, or parsing** — the domain side is untouched.
- `EX2` — **Pickup-into-section** semantics and any multi-bar alignment / section-anchored fills — owned by the deferred `multi-bar` thread.
- `EX3` — Authoring our own compensating short **final bar** — that is alphaTab's anacrusis behavior, not something we emit.
- `EX4` — Any behavioral change to **non-pickup** bars; their output stays byte-identical.

### ⛓ Constraints

- `C1` — `AlphaTexRenderer` stays the **only** alphaTex-aware code; the `\ac` token is emitted there, nowhere else.
- `C2` — Pure rendering change: the pickup remains **one bar** (`\ac` is a prefix, not a new bar), so bar/pipe counts are unchanged.
- `C3` — `\ac` is emitted **only** when a pickup exists; never on a regular section bar.
- `C4` — Solution builds and **all tests stay green**.
- `C5` — A passing string assertion is **not sufficient** acceptance — true pickup rendering must be confirmed visually (`IN6`).
- `C6` — **Known limitation (accepted):** alphaTab **numbers the anacrusis bar as bar 1** (the first full bar then displays as bar 2). `\ac` makes the short bar legal and renders it as a pickup, but it does **not** suppress the bar number, and alphaTex exposes **no directive** to renumber — only bar-number *visibility* (show/hide) is controllable, JS-side. Musically a pickup should be unnumbered; this gap is **accepted for now** (verified against the alphaTex docs — no fix available) and revisited only if alphaTab adds support or a JS display workaround proves viable.
