using ChordFlow.Music.Harmony;

namespace ChordFlow.Instruments.Guitar;

/// <summary>
/// Slides a canonical-C <see cref="VoicingShape"/> to a target root — the movable transpose that turns
/// one authored shape into all 12 chords. Pure geometry over <see cref="PitchClass"/> + frets; no
/// first-class interval type (that is the deferred <c>domain/intervals</c> work).
/// </summary>
public static class VoicingRealizer
{
    /// <summary>Highest fret a realized voicing may use — the 0..15 playable window.</summary>
    public const int MaxFret = 15;

    /// <summary>
    /// The <see cref="Voicing"/> for <paramref name="shape"/> sounded at <paramref name="targetRoot"/>:
    /// every fretted string shifts by the C→root interval, then the whole shape octave-folds to its
    /// lowest non-negative placement. Returns <c>null</c> when no octave placement fits the 0..15 window.
    /// Muted strings stay muted; open strings ride the shift (the open ↔ barre identity).
    /// </summary>
    public static Voicing? Realize(this VoicingShape shape, PitchClass targetRoot)
    {
        ArgumentNullException.ThrowIfNull(shape);

        int semis = Mod12(targetRoot.Value); // canonical is C-anchored (pc 0); interval C→target is 0..11

        var shifted = shape.Canonical.Positions
            .Select(p => new FretPosition(p.String, p.Fret + semis))
            .ToList();

        // Octave-fold uniformly so the lowest fret sits in [0, 11] — the lowest placement on the neck.
        int min = shifted.Min(p => p.Fret);
        int fold = 0;
        while (min + fold >= 12)
        {
            fold -= 12;
        }

        while (min + fold < 0)
        {
            fold += 12;
        }

        if (fold != 0)
        {
            shifted = shifted.Select(p => new FretPosition(p.String, p.Fret + fold)).ToList();
        }

        int lo = shifted.Min(p => p.Fret);
        int hi = shifted.Max(p => p.Fret);
        if (hi > MaxFret)
        {
            // The shape spans past the 15th fret even at its lowest placement — no octave fits.
            return null;
        }

        return new Voicing(
            shifted,
            BarreFret: null,
            FirstFret: lo,
            MutedStrings: shape.Canonical.MutedStrings);
    }

    private static int Mod12(int value) => ((value % 12) + 12) % 12;
}
