using ChordFlow.Music.Harmony;

namespace ChordFlow.Instruments.Guitar;

/// <summary>
/// The <b>real-root</b> voicing producer of the general <see cref="FretboardDiagram"/> carrier: turns a concrete
/// <see cref="Voicing"/> (actual frets at the chord's actual root) into a diagram, intervals/functions resolved
/// against <see cref="Chord.Root"/>. This is the general case; <see cref="VoicingDiagram.Build"/> is the
/// canonical-C special case (it delegates here at a C anchor).
/// <para>
/// For each sounding string it resolves the pitch class (<see cref="Fretboard.PitchClassAt"/>), its interval
/// against the chord root, the chord-tone function (root/third/fifth/seventh by tertian position in
/// <see cref="QualityIntervals"/>) or <c>tension</c> for a note outside the quality, the interval label, and the
/// spelled note (<see cref="NoteSpeller"/> against <paramref name="key"/> so accidentals match the score), and
/// emits one <see cref="MarkerShape.Circle"/> marker (fret 0 ⇒ an open marker). Muted/unfretted strings become
/// diagram-level chrome (<see cref="FretboardDiagram.MutedStrings"/>), not markers. All music theory is resolved
/// here in Core — the JS view is a dumb drawer (ctx C1).
/// </para>
/// </summary>
public static class RealizedVoicingDiagram
{
    /// <summary>
    /// Compute the fretboard diagram for <paramref name="voicing"/> as the realization of <paramref name="chord"/>,
    /// with note names spelled against <paramref name="key"/>.
    /// </summary>
    public static FretboardDiagram Build(Chord chord, Voicing voicing, Key key)
    {
        ArgumentNullException.ThrowIfNull(chord);
        ArgumentNullException.ThrowIfNull(voicing);

        Dictionary<int, int> fretByString = voicing.Positions.ToDictionary(p => p.String, p => p.Fret);
        var muted = voicing.MutedStrings is { } m ? new HashSet<int>(m) : new HashSet<int>();
        Dictionary<int, ChordToneFunction> roleByInterval = RoleByInterval(chord.Quality);
        int root = chord.Root.Value;

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
            int semitone = Mod12(pc.Value - root); // interval above the chord root
            ChordToneFunction? role = roleByInterval.TryGetValue(semitone, out ChordToneFunction f) ? f : null;

            markers.Add(new FretboardMarker(
                s,
                fret,
                NoteSpeller.Name(pc, key),
                IntervalSpeller.Label(semitone, role),
                FunctionName(role),
                MarkerShape.Circle));
        }

        int firstFret = voicing.FirstFret ?? (fretByString.Count == 0 ? 0 : fretByString.Values.Min());
        string title = ChordSymbol.Format(chord, key);
        return new FretboardDiagram(title, markers, mutedStrings, voicing.BarreFret, FretMin: firstFret, FretMax: null);
    }

    // Each chord-tone semitone (relative to the root) → its function, read from the quality's formula degree
    // (ChordTones / C6). The 6 and the bb7 — both semitone 9 — separate by degree, so a 6/m6 voicing lights its 6
    // as a sixth while dim7 keeps its bb7 as a seventh; tertian qualities are byte-identical to the old mapping.
    private static Dictionary<int, ChordToneFunction> RoleByInterval(Quality quality) =>
        ChordTones.Of(new Chord(new PitchClass(0), quality))
            .ToDictionary(t => ((t.Interval % 12) + 12) % 12, t => t.Function);

    private static string FunctionName(ChordToneFunction? role) => role switch
    {
        ChordToneFunction.Root => "root",
        ChordToneFunction.Third => "third",
        ChordToneFunction.Fifth => "fifth",
        ChordToneFunction.Sixth => "sixth",
        ChordToneFunction.Seventh => "seventh",
        _ => "tension",
    };

    private static int Mod12(int value) => ((value % 12) + 12) % 12;
}
