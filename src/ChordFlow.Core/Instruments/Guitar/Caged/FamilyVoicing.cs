using ChordFlow.Music.Harmony;

namespace ChordFlow.Instruments.Guitar;

/// <summary>
/// The single dispatch from a <see cref="VoicingFamily"/> to its derivation (shell-voicing-derivation): one
/// engine grip for a (family, quality, shape) at a root within a fret window. <see cref="VoicingFamily.Caged"/>
/// is the full derived chord; <see cref="VoicingFamily.DoubledShell"/> is that chord minus the 5th; and
/// <see cref="VoicingFamily.Shell"/> is the 2-form compact shell. Shared by the comping resolver and the
/// Content-preview doc so the family→derivation mapping never drifts. Throws the usual derivation exceptions
/// (no anchor / unspellable) when no clean grip exists in the window — that is the caller's region filter.
/// <para>
/// Now a thin <b>grip shim</b> over the introspectable <see cref="VoicingOperators"/> registry (voicings-engine):
/// it builds a <see cref="VoicingRequest"/> and returns <c>operator.Derive(request).Grip</c>. The grip is
/// byte-identical to the pre-registry dispatch, so <c>CompingResolver</c> and <c>VoicingGridHandler</c> are
/// untouched — they still consume a <see cref="ChordShape"/>.
/// </para>
/// </summary>
public static class FamilyVoicing
{
    /// <summary>Derive the <paramref name="family"/> grip for <paramref name="quality"/> in <paramref name="shape"/>.</summary>
    public static ChordShape Derive(
        VoicingFamily family, Quality quality, CagedShape shape, PitchClass root, int minFret, int maxFret) =>
        Voicing(family, quality, shape, root, minFret, maxFret).Grip;

    /// <summary>The full derivation trace for the <paramref name="family"/> grip — the introspectable form of <see cref="Derive"/>.</summary>
    public static VoicingDerivation Voicing(
        VoicingFamily family, Quality quality, CagedShape shape, PitchClass root, int minFret, int maxFret)
    {
        var request = new VoicingRequest(
            quality, root, new FretRegion(minFret, maxFret),
            ParameterValues.Of((VoicingOperators.ShapeParamName(family), shape.ToString())));

        return VoicingOperators.For(family).Derive(request);
    }
}
