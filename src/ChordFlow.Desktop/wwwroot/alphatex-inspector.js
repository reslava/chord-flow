// ChordFlow Debug view — the alphaTex inspector.
//
// A diagnostic surface on the engine↔alphaTab seam: show the alphaTex the host last pushed, let it be edited
// freely, and render/play the edited text through its own shared ChordFlowScore (full player). When a score
// looks wrong this isolates the cause — read the emitted tex (wrong ⇒ AlphaTexRenderer), or hand-edit until
// it's right (then the bug is in our emit, not alphaTab). Also a live scratchpad for the alphaTex syntax ref.
//
// Front-end only: the loadScore envelope already carries `tex`, and ChordFlowScore.load(tex) already renders
// AND plays a raw string. We cache every loadScore.tex off the shared bridge fan-out (eagerly, so "Load
// current" works even before the Debug view is first opened) and feed the textarea straight into a component.
"use strict";

window.ChordFlowInspector = (function () {
  const Bridge = window.ChordFlowBridge;

  // A minimal valid score so Render works before any host score has arrived (dev / first run).
  const SAMPLE_TEX = ['\\title "Scratch"', ".", ":4 3.3 3.3 3.3 3.3 |"].join("\n");

  let lastTex = null;       // the most recent alphaTex the host pushed (loadScore.tex)
  let initialized = false;
  let view = null;          // this view's own ChordFlowScore handle (full player)
  let textEl = null;        // the alphaTex <textarea>

  // Eagerly cache the host's pushed alphaTex (fan-out sees every message). Registered at load — before the
  // Debug view is opened — so the first "Load current" already has the boot/generated score.
  if (Bridge && typeof Bridge.onReceive === "function") {
    Bridge.onReceive((raw) => {
      let msg;
      try { msg = JSON.parse(raw); } catch { return; }
      if (msg && msg.type === "loadScore" && typeof msg.tex === "string") lastTex = msg.tex;
    });
  }

  function show() {
    if (!initialized) init();
    // Prefill from the latest host score the first time it's empty, so the box opens on real engine output.
    if (textEl && !textEl.value && lastTex) textEl.value = lastTex;
  }

  function init() {
    initialized = true;
    const root = document.getElementById("debug-view");
    root.innerHTML = `
      <div class="atx-toolbar">
        <button type="button" id="atxLoad">Load current</button>
        <button type="button" id="atxRender" class="primary">Render</button>
        <span class="atx-hint">Edit the engine's alphaTex and Render — then play with the transport below.</span>
        <span class="atx-version" id="atxVersion"></span>
      </div>
      <textarea id="atxText" spellcheck="false"
        placeholder="alphaTex — click “Load current” to pull the last generated score, or paste your own."></textarea>
      <div id="atxScore"></div>`;

    textEl = document.getElementById("atxText");
    document.getElementById("atxLoad").addEventListener("click", onLoadCurrent);
    document.getElementById("atxRender").addEventListener("click", onRender);

    // Show which engine we're triaging against, read straight from the loaded build's meta.
    const verEl = document.getElementById("atxVersion");
    const version = typeof alphaTab !== "undefined" && alphaTab.meta && alphaTab.meta.version;
    if (verEl && version) verEl.textContent = "alphaTab v" + version;

    if (!window.ChordFlowScore) {
      setStatus("render component failed to load");
      return;
    }
    // Its own full-player component — transport + toggles come for free; content toggles re-render locally
    // from the current textarea (not via the bridge), keeping the inspector self-contained.
    view = window.ChordFlowScore.create(document.getElementById("atxScore"), {
      player: true,
      controls: "full",
      onNeedsRerender: () => onRender(),
    });
  }

  function onLoadCurrent() {
    if (lastTex) {
      textEl.value = lastTex;
      setStatus("loaded current score's alphaTex");
    } else {
      textEl.value = SAMPLE_TEX;
      setStatus("no host score yet — loaded a sample");
    }
  }

  function onRender() {
    if (!view) return;
    const tex = textEl.value.trim() || SAMPLE_TEX;
    try {
      view.load(tex);
      setStatus("rendered ✓");
    } catch (e) {
      setStatus("render failed — see console");
      console.error("alphaTex inspector render error:", e);
    }
  }

  function setStatus(text) {
    const el = document.getElementById("status");
    if (el) el.textContent = text;
  }

  return { show };
})();
