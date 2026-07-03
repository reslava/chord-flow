using ChordFlow.Music.Harmony;

namespace ChordFlow.Instruments.Guitar;

/// <summary>
/// The <see cref="VoicingFamily.Caged"/> operator — the introspectable wrapper over <see cref="CagedDerivation"/>
/// (voicings-engine design §2.1). <see cref="OperatorKind.DeriveFromFormula"/>: it builds the full chord in a CAGED
/// shape from the quality formula + fretboard geometry. Declares two parameters — the CAGED <c>shape</c> and the
/// neck <c>region</c> — and returns the derivation trace with the resolved parameter echo attached.
/// </summary>
public sealed class CagedOperator : IVoicingOperator
{
    /// <summary>The parameter name for the CAGED shape choice.</summary>
    public const string ShapeParam = "shape";

    /// <summary>The parameter name for the neck fret region.</summary>
    public const string RegionParamName = "region";

    public VoicingFamily Family => VoicingFamily.Caged;
    public OperatorKind Kind => OperatorKind.DeriveFromFormula;
    public string DisplayName => "CAGED (full chord)";

    public ParameterSchema Parameters { get; } = new(
        EnumParam.Of(ShapeParam, new[] { CagedShape.C, CagedShape.A, CagedShape.G, CagedShape.E, CagedShape.D }, CagedShape.E),
        new RegionParam(RegionParamName, 0, 24));

    public VoicingDerivation Derive(VoicingRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        Parameters.Validate(request);

        CagedShape shape = request.Params.Enum<CagedShape>(ShapeParam);
        VoicingDerivation derivation = CagedDerivation.DeriveVoicing(
            request.Quality, shape, request.Root, request.Region.MinFret, request.Region.MaxFret);

        return derivation with { Params = Parameters.Resolve(request) };
    }
}
