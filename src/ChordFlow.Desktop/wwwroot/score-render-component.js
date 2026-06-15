// ChordFlow shared score render component.
//
// The single owner of "alphaTex string → alphaTab notation + (optional) playback". Every screen that
// shows a score uses it: the Practice view (app.js, full player) and the Content-CRUD preview
// (content-crud.js, lite render-only). It centralizes the alphaTab settings (one source of truth, no
// per-consumer drift) and the on/off render options.
//
// Options split in two (the load-bearing distinction):
//   • player-kind  (metronome, count-in) — applied locally via the alphaTab API, no round-trip.
//   • content-kind (chord names, chord diagrams, voicing) — change the alphaTex the C# renderer emits,
//                   so flipping one fires onNeedsRerender(renderOptions) and the consumer re-requests.
//
// alphaTex is NEVER built here — generation stays in C# AlphaTexRenderer (the exporter seam). This module
// only displays the string the host sends and surfaces transport + toggles.
//
//   const view = ChordFlowScore.create(containerEl, {
//     player: true,            // false = lite render-only (no soundfont, no transport)
//     controls: "full",        // "full" | "mini" | "none"
//     options: { metronome:false, countIn:false, chordNames:false, diagrams:false, voicing:"byDifficulty" },
//     onBeat:(bar,beat)=>…, onStateChange:(playing)=>…, onFinished:()=>…, onNeedsRerender:(ro)=>…,
//   });
//   view.load(tex, { tempo }); view.play(); view.stop(); view.setTempo(bpm);
//   view.setOption("chordNames", true); view.getRenderOptions(); view.dispose();
"use strict";

window.ChordFlowScore = (function () {
  // Player-state enum, resolved defensively (the minified bundle may shuffle namespaces).
  const PlayerState =
    (alphaTab.synth && alphaTab.synth.PlayerState) ||
    alphaTab.PlayerState ||
    { Paused: 0, Playing: 1 };

  const PLAYER_KIND = new Set(["metronome", "countIn"]);   // applied via the alphaTab API
  const CONTENT_KIND = new Set(["chordNames", "diagrams", "voicing"]); // require a C# re-render

  const DEFAULT_OPTIONS = {
    metronome: false,
    countIn: false,
    chordNames: false,
    diagrams: false,
    voicing: "byDifficulty",
  };

  // The single alphaTab settings source of truth. Player settings are added only in player mode so a
  // lite preview never pays the soundfont/worker-player cost.
  function buildSettings(player) {
    const settings = {
      core: {
        fontDirectory: "font/",   // relative to index.html, served same-origin under the virtual host
        useWorkers: true,         // real https origin → layout worker is allowed off the main thread
      },
    };
    if (player) {
      settings.player = {
        enablePlayer: true,
        enableCursor: true,
        enableAnimatedBeatCursor: true,
        enableElementHighlighting: true,
        soundFont: "soundfont/sonivox.sf2",
        scrollMode: alphaTab.ScrollMode.Off,
      };
    }
    return settings;
  }

  function create(container, opts) {
    opts = opts || {};
    const player = opts.player !== false;                 // default true
    const controls = opts.controls || (player ? "full" : "none");
    const options = Object.assign({}, DEFAULT_OPTIONS, opts.options || {});
    const cb = {
      onBeat: opts.onBeat || function () {},
      onStateChange: opts.onStateChange || function () {},
      onFinished: opts.onFinished || function () {},
      onNeedsRerender: opts.onNeedsRerender || function () {},
    };

    container.classList.add("cf-score");
    const surface = document.createElement("div");
    surface.className = "cf-score-surface";

    const api = new alphaTab.AlphaTabApi(surface, buildSettings(player));
    let baseTempo = 80;   // the score's authored \tempo; runtime tempo scales off it

    // Control refs, populated by buildControls when a strip is rendered.
    const ui = { play: null, stop: null, tempo: null, toggles: {} };

    function applyPlayerOption(name, value) {
      if (!player) return;
      if (name === "metronome") api.metronomeVolume = value ? 1 : 0;
      else if (name === "countIn") api.countInVolume = value ? 1 : 0;
    }

    function reflectPlayState(playing) {
      if (ui.play) ui.play.textContent = playing ? "⏸ Pause" : "▶ Play";
    }

    function setTransportEnabled(enabled) {
      [ui.play, ui.stop, ui.tempo].forEach((el) => { if (el) el.disabled = !enabled; });
    }

    const handle = {
      // Render an alphaTex string. `tempo` (the score's authored BPM) re-bases setTempo's speed multiplier.
      load(tex, o) {
        if (o && o.tempo) baseTempo = o.tempo;
        if (ui.tempo) ui.tempo.value = String(baseTempo);
        api.tex(tex);
      },
      play() { api.playPause(); },
      stop() { api.stop(); },
      // Translate absolute BPM into alphaTab's playbackSpeed multiplier (1.0 = authored tempo) — no re-render.
      setTempo(bpm) { if (bpm && baseTempo) api.playbackSpeed = bpm / baseTempo; },
      // Player-kind → applied locally; content-kind → ask the consumer to re-render with the new options.
      setOption(name, value) {
        options[name] = value;
        if (PLAYER_KIND.has(name)) applyPlayerOption(name, value);
        else if (CONTENT_KIND.has(name)) cb.onNeedsRerender(handle.getRenderOptions());
        const toggle = ui.toggles[name];
        if (toggle && toggle.checked !== !!value) toggle.checked = !!value;
      },
      // The renderOptions payload to attach to a C# render request (generate / entityPreview / loadExercise).
      getRenderOptions() {
        return {
          showChordNames: !!options.chordNames,
          showChordDiagrams: !!options.diagrams,
          voicing: options.voicing,
        };
      },
      getApi() { return api; },
      // The current tempo shown in the transport (BPM), else the loaded score's authored tempo. Lets a
      // consumer carry the user's tempo choice onto the next generate request.
      getTempo() {
        const shown = ui.tempo ? parseInt(ui.tempo.value, 10) : NaN;
        return shown || baseTempo;
      },
      dispose() {
        try { api.destroy(); } catch (_) { /* already torn down */ }
        container.innerHTML = "";
        container.classList.remove("cf-score");
      },
    };

    const strip = buildControls(player, controls, options, handle, ui);
    if (strip) container.appendChild(strip);
    container.appendChild(surface);

    if (player) {
      // playerStateChanged: { state: Paused/Playing, stopped: bool }. `stopped` fires at natural end and
      // on stop() — both mean "session ended" for the consumer's onFinished.
      api.playerStateChanged.on((e) => {
        const playing = e.state === PlayerState.Playing;
        reflectPlayState(playing);
        cb.onStateChange(playing);
        if (e.stopped) cb.onFinished();
      });
      // activeBeatsChanged: report the first active beat's (bar, beat), both 1-based.
      api.activeBeatsChanged.on((e) => {
        const beats = e && e.activeBeats && e.activeBeats.beats;
        if (!beats || beats.length === 0) return;
        const beat = beats[0];
        const bar = (beat.voice && beat.voice.bar ? beat.voice.bar.index : 0) + 1;
        const beatInBar = (typeof beat.index === "number" ? beat.index : 0) + 1;
        cb.onBeat(bar, beatInBar);
      });
      // Transport needs the player; enable it once the soundfont is ready.
      api.soundFontLoaded.on(() => setTransportEnabled(true));

      // Apply the initial player-kind option state (content-kind already rides the first render request).
      applyPlayerOption("metronome", options.metronome);
      applyPlayerOption("countIn", options.countIn);
    }

    return handle;
  }

  // Build the control strip per profile. Transport + player-kind toggles need the player; content-kind
  // toggles render only in the "full" profile. Returns null when nothing is rendered (mini render-only / none).
  function buildControls(player, controls, options, handle, ui) {
    if (controls === "none") return null;

    const strip = document.createElement("div");
    strip.className = "cf-controls";

    if (player && (controls === "full" || controls === "mini")) {
      ui.play = button("▶ Play", () => handle.play());
      ui.play.disabled = true;
      ui.stop = button("■ Stop", () => handle.stop());
      ui.stop.disabled = true;
      strip.append(ui.play, ui.stop);

      const tempoLabel = document.createElement("label");
      tempoLabel.textContent = "Tempo";
      ui.tempo = document.createElement("input");
      ui.tempo.type = "number";
      ui.tempo.min = "40"; ui.tempo.max = "240"; ui.tempo.step = "1";
      ui.tempo.disabled = true;
      ui.tempo.className = "cf-tempo";
      ui.tempo.addEventListener("change", () => {
        const bpm = parseInt(ui.tempo.value, 10);
        if (bpm) handle.setTempo(bpm);
      });
      strip.append(tempoLabel, ui.tempo, span("BPM"));
    }

    if (player && controls === "full") {
      strip.append(
        toggle("metronome", "Metronome", options, handle, ui),
        toggle("countIn", "Count-in", options, handle, ui),
      );
    }

    if (controls === "full") {
      strip.append(
        toggle("chordNames", "Chord names", options, handle, ui),
        toggle("diagrams", "Diagrams", options, handle, ui),
      );
    }

    return strip.childElementCount > 0 ? strip : null;
  }

  // --- small DOM builders ----------------------------------------------------
  function button(label, onClick) {
    const b = document.createElement("button");
    b.type = "button";
    b.textContent = label;
    b.addEventListener("click", onClick);
    return b;
  }

  function span(text) {
    const s = document.createElement("span");
    s.textContent = text;
    return s;
  }

  function toggle(name, label, options, handle, ui) {
    const wrap = document.createElement("label");
    wrap.className = "cf-toggle";
    const input = document.createElement("input");
    input.type = "checkbox";
    input.checked = !!options[name];
    input.addEventListener("change", () => handle.setOption(name, input.checked));
    wrap.append(input, document.createTextNode(" " + label));
    ui.toggles[name] = input;
    return wrap;
  }

  return { create };
})();
