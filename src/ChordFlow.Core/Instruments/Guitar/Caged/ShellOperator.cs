using ChordFlow.Music.Harmony;

namespace ChordFlow.Instruments.Guitar;

/// <summary>
/// The <see cref="VoicingFamily.Shell"/> operator — the introspectable wrapper over <see cref="ShellDerivation"/>
/// (voicings-engine design §2.1). <see cref="OperatorKind.DeriveFromFormula"/>: a distinct 2-form guide-tone
/// derivation (root + 3rd + 7th|6th, 5th omitted), <b>not</b> a reduction of CAGED. Declares the shell <c>form</c>
/// (C = 5th-string root, E = 6th-string root) and the neck <c>region</c>.
/// </summary>
public sealed class ShellOperator : IVoicingOperator
{
    /// <summary>The parameter name for the shell form.</summary>
    public const string FormParam = "form";

    /// <summary>The parameter name for the neck fret region.</summary>
    public const string RegionParamName = "region";

    public VoicingFamily Family => VoicingFamily.Shell;
    public OperatorKind Kind => OperatorKind.DeriveFromFormula;
    public string DisplayName => "Shell (guide-tone)";

    public ParameterSchema Parameters { get; } = new(
        EnumParam.Of(FormParam, new[] { CagedShape.C, CagedShape.E }, CagedShape.C),
        new RegionParam(RegionParamName, 0, 24));

    public VoicingDerivation Derive(VoicingRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        Parameters.Validate(request);

        CagedShape form = request.Params.Enum<CagedShape>(FormParam);
        VoicingDerivation derivation = ShellDerivation.DeriveVoicing(
            request.Quality, form, request.Root, request.Region.MinFret, request.Region.MaxFret);

        return derivation with { Params = Parameters.Resolve(request) };
    }
}
