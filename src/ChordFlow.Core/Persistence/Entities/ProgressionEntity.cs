namespace ChordFlow.Persistence.Entities;

/// <summary>
/// Persisted progression <b>definition</b> — the canonical Nashville <see cref="Dsl"/> string is the v1
/// serialization (constraint C5: a future richer form can add a <c>spans_json</c> column or normalized
/// tables without losing this string). The <see cref="Dsl"/> may carry an optional leading
/// <see cref="CatalogHeader"/> (<c>genre:</c>/<c>subgenre:</c>/<c>tags:</c>); realization strips it via
/// <c>CatalogHeader.Parse</c> so the pure <c>ProgressionParser</c> only sees the bar grammar. Load =
/// strip header → <c>ProgressionParser.Parse(body)</c> → realize → render; alphaTex is never stored.
/// Mirrors the <see cref="ExerciseEntity"/> "store the definition, regenerate on load" pattern.
/// </summary>
public sealed class ProgressionEntity : IOriginated
{
    /// <summary>Stable id and primary key. Human slug for built-ins (e.g. <c>12bar_blues</c>), GUID for user progressions.</summary>
    public string Id { get; set; } = "";

    /// <summary>Display name (e.g. <c>12-Bar Blues</c>).</summary>
    public string Name { get; set; } = "";

    /// <summary>Canonical Nashville DSL — the v1 serialization, optionally prefixed by a catalog header (e.g. <c>17 17 17 17 47 47 17 17 57 47 17 57</c>).</summary>
    public string Dsl { get; set; } = "";

    /// <summary>Provenance: built-in / user-defined / pack (stored as its name).</summary>
    public Origin Origin { get; set; }

    /// <summary>For <see cref="Origin.Pack"/>, the source pack's id; null for <see cref="Origin.BuiltIn"/>/<see cref="Origin.UserDefined"/> (design §2: discriminator + optional PackId).</summary>
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
