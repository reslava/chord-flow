using ChordFlow.Instruments.Guitar;
using ChordFlow.Music.Harmony;
using ChordFlow.Persistence;

namespace ChordFlow.Features.Voicings;

/// <summary>
/// The DB-backed <see cref="IStoredVoicingSource"/>: source-tagged authored voicings (package + user, no
/// collapse — content-source-model) realized per chord. Built from <see cref="VoicingStore.LoadShapesBySource"/>
/// once per render and queried per chord. Exact-quality matching (a maj7 never returns a maj).
/// </summary>
public sealed class StoredVoicingSource : IStoredVoicingSource
{
    private readonly IReadOnlyList<(VoicingShape Shape, ContentSource Source, string? PackId)> _shapes;

    public StoredVoicingSource(IReadOnlyList<(VoicingShape Shape, ContentSource Source, string? PackId)> shapes)
    {
        ArgumentNullException.ThrowIfNull(shapes);
        _shapes = shapes;
    }

    /// <summary>Load the source-tagged stored voicings from <paramref name="store"/>.</summary>
    public static StoredVoicingSource From(VoicingStore store)
    {
        ArgumentNullException.ThrowIfNull(store);
        return new StoredVoicingSource(store.LoadShapesBySource());
    }

    /// <summary>A source with no stored voicings (the fallback always yields empty).</summary>
    public static readonly IStoredVoicingSource Empty =
        new StoredVoicingSource(Array.Empty<(VoicingShape, ContentSource, string?)>());

    public IReadOnlyList<Voicing> Candidates(Chord chord, ContentSource source, string? packageId)
    {
        ArgumentNullException.ThrowIfNull(chord);

        var candidates = new List<Voicing>();
        foreach ((VoicingShape shape, ContentSource rowSource, string? packId) in _shapes)
        {
            if (rowSource != source || shape.Quality != chord.Quality)
            {
                continue;
            }

            if (packageId is not null && packId != packageId)
            {
                continue;
            }

            if (shape.Realize(chord.Root) is { } voicing)
            {
                candidates.Add(voicing);
            }
        }

        return candidates;
    }
}
