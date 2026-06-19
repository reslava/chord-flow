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
//     orientation: "vertical",  // "vertical" = chord box (strings as columns) | "horizontal" = neck (frets left→right).
//     labelMode:   "interval",  // "interval" | "note" — toggled by the component's own toolbar.
//     showLegend:  true,
//     palette:     null,        // null = default 5-color function palette; or { "b3":"#…", "*":"#000" } keyed on
//                               //   interval, with an optional "*" fallback color for any interval not listed.
//     controls:    {},          // per-control visibility, all true by default; a consumer hides what it fixes:
//                               //   { orientation, fretWindow, label, legend }. e.g. a scale page locks horizontal
//                               //   with controls:{ orientation:false }; a voicing hides controls:{ fretWindow:false }.
//   });
//   view.render(model); view.setLabelMode("note"); view.setOrientation("horizontal"); view.dispose();
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
    const palette = opts.palette || null; // override, keyed on interval token (with an optional "*" fallback)
    const controls = opts.controls || {}; // per-control visibility; every control defaults visible (!== false)
    let labelMode = opts.labelMode === "note" ? "note" : "interval";
    let orientation = opts.orientation === "horizontal" ? "horizontal" : "vertical";
    const showLegend = opts.showLegend !== false && controls.legend !== false;
    let userFretMin = null; // fret-window overrides set via the toolbar (null = honor the model / auto-fit)
    let userFretMax = null;
    let model = null;

    // Color = interval. An override palette wins: the exact interval token, else a "*" fallback, else the function default.
    function colorFor(marker) {
      if (palette) {
        if (marker.interval in palette) return palette[marker.interval];
        if ("*" in palette) return palette["*"];
      }
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
      container.appendChild(orientation === "horizontal" ? buildSvgHorizontal() : buildSvg());
      if (showLegend) container.appendChild(buildLegend());
    }

    // The fret window honored by both orientations: a toolbar override wins, else the model's, else auto-fit (null).
    function effectiveWindow() {
      return {
        fretMin: userFretMin != null ? userFretMin : model.fretMin,
        fretMax: userFretMax != null ? userFretMax : model.fretMax,
      };
    }

    // Orientation-independent window math: the lowest shown fret (with nut when ≤1) and the fret-cell count.
    function computeWindow() {
      const w = effectiveWindow();
      const frettedFrets = model.markers.filter((m) => m.fret > 0).map((m) => m.fret);
      const windowMin = w.fretMin != null ? w.fretMin : frettedFrets.length ? Math.min(...frettedFrets) : 1;
      const showNut = windowMin <= 1;
      const topFret = showNut ? 1 : windowMin;
      const windowMax = w.fretMax != null ? w.fretMax : frettedFrets.length ? Math.max(...frettedFrets) : topFret;
      const fretCount = Math.max(4, windowMax - topFret + 1);
      return { showNut, topFret, fretCount };
    }

    // A small toolbar button (the shared style for the label/orientation toggles).
    function toolbarButton(text, onClick) {
      const btn = document.createElement("button");
      btn.type = "button";
      btn.textContent = text;
      btn.style.cssText =
        "font:inherit;font-size:.75rem;padding:.15rem .55rem;border:1px solid #4a4a4f;border-radius:4px;background:#3a3a3d;color:#e6e6e6;cursor:pointer;";
      btn.addEventListener("click", onClick);
      return btn;
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

      // Fret-window controls (min/max). Blank = auto-fit; honored by both orientations via effectiveWindow().
      if (controls.fretWindow !== false) {
        const mk = (placeholder, get, set) => {
          const input = document.createElement("input");
          input.type = "number";
          input.min = "0";
          input.placeholder = placeholder;
          input.value = get() != null ? String(get()) : "";
          input.style.cssText =
            "font:inherit;font-size:.75rem;width:3.2rem;padding:.1rem .3rem;border:1px solid #4a4a4f;border-radius:4px;background:#2c2c2f;color:#e6e6e6;";
          input.addEventListener("change", () => {
            const v = input.value.trim();
            set(v === "" ? null : Math.max(0, parseInt(v, 10) || 0));
            render();
          });
          return input;
        };
        const fretsLabel = document.createElement("span");
        fretsLabel.textContent = "Frets:";
        fretsLabel.style.cssText = "font-size:.75rem;color:#9aa0a6;";
        bar.appendChild(fretsLabel);
        bar.appendChild(mk("min", () => userFretMin, (v) => (userFretMin = v)));
        bar.appendChild(mk("max", () => userFretMax, (v) => (userFretMax = v)));
      }

      // Orientation toggle (vertical chord box ↔ horizontal neck).
      if (controls.orientation !== false) {
        bar.appendChild(toolbarButton(
          orientation === "horizontal" ? "Horizontal" : "Vertical",
          () => setOrientation(orientation === "horizontal" ? "vertical" : "horizontal")));
      }

      // Label toggle (interval ↔ note).
      if (controls.label !== false) {
        const label = document.createElement("span");
        label.textContent = "Labels:";
        label.style.cssText = "font-size:.75rem;color:#9aa0a6;";
        bar.appendChild(label);
        bar.appendChild(toolbarButton(
          labelMode === "interval" ? "Intervals" : "Notes",
          () => setLabelMode(labelMode === "interval" ? "note" : "interval")));
      }
      return bar;
    }

    function buildSvg() {
      const muted = new Set(model.mutedStrings || []);
      const { showNut, topFret, fretCount: rows } = computeWindow();

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

    // Horizontal neck: strings as rows (1 = high E on top .. 6 = low E at bottom), frets left→right from the nut.
    // The first many-per-string producer (scales) needs this; the marker model is orientation-agnostic.
    function buildSvgHorizontal() {
      const muted = new Set(model.mutedStrings || []);
      const { showNut, topFret, fretCount } = computeWindow();

      const stringGap = 22; // vertical spacing between strings
      const fretW = 36; // horizontal width of one fret cell
      const padTop = 16;
      const padLeft = 34; // room for open/mute markers + the position label
      const padRight = 12;
      const padBottom = 20; // room for fret numbers

      const nutX = padLeft;
      const rightX = nutX + fretCount * fretW;
      const topY = padTop;
      const bottomY = topY + (STRINGS - 1) * stringGap;
      const width = rightX + padRight;
      const height = bottomY + padBottom;

      const stringY = (s) => topY + (s - 1) * stringGap;
      const cellCenterX = (fret) => nutX + (fret - topFret + 0.5) * fretW;

      const svg = el("svg", {
        width, height, viewBox: `0 0 ${width} ${height}`,
        style: "display:block;margin:.2rem auto;font-family:system-ui,sans-serif;",
      });

      // Nut (thick at the open position) or a position label above it.
      svg.appendChild(el("line", {
        x1: nutX, y1: topY, x2: nutX, y2: bottomY, stroke: "#222", "stroke-width": showNut ? 4 : 1.5,
      }));
      if (!showNut) {
        svg.appendChild(el("text", { x: nutX, y: topY - 5, "text-anchor": "middle", "font-size": 10, fill: "#555" }, `${topFret}fr`));
      }

      // Fret lines (vertical) + a fret number under each cell.
      for (let k = 1; k <= fretCount; k++) {
        const x = nutX + k * fretW;
        svg.appendChild(el("line", { x1: x, y1: topY, x2: x, y2: bottomY, stroke: "#999", "stroke-width": 1 }));
      }
      for (let k = 0; k < fretCount; k++) {
        svg.appendChild(el("text", {
          x: nutX + (k + 0.5) * fretW, y: bottomY + 14, "text-anchor": "middle", "font-size": 9, fill: "#777",
        }, String(topFret + k)));
      }

      // String lines (horizontal).
      for (let s = 1; s <= STRINGS; s++) {
        const y = stringY(s);
        svg.appendChild(el("line", { x1: nutX, y1: y, x2: rightX, y2: y, stroke: "#999", "stroke-width": 1 }));
      }

      // Optional barre → a vertical bar at the fret column.
      if (model.barreFret != null && model.barreFret >= topFret && model.barreFret < topFret + fretCount) {
        const x = cellCenterX(model.barreFret);
        svg.appendChild(el("rect", {
          x: x - 6, y: stringY(1) - DOT_R, width: 12, height: bottomY - topY + 2 * DOT_R, rx: 6, fill: "#33333355",
        }));
      }

      // Muted strings → ✕ left of the nut.
      for (const s of muted) {
        svg.appendChild(el("text", { x: nutX - 16, y: stringY(s) + 4, "text-anchor": "middle", "font-size": 12, fill: "#888" }, "✕"));
      }

      // Markers (many may share a string row). Open = a ringed dot left of the nut; out-of-window frets are clipped.
      for (const marker of model.markers) {
        const y = stringY(marker.string);
        const color = colorFor(marker);
        const text = labelMode === "note" ? marker.note : marker.interval;
        if (marker.fret === 0) {
          svg.appendChild(el("circle", { cx: nutX - 16, cy: y, r: 5, fill: "none", stroke: color, "stroke-width": 1.6 }));
        } else if (marker.fret >= topFret && marker.fret < topFret + fretCount) {
          drawMarker(svg, cellCenterX(marker.fret), y, color, shapeName(marker.shape), text);
        }
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

    function setOrientation(mode) {
      orientation = mode === "horizontal" ? "horizontal" : "vertical";
      render();
    }

    function dispose() {
      container.innerHTML = "";
      model = null;
    }

    return { render, setLabelMode, setOrientation, dispose };
  }

  return { create };
})();
