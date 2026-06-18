using ChordFlow.Domain;

namespace ChordFlow.Instruments.Guitar;

/// <summary>
/// A CAGED chord-shape family — the five movable forms a chord quality can take across the neck.
/// Authored as metadata on a <see cref="VoicingShape"/>: it labels the chord diagram and (Step 3)
/// breaks ties when the book ranks realized voicings by neck position.
/// </summary>
public enum CagedShape
{
    C,
    A,
    G,
    E,
    D,
}

/// <summary>
/// Ranking metadata for <see cref="CagedShape"/>. The familiarity order breaks ties when the book ranks
/// equally-placed realized voicings — the two barre-root shapes (E, A) first, then G, C, D. A default for
/// slice 1; pack-overridable ordering is the deferred difficulty/packs work (req IN5).
/// </summary>
public static class CagedShapeRanking
{
    // Lower = more familiar / more commonly taught.
    private static readonly IReadOnlyDictionary<CagedShape, int> Familiarity =
        new Dictionary<CagedShape, int>
        {
            [CagedShape.E] = 0,
            [CagedShape.A] = 1,
            [CagedShape.G] = 2,
            [CagedShape.C] = 3,
            [CagedShape.D] = 4,
        };

    /// <summary>The familiarity rank of <paramref name="shape"/> (0 = most familiar).</summary>
    public static int FamiliarityRank(this CagedShape shape) => Familiarity[shape];
}
