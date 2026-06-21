<img src="../images/icon.png" width="96" align="right" alt="ChordFlow app icon">

# ChordFlow — User Guide

Welcome! This guide is for guitarists who downloaded ChordFlow and want to start
practicing. It covers installing the app, building and playing an exercise, adding
your own material, and choosing a sound. No setup or account needed — ChordFlow runs
entirely on your PC.

> Looking for the developer/architecture docs instead? See the
> [Architecture overview](../loom/refs/chordflow-architecture-reference.md).

---

## 1. What is ChordFlow?

ChordFlow is a **rhythm & progression trainer**. You pick a chord progression, a
strum pattern, a key, a tempo and a difficulty, and ChordFlow **generates a practice
exercise** — rendered as guitar tablature with **synchronized playback**: a cursor
moves along the staff and highlights the beat as it plays, so you can strum along in
time.

It's a *generator*, not a tab viewer and not a recording studio — there's no song
library to browse and no multitrack mixing. You assemble the ingredients, and
ChordFlow writes the exercise for you, in any key.

---

## 2. Install & first run

1. **[Download the latest Windows release →](https://github.com/reslava/chord-flow/releases/latest)**
   Grab the `ChordFlow-vX.Y.Z-win-x64.zip` asset.
2. **Unzip it anywhere** (your Desktop, Documents — wherever you like).
3. **Run `ChordFlow.exe`.** It's a self-contained build, so there's no .NET to install.

Requirements: **Windows 10 or 11** with the **WebView2 Runtime** — already present on
Windows 11 and anywhere current Microsoft Edge is installed.

> **First-run warning is normal.** The build is unsigned, so Windows **SmartScreen**
> shows an *"unknown publisher"* prompt the first time you run it. Click
> **More info → Run anyway**. This is expected for a small independent app and clears
> as the download earns reputation.

---

## 3. Build & play an exercise

This all happens on the **Practice** tab (the default view). The toolbar across the
top is the **builder** — choose your ingredients left to right:

| Picker | What it sets |
|--------|--------------|
| **Harmony** | The chord material — a song or a progression (e.g. *Blues Song Demo*). |
| **Comping** | The rhythm/strum pattern the chords are played with (e.g. *Beats 1 & 3*). |
| **Lead** | An optional lead line over the chords — leave it on *(none)* for rhythm-only practice. |
| **Key** | Transpose the whole exercise to any of the 12 keys. |
| **Difficulty** | How involved the chord voicings are. |
| **Feel** | Straight, swing, and other rhythmic feels. |

Then:

1. Click **Generate**. ChordFlow renders the exercise as notation + TAB, with chord
   diagrams above the staff.
2. Click **Play** to hear it — a highlight follows the current bar and a line marks
   the beat you're on. **Stop** halts playback.
3. Adjust **Tempo** (BPM) to slow a tough passage down. The transport also has a
   **Metronome**, a **Count-in**, separate **Rhythm vol** / **Lead vol** sliders, and
   toggles for **Chord names** and the **Diagrams**.
4. Click **Save** to keep the exercise — it appears in the **Saved exercises** list on
   the right; click any saved item to reload it.
5. Click **Mark practiced** to record that you ran through it.

<figure>
  <a href="../images/screenshots/01-practice.png"><img src="../images/screenshots/01-practice.png" width="480" alt="The Practice tab: the builder pickers, the transport with the playback cursor, the rendered score, and the saved-exercise list"></a>
  <figcaption><em>The Practice tab — build, generate, play, and save an exercise.</em></figcaption>
</figure>

---

## 4. Make your own content

Switch to the **Content** tab to create and edit your own material — progressions,
songs, rhythms, and voicings. You write them in short **text DSLs** that use **scale
degrees instead of letter names**, so anything you write works in *every* key (you
pick the key back on the Practice tab).

For example, a I–IV–V progression is just:

```
1 4 5
```

and a 12-bar blues (all dominant 7th chords) is:

```
17 17 17 17 47 47 17 17 57 47 17 57
```

That's the whole idea — degrees `1`–`7`, a space starts a new bar, and a suffix like
`7` or `m` sets the chord quality. The full grammar (chord splits within a bar,
qualities, durations, and the Song DSL for arranging progressions into a piece) is in
the **[DSL guide](../loom/refs/chordflow-dsl-reference.md)**.

<figure>
  <a href="../images/screenshots/02-content-progressions.png"><img src="../images/screenshots/02-content-progressions.png" width="480" alt="Editing a progression in the Content tab"></a>
  <figcaption><em>The Content tab — editing progressions, songs, and rhythms in the text DSLs.</em></figcaption>
</figure>

<p>
  <a href="../images/screenshots/03-content-songs.png"><img src="../images/screenshots/03-content-songs.png" width="480" alt="Editing a song arrangement in the Content tab"></a>
  <a href="../images/screenshots/04-content-rhythms.png"><img src="../images/screenshots/04-content-rhythms.png" width="480" alt="Editing a rhythm pattern in the Content tab"></a>
</p>

---

## 5. Soundfonts

Playback uses a **soundfont** — the file that holds the instrument sounds. ChordFlow
ships with the **Sonivox** General-MIDI soundfont as the default, so it makes sound
out of the box.

To use a different one:

1. Drop any **`.sf2`** or **`.sf3`** soundfont into the **`wwwroot/soundfont/`** folder
   that sits next to `ChordFlow.exe`.
2. Back in the app, pick it from the **Sound** dropdown in the player controls.
   ChordFlow finds new fonts automatically — no restart, no settings file — and
   remembers your choice across sessions.

Need a soundfont? The project README lists a few free, redistributable ones (FluidR3
GM, GeneralUser GS, and the MuseScore collection) — see
**[Soundfonts in the README](../README.md#soundfonts)**.

---

## 6. Known limits

- **Windows only**, for now.
- **No "did I play it right?" detection.** ChordFlow doesn't listen through your
  microphone or grade your playing — it's a play-along trainer that shows you what to
  play and keeps time.
- **Local and offline.** There's no account and nothing is uploaded; your saved
  exercises live on your PC.
