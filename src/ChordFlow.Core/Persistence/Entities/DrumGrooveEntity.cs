namespace ChordFlow.Persistence.Entities;

/// <summary>
/// Persisted drum-groove <b>definition</b> — the canonical hit-grid <see cref="Dsl"/> string is the single
/// serialization (alphaTex + the parsed lanes are never stored, they regenerate on load). Mirrors
/// <see cref="ProgressionEntity"/>'s "store the definition, regenerate on load" pattern and, like it, carries
/// full catalog metadata (grooves are genre-tagged: rock/blues/funk/jazz — req IN6), so it is an
/// <see cref="ICatalogEntity"/>. Like <see cref="RhythmPatternEntity"/> it also stores the meter
/// (<see cref="TsNumerator"/>/<see cref="TsDenominator"/>, 4/4 only today — stored so non-4/4 is additive).
/// Load = strip header → <c>DrumGrooveParser.Parse(body, ts)</c> → <c>DrumGroove</c>.
/// </summary>
public sealed class DrumGrooveEntity : ICatalogEntity
{
    /// <summary>Stable id and primary key. Human slug for pack grooves (e.g. <c>rock</c>), GUID for user grooves.</summary>
    public string Id { get; set; } = "";

    /// <summary>Display name (e.g. <c>Rock (straight 8ths)</c>).</summary>
    public string Name { get; set; } = "";

    /// <summary>Canonical hit-grid DSL, optionally prefixed by a catalog header (<c>genre:</c>/<c>tags:</c>/…).</summary>
    public string Dsl { get; set; } = "";

    /// <summary>Time-signature numerator (4 for 4/4). Stored so a future non-4/4 groove is additive.</summary>
    public int TsNumerator { get; set; } = 4;

    /// <summary>Time-signature denominator (4 for 4/4).</summary>
    public int TsDenominator { get; set; } = 4;

    /// <summary>Provenance: user-defined / pack (stored as its name).</summary>
    public Origin Origin { get; set; }

    /// <summary>For <see cref="Origin.Pack"/>, the source pack's id; null for user-defined.</summary>
    public string? PackId { get; set; }

    /// <summary>Catalog genre, denormalized from the DSL header for filter queries (null when unset).</summary>
    public string? Genre { get; set; }

    /// <summary>Catalog subgenre, denormalized from the DSL header (null when unset).</summary>
    public string? Subgenre { get; set; }

    /// <summary>Catalog tags as a JSON array, denormalized from the DSL header's <c>tags: [...]</c>.</summary>
    public string Tags { get; set; } = "[]";

    /// <summary>When this definition was first saved (UTC).</summary>
    public DateTime CreatedUtc { get; set; }
}
