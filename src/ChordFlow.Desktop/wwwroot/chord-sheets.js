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
  let pageEl, toolbarEl, transportEl, scoreWrapEl, sheetEl, errorEl, hintEl;
  let view = null;        // ChordSheetR handle
  let lastModel = null;   // last chordSheetResult model (for pure-JS re-renders)
  const harmony = [];     // [{ entity, id, name }] from the song + progression entity lists
  let harmonySel;

  // Playback (the page owns its OWN ChordFlowPlayback engine — option a; no cross-page transport).
  let engine = null;               // ChordFlowPlayback handle (hidden staff surface)
  let scheduleByBar = new Map();    // bar (0-based) → [cellSchedule entries sorted by beat] for the marker
  let lastMarkerKey = null;         // last highlighted key — skip redundant re-highlights
  let markerMode = "metronome";     // "metronome" (per-beat, default) | "chord" (per-chord segment)
  let playBtn, stopBtn, tempoInput, soundFontSel;   // transport controls

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

    // Display toggles reuse the live ChordSheetR via its setters (not a rebuild) so the playback marker
    // survives; harmony/key/adornment change what Core computes, so those re-request (requestSheet).
    toolbarEl.appendChild(select("Layout",
      [{ value: "A", label: "A · Leadsheet" }, { value: "B", label: "B · Grid" }],
      state.layout, (v) => { state.layout = v; if (view) view.setLayout(v); }).wrap);

    const notationOpts = [
      { value: "concrete", label: "Letter" }, { value: "nashville", label: "Nashville" }, { value: "roman", label: "Roman" },
    ];
    toolbarEl.appendChild(select("Chords", notationOpts, state.primary,
      (v) => { state.primary = v; if (view) view.setNotation({ primary: v, secondary: state.secondary || null }); }).wrap);
    toolbarEl.appendChild(select("+ line",
      [{ value: "", label: "None" }].concat(notationOpts), state.secondary,
      (v) => { state.secondary = v; if (view) view.setNotation({ primary: state.primary, secondary: v || null }); }).wrap);

    toolbarEl.appendChild(select("Below cell",
      [{ value: "none", label: "None" }, { value: "tones", label: "Tone strip" }, { value: "diagram", label: "Fret diagram" }, { value: "both", label: "Both" }],
      state.adornment, (v) => {
        state.adornment = v;
        // Update the reused component's adornment flags (tones show at once; diagrams appear when the
        // re-fetch below returns their Core-computed data). requestSheet re-requests only for the diagram.
        if (view) view.setAdornments({ tones: v === "tones" || v === "both", diagrams: v === "diagram" || v === "both" });
        requestSheet();
      }).wrap);

    toolbarEl.appendChild(select("Tone labels",
      [{ value: "notes", label: "Notes" }, { value: "intervals", label: "Intervals" }],
      state.toneLabels, (v) => { state.toneLabels = v; if (view) view.setToneLabels(v); }).wrap);

    toolbarEl.appendChild(select("Theme",
      [{ value: "auto", label: "Auto" }, { value: "light", label: "Light" }, { value: "dark", label: "Dark" }],
      state.theme, (v) => { state.theme = v; if (view) view.setTheme(v); }).wrap);

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

  // Render the held model, creating the ChordSheetR once and reusing it thereafter (display toggles go
  // through its setters — see buildToolbar — so the playback marker survives).
  function renderNow() {
    if (!lastModel) return;
    if (!view) {
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
    }
    view.render(lastModel);
  }

  // --- playback: the page owns a ChordFlowPlayback (option a) ----------------
  // Build the transport strip (play/stop/tempo/soundfont + Show tab). The engine renders its staff into a
  // collapsed surface; "Show tab" reveals it (D4 — hidden by default).
  function buildTransport() {
    transportEl.innerHTML = "";
    transportEl.style.cssText =
      "display:flex;align-items:center;gap:.6rem;flex-wrap:wrap;padding:.4rem .75rem;margin-bottom:.6rem;" +
      "background:#2d2d30;border:1px solid #333;border-radius:6px;font-size:.85rem;";

    playBtn = button("▶ Play", () => { if (engine) engine.play(); });
    playBtn.disabled = true;
    stopBtn = button("■ Stop", () => { if (engine) engine.stop(); });
    stopBtn.disabled = true;
    transportEl.append(playBtn, stopBtn);

    const tempoLab = document.createElement("label");
    tempoLab.textContent = "Tempo"; tempoLab.style.cssText = "color:#9aa0a6;";
    tempoInput = document.createElement("input");
    tempoInput.type = "number"; tempoInput.min = "40"; tempoInput.max = "240"; tempoInput.step = "1";
    tempoInput.disabled = true;
    tempoInput.style.cssText = "width:4rem;font:inherit;padding:.2rem .3rem;background:#3a3a3d;color:#e6e6e6;border:1px solid #4a4a4f;border-radius:4px;";
    tempoInput.addEventListener("change", () => {
      const bpm = parseInt(tempoInput.value, 10);
      if (bpm && engine) engine.setTempo(bpm);
    });
    const bpm = document.createElement("span");
    bpm.textContent = "BPM"; bpm.style.color = "#9aa0a6";
    transportEl.append(tempoLab, tempoInput, bpm);

    const sfLab = document.createElement("label");
    sfLab.textContent = "Sound"; sfLab.style.cssText = "color:#9aa0a6;";
    soundFontSel = document.createElement("select");
    soundFontSel.style.cssText = "font:inherit;padding:.2rem .3rem;background:#3a3a3d;color:#e6e6e6;border:1px solid #4a4a4f;border-radius:4px;";
    soundFontSel.addEventListener("change", () => { if (engine) engine.setSoundFont(soundFontSel.value); });
    transportEl.append(sfLab, soundFontSel);

    // Marker mode: Visual metronome (per-beat, default) vs Per chord (the active chord segment).
    const markerLab = document.createElement("label");
    markerLab.textContent = "Marker"; markerLab.style.cssText = "color:#9aa0a6;";
    const markerSel = document.createElement("select");
    markerSel.style.cssText = "font:inherit;padding:.2rem .3rem;background:#3a3a3d;color:#e6e6e6;border:1px solid #4a4a4f;border-radius:4px;";
    [["metronome", "Visual metronome"], ["chord", "Per chord"]].forEach(([v, l]) => {
      const o = document.createElement("option"); o.value = v; o.textContent = l; markerSel.appendChild(o);
    });
    markerSel.value = markerMode;
    markerSel.addEventListener("change", () => { markerMode = markerSel.value; lastMarkerKey = null; });
    transportEl.append(markerLab, markerSel);

    const showTab = document.createElement("label");
    showTab.style.cssText = "display:inline-flex;align-items:center;gap:.3rem;color:#9aa0a6;margin-left:auto;";
    const showTabCb = document.createElement("input");
    showTabCb.type = "checkbox";
    showTabCb.addEventListener("change", () => { scoreWrapEl.style.maxHeight = showTabCb.checked ? "" : "0"; });
    showTab.append(showTabCb, document.createTextNode("Show tab"));
    transportEl.append(showTab);
  }

  // Create the page's own ChordFlowPlayback engine (once), rendering its staff into the collapsed surface.
  function setupEngine() {
    if (engine || !window.ChordFlowPlayback) return;
    const surface = document.createElement("div");
    scoreWrapEl.appendChild(surface);
    engine = window.ChordFlowPlayback.create(surface, {
      player: true,
      onBeat: onBeat,
      onStateChange: (playing) => { if (playBtn) playBtn.textContent = playing ? "⏸ Pause" : "▶ Play"; },
      onFinished: () => { if (view) view.clearHighlight(); lastMarkerKey = null; if (playBtn) playBtn.textContent = "▶ Play"; },
      onReady: () => { if (playBtn) playBtn.disabled = false; if (stopBtn) stopBtn.disabled = false; if (tempoInput) tempoInput.disabled = false; },
      onSoundFontsListed: (fonts, selectedId) => {
        if (!soundFontSel) return;
        soundFontSel.innerHTML = "";
        for (const f of (fonts || [])) {
          const o = document.createElement("option");
          o.value = f.id; o.textContent = f.name;
          soundFontSel.appendChild(o);
        }
        if (selectedId) soundFontSel.value = selectedId;
      },
    });
  }

  // Group the cellSchedule by bar (0-based), each bar's entries sorted by beat — for the beat→cell lookup.
  function buildSchedule(cellSchedule) {
    scheduleByBar = new Map();
    for (const e of (cellSchedule || [])) {
      let arr = scheduleByBar.get(e.bar);
      if (!arr) { arr = []; scheduleByBar.set(e.bar, arr); }
      arr.push(e);
    }
    for (const arr of scheduleByBar.values()) arr.sort((a, b) => a.beat - b.beat);
  }

  // The engine reports 1-based (bar,beat); the cellSchedule is 0-based (like NowNext). Both modes wash the
  // sounding bar (from its downbeat entry); they differ in the sub-highlight.
  function onBeat(bar, beat) {
    if (!view) return;
    const entries = scheduleByBar.get(bar - 1);
    if (!entries || entries.length === 0) return;   // out-of-range bar → keep the last marker
    const cell = entries[0];                         // downbeat entry → this bar's cell

    if (markerMode === "metronome") {
      // Visual metronome: light the current beat column of the bar.
      const beatIx = beat - 1;
      const key = "b:" + cell.section + ":" + cell.row + ":" + cell.cell + ":" + beatIx;
      if (key === lastMarkerKey) return;
      lastMarkerKey = key;
      view.highlightBeat(cell.section, cell.row, cell.cell, beatIx);
      return;
    }

    // Per chord: the active segment = the last entry whose beat <= the current beat (sub-chord onsets + sustain).
    const b = beat - 1;
    let active = entries[0];
    for (const e of entries) { if (e.beat <= b) active = e; else break; }
    const key = "c:" + active.section + ":" + active.row + ":" + active.cell + ":" + active.chord;
    if (key === lastMarkerKey) return;               // no change → skip a redundant DOM re-query
    lastMarkerKey = key;
    view.highlight(active.section, active.row, active.cell, active.chord);
  }

  // Ask the host to (re)build the model — the recompute path (harmony/key/adornment).
  function requestSheet() {
    setError("");
    if (!state.harmonyEntity || !state.harmonyId) {
      lastModel = null;
      if (view) { view.dispose(); view = null; }
      if (engine) engine.stop();
      scheduleByBar = new Map();
      lastMarkerKey = null;
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
      buildSchedule(msg.cellSchedule);
      renderNow();
      lastMarkerKey = null;
      if (view) view.clearHighlight();
      // New content → stop any playback and load the playable tex into the page's engine.
      if (engine) {
        engine.stop();
        const tempo = (msg.sheet.header && msg.sheet.header.tempo) || 100;
        engine.load(msg.tex, { tempo });
        if (tempoInput) tempoInput.value = String(tempo);
      }
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
    transportEl = document.createElement("div");
    errorEl = document.createElement("div");
    errorEl.style.cssText = "color:#ff8a8a;font-size:.8rem;min-height:1.1rem;margin:.2rem 0;";
    hintEl = document.createElement("div");
    hintEl.style.cssText = "color:#8a8f94;font-size:.85rem;margin:.2rem 0;";
    // The engine's staff surface — collapsed by default (Show tab reveals it); kept full-width so alphaTab lays out.
    scoreWrapEl = document.createElement("div");
    scoreWrapEl.style.cssText = "overflow:hidden;max-height:0;";
    sheetEl = document.createElement("div");
    sheetEl.style.cssText = "overflow:auto;";
    pageEl.appendChild(toolbarEl);
    pageEl.appendChild(transportEl);
    pageEl.appendChild(errorEl);
    pageEl.appendChild(hintEl);
    pageEl.appendChild(scoreWrapEl);
    pageEl.appendChild(sheetEl);
    buildToolbar();
    buildTransport();
    if (Bridge.available) Bridge.onReceive(onHostMessage);
    setupEngine();
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
