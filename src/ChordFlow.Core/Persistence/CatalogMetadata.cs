using ChordFlow.Music.Progressions;

namespace ChordFlow.Persistence;

/// <summary>
/// Catalog metadata — genre / subgenre / tags / description / tonality — for a content definition
/// (progression, song, rhythm, voicing). An <b>Entity-layer</b> value (constraint C1: catalog metadata never
/// lives on pure <c>Domain/</c> music-theory records — those stay theory-pure). Denormalized from the
/// self-describing DSL header (<see cref="CatalogHeader"/>), while the DSL header remains the canonical source.
/// Shared by every content entity so the fields mean the same thing across the catalog. <see cref="Description"/>
/// and <see cref="Tonality"/> ride in the stored DSL header (no dedicated column) and are re-parsed on load, so
/// they never reach the pure Domain parser as text — the tonality reaches it as a resolved
/// <see cref="Music.Progressions.Tonality"/> param (first-class-minor-keys).
/// </summary>
public sealed record CatalogMetadata(
    string? Genre,
    string? Subgenre,
    IReadOnlyList<string> Tags,
    string? Description = null,
    Tonality Tonality = Tonality.Major)
{
    /// <summary>No catalog metadata — a definition whose DSL carries no header.</summary>
    public static readonly CatalogMetadata Empty = new(null, null, Array.Empty<string>());

    /// <summary>True when there is nothing to serialize as a header (no genre, subgenre, tags, description, or a non-Major tonality).</summary>
    public bool IsEmpty =>
        Genre is null && Subgenre is null && Tags.Count == 0 && Description is null && Tonality == Tonality.Major;
}
