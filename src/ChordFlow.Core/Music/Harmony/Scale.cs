namespace ChordFlow.Music.Harmony;

/// <summary>
/// A scale: a tonic pitch class plus its semitone intervals from that tonic (e.g. major
/// <c>{0,2,4,5,7,9,11}</c>). First-class so transposition and diatonic generation read the
/// offsets from here instead of <see cref="Transposer"/> owning hardcoded arrays. Modes and
/// pentatonics slot in later as more interval sets.
/// </summary>
public sealed record Scale(PitchClass Tonic, IReadOnlyList<int> Intervals)
{
    /// <summary>Semitone offsets of the major scale (Ionian).</summary>
    public static readonly IReadOnlyList<int> MajorIntervals = new[] { 0, 2, 4, 5, 7, 9, 11 };

    /// <summary>Semitone offsets of the natural minor scale (Aeolian).</summary>
    public static readonly IReadOnlyList<int> NaturalMinorIntervals = new[] { 0, 2, 3, 5, 7, 8, 10 };

    /// <summary>The major scale rooted at <paramref name="tonic"/>.</summary>
    public static Scale Major(PitchClass tonic) => new(tonic, MajorIntervals);

    /// <summary>The natural-minor scale rooted at <paramref name="tonic"/>.</summary>
    public static Scale NaturalMinor(PitchClass tonic) => new(tonic, NaturalMinorIntervals);

    /// <summary>The scale that matches a <see cref="Key"/>'s mode, rooted at its tonic.</summary>
    public static Scale ForKey(Key key)
    {
        ArgumentNullException.ThrowIfNull(key);
        return key.IsMinor ? NaturalMinor(key.Tonic) : Major(key.Tonic);
    }

    /// <summary>The number of notes in the scale.</summary>
    public int Count => Intervals.Count;

    /// <summary>
    /// The concrete pitch class of the 1-based scale <paramref name="degree"/> (1 = tonic),
    /// within the octave. Throws if the degree is outside <c>1..Count</c>.
    /// </summary>
    public PitchClass DegreePitchClass(int degree)
    {
        int index = degree - 1;
        if (index < 0 || index >= Intervals.Count)
        {
            throw new ArgumentOutOfRangeException(
                nameof(degree), degree, $"Scale degree {degree} is out of the supported range 1..{Intervals.Count}.");
        }

        return new PitchClass(Mod12(Tonic.Value + Intervals[index]));
    }

    private static int Mod12(int value) => ((value % 12) + 12) % 12;
}
