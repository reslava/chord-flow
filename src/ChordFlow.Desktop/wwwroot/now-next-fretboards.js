// ChordFlow now/next fretboards — sibling shared module (the playback-aware twin of the lone fretboard view).
//
// Shows the CURRENT and NEXT chord of a playing score as two side-by-side fretboards, driven by the engine's
// chord schedule (one entry per chord change, each carrying a Core-computed FretboardDiagram of the comped
// voicing). It owns the two ChordFlowFretboard instances, the beat→schedule lookup, and the now/next update; a
// consumer (app.js) just mounts it once and forwards loadScore.schedule + the score component's beat signal.
//
// It is a DUMB ASSEMBLER: no music theory here — the diagrams arrive fully resolved from Core (C1/C3). The two
// fretboards are the shared ChordFlowFretboard, so chord-box geometry/colours stay identical to everywhere else.
//
//   const nn = ChordFlowNowNext.create(containerEl);
//   nn.setSchedule(loadScoreMsg.schedule);   // rebuild on every score load
//   nn.onBeat(bar, beat);                     // 0-based (bar, beat) — the engine/alphaTab convention the
//                                             //   schedule uses; the consumer converts the score component's
//                                             //   1-based onBeat (see app.js) down to it
//   nn.reset();                               // re-prime to the first chord (e.g. on stop) — schedule is KEPT
//                                             //   so the next play re-syncs from the start
//   nn.dispose();
"use strict";

window.ChordFlowNowNext = (function () {
  // An empty fretboard model — the "no chord" state (next box past the last change, or before a schedule loads).
  function blankDiagram() {
    return {
      title: "—",
      markers: [],
      mutedStrings: [6, 5, 4, 3, 2, 1],
      barreFret: null,
      fretMin: 0,
      fretMax: 4,
    };
  }

  function boxWrap(caption) {
    const wrap = document.createElement("div");
    wrap.style.cssText = "display:flex;flex-direction:column;gap:.3rem;align-items:center;";

    const cap = document.createElement("div");
    cap.textContent = caption;
    cap.style.cssText = "font-size:.72rem;font-weight:600;letter-spacing:.06em;text-transform:uppercase;color:#9aa0a6;";

    // White fret-box panel, matching the Voicings / CAGED diagram surface so the neck + dots read clearly.
    const body = document.createElement("div");
    body.style.cssText = "background:#fff;border:1px solid #333;border-radius:6px;";

    wrap.appendChild(cap);
    wrap.appendChild(body);
    return { wrap, body };
  }

  function create(container) {
    container.style.cssText =
      "display:flex;gap:1.5rem;align-items:flex-start;padding:.5rem 0;margin-bottom:.5rem;";

    const nowBox = boxWrap("Now");
    const nextBox = boxWrap("Next");
    container.appendChild(nowBox.wrap);
    container.appendChild(nextBox.wrap);

    // Fixed vertical chord-box: hide the orientation + fret-window controls (IN5); the chord name rides the
    // diagram title, so each box self-labels.
    const opts = { orientation: "vertical", controls: { orientation: false, fretWindow: false } };
    const nowView = window.ChordFlowFretboard.create(nowBox.body, opts);
    const nextView = window.ChordFlowFretboard.create(nextBox.body, { orientation: "vertical", controls: { orientation: false, fretWindow: false } });

    let schedule = [];
    let indexByKey = new Map();
    let currentIndex = -1;

    const keyOf = (bar, beat) => bar + ":" + beat;

    function renderBox(view, change) {
      view.render(change && change.diagram ? change.diagram : blankDiagram());
    }

    // Show the chord at schedule[idx] as "now" and schedule[idx+1] as "next" (blank past the last change).
    function apply(idx) {
      currentIndex = idx;
      const now = idx >= 0 ? schedule[idx] : null;
      const next = idx >= 0 && idx + 1 < schedule.length ? schedule[idx + 1] : null;
      renderBox(nowView, now);
      renderBox(nextView, next);
    }

    function setSchedule(next) {
      schedule = Array.isArray(next) ? next : [];
      indexByKey = new Map();
      for (let i = 0; i < schedule.length; i++) {
        indexByKey.set(keyOf(schedule[i].bar, schedule[i].beat), i);
      }
      // Prime to the first chord so the boxes show something before playback starts.
      apply(schedule.length > 0 ? 0 : -1);
    }

    function onBeat(bar, beat) {
      const idx = indexByKey.get(keyOf(bar, beat));
      if (idx === undefined || idx === currentIndex) return; // not a change beat, or already showing it
      apply(idx);
    }

    // Re-prime to the first chord (or blank if no schedule). Keeps the schedule so the next play re-syncs —
    // dropping it on stop would leave replay blank until another loadScore.
    function reset() {
      apply(schedule.length > 0 ? 0 : -1);
    }

    function dispose() {
      nowView.dispose();
      nextView.dispose();
      container.replaceChildren();
    }

    apply(-1); // start blank until a schedule arrives
    return { setSchedule, onBeat, reset, dispose };
  }

  return { create };
})();
