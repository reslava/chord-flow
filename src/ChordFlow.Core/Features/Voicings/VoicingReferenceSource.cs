using ChordFlow.Instruments.Guitar;
using ChordFlow.Music.Harmony;
using ChordFlow.Persistence;

namespace ChordFlow.Features.Voicings;

/// <summary>
/// The concrete <see cref="IVoicingReferenceSource"/>: resolves <c>u:</c>/<c>&lt;packageId&gt;:</c> references
/// against the id-tagged stored voicing shapes (origin-strict — a <c>u:</c> id never matches a package row and
/// vice-versa, req <c>IN6</c>), and <c>a:</c> references against the engine <c>auto:…</c> catalog derived on
/// the fly. A stored shape is realized to the chord's root exactly like the authored library; an <c>a:</c> id
/// is parsed to its (family, quality, shape) and derived at the root. Built once per render from
/// <see cref="VoicingStore.LoadShapesWithIds"/> (the pure list keeps it DB-free to unit-test).
/// </summary>
public sealed class VoicingReferenceSource : IVoicingReferenceSource
{
    /// <summary>The reserved source token for the user library.</summary>
    public const string UserSource = "u";

    /// <summary>The reserved source token for the engine <c>automatic</c> catalog.</summary>
    public const string AutomaticSource = "a";

    private readonly IReadOnlyList<(string Id, VoicingShape Shape, ContentSource Source, string? PackId)> _rows;

    public VoicingReferenceSource(IReadOnlyList<(string Id, VoicingShape Shape, ContentSource Source, string? PackId)> rows)
    {
        ArgumentNullException.ThrowIfNull(rows);
        _rows = rows;
    }

    /// <summary>Build the reference source from the store's id-tagged, source-tagged voicing rows.</summary>
    public static VoicingReferenceSource From(VoicingStore store)
    {
        ArgumentNullException.ThrowIfNull(store);
        return new VoicingReferenceSource(store.LoadShapesWithIds());
    }

    /// <summary>A reference source over no stored rows — only <c>a:</c> engine references resolve.</summary>
    public static readonly IVoicingReferenceSource Empty =
        new VoicingReferenceSource(Array.Empty<(string, VoicingShape, ContentSource, string?)>());

    /// <inheritdoc/>
    public Voicing? Resolve(string source, string id, Chord chord)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(chord);

        return source switch
        {
            UserSource => Stored(id, ContentSource.User, packageId: null, chord),
            AutomaticSource => Automatic(id, chord),
            _ => Stored(id, ContentSource.Package, packageId: source, chord),   // any other token is a package id
        };
    }

    // Origin-strict stored lookup: match id + source (+ pack for a package), then realize the canonical-C
    // shape to the chord's root. A miss (unknown id, or the id living only in a different source) returns null.
    private Voicing? Stored(string id, ContentSource source, string? packageId, Chord chord)
    {
        foreach ((string rowId, VoicingShape shape, ContentSource rowSource, string? packId) in _rows)
        {
            if (rowId == id && rowSource == source && (packageId is null || packId == packageId))
            {
                return shape.Realize(chord.Root);
            }
        }

        return null;
    }

    // Engine reference: parse the auto:<family>:<quality>:<shape> id and derive that grip at the chord's root.
    // A malformed id, or a shape that has no clean grip at this root, is a miss (null → fail loud upstream).
    private static Voicing? Automatic(string id, Chord chord)
    {
        if (!AutomaticVoicingId.TryParse(id, out VoicingFamily family, out Quality quality, out CagedShape shape))
        {
            return null;
        }

        try
        {
            ChordShape derived = FamilyVoicing.Derive(family, quality, shape, chord.Root, 0, VoicingRealizer.MaxFret);
            return ChordShapeVoicing.ToVoicing(derived);
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentOutOfRangeException)
        {
            return null;
        }
    }
}
