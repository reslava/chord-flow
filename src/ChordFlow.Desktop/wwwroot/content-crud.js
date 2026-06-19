// ChordFlow Content view — the generic CRUD editor for DSL-backed entities.
//
// One component, parameterized by entity type (progression / song / rhythm /
// voicing), driving the generic `entity*` bridge protocol. Left: a list of the
// chosen entity's definitions (with origin badges). Right: a name field + DSL
// textarea with live parse + preview, and Save / Delete. The only per-entity
// divergence is the PREVIEW STRATEGY — the shared ChordFlowScore render component (full player + toggles)
// for progression/song/rhythm, the shared ChordFlowFretboard SVG fret-box (window.ChordFlowFretboard,
// fretboard-render-component.js) for voicing. Lazily initialized the first time the Content view is shown.
"use strict";

window.ChordFlowContent = (function () {
  const Bridge = window.ChordFlowBridge;

  // The per-entity config — the table that makes one component serve all four.
  const ENTITIES = [
    {
      key: "progression", label: "Progressions", previewKind: "score",
      placeholder: "17 47 17 57",
      help: "Nashville numbers. Space = next bar, _ = next chord in the bar (e.g. 1_4 5).",
    },
    {
      key: "song", label: "Songs", previewKind: "score",
      placeholder: "intro = 17 47 17 17\nintro",
      help: "Define parts (NAME = inline | NAME: stored-id), then list them in order.",
    },
    {
      key: "rhythm", label: "Rhythms", previewKind: "score",
      placeholder: "X...X...X...X...",
      help: "X = attack, . = sustain, - = rest. Leading :n sets the subdivision.",
    },
    {
      key: "voicing", label: "Voicings", previewKind: "diagram",
      placeholder: "voicing Cmaj shape:C root:5 frets: x 3 2 0 1 0",
      help: "Author once at the C anchor; the shape is movable to every key.",
    },
  ];

  let initialized = false;
  let current = ENTITIES[0];   // selected entity config
  let editingId = null;        // id being edited (null = a new, unsaved definition)
  let items = [];              // last-rendered list summaries, for Delete/Revert labeling
  let scoreView = null;        // lazy ChordFlowScore handle (full player + toggles) for the score strategy
  let diagramView = null;      // lazy ChordFlowFretboard handle for the voicing fret-box strategy
  let debounceTimer = null;

  // DOM refs (set in buildDom)
  let root, tabsEl, listEl, nameEl, dslEl, helpEl, errorEl;
  let saveBtn, deleteBtn, newBtn, previewWrap, scoreEl, diagramEl;

  const setStatus = (text) => {
    const el = document.getElementById("status");
    if (el) el.textContent = text;
  };

  function show() {
    if (!initialized) init();
  }

  function init() {
    initialized = true;
    buildDom();
    Bridge.onReceive(onMessage);
    selectEntity(current.key);
  }

  function buildDom() {
    root = document.getElementById("content-view");
    root.innerHTML = `
      <div class="cc-tabs" id="ccTabs"></div>
      <div class="cc-body">
        <div class="cc-list-pane">
          <h2>Definitions</h2>
          <ul class="cc-list" id="ccList"></ul>
        </div>
        <div class="cc-editor">
          <div class="cc-row">
            <input type="text" id="ccName" placeholder="Name" />
          </div>
          <textarea id="ccDsl" spellcheck="false"></textarea>
          <div class="cc-help" id="ccHelp"></div>
          <div class="cc-error" id="ccError"></div>
          <div class="cc-actions">
            <button type="button" id="ccNew">+ New</button>
            <button type="button" id="ccSave" class="primary">Save</button>
            <button type="button" id="ccDelete">Delete</button>
          </div>
          <div class="cc-preview empty" id="ccPreview">
            <div id="ccPreviewScore"></div>
            <div id="ccPreviewDiagram"></div>
          </div>
        </div>
      </div>`;

    tabsEl = document.getElementById("ccTabs");
    listEl = document.getElementById("ccList");
    nameEl = document.getElementById("ccName");
    dslEl = document.getElementById("ccDsl");
    helpEl = document.getElementById("ccHelp");
    errorEl = document.getElementById("ccError");
    saveBtn = document.getElementById("ccSave");
    deleteBtn = document.getElementById("ccDelete");
    newBtn = document.getElementById("ccNew");
    previewWrap = document.getElementById("ccPreview");
    scoreEl = document.getElementById("ccPreviewScore");
    diagramEl = document.getElementById("ccPreviewDiagram");

    // Entity tabs
    for (const e of ENTITIES) {
      const b = document.createElement("button");
      b.textContent = e.label;
      b.dataset.entity = e.key;
      b.addEventListener("click", () => selectEntity(e.key));
      tabsEl.appendChild(b);
    }

    dslEl.addEventListener("input", onDslInput);
    saveBtn.addEventListener("click", onSave);
    deleteBtn.addEventListener("click", onDelete);
    newBtn.addEventListener("click", () => newItem());
  }

  function selectEntity(key) {
    current = ENTITIES.find((e) => e.key === key) || ENTITIES[0];
    for (const b of tabsEl.children) b.classList.toggle("active", b.dataset.entity === current.key);
    dslEl.placeholder = current.placeholder;
    helpEl.textContent = current.help;
    newItem();
    requestList();
  }

  function requestList() {
    Bridge.send({ type: "entityList", entity: current.key });
  }

  // Reset the editor to a fresh, unsaved definition.
  function newItem() {
    editingId = null;
    nameEl.value = "";
    dslEl.value = "";
    clearError();
    clearPreview();
    refreshDeleteButton();
    highlightSelected();
  }

  function loadItem(id) {
    Bridge.send({ type: "entityGet", entity: current.key, entityId: id });
  }

  function onSave() {
    const name = nameEl.value.trim();
    if (!name) {
      showError("Give the definition a name.");
      return;
    }
    Bridge.send({
      type: "entitySave",
      entity: current.key,
      entityId: editingId, // null = create
      name,
      dsl: dslEl.value,
    });
  }

  function onDelete() {
    if (editingId) Bridge.send({ type: "entityDelete", entity: current.key, entityId: editingId });
  }

  function onDslInput() {
    clearTimeout(debounceTimer);
    debounceTimer = setTimeout(requestPreview, 300);
  }

  function requestPreview() {
    const dsl = dslEl.value.trim();
    if (!dsl) {
      clearError();
      clearPreview();
      return;
    }
    // Carry the preview component's current toggles so the re-rendered score reflects them (undefined
    // before the score component exists ⇒ omitted ⇒ host defaults).
    const renderOptions = scoreView ? scoreView.getRenderOptions() : undefined;
    Bridge.send({ type: "entityPreview", entity: current.key, dsl: dslEl.value, renderOptions });
  }

  // --- inbound ---------------------------------------------------------------
  function onMessage(raw) {
    let msg;
    try {
      msg = JSON.parse(raw);
    } catch {
      return;
    }
    // Every view sees every message; ignore anything not for the active entity / not ours.
    if (!msg || msg.entity !== current.key) return;

    switch (msg.type) {
      case "entityList":
        renderList(msg.items || []);
        break;
      case "entityLoaded":
        editingId = msg.id;
        nameEl.value = msg.name || "";
        dslEl.value = msg.dsl || "";
        clearError();
        refreshDeleteButton();
        highlightSelected();
        requestPreview();
        break;
      case "entityPreview":
        clearError();
        renderPreview(msg);
        break;
      case "entityParseError":
        showError(msg.message || "Invalid definition.");
        clearPreview();
        break;
      case "entitySaved":
        editingId = msg.id;
        setStatus("saved ✓");
        requestList();
        break;
      case "entityDeleted":
        setStatus(msg.outcome === "Reverted" ? "reverted to default ✓" : "deleted ✓");
        newItem();
        requestList();
        break;
    }
  }

  function renderList(list) {
    items = list;
    listEl.innerHTML = "";
    if (list.length === 0) {
      const li = document.createElement("li");
      li.className = "empty";
      li.textContent = "No definitions yet";
      listEl.appendChild(li);
      refreshDeleteButton();
      return;
    }

    for (const it of list) {
      const li = document.createElement("li");
      li.dataset.id = it.id;
      const name = document.createElement("span");
      name.textContent = it.name;
      const badge = document.createElement("span");
      badge.className = "cc-badge" + (it.origin === "UserDefined" ? " user" : "");
      badge.textContent = it.origin === "UserDefined" ? "User" : it.origin;
      li.appendChild(name);
      li.appendChild(badge);
      li.addEventListener("click", () => loadItem(it.id));
      listEl.appendChild(li);
    }
    highlightSelected();
    refreshDeleteButton();
  }

  function highlightSelected() {
    for (const li of listEl.children) {
      if (li.dataset) li.classList.toggle("active", li.dataset.id === editingId);
    }
  }

  // Delete vs Revert vs disabled, per the selected item's origin/lower tier (IN13).
  function refreshDeleteButton() {
    const summary = items.find((i) => i.id === editingId);
    if (!editingId || !summary || summary.origin !== "UserDefined") {
      deleteBtn.disabled = true;
      deleteBtn.textContent = "Delete";
      return;
    }
    deleteBtn.disabled = false;
    deleteBtn.textContent = summary.hasLowerTier ? "Revert to default" : "Delete";
  }

  // --- preview strategies ----------------------------------------------------
  function renderPreview(msg) {
    previewWrap.classList.remove("empty");
    if (msg.kind === "diagram") {
      scoreEl.hidden = true;
      diagramEl.hidden = false;
      if (window.ChordFlowFretboard && msg.diagram) {
        // Voicings are vertical chord-boxes with an auto-fit window — hide the orientation + fret-window controls.
        if (!diagramView) diagramView = window.ChordFlowFretboard.create(diagramEl, {
          labelMode: "interval", controls: { orientation: false, fretWindow: false },
        });
        diagramView.render(msg.diagram);
      } else {
        diagramEl.textContent = "";
      }
      return;
    }
    // score
    diagramEl.hidden = true;
    scoreEl.hidden = false;
    renderScore(msg.tex);
  }

  // Reuse the shared render component (full player + toggles) so progression/song/rhythm previews get the
  // same transport + metronome/count-in/chord-name/diagram options as Practice, off one alphaTab
  // integration. A content-toggle change re-requests the preview with the new renderOptions.
  function renderScore(tex) {
    if (!tex || !window.ChordFlowScore) return;
    if (!scoreView) {
      scoreView = window.ChordFlowScore.create(scoreEl, {
        player: true,
        controls: "full",
        onNeedsRerender: () => requestPreview(),
      });
    }
    scoreView.load(tex);
  }

  function clearPreview() {
    previewWrap.classList.add("empty");
    scoreEl.hidden = true;
    diagramEl.hidden = true;
    diagramEl.textContent = "";
  }

  function showError(text) {
    errorEl.textContent = text;
  }

  function clearError() {
    errorEl.textContent = "";
  }

  return { show };
})();
