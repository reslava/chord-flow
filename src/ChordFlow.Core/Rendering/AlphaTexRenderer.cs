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
    // The voicing source for rendered chords. Stored authored voicings shadow the generated strategy
    // shapes; an empty-library book resolves every chord through the strategy fallback.
    private readonly VoicingBook _book;

    /// <summary>Render using <paramref name="book"/> as the voicing source (stored voicings shadow generated shapes).</summary>
    public AlphaTexRenderer(VoicingBook book)
    {
        ArgumentNullException.ThrowIfNull(book);
        _book = book;
    }

    /// <summary>Render with no authored library — every chord resolves through the generated strategy shapes.</summary>
    public AlphaTexRenderer() : this(new VoicingBook(Array.Empty<VoicingShape>()))
    {
    }

    public string Render(Exercise exercise)
    {
        ArgumentNullException.ThrowIfNull(exercise);
        EnsureMajorSupported(exercise.Key);

        // Spelling lives in the domain (NoteSpeller), keeping this the only alphaTex-aware code.
        string keyName = NoteSpeller.Name(exercise.Key.Tonic, exercise.Key);

        RhythmPattern rhythm = exercise.Rhythm;
        TimeSignature ts = rhythm.TimeSignature;
        IReadOnlyList<RealizedBar> bars = Transposer.RealizeBars(exercise.Progression, exercise.Key);

        var sb = new StringBuilder();
        AppendHeader(
            sb,
            $"{exercise.Progression.Name} — {keyName}",
            $"{exercise.Difficulty} — {rhythm.Name}",
            exercise.Tempo,
            ts,
            NoteSpeller.KeySignatureToken(exercise.Key));

        // Duration is stateful in alphaTex: a ":N" token persists across beats and bars until changed.
        string? currentDuration = null;
        var barLines = new List<string>(bars.Count + 1);

        // Apply the exercise's groove feel as a playback-time warp before quantizing (identity for
        // Straight). The stored pattern stays straight — Feel is never baked into it (C4). Each pattern
        // bar is warped once; multi-bar patterns tile cyclically onto the progression (design §7).
        IReadOnlyList<IReadOnlyList<RhythmEvent>> feltBars = WarpBars(rhythm, exercise.Feel, ts);

        // A pickup/anacrusis renders as a leading measure, voiced with the first chord of the first bar.
        if (rhythm.Pickup is { } pickup && bars.Count > 0)
        {
            Chord firstChord = bars[0].Spans[0].Chord;
            IReadOnlyList<RhythmSlot> pickupSlots = RhythmQuantizer.Quantize(pickup);
            barLines.Add(RenderBar(pickupSlots, _ => firstChord, exercise.Difficulty, ref currentDuration));
        }

        RenderBars(bars, feltBars, ts, exercise.Difficulty, barLines, ref currentDuration);

        sb.Append(string.Join("\n", barLines));

        return sb.ToString();
    }

    public string Render(RealizedSong song, RhythmPattern rhythm, int tempo, Difficulty difficulty, Feel feel = Feel.Straight)
    {
        ArgumentNullException.ThrowIfNull(song);
        ArgumentNullException.ThrowIfNull(rhythm);
        if (song.Sections.Count == 0)
        {
            throw new ArgumentException("Cannot render a song with no sections.", nameof(song));
        }

        TimeSignature ts = rhythm.TimeSignature;
        IReadOnlyList<IReadOnlyList<RhythmEvent>> feltBars = WarpBars(rhythm, feel, ts);

        // One header, seeded from the first section's key (\ks is legal mid-score, so later key changes are
        // emitted inline — no per-key score splitting; design §8.3).
        RealizedSection first = song.Sections[0];
        EnsureMajorSupported(first.Key);

        var sb = new StringBuilder();
        AppendHeader(
            sb,
            $"{first.Label} — {NoteSpeller.Name(first.Key.Tonic, first.Key)}",
            $"{difficulty} — {rhythm.Name}",
            tempo,
            ts,
            NoteSpeller.KeySignatureToken(first.Key));

        // currentDuration is threaded across every section so a ":N" carries over section seams unchanged.
        string? currentDuration = null;
        var barLines = new List<string>();
        Key? previousKey = null;

        foreach (RealizedSection section in song.Sections)
        {
            EnsureMajorSupported(section.Key);

            // Inline \ks only when the key changes; the first section's key already sits in the header.
            if (previousKey is not null && !section.Key.Equals(previousKey))
            {
                barLines.Add("\\ks " + NoteSpeller.KeySignatureToken(section.Key));
            }

            RenderBars(section.Bars, feltBars, ts, difficulty, barLines, ref currentDuration);
            previousKey = section.Key;
        }

        sb.Append(string.Join("\n", barLines));

        return sb.ToString();
    }

    private static void EnsureMajorSupported(Key key)
    {
        if (key.IsMinor)
        {
            throw new NotSupportedException("The MVP renderer supports major keys only.");
        }
    }

    private static void AppendHeader(
        StringBuilder sb, string title, string subtitle, int tempo, TimeSignature ts, string keySig)
    {
        sb.Append("\\title \"").Append(title).Append("\"\n");
        sb.Append("\\subtitle \"").Append(subtitle).Append("\"\n");
        sb.Append("\\tempo ").Append(tempo.ToString(CultureInfo.InvariantCulture)).Append('\n');
        sb.Append("\\ts ").Append(ts.Numerator).Append(' ').Append(ts.Denominator).Append('\n');
        sb.Append("\\ks ").Append(keySig).Append('\n');
        sb.Append(".\n");
    }

    // Warp every pattern bar by the groove feel once (identity for Straight). Returns one event list per
    // PatternBar; the base pattern is never mutated (C4). RenderBars tiles these onto the progression.
    private static IReadOnlyList<IReadOnlyList<RhythmEvent>> WarpBars(
        RhythmPattern rhythm, Feel feel, TimeSignature ts) =>
        rhythm.Bars.Select(b => FeelTransform.Apply(b.Events, feel, ts)).ToList();

    // Render a key-resolved run of bars, appending one line per bar. The per-bar logic (interior chord
    // boundaries → quantize → RenderBar) is shared verbatim by Render(Exercise) and Render(RealizedSong);
    // currentDuration is passed by ref so the stateful ":N" carries across both bars and section seams.
    // An m-bar pattern tiles cyclically: progression bar i uses pattern bar i % m (design §7 default; the
    // richer section-anchored alignment is owned by domain/multi-bar). Single-bar patterns (m=1) reduce to
    // the original "same bar everywhere" output.
    private void RenderBars(
        IReadOnlyList<RealizedBar> bars,
        IReadOnlyList<IReadOnlyList<RhythmEvent>> feltBars,
        TimeSignature ts,
        Difficulty difficulty,
        List<string> barLines,
        ref string? currentDuration)
    {
        for (int i = 0; i < bars.Count; i++)
        {
            RealizedBar bar = bars[i];
            IReadOnlyList<RhythmEvent> feltEvents = feltBars[i % feltBars.Count];

            // Re-attack the strum at each interior chord change; quantize this bar against its own
            // boundaries so a slot landing on a new chord starts a fresh attack.
            IReadOnlyList<int> boundaries = InteriorBoundaries(bar);
            IReadOnlyList<RhythmSlot> slots = RhythmQuantizer.Quantize(feltEvents, ts, boundaries);
            barLines.Add(RenderBar(slots, bar.ChordCovering, difficulty, ref currentDuration));
        }
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

    private string RenderBar(
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

            // A triplet-grid slot carries the verified alphaTex {tu N} beat effect (N = numerator).
            // Unlike :N duration, {tu} does not persist in alphaTex, so it is emitted on every slot.
            if (slot.Tuplet is { } tuplet)
            {
                body += "{tu " + tuplet.Numerator.ToString(CultureInfo.InvariantCulture) + "}";
            }

            tokens.Add(prefix + body);
        }

        return string.Join(" ", tokens) + " |";
    }

    private string FormatChord(Chord chord, Difficulty difficulty)
    {
        Voicing voicing = _book.Lookup(chord, difficulty);
        IEnumerable<string> notes = voicing.Positions.Select(p => $"{p.Fret}.{p.String}");
        return "(" + string.Join(" ", notes) + ")";
    }
}
