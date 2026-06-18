namespace ChordFlow.Domain;

/// <summary>
/// The voicing <b>producer</b> of the general <see cref="FretboardDiagram"/> carrier — the music-theory side of
/// the voicing preview (IN5/IN6/IN7). For each sounding string it resolves the pitch class (<see cref="Fretboard"/>),
/// its interval against the chord root (canonical anchor = C, so the interval is just the pitch class), the
/// chord-tone function (root/third/fifth/seventh, by the tertian position in <see cref="QualityIntervals"/>) or
/// <c>tension</c> for a note outside the quality, the interval label, and the spelled note (<see cref="NoteSpeller"/>),
/// and emits one <see cref="MarkerShape.Circle"/> marker (fret 0 ⇒ an open marker). Muted strings become
/// diagram-level chrome (<see cref="FretboardDiagram.MutedStrings"/>), not markers. The diagram is shown at the
/// canonical-C anchor (EX2: no root-picker in v1) — movability is a later add.
/// </summary>
public static class VoicingDiagram
{
    private static readonly Key CAnchor = new(new PitchClass(0), IsMinor: false);

    /// <summary>Compute the fretboard diagram for <paramref name="shape"/> at its canonical-C anchor.</summary>
    public static FretboardDiagram Build(VoicingShape shape)
    {
        ArgumentNullException.ThrowIfNull(shape);

        Voicing voicing = shape.Canonical;
        Dictionary<int, int> fretByString = voicing.Positions.ToDictionary(p => p.String, p => p.Fret);
        var muted = voicing.MutedStrings is { } m ? new HashSet<int>(m) : new HashSet<int>();
        Dictionary<int, ChordToneFunction> roleByInterval = RoleByInterval(shape.Quality);

        var markers = new List<FretboardMarker>();
        var mutedStrings = new List<int>();
        for (int s = Fretboard.StringCount; s >= 1; s--) // low-E(6) → high-E(1)
        {
            if (muted.Contains(s) || !fretByString.TryGetValue(s, out int fret))
            {
                mutedStrings.Add(s);
                continue;
            }

            PitchClass pc = Fretboard.PitchClassAt(s, fret);
            int semitone = pc.Value; // root is C (0), so the interval equals the pitch class
            ChordToneFunction? role = roleByInterval.TryGetValue(semitone, out ChordToneFunction f) ? f : null;

            markers.Add(new FretboardMarker(
                s,
                fret,
                NoteSpeller.Name(pc, CAnchor),
                IntervalLabel(semitone, role),
                FunctionName(role),
                MarkerShape.Circle));
        }

        int firstFret = voicing.FirstFret ?? (fretByString.Count == 0 ? 0 : fretByString.Values.Min());
        string title = ChordSymbol.Format(new Chord(new PitchClass(0), shape.Quality), CAnchor);
        return new FretboardDiagram(title, markers, mutedStrings, voicing.BarreFret, FretMin: firstFret, FretMax: null);
    }

    // Map each chord-tone semitone to its function by its tertian position (root, third, fifth, seventh). Every
    // v1 quality is a stacked-thirds set, so index 0/1/2/3 is exactly root/third/fifth/seventh.
    private static Dictionary<int, ChordToneFunction> RoleByInterval(Quality quality)
    {
        var map = new Dictionary<int, ChordToneFunction>();
        IReadOnlyList<int> intervals = QualityIntervals.Intervals(quality);
        for (int i = 0; i < intervals.Count; i++)
        {
            map[intervals[i]] = i switch
            {
                0 => ChordToneFunction.Root,
                1 => ChordToneFunction.Third,
                2 => ChordToneFunction.Fifth,
                _ => ChordToneFunction.Seventh,
            };
        }

        return map;
    }

    private static string FunctionName(ChordToneFunction? role) => role switch
    {
        ChordToneFunction.Root => "root",
        ChordToneFunction.Third => "third",
        ChordToneFunction.Fifth => "fifth",
        ChordToneFunction.Seventh => "seventh",
        _ => "tension",
    };

    // Chord tones get their precise role-aware label (a dim7's 9 = bb7, an aug's 8 = #5); a note outside the
    // quality falls back to a generic interval name and is colored as a tension.
    private static string IntervalLabel(int semitone, ChordToneFunction? role) => role switch
    {
        ChordToneFunction.Root => "R",
        ChordToneFunction.Third => semitone == 3 ? "b3" : "3",
        ChordToneFunction.Fifth => semitone switch { 6 => "b5", 8 => "#5", _ => "5" },
        ChordToneFunction.Seventh => semitone switch { 9 => "bb7", 11 => "7", _ => "b7" },
        _ => GenericLabel(semitone),
    };

    private static string GenericLabel(int semitone) => semitone switch
    {
        0 => "R", 1 => "b9", 2 => "9", 3 => "#9", 4 => "3", 5 => "11",
        6 => "#11", 7 => "5", 8 => "b13", 9 => "13", 10 => "b7", _ => "7",
    };
}
