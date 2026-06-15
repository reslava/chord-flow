// ChordFlow Practice view.
//
// Owns the Practice generator UI (key/rhythm pickers, Generate, Save, Mark-practiced, the saved list)
// and hosts the shared ChordFlowScore render component (score-render-component.js) for notation +
// playback. The component owns alphaTab and the transport strip (Play/Stop/Tempo + toggles); this view
// only feeds it alphaTex and wires its callbacks back to the bridge.
//
// Each builder control posts a bridge envelope to its C# slice; inbound envelopes from the host
// (loadScore, exerciseList, practiceRecorded, status) drive the component and the UI. Render-producing
// requests (generate, loadExercise) carry the component's current renderOptions; a content-toggle change
// (onNeedsRerender) replays the last request with the new options.
//
// Transport is the shared window.ChordFlowBridge (bridge.js). With no host (plain browser) the bridge is
// unavailable, so the component renders a SAMPLE_TEX score directly and the DB-backed actions are no-ops.
// This module also wires the header Practice ⇄ Content view toggle.
"use strict";

const Bridge = window.ChordFlowBridge;

const ChordFlow = (function () {
  // Key names per tonic pitch class (0 = C .. 11 = B), spelled to match the
  // renderer's \ks. Used for the key picker and the saved-list labels.
  const KEY_NAMES = ["C", "Db", "D", "Eb", "E", "F", "F#", "G", "Ab", "A", "Bb", "B"];

  // The three MVP rhythm patterns (id + display name), matching SeedData.
  const RHYTHMS = [
    { id: "beat_1", name: "Beat 1" },
    { id: "beat_1_3", name: "Beats 1 & 3" },
    { id: "quarters", name: "Quarters" },
  ];

  // Browser-dev fallback only — in the app the host pushes the real score.
  // Matches AlphaTexRenderer's output for 12-bar blues in Bb, "Beats 1 & 3".
  const SAMPLE_TEX = [
    '\\title "12-Bar Blues — Bb"',
    '\\subtitle "Beginner — Beats 1 & 3"',
    "\\tempo 80",
    "\\ts 4 4",
    "\\ks bb",
    ".",
    ":4 (1.5 0.4 1.3) r (1.5 0.4 1.3) r |",
    "(1.5 0.4 1.3) r (1.5 0.4 1.3) r |",
    "(1.5 0.4 1.3) r (1.5 0.4 1.3) r |",
    "(1.5 0.4 1.3) r (1.5 0.4 1.3) r |",
    "(6.5 5.4 6.3) r (6.5 5.4 6.3) r |",
    "(6.5 5.4 6.3) r (6.5 5.4 6.3) r |",
    "(1.5 0.4 1.3) r (1.5 0.4 1.3) r |",
    "(1.5 0.4 1.3) r (1.5 0.4 1.3) r |",
    "(8.5 7.4 8.3) r (8.5 7.4 8.3) r |",
    "(6.5 5.4 6.3) r (6.5 5.4 6.3) r |",
    "(1.5 0.4 1.3) r (1.5 0.4 1.3) r |",
    "(8.5 7.4 8.3) r (8.5 7.4 8.3) r |",
  ].join("\n");

  let view = null;            // the ChordFlowScore handle (owns alphaTab + transport)
  let lastScoreRequest = null; // last render-producing envelope (sans renderOptions), for onNeedsRerender replay

  const $ = (id) => document.getElementById(id);
  const statusEl = () => $("status");
  function setStatus(text) {
    const el = statusEl();
    if (el) el.textContent = text;
  }

  // --- pickers -------------------------------------------------------------
  function populatePickers() {
    const keySel = $("key");
    if (keySel) {
      KEY_NAMES.forEach((name, pc) => {
        const o = document.createElement("option");
        o.value = String(pc);
        o.textContent = name;
        keySel.appendChild(o);
      });
      keySel.value = "10"; // Bb default, matching the host's boot score
    }

    const rSel = $("rhythm");
    if (rSel) {
      RHYTHMS.forEach((r) => {
        const o = document.createElement("option");
        o.value = r.id;
        o.textContent = r.name;
        rSel.appendChild(o);
      });
      rSel.value = "beat_1_3";
    }
  }

  function rhythmName(id) {
    const r = RHYTHMS.find((x) => x.id === id);
    return r ? r.name : id;
  }

  // The current builder selections — the payload for a generate envelope. Tempo comes from the
  // component's transport so the user's tempo choice authors the next generated exercise.
  function selections() {
    return {
      keyPitchClass: parseInt($("key").value, 10) || 0,
      rhythmId: $("rhythm").value || "beat_1_3",
      tempo: view ? view.getTempo() : 80,
    };
  }

  // Send a render-producing request with the component's current renderOptions attached, remembering it
  // so a content-toggle change can replay it. In browser-dev (no bridge) renders the sample directly.
  function sendScoreRequest(envelope) {
    lastScoreRequest = envelope;
    if (Bridge.available) {
      Bridge.send({ ...envelope, renderOptions: view.getRenderOptions() });
    } else if (view) {
      view.load(SAMPLE_TEX);
    }
  }

  // --- control wiring ------------------------------------------------------
  function setupControls() {
    const gen = $("btnGenerate");
    const save = $("btnSave");
    const practice = $("btnPractice");

    if (gen) {
      gen.addEventListener("click", () => sendScoreRequest({ type: "generate", ...selections() }));
    }
    if (save) {
      save.addEventListener("click", () => Bridge.send({ type: "save" }));
    }
    if (practice) {
      practice.addEventListener("click", () => Bridge.send({ type: "markPracticed" }));
    }
  }

  // --- view toggle (Practice ⇄ Content) ------------------------------------
  // Switches the two top-level views in the single page. Exposed as a tiny global
  // so the Content view (content-crud.js) can lazily initialize the first time it
  // is shown, without app.js depending on it.
  function setupViewToggle() {
    const navPractice = $("navPractice");
    const navContent = $("navContent");
    const practiceView = $("practice-view");
    const contentView = $("content-view");

    function show(viewName) {
      const content = viewName === "content";
      if (practiceView) practiceView.hidden = content;
      if (contentView) contentView.hidden = !content;
      if (navPractice) navPractice.classList.toggle("active", !content);
      if (navContent) navContent.classList.toggle("active", content);
      if (content && window.ChordFlowContent) window.ChordFlowContent.show();
    }

    if (navPractice) navPractice.addEventListener("click", () => show("practice"));
    if (navContent) navContent.addEventListener("click", () => show("content"));
    window.ChordFlowViews = { show };
  }

  // --- saved-exercise library ----------------------------------------------
  function libraryLabel(ex) {
    const key = KEY_NAMES[ex.key] !== undefined ? KEY_NAMES[ex.key] : ex.key;
    const base = `${key} · ${rhythmName(ex.rhythmId)} · ${ex.tempo} BPM`;
    // Mark practiced exercises with a ✓ and the count.
    return ex.practicedCount > 0 ? `${base}  ✅ ${ex.practicedCount}` : base;
  }

  function renderLibrary(exercises) {
    const ul = $("library");
    if (!ul) return;
    ul.innerHTML = "";

    if (!exercises || exercises.length === 0) {
      const li = document.createElement("li");
      li.className = "empty";
      li.textContent = "No saved exercises";
      ul.appendChild(li);
      return;
    }

    for (const ex of exercises) {
      const li = document.createElement("li");
      li.textContent = libraryLabel(ex);
      li.title = "Load this exercise";
      li.addEventListener("click", () => sendScoreRequest({ type: "loadExercise", id: ex.id }));
      ul.appendChild(li);
    }
  }

  // --- bridge: inbound envelope from the host (raw JSON string) -------------
  function onHostMessage(raw) {
    let msg;
    try {
      msg = JSON.parse(raw);
    } catch (e) {
      console.error("ChordFlow: malformed envelope from host:", raw, e);
      return;
    }

    switch (msg.type) {
      case "loadScore":
        view.load(msg.tex, { tempo: msg.tempo });
        setStatus("score loaded");
        break;
      case "exerciseList":
        renderLibrary(msg.exercises);
        break;
      case "practiceRecorded":
        setStatus(`practiced ✓ — recorded ${msg.count}×`);
        break;
      case "status":
        setStatus(msg.text);
        if (msg.isError) console.error("ChordFlow host:", msg.text);
        break;
      // play/stop/setTempo are no longer host-driven — the ChordFlowScore component owns alphaTab transport.
      // Other envelope types (entityList/entityPreview/… for the Content view) fan out to every receiver via
      // the shared bridge; this view simply ignores the ones it doesn't own.
    }
  }

  function init() {
    populatePickers();

    if (typeof alphaTab === "undefined" || !window.ChordFlowScore) {
      setStatus("alphaTab/render component failed to load");
      console.error("alphaTab global or ChordFlowScore not found — check wwwroot bundling.");
      return;
    }

    // The render component owns alphaTab + the transport strip; this view feeds it alphaTex and reacts to
    // its callbacks. Position echoes (beatChanged/playbackFinished) keep the C# PracticeSession seam fed;
    // a content-toggle change replays the last render request with the new renderOptions.
    view = window.ChordFlowScore.create($("score-pane"), {
      player: true,
      controls: "full",
      onBeat: (bar, beat) => { if (Bridge.available) Bridge.send({ type: "beatChanged", bar, beat }); },
      onFinished: () => { if (Bridge.available) Bridge.send({ type: "playbackFinished" }); },
      onNeedsRerender: (renderOptions) => {
        if (Bridge.available && lastScoreRequest) {
          Bridge.send({ ...lastScoreRequest, renderOptions });
        }
      },
    });

    setupControls();
    setupViewToggle();

    if (Bridge.available) {
      // Register the inbound handler BEFORE announcing ready, or we could miss
      // the host's loadScore reply.
      Bridge.onReceive(onHostMessage);
      // Seed the replay target with the boot exercise (the host pushes Bb / Beats 1 & 3 on ready). Without
      // this a content-toggle change before the first Generate/Load would have nothing to re-render.
      lastScoreRequest = { type: "generate", ...selections() };
      // Carry the component's default render options on ready so the boot score reflects the checked
      // toggles (names + on-top) instead of a neutral render.
      Bridge.send({ type: "ready", renderOptions: view.getRenderOptions() });
      setStatus("waiting for score…");
    } else {
      // Standalone browser: no host to push a score — render the dev sample.
      view.load(SAMPLE_TEX);
      setStatus("score loaded (dev fallback)");
    }
  }

  return { init, getView: () => view };
})();

if (document.readyState === "loading") {
  document.addEventListener("DOMContentLoaded", ChordFlow.init);
} else {
  ChordFlow.init();
}
