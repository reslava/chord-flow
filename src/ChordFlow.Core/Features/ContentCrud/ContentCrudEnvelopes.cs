using ChordFlow.Domain;

namespace ChordFlow.Features.ContentCrud;

/// <summary>
/// Outbound bridge envelopes (C#→JS) for the generic content-CRUD surface. Every one carries the
/// <c>entity</c> discriminator so the single JS editor knows which entity a reply concerns (design §4). The
/// real payloads are DSL strings and the preview render — never parsed domain objects. Each <c>Type</c>
/// defaults to the wire string the JS side switches on, mirroring the existing envelope convention
/// (<c>loadScore</c>, <c>exerciseList</c>, …).
/// </summary>

/// <summary>A list row: id, display name, the resolved winning origin, and whether a lower tier exists under it
/// (so the UI labels the destructive action "Delete" vs "Revert to default" — IN13).</summary>
public sealed record ContentItem(string Id, string Name, string Origin, bool HasLowerTier);

/// <summary>The definitions of one entity type, for the list pane: <c>{"type":"entityList","entity":"…","items":[…]}</c>.</summary>
public sealed record EntityListEnvelope(string Entity, IReadOnlyList<ContentItem> Items, string Type = "entityList");

/// <summary>One definition opened for editing: <c>{"type":"entityLoaded","entity":"…","id":"…","name":"…","dsl":"…"}</c>.</summary>
public sealed record EntityLoadedEnvelope(string Entity, string Id, string Name, string Dsl, string Type = "entityLoaded");

/// <summary>
/// A live preview of an unsaved DSL. <see cref="Kind"/> is <c>score</c> (progression/song/rhythm — carries
/// <see cref="Tex"/> + <see cref="Tempo"/> for a small alphaTab render) or <c>diagram</c> (voicing — the
/// <see cref="Diagram"/> <see cref="FretboardDiagram"/> marker model the JS fret-box draws). Strategy-shaped so
/// the one editor switches on it.
/// </summary>
public sealed record EntityPreviewEnvelope(
    string Entity,
    string Kind,
    string? Tex = null,
    int? Tempo = null,
    FretboardDiagram? Diagram = null,
    string Type = "entityPreview");

/// <summary>An invalid DSL: the parser's located message, shown inline (IN3). <c>{"type":"entityParseError","entity":"…","message":"…"}</c>.</summary>
public sealed record EntityParseErrorEnvelope(string Entity, string Message, string Type = "entityParseError");

/// <summary>A save succeeded; carries the (possibly newly minted) id so the editor can track it. <c>{"type":"entitySaved","entity":"…","id":"…"}</c>.</summary>
public sealed record EntitySavedEnvelope(string Entity, string Id, string Type = "entitySaved");

/// <summary>A delete completed; <see cref="Outcome"/> is <c>Deleted</c>/<c>Reverted</c>/<c>NotFound</c>. <c>{"type":"entityDeleted","entity":"…","id":"…","outcome":"…"}</c>.</summary>
public sealed record EntityDeletedEnvelope(string Entity, string Id, string Outcome, string Type = "entityDeleted");
