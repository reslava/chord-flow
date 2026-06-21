using System.Globalization;

namespace ChordFlow.Music.Rhythm;

/// <summary>
/// Pure DSL parser for <see cref="RhythmPattern"/>s — the rhythmic peer of <see cref="ProgressionParser"/>
/// and <c>SongParser</c>, producing the existing tick-grid types from a character grid (design §5).
/// <list type="bullet">
/// <item>Space separates <b>subdivision runs</b>; a run's cells split into consecutive beats by count, so
///   a same-<c>n</c> run may omit inner spaces (<c>X...X...X...X...</c> = four beats). Spaces are only
///   needed to switch subdivision or attach a per-beat <c>:n</c> (model B).</item>
/// <item>Glyphs: <c>X</c> = attack (start a note), <c>.</c> = sustain (extend the current note or rest),
///   <c>-</c> = rest/mute (end the current note, start silence). A note's length runs to the next
///   <c>X</c>/<c>-</c> or the bar end (the locked sustain rule — never staccato).</item>
/// <item>Subdivision <c>:n</c> = cells per beat (default 4 = 16ths); <c>n</c> must divide
///   <see cref="TimeSignature.BeatTicks"/> and each run must hold a whole multiple of <c>n</c> cells. A
///   leading <c>:n</c> token sets the whole row's default; a <c>:n</c> suffix on a run overrides it
///   (mixed subdivisions). Cell ticks = <c>BeatTicks / n</c>.</item>
/// <item><c>|</c> separates bars (each parsed independently into a <see cref="PatternBar"/>); an optional
///   leading <c>PICKUP: &lt;grid&gt;</c> segment becomes a shorter-than-a-bar <see cref="PickupMeasure"/>.
///   Newlines are insignificant.</item>
/// </list>
/// Bad input throws <see cref="FormatException"/> naming the offending cell / run. Stroke and accent are
/// never authored here (C2).
/// </summary>
public static class RhythmPatternParser
{
    private const int DefaultSubdivision = 4;
    private const string PickupKeyword = "PICKUP:";

    private static readonly System.Buffers.SearchValues<char> Digits =
        System.Buffers.SearchValues.Create("0123456789");

    /// <summary>
    /// Parse <paramref name="dsl"/> into a validated <see cref="RhythmPattern"/> in <paramref name="ts"/>:
    /// an optional leading <c>PICKUP:</c> block, then one or more <c>|</c>-separated bars. Throws
    /// <see cref="FormatException"/> on malformed input.
    /// </summary>
    public static RhythmPattern Parse(string id, string name, string dsl, TimeSignature ts)
    {
        ArgumentNullException.ThrowIfNull(dsl);

        // Newlines are insignificant — collapse to spaces so authors can lay patterns out over lines.
        string normalized = dsl.Replace('\n', ' ').Replace('\r', ' ');

        string[] segments = normalized.Split('|');
        PickupMeasure? pickup = null;
        int firstBar = 0;

        if (segments[0].TrimStart().StartsWith(PickupKeyword, StringComparison.OrdinalIgnoreCase))
        {
            string grid = segments[0].TrimStart()[PickupKeyword.Length..];
            pickup = ParsePickup(grid, ts);
            firstBar = 1;

            if (segments.Length == 1)
            {
                throw new FormatException("Rhythm DSL has a PICKUP: block but no bars.");
            }
        }

        var bars = new List<PatternBar>(segments.Length - firstBar);
        for (int i = firstBar; i < segments.Length; i++)
        {
            bars.Add(new PatternBar(ParseBar(segments[i], ts)));
        }

        return new RhythmPattern(id, name, bars, ts, pickup);
    }

    /// <summary>Parse one <c>|</c>-delimited bar segment into its ordered <see cref="RhythmEvent"/>s.</summary>
    internal static IReadOnlyList<RhythmEvent> ParseBar(string barDsl, TimeSignature ts)
    {
        string[] tokens = barDsl.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length == 0)
        {
            throw new FormatException("Rhythm DSL bar is empty.");
        }

        (int rowDefault, int firstRun) = ReadRowDefault(tokens, ts);
        (List<RhythmEvent> events, int ticks) = Walk(tokens, firstRun, rowDefault, ts, requireWholeBeats: true);

        if (ticks != ts.BarTicks)
        {
            throw new FormatException(
                $"Bar \"{barDsl}\" spans {ticks / ts.BeatTicks} beat(s), expected {ts.BarTicks / ts.BeatTicks}.");
        }

        return events;
    }

    // A pickup is a shorter leading measure: same glyph/subdivision/walk rules, but any cell count
    // (1..cellsPerBar) — its LengthTicks is whatever the cells span, not a full bar (design §4).
    private static PickupMeasure ParsePickup(string grid, TimeSignature ts)
    {
        string[] tokens = grid.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length == 0)
        {
            throw new FormatException("PICKUP: block has no cells.");
        }

        (int rowDefault, int firstRun) = ReadRowDefault(tokens, ts);
        (List<RhythmEvent> events, int ticks) = Walk(tokens, firstRun, rowDefault, ts, requireWholeBeats: false);

        if (ticks <= 0 || ticks > ts.BarTicks)
        {
            throw new FormatException($"PICKUP: block spans {ticks} ticks, must be 1..{ts.BarTicks} (at most one bar).");
        }

        return new PickupMeasure(events, ticks);
    }

    // A leading bare ":n" token sets the row default for every run (a run always carries glyph cells, so
    // a token that is only ":n" can only be the row-level subdivision).
    private static (int RowDefault, int FirstRun) ReadRowDefault(string[] tokens, TimeSignature ts) =>
        IsRowSubdivision(tokens[0])
            ? (ParseSubdivision(tokens[0][1..], tokens[0], ts), 1)
            : (DefaultSubdivision, 0);

    // Walk the runs left→right, carrying the current note/rest state. Returns the emitted hits and the
    // total ticks spanned; a still-open note is closed at the end (a trailing rest needs no event — the
    // quantizer fills it). Shared by bars (requireWholeBeats) and the partial-length pickup.
    private static (List<RhythmEvent> Events, int Ticks) Walk(
        string[] tokens, int firstRun, int rowDefault, TimeSignature ts, bool requireWholeBeats)
    {
        var events = new List<RhythmEvent>();
        int pos = 0;
        int? openNoteStart = null;

        for (int t = firstRun; t < tokens.Length; t++)
        {
            string run = tokens[t];
            (string cells, int n) = SplitRun(run, rowDefault, ts, requireWholeBeats);
            int cellTicks = ts.BeatTicks / n;

            foreach (char glyph in cells)
            {
                switch (glyph)
                {
                    case 'X':
                        if (openNoteStart is int attackEnd)
                        {
                            events.Add(RhythmEvent.Hit(attackEnd, pos - attackEnd));
                        }

                        openNoteStart = pos;
                        break;

                    case '-':
                        if (openNoteStart is int restEnd)
                        {
                            events.Add(RhythmEvent.Hit(restEnd, pos - restEnd));
                        }

                        openNoteStart = null;
                        break;

                    case '.':
                        // Sustain — extend whatever is currently ringing (a note) or silent (a rest).
                        break;

                    default:
                        throw new FormatException(
                            $"Beat group \"{run}\" contains invalid glyph '{glyph}' (allowed: X . -).");
                }

                pos += cellTicks;
            }
        }

        if (openNoteStart is int finalStart)
        {
            events.Add(RhythmEvent.Hit(finalStart, pos - finalStart));
        }

        return (events, pos);
    }

    // True when a whole token is just ":n" — the row-level subdivision marker, never a run of cells.
    private static bool IsRowSubdivision(string token) =>
        token.Length >= 2 && token[0] == ':' && token.AsSpan(1).IndexOfAnyExcept(Digits) < 0;

    // Split a run into its glyph cells and effective subdivision, validating the cell count.
    private static (string Cells, int N) SplitRun(string run, int rowDefault, TimeSignature ts, bool requireWholeBeats)
    {
        string cells = run;
        int n = rowDefault;

        int colon = run.IndexOf(':');
        if (colon >= 0)
        {
            if (run.IndexOf(':', colon + 1) >= 0)
            {
                throw new FormatException($"Beat group \"{run}\" has more than one ':n' suffix.");
            }

            cells = run[..colon];
            n = ParseSubdivision(run[(colon + 1)..], run, ts);
        }

        if (cells.Length == 0)
        {
            throw new FormatException($"Beat group \"{run}\" has no cells.");
        }

        if (requireWholeBeats && cells.Length % n != 0)
        {
            throw new FormatException(
                $"Beat group \"{run}\" has {cells.Length} cell(s), not a whole multiple of subdivision {n}.");
        }

        return (cells, n);
    }

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
