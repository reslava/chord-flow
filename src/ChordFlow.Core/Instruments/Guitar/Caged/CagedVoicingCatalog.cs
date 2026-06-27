using ChordFlow.Music.Harmony;

namespace ChordFlow.Instruments.Guitar;

/// <summary>
/// The pinned set of (family, quality, CAGED-shape) combos the engine offers as <c>automatic</c> voicings
/// (shell-voicing-derivation, req IN6) — <b>64</b> combos:
/// <list type="bullet">
/// <item><c>caged</c> (full chord) over all 46 quality×shape combos — the eight five-shape qualities ×5, the
///   two diminished-family qualities (m7♭5, dim7) ×3.</item>
/// <item><c>doubled-shell</c> (chord minus 5th, doubled root) — a curated set of the commonly-played doubled-root
///   voicings: the <b>C form only</b>, for <c>dom7 / dim7 / 6 / m6</c> (e.g. the open-ish C7, C6) — 4.</item>
/// <item><c>shell</c> (compact 2-form) over the 7 shell-eligible qualities × the two forms {C (5th-string root),
///   E (6th-string root)} — 14.</item>
/// </list>
/// One source of truth shared by the listing source, the comping resolver, and the coverage test, so they can
/// never drift. (Triads have only the <c>caged</c> family — shells need a 7th or 6th.)
/// </summary>
public static class CagedVoicingCatalog
{
    private static readonly Quality[] FiveShapeQualities =
    {
        Quality.Major, Quality.Minor, Quality.Major7, Quality.Dominant7, Quality.Minor7, Quality.Augmented,
        Quality.Major6, Quality.Minor6,
    };

    private static readonly Quality[] ThreeShapeQualities =
    {
        Quality.HalfDiminished7, Quality.Diminished7,
    };

    private static readonly CagedShape[] AllShapes =
        { CagedShape.C, CagedShape.A, CagedShape.G, CagedShape.E, CagedShape.D };

    private static readonly CagedShape[] DiminishedShapes =
        { CagedShape.A, CagedShape.E, CagedShape.D };

    /// <summary>The two shell forms: C = 5th-string root, E = 6th-string root.</summary>
    private static readonly CagedShape[] ShellForms = { CagedShape.C, CagedShape.E };

    /// <summary>The qualities a shell can voice — those with a 7th or a 6th.</summary>
    private static readonly IReadOnlySet<Quality> ShellEligible = new HashSet<Quality>
    {
        Quality.Dominant7, Quality.Major7, Quality.Minor7,
        Quality.HalfDiminished7, Quality.Diminished7, Quality.Major6, Quality.Minor6,
    };

    /// <summary>The curated `doubled-shell` qualities — the commonly-played doubled-root voicings (chat-001).</summary>
    private static readonly IReadOnlySet<Quality> DoubledShellQualities = new HashSet<Quality>
    {
        Quality.Dominant7, Quality.Diminished7, Quality.Major6, Quality.Minor6,
    };

    /// <summary>`doubled-shell` is offered in the C form only.</summary>
    private static readonly CagedShape[] DoubledShellForms = { CagedShape.C };

    private static readonly VoicingFamily[] Families =
        { VoicingFamily.Caged, VoicingFamily.DoubledShell, VoicingFamily.Shell };

    /// <summary>All 91 (family, quality, shape) combos, in family then quality then shape order.</summary>
    public static readonly IReadOnlyList<(VoicingFamily Family, Quality Quality, CagedShape Shape)> Combos = Build();

    /// <summary>Whether a quality can be voiced as a shell (has a 7th or 6th).</summary>
    public static bool IsShellEligible(Quality quality) => ShellEligible.Contains(quality);

    /// <summary>
    /// The CAGED shapes the engine offers for <paramref name="family"/> × <paramref name="quality"/> (empty when
    /// the family does not cover that quality — e.g. a shell of a triad).
    /// </summary>
    public static IReadOnlyList<CagedShape> ShapesFor(VoicingFamily family, Quality quality) => family switch
    {
        VoicingFamily.Caged => CagedShapesFor(quality),
        VoicingFamily.DoubledShell => DoubledShellQualities.Contains(quality) ? DoubledShellForms : Array.Empty<CagedShape>(),
        VoicingFamily.Shell => ShellEligible.Contains(quality) ? ShellForms : Array.Empty<CagedShape>(),
        _ => Array.Empty<CagedShape>(),
    };

    // The full chord's shapes for a quality (the caged family's coverage).
    private static IReadOnlyList<CagedShape> CagedShapesFor(Quality quality) =>
        ThreeShapeQualities.Contains(quality) ? DiminishedShapes
        : FiveShapeQualities.Contains(quality) ? AllShapes
        : Array.Empty<CagedShape>();

    private static IReadOnlyList<(VoicingFamily, Quality, CagedShape)> Build()
    {
        var combos = new List<(VoicingFamily, Quality, CagedShape)>();
        foreach (VoicingFamily family in Families)
        {
            foreach (Quality quality in FiveShapeQualities.Concat(ThreeShapeQualities))
            {
                foreach (CagedShape shape in ShapesFor(family, quality))
                {
                    combos.Add((family, quality, shape));
                }
            }
        }

        return combos;
    }
}
