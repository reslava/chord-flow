// ChordFlow shared player-transport controls (PlayerControlsR) — the one place the transport lives.
//
// A PURE controls widget over a ChordFlowPlayback *handle*: it does NOT own the engine. Each render surface
// owns its own engine (ScoreR internally; the Chord Sheets page in setupEngine); PlayerControlsR binds to that
// handle and SELF-SUBSCRIBES to its event bus (engine.on(...)), so there is no per-page event forwarding to
// drift — the class of bug that dropped `syncToggle` and left Chord Sheets without metronome/count-in.
//
// Renders: play/pause · stop · tempo · (soundfont) · (metronome) · (count-in) · (optional Now/Next toggle).
// The optional controls are omitted when their opt is false/absent; the Now/Next toggle appears ONLY when the
// consumer supplies an onToggleNowNext handler (it wires the boards — PlayerControlsR just flips their view).
//
//   const pc = ChordFlowPlayerControls.create(container, engine, {
//     soundFont: true, metronome: true, countIn: true,   // which optional controls to show (default true)
//     onToggleNowNext: null,   // fn(visible) → renders a Now/Next toggle bound to it
//   });
//   pc.el;                 // the controls node (also appended to `container` when one is given)
//   pc.setTempoValue(bpm); // seed the tempo input WITHOUT firing setTempo (call after load)
//   pc.getTempo();         // current input BPM (0 if empty)
//   pc.dispose();
"use strict";

window.ChordFlowPlayerControls = (function () {
  // Wrap every control handler so a throw surfaces in the console instead of being swallowed by the DOM event
  // dispatcher — the exact failure mode of the syncToggle regression (playback/metronome-countin-fix).
  function guard(name, fn) {
    return function (ev) {
      try { return fn(ev); }
      catch (e) { console.error("[PlayerControlsR] " + name + " handler failed:", e); }
    };
  }

  function button(label, onClick) {
    const b = document.createElement("button");
    b.type = "button";
    b.textContent = label;
    b.addEventListener("click", onClick);
    return b;
  }

  // A checkbox toggle ("cf-toggle" chrome, shared with ScoreR). Returns { wrap, input }.
  function checkbox(label, checked, onChange) {
    const wrap = document.createElement("label");
    wrap.className = "cf-toggle";
    const input = document.createElement("input");
    input.type = "checkbox";
    input.checked = !!checked;
    input.addEventListener("change", onChange);
    wrap.append(input, document.createTextNode(" " + label));
    return { wrap, input };
  }

  function create(container, engine, opts) {
    opts = opts || {};
    const showSoundFont = opts.soundFont !== false;
    const showMetronome = opts.metronome !== false;
    const showCountIn = opts.countIn !== false;
    const onToggleNowNext = typeof opts.onToggleNowNext === "function" ? opts.onToggleNowNext : null;

    const el = document.createElement("span");
    el.className = "cf-player-controls";

    // Transport: disabled until the engine reports "ready" (soundfont loaded).
    const playBtn = button("▶ Play", guard("play", () => engine.play()));
    playBtn.disabled = true;
    const stopBtn = button("■ Stop", guard("stop", () => engine.stop()));
    stopBtn.disabled = true;
    el.append(playBtn, stopBtn);

    // Tempo (absolute BPM → engine playbackSpeed).
    const tempoLabel = document.createElement("label");
    tempoLabel.textContent = "Tempo";
    const tempoInput = document.createElement("input");
    tempoInput.type = "number";
    tempoInput.min = "40"; tempoInput.max = "240"; tempoInput.step = "1";
    tempoInput.disabled = true;
    tempoInput.className = "cf-tempo";
    tempoInput.addEventListener("change", guard("tempo", () => {
      const bpm = parseInt(tempoInput.value, 10);
      if (bpm) engine.setTempo(bpm);
    }));
    const bpmSpan = document.createElement("span");
    bpmSpan.textContent = "BPM";
    el.append(tempoLabel, tempoInput, bpmSpan);

    // Soundfont picker — starts empty, filled by the engine's soundFontsListed event (global host-persisted
    // choice; each surface's picker reflects the same selection).
    let soundFontSel = null;
    if (showSoundFont) {
      const wrap = document.createElement("label");
      wrap.className = "cf-toggle";
      soundFontSel = document.createElement("select");
      soundFontSel.className = "cf-soundfont";
      soundFontSel.addEventListener("change", guard("soundFont", () => engine.setSoundFont(soundFontSel.value)));
      wrap.append(document.createTextNode("Sound "), soundFontSel);
      el.append(wrap);
    }

    // Metronome / count-in — the whole point: applied straight on the engine handle.
    if (showMetronome) {
      el.append(checkbox("Metronome", false, guard("metronome", (e) => engine.setMetronome(e.target.checked))).wrap);
    }
    if (showCountIn) {
      el.append(checkbox("Count-in", false, guard("countIn", (e) => engine.setCountIn(e.target.checked))).wrap);
    }

    // Optional Now/Next view toggle — rendered only when the consumer wires the boards.
    if (onToggleNowNext) {
      el.append(checkbox("Now/Next", true, guard("nowNext", (e) => onToggleNowNext(e.target.checked))).wrap);
    }

    // --- self-subscribe to the engine event bus (no per-page forwarding) ---
    engine.on("ready", () => { playBtn.disabled = false; stopBtn.disabled = false; tempoInput.disabled = false; });
    engine.on("stateChange", (playing) => { playBtn.textContent = playing ? "⏸ Pause" : "▶ Play"; });
    engine.on("finished", () => { playBtn.textContent = "▶ Play"; });
    engine.on("soundFontsListed", (fonts, selectedId) => {
      if (!soundFontSel) return;
      soundFontSel.innerHTML = "";
      for (const f of (fonts || [])) {
        const o = document.createElement("option");
        o.value = f.id; o.textContent = f.name;
        soundFontSel.appendChild(o);
      }
      if (selectedId) soundFontSel.value = selectedId;
    });

    if (container) container.appendChild(el);

    return {
      el,
      // Seed the tempo input WITHOUT firing setTempo (the load() re-bases speed; this just reflects the value).
      setTempoValue(bpm) { if (bpm) tempoInput.value = String(bpm); },
      getTempo() { return parseInt(tempoInput.value, 10) || 0; },
      dispose() { if (el.parentNode) el.parentNode.removeChild(el); },
    };
  }

  return { create };
})();
