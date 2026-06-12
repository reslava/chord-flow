namespace ChordFlow.Persistence.Entities;

/// <summary>
/// Persisted song <b>definition</b> — the canonical Song <see cref="Dsl"/> string is the only stored form
/// (constraint C4: <c>RealizedSong</c>/alphaTex are regenerated on load, never persisted). The <see cref="Dsl"/>
/// may carry an optional leading catalog header (<c>genre:</c>/<c>subgenre:</c>/<c>tags:</c>) which
/// <c>CatalogHeader.Parse</c> strips before <c>SongParser</c> sees the arrangement grammar. Load = strip header
/// → <c>SongParser.Parse(body)</c> → <c>SongExpander.Expand</c> (resolving stored references against the
/// <c>Progressions</c> table) → render. Mirrors <see cref="ProgressionEntity"/> field-for-field.
/// </summary>
public sealed class SongEntity : IOriginated
{
    /// <summary>Stable id and primary key. Human slug for built-ins (e.g. <c>blues_song_demo</c>), GUID for user songs.</summary>
    public string Id { get; set; } = "";

    /// <summary>Display name (e.g. <c>Blues Song Demo</c>).</summary>
    public string Name { get; set; } = "";

    /// <summary>Canonical Song DSL — the only stored serialization, optionally prefixed by a catalog header.</summary>
    public string Dsl { get; set; } = "";

    /// <summary>Provenance: built-in / user-defined / pack (stored as its name).</summary>
    public Origin Origin { get; set; }

    /// <summary>For <see cref="Origin.Pack"/>, the source pack's id; null for built-in/user-defined.</summary>
    public string? PackId { get; set; }

    /// <summary>Catalog genre, denormalized from the DSL header for filter queries (null when unset).</summary>
    public string? Genre { get; set; }

    /// <summary>Catalog subgenre, denormalized from the DSL header for filter queries (null when unset).</summary>
    public string? Subgenre { get; set; }

    /// <summary>Catalog tags as a JSON array (constraint C3), denormalized from the DSL header's <c>tags: [...]</c>.</summary>
    public string Tags { get; set; } = "[]";

    /// <summary>When this definition was first saved (UTC).</summary>
    public DateTime CreatedUtc { get; set; }
}
