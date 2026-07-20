// ChordFlow Metadata Editor — a dumb, reusable control for authoring catalog metadata (content-metadata-editing).
//
// The editing twin of the read-only per-row meta line + FilterR: genre / subgenre as <input>+<datalist>
// (pick a value already in use, or type a new one) and a tags pill editor (add from the datalist or type;
// remove each individually). Like FretR/FilterR it owns ONLY its DOM + local state — no data source, no
// persistence, no music theory. The consumer:
//   - feeds suggestion values discovered from its own list rows via setSuggestions(items)  (client-side, no round-trip — IN4)
//   - seeds the controls from the clicked row via seed({genre, subgenre, tags})            (IN7)
//   - reads the authoritative patch on save via getValues() -> {genre, subgenre, tags}     (IN5)
// Two consumers mount it today: the Content editor (Progression/Song/Voicing) and the Drums page (Drums).
"use strict";

window.ChordFlowMetadataEditor = (function () {
  // Distinct, sorted, non-empty values discovered across the list rows for one facet.
  function distinct(items, pick) {
    const set = new Set();
    for (const it of items || []) {
      for (const v of pick(it) || []) {
        if (v != null && v !== "") set.add(v);
      }
    }
    return [...set].sort((a, b) => a.localeCompare(b));
  }

  function fillDatalist(el, values) {
    el.innerHTML = "";
    for (const v of values) {
      const o = document.createElement("option");
      o.value = v;
      el.appendChild(o);
    }
  }

  function create(container) {
    const base = "cfme-" + Math.random().toString(36).slice(2, 8); // unique datalist ids per instance
    container.classList.add("cf-metadata");
    container.innerHTML = `
      <div class="cf-metadata-field">
        <label>Genre</label>
        <input type="text" data-role="genre" list="${base}-genres" autocomplete="off" placeholder="e.g. Blues" />
        <datalist id="${base}-genres"></datalist>
      </div>
      <div class="cf-metadata-field">
        <label>Subgenre</label>
        <input type="text" data-role="subgenre" list="${base}-subs" autocomplete="off" placeholder="e.g. Shuffle" />
        <datalist id="${base}-subs"></datalist>
      </div>
      <div class="cf-metadata-field cf-metadata-tags">
        <label>Tags</label>
        <div class="cf-tag-pills" data-role="pills"></div>
        <input type="text" data-role="tag" list="${base}-tags" autocomplete="off" placeholder="add tag…" />
        <datalist id="${base}-tags"></datalist>
      </div>`;

    const genreEl = container.querySelector('[data-role="genre"]');
    const subgenreEl = container.querySelector('[data-role="subgenre"]');
    const tagInputEl = container.querySelector('[data-role="tag"]');
    const pillsEl = container.querySelector('[data-role="pills"]');
    const genreList = container.querySelector("#" + base + "-genres");
    const subList = container.querySelector("#" + base + "-subs");
    const tagList = container.querySelector("#" + base + "-tags");

    let tags = [];

    function renderPills() {
      pillsEl.innerHTML = "";
      tags.forEach((t, i) => {
        const pill = document.createElement("span");
        pill.className = "cf-tag-pill";
        pill.textContent = t;
        const x = document.createElement("button");
        x.type = "button";
        x.className = "cf-tag-remove";
        x.textContent = "×";
        x.setAttribute("aria-label", "Remove tag " + t);
        x.addEventListener("click", () => { tags.splice(i, 1); renderPills(); });
        pill.appendChild(x);
        pillsEl.appendChild(pill);
      });
    }

    // Commit a tag (from typing + Enter/comma, picking a datalist option, or blur). Case-insensitive de-dupe.
    function addTag(raw) {
      const v = (raw || "").trim();
      if (v && !tags.some((t) => t.toLowerCase() === v.toLowerCase())) {
        tags.push(v);
        renderPills();
      }
      tagInputEl.value = "";
    }

    tagInputEl.addEventListener("keydown", (e) => {
      if (e.key === "Enter" || e.key === ",") {
        e.preventDefault();
        addTag(tagInputEl.value);
      } else if (e.key === "Backspace" && tagInputEl.value === "" && tags.length) {
        tags.pop();
        renderPills();
      }
    });
    tagInputEl.addEventListener("change", () => addTag(tagInputEl.value)); // picking a datalist option commits
    tagInputEl.addEventListener("blur", () => addTag(tagInputEl.value));

    return {
      // Discover the datalist suggestions from the current list rows (IN4) — client-side, no round-trip.
      setSuggestions(items) {
        fillDatalist(genreList, distinct(items, (it) => (it.genre ? [it.genre] : [])));
        fillDatalist(subList, distinct(items, (it) => (it.subgenre ? [it.subgenre] : [])));
        fillDatalist(tagList, distinct(items, (it) => it.tags || []));
      },
      // Seed the controls from the clicked row (IN7); no arg / null clears them (a new definition).
      seed(meta) {
        meta = meta || {};
        genreEl.value = meta.genre || "";
        subgenreEl.value = meta.subgenre || "";
        tags = Array.isArray(meta.tags) ? meta.tags.slice() : [];
        tagInputEl.value = "";
        renderPills();
      },
      clear() { this.seed(null); },
      // The authoritative patch to send on save (IN5). Commits any half-typed tag first. Empty strings / an
      // empty array are intentional: the store treats a present-but-empty field as a CLEAR (IN9).
      getValues() {
        addTag(tagInputEl.value);
        return { genre: genreEl.value.trim(), subgenre: subgenreEl.value.trim(), tags: tags.slice() };
      },
      // Read-only mode for a package/automatic item (mirrors the editor's name/dsl disabling).
      setEnabled(on) {
        genreEl.disabled = !on;
        subgenreEl.disabled = !on;
        tagInputEl.disabled = !on;
        container.classList.toggle("cf-metadata-disabled", !on);
      },
    };
  }

  return { create };
})();
