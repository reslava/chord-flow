// ChordFlow shared fretboard render component — the spatial twin of score-render-component.js.
//
// The single owner of "Core-computed FretboardDiagram marker model → SVG fretboard". Every screen that shows a
// positional entity on a fretboard (a voicing today; scales / arpeggios / the interval lattice as the derivation
// engine ships) uses it, so the chrome (label toggle, legend) and geometry stay consistent. It is a DUMB DRAWER:
// NO music theory here (IN6/C1) — it consumes the note / interval / function / shape already resolved in Core.
//
// The model (camelCase over the bridge, matching FretboardDiagram):
//   { title, markers: [{ string, fret, note, interval, function, shape }], mutedStrings, barreFret, fretMin, fretMax }
//   • string  1 = high E .. 6 = low E (alphaTab numbering). Many markers may share a string.
//   • fret    0 = open (drawn as a ringed dot above the nut).
//   • function color-key: "root"|"third"|"fifth"|"seventh"|"tension" (the default palette).
//   • interval label ("R"/"b3"/"5"/"#5"/"bb7"…) — also the key for an override per-interval palette.
//   • shape   layer channel, MarkerShape ordinal: 0 Circle, 1 Square, 2 Diamond, 3 Ring (a name string is tolerated).
//
//   const view = ChordFlowFretboard.create(containerEl, {
//     orientation: "vertical",  // "vertical" = chord box. "horizontal" accepted but deferred to vertical in v1.
//     labelMode:   "interval",  // "interval" | "note" — toggled by the component's own toolbar.
//     showLegend:  true,
//     palette:     null,        // null = default 5-color function palette; or { "b3": "#…", … } keyed on interval.
//   });
//   view.render(model); view.setLabelMode("note"); view.dispose();
"use strict";

window.ChordFlowFretboard = (function () {
  const NS = "http://www.w3.org/2000/svg";
  const STRINGS = 6; // standard tuning only (EX3)

  // Default palette: function color-key → color. Chord diagrams render byte-identical to the old chord-diagram.js.
  const FUNCTION_COLORS = {
    root: "#e2574c",
    third: "#3b82f6",
    fifth: "#22a06b",
    seventh: "#a855f7",
    tension: "#9aa0a6",
  };
  // Display label per function, for the legend in default-palette mode.
  const FUNCTION_LABELS = {
    root: "Root",
    third: "3rd",
    fifth: "5th",
    seventh: "7th",
    tension: "Tension",
  };
  const SHAPE_NAMES = ["circle", "square", "diamond", "ring"]; // MarkerShape ordinal → name

  // Geometry (vertical chord box)
  const COL_GAP = 26;
  const ROW_H = 24;
  const LEFT = 22; // room for a position label
  const TOP = 22; // room for open/mute markers
  const DOT_R = 9;

  function el(tag, attrs, text) {
    const node = document.createElementNS(NS, tag);
    for (const k in attrs) node.setAttribute(k, attrs[k]);
    if (text != null) node.textContent = text;
    return node;
  }

  function shapeName(shape) {
    if (typeof shape === "number") return SHAPE_NAMES[shape] || "circle";
    return String(shape || "circle").toLowerCase();
  }

  function create(container, opts) {
    opts = opts || {};
    const palette = opts.palette || null; // override, keyed on interval token
    let labelMode = opts.labelMode === "note" ? "note" : "interval";
    const showLegend = opts.showLegend !== false;
    let model = null;

    // Color = interval. An override palette (keyed on the interval token) wins; otherwise the function default.
    function colorFor(marker) {
      if (palette && marker.interval in palette) return palette[marker.interval];
      return FUNCTION_COLORS[marker.function] || FUNCTION_COLORS.tension;
    }
    // Legend key/label for a marker: the interval token under an override palette, else the function bucket.
    function legendKeyLabel(marker) {
      if (palette) return { key: marker.interval, label: marker.interval };
      return { key: marker.function, label: FUNCTION_LABELS[marker.function] || marker.function };
    }

    function colX(i) {
      return LEFT + i * COL_GAP;
    }

    function render(nextModel) {
      if (nextModel !== undefined) model = nextModel;
      container.innerHTML = "";
      if (!model || !model.markers) return;

      container.appendChild(buildToolbar());
      container.appendChild(buildSvg());
      if (showLegend) container.appendChild(buildLegend());
    }

    function buildToolbar() {
      const bar = document.createElement("div");
      bar.style.cssText = "padding:.4rem .5rem;display:flex;gap:.5rem;align-items:center;";
      if (model.title) {
        const title = document.createElement("span");
        title.textContent = model.title;
        title.style.cssText = "font-size:.85rem;font-weight:600;color:#e6e6e6;";
        bar.appendChild(title);
      }
      const spacer = document.createElement("span");
      spacer.style.cssText = "flex:1;";
      bar.appendChild(spacer);

      const label = document.createElement("span");
      label.textContent = "Labels:";
      label.style.cssText = "font-size:.75rem;color:#9aa0a6;";
      const btn = document.createElement("button");
      btn.type = "button";
      btn.textContent = labelMode === "interval" ? "Intervals" : "Notes";
      btn.style.cssText =
        "font:inherit;font-size:.75rem;padding:.15rem .55rem;border:1px solid #4a4a4f;border-radius:4px;background:#3a3a3d;color:#e6e6e6;cursor:pointer;";
      btn.addEventListener("click", () => setLabelMode(labelMode === "interval" ? "note" : "interval"));
      bar.appendChild(label);
      bar.appendChild(btn);
      return bar;
    }

    function buildSvg() {
      const muted = new Set(model.mutedStrings || []);
      const frettedFrets = model.markers.filter((m) => m.fret > 0).map((m) => m.fret);

      const windowMin = model.fretMin != null ? model.fretMin : frettedFrets.length ? Math.min(...frettedFrets) : 1;
      const showNut = windowMin <= 1;
      const topFret = showNut ? 1 : windowMin;
      const windowMax = model.fretMax != null ? model.fretMax : frettedFrets.length ? Math.max(...frettedFrets) : topFret;
      const rows = Math.max(4, windowMax - topFret + 1);

      const boxLeft = colX(0);
      const boxRight = colX(STRINGS - 1);
      const nutY = TOP;
      const boxBottom = nutY + rows * ROW_H;
      const width = boxRight + LEFT;
      const height = boxBottom + 26; // room for bottom labels

      const svg = el("svg", {
        width,
        height,
        viewBox: `0 0 ${width} ${height}`,
        style: "display:block;margin:.2rem auto;font-family:system-ui,sans-serif;",
      });

      // Nut (thick if open position) or a position label.
      svg.appendChild(el("line", {
        x1: boxLeft, y1: nutY, x2: boxRight, y2: nutY,
        stroke: "#222", "stroke-width": showNut ? 4 : 1.5,
      }));
      if (!showNut) {
        svg.appendChild(el("text", {
          x: boxLeft - 8, y: nutY + ROW_H * 0.7, "text-anchor": "end", "font-size": 10, fill: "#555",
        }, `${topFret}fr`));
      }

      // Fret lines
      for (let r = 1; r <= rows; r++) {
        const y = nutY + r * ROW_H;
        svg.appendChild(el("line", { x1: boxLeft, y1: y, x2: boxRight, y2: y, stroke: "#999", "stroke-width": 1 }));
      }
      // String lines (column i = string number STRINGS - i: leftmost = low E)
      for (let i = 0; i < STRINGS; i++) {
        const x = colX(i);
        svg.appendChild(el("line", { x1: x, y1: nutY, x2: x, y2: boxBottom, stroke: "#999", "stroke-width": 1 }));
      }

      // Optional barre across a fret.
      if (model.barreFret != null && model.barreFret >= topFret) {
        const y = nutY + (model.barreFret - topFret + 1 - 0.5) * ROW_H;
        svg.appendChild(el("rect", {
          x: boxLeft - DOT_R, y: y - 6, width: boxRight - boxLeft + 2 * DOT_R, height: 12, rx: 6, fill: "#33333355",
        }));
      }

      // Muted strings (diagram chrome) → ✕ above the nut.
      for (const s of muted) {
        const x = colX(STRINGS - s);
        svg.appendChild(el("text", { x, y: nutY - 8, "text-anchor": "middle", "font-size": 12, fill: "#888" }, "✕"));
      }

      // Markers (many may share a string). The lowest-fret marker on each string also gets a bottom label.
      const bottomByCol = {};
      for (const marker of model.markers) {
        const x = colX(STRINGS - marker.string);
        const color = colorFor(marker);
        const text = labelMode === "note" ? marker.note : marker.interval;

        if (marker.fret === 0) {
          svg.appendChild(el("circle", { cx: x, cy: nutY - 10, r: 5, fill: "none", stroke: color, "stroke-width": 1.6 }));
        } else {
          const cy = nutY + (marker.fret - topFret + 1 - 0.5) * ROW_H;
          drawMarker(svg, x, cy, color, shapeName(marker.shape), text);
        }
        if (!bottomByCol[x] || marker.fret < bottomByCol[x].fret) bottomByCol[x] = { text, color, fret: marker.fret };
      }

      // Bottom label — the interval/note for each string's lowest marker (so open strings are labeled too).
      for (const x in bottomByCol) {
        const entry = bottomByCol[x];
        svg.appendChild(el("text", {
          x, y: boxBottom + 16, "text-anchor": "middle", "font-size": 10, fill: entry.color, "font-weight": 600,
        }, entry.text || ""));
      }

      return svg;
    }

    // Shape = layer channel. Filled shapes carry the label in white; a ring is hollow with a colored label.
    function drawMarker(svg, x, cy, color, shape, text) {
      const r = DOT_R;
      if (shape === "ring") {
        svg.appendChild(el("circle", { cx: x, cy, r, fill: "none", stroke: color, "stroke-width": 2.2 }));
        appendLabel(svg, x, cy, text, color);
        return;
      }
      if (shape === "square") {
        svg.appendChild(el("rect", { x: x - r, y: cy - r, width: 2 * r, height: 2 * r, rx: 2, fill: color }));
      } else if (shape === "diamond") {
        svg.appendChild(el("polygon", {
          points: `${x},${cy - r - 1} ${x + r + 1},${cy} ${x},${cy + r + 1} ${x - r - 1},${cy}`, fill: color,
        }));
      } else {
        svg.appendChild(el("circle", { cx: x, cy, r, fill: color }));
      }
      appendLabel(svg, x, cy, text, "#fff");
    }

    function appendLabel(svg, x, cy, text, fill) {
      svg.appendChild(el("text", {
        x, y: cy, "text-anchor": "middle", "dominant-baseline": "central", "font-size": 9, fill, "font-weight": 600,
      }, text || ""));
    }

    function buildLegend() {
      const wrap = document.createElement("div");
      wrap.style.cssText = "display:flex;gap:.6rem;flex-wrap:wrap;justify-content:center;padding:.3rem .5rem .6rem;";
      const seen = new Set();
      for (const marker of model.markers) {
        const { key, label } = legendKeyLabel(marker);
        if (seen.has(key)) continue;
        seen.add(key);
        const item = document.createElement("span");
        item.style.cssText = "display:inline-flex;align-items:center;gap:.25rem;font-size:.7rem;color:#9aa0a6;";
        const dot = document.createElement("span");
        dot.style.cssText = `width:10px;height:10px;border-radius:50%;background:${colorFor(marker)};display:inline-block;`;
        item.appendChild(dot);
        item.appendChild(document.createTextNode(label));
        wrap.appendChild(item);
      }
      return wrap;
    }

    function setLabelMode(mode) {
      labelMode = mode === "note" ? "note" : "interval";
      render();
    }

    function dispose() {
      container.innerHTML = "";
      model = null;
    }

    return { render, setLabelMode, dispose };
  }

  return { create };
})();
