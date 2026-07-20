// ChordFlow FilterR — the shared, dumb, faceted toggle-chip filter (filter-toggle-buttons IN3).
//
// The toggle-filter twin of FretR / ChordSheetR / PlayerControlsR: it renders a stack of chip rows (one row per
// "level"), tracks which chips are enabled, and reports the enabled sets via `onChange`. That is ALL it does —
// it owns NO data source, NO filtering logic, and NO music theory (C1). Each consumer supplies the levels and
// wires `onChange` to its own behavior: the Content list + Practice pickers filter client-side over already-loaded
// rows; GuitarVoicingsR re-issues its server-side `voicingGrid` round-trip. One component, both idioms.
//
//   const f = ChordFlowFilter.create(el, {
//     levels: [{ key:"genre", label:"Genre", chips:[{token:"Blues",label:"Blues"}, ...] }, ...],
//     onChange(enabledByKey) { /* enabledByKey = { genre: Set<token>, ... } — the enabled tokens per level */ },
//   });
//   f.setLevels(nextLevels);   // re-render when the discovered facet values change (state is preserved per token)
//   f.getState();              // { [key]: Set<token> } — the same shape onChange receives
//
// Semantics are the CONSUMER's (C1/C2): FilterR only reports which chips are on. The convention every consumer
// follows — OR within a level, AND across levels, and a level whose chips are ALL on is treated as unconstrained
// (all-on ⇒ everything, so items with no value for that facet still pass). "Default on, sticky off": a chip the
// user turns off stays off across `setLevels`; a newly-appearing value arrives on. A level with fewer than two
// chips renders nothing (nothing to narrow) unless `hideSingleChoiceLevels:false`.
"use strict";

window.ChordFlowFilter = (function () {
  let stylesInjected = false;
  function injectStyles() {
    if (stylesInjected) return;
    stylesInjected = true;
    const css = `
      .cf-filter { display:flex; flex-direction:column; gap:.35rem; }
      .cf-level { display:flex; flex-wrap:wrap; gap:.3rem; align-items:center; }
      .cf-level-label { font-size:.72rem; color:#9aa0a6; min-width:3.4rem; }
      .cf-chip { font-size:.68rem; padding:.12rem .55rem; border-radius:999px; cursor:pointer;
        background:#252526; color:#6b7075; border:1px solid #3a3a3d; }
      .cf-chip:hover { background:#2d2d30; }
      .cf-chip.active { background:#3a4d78; color:#fff; border-color:#3a4d78; }`;
    const style = document.createElement("style");
    style.textContent = css;
    document.head.appendChild(style);
  }

  function el(tag, cls, text) {
    const node = document.createElement(tag);
    if (cls) node.className = cls;
    if (text != null) node.textContent = text;
    return node;
  }

  function create(container, opts) {
    opts = opts || {};
    injectStyles();
    const onChange = typeof opts.onChange === "function" ? opts.onChange : function () {};
    const hideSingle = opts.hideSingleChoiceLevels !== false; // default: hide a level with <2 chips

    let levels = Array.isArray(opts.levels) ? opts.levels : [];
    // "Sticky off": the tokens the user has explicitly turned off, per level. Everything else is on — including
    // values that first appear on a later setLevels(). Kept across re-renders so a narrowed filter survives a refresh.
    const off = {}; // { [key]: Set<token> }

    const root = el("div", "cf-filter");
    container.appendChild(root);

    function offSet(key) {
      if (!off[key]) off[key] = new Set();
      return off[key];
    }

    // The enabled tokens PRESENT in a level right now = its chips minus the sticky-off set.
    function enabledFor(level) {
      const gone = offSet(level.key);
      const set = new Set();
      for (const chip of level.chips) if (!gone.has(chip.token)) set.add(chip.token);
      return set;
    }

    function state() {
      const out = {};
      for (const level of levels) out[level.key] = enabledFor(level);
      return out;
    }

    function emit() {
      onChange(state());
    }

    function toggle(level, token, mode) {
      const gone = offSet(level.key);
      if (mode === "single") {
        // Radio-like: the clicked token becomes the only one on (all others off).
        for (const chip of level.chips) gone.add(chip.token);
        gone.delete(token);
      } else if (gone.has(token)) {
        gone.delete(token); // turn on
      } else {
        gone.add(token); // turn off
      }
    }

    function render() {
      root.innerHTML = "";
      for (const level of levels) {
        if (hideSingle && level.chips.length < 2) continue;
        const gone = offSet(level.key);
        const row = el("div", "cf-level");
        if (level.label != null) row.appendChild(el("span", "cf-level-label", level.label));
        for (const chip of level.chips) {
          const active = !gone.has(chip.token);
          const btn = el("button", "cf-chip" + (active ? " active" : ""), chip.label != null ? chip.label : chip.token);
          btn.type = "button";
          btn.addEventListener("click", () => {
            toggle(level, chip.token, level.mode);
            if (level.mode === "single") render(); // a single-select flip changes sibling chips too
            else btn.classList.toggle("active");
            emit();
          });
          row.appendChild(btn);
        }
        root.appendChild(row);
      }
    }

    function setLevels(next) {
      levels = Array.isArray(next) ? next : [];
      // Drop sticky-off entries for tokens that no longer exist anywhere in a level, so the set can't grow forever;
      // a token that reappears is treated as new (on) — matching "default on, sticky off" for values in view.
      for (const level of levels) {
        const present = new Set(level.chips.map((c) => c.token));
        const gone = offSet(level.key);
        for (const t of [...gone]) if (!present.has(t)) gone.delete(t);
      }
      render();
    }

    render();

    return {
      setLevels,
      getState: state,
      render,
      dispose() {
        if (root.parentNode) root.parentNode.removeChild(root);
      },
    };
  }

  return { create };
})();
