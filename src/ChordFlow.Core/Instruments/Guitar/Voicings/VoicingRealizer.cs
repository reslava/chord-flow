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

    /// <summary>
    /// Realize a literal <see cref="GripSpec"/> (a per-chord <c>{…}</c> annotation or a <c>voice</c> default)
    /// to a movable <see cref="Voicing"/> sounding at <paramref name="targetRoot"/> (design D1/D8; req
    /// <c>IN3</c>/<c>IN11</c>/<c>C3</c>/<c>C9</c>). The grip is anchored (explicit <c>root:</c> clause, else the
    /// bass = lowest-pitched sounded string), the anchor's pitch class is pivoted onto the target root, and the
    /// whole shape octave-folds to its lowest non-negative placement — identical to <see cref="Realize"/>. A
    /// <b>phantom</b> anchor (<c>root:S@F</c>) lets a rootless grip transpose without sounding its root.
    /// Returns <c>null</c> when no octave placement fits the 0..15 window; throws <see cref="FormatException"/>
    /// when the anchor is unusable (a voiced <c>root:</c> on a muted string, or no sounded string at all).
    /// </summary>
    public static Voicing? RealizeGrip(GripSpec grip, PitchClass targetRoot)
    {
        ArgumentNullException.ThrowIfNull(grip);

        (int rootString, int rootFret) = ResolveGripAnchor(grip);
        int anchorPc = Fretboard.PitchClassAt(rootString, rootFret).Value;

        // Pivot the anchor's pitch class onto the target root (0..11 up-shift), then octave-fold the shape to
        // its lowest non-negative placement — the same normalize-to-C + slide the authored voicings use.
        int shift = Mod12(targetRoot.Value - anchorPc);

        var shifted = grip.Positions
            .Select(p => new FretPosition(p.String, p.Fret + shift))
            .ToList();

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
            return null;
        }

        return new Voicing(
            shifted,
            BarreFret: null,
            FirstFret: lo,
            MutedStrings: grip.MutedStrings.Count > 0 ? grip.MutedStrings : null);
    }

    // The (string, fret) whose pitch class anchors the grip's transpose. An explicit `root:` clause wins:
    // `root:S@F` is a phantom root (S may be muted — the rootless case), `root:S` reads the fret off the grip
    // on string S (muted → error). With no clause, the bass (lowest-pitched sounded string = highest string
    // number) is the root — correct for root-position grips; inversions/rootless voicings must declare `root:`.
    private static (int RootString, int RootFret) ResolveGripAnchor(GripSpec grip)
    {
        if (grip.Anchor is { } anchor)
        {
            if (anchor.Fret is { } phantom)
            {
                return (anchor.String, phantom);
            }

            foreach (FretPosition p in grip.Positions)
            {
                if (p.String == anchor.String)
                {
                    return (anchor.String, p.Fret);
                }
            }

            throw new FormatException(
                $"Voicing grip root string {anchor.String} is muted — declare a phantom root as root:{anchor.String}@<fret>.");
        }

        for (int s = Fretboard.StringCount; s >= 1; s--)
        {
            foreach (FretPosition p in grip.Positions)
            {
                if (p.String == s)
                {
                    return (s, p.Fret);
                }
            }
        }

        throw new FormatException("Voicing grip has no sounded string to anchor the root.");
    }

    private static int Mod12(int value) => ((value % 12) + 12) % 12;
}
