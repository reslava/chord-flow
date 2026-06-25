using ChordFlow.Music.Harmony;

namespace ChordFlow.Instruments.Guitar;

/// <summary>
/// The pinned set of quality×CAGED-shape combos the engine offers as <c>automatic</c> voicings
/// (engine-derived-as-app-source, req IN2/IN9/C5) — the same <b>36</b> the golden oracle verifies. The six
/// tertian qualities take all five CAGED shapes; the two diminished-family qualities (m7♭5, dim7) trim to
/// E/A/D only (no C/G), matching the oracle and the caged-c-full rule. One source of truth shared by the
/// listing source, the comping resolver, and the coverage test, so they can never drift.
/// </summary>
public static class CagedVoicingCatalog
{
    private static readonly Quality[] FiveShapeQualities =
    {
        Quality.Major, Quality.Minor, Quality.Major7, Quality.Dominant7, Quality.Minor7, Quality.Augmented,
    };

    private static readonly Quality[] ThreeShapeQualities =
    {
        Quality.HalfDiminished7, Quality.Diminished7,
    };

    private static readonly CagedShape[] AllShapes =
        { CagedShape.C, CagedShape.A, CagedShape.G, CagedShape.E, CagedShape.D };

    private static readonly CagedShape[] DiminishedShapes =
        { CagedShape.A, CagedShape.E, CagedShape.D };

    /// <summary>The 36 (quality, shape) combos, tertian qualities first (each ×5), then m7♭5/dim7 (each ×3).</summary>
    public static readonly IReadOnlyList<(Quality Quality, CagedShape Shape)> Combos = Build();

    /// <summary>The CAGED shapes the engine can derive for <paramref name="quality"/> (empty for an unsupported quality).</summary>
    public static IReadOnlyList<CagedShape> ShapesFor(Quality quality) =>
        ThreeShapeQualities.Contains(quality) ? DiminishedShapes
        : FiveShapeQualities.Contains(quality) ? AllShapes
        : Array.Empty<CagedShape>();

    private static IReadOnlyList<(Quality, CagedShape)> Build()
    {
        var combos = new List<(Quality, CagedShape)>();
        foreach (Quality quality in FiveShapeQualities)
        {
            foreach (CagedShape shape in AllShapes)
            {
                combos.Add((quality, shape));
            }
        }

        foreach (Quality quality in ThreeShapeQualities)
        {
            foreach (CagedShape shape in DiminishedShapes)
            {
                combos.Add((quality, shape));
            }
        }

        return combos;
    }
}
