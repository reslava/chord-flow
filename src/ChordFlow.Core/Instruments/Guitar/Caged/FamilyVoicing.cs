using ChordFlow.Music.Harmony;

namespace ChordFlow.Instruments.Guitar;

/// <summary>
/// The single dispatch from a <see cref="VoicingFamily"/> to its derivation (shell-voicing-derivation): one
/// engine grip for a (family, quality, shape) at a root within a fret window. <see cref="VoicingFamily.Caged"/>
/// is the full derived chord; <see cref="VoicingFamily.DoubledShell"/> is that chord minus the 5th; and
/// <see cref="VoicingFamily.Shell"/> is the 2-form compact shell. Shared by the comping resolver and the
/// Content-preview doc so the family→derivation mapping never drifts. Throws the usual derivation exceptions
/// (no anchor / unspellable) when no clean grip exists in the window — that is the caller's region filter.
/// </summary>
public static class FamilyVoicing
{
    /// <summary>Derive the <paramref name="family"/> grip for <paramref name="quality"/> in <paramref name="shape"/>.</summary>
    public static ChordShape Derive(
        VoicingFamily family, Quality quality, CagedShape shape, PitchClass root, int minFret, int maxFret) => family switch
        {
            VoicingFamily.Caged => CagedDerivation.Derive(quality, shape, root, minFret, maxFret),
            VoicingFamily.DoubledShell => ShellReduction.MuteFifth(
                CagedDerivation.Derive(quality, shape, root, minFret, maxFret)),
            VoicingFamily.Shell => ShellDerivation.Derive(quality, shape, root, minFret, maxFret),
            _ => throw new ArgumentOutOfRangeException(nameof(family), family, "Unknown voicing family."),
        };
}
