// ChordFlow WebView glue.
//
// Owns the alphaTab instance and the C#<->JS bridge. UI controls (key/rhythm
// pickers, Generate, Play/Stop, Tempo, Save, Mark-practiced, the saved list) each
// post a bridge envelope to their C# slice; inbound envelopes from the host
// (loadScore, play/stop/setTempo, exerciseList, practiceRecorded) drive alphaTab
// and the UI. The synced beat cursor is alphaTab's built-in highlight.
//
// Transport is WebView2's window.chrome.webview (see the Bridge module). When
// opened with no host (plain browser) the bridge is absent, so transport falls
// back to driving alphaTab directly and a SAMPLE_TEX score is rendered.
"use strict";

// --- WebView2 transport -----------------------------------------------------
// WebView2 injects window.chrome.webview into the page. JS→C# via postMessage;
// C#→JS arrives as 'message' events whose e.data is the string the host sent
// with PostWebMessageAsString. (Feature-detected, so opening the page with no
// host still works — see the SAMPLE_TEX fallback below.)
const Bridge = (function () {
  const wv =
    typeof window !== "undefined" && window.chrome ? window.chrome.webview : undefined;
  const available = !!wv && typeof wv.postMessage === "function";

  return {
    available,
    send(obj) {
      if (available) wv.postMessage(JSON.stringify(obj));
    },
    onReceive(handler) {
      if (available) {
        wv.addEventListener("message", (e) => handler(e.data));
      }
    },
  };
})();

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

  let api = null;
  let baseTempo = 80;   // the score's authored \tempo; runtime tempo scales off it

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

  // The current builder selections — the payload for a generate envelope.
  function selections() {
    return {
      keyPitchClass: parseInt($("key").value, 10) || 0,
      rhythmId: $("rhythm").value || "beat_1_3",
      tempo: parseInt($("tempo").value, 10) || baseTempo,
    };
  }

  // --- transport controls --------------------------------------------------
  function setTransportEnabled(enabled) {
    ["btnPlay", "btnStop", "tempo"].forEach((id) => {
      const el = $(id);
      if (el) el.disabled = !enabled;
    });
  }

  // Translate an absolute BPM into alphaTab's playbackSpeed multiplier (1.0 =
  // the score's authored tempo). Avoids re-rendering for a tempo tweak.
  function applyTempo(bpm) {
    if (!api || !bpm || !baseTempo) return;
    api.playbackSpeed = bpm / baseTempo;
  }

  // --- control wiring ------------------------------------------------------
  // In host mode each control posts an envelope to its C# slice; the host echoes
  // play/stop/setTempo back to drive alphaTab. In browser-dev (no bridge) the
  // transport drives alphaTab directly and the DB-backed actions are no-ops.
  function setupControls() {
    const play = $("btnPlay");
    const stop = $("btnStop");
    const tempo = $("tempo");
    const gen = $("btnGenerate");
    const save = $("btnSave");
    const practice = $("btnPractice");

    if (play) {
      play.addEventListener("click", () => {
        if (Bridge.available) Bridge.send({ type: "play" });
        else if (api) api.playPause();
      });
    }
    if (stop) {
      stop.addEventListener("click", () => {
        if (Bridge.available) Bridge.send({ type: "stop" });
        else if (api) api.stop();
      });
    }
    if (tempo) {
      tempo.addEventListener("change", () => {
        const bpm = parseInt(tempo.value, 10);
        if (!bpm) return;
        if (Bridge.available) Bridge.send({ type: "setTempo", bpm });
        else applyTempo(bpm);
      });
    }
    if (gen) {
      gen.addEventListener("click", () => {
        if (Bridge.available) Bridge.send({ type: "generate", ...selections() });
        else if (api) api.tex(SAMPLE_TEX); // dev fallback: no engine in the browser
      });
    }
    if (save) {
      save.addEventListener("click", () => Bridge.send({ type: "save" }));
    }
    if (practice) {
      practice.addEventListener("click", () => Bridge.send({ type: "markPracticed" }));
    }
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
      li.addEventListener("click", () => Bridge.send({ type: "loadExercise", id: ex.id }));
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
        baseTempo = msg.tempo || baseTempo;
        api.tex(msg.tex);
        setTempoInput(baseTempo);
        setStatus("score loaded");
        break;
      case "play":
        if (api) api.playPause();
        break;
      case "stop":
        if (api) api.stop();
        break;
      case "setTempo":
        if (msg.bpm) {
          setTempoInput(msg.bpm);
          applyTempo(msg.bpm);
        }
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
      default:
        console.warn("ChordFlow: unhandled envelope type:", msg.type);
    }
  }

  function setTempoInput(bpm) {
    const tempo = $("tempo");
    if (tempo) tempo.value = String(bpm);
  }

  // Resolve the PlayerState enum defensively — the minified bundle may expose it
  // at alphaTab.synth.PlayerState or top-level; fall back to the known values
  // (Paused=0, Playing=1) so a namespace shuffle can't break play-state display.
  const PlayerState =
    (alphaTab.synth && alphaTab.synth.PlayerState) ||
    alphaTab.PlayerState ||
    { Paused: 0, Playing: 1 };

  // --- alphaTab events -> bridge (JS -> C#) --------------------------------
  function wirePlaybackEvents() {
    // playerStateChanged carries { state: PlayerState (Paused=0/Playing=1),
    // stopped: bool }. `stopped` is set both at natural end and on api.stop();
    // for the MVP both mean "the session ended" -> playbackFinished.
    api.playerStateChanged.on((e) => {
      const playing = e.state === PlayerState.Playing;
      reflectPlayState(playing);
      if (e.stopped) Bridge.send({ type: "playbackFinished" });
    });

    // activeBeatsChanged carries { activeBeats: { beats: Beat[] } }. Report the
    // first active beat's (bar, beat) — 1-based — for the synced cursor / future
    // progress tracking. alphaTab also paints its own cursor highlight in time.
    api.activeBeatsChanged.on((e) => {
      const beats = e && e.activeBeats && e.activeBeats.beats;
      if (!beats || beats.length === 0) return;
      const beat = beats[0];
      const bar = (beat.voice && beat.voice.bar ? beat.voice.bar.index : 0) + 1;
      const beatInBar = (typeof beat.index === "number" ? beat.index : 0) + 1;
      Bridge.send({ type: "beatChanged", bar, beat: beatInBar });
    });
  }

  function reflectPlayState(playing) {
    const play = $("btnPlay");
    if (play) play.textContent = playing ? "⏸ Pause" : "▶ Play";
  }

  function init() {
    populatePickers();

    if (typeof alphaTab === "undefined") {
      setStatus("alphaTab failed to load");
      console.error("alphaTab global not found — check wwwroot/alphaTab.min.js bundling.");
      return;
    }

    const settings = {
      core: {
        // Paths are relative to index.html. The host serves wwwroot under the
        // https://chordflow.local/ virtual host, so relative paths resolve same-
        // origin. Bravura lives in wwwroot/font (siblings of alphaTab.min.js).
        fontDirectory: "font/",
        // Real https origin (not file://), so alphaTab's layout Web Worker can be
        // spawned — let it run off the main thread.
        useWorkers: true,
      },
      player: {
        enablePlayer: true,                    // required for audio + cursor
        enableCursor: true,                    // show the bar/beat cursor (not on by default)
        enableAnimatedBeatCursor: true,        // animate the beat cursor between beats
        enableElementHighlighting: true,       // highlight the active beat's notes
        soundFont: "soundfont/sonivox.sf2",    // Apache-2.0 GM soundfont, bundled
        scrollMode: alphaTab.ScrollMode.Off,   // fixed container; no page auto-scroll
      },
    };

    api = new alphaTab.AlphaTabApi($("score"), settings);

    // Surface lifecycle in the status line so a glance confirms the integration.
    api.renderStarted.on(() => setStatus("rendering…"));
    api.renderFinished.on(() => setStatus("score rendered"));
    // Transport needs the player; enable it once the soundfont is ready.
    api.soundFontLoaded.on(() => {
      setTransportEnabled(true);
      setStatus("ready · soundfont loaded");
    });
    api.error.on((err) => {
      setStatus("alphaTab error — see console");
      console.error("alphaTab error:", err);
    });

    setupControls();
    wirePlaybackEvents();

    if (Bridge.available) {
      // Register the inbound handler BEFORE announcing ready, or we could miss
      // the host's loadScore reply.
      Bridge.onReceive(onHostMessage);
      Bridge.send({ type: "ready" });
      setStatus("waiting for score…");
    } else {
      // Standalone browser: no host to push a score — render the dev sample.
      api.tex(SAMPLE_TEX);
      setStatus("score loaded (dev fallback)");
    }
  }

  return { init, getApi: () => api, getTempo: () => baseTempo };
})();

if (document.readyState === "loading") {
  document.addEventListener("DOMContentLoaded", ChordFlow.init);
} else {
  ChordFlow.init();
}
