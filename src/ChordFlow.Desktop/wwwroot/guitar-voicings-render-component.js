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

  // The faceted filter levels handed to the shared FilterR: { key, label, chips:[{token,label}] }. Tokens are the
  // wire vocabulary the Core handler filters on (family tokens + the 3rd/5th/7th facet tokens of QualityFacets);
  // labels are presentation only. Order top→bottom. Root stays a dropdown (single global root), not a chip level.
  const LEVELS = [
    { key: "sources", label: "Source", chips: [
      { token: "automatic", label: "Automatic" }, { token: "package", label: "Package" }, { token: "user", label: "User" }] },
    { key: "families", label: "Family", chips: [
      { token: "caged", label: "CAGED" }, { token: "shell", label: "Shell" }, { token: "dshell", label: "Doubled shell" }] },
    { key: "thirds", label: "3rd", chips: [
      { token: "major", label: "Major" }, { token: "minor", label: "Minor" }, { token: "suspended", label: "Suspended" }] },
    { key: "fifths", label: "5th", chips: [
      { token: "perfect", label: "Perfect" }, { token: "augmented", label: "Augmented" }, { token: "diminished", label: "Diminished" }] },
    { key: "sevenths", label: "7th", chips: [
      { token: "triad", label: "Triad" }, { token: "6", label: "6" }, { token: "7", label: "7" }, { token: "maj7", label: "maj7" }, { token: "dim7", label: "dim7" }] },
  ];

  // Quality enum name → display label (presentation only; mirrors Core's EngineVoicingSource display names). Used
  // for the row headings — the grid lays out rows by quality.
  const QUALITY_LABELS = {
    Major: "Major", Minor: "Minor", Dominant7: "Dominant 7", Major7: "Major 7", Minor7: "Minor 7",
    HalfDiminished7: "m7♭5", Diminished: "Diminished", Diminished7: "dim7", Augmented: "Augmented",
    Major6: "Major 6", Minor6: "Minor 6",
  };

  // Per-cell FretR config: the grid owns orientation + label mode + theme globally, so each cell hides its own
  // orientation, label, fret-window, legend and theme chrome — leaving just the title + id + copy header.
  const CELL_CONTROLS = { orientation: false, label: false, fretWindow: false, legend: false, theme: false };

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

    // Filter state now lives in the shared FilterR (all-on by default ⇒ everything); the single root stays local.
    let filterR = null;
    let rootPc = Number.isInteger(opts.defaultRoot) ? opts.defaultRoot : 0;
    let orientation = opts.orientation === "horizontal" ? "horizontal" : "vertical";
    let labelMode = opts.labelMode === "note" ? "note" : "interval";
    let theme = opts.theme === "light" ? "light" : "dark"; // grid defaults dark (matches the cell background)

    let cellViews = []; // live FretR handles, so the global orientation/label/theme toggles fan out without a re-fetch
    let gridEl, orientBtn, labelBtn, themeBtn;
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

      themeBtn = el("button", "gv-btn", theme === "dark" ? "Dark" : "Light");
      themeBtn.type = "button";
      themeBtn.addEventListener("click", () => setTheme(theme === "dark" ? "light" : "dark"));
      bar.appendChild(el("span", "gv-lbl", "Theme:"));
      bar.appendChild(themeBtn);

      wrap.appendChild(bar);

      // Faceted filter stack — the shared FilterR (chip rendering + enabled-set state); a change re-issues the
      // server-side voicingGrid round-trip. FilterR's per-level All/None comes for free (filter-ux-facets IN6);
      // the grid has NO cascade/counts (its facets are a fixed vocabulary filtered server-side — EX1).
      const filters = el("div", "gv-filters");
      wrap.appendChild(filters);
      filterR = window.ChordFlowFilter.create(filters, { levels: LEVELS, onChange: sendQuery });

      gridEl = el("div", "gv-grid");
      wrap.appendChild(gridEl);
      container.appendChild(wrap);
    }

    // Ask the host to resolve the whole filtered grid (one round-trip). The arrays are FilterR's enabled-token
    // sets: all-on ⇒ everything; a level emptied ⇒ that level admits nothing ⇒ an empty grid (never an error, C5).
    function sendQuery() {
      if (!Bridge || !Bridge.available) {
        showMessage("Open in the ChordFlow app to render voicings.");
        return;
      }
      const s = filterR ? filterR.getState() : {};
      Bridge.send({
        type: "voicingGrid",
        rootPitchClass: rootPc,
        sources: [...(s.sources || [])],
        families: [...(s.families || [])],
        thirds: [...(s.thirds || [])],
        fifths: [...(s.fifths || [])],
        sevenths: [...(s.sevenths || [])],
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
          theme,
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

    function setTheme(mode) {
      theme = mode === "light" ? "light" : "dark";
      if (themeBtn) themeBtn.textContent = theme === "dark" ? "Dark" : "Light";
      for (const v of cellViews) v.setTheme(theme); // fan out — no re-fetch needed
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
      if (filterR) { filterR.dispose(); filterR = null; }
      container.innerHTML = "";
      built = false;
    }

    return { show, dispose, setOrientation, setLabelMode, setTheme };
  }

  return { create };
})();
