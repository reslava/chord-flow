// ChordFlow SVG chord-diagram (fret-box) renderer.
//
// Draws a voicing DiagramModel computed in Core (VoicingDiagram.Build) — a pure
// presentation layer: NO music theory here (IN6). The model gives, per string
// (low-E(6) → high-E(1)): state (muted/open/fretted), absolute fret, spelled note,
// interval label, and chord-tone function. We map function → color, draw a
// fret-box, and label dots by interval or note (toggle). A legend explains the
// colors. window.ChordFlowDiagram.render(container, model).
"use strict";

window.ChordFlowDiagram = (function () {
  const NS = "http://www.w3.org/2000/svg";

  // Function → color (the interval color key) + legend label.
  const COLORS = {
    root: "#e2574c",
    third: "#3b82f6",
    fifth: "#22a06b",
    seventh: "#a855f7",
    tension: "#9aa0a6",
  };
  const LEGEND = [
    ["root", "Root"],
    ["third", "3rd"],
    ["fifth", "5th"],
    ["seventh", "7th"],
    ["tension", "Tension"],
  ];

  // Geometry
  const COLS = 6;
  const COL_GAP = 26;
  const ROW_H = 24;
  const LEFT = 22;   // room for a position label
  const TOP = 22;    // room for open/mute markers
  const DOT_R = 9;

  let labelMode = "interval"; // "interval" | "note"
  let lastContainer = null;
  let lastModel = null;

  function el(tag, attrs, text) {
    const node = document.createElementNS(NS, tag);
    for (const k in attrs) node.setAttribute(k, attrs[k]);
    if (text != null) node.textContent = text;
    return node;
  }

  function colX(i) {
    return LEFT + i * COL_GAP;
  }

  function render(container, model) {
    lastContainer = container;
    lastModel = model;
    container.innerHTML = "";
    if (!model || !model.strings) return;

    container.appendChild(buildToolbar());
    container.appendChild(buildSvg(model));
    container.appendChild(buildLegend());
  }

  function buildToolbar() {
    const bar = document.createElement("div");
    bar.style.cssText = "padding:.4rem .5rem;display:flex;gap:.4rem;align-items:center;";
    const label = document.createElement("span");
    label.textContent = "Labels:";
    label.style.cssText = "font-size:.75rem;color:#9aa0a6;";
    const btn = document.createElement("button");
    btn.type = "button";
    btn.textContent = labelMode === "interval" ? "Intervals" : "Notes";
    btn.style.cssText =
      "font:inherit;font-size:.75rem;padding:.15rem .55rem;border:1px solid #4a4a4f;border-radius:4px;background:#3a3a3d;color:#e6e6e6;cursor:pointer;";
    btn.addEventListener("click", () => {
      labelMode = labelMode === "interval" ? "note" : "interval";
      render(lastContainer, lastModel); // re-render with the other label set
    });
    bar.appendChild(label);
    bar.appendChild(btn);
    return bar;
  }

  function buildSvg(model) {
    const fretted = model.strings.filter((s) => s.state === "fretted").map((s) => s.fret);
    const showNut = model.firstFret <= 1;
    const topFret = showNut ? 1 : model.firstFret;
    const maxFret = fretted.length ? Math.max(...fretted) : topFret;
    const rows = Math.max(4, maxFret - topFret + 1);

    const boxLeft = colX(0);
    const boxRight = colX(COLS - 1);
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
        x: boxLeft - 8, y: nutY + ROW_H * 0.7, "text-anchor": "end",
        "font-size": 10, fill: "#555",
      }, `${model.firstFret}fr`));
    }

    // Fret lines
    for (let r = 1; r <= rows; r++) {
      const y = nutY + r * ROW_H;
      svg.appendChild(el("line", { x1: boxLeft, y1: y, x2: boxRight, y2: y, stroke: "#999", "stroke-width": 1 }));
    }
    // String lines
    for (let i = 0; i < COLS; i++) {
      const x = colX(i);
      svg.appendChild(el("line", { x1: x, y1: nutY, x2: x, y2: boxBottom, stroke: "#999", "stroke-width": 1 }));
    }

    // Optional barre across a fret.
    if (model.barreFret != null && model.barreFret >= topFret) {
      const y = nutY + (model.barreFret - topFret + 1 - 0.5) * ROW_H;
      svg.appendChild(el("rect", {
        x: boxLeft - DOT_R, y: y - 6, width: boxRight - boxLeft + 2 * DOT_R, height: 12,
        rx: 6, fill: "#33333355",
      }));
    }

    // Per string: markers, dots, bottom labels.
    model.strings.forEach((s, i) => {
      const x = colX(i);
      if (s.state === "muted") {
        svg.appendChild(el("text", { x, y: nutY - 8, "text-anchor": "middle", "font-size": 12, fill: "#888" }, "✕"));
        return;
      }

      const color = COLORS[s.function] || COLORS.tension;
      const text = labelMode === "note" ? s.note : s.interval;

      if (s.state === "open") {
        svg.appendChild(el("circle", {
          cx: x, cy: nutY - 10, r: 5, fill: "none", stroke: color, "stroke-width": 1.6,
        }));
      } else {
        const cy = nutY + (s.fret - topFret + 1 - 0.5) * ROW_H;
        svg.appendChild(el("circle", { cx: x, cy, r: DOT_R, fill: color }));
        svg.appendChild(el("text", {
          x, y: cy, "text-anchor": "middle", "dominant-baseline": "central",
          "font-size": 9, fill: "#fff", "font-weight": 600,
        }, text || ""));
      }

      // Bottom label — the interval/note for every sounding string (so open strings are labeled too).
      svg.appendChild(el("text", {
        x, y: boxBottom + 16, "text-anchor": "middle", "font-size": 10, fill: color, "font-weight": 600,
      }, text || ""));
    });

    return svg;
  }

  function buildLegend() {
    const wrap = document.createElement("div");
    wrap.style.cssText =
      "display:flex;gap:.6rem;flex-wrap:wrap;justify-content:center;padding:.3rem .5rem .6rem;";
    for (const [fn, name] of LEGEND) {
      const item = document.createElement("span");
      item.style.cssText = "display:inline-flex;align-items:center;gap:.25rem;font-size:.7rem;color:#9aa0a6;";
      const dot = document.createElement("span");
      dot.style.cssText = `width:10px;height:10px;border-radius:50%;background:${COLORS[fn]};display:inline-block;`;
      item.appendChild(dot);
      item.appendChild(document.createTextNode(name));
      wrap.appendChild(item);
    }
    return wrap;
  }

  return { render };
})();
