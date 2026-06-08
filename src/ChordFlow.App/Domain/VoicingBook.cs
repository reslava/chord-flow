namespace ChordFlow.Domain;

/// <summary>
/// Hand-authored voicing table — the one place literal frets live. For the MVP this
/// holds beginner <b>shell voicings</b> for dominant-7 chords (root + major 3rd + minor
/// 7th, 5th omitted) on the A/D/G strings (alphaTab strings 5/4/3).
/// </summary>
/// <remarks>
/// Authored for the three chords of the Bb 12-bar blues (I=Bb7, IV=Eb7, V=F7). The
/// pitch-class content of every voicing is unit-verified (it provably spells the chord);
/// exact fret ergonomics are still to be confirmed in the alphaTab playground before the
/// data is considered locked. Other keys are added as more authored rows, not code changes.
/// </remarks>
public static class VoicingBook
{
    private static readonly Dictionary<(int Root, Quality Quality), Voicing> BeginnerVoicings = new()
    {
        // Bb7 — Bb(5,1) D(4,0) Ab(3,1)
        [(10, Quality.Dominant7)] = new Voicing(new FretPosition[] { new(5, 1), new(4, 0), new(3, 1) }),
        // Eb7 — Eb(5,6) G(4,5) Db(3,6)
        [(3, Quality.Dominant7)] = new Voicing(new FretPosition[] { new(5, 6), new(4, 5), new(3, 6) }),
        // F7 — F(5,8) A(4,7) Eb(3,8)
        [(5, Quality.Dominant7)] = new Voicing(new FretPosition[] { new(5, 8), new(4, 7), new(3, 8) }),
    };

    /// <summary>
    /// Returns the authored <see cref="Voicing"/> for <paramref name="chord"/> at the given
    /// <paramref name="difficulty"/>. Throws if the difficulty is unsupported or no voicing
    /// has been authored for the chord.
    /// </summary>
    public static Voicing Lookup(Chord chord, Difficulty difficulty)
    {
        ArgumentNullException.ThrowIfNull(chord);

        if (difficulty != Difficulty.Beginner)
        {
            throw new NotSupportedException(
                $"Only Beginner voicings are authored for the MVP; got {difficulty}.");
        }

        if (BeginnerVoicings.TryGetValue((chord.Root.Value, chord.Quality), out Voicing? voicing))
        {
            return voicing;
        }

        throw new KeyNotFoundException(
            $"No beginner voicing authored for a {chord.Quality} chord with root pitch class " +
            $"{chord.Root.Value}. The MVP authors Bb7, Eb7 and F7 (the Bb 12-bar blues).");
    }
}
