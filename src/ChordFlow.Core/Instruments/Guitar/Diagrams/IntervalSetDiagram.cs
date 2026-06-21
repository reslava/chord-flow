using ChordFlow.Music.Harmony;

namespace ChordFlow.Instruments.Guitar;

/// <summary>
/// The scale / interval-set <b>producer</b> of the general <see cref="FretboardDiagram"/> carrier: given an
/// interval set (e.g. <c>"1 b3 4 5 b7"</c>) and a root pitch class, it lights every occurrence of each degree
/// across the neck — the guitar projection that powers the Scales dogfood page for <see cref="IntervalLattice"/>.
/// Theory stays upstream: it parses the set through <see cref="IntervalSpeller"/> and places each degree through
/// <see cref="IntervalLattice"/>, adding no geometry or vocabulary of its own. Unlike a voicing it has <b>many
/// notes per string</b> and no muted strings. The window is left open so the view auto-fits to the markers
/// (root-note + auto-window; no root-fret picker in v1).
/// </summary>
public static class IntervalSetDiagram
{
    private static readonly char[] Separators = { ' ', '\t', '\n', '\r', ',' };

    /// <summary>
    /// Build the diagram for <paramref name="intervals"/> (whitespace/comma-separated tokens such as
    /// <c>"1 b3 4 5 b7"</c>) rooted at <paramref name="root"/>, across frets <c>0..<paramref name="maxFret"/></c>.
    /// Each distinct token becomes one degree lit at every position in range; each marker keeps the user's typed
    /// token as its interval label (a typed <c>#4</c> reads <c>#4</c>, not its enharmonic flat). The diagram's
    /// fret window is left null so the view auto-fits.
    /// </summary>
    /// <exception cref="FormatException">A token is not a valid interval label (see <see cref="IntervalSpeller.Parse"/>).</exception>
    public static FretboardDiagram Build(string intervals, PitchClass root, int maxFret = 15)
    {
        ArgumentNullException.ThrowIfNull(intervals);
        if (maxFret < 0) throw new ArgumentOutOfRangeException(nameof(maxFret));

        var key = new Key(root, IsMinor: false);                            // spells the note names for this root
        List<string> tokens = intervals
            .Split(Separators, StringSplitOptions.RemoveEmptyEntries)
            .Distinct()
            .ToList();

        var markers = new List<FretboardMarker>();
        if (tokens.Count > 0)
        {
            FretPosition anchor = Fretboard.PositionsFor(root, maxFret)[0]; // any position of the root pitch class
            foreach (string token in tokens)
            {
                int semitone = IntervalSpeller.Parse(token);
                string function = FunctionFor(((semitone % 12) + 12) % 12);
                foreach (FretPosition pos in IntervalLattice.PositionsOfInterval(anchor, semitone, 0, maxFret))
                {
                    PitchClass notePc = Fretboard.PitchClassAt(pos.String, pos.Fret);
                    markers.Add(new FretboardMarker(
                        pos.String,
                        pos.Fret,
                        NoteSpeller.Name(notePc, key),
                        token,                 // the user's own spelling, preserved
                        function,
                        MarkerShape.Circle));
                }
            }
        }

        string rootName = NoteSpeller.Name(root, key);
        string title = tokens.Count == 0 ? rootName : $"{rootName} — {string.Join(" ", tokens)}";
        return new FretboardDiagram(
            title, markers, MutedStrings: Array.Empty<int>(), BarreFret: null, FretMin: null, FretMax: null);
    }

    // Default color bucket for a scale degree — used only if rendered without an override palette (the Scales page
    // supplies its own root-red/rest-black palette). Mirrors the chord-tone function buckets so a bare render still
    // reads musically: root / third / fifth / seventh by pitch class, everything else a tension.
    private static string FunctionFor(int pitchClass) => pitchClass switch
    {
        0 => "root",
        3 or 4 => "third",
        6 or 7 or 8 => "fifth",
        9 or 10 or 11 => "seventh",
        _ => "tension",
    };
}
