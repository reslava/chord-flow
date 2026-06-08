namespace ChordFlow.Domain;

/// <summary>
/// A standard-tuning guitar fretboard: maps a <see cref="PitchClass"/> to every <see cref="FretPosition"/>
/// that sounds it within a fret range. Pure geometry — no UI (ctx EX5). String numbers follow alphaTab
/// (1 = high E .. 6 = low E).
/// </summary>
public static class Fretboard
{
    // Open-string pitch classes indexed by alphaTab string number; index 0 is unused so the string
    // number indexes directly. Standard tuning: E A D G B E.
    private static readonly int[] OpenPitchClass = { 0, 4, 11, 7, 2, 9, 4 };

    /// <summary>Number of strings (6, standard guitar).</summary>
    public const int StringCount = 6;

    /// <summary>Default highest fret considered when resolving positions.</summary>
    public const int DefaultMaxFret = 12;

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
                if ((OpenPitchClass[stringNumber] + fret) % 12 == target)
                {
                    positions.Add(new FretPosition(stringNumber, fret));
                }
            }
        }

        return positions;
    }

    private static int Mod12(int value) => ((value % 12) + 12) % 12;
}
