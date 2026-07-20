// ChordFlow filter cascade — the PURE hierarchical-facet logic behind FilterR's consumers (filter-ux-facets IN3).
//
// FilterR is a dumb view; the *data* logic of the facet cascade lives here, in one pure module shared by the
// Content list and the Practice strip (so the two cascades can't drift). No DOM, no bridge — just items in, chip
// levels + filtered items out. Browser (window.ChordFlowFilterCascade) AND node (module.exports) — so it is
// unit-testable in isolation (C5).
//
// A "level def" is { key, label, values(item) -> string[] } in cascade order (e.g. Source → Genre → Subgenre →
// Tags). `selected` is { [key]: Set<token> } — the user's current per-level selection.
//
//   build(items, levelDefs, selected) -> { levels, filtered, total }
//     levels: [{ key, label, chips:[{ token, label, count, disabled, selected }] }]  (chips for FilterR.setLevels)
//     filtered: the items passing every level's selection ; total = filtered.length
//   initialSelected(items, levelDefs) -> { [key]: Set }   (all values selected = all-on)
//   resetBelow(items, levelDefs, selected, changedKey) -> new selected   (levels below changedKey reset to all
//     still-available; changedKey and above kept — the "change a higher level resets the lower ones" model)
"use strict";

(function (factory) {
  const api = factory();
  if (typeof module !== "undefined" && module.exports) module.exports = api;
  if (typeof window !== "undefined") window.ChordFlowFilterCascade = api;
})(function () {
  // Distinct non-empty values across all items for an accessor — the STABLE full vocabulary of a level (a value
  // greys out rather than vanishing when it has no matches under the current higher selection).
  function distinctValues(items, accessor) {
    const seen = new Set();
    for (const it of items) for (const v of accessor(it)) if (v != null && v !== "") seen.add(v);
    return [...seen].sort((a, b) => String(a).localeCompare(String(b)));
  }

  // Every element of `available` is in `selectedSet` — i.e. the user hasn't narrowed this level (all-on).
  function isSuperset(selectedSet, available) {
    for (const a of available) if (!selectedSet.has(a)) return false;
    return true;
  }

  // Does `it` pass one level? All-on (selected ⊇ available) ⇒ unconstrained (items with no value here still pass).
  // Otherwise the item must carry a value that is selected (OR within the level).
  function passesLevel(it, def, selectedSet, available) {
    if (isSuperset(selectedSet, available)) return true;
    for (const v of def.values(it)) if (selectedSet.has(v)) return true;
    return false;
  }

  // Passes every level strictly ABOVE `upto` (levels [0, upto)); availableByKey must hold those levels already.
  function passesUpTo(it, levelDefs, selected, availableByKey, upto) {
    for (let j = 0; j < upto; j++) {
      const def = levelDefs[j];
      if (!passesLevel(it, def, selected[def.key] || new Set(), availableByKey[def.key] || new Set())) return false;
    }
    return true;
  }

  function build(items, levelDefs, selected) {
    const availableByKey = {};
    const levels = [];
    for (let i = 0; i < levelDefs.length; i++) {
      const def = levelDefs[i];
      const higherItems = items.filter((it) => passesUpTo(it, levelDefs, selected, availableByKey, i));
      const values = distinctValues(items, def.values);
      const selSet = selected[def.key] || new Set();
      const availSet = new Set();
      const chips = values.map((v) => {
        let count = 0;
        for (const it of higherItems) if (def.values(it).indexOf(v) >= 0) count++;
        if (count > 0) availSet.add(v);
        const label = def.tokenLabel ? def.tokenLabel(v) : v; // e.g. Source maps "pack:default" → the pack's name
        return { token: v, label: label, count: count, disabled: count === 0, selected: count > 0 && selSet.has(v) };
      });
      availableByKey[def.key] = availSet;
      levels.push({ key: def.key, label: def.label, chips: chips });
    }
    const filtered = items.filter((it) => passesUpTo(it, levelDefs, selected, availableByKey, levelDefs.length));
    return { levels: levels, filtered: filtered, total: filtered.length };
  }

  function initialSelected(items, levelDefs) {
    const sel = {};
    for (const def of levelDefs) sel[def.key] = new Set(distinctValues(items, def.values));
    return sel;
  }

  function resetBelow(items, levelDefs, selected, changedKey) {
    const idx = levelDefs.findIndex((d) => d.key === changedKey);
    const next = {};
    for (const def of levelDefs) next[def.key] = new Set(selected[def.key] || []);
    if (idx < 0) return next;
    const availableByKey = {};
    for (let i = 0; i < levelDefs.length; i++) {
      const def = levelDefs[i];
      const higherItems = items.filter((it) => passesUpTo(it, levelDefs, next, availableByKey, i));
      const availSet = new Set();
      for (const it of higherItems) for (const v of def.values(it)) if (v != null && v !== "") availSet.add(v);
      availableByKey[def.key] = availSet;
      if (i > idx) next[def.key] = new Set(availSet); // reset the lower level to all still-available
    }
    return next;
  }

  return { distinctValues: distinctValues, build: build, initialSelected: initialSelected, resetBelow: resetBelow };
});
