using ChordFlow.Music.Harmony;

namespace ChordFlow.Instruments.Guitar;

/// <summary>
/// The <see cref="VoicingFamily.DoubledShell"/> operator — the one <see cref="OperatorKind.Reduce"/> in v1
/// (voicings-engine design §4). It is <c>Reduce(operand: Caged)</c>: derive the full CAGED grip, then mute the
/// Fifth (<see cref="ShellReduction.MuteFifth"/>), keeping root/3rd/7th/6th + doublings — "a chord minus the 5th".
/// Inherits the inner CAGED trace and appends a single <see cref="RealizationStepKind.Reduce"/> step; its abstract
/// <see cref="ToneSelection"/> is the inner selection minus the Fifth. Declares the <c>baseShape</c> (the CAGED
/// shape it reduces — the catalog offers the C form only today) and the neck <c>region</c>.
/// </summary>
public sealed class DoubledShellOperator : IVoicingOperator
{
    /// <summary>The parameter name for the base CAGED shape being reduced.</summary>
    public const string BaseShapeParam = "baseShape";

    /// <summary>The parameter name for the neck fret region.</summary>
    public const string RegionParamName = "region";

    public VoicingFamily Family => VoicingFamily.DoubledShell;
    public OperatorKind Kind => OperatorKind.Reduce;
    public string DisplayName => "Doubled shell (chord − 5th)";

    public ParameterSchema Parameters { get; } = new(
        EnumParam.Of(BaseShapeParam, new[] { CagedShape.C, CagedShape.A, CagedShape.G, CagedShape.E, CagedShape.D }, CagedShape.C),
        new RegionParam(RegionParamName, 0, 24));

    public VoicingDerivation Derive(VoicingRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        Parameters.Validate(request);

        CagedShape baseShape = request.Params.Enum<CagedShape>(BaseShapeParam);

        // Operand: the full CAGED derivation (its own trace + grip).
        VoicingDerivation inner = CagedDerivation.DeriveVoicing(
            request.Quality, baseShape, request.Root, request.Region.MinFret, request.Region.MaxFret);

        ChordShape reduced = ShellReduction.MuteFifth(inner.Grip);

        // The strings the reduction newly muted (sounded in the CAGED grip, muted after) — the Fifth occurrences.
        var mutedFifths = inner.Grip.Strings
            .Where(s => !s.IsMuted && reduced.Strings.First(r => r.String == s.String).IsMuted)
            .Select(s => s.String)
            .OrderByDescending(s => s)
            .ToList();

        var realization = new List<RealizationStep>(inner.Realization)
        {
            new(RealizationStepKind.Reduce,
                mutedFifths.Count > 0
                    ? $"Doubled-shell reduction: muted the Fifth on string(s) {string.Join(", ", mutedFifths)}."
                    : "Doubled-shell reduction: no Fifth was sounded to mute.",
                mutedFifths),
        };

        IReadOnlyList<ToneSelection> toneSelection = inner.ToneSelection
            .Where(t => t.Function != ChordToneFunction.Fifth)
            .ToList();

        return new VoicingDerivation(
            VoicingFamily.DoubledShell, OperatorKind.Reduce, Parameters.Resolve(request), toneSelection, realization, reduced);
    }
}
