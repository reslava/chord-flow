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

/// <summary>
/// An editor-authoritative overlay of the three user-editable catalog fields — <see cref="Genre"/>,
/// <see cref="Subgenre"/>, and <see cref="Tags"/> (content-metadata-editing IN5). A save carries one when the
/// editor's metadata controls were shown (the four metadata-bearing entities — Progression / Song / Voicing /
/// Drums); it is <b>authoritative</b> for exactly those three fields. <see cref="ApplyTo"/> overlays them onto
/// the preserved header while leaving <see cref="CatalogMetadata.Description"/> and
/// <see cref="CatalogMetadata.Tonality"/> untouched (constraint C4 — a metadata edit never destroys them;
/// tonality keeps its own dedicated control). <b>Present-but-empty clears:</b> a blank genre/subgenre or an
/// empty tag list removes that field — distinct from a <c>null</c> patch ("not edited"), which preserves the
/// source header as before (IN9). Rhythm carries no catalog metadata (EX1) and never receives a patch.
/// </summary>
public sealed record CatalogMetadataPatch(string? Genre, string? Subgenre, IReadOnlyList<string> Tags)
{
    /// <summary>Overlay this patch's three fields onto <paramref name="preserved"/>, keeping its
    /// <see cref="CatalogMetadata.Description"/> and <see cref="CatalogMetadata.Tonality"/>. A blank
    /// genre/subgenre normalizes to null (a cleared field emits no header line).</summary>
    public CatalogMetadata ApplyTo(CatalogMetadata preserved)
    {
        ArgumentNullException.ThrowIfNull(preserved);
        return preserved with
        {
            Genre = string.IsNullOrWhiteSpace(Genre) ? null : Genre.Trim(),
            Subgenre = string.IsNullOrWhiteSpace(Subgenre) ? null : Subgenre.Trim(),
            Tags = Tags ?? Array.Empty<string>(),
        };
    }
}
