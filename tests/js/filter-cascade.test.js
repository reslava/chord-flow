// Node unit test for the pure filter cascade (filter-ux-facets IN3/C5). No test framework — plain `node`:
//   node tests/js/filter-cascade.test.js
// Exits non-zero on the first failed assertion.
"use strict";

const assert = require("assert");
const Cascade = require("../../src/ChordFlow.Desktop/wwwroot/filter-cascade.js");

const defs = [
  { key: "genre", label: "Genre", values: (it) => (it.genre ? [it.genre] : []) },
  { key: "subgenre", label: "Subgenre", values: (it) => (it.subgenre ? [it.subgenre] : []) },
  { key: "tags", label: "Tags", values: (it) => it.tags || [] },
];

const items = [
  { id: "a", genre: "Blues", subgenre: "Shuffle", tags: ["12-bar", "beginner"] },
  { id: "b", genre: "Blues", subgenre: "Slow", tags: ["12-bar"] },
  { id: "c", genre: "Jazz", subgenre: "Bebop", tags: ["turnaround"] },
  { id: "d", genre: "Jazz", tags: [] }, // no subgenre, no tags
];

function chip(level, token) {
  return level.chips.find((c) => c.token === token);
}

// 1) initialSelected = every value; build with it is all-on ⇒ everything passes (incl. the no-subgenre item).
(function initialIsAllOn() {
  const sel = Cascade.initialSelected(items, defs);
  assert.deepStrictEqual([...sel.genre].sort(), ["Blues", "Jazz"]);
  assert.deepStrictEqual([...sel.subgenre].sort(), ["Bebop", "Shuffle", "Slow"]);
  const built = Cascade.build(items, defs, sel);
  assert.strictEqual(built.total, 4, "all-on ⇒ every item passes");
  // Counts at the top level (no higher constraint): Blues 2, Jazz 2.
  assert.strictEqual(chip(built.levels[0], "Blues").count, 2);
  assert.strictEqual(chip(built.levels[0], "Jazz").count, 2);
  // Nothing disabled at all-on.
  for (const lvl of built.levels) for (const c of lvl.chips) assert.strictEqual(c.disabled, false);
  // The no-subgenre item d still passes (all-on subgenre level is unconstrained).
  assert.ok(built.filtered.some((i) => i.id === "d"));
})();

// 2) Narrow genre → Blues only: resetBelow cascades, lower values with no matches grey out + counts update.
(function narrowGenreCascades() {
  let sel = Cascade.initialSelected(items, defs);
  sel.genre = new Set(["Blues"]);                    // the user deselected Jazz
  sel = Cascade.resetBelow(items, defs, sel, "genre"); // lower levels reset to all-available under Blues
  const built = Cascade.build(items, defs, sel);

  assert.strictEqual(built.total, 2, "only the two Blues items pass");
  assert.ok(built.filtered.every((i) => i.genre === "Blues"));

  const sub = built.levels[1];
  assert.strictEqual(chip(sub, "Bebop").disabled, true, "Bebop has no Blues item ⇒ greyed");
  assert.strictEqual(chip(sub, "Bebop").count, 0);
  assert.strictEqual(chip(sub, "Shuffle").disabled, false);
  assert.strictEqual(chip(sub, "Shuffle").count, 1);
  // resetBelow left the available subgenres selected, the empty one not.
  assert.strictEqual(chip(sub, "Shuffle").selected, true);
  assert.strictEqual(chip(sub, "Bebop").selected, false);

  const tags = built.levels[2];
  assert.strictEqual(chip(tags, "turnaround").disabled, true, "turnaround is Jazz-only ⇒ greyed");
  assert.strictEqual(chip(tags, "12-bar").count, 2);
})();

// 3) resetBelow keeps the changed level and everything above it; only lower levels are reset.
(function resetBelowKeepsHigher() {
  let sel = Cascade.initialSelected(items, defs);
  sel.genre = new Set(["Jazz"]);
  sel.subgenre = new Set(["Bebop"]);
  const next = Cascade.resetBelow(items, defs, sel, "subgenre"); // change subgenre → tags reset, genre kept
  assert.deepStrictEqual([...next.genre].sort(), ["Jazz"], "genre (above) untouched");
  assert.deepStrictEqual([...next.subgenre], ["Bebop"], "the changed level kept");
  assert.deepStrictEqual([...next.tags], ["turnaround"], "tags reset to what's available under Jazz+Bebop");
})();

// 4) Deselecting within a level (not all-on) drops items lacking a selected value.
(function constrainedDropsNoValue() {
  let sel = Cascade.initialSelected(items, defs);
  sel.subgenre = new Set(["Shuffle"]); // constrained (not all subgenres) ⇒ item d (no subgenre) drops
  const built = Cascade.build(items, defs, sel);
  assert.ok(!built.filtered.some((i) => i.id === "d"), "no-subgenre item drops once subgenre is narrowed");
  assert.deepStrictEqual(built.filtered.map((i) => i.id), ["a"]);
})();

console.log("filter-cascade: all assertions passed");
