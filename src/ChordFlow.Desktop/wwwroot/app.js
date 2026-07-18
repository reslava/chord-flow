// ChordFlow Practice view — ONE page, two view surfaces (harmony-controls-r).
//
// The shell of the single Practice page: it mounts the shared components ONCE and swaps only the view surface —
//   • HarmonyControlsR (harmony-controls-component.js) — the definition strip (harmony/key/feel/comping+vol/
//     lead+vol/difficulty/voicing window/Generate/Save/Mark practiced), fed the entity* catalog.
//   • a page-level transport strip: the Score ⇄ Sheet segmented toggle + PlayerControlsR bound to ScoreR's
//     engine (ScoreR is created with transport:false — the transport must survive the view toggle).
//   • ChordFlowNowNext — the now/next chord fretboards, view-independent.
//   • the Score view: ScoreR (staff/notation toggles + debug panel + alphaTab), volumes/key/feel off — those
//     live in HarmonyControlsR now.
//   • the Sheet view: ChordFlowSheetView (chord-sheets.js) — sheet display strip + ChordSheetR + exports.
//
// ONE render-producing reply feeds both views (IN3): loadScore carries { tex, tempo, key, tripletFeel,
// schedule, sheet, cellSchedule } — the score and the chord sheet are projections of the same Exercise pass.
// The view toggle collapses (max-height:0) rather than display:none's the hidden surface, so alphaTab keeps
// its layout width and the toggle works MID-PLAYBACK (IN7/C4): audio continues, both markers keep tracking.
//
// Transport is the shared window.ChordFlowBridge (bridge.js). With no host (plain browser) the bridge is
// unavailable, so the component renders a SAMPLE_TEX score directly and the DB-backed actions are no-ops.
// This module also wires the header view toggle (Practice ⇄ Content ⇄ …).
"use strict";

const Bridge = window.ChordFlowBridge;

const ChordFlow = (function () {
  // The boot definition the host renders on ready (12-bar blues, C, Beats 1 & 3). Mirrored here as the
  // generate-envelope defaults so an early content-toggle replay (before the catalog loads) is still valid.
  const BOOT_REQUEST = {
    type: "generate",
    harmonyEntity: "progression", harmonyId: "12bar_blues",
    compingPatternId: "beat_1_3", leadPatternId: null,
    keyPitchClass: 0, tempo: 80, difficulty: "Beginner", tripletFeel: "None",
  };

  // Browser-dev fallback only — in the app the host pushes the real score.
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

  let view = null;             // ScoreR handle (owns alphaTab + the one page engine)
  let pc = null;               // page-level PlayerControlsR, bound to ScoreR's engine (survives the view toggle)
  let hc = null;               // HarmonyControlsR — the definition strip
  let sheetView = null;        // ChordFlowSheetView — the Sheet view surface
  let nowNext = null;          // ChordFlowNowNext (the now/next chord fretboards, synced to playback)
  let lastScoreRequest = null; // last render-producing envelope (sans renderOptions), for replay

  // Content catalog, populated from the entity* bridge — feeds HarmonyControlsR and the library's name map.
  const catalog = { progression: [], song: [], rhythm: [] };

  const $ = (id) => document.getElementById(id);
  const statusEl = () => $("status");
  function setStatus(text) {
    const el = statusEl();
    if (el) el.textContent = text;
  }

  // --- the definition → request plumbing -------------------------------------
  // The automatic comping-voicing region from the definition (engine-derived-as-app-source IN14), sent on
  // renderOptions.voicing — the engine derives Closest grips within [minFret, maxFret].
  function voicingOf(def) {
    return { kind: "automatic", minFret: def.voicingMinFret, maxFret: def.voicingMaxFret };
  }

  // Send a render-producing request with the component's current renderOptions + the voicing region attached,
  // remembering it so a live-param change can replay it. In browser-dev (no bridge) renders the sample.
  function sendScoreRequest(envelope) {
    lastScoreRequest = envelope;
    if (Bridge.available) {
      const def = hc.getDefinition();
      Bridge.send({ ...envelope, renderOptions: { ...view.getRenderOptions(), voicing: voicingOf(def) } });
    } else if (view) {
      view.load(SAMPLE_TEX);
    }
  }

  // Replay the last request with the CURRENT definition's live params (key/feel/voicing) + renderOptions.
  // The one re-render path shared by HarmonyControlsR's live params and ScoreR's content-kind toggles: key is
  // a transpose re-emit, feel is the \tf line, the voicing window re-voices the comping grips.
  function replayScoreRequest(renderOptions) {
    if (!Bridge.available || !lastScoreRequest) return;
    const def = hc.getDefinition();
    Bridge.send({
      ...lastScoreRequest,
      keyPitchClass: def.keyPitchClass,
      keyIsMinor: def.keyIsMinor,
      tripletFeel: def.tripletFeel,
      renderOptions: { ...(renderOptions || view.getRenderOptions()), voicing: voicingOf(def) },
    });
  }

  // Generate: the definition becomes the generate envelope; tempo rides from the page transport so the user's
  // tempo choice authors the next generated exercise (C1 — tempo is a PlayerControlsR param).
  function onGenerate(def) {
    sendScoreRequest({
      type: "generate",
      harmonyEntity: def.harmonyEntity,
      harmonyId: def.harmonyId,
      compingPatternId: def.compingPatternId,
      leadPatternId: def.leadPatternId,
      keyPitchClass: def.keyPitchClass,
      keyIsMinor: def.keyIsMinor,
      tempo: (pc && pc.getTempo()) || 80,
      difficulty: def.difficulty,
      tripletFeel: def.tripletFeel,
    });
  }

  // Switching the harmony adopts that piece's tempo onto the page transport (song → DefaultTempo, else 80) —
  // a seed only, no re-render (Generate applies it). Key/Feel seed INSIDE HarmonyControlsR; tempo is the
  // shell's job because PlayerControlsR owns it (C1).
  function onHarmonySwitch(item) {
    const tempo = item && item.defaultTempo != null ? item.defaultTempo : 80;
    view.seedTempo(tempo);           // base tempo for the next load/generate
    if (pc) pc.setTempoValue(tempo); // reflect it in the page transport input
  }

  // --- catalog ---------------------------------------------------------------
  // Ask the host for the content lists that feed HarmonyControlsR (and the library's name map).
  function requestCatalog() {
    for (const entity of ["progression", "song", "rhythm"]) {
      Bridge.send({ type: "entityList", entity });
    }
  }

  // A catalog list arrived — cache it, feed HarmonyControlsR (the single population path, IN8), and re-label
  // the library (whose names resolve against the catalog). entityList also fans out from the Content view's
  // own requests; harmless to re-apply.
  function onCatalogList(entity, items) {
    if (!(entity in catalog)) return;
    catalog[entity] = items || [];
    if (hc) hc.setCatalog(entity, items || []);
    if (lastLibrary) renderLibrary(lastLibrary); // re-label with freshly resolved names
  }

  // --- Score ⇄ Sheet view toggle (IN2/IN7) ------------------------------------
  // Swaps ONLY the view surface + its view-specific strip; engine, definition, schedules, Now/Next and the
  // library are untouched — so toggling mid-playback just changes how you look at the same run (IN7). The
  // hidden surface is COLLAPSED (max-height:0, width kept) so alphaTab never re-measures while hidden (C4).
  function buildViewToggle(container) {
    const wrap = document.createElement("span");
    wrap.className = "view-toggle";
    const buttons = {};
    for (const [name, label] of [["score", "Score"], ["sheet", "Sheet"]]) {
      const b = document.createElement("button");
      b.type = "button";
      b.textContent = label;
      b.addEventListener("click", () => showSurface(name));
      buttons[name] = b;
      wrap.appendChild(b);
    }
    function showSurface(name) {
      $("score-pane").classList.toggle("view-collapsed", name !== "score");
      $("sheet-pane").classList.toggle("view-collapsed", name !== "sheet");
      for (const [n, b] of Object.entries(buttons)) b.classList.toggle("active", n === name);
    }
    container.appendChild(wrap);
    showSurface("score");
  }

  // --- top-level view toggle (Practice ⇄ Content ⇄ diagnostics pages) ---------
  function setupViewToggle() {
    let voicingsView = null; // lazy GuitarVoicingsR handle (created on first show of the Voicings tab)
    // The top-level views in the single page; each lazily inits its module on first show.
    const views = {
      practice: { nav: $("navPractice"), el: $("practice-view") },
      content: { nav: $("navContent"), el: $("content-view"),
        onShow: () => window.ChordFlowContent && window.ChordFlowContent.show() },
      scales: { nav: $("navScales"), el: $("scales-view"),
        onShow: () => window.ChordFlowScales && window.ChordFlowScales.show() },
      caged: { nav: $("navCaged"), el: $("caged-shapes-view"),
        onShow: () => window.ChordFlowCagedShapes && window.ChordFlowCagedShapes.show() },
      cagedChords: { nav: $("navCagedChords"), el: $("caged-chords-view"),
        onShow: () => window.ChordFlowCagedChords && window.ChordFlowCagedChords.show() },
      voicings: { nav: $("navVoicings"), el: $("voicings-view"),
        onShow: () => {
          if (!voicingsView && window.ChordFlowGuitarVoicings)
            voicingsView = window.ChordFlowGuitarVoicings.create($("voicings-mount"));
          if (voicingsView) voicingsView.show();
        } },
      voicingsEngine: { nav: $("navVoicingsEngine"), el: $("voicings-engine-view"),
        onShow: () => window.ChordFlowVoicingsEngine && window.ChordFlowVoicingsEngine.show() },
    };

    function show(viewName) {
      const target = views[viewName] ? viewName : "practice";
      // Changing pages silences audio: stop every live player engine (Practice, Content preview, …) before
      // switching, so a score left playing doesn't keep sounding on the page you left. (The Score ⇄ Sheet
      // toggle INSIDE Practice deliberately does NOT do this — same page, same run, different view.)
      if (window.ChordFlowPlayback) window.ChordFlowPlayback.stopAll();
      for (const [name, v] of Object.entries(views)) {
        const active = name === target;
        if (v.el) v.el.hidden = !active;
        if (v.nav) v.nav.classList.toggle("active", active);
        if (active && v.onShow) v.onShow();
      }
      // Returning to Practice: the user may have authored content in the meantime — refresh the pickers.
      if (target === "practice" && Bridge.available) requestCatalog();
    }

    for (const [name, v] of Object.entries(views)) {
      if (v.nav) v.nav.addEventListener("click", () => show(name));
    }
    window.ChordFlowViews = { show };
  }

  // --- saved-exercise library ----------------------------------------------
  let lastLibrary = null; // last exerciseList payload, re-rendered when the catalog (name map) updates

  // Resolve a harmony id to a display name from the catalog (Song first, then Progression), else the raw id.
  function harmonyName(id) {
    return (catalog.song.find((s) => s.id === id) || catalog.progression.find((p) => p.id === id))?.name || id;
  }
  function rhythmName(id) {
    return catalog.rhythm.find((r) => r.id === id)?.name || id;
  }
  function keyLabel(token) {
    return token ? token.charAt(0).toUpperCase() + token.slice(1) : "—";
  }

  function libraryLabel(ex) {
    const parts = [harmonyName(ex.songId), rhythmName(ex.compingPatternId), keyLabel(ex.keyOverride),
      `${ex.tempo} BPM`, ex.difficulty];
    if (ex.leadPatternId) parts.push(`+lead ${rhythmName(ex.leadPatternId)}`);
    const base = parts.join(" · ");
    return ex.practicedCount > 0 ? `${base}  ✅ ${ex.practicedCount}` : base;
  }

  function renderLibrary(exercises) {
    lastLibrary = exercises;
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
      case "loadScore": {
        // ONE reply, both projections (IN3): the score view loads the tex, the Sheet view renders the sheet
        // model + takes its marker schedule, Now/Next takes the chord schedule — nothing can drift.
        view.load(msg.tex, { tempo: msg.tempo });
        if (pc) pc.setTempoValue(msg.tempo);
        // Seed the definition controls from the piece the host rendered: a loaded exercise shows its persisted
        // key/feel (override wins over content defaults, C3). Seeds only — no re-render.
        if (msg.key != null) hc.seedKey(msg.key);
        if (msg.keyIsMinor != null) hc.seedKeyMode(msg.keyIsMinor); // a saved minor exercise reopens minor (IN5)
        if (msg.tripletFeel) hc.seedTripletFeel(msg.tripletFeel);
        if (nowNext) nowNext.setSchedule(msg.schedule);
        if (sheetView) {
          const def = hc.getDefinition();
          sheetView.render(msg.sheet, harmonyName(def.harmonyId)); // name = the export filename base
          sheetView.setSchedule(msg.cellSchedule);
        }
        setStatus("score loaded");
        break;
      }
      case "exerciseList":
        renderLibrary(msg.exercises);
        break;
      case "entityList":
        // The catalog feeds HarmonyControlsR; the Content view owns the editor's own use of this envelope.
        onCatalogList(msg.entity, msg.items);
        break;
      case "practiceRecorded":
        setStatus(`practiced ✓ — recorded ${msg.count}×`);
        break;
      case "status":
        setStatus(msg.text);
        if (msg.isError) console.error("ChordFlow host:", msg.text);
        break;
      // play/stop/setTempo are not host-driven — the page engine owns the alphaTab transport.
      // Other Content-view envelopes (entityPreview/entityLoaded/…) fan out here and are ignored.
    }
  }

  function init() {
    if (typeof alphaTab === "undefined" || !window.ChordFlowScore) {
      setStatus("alphaTab/render component failed to load");
      console.error("alphaTab global or ChordFlowScore not found — check wwwroot bundling.");
      return;
    }

    // The Score view: ScoreR keeps the staff/notation toggles + debug panel + the ONE page engine, but its
    // transport, volumes, key and feel are page concerns now (PlayerControlsR / HarmonyControlsR).
    view = window.ChordFlowScore.create($("score-pane"), {
      player: true,
      controls: "full",
      transport: false,   // the shell mounts PlayerControlsR at page level (it must survive the view toggle)
      volumes: false,     // Rhythm/Lead sliders live in HarmonyControlsR, next to their voice (C2)
      scroll: true,       // auto-follow the cursor: bound the staff + scroll it so the played bar stays under Now/Next
      debugPanel: true,   // the alphaTex scratchpad lives on the score component
      onBeat: (bar, beat) => {
        // The engine reports 1-based (bar, beat). Fan the EVENT signal out to every event-shaped surface —
        // BOTH views' markers track even while hidden, so a mid-playback Score ⇄ Sheet toggle is seamless
        // (IN7). The sheet's Visual-metronome mode ignores this — it follows the "position" time clock below.
        if (nowNext) nowNext.onBeat(bar - 1, beat - 1); // chord schedule is 0-based (alphaTab raw)
        if (sheetView) sheetView.onBeat(bar, beat);      // Sheet view steps down internally (Per-chord mode)
        if (Bridge.available) Bridge.send({ type: "beatChanged", bar, beat });
      },
      onFinished: () => {
        if (nowNext) nowNext.reset();          // back to the first chord on stop / end (schedule kept for replay)
        if (sheetView) sheetView.clearMarker();
        if (Bridge.available) Bridge.send({ type: "playbackFinished" });
      },
      onToggleNowNext: (visible) => {
        // The transport owns the toggle but not the boards — flip the pane it doesn't see.
        const pane = $("now-next-pane");
        if (pane) pane.hidden = !visible;
      },
      onNeedsRerender: (renderOptions) => replayScoreRequest(renderOptions),
    });

    // The TIME-clock fan-out (metronome-true-marker): the engine's PlaybackClock emits "position" — one
    // even step per quarter, silence or note — and the sheet's Visual-metronome marker follows it. Wired
    // page-level via the engine handle (ScoreR needs no passthrough opt), like the volume sliders.
    view.getEngine().on("position", (bar, quarterBeat) => {
      if (sheetView) sheetView.onPosition(bar, quarterBeat);
    });

    // Page-level transport strip: the Score ⇄ Sheet toggle at its head, then PlayerControlsR bound to ScoreR's
    // engine — ONE engine, one transport, alive across the view toggle (IN6/IN7).
    const transportStrip = $("transport-strip");
    buildViewToggle(transportStrip);
    pc = window.ChordFlowPlayerControls.create(transportStrip, view.getEngine(), {
      onToggleNowNext: (visible) => { const pane = $("now-next-pane"); if (pane) pane.hidden = !visible; },
    });

    // The definition strip (HarmonyControlsR): owns harmony/key/feel/comping/lead/difficulty/voicing window +
    // the actions; volume sliders bind to the same page engine.
    hc = window.ChordFlowHarmonyControls.create($("harmony-controls"), {
      engine: view.getEngine(),
      onGenerate: onGenerate,
      onSave: () => Bridge.send({ type: "save" }),
      onMarkPracticed: () => Bridge.send({ type: "markPracticed" }),
      // A live param changed (key = transpose re-emit, feel = \tf line, voicing = re-voiced grips): replay.
      onDefinitionChange: () => replayScoreRequest(),
      onHarmonySwitch: onHarmonySwitch,
    });

    // The Sheet view surface (collapsed by default; the toggle reveals it).
    if (window.ChordFlowSheetView) {
      sheetView = window.ChordFlowSheetView.create($("sheet-pane"));
    }

    // The now/next chord fretboards live above the surfaces; fed the loadScore schedule and the beat fan-out.
    if (window.ChordFlowNowNext) {
      nowNext = window.ChordFlowNowNext.create($("now-next-pane"));
    }

    setupViewToggle();

    // Silence playback when the app window loses focus or is closing — a score shouldn't keep sounding while
    // ChordFlow is in the background or on its way out. Reuses the same registry-wide stopAll() the page
    // toggle uses, so every sound surface is covered. `pagehide` is the reliable close/navigate-away signal.
    if (window.ChordFlowPlayback) {
      const stopAll = () => window.ChordFlowPlayback.stopAll();
      window.addEventListener("blur", stopAll);
      window.addEventListener("pagehide", stopAll);
    }

    if (Bridge.available) {
      // Register the inbound handler BEFORE announcing ready, or we could miss the host's loadScore reply.
      Bridge.onReceive(onHostMessage);
      // Seed the replay target with the boot definition (the host pushes the boot blues on ready). Without
      // this a content-toggle change before the first Generate/Load would have nothing valid to re-render.
      lastScoreRequest = { ...BOOT_REQUEST };
      requestCatalog(); // populate HarmonyControlsR + the library name map
      // Carry the component's default render options on ready so the boot score reflects the checked toggles.
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
