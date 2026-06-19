// ChordFlow Scales view — the interval-lattice dogfood page.
//
// Type an interval set ("1 b3 4 5 b7") + pick a root, and the host (ScalesHandler → IntervalSetDiagram) returns
// a FretboardDiagram lit across the neck. This view owns only the scale-specific chrome (the interval text box +
// root selector) and the page palette (root red, every other degree black); the shared ChordFlowFretboard owns
// the drawing, the orientation/label/legend/fret-window controls, and the auto-fit window. No music theory here —
// the page just ships the request and renders the Core-computed model (locked to the horizontal neck layout).
"use strict";

window.ChordFlowScales = (function () {
  const Bridge = window.ChordFlowBridge;
  // Root names per pitch class (0 = C .. 11 = B), matching the renderer's spelling. The selector value is the pc.
  const KEY_NAMES = ["C", "Db", "D", "Eb", "E", "F", "F#", "G", "Ab", "A", "Bb", "B"];
  // The page palette: root (interval "1") red, every other degree black via the "*" fallback. The per-dot interval
  // label carries the identity, so color only has to anchor the root.
  const ROOT_PALETTE = { "1": "#e2574c", "*": "#000" };
  const DEBOUNCE_MS = 200;

  const $ = (id) => document.getElementById(id);

  let initialized = false;
  let fretView = null; // lazy ChordFlowFretboard handle (created on the first diagram)
  let rootEl, intervalsEl, errorEl, diagramEl;
  let debounceTimer = null;

  function setError(text) {
    if (errorEl) errorEl.textContent = text || "";
  }

  // Ask the host to build the interval-set diagram for the current input.
  function requestPreview() {
    if (!Bridge.available) {
      setError("Open in the ChordFlow app to render scales.");
      return;
    }
    setError("");
    Bridge.send({
      type: "scalePreview",
      intervals: intervalsEl.value,
      rootPitchClass: parseInt(rootEl.value, 10) || 0,
    });
  }

  function schedulePreview() {
    clearTimeout(debounceTimer);
    debounceTimer = setTimeout(requestPreview, DEBOUNCE_MS);
  }

  // Inbound from the host. Every registered handler sees every message; we own only scaleDiagram / scaleError.
  function onHostMessage(raw) {
    let msg;
    try {
      msg = JSON.parse(raw);
    } catch (e) {
      return;
    }
    if (msg.type === "scaleDiagram") {
      setError("");
      if (!fretView && window.ChordFlowFretboard) {
        fretView = window.ChordFlowFretboard.create(diagramEl, {
          orientation: "horizontal",
          labelMode: "interval",
          controls: { orientation: false }, // locked to the neck layout (Scales is always horizontal)
          palette: ROOT_PALETTE,
        });
      }
      if (fretView) fretView.render(msg.diagram);
    } else if (msg.type === "scaleError") {
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
    rootEl.value = "9"; // default A, so the prefilled minor-pentatonic example reads as A
  }

  function init() {
    rootEl = $("scaleRoot");
    intervalsEl = $("scaleIntervals");
    errorEl = $("scaleError");
    diagramEl = $("scale-diagram");
    fillRoots();
    if (!intervalsEl.value) intervalsEl.value = "1 b3 4 5 b7"; // minor pentatonic, a ready example
    intervalsEl.addEventListener("input", schedulePreview);
    rootEl.addEventListener("change", requestPreview);
    if (Bridge.available) Bridge.onReceive(onHostMessage);
    initialized = true;
  }

  // Called by the view toggle when the Scales tab is shown — lazily inits, then renders the current input.
  function show() {
    if (!initialized) init();
    requestPreview();
  }

  return { show };
})();
