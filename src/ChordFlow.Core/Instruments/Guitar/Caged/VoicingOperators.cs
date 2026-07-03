namespace ChordFlow.Instruments.Guitar;

/// <summary>
/// The registry of the guitar Voicings Engine's <see cref="IVoicingOperator"/>s (voicings-engine design §2.1) — the
/// single enumeration surface for the inspector page (the <c>voicingOperators</c> catalog verb) and the dispatch
/// home behind <see cref="FamilyVoicing"/>. Ordered by <see cref="VoicingFamily"/> so the listing is stable.
/// </summary>
public static class VoicingOperators
{
    private static readonly IReadOnlyDictionary<VoicingFamily, IVoicingOperator> ByFamily =
        new IVoicingOperator[] { new CagedOperator(), new DoubledShellOperator(), new ShellOperator() }
            .ToDictionary(op => op.Family);

    /// <summary>All operators, ordered by <see cref="VoicingFamily"/>.</summary>
    public static IReadOnlyList<IVoicingOperator> All { get; } =
        ByFamily.Values.OrderBy(op => (int)op.Family).ToList();

    /// <summary>The operator for <paramref name="family"/>; throws if the family has no registered operator.</summary>
    public static IVoicingOperator For(VoicingFamily family) =>
        ByFamily.TryGetValue(family, out IVoicingOperator? op)
            ? op
            : throw new ArgumentOutOfRangeException(nameof(family), family, "No operator is registered for this voicing family.");

    /// <summary>The enum-parameter name that carries the CAGED shape / shell form for <paramref name="family"/>.</summary>
    public static string ShapeParamName(VoicingFamily family) => family switch
    {
        VoicingFamily.Caged => CagedOperator.ShapeParam,
        VoicingFamily.Shell => ShellOperator.FormParam,
        VoicingFamily.DoubledShell => DoubledShellOperator.BaseShapeParam,
        _ => throw new ArgumentOutOfRangeException(nameof(family), family, "Unknown voicing family."),
    };
}
