// ChordFlow Practice view.
//
// Owns the Practice builder UI (definition pickers — harmony/comping/lead — sourced from the content stores
// via the entity* bridge, plus the key/difficulty/feel param pickers), and hosts the shared ChordFlowScore
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
  // Key names per tonic pitch class (0 = C .. 11 = B), spelled to match the renderer's \ks. Key picker only.
  const KEY_NAMES = ["C", "Db", "D", "Eb", "E", "F", "F#", "G", "Ab", "A", "Bb", "B"];

  // Param enums — stable Domain enums (Difficulty / Feel); enumerated here rather than over a bridge.
  const DIFFICULTIES = ["Beginner", "Intermediate", "Advanced"];
  const FEELS = ["Straight", "Swing", "Shuffle", "Triplet"];

  // The boot definition the host renders on ready (12-bar blues, Bb, Beats 1 & 3). Mirrored here as the
  // generate-envelope defaults so an early content-toggle replay (before the catalog loads) is still valid.
  const BOOT_REQUEST = {
    type: "generate",
    harmonyEntity: "progression", harmonyId: "12bar_blues",
    compingPatternId: "beat_1_3", leadPatternId: null,
    keyPitchClass: 10, tempo: 80, difficulty: "Beginner", feel: "Straight",
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

  // The static param pickers (key/difficulty/feel) — built once. Harmony/comping/lead come from the catalog.
  function populateStaticPickers() {
    fillSelect($("key"), KEY_NAMES.map((name, pc) => ({ value: String(pc), label: name })), "10"); // Bb default
    fillSelect($("difficulty"), DIFFICULTIES.map((d) => ({ value: d, label: d })), "Beginner");
    fillSelect($("feel"), FEELS.map((f) => ({ value: f, label: f })), "Straight");
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
      keyPitchClass: parseInt($("key").value, 10) || 0,
      tempo: view ? view.getTempo() : 80,
      difficulty: $("difficulty").value || "Beginner",
      feel: $("feel").value || "Straight",
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
      // Returning to Practice: the user may have authored content in the meantime — refresh the pickers.
      if (!content && Bridge.available) requestCatalog();
    }

    if (navPractice) navPractice.addEventListener("click", () => show("practice"));
    if (navContent) navContent.addEventListener("click", () => show("content"));
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
