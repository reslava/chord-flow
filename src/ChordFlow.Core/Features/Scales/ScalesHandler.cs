using ChordFlow.Domain;
using ChordFlow.Instruments.Guitar;

namespace ChordFlow.Features.Scales;

/// <summary>
/// Scales vertical slice: the one handler behind the <c>scalePreview</c> bridge verb. It turns an interval set
/// (e.g. <c>"1 b3 4 5 b7"</c>) + a root pitch class into a <see cref="FretboardDiagram"/> via
/// <see cref="IntervalSetDiagram"/> — the dogfood harness for <see cref="IntervalLattice"/>. Stateless and
/// pure (no db, no renderer): all the theory lives in Core. A bad token throws <see cref="FormatException"/>,
/// which the host maps to a <see cref="ScaleErrorEnvelope"/> (mirrors the content-CRUD parse-error path).
/// </summary>
public sealed class ScalesHandler
{
    /// <summary>Build the diagram for an interval set rooted at <paramref name="rootPitchClass"/> (reduced mod-12).</summary>
    /// <exception cref="FormatException">An interval token is invalid (see <see cref="IntervalSpeller.Parse"/>).</exception>
    public ScaleDiagramEnvelope Preview(string intervals, int rootPitchClass)
    {
        ArgumentNullException.ThrowIfNull(intervals);
        var root = new PitchClass(((rootPitchClass % 12) + 12) % 12);
        return new ScaleDiagramEnvelope(IntervalSetDiagram.Build(intervals, root));
    }
}
