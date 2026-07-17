// ChordFlow shared harmony/definition controls (HarmonyControlsR) — the one place the exercise DEFINITION lives.
//
// The PlayerControlsR sibling (harmony-controls-r IN1): a PURE controls widget that owns the practice
// definition — harmony (song/progression), Key, Feel, Comping + Rhythm vol, Lead + Lead vol, Difficulty, the
// automatic-voicing fret window, and the Generate / Save / Mark practiced actions. Both view surfaces of the
// Practice page read the SAME instance, so the old Practice-vs-Chord-Sheets combo drift is impossible by
// construction (IN8): there is exactly one harmony picker, one population path, one definition state.
//
// Ownership split (req C1): tempo is a PlayerControlsR param — this component never renders a tempo control;
// a harmony switch hands the song's DefaultTempo to the shell via onHarmonySwitch so IT seeds PlayerControlsR.
// The volume sliders are player-kind and bind straight to the page engine (setTrackVolume) — they sit here,
// next to the voice they control, not on the transport.
//
// Seeding (IN4/IN5, the scorer-render-params behavior moved in): a harmony *switch* seeds Key/Feel from the
// selected song (InitialKey/DefaultFeel; a progression or a song without them seeds C/Straight — the controls
// always show a concrete value, never blank). A manual Key/Feel edit survives until the next switch. Seeds set
// control values only — Generate applies them; they never fire onDefinitionChange. The loadExercise reply path
// seeds through seedKey/seedTripletFeel (stored override wins, C3).
//
//   const hc = ChordFlowHarmonyControls.create(container, {
//     engine,                          // ChordFlowPlayback handle — binds the Rhythm/Lead volume sliders
//     onGenerate: (def) => {},         // Generate clicked (def = getDefinition())
//     onSave: () => {},                // Save clicked
//     onMarkPracticed: () => {},       // Mark practiced clicked
//     onDefinitionChange: (def, what) => {},  // a LIVE re-render param changed: "key" | "tripletFeel" | "voicing"
//     onHarmonySwitch: (item) => {},   // harmony switched — item = the catalog entry (or null); seed tempo here
//   });
//   hc.el;                             // the strip node (also appended to `container` when one is given)
//   hc.setCatalog(entity, items);      // feed raw entityList payloads ("song" | "progression" | "rhythm")
//   hc.getDefinition();                // { harmonyEntity, harmonyId, compingPatternId, leadPatternId,
//                                      //   keyPitchClass, keyIsMinor, tripletFeel, difficulty, voicingMinFret, voicingMaxFret }
//   hc.seedKey(pc); hc.seedKeyMode(isMinor); hc.seedTripletFeel(v);   // load-path seeds (silent — no change events)
//   hc.dispose();
"use strict";

window.ChordFlowHarmonyControls = (function () {
  // Key names per tonic pitch class (0 = C .. 11 = B), spelled to match the renderer's \ks (as ScoreR/app.js).
  const KEY_NAMES = ["C", "Db", "D", "Eb", "E", "F", "F#", "G", "Ab", "A", "Bb", "B"];

  // C# TripletFeel enum names (the bridge parses them by name) — mirrors ScoreR's transport picker.
  const TRIPLET_FEELS = [
    { value: "None", label: "Straight" },
    { value: "Triplet8th", label: "Triplet 8th (swing)" },
    { value: "Triplet16th", label: "Triplet 16th" },
  ];

  // Stable Domain enum names, enumerated here rather than over a bridge (as app.js did).
  const DIFFICULTIES = ["Beginner", "Intermediate", "Advanced"];

  // The boot default when the catalog contains it (the MVP blues the host renders on ready).
  const BOOT_HARMONY = "progression:12bar_blues";
  const BOOT_COMPING = "beat_1_3";

  // Wrap every control handler so a throw surfaces in the console instead of being swallowed by the DOM event
  // dispatcher (the PlayerControlsR guard).
  function guard(name, fn) {
    return function (ev) {
      try { return fn(ev); }
      catch (e) { console.error("[HarmonyControlsR] " + name + " handler failed:", e); }
    };
  }

  function labelled(text, control) {
    const wrap = document.createElement("label");
    wrap.className = "cf-toggle";
    wrap.append(document.createTextNode(text + " "), control);
    return wrap;
  }

  function select(className) {
    const sel = document.createElement("select");
    sel.className = className;
    return sel;
  }

  function button(label, onClick) {
    const b = document.createElement("button");
    b.type = "button";
    b.textContent = label;
    b.addEventListener("click", onClick);
    return b;
  }

  function fillOptions(sel, options, fallbackValue) {
    const prev = sel.value;
    sel.innerHTML = "";
    for (const o of options) {
      const opt = document.createElement("option");
      opt.value = o.value;
      opt.textContent = o.label;
      sel.appendChild(opt);
    }
    const values = options.map((o) => o.value);
    sel.value = values.includes(prev) ? prev : (values.includes(fallbackValue) ? fallbackValue : (values[0] ?? ""));
  }

  function create(container, opts) {
    opts = opts || {};
    const engine = opts.engine || null;
    const cb = {
      onGenerate: opts.onGenerate || function () {},
      onSave: opts.onSave || function () {},
      onMarkPracticed: opts.onMarkPracticed || function () {},
      onDefinitionChange: opts.onDefinitionChange || function () {},
      onHarmonySwitch: opts.onHarmonySwitch || function () {},
    };

    // Content catalog, fed raw entityList payloads via setCatalog — the single population path (IN8).
    const catalog = { song: [], progression: [], rhythm: [] };

    const el = document.createElement("div");
    el.className = "cf-harmony-controls";

    // --- harmony picker (Songs / Progressions optgroups) ---------------------
    const harmonySel = select("cf-harmony");
    harmonySel.addEventListener("change", guard("harmony", onHarmonySwitched));
    el.append(labelled("Harmony", harmonySel));

    // --- Key + Feel (moved in from ScoreR's transport — IN4) ------------------
    const keySel = select("cf-key");
    KEY_NAMES.forEach((name, pc) => {
      const o = document.createElement("option");
      o.value = String(pc);
      o.textContent = name;
      keySel.appendChild(o);
    });
    keySel.value = "0";
    keySel.addEventListener("change", guard("key", () => cb.onDefinitionChange(getDefinition(), "key")));

    // The key's MODE (major/minor) — the tonic above + the mode form the realization Key. A minor mode picks
    // the parent major for realization and emits \ks {tonic}minor (first-class-minor-keys). A mode change is a
    // live transpose, same as a tonic change.
    const keyModeSel = select("cf-key-mode");
    [{ value: "major", label: "major" }, { value: "minor", label: "minor" }].forEach((m) => {
      const o = document.createElement("option");
      o.value = m.value;
      o.textContent = m.label;
      keyModeSel.appendChild(o);
    });
    keyModeSel.value = "major";
    keyModeSel.addEventListener("change", guard("keyMode", () => cb.onDefinitionChange(getDefinition(), "key")));

    const keyGroup = document.createElement("span");
    keyGroup.append(keySel, keyModeSel);
    el.append(labelled("Key", keyGroup));

    const feelSel = select("cf-feel");
    fillOptions(feelSel, TRIPLET_FEELS, "None");
    feelSel.addEventListener("change", guard("feel", () => cb.onDefinitionChange(getDefinition(), "tripletFeel")));
    el.append(labelled("Feel", feelSel));

    // --- Comping + Rhythm vol · Lead + Lead vol (vol next to its voice) -------
    const compingSel = select("cf-comping");
    el.append(labelled("Comping", compingSel), volumeSlider("rhythm", "Rhythm vol"));

    const leadSel = select("cf-lead");
    el.append(labelled("Lead", leadSel), volumeSlider("lead", "Lead vol"));

    // --- Difficulty ----------------------------------------------------------
    const difficultySel = select("cf-difficulty");
    fillOptions(difficultySel, DIFFICULTIES.map((d) => ({ value: d, label: d })), "Beginner");
    el.append(labelled("Difficulty", difficultySel));

    // --- automatic comping-voicing fret window (engine-derived-as-app-source IN14) ---
    const minFret = fretInput(0);
    const maxFret = fretInput(15);
    const dash = document.createElement("span");
    dash.textContent = "–";
    const fretWrap = document.createElement("label");
    fretWrap.className = "cf-toggle";
    fretWrap.append(document.createTextNode("Voicing frets "), minFret, dash, maxFret);
    el.append(fretWrap);
    for (const input of [minFret, maxFret]) {
      // A window change re-voices the current exercise live (the old app.js replay behavior).
      input.addEventListener("change", guard("voicing", () => cb.onDefinitionChange(getDefinition(), "voicing")));
    }

    // --- actions --------------------------------------------------------------
    el.append(
      button("Generate", guard("generate", () => cb.onGenerate(getDefinition()))),
      button("Save", guard("save", () => cb.onSave())),
      button("Mark practiced", guard("markPracticed", () => cb.onMarkPracticed())),
    );

    function fretInput(value) {
      const input = document.createElement("input");
      input.type = "number";
      input.min = "0"; input.max = "15"; input.step = "1";
      input.value = String(value);
      input.className = "cf-fret";
      return input;
    }

    // Player-kind, bound straight to the page engine; inert (hidden) when no engine is supplied.
    function volumeSlider(which, label) {
      const input = document.createElement("input");
      input.type = "range";
      input.min = "0"; input.max = "1"; input.step = "0.05"; input.value = "1";
      input.addEventListener("input", guard(which + "Vol", () => {
        if (engine) engine.setTrackVolume(which, parseFloat(input.value));
      }));
      const wrap = labelled(label, input);
      if (!engine) wrap.hidden = true;
      return wrap;
    }

    // --- harmony switch: seed Key/Feel here, hand tempo to the shell (C1) ------
    function selectedHarmonyItem() {
      const [entity, id] = (harmonySel.value || "").split(/:(.*)/s);
      const items = catalog[entity] || [];
      const item = items.find((it) => it.id === id) || null;
      return item ? { entity, ...item } : null;
    }

    function onHarmonySwitched() {
      const item = selectedHarmonyItem();
      // Never blank (IN5): a song's own values when it has them, else the C / Straight defaults — a
      // key-independent progression (or a keyless song) seeds C; no feel directive seeds Straight.
      keySel.value = String(item && item.initialKey != null ? item.initialKey : 0);
      // A song can carry its key's mode; a key-independent progression has none, so default major. (The
      // content-list payload gains initialKeyIsMinor when minor songs land — this seed is already ready for it.)
      keyModeSel.value = item && item.initialKeyIsMinor ? "minor" : "major";
      feelSel.value = item && item.defaultFeel ? item.defaultFeel : "None";
      cb.onHarmonySwitch(item); // the shell seeds tempo into PlayerControlsR from item.defaultTempo (C1)
    }

    // --- catalog population (one path for every consumer — IN8) ----------------
    function rebuildHarmonyPicker() {
      const prev = harmonySel.value;
      harmonySel.innerHTML = "";
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
          o.value = g.entity + ":" + it.id;
          o.textContent = it.name || it.id;
          og.appendChild(o);
        }
        harmonySel.appendChild(og);
      }
      const values = Array.from(harmonySel.options).map((o) => o.value);
      harmonySel.value = values.includes(prev) ? prev : (values.includes(BOOT_HARMONY) ? BOOT_HARMONY : (values[0] ?? ""));
    }

    function rebuildRhythmPickers() {
      const rhythmOpts = catalog.rhythm.map((r) => ({ value: r.id, label: r.name || r.id }));
      fillOptions(compingSel, rhythmOpts, BOOT_COMPING);
      fillOptions(leadSel, [{ value: "", label: "(none)" }, ...rhythmOpts], "");
    }

    // --- the definition (the generate payload) ---------------------------------
    function getDefinition() {
      const [harmonyEntity, harmonyId] = (harmonySel.value || BOOT_HARMONY).split(/:(.*)/s);
      const clamp = (v, d) => Math.min(15, Math.max(0, Number.isFinite(v) ? v : d));
      let min = clamp(parseInt(minFret.value, 10), 0);
      let max = clamp(parseInt(maxFret.value, 10), 15);
      if (min > max) [min, max] = [max, min];
      return {
        harmonyEntity,
        harmonyId,
        compingPatternId: compingSel.value || BOOT_COMPING,
        leadPatternId: leadSel.value || null,
        keyPitchClass: parseInt(keySel.value, 10) || 0,
        keyIsMinor: keyModeSel.value === "minor",
        tripletFeel: feelSel.value || "None",
        difficulty: difficultySel.value || "Beginner",
        voicingMinFret: min,
        voicingMaxFret: max,
      };
    }

    if (container) container.appendChild(el);

    return {
      el,
      getDefinition,
      // Feed one raw entityList payload; rebuilds the affected picker(s), preserving the current selection.
      setCatalog(entity, items) {
        if (!(entity in catalog)) return;
        catalog[entity] = items || [];
        if (entity === "rhythm") rebuildRhythmPickers();
        else rebuildHarmonyPicker();
      },
      // Load-path seeds (C3): reflect a loaded exercise's stored key/feel WITHOUT firing change events —
      // the override wins over content defaults and survives until the next harmony switch.
      seedKey(pc) { if (pc != null) keySel.value = String(((pc % 12) + 12) % 12); },
      seedKeyMode(isMinor) { keyModeSel.value = isMinor ? "minor" : "major"; },
      seedTripletFeel(v) { if (v) feelSel.value = v; },
      dispose() { if (el.parentNode) el.parentNode.removeChild(el); },
    };
  }

  return { create };
})();
