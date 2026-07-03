// ChordFlow Voicings Engine view — the introspectable operator inspector (voicings-engine dogfood page).
//
// Pick an operator (caged/dshell/shell), a quality, a root, and the operator's declared params, and the host
// (VoicingDeriveHandler → FamilyVoicing.Voicing + RealizedVoicingDiagram) *derives* ONE voicing and returns its
// VoicingDerivation: the abstract tone selection (which chord tones, by function), the ordered "show your work"
// steps, and the realized grip. This is the live form of the golden oracle and the "explain this voicing" surface.
//
// A dumb view (ctx C1): the controls are schema-driven from the voicingOperators catalog (operators + their declared
// ParameterSchema + the eligible shapes per quality), the engine derives everything, and the shared ChordFlowFretboard
// draws the grip. No music theory in JS.
"use strict";

window.ChordFlowVoicingsEngine = (function () {
  const Bridge = window.ChordFlowBridge;
  const KEY_NAMES = ["C", "Db", "D", "Eb", "E", "F", "F#", "G", "Ab", "A", "Bb", "B"];
  // Familiar labels for the Quality enum names the host parses (value = enum name sent on the wire).
  const QUALITY_LABELS = {
    Major: "maj", Minor: "min", Major7: "maj7", Dominant7: "dom7", Minor7: "m7",
    HalfDiminished7: "m7b5", Diminished7: "dim7", Augmented: "aug", Major6: "6", Minor6: "m6",
  };
  const qualityLabel = (q) => QUALITY_LABELS[q] || q;

  const $ = (id) => document.getElementById(id);

  let initialized = false;
  let operators = null;                // the voicingOperators catalog (array), null until first reply
  let fretView = null;                 // lazy ChordFlowFretboard handle
  let operatorEl, qualityEl, rootEl, shapeEl, minFretEl, maxFretEl, errorEl, abstractEl, diagramEl;

  function setError(text) {
    if (errorEl) errorEl.textContent = text || "";
  }

  const currentOperator = () => operators && operators.find((o) => o.family === operatorEl.value);

  // The eligible shapes for the current operator + quality, from the catalog coverage.
  function eligibleShapes(op, quality) {
    const entry = op && op.eligibleShapesByQuality.find((q) => q.quality === quality);
    return entry ? entry.shapes : [];
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

  // Refill the quality select (the operator's eligible qualities), then the shape select for the chosen quality —
  // keeping the current values when they survive the operator/quality change.
  function populateForOperator() {
    const op = currentOperator();
    if (!op) return;

    const quals = op.eligibleShapesByQuality.map((q) => ({ value: q.quality, label: qualityLabel(q.quality) }));
    const curQual = qualityEl.value;
    fillSelect(qualityEl, quals);
    qualityEl.value = quals.some((q) => q.value === curQual) ? curQual : (quals[0] && quals[0].value);

    populateShapes();
  }

  function populateShapes() {
    const op = currentOperator();
    if (!op) return;
    const shapes = eligibleShapes(op, qualityEl.value);
    const curShape = shapeEl.value;
    fillSelect(shapeEl, shapes.map((s) => ({ value: s, label: s })));
    shapeEl.value = shapes.includes(curShape) ? curShape : (shapes[0] || "");
  }

  // Ask the host to derive the current selection.
  function requestDerive() {
    const op = currentOperator();
    if (!Bridge.available) {
      setError("Open in the ChordFlow app to run the voicings engine.");
      return;
    }
    if (!op || !shapeEl.value) return; // catalog not ready / no eligible shape
    setError("");
    Bridge.send({
      type: "voicingDerive",
      family: op.family,
      quality: qualityEl.value,
      shape: shapeEl.value,
      rootPitchClass: parseInt(rootEl.value, 10) || 0,
      minFret: parseInt(minFretEl.value, 10) || 0,
      maxFret: parseInt(maxFretEl.value, 10) || 0,
    });
  }

  // Build the operator select from the catalog, choosing a sensible default, then derive.
  function onOperatorsLoaded(list) {
    operators = list;
    fillSelect(operatorEl, operators.map((o) => ({ value: o.family, label: o.displayName })));
    operatorEl.value = operators.some((o) => o.family === "caged") ? "caged" : operators[0].family;
    populateForOperator();
    // Sensible starting point: a dom7 if the operator offers it.
    if (qualityEl.querySelector('option[value="Dominant7"]')) {
      qualityEl.value = "Dominant7";
      populateShapes();
    }
    requestDerive();
  }

  // Render the left column: the abstract voicing (tone selection) + the ordered derivation steps + the id.
  function renderAbstract(msg) {
    abstractEl.innerHTML = "";

    const head = document.createElement("h3");
    head.textContent = `Abstract voicing — ${msg.kind}`;
    head.style.margin = "0 0 .4rem";
    abstractEl.appendChild(head);

    const table = document.createElement("table");
    table.style.borderCollapse = "collapse";
    table.style.width = "100%";
    table.style.marginBottom = ".8rem";
    msg.toneSelection.forEach((t) => {
      const tr = document.createElement("tr");
      [t.function, t.intervalLabel, t.note].forEach((cell, i) => {
        const td = document.createElement("td");
        td.textContent = cell;
        td.style.padding = ".15rem .5rem";
        td.style.borderBottom = "1px solid #eee";
        if (i === 1) td.style.fontWeight = "600";
        tr.appendChild(td);
      });
      table.appendChild(tr);
    });
    abstractEl.appendChild(table);

    const stepsHead = document.createElement("h3");
    stepsHead.textContent = "Derivation";
    stepsHead.style.margin = "0 0 .4rem";
    abstractEl.appendChild(stepsHead);

    const ol = document.createElement("ol");
    ol.style.margin = "0 0 .6rem";
    ol.style.paddingLeft = "1.2rem";
    msg.realizationSteps.forEach((s) => {
      const li = document.createElement("li");
      li.style.marginBottom = ".2rem";
      const tag = document.createElement("span");
      tag.textContent = s.kind;
      tag.style.cssText = "display:inline-block;font-size:.7rem;color:#3a4d78;background:#eef1f8;border-radius:3px;padding:0 .3rem;margin-right:.4rem;";
      li.appendChild(tag);
      li.appendChild(document.createTextNode(s.label));
      ol.appendChild(li);
    });
    abstractEl.appendChild(ol);

    const id = document.createElement("code");
    id.textContent = msg.id;
    id.style.cssText = "font-size:.8rem;color:#666;";
    abstractEl.appendChild(id);
  }

  // Inbound from the host. Every registered handler sees every message; we own voicingOperators / voicingDerivation
  // / voicingDeriveError.
  function onHostMessage(raw) {
    let msg;
    try {
      msg = JSON.parse(raw);
    } catch (e) {
      return;
    }
    if (msg.type === "voicingOperators") {
      onOperatorsLoaded(msg.operators);
    } else if (msg.type === "voicingDerivation") {
      setError("");
      renderAbstract(msg);
      if (!fretView && window.ChordFlowFretboard) {
        fretView = window.ChordFlowFretboard.create(diagramEl, {
          orientation: "vertical", // a single grip reads best as a chord box; the toggle lets the user flip it
          labelMode: "interval",
        });
      }
      if (fretView) fretView.render(msg.diagram);
    } else if (msg.type === "voicingDeriveError") {
      setError(msg.message);
    }
  }

  function init() {
    operatorEl = $("veOperator");
    qualityEl = $("veQuality");
    rootEl = $("veRoot");
    shapeEl = $("veShape");
    minFretEl = $("veMinFret");
    maxFretEl = $("veMaxFret");
    errorEl = $("veError");
    abstractEl = $("ve-abstract");
    diagramEl = $("ve-diagram");

    fillSelect(rootEl, KEY_NAMES.map((name, pc) => ({ value: String(pc), label: name })));
    rootEl.value = "0"; // C

    operatorEl.addEventListener("change", () => { populateForOperator(); requestDerive(); });
    qualityEl.addEventListener("change", () => { populateShapes(); requestDerive(); });
    shapeEl.addEventListener("change", requestDerive);
    rootEl.addEventListener("change", requestDerive);
    minFretEl.addEventListener("change", requestDerive);
    maxFretEl.addEventListener("change", requestDerive);

    if (Bridge.available) Bridge.onReceive(onHostMessage);
    initialized = true;
  }

  // Called by the view toggle when the Voicings Engine tab is shown — lazily inits, fetches the operator catalog
  // once (which then derives), or re-derives the current selection if the catalog is already loaded.
  function show() {
    if (!initialized) init();
    if (!Bridge.available) {
      setError("Open in the ChordFlow app to run the voicings engine.");
      return;
    }
    if (!operators) {
      Bridge.send({ type: "voicingOperators" }); // reply drives the first derivation
    } else {
      requestDerive();
    }
  }

  return { show };
})();
