using System.Globalization;
using ChordFlow.Music.Rhythm;

namespace ChordFlow.Instruments.Drums;

/// <summary>
/// Pure parser for the <b>drums hit-grid DSL</b> → <see cref="DrumGroove"/> — the percussion peer of
/// <see cref="RhythmPatternParser"/>. It reuses the same tick-grid model (req C2) but with
/// instrument-optimized notation: hits are instantaneous, so there is a single hit glyph and no
/// sustain/rest/tie distinction (req C3).
/// <list type="bullet">
/// <item><b>Rows are lanes.</b> Each non-blank line is one voice lane: <c>&lt;VOICE&gt; [:n] cells [| cells …]</c>.
///   Newlines are <b>significant</b> here (unlike the Rhythm DSL) — one row per voice. A <c>#</c> starts an
///   end-of-line comment. The first token is the voice label, resolved via <see cref="DrumVoices.TryParse"/>
///   (short token or full-name alias, case-insensitive).</item>
/// <item><b>Glyphs:</b> <c>x</c> = hit, <c>.</c> = no hit. (<c>X</c> is reserved for a future accent glyph —
///   <c>drums/drums-accent-ghost</c> — and is rejected today.)</item>
/// <item><b>Whitespace between cells is insignificant</b> (visual grid alignment). Runs are delimited by
///   <c>:n</c> markers, not spaces: a bare <c>:n</c> token starts a new run at that subdivision. A row-level
///   <c>:n</c> immediately after the voice sets each bar's starting subdivision (default 4 = 16ths); an
///   in-bar <c>:n</c> overrides locally, so straight and triplet (<c>:3</c>/<c>:6</c>) beats mix in one bar.</item>
/// <item><c>|</c> separates <b>bars</b>. Every row must agree on the bar count; a voice may appear in only one
///   row. Cells assemble <b>bar-major</b> into <see cref="DrumBar"/>s (each row → one <see cref="DrumLane"/>
///   per bar), the shape the renderer walks.</item>
/// </list>
/// 4/4 only (req C8). Bad input throws <see cref="FormatException"/> naming the offending voice / run / cell.
/// </summary>
public static class DrumGrooveParser
{
    private const int DefaultSubdivision = 4;

    private static readonly System.Buffers.SearchValues<char> Digits =
        System.Buffers.SearchValues.Create("0123456789");

    /// <summary>
    /// Parse <paramref name="dsl"/> into a validated <see cref="DrumGroove"/> in <paramref name="ts"/>.
    /// Throws <see cref="FormatException"/> on malformed input.
    /// </summary>
    public static DrumGroove Parse(string id, string name, string dsl, TimeSignature ts)
    {
        ArgumentNullException.ThrowIfNull(dsl);

        var rows = new List<(DrumVoice Voice, IReadOnlyList<IReadOnlyList<RhythmEvent>> Bars)>();

        foreach (string rawLine in dsl.Split('\n'))
        {
            string line = StripComment(rawLine).Trim();
            if (line.Length == 0)
            {
                continue;
            }

            rows.Add(ParseRow(line, ts));
        }

        if (rows.Count == 0)
        {
            throw new FormatException("Drum groove has no voice rows.");
        }

        int barCount = rows[0].Bars.Count;
        var seenVoices = new HashSet<DrumVoice>();
        foreach ((DrumVoice voice, IReadOnlyList<IReadOnlyList<RhythmEvent>> bars) in rows)
        {
            if (!seenVoices.Add(voice))
            {
                throw new FormatException($"Voice '{voice.Token()}' appears in more than one row.");
            }

            if (bars.Count != barCount)
            {
                throw new FormatException(
                    $"Voice '{voice.Token()}' has {bars.Count} bar(s), but the groove has {barCount}.");
            }
        }

        var groveBars = new List<DrumBar>(barCount);
        for (int b = 0; b < barCount; b++)
        {
            int barIndex = b;
            var lanes = rows.Select(r => new DrumLane(r.Voice, r.Bars[barIndex])).ToList();
            groveBars.Add(new DrumBar(lanes));
        }

        return new DrumGroove(id, name, groveBars, ts);
    }

    // A row: "<VOICE> [:n] cells [| cells ...]". Returns the voice + its per-bar event lists.
    private static (DrumVoice Voice, IReadOnlyList<IReadOnlyList<RhythmEvent>> Bars) ParseRow(string line, TimeSignature ts)
    {
        string[] tokens = line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);

        string label = tokens[0];
        if (!DrumVoices.TryParse(label, out DrumVoice voice))
        {
            throw new FormatException($"Unknown drum voice '{label}'.");
        }

        int idx = 1;
        int rowDefault = DefaultSubdivision;
        if (idx < tokens.Length && IsBareSubdivision(tokens[idx]))
        {
            rowDefault = ParseSubdivision(tokens[idx][1..], tokens[idx], ts);
            idx++;
        }

        // Rejoin the remaining tokens and split into bars on '|'. Whitespace between cells is insignificant.
        string cellPart = string.Join(' ', tokens[idx..]);
        string[] barSegments = cellPart.Split('|');

        var bars = new List<IReadOnlyList<RhythmEvent>>(barSegments.Length);
        foreach (string segment in barSegments)
        {
            bars.Add(ParseBar(segment, label, rowDefault, ts));
        }

        return (voice, bars);
    }

    // One bar of a row: '|'-delimited cell text. Runs are delimited by ':n' markers; whitespace is
    // insignificant otherwise. Each run's cell count must be a whole multiple of its subdivision, and the
    // bar must span exactly one bar of ticks.
    private static IReadOnlyList<RhythmEvent> ParseBar(string segment, string label, int rowDefault, TimeSignature ts)
    {
        string[] barTokens = segment.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);

        var runs = new List<(int N, string Cells)>();
        int currentN = rowDefault;
        var pending = new System.Text.StringBuilder();

        foreach (string token in barTokens)
        {
            if (IsBareSubdivision(token))
            {
                if (pending.Length > 0)
                {
                    runs.Add((currentN, pending.ToString()));
                    pending.Clear();
                }

                currentN = ParseSubdivision(token[1..], token, ts);
            }
            else
            {
                pending.Append(token);
            }
        }

        if (pending.Length > 0)
        {
            runs.Add((currentN, pending.ToString()));
        }

        if (runs.Count == 0)
        {
            throw new FormatException($"Voice '{label}' has an empty bar (no cells).");
        }

        var events = new List<RhythmEvent>();
        int pos = 0;
        foreach ((int n, string cells) in runs)
        {
            if (cells.Length % n != 0)
            {
                throw new FormatException(
                    $"Voice '{label}' run \"{cells}\" has {cells.Length} cell(s), not a whole multiple of subdivision {n}.");
            }

            int cellTicks = ts.BeatTicks / n;
            foreach (char glyph in cells)
            {
                switch (glyph)
                {
                    case 'x':
                        events.Add(RhythmEvent.Hit(pos, cellTicks));
                        break;
                    case '.':
                        break;
                    default:
                        throw new FormatException(
                            $"Voice '{label}' cell '{glyph}' is invalid (allowed: x = hit, . = no hit).");
                }

                pos += cellTicks;
            }
        }

        if (pos != ts.BarTicks)
        {
            throw new FormatException(
                $"Voice '{label}' bar spans {pos / ts.BeatTicks} beat(s), expected {ts.BarTicks / ts.BeatTicks}.");
        }

        return events;
    }

    private static string StripComment(string line)
    {
        int hash = line.IndexOf('#');
        return hash < 0 ? line.Replace("\r", string.Empty) : line[..hash];
    }

    // True when a whole token is just ":n" — a subdivision marker, never a cell run.
    private static bool IsBareSubdivision(string token) =>
        token.Length >= 2 && token[0] == ':' && token.AsSpan(1).IndexOfAnyExcept(Digits) < 0;

    // Parse and validate a subdivision: a positive integer that divides BeatTicks (so cell ticks are whole).
    private static int ParseSubdivision(string text, string owner, TimeSignature ts)
    {
        if (!int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out int n) || n < 1)
        {
            throw new FormatException($"\"{owner}\" has a non-numeric or non-positive subdivision \"{text}\".");
        }

        if (ts.BeatTicks % n != 0)
        {
            throw new FormatException(
                $"\"{owner}\" uses subdivision {n}, which does not divide a beat of {ts.BeatTicks} ticks.");
        }

        return n;
    }
}
