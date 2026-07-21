// ChordFlow Rhythm Generator view — the generation-engine dogfood page.
//
// Three strategies: FIGURE (a named groove figure), PATTERN (a placement family — subdivision × region ×
// onset count), RANDOM (free fill). Figure/Pattern draw bars from their kind via a SELECTION (fixed/cycle/
// randomInKind/fixedPlusRotating, with indexes) and behaviours (displace/sweep/restBar/callResponse). The
// host projects the onset grid to a single-voice drum groove → percussion tex (played, looping) + DrumsR
// grid (with the "1 e & a" overlay + a Beat-1 reference row) + an onset-ASCII debug line. Ephemeral.
//
// A DUMB page: it assembles the request and draws the reply; all rhythm theory lives in Core.
"use strict";

window.ChordFlowRhythmGen = (function () {
  const Bridge = window.ChordFlowBridge;
  const DEBOUNCE_MS = 200;
  const $ = (id) => document.getElementById(id);

  const FIGURES = [
    ["four-on-floor", "Four-on-the-floor"], ["downbeats", "Downbeats (1 & 3)"], ["backbeat", "Backbeat (2 & 4)"],
    ["beat1", "Beat-1 anchor"], ["straight-8ths", "Straight eighths"], ["offbeats", "Offbeats (all &s)"],
    ["charleston", "Charleston"], ["rev-charleston", "Reverse Charleston"], ["tresillo", "Tresillo (3-3-2)"],
    ["cinquillo", "Cinquillo"], ["dotted-push", "Dotted-quarter push"], ["habanera", "Habanera"],
    ["son-clave-32", "Son clave (3-2)"], ["son-clave-23", "Son clave (2-3)"], ["rumba-clave-32", "Rumba clave (3-2)"],
    ["bossa-clave", "Bossa clave"],
  ];
  const STRATEGIES = ["figure", "pattern", "random"];
  const SUBDIVISIONS = [["1", "Quarter"], ["2", "Eighth"]];
  const REGIONS = ["all", "onbeat", "offbeat"];
  const SELECTIONS = ["cycle", "fixed", "randomInKind", "fixedPlusRotating"];
  const VOICES = ["HH", "SD", "BD", "OH", "RD", "CC"];
  const PALETTE_VALUES = [4, 8, 16];
  const MAX_BARS = 16;

  let initialized = false;
  let scoreView = null;
  let gridView = null;
  let controlsEl, errorEl, gridEl, gridTextEl, scoreEl;
  let ctrl = {};
  let debounceTimer = null;

  function setError(text) { if (errorEl) errorEl.textContent = text || ""; }
  const pick = (arr) => arr[Math.floor(Math.random() * arr.length)];
  const randInt = (lo, hi) => lo + Math.floor(Math.random() * (hi - lo + 1));

  // --- control builders --------------------------------------------------
  function field(labelText, node) {
    const wrap = document.createElement("label");
    wrap.style.cssText = "display:flex; flex-direction:column; gap:.15rem; font-size:.8rem; color:#5a5f64;";
    const span = document.createElement("span");
    span.textContent = labelText;
    wrap.append(span, node);
    return wrap;
  }

  function select(options, initial) {
    const s = document.createElement("select");
    for (const o of options) {
      const [value, label] = Array.isArray(o) ? o : [o, o];
      const opt = document.createElement("option");
      opt.value = value; opt.textContent = label;
      s.appendChild(opt);
    }
    if (initial != null) s.value = initial;
    s.addEventListener("change", onChange);
    return s;
  }

  function number(value, min, max) {
    const n = document.createElement("input");
    n.type = "number"; n.value = value; if (min != null) n.min = min; if (max != null) n.max = max;
    n.style.width = "4.5rem";
    n.addEventListener("input", onChange);
    return n;
  }

  function checkbox(label, checked) {
    const wrap = document.createElement("label");
    wrap.style.cssText = "display:flex; gap:.25rem; align-items:center; font-size:.8rem;";
    const input = document.createElement("input");
    input.type = "checkbox"; input.checked = !!checked;
    input.addEventListener("change", onChange);
    wrap.append(input, document.createTextNode(label));
    wrap._input = input;
    return wrap;
  }

  function row(...children) {
    const d = document.createElement("div");
    d.style.cssText = "display:flex; gap:.75rem; flex-wrap:wrap; align-items:flex-end;";
    d.append(...children);
    return d;
  }

  function buildControls() {
    controlsEl.innerHTML = "";
    ctrl.strategy = select(STRATEGIES, "figure");
    // Landing on Figure should play the figure's natural self — reset the modifiers to neutral defaults.
    // (Surprise-me sets its randoms programmatically, which does NOT fire this change event.)
    ctrl.strategy.addEventListener("change", () => { if (ctrl.strategy.value === "figure") resetModifiers(); });

    // Figure + Pattern kind controls.
    ctrl.figureId = select(FIGURES, "tresillo");
    ctrl.subdivision = select(SUBDIVISIONS, "2");
    ctrl.region = select(REGIONS, "all");
    ctrl.onsetCount = number(2, 1, 8);

    // Shared selection + behaviours + bars (figure & pattern).
    ctrl.selection = select(SELECTIONS, "cycle");
    ctrl.selIndex = number(0, 0, 999);
    ctrl.selRotIndex = number(0, 0, 999);
    ctrl.displace = number(0, 0, 15);
    ctrl.sweep = checkbox("Sweep", false);
    ctrl.restBar = checkbox("RestBar", false);
    ctrl.restContent = number(1, 1, 8);
    ctrl.restRest = number(1, 0, 8);
    ctrl.callResponse = checkbox("Call/Resp", false);
    ctrl.barCount = number(4, 1, MAX_BARS);

    ctrl.figureGroup = row(field("Figure", ctrl.figureId));
    ctrl.placementGroup = row(field("Subdiv", ctrl.subdivision), field("Region", ctrl.region), field("Onsets", ctrl.onsetCount));
    ctrl.selectionGroup = row(
      field("Selection", ctrl.selection), field("Index", ctrl.selIndex), field("Rot idx", ctrl.selRotIndex),
      field("Displace", ctrl.displace), ctrl.sweep, ctrl.restBar,
      field("Content", ctrl.restContent), field("Rest", ctrl.restRest), ctrl.callResponse,
      field("Bars", ctrl.barCount));

    // Random group.
    ctrl.palette = PALETTE_VALUES.map((v) => {
      const cb = document.createElement("input");
      cb.type = "checkbox"; cb.value = v; cb.checked = v !== 16;
      cb.addEventListener("change", onChange);
      return cb;
    });
    ctrl.contentBars = number(2, 1, 4);
    ctrl.silenceBars = number(0, 0, 4);
    ctrl.restPct = document.createElement("input");
    ctrl.restPct.type = "range"; ctrl.restPct.min = "0"; ctrl.restPct.max = "80"; ctrl.restPct.value = "30";
    ctrl.restPct.style.width = "6rem";
    ctrl.restPctVal = document.createElement("span");
    ctrl.restPctVal.style.cssText = "font-size:.75rem; min-width:2.6rem;";
    const syncRestPct = () => { ctrl.restPctVal.textContent = ctrl.restPct.value + "%"; };
    ctrl.restPct.addEventListener("input", () => { syncRestPct(); onChange(); });
    syncRestPct();

    const paletteWrap = document.createElement("div");
    paletteWrap.style.cssText = "display:flex; gap:.5rem; align-items:center;";
    PALETTE_VALUES.forEach((v, i) => {
      const l = document.createElement("label");
      l.style.cssText = "display:flex; gap:.2rem; align-items:center; font-size:.8rem;";
      l.append(ctrl.palette[i], document.createTextNode(String(v)));
      paletteWrap.appendChild(l);
    });
    const restWrap = document.createElement("div");
    restWrap.style.cssText = "display:flex; align-items:center; gap:.4rem;";
    restWrap.append(ctrl.restPct, ctrl.restPctVal);
    ctrl.randomGroup = row(
      field("Palette", paletteWrap), field("Content bars", ctrl.contentBars),
      field("Silence bars", ctrl.silenceBars), field("Rest", restWrap));

    // Common.
    ctrl.voice = select(VOICES, "HH");
    ctrl.tempo = number(100, 40, 240);
    ctrl.seed = number(1, 0);
    const reroll = button("Reroll", () => { ctrl.seed.value = String(randInt(0, 99999)); generate(); });
    const reset = button("Reset", () => { resetModifiers(); generate(); });
    const surprise = button("🎲 Surprise me", surpriseMe);
    const gen = button("Generate", generate);
    const commonRow = row(field("Voice", ctrl.voice), field("Tempo", ctrl.tempo), field("Seed", ctrl.seed), reroll, reset, surprise, gen);

    controlsEl.append(
      field("Strategy", ctrl.strategy),
      ctrl.figureGroup, ctrl.placementGroup, ctrl.selectionGroup, ctrl.randomGroup, commonRow);
    sync();
  }

  function button(label, onClick) {
    const b = document.createElement("button");
    b.type = "button"; b.textContent = label;
    b.addEventListener("click", onClick);
    return b;
  }

  // Show only the fields relevant to the current strategy / selection.
  function sync() {
    const s = ctrl.strategy.value;
    const isFigure = s === "figure", isPattern = s === "pattern", isRandom = s === "random";
    ctrl.figureGroup.style.display = isFigure ? "flex" : "none";
    ctrl.placementGroup.style.display = isPattern ? "flex" : "none";
    ctrl.selectionGroup.style.display = isRandom ? "none" : "flex";
    ctrl.randomGroup.style.display = isRandom ? "flex" : "none";

    if (isPattern) {
      const sub = intVal(ctrl.subdivision, 2);
      showField(ctrl.region, sub >= 2); // quarter has no off-beat cells; on-beat == all — so region is eighth-only
      if (sub < 2) ctrl.region.value = "all";
      const avail = availCells(sub, ctrl.region.value);
      ctrl.onsetCount.max = String(avail);
      if (intVal(ctrl.onsetCount, 1) > avail) ctrl.onsetCount.value = String(avail);
    }

    const sel = ctrl.selection.value;
    showField(ctrl.selIndex, sel === "fixed" || sel === "cycle" || sel === "fixedPlusRotating");
    showField(ctrl.selRotIndex, sel === "fixedPlusRotating");
    const rest = ctrl.restBar._input.checked;
    showField(ctrl.restContent, rest);
    showField(ctrl.restRest, rest);
  }

  // Cells available for onsets at a subdivision × region (quarter off-beat = 0, so it's hidden for quarter).
  function availCells(subdivision, region) {
    const total = subdivision * 4;
    return region === "onbeat" ? 4 : region === "offbeat" ? total - 4 : total;
  }

  function showField(node, visible) {
    const el = node.parentElement && node.parentElement.tagName === "LABEL" ? node.parentElement : node;
    el.style.display = visible ? "" : "none";
  }

  // --- request assembly --------------------------------------------------
  function intVal(el, fallback) { const n = parseInt(el.value, 10); return Number.isFinite(n) ? n : fallback; }

  function buildSelection() {
    const kind = ctrl.selection.value;
    const sel = { kind };
    if (kind === "fixed" || kind === "cycle") sel.index = intVal(ctrl.selIndex, 0);
    if (kind === "fixedPlusRotating") { sel.index = intVal(ctrl.selIndex, 0); sel.rotatingIndex = intVal(ctrl.selRotIndex, 0); }
    return sel;
  }

  function buildBehaviours() {
    const bs = [];
    const disp = intVal(ctrl.displace, 0);
    if (disp > 0) bs.push({ kind: "displace", args: [disp] });
    if (ctrl.sweep._input.checked) bs.push({ kind: "sweep" });
    if (ctrl.restBar._input.checked) bs.push({ kind: "restBar", args: [intVal(ctrl.restContent, 1), intVal(ctrl.restRest, 1)] });
    if (ctrl.callResponse._input.checked) bs.push({ kind: "callResponse" });
    return bs;
  }

  function buildRequest() {
    const s = ctrl.strategy.value;
    const req = {
      strategy: s, seed: intVal(ctrl.seed, 0), voice: ctrl.voice.value, tempo: intVal(ctrl.tempo, 100),
      referencePulse: "beat1",
    };
    if (s === "figure") {
      req.figureId = ctrl.figureId.value;
      req.selection = buildSelection(); req.behaviours = buildBehaviours(); req.barCount = intVal(ctrl.barCount, 1);
    } else if (s === "pattern") {
      req.subdivision = intVal(ctrl.subdivision, 2); req.region = ctrl.region.value; req.onsetCount = intVal(ctrl.onsetCount, 1);
      req.selection = buildSelection(); req.behaviours = buildBehaviours(); req.barCount = intVal(ctrl.barCount, 1);
    } else {
      req.palette = ctrl.palette.filter((cb) => cb.checked).map((cb) => parseInt(cb.value, 10));
      req.contentBars = intVal(ctrl.contentBars, 1); req.silenceBars = intVal(ctrl.silenceBars, 0);
      req.restProbability = intVal(ctrl.restPct, 30) / 100;
    }
    // The router reads the request nested (envelope.RhythmGenerate).
    return { type: "rhythmGenerate", rhythmGenerate: req };
  }

  // Restore the modifier controls to their neutral defaults, so a figure plays its natural self (req IN7).
  // selection = Cycle(0) is neutral for both a 1-bar figure (== repeat) and a 2-bar clave (plays both bars).
  function resetModifiers() {
    ctrl.selection.value = "cycle";
    ctrl.selIndex.value = "0";
    ctrl.selRotIndex.value = "0";
    ctrl.displace.value = "0";
    ctrl.sweep._input.checked = false;
    ctrl.restBar._input.checked = false;
    ctrl.restContent.value = "1";
    ctrl.restRest.value = "0";
    ctrl.callResponse._input.checked = false;
    ctrl.barCount.value = "4";
    sync();
  }

  // Randomize all Pattern params (strategy figure/pattern) and generate.
  function surpriseMe() {
    ctrl.strategy.value = pick(["figure", "pattern"]);
    ctrl.figureId.value = pick(FIGURES)[0];
    const sub = pick([1, 2]);
    ctrl.subdivision.value = String(sub);
    ctrl.region.value = sub === 1 ? "all" : pick(REGIONS); // no off-beat at the quarter grid
    ctrl.onsetCount.value = String(randInt(1, availCells(sub, ctrl.region.value)));
    ctrl.selection.value = pick(SELECTIONS);
    ctrl.selIndex.value = String(randInt(0, 5));
    ctrl.selRotIndex.value = String(randInt(0, 5));
    ctrl.displace.value = String(Math.random() < 0.4 ? randInt(1, parseInt(ctrl.subdivision.value, 10) * 2) : 0);
    ctrl.sweep._input.checked = Math.random() < 0.25;
    ctrl.restBar._input.checked = Math.random() < 0.3;
    ctrl.callResponse._input.checked = Math.random() < 0.2;
    ctrl.barCount.value = String(randInt(2, 8));
    ctrl.seed.value = String(randInt(0, 99999));
    sync();
    generate();
  }

  function generate() {
    if (!Bridge.available) { setError("Open in the ChordFlow app to generate rhythms."); return; }
    setError("");
    Bridge.send(buildRequest());
  }

  function onChange() {
    sync();
    clearTimeout(debounceTimer);
    debounceTimer = setTimeout(generate, DEBOUNCE_MS);
  }

  function ensureScore() {
    if (scoreView || !window.ChordFlowScore) return;
    scoreView = window.ChordFlowScore.create(scoreEl, {
      player: true, controls: "full", transport: true,
      onStateChange: (playing) => { if (!playing && gridView) gridView.clearHighlight(); },
      onFinished: () => { if (gridView) gridView.clearHighlight(); },
    });
    const engine = scoreView.getEngine && scoreView.getEngine();
    if (engine) engine.on("position", (bar, quarterBeat) => {
      if (gridView) gridView.highlightCell(bar - 1, quarterBeat - 1);
    });
  }

  function onHostMessage(raw) {
    let msg;
    try { msg = JSON.parse(raw); } catch (e) { return; }
    if (msg.type === "rhythmGenerated") {
      setError("");
      if (!gridView && window.ChordFlowDrums) {
        gridView = window.ChordFlowDrums.create(gridEl, { theme: "light", countLabels: true });
      }
      if (gridView) gridView.render(msg.diagram);
      if (gridTextEl) gridTextEl.textContent = msg.grid || "";
      ensureScore();
      if (scoreView) scoreView.load(msg.tex, { tempo: intVal(ctrl.tempo, 100) });
    } else if (msg.type === "rhythmGenerateError") {
      setError(msg.message);
    }
  }

  function init() {
    controlsEl = $("rgControls");
    errorEl = $("rgError");
    gridEl = $("rgGrid");
    gridTextEl = $("rgGridText");
    scoreEl = $("rgScore");
    buildControls();
    if (Bridge.available) Bridge.onReceive(onHostMessage);
    initialized = true;
  }

  function show() {
    if (!initialized) init();
    generate();
  }

  return { show };
})();
