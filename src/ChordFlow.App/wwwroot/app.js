// ChordFlow WebView glue.
//
// Owns the alphaTab instance and the C#<->JS bridge. Local transport controls
// (play/stop/tempo) drive alphaTab; alphaTab events are mapped back over the
// bridge (playerStateChanged -> playbackFinished, activeBeatsChanged ->
// beatChanged); and inbound loadScore/play/stop/setTempo envelopes let the C#
// host drive it too. The synced beat cursor is alphaTab's built-in highlight,
// enabled by player.enablePlayer.
//
// Transport is WebView2's window.chrome.webview (see the Bridge module). When
// opened with no host (plain browser) the bridge is absent, so we fall back to
// rendering SAMPLE_TEX for standalone dev.
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

  function setupTransport() {
    const play = $("btnPlay");
    const stop = $("btnStop");
    const tempo = $("tempo");

    if (play) play.addEventListener("click", () => api && api.playPause());
    if (stop) stop.addEventListener("click", () => api && api.stop());
    if (tempo) {
      tempo.addEventListener("change", () => applyTempo(parseInt(tempo.value, 10)));
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

    setupTransport();
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
