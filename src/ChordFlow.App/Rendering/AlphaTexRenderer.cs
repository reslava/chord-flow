using System.Globalization;
using System.Text;
using ChordFlow.Domain;

namespace ChordFlow.Rendering;

/// <summary>
/// The <b>only</b> component that knows alphaTex syntax. Turns an <see cref="Exercise"/>
/// into an alphaTex string per loom/refs/alphatex-syntax-reference.md: header metadata
/// (<c>\title \subtitle \tempo \ts \ks</c>), a lone <c>.</c> to end the header, then bars
/// of stateful <c>:N</c> durations, <c>( )</c> chord groups, <c>r</c> rests, separated by <c>|</c>.
/// </summary>
public sealed class AlphaTexRenderer : IScoreRenderer
{
    // alphaTex key-signature token per major-key tonic pitch class (0 = C .. 11 = B).
    // Lowercase flats per the verified reference. MVP renders major keys only.
    private static readonly string[] MajorKeySignature =
        { "c", "db", "d", "eb", "e", "f", "f#", "g", "ab", "a", "bb", "b" };

    // Human-readable key name for the title, spelled to match the key signature above.
    private static readonly string[] MajorKeyName =
        { "C", "Db", "D", "Eb", "E", "F", "F#", "G", "Ab", "A", "Bb", "B" };

    public string Render(Exercise exercise)
    {
        ArgumentNullException.ThrowIfNull(exercise);

        if (exercise.Key.IsMinor)
        {
            throw new NotSupportedException("The MVP renderer supports major keys only.");
        }

        int tonic = ((exercise.Key.Tonic.Value % 12) + 12) % 12;
        string keyName = MajorKeyName[tonic];
        string keySig = MajorKeySignature[tonic];

        Chord[] chords = Transposer.Realize(exercise.Progression, exercise.Key);

        var sb = new StringBuilder();

        // --- Header metadata ---
        sb.Append("\\title \"").Append(exercise.Progression.Name).Append(" — ").Append(keyName).Append("\"\n");
        sb.Append("\\subtitle \"").Append(exercise.Difficulty).Append(" — ").Append(exercise.Rhythm.Name).Append("\"\n");
        sb.Append("\\tempo ").Append(exercise.Tempo.ToString(CultureInfo.InvariantCulture)).Append('\n');
        sb.Append("\\ts 4 4\n");
        sb.Append("\\ks ").Append(keySig).Append('\n');
        sb.Append(".\n");

        // --- Bars ---
        // Duration is stateful in alphaTex: emit ":N" only when it changes, persisting
        // across beats and bars.
        string? currentDuration = null;
        var barLines = new List<string>(chords.Length);

        foreach (Chord chord in chords)
        {
            var beats = new List<string>(exercise.Rhythm.Beats.Count);

            foreach (Beat beat in exercise.Rhythm.Beats)
            {
                string durationToken = DurationToken(beat.Duration);
                string prefix = string.Empty;
                if (durationToken != currentDuration)
                {
                    prefix = ":" + durationToken + " ";
                    currentDuration = durationToken;
                }

                string body = beat.IsHit
                    ? FormatChord(chord, exercise.Difficulty)
                    : "r";

                beats.Add(prefix + body);
            }

            barLines.Add(string.Join(" ", beats) + " |");
        }

        sb.Append(string.Join("\n", barLines));

        return sb.ToString();
    }

    private static string FormatChord(Chord chord, Difficulty difficulty)
    {
        Voicing voicing = VoicingBook.Lookup(chord, difficulty);
        IEnumerable<string> notes = voicing.Positions.Select(p => $"{p.Fret}.{p.String}");
        return "(" + string.Join(" ", notes) + ")";
    }

    private static string DurationToken(Duration duration) => duration switch
    {
        Duration.Whole => "1",
        Duration.Half => "2",
        Duration.Quarter => "4",
        Duration.Eighth => "8",
        Duration.Sixteenth => "16",
        _ => throw new ArgumentOutOfRangeException(nameof(duration), duration, "Unknown duration."),
    };
}
