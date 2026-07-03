using ChordFlow.Bridge;
using ChordFlow.Instruments.Guitar;
using ChordFlow.Music.Harmony;

namespace ChordFlow.Features.Voicings;

/// <summary>
/// The Voicings Engine inspector slice: the handler behind the <c>voicingDerive</c> and <c>voicingOperators</c>
/// bridge verbs (voicings-engine, req IN11/IN16). <see cref="Derive"/> runs one operator for a
/// (family, quality, root, params) request and returns the introspectable derivation — the abstract
/// <see cref="ToneSelection"/>, the ordered <see cref="RealizationStep"/>s, and the realized grip as a
/// <see cref="FretboardDiagram"/> (the same <see cref="RealizedVoicingDiagram"/> path the grid uses).
/// <see cref="Operators"/> projects the <see cref="VoicingOperators"/> registry + each operator's
/// <see cref="ParameterSchema"/> so the page's controls are schema-driven. Stateless and pure; fails loud on a bad
/// family/quality/shape or an ineligible (family, quality) combo.
/// </summary>
public sealed class VoicingDeriveHandler
{
    private const int DefaultMinFret = 0;
    private const int DefaultMaxFret = 15;   // mirrors VoicingGridHandler.NeckMaxFret

    // The distinct qualities the catalog knows, in enum order — the domain for each operator's eligible coverage.
    private static readonly IReadOnlyList<Quality> CatalogQualities =
        CagedVoicingCatalog.Combos.Select(c => c.Quality).Distinct().OrderBy(q => (int)q).ToList();

    /// <summary>Derive one voicing for <paramref name="request"/> and build its reply. Throws on invalid input.</summary>
    public VoicingDerivationEnvelope Derive(VoicingDeriveRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        VoicingFamily family = ParseFamily(request.Family);
        Quality quality = ParseEnum<Quality>(request.Quality, "quality");
        CagedShape shape = ParseEnum<CagedShape>(request.Shape, "shape");
        var root = new PitchClass(((request.Root % 12) + 12) % 12);
        int minFret = request.MinFret ?? DefaultMinFret;
        int maxFret = request.MaxFret ?? DefaultMaxFret;

        VoicingDerivation derivation = FamilyVoicing.Voicing(family, quality, shape, root, minFret, maxFret);

        var key = new Key(root, IsMinor: false);
        var chord = new Chord(root, quality);
        FretboardDiagram diagram = RealizedVoicingDiagram.Build(chord, ChordShapeVoicing.ToVoicing(derivation.Grip), key);

        IReadOnlyList<ToneSelectionDto> tones = derivation.ToneSelection
            .Select(t => new ToneSelectionDto(
                t.Interval,
                IntervalSpeller.Label(t.Interval, t.Function),
                t.Function.ToString(),
                NoteSpeller.Name(t.PitchClassFor(root), key)))
            .ToList();

        IReadOnlyList<RealizationStepDto> steps = derivation.Realization
            .Select(s => new RealizationStepDto(s.Kind.ToString(), s.Label))
            .ToList();

        return new VoicingDerivationEnvelope(
            AutomaticVoicingId.For(family, quality, shape),
            family.Token(),
            derivation.Kind.ToString(),
            tones,
            steps,
            diagram);
    }

    /// <summary>Project the operator registry + each operator's declared schema and eligible coverage for the page.</summary>
    public VoicingOperatorsEnvelope Operators()
    {
        var operators = VoicingOperators.All
            .Select(op => new OperatorDto(
                op.Family.Token(),
                op.Kind.ToString(),
                op.DisplayName,
                op.Parameters.Parameters.Select(ToParamDto).ToList(),
                EligibleShapesByQuality(op.Family)))
            .ToList();

        return new VoicingOperatorsEnvelope(operators);
    }

    private static IReadOnlyList<QualityShapesDto> EligibleShapesByQuality(VoicingFamily family) =>
        CatalogQualities
            .Select(q => (Quality: q, Shapes: CagedVoicingCatalog.ShapesFor(family, q)))
            .Where(x => x.Shapes.Count > 0)
            .Select(x => new QualityShapesDto(x.Quality.ToString(), x.Shapes.Select(s => s.ToString()).ToList()))
            .ToList();

    private static OperatorParamDto ToParamDto(ParameterDef def) => def switch
    {
        EnumParam e => new OperatorParamDto(e.Name, "enum", e.Values, e.Default, null, null),
        RegionParam r => new OperatorParamDto(r.Name, "region", null, null, r.Min, r.Max),
        _ => throw new ArgumentOutOfRangeException(nameof(def), def, "Unknown parameter kind."),
    };

    private static VoicingFamily ParseFamily(string? token) =>
        VoicingFamilies.TryParse((token ?? "").Trim().ToLowerInvariant(), out VoicingFamily family)
            ? family
            : throw new ArgumentException($"Unknown voicing family '{token}'.", nameof(token));

    private static TEnum ParseEnum<TEnum>(string? value, string label) where TEnum : struct, Enum =>
        Enum.TryParse(value, ignoreCase: true, out TEnum parsed) && Enum.IsDefined(parsed)
            ? parsed
            : throw new ArgumentException($"Unknown {label} '{value}'.", nameof(value));
}
