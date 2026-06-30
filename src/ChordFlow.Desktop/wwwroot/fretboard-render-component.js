// ChordFlow shared fretboard render component — the spatial twin of score-render-component.js.
//
// The single owner of "Core-computed FretboardDiagram marker model → SVG fretboard". Every screen that shows a
// positional entity on a fretboard (a voicing today; scales / arpeggios / the interval lattice as the derivation
// engine ships) uses it, so the chrome (label toggle, legend) and geometry stay consistent. It is a DUMB DRAWER:
// NO music theory here (IN6/C1) — it consumes the note / interval / function / shape already resolved in Core.
//
// The model (camelCase over the bridge, matching FretboardDiagram):
//   { title, markers: [{ string, fret, note, interval, function, shape }], mutedStrings, barreFret, fretMin, fretMax, zoneFretMin, zoneFretMax }
//   • string  1 = high E .. 6 = low E (alphaTab numbering). Many markers may share a string.
//   • fret    0 = open (drawn as a ringed dot above the nut).
//   • function color-key: "root"|"third"|"fifth"|"seventh"|"tension" (the default palette).
//   • interval label ("R"/"b3"/"5"/"#5"/"bb7"…) — also the key for an override per-interval palette.
//   • shape   layer channel, MarkerShape ordinal: 0 Circle, 1 Square, 2 Diamond, 3 Ring (a name string is tolerated).
//   • zoneFretMin/zoneFretMax (optional): a translucent highlight band behind those fret columns (e.g. the CAGED
//     octave zone). The drawn fret window always grows to contain the band (neither the model window nor a user
//     min/max override can clip it). Omit both for no band — chord/scale diagrams render byte-identical.
//
//   const view = ChordFlowFretboard.create(containerEl, {
//     orientation: "vertical",  // "vertical" = chord box (strings as columns) | "horizontal" = neck (frets left→right).
//     labelMode:   "interval",  // "interval" | "note" — toggled by the component's own toolbar.
//     showLegend:  true,
//     theme:       "light",     // "light" (default; dark lines on white) | "dark" (light lines, white fret numbers
//                               //   + ✕ for a dark cell background). Chrome only — the marker palette is unchanged.
//                               //   Toggle at runtime with setTheme("dark"); a grid drives it globally (see below).
//     title:       null,        // optional per-cell heading (e.g. "Dominant 7 (shell) — E shape"); overrides the
//                               //   diagram's model.title when set. A grid cell passes the voicing's display name here.
//     id:          null,        // optional synthetic voicing id (e.g. "auto:shell:dom7:E"); shown with a copy-to-
//                               //   clipboard control — the oracle/debug handle (the seed of "explain this voicing").
//     palette:     null,        // null = default 5-color function palette; or { "b3":"#…", "*":"#000" } keyed on
//                               //   interval, with an optional "*" fallback color for any interval not listed.
//     controls:    {},          // per-control visibility, all true by default; a consumer hides what it fixes:
//                               //   { orientation, fretWindow, label, legend, theme }. e.g. a scale page locks horizontal
//                               //   with controls:{ orientation:false }; inside a VoicingsR grid the cell locks
//                               //   controls:{ orientation:false } so the grid's one global toggle drives every cell.
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
    sixth: "#f59e0b",
    seventh: "#a855f7",
    tension: "#9aa0a6",
  };
  // Display label per function, for the legend in default-palette mode.
  const FUNCTION_LABELS = {
    root: "Root",
    third: "3rd",
    fifth: "5th",
    sixth: "6th",
    seventh: "7th",
    tension: "Tension",
  };
  // Theme tables (NOT the marker palette — that reads on both backgrounds and is untouched). The component owns its
  // whole render area: `bg` is the surface behind the toolbar + SVG + legend (so the theme actually changes the
  // background, not just the strokes), `text`/`muted` are the toolbar/legend foreground, `ctrl*` styles the buttons +
  // inputs, and the remaining keys are the SVG chrome (nut, lines, fret numbers, position label, muted ✕, barre).
  // "light" = white surface + dark foreground; "dark" = dark-grey surface + light foreground.
  const THEMES = {
    light: {
      bg: "#ffffff", text: "#222222", muted: "#5a5f64",
      ctrlBg: "#eceef1", ctrlBorder: "#c4c8cd", ctrlText: "#222222",
      nut: "#222", line: "#999", posLabel: "#555", fretNum: "#777", svgMuted: "#888", barre: "#33333355",
    },
    dark: {
      bg: "#2a2a2d", text: "#e6e6e6", muted: "#9aa0a6",
      ctrlBg: "#3a3a3d", ctrlBorder: "#4a4a4f", ctrlText: "#e6e6e6",
      nut: "#cfcfd2", line: "#6b6b70", posLabel: "#e6e6e6", fretNum: "#ffffff", svgMuted: "#ffffff", barre: "#e6e6e655",
    },
  };

  const SHAPE_NAMES = ["circle", "square", "diamond", "ring"]; // MarkerShape ordinal → name
  // Sort weight for a legend entry: the interval's degree number, so the legend reads low→high (1, 3, 5, 6, 7, …)
  // instead of string-encounter order. "R" is the root (1); any token's degree is its first run of digits.
  function legendRank(interval) {
    if (interval === "R") return 1;
    const m = String(interval).match(/\d+/);
    return m ? parseInt(m[0], 10) : 999;
  }

  // Geometry (vertical chord box)
  const COL_GAP = 26;
  const ROW_H = 24;
  const LEFT = 34; // room for a position label (end-anchored at LEFT-8; sized for a 2-digit "12fr")
  const TOP = 22; // room for open/mute markers
  const DOT_R = 9;
  const ZONE_MARGIN = 2; // frets of context kept each side of the zone band so it reads within the neck, not edge-to-edge

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

  // Copy text to the clipboard with brief "Copied!" feedback on the triggering button. Prefers the async
  // Clipboard API (available over the https virtual host); falls back to a hidden-textarea execCommand copy.
  function copyToClipboard(text, btn) {
    const flash = () => {
      if (!btn) return;
      const prev = btn.textContent;
      btn.textContent = "Copied!";
      setTimeout(() => (btn.textContent = prev), 1200);
    };
    if (navigator.clipboard && navigator.clipboard.writeText) {
      navigator.clipboard.writeText(text).then(flash, () => fallbackCopy(text, flash));
    } else {
      fallbackCopy(text, flash);
    }
  }

  function fallbackCopy(text, done) {
    const ta = document.createElement("textarea");
    ta.value = text;
    ta.style.cssText = "position:fixed;top:-1000px;opacity:0;";
    document.body.appendChild(ta);
    ta.select();
    try {
      document.execCommand("copy");
      done();
    } catch (_) {
      /* clipboard unavailable — silently no-op */
    }
    document.body.removeChild(ta);
  }

  function create(container, opts) {
    opts = opts || {};
    const palette = opts.palette || null; // override, keyed on interval token (with an optional "*" fallback)
    const controls = opts.controls || {}; // per-control visibility; every control defaults visible (!== false)
    const fixedTitle = opts.title || null; // optional per-cell heading; overrides model.title when set
    const voicingId = opts.id || null; // optional synthetic voicing id, shown with copy-to-clipboard
    let labelMode = opts.labelMode === "note" ? "note" : "interval";
    let orientation = opts.orientation === "horizontal" ? "horizontal" : "vertical";
    let theme = opts.theme === "dark" ? "dark" : "light";
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

      // The component owns its own themed surface (a root wrapper) rather than inheriting the host container's
      // background — that is what makes the light/dark toggle actually change the background, not just the strokes.
      const t = THEMES[theme];
      const root = document.createElement("div");
      root.style.cssText = `background:${t.bg};color:${t.text};border-radius:6px;overflow:hidden;`;
      root.appendChild(buildToolbar());
      root.appendChild(orientation === "horizontal" ? buildSvgHorizontal() : buildSvg());
      if (showLegend) root.appendChild(buildLegend());
      container.appendChild(root);
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
      let windowMin = w.fretMin != null ? w.fretMin : frettedFrets.length ? Math.min(...frettedFrets) : 1;
      let windowMax = w.fretMax != null ? w.fretMax : frettedFrets.length ? Math.max(...frettedFrets) : windowMin;
      // The zone band is part of what the diagram shows: always grow the window to contain it — plus ZONE_MARGIN
      // frets of context each side so the band reads within the neck rather than edge-to-edge — so neither the model
      // window nor a user min/max override can clip it (caged-chords-chat-002 — limit the window to the zone).
      if (model.zoneFretMin != null) windowMin = Math.min(windowMin, model.zoneFretMin - ZONE_MARGIN);
      if (model.zoneFretMax != null) windowMax = Math.max(windowMax, model.zoneFretMax + ZONE_MARGIN);
      const showNut = windowMin <= 1;
      const topFret = showNut ? 1 : windowMin;
      const fretCount = Math.max(4, windowMax - topFret + 1);
      return { showNut, topFret, fretCount };
    }

    // The fret window actually drawn — [min, max] frets — after the model/override + zone-containment math above.
    function shownWindow() {
      const { topFret, fretCount } = computeWindow();
      return { min: topFret, max: topFret + fretCount - 1 };
    }

    // A small toolbar button (the shared style for the label/orientation toggles).
    function toolbarButton(text, onClick) {
      const t = THEMES[theme];
      const btn = document.createElement("button");
      btn.type = "button";
      btn.textContent = text;
      btn.style.cssText =
        `font:inherit;font-size:.75rem;padding:.15rem .55rem;border:1px solid ${t.ctrlBorder};border-radius:4px;background:${t.ctrlBg};color:${t.ctrlText};cursor:pointer;`;
      btn.addEventListener("click", onClick);
      return btn;
    }

    function buildToolbar() {
      const t = THEMES[theme];
      const bar = document.createElement("div");
      bar.style.cssText = "padding:.4rem .5rem;display:flex;gap:.5rem;align-items:center;flex-wrap:wrap;";
      // Title: the per-cell heading (opts.title) wins over the diagram's own title (the chord symbol).
      const titleText = fixedTitle || model.title;
      if (titleText) {
        const title = document.createElement("span");
        title.textContent = titleText;
        title.style.cssText = `font-size:.85rem;font-weight:600;color:${t.text};`;
        bar.appendChild(title);
      }
      // Synthetic voicing id + copy-to-clipboard (the oracle/debug handle). Shown right after the title.
      if (voicingId) {
        const idChip = document.createElement("code");
        idChip.textContent = voicingId;
        idChip.title = voicingId;
        idChip.style.cssText =
          `font-size:.7rem;color:${t.muted};font-family:ui-monospace,SFMono-Regular,Menlo,monospace;max-width:14rem;overflow:hidden;text-overflow:ellipsis;white-space:nowrap;`;
        bar.appendChild(idChip);
        bar.appendChild(toolbarButton("Copy id", (e) => copyToClipboard(voicingId, e.currentTarget)));
      }
      const spacer = document.createElement("span");
      spacer.style.cssText = "flex:1;";
      bar.appendChild(spacer);

      // Fret-window controls (min/max). The inputs show the *current* drawn window (so the limits are visible);
      // editing one sets an override and re-renders, clearing it (blank) reverts to auto. The window can never shrink
      // past the zone band (computeWindow grows it back), so a too-tight entry visibly snaps to the zone on render.
      if (controls.fretWindow !== false) {
        const shown = shownWindow();
        const mk = (placeholder, value, set) => {
          const input = document.createElement("input");
          input.type = "number";
          input.min = "0";
          input.placeholder = placeholder;
          input.value = value != null ? String(value) : "";
          input.style.cssText =
            `font:inherit;font-size:.75rem;width:3.2rem;padding:.1rem .3rem;border:1px solid ${t.ctrlBorder};border-radius:4px;background:${t.ctrlBg};color:${t.ctrlText};`;
          input.addEventListener("change", () => {
            const v = input.value.trim();
            set(v === "" ? null : Math.max(0, parseInt(v, 10) || 0));
            render();
          });
          return input;
        };
        const fretsLabel = document.createElement("span");
        fretsLabel.textContent = "Frets:";
        fretsLabel.style.cssText = `font-size:.75rem;color:${t.muted};`;
        bar.appendChild(fretsLabel);
        bar.appendChild(mk("min", shown.min, (v) => (userFretMin = v)));
        bar.appendChild(mk("max", shown.max, (v) => (userFretMax = v)));
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
        label.style.cssText = `font-size:.75rem;color:${t.muted};`;
        bar.appendChild(label);
        bar.appendChild(toolbarButton(
          labelMode === "interval" ? "Intervals" : "Notes",
          () => setLabelMode(labelMode === "interval" ? "note" : "interval")));
      }

      // Theme toggle (light ↔ dark). Hidden inside a grid (controls.theme:false), where the grid drives it globally.
      if (controls.theme !== false) {
        bar.appendChild(toolbarButton(
          theme === "dark" ? "Dark" : "Light",
          () => setTheme(theme === "dark" ? "light" : "dark")));
      }
      return bar;
    }

    function buildSvg() {
      const muted = new Set(model.mutedStrings || []);
      const t = THEMES[theme];
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

      // Optional zone band (under the grid + markers): a translucent strip behind the highlighted fret rows.
      if (model.zoneFretMin != null && model.zoneFretMax != null) {
        const zMin = Math.max(model.zoneFretMin, topFret);
        const zMax = Math.min(model.zoneFretMax, topFret + rows - 1);
        if (zMax >= zMin) {
          svg.appendChild(el("rect", {
            x: boxLeft, y: nutY + (zMin - topFret) * ROW_H,
            width: boxRight - boxLeft, height: (zMax - zMin + 1) * ROW_H, fill: "#ffd54f33",
          }));
        }
      }

      // Nut (thick if open position) or a position label.
      svg.appendChild(el("line", {
        x1: boxLeft, y1: nutY, x2: boxRight, y2: nutY,
        stroke: t.nut, "stroke-width": showNut ? 4 : 1.5,
      }));
      if (!showNut) {
        svg.appendChild(el("text", {
          x: boxLeft - 8, y: nutY + ROW_H * 0.7, "text-anchor": "end", "font-size": 11, fill: t.posLabel,
        }, `${topFret}fr`));
      }

      // Fret lines
      for (let r = 1; r <= rows; r++) {
        const y = nutY + r * ROW_H;
        svg.appendChild(el("line", { x1: boxLeft, y1: y, x2: boxRight, y2: y, stroke: t.line, "stroke-width": 1 }));
      }
      // String lines (column i = string number STRINGS - i: leftmost = low E)
      for (let i = 0; i < STRINGS; i++) {
        const x = colX(i);
        svg.appendChild(el("line", { x1: x, y1: nutY, x2: x, y2: boxBottom, stroke: t.line, "stroke-width": 1 }));
      }

      // Optional barre across a fret.
      if (model.barreFret != null && model.barreFret >= topFret) {
        const y = nutY + (model.barreFret - topFret + 1 - 0.5) * ROW_H;
        svg.appendChild(el("rect", {
          x: boxLeft - DOT_R, y: y - 6, width: boxRight - boxLeft + 2 * DOT_R, height: 12, rx: 6, fill: t.barre,
        }));
      }

      // Muted strings (diagram chrome) → ✕ above the nut.
      for (const s of muted) {
        const x = colX(STRINGS - s);
        svg.appendChild(el("text", { x, y: nutY - 8, "text-anchor": "middle", "font-size": 12, fill: t.svgMuted }, "✕"));
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
      const t = THEMES[theme];
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

      // Optional zone band (under the grid + markers): a translucent strip behind the highlighted fret columns.
      if (model.zoneFretMin != null && model.zoneFretMax != null) {
        const zMin = Math.max(model.zoneFretMin, topFret);
        const zMax = Math.min(model.zoneFretMax, topFret + fretCount - 1);
        if (zMax >= zMin) {
          svg.appendChild(el("rect", {
            x: nutX + (zMin - topFret) * fretW, y: topY,
            width: (zMax - zMin + 1) * fretW, height: bottomY - topY, fill: "#ffd54f33",
          }));
        }
      }

      // Nut (thick at the open position) or a position label above it.
      svg.appendChild(el("line", {
        x1: nutX, y1: topY, x2: nutX, y2: bottomY, stroke: t.nut, "stroke-width": showNut ? 4 : 1.5,
      }));
      if (!showNut) {
        svg.appendChild(el("text", { x: nutX, y: topY - 5, "text-anchor": "middle", "font-size": 11, fill: t.posLabel }, `${topFret}fr`));
      }

      // Fret lines (vertical) + a fret number under each cell.
      for (let k = 1; k <= fretCount; k++) {
        const x = nutX + k * fretW;
        svg.appendChild(el("line", { x1: x, y1: topY, x2: x, y2: bottomY, stroke: t.line, "stroke-width": 1 }));
      }
      for (let k = 0; k < fretCount; k++) {
        svg.appendChild(el("text", {
          x: nutX + (k + 0.5) * fretW, y: bottomY + 14, "text-anchor": "middle", "font-size": 11, fill: t.fretNum,
        }, String(topFret + k)));
      }

      // String lines (horizontal).
      for (let s = 1; s <= STRINGS; s++) {
        const y = stringY(s);
        svg.appendChild(el("line", { x1: nutX, y1: y, x2: rightX, y2: y, stroke: t.line, "stroke-width": 1 }));
      }

      // Optional barre → a vertical bar at the fret column.
      if (model.barreFret != null && model.barreFret >= topFret && model.barreFret < topFret + fretCount) {
        const x = cellCenterX(model.barreFret);
        svg.appendChild(el("rect", {
          x: x - 6, y: stringY(1) - DOT_R, width: 12, height: bottomY - topY + 2 * DOT_R, rx: 6, fill: t.barre,
        }));
      }

      // Muted strings → ✕ left of the nut.
      for (const s of muted) {
        svg.appendChild(el("text", { x: nutX - 16, y: stringY(s) + 4, "text-anchor": "middle", "font-size": 12, fill: t.svgMuted }, "✕"));
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
      const t = THEMES[theme];
      const wrap = document.createElement("div");
      wrap.style.cssText = "display:flex;gap:.6rem;flex-wrap:wrap;justify-content:center;padding:.3rem .5rem .6rem;";
      const seen = new Set();
      const entries = [];
      for (const marker of model.markers) {
        const { key, label } = legendKeyLabel(marker);
        if (seen.has(key)) continue;
        seen.add(key);
        entries.push({ label, color: colorFor(marker), rank: legendRank(marker.interval) });
      }
      entries.sort((a, b) => a.rank - b.rank);
      for (const entry of entries) {
        const item = document.createElement("span");
        item.style.cssText = `display:inline-flex;align-items:center;gap:.25rem;font-size:.7rem;color:${t.muted};`;
        const dot = document.createElement("span");
        dot.style.cssText = `width:10px;height:10px;border-radius:50%;background:${entry.color};display:inline-block;`;
        item.appendChild(dot);
        item.appendChild(document.createTextNode(entry.label));
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

    function setTheme(mode) {
      theme = mode === "dark" ? "dark" : "light";
      render();
    }

    function dispose() {
      container.innerHTML = "";
      model = null;
    }

    return { render, setLabelMode, setOrientation, setTheme, dispose };
  }

  return { create };
})();
