// ChordFlow Rhythm Generator view — the generation-engine dogfood page.
//
// Pick a strategy + params; the host (RhythmGenerateHandler) generates an onset grid, projects it to a
// single-voice drum groove, and returns the percussion tex (played by the shared ScoreR, looping by default)
// + the grid model (drawn by DrumsR with the "1 e & a" overlay) + an onset-ASCII debug string. A Beat-1
// reference row anchors the ear. Generations are EPHEMERAL (no save/library).
//
// Pattern strategy (v2): a KIND of bar patterns (density/placement family or a named figure) drawn across
// bars by a SELECTION (fixed/cycle/randomInKind/fixedPlusRotating), with behaviours (displace/sweep/restBar/
// callResponse). A DUMB page: it assembles the request and draws the reply; all rhythm theory lives in Core.
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
  const KIND_SOURCES = ["figure", "density", "placement"];
  const SUBDIVISIONS = [["1", "Quarter"], ["2", "Eighth"]];
  const REGIONS = ["all", "onbeat", "offbeat"];
  const SELECTIONS = ["cycle", "fixed", "randomInKind", "fixedPlusRotating"];
  const VOICES = ["HH", "SD", "BD", "OH", "RD", "CC"];
  const PALETTE_VALUES = [4, 8, 16];

  let initialized = false;
  let scoreView = null;
  let gridView = null;
  let controlsEl, errorEl, gridEl, gridTextEl, scoreEl;
  let ctrl = {};
  let debounceTimer = null;

  function setError(text) { if (errorEl) errorEl.textContent = text || ""; }

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

  function buildControls() {
    controlsEl.innerHTML = "";
    ctrl.strategy = select(["pattern", "random"], "pattern");

    // Pattern group — Kind (source + params) · Selection · Behaviours · Bars.
    ctrl.kindSource = select(KIND_SOURCES, "figure");
    ctrl.figureId = select(FIGURES, "tresillo");
    ctrl.subdivision = select(SUBDIVISIONS, "2");
    ctrl.onsetCount = number(2, 1, 4);
    ctrl.region = select(REGIONS, "all");
    ctrl.selection = select(SELECTIONS, "cycle");
    ctrl.selIndex = number(0, 0, 63);
    ctrl.displace = number(0, 0, 8);
    ctrl.sweep = checkbox("Sweep", false);
    ctrl.restBar = checkbox("RestBar", false);
    ctrl.restContent = number(1, 1, 4);
    ctrl.restRest = number(1, 0, 4);
    ctrl.callResponse = checkbox("Call/Resp", false);
    ctrl.barCount = number(2, 1, 4);

    ctrl.patternGroup = row(
      field("Kind", ctrl.kindSource), field("Figure", ctrl.figureId),
      field("Subdiv", ctrl.subdivision), field("Onsets", ctrl.onsetCount), field("Region", ctrl.region),
      field("Selection", ctrl.selection), field("Index", ctrl.selIndex),
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
    const reroll = document.createElement("button");
    reroll.type = "button"; reroll.textContent = "Reroll";
    reroll.addEventListener("click", () => { ctrl.seed.value = String(Math.floor(Math.random() * 100000)); generate(); });
    const gen = document.createElement("button");
    gen.type = "button"; gen.textContent = "Generate";
    gen.addEventListener("click", generate);
    const commonRow = row(field("Voice", ctrl.voice), field("Tempo", ctrl.tempo), field("Seed", ctrl.seed), reroll, gen);

    controlsEl.append(field("Strategy", ctrl.strategy), ctrl.patternGroup, ctrl.randomGroup, commonRow);
    sync();
  }

  function row(...children) {
    const d = document.createElement("div");
    d.style.cssText = "display:flex; gap:.75rem; flex-wrap:wrap; align-items:flex-end;";
    d.append(...children);
    return d;
  }

  // Show only the fields relevant to the current strategy / kind source / selection.
  function sync() {
    const pattern = ctrl.strategy.value === "pattern";
    ctrl.patternGroup.style.display = pattern ? "flex" : "none";
    ctrl.randomGroup.style.display = pattern ? "none" : "flex";

    const source = ctrl.kindSource.value;
    showField(ctrl.figureId, source === "figure");
    showField(ctrl.subdivision, source !== "figure");
    showField(ctrl.onsetCount, source !== "figure");
    showField(ctrl.region, source === "placement");
    const sel = ctrl.selection.value;
    showField(ctrl.selIndex, sel === "fixed" || sel === "fixedPlusRotating");
    const rest = ctrl.restBar._input.checked;
    showField(ctrl.restContent, rest);
    showField(ctrl.restRest, rest);
  }

  function showField(node, visible) {
    const el = node.parentElement && node.parentElement.tagName === "LABEL" ? node.parentElement : node;
    el.style.display = visible ? "" : "none";
  }

  // --- request assembly --------------------------------------------------
  function intVal(el, fallback) { const n = parseInt(el.value, 10); return Number.isFinite(n) ? n : fallback; }

  function buildKind() {
    const source = ctrl.kindSource.value;
    if (source === "figure") return { source: "figure", figureId: ctrl.figureId.value };
    const kind = { source, subdivision: intVal(ctrl.subdivision, 2), onsetCount: intVal(ctrl.onsetCount, 1) };
    if (source === "placement") kind.region = ctrl.region.value;
    return kind;
  }

  function buildSelection() {
    const kind = ctrl.selection.value;
    const sel = { kind };
    if (kind === "fixed" || kind === "fixedPlusRotating") sel.index = intVal(ctrl.selIndex, 0);
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
    const req = {
      strategy: ctrl.strategy.value,
      seed: intVal(ctrl.seed, 0), voice: ctrl.voice.value, tempo: intVal(ctrl.tempo, 100),
      // The rhythm is the only sound here, so anchor the ear with an implicit (non-generated) beat-1 reference.
      referencePulse: "beat1",
    };
    if (ctrl.strategy.value === "pattern") {
      req.kind = buildKind();
      req.selection = buildSelection();
      req.behaviours = buildBehaviours();
      req.barCount = intVal(ctrl.barCount, 1);
    } else {
      req.palette = ctrl.palette.filter((cb) => cb.checked).map((cb) => parseInt(cb.value, 10));
      req.contentBars = intVal(ctrl.contentBars, 1);
      req.silenceBars = intVal(ctrl.silenceBars, 0);
      req.restProbability = intVal(ctrl.restPct, 30) / 100;
    }
    // The router reads the request nested (envelope.RhythmGenerate).
    return { type: "rhythmGenerate", rhythmGenerate: req };
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
