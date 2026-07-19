// ChordFlow Drums view — the basic-drums dogfood page.
//
// Author a groove in the hit-grid DSL; the host (DrumGroovePreviewHandler) parses it once and returns two
// projections — the alphaTex percussion track (rendered + played by the shared ScoreR) and the grid model
// (drawn by DrumsR). While it plays, the engine's time-linear "position" clock drives the DrumsR marker, so
// the grid animates in time with the audio. This view owns only the drums-specific chrome (the DSL editor +
// tempo); ScoreR owns notation/transport and DrumsR owns the grid. No music theory here — the host resolves
// everything (IN5/C1).
"use strict";

window.ChordFlowDrumsView = (function () {
  const Bridge = window.ChordFlowBridge;
  const DEBOUNCE_MS = 300;
  const $ = (id) => document.getElementById(id);
  // A ready example: a basic rock beat (kick/snare backbeat/straight-8th hi-hat).
  const EXAMPLE = "HH :2 x x x x x x x x\nSD :2 . . x . . . x .\nBD :2 x . . . x . . .";

  let initialized = false;
  let scoreView = null; // ChordFlowScore handle (notation + transport)
  let gridView = null;  // ChordFlowDrums (DrumsR) handle
  let dslEl, tempoEl, errorEl, scoreEl, gridEl;
  let debounceTimer = null;

  function setError(text) { if (errorEl) errorEl.textContent = text || ""; }
  function tempo() { return parseInt(tempoEl.value, 10) || 100; }

  function requestPreview() {
    if (!Bridge.available) { setError("Open in the ChordFlow app to preview drums."); return; }
    setError("");
    Bridge.send({ type: "drumPreview", dsl: dslEl.value, tempo: tempo() });
  }

  function schedulePreview() {
    clearTimeout(debounceTimer);
    debounceTimer = setTimeout(requestPreview, DEBOUNCE_MS);
  }

  // Lazily create the shared ScoreR (notation + play/stop/tempo) and wire the grid marker to the engine's
  // time-linear "position" clock (bar/quarterBeat are 1-based → DrumsR's 0-based cell). Stopping clears it.
  function ensureScore() {
    if (scoreView || !window.ChordFlowScore) return;
    scoreView = window.ChordFlowScore.create(scoreEl, {
      player: true,
      controls: "full",
      transport: true,
      onStateChange: (playing) => { if (!playing && gridView) gridView.clearHighlight(); },
      onFinished: () => { if (gridView) gridView.clearHighlight(); },
    });
    const engine = scoreView.getEngine && scoreView.getEngine();
    if (engine) {
      engine.on("position", (bar, quarterBeat) => {
        if (gridView) gridView.highlightCell(bar - 1, quarterBeat - 1);
      });
    }
  }

  // Inbound from the host. Every registered handler sees every message; we own only drumPreview / drumPreviewError.
  function onHostMessage(raw) {
    let msg;
    try { msg = JSON.parse(raw); } catch (e) { return; }

    if (msg.type === "drumPreview") {
      setError("");
      if (!gridView && window.ChordFlowDrums) gridView = window.ChordFlowDrums.create(gridEl, { theme: "light" });
      if (gridView) gridView.render(msg.diagram);
      ensureScore();
      if (scoreView) scoreView.load(msg.tex, { tempo: tempo() });
    } else if (msg.type === "drumPreviewError") {
      setError(msg.message);
    }
  }

  function init() {
    dslEl = $("drumDsl");
    tempoEl = $("drumTempo");
    errorEl = $("drumError");
    scoreEl = $("drums-score");
    gridEl = $("drums-grid");
    if (!dslEl.value) dslEl.value = EXAMPLE;
    dslEl.addEventListener("input", schedulePreview);
    tempoEl.addEventListener("change", requestPreview);
    if (Bridge.available) Bridge.onReceive(onHostMessage);
    initialized = true;
  }

  // Called by the view toggle when the Drums tab is shown — lazily inits, then previews the current groove.
  function show() {
    if (!initialized) init();
    requestPreview();
  }

  return { show };
})();
