namespace ChordFlow.Domain;

/// <summary>
/// The first voicing strategy: beginner <b>shell voicings</b> (root + 3rd + minor 7th; 5th omitted)
/// for dominant-7 and minor-7 chords as a single <b>movable shape</b> on the A/D/G strings (alphaTab
/// strings 5/4/3). The shape slides to any root, so all 12 keys are covered by one rule instead of a
/// per-chord table — the algorithmic shape that previously lived inline in <see cref="VoicingBook"/>,
/// now the Beginner <see cref="IVoicingStrategy"/>.
/// </summary>
/// <remarks>
/// Shape: with the root at fret <c>R</c> on the A string, the voicing is <c>(s5:R, s4:R+t, s3:R)</c> —
/// root on A, the 3rd on D, minor 7th on G at the same fret. The D-string offset <c>t</c> is the 3rd's
/// distance below the octave-root: <c>-1</c> for a Dominant7 (major 3rd), <c>-2</c> for a Minor7 (minor
/// 3rd). <c>R</c> is taken so the 3rd never needs a negative fret. Bb7/Eb7/F7 fall out as
/// <c>(1,0,1)/(6,5,6)/(8,7,8)</c> exactly as the previously hand-authored rows did. Strings 1/2/6 are
/// unplayed; the diagram metadata records them as muted and the lowest fret as the diagram's first fret.
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

        // The D-string offset selects the 3rd: major (−1) for Dominant7, minor (−2) for Minor7. The
        // minor 7th (G string at the root fret) is shared by both shells.
        int thirdOffset = chord.Quality switch
        {
            Quality.Dominant7 => -1,
            Quality.Minor7 => -2,
            _ => throw new NotSupportedException(
                $"The MVP shell shape covers Dominant7 and Minor7 only; got {chord.Quality}."),
        };

        int root = Mod12(chord.Root.Value);

        // Root fret on the A string, lifted an octave when needed so the 3rd (R + thirdOffset) never
        // goes negative and the shape stays contiguous.
        int r = Mod12(root - OpenAPitchClass);
        if (r < -thirdOffset)
        {
            r += 12;
        }

        var positions = new FretPosition[]
        {
            new(AString, r),                // root
            new(DString, r + thirdOffset),  // 3rd (major for Dominant7, minor for Minor7)
            new(GString, r),                // minor 7th
        };

        // Diagram hints (presentation only): the diagram starts at the lowest fret used, and the
        // strings the shape does not touch (high E, B, low E) are muted.
        int firstFret = Math.Min(r + thirdOffset, r);
        var mutedStrings = new[] { 1, 2, 6 };

        return new Voicing(positions, BarreFret: null, FirstFret: firstFret, MutedStrings: mutedStrings);
    }

    private static int Mod12(int value) => ((value % 12) + 12) % 12;
}
