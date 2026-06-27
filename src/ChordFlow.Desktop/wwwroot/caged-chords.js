// ChordFlow CAGED Chords view — the derivation-engine dogfood page.
//
// Pick a CAGED shape (C/A/G/E/D), a quality, and a root, and the host (CagedChordHandler → ChordShapeDiagram)
// *derives* the grip and returns a FretboardDiagram: frets coloured by chord-tone function, the octave zone as a
// shaded band, the anchor finger in the title. A generator — every quality × shape is offered, including combos the
// pack never authored (m7b5·C, dim7·G…); an unvoiceable one comes back as cagedChordError. This view owns only the
// three selectors; the shared ChordFlowFretboard owns the drawing (locked to the horizontal neck layout). No music
// theory here — the page ships the request and renders the Core-computed model.
"use strict";

window.ChordFlowCagedChords = (function () {
  const Bridge = window.ChordFlowBridge;
  // Root names per pitch class (0 = C .. 11 = B), matching the renderer's spelling. The selector value is the pc.
  const KEY_NAMES = ["C", "Db", "D", "Eb", "E", "F", "F#", "G", "Ab", "A", "Bb", "B"];
  const SHAPES = ["C", "A", "G", "E", "D"];
  // value = the Quality enum name the host parses; label = the familiar symbol shown to the player.
  const QUALITIES = [
    { value: "Major", label: "maj" },
    { value: "Minor", label: "min" },
    { value: "Major7", label: "maj7" },
    { value: "Dominant7", label: "dom7" },
    { value: "Minor7", label: "m7" },
    { value: "HalfDiminished7", label: "m7b5" },
    { value: "Diminished7", label: "dim7" },
    { value: "Augmented", label: "aug" },
    { value: "Major6", label: "6" },
    { value: "Minor6", label: "m6" },
  ];

  const $ = (id) => document.getElementById(id);

  let initialized = false;
  let fretView = null; // lazy ChordFlowFretboard handle (created on the first diagram)
  let shapeEl, qualityEl, rootEl, errorEl, diagramEl;

  function setError(text) {
    if (errorEl) errorEl.textContent = text || "";
  }

  // Ask the host to derive + diagram the current (shape, quality, root) selection.
  function requestPreview() {
    if (!Bridge.available) {
      setError("Open in the ChordFlow app to render CAGED chords.");
      return;
    }
    setError("");
    Bridge.send({
      type: "cagedChordPreview",
      shape: shapeEl.value,
      quality: qualityEl.value,
      rootPitchClass: parseInt(rootEl.value, 10) || 0,
    });
  }

  // Inbound from the host. Every registered handler sees every message; we own only cagedChordDiagram / cagedChordError.
  function onHostMessage(raw) {
    let msg;
    try {
      msg = JSON.parse(raw);
    } catch (e) {
      return;
    }
    if (msg.type === "cagedChordDiagram") {
      setError("");
      if (!fretView && window.ChordFlowFretboard) {
        fretView = window.ChordFlowFretboard.create(diagramEl, {
          orientation: "horizontal",
          labelMode: "interval",
          controls: { orientation: false }, // CAGED chords are always the neck layout
        });
      }
      if (fretView) fretView.render(msg.diagram);
    } else if (msg.type === "cagedChordError") {
      setError(msg.message);
    }
  }

  function fillSelect(el, items) {
    el.innerHTML = "";
    items.forEach((it) => {
      const o = document.createElement("option");
      o.value = it.value;
      o.textContent = it.label;
      el.appendChild(o);
    });
  }

  function init() {
    shapeEl = $("cagedChordShape");
    qualityEl = $("cagedChordQuality");
    rootEl = $("cagedChordRoot");
    errorEl = $("cagedChordError");
    diagramEl = $("caged-chord-diagram");

    fillSelect(shapeEl, SHAPES.map((s) => ({ value: s, label: s })));
    fillSelect(qualityEl, QUALITIES);
    fillSelect(rootEl, KEY_NAMES.map((name, pc) => ({ value: String(pc), label: name })));

    shapeEl.value = "E"; // a familiar E-shape barre
    qualityEl.value = "Major7";
    rootEl.value = "9"; // A — the E-shape A-major barre sits at fret 5

    shapeEl.addEventListener("change", requestPreview);
    qualityEl.addEventListener("change", requestPreview);
    rootEl.addEventListener("change", requestPreview);
    if (Bridge.available) Bridge.onReceive(onHostMessage);
    initialized = true;
  }

  // Called by the view toggle when the CAGED Chords tab is shown — lazily inits, then renders the current selection.
  function show() {
    if (!initialized) init();
    requestPreview();
  }

  return { show };
})();
