using System.Globalization;

namespace ChordFlow.Domain;

/// <summary>
/// Pure DSL parser for authored voicings — the harmonic peer of <see cref="ProgressionParser"/> /
/// <see cref="RhythmPatternParser"/>. Grammar:
/// <code>voicing &lt;Chord&gt;  shape:&lt;C|A|G|E|D&gt;  root:&lt;6..1&gt;  frets: &lt;s6 s5 s4 s3 s2 s1&gt;</code>
/// <list type="bullet">
/// <item><c>&lt;Chord&gt;</c> = the anchor chord: note name + quality suffix (<c>Cmaj</c>, <c>C7</c>,
///   <c>Ebm7</c>, …). The authoring convention is <b>C</b>, but any anchor is accepted. The quality is
///   what the book matches; the root pitch is the transpose anchor.</item>
/// <item><c>frets</c> = six absolute frets at the anchor, low-E→high-E (alphaTab strings 6→1);
///   <c>x</c> = muted, <c>0</c> = open.</item>
/// <item><c>shape</c> = the CAGED family (metadata); <c>root</c> = the alphaTab string (6..1) sounding
///   the root.</item>
/// </list>
/// The voicing is normalized to its <b>lowest non-negative C placement</b> — any anchor folds onto a
/// single canonical-C form, so <c>(Quality, Shape)</c> dedups — because every voicing is inherently
/// movable (open ↔ barre under transpose). A trailing <c>#</c> comment is ignored. Bad input throws
/// <see cref="FormatException"/> naming the offending clause.
/// </summary>
public static class VoicingDslParser
{
    private const string Keyword = "voicing";
    private const string ShapePrefix = "shape:";
    private const string RootPrefix = "root:";
    private const string FretsPrefix = "frets:";

    // Anchor-chord quality suffixes — the same vocabulary as ProgressionParser's Nashville suffixes,
    // keyed off a note name instead of a scale degree (kept in sync by convention; a shared suffix
    // table is a future cleanup). The empty suffix is plain Major.
    private static readonly IReadOnlyDictionary<string, Quality> QualitySuffixes =
        new Dictionary<string, Quality>(StringComparer.Ordinal)
        {
            [""] = Quality.Major,
            ["maj"] = Quality.Major,
            ["-"] = Quality.Minor,
            ["m"] = Quality.Minor,
            ["min"] = Quality.Minor,
            ["7"] = Quality.Dominant7,
            ["-7"] = Quality.Minor7,
            ["m7"] = Quality.Minor7,
            ["min7"] = Quality.Minor7,
            ["maj7"] = Quality.Major7,
            ["^7"] = Quality.Major7,
            ["°"] = Quality.Diminished,
            ["dim"] = Quality.Diminished,
            ["ø"] = Quality.HalfDiminished7,
            ["m7b5"] = Quality.HalfDiminished7,
            ["+"] = Quality.Augmented,
            ["aug"] = Quality.Augmented,
        };

    // Natural note letter → pitch class (C = 0 .. B = 11), before any accidental.
    private static readonly IReadOnlyDictionary<char, int> NaturalPc =
        new Dictionary<char, int>
        {
            ['C'] = 0, ['D'] = 2, ['E'] = 4, ['F'] = 5, ['G'] = 7, ['A'] = 9, ['B'] = 11,
        };

    /// <summary>
    /// Parse one <c>voicing …</c> line into a canonical-C <see cref="VoicingShape"/>. Throws
    /// <see cref="FormatException"/> on malformed input.
    /// </summary>
    public static VoicingShape Parse(string dsl)
    {
        ArgumentNullException.ThrowIfNull(dsl);

        // Strip a trailing "# comment" then surrounding whitespace.
        string line = dsl;
        int hash = line.IndexOf('#');
        if (hash >= 0)
        {
            line = line[..hash];
        }

        line = line.Trim();
        if (line.Length == 0)
        {
            throw new FormatException("Voicing DSL is empty.");
        }

        // The frets: clause carries spaces, so split it off the end; the header holds the rest.
        int fretsAt = line.IndexOf(FretsPrefix, StringComparison.OrdinalIgnoreCase);
        if (fretsAt < 0)
        {
            throw new FormatException($"Voicing \"{dsl}\" is missing the 'frets:' clause.");
        }

        string header = line[..fretsAt].Trim();
        string fretList = line[(fretsAt + FretsPrefix.Length)..].Trim();

        string[] headerTokens = header.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        if (headerTokens.Length == 0 || !headerTokens[0].Equals(Keyword, StringComparison.OrdinalIgnoreCase))
        {
            throw new FormatException($"Voicing \"{dsl}\" must start with 'voicing'.");
        }

        string? chordToken = null;
        CagedShape? shape = null;
        int? rootString = null;

        for (int i = 1; i < headerTokens.Length; i++)
        {
            string tok = headerTokens[i];
            if (tok.StartsWith(ShapePrefix, StringComparison.OrdinalIgnoreCase))
            {
                shape = ParseShape(tok[ShapePrefix.Length..], dsl);
            }
            else if (tok.StartsWith(RootPrefix, StringComparison.OrdinalIgnoreCase))
            {
                rootString = ParseRootString(tok[RootPrefix.Length..], dsl);
            }
            else if (chordToken is null)
            {
                chordToken = tok;
            }
            else
            {
                throw new FormatException($"Voicing \"{dsl}\" has an unexpected token \"{tok}\".");
            }
        }

        if (chordToken is null)
        {
            throw new FormatException($"Voicing \"{dsl}\" is missing the anchor chord.");
        }

        if (shape is null)
        {
            throw new FormatException($"Voicing \"{dsl}\" is missing 'shape:'.");
        }

        if (rootString is null)
        {
            throw new FormatException($"Voicing \"{dsl}\" is missing 'root:'.");
        }

        (PitchClass anchorRoot, Quality quality) = ParseChord(chordToken, dsl);
        (IReadOnlyList<FretPosition> positions, IReadOnlyList<int> muted) = ParseFrets(fretList, dsl);
        Voicing canonical = NormalizeToC(positions, muted, anchorRoot);

        return new VoicingShape(quality, shape.Value, rootString.Value, canonical);
    }

    // Transpose every fretted string so the anchor sits at C, then octave-fold uniformly so the lowest
    // fret lands in [0, 11] — the canonical lowest non-negative placement. Muted strings are untouched;
    // open strings (fret 0) ride the transpose and become fretted (the open ↔ barre identity).
    private static Voicing NormalizeToC(
        IReadOnlyList<FretPosition> positions, IReadOnlyList<int> muted, PitchClass anchorRoot)
    {
        int semisToC = Mod12(-anchorRoot.Value); // transpose the anchor up to the next C (0..11)

        var shifted = positions
            .Select(p => new FretPosition(p.String, p.Fret + semisToC))
            .ToList();

        int min = shifted.Min(p => p.Fret);
        int fold = 0;
        while (min + fold >= 12)
        {
            fold -= 12;
        }

        while (min + fold < 0)
        {
            fold += 12;
        }

        if (fold != 0)
        {
            shifted = shifted.Select(p => new FretPosition(p.String, p.Fret + fold)).ToList();
        }

        int firstFret = shifted.Min(p => p.Fret);
        return new Voicing(
            shifted,
            BarreFret: null,
            FirstFret: firstFret,
            MutedStrings: muted.Count > 0 ? muted : null);
    }

    private static CagedShape ParseShape(string text, string dsl)
    {
        if (text.Length == 1
            && char.IsAsciiLetter(text[0])
            && Enum.TryParse(text.ToUpperInvariant(), out CagedShape shape)
            && Enum.IsDefined(shape))
        {
            return shape;
        }

        throw new FormatException($"Voicing \"{dsl}\" has an invalid shape \"{text}\" (expected one of C A G E D).");
    }

    private static int ParseRootString(string text, string dsl)
    {
        if (!int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out int s) || s < 1 || s > Fretboard.StringCount)
        {
            throw new FormatException($"Voicing \"{dsl}\" has root string \"{text}\" outside 1..{Fretboard.StringCount}.");
        }

        return s;
    }

    private static (PitchClass Root, Quality Quality) ParseChord(string token, string dsl)
    {
        char letter = char.ToUpperInvariant(token[0]);
        if (!NaturalPc.TryGetValue(letter, out int pc))
        {
            throw new FormatException($"Voicing \"{dsl}\" has an unknown note name in \"{token}\".");
        }

        int idx = 1;
        if (idx < token.Length && (token[idx] == '#' || token[idx] == 'b'))
        {
            pc += token[idx] == '#' ? 1 : -1;
            idx++;
        }

        string suffix = token[idx..];
        if (!QualitySuffixes.TryGetValue(suffix, out Quality quality))
        {
            throw new FormatException($"Voicing \"{dsl}\" has an unknown quality suffix \"{suffix}\".");
        }

        return (new PitchClass(Mod12(pc)), quality);
    }

    private static (IReadOnlyList<FretPosition> Positions, IReadOnlyList<int> Muted) ParseFrets(string fretList, string dsl)
    {
        string[] tokens = fretList.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length != Fretboard.StringCount)
        {
            throw new FormatException(
                $"Voicing \"{dsl}\" needs {Fretboard.StringCount} fret values (low-E→high-E), got {tokens.Length}.");
        }

        var positions = new List<FretPosition>();
        var muted = new List<int>();
        for (int i = 0; i < tokens.Length; i++)
        {
            int stringNumber = Fretboard.StringCount - i; // token 0 = s6 (low E) .. token 5 = s1 (high E)
            string t = tokens[i];

            if (t is "x" or "X")
            {
                muted.Add(stringNumber);
                continue;
            }

            if (!int.TryParse(t, NumberStyles.None, CultureInfo.InvariantCulture, out int fret) || fret < 0)
            {
                throw new FormatException(
                    $"Voicing \"{dsl}\" has an invalid fret \"{t}\" (use a non-negative number or 'x').");
            }

            positions.Add(new FretPosition(stringNumber, fret));
        }

        if (positions.Count == 0)
        {
            throw new FormatException($"Voicing \"{dsl}\" has no fretted strings.");
        }

        return (positions, muted);
    }

    private static int Mod12(int value) => ((value % 12) + 12) % 12;
}
