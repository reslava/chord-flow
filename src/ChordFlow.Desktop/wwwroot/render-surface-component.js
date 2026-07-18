// ChordFlow shared render-surface composite (ChordFlowRenderSurface).
//
// The ONE place the "look at a piece" surface is composed, so Practice (app.js) and the Content preview
// (content-crud.js) mount the exact same thing and cannot drift (content-shared-render-surfaces C4). It owns:
//   • ScoreR (score-render-component.js) created transport:false — the notation surface + the ONE engine.
//   • ChordSheetR via ChordFlowSheetView (chord-sheets.js) — the chord-sheet surface (when sheet:true).
//   • the Score ⇄ Sheet segmented toggle — collapse-swap (max-height:0, width kept) so it survives MID-PLAYBACK.
//   • a page-level PlayerControlsR (player-controls-component.js) bound to ScoreR's engine, so the transport
//     lives across the view toggle (a view switch never stops playback).
//   • the beat/position fan-out: the engine's event-driven "beat" drives the sheet's Per-chord marker and the
//     consumer's own event surfaces (Now/Next); the time-linear "position" clock drives the sheet's
//     Visual-metronome marker. Both markers track even while their surface is hidden, so the toggle is seamless.
//
// It is a DUMB view (C1): zero music theory in JS — every input (tex, sheet model, cellSchedule) is built in
// Core and arrives on load(). It does NOT own Now/Next (a Practice performance affordance, EX4) nor the
// definition/authoring controls — those stay page-specific and wrap the composite.
//
// Placement stays with the page: the consumer passes the three mount elements it has laid out (so Practice can
// interleave Now/Next between the transport and the surfaces, and Content can lay out its editor around them).
// The composite owns only the WIRING between them.
//
//   const surface = ChordFlowRenderSurface.create({
//     transportEl,               // the Score ⇄ Sheet toggle + PlayerControlsR mount
//     scoreEl,                   // ScoreR mounts here
//     sheetEl,                   // ChordFlowSheetView mounts here (omit / null when sheet:false)
//     sheet: true,               // false = score-only (no toggle, no sheet surface) — e.g. a rhythm preview
//     scoreOpts: { ... },        // forwarded to ChordFlowScore.create (transport is forced false)
//     playerOpts: { onToggleNowNext },  // forwarded to PlayerControlsR
//     onBeat, onFinished, onNeedsRerender,  // consumer hooks (the composite adds the sheet fan-out itself)
//   });
//   surface.load({ tex, tempo, sheet, cellSchedule, key?, tripletFeel?, name? }); // feeds BOTH projections
//   surface.getEngine();       // the one ChordFlowPlayback (HarmonyControlsR volume binds, Now/Next feed)
//   surface.getRenderParams(); // { renderOptions, key, tempo, tripletFeel } for the next C# render request
//   surface.seedKey(pc); surface.seedTempo(bpm); surface.seedTripletFeel(v); // seed ScoreR's controls, no re-render
//   surface.showSurface("score" | "sheet"); surface.dispose();
"use strict";

window.ChordFlowRenderSurface = (function () {
  // The page-level Score ⇄ Sheet toggle: swaps ONLY which surface is visible; the engine, definition, schedules
  // and both markers are untouched, so toggling mid-playback just changes how you look at the same run. The
  // hidden surface COLLAPSES (max-height:0, width kept — the .view-collapsed rule) so alphaTab never re-measures
  // while hidden and its playback cursor stays valid. Mirrors app.js's original buildViewToggle.
  function buildToggle(container, scoreEl, sheetEl) {
    const wrap = document.createElement("span");
    wrap.className = "view-toggle";
    const buttons = {};
    for (const [name, label] of [["score", "Score"], ["sheet", "Sheet"]]) {
      const b = document.createElement("button");
      b.type = "button";
      b.textContent = label;
      b.addEventListener("click", () => show(name));
      buttons[name] = b;
      wrap.appendChild(b);
    }
    function show(name) {
      scoreEl.classList.toggle("view-collapsed", name !== "score");
      if (sheetEl) sheetEl.classList.toggle("view-collapsed", name !== "sheet");
      for (const [n, b] of Object.entries(buttons)) b.classList.toggle("active", n === name);
    }
    // Prepend the toggle so it leads the transport strip (PlayerControlsR is appended after).
    container.insertBefore(wrap, container.firstChild);
    show("score");
    return { show, wrap };
  }

  function create(opts) {
    opts = opts || {};
    const scoreEl = opts.scoreEl;
    const sheetEl = opts.sheetEl || null;
    const transportEl = opts.transportEl;
    const sheetEnabled = opts.sheet !== false && !!sheetEl && !!window.ChordFlowSheetView;

    const userOnBeat = typeof opts.onBeat === "function" ? opts.onBeat : function () {};
    const userOnFinished = typeof opts.onFinished === "function" ? opts.onFinished : function () {};
    const userOnNeedsRerender = typeof opts.onNeedsRerender === "function" ? opts.onNeedsRerender : function () {};

    // The Sheet surface (created before ScoreR so the beat fan-out can reference it). Score-only when sheet:false.
    const sheetView = sheetEnabled ? window.ChordFlowSheetView.create(sheetEl) : null;

    // ScoreR owns the notation surface + the ONE engine. transport:false — the page-level PlayerControlsR below
    // binds to this engine, so the transport survives the Score ⇄ Sheet toggle. The consumer's scoreOpts (key/
    // feel/volumes/debugPanel/scroll/…) pass straight through; the composite manages the three engine callbacks
    // so it can fan the signals into the sheet marker AND the consumer's own surfaces.
    const scoreHandle = window.ChordFlowScore.create(scoreEl, Object.assign({}, opts.scoreOpts, {
      transport: false,
      onBeat: (bar, beat) => {
        if (sheetView) sheetView.onBeat(bar, beat); // 1-based; Per-chord marker steps down internally
        userOnBeat(bar, beat);                      // Now/Next + the bridge echo (Practice) hang off this
      },
      onFinished: () => {
        if (sheetView) sheetView.clearMarker();
        userOnFinished();
      },
      onNeedsRerender: (ro) => userOnNeedsRerender(ro),
    }));

    const engine = scoreHandle.getEngine();

    // The TIME clock (PlaybackClock's "position", one even step per quarter — metronome-true-marker): drives the
    // sheet's Visual-metronome marker. Wired here off the engine handle, exactly as app.js wired it page-level.
    engine.on("position", (bar, quarterBeat) => {
      if (sheetView) sheetView.onPosition(bar, quarterBeat);
    });

    // The Score ⇄ Sheet toggle leads the transport strip; PlayerControlsR (bound to the one engine) follows.
    // Score-only mode has no toggle — the single score surface is always visible.
    const toggle = sheetEnabled ? buildToggle(transportEl, scoreEl, sheetEl) : null;
    const showSurface = toggle ? toggle.show : function () {};
    const pc = window.ChordFlowPlayerControls.create(transportEl, engine, opts.playerOpts || {});

    return {
      // Feed BOTH projections of one realized-song pass: ScoreR loads the tex; the Sheet surface renders the
      // model + takes its marker schedule. Seeds ScoreR's key/feel controls (no-op when the page keeps them
      // elsewhere, e.g. Practice's HarmonyControlsR) and the transport tempo — seeds only, never a re-render
      // (the score the host already sent is in the seeded key/tempo/feel).
      load(payload) {
        payload = payload || {};
        scoreHandle.load(payload.tex, { tempo: payload.tempo });
        pc.setTempoValue(engine.getBaseTempo());
        if (payload.key != null) scoreHandle.seedKey(payload.key);
        if (payload.tripletFeel) scoreHandle.seedTripletFeel(payload.tripletFeel);
        if (sheetView) {
          sheetView.render(payload.sheet || null, payload.name);
          sheetView.setSchedule(payload.cellSchedule);
        }
      },
      // The one engine — for a page binding controls to it (HarmonyControlsR volumes) or feeding Now/Next.
      getEngine() { return engine; },
      // The params to author the next C# render request (generate / entityPreview / loadExercise).
      getRenderParams() {
        return {
          renderOptions: scoreHandle.getRenderOptions(),
          key: scoreHandle.getKey(),
          tempo: scoreHandle.getTempo(),
          tripletFeel: scoreHandle.getTripletFeel(),
        };
      },
      // Seed passthroughs (no re-render) — for a page that applies content seeds before the first preview arrives.
      seedKey(pc2) { scoreHandle.seedKey(pc2); },
      // Seed the base tempo AND the page-level transport input — ScoreR's own pc is null under transport:false,
      // so its seedTempo can't update the visible control; the composite's pc must be updated here.
      seedTempo(bpm) { scoreHandle.seedTempo(bpm); if (bpm) pc.setTempoValue(bpm); },
      seedTripletFeel(v) { scoreHandle.seedTripletFeel(v); },
      // Programmatic surface switch (e.g. reset to Score on a fresh selection). No-op in score-only mode.
      showSurface(name) { showSurface(name); },
      dispose() {
        pc.dispose();                                        // removes its own element from transportEl
        if (toggle && toggle.wrap.parentNode) toggle.wrap.parentNode.removeChild(toggle.wrap);
        if (sheetView) sheetView.dispose();
        scoreHandle.dispose();
      },
    };
  }

  return { create };
})();
