// ChordFlow Drums view — the basic-drums page (authoring + a saved-grooves library).
//
// Author a groove in the hit-grid DSL; the host (DrumGroovePreviewHandler) parses it once and returns two
// projections — the alphaTex percussion track (rendered + played by the shared ScoreR) and the grid model
// (drawn by DrumsR). While it plays, the engine's time-linear "position" clock drives the DrumsR marker, so
// the grid animates in time with the audio.
//
// Persistence (req IN6): grooves save/list/load/delete through the shared `entity*` CRUD family (entity
// "drums" → DrumGrooveStore), the same protocol the Content editor uses — surfaced here as a saved-grooves
// library rather than in the harmony-oriented Content page (a groove has no comping/key/feel/sheet). Writes
// are user-only, fork-on-edit: editing a pack groove and saving mints a new user copy (the store forks).
// This view owns only the drums-specific chrome; ScoreR owns notation/transport, DrumsR owns the grid.
"use strict";

window.ChordFlowDrumsView = (function () {
  const Bridge = window.ChordFlowBridge;
  const DEBOUNCE_MS = 300;
  const $ = (id) => document.getElementById(id);
  // A ready example: a basic rock beat (kick/snare backbeat/straight-8th hi-hat).
  const EXAMPLE = "HH :2 x x x x x x x x\nSD :2 . . x . . . x .\nBD :2 x . . . x . . .";

  let initialized = false;
  let scoreView = null; // ChordFlowScore handle (notation + transport)
  let gridView = null;  // ChordFlowDrums (DrumsR) handle
  let dslEl, tempoEl, nameEl, errorEl, scoreEl, gridEl, listEl, saveBtn, newBtn, deleteBtn;
  let debounceTimer = null;
  let editingId = null; // the saved groove being edited (null = a new/unsaved groove; a fork mints a new id)

  function setError(text) { if (errorEl) errorEl.textContent = text || ""; }
  function tempo() { return parseInt(tempoEl.value, 10) || 100; }

  function requestPreview() {
    if (!Bridge.available) { setError("Open in the ChordFlow app to preview drums."); return; }
    setError("");
    Bridge.send({ type: "drumPreview", dsl: dslEl.value, tempo: tempo() });
  }

  function schedulePreview() {
    clearTimeout(debounceTimer);
    debounceTimer = setTimeout(requestPreview, DEBOUNCE_MS);
  }

  // --- saved-grooves library (the shared entity* CRUD family, entity "drums") --------------------------
  function requestList() { if (Bridge.available) Bridge.send({ type: "entityList", entity: "drums" }); }

  function renderList(items) {
    listEl.innerHTML = "";
    if (!items || items.length === 0) {
      listEl.innerHTML = '<li class="empty">No saved grooves</li>';
      return;
    }
    for (const item of items) {
      const li = document.createElement("li");
      li.style.display = "flex";
      li.style.justifyContent = "space-between";
      li.style.gap = ".4rem";
      const name = document.createElement("span");
      name.textContent = item.name + (item.source === "package" ? "  ·  " + (item.packName || "pack") : "");
      li.appendChild(name);
      li.addEventListener("click", () => Bridge.send({ type: "entityGet", entity: "drums", entityId: item.id }));
      if (item.source === "user") {
        const del = document.createElement("span");
        del.title = "Delete";
        del.textContent = "×";
        del.style.cssText = "color:#9aa0a6; font-weight:700; padding:0 .2rem;";
        del.addEventListener("click", (e) => {
          e.stopPropagation();
          Bridge.send({ type: "entityDelete", entity: "drums", entityId: item.id });
        });
        li.appendChild(del);
      }
      li.classList.toggle("active", item.id === editingId);
      listEl.appendChild(li);
    }
  }

  function save() {
    if (!Bridge.available) return;
    Bridge.send({
      type: "entitySave",
      entity: "drums",
      entityId: editingId, // null = create; a pack id forks a new user copy (store fork-on-edit)
      name: (nameEl.value || "Groove").trim(),
      dsl: dslEl.value,
    });
  }

  function newGroove() {
    editingId = null;
    nameEl.value = "";
    dslEl.value = EXAMPLE;
    requestPreview();
    requestList(); // refresh active-row highlight
  }

  // Lazily create the shared ScoreR (notation + play/stop/tempo) and wire the grid marker to the engine's
  // time-linear "position" clock (bar/quarterBeat are 1-based → DrumsR's 0-based cell). Stopping clears it.
  function ensureScore() {
    if (scoreView || !window.ChordFlowScore) return;
    scoreView = window.ChordFlowScore.create(scoreEl, {
      player: true,
      controls: "full",
      transport: true,
      onStateChange: (playing) => { if (!playing && gridView) gridView.clearHighlight(); },
      onFinished: () => { if (gridView) gridView.clearHighlight(); },
    });
    const engine = scoreView.getEngine && scoreView.getEngine();
    if (engine) {
      engine.on("position", (bar, quarterBeat) => {
        if (gridView) gridView.highlightCell(bar - 1, quarterBeat - 1);
      });
    }
  }

  // Inbound from the host. Every registered handler sees every message; we own the drum* + drums entity* replies.
  function onHostMessage(raw) {
    let msg;
    try { msg = JSON.parse(raw); } catch (e) { return; }

    if (msg.type === "drumPreview") {
      setError("");
      if (!gridView && window.ChordFlowDrums) gridView = window.ChordFlowDrums.create(gridEl, { theme: "light" });
      if (gridView) gridView.render(msg.diagram);
      ensureScore();
      if (scoreView) scoreView.load(msg.tex, { tempo: tempo() });
    } else if (msg.type === "drumPreviewError") {
      setError(msg.message);
    } else if (msg.entity === "drums") {
      // The shared entity* CRUD replies, filtered to our entity.
      if (msg.type === "entityList") {
        renderList(msg.items);
      } else if (msg.type === "entityLoaded") {
        editingId = msg.id;
        nameEl.value = msg.name;
        dslEl.value = msg.dsl;
        setError("");
        requestPreview();
        requestList();
      } else if (msg.type === "entitySaved") {
        editingId = msg.id; // a fork returns the new user id
        requestList();
      } else if (msg.type === "entityDeleted") {
        if (msg.id === editingId) newGroove();
        else requestList();
      } else if (msg.type === "entityParseError") {
        setError(msg.message);
      }
    }
  }

  function init() {
    dslEl = $("drumDsl");
    tempoEl = $("drumTempo");
    nameEl = $("drumName");
    errorEl = $("drumError");
    scoreEl = $("drums-score");
    gridEl = $("drums-grid");
    listEl = $("drumList");
    saveBtn = $("drumSave");
    newBtn = $("drumNew");
    deleteBtn = $("drumDelete");
    if (!dslEl.value) dslEl.value = EXAMPLE;
    dslEl.addEventListener("input", schedulePreview);
    tempoEl.addEventListener("change", requestPreview);
    saveBtn.addEventListener("click", save);
    newBtn.addEventListener("click", newGroove);
    deleteBtn.addEventListener("click", () => { if (editingId) Bridge.send({ type: "entityDelete", entity: "drums", entityId: editingId }); });
    if (Bridge.available) Bridge.onReceive(onHostMessage);
    initialized = true;
  }

  // Called by the view toggle when the Drums tab is shown — lazily inits, then previews + lists.
  function show() {
    if (!initialized) init();
    requestPreview();
    requestList();
  }

  return { show };
})();
