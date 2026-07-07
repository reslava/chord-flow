using ChordFlow.Music.Progressions;
using ChordFlow.Music.Progressions.Transforms;
using ChordFlow.Music.Rhythm;
using ChordFlow.Music.Harmony;
using System.Globalization;
using System.Linq;

namespace ChordFlow.Music.Songs;

/// <summary>
/// Pure DSL parser for <see cref="Song"/>s (peer of <see cref="ProgressionParser"/>), no I/O. The grammar is
/// line-oriented with two regions — order-free <b>definitions</b> and an order-significant <b>stream</b>:
/// <list type="bullet">
/// <item><c>NAME = &lt;prog-dsl&gt;</c> — an inline part; the RHS is parsed verbatim by <see cref="ProgressionParser"/>.</item>
/// <item><c>NAME: &lt;stored-id&gt;</c> — a reference to a stored progression (resolved later by <see cref="SongExpander"/>).</item>
/// <item><c>key &lt;token&gt;</c> — sets <see cref="Song.InitialKey"/> when it precedes the stream; an <see cref="AbsoluteKey"/> reset once in the stream.</item>
/// <item><c>feel &lt;token&gt;</c> — sets <see cref="Song.DefaultFeel"/> (the whole-song groove default: <c>none</c>/<c>triplet8th</c>/<c>triplet16th</c>), at most once; position-independent.</item>
/// <item><c>tempo &lt;bpm&gt;</c> — sets <see cref="Song.DefaultTempo"/> (the play-time tempo seed, 40–240), at most once; position-independent.</item>
/// <item><c>NAME</c> / <c>NAME x&lt;n&gt;</c> — a <see cref="PartPlay"/> (<c>n</c> defaults to 1). The name must be a defined part.</item>
/// <item><c>mod &lt;spec&gt;</c> — a relative <see cref="RelativeMod"/> (<c>+n</c>/<c>-n</c> or a roman degree).</item>
/// </list>
/// <c>#</c> starts a line comment. A play line is <c>NAME ( x&lt;n&gt; | @name(args) )*</c>: at most one
/// <c>x&lt;n&gt;</c> section-repeat plus zero-or-more <c>@op</c> progression transforms, in either order
/// (the transform rewrites the progression, then the section repeats — order is fixed regardless of token
/// order). The <c>@name(args)</c> shape is lexed here and handed to <see cref="ProgressionTransform.Parse"/>
/// for construction. Grammar errors throw <see cref="FormatException"/> naming the offending line/token;
/// structural validation is delegated to <see cref="Song.FromSections"/>.
/// </summary>
public static class SongParser
{
    private const string KeyKeyword = "key";
    private const string ModKeyword = "mod";
    private const string FeelKeyword = "feel";
    private const string TempoKeyword = "tempo";

    // The play-time tempo seed accepts the same BPM window as the ScoreR transport's tempo input (40–240).
    private const int MinTempo = 40;
    private const int MaxTempo = 240;

    // Whole-song `feel <token>` idents → TripletFeel. Only the offered set is accepted (req IN2); the
    // reserved (dotted/scottish) enum members are deliberately not parseable yet.
    private static readonly IReadOnlyDictionary<string, TripletFeel> FeelTokens =
        new Dictionary<string, TripletFeel>(StringComparer.OrdinalIgnoreCase)
        {
            ["none"] = TripletFeel.None,
            ["triplet8th"] = TripletFeel.Triplet8th,
            ["triplet16th"] = TripletFeel.Triplet16th,
        };

    /// <summary>Parse <paramref name="dsl"/> (header-stripped Song body) into a validated <see cref="Song"/>.</summary>
    public static Song Parse(string id, string name, string dsl, TimeSignature ts)
    {
        ArgumentNullException.ThrowIfNull(dsl);

        var parts = new Dictionary<string, Part>(StringComparer.Ordinal);
        var streamLines = new List<string>();

        // Pass 1 — pull definitions into Parts (order-free); keep stream lines in order.
        foreach (string rawLine in dsl.Split('\n'))
        {
            string line = StripComment(rawLine).Trim();
            if (line.Length == 0)
            {
                continue;
            }

            if (TrySplitDefinition(line, out string defName, out string rhs, out bool isInline))
            {
                if (parts.ContainsKey(defName))
                {
                    throw new FormatException($"Song DSL defines part \"{defName}\" more than once.");
                }

                parts[defName] = isInline
                    ? new InlineProgression(defName, ProgressionParser.Parse(defName, defName, rhs, ts))
                    : new ProgressionReference(defName, rhs);
            }
            else
            {
                streamLines.Add(line);
            }
        }

        // Pass 2 — the order-significant stream.
        Key initialKey = new(new PitchClass(0), false);   // default C major (constraint C6)
        bool initialKeySet = false;
        TripletFeel? defaultFeel = null;                  // whole-song groove default; null = no preference (req IN7)
        int? defaultTempo = null;                         // play-time tempo seed; null = no preference (ChordFlow default 80 downstream)
        var items = new List<ArrangementItem>();

        foreach (string line in streamLines)
        {
            string[] tokens = line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            string head = tokens[0];

            if (head == KeyKeyword)
            {
                if (tokens.Length != 2)
                {
                    throw new FormatException($"Song DSL \"key\" line must be \"key <token>\": \"{line}\".");
                }

                Key key = ParseKey(tokens[1]);

                // A key line before any stream item sets the initial key; later it is an absolute reset.
                if (items.Count == 0 && !initialKeySet)
                {
                    initialKey = key;
                    initialKeySet = true;
                }
                else
                {
                    items.Add(new AbsoluteKey(key));
                }
            }
            else if (head == FeelKeyword)
            {
                // `feel <token>` is a whole-song groove default (not a stream item; position is irrelevant).
                if (tokens.Length != 2)
                {
                    throw new FormatException($"Song DSL \"feel\" line must be \"feel <token>\": \"{line}\".");
                }

                if (defaultFeel is not null)
                {
                    throw new FormatException($"Song DSL sets \"feel\" more than once: \"{line}\".");
                }

                if (!FeelTokens.TryGetValue(tokens[1], out TripletFeel feel))
                {
                    throw new FormatException(
                        $"Song DSL \"feel\" token \"{tokens[1]}\" is unknown (expected none, triplet8th, or triplet16th).");
                }

                defaultFeel = feel;
            }
            else if (head == TempoKeyword)
            {
                // `tempo <bpm>` is a whole-song play-time seed (not a stream item; position is irrelevant).
                if (tokens.Length != 2)
                {
                    throw new FormatException($"Song DSL \"tempo\" line must be \"tempo <bpm>\": \"{line}\".");
                }

                if (defaultTempo is not null)
                {
                    throw new FormatException($"Song DSL sets \"tempo\" more than once: \"{line}\".");
                }

                if (!int.TryParse(tokens[1], NumberStyles.None, CultureInfo.InvariantCulture, out int bpm)
                    || bpm < MinTempo || bpm > MaxTempo)
                {
                    throw new FormatException(
                        $"Song DSL \"tempo\" value \"{tokens[1]}\" must be an integer BPM in {MinTempo}–{MaxTempo}.");
                }

                defaultTempo = bpm;
            }
            else if (head == ModKeyword)
            {
                if (tokens.Length != 2)
                {
                    throw new FormatException($"Song DSL \"mod\" line must be \"mod <spec>\": \"{line}\".");
                }

                items.Add(new RelativeMod(ParseModSpec(tokens[1], line)));
            }
            else
            {
                string partName = tokens[0];
                if (!parts.ContainsKey(partName))
                {
                    throw new FormatException($"Song DSL plays undefined part \"{partName}\".");
                }

                int repeat = 1;
                bool repeatSet = false;
                var transforms = new List<IProgressionTransform>();

                // After the part name: one optional x<n> repeat plus any number of @op transforms, in any
                // order. Transforms apply to the progression, then the section repeats (semantics fixed).
                for (int t = 1; t < tokens.Length; t++)
                {
                    string token = tokens[t];
                    if (token.Length != 0 && token[0] == '@')
                    {
                        transforms.Add(ParseTransform(token, line));
                    }
                    else if (repeatSet)
                    {
                        throw new FormatException($"Song DSL play \"{line}\" has more than one repeat token.");
                    }
                    else
                    {
                        repeat = ParseRepeat(token, line);
                        repeatSet = true;
                    }
                }

                items.Add(new PartPlay(partName, repeat, transforms));
            }
        }

        return Song.FromSections(id, name, initialKey, parts, items, defaultFeel, defaultTempo);
    }

    private static string StripComment(string line)
    {
        int hash = line.IndexOf('#');
        return hash < 0 ? line : line[..hash];
    }

    // A definition has '=' (inline) or ':' (reference). '=' wins so an inline RHS may itself carry ':slots'.
    private static bool TrySplitDefinition(string line, out string name, out string rhs, out bool isInline)
    {
        name = "";
        rhs = "";
        isInline = false;

        int eq = line.IndexOf('=');
        int colon = line.IndexOf(':');

        if (eq >= 0)
        {
            isInline = true;
            name = line[..eq].Trim();
            rhs = line[(eq + 1)..].Trim();
        }
        else if (colon >= 0)
        {
            name = line[..colon].Trim();
            rhs = line[(colon + 1)..].Trim();
        }
        else
        {
            return false;
        }

        if (name.Length == 0 || name.Any(char.IsWhiteSpace))
        {
            throw new FormatException($"Song DSL definition has an invalid part name in \"{line}\".");
        }

        if (name is KeyKeyword or ModKeyword or FeelKeyword or TempoKeyword)
        {
            throw new FormatException($"Song DSL cannot define a part named \"{name}\" (reserved keyword).");
        }

        if (rhs.Length == 0)
        {
            throw new FormatException($"Song DSL definition of \"{name}\" has an empty body.");
        }

        return true;
    }

    // @name(args) → an IProgressionTransform. The lexical shape (the @, the parens, the split) is owned
    // here; the name→transform mapping and argument validation live in ProgressionTransform.Parse.
    private static IProgressionTransform ParseTransform(string token, string line)
    {
        int open = token.IndexOf('(');
        if (open < 0 || token[^1] != ')')
        {
            throw new FormatException(
                $"Song DSL transform \"{token}\" must look like \"@name(args)\" in \"{line}\".");
        }

        string name = token[1..open];
        string args = token[(open + 1)..^1];
        if (name.Length == 0)
        {
            throw new FormatException($"Song DSL transform \"{token}\" has no name in \"{line}\".");
        }

        return ProgressionTransform.Parse(name, args);
    }

    private static int ParseRepeat(string token, string line)
    {
        if (token.Length < 2 || (token[0] != 'x' && token[0] != 'X'))
        {
            throw new FormatException($"Song DSL repeat must look like \"x<n>\": got \"{token}\" in \"{line}\".");
        }

        if (!int.TryParse(token[1..], NumberStyles.None, CultureInfo.InvariantCulture, out int n) || n < 1)
        {
            throw new FormatException($"Song DSL repeat \"{token}\" must be a positive integer.");
        }

        return n;
    }

    // Note letter + optional accidental (#/b) + optional minor suffix (m/min). Major by default (v1 renders major).
    private static Key ParseKey(string token)
    {
        if (token.Length == 0)
        {
            throw new FormatException("Song DSL key token is empty.");
        }

        int basePc = char.ToUpperInvariant(token[0]) switch
        {
            'C' => 0, 'D' => 2, 'E' => 4, 'F' => 5, 'G' => 7, 'A' => 9, 'B' => 11,
            _ => throw new FormatException($"Song DSL key \"{token}\" has an unknown note letter."),
        };

        int i = 1;
        if (i < token.Length && token[i] is '#' or 'b')
        {
            basePc += token[i] == '#' ? 1 : -1;
            i++;
        }

        bool isMinor = false;
        string suffix = token[i..];
        if (suffix.Length != 0)
        {
            isMinor = suffix is "m" or "min"
                ? true
                : throw new FormatException($"Song DSL key \"{token}\" has an unknown suffix \"{suffix}\".");
        }

        return new Key(new PitchClass(Mod12(basePc)), isMinor);
    }

    // +n / -n, or an (optionally accidental-prefixed) roman degree. A lowercase numeral flips mode to minor.
    private static Modulation ParseModSpec(string spec, string line)
    {
        if (spec.Length == 0)
        {
            throw new FormatException($"Song DSL mod spec is empty in \"{line}\".");
        }

        if (spec[0] is '+' or '-')
        {
            if (int.TryParse(spec, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out int n))
            {
                return new Modulation(n, null);
            }

            throw new FormatException($"Song DSL mod spec \"{spec}\" is not a valid signed integer.");
        }

        int accidental = 0;
        int i = 0;
        if (spec[0] == 'b')
        {
            accidental = -1;
            i = 1;
        }
        else if (spec[0] == '#')
        {
            accidental = 1;
            i = 1;
        }

        string numeral = spec[i..];
        if (numeral.Length == 0)
        {
            throw new FormatException($"Song DSL mod spec \"{spec}\" has no roman numeral.");
        }

        bool isMinor = numeral.All(char.IsLower);
        int offset = numeral.ToUpperInvariant() switch
        {
            "I" => 0, "II" => 2, "III" => 4, "IV" => 5, "V" => 7, "VI" => 9, "VII" => 11,
            _ => throw new FormatException($"Song DSL mod spec \"{spec}\" has an unknown roman numeral."),
        };

        return new Modulation(Mod12(offset + accidental), isMinor ? true : null);
    }

    private static int Mod12(int value) => ((value % 12) + 12) % 12;
}
