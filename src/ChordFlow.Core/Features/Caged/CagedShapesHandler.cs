using ChordFlow.Music.Harmony;
using ChordFlow.Instruments.Guitar;

namespace ChordFlow.Features.Caged;

/// <summary>
/// CAGED-shapes vertical slice: the one handler behind the <c>cagedPreview</c> bridge verb. It turns a CAGED
/// shape name (<c>C</c>/<c>A</c>/<c>G</c>/<c>E</c>/<c>D</c>) + a root pitch class into a <see cref="FretboardDiagram"/>
/// via <see cref="CagedShapeDiagram"/> — the dogfood harness for <see cref="OctaveShape"/>. Stateless and pure (no
/// db, no renderer): all the theory lives in Core. An unknown shape throws <see cref="FormatException"/>, which the
/// host maps to a <see cref="CagedErrorEnvelope"/> (mirrors the Scales parse-error path).
/// </summary>
public sealed class CagedShapesHandler
{
    /// <summary>Build the diagram for <paramref name="shape"/> rooted at <paramref name="rootPitchClass"/> (reduced mod-12).</summary>
    /// <exception cref="FormatException"><paramref name="shape"/> is not one of C, A, G, E, D.</exception>
    public CagedDiagramEnvelope Preview(string shape, int rootPitchClass)
    {
        ArgumentNullException.ThrowIfNull(shape);
        if (!Enum.TryParse(shape, ignoreCase: true, out CagedShape parsed) || !Enum.IsDefined(parsed))
            throw new FormatException($"Unknown CAGED shape '{shape}'. Expected one of C, A, G, E, D.");

        var root = new PitchClass(((rootPitchClass % 12) + 12) % 12);
        return new CagedDiagramEnvelope(CagedShapeDiagram.Build(parsed, root));
    }
}
