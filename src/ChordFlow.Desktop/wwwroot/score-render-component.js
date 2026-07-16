// ChordFlow shared score render component.
//
// The single owner of "alphaTex string → alphaTab notation + (optional) playback". Every screen that
// shows a score uses it: the Practice view (app.js, full player) and the Content-CRUD preview
// (content-crud.js, lite render-only). It centralizes the alphaTab settings (one source of truth, no
// per-consumer drift) and the on/off render options.
//
// The alphaTab api + transport + audio + soundfont + beat events live in a shared **ChordFlowPlayback**
// engine (playback-component.js); ScoreR composes one and reaches through engine.getApi() for its own
// render/notation concerns. ScoreR owns the visible staff, the control strip, the notation-display
// options, the staff-display profile, the key/feel/tempo pickers, and the alphaTex debug panel. Its public
// handle is unchanged — Practice and Content-preview consume it exactly as before.
//
// Options split in two (the load-bearing distinction):
//   • player-kind  (metronome, count-in) — applied locally via the engine, no round-trip.
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
//     key: true,               // adds a Key select to the transport (Content preview; Practice uses HarmonyControlsR)
//     volumes: false,          // hides the Rhythm/Lead sliders (default true; Practice puts them in HarmonyControlsR)
//     transport: false,        // skip the in-strip PlayerControlsR (default true; the Practice shell mounts its
//                              // own at page level, bound to view.getEngine(), so it survives the view toggle)
//     options: { chordNames:false, diagrams:false, voicing:"byDifficulty" },   // metronome/count-in live in PlayerControlsR
//     onBeat:(bar,beat)=>…, onStateChange:(playing)=>…, onFinished:()=>…, onNeedsRerender:(ro)=>…,
//   });
//   view.load(tex, { tempo }); view.play(); view.stop(); view.setTempo(bpm);
//   // Render params (ScoreR-owned, seeded per content): key + feel re-emit via onNeedsRerender; tempo is local.
//   view.getKey(); view.seedKey(pc); view.setKey(pc);  view.getTempo(); view.seedTempo(bpm);
//   view.getTripletFeel(); view.seedTripletFeel(v); view.setTripletFeel(v);
//   view.setOption("chordNames", true); view.getRenderOptions(); view.dispose();
"use strict";

window.ChordFlowScore = (function () {
  const CONTENT_KIND = new Set(["chordNames", "diagramsOverStaff", "diagramsOnTop", "voicing"]); // require a C# re-render
  const DISPLAY_KIND = new Set(["autoLayout"]); // applied locally via updateSettings()+render() — no re-render request

  const DEFAULT_OPTIONS = {
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

  // Key names per tonic pitch class (0 = C .. 11 = B), spelled to match the renderer's \ks (mirrors app.js).
  // The Key control is a render/interpretation param like feel: changing it re-emits the alphaTex in a new key
  // (a transpose), so it's content-kind and routes through onNeedsRerender. Shown for key-independent content
  // (progression/rhythm) too, defaulted to C — transposing just realizes the degrees into that key.
  const KEY_NAMES = ["C", "Db", "D", "Eb", "E", "F", "F#", "G", "Ab", "A", "Bb", "B"];

  // Bars-per-row control. The score's authored `defaultSystemsLayout` only takes effect on multi-track scores
  // (and needs UseModelLayout), so it's unreliable as the single knob. `display.barsPerRow` on the Page layout
  // is the global control that works for single- AND multi-track alike: 4 = fixed four bars per row, -1 =
  // automatic (fit to width, alphaTab's default). "Auto layout" toggles between them.
  function layoutDisplay(auto) {
    // justifyLastSystem: fixed 4-bar layout stretches the last (partial) row to full width;
    // auto (fit-to-width) leaves it natural.
    return { layoutMode: alphaTab.LayoutMode.Page, barsPerRow: auto ? -1 : 4, justifyLastSystem: !auto };
  }

  // A minimal valid score so the debug panel's Render works before any host score has arrived (dev / first run).
  const DEBUG_SAMPLE_TEX = ['\\title "Scratch"', ".", ":4 3.3 3.3 3.3 3.3 |"].join("\n");

  function create(container, opts) {
    opts = opts || {};
    const player = opts.player !== false;                 // default true
    const controls = opts.controls || (player ? "full" : "none");
    const debugPanel = !!opts.debugPanel;                 // opt-in alphaTex scratchpad, default off
    const tripletFeelEnabled = !!opts.tripletFeel;        // opt-in feel select (Practice only), default off
    let tripletFeel = "None";                             // current whole-song feel (C# TripletFeel name)
    const keyEnabled = !!opts.key;                        // opt-in Key control (Content preview), default off
    let key = 0;                                          // current key tonic pitch class (0 = C); ScoreR-owned
    const volumesEnabled = opts.volumes !== false;        // Rhythm/Lead sliders (default on); Practice mounts
                                                          // them off — they live in HarmonyControlsR there (C2)
    const transportEnabled = opts.transport !== false;    // mount PlayerControlsR in the strip (default on);
                                                          // the Practice shell mounts its OWN pc at page level
                                                          // (bound via getEngine()) so it survives the view toggle
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

    let lastHostTex = null;   // the alphaTex this component last rendered from a host load() — for the debug panel
    let debugDirty = false;   // user has edited the debug textarea; host re-renders stop overwriting it until reload
    let disposed = false;

    // Control refs, populated by buildControls when a strip is rendered.
    const ui = { key: null, tripletFeel: null, staffProfile: null, toggles: {} };
    let pc = null;   // the shared PlayerControlsR (transport/metronome/count-in/now-next), created before buildControls
    // Debug-panel refs, populated by buildDebugPanel when debugPanel is on.
    const debugUi = { textarea: null, hint: null };

    // Keep a toggle checkbox in sync when its option is set programmatically (e.g. the on-top coupling).
    function syncToggle(name, value) {
      const toggle = ui.toggles[name];
      if (toggle && toggle.checked !== !!value) toggle.checked = !!value;
    }

    // The shared playback engine owns the alphaTab api + transport + audio + soundfont + beat/state events.
    // ScoreR grabs its api for all render/notation concerns below, and forwards the engine's events to the
    // consumer callbacks. The engine renders into `surface`; ScoreR appends `surface` to the container.
    const engine = ChordFlowPlayback.create(surface, {
      player,
      scroll: !!opts.scroll,
      display: layoutDisplay(!!options.autoLayout),   // ScoreR owns layout; the engine takes the initial blob
      onBeat: (bar, beat) => cb.onBeat(bar, beat),
      onStateChange: (playing) => cb.onStateChange(playing),
      onFinished: () => cb.onFinished(),
      // `ready` + `soundFontsListed` are consumed by the shared PlayerControlsR via its own engine.on(...)
      // subscriptions (it enables the transport and fills the soundfont picker) — ScoreR no longer wires them.
    });
    const api = engine.getApi();
    // Debug hook (default off): when the host sets window.__cfDebug (via the CHORDFLOW_DEVTOOLS env var),
    // expose the ScoreR engine + api for live devtools inspection. Inert in normal runs; kept for bug hunts.
    if (player && window.__cfDebug) { window.__cfEngine = engine; window.__cfApi = api; }

    // Auto-follow mode ("off" | "offscreen" | "continuous") lives in the engine; ScoreR just tracks the
    // initial value for the transport select and delegates changes.
    let scrollMode = opts.scroll ? "offscreen" : "off";

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
    // Runs alongside the engine's own scoreLoaded handler (which re-asserts per-track volumes).
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
    });

    const handle = {
      // Render an alphaTex string. `tempo` (the score's authored BPM) re-bases the engine's speed multiplier.
      load(tex, o) {
        engine.load(tex, o);
        if (pc) pc.setTempoValue(engine.getBaseTempo());
        lastHostTex = tex;
        syncDebugTextarea(tex);   // mirror into the debug panel (no-op when off / preserved when dirty)
      },
      play() { engine.play(); },
      stop() { engine.stop(); },
      // Translate absolute BPM into the engine's playbackSpeed multiplier (1.0 = authored tempo) — no re-render.
      setTempo(bpm) { engine.setTempo(bpm); },
      // Seed the tempo from selected content WITHOUT re-rendering (the twin of seedTripletFeel/seedKey): tempo is
      // a LOCAL playback-speed param, never a C# re-emit (unlike key/feel). Sets baseTempo + the input so the next
      // render/generate carries it and getTempo() returns it. The following load(tex,{tempo}) re-bases as usual.
      seedTempo(bpm) {
        if (!bpm) return;
        engine.seedTempo(bpm);
        if (pc) pc.setTempoValue(bpm);
      },
      // Per-track playback volume (0..1). which = "rhythm" | "lead"; lead is a no-op on a single-track score.
      setTrackVolume(which, value) { engine.setTrackVolume(which, value); },
      // User picked a soundfont: the engine applies it live and persists the new global choice host-side.
      setSoundFont(id) { engine.setSoundFont(id); },
      // User picked a staff-display profile (tab/standard/both): apply it live (display-only, no re-render
      // request) and persist the new global choice host-side, mirroring the soundfont path.
      setStaffProfile(profile) {
        applyStaffProfile(profile);
        if (bridge) bridge.send({ type: "setStaffProfile", profile });
      },
      // Player-kind → applied via the engine; content-kind → ask the consumer to re-render with the new options.
      setOption(name, value) {
        options[name] = value;
        syncToggle(name, value);
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
      // Auto-follow mode ("off" | "offscreen" | "continuous") — delegated to the engine (scrollMode + the
      // bounded-surface binding + the paired nativeBrowserSmoothScroll).
      setScrollMode(mode) { scrollMode = mode; engine.setScrollMode(mode); },
      // Ask the consumer to show/hide its Now/Next fretboards (the component doesn't own that container).
      toggleNowNext(visible) { cb.onToggleNowNext(!!visible); },
      getApi() { return engine.getApi(); },
      // The underlying ChordFlowPlayback handle — for a shell that binds page-level controls to ScoreR's
      // engine (PlayerControlsR with transport:false, HarmonyControlsR's volume sliders). One page, one engine.
      getEngine() { return engine; },
      // The current tempo shown in the transport (BPM), else the loaded score's authored tempo. Lets a
      // consumer carry the user's tempo choice onto the next generate request.
      getTempo() {
        const shown = pc ? pc.getTempo() : 0;
        return shown || engine.getBaseTempo();
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
      // Seed the feel from selected content WITHOUT re-rendering (the twin of the key-picker seed): updates the
      // component-owned value + the picker UI so the next generate carries it, but no \tf re-emit yet. Song
      // selection calls this (song-default-feel IN4); a manual change afterwards still wins (C6).
      seedTripletFeel(value) {
        tripletFeel = value;
        if (ui.tripletFeel) ui.tripletFeel.value = value;
      },
      // The current key tonic pitch class (0..11). ScoreR owns the key now (moved off the Practice page); the
      // consumer reads it with getKey() to author the next generate/render request.
      getKey() { return key; },
      // Seed the key from selected content WITHOUT re-rendering (the twin of seedTripletFeel): updates the
      // component-owned value + the picker so the next generate carries it, but no transpose re-emit yet. A song
      // seeds its InitialKey, a key-independent progression/rhythm seeds C; a manual change afterwards still wins.
      seedKey(pc) {
        key = ((pc % 12) + 12) % 12;
        if (ui.key) ui.key.value = String(key);
      },
      // Set the key and ask the consumer to re-render: a new key changes the realized pitches (the alphaTex), so
      // this is content-kind — the exact peer of setTripletFeel (harmony unchanged → a cheap transpose re-emit,
      // not a regenerate).
      setKey(pc) {
        key = ((pc % 12) + 12) % 12;
        cb.onNeedsRerender(handle.getRenderOptions());
      },
      dispose() {
        disposed = true;   // a late staffProfile fan-out must not touch a destroyed api
        engine.dispose();
        container.innerHTML = "";
        container.classList.remove("cf-score");
      },
    };

    const bridge = (typeof window !== "undefined" && window.ChordFlowBridge) || null;

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

    // The shared player-transport controls (PlayerControlsR), bound to ScoreR's engine handle: play/stop/tempo/
    // soundfont/metronome/count-in, plus the Now/Next toggle when the consumer wires the boards. ScoreR keeps
    // owning the engine, the surface, getApi, and its notation-display controls (below).
    pc = (player && transportEnabled) ? window.ChordFlowPlayerControls.create(null, engine, {
      onToggleNowNext: opts.onToggleNowNext ? (v) => handle.toggleNowNext(v) : null,
    }) : null;

    const strip = buildControls(player, controls, options, handle, ui, tripletFeelEnabled, { scrollMode, keyEnabled, volumesEnabled }, pc);
    if (strip) container.appendChild(strip);
    container.appendChild(surface);
    if (debugPanel) container.appendChild(buildDebugPanel());

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
  function buildControls(player, controls, options, handle, ui, tripletFeelEnabled, extra, pc) {
    if (controls === "none") return null;
    extra = extra || {};

    const strip = document.createElement("div");
    strip.className = "cf-controls";

    // The shared transport (play/stop/tempo/soundfont/metronome/count-in/now-next) is PlayerControlsR, created
    // in create() and mounted first so it leads the strip.
    if (pc) strip.appendChild(pc.el);

    if (extra.keyEnabled) {
      strip.append(keyPicker(handle, ui));
    }

    if (tripletFeelEnabled) {
      strip.append(tripletFeelPicker(handle, ui));
    }

    if (player && controls === "full") {
      strip.append(scrollModeSelect(handle, ui, extra.scrollMode));
      // Rhythm/Lead volume sliders — suppressed on the Practice page, where they live next to their voice in
      // HarmonyControlsR (harmony-controls-r C2); other full-player consumers (Content preview) keep them here.
      if (extra.volumesEnabled) {
        strip.append(
          volumeSlider("rhythm", "Rhythm vol", handle, ui),
          volumeSlider("lead", "Lead vol", handle, ui),
        );
      }
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

  // The Key picker (tonic pitch class 0..11). Content-kind: a change re-emits the alphaTex in the new key via
  // handle.setKey (a transpose). The consumer reads the choice with handle.getKey(); ScoreR owns the key now.
  function keyPicker(handle, ui) {
    const wrap = document.createElement("label");
    wrap.className = "cf-toggle";
    const select = document.createElement("select");
    select.className = "cf-key";
    KEY_NAMES.forEach((name, pc) => {
      const o = document.createElement("option");
      o.value = String(pc);
      o.textContent = name;
      select.appendChild(o);
    });
    select.value = String(handle.getKey());
    select.addEventListener("change", () => handle.setKey(parseInt(select.value, 10) || 0));
    wrap.append(document.createTextNode("Key "), select);
    ui.key = select;
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

  // A per-track volume slider (0..1). Player-kind: applied locally via the engine, never re-rendered.
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
