// ChordFlow Rhythm Generator view — the generation-engine dogfood page (Phase 2).
//
// Pick a strategy + params; the host (RhythmGenerateHandler) generates an onset grid, projects it to a
// single-voice drum groove, and returns the percussion tex (played by the shared ScoreR) + the grid model
// (drawn by DrumsR with the "1 e & a" count overlay) + an onset-ASCII debug string. Generations are
// EPHEMERAL (req EX1) — there is no save/library here (that lands with the "save into exercise" phase).
// Named-trainer presets and the reference pulse are Phase 3; this page exposes the raw params.
//
// A DUMB page: it only assembles the request and draws the reply. All rhythm theory lives in Core.
"use strict";

window.ChordFlowRhythmGen = (function () {
  const Bridge = window.ChordFlowBridge;
  const DEBOUNCE_MS = 200;
  const $ = (id) => document.getElementById(id);

  const OPERATORS = ["uniform", "isolate", "anchorRotate", "mask", "displace", "accumulate", "thin"];
  const BEHAVIOURS = ["repeat", "cycle", "sweep", "restBar", "callResponse"];
  const FAMILIES = ["quarter", "eighth"];
  const VOICES = ["HH", "SD", "BD", "OH", "RD", "CC"];
  const PALETTE_VALUES = [4, 8, 16]; // alphaTex note values: quarter / eighth / sixteenth
  const ARG_OPERATORS = { isolate: "Beat (0–3)", displace: "Cells", accumulate: "Count", thin: "Count" };

  let initialized = false;
  let scoreView = null; // ChordFlowScore (notation + transport)
  let gridView = null;  // ChordFlowDrums (DrumsR) with count overlay
  let controlsEl, errorEl, gridEl, gridTextEl, scoreEl;
  let ctrl = {};        // the built control elements, by key
  let debounceTimer = null;

  function setError(text) { if (errorEl) errorEl.textContent = text || ""; }

  // --- control builders --------------------------------------------------
  function field(labelText, node) {
    const wrap = document.createElement("label");
    wrap.style.cssText = "display:flex; flex-direction:column; gap:.15rem; font-size:.8rem; color:#5a5f64;";
    const span = document.createElement("span");
    span.textContent = labelText;
    wrap.appendChild(span);
    wrap.appendChild(node);
    return wrap;
  }

  function select(options, initial) {
    const s = document.createElement("select");
    for (const o of options) {
      const opt = document.createElement("option");
      opt.value = o; opt.textContent = o;
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

  function text(value) {
    const n = document.createElement("input");
    n.type = "text"; n.value = value; n.style.width = "5rem";
    n.addEventListener("input", onChange);
    return n;
  }

  function buildControls() {
    controlsEl.innerHTML = "";
    ctrl.strategy = select(["pattern", "random"], "pattern");

    // Pattern group
    ctrl.family = select(FAMILIES, "eighth");
    ctrl.operator = select(OPERATORS, "anchorRotate");
    ctrl.operatorArg = number(0, 0, 8);
    ctrl.maskBeats = text("1,3");
    ctrl.behaviour = select(BEHAVIOURS, "cycle");
    ctrl.restContent = number(1, 1, 4);
    ctrl.restRest = number(1, 0, 4);
    ctrl.barCount = number(2, 1, 4);

    // Random group
    ctrl.palette = PALETTE_VALUES.map((v) => {
      const cb = document.createElement("input");
      cb.type = "checkbox"; cb.value = v; cb.checked = v !== 16;
      cb.addEventListener("change", onChange);
      return cb;
    });
    ctrl.contentBars = number(2, 1, 4);
    ctrl.silenceBars = number(0, 0, 4);

    // Rest % slider — how often a drawn slot is a rest vs. an onset (req IN12).
    ctrl.restPct = document.createElement("input");
    ctrl.restPct.type = "range"; ctrl.restPct.min = "0"; ctrl.restPct.max = "80"; ctrl.restPct.value = "30";
    ctrl.restPct.style.width = "6rem";
    ctrl.restPctVal = document.createElement("span");
    ctrl.restPctVal.style.cssText = "font-size:.75rem; min-width:2.6rem;";
    const syncRestPct = () => { ctrl.restPctVal.textContent = ctrl.restPct.value + "%"; };
    ctrl.restPct.addEventListener("input", () => { syncRestPct(); onChange(); });
    syncRestPct();

    // Common
    ctrl.voice = select(VOICES, "HH");
    ctrl.tempo = number(100, 40, 240);
    ctrl.seed = number(1, 0);

    const reroll = document.createElement("button");
    reroll.type = "button"; reroll.textContent = "Reroll";
    reroll.addEventListener("click", () => { ctrl.seed.value = String(Math.floor(Math.random() * 100000)); generate(); });

    const gen = document.createElement("button");
    gen.type = "button"; gen.textContent = "Generate";
    gen.addEventListener("click", generate);

    // Layout — two named groups + a common row; contextual fields toggle in sync().
    ctrl.patternGroup = document.createElement("div");
    ctrl.patternGroup.style.cssText = "display:flex; gap:.75rem; flex-wrap:wrap; align-items:flex-end;";
    ctrl.patternGroup.append(
      field("Family", ctrl.family), field("Operator", ctrl.operator),
      field("Arg", ctrl.operatorArg), field("Mask beats", ctrl.maskBeats),
      field("Behaviour", ctrl.behaviour), field("Content", ctrl.restContent),
      field("Rest", ctrl.restRest), field("Bars", ctrl.barCount));

    ctrl.randomGroup = document.createElement("div");
    ctrl.randomGroup.style.cssText = "display:flex; gap:.75rem; flex-wrap:wrap; align-items:flex-end;";
    const paletteWrap = document.createElement("div");
    paletteWrap.style.cssText = "display:flex; gap:.5rem; align-items:center;";
    PALETTE_VALUES.forEach((v, i) => {
      const l = document.createElement("label");
      l.style.cssText = "display:flex; gap:.2rem; align-items:center; font-size:.8rem;";
      l.append(ctrl.palette[i], document.createTextNode(v === 4 ? "♩" : v === 8 ? "♪" : "𝅘𝅥𝅯"));
      paletteWrap.appendChild(l);
    });
    const restWrap = document.createElement("div");
    restWrap.style.cssText = "display:flex; align-items:center; gap:.4rem;";
    restWrap.append(ctrl.restPct, ctrl.restPctVal);
    ctrl.randomGroup.append(
      field("Palette", paletteWrap), field("Content bars", ctrl.contentBars),
      field("Silence bars", ctrl.silenceBars), field("Rest", restWrap));

    const commonRow = document.createElement("div");
    commonRow.style.cssText = "display:flex; gap:.75rem; flex-wrap:wrap; align-items:flex-end;";
    commonRow.append(field("Voice", ctrl.voice), field("Tempo", ctrl.tempo), field("Seed", ctrl.seed), reroll, gen);

    controlsEl.append(field("Strategy", ctrl.strategy), ctrl.patternGroup, ctrl.randomGroup, commonRow);
    sync();
  }

  // Show only the fields relevant to the current strategy/operator/behaviour.
  function sync() {
    const pattern = ctrl.strategy.value === "pattern";
    ctrl.patternGroup.style.display = pattern ? "flex" : "none";
    ctrl.randomGroup.style.display = pattern ? "none" : "flex";
    const argLabel = ARG_OPERATORS[ctrl.operator.value];
    ctrl.operatorArg.parentElement.style.display = argLabel ? "flex" : "none";
    if (argLabel) ctrl.operatorArg.parentElement.firstChild.textContent = argLabel;
    ctrl.maskBeats.parentElement.style.display = ctrl.operator.value === "mask" ? "flex" : "none";
    const isRest = ctrl.behaviour.value === "restBar";
    ctrl.restContent.parentElement.style.display = isRest ? "flex" : "none";
    ctrl.restRest.parentElement.style.display = isRest ? "flex" : "none";
  }

  // --- request assembly --------------------------------------------------
  function intVal(el, fallback) { const n = parseInt(el.value, 10); return Number.isFinite(n) ? n : fallback; }

  function buildOperator() {
    const kind = ctrl.operator.value;
    if (kind === "mask") {
      const args = ctrl.maskBeats.value.split(",").map((s) => parseInt(s.trim(), 10)).filter(Number.isFinite);
      return { kind, args };
    }
    if (ARG_OPERATORS[kind]) return { kind, args: [intVal(ctrl.operatorArg, 0)] };
    return { kind, args: null };
  }

  function buildBehaviour() {
    const kind = ctrl.behaviour.value;
    if (kind === "restBar") return { kind, args: [intVal(ctrl.restContent, 1), intVal(ctrl.restRest, 1)] };
    return { kind, args: null };
  }

  function buildRequest() {
    const req = {
      strategy: ctrl.strategy.value,
      seed: intVal(ctrl.seed, 0), voice: ctrl.voice.value, tempo: intVal(ctrl.tempo, 100),
      // On this page the rhythm is the only sound, so anchor the ear with an implicit (non-generated) beat-1
      // reference; in Practice the song is the reference, so integration will omit it (req IN8).
      referencePulse: "beat1",
    };
    if (ctrl.strategy.value === "pattern") {
      Object.assign(req, {
        family: ctrl.family.value, operator: buildOperator(),
        behaviour: buildBehaviour(), barCount: intVal(ctrl.barCount, 1),
      });
    } else {
      req.palette = ctrl.palette.filter((cb) => cb.checked).map((cb) => parseInt(cb.value, 10));
      req.contentBars = intVal(ctrl.contentBars, 1);
      req.silenceBars = intVal(ctrl.silenceBars, 0);
      req.restProbability = intVal(ctrl.restPct, 30) / 100;
    }
    // The router reads the request as a NESTED object (envelope.RhythmGenerate), so send it nested — not flat.
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

  // Lazily create the shared ScoreR (percussion notation + play/stop) and drive the DrumsR marker off the
  // engine's time-linear "position" clock (bar/quarterBeat 1-based → DrumsR 0-based cell).
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

  // Called by the view toggle when the Rhythm Generator tab is shown — lazily inits, then generates once.
  function show() {
    if (!initialized) init();
    generate();
  }

  return { show };
})();
