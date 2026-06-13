namespace ChordFlow.Persistence.Entities;

/// <summary>
/// Persisted authored-voicing <b>definition</b> — the canonical-C Voicing-DSL <see cref="Dsl"/> string is
/// the single serialization (constraint C3: the parsed <c>VoicingShape</c>/realized frets are never stored,
/// they are regenerated on load via <c>VoicingDslParser.Parse(Dsl)</c>). Mirrors <see cref="ProgressionEntity"/>:
/// stable string PK, <see cref="Origin"/> provenance, and the catalog filter columns
/// (<see cref="Genre"/>/<see cref="Subgenre"/>/<see cref="Tags"/>) so voicing packs are genre-filterable like
/// the other content pillars. The stored <see cref="Dsl"/> is always the canonical-C form (any authoring
/// anchor is normalized before save — IN2), so the same shape never appears twice under different roots.
/// </summary>
public sealed class VoicingEntity : IOriginated
{
    /// <summary>Stable id and primary key. Human slug for built-ins, GUID for user voicings.</summary>
    public string Id { get; set; } = "";

    /// <summary>Display name (e.g. <c>Open C</c>, <c>E-shape maj7</c>).</summary>
    public string Name { get; set; } = "";

    /// <summary>Canonical-C Voicing-DSL line — the single serialization (e.g. <c>voicing Cmaj shape:C root:5 frets: x 3 2 0 1 0</c>).</summary>
    public string Dsl { get; set; } = "";

    /// <summary>Provenance: built-in / user-defined / pack (stored as its name).</summary>
    public Origin Origin { get; set; }

    /// <summary>For <see cref="Origin.Pack"/>, the source pack's id; null for built-in/user-defined.</summary>
    public string? PackId { get; set; }

    /// <summary>Catalog genre, denormalized for filter queries (null when unset).</summary>
    public string? Genre { get; set; }

    /// <summary>Catalog subgenre, denormalized for filter queries (null when unset).</summary>
    public string? Subgenre { get; set; }

    /// <summary>Catalog tags as a JSON array (constraint C3), denormalized from the DSL header's <c>tags: [...]</c>.</summary>
    public string Tags { get; set; } = "[]";

    /// <summary>When this definition was first saved (UTC).</summary>
    public DateTime CreatedUtc { get; set; }
}
