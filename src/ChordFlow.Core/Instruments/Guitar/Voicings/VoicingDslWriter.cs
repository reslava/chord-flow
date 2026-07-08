using ChordFlow.Music.Harmony;
using System.Globalization;
using System.Text;


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
            [Quality.Major6] = "6",
            [Quality.Minor6] = "m6",
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

    /// <summary>
    /// Serialize a <see cref="VoicingSpec"/> to its canonical inner text — the value that sits inside a
    /// per-chord <c>{…}</c> annotation or after <c>voice &lt;selector&gt; =</c> (req <c>IN8</c>). A grip emits
    /// its six frets (bare, low-E→high-E) plus any <c>root:</c> anchor; a reference emits <c>&lt;source&gt;: &lt;id&gt;</c>.
    /// Round-trips: <c>ParseSpec(SpecToDsl(spec))</c> reproduces <paramref name="spec"/>.
    /// </summary>
    public static string SpecToDsl(VoicingSpec spec)
    {
        ArgumentNullException.ThrowIfNull(spec);

        return spec switch
        {
            ReferenceSpec r => $"{r.Source}: {r.Id}",
            GripSpec g => GripToDsl(g),
            _ => throw new ArgumentOutOfRangeException(nameof(spec), spec, "Unknown voicing-spec form."),
        };
    }

    private static string GripToDsl(GripSpec grip)
    {
        var muted = new HashSet<int>(grip.MutedStrings);
        var fretByString = grip.Positions.ToDictionary(p => p.String, p => p.Fret);

        var sb = new StringBuilder();
        for (int stringNumber = Fretboard.StringCount; stringNumber >= 1; stringNumber--) // s6 → s1
        {
            if (stringNumber < Fretboard.StringCount)
            {
                sb.Append(' ');
            }

            sb.Append(muted.Contains(stringNumber) || !fretByString.ContainsKey(stringNumber)
                ? "x"
                : fretByString[stringNumber].ToString(CultureInfo.InvariantCulture));
        }

        if (grip.Anchor is { } a)
        {
            sb.Append(" root:").Append(a.String.ToString(CultureInfo.InvariantCulture));
            if (a.Fret is { } f)
            {
                sb.Append('@').Append(f.ToString(CultureInfo.InvariantCulture));
            }
        }

        return sb.ToString();
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
