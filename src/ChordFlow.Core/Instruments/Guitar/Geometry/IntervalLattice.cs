using ChordFlow.Music.Harmony;

namespace ChordFlow.Instruments.Guitar;

/// <summary>
/// The guitar projection of the <see cref="IntervalSpeller"/> interval vocabulary onto the fretboard — the
/// base primitive the CAGED engine queries. Built entirely on <see cref="Fretboard"/>'s octave-preserving
/// absolute coordinate: the canonical value is a <b>signed semitone distance</b>, and interval labels are
/// two thin views over it (a direction-free pitch-class label and the octave-aware <see cref="LatticeInterval"/>),
/// both routed through <see cref="IntervalSpeller.Name"/> so the vocabulary is never re-authored here.
/// Pure geometry — no I/O, no UI. The unison/octave special case of this lattice is the octave-shapes root map.
/// </summary>
public static class IntervalLattice
{
    /// <summary>The octave-preserving absolute semitone coordinate of <paramref name="position"/> — a delegate to
    /// <see cref="Fretboard.AbsoluteSemitone"/> (the lattice authors no tuning of its own).</summary>
    public static int Absolute(FretPosition position) =>
        Fretboard.AbsoluteSemitone(position.String, position.Fret);

    /// <summary>The signed semitone distance from <paramref name="origin"/> to <paramref name="target"/> —
    /// positive if the target is higher in pitch. The canonical value; everything else is a view over it.</summary>
    public static int Distance(FretPosition origin, FretPosition target) =>
        Absolute(target) - Absolute(origin);

    /// <summary>The direction-free, pitch-class interval label of a signed <paramref name="distance"/>
    /// (<c>1 … 7</c>) — the everyday fretboard "what interval is this vs. the root." Octaves fold away.</summary>
    public static string PitchClassLabel(int distance) =>
        IntervalSpeller.Name(((distance % 12) + 12) % 12);

    /// <summary>The octave-aware description of a signed <paramref name="distance"/>: the unfolded label
    /// (<c>8, 9 … 15</c>) plus octave count and direction — for scales / arpeggios and the dogfood UI.</summary>
    public static LatticeInterval Describe(int distance)
    {
        int magnitude = Math.Abs(distance);
        return new LatticeInterval(distance, IntervalSpeller.Name(magnitude), magnitude / 12, Math.Sign(distance));
    }

    /// <summary>The octave-aware interval of <paramref name="target"/> measured from <paramref name="root"/> —
    /// convenience over <see cref="Distance"/> + <see cref="Describe"/> (powers the dogfood UI's per-fret labels).</summary>
    public static LatticeInterval LabelAt(FretPosition root, FretPosition target) =>
        Describe(Distance(root, target));

    /// <summary>
    /// Every <see cref="FretPosition"/> in the fret window [<paramref name="minFret"/>, <paramref name="maxFret"/>]
    /// that sits <paramref name="semitones"/> from <paramref name="root"/> by <b>pitch class</b> (all octaves of the
    /// degree in range). The engine query for placing a quality's tones in a neck zone. Implemented on top of
    /// <see cref="Fretboard.PositionsFor"/> — no second neck-walk.
    /// </summary>
    public static IReadOnlyList<FretPosition> PositionsOfInterval(
        FretPosition root, int semitones, int minFret, int maxFret)
    {
        if (minFret < 0) throw new ArgumentOutOfRangeException(nameof(minFret));
        if (maxFret < minFret) throw new ArgumentOutOfRangeException(nameof(maxFret));

        int rootPc = Fretboard.PitchClassAt(root.String, root.Fret).Value;
        int targetPc = ((rootPc + semitones) % 12 + 12) % 12;

        var result = new List<FretPosition>();
        foreach (FretPosition position in Fretboard.PositionsFor(new PitchClass(targetPc), maxFret))
        {
            if (position.Fret >= minFret)
            {
                result.Add(position);
            }
        }

        return result;
    }
}
