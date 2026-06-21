using ChordFlow.Music.Harmony;

namespace ChordFlow.Instruments.Guitar;

/// <summary>
/// A standard-tuning guitar fretboard: maps a <see cref="PitchClass"/> to every <see cref="FretPosition"/>
/// that sounds it within a fret range. Pure geometry — no UI (ctx EX5). String numbers follow alphaTab
/// (1 = high E .. 6 = low E).
/// </summary>
/// <remarks>
/// The single source of tuning is the <b>octave-preserving</b> <see cref="StringSemitoneBase"/>: absolute
/// semitones of each open string measured from the open low E. The mod-12 pitch-class lookups
/// (<see cref="PitchClassAt"/>, <see cref="PositionsFor"/>) are <i>derived</i> from it — there is no second
/// tuning table to drift. <see cref="IntervalLattice"/> consumes <see cref="AbsoluteSemitone"/> and authors
/// no tuning of its own.
/// </remarks>
public static class Fretboard
{
    // Absolute semitone of each open string, measured from the open low E (string 6 = 0). Octave-preserving
    // (NOT mod-12), indexed by alphaTab string number; index 0 is unused so the string number indexes
    // directly. Standard tuning E A D G B E: the only irregular step is string 3 -> 2 (+4, the B string).
    private static readonly int[] StringSemitoneBase = { 0, 24, 19, 15, 10, 5, 0 };

    // Pitch class of the open low E (string 6 absolute 0) — the origin the absolute coordinate is reduced
    // against to recover a mod-12 pitch class.
    private const int LowEPitchClass = 4;

    /// <summary>Number of strings (6, standard guitar).</summary>
    public const int StringCount = 6;

    /// <summary>Default highest fret considered when resolving positions.</summary>
    public const int DefaultMaxFret = 12;

    /// <summary>
    /// The <b>octave-preserving</b> absolute semitone coordinate of <paramref name="stringNumber"/>
    /// (1 = high E .. 6 = low E) at <paramref name="fret"/> (0 = open), measured from the open low E.
    /// The single source of tuning — pitch-class lookups and <see cref="IntervalLattice"/> derive from it.
    /// </summary>
    public static int AbsoluteSemitone(int stringNumber, int fret)
    {
        if (stringNumber < 1 || stringNumber > StringCount)
            throw new ArgumentOutOfRangeException(nameof(stringNumber));
        if (fret < 0) throw new ArgumentOutOfRangeException(nameof(fret));

        return StringSemitoneBase[stringNumber] + fret;
    }

    /// <summary>
    /// Every <see cref="FretPosition"/> from fret 0..<paramref name="maxFret"/> that sounds
    /// <paramref name="pitchClass"/>, ordered by string then fret.
    /// </summary>
    public static IReadOnlyList<FretPosition> PositionsFor(PitchClass pitchClass, int maxFret = DefaultMaxFret)
    {
        if (maxFret < 0) throw new ArgumentOutOfRangeException(nameof(maxFret));

        int target = Mod12(pitchClass.Value);
        var positions = new List<FretPosition>();

        for (int stringNumber = 1; stringNumber <= StringCount; stringNumber++)
        {
            for (int fret = 0; fret <= maxFret; fret++)
            {
                if (Mod12(LowEPitchClass + AbsoluteSemitone(stringNumber, fret)) == target)
                {
                    positions.Add(new FretPosition(stringNumber, fret));
                }
            }
        }

        return positions;
    }

    /// <summary>
    /// The <see cref="PitchClass"/> sounding on <paramref name="stringNumber"/> (1 = high E .. 6 = low E) at
    /// <paramref name="fret"/> (0 = open). The inverse of <see cref="PositionsFor"/> — used to label a voicing's
    /// notes by their interval against a chord root. Derived from <see cref="AbsoluteSemitone"/>.
    /// </summary>
    public static PitchClass PitchClassAt(int stringNumber, int fret) =>
        new(Mod12(LowEPitchClass + AbsoluteSemitone(stringNumber, fret)));

    private static int Mod12(int value) => ((value % 12) + 12) % 12;
}
