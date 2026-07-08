using ChordFlow.Music.Rhythm;
using ChordFlow.Music.Harmony;
using System.Globalization;
using System.Text;

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
    /// <param name="allowVoicingAnnotations">When <c>false</c> (the default, for a stored/standalone
    /// progression) a per-chord <c>{…}</c> voicing annotation is rejected — progressions stay pure
    /// harmony (req <c>IN7</c>). A Song passes <c>true</c> when parsing an <b>inline</b> progression, where
    /// the annotation is an arrangement concern.</param>
    public static Progression Parse(string id, string name, string dsl, TimeSignature ts, bool allowVoicingAnnotations = false)
    {
        ArgumentNullException.ThrowIfNull(dsl);

        string[] barTokens = TokenizeBars(dsl);
        if (barTokens.Length == 0)
        {
            throw new FormatException("Progression DSL is empty.");
        }

        var bars = new HarmonicBar[barTokens.Length];
        for (int i = 0; i < barTokens.Length; i++)
        {
            bars[i] = ParseBar(barTokens[i], ts, allowVoicingAnnotations);
        }

        // Re-uses Step 1's per-bar validation (sum == BarTicks, > 0, quarter-aligned).
        return Progression.FromBars(id, name, bars, ts);
    }

    /// <summary>
    /// Parse a single chord symbol — <c>[accidental]&lt;degree&gt;&lt;quality&gt;</c> (<c>17</c>, <c>#4dim7</c>,
    /// <c>2-7</c>) — into a <see cref="RomanDegree"/>, with no <c>:slots</c> or annotation. Shared with the
    /// Song <c>voice</c> directive's degree-scoped selector so that selector grammar is exactly a chord token.
    /// </summary>
    public static RomanDegree ParseChordSymbol(string token)
    {
        ArgumentNullException.ThrowIfNull(token);
        return ParseDegreeQuality(token, token);
    }

    /// <summary>
    /// Map a bare quality suffix (<c>""</c> Major, <c>7</c>, <c>m7</c>/<c>-7</c>, <c>dim7</c>, …) to its
    /// <see cref="Quality"/>. Shared with the Song <c>voice</c> directive's <c>*&lt;quality&gt;</c> wildcard
    /// selector. Throws <see cref="FormatException"/> on an unknown suffix.
    /// </summary>
    public static Quality ParseQualitySuffix(string suffix)
    {
        ArgumentNullException.ThrowIfNull(suffix);
        return QualitySuffixes.TryGetValue(suffix, out Quality quality)
            ? quality
            : throw new FormatException($"Unknown quality suffix \"{suffix}\".");
    }

    // Split the DSL into space-separated bar tokens while keeping a `{…}` voicing annotation atomic (its
    // grip carries internal spaces). An annotation-only token (starts with '{') binds to the immediately
    // preceding chord token, whether or not a space separated them — `{` can never begin a bar/chord.
    private static string[] TokenizeBars(string dsl)
    {
        var tokens = new List<string>();
        var current = new StringBuilder();
        int depth = 0;

        foreach (char c in dsl)
        {
            if (c == '{')
            {
                depth++;
                current.Append(c);
            }
            else if (c == '}')
            {
                if (depth == 0)
                {
                    throw new FormatException("Progression DSL has an unmatched '}'.");
                }

                depth--;
                current.Append(c);
            }
            else if (char.IsWhiteSpace(c) && depth == 0)
            {
                if (current.Length > 0)
                {
                    tokens.Add(current.ToString());
                    current.Clear();
                }
            }
            else
            {
                current.Append(c);
            }
        }

        if (depth != 0)
        {
            throw new FormatException("Progression DSL has an unclosed '{'.");
        }

        if (current.Length > 0)
        {
            tokens.Add(current.ToString());
        }

        var folded = new List<string>();
        foreach (string tok in tokens)
        {
            if (tok.StartsWith('{'))
            {
                if (folded.Count == 0)
                {
                    throw new FormatException($"Voicing annotation \"{tok}\" has no chord to attach to.");
                }

                folded[^1] += tok;
            }
            else
            {
                folded.Add(tok);
            }
        }

        return folded.ToArray();
    }

    private static HarmonicBar ParseBar(string barToken, TimeSignature ts, bool allowVoicingAnnotations)
    {
        string[] chordTokens = barToken.Split('_');
        if (chordTokens.Any(string.IsNullOrEmpty))
        {
            throw new FormatException($"Bar \"{barToken}\" has an empty chord (a stray or trailing '_').");
        }

        // Parse each chord's degree/quality, optional explicit slot count, and optional voicing annotation.
        var degrees = new RomanDegree[chordTokens.Length];
        var slots = new int?[chordTokens.Length];
        var annotations = new string?[chordTokens.Length];
        for (int i = 0; i < chordTokens.Length; i++)
        {
            (degrees[i], slots[i], annotations[i]) = ParseChord(chordTokens[i], allowVoicingAnnotations);
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
            spans[i] = new ChordSpan(degrees[i], tickDurations[i], annotations[i]);
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

    private static (RomanDegree Degree, int? Slots, string? Annotation) ParseChord(string chordToken, bool allowVoicingAnnotations)
    {
        // Peel off a trailing `{…}` voicing annotation first — it may hold ':' (root:6@8) and spaces, so it
        // must come off before the ':slots' split. It is the last element of the chord token.
        string core = chordToken;
        string? annotation = null;
        int brace = chordToken.IndexOf('{');
        if (brace >= 0)
        {
            if (chordToken[^1] != '}')
            {
                throw new FormatException($"Chord \"{chordToken}\" — a voicing annotation must be the last part of the chord.");
            }

            core = chordToken[..brace];
            annotation = chordToken[(brace + 1)..^1].Trim();

            if (core.Length == 0)
            {
                throw new FormatException($"Voicing annotation \"{chordToken}\" has no chord to attach to.");
            }

            if (annotation.Length == 0 || annotation.Contains('{') || annotation.Contains('}'))
            {
                throw new FormatException($"Chord \"{chordToken}\" has a malformed voicing annotation.");
            }

            if (!allowVoicingAnnotations)
            {
                throw new FormatException(
                    "Voicing annotations are a Song-level concern — a stored progression carries pure harmony only.");
            }
        }

        string[] parts = core.Split(':');
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

        return (degree, slots, annotation);
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
