namespace ChordFlow.Domain;

/// <summary>
/// Pure transposition: realizes a key-independent <see cref="Progression"/> into concrete
/// <see cref="Chord"/>s for a given <see cref="Key"/> (or <see cref="Scale"/>). No I/O, no state.
/// The scale-degree offsets now live in <see cref="Scale"/>; this type just maps degrees through it.
/// </summary>
public static class Transposer
{
    /// <summary>
    /// Maps each <see cref="RomanDegree"/> of <paramref name="progression"/> to a concrete chord in
    /// <paramref name="key"/>, using the key's major/natural-minor <see cref="Scale"/>.
    /// </summary>
    public static Chord[] Realize(Progression progression, Key key)
    {
        ArgumentNullException.ThrowIfNull(key);
        return Realize(progression, Scale.ForKey(key));
    }

    /// <summary>
    /// Maps each <see cref="RomanDegree"/> of <paramref name="progression"/> to a concrete chord in
    /// <paramref name="scale"/>. The root pitch class is the scale degree's pitch class; the quality
    /// is carried straight through from the degree (e.g. Dominant7 for blues).
    /// </summary>
    public static Chord[] Realize(Progression progression, Scale scale)
    {
        ArgumentNullException.ThrowIfNull(progression);
        ArgumentNullException.ThrowIfNull(scale);

        var chords = new Chord[progression.Degrees.Count];
        for (int i = 0; i < progression.Degrees.Count; i++)
        {
            RomanDegree degree = progression.Degrees[i];
            chords[i] = new Chord(scale.DegreePitchClass(degree.Degree), degree.Quality);
        }

        return chords;
    }
}
