// ChordFlow CAGED Shapes view — the octave-shapes dogfood page.
//
// Pick a CAGED shape (C/A/G/E/D) + a root, and the host (CagedShapesHandler → CagedShapeDiagram) returns a
// FretboardDiagram: the shape's root anchors (1 / 8 / 15 by octave) lit on the neck with the octave zone as a
// shaded band. This view owns only the shape + root selectors and the page palette (a root-family of reds); the
// shared ChordFlowFretboard owns the drawing, locked to the horizontal neck layout. No music theory here — the
// page ships the request and renders the Core-computed model. The visual check for OctaveShape before
// caged-system builds chords on it.
"use strict";

window.ChordFlowCagedShapes = (function () {
  const Bridge = window.ChordFlowBridge;
  // Root names per pitch class (0 = C .. 11 = B), matching the renderer's spelling. The selector value is the pc.
  const KEY_NAMES = ["C", "Db", "D", "Eb", "E", "F", "F#", "G", "Ab", "A", "Bb", "B"];
  const SHAPES = ["C", "A", "G", "E", "D"];
  // Root-family reds: the fundamental "1" brightest, its octaves 8 / 15 progressively dimmer so the octave stack
  // reads at a glance; the shaded band (drawn by the component) marks the zone.
  const PALETTE = { "1": "#e2574c", "8": "#b3433b", "15": "#7d2f2a", "*": "#000" };

  const $ = (id) => document.getElementById(id);

  let initialized = false;
  let fretView = null; // lazy ChordFlowFretboard handle (created on the first diagram)
  let rootEl, shapeEl, errorEl, diagramEl;

  function setError(text) {
    if (errorEl) errorEl.textContent = text || "";
  }

  // Ask the host to build the CAGED-shape diagram for the current selection.
  function requestPreview() {
    if (!Bridge.available) {
      setError("Open in the ChordFlow app to render CAGED shapes.");
      return;
    }
    setError("");
    Bridge.send({
      type: "cagedPreview",
      shape: shapeEl.value,
      rootPitchClass: parseInt(rootEl.value, 10) || 0,
    });
  }

  // Inbound from the host. Every registered handler sees every message; we own only cagedDiagram / cagedError.
  function onHostMessage(raw) {
    let msg;
    try {
      msg = JSON.parse(raw);
    } catch (e) {
      return;
    }
    if (msg.type === "cagedDiagram") {
      setError("");
      if (!fretView && window.ChordFlowFretboard) {
        fretView = window.ChordFlowFretboard.create(diagramEl, {
          orientation: "horizontal", // defaults to the neck layout; the orientation toggle lets the user flip it
          labelMode: "interval",
          palette: PALETTE,
        });
      }
      if (fretView) fretView.render(msg.diagram);
    } else if (msg.type === "cagedError") {
      setError(msg.message);
    }
  }

  function fillRoots() {
    rootEl.innerHTML = "";
    KEY_NAMES.forEach((name, pc) => {
      const o = document.createElement("option");
      o.value = String(pc);
      o.textContent = name;
      rootEl.appendChild(o);
    });
    rootEl.value = "9"; // default A — the familiar E-shape A barre sits at fret 5
  }

  function fillShapes() {
    shapeEl.innerHTML = "";
    SHAPES.forEach((s) => {
      const o = document.createElement("option");
      o.value = s;
      o.textContent = s;
      shapeEl.appendChild(o);
    });
    shapeEl.value = "E"; // default the familiar E shape
  }

  function init() {
    rootEl = $("cagedRoot");
    shapeEl = $("cagedShape");
    errorEl = $("cagedError");
    diagramEl = $("caged-diagram");
    fillShapes();
    fillRoots();
    shapeEl.addEventListener("change", requestPreview);
    rootEl.addEventListener("change", requestPreview);
    if (Bridge.available) Bridge.onReceive(onHostMessage);
    initialized = true;
  }

  // Called by the view toggle when the CAGED tab is shown — lazily inits, then renders the current selection.
  function show() {
    if (!initialized) init();
    requestPreview();
  }

  return { show };
})();
