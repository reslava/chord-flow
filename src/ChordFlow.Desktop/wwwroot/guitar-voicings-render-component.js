// ChordFlow GuitarVoicingsR — the faceted voicings grid (the engine's visual oracle / dogfood surface).
//
// Shows MANY realized voicings at once as a grid of FretR chord-boxes, with a faceted toggle-button filter stack
// above it (styled like the Content → Voicings → Definitions toggles). It is a PROJECTION + LAYOUT over the engine
// catalog — no new derivation. It issues the `voicingGrid` bridge verb (the whole filter state) and renders the
// `voicingGridResult` reply (the whole grid resolved in one round-trip).
//
// DUMB VIEW (C1): NO music theory here. The facet axes below are filter *labels* only — the engine derives which
// quality has which facets (Core `QualityFacets`); this component just lists the toggle buttons and ships the
// selected enabled-token sets. A cell is rendered exactly as Core hands it back (a `FretboardDiagram`).
//
//   const view = ChordFlowGuitarVoicings.create(containerEl);  // then view.show() when the page/tab is shown.
"use strict";

window.ChordFlowGuitarVoicings = (function () {
  const Bridge = window.ChordFlowBridge;

  // Root names per pitch class (0 = C .. 11 = B), matching the renderer's spelling (the selector value is the pc).
  const KEY_NAMES = ["C", "Db", "D", "Eb", "E", "F", "F#", "G", "Ab", "A", "Bb", "B"];

  // The faceted filter levels: { token, label }. Tokens are the wire vocabulary the Core handler filters on
  // (family tokens + the 3rd/5th/7th facet tokens of QualityFacets); labels are presentation only. Order top→bottom.
  const LEVELS = [
    { key: "sources", label: "Source", chips: [
      { t: "automatic", l: "Automatic" }, { t: "package", l: "Package" }, { t: "user", l: "User" }] },
    { key: "families", label: "Family", chips: [
      { t: "caged", l: "CAGED" }, { t: "shell", l: "Shell" }, { t: "dshell", l: "Doubled shell" }] },
    { key: "thirds", label: "3rd", chips: [
      { t: "major", l: "Major" }, { t: "minor", l: "Minor" }, { t: "suspended", l: "Suspended" }] },
    { key: "fifths", label: "5th", chips: [
      { t: "perfect", l: "Perfect" }, { t: "augmented", l: "Augmented" }, { t: "diminished", l: "Diminished" }] },
    { key: "sevenths", label: "7th", chips: [
      { t: "triad", l: "Triad" }, { t: "6", l: "6" }, { t: "7", l: "7" }, { t: "maj7", l: "maj7" }, { t: "dim7", l: "dim7" }] },
  ];

  // Quality enum name → display label (presentation only; mirrors Core's EngineVoicingSource display names). Used
  // for the row headings — the grid lays out rows by quality.
  const QUALITY_LABELS = {
    Major: "Major", Minor: "Minor", Dominant7: "Dominant 7", Major7: "Major 7", Minor7: "Minor 7",
    HalfDiminished7: "m7♭5", Diminished: "Diminished", Diminished7: "dim7", Augmented: "Augmented",
    Major6: "Major 6", Minor6: "Minor 6",
  };

  // Per-cell FretR config: the grid owns orientation + label mode globally, so each cell hides its own orientation,
  // label, fret-window and legend chrome — leaving just the title + id + copy header.
  const CELL_CONTROLS = { orientation: false, label: false, fretWindow: false, legend: false };

  let stylesInjected = false;
  function injectStyles() {
    if (stylesInjected) return;
    stylesInjected = true;
    const css = `
      .gv-wrap { display:flex; flex-direction:column; gap:.7rem; }
      .gv-bar { display:flex; flex-wrap:wrap; gap:.5rem; align-items:center; }
      .gv-bar .gv-lbl { font-size:.74rem; color:#9aa0a6; }
      .gv-select { font:inherit; font-size:.78rem; padding:.2rem .4rem; border:1px solid #4a4a4f; border-radius:4px; background:#2c2c2f; color:#e6e6e6; }
      .gv-btn { font:inherit; font-size:.75rem; padding:.15rem .6rem; border:1px solid #4a4a4f; border-radius:4px; background:#3a3a3d; color:#e6e6e6; cursor:pointer; }
      .gv-filters { display:flex; flex-direction:column; gap:.35rem; }
      .gv-level { display:flex; flex-wrap:wrap; gap:.3rem; align-items:center; }
      .gv-level .gv-level-label { font-size:.72rem; color:#9aa0a6; min-width:3.4rem; }
      .gv-chip { font-size:.68rem; padding:.12rem .55rem; border-radius:999px; cursor:pointer; background:#252526; color:#6b7075; border:1px solid #3a3a3d; }
      .gv-chip:hover { background:#2d2d30; }
      .gv-chip.active { background:#3a4d78; color:#fff; border-color:#3a4d78; }
      .gv-grid { display:flex; flex-direction:column; gap:1.1rem; }
      .gv-qgroup { display:flex; flex-direction:column; gap:.3rem; }
      .gv-qhead { font-size:.82rem; font-weight:600; color:#e6e6e6; }
      .gv-cells { display:flex; flex-wrap:wrap; gap:.6rem; }
      .gv-cell { background:#2a2a2d; border:1px solid #3a3a3d; border-radius:8px; padding:.3rem; }
      .gv-msg { color:#9aa0a6; font-size:.85rem; padding:1rem .2rem; }`;
    const style = document.createElement("style");
    style.textContent = css;
    document.head.appendChild(style);
  }

  function create(container, opts) {
    opts = opts || {};
    injectStyles();

    // Filter state: a Set of enabled tokens per level (all-on by default ⇒ everything), and the single root.
    const enabled = {};
    for (const level of LEVELS) enabled[level.key] = new Set(level.chips.map((c) => c.t));
    let rootPc = Number.isInteger(opts.defaultRoot) ? opts.defaultRoot : 0;
    let orientation = opts.orientation === "horizontal" ? "horizontal" : "vertical";
    let labelMode = opts.labelMode === "note" ? "note" : "interval";

    let cellViews = []; // live FretR handles, so the global orientation/label toggles fan out without a re-fetch
    let gridEl, orientBtn, labelBtn;
    let built = false;
    let registered = false;

    function el(tag, cls, text) {
      const node = document.createElement(tag);
      if (cls) node.className = cls;
      if (text != null) node.textContent = text;
      return node;
    }

    function build() {
      if (built) return;
      built = true;
      container.innerHTML = "";
      const wrap = el("div", "gv-wrap");

      // Shared controls bar: root selector + the two global display toggles fanned out to every cell.
      const bar = el("div", "gv-bar");
      bar.appendChild(el("span", "gv-lbl", "Root:"));
      const rootSel = el("select", "gv-select");
      KEY_NAMES.forEach((name, pc) => {
        const o = el("option", null, name);
        o.value = String(pc);
        rootSel.appendChild(o);
      });
      rootSel.value = String(rootPc);
      rootSel.addEventListener("change", () => {
        rootPc = parseInt(rootSel.value, 10) || 0;
        sendQuery();
      });
      bar.appendChild(rootSel);

      orientBtn = el("button", "gv-btn", orientation === "horizontal" ? "Horizontal" : "Vertical");
      orientBtn.type = "button";
      orientBtn.addEventListener("click", () =>
        setOrientation(orientation === "horizontal" ? "vertical" : "horizontal"));
      bar.appendChild(el("span", "gv-lbl", "Layout:"));
      bar.appendChild(orientBtn);

      labelBtn = el("button", "gv-btn", labelMode === "note" ? "Notes" : "Intervals");
      labelBtn.type = "button";
      labelBtn.addEventListener("click", () => setLabelMode(labelMode === "note" ? "interval" : "note"));
      bar.appendChild(el("span", "gv-lbl", "Labels:"));
      bar.appendChild(labelBtn);

      wrap.appendChild(bar);

      // Faceted filter stack: one row of toggle chips per level.
      const filters = el("div", "gv-filters");
      for (const level of LEVELS) filters.appendChild(buildLevel(level));
      wrap.appendChild(filters);

      gridEl = el("div", "gv-grid");
      wrap.appendChild(gridEl);
      container.appendChild(wrap);
    }

    function buildLevel(level) {
      const row = el("div", "gv-level");
      row.appendChild(el("span", "gv-level-label", level.label));
      for (const chip of level.chips) {
        const btn = el("button", "gv-chip" + (enabled[level.key].has(chip.t) ? " active" : ""), chip.l);
        btn.type = "button";
        btn.addEventListener("click", () => {
          if (enabled[level.key].has(chip.t)) enabled[level.key].delete(chip.t);
          else enabled[level.key].add(chip.t);
          btn.classList.toggle("active");
          sendQuery();
        });
        row.appendChild(btn);
      }
      return row;
    }

    // Ask the host to resolve the whole filtered grid (one round-trip). The arrays are the enabled-token sets:
    // all-on ⇒ everything; a level emptied ⇒ that level admits nothing ⇒ an empty grid (never an error, C5).
    function sendQuery() {
      if (!Bridge || !Bridge.available) {
        showMessage("Open in the ChordFlow app to render voicings.");
        return;
      }
      Bridge.send({
        type: "voicingGrid",
        rootPitchClass: rootPc,
        sources: [...enabled.sources],
        families: [...enabled.families],
        thirds: [...enabled.thirds],
        fifths: [...enabled.fifths],
        sevenths: [...enabled.sevenths],
      });
    }

    function onHostMessage(raw) {
      let msg;
      try {
        msg = JSON.parse(raw);
      } catch (e) {
        return;
      }
      if (msg && msg.type === "voicingGridResult") renderGrid(msg.cells || []);
    }

    function disposeCells() {
      for (const v of cellViews) v.dispose();
      cellViews = [];
    }

    function showMessage(text) {
      if (!gridEl) return;
      disposeCells();
      gridEl.innerHTML = "";
      gridEl.appendChild(el("div", "gv-msg", text));
    }

    // Lay out the cells as rows-by-quality (the cells arrive ordered quality→family→shape, so same-quality cells
    // are contiguous), each cell a FretR chord-box created with the current global orientation + label mode.
    function renderGrid(cells) {
      disposeCells();
      gridEl.innerHTML = "";
      if (cells.length === 0) {
        gridEl.appendChild(el("div", "gv-msg", "No voicings match the current filter."));
        return;
      }

      let group = null;
      let currentQuality = null;
      for (const cell of cells) {
        if (cell.quality !== currentQuality) {
          currentQuality = cell.quality;
          group = el("div", "gv-qgroup");
          group.appendChild(el("div", "gv-qhead", QUALITY_LABELS[cell.quality] || cell.quality));
          const cellsWrap = el("div", "gv-cells");
          group.appendChild(cellsWrap);
          group._cells = cellsWrap;
          gridEl.appendChild(group);
        }
        const box = el("div", "gv-cell");
        group._cells.appendChild(box);
        const view = window.ChordFlowFretboard.create(box, {
          orientation,
          labelMode,
          title: cell.title,
          id: cell.id,
          controls: CELL_CONTROLS,
        });
        view.render(cell.diagram);
        cellViews.push(view);
      }
    }

    function setOrientation(mode) {
      orientation = mode === "horizontal" ? "horizontal" : "vertical";
      if (orientBtn) orientBtn.textContent = orientation === "horizontal" ? "Horizontal" : "Vertical";
      for (const v of cellViews) v.setOrientation(orientation); // fan out — no re-fetch needed
    }

    function setLabelMode(mode) {
      labelMode = mode === "note" ? "note" : "interval";
      if (labelBtn) labelBtn.textContent = labelMode === "note" ? "Notes" : "Intervals";
      for (const v of cellViews) v.setLabelMode(labelMode); // fan out — no re-fetch needed
    }

    // Mount (idempotent), register the inbound handler once, and request the current grid.
    function show() {
      build();
      if (!registered && Bridge && Bridge.available) {
        Bridge.onReceive(onHostMessage);
        registered = true;
      }
      sendQuery();
    }

    function dispose() {
      disposeCells();
      container.innerHTML = "";
      built = false;
    }

    return { show, dispose, setOrientation, setLabelMode };
  }

  return { create };
})();
