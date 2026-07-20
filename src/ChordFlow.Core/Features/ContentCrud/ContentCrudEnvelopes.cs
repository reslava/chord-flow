
using ChordFlow.Instruments.Guitar;
using ChordFlow.Rendering.ChordSheets;

namespace ChordFlow.Features.ContentCrud;

/// <summary>
/// Outbound bridge envelopes (C#→JS) for the generic content-CRUD surface. Every one carries the
/// <c>entity</c> discriminator so the single JS editor knows which entity a reply concerns (design §4). The
/// real payloads are DSL strings and the preview render — never parsed domain objects. Each <c>Type</c>
/// defaults to the wire string the JS side switches on, mirroring the existing envelope convention
/// (<c>loadScore</c>, <c>exerciseList</c>, …).
/// </summary>

/// <summary>A list row: id, display name, its <see cref="Source"/> (<c>"package"</c> / <c>"user"</c> /
/// <c>"automatic"</c>), and — when <see cref="Source"/> is <c>"package"</c> — the source pack's display
/// <see cref="PackName"/> (else null). The UI tags + filters on source/packName (content-source-model).
/// <see cref="InitialKey"/> is the song's starting-key tonic pitch class (0..11) for seeding the Practice key
/// picker (play-ui-key-init IN1); null for the key-independent entities. <see cref="DefaultFeel"/> is the
/// song's feel ident ("None"/"Triplet8th"/"Triplet16th") for seeding the feel control (song-default-feel IN4);
/// null when the song declares no feel or for the feel-independent entities. <see cref="DefaultTempo"/> is the
/// song's <see cref="Song.DefaultTempo"/> (BPM) for seeding the tempo control (scorer-render-params IN1); null
/// when the song declares no tempo or for the other entities.</summary>
public sealed record ContentItem(
    string Id, string Name, string Source, string? PackName, int? InitialKey = null, string? DefaultFeel = null,
    int? DefaultTempo = null, bool? InitialKeyIsMinor = null,
    // Catalog metadata for the list fields + the shared FilterR (filter-toggle-buttons IN2); empty for rhythm patterns (EX3).
    string? Genre = null, string? Subgenre = null, IReadOnlyList<string>? Tags = null);

/// <summary>The definitions of one entity type, for the list pane: <c>{"type":"entityList","entity":"…","items":[…]}</c>.</summary>
public sealed record EntityListEnvelope(string Entity, IReadOnlyList<ContentItem> Items, string Type = "entityList");

/// <summary>One definition opened for editing: <c>{"type":"entityLoaded","entity":"…","id":"…","name":"…","dsl":"…"}</c>.</summary>
public sealed record EntityLoadedEnvelope(string Entity, string Id, string Name, string Dsl, string Type = "entityLoaded");

/// <summary>
/// A live preview of an unsaved DSL. <see cref="Kind"/> is <c>score</c> (progression/song/rhythm — carries
/// <see cref="Tex"/> + <see cref="Tempo"/> for a small alphaTab render) or <c>diagram</c> (voicing — the
/// <see cref="Diagram"/> <see cref="FretboardDiagram"/> marker model the JS fret-box draws). Strategy-shaped so
/// the one editor switches on it.
/// <para>
/// For a progression/song score preview it additionally carries the chord-sheet projection of the SAME
/// realized-song pass (content-shared-render-surfaces IN4/IN5): the <see cref="Sheet"/> model + its playback
/// <see cref="CellSchedule"/>, mirroring the unified <c>loadScore</c> reply, so the Content preview can mount
/// the shared render-surface composite (Score⇄Sheet) and its score + sheet cannot drift. Both are null for a
/// rhythm preview (score-only — a bare rhythm has no meaningful sheet) and for a voicing (diagram kind).
/// </para>
/// </summary>
public sealed record EntityPreviewEnvelope(
    string Entity,
    string Kind,
    string? Tex = null,
    int? Tempo = null,
    FretboardDiagram? Diagram = null,
    ChordSheet? Sheet = null,
    IReadOnlyList<CellScheduleEntry>? CellSchedule = null,
    string Type = "entityPreview");

/// <summary>An invalid DSL: the parser's located message, shown inline (IN3). <c>{"type":"entityParseError","entity":"…","message":"…"}</c>.</summary>
public sealed record EntityParseErrorEnvelope(string Entity, string Message, string Type = "entityParseError");

/// <summary>A save succeeded; carries the (possibly newly minted) id so the editor can track it. <c>{"type":"entitySaved","entity":"…","id":"…"}</c>.</summary>
public sealed record EntitySavedEnvelope(string Entity, string Id, string Type = "entitySaved");

/// <summary>A delete completed; <see cref="Outcome"/> is <c>Deleted</c>/<c>Reverted</c>/<c>NotFound</c>. <c>{"type":"entityDeleted","entity":"…","id":"…","outcome":"…"}</c>.</summary>
public sealed record EntityDeletedEnvelope(string Entity, string Id, string Outcome, string Type = "entityDeleted");
