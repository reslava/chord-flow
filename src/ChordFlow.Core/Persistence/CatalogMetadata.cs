namespace ChordFlow.Persistence;

/// <summary>
/// Catalog metadata — genre / subgenre / tags — for a content definition (progression, song, rhythm,
/// voicing). An <b>Entity-layer</b> value (constraint C1: catalog metadata never lives on pure
/// <c>Domain/</c> music-theory records — those stay theory-pure). Denormalized from the self-describing
/// DSL header (<see cref="CatalogHeader"/>) into entity columns for filter queries, while the DSL header
/// remains the canonical source. Shared by every content entity so genre/subgenre/tags mean the same
/// thing across the catalog.
/// </summary>
public sealed record CatalogMetadata(string? Genre, string? Subgenre, IReadOnlyList<string> Tags)
{
    /// <summary>No catalog metadata — a definition whose DSL carries no header.</summary>
    public static readonly CatalogMetadata Empty = new(null, null, Array.Empty<string>());

    /// <summary>True when there is nothing to serialize as a header (no genre, subgenre, or tags).</summary>
    public bool IsEmpty => Genre is null && Subgenre is null && Tags.Count == 0;
}
