// ChordFlow playback engine (ChordFlowPlayback) — the shared alphaTab api + transport wrapper.
//
// Extracted from score-render-component.js so both ScoreR (the Practice player + the Content-preview lite
// render) AND the Chord Sheets page can drive alphaTab without duplicating the api / transport / soundfont
// wiring. alphaTab fuses rendering and playback in ONE AlphaTabApi(surface, settings): the synth, cursor,
// activeBeatsChanged, and beat schedule are all bound to a rendered score surface. So this engine OWNS the
// api + a surface (a headless consumer just keeps that surface off-screen); a consumer reaches through
// getApi() for its own render/notation concerns.
//
// The engine knows NOTHING about notation display, staff profiles, key/feel pickers, layout toggles, or the
// debug panel — those stay in ScoreR. It is purely: the api lifecycle + player settings + transport +
// soundfont round-trip + scroll-follow + the beat/state/ready events.
//
//   const engine = ChordFlowPlayback.create(surfaceEl, {
//     player: true,             // false = lite render-only (Content preview): api without player settings
//     scroll: false,            // true = auto-follow the cursor (OffScreen page-flip)
//     soundFont: "sonivox.sf2", // boot default; the host's saved choice replaces it live
//     display: {...},           // initial alphaTab display settings blob (the consumer owns layout)
//     onBeat:(bar,beat)=>…,     // 1-based, from activeBeatsChanged
//     onStateChange:(playing)=>…, onFinished:()=>…, onReady:()=>…,  // onReady = soundFontLoaded
//     onSoundFontsListed:(fonts, selectedId)=>…,   // host reply: fill the consumer's picker UI
//   });
//   engine.load(tex, { tempo }); engine.play(); engine.stop(); engine.setTempo(bpm);
//   engine.setMetronome(on); engine.setCountIn(on); engine.setTrackVolume("rhythm", 0.5);
//   engine.setSoundFont(id); engine.setScrollMode("offscreen");
//   engine.seedTempo(bpm); engine.getBaseTempo(); engine.getApi(); engine.isPlayer(); engine.dispose();
"use strict";

window.ChordFlowPlayback = (function () {
  // Player-state enum, resolved defensively (the minified bundle may shuffle namespaces).
  const PlayerState =
    (alphaTab.synth && alphaTab.synth.PlayerState) ||
    alphaTab.PlayerState ||
    { Paused: 0, Playing: 1 };

  // The soundfont shipped in the repo — the boot default and the fallback when no choice is stored.
  const DEFAULT_SOUNDFONT = "sonivox.sf2";
  function fontUrl(id) { return "soundfont/" + id; }

  // Live player-engine registry. Every player-mode engine self-registers on create() and drops out on
  // dispose(), so a single stopAll() silences every sound surface (Practice, Content preview, Chord Sheets,
  // and anything added later) with zero per-view wiring — the app's view toggle calls it on page change.
  const liveEngines = new Set();
  function stopAll() {
    for (const engine of liveEngines) {
      try { engine.stop(); }
      catch (e) { console.error("[ChordFlowPlayback] stopAll failed for an engine:", e); }
    }
  }

  const SCROLL_MODES = { off: alphaTab.ScrollMode.Off, offscreen: alphaTab.ScrollMode.OffScreen, continuous: alphaTab.ScrollMode.Continuous };

  // The single alphaTab settings source of truth. Player settings are added only in player mode so a lite
  // preview never pays the soundfont/worker-player cost. `display` is supplied by the consumer (it owns layout).
  function buildSettings(player, scroll, soundFont, display) {
    const settings = {
      core: {
        fontDirectory: "font/",   // relative to index.html, served same-origin under the virtual host
        useWorkers: true,         // real https origin → layout worker is allowed off the main thread
      },
      display: display || {},
    };
    if (player) {
      settings.player = {
        enablePlayer: true,
        enableCursor: true,
        enableAnimatedBeatCursor: true,
        enableElementHighlighting: true,
        soundFont: fontUrl(soundFont || DEFAULT_SOUNDFONT),   // boot default; replaced live once the host reports the saved choice
        scrollMode: scroll ? alphaTab.ScrollMode.OffScreen : alphaTab.ScrollMode.Off,
        nativeBrowserSmoothScroll: scroll,
      };
    }
    return settings;
  }

  function create(surface, opts) {
    opts = opts || {};
    const player = opts.player !== false;                 // default true
    // A small multi-subscriber event bus so several consumers (a page's playback marker AND a shared
    // PlayerControlsR) can each react to the same engine events without the page forwarding them. The
    // create()-time on* callbacks are sugar that register on these same buses.
    //   beat(bar,beat 1-based) · stateChange(playing) · ready · finished · soundFontsListed(fonts, selectedId)
    const listeners = { beat: [], stateChange: [], ready: [], finished: [], soundFontsListed: [] };
    function on(event, handler) {
      if (listeners[event] && typeof handler === "function") listeners[event].push(handler);
    }
    function emit(event) {
      const hs = listeners[event];
      if (!hs) return;
      const args = Array.prototype.slice.call(arguments, 1);
      for (const h of hs) {
        try { h.apply(null, args); }
        catch (e) { console.error("[ChordFlowPlayback] " + event + " listener failed:", e); }
      }
    }
    // Seed the create-time callbacks onto the buses (back-compat sugar).
    if (opts.onBeat) on("beat", opts.onBeat);
    if (opts.onStateChange) on("stateChange", opts.onStateChange);
    if (opts.onReady) on("ready", opts.onReady);
    if (opts.onFinished) on("finished", opts.onFinished);
    if (opts.onSoundFontsListed) on("soundFontsListed", opts.onSoundFontsListed);
    const bridge = (typeof window !== "undefined" && window.ChordFlowBridge) || null;

    let baseTempo = 80;   // the score's authored \tempo; runtime tempo scales off it
    let currentSoundFont = opts.soundFont || DEFAULT_SOUNDFONT;   // mirrors what's loaded (skip redundant reloads)
    let disposed = false;
    let scrollMode = "off";
    let metronomeOn = false;   // desired click-track state, re-asserted once the synth is ready (see below)
    let countInOn = false;     // desired count-in state, re-asserted once the synth is ready
    const trackVolumes = { rhythm: 1, lead: 1 };   // Rhythm = track 0, Lead = track 1 (two-track exercises)

    const api = new alphaTab.AlphaTabApi(surface, buildSettings(player, opts.scroll, currentSoundFont, opts.display));

    // Auto-follow the cursor by scrolling the score's OWN bounded surface — never the window. Mode is
    // "off" | "offscreen" | "continuous"; see the ScoreR history for the full rationale of each.
    function applyScrollMode(mode) {
      if (!player) return;
      scrollMode = SCROLL_MODES[mode] !== undefined ? mode : "off";
      const p = api.settings.player;
      p.scrollMode = SCROLL_MODES[scrollMode];
      if (scrollMode === "off") {
        surface.style.maxHeight = "";
        p.scrollElement = "html,body";   // alphaTab default; inert while scrollMode is Off
      } else {
        p.nativeBrowserSmoothScroll = scrollMode === "offscreen";   // animate the flip; instant for Continuous
        surface.style.maxHeight = "60vh";
        p.scrollElement = surface;
        p.scrollOffsetY = -15;
      }
      api.updateSettings();
    }
    if (player) applyScrollMode(opts.scroll ? "offscreen" : "off");

    // Per-track playback volumes (player-kind, local). alphaTab rebuilds the tracks on every load, so these
    // are re-asserted on scoreLoaded.
    function applyTrackVolume(which) {
      if (!player || !api.score || !api.score.tracks) return;
      const track = api.score.tracks[which === "lead" ? 1 : 0];
      if (track && typeof api.changeTrackVolume === "function") {
        api.changeTrackVolume([track], trackVolumes[which]);
      }
    }

    // metronomeVolume/countInVolume are synth properties: alphaTab STORES the value but only pushes it to the
    // live synth output if that output already exists — a value set before the synth is ready (or a synth
    // rebuilt on a new score / soundfont load) silently reverts to its default. So we hold the desired state
    // and (re)assert it here, and call this whenever the synth (re)initializes.
    function applyMetronomeCountIn() {
      if (!player) return;
      api.metronomeVolume = metronomeOn ? 1 : 0;
      api.countInVolume = countInOn ? 1 : 0;
    }

    // Swap the active synth soundfont live (no re-render, no persist).
    function applySoundFont(id) {
      if (!id || disposed) return;
      currentSoundFont = id;
      if (api.settings && api.settings.player) api.settings.player.soundFont = fontUrl(id);
      if (typeof api.loadSoundFontFromUrl === "function") api.loadSoundFontFromUrl(fontUrl(id), false);
    }

    // Host reply to listSoundFonts: hand the list + effective selection to the consumer (for its picker UI),
    // and apply the persisted selection ourselves (even without a picker).
    function onSoundFontsListed(msg) {
      if (disposed) return;
      const fonts = (msg && msg.fonts) || [];
      const selected = (msg && msg.selectedId) || null;
      emit("soundFontsListed", fonts, selected || currentSoundFont);
      if (selected && selected !== currentSoundFont) applySoundFont(selected);
    }

    if (player) {
      // Re-assert per-track volumes + metronome/count-in for each freshly loaded score (the synth rebuilds
      // its channels on every load, dropping any previously-set values otherwise).
      api.scoreLoaded.on(() => {
        applyTrackVolume("rhythm");
        applyTrackVolume("lead");
        applyMetronomeCountIn();
      });

      // playerStateChanged: { state: Paused/Playing, stopped: bool }. `stopped` fires at natural end and on
      // stop() — both mean "session ended" for the consumer's onFinished.
      api.playerStateChanged.on((e) => {
        const playing = e.state === PlayerState.Playing;
        emit("stateChange", playing);
        if (e.stopped) emit("finished");
      });

      // activeBeatsChanged: report the first active beat's (bar, beat), both 1-based.
      api.activeBeatsChanged.on((e) => {
        const active = e && e.activeBeats;
        const beats = Array.isArray(active) ? active : active && active.beats;
        if (!beats || beats.length === 0) return;
        const beat = beats[0];
        const bar = (beat.voice && beat.voice.bar ? beat.voice.bar.index : 0) + 1;
        const beatInBar = (typeof beat.index === "number" ? beat.index : 0) + 1;
        emit("beat", bar, beatInBar);
      });

      // Transport needs the player; the consumer enables its controls once the soundfont is ready. The synth
      // output exists by now, so (re)assert the desired metronome/count-in state — a value set earlier (before
      // the synth existed) would otherwise have been dropped.
      api.soundFontLoaded.on(() => { applyMetronomeCountIn(); emit("ready"); });

      // Ask the host which soundfonts exist + which is the saved choice; feature-detected (no host → boot default).
      if (bridge && bridge.available) {
        bridge.onReceive((data) => {
          let msg;
          try { msg = typeof data === "string" ? JSON.parse(data) : data; }
          catch (_) { return; }
          if (msg && msg.type === "soundFontsListed") onSoundFontsListed(msg);
        });
        bridge.send({ type: "listSoundFonts" });
      }
    }

    const handle = {
      // Render + prime an alphaTex string. `tempo` (authored BPM) re-bases setTempo's speed multiplier.
      load(tex, o) {
        if (o && o.tempo) baseTempo = o.tempo;
        api.tex(tex);
      },
      play() { if (player) api.playPause(); },
      stop() { if (player) api.stop(); },
      // Translate absolute BPM into alphaTab's playbackSpeed multiplier (1.0 = authored tempo) — no re-render.
      setTempo(bpm) { if (player && bpm && baseTempo) api.playbackSpeed = bpm / baseTempo; },
      setMetronome(state) { metronomeOn = !!state; applyMetronomeCountIn(); },
      setCountIn(state) { countInOn = !!state; applyMetronomeCountIn(); },
      // Subscribe to an engine event bus (beat/stateChange/ready/finished/soundFontsListed). Multiple
      // consumers (page marker + PlayerControlsR) can each subscribe without the page forwarding events.
      on(event, handler) { on(event, handler); return this; },
      // Per-track playback volume (0..1). which = "rhythm" | "lead"; lead is a no-op on a single-track score.
      setTrackVolume(which, value) { trackVolumes[which] = value; applyTrackVolume(which); },
      // Apply a soundfont live and persist the new global choice host-side.
      setSoundFont(id) {
        applySoundFont(id);
        if (bridge) bridge.send({ type: "setSoundFont", id });
      },
      setScrollMode(mode) { applyScrollMode(mode); },
      // Seed the authored tempo WITHOUT re-rendering (so the next load re-bases correctly).
      seedTempo(bpm) { if (bpm) baseTempo = bpm; },
      getBaseTempo() { return baseTempo; },
      isPlayer() { return player; },
      getApi() { return api; },
      dispose() {
        disposed = true;   // a late soundFontsListed fan-out must not touch a destroyed api
        liveEngines.delete(handle);
        try { api.destroy(); } catch (_) { /* already torn down */ }
      },
    };

    // Only player-mode engines make sound, so only those join the stopAll() registry (a lite render-only
    // preview has no synth to stop).
    if (player) liveEngines.add(handle);
    return handle;
  }

  return { create, stopAll, DEFAULT_SOUNDFONT, fontUrl };
})();
