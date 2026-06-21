namespace ChordFlow.Music.Harmony;

/// <summary>
/// Builds a diatonic seventh chord by stacking scale thirds on a <see cref="ScaleDegree"/>.
/// The quality is <b>derived</b> from the scale (root, 3rd, 5th, 7th picked from the scale and
/// matched back to a <see cref="Quality"/>), so a major scale yields <c>I maj7 .. vii m7b5</c>
/// automatically — never hand-authored per degree.
/// </summary>
public static class DiatonicChord
{
    /// <summary>
    /// The diatonic 7th chord rooted on <paramref name="degree"/> of <paramref name="scale"/>.
    /// </summary>
    public static Chord Build(Scale scale, ScaleDegree degree)
    {
        ArgumentNullException.ThrowIfNull(scale);

        int count = scale.Count;
        int rootIndex = degree.Number - 1;
        if (rootIndex < 0 || rootIndex >= count)
        {
            throw new ArgumentOutOfRangeException(
                nameof(degree), degree.Number, $"Scale degree {degree.Number} is out of the supported range 1..{count}.");
        }

        // Stack thirds: scale positions rootIndex, +2, +4, +6. Wrapping past the top of the scale
        // adds an octave (12 semitones) so the stack stays strictly ascending.
        int rootSemitone = scale.Intervals[rootIndex];
        var intervals = new int[4];
        for (int k = 0; k < 4; k++)
        {
            int idx = rootIndex + (2 * k);
            int octave = (idx / count) * 12;
            int semitone = scale.Intervals[idx % count] + octave;
            intervals[k] = semitone - rootSemitone; // interval from the chord root, strictly ascending
        }

        var root = scale.DegreePitchClass(degree.Number);
        Quality quality = QualityIntervals.FromIntervals(intervals);
        return new Chord(root, quality);
    }
}
