namespace ChordFlow.Domain;

/// <summary>
/// The first voicing strategy: beginner <b>shell voicings</b> (root + major 3rd + minor 7th;
/// 5th omitted) for dominant-7 chords as a single <b>movable shape</b> on the A/D/G strings
/// (alphaTab strings 5/4/3). The shape slides to any root, so all 12 keys are covered by one rule
/// instead of a per-chord table — the algorithmic shape that previously lived inline in
/// <see cref="VoicingBook"/>, now the Beginner <see cref="IVoicingStrategy"/>.
/// </summary>
/// <remarks>
/// Shape: with the root at fret <c>R</c> on the A string, the voicing is
/// <c>(s5:R, s4:R-1, s3:R)</c> — root on A, major 3rd on D one fret lower, minor 7th on G at the
/// same fret. <c>R</c> is taken in 1..12 so the three notes stay on adjacent frets and never need a
/// negative fret. Bb7/Eb7/F7 fall out as <c>(1,0,1)/(6,5,6)/(8,7,8)</c> exactly as the previously
/// hand-authored rows did. Strings 1/2/6 are unplayed; the diagram metadata records them as muted
/// and the lowest fret as the diagram's first fret.
/// </remarks>
public sealed class BeginnerShellStrategy : IVoicingStrategy
{
    // alphaTab string numbers for the shell shape (1 = high E .. 6 = low E).
    private const int AString = 5;
    private const int DString = 4;
    private const int GString = 3;

    // Open A string pitch class (A = 9); the root fret on the A string is measured from it.
    private const int OpenAPitchClass = 9;

    public Difficulty Difficulty => Difficulty.Beginner;

    public Voicing Voice(Chord chord)
    {
        ArgumentNullException.ThrowIfNull(chord);

        if (chord.Quality != Quality.Dominant7)
        {
            throw new NotSupportedException(
                $"The MVP shell shape covers Dominant7 only; got {chord.Quality}.");
        }

        int root = Mod12(chord.Root.Value);

        // Root fret on the A string, kept in 1..12 so the 3rd (R-1) never goes negative
        // and the shape stays contiguous.
        int r = Mod12(root - OpenAPitchClass);
        if (r < 1)
        {
            r += 12;
        }

        var positions = new FretPosition[]
        {
            new(AString, r),      // root
            new(DString, r - 1),  // major 3rd
            new(GString, r),      // minor 7th
        };

        // Diagram hints (presentation only): the diagram starts at the lowest fret used, and the
        // strings the shape does not touch (high E, B, low E) are muted.
        int firstFret = Math.Min(r - 1, r);
        var mutedStrings = new[] { 1, 2, 6 };

        return new Voicing(positions, BarreFret: null, FirstFret: firstFret, MutedStrings: mutedStrings);
    }

    private static int Mod12(int value) => ((value % 12) + 12) % 12;
}
