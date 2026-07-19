// ChordFlow shared drums render component (DrumsR) — the percussion twin of fretboard-render-component.js.
//
// The single owner of "Core-computed DrumGrooveDiagram → SVG drum grid". A DUMB DRAWER: NO music theory here
// (IN4/C1) — it consumes the lanes/hits/geometry already resolved in Core and only lays out rows × a time axis
// and animates a playback marker off the shared beat/position bus.
//
// The model (camelCase over the bridge, matching DrumGrooveDiagram):
//   { title, lanes: [{ label, voice, hits: [{ bar, tick }] }], barCount, beatsPerBar, ticksPerBar }
//   • lanes are voice-major rows top→bottom (first-seen order); label is the short DSL token ("HH"/"SD"/"BD").
//   • a hit's x = (bar*beatsPerBar + tick/beatTicks) * beatWidth, beatTicks = ticksPerBar/beatsPerBar.
//
//   const view = ChordFlowDrums.create(containerEl, { theme: "light" });
//   view.render(model);
//   view.highlightCell(bar, beat);  // move the playback marker (0-based bar + 0-based quarter-beat)
//   view.clearHighlight();
//   view.setTheme("dark");
//   view.dispose();
"use strict";

window.ChordFlowDrums = (function () {
  const NS = "http://www.w3.org/2000/svg";

  // Voice → colour, keyed by the short DSL token. Cymbals warm, snare red, kick blue, toms green — enough
  // contrast to read the grid at a glance. Unknown labels fall back to grey.
  const VOICE_COLORS = {
    HH: "#f59e0b", OH: "#f59e0b", PH: "#d97706", RD: "#eab308", RB: "#eab308", CC: "#f97316",
    SD: "#e2574c",
    BD: "#3b82f6",
    HT: "#22a06b", MT: "#22a06b", FT: "#16a34a",
  };

  const THEMES = {
    light: { bg: "#ffffff", text: "#222222", muted: "#5a5f64", grid: "#d7dade", barLine: "#9aa0a6", marker: "rgba(59,130,246,0.16)", rowAlt: "#f6f7f9" },
    dark: { bg: "#23262b", text: "#e8eaed", muted: "#b0b4b9", grid: "#3a3f45", barLine: "#6b7178", marker: "rgba(96,165,250,0.22)", rowAlt: "#282c31" },
  };

  const LABEL_W = 44;   // left gutter for row labels
  const ROW_H = 30;     // per-lane row height
  const BEAT_W = 40;    // width of one quarter-beat column
  const PAD_T = 22;     // top pad (bar numbers)
  const PAD_B = 8;
  const HIT_R = 8;

  function el(name, attrs) {
    const n = document.createElementNS(NS, name);
    for (const k in attrs) n.setAttribute(k, attrs[k]);
    return n;
  }

  function create(container, opts) {
    opts = opts || {};
    let theme = opts.theme === "dark" ? "dark" : "light";
    let model = null;
    let marker = null; // { bar, beat } or null

    const root = document.createElement("div");
    root.style.overflowX = "auto";
    root.style.width = "100%";
    container.appendChild(root);

    let svg = null;
    let markerEl = null;

    function beatTicks() { return model.ticksPerBar / model.beatsPerBar; }
    function totalBeats() { return model.barCount * model.beatsPerBar; }
    function xForBeat(globalBeat) { return LABEL_W + globalBeat * BEAT_W; }
    function xForHit(h) { return xForBeat(h.bar * model.beatsPerBar + h.tick / beatTicks()); }

    function draw() {
      if (svg) svg.remove();
      markerEl = null;
      if (!model || !model.lanes) return;

      const t = THEMES[theme];
      const gridW = LABEL_W + totalBeats() * BEAT_W;
      const gridH = PAD_T + model.lanes.length * ROW_H + PAD_B;

      svg = el("svg", { viewBox: `0 0 ${gridW} ${gridH}`, width: gridW, height: gridH, role: "img" });
      svg.style.maxWidth = "none";
      svg.style.background = t.bg;
      svg.style.borderRadius = "6px";
      svg.style.fontFamily = "system-ui, sans-serif";

      // Row backgrounds (alternating) + row labels.
      model.lanes.forEach((lane, i) => {
        const y = PAD_T + i * ROW_H;
        if (i % 2 === 1) svg.appendChild(el("rect", { x: LABEL_W, y, width: totalBeats() * BEAT_W, height: ROW_H, fill: t.rowAlt }));
        const label = el("text", { x: LABEL_W - 8, y: y + ROW_H / 2, "text-anchor": "end", "dominant-baseline": "central", "font-size": 13, "font-weight": 600, fill: VOICE_COLORS[lane.label] || t.text });
        label.textContent = lane.label;
        svg.appendChild(label);
      });

      // The playback marker band (behind hits) — created once, positioned by highlightCell.
      markerEl = el("rect", { x: 0, y: PAD_T, width: BEAT_W, height: model.lanes.length * ROW_H, fill: t.marker, visibility: "hidden" });
      svg.appendChild(markerEl);

      // Beat gridlines (light) + bar lines (strong) + bar numbers.
      for (let b = 0; b <= totalBeats(); b++) {
        const x = xForBeat(b);
        const isBar = b % model.beatsPerBar === 0;
        svg.appendChild(el("line", { x1: x, y1: PAD_T, x2: x, y2: gridH - PAD_B, stroke: isBar ? t.barLine : t.grid, "stroke-width": isBar ? 1.5 : 1 }));
        if (isBar && b < totalBeats()) {
          const num = el("text", { x: x + 3, y: 13, "font-size": 10, fill: t.muted });
          num.textContent = String(b / model.beatsPerBar + 1);
          svg.appendChild(num);
        }
      }

      // Horizontal row separators.
      for (let i = 0; i <= model.lanes.length; i++) {
        const y = PAD_T + i * ROW_H;
        svg.appendChild(el("line", { x1: LABEL_W, y1: y, x2: LABEL_W + totalBeats() * BEAT_W, y2: y, stroke: t.grid, "stroke-width": 1 }));
      }

      // Hits.
      model.lanes.forEach((lane, i) => {
        const cy = PAD_T + i * ROW_H + ROW_H / 2;
        const color = VOICE_COLORS[lane.label] || t.muted;
        (lane.hits || []).forEach(h => {
          svg.appendChild(el("circle", { cx: xForHit(h), cy, r: HIT_R, fill: color, stroke: t.bg, "stroke-width": 1.5 }));
        });
      });

      root.appendChild(svg);
      applyMarker();
    }

    function applyMarker() {
      if (!markerEl) return;
      if (!marker) { markerEl.setAttribute("visibility", "hidden"); return; }
      const globalBeat = marker.bar * model.beatsPerBar + marker.beat;
      markerEl.setAttribute("x", xForBeat(globalBeat));
      markerEl.setAttribute("visibility", "visible");
    }

    return {
      render(m) { model = m; marker = null; draw(); },
      highlightCell(bar, beat) { if (!model) return; marker = { bar, beat }; applyMarker(); },
      clearHighlight() { marker = null; applyMarker(); },
      setTheme(next) { theme = next === "dark" ? "dark" : "light"; draw(); },
      dispose() { if (svg) svg.remove(); if (root.parentNode) root.parentNode.removeChild(root); },
    };
  }

  return { create };
})();
