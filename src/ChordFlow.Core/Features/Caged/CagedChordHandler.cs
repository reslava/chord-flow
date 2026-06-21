using ChordFlow.Music.Harmony;
using ChordFlow.Instruments.Guitar;

namespace ChordFlow.Features.Caged;

/// <summary>
/// CAGED Chords vertical slice: the one handler behind the <c>cagedChordPreview</c> bridge verb. It runs the
/// <see cref="CagedDerivation">derivation engine</see> for a (quality, CAGED shape, root) and turns the derived
/// <see cref="ChordShape"/> into a <see cref="FretboardDiagram"/> via <see cref="ChordShapeDiagram"/> — the dogfood
/// harness for the engine. <b>Auto-region:</b> derive at <c>[0, <see cref="NeckMaxFret"/>]</c>; the engine anchors the
/// shape's lowest placement in that window, so this is "pick the lowest position." The page is a generator — every
/// quality × shape is accepted, including combos the pack never authored. Stateless and pure (no db, no renderer).
/// An unknown quality/shape (<see cref="FormatException"/>) or an unvoiceable combo (<see cref="InvalidOperationException"/>)
/// is mapped by the host to a <see cref="CagedChordErrorEnvelope"/>.
/// </summary>
public sealed class CagedChordHandler
{
    // Auto-region search bound: every root's lowest octave anchor lands within the first 12 frets, so [0, 15] always
    // finds it; the grip's own span is the reach window (≤4 frets), independent of this bound.
    private const int NeckMaxFret = 15;

    /// <summary>Derive <paramref name="quality"/> in CAGED <paramref name="shape"/> at <paramref name="rootPitchClass"/> (mod-12) and build its diagram.</summary>
    /// <exception cref="FormatException"><paramref name="quality"/> or <paramref name="shape"/> is not a known name.</exception>
    /// <exception cref="InvalidOperationException">the combo has no voiceable placement in range.</exception>
    public CagedChordDiagramEnvelope Preview(string quality, string shape, int rootPitchClass)
    {
        ArgumentNullException.ThrowIfNull(quality);
        ArgumentNullException.ThrowIfNull(shape);

        if (!Enum.TryParse(quality, ignoreCase: true, out Quality parsedQuality) || !Enum.IsDefined(parsedQuality))
            throw new FormatException($"Unknown chord quality '{quality}'.");
        if (!Enum.TryParse(shape, ignoreCase: true, out CagedShape parsedShape) || !Enum.IsDefined(parsedShape))
            throw new FormatException($"Unknown CAGED shape '{shape}'. Expected one of C, A, G, E, D.");

        var root = new PitchClass(((rootPitchClass % 12) + 12) % 12);
        ChordShape derived = CagedDerivation.Derive(parsedQuality, parsedShape, root, 0, NeckMaxFret);
        return new CagedChordDiagramEnvelope(ChordShapeDiagram.Build(derived, root));
    }
}
