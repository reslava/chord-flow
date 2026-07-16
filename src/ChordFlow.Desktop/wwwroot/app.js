// ChordFlow Practice view.
//
// Owns the Practice builder UI (definition pickers — harmony/comping/lead — sourced from the content stores
// via the entity* bridge, plus the key/difficulty param pickers; feel lives on the score transport), and hosts the shared ChordFlowScore
// render component (score-render-component.js) for notation + playback. The component owns alphaTab and the
// transport strip (Play/Stop/Tempo + toggles); this view only feeds it alphaTex and wires its callbacks.
//
// Generate posts the chosen content references + params; the host resolves them into a canonical Exercise and
// pushes back a loadScore. Inbound envelopes (loadScore, exerciseList, practiceRecorded, status, and the
// entityList catalog replies) drive the component, the library, and the pickers. A content-toggle change
// (onNeedsRerender) replays the last render request with the new options.
//
// Transport is the shared window.ChordFlowBridge (bridge.js). With no host (plain browser) the bridge is
// unavailable, so the component renders a SAMPLE_TEX score directly and the DB-backed actions are no-ops.
// This module also wires the header Practice ⇄ Content view toggle.
"use strict";

const Bridge = window.ChordFlowBridge;

const ChordFlow = (function () {
  // Param enums — stable Domain enums; enumerated here rather than over a bridge. Triplet feel (swing) is NOT
  // here: it's a render/playback knob owned by the ChordFlowScore component (its transport), carried onto the
  // render request via view.getTripletFeel() — see selections() and onNeedsRerender.
  const DIFFICULTIES = ["Beginner", "Intermediate", "Advanced"];

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

  let view = null;             // the ChordFlowScore handle (owns alphaTab + transport)
  let nowNext = null;          // the ChordFlowNowNext handle (the now/next chord fretboards, synced to playback)
  let lastScoreRequest = null; // last render-producing envelope (sans renderOptions), for onNeedsRerender replay

  // Content catalog, populated from the entity* bridge — the pickers' source and the library's name map.
  const catalog = { progression: [], song: [], rhythm: [] };

  const $ = (id) => document.getElementById(id);
  const statusEl = () => $("status");
  function setStatus(text) {
    const el = statusEl();
    if (el) el.textContent = text;
  }

  // --- pickers -------------------------------------------------------------
  // Fill a <select> from [{value,label}] options, preserving the current value if still present.
  function fillSelect(sel, options, fallbackValue) {
    if (!sel) return;
    const prev = sel.value;
    sel.innerHTML = "";
    for (const opt of options) {
      const o = document.createElement("option");
      o.value = opt.value;
      o.textContent = opt.label;
      sel.appendChild(o);
    }
    const values = options.map((o) => o.value);
    sel.value = values.includes(prev) ? prev : (values.includes(fallbackValue) ? fallbackValue : (values[0] ?? ""));
  }

  // The static param pickers (key/difficulty) — built once. Harmony/comping/lead come from the catalog; feel
  // lives on the score component's transport.
  function populateStaticPickers() {
    // Key now lives on the ScoreR transport (scorer-render-params) — seeded per content via seedKeyForHarmony().
    fillSelect($("difficulty"), DIFFICULTIES.map((d) => ({ value: d, label: d })), "Beginner");
  }

  // Seed ScoreR's key from the selected harmony so a piece plays in its authored key by default: a song →
  // its InitialKey (carried on the catalog item, play-ui-key-init IN1/IN2), a key-independent progression → C.
  // Fires on a harmony *switch* only (IN4), so a manual key edit for the current selection survives until the
  // next switch. The saved-exercise load path never calls this, so a stored KeyOverride still wins (C2).
  function seedKeyForHarmony() {
    if (!view) return;
    const sel = $("harmony");
    if (!sel) return;
    const [entity, id] = (sel.value || "").split(/:(.*)/s);
    let pc = 0; // C — the key-independent / progression default
    if (entity === "song") {
      const song = catalog.song.find((s) => s.id === id);
      if (song && song.initialKey != null) pc = song.initialKey;
    }
    view.seedKey(pc);
  }

  // Seed ScoreR's tempo from the selected harmony: a song → its DefaultTempo (carried on the catalog item,
  // scorer-render-params IN1), else the ChordFlow default 80. The twin of seedKeyForHarmony/seedFeelForHarmony —
  // fires on a harmony *switch* only, so a manual tempo edit survives until the next switch (C2); it never
  // re-renders (tempo is a local playback-speed param, and generate carries the seeded value).
  function seedTempoForHarmony() {
    if (!view) return;
    const sel = $("harmony");
    if (!sel) return;
    const [entity, id] = (sel.value || "").split(/:(.*)/s);
    let tempo = 80; // the tempo-independent / progression / no-directive default
    if (entity === "song") {
      const song = catalog.song.find((s) => s.id === id);
      if (song && song.defaultTempo != null) tempo = song.defaultTempo;
    }
    view.seedTempo(tempo);
  }

  // Seed the transport's feel picker from the selected harmony: a song → its DefaultFeel (carried on the catalog
  // item, song-default-feel IN4), else straight ("None"). Like seedKeyForHarmony this fires on a harmony *switch*
  // only, so a manual feel edit for the current selection survives until the next switch (C6); it never re-renders
  // (generate carries the seeded value). A song with no `feel` directive (defaultFeel null) seeds straight.
  function seedFeelForHarmony() {
    if (!view) return;
    const sel = $("harmony");
    if (!sel) return;
    const [entity, id] = (sel.value || "").split(/:(.*)/s);
    let feel = "None"; // the feel-independent / progression / no-directive default
    if (entity === "song") {
      const song = catalog.song.find((s) => s.id === id);
      if (song && song.defaultFeel != null) feel = song.defaultFeel;
    }
    view.seedTripletFeel(feel);
  }

  // Rebuild the harmony picker from songs + progressions; each option's value is "<entity>:<id>" so generate
  // sends the right discriminator. Default to the boot blues progression when present.
  function rebuildHarmonyPicker() {
    const sel = $("harmony");
    if (!sel) return;
    const prev = sel.value;
    sel.innerHTML = "";
    const groups = [
      { label: "Songs", entity: "song", items: catalog.song },
      { label: "Progressions", entity: "progression", items: catalog.progression },
    ];
    for (const g of groups) {
      if (g.items.length === 0) continue;
      const og = document.createElement("optgroup");
      og.label = g.label;
      for (const it of g.items) {
        const o = document.createElement("option");
        o.value = `${g.entity}:${it.id}`;
        o.textContent = it.name;
        og.appendChild(o);
      }
      sel.appendChild(og);
    }
    const bootValue = "progression:12bar_blues";
    const values = Array.from(sel.options).map((o) => o.value);
    sel.value = values.includes(prev) ? prev : (values.includes(bootValue) ? bootValue : (values[0] ?? ""));
  }

  // Comping (required) + Lead (optional, with a "(none)" choice) from the rhythm catalog.
  function rebuildRhythmPickers() {
    const rhythmOpts = catalog.rhythm.map((r) => ({ value: r.id, label: r.name }));
    fillSelect($("comping"), rhythmOpts, "beat_1_3");
    fillSelect($("lead"), [{ value: "", label: "(none)" }, ...rhythmOpts], "");
  }

  // The current builder selections — the payload for a generate envelope. Tempo comes from the component
  // transport so the user's tempo choice authors the next generated exercise.
  function selections() {
    const [harmonyEntity, harmonyId] = ($("harmony").value || "progression:12bar_blues").split(/:(.*)/s);
    const lead = $("lead").value;
    return {
      harmonyEntity,
      harmonyId,
      compingPatternId: $("comping").value || "beat_1_3",
      leadPatternId: lead || null,
      keyPitchClass: view ? view.getKey() : 0,
      tempo: view ? view.getTempo() : 80,
      difficulty: $("difficulty").value || "Beginner",
      tripletFeel: view ? view.getTripletFeel() : "None",
    };
  }

  // The automatic comping-voicing region from the builder inputs (engine-derived-as-app-source IN14), sent on
  // renderOptions.voicing — the engine derives Closest grips within [minFret, maxFret]. Defaults 0–15 = full neck.
  function voicingSource() {
    const clamp = (v, d) => Math.min(15, Math.max(0, Number.isFinite(v) ? v : d));
    let min = clamp(parseInt($("voicingMinFret").value, 10), 0);
    let max = clamp(parseInt($("voicingMaxFret").value, 10), 15);
    if (min > max) [min, max] = [max, min];
    return { kind: "automatic", minFret: min, maxFret: max };
  }

  // Send a render-producing request with the component's current renderOptions + the voicing region attached,
  // remembering it so a content-toggle change can replay it. In browser-dev (no bridge) renders the sample.
  function sendScoreRequest(envelope) {
    lastScoreRequest = envelope;
    if (Bridge.available) {
      Bridge.send({ ...envelope, renderOptions: { ...view.getRenderOptions(), voicing: voicingSource() } });
    } else if (view) {
      view.load(SAMPLE_TEX);
    }
  }

  // --- control wiring ------------------------------------------------------
  function setupControls() {
    const gen = $("btnGenerate");
    const save = $("btnSave");
    const practice = $("btnPractice");
    const harmony = $("harmony");

    if (harmony) {
      // Switching the harmony adopts that piece's render params (song → its InitialKey / DefaultTempo /
      // DefaultFeel, progression → C / 80 / None) onto ScoreR — a seed only, no re-render (Generate applies them).
      harmony.addEventListener("change", seedKeyForHarmony);
      harmony.addEventListener("change", seedTempoForHarmony);
      harmony.addEventListener("change", seedFeelForHarmony);
    }
    if (gen) {
      gen.addEventListener("click", () => sendScoreRequest({ type: "generate", ...selections() }));
    }
    if (save) {
      save.addEventListener("click", () => Bridge.send({ type: "save" }));
    }
    if (practice) {
      practice.addEventListener("click", () => Bridge.send({ type: "markPracticed" }));
    }
    // Changing the voicing region re-renders the current exercise with the new comping grips (IN14).
    for (const id of ["voicingMinFret", "voicingMaxFret"]) {
      const el = $(id);
      if (el) el.addEventListener("change", () => { if (lastScoreRequest) sendScoreRequest(lastScoreRequest); });
    }
  }

  // Ask the host for the content lists that feed the pickers (and the library's name map).
  function requestCatalog() {
    for (const entity of ["progression", "song", "rhythm"]) {
      Bridge.send({ type: "entityList", entity });
    }
  }

  // A catalog list arrived — cache it and rebuild the pickers (and re-label the library, whose names resolve
  // against the catalog). entityList also fans out from the Content view's own requests; harmless to re-apply.
  function onCatalogList(entity, items) {
    if (!(entity in catalog)) return;
    catalog[entity] = items || [];
    rebuildHarmonyPicker();
    rebuildRhythmPickers();
    if (lastLibrary) renderLibrary(lastLibrary); // re-label with freshly resolved names
  }

  // --- view toggle (Practice ⇄ Content) ------------------------------------
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
      chordSheets: { nav: $("navChordSheets"), el: $("chord-sheets-view"),
        onShow: () => window.ChordFlowChordSheets && window.ChordFlowChordSheets.show() },
    };

    function show(viewName) {
      const target = views[viewName] ? viewName : "practice";
      // Changing pages silences audio: stop every live player engine (Practice, Content preview, Chord
      // Sheets, …) before switching, so a score left playing doesn't keep sounding on the page you left.
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
      case "loadScore":
        view.load(msg.tex, { tempo: msg.tempo });
        // Seed ScoreR's render-param controls from the piece the host rendered (scorer-render-params IN6): a
        // loaded exercise shows its persisted key/tempo/feel (override wins over content defaults, C2). Seeds
        // only — no re-render. load({tempo}) already seeded the tempo input; key + feel are seeded here.
        if (msg.key != null) view.seedKey(msg.key);
        if (msg.tripletFeel) view.seedTripletFeel(msg.tripletFeel);
        if (nowNext) nowNext.setSchedule(msg.schedule);
        setStatus("score loaded");
        break;
      case "exerciseList":
        renderLibrary(msg.exercises);
        break;
      case "entityList":
        // The catalog feeds the pickers; the Content view owns the editor's own use of this same envelope.
        onCatalogList(msg.entity, msg.items);
        break;
      case "practiceRecorded":
        setStatus(`practiced ✓ — recorded ${msg.count}×`);
        break;
      case "status":
        setStatus(msg.text);
        if (msg.isError) console.error("ChordFlow host:", msg.text);
        break;
      // play/stop/setTempo are not host-driven — the ChordFlowScore component owns alphaTab transport.
      // Other Content-view envelopes (entityPreview/entityLoaded/…) fan out here and are ignored.
    }
  }

  function init() {
    populateStaticPickers();
    rebuildHarmonyPicker();   // empty until the catalog arrives, but wires the element
    rebuildRhythmPickers();

    if (typeof alphaTab === "undefined" || !window.ChordFlowScore) {
      setStatus("alphaTab/render component failed to load");
      console.error("alphaTab global or ChordFlowScore not found — check wwwroot bundling.");
      return;
    }

    view = window.ChordFlowScore.create($("score-pane"), {
      player: true,
      controls: "full",
      scroll: true,       // auto-follow the cursor: bound the staff + scroll it so the played bar stays under Now/Next
      debugPanel: true,   // the alphaTex scratchpad lives on the score component now (replaces the Debug view)
      tripletFeel: true,  // the whole-song feel (swing) select lives on the transport — see getTripletFeel()
      key: true,          // the Key control lives on the transport now (scorer-render-params) — see getKey()
      onBeat: (bar, beat) => {
        // The score component reports 1-based (bar, beat); the chord schedule is 0-based (alphaTab raw), so
        // step the now/next boards down by one.
        if (nowNext) nowNext.onBeat(bar - 1, beat - 1);
        if (Bridge.available) Bridge.send({ type: "beatChanged", bar, beat });
      },
      onFinished: () => {
        if (nowNext) nowNext.reset(); // back to the first chord on stop / end (schedule kept for replay)
        if (Bridge.available) Bridge.send({ type: "playbackFinished" });
      },
      onToggleNowNext: (visible) => {
        // The score component owns the transport toggle but not the boards — flip the pane it doesn't see.
        const pane = $("now-next-pane");
        if (pane) pane.hidden = !visible;
      },
      onNeedsRerender: (renderOptions) => {
        if (Bridge.available && lastScoreRequest) {
          // Carry the component's current key + feel too (a key/feel change routes through here) — both are
          // first-class request params, not renderOptions. Key is a transpose re-emit; feel is the \tf line.
          Bridge.send({
            ...lastScoreRequest,
            keyPitchClass: view.getKey(),
            tripletFeel: view.getTripletFeel(),
            renderOptions,
          });
        }
      },
    });

    // The now/next chord fretboards live above the score; they're fed the loadScore schedule and the score
    // component's beat signal (wired in the onBeat callback above).
    if (window.ChordFlowNowNext) {
      nowNext = window.ChordFlowNowNext.create($("now-next-pane"));
    }

    setupControls();
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
      // Seed the replay target with the boot definition (the host pushes Bb / Beats 1 & 3 on ready). Without
      // this a content-toggle change before the first Generate/Load would have nothing valid to re-render.
      lastScoreRequest = { ...BOOT_REQUEST };
      requestCatalog(); // populate the pickers + library name map
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
