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
// An opt-in `debugPanel` adds a collapsed alphaTex scratchpad under the staff: it shows the tex this component
// last rendered, and lets it be edited and re-rendered through THIS alphaTab instance (Render from alphaTex)
// — bypassing C#, the tightest loop for triaging the engine↔alphaTab seam. (Replaces the standalone Debug view.)
//
//   const view = ChordFlowScore.create(containerEl, {
//     player: true,            // false = lite render-only (no soundfont, no transport)
//     controls: "full",        // "full" | "mini" | "none"
//     debugPanel: true,        // adds a collapsed editable alphaTex panel under the staff (default false)
//     tripletFeel: true,       // adds a whole-song feel (swing) select to the transport (default false)
//     options: { metronome:false, countIn:false, chordNames:false, diagrams:false, voicing:"byDifficulty" },
//     onBeat:(bar,beat)=>…, onStateChange:(playing)=>…, onFinished:()=>…, onNeedsRerender:(ro)=>…,
//   });
//   view.load(tex, { tempo }); view.play(); view.stop(); view.setTempo(bpm);
//   view.setOption("chordNames", true); view.getRenderOptions(); view.getTripletFeel(); view.dispose();
"use strict";

window.ChordFlowScore = (function () {
  // Player-state enum, resolved defensively (the minified bundle may shuffle namespaces).
  const PlayerState =
    (alphaTab.synth && alphaTab.synth.PlayerState) ||
    alphaTab.PlayerState ||
    { Paused: 0, Playing: 1 };

  const PLAYER_KIND = new Set(["metronome", "countIn"]);   // applied via the alphaTab API
  const CONTENT_KIND = new Set(["chordNames", "diagramsOverStaff", "diagramsOnTop", "voicing"]); // require a C# re-render
  const DISPLAY_KIND = new Set(["autoLayout"]); // applied locally via updateSettings()+render() — no re-render request

  const DEFAULT_OPTIONS = {
    metronome: false,
    countIn: false,
    chordNames: true,        // default selected
    diagramsOverStaff: false,
    diagramsOnTop: true,     // default selected
    voicing: { kind: "automatic" }, // comping voicing source (engine-derived-as-app-source IN6); no UI picker yet → engine default (full neck, Closest)
    autoLayout: false,       // false = honor the score's defaultSystemsLayout (fixed bars/row); true = fit to width
    staffProfile: "tab",     // tab (default) | standard | both — display-only, persisted globally host-side
  };

  // Staff-display profile → the two per-staff alphaTab model flags it sets. A display-only choice (which staves
  // alphaTab shows) over unchanged content: no alphaTex/C# round-trip, the barsPerRow sibling. `both` is
  // alphaTab's no-`\staff` default, so it reproduces today's combined render byte-for-byte.
  const STAFF_FLAGS = {
    tab:      { std: false, tab: true },
    standard: { std: true,  tab: false },
    both:     { std: true,  tab: true },
  };
  const STAFF_PROFILES = [
    { value: "tab", label: "Tab" },
    { value: "standard", label: "Standard" },
    { value: "both", label: "Both" },
  ];

  // Triplet feel (swing) — a render/playback knob like tempo, delegated to alphaTab's \tf. Values are the C#
  // TripletFeel enum names (the bridge parses them by name); only these three are wired today. Changing it is
  // content-kind (the \tf line changes the alphaTex), so it re-renders via onNeedsRerender like a content toggle.
  const TRIPLET_FEELS = [
    { value: "None", label: "Straight" },
    { value: "Triplet8th", label: "Triplet 8th (swing)" },
    { value: "Triplet16th", label: "Triplet 16th" },
  ];

  // Bars-per-row control. The score's authored `defaultSystemsLayout` only takes effect on multi-track scores
  // (and needs UseModelLayout), so it's unreliable as the single knob. `display.barsPerRow` on the Page layout
  // is the global control that works for single- AND multi-track alike: 4 = fixed four bars per row, -1 =
  // automatic (fit to width, alphaTab's default). "Auto layout" toggles between them.
  function layoutDisplay(auto) {
    // justifyLastSystem: fixed 4-bar layout stretches the last (partial) row to full width;
    // auto (fit-to-width) leaves it natural.
    return { layoutMode: alphaTab.LayoutMode.Page, barsPerRow: auto ? -1 : 4, justifyLastSystem: !auto };
  }

  // The soundfont shipped in the repo — the boot default and the fallback the host falls back to when no
  // choice is stored. The picker lists whatever the host discovers; ids are file names under soundfont/.
  const DEFAULT_SOUNDFONT = "sonivox.sf2";
  function fontUrl(id) { return "soundfont/" + id; }

  // A minimal valid score so the debug panel's Render works before any host score has arrived (dev / first run).
  const DEBUG_SAMPLE_TEX = ['\\title "Scratch"', ".", ":4 3.3 3.3 3.3 3.3 |"].join("\n");

  // The single alphaTab settings source of truth. Player settings are added only in player mode so a
  // lite preview never pays the soundfont/worker-player cost.
  function buildSettings(player, options, scroll) {
    const settings = {
      core: {
        fontDirectory: "font/",   // relative to index.html, served same-origin under the virtual host
        useWorkers: true,         // real https origin → layout worker is allowed off the main thread
      },
      // Honor the engine's authored `defaultSystemsLayout N` unless the user flips to auto (fit-to-width).
      display: layoutDisplay(!!(options && options.autoLayout)),
    };
    if (player) {
      settings.player = {
        enablePlayer: true,
        enableCursor: true,
        enableAnimatedBeatCursor: true,
        enableElementHighlighting: true,
        soundFont: fontUrl(DEFAULT_SOUNDFONT),   // boot default; replaced live once the host reports the saved choice
        // Auto-follow the cursor only when the consumer opts in (Practice); scrollElement + a bounded surface
        // are wired after the api exists (see applyScroll). Off by default keeps the Content preview's free layout.
        // OffScreen page-flips the surface only when the cursor would leave view — no per-frame creep (Smooth's
        // problem) and no cross-row offset miscompute (Continuous's). The native browser smooth-scroll animates
        // each flip so it glides instead of snapping.
        scrollMode: scroll ? alphaTab.ScrollMode.OffScreen : alphaTab.ScrollMode.Off,
        nativeBrowserSmoothScroll: scroll,
      };
    }
    return settings;
  }

  function create(container, opts) {
    opts = opts || {};
    const player = opts.player !== false;                 // default true
    const controls = opts.controls || (player ? "full" : "none");
    const debugPanel = !!opts.debugPanel;                 // opt-in alphaTex scratchpad, default off
    const tripletFeelEnabled = !!opts.tripletFeel;        // opt-in feel select (Practice only), default off
    let tripletFeel = "None";                             // current whole-song feel (C# TripletFeel name)
    const options = Object.assign({}, DEFAULT_OPTIONS, opts.options || {});
    const cb = {
      onBeat: opts.onBeat || function () {},
      onStateChange: opts.onStateChange || function () {},
      onFinished: opts.onFinished || function () {},
      onNeedsRerender: opts.onNeedsRerender || function () {},
      onToggleNowNext: opts.onToggleNowNext || function () {},
    };

    container.classList.add("cf-score");
    const surface = document.createElement("div");
    surface.className = "cf-score-surface";

    const api = new alphaTab.AlphaTabApi(surface, buildSettings(player, options, opts.scroll));

    // Auto-follow the cursor by scrolling the score's OWN bounded surface (Model A) — never the window, which
    // would carry the Now/Next boards + transport off the top. Mode is "off" | "offscreen" | "continuous":
    //   - offscreen  → page-flip only when the cursor would leave view; nativeBrowserSmoothScroll animates the flip.
    //   - continuous → keep the cursor in view every beat; native smooth-scroll OFF so it doesn't fight the
    //                  per-frame repositioning (it would rubber-band).
    //   - off        → release the bound so the full score sits in normal flow for manual scrolling.
    // Both follow modes share the same 60vh bound + scrollOffsetY headroom; switching them is just scrollMode +
    // the paired nativeBrowserSmoothScroll. The transport mode-select flips this live via handle.setScrollMode.
    const SCROLL_MODES = { off: alphaTab.ScrollMode.Off, offscreen: alphaTab.ScrollMode.OffScreen, continuous: alphaTab.ScrollMode.Continuous };
    let scrollMode = "off";
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

    let baseTempo = 80;   // the score's authored \tempo; runtime tempo scales off it
    let lastHostTex = null;   // the alphaTex this component last rendered from a host load() — for the debug panel
    let debugDirty = false;   // user has edited the debug textarea; host re-renders stop overwriting it until reload

    // Mirror host output into the debug textarea — unless the user has unsaved edits (dirty), in which case the
    // edits are preserved and we surface a hint that the engine pushed something newer (Reload to pull it in).
    function syncDebugTextarea(tex) {
      if (!debugUi.textarea) return;
      if (debugDirty) {
        debugUi.hint.textContent = "engine output changed — Reload from engine";
        return;
      }
      debugUi.textarea.value = tex || "";
      debugUi.hint.textContent = "";
    }

    // Re-apply the layout pair at runtime when "Auto layout" toggles (display-only — no C# re-render).
    function applyLayout() {
      Object.assign(api.settings.display, layoutDisplay(options.autoLayout));
      api.updateSettings();
      api.render();
    }

    // Per-track playback volumes (player-kind, local — never part of renderOptions). Rhythm = track 0,
    // Lead = track 1 (present only for a two-track exercise; a no-op otherwise). alphaTab rebuilds the tracks
    // on every load, so these are re-asserted on scoreLoaded.
    const trackVolumes = { rhythm: 1, lead: 1 };
    function applyTrackVolume(which) {
      if (!player || !api.score || !api.score.tracks) return;
      const track = api.score.tracks[which === "lead" ? 1 : 0];
      if (track && typeof api.changeTrackVolume === "function") {
        api.changeTrackVolume([track], trackVolumes[which]);
      }
    }

    // Staff-display profile (tab/standard/both): set the per-staff showStandardNotation/showTablature model
    // flags on every staff of every track from the current option. Display-only — no alphaTex/C# round-trip
    // (C6). Called in scoreLoaded BEFORE the render so the pending render honors it (the flags are read at
    // render time, like the diagrams-on-top stylesheet flag) — a freshly loaded score never flashes the default.
    function setStaffFlags(score) {
      const f = STAFF_FLAGS[options.staffProfile] || STAFF_FLAGS.tab;
      if (!score || !score.tracks) return;
      for (const track of score.tracks) {
        for (const staff of (track.staves || [])) {
          staff.showStandardNotation = f.std;
          staff.showTablature = f.tab;
        }
      }
    }

    // Runtime profile change (user picked from the select, or the host's saved value arrived): record the
    // choice, set the flags, and re-render through the same path scoreLoaded uses (renderTracks for a
    // multi-track score, else render). A no-op render-wise until a score exists; the eventual scoreLoaded applies it.
    function applyStaffProfile(profile) {
      options.staffProfile = STAFF_FLAGS[profile] ? profile : "tab";
      if (ui.staffProfile) ui.staffProfile.value = options.staffProfile;
      if (!api.score || !api.score.tracks) return;
      setStaffFlags(api.score);
      if (api.score.tracks.length > 1) api.renderTracks(api.score.tracks);
      else api.render();
    }

    // Host reply to getStaffProfile: adopt the persisted profile (coalescing an unknown/blank value to "tab").
    function onStaffProfile(profile) {
      if (disposed) return;
      applyStaffProfile(STAFF_FLAGS[profile] ? profile : "tab");
    }

    // "Diagrams on top" has no alphaTex directive — it's the score stylesheet's globalDisplayChordDiagramsOnTop
    // flag (defaults to shown when chords are defined). Set it from the current option each time a score
    // loads, so the top list shows/hides independently of the over-staff boxes (driven from the alphaTex).
    api.scoreLoaded.on((score) => {
      if (score && score.stylesheet) {
        score.stylesheet.globalDisplayChordDiagramsOnTop = !!options.diagramsOnTop;
      }
      // Re-assert the staff-display profile (the score model is rebuilt on every load, so the flags reset to
      // alphaTab's default both-staves otherwise) — set before the render below so it takes effect in one pass.
      setStaffFlags(score);
      // alphaTab renders only the FIRST track by default, so a two-track exercise (comping + lead) would
      // hide the lead staff. Render every track the score defines so both staves show. Only intervene when
      // there's more than one track — a single-track score keeps the default render untouched.
      if (score && score.tracks && score.tracks.length > 1) {
        api.renderTracks(score.tracks);
      }
      // Re-assert per-track volumes for the freshly loaded score (tracks are rebuilt on every load).
      applyTrackVolume("rhythm");
      applyTrackVolume("lead");
    });

    // Control refs, populated by buildControls when a strip is rendered.
    const ui = { play: null, stop: null, tempo: null, soundFont: null, tripletFeel: null, staffProfile: null, toggles: {} };
    // Debug-panel refs, populated by buildDebugPanel when debugPanel is on.
    const debugUi = { textarea: null, hint: null };

    // Playback soundfont. The choice is a global host setting; this component requests the list on init and
    // applies the host's persisted selection live. `currentSoundFont` mirrors what's loaded so we skip a
    // redundant reload when the saved choice already matches the boot default.
    let currentSoundFont = DEFAULT_SOUNDFONT;
    let disposed = false;
    const bridge = (typeof window !== "undefined" && window.ChordFlowBridge) || null;

    // Swap the active synth soundfont live (no re-render, no persist). The bundled alphaTab loads a font by
    // URL via loadSoundFontFromUrl(url, append=false); updating settings.player.soundFont keeps any internal
    // reload consistent.
    function applySoundFont(id) {
      if (!id || disposed) return;
      currentSoundFont = id;
      if (api.settings && api.settings.player) api.settings.player.soundFont = fontUrl(id);
      if (typeof api.loadSoundFontFromUrl === "function") api.loadSoundFontFromUrl(fontUrl(id), false);
    }

    // Host reply: fill the picker (if shown) and apply the persisted selection (even without a picker).
    function onSoundFontsListed(msg) {
      if (disposed) return;
      const fonts = (msg && msg.fonts) || [];
      if (ui.soundFont) {
        ui.soundFont.innerHTML = "";
        for (const f of fonts) {
          const opt = document.createElement("option");
          opt.value = f.id;
          opt.textContent = f.name;
          ui.soundFont.appendChild(opt);
        }
      }
      const selected = msg && msg.selectedId;
      if (selected) {
        if (ui.soundFont) ui.soundFont.value = selected;
        if (selected !== currentSoundFont) applySoundFont(selected);
      }
    }

    function applyPlayerOption(name, value) {
      if (!player) return;
      if (name === "metronome") api.metronomeVolume = value ? 1 : 0;
      else if (name === "countIn") api.countInVolume = value ? 1 : 0;
    }

    function reflectPlayState(playing) {
      if (ui.play) ui.play.textContent = playing ? "⏸ Pause" : "▶ Play";
    }

    // Keep a toggle checkbox in sync when its option is set programmatically (e.g. the on-top coupling).
    function syncToggle(name, value) {
      const toggle = ui.toggles[name];
      if (toggle && toggle.checked !== !!value) toggle.checked = !!value;
    }

    function setTransportEnabled(enabled) {
      [ui.play, ui.stop, ui.tempo].forEach((el) => { if (el) el.disabled = !enabled; });
    }

    const handle = {
      // Render an alphaTex string. `tempo` (the score's authored BPM) re-bases setTempo's speed multiplier.
      load(tex, o) {
        if (o && o.tempo) baseTempo = o.tempo;
        if (ui.tempo) ui.tempo.value = String(baseTempo);
        lastHostTex = tex;
        syncDebugTextarea(tex);   // mirror into the debug panel (no-op when off / preserved when dirty)
        api.tex(tex);
      },
      play() { api.playPause(); },
      stop() { api.stop(); },
      // Translate absolute BPM into alphaTab's playbackSpeed multiplier (1.0 = authored tempo) — no re-render.
      setTempo(bpm) { if (bpm && baseTempo) api.playbackSpeed = bpm / baseTempo; },
      // Per-track playback volume (0..1). which = "rhythm" | "lead"; lead is a no-op on a single-track score.
      setTrackVolume(which, value) {
        trackVolumes[which] = value;
        applyTrackVolume(which);
      },
      // User picked a soundfont: apply it live and persist the new global choice host-side.
      setSoundFont(id) {
        applySoundFont(id);
        if (bridge) bridge.send({ type: "setSoundFont", id });
      },
      // User picked a staff-display profile (tab/standard/both): apply it live (display-only, no re-render
      // request) and persist the new global choice host-side, mirroring the soundfont path.
      setStaffProfile(profile) {
        applyStaffProfile(profile);
        if (bridge) bridge.send({ type: "setStaffProfile", profile });
      },
      // Player-kind → applied locally; content-kind → ask the consumer to re-render with the new options.
      setOption(name, value) {
        options[name] = value;
        syncToggle(name, value);
        if (PLAYER_KIND.has(name)) {
          applyPlayerOption(name, value);
          return;
        }
        if (DISPLAY_KIND.has(name)) {
          applyLayout();
          return;
        }
        if (CONTENT_KIND.has(name)) {
          // Coupling: diagrams on top without over-staff leaves the staff with no chord indication, so
          // auto-enable chord names (still user-overridable afterwards).
          if ((name === "diagramsOnTop" || name === "diagramsOverStaff") &&
              options.diagramsOnTop && !options.diagramsOverStaff && !options.chordNames) {
            options.chordNames = true;
            syncToggle("chordNames", true);
          }
          cb.onNeedsRerender(handle.getRenderOptions());
        }
      },
      // The renderOptions payload to attach to a C# render request (generate / entityPreview / loadExercise).
      getRenderOptions() {
        return {
          showChordNames: !!options.chordNames,
          showChordDiagramsOverStaff: !!options.diagramsOverStaff,
          showChordDiagramsOnTop: !!options.diagramsOnTop,
          voicing: options.voicing,
        };
      },
      // Auto-follow mode ("off" | "offscreen" | "continuous") — flips scrollMode + the bounded-surface binding
      // + the paired nativeBrowserSmoothScroll live (see applyScrollMode).
      setScrollMode(mode) { applyScrollMode(mode); },
      // Ask the consumer to show/hide its Now/Next fretboards (the component doesn't own that container).
      toggleNowNext(visible) { cb.onToggleNowNext(!!visible); },
      getApi() { return api; },
      // The current tempo shown in the transport (BPM), else the loaded score's authored tempo. Lets a
      // consumer carry the user's tempo choice onto the next generate request.
      getTempo() {
        const shown = ui.tempo ? parseInt(ui.tempo.value, 10) : NaN;
        return shown || baseTempo;
      },
      // The current whole-song triplet feel (C# TripletFeel name). Tempo's twin — a component-owned value the
      // consumer carries onto the next render request (kept OUT of getRenderOptions; it's a first-class param).
      getTripletFeel() { return tripletFeel; },
      // Set the feel and ask the consumer to re-render: the \tf line changes the alphaTex, so this is
      // content-kind (harmony unchanged → a cheap re-emit, not a regenerate).
      setTripletFeel(value) {
        tripletFeel = value;
        cb.onNeedsRerender(handle.getRenderOptions());
      },
      dispose() {
        disposed = true;   // a late soundFontsListed fan-out must not touch a destroyed api
        try { api.destroy(); } catch (_) { /* already torn down */ }
        container.innerHTML = "";
        container.classList.remove("cf-score");
      },
    };

    // The opt-in alphaTex debug panel (collapsed). Edits the rendered tex and re-renders through THIS component's
    // alphaTab instance — bypassing C#. Dirty-state (see syncDebugTextarea): once edited, host re-renders stop
    // overwriting the textarea until "Reload from engine". alphaTex is never built here; this only feeds api.tex().
    function buildDebugPanel() {
      const panel = document.createElement("details");
      panel.className = "cf-debug";

      const summary = document.createElement("summary");
      summary.textContent = "alphaTex";
      const version = typeof alphaTab !== "undefined" && alphaTab.meta && alphaTab.meta.version;
      if (version) {
        const ver = document.createElement("span");
        ver.className = "cf-debug-version";
        ver.textContent = "alphaTab v" + version;
        summary.appendChild(ver);
      }

      const textarea = document.createElement("textarea");
      textarea.className = "cf-debug-tex";
      textarea.spellcheck = false;
      textarea.placeholder = "alphaTex the engine rendered — edit and Render from alphaTex, or Reload from engine.";
      textarea.value = lastHostTex || "";   // a score may already have rendered before the panel built
      textarea.addEventListener("input", () => {
        debugDirty = true;
        debugUi.hint.textContent = "";
      });
      debugUi.textarea = textarea;

      const bar = document.createElement("div");
      bar.className = "cf-debug-bar";
      const renderBtn = button("Render from alphaTex", () => {
        api.tex(textarea.value.trim() || DEBUG_SAMPLE_TEX);
      });
      renderBtn.className = "primary";
      const reloadBtn = button("Reload from engine", () => {
        debugDirty = false;
        textarea.value = lastHostTex || "";
        debugUi.hint.textContent = "";
        api.tex(textarea.value.trim() || DEBUG_SAMPLE_TEX);
      });
      const hint = document.createElement("span");
      hint.className = "cf-debug-hint";
      debugUi.hint = hint;
      bar.append(renderBtn, reloadBtn, hint);

      panel.append(summary, textarea, bar);
      return panel;
    }

    const strip = buildControls(player, controls, options, handle, ui, tripletFeelEnabled, {
      scrollMode,
      nowNextToggle: !!opts.onToggleNowNext,
    });
    if (strip) container.appendChild(strip);
    container.appendChild(surface);
    if (debugPanel) container.appendChild(buildDebugPanel());

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
        // alphaTab's ActiveBeatsChangedEventArgs.activeBeats is a Beat[]; tolerate a { beats: [] } wrapper too.
        const active = e && e.activeBeats;
        const beats = Array.isArray(active) ? active : active && active.beats;
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

      // Ask the host which soundfonts exist + which is the saved choice; the reply fills the picker and applies
      // the selection. Feature-detected: in a plain browser (no host) the boot default stays in effect.
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

    // Staff-display profile (tab/standard/both): a global, display-only score-view preference. Request the saved
    // value on init and apply it; a new choice is persisted host-side via setStaffProfile. Runs in BOTH player
    // and lite modes — every score view honors the profile. Feature-detected: in a plain browser (no host) the
    // "tab" default stays in effect.
    if (bridge && bridge.available) {
      bridge.onReceive((data) => {
        let msg;
        try { msg = typeof data === "string" ? JSON.parse(data) : data; }
        catch (_) { return; }
        if (msg && msg.type === "staffProfile") onStaffProfile(msg.profile);
      });
      bridge.send({ type: "getStaffProfile" });
    }

    return handle;
  }

  // Build the control strip per profile. Transport + player-kind toggles need the player; content-kind
  // toggles render only in the "full" profile. Returns null when nothing is rendered (mini render-only / none).
  function buildControls(player, controls, options, handle, ui, tripletFeelEnabled, extra) {
    if (controls === "none") return null;
    extra = extra || {};

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

    if (tripletFeelEnabled) {
      strip.append(tripletFeelPicker(handle, ui));
    }

    if (player && controls === "full") {
      strip.append(
        scrollModeSelect(handle, ui, extra.scrollMode),
        toggle("metronome", "Metronome", options, handle, ui),
        toggle("countIn", "Count-in", options, handle, ui),
        volumeSlider("rhythm", "Rhythm vol", handle, ui),
        volumeSlider("lead", "Lead vol", handle, ui),
        soundFontPicker(handle, ui),
      );
    }

    if (extra.nowNextToggle) {
      strip.append(nowNextToggle(handle, ui));
    }

    // Staff-display profile — a display-only knob over any shown score, so it appears in both the full
    // (Practice) and mini (Content preview) profiles. The persisted global value applies everywhere.
    if (controls === "full" || controls === "mini") {
      strip.append(staffProfileSelect(handle, ui, options));
    }

    if (controls === "full") {
      strip.append(
        toggle("chordNames", "Chord names", options, handle, ui),
        toggle("diagramsOverStaff", "Diagrams over staff", options, handle, ui),
        toggle("diagramsOnTop", "Diagrams on top", options, handle, ui),
        toggle("autoLayout", "Auto layout", options, handle, ui),
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

  // Auto-follow mode select (player-kind, local). Not a render-`options` toggle: it flips scrollMode + the
  // bounded-surface binding live via handle.setScrollMode, no re-render. Off / OffScreen / Continuous so both
  // follow modes can be A/B-tested live. Starts from the consumer's initial mode.
  const SCROLL_MODE_OPTIONS = [
    { value: "off", label: "Off" },
    { value: "offscreen", label: "OffScreen" },
    { value: "continuous", label: "Continuous" },
  ];
  function scrollModeSelect(handle, ui, initial) {
    const wrap = document.createElement("label");
    wrap.className = "cf-toggle";
    const select = document.createElement("select");
    select.className = "cf-scroll-mode";
    for (const m of SCROLL_MODE_OPTIONS) {
      const o = document.createElement("option");
      o.value = m.value;
      o.textContent = m.label;
      select.appendChild(o);
    }
    select.value = initial || "off";
    select.addEventListener("change", () => handle.setScrollMode(select.value));
    wrap.append(document.createTextNode("Scroll "), select);
    ui.toggles.scrollMode = select;
    return wrap;
  }

  // Show/hide the consumer's Now/Next fretboards. The component doesn't own that container — it just fires
  // handle.toggleNowNext and lets the consumer (app.js) flip its visibility. Defaults visible.
  function nowNextToggle(handle, ui) {
    const wrap = document.createElement("label");
    wrap.className = "cf-toggle";
    const input = document.createElement("input");
    input.type = "checkbox";
    input.checked = true;
    input.addEventListener("change", () => handle.toggleNowNext(input.checked));
    wrap.append(input, document.createTextNode(" Now/Next"));
    ui.toggles.nowNext = input;
    return wrap;
  }

  // The triplet-feel (swing) picker. Content-kind: a change re-renders via handle.setTripletFeel. Values are
  // the C# TripletFeel enum names; the consumer reads the choice with handle.getTripletFeel().
  function tripletFeelPicker(handle, ui) {
    const wrap = document.createElement("label");
    wrap.className = "cf-toggle";
    const select = document.createElement("select");
    select.className = "cf-feel";
    for (const f of TRIPLET_FEELS) {
      const o = document.createElement("option");
      o.value = f.value;
      o.textContent = f.label;
      select.appendChild(o);
    }
    select.value = handle.getTripletFeel();
    select.addEventListener("change", () => handle.setTripletFeel(select.value));
    wrap.append(document.createTextNode("Feel "), select);
    ui.tripletFeel = select;
    return wrap;
  }

  // The soundfont picker (player-kind, local apply + host persist). Starts empty; populated by the host's
  // soundFontsListed reply. Hidden in plain-browser/no-host runs (no list ever arrives, options stay empty).
  function soundFontPicker(handle, ui) {
    const wrap = document.createElement("label");
    wrap.className = "cf-toggle";
    const select = document.createElement("select");
    select.className = "cf-soundfont";
    select.addEventListener("change", () => handle.setSoundFont(select.value));
    wrap.append(document.createTextNode("Sound "), select);
    ui.soundFont = select;
    return wrap;
  }

  // The staff-display profile picker (Tab / Standard / Both). Display-only: a change flips the per-staff
  // showStandardNotation/showTablature flags locally via handle.setStaffProfile (no re-render request) and
  // persists the global choice host-side. Starts from the current option (default "tab", overwritten when the
  // host's saved value arrives).
  function staffProfileSelect(handle, ui, options) {
    const wrap = document.createElement("label");
    wrap.className = "cf-toggle";
    const select = document.createElement("select");
    select.className = "cf-staff-profile";
    for (const p of STAFF_PROFILES) {
      const o = document.createElement("option");
      o.value = p.value;
      o.textContent = p.label;
      select.appendChild(o);
    }
    select.value = options.staffProfile || "tab";
    select.addEventListener("change", () => handle.setStaffProfile(select.value));
    wrap.append(document.createTextNode("Staff "), select);
    ui.staffProfile = select;
    return wrap;
  }

  // A per-track volume slider (0..1). Player-kind: applied locally via the alphaTab API, never re-rendered.
  function volumeSlider(which, label, handle, ui) {
    const wrap = document.createElement("label");
    wrap.className = "cf-toggle";
    const input = document.createElement("input");
    input.type = "range";
    input.min = "0"; input.max = "1"; input.step = "0.05"; input.value = "1";
    input.addEventListener("input", () => handle.setTrackVolume(which, parseFloat(input.value)));
    wrap.append(document.createTextNode(label + " "), input);
    ui.toggles[which + "Vol"] = input;
    return wrap;
  }

  return { create };
})();
