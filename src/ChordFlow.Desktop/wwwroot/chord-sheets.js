// ChordFlow Sheet VIEW (chord-sheets.js) — the Chord-Sheet view of the single Practice page.
//
// Refactored from the standalone Chord Sheets page shell into a VIEW module (harmony-controls-r IN2/IN9): the
// Practice shell (app.js) owns the page — the definition strip (HarmonyControlsR), the one playback engine,
// PlayerControlsR, the Now/Next boards, and the Score ⇄ Sheet toggle. This module owns only what is
// sheet-specific: the display strip (Layout / Chords / + line / Below cell / Tone labels / Theme / Marker mode),
// the pure-SVG ChordSheetR body, the playback bar-marker, and the three exports (SVG/PNG client-side, PDF via
// the #chord-sheet-print + host-print flow — EX3).
//
// It owns NO engine and issues NO render requests: the shell feeds it the sheet projection of the unified
// generate/loadExercise reply — render(sheet, name) + setSchedule(cellSchedule) — and fans the engine's two
// playback signals in: onBeat(bar, beat) (event-driven → Per-chord mode) and onPosition(bar, quarterBeat)
// (the PlaybackClock's time-linear steps → Visual-metronome mode; metronome-true-marker). Every strip
// control is a pure-JS re-render over the held model (req C3-style);
// Below cell too (IN10) — the model always carries tone + diagram data, so adornments never re-request.
"use strict";

window.ChordFlowSheetView = (function () {
  const Bridge = window.ChordFlowBridge;

  // --- small labelled <select> / button builders (sheet-strip chrome) ---------
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

  function create(container, opts) {
    opts = opts || {};

    let view = null;              // ChordSheetR handle (created once, reused — the marker survives toggles)
    let lastModel = null;         // last sheet model (for pure-JS re-renders)
    let sheetName = "chord-sheet";// export filename base (the shell passes the harmony name on render)
    let scheduleByBar = new Map();// bar (0-based) → [cellSchedule entries sorted by beat] for the marker
    let lastMarkerKey = null;     // last highlighted key — skip redundant re-highlights
    let markerMode = "metronome"; // "metronome" (per-beat, default) | "chord" (per-chord segment)

    const state = {
      layout: "A", primary: "concrete", secondary: "", toneLabels: "notes", adornment: "none", theme: "auto",
    };

    const stripEl = document.createElement("div");
    const errorEl = document.createElement("div");
    errorEl.style.cssText = "color:#ff8a8a;font-size:.8rem;min-height:1.1rem;margin:.2rem 0;";
    const sheetEl = document.createElement("div");
    sheetEl.style.cssText = "overflow:auto;";
    container.appendChild(stripEl);
    container.appendChild(errorEl);
    container.appendChild(sheetEl);

    function setError(text) { errorEl.textContent = text || ""; }

    // --- the sheet-specific display strip (IN9) — every control is pure-JS ----
    function buildStrip() {
      stripEl.innerHTML = "";
      stripEl.style.cssText =
        "display:flex;align-items:center;gap:.75rem;flex-wrap:wrap;padding:.5rem .75rem;margin-bottom:.6rem;" +
        "background:#2d2d30;border:1px solid #333;border-radius:6px;font-size:.85rem;";

      stripEl.appendChild(select("Layout",
        [{ value: "A", label: "A · Leadsheet" }, { value: "B", label: "B · Grid" }],
        state.layout, (v) => { state.layout = v; if (view) view.setLayout(v); }).wrap);

      const notationOpts = [
        { value: "concrete", label: "Letter" }, { value: "nashville", label: "Nashville" }, { value: "roman", label: "Roman" },
      ];
      stripEl.appendChild(select("Chords", notationOpts, state.primary,
        (v) => { state.primary = v; if (view) view.setNotation({ primary: v, secondary: state.secondary || null }); }).wrap);
      stripEl.appendChild(select("+ line",
        [{ value: "", label: "None" }].concat(notationOpts), state.secondary,
        (v) => { state.secondary = v; if (view) view.setNotation({ primary: state.primary, secondary: v || null }); }).wrap);

      // Below cell is a pure display toggle now (IN10): the unified reply's sheet model ALWAYS carries the
      // tone strips + comped fret diagrams, so flipping adornments never re-requests.
      stripEl.appendChild(select("Below cell",
        [{ value: "none", label: "None" }, { value: "tones", label: "Tone strip" }, { value: "diagram", label: "Fret diagram" }, { value: "both", label: "Both" }],
        state.adornment, (v) => {
          state.adornment = v;
          if (view) view.setAdornments(adornments());
        }).wrap);

      stripEl.appendChild(select("Tone labels",
        [{ value: "notes", label: "Notes" }, { value: "intervals", label: "Intervals" }],
        state.toneLabels, (v) => { state.toneLabels = v; if (view) view.setToneLabels(v); }).wrap);

      stripEl.appendChild(select("Theme",
        [{ value: "auto", label: "Auto" }, { value: "light", label: "Light" }, { value: "dark", label: "Dark" }],
        state.theme, (v) => { state.theme = v; if (view) view.setTheme(v); }).wrap);

      // Marker mode: Visual metronome (per-beat, default) vs Per chord (the active chord segment).
      stripEl.appendChild(select("Marker",
        [{ value: "metronome", label: "Visual metronome" }, { value: "chord", label: "Per chord" }],
        markerMode, (v) => { markerMode = v; lastMarkerKey = null; }).wrap);

      // Export group (right-aligned). SVG/PNG are client-side; PDF prints via the host (always light — EX3).
      const spacer = document.createElement("span");
      spacer.style.cssText = "flex:1;";
      stripEl.appendChild(spacer);
      stripEl.appendChild(button("Export SVG", exportSvg));
      stripEl.appendChild(button("Export PNG", exportPng));
      stripEl.appendChild(button("Export PDF", exportPdf));
    }

    function adornments() {
      return {
        tones: state.adornment === "tones" || state.adornment === "both",
        diagrams: state.adornment === "diagram" || state.adornment === "both",
      };
    }

    // --- exports (EX3 — mechanics unchanged) -----------------------------------
    function sheetFilename(ext) {
      return (sheetName.replace(/[^\w.-]+/g, "_") || "chord-sheet") + "." + ext;
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

    // PDF: inject a print-styled light copy into #chord-sheet-print (an @media print rule hides the rest) and
    // ask the host to print the page via WebView2 PrintToPdfAsync. chordSheetPdfDone tears the copy back down.
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

    // The PDF-done teardown is the one bridge envelope this module still owns (its own export round-trip).
    function onHostMessage(raw) {
      let msg;
      try { msg = JSON.parse(raw); } catch (e) { return; }
      if (msg.type === "chordSheetPdfDone") {
        const printEl = document.getElementById("chord-sheet-print");
        if (printEl) printEl.innerHTML = "";
        if (msg.ok === false && msg.message) setError("PDF export failed: " + msg.message);
      }
    }

    // --- playback marker (driven by the shell's beat fan-out — C5) --------------
    // Group the cellSchedule by bar (0-based), each bar's entries sorted by beat, for the beat→cell lookup.
    function setSchedule(cellSchedule) {
      scheduleByBar = new Map();
      for (const e of (cellSchedule || [])) {
        let arr = scheduleByBar.get(e.bar);
        if (!arr) { arr = []; scheduleByBar.set(e.bar, arr); }
        arr.push(e);
      }
      for (const arr of scheduleByBar.values()) arr.sort((a, b) => a.beat - b.beat);
      lastMarkerKey = null;
    }

    // The engine's time clock ("position", 1-based quarters — metronome-true-marker): drives ONLY the
    // Visual-metronome marker, one even step per quarter through notes, sustains and silences alike. The
    // event-driven onBeat below cannot do this — its beat number is the rendered note/rest index, so it
    // accelerates through silences and drags through long notes. Shares lastMarkerKey with onBeat (the mode
    // select resets it), so a mid-playback mode switch stays clean. Runs even while the Sheet view is
    // hidden, so a mid-playback Score ⇄ Sheet toggle reveals a marker already in the right place (IN7).
    function onPosition(bar, quarterBeat) {
      if (!view || markerMode !== "metronome") return;
      const entries = scheduleByBar.get(bar - 1);      // cellSchedule is 0-based
      if (!entries || entries.length === 0) return;    // out-of-range bar → keep the last marker
      const cell = entries[0];                         // downbeat entry → this bar's cell
      const beatIx = quarterBeat - 1;
      const key = "b:" + cell.section + ":" + cell.row + ":" + cell.cell + ":" + beatIx;
      if (key === lastMarkerKey) return;
      lastMarkerKey = key;
      view.highlightBeat(cell.section, cell.row, cell.cell, beatIx);
    }

    // The engine reports 1-based (bar,beat); the cellSchedule is 0-based. Event-driven — drives ONLY the
    // Per-chord mode (chord onsets ARE events, so this signal is correct there); the Visual-metronome mode
    // follows onPosition's time clock instead. Runs even while the Sheet view is hidden (IN7, as above).
    function onBeat(bar, beat) {
      if (!view || markerMode !== "chord") return;
      const entries = scheduleByBar.get(bar - 1);
      if (!entries || entries.length === 0) return;   // out-of-range bar → keep the last marker

      // Per chord: the active segment = the last entry whose beat <= the current beat (sub-chord onsets + sustain).
      const b = beat - 1;
      let active = entries[0];
      for (const e of entries) { if (e.beat <= b) active = e; else break; }
      const key = "c:" + active.section + ":" + active.row + ":" + active.cell + ":" + active.chord;
      if (key === lastMarkerKey) return;               // no change → skip a redundant DOM re-query
      lastMarkerKey = key;
      view.highlight(active.section, active.row, active.cell, active.chord);
    }

    function clearMarker() {
      if (view) view.clearHighlight();
      lastMarkerKey = null;
    }

    // --- render (fed the unified reply's sheet projection by the shell) ---------
    // Creates the ChordSheetR once and reuses it (display toggles go through its setters, so the marker
    // survives); a null sheet clears the view.
    function render(sheet, name) {
      setError("");
      if (name) sheetName = name;
      if (!sheet) {
        lastModel = null;
        if (view) { view.dispose(); view = null; }
        scheduleByBar = new Map();
        lastMarkerKey = null;
        return;
      }
      lastModel = sheet;
      if (!view) {
        view = window.ChordFlowChordSheet.create(sheetEl, {
          layout: state.layout,
          notation: { primary: state.primary, secondary: state.secondary || null },
          toneLabels: state.toneLabels,
          adornments: adornments(),
          theme: state.theme,
        });
      }
      view.render(lastModel);
      clearMarker();
    }

    buildStrip();
    if (Bridge.available) Bridge.onReceive(onHostMessage);

    return {
      render,        // render(sheet, name) — the unified reply's sheet projection (+ export filename base)
      setSchedule,   // setSchedule(cellSchedule) — the marker feed of the same reply
      onBeat,        // onBeat(bar, beat) — 1-based event signal, from the shell's engine fan-out (Per-chord mode)
      onPosition,    // onPosition(bar, quarterBeat) — 1-based time clock, same fan-out (Visual-metronome mode)
      clearMarker,   // clear the highlight (end of playback / stop)
      dispose() {
        if (view) { view.dispose(); view = null; }
        container.innerHTML = "";
      },
    };
  }

  return { create };
})();
