using System.Globalization;
using System.Text;
using ChordFlow.Domain;

namespace ChordFlow.Rendering;

/// <summary>
/// The <b>only</b> component that knows alphaTex syntax. Turns an <see cref="Exercise"/> into an
/// alphaTex string per loom/refs/alphatex-syntax-reference.md: header metadata
/// (<c>\title \subtitle \tempo \ts \ks</c>), a lone <c>.</c> to end the header, then bars of stateful
/// <c>:N</c> durations, <c>( )</c> chord groups, <c>r</c> rests, separated by <c>|</c>. Note spelling
/// comes from <see cref="NoteSpeller"/> and rhythm tokens from <see cref="RhythmQuantizer"/> — this
/// type only formats them.
/// </summary>
public sealed class AlphaTexRenderer : IScoreRenderer
{
    public string Render(Exercise exercise)
    {
        ArgumentNullException.ThrowIfNull(exercise);

        if (exercise.Key.IsMinor)
        {
            throw new NotSupportedException("The MVP renderer supports major keys only.");
        }

        // Spelling lives in the domain (NoteSpeller), keeping this the only alphaTex-aware code.
        string keyName = NoteSpeller.Name(exercise.Key.Tonic, exercise.Key);
        string keySig = NoteSpeller.KeySignatureToken(exercise.Key);

        RhythmPattern rhythm = exercise.Rhythm;
        TimeSignature ts = rhythm.TimeSignature;
        Chord[] chords = Transposer.Realize(exercise.Progression, exercise.Key);

        var sb = new StringBuilder();

        // --- Header metadata ---
        sb.Append("\\title \"").Append(exercise.Progression.Name).Append(" — ").Append(keyName).Append("\"\n");
        sb.Append("\\subtitle \"").Append(exercise.Difficulty).Append(" — ").Append(rhythm.Name).Append("\"\n");
        sb.Append("\\tempo ").Append(exercise.Tempo.ToString(CultureInfo.InvariantCulture)).Append('\n');
        sb.Append("\\ts ").Append(ts.Numerator).Append(' ').Append(ts.Denominator).Append('\n');
        sb.Append("\\ks ").Append(keySig).Append('\n');
        sb.Append(".\n");

        // --- Bars ---
        // Duration is stateful in alphaTex: a ":N" token persists across beats and bars until changed.
        string? currentDuration = null;
        var barLines = new List<string>(chords.Length + 1);

        // A pickup/anacrusis renders as a leading measure, voiced with the first chord.
        if (rhythm.Pickup is { } pickup && chords.Length > 0)
        {
            IReadOnlyList<RhythmSlot> pickupSlots = RhythmQuantizer.Quantize(pickup);
            barLines.Add(RenderBar(pickupSlots, chords[0], exercise.Difficulty, ref currentDuration));
        }

        // Apply the exercise's groove feel as a playback-time warp before quantizing (identity for
        // Straight). The stored pattern stays straight — Feel is never baked into it (C4).
        IReadOnlyList<RhythmEvent> feltEvents = FeelTransform.Apply(rhythm.Events, exercise.Feel, ts);
        IReadOnlyList<RhythmSlot> barSlots = RhythmQuantizer.Quantize(feltEvents, ts);
        foreach (Chord chord in chords)
        {
            barLines.Add(RenderBar(barSlots, chord, exercise.Difficulty, ref currentDuration));
        }

        sb.Append(string.Join("\n", barLines));

        return sb.ToString();
    }

    private static string RenderBar(
        IReadOnlyList<RhythmSlot> slots, Chord chord, Difficulty difficulty, ref string? currentDuration)
    {
        string chordGroup = FormatChord(chord, difficulty);
        var tokens = new List<string>(slots.Count);

        foreach (RhythmSlot slot in slots)
        {
            if (slot.TiedToPrevious)
            {
                // The MVP patterns never tie; alphaTex's tie token is unverified (see the syntax
                // reference), so we refuse rather than emit something that may not parse.
                throw new NotSupportedException(
                    "alphaTex tie rendering is not supported in v1 (tie token unverified).");
            }

            string durationToken = slot.NoteValue.ToString(CultureInfo.InvariantCulture);
            string prefix = string.Empty;
            if (durationToken != currentDuration)
            {
                prefix = ":" + durationToken + " ";
                currentDuration = durationToken;
            }

            string body = slot.IsRest ? "r" : chordGroup;
            tokens.Add(prefix + body);
        }

        return string.Join(" ", tokens) + " |";
    }

    private static string FormatChord(Chord chord, Difficulty difficulty)
    {
        Voicing voicing = VoicingBook.Lookup(chord, difficulty);
        IEnumerable<string> notes = voicing.Positions.Select(p => $"{p.Fret}.{p.String}");
        return "(" + string.Join(" ", notes) + ")";
    }
}
