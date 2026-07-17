---
type: plan
id: pl_01KXQN0WSQWTV3QXSFQZARCJRF
title: "Content: description: catalog field + major-frame progression pack"
status: done
created: 2026-07-17
updated: 2026-07-17
version: 1
design_version: 3
req_version: 2
tags: []
parent_id: de_01KXQFJYNF0D10R9B5VR3JP930
requires_load: []
target_version: 0.1.0
steps:
  - id: added-a-key-to-the-shared
    order: 1
    status: done
    description: "Added a `description:` key to the shared catalog header (`CatalogHeader` parse/serialize + `CatalogMetadata.Description`, chat decision 1A) — a free-text, human-readable blurb that round-trips through the DSL and rides in the stored content, no dedicated column; registered as a recognized header key so it never leaks into the pure Domain parser."
    files_touched: []
    blocked_by: []
    satisfies: []
  - id: added-10-major-frame-progressions-to
    order: 2
    status: done
    description: Added 10 major-frame progressions to the default pack (ii-V-I, I-vi-ii-V turnaround, secondary-dominant turnaround, circle of secondary dominants, tritone-sub ii-V-I, Tadd Dameron turnaround, borrowed iv, Mixolydian bVII, Aeolian cadence, chromatic passing diminished) with descriptions + harmonic-concept/difficulty tag vocabulary (chat decision (a)) — real content and golden dogfood for the harmonic analyzer; the minor-tonic ones are held for the first-class-minor-keys thread.
    files_touched: []
    blocked_by: []
    satisfies: []
  - id: added-a-catalogheader-description-round-trip
    order: 3
    status: done
    description: Added a CatalogHeader description round-trip test and updated the DSL reference; full Core suite 944 passing (ProgressionSeedTests drives every new progression DSL->model->render).
    files_touched: []
    blocked_by: []
    satisfies: []
---
# Content: description: catalog field + major-frame progression pack

## Goal

Quick-ship record of 3 completed changes.

---

## Steps

| Done | # | Step | Files touched | Blocked by | Satisfies |
|---|---|---|---|---|---|
| ✅ | 1 | Added a `description:` key to the shared catalog header (`CatalogHeader` parse/serialize + `CatalogMetadata.Description`, chat decision 1A) — a free-text, human-readable blurb that round-trips through the DSL and rides in the stored content, no dedicated column; registered as a recognized header key so it never leaks into the pure Domain parser. | — | — | — |
| ✅ | 2 | Added 10 major-frame progressions to the default pack (ii-V-I, I-vi-ii-V turnaround, secondary-dominant turnaround, circle of secondary dominants, tritone-sub ii-V-I, Tadd Dameron turnaround, borrowed iv, Mixolydian bVII, Aeolian cadence, chromatic passing diminished) with descriptions + harmonic-concept/difficulty tag vocabulary (chat decision (a)) — real content and golden dogfood for the harmonic analyzer; the minor-tonic ones are held for the first-class-minor-keys thread. | — | — | — |
| ✅ | 3 | Added a CatalogHeader description round-trip test and updated the DSL reference; full Core suite 944 passing (ProgressionSeedTests drives every new progression DSL->model->render). | — | — | — |
---

### Legend

| Symbol | Meaning |
|--------|---------|
| ✅ | Done |
| 🔄 | In Progress |
| 🔳 | Pending |
| ❌ | Cancelled |
