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
/// <para>
/// Chords are no longer 1:1 with bars (harmonic-rhythm layer): each bar is realized into ordered
/// <see cref="RealizedSpan"/>s and the rhythm is quantized with that bar's chord boundaries, so every
/// slot is mapped to the chord covering its <see cref="RhythmSlot.StartTick"/>. A single-chord bar has
/// no interior boundaries, so it reduces to the original one-chord-per-bar output (C4/C6).
/// </para>
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
        IReadOnlyList<RealizedBar> bars = Transposer.RealizeBars(exercise.Progression, exercise.Key);

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
        var barLines = new List<string>(bars.Count + 1);

        // A pickup/anacrusis renders as a leading measure, voiced with the first chord of the first bar.
        if (rhythm.Pickup is { } pickup && bars.Count > 0)
        {
            Chord firstChord = bars[0].Spans[0].Chord;
            IReadOnlyList<RhythmSlot> pickupSlots = RhythmQuantizer.Quantize(pickup);
            barLines.Add(RenderBar(pickupSlots, _ => firstChord, exercise.Difficulty, ref currentDuration));
        }

        // Apply the exercise's groove feel as a playback-time warp before quantizing (identity for
        // Straight). The stored pattern stays straight — Feel is never baked into it (C4).
        IReadOnlyList<RhythmEvent> feltEvents = FeelTransform.Apply(rhythm.Events, exercise.Feel, ts);
        foreach (RealizedBar bar in bars)
        {
            // Re-attack the strum at each interior chord change; quantize this bar against its own
            // boundaries so a slot landing on a new chord starts a fresh attack.
            IReadOnlyList<int> boundaries = InteriorBoundaries(bar);
            IReadOnlyList<RhythmSlot> slots = RhythmQuantizer.Quantize(feltEvents, ts, boundaries);
            barLines.Add(RenderBar(slots, bar.ChordCovering, exercise.Difficulty, ref currentDuration));
        }

        sb.Append(string.Join("\n", barLines));

        return sb.ToString();
    }

    // Bar-relative ticks where the chord changes (exclusive of 0 and the bar end). Empty for a
    // single-chord bar, so its quantization is identical to the pre-harmonic-rhythm output.
    private static IReadOnlyList<int> InteriorBoundaries(RealizedBar bar)
    {
        if (bar.Spans.Count <= 1)
        {
            return Array.Empty<int>();
        }

        var boundaries = new List<int>(bar.Spans.Count - 1);
        int tick = 0;
        for (int i = 0; i < bar.Spans.Count - 1; i++)
        {
            tick += bar.Spans[i].DurationTicks;
            boundaries.Add(tick);
        }

        return boundaries;
    }

    private static string RenderBar(
        IReadOnlyList<RhythmSlot> slots,
        Func<int, Chord> chordForTick,
        Difficulty difficulty,
        ref string? currentDuration)
    {
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

            // Each slot is voiced with the chord covering its onset tick (harmonic-rhythm lookup).
            string body = slot.IsRest ? "r" : FormatChord(chordForTick(slot.StartTick), difficulty);
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
