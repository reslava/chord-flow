using ChordFlow.Music.Rhythm;
using ChordFlow.Music.Harmony;
using System.Globalization;

namespace ChordFlow.Music.Progressions;

/// <summary>
/// Pure Nashville-style DSL parser for <see cref="Progression"/>s (peer of <see cref="NoteSpeller"/>),
/// implementing the locked M1 grammar (design §3):
/// <list type="bullet">
/// <item>space = bar separator, <c>_</c> = chord separator within a bar.</item>
/// <item>token = <c>&lt;degree:1..7&gt;&lt;quality?&gt;[:&lt;slots&gt;]</c>.</item>
/// <item>Quality suffixes map onto the 8-value <see cref="Quality"/> enum.</item>
/// <item>Duration is all-or-nothing per bar: with no <c>:slots</c> the chords split the bar evenly
///   (valid only when each share is quarter-aligned, i.e. n ∈ {1,2,4} in 4/4); with <c>:slots</c> on
///   every chord, each carries that many quarter slots (×<see cref="TimeSignature.BeatTicks"/>) and they
///   must sum to the bar's beat count. Mixing the two modes in one bar is an error.</item>
/// </list>
/// Bad input throws <see cref="FormatException"/> naming the offending token; structural validation
/// (spans sum to the bar, &gt; 0, quarter-aligned) is delegated to <see cref="Progression.FromBars"/>.
/// </summary>
public static class ProgressionParser
{
    // Exact full-suffix → quality map. The empty suffix is plain Major.
    private static readonly IReadOnlyDictionary<string, Quality> QualitySuffixes =
        new Dictionary<string, Quality>(StringComparer.Ordinal)
        {
            [""] = Quality.Major,
            ["-"] = Quality.Minor,
            ["m"] = Quality.Minor,
            ["7"] = Quality.Dominant7,
            ["-7"] = Quality.Minor7,
            ["m7"] = Quality.Minor7,
            ["maj7"] = Quality.Major7,
            ["^7"] = Quality.Major7,
            ["°"] = Quality.Diminished,
            ["dim"] = Quality.Diminished,
            ["°7"] = Quality.Diminished7,
            ["dim7"] = Quality.Diminished7,
            ["ø"] = Quality.HalfDiminished7,
            ["m7b5"] = Quality.HalfDiminished7,
            ["+"] = Quality.Augmented,
            ["aug"] = Quality.Augmented,
        };

    /// <summary>
    /// Parse <paramref name="dsl"/> into a validated <see cref="Progression"/> in
    /// <paramref name="ts"/>. Throws <see cref="FormatException"/> on malformed input.
    /// </summary>
    public static Progression Parse(string id, string name, string dsl, TimeSignature ts)
    {
        ArgumentNullException.ThrowIfNull(dsl);

        string[] barTokens = dsl.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (barTokens.Length == 0)
        {
            throw new FormatException("Progression DSL is empty.");
        }

        var bars = new HarmonicBar[barTokens.Length];
        for (int i = 0; i < barTokens.Length; i++)
        {
            bars[i] = ParseBar(barTokens[i], ts);
        }

        // Re-uses Step 1's per-bar validation (sum == BarTicks, > 0, quarter-aligned).
        return Progression.FromBars(id, name, bars, ts);
    }

    private static HarmonicBar ParseBar(string barToken, TimeSignature ts)
    {
        string[] chordTokens = barToken.Split('_');
        if (chordTokens.Any(string.IsNullOrEmpty))
        {
            throw new FormatException($"Bar \"{barToken}\" has an empty chord (a stray or trailing '_').");
        }

        // Parse each chord's degree/quality and optional explicit slot count.
        var degrees = new RomanDegree[chordTokens.Length];
        var slots = new int?[chordTokens.Length];
        for (int i = 0; i < chordTokens.Length; i++)
        {
            (degrees[i], slots[i]) = ParseChord(chordTokens[i]);
        }

        int beatsPerBar = ts.BarTicks / ts.BeatTicks;
        int explicitCount = slots.Count(s => s.HasValue);

        int[] tickDurations;
        if (explicitCount == 0)
        {
            tickDurations = EvenSplit(chordTokens, barToken, ts);
        }
        else if (explicitCount == chordTokens.Length)
        {
            tickDurations = ExplicitSplit(slots!, barToken, ts, beatsPerBar);
        }
        else
        {
            throw new FormatException(
                $"Bar \"{barToken}\" mixes explicit ':slots' with even-split chords — use one mode for the whole bar.");
        }

        var spans = new ChordSpan[chordTokens.Length];
        for (int i = 0; i < chordTokens.Length; i++)
        {
            spans[i] = new ChordSpan(degrees[i], tickDurations[i]);
        }

        return new HarmonicBar(spans);
    }

    private static int[] EvenSplit(string[] chordTokens, string barToken, TimeSignature ts)
    {
        int n = chordTokens.Length;
        // Each chord gets BarTicks / n; valid only when that is a whole, quarter-aligned span
        // (n ∈ {1,2,4} in 4/4). n = 3 yields 64 ticks → not quarter-aligned → error.
        if (ts.BarTicks % n != 0 || (ts.BarTicks / n) % ts.BeatTicks != 0)
        {
            throw new FormatException(
                $"Bar \"{barToken}\" cannot be split evenly into {n} quarter-aligned chords — " +
                $"use the ':slots' suffix for uneven layouts (e.g. 3 chords).");
        }

        int each = ts.BarTicks / n;
        var ticks = new int[n];
        Array.Fill(ticks, each);
        return ticks;
    }

    private static int[] ExplicitSplit(int?[] slots, string barToken, TimeSignature ts, int beatsPerBar)
    {
        int sum = 0;
        var ticks = new int[slots.Length];
        for (int i = 0; i < slots.Length; i++)
        {
            int s = slots[i]!.Value;
            if (s < 1 || s > beatsPerBar)
            {
                throw new FormatException(
                    $"Bar \"{barToken}\" has a ':slots' value {s} outside 1..{beatsPerBar}.");
            }

            ticks[i] = s * ts.BeatTicks;
            sum += s;
        }

        if (sum != beatsPerBar)
        {
            throw new FormatException(
                $"Bar \"{barToken}\" ':slots' sum to {sum} quarters, expected {beatsPerBar}.");
        }

        return ticks;
    }

    private static (RomanDegree Degree, int? Slots) ParseChord(string chordToken)
    {
        string[] parts = chordToken.Split(':');
        if (parts.Length > 2)
        {
            throw new FormatException($"Chord \"{chordToken}\" has more than one ':slots' suffix.");
        }

        RomanDegree degree = ParseDegreeQuality(parts[0], chordToken);

        int? slots = null;
        if (parts.Length == 2)
        {
            if (!int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out int s))
            {
                throw new FormatException($"Chord \"{chordToken}\" has a non-numeric ':slots' value \"{parts[1]}\".");
            }

            slots = s;
        }

        return (degree, slots);
    }

    private static RomanDegree ParseDegreeQuality(string text, string chordToken)
    {
        // An optional single leading '#'/'b' chromatically alters the degree's root (e.g. "#4dim7",
        // "b27"). Only one accidental is allowed — "##4"/"#b4" and a bare "#"/"b" are rejected because
        // a digit must follow the accidental.
        Accidental accidental = Accidental.Natural;
        int start = 0;
        if (text.Length > 0 && (text[0] == '#' || text[0] == 'b'))
        {
            accidental = text[0] == '#' ? Accidental.Sharp : Accidental.Flat;
            start = 1;
        }

        // The degree is exactly one digit (1..7); everything after it is the quality suffix — note the
        // suffixes themselves contain digits (e.g. "7" Dominant7, "-7" Minor7, "m7b5"), so we never
        // greedily swallow them into the degree.
        if (text.Length <= start || !char.IsDigit(text[start]))
        {
            throw new FormatException($"Chord \"{chordToken}\" is missing a scale degree.");
        }

        int degree = text[start] - '0';
        if (degree < 1 || degree > 7)
        {
            throw new FormatException($"Chord \"{chordToken}\" has degree {degree} outside 1..7.");
        }

        string suffix = text[(start + 1)..];
        if (!QualitySuffixes.TryGetValue(suffix, out Quality quality))
        {
            throw new FormatException($"Chord \"{chordToken}\" has an unknown quality suffix \"{suffix}\".");
        }

        return new RomanDegree(degree, quality, accidental);
    }
}
