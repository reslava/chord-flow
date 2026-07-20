// ChordFlow FilterR — the shared, dumb, faceted toggle-chip filter (filter-toggle-buttons IN3, filter-ux-facets).
//
// The toggle-filter twin of FretR / ChordSheetR / PlayerControlsR: it renders a stack of chip rows (one per
// "level"), each chip carrying an optional count, a disabled/greyed state, and a selected state — and reports the
// selected sets via `onChange`. That is ALL it does — NO data source, NO filtering logic, NO music theory (C1).
// Consumers own the data: the Content list + Practice strip cascade client-side (via filter-cascade.js) and hand
// FilterR fully-specified chips through `setLevels`; GuitarVoicingsR wires `onChange` to its server round-trip.
//
//   const f = ChordFlowFilter.create(el, {
//     levels: [{ key:"genre", label:"Genre", chips:[{token:"Blues", label:"Blues", count:3, disabled:false, selected:true}, ...] }],
//     onChange(enabledByKey, changedKey) { /* enabledByKey = { genre: Set<token>, ... }; changedKey = the level toggled */ },
//     showAllNone: true,   // per-level "All · None" (default true)
//   });
//   f.setLevels(nextLevels);   // re-render with fully-specified chips (the cascade owns counts/availability/selection)
//   f.getState();              // { [key]: Set<token> } — the selected, non-disabled tokens per level
//
// Chip fields: { token, label?, count?, disabled?, selected? }. `label` defaults to the token; `count` renders as
// a "(n)" suffix when present; `disabled` greys the chip and blocks clicks + selection; `selected` defaults to
// true (so a consumer that passes bare chips — e.g. GuitarVoicingsR — gets all-on). FilterR clones the chips, so
// the caller's objects are never mutated. Semantics (OR-within / AND-across, cascade reset) live in the consumer.
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
      .cf-chip.active { background:#3a4d78; color:#fff; border-color:#3a4d78; }
      .cf-chip.disabled { opacity:.4; cursor:default; background:#252526; color:#6b7075; border-color:#333; }
      .cf-chip-count { opacity:.7; margin-left:.25rem; }
      .cf-allnone { display:inline-flex; gap:.3rem; margin-left:.2rem; }
      .cf-allnone button { font-size:.62rem; padding:.05rem .3rem; background:none; border:none;
        color:#7a8ea8; cursor:pointer; text-decoration:underline; }
      .cf-allnone button:hover { color:#a9c0e0; }`;
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

  // Clone the caller's levels into FilterR-owned chip objects (so clicks/All/None never mutate the caller's data).
  function cloneLevels(levels) {
    return (Array.isArray(levels) ? levels : []).map((level) => ({
      key: level.key,
      label: level.label,
      mode: level.mode,
      chips: (level.chips || []).map((c) => ({
        token: c.token,
        label: c.label != null ? c.label : c.token,
        count: c.count,
        disabled: !!c.disabled,
        selected: c.selected !== false, // default on
      })),
    }));
  }

  function create(container, opts) {
    opts = opts || {};
    injectStyles();
    const onChange = typeof opts.onChange === "function" ? opts.onChange : function () {};
    const showAllNone = opts.showAllNone !== false;

    let levels = cloneLevels(opts.levels);

    const root = el("div", "cf-filter");
    container.appendChild(root);

    // The selected, non-disabled tokens per level — the shape onChange receives and getState returns.
    function state() {
      const out = {};
      for (const level of levels) {
        const set = new Set();
        for (const chip of level.chips) if (chip.selected && !chip.disabled) set.add(chip.token);
        out[level.key] = set;
      }
      return out;
    }

    function emit(changedKey) {
      onChange(state(), changedKey);
    }

    function render() {
      root.innerHTML = "";
      for (const level of levels) {
        if (!level.chips.length) continue; // nothing to show for an empty-vocabulary level
        const row = el("div", "cf-level");
        if (level.label != null) row.appendChild(el("span", "cf-level-label", level.label));
        for (const chip of level.chips) row.appendChild(buildChip(level, chip));
        if (showAllNone) row.appendChild(buildAllNone(level));
        root.appendChild(row);
      }
    }

    function buildChip(level, chip) {
      const btn = el("button", "cf-chip"
        + (chip.selected && !chip.disabled ? " active" : "")
        + (chip.disabled ? " disabled" : ""));
      btn.type = "button";
      btn.appendChild(document.createTextNode(chip.label));
      if (chip.count != null) btn.appendChild(el("span", "cf-chip-count", "(" + chip.count + ")"));
      if (!chip.disabled) {
        btn.addEventListener("click", () => {
          chip.selected = !chip.selected;
          btn.classList.toggle("active", chip.selected);
          emit(level.key); // a consumer may setLevels() here (cascade re-render); nothing touched after this
        });
      }
      return btn;
    }

    function buildAllNone(level) {
      const wrap = el("div", "cf-allnone");
      const set = (on) => {
        for (const chip of level.chips) if (!chip.disabled) chip.selected = on;
        render();          // reflect the bulk change (All selects only available chips — disabled stay off)
        emit(level.key);
      };
      const all = el("button", null, "All");
      all.type = "button";
      all.addEventListener("click", () => set(true));
      const none = el("button", null, "None");
      none.type = "button";
      none.addEventListener("click", () => set(false));
      wrap.appendChild(all);
      wrap.appendChild(none);
      return wrap;
    }

    function setLevels(next) {
      levels = cloneLevels(next);
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
