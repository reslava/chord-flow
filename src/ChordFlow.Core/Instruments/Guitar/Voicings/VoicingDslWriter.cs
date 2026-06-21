using System.Text;

using ChordFlow.Domain;

namespace ChordFlow.Instruments.Guitar;

/// <summary>
/// Serializes a <see cref="VoicingShape"/> back to its canonical DSL line — the inverse of
/// <see cref="VoicingDslParser"/>. Because the shape is already normalized to the C anchor, the emitted
/// line is the <b>canonical-C</b> form (the string actually persisted, so any anchor an author typed
/// collapses to one stored representation). Round-trips: <c>Parse(ToDsl(shape))</c> reproduces
/// <paramref name="shape"/>.
/// </summary>
public static class VoicingDslWriter
{
    // Canonical quality suffix per quality — each is accepted by VoicingDslParser, so the output re-parses.
    private static readonly IReadOnlyDictionary<Quality, string> Suffixes =
        new Dictionary<Quality, string>
        {
            [Quality.Major] = "maj",
            [Quality.Minor] = "min",
            [Quality.Dominant7] = "7",
            [Quality.Major7] = "maj7",
            [Quality.Minor7] = "min7",
            [Quality.HalfDiminished7] = "m7b5",
            [Quality.Diminished] = "dim",
            [Quality.Diminished7] = "dim7",
            [Quality.Augmented] = "aug",
        };

    /// <summary>The canonical-C DSL line for <paramref name="shape"/>.</summary>
    public static string ToDsl(VoicingShape shape)
    {
        ArgumentNullException.ThrowIfNull(shape);

        var muted = shape.Canonical.MutedStrings is { } m ? new HashSet<int>(m) : new HashSet<int>();
        var fretByString = shape.Canonical.Positions.ToDictionary(p => p.String, p => p.Fret);

        var frets = new StringBuilder();
        for (int stringNumber = Fretboard.StringCount; stringNumber >= 1; stringNumber--) // s6 → s1
        {
            if (stringNumber < Fretboard.StringCount)
            {
                frets.Append(' ');
            }

            frets.Append(muted.Contains(stringNumber) || !fretByString.ContainsKey(stringNumber)
                ? "x"
                : fretByString[stringNumber].ToString(System.Globalization.CultureInfo.InvariantCulture));
        }

        string anchor = shape.Anchor is { } a ? $"anchor:{AnchorLetter(a)} " : string.Empty;
        return $"voicing C{Suffixes[shape.Quality]} shape:{shape.Shape} root:{shape.RootString} {anchor}frets: {frets}";
    }

    private static char AnchorLetter(Finger finger) => finger switch
    {
        Finger.Index => 'i',
        Finger.Middle => 'm',
        Finger.Ring => 'r',
        Finger.Pinky => 'p',
        _ => throw new ArgumentOutOfRangeException(nameof(finger), finger, "No DSL letter for this finger."),
    };
}
