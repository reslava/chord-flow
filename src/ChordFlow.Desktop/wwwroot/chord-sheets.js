// ChordFlow Chord Sheets view — the interactive HTML shell around ChordSheetR ("ChordSheetUIR").
//
// Owns the page chrome (harmony/key/layout/notation/adornment/theme controls + export) and drives the pure-SVG
// ChordSheetR (chord-sheet-render-component.js) which draws the sheet body. Split by design (chat-001): the SVG
// body is used for BOTH screen and export (no parity drift); this shell adds the controls, the export actions,
// and — later — playback highlighting + the separate FretR now/next boards.
//
// Recompute vs pure-JS (req C3): harmony / key / adornment changes re-request the model from Core (the handler
// only resolves comping voicings for the diagram/both adornments); layout / notation / tone-label / theme are
// pure-JS re-renders over the held model — no round-trip.
"use strict";

window.ChordFlowChordSheets = (function () {
  const Bridge = window.ChordFlowBridge;
  const KEY_NAMES = ["C", "Db", "D", "Eb", "E", "F", "F#", "G", "Ab", "A", "Bb", "B"];

  let initialized = false;
  let pageEl, toolbarEl, sheetEl, errorEl, hintEl;
  let view = null;        // ChordSheetR handle
  let lastModel = null;   // last chordSheetResult model (for pure-JS re-renders)
  const harmony = [];     // [{ entity, id, name }] from the song + progression entity lists
  let harmonySel;

  const state = {
    harmonyEntity: null, harmonyId: null, key: "", // key "" = the song's own key
    layout: "A", primary: "concrete", secondary: "", toneLabels: "notes", adornment: "none", theme: "auto",
  };

  function setError(text) { if (errorEl) errorEl.textContent = text || ""; }
  function setHint(text) { if (hintEl) hintEl.textContent = text || ""; }

  // --- a small labelled <select> control ------------------------------------
  function select(label, options, value, onChange) {
    const wrap = document.createElement("span");
    wrap.style.cssText = "display:inline-flex;align-items:center;gap:.3rem;";
    const lab = document.createElement("label");
    lab.textContent = label;
    lab.style.cssText = "color:#9aa0a6;font-size:.82rem;";
    const sel = document.createElement("select");
    sel.style.cssText = "font:inherit;padding:.25rem .4rem;background:#3a3a3d;color:#e6e6e6;border:1px solid #4a4a4f;border-radius:4px;";
    for (const o of options) {
      const opt = document.createElement("option");
      opt.value = o.value; opt.textContent = o.label;
      sel.appendChild(opt);
    }
    sel.value = value;
    sel.addEventListener("change", () => onChange(sel.value));
    wrap.appendChild(lab); wrap.appendChild(sel);
    return { wrap, sel };
  }

  function button(label, onClick) {
    const b = document.createElement("button");
    b.type = "button";
    b.textContent = label;
    b.style.cssText = "font:inherit;padding:.3rem .8rem;border:1px solid #4a4a4f;border-radius:4px;background:#3a3a3d;color:#e6e6e6;cursor:pointer;";
    b.addEventListener("click", onClick);
    return b;
  }

  function buildToolbar() {
    toolbarEl.innerHTML = "";
    toolbarEl.style.cssText =
      "display:flex;align-items:center;gap:.75rem;flex-wrap:wrap;padding:.5rem .75rem;margin-bottom:.6rem;" +
      "background:#2d2d30;border:1px solid #333;border-radius:6px;font-size:.85rem;";

    const harmonyCtl = select("Sheet", [{ value: "", label: "— pick a song/progression —" }], "", (v) => {
      const picked = harmony.find((h) => h.entity + ":" + h.id === v);
      state.harmonyEntity = picked ? picked.entity : null;
      state.harmonyId = picked ? picked.id : null;
      requestSheet();
    });
    harmonySel = harmonyCtl.sel;
    toolbarEl.appendChild(harmonyCtl.wrap);

    const keyOpts = [{ value: "", label: "Song key" }].concat(
      KEY_NAMES.map((n, pc) => ({ value: String(pc), label: n })));
    toolbarEl.appendChild(select("Key", keyOpts, state.key, (v) => { state.key = v; requestSheet(); }).wrap);

    toolbarEl.appendChild(select("Layout",
      [{ value: "A", label: "A · Leadsheet" }, { value: "B", label: "B · Grid" }],
      state.layout, (v) => { state.layout = v; renderNow(); }).wrap);

    const notationOpts = [
      { value: "concrete", label: "Letter" }, { value: "nashville", label: "Nashville" }, { value: "roman", label: "Roman" },
    ];
    toolbarEl.appendChild(select("Chords", notationOpts, state.primary, (v) => { state.primary = v; renderNow(); }).wrap);
    toolbarEl.appendChild(select("+ line",
      [{ value: "", label: "None" }].concat(notationOpts), state.secondary, (v) => { state.secondary = v; renderNow(); }).wrap);

    toolbarEl.appendChild(select("Below cell",
      [{ value: "none", label: "None" }, { value: "tones", label: "Tone strip" }, { value: "diagram", label: "Fret diagram" }, { value: "both", label: "Both" }],
      state.adornment, (v) => { state.adornment = v; requestSheet(); }).wrap);

    toolbarEl.appendChild(select("Tone labels",
      [{ value: "notes", label: "Notes" }, { value: "intervals", label: "Intervals" }],
      state.toneLabels, (v) => { state.toneLabels = v; renderNow(); }).wrap);

    toolbarEl.appendChild(select("Theme",
      [{ value: "auto", label: "Auto" }, { value: "light", label: "Light" }, { value: "dark", label: "Dark" }],
      state.theme, (v) => { state.theme = v; renderNow(); }).wrap);

    // Export group (right-aligned). SVG/PNG are client-side; PDF prints via the host (always light — IN11).
    const spacer = document.createElement("span");
    spacer.style.cssText = "flex:1;";
    toolbarEl.appendChild(spacer);
    toolbarEl.appendChild(button("Export SVG", exportSvg));
    toolbarEl.appendChild(button("Export PNG", exportPng));
    toolbarEl.appendChild(button("Export PDF", exportPdf));
  }

  function sheetFilename(ext) {
    const picked = harmony.find((h) => h.entity === state.harmonyEntity && h.id === state.harmonyId);
    const base = (picked ? picked.name : "chord-sheet").replace(/[^\w.-]+/g, "_") || "chord-sheet";
    return base + "." + ext;
  }

  function downloadBlob(blob, filename) {
    const url = URL.createObjectURL(blob);
    const a = document.createElement("a");
    a.href = url; a.download = filename;
    document.body.appendChild(a);
    a.click();
    a.remove();
    setTimeout(() => URL.revokeObjectURL(url), 1000);
  }

  function exportSvg() {
    if (!view) { setError("Nothing to export yet."); return; }
    const s = view.toSvgString();
    if (!s) { setError("Nothing to export yet."); return; }
    downloadBlob(new Blob([s], { type: "image/svg+xml" }), sheetFilename("svg"));
  }

  function exportPng() {
    if (!view) { setError("Nothing to export yet."); return; }
    view.toPngBlob(2, (blob) => {
      if (blob) downloadBlob(blob, sheetFilename("png"));
      else setError("PNG export failed.");
    });
  }

  // PDF: inject a print-styled light copy into #chord-sheet-print (an @media print rule hides the rest) and ask
  // the host to print the page via WebView2 PrintToPdfAsync. The chordSheetPdfDone reply tears the copy back down.
  function exportPdf() {
    if (!view) { setError("Nothing to export yet."); return; }
    if (!Bridge.available) { setError("Open in the ChordFlow app to export PDF."); return; }
    const printEl = document.getElementById("chord-sheet-print");
    const svg = view.lightSvg();
    if (!printEl || !svg) { setError("Nothing to export yet."); return; }
    printEl.innerHTML = "";
    printEl.appendChild(svg);
    Bridge.send({ type: "exportChordSheet" });
  }

  // Re-render the held model with the current display settings (pure JS, no round-trip).
  function renderNow() {
    if (!lastModel) return;
    if (view) view.dispose();
    view = window.ChordFlowChordSheet.create(sheetEl, {
      layout: state.layout,
      notation: { primary: state.primary, secondary: state.secondary || null },
      toneLabels: state.toneLabels,
      adornments: {
        tones: state.adornment === "tones" || state.adornment === "both",
        diagrams: state.adornment === "diagram" || state.adornment === "both",
      },
      theme: state.theme,
    });
    view.render(lastModel);
  }

  // Ask the host to (re)build the model — the recompute path (harmony/key/adornment).
  function requestSheet() {
    setError("");
    if (!state.harmonyEntity || !state.harmonyId) {
      lastModel = null;
      if (view) { view.dispose(); view = null; }
      setHint("Pick a song or progression to render its chord sheet.");
      return;
    }
    if (!Bridge.available) { setHint("Open in the ChordFlow app to render chord sheets."); return; }
    setHint("");
    const msg = {
      type: "chordSheet",
      harmonyEntity: state.harmonyEntity,
      harmonyId: state.harmonyId,
      barsPerRow: 4,
      adornment: state.adornment,
    };
    if (state.key !== "") msg.keyPitchClass = parseInt(state.key, 10) || 0;
    Bridge.send(msg);
  }

  function onHostMessage(raw) {
    let msg;
    try { msg = JSON.parse(raw); } catch (e) { return; }
    if (msg.type === "chordSheetResult") {
      setError("");
      lastModel = msg.sheet;
      renderNow();
    } else if (msg.type === "chordSheetError") {
      setError(msg.message || "Couldn't build this chord sheet.");
    } else if (msg.type === "chordSheetPdfDone") {
      const printEl = document.getElementById("chord-sheet-print");
      if (printEl) printEl.innerHTML = "";
      if (msg.ok === false && msg.message) setError("PDF export failed: " + msg.message);
    } else if (msg.type === "entityList" && (msg.entity === "song" || msg.entity === "progression")) {
      mergeHarmony(msg.entity, msg.items || []);
    }
  }

  // Merge one entity list into the harmony dropdown (deduped by entity:id; source rows collapse by id).
  function mergeHarmony(entity, items) {
    for (const it of items) {
      if (!harmony.some((h) => h.entity === entity && h.id === it.id)) {
        harmony.push({ entity, id: it.id, name: it.name || it.id });
      }
    }
    if (!harmonySel) return;
    const keep = harmonySel.value;
    harmonySel.innerHTML = "";
    const first = document.createElement("option");
    first.value = ""; first.textContent = "— pick a song/progression —";
    harmonySel.appendChild(first);
    harmony
      .slice()
      .sort((a, b) => a.name.localeCompare(b.name))
      .forEach((h) => {
        const opt = document.createElement("option");
        opt.value = h.entity + ":" + h.id;
        opt.textContent = (h.entity === "song" ? "♪ " : "→ ") + h.name;
        harmonySel.appendChild(opt);
      });
    harmonySel.value = keep;
  }

  function init() {
    pageEl = document.getElementById("chord-sheets-page");
    pageEl.innerHTML = "";
    toolbarEl = document.createElement("div");
    errorEl = document.createElement("div");
    errorEl.style.cssText = "color:#ff8a8a;font-size:.8rem;min-height:1.1rem;margin:.2rem 0;";
    hintEl = document.createElement("div");
    hintEl.style.cssText = "color:#8a8f94;font-size:.85rem;margin:.2rem 0;";
    sheetEl = document.createElement("div");
    sheetEl.style.cssText = "overflow:auto;";
    pageEl.appendChild(toolbarEl);
    pageEl.appendChild(errorEl);
    pageEl.appendChild(hintEl);
    pageEl.appendChild(sheetEl);
    buildToolbar();
    if (Bridge.available) Bridge.onReceive(onHostMessage);
    initialized = true;
  }

  // Shown when the Chord Sheets tab is selected: lazy-init, refresh the harmony lists, and (re)request.
  function show() {
    if (!initialized) init();
    if (Bridge.available) {
      Bridge.send({ type: "entityList", entity: "song" });
      Bridge.send({ type: "entityList", entity: "progression" });
    }
    requestSheet();
  }

  return { show };
})();
