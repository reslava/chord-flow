// ChordFlow Content view — the generic CRUD editor for DSL-backed entities.
//
// One component, parameterized by entity type (progression / song / rhythm /
// voicing), driving the generic `entity*` bridge protocol. Left: a source filter
// + a list of the chosen entity's definitions (with source badges — content-source-model).
// Right: a name field + DSL textarea with live parse + preview, and Save / Delete.
//
// Multi-source model: every source is shown (no hiding) — `user`, `automatic` (engine-derived), and each
// `package` by name. Package/automatic items are read-only; "Duplicate to user" forks an editable copy.
// The source filter is transient (resets when the view re-inits / the entity tab changes).
//
// The only per-entity divergence is the PREVIEW STRATEGY — the shared ChordFlowRenderSurface composite (ScoreR +
// ChordSheetR behind the Score⇄Sheet toggle + page transport) for progression/song, the same composite in
// score-only mode for rhythm (no sheet), and the shared ChordFlowFretboard SVG fret-box for voicing. Lazily
// initialized; the composite is the SAME one Practice mounts, so the two pages' render surface cannot drift.
"use strict";

window.ChordFlowContent = (function () {
  const Bridge = window.ChordFlowBridge;

  // The per-entity config — the table that makes one component serve all four.
  const ENTITIES = [
    {
      key: "progression", label: "Progressions", previewKind: "score", sheet: true, comping: true, tonality: true, metadata: true,
      placeholder: "17 47 17 57",
      help: "Nashville numbers. Space = next bar, _ = next chord in the bar (e.g. 1_4 5).",
    },
    {
      key: "song", label: "Songs", previewKind: "score", sheet: true, comping: true, metadata: true,
      placeholder: "intro = 17 47 17 17\nintro",
      help: "Define parts (NAME = inline | NAME: stored-id), then list them in order.",
    },
    {
      // Rhythm previews score-only — a bare rhythm on a single I chord has no meaningful chord sheet (C2).
      // No metadata block: rhythm patterns carry no catalog metadata (EX1).
      key: "rhythm", label: "Rhythms", previewKind: "score",
      placeholder: "X...X...X...X...",
      help: "X = attack, . = sustain the sounding note, - = rest, _ = tie. A note lasts its dots; X..... = dotted quarter. Leading :n sets the subdivision.",
    },
    {
      key: "voicing", label: "Voicings", previewKind: "diagram", metadata: true,
      placeholder: "voicing Cmaj shape:C root:5 frets: x 3 2 0 1 0",
      help: "Author once at the C anchor; the shape is movable to every key.",
    },
  ];

  const DEFAULT_COMPING = "beat_1_3"; // the app's default comping; the picker's transient default each load

  let initialized = false;
  let current = ENTITIES[0];   // selected entity config
  let editingId = null;        // user-row id to update on Save (null = create a new/forked definition)
  let selectedId = null;       // clicked list item (for highlight)
  let forkSourceId = null;     // id of the item the editor is showing → the server preserves its catalog header
                               // (genre/tags/tonality) on Save even when we fork to a new user copy (EX3)
  let items = [];              // last-rendered list items
  let filterR = null;          // shared FilterR handle (Source + Genre/Subgenre/Tags cascade); recreated per entity (transient)
  let selected = {};           // cascade selection per level ({ [key]: Set }); recomputed on each toggle (filter-ux-facets)
  let surfaceView = null;      // lazy ChordFlowRenderSurface handle (composite: ScoreR + ChordSheetR + Score⇄Sheet
                               // toggle + page transport) for the score strategy
  let surfaceHasSheet = false; // whether the live composite was built with a Sheet surface (progression/song); a
                               // switch to/from rhythm (score-only) recreates it (ensureSurface)
  let diagramView = null;      // lazy ChordFlowFretboard handle for the voicing fret-box strategy
  let debounceTimer = null;
  // The selected item's render-param seeds (scorer-render-params IN7): captured on entityLoaded and applied to
  // ScoreR — either now (if it exists) or right after its lazy creation. requestPreview also reads these as the
  // pre-ScoreR fallback so the FIRST preview already renders in the seeded key/tempo/feel (not the C/80/straight
  // default), which is what fixed the "preview always Straight" bug.
  let pendingSeeds = { key: 0, tempo: 80, feel: "None", keyIsMinor: false };

  // DOM refs (set in buildDom)
  let root, tabsEl, listEl, filterEl, countEl, nameEl, dslEl, helpEl, errorEl;
  let saveBtn, deleteBtn, duplicateBtn, newBtn, previewWrap, scoreSurfaceEl, transportEl, scoreEl, sheetEl, diagramEl, compingBar, compingEl;
  let tonalityRow, tonalityEl, metadataEl;
  let metaEditor = null;      // shared ChordFlowMetadataEditor handle (genre/subgenre/tags); one instance, gated per entity

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
          <h2>Definitions <span class="cc-count" id="ccCount"></span></h2>
          <div class="cc-filter" id="ccFilter"></div>
          <ul class="cc-list" id="ccList"></ul>
        </div>
        <div class="cc-editor">
          <div class="cc-row">
            <input type="text" id="ccName" placeholder="Name" />
          </div>
          <div class="cc-tonality" id="ccTonalityRow" hidden>
            <label for="ccTonality">Tonality</label>
            <select id="ccTonality">
              <option value="major">Major</option>
              <option value="minor">Minor</option>
            </select>
          </div>
          <div class="cc-metadata-editor" id="ccMetadata" hidden></div>
          <textarea id="ccDsl" class="dsl-input" spellcheck="false"></textarea>
          <div class="cc-help" id="ccHelp"></div>
          <div class="cc-error" id="ccError"></div>
          <div class="cc-actions">
            <button type="button" id="ccNew">+ New</button>
            <button type="button" id="ccSave" class="primary">Save</button>
            <button type="button" id="ccDuplicate" hidden>Duplicate to user</button>
            <button type="button" id="ccDelete">Delete</button>
          </div>
          <div class="cc-preview-toolbar" id="ccCompingBar" hidden>
            <label for="ccComping">Comping</label>
            <select id="ccComping"></select>
          </div>
          <div class="cc-preview empty" id="ccPreview">
            <div id="ccScoreSurface" hidden>
              <div id="ccPreviewTransport" class="cf-controls"></div>
              <div id="ccPreviewScore"></div>
              <div id="ccPreviewSheet" class="view-collapsed"></div>
            </div>
            <div id="ccPreviewDiagram" hidden></div>
          </div>
        </div>
      </div>`;

    tabsEl = document.getElementById("ccTabs");
    listEl = document.getElementById("ccList");
    filterEl = document.getElementById("ccFilter");
    countEl = document.getElementById("ccCount");
    nameEl = document.getElementById("ccName");
    dslEl = document.getElementById("ccDsl");
    helpEl = document.getElementById("ccHelp");
    errorEl = document.getElementById("ccError");
    saveBtn = document.getElementById("ccSave");
    deleteBtn = document.getElementById("ccDelete");
    duplicateBtn = document.getElementById("ccDuplicate");
    newBtn = document.getElementById("ccNew");
    previewWrap = document.getElementById("ccPreview");
    scoreSurfaceEl = document.getElementById("ccScoreSurface");
    transportEl = document.getElementById("ccPreviewTransport");
    scoreEl = document.getElementById("ccPreviewScore");
    sheetEl = document.getElementById("ccPreviewSheet");
    diagramEl = document.getElementById("ccPreviewDiagram");
    compingBar = document.getElementById("ccCompingBar");
    compingEl = document.getElementById("ccComping");
    tonalityRow = document.getElementById("ccTonalityRow");
    tonalityEl = document.getElementById("ccTonality");
    metadataEl = document.getElementById("ccMetadata");
    if (window.ChordFlowMetadataEditor) metaEditor = window.ChordFlowMetadataEditor.create(metadataEl);

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
    duplicateBtn.addEventListener("click", onDuplicate);
    newBtn.addEventListener("click", () => newItem());
    compingEl.addEventListener("change", requestPreview); // re-preview with the chosen comping
    tonalityEl.addEventListener("change", requestPreview); // a major↔minor flip re-realizes the preview live
  }

  function selectEntity(key) {
    current = ENTITIES.find((e) => e.key === key) || ENTITIES[0];
    for (const b of tabsEl.children) b.classList.toggle("active", b.dataset.entity === current.key);
    dslEl.placeholder = current.placeholder;
    helpEl.textContent = current.help;
    // The filter is transient per entity (content-source-model D5): drop the FilterR so it rebuilds all-on.
    if (filterR) { filterR.dispose(); filterR = null; }
    selected = {};
    // The comping picker is a progression/song-only content knob; fetch the rhythm catalog to fill it.
    compingBar.hidden = !current.comping;
    // The tonality control is a progression-only content property (a song's mode is its key/mod stream, EX4).
    tonalityRow.hidden = !current.tonality;
    // The metadata block is shown for the catalog entities (progression/song/voicing); hidden for rhythm (EX1).
    metadataEl.hidden = !current.metadata;
    if (current.comping) Bridge.send({ type: "entityList", entity: "rhythm" });
    newItem();
    requestList();
  }

  function requestList() {
    Bridge.send({ type: "entityList", entity: current.key });
  }

  // Reset the editor to a fresh, unsaved definition (a new user item).
  function newItem() {
    editingId = null;
    selectedId = null;
    forkSourceId = null;   // authored from scratch — no source header to inherit
    nameEl.value = "";
    if (tonalityEl) tonalityEl.value = "major"; // a new definition defaults to major
    if (metaEditor) metaEditor.clear();          // a new definition starts with no genre/subgenre/tags
    dslEl.value = "";
    clearError();
    clearPreview();
    setEditorMode(null);
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
    // The editor's authoritative metadata patch (progression/song/voicing); omitted for rhythm so the store
    // preserves as before (EX1). A present-but-empty field clears (IN9).
    const meta = current.metadata && metaEditor ? metaEditor.getValues() : null;
    Bridge.send({
      type: "entitySave",
      entity: current.key,
      entityId: editingId, // null = create (or fork a package item into a new user copy)
      sourceId: forkSourceId, // the shown item, so its catalog header (tonality/…) survives a fork (EX3)
      tonality: current.tonality ? tonalityEl.value : undefined, // the editor's explicit mode (progressions only)
      genre: meta ? meta.genre : undefined,
      subgenre: meta ? meta.subgenre : undefined,
      tags: meta ? meta.tags : undefined,
      name,
      dsl: dslEl.value,
    });
  }

  function onDelete() {
    if (editingId) Bridge.send({ type: "entityDelete", entity: current.key, entityId: editingId });
  }

  // Fork the currently-shown package/automatic item into an editable user copy: keep its name + DSL but
  // detach the id so the next Save mints a new user definition (content-source-model fork-on-edit).
  function onDuplicate() {
    editingId = null;
    selectedId = null;
    setEditorMode(null);
    highlightSelected();
    nameEl.focus();
    setStatus("editing a copy — Save to add it as your own");
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
    // Carry the preview component's current toggles + render params so the re-rendered score reflects them. Before
    // the render surface exists we fall back to the selected item's seeds (pendingSeeds) so even the FIRST preview
    // renders in the right key/tempo/feel (scorer-render-params IN7); renderOptions still omits ⇒ host defaults.
    const params = surfaceView ? surfaceView.getRenderParams() : null;
    const renderOptions = params ? params.renderOptions : undefined;
    const tripletFeel = params ? params.tripletFeel : pendingSeeds.feel;
    const keyPitchClass = params ? params.key : pendingSeeds.key;
    const tempo = params ? params.tempo : pendingSeeds.tempo;
    // Comping is a progression/song-only content knob; omitted elsewhere ⇒ host applies the beat_1_3 default.
    const compingPatternId = current.comping ? (compingEl.value || DEFAULT_COMPING) : undefined;
    // Tonality is a progression-only content property; the control drives the live preview (\ks major vs minor).
    const keyIsMinor = current.tonality ? tonalityEl.value === "minor" : undefined;
    Bridge.send({ type: "entityPreview", entity: current.key, dsl: dslEl.value, renderOptions, tripletFeel, keyPitchClass, keyIsMinor, tempo, compingPatternId });
  }

  // Fill the comping <select> from the rhythm catalog. Keep the current pick if it survived a catalog refresh,
  // else default to beat_1_3 (the transient default), else the first option.
  function populateCompingOptions(list) {
    const prev = compingEl.value;
    compingEl.innerHTML = "";
    for (const it of list) {
      const opt = document.createElement("option");
      opt.value = it.id;
      opt.textContent = it.name;
      compingEl.appendChild(opt);
    }
    const has = (id) => list.some((it) => it.id === id);
    compingEl.value = has(prev) ? prev : has(DEFAULT_COMPING) ? DEFAULT_COMPING : (list[0]?.id || "");
  }

  // --- inbound ---------------------------------------------------------------
  function onMessage(raw) {
    let msg;
    try {
      msg = JSON.parse(raw);
    } catch {
      return;
    }
    // The comping picker needs the rhythm catalog even while the active entity is progression/song — so capture
    // a rhythm entityList here, before the active-entity filter below would drop it. (Skip when rhythm is itself
    // the active entity; then it flows through the normal list path.)
    if (msg && msg.type === "entityList" && msg.entity === "rhythm" && current.key !== "rhythm") {
      populateCompingOptions(msg.items || []);
      return;
    }
    // Every view sees every message; ignore anything not for the active entity / not ours.
    if (!msg || msg.entity !== current.key) return;

    switch (msg.type) {
      case "entityList":
        renderList(msg.items || []);
        break;
      case "entityLoaded": {
        selectedId = msg.id;
        forkSourceId = msg.id; // the loaded item is the header source, whether we edit it or fork from it
        nameEl.value = msg.name || "";
        dslEl.value = msg.dsl || "";
        clearError();
        const it = items.find((i) => i.id === msg.id);
        const src = it ? it.source : "user";
        // A user item edits in place; a package/automatic item opens read-only (Duplicate to user to fork).
        editingId = src === "user" ? msg.id : null;
        setEditorMode(src === "user" ? "user" : src);
        highlightSelected();
        // Capture the item's render-param seeds (song → its InitialKey/DefaultTempo/DefaultFeel; a key/feel-
        // independent progression/rhythm → C/80/None) and apply them to ScoreR before previewing (IN7). This is
        // what fixes the preview rendering "always Straight" (feel never seeded) and never-seeded key/tempo.
        pendingSeeds = {
          key: it && it.initialKey != null ? it.initialKey : 0,
          tempo: it && it.defaultTempo != null ? it.defaultTempo : 80,
          feel: it && it.defaultFeel != null ? it.defaultFeel : "None",
          keyIsMinor: !!(it && it.initialKeyIsMinor),
        };
        // Seed the tonality control from the content's own mode (a manual flip afterward still wins).
        if (tonalityEl) tonalityEl.value = pendingSeeds.keyIsMinor ? "minor" : "major";
        // Seed the metadata controls from the clicked list row (no load-path change — the row already carries
        // genre/subgenre/tags; IN7). A manual edit afterward still wins.
        if (metaEditor) metaEditor.seed(it ? { genre: it.genre, subgenre: it.subgenre, tags: it.tags } : null);
        applySeeds();
        requestPreview();
        break;
      }
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
        selectedId = msg.id;
        forkSourceId = msg.id; // the saved user row is now the header source for further in-place edits
        setEditorMode("user");
        setStatus("saved ✓");
        requestList();
        break;
      case "entityDeleted":
        setStatus("deleted ✓");
        newItem();
        requestList();
        break;
    }
  }

  // --- list + FilterR (Source + Genre/Subgenre/Tags) -------------------------
  // A filter token identifies a source: a pack is keyed by its name, user/automatic by kind. This is also the
  // per-item value the Source level filters on (valuesFor).
  function filterKey(it) {
    return it.source === "package" ? "pack:" + (it.packName || "") : it.source;
  }

  function sourceLabel(it) {
    if (it.source === "package") return it.packName || "Package";
    if (it.source === "automatic") return "Automatic";
    return "User";
  }

  function keyLabel(key) {
    if (key === "user") return "User";
    if (key === "automatic") return "Automatic";
    return key.slice("pack:".length) || "Package";
  }

  // Ordered cascade level defs (filter-ux-facets): Source heads the chain, then the catalog facets. Rhythm rows
  // carry no genre/subgenre/tags, so those levels come out empty (no chips ⇒ FilterR renders nothing for them).
  const CONTENT_LEVELS = [
    { key: "source", label: "Source", tokenLabel: keyLabel, values: (it) => [filterKey(it)] },
    { key: "genre", label: "Genre", values: (it) => (it.genre ? [it.genre] : []) },
    { key: "subgenre", label: "Subgenre", values: (it) => (it.subgenre ? [it.subgenre] : []) },
    { key: "tags", label: "Tags", values: (it) => it.tags || [] },
  ];

  function renderList(list) {
    items = list;
    // Refresh the metadata editor's datalist suggestions from the current rows (client-side discovery — IN4).
    if (metaEditor) metaEditor.setSuggestions(items);
    selected = window.ChordFlowFilterCascade.initialSelected(items, CONTENT_LEVELS); // all-on
    rebuildFilter();
  }

  // Build the cascade from the current selection, (re)mount FilterR with the computed chips (counts + greyed
  // unavailable + selection), and render the filtered rows + the "N of M" total. Client-side, no round-trip (C2).
  function rebuildFilter() {
    const built = window.ChordFlowFilterCascade.build(items, CONTENT_LEVELS, selected);
    if (!filterR) {
      filterR = window.ChordFlowFilter.create(filterEl, { levels: built.levels, onChange: onFilterChange });
    } else {
      filterR.setLevels(built.levels);
    }
    renderRows(built.filtered);
    renderCount(built.total, items.length);
  }

  // A chip toggled: adopt the changed level's selection, reset the levels below it to all-available (the cascade
  // reset model — C3/C4), then rebuild.
  function onFilterChange(state, changedKey) {
    selected[changedKey] = state[changedKey];
    selected = window.ChordFlowFilterCascade.resetBelow(items, CONTENT_LEVELS, selected, changedKey);
    rebuildFilter();
  }

  function renderCount(shown, total) {
    if (!countEl) return;
    countEl.textContent = total === 0 ? "" : (shown === total ? "(" + total + ")" : shown + " of " + total);
  }

  function renderRows(visible) {
    listEl.innerHTML = "";
    if (visible.length === 0) {
      const li = document.createElement("li");
      li.className = "empty";
      li.textContent = items.length === 0 ? "No definitions yet" : "No definitions match the filter";
      listEl.appendChild(li);
      return;
    }

    for (const it of visible) {
      const li = document.createElement("li");
      li.dataset.id = it.id;
      // Top row: name + source badge.
      const top = document.createElement("div");
      top.className = "cc-row-top";
      const name = document.createElement("span");
      name.textContent = it.name;
      const badge = document.createElement("span");
      badge.className = "cc-badge " + it.source; // cc-badge user|package|automatic
      badge.textContent = sourceLabel(it);
      top.appendChild(name);
      top.appendChild(badge);
      li.appendChild(top);
      // Meta row: genre · subgenre + tag pills (omitted for rows with no catalog metadata, e.g. rhythms).
      const metaEl = renderMeta(it);
      if (metaEl) li.appendChild(metaEl);
      li.addEventListener("click", () => loadItem(it.id));
      listEl.appendChild(li);
    }
    highlightSelected();
  }

  // The per-row catalog-metadata line: "Genre · Subgenre" then tag pills. Returns null when the row carries none.
  function renderMeta(it) {
    const gs = [it.genre, it.subgenre].filter((v) => v != null && v !== "");
    const tags = it.tags || [];
    if (gs.length === 0 && tags.length === 0) return null;
    const meta = document.createElement("div");
    meta.className = "cc-meta";
    if (gs.length) {
      const label = document.createElement("span");
      label.className = "cc-meta-genre";
      label.textContent = gs.join(" · ");
      meta.appendChild(label);
    }
    for (const t of tags) {
      const tag = document.createElement("span");
      tag.className = "cc-tag";
      tag.textContent = t;
      meta.appendChild(tag);
    }
    return meta;
  }

  function highlightSelected() {
    for (const li of listEl.children) {
      if (li.dataset) li.classList.toggle("active", li.dataset.id === selectedId);
    }
  }

  // Editor affordances by source (content-source-model): user (or a new/forked item) is editable + deletable;
  // a package/automatic item is read-only with "Duplicate to user".
  function setEditorMode(source) {
    const editable = source === "user" || source == null;
    nameEl.disabled = !editable;
    dslEl.readOnly = !editable;
    if (metaEditor) metaEditor.setEnabled(editable); // a package/automatic item shows its metadata read-only
    saveBtn.hidden = !editable;
    duplicateBtn.hidden = editable;
    deleteBtn.hidden = !editable;
    deleteBtn.disabled = !(editable && editingId);
    deleteBtn.textContent = "Delete";
  }

  // --- preview strategies ----------------------------------------------------
  function renderPreview(msg) {
    previewWrap.classList.remove("empty");
    if (msg.kind === "diagram") {
      scoreSurfaceEl.hidden = true;   // hide the whole score surface (transport + score + sheet)
      diagramEl.hidden = false;
      if (window.ChordFlowFretboard && msg.diagram) {
        // Voicings default to a vertical chord-box with an auto-fit window — hide the fret-window control, but
        // expose the orientation toggle so the user can flip to the horizontal neck.
        if (!diagramView) diagramView = window.ChordFlowFretboard.create(diagramEl, {
          labelMode: "interval", controls: { fretWindow: false },
        });
        diagramView.render(msg.diagram);
      } else {
        diagramEl.textContent = "";
      }
      return;
    }
    // score (progression/song = with Sheet behind the toggle; rhythm = score-only)
    diagramEl.hidden = true;
    scoreSurfaceEl.hidden = false;
    renderScore(msg);
  }

  // Push the captured item seeds onto the composite's ScoreR (no-op until it exists — ensureSurface re-applies on
  // lazy creation). Seeds only, never a re-render: the score the host already sent is in the seeded key/tempo/feel.
  function applySeeds() {
    if (!surfaceView) return;
    surfaceView.seedKey(pendingSeeds.key);
    surfaceView.seedTempo(pendingSeeds.tempo);
    surfaceView.seedTripletFeel(pendingSeeds.feel);
  }

  // Create the shared render-surface composite lazily, or recreate it when the needed Sheet mode changes
  // (progression/song = with Sheet + toggle; rhythm = score-only). Content keeps Key/Feel pickers ON ScoreR
  // (there's no HarmonyControlsR here, C3); the page transport rides inside the composite.
  function ensureSurface() {
    const needSheet = !!current.sheet;
    if (surfaceView && surfaceHasSheet !== needSheet) {
      surfaceView.dispose();       // self-cleans its toggle + transport + surfaces
      surfaceView = null;
    }
    if (!surfaceView) {
      surfaceView = window.ChordFlowRenderSurface.create({
        transportEl, scoreEl, sheetEl,
        sheet: needSheet,
        scoreOpts: {
          player: true,
          controls: "full",
          debugPanel: true,   // the alphaTex scratchpad is available on every score-rendering page
          tripletFeel: true,  // preview progression/song/rhythm with a chosen swing (carried on entityPreview)
          key: true,          // Key/Tempo/Feel are seeded per content + live like Practice (scorer-render-params IN7)
        },
        onNeedsRerender: () => requestPreview(),
      });
      surfaceHasSheet = needSheet;
      applySeeds();   // the surface was just created — reflect the selected item's seeds on its controls
    }
  }

  // Feed both projections of the entityPreview reply to the composite (IN2/IN4): ScoreR loads the tex, the Sheet
  // surface (progression/song) renders the sheet model + its marker schedule. The host's rendered tempo re-bases
  // the engine so playback matches the alphaTex \tempo (twin of the Practice loadScore path).
  function renderScore(msg) {
    if (!msg.tex || !window.ChordFlowRenderSurface) return;
    ensureSurface();
    surfaceView.load({
      tex: msg.tex, tempo: msg.tempo,
      sheet: msg.sheet, cellSchedule: msg.cellSchedule,
      name: nameEl.value.trim() || current.label, // the Sheet export filename base
    });
  }

  function clearPreview() {
    previewWrap.classList.add("empty");
    scoreSurfaceEl.hidden = true;
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
