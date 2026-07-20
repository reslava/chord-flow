// ChordFlow Practice view — ONE page, the shared render-surface composite + the page-specific controls.
//
// The shell of the single Practice page: it mounts the shared components ONCE and wraps the composite —
//   • HarmonyControlsR (harmony-controls-component.js) — the definition strip (harmony/key/feel/comping+vol/
//     lead+vol/difficulty/voicing window/Generate/Save/Mark practiced), fed the entity* catalog.
//   • ChordFlowRenderSurface (render-surface-component.js) — the shared composite: ScoreR (staff/notation
//     toggles + debug panel + the ONE engine) + ChordSheetR behind the Score ⇄ Sheet toggle + a page-level
//     PlayerControlsR bound to that engine (survives the toggle) + the beat/position marker fan-out. Practice
//     mounts it into #transport-strip / #score-pane / #sheet-pane and keeps volumes/key/feel OUT of ScoreR
//     (they live in HarmonyControlsR beside it). The SAME composite backs the Content preview.
//   • ChordFlowNowNext — the now/next chord fretboards, view-independent (Practice-only, EX4): fed the
//     loadScore schedule + the composite's onBeat passthrough, never owned by the composite.
//
// ONE render-producing reply feeds both surfaces (IN3): loadScore carries { tex, tempo, key, tripletFeel,
// schedule, sheet, cellSchedule } — the score and the chord sheet are projections of the same Exercise pass;
// the shell hands it to surface.load(). The composite's toggle collapses (max-height:0) rather than
// display:none's the hidden surface, so alphaTab keeps its layout width and the toggle works MID-PLAYBACK
// (IN7/C4): audio continues, both markers keep tracking.
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

  let surface = null;          // ChordFlowRenderSurface — the shared composite (ScoreR + ChordSheetR + toggle +
                               // page-level PlayerControlsR + the one engine + beat/position fan-out)
  let hc = null;               // HarmonyControlsR — the definition strip (page-specific, wraps the composite)
  let nowNext = null;          // ChordFlowNowNext (the now/next chord fretboards, synced to playback)
  let lastScoreRequest = null; // last render-producing envelope (sans renderOptions), for replay
  let practiceFilter = null;   // shared FilterR narrowing the metadata-bearing pickers (Harmony + Drums) by g/s/t
  let practiceSelected = {};   // cascade selection per level ({ [key]: Set }) — recomputed on toggle (filter-ux-facets)
  const practiceKnown = {};    // values ever seen per level, so a value toggled off stays off across catalog re-arrivals

  // Content catalog, populated from the entity* bridge — feeds HarmonyControlsR and the library's name map.
  const catalog = { progression: [], song: [], rhythm: [], drums: [] };

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
      Bridge.send({ ...envelope, renderOptions: { ...surface.getRenderParams().renderOptions, voicing: voicingOf(def) } });
    } else if (surface) {
      surface.load({ tex: SAMPLE_TEX });
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
      renderOptions: { ...(renderOptions || surface.getRenderParams().renderOptions), voicing: voicingOf(def) },
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
      drumGrooveId: def.drumGrooveId,
      drumVolume: def.drumVolume,
      keyPitchClass: def.keyPitchClass,
      keyIsMinor: def.keyIsMinor,
      tempo: surface.getRenderParams().tempo || 80,
      difficulty: def.difficulty,
      tripletFeel: def.tripletFeel,
    });
  }

  // Switching the harmony adopts that piece's tempo onto the page transport (song → DefaultTempo, else 80) —
  // a seed only, no re-render (Generate applies it). Key/Feel seed INSIDE HarmonyControlsR; tempo is the
  // shell's job because PlayerControlsR owns it (C1).
  function onHarmonySwitch(item) {
    const tempo = item && item.defaultTempo != null ? item.defaultTempo : 80;
    surface.seedTempo(tempo); // base tempo for the next load/generate + reflect it in the page transport input
  }

  // --- catalog ---------------------------------------------------------------
  // Ask the host for the content lists that feed HarmonyControlsR (and the library's name map).
  function requestCatalog() {
    for (const entity of ["progression", "song", "rhythm", "drums"]) {
      Bridge.send({ type: "entityList", entity });
    }
  }

  // A catalog list arrived — cache it, refresh the Practice cascade filter and feed HarmonyControlsR through it
  // (the single population path, IN8), and re-label the library. entityList also fans out from the Content view's
  // own requests; harmless to re-apply.
  function onCatalogList(entity, items) {
    if (!(entity in catalog)) return;
    catalog[entity] = items || [];
    if (entity === "rhythm") {
      if (hc) hc.setCatalog("rhythm", catalog.rhythm); // Comping/Lead never narrowed (C4)
    } else {
      ensurePracticeSelection(); // new g/s/t values default on
      rebuildPracticeFilter();
    }
    if (lastLibrary) renderLibrary(lastLibrary); // re-label with freshly resolved names
  }

  // --- Practice content filter (filter-toggle-buttons IN6, filter-ux-facets) --
  // The metadata-bearing Practice pickers are Harmony (Song + Progression) and Drums; Comping/Lead are rhythm-
  // backed and carry no catalog metadata (EX3) so are never narrowed (C4). Source is always all here (no Source level).
  const Cascade = window.ChordFlowFilterCascade;
  const PRACTICE_LEVELS = [
    { key: "genre", label: "Genre", values: (it) => (it.genre ? [it.genre] : []) },
    { key: "subgenre", label: "Subgenre", values: (it) => (it.subgenre ? [it.subgenre] : []) },
    { key: "tags", label: "Tags", values: (it) => it.tags || [] },
  ];

  // The union of the metadata-bearing pickers (same object refs as the catalogs, so a membership Set filters them).
  function metadataItems() {
    return [...catalog.song, ...catalog.progression, ...catalog.drums];
  }

  // Persist the selection across incremental catalog arrivals: a first-seen value defaults on; a value the user
  // turned off stays off (tracked in practiceKnown).
  function ensurePracticeSelection() {
    const items = metadataItems();
    for (const def of PRACTICE_LEVELS) {
      if (!practiceSelected[def.key]) practiceSelected[def.key] = new Set();
      const known = practiceKnown[def.key] || (practiceKnown[def.key] = new Set());
      for (const v of Cascade.distinctValues(items, def.values)) {
        if (!known.has(v)) { known.add(v); practiceSelected[def.key].add(v); }
      }
    }
  }

  // Build the cascade over the union, feed each metadata-bearing picker its filtered subset (rhythm full), show total.
  function rebuildPracticeFilter() {
    const items = metadataItems();
    const built = Cascade.build(items, PRACTICE_LEVELS, practiceSelected);
    if (practiceFilter) practiceFilter.setLevels(built.levels);
    const keep = new Set(built.filtered);
    if (hc) {
      hc.setCatalog("song", catalog.song.filter((it) => keep.has(it)));
      hc.setCatalog("progression", catalog.progression.filter((it) => keep.has(it)));
      hc.setCatalog("drums", catalog.drums.filter((it) => keep.has(it)));
    }
    renderPracticeCount(built.total, items.length);
  }

  // A chip toggled: adopt the changed level, reset the levels below it (cascade), rebuild.
  function onPracticeFilterChange(state, changedKey) {
    practiceSelected[changedKey] = state[changedKey];
    practiceSelected = Cascade.resetBelow(metadataItems(), PRACTICE_LEVELS, practiceSelected, changedKey);
    rebuildPracticeFilter();
  }

  function renderPracticeCount(shown, total) {
    const elc = $("practice-filter-count");
    if (elc) elc.textContent = (total === 0 || shown === total) ? "" : shown + " of " + total + " shown";
  }

  // --- top-level view toggle (Practice ⇄ Content ⇄ diagnostics pages) ---------
  function setupViewToggle() {
    let voicingsView = null; // lazy GuitarVoicingsR handle (created on first show of the Voicings tab)
    // The top-level views in the single page; each lazily inits its module on first show.
    const views = {
      practice: { nav: $("navPractice"), el: $("practice-view") },
      content: { nav: $("navContent"), el: $("content-view"),
        onShow: () => window.ChordFlowContent && window.ChordFlowContent.show() },
      drums: { nav: $("navDrums"), el: $("drums-view"),
        onShow: () => window.ChordFlowDrumsView && window.ChordFlowDrumsView.show() },
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
        // ONE reply, both projections (IN3): the composite loads the tex into ScoreR AND renders the sheet
        // model + takes its marker schedule; Now/Next takes the chord schedule — nothing can drift.
        const def = hc.getDefinition();
        surface.load({
          tex: msg.tex, tempo: msg.tempo, key: msg.key, tripletFeel: msg.tripletFeel,
          sheet: msg.sheet, cellSchedule: msg.cellSchedule,
          name: harmonyName(def.harmonyId), // the Sheet export filename base
        });
        // Seed the definition strip (HarmonyControlsR, outside the composite) from the piece the host rendered:
        // a loaded exercise shows its persisted key/feel (override wins over content defaults, C3). Seeds only.
        if (msg.key != null) hc.seedKey(msg.key);
        if (msg.keyIsMinor != null) hc.seedKeyMode(msg.keyIsMinor); // a saved minor exercise reopens minor (IN5)
        if (msg.tripletFeel) hc.seedTripletFeel(msg.tripletFeel);
        if (nowNext) nowNext.setSchedule(msg.schedule);
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
    if (typeof alphaTab === "undefined" || !window.ChordFlowRenderSurface) {
      setStatus("alphaTab/render component failed to load");
      console.error("alphaTab global or ChordFlowRenderSurface not found — check wwwroot bundling.");
      return;
    }

    // The shared render surface (composite): ScoreR (staff/notation toggles + debug panel + the ONE engine) +
    // ChordSheetR behind the Score ⇄ Sheet toggle + a page-level PlayerControlsR bound to that engine + the
    // beat/position fan-out. Practice keeps transport/volumes/key/feel OUT of ScoreR — they live at page level
    // (PlayerControlsR inside the composite / HarmonyControlsR beside it). Now/Next is Practice-only (EX4): fed
    // from the composite's onBeat passthrough + engine, never owned by the composite.
    surface = window.ChordFlowRenderSurface.create({
      transportEl: $("transport-strip"), // toggle + PlayerControlsR mount — page-level, survives the view toggle
      scoreEl: $("score-pane"),
      sheetEl: $("sheet-pane"),
      sheet: true,
      scoreOpts: {
        player: true,
        controls: "full",
        volumes: false,   // Rhythm/Lead sliders live in HarmonyControlsR, next to their voice (C2)
        scroll: true,     // auto-follow the cursor so the played bar stays under Now/Next
        debugPanel: true, // the alphaTex scratchpad lives on the score component
      },
      playerOpts: {
        // The transport owns the Now/Next toggle but not the boards — flip the pane it doesn't see.
        onToggleNowNext: (visible) => { const pane = $("now-next-pane"); if (pane) pane.hidden = !visible; },
      },
      onBeat: (bar, beat) => {
        // The engine reports 1-based (bar, beat); the composite already fans this into the sheet's Per-chord
        // marker. Here Practice feeds its OWN event-shaped surfaces — Now/Next + the bridge echo.
        if (nowNext) nowNext.onBeat(bar - 1, beat - 1); // chord schedule is 0-based (alphaTab raw)
        if (Bridge.available) Bridge.send({ type: "beatChanged", bar, beat });
      },
      onFinished: () => {
        if (nowNext) nowNext.reset();          // back to the first chord on stop / end (schedule kept for replay)
        if (Bridge.available) Bridge.send({ type: "playbackFinished" });
      },
      onNeedsRerender: (renderOptions) => replayScoreRequest(renderOptions),
    });

    // The definition strip (HarmonyControlsR): owns harmony/key/feel/comping/lead/difficulty/voicing window +
    // the actions; volume sliders bind to the composite's one engine.
    hc = window.ChordFlowHarmonyControls.create($("harmony-controls"), {
      engine: surface.getEngine(),
      onGenerate: onGenerate,
      onSave: () => Bridge.send({ type: "save" }),
      onMarkPracticed: () => Bridge.send({ type: "markPracticed" }),
      // A live param changed (key = transpose re-emit, feel = \tf line, voicing = re-voiced grips): replay.
      onDefinitionChange: () => replayScoreRequest(),
      onHarmonySwitch: onHarmonySwitch,
    });

    // The Practice content filter (IN6): narrows the Harmony + Drums pickers by genre/subgenre/tags. Levels are
    // discovered from the catalogs as they arrive (refreshPracticeLevels); a toggle re-feeds HarmonyControlsR.
    practiceFilter = window.ChordFlowFilter.create($("practice-filter"), {
      levels: [],
      onChange: onPracticeFilterChange,
    });

    // The now/next chord fretboards live above the surfaces; fed the loadScore schedule + the composite's beat fan-out.
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
      Bridge.send({ type: "ready", renderOptions: surface.getRenderParams().renderOptions });
      setStatus("waiting for score…");
    } else {
      // Standalone browser: no host to push a score — render the dev sample.
      surface.load({ tex: SAMPLE_TEX });
      setStatus("score loaded (dev fallback)");
    }
  }

  return { init, getSurface: () => surface };
})();

if (document.readyState === "loading") {
  document.addEventListener("DOMContentLoaded", ChordFlow.init);
} else {
  ChordFlow.init();
}
