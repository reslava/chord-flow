using ChordFlow.Instruments.Guitar;
using ChordFlow.Music.Harmony;
using ChordFlow.Music.Progressions;
using ChordFlow.Music.Rhythm;
using ChordFlow.Music.Songs;
using ChordFlow.Rendering;
using ChordFlow.Rendering.ChordSheets;

namespace ChordFlow.Features.ChordSheets;

/// <summary>Render-independent inputs for a <see cref="ChordSheetBuilder"/> build.</summary>
/// <param name="BarsPerRow">Printed bars per row (the section-row chunk size); default 4.</param>
public sealed record ChordSheetOptions(int BarsPerRow = 4);

/// <summary>
/// A <see cref="ChordSheetBuilder"/> build result: the drawn <see cref="Sheet"/> plus the per-bar
/// <see cref="BarSchedule"/> — one downbeat <see cref="CellScheduleEntry"/> per bar (in walk order, its global
/// 0-based bar index), covering <c>%</c> similes and sustained bars. The handler overlays split-bar sub-chord
/// beats onto this from the render schedule to produce the final playback cellSchedule (approach A).
/// </summary>
public sealed record ChordSheetBuildResult(ChordSheet Sheet, IReadOnlyList<CellScheduleEntry> BarSchedule);

/// <summary>
/// Builds a <see cref="ChordSheet"/> from an already-realized song — the Features-layer producer, peer of
/// <see cref="ExerciseRendering"/> (which builds alphaTex). The caller owns the I/O seam (resolve the harmony
/// via <see cref="ExerciseRefs"/>, <see cref="SongExpander.Expand"/> it into a <see cref="RealizedSong"/>, and —
/// only when the diagram adornment is on — resolve a <see cref="CompingPlan"/> with
/// <see cref="ChordFlow.Features.Voicings.CompingResolver"/>); this method is a <b>pure walk</b> that carries no
/// store, so it is trivially testable. Every field is derived from existing kernel types (constraint C2): no new
/// music theory, only projection into the sheet model.
/// </summary>
public static class ChordSheetBuilder
{
    /// <summary>
    /// Project <paramref name="realized"/> into a <see cref="ChordSheet"/>. <paramref name="sheetKey"/> is the
    /// song's overall (sounding) key for the header; per-chord spelling uses each section's own realized key so
    /// accidentals follow modulations. Pass <paramref name="comping"/> (resolved over the same
    /// <paramref name="realized"/>) to fill the fret-diagram adornment; leave it null for no diagram.
    /// </summary>
    public static ChordSheetBuildResult Build(
        Song song,
        RealizedSong realized,
        Key sheetKey,
        TimeSignature ts,
        ChordSheetOptions options,
        CompingPlan? comping = null)
    {
        ArgumentNullException.ThrowIfNull(song);
        ArgumentNullException.ThrowIfNull(realized);
        ArgumentNullException.ThrowIfNull(sheetKey);
        ArgumentNullException.ThrowIfNull(options);
        if (options.BarsPerRow < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(options), options.BarsPerRow, "BarsPerRow must be >= 1.");
        }

        var header = new ChordSheetHeader(
            Title: song.Name,
            Artist: null,                                  // the Song model carries no artist field yet (v1)
            KeyName: KeyName(sheetKey),
            Tempo: song.DefaultTempo,
            Feel: song.DefaultFeel?.ToString(),
            TimeSig: $"{ts.Numerator}/{ts.Denominator}",
            Capo: song.Capo);

        var sections = new List<ChordSheetSection>(realized.Sections.Count);
        var barSchedule = new List<CellScheduleEntry>();
        // The running master-bar index, advanced once per bar across ALL sections — this lines up with the
        // AlphaTexRenderer's BarIndex and alphaTab's master-bar index (both walk the same realized bars).
        int globalBar = 0;

        for (int si = 0; si < realized.Sections.Count; si++)
        {
            RealizedSection section = realized.Sections[si];
            // Similes are scoped to a section: a section's first bar is never a "%", even if it happens to
            // repeat the previous section's last bar.
            RealizedBar? previous = null;
            var rows = new List<ChordSheetRow>();
            int rowIndex = 0;

            for (int i = 0; i < section.Bars.Count; i += options.BarsPerRow)
            {
                int count = Math.Min(options.BarsPerRow, section.Bars.Count - i);
                var cells = new List<ChordSheetCell>(count);
                for (int b = i; b < i + count; b++)
                {
                    int cellIndex = b - i;
                    RealizedBar bar = section.Bars[b];
                    int barTicks = bar.Spans.Sum(s => s.DurationTicks);

                    if (previous is not null && BarsEqual(bar, previous))
                    {
                        cells.Add(new ChordSheetCell(Array.Empty<ChordRef>(), RepeatOfPrev: true, barTicks));
                    }
                    else
                    {
                        var chords = bar.Spans
                            .Select(span => ToChordRef(span, section.Key, comping))
                            .ToList();
                        cells.Add(new ChordSheetCell(chords, RepeatOfPrev: false, barTicks));
                    }

                    // One per-bar downbeat entry (Beat 0, Chord 0) for EVERY bar — including % similes and
                    // sustained bars — so the marker can highlight any sounding bar. Split-bar sub-chord onsets
                    // are overlaid later in the handler from the render schedule (approach A).
                    barSchedule.Add(new CellScheduleEntry(globalBar, 0, si, rowIndex, cellIndex, 0));
                    globalBar++;

                    previous = bar;
                }

                rows.Add(new ChordSheetRow(cells));
                rowIndex++;
            }

            sections.Add(new ChordSheetSection(section.Label, rows));
        }

        return new ChordSheetBuildResult(new ChordSheet(header, sections), barSchedule);
    }

    /// <summary>
    /// Overlay the render schedule's mid-bar chord onsets onto the builder's per-bar downbeats (approach A):
    /// every bar keeps its downbeat entry (bar-level highlight, incl. <c>%</c> and sustained bars); a split bar
    /// gains one entry per mid-bar chord change, mapped to its chord-segment index (1, 2, … in beat order —
    /// segment 0 is the downbeat). (bar,beat) come straight from the alphaTab-aligned render schedule. A pure
    /// walk producing the final playback cellSchedule for the unified generate/loadExercise reply
    /// (<c>ExerciseRendering.RenderWithSheet</c>).
    /// </summary>
    public static IReadOnlyList<CellScheduleEntry> OverlaySchedule(
        IReadOnlyList<CellScheduleEntry> barSchedule, IReadOnlyList<ChordChange> renderSchedule)
    {
        ArgumentNullException.ThrowIfNull(barSchedule);
        ArgumentNullException.ThrowIfNull(renderSchedule);

        var cellByBar = barSchedule.ToDictionary(e => e.Bar);
        var entries = new List<CellScheduleEntry>(barSchedule);

        foreach (var barChanges in renderSchedule.GroupBy(c => c.Bar))
        {
            if (!cellByBar.TryGetValue(barChanges.Key, out CellScheduleEntry? cell))
            {
                continue;
            }

            var midBar = barChanges.Where(c => c.Beat > 0).OrderBy(c => c.Beat).ToList();
            for (int j = 0; j < midBar.Count; j++)
            {
                entries.Add(new CellScheduleEntry(
                    barChanges.Key, midBar[j].Beat, cell.Section, cell.Row, cell.Cell, Chord: j + 1));
            }
        }

        return entries.OrderBy(e => e.Bar).ThenBy(e => e.Beat).ToList();
    }

    // One chord span → a ChordRef carrying every notation (concrete/Nashville/Roman) and the tone strip, plus
    // the comped diagram when a plan is supplied. Note names are spelled against the section's key.
    private static ChordRef ToChordRef(RealizedSpan span, Key key, CompingPlan? comping)
    {
        Chord chord = span.Chord;

        var tones = ChordTones.Of(chord)
            .Select(t => new ChordSheetTone(
                Note: NoteSpeller.Name(t.PitchClassFor(chord.Root), key),
                Interval: IntervalSpeller.Label(t.Interval, t.Function),
                Function: FunctionName(t.Function)))
            .ToList();

        FretboardDiagram? diagram = comping is null
            ? null
            : RealizedVoicingDiagram.Build(chord, comping.For(span), key);

        return new ChordRef(
            Concrete: ChordSymbol.Format(chord, key),
            Degree: NashvilleToken(span.Degree),
            Roman: RomanFunction(span.Degree),
            DurationTicks: span.DurationTicks,
            Tones: tones,
            Diagram: diagram);
    }

    // Two bars are "the same bar" (a simile) when they have the same ordered spans by concrete chord and
    // duration — StartTick is derived from the durations, so it need not be compared. Chord is a record, so its
    // equality already covers root/quality/spelling.
    private static bool BarsEqual(RealizedBar a, RealizedBar b)
    {
        if (a.Spans.Count != b.Spans.Count)
        {
            return false;
        }

        for (int i = 0; i < a.Spans.Count; i++)
        {
            if (a.Spans[i].Chord != b.Spans[i].Chord || a.Spans[i].DurationTicks != b.Spans[i].DurationTicks)
            {
                return false;
            }
        }

        return true;
    }

    private static string KeyName(Key key) =>
        NoteSpeller.Name(key.Tonic, key) + (key.IsMinor ? "m" : "");

    private static string FunctionName(ChordToneFunction function) => function switch
    {
        ChordToneFunction.Root => "root",
        ChordToneFunction.Third => "third",
        ChordToneFunction.Fifth => "fifth",
        ChordToneFunction.Sixth => "sixth",
        ChordToneFunction.Seventh => "seventh",
        _ => "tension",
    };

    // The Nashville scale-degree token for a degree: optional accidental prefix + degree number + quality
    // suffix (the same suffix vocabulary the ProgressionParser accepts, in its canonical output form).
    private static string NashvilleToken(RomanDegree degree) =>
        AccidentalPrefix(degree.Accidental) + degree.Degree + degree.Quality switch
        {
            Quality.Major => "",
            Quality.Minor => "-",
            Quality.Dominant7 => "7",
            Quality.Minor7 => "-7",
            Quality.Major7 => "maj7",
            Quality.HalfDiminished7 => "m7b5",
            Quality.Diminished => "dim",
            Quality.Diminished7 => "dim7",
            Quality.Augmented => "+",
            Quality.Major6 => "6",
            Quality.Minor6 => "-6",
            _ => "",
        };

    // The honest diatonic Roman-function label: an accidental prefix + the roman numeral (case carries
    // major/minor) + a quality decoration. This only formats the degree's OWN quality — it does not infer
    // secondary dominants or borrowing (req IN7); those labels come later from the harmonic-analysis pass.
    private static string RomanFunction(RomanDegree degree)
    {
        string numeral = Numerals[degree.Degree];
        bool lower = degree.Quality is Quality.Minor or Quality.Minor7 or Quality.Minor6
            or Quality.Diminished or Quality.Diminished7 or Quality.HalfDiminished7;

        string suffix = degree.Quality switch
        {
            Quality.Dominant7 => "7",
            Quality.Minor7 => "7",
            Quality.Major7 => "maj7",
            Quality.HalfDiminished7 => "ø7",
            Quality.Diminished => "°",
            Quality.Diminished7 => "°7",
            Quality.Augmented => "+",
            Quality.Major6 => "6",
            Quality.Minor6 => "6",
            _ => "",
        };

        return AccidentalPrefix(degree.Accidental) + (lower ? numeral.ToLowerInvariant() : numeral) + suffix;
    }

    // Roman numerals indexed by scale degree (1..7); index 0 is unused.
    private static readonly string[] Numerals = { "", "I", "II", "III", "IV", "V", "VI", "VII" };

    private static string AccidentalPrefix(Accidental accidental) => accidental switch
    {
        Accidental.Sharp => "#",
        Accidental.Flat => "b",
        _ => "",
    };
}
