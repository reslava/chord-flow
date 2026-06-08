namespace ChordFlow.Domain;

/// <summary>
/// Pure transposition: realizes a key-independent <see cref="Progression"/> into
/// concrete <see cref="Chord"/>s for a given <see cref="Key"/>. No I/O, no state.
/// </summary>
public static class Transposer
{
    // Semitone offsets from the tonic for scale degrees 1..7.
    private static readonly int[] MajorOffsets = { 0, 2, 4, 5, 7, 9, 11 };
    private static readonly int[] NaturalMinorOffsets = { 0, 2, 3, 5, 7, 8, 10 };

    /// <summary>
    /// Maps each <see cref="RomanDegree"/> of <paramref name="progression"/> to a
    /// concrete chord in <paramref name="key"/>. The root pitch class is the tonic
    /// shifted by the scale-degree offset (major or natural-minor per the key); the
    /// quality is carried straight through from the degree (e.g. Dominant7 for blues).
    /// </summary>
    public static Chord[] Realize(Progression progression, Key key)
    {
        ArgumentNullException.ThrowIfNull(progression);
        ArgumentNullException.ThrowIfNull(key);

        var offsets = key.IsMinor ? NaturalMinorOffsets : MajorOffsets;
        var chords = new Chord[progression.Degrees.Count];

        for (int i = 0; i < progression.Degrees.Count; i++)
        {
            RomanDegree degree = progression.Degrees[i];
            int index = degree.Degree - 1;
            if (index < 0 || index >= offsets.Length)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(progression),
                    $"Scale degree {degree.Degree} is out of the supported range 1..7.");
            }

            int root = Mod12(key.Tonic.Value + offsets[index]);
            chords[i] = new Chord(new PitchClass(root), degree.Quality);
        }

        return chords;
    }

    private static int Mod12(int value) => ((value % 12) + 12) % 12;
}
