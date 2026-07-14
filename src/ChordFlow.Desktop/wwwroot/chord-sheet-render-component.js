// ChordFlow chord-sheet render component (ChordSheetR) — the page twin of score-render-component.js and
// fretboard-render-component.js.
//
// The single owner of "Core-computed ChordSheet model → one self-contained <svg>". A DUMB DRAWER: no music
// theory here (C1) — it paints the concrete/Nashville/Roman notations, tone strips, fret diagrams, and `%`
// similes the Core ChordSheetBuilder already resolved. It draws the WHOLE sheet (including compact fret
// diagrams) into a single <svg> so the same node serves both the on-screen body AND export (SVG/PNG/PDF),
// with no screen-vs-export parity drift — reusing FretR's diagram *model* + *palette*, not its DOM component
// (agreed in chord-sheets-maker/chat-001; a reversal of the design's "embed FretR" idea).
//
// The model (camelCase over the bridge, matching Rendering/ChordSheets/ChordSheet):
//   { header:{ title, artist, keyName, tempo, feel, timeSig, capo },
//     sections:[ { label, rows:[ { cells:[
//       { chords:[ { concrete, degree, roman, durationTicks, tones:[{note,interval,function}], diagram } ],
//         repeatOfPrev, barTicks } ] } ] } ] }
//
//   const sheet = ChordFlowChordSheet.create(containerEl, {
//     layout:    "A",          // "A" flowing leadsheet | "B" fixed grid
//     notation:  { primary:"concrete", secondary:null },  // each ∈ "concrete"|"nashville"|"roman"; secondary may be null
//     toneLabels:"notes",      // "notes" | "intervals"  (the tone-strip label toggle)
//     adornments:{ tones:false, diagrams:false },  // which below-cell adornments to paint
//     theme:     "auto",       // "light" | "dark" | "auto" (follows prefers-color-scheme)
//     barsPerRow:4,            // only affects Layout A wrapping (Layout B rows are pre-chunked by Core)
//   });
//   sheet.render(model); sheet.setLayout("B"); sheet.setNotation({primary:"roman"}); sheet.setTheme("dark");
//   sheet.svgElement();  // the raw <svg> (for export)
"use strict";

window.ChordFlowChordSheet = (function () {
  const NS = "http://www.w3.org/2000/svg";
  const STRINGS = 6;

  // Reused from FretR so a "3rd" is the same hue on the sheet as on any fretboard (one visual system).
  const FUNCTION_COLORS = {
    root: "#e2574c", third: "#3b82f6", fifth: "#22a06b",
    sixth: "#f59e0b", seventh: "#a855f7", tension: "#9aa0a6",
  };

  // Theme tables (chrome only; the function palette reads on both). "light" is pinned on export.
  const THEMES = {
    light: {
      bg: "#ffffff", ink: "#1a1a1a", muted: "#5a5f64", rule: "#333", cellBorder: "#c4c8cd",
      sectionBorder: "#9aa0a6", tag: "#eceef1", tagText: "#222", diagLine: "#999", diagMuted: "#888",
    },
    dark: {
      bg: "#242427", ink: "#ededed", muted: "#9aa0a6", rule: "#c9c9cd", cellBorder: "#4a4a4f",
      sectionBorder: "#6b6b70", tag: "#3a3a3d", tagText: "#e6e6e6", diagLine: "#6b6b70", diagMuted: "#cfcfd2",
    },
  };

  // ---- geometry --------------------------------------------------------------------------------
  const MARGIN = 16;
  const HEADER_H = 52;
  const SECTION_GAP = 16;
  const TAG_H = 18;
  const BAR_W = 138;          // one bar's width (both layouts)
  const CHORD_ROW_H = 40;     // primary token band
  const SECONDARY_H = 15;     // extra band when a secondary notation line is shown
  const TONE_STRIP_H = 22;
  const DIAGRAM_H = 92;
  const PRIMARY_FONT = 22;
  const SECONDARY_FONT = 12;

  function el(tag, attrs, text) {
    const node = document.createElementNS(NS, tag);
    for (const k in attrs) node.setAttribute(k, attrs[k]);
    if (text != null) node.textContent = text;
    return node;
  }

  function tokenFor(chord, which) {
    if (which === "nashville") return chord.degree;
    if (which === "roman") return chord.roman;
    return chord.concrete;
  }

  function resolveTheme(theme) {
    if (theme === "light" || theme === "dark") return theme;
    const dark = typeof window.matchMedia === "function" && window.matchMedia("(prefers-color-scheme: dark)").matches;
    return dark ? "dark" : "light";
  }

  function create(container, opts) {
    opts = opts || {};
    let layout = opts.layout === "B" ? "B" : "A";
    let notation = normalizeNotation(opts.notation);
    let toneLabels = opts.toneLabels === "intervals" ? "intervals" : "notes";
    let adornments = { tones: false, diagrams: false, ...(opts.adornments || {}) };
    let theme = opts.theme || "auto";
    let barsPerRow = opts.barsPerRow > 0 ? opts.barsPerRow : 4;
    let model = null;
    let svgNode = null;

    function normalizeNotation(n) {
      n = n || {};
      const valid = (v) => (v === "nashville" || v === "roman" || v === "concrete" ? v : null);
      return { primary: valid(n.primary) || "concrete", secondary: valid(n.secondary) };
    }

    // The height one cell occupies with the current toggles (uniform, so grids/rows align).
    function cellHeight() {
      return CHORD_ROW_H
        + (notation.secondary ? SECONDARY_H : 0)
        + (adornments.tones ? TONE_STRIP_H : 0)
        + (adornments.diagrams ? DIAGRAM_H : 0);
    }

    // ---- svg builder (shared by on-screen render and export) -----------------------------------
    // Build the whole sheet as one detached <svg> in the given theme. render() attaches it; export serializes it.
    function buildSheetSvg(themeName) {
      const t = THEMES[themeName];
      const rowH = cellHeight();

      // Layout the sheet into rows first so we know the total size, then size the <svg>.
      const rowsPerSection = model.sections.map((s) => layoutSection(s));
      const cols = layout === "B" ? maxCols(rowsPerSection) : barsPerRow;
      const width = MARGIN * 2 + cols * BAR_W;

      const svg = el("svg", { xmlns: NS, style: `display:block;background:${t.bg};font-family:system-ui,sans-serif;` });
      let y = MARGIN;
      y = drawHeader(svg, MARGIN, y, cols * BAR_W, model.header, t);

      for (const section of rowsPerSection) {
        y = drawSection(svg, MARGIN, y, section, rowH, t);
        y += SECTION_GAP;
      }

      const height = y - SECTION_GAP + MARGIN;
      svg.setAttribute("width", width);
      svg.setAttribute("height", height);
      svg.setAttribute("viewBox", `0 0 ${width} ${height}`);
      return svg;
    }

    // ---- public render -------------------------------------------------------------------------
    function render(next) {
      if (next !== undefined) model = next;
      container.innerHTML = "";
      svgNode = null;
      if (!model || !model.sections) return;

      const themeName = resolveTheme(theme);
      const svg = buildSheetSvg(themeName);
      const root = document.createElement("div");
      root.style.cssText = `background:${THEMES[themeName].bg};overflow:auto;border-radius:6px;`;
      root.appendChild(svg);
      container.appendChild(root);
      svgNode = svg;
    }

    // ---- export (always LIGHT — req IN11) ------------------------------------------------------
    function toSvgString() {
      if (!model || !model.sections) return null;
      const svg = buildSheetSvg("light");
      return '<?xml version="1.0" encoding="UTF-8"?>\n' + new XMLSerializer().serializeToString(svg);
    }

    // Rasterize the light SVG to a PNG blob via a canvas (no external lib; data-URI image source).
    function toPngBlob(scale, cb) {
      if (!model || !model.sections) { cb(null); return; }
      const svg = buildSheetSvg("light");
      const w = parseInt(svg.getAttribute("width"), 10);
      const h = parseInt(svg.getAttribute("height"), 10);
      const s = scale > 0 ? scale : 2;
      const url = "data:image/svg+xml;charset=utf-8," + encodeURIComponent(new XMLSerializer().serializeToString(svg));
      const img = new Image();
      img.onload = () => {
        const canvas = document.createElement("canvas");
        canvas.width = Math.max(1, Math.round(w * s));
        canvas.height = Math.max(1, Math.round(h * s));
        const ctx = canvas.getContext("2d");
        ctx.fillStyle = "#ffffff";
        ctx.fillRect(0, 0, canvas.width, canvas.height);
        ctx.setTransform(s, 0, 0, s, 0, 0);
        ctx.drawImage(img, 0, 0);
        canvas.toBlob((blob) => cb(blob), "image/png");
      };
      img.onerror = () => cb(null);
      img.src = url;
    }

    // The detached light <svg> element — the shell drops it into the print container for host PDF export.
    function lightSvg() { return (model && model.sections) ? buildSheetSvg("light") : null; }

    // Pre-chunk a section into rows of cells (Layout B rows come pre-chunked from Core; Layout A re-wraps to barsPerRow).
    function layoutSection(section) {
      let rows;
      if (layout === "B") {
        rows = section.rows.map((r) => r.cells);
      } else {
        const flat = section.rows.flatMap((r) => r.cells);
        rows = [];
        for (let i = 0; i < flat.length; i += barsPerRow) rows.push(flat.slice(i, i + barsPerRow));
      }
      return { label: section.label, rows };
    }

    function maxCols(sections) {
      let m = 1;
      for (const s of sections) for (const row of s.rows) m = Math.max(m, row.length);
      return Math.max(m, 1);
    }

    // ---- header --------------------------------------------------------------------------------
    function drawHeader(svg, x, y, w, header, t) {
      if (!header) return y;
      svg.appendChild(el("text", { x, y: y + 20, "font-size": 20, "font-weight": 700, fill: t.ink }, header.title || ""));
      if (header.artist) {
        svg.appendChild(el("text", { x: x + w, y: y + 20, "text-anchor": "end", "font-size": 15, fill: t.ink }, header.artist));
      }
      const bits = [];
      if (header.keyName) bits.push(`key of ${prettyKey(header.keyName)}`);
      if (header.capo != null) bits.push(`capo ${ordinal(header.capo)} fret`);
      if (header.tempo != null) bits.push(`♩=${header.tempo}`);
      if (header.timeSig) bits.push(header.timeSig);
      if (header.feel) bits.push(feelLabel(header.feel));
      svg.appendChild(el("text", { x, y: y + 40, "font-size": 12, fill: t.muted }, bits.join("   ·   ")));
      svg.appendChild(el("line", { x1: x, y1: y + HEADER_H - 4, x2: x + w, y2: y + HEADER_H - 4, stroke: t.rule, "stroke-width": 1 }));
      return y + HEADER_H;
    }

    // ---- a section (rows of cells) -------------------------------------------------------------
    function drawSection(svg, x, y, section, rowH, t) {
      const topY = y;
      // Boxed section tag (Intro/Verse/A/…) above the first row.
      let contentY = y;
      if (section.label) {
        const tagW = 12 + section.label.length * 7.5;
        svg.appendChild(el("rect", { x, y, width: tagW, height: TAG_H, rx: 3, fill: t.tag, stroke: t.sectionBorder, "stroke-width": 0.75 }));
        svg.appendChild(el("text", { x: x + tagW / 2, y: y + TAG_H - 5, "text-anchor": "middle", "font-size": 11, "font-weight": 600, fill: t.tagText }, section.label));
        contentY = y + TAG_H + 4;
      }

      for (const cells of section.rows) {
        drawRow(svg, x, contentY, cells, rowH, t);
        contentY += rowH;
      }

      // Layout B frames each section block; Layout A leaves the leadsheet open (barlines carry the structure).
      if (layout === "B") {
        const rowsTop = section.label ? topY + TAG_H + 4 : topY;
        svg.appendChild(el("rect", {
          x, y: rowsTop, width: maxRowWidth(section) , height: contentY - rowsTop,
          fill: "none", stroke: t.sectionBorder, "stroke-width": 1, rx: 3,
        }));
      }
      return contentY;
    }

    function maxRowWidth(section) {
      let m = 1;
      for (const row of section.rows) m = Math.max(m, row.length);
      return m * BAR_W;
    }

    function drawRow(svg, x, y, cells, rowH, t) {
      for (let i = 0; i < cells.length; i++) {
        const cx = x + i * BAR_W;
        if (layout === "B") {
          svg.appendChild(el("rect", { x: cx, y, width: BAR_W, height: rowH, fill: "none", stroke: t.cellBorder, "stroke-width": 1 }));
        } else {
          // Leadsheet barline at the left edge of each bar, plus a closing line after the last bar of the row.
          svg.appendChild(el("line", { x1: cx, y1: y + 4, x2: cx, y2: y + CHORD_ROW_H - 2, stroke: t.rule, "stroke-width": 1.5 }));
          if (i === cells.length - 1) {
            svg.appendChild(el("line", { x1: cx + BAR_W, y1: y + 4, x2: cx + BAR_W, y2: y + CHORD_ROW_H - 2, stroke: t.rule, "stroke-width": 1.5 }));
          }
        }
        drawCell(svg, cx, y, BAR_W, cells[i], t);
      }
    }

    // ---- one bar cell --------------------------------------------------------------------------
    function drawCell(svg, x, y, w, cell, t) {
      const cx = x + w / 2;
      const primaryY = y + 26;

      if (cell.repeatOfPrev) {
        drawSimile(svg, cx, y + CHORD_ROW_H / 2 + 4, t);
        return;
      }

      const chords = cell.chords || [];
      if (chords.length === 1) {
        drawChord(svg, cx, primaryY, chords[0], t);
        let belowY = y + CHORD_ROW_H + (notation.secondary ? SECONDARY_H : 0);
        if (adornments.tones && chords[0].tones && chords[0].tones.length) {
          drawToneStrip(svg, x + 8, belowY, w - 16, chords[0].tones, t);
          belowY += TONE_STRIP_H;
        }
        if (adornments.diagrams && chords[0].diagram) {
          drawDiagram(svg, x, belowY, w, chords[0].diagram, t);
        }
        return;
      }

      // Multi-chord bar: split by each chord's tick share; tokens only (adornments are single-chord, v1).
      const total = cell.barTicks || chords.reduce((s, c) => s + (c.durationTicks || 0), 0) || 1;
      let acc = 0;
      for (const c of chords) {
        const frac = (c.durationTicks || 0) / total;
        const sub = x + (acc / total) * w;
        drawChord(svg, sub + (frac * w) / 2, primaryY, c, t);
        acc += c.durationTicks || 0;
      }
    }

    function drawChord(svg, cx, baselineY, chord, t) {
      svg.appendChild(el("text", {
        x: cx, y: baselineY, "text-anchor": "middle", "font-size": PRIMARY_FONT, "font-weight": 600, fill: t.ink,
      }, tokenFor(chord, notation.primary)));
      if (notation.secondary) {
        svg.appendChild(el("text", {
          x: cx, y: baselineY + SECONDARY_H, "text-anchor": "middle", "font-size": SECONDARY_FONT, fill: t.muted,
        }, tokenFor(chord, notation.secondary)));
      }
    }

    // The one-bar-repeat simile: a bold diagonal with a dot each side (the `%`-like mark on the references).
    function drawSimile(svg, cx, cy, t) {
      svg.appendChild(el("line", { x1: cx - 7, y1: cy + 7, x2: cx + 7, y2: cy - 7, stroke: t.ink, "stroke-width": 2.4 }));
      svg.appendChild(el("circle", { cx: cx - 6, cy: cy - 6, r: 1.8, fill: t.ink }));
      svg.appendChild(el("circle", { cx: cx + 6, cy: cy + 6, r: 1.8, fill: t.ink }));
    }

    // The tone strip: one segment per chord tone, coloured by function, labelled note ⇄ interval.
    function drawToneStrip(svg, x, y, w, tones, t) {
      const seg = w / tones.length;
      for (let i = 0; i < tones.length; i++) {
        const tone = tones[i];
        const sx = x + i * seg;
        const color = FUNCTION_COLORS[tone.function] || FUNCTION_COLORS.tension;
        svg.appendChild(el("rect", { x: sx, y: y + 3, width: seg - 2, height: TONE_STRIP_H - 7, rx: 2, fill: "none", stroke: t.cellBorder, "stroke-width": 0.75 }));
        svg.appendChild(el("circle", { cx: sx + 6, cy: y + TONE_STRIP_H / 2, r: 3, fill: color }));
        svg.appendChild(el("text", {
          x: sx + 12, y: y + TONE_STRIP_H / 2 + 3.5, "font-size": 10, fill: t.ink,
        }, toneLabels === "intervals" ? tone.interval : tone.note));
      }
    }

    // A compact vertical chord box drawn IN the sheet svg (option A) — reuses the FretboardDiagram model +
    // FretR's function palette, not FretR's DOM component. Strings 6..1 low→high left→right; dots coloured by function.
    function drawDiagram(svg, x, y, w, diagram, t) {
      const colGap = 11, rowH = 12, dotR = 4;
      const boxW = (STRINGS - 1) * colGap;
      const left = x + (w - boxW) / 2;
      const top = y + 8;

      const fretted = (diagram.markers || []).filter((m) => m.fret > 0).map((m) => m.fret);
      const windowMin = diagram.fretMin != null ? diagram.fretMin : (fretted.length ? Math.min(...fretted) : 1);
      const windowMax = fretted.length ? Math.max(...fretted) : windowMin;
      const showNut = windowMin <= 1;
      const topFret = showNut ? 1 : windowMin;
      const rows = Math.max(4, windowMax - topFret + 1);
      const colX = (i) => left + i * colGap;

      svg.appendChild(el("line", { x1: colX(0), y1: top, x2: colX(STRINGS - 1), y2: top, stroke: t.diagLine, "stroke-width": showNut ? 3 : 1 }));
      if (!showNut) svg.appendChild(el("text", { x: colX(0) - 6, y: top + rowH * 0.8, "text-anchor": "end", "font-size": 8, fill: t.muted }, `${topFret}fr`));
      for (let r = 1; r <= rows; r++) svg.appendChild(el("line", { x1: colX(0), y1: top + r * rowH, x2: colX(STRINGS - 1), y2: top + r * rowH, stroke: t.diagLine, "stroke-width": 0.75 }));
      for (let i = 0; i < STRINGS; i++) svg.appendChild(el("line", { x1: colX(i), y1: top, x2: colX(i), y2: top + rows * rowH, stroke: t.diagLine, "stroke-width": 0.75 }));

      const muted = new Set(diagram.mutedStrings || []);
      for (const s of muted) svg.appendChild(el("text", { x: colX(STRINGS - s), y: top - 3, "text-anchor": "middle", "font-size": 9, fill: t.diagMuted }, "✕"));

      for (const marker of diagram.markers || []) {
        const mx = colX(STRINGS - marker.string);
        const color = FUNCTION_COLORS[marker.function] || FUNCTION_COLORS.tension;
        if (marker.fret === 0) {
          svg.appendChild(el("circle", { cx: mx, cy: top - 5, r: 3, fill: "none", stroke: color, "stroke-width": 1.4 }));
        } else if (marker.fret >= topFret && marker.fret < topFret + rows) {
          svg.appendChild(el("circle", { cx: mx, cy: top + (marker.fret - topFret + 0.5) * rowH, r: dotR, fill: color }));
        }
      }
    }

    // ---- small presentation helpers ------------------------------------------------------------
    function prettyKey(name) { return name.replace("b", "♭").replace("#", "♯"); }
    function feelLabel(feel) {
      const f = String(feel).toLowerCase();
      if (f.includes("triplet8")) return "swing 8ths";
      if (f.includes("triplet16")) return "swing 16ths";
      if (f === "none") return "straight";
      return feel;
    }
    function ordinal(n) {
      const s = ["th", "st", "nd", "rd"], v = n % 100;
      return n + (s[(v - 20) % 10] || s[v] || s[0]);
    }

    // ---- setters / accessors -------------------------------------------------------------------
    function setLayout(next) { layout = next === "B" ? "B" : "A"; render(); }
    function setNotation(next) { notation = normalizeNotation({ ...notation, ...(next || {}) }); render(); }
    function setToneLabels(mode) { toneLabels = mode === "intervals" ? "intervals" : "notes"; render(); }
    function setAdornments(next) { adornments = { ...adornments, ...(next || {}) }; render(); }
    function setTheme(next) { theme = next || "auto"; render(); }
    function setBarsPerRow(n) { if (n > 0) { barsPerRow = n; render(); } }
    function svgElement() { return svgNode; }
    function dispose() { container.innerHTML = ""; model = null; svgNode = null; }

    return {
      render, setLayout, setNotation, setToneLabels, setAdornments, setTheme, setBarsPerRow,
      svgElement, toSvgString, toPngBlob, lightSvg, dispose,
    };
  }

  return { create };
})();
