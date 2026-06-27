using ChordFlow.Music.Harmony;

namespace ChordFlow.Instruments.Guitar;

/// <summary>
/// The CAGED <b>derived-chord</b> producer of the general <see cref="FretboardDiagram"/> carrier — the
/// <see cref="ChordShape"/> twin of <see cref="VoicingDiagram"/>. Lights an engine-derived grip
/// (<see cref="CagedDerivation.Derive"/>): one <see cref="MarkerShape.Circle"/> marker per sounded string, muted
/// strings as diagram chrome, the octave <see cref="ChordShape.Zone"/> as the band, and the derived
/// <see cref="ChordShape.AnchorFinger">anchor finger</see> surfaced in the title. Powers the CAGED Chords dogfood
/// page — the visual check for the derivation engine. Theory stays upstream (C1); it adds no geometry of its own.
/// </summary>
public static class ChordShapeDiagram
{
    /// <summary>Build the fretboard diagram for the derived <paramref name="shape"/> rooted at <paramref name="root"/>.</summary>
    public static FretboardDiagram Build(ChordShape shape, PitchClass root)
    {
        ArgumentNullException.ThrowIfNull(shape);

        var key = new Key(root, IsMinor: false); // spells the note names for this root
        Dictionary<int, ChordToneFunction> roleByInterval = RoleByInterval(shape.Quality);

        var markers = new List<FretboardMarker>();
        var mutedStrings = new List<int>();
        foreach (ChordShapeString s in shape.Strings)
        {
            if (s.IsMuted)
            {
                mutedStrings.Add(s.String);
                continue;
            }

            int fret = s.Fret!.Value;
            PitchClass pc = Fretboard.PitchClassAt(s.String, fret);
            int semitone = ((s.Semitones % 12) + 12) % 12;
            ChordToneFunction? role = roleByInterval.TryGetValue(semitone, out ChordToneFunction f) ? f : null;

            markers.Add(new FretboardMarker(
                s.String,
                fret,
                NoteSpeller.Name(pc, key),
                IntervalSpeller.Label(semitone, role),
                FunctionName(role),
                MarkerShape.Circle));
        }

        // Frame an explicit fret window over the union of the fretted markers and the octave zone, so the zone band
        // is never clipped by an auto-fit that stops at the top marker (caged-chords-chat-002). Open-string (fret-0)
        // markers don't bound the window — the JS view always draws the nut once the window reaches fret ≤ 1.
        IReadOnlyList<int> fretted = markers.Where(m => m.Fret > 0).Select(m => m.Fret).ToList();
        int zoneMin = shape.Zone.MinFret;
        int zoneMax = shape.Zone.MaxFret;
        int windowMin = fretted.Count > 0 ? Math.Min(fretted.Min(), zoneMin) : zoneMin;
        int windowMax = fretted.Count > 0 ? Math.Max(fretted.Max(), zoneMax) : zoneMax;
        string symbol = ChordSymbol.Format(new Chord(root, shape.Quality), key);
        string title = $"{symbol} · {shape.Shape} shape · {FingerName(shape.AnchorFinger)}";

        return new FretboardDiagram(
            title,
            markers,
            mutedStrings,
            BarreFret: null,
            FretMin: Math.Max(0, windowMin),
            FretMax: windowMax,
            ZoneFretMin: zoneMin,
            ZoneFretMax: zoneMax);
    }

    // Each chord-tone semitone → its function, read from the quality's formula degree (ChordTones / C6): the 6
    // and the bb7 (both semitone 9) separate by degree, so a 6/m6 lights its 6 as a sixth, dim7 its bb7 as a seventh.
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

    private static string FingerName(Finger finger) => finger.ToString().ToLowerInvariant();
}
