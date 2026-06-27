namespace ChordFlow.Music.Harmony;

/// <summary>
/// The single authority for naming a semitone distance — the interval peer of
/// <see cref="NoteSpeller"/> (which spells pitch classes per key). It owns two label spaces:
/// <list type="bullet">
/// <item><see cref="Name"/> — the flats-only, role-free <b>substrate vocabulary</b>, computed
/// and <b>unfolded</b> so the second octave yields <c>9/10/11/13…</c> for free (used by scales /
/// arpeggios, which have real octaves).</item>
/// <item><see cref="Label"/> — the <b>chord-context</b> spelling: role-keyed chord tones
/// (<c>R/b3/3/b5/5/#5/6/b7/bb7/7</c>) falling back to the conventional compound tensions
/// (<c>b9/9/#9/11/#11/b13/13</c>) for a note outside the quality.</item>
/// </list>
/// The two differ by design: <see cref="Name"/> is indexed by the <i>absolute</i> semitone
/// (octaves are real); <see cref="Label"/> is indexed by <c>(pitch-class mod-12, role)</c>
/// (octaves folded by function — a tension reads <c>9</c> regardless of register).
/// </summary>
public static class IntervalSpeller
{
    // The only data: per pitch-class step (0..11) its flats accidental and its base degree number.
    // Every octave is derived from this by the formula in Name — no per-degree literal to maintain.
    private static readonly (string Accidental, int Number)[] FlatsBase =
    {
        ("", 1),  ("b", 2), ("", 2), ("b", 3), ("", 3), ("", 4),
        ("b", 5), ("", 5),  ("b", 6), ("", 6), ("b", 7), ("", 7),
    };

    /// <summary>
    /// The flats-only degree name of <paramref name="semitone"/> from the tonic, role-free and
    /// <b>unfolded</b>: <c>0→"1" … 11→"7", 12→"8", 14→"9", 17→"11", 21→"13", 24→"15"</c>, and on up.
    /// The root glyph is <c>"1"</c> (a counted scale degree), distinct from <see cref="Label"/>'s
    /// <c>"R"</c> (a chord root). Negative distances are not supported.
    /// </summary>
    public static string Name(int semitone)
    {
        if (semitone < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(semitone), semitone, "Interval degrees are named from a non-negative distance.");
        }

        (string accidental, int baseNumber) = FlatsBase[semitone % 12];
        int number = baseNumber + 7 * (semitone / 12); // +7 scale-steps per octave: 2→9, 4→11, 6→13
        return $"{accidental}{number}";
    }

    // Semitone of each natural scale degree (1..7) within one octave — the major-scale steps.
    // Compound degrees (8..) unfold from this by +12 per octave; the inverse of Name's +7-per-octave formula.
    private static readonly int[] NaturalDegreeSemitone = { 0, 2, 4, 5, 7, 9, 11 };

    /// <summary>
    /// The <b>inverse</b> of <see cref="Name"/>: the semitone distance of an interval label such as
    /// <c>"1"</c>, <c>"b3"</c>, <c>"#4"</c>, <c>"5"</c>, <c>"b7"</c>, or a compound <c>"9"/"11"/"13"</c>.
    /// Accepts <b>flats (<c>b</c>), sharps (<c>#</c>), and naturals</b>, including <b>repeated accidentals</b>
    /// (<c>bb7</c> the dim7 double-flat seventh) — so it parses the scale/chord spellings (<c>#4</c> lydian,
    /// <c>#5</c>, <c>#9</c>, <c>#11</c>, <c>bb7</c>) that <see cref="Name"/> (single-flats-only) never emits.
    /// Compound degrees are <b>unfolded</b> (<c>9→14, 11→17, 13→21</c>), mirroring <see cref="Name"/>'s domain,
    /// so <c>Parse(Name(n)) == n</c> for every non-negative <c>n</c>. A token is an optional run of <c>b</c>/<c>#</c>
    /// (each ∓1 semitone) followed by a degree number ≥ 1. The vocabulary lives here, never re-authored by callers.
    /// </summary>
    /// <exception cref="FormatException"><paramref name="token"/> is empty or not a valid interval label.</exception>
    public static int Parse(string token)
    {
        (int accidental, int degree) = ParseToken(token);
        int step = (degree - 1) % 7;
        int octave = (degree - 1) / 7;
        return 12 * octave + NaturalDegreeSemitone[step] + accidental;
    }

    /// <summary>
    /// The scale-<b>degree number</b> of an interval label, accidentals stripped: <c>"b3"→3</c>, <c>"6"→6</c>,
    /// <c>"bb7"→7</c>, <c>"9"→9</c>. The degree (not the semitone) is what names a chord tone's function, so two
    /// enharmonic spellings of the same pitch (<c>bb7</c> vs <c>6</c>, both semitone 9) read as different degrees.
    /// </summary>
    /// <exception cref="FormatException"><paramref name="token"/> is empty or not a valid interval label.</exception>
    public static int Degree(string token) => ParseToken(token).Degree;

    // Shared parse for Parse/Degree: an optional run of b/# accidentals (each ∓1 semitone) then a degree number ≥ 1.
    private static (int Accidental, int Degree) ParseToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new FormatException("An interval token cannot be empty.");
        }

        string t = token.Trim();
        int accidental = 0;
        int i = 0;
        for (; i < t.Length && (t[i] == 'b' || t[i] == '#'); i++)
        {
            accidental += t[i] == 'b' ? -1 : 1;
        }

        if (i == t.Length || !int.TryParse(t.AsSpan(i), out int degree) || degree < 1)
        {
            throw new FormatException(
                $"'{token}' is not a valid interval label (expected an optional run of 'b'/'#' then a degree number ≥ 1).");
        }

        return (accidental, degree);
    }

    /// <summary>
    /// Parse a whitespace/comma-separated interval set (e.g. <c>"1 b3 4 5 b7"</c>) into its distinct semitones,
    /// in input order — the entry point a scale/interval-set producer parses user text through.
    /// </summary>
    /// <exception cref="FormatException">Any token is not a valid interval label (see <see cref="Parse"/>).</exception>
    public static IReadOnlyList<int> ParseSet(string tokens)
    {
        ArgumentNullException.ThrowIfNull(tokens);

        var result = new List<int>();
        foreach (string token in tokens.Split(
            new[] { ' ', '\t', '\n', '\r', ',' }, StringSplitOptions.RemoveEmptyEntries))
        {
            int semitone = Parse(token);
            if (!result.Contains(semitone))
            {
                result.Add(semitone);
            }
        }

        return result;
    }

    // The conventional compound-tension names for a note that is NOT a chord tone (role-free fallback).
    private static readonly string[] Tension =
    {
        "R", "b9", "9", "#9", "3", "11", "#11", "5", "b13", "13", "b7", "7",
    };

    /// <summary>
    /// The chord-context label of <paramref name="semitone"/> given its chord-tone
    /// <paramref name="role"/>. Spelling is <b>role-keyed</b>, not pitch-keyed: the same pitch
    /// class spells differently by role (semitone 3 is <c>b3</c> as a third but <c>#9</c> as a
    /// tension; 8 is <c>#5</c> as a fifth but <c>b13</c> as a tension; 9 is <c>bb7</c> as a
    /// seventh but <c>13</c> as a tension). A <see langword="null"/> role (a note outside the
    /// quality) gets the conventional compound tension name. The semitone is reduced mod-12.
    /// </summary>
    public static string Label(int semitone, ChordToneFunction? role)
    {
        int s = ((semitone % 12) + 12) % 12;
        return role switch
        {
            ChordToneFunction.Root => "R",
            ChordToneFunction.Third => s == 3 ? "b3" : "3",
            ChordToneFunction.Fifth => s switch { 6 => "b5", 8 => "#5", _ => "5" },
            ChordToneFunction.Sixth => "6",
            ChordToneFunction.Seventh => s switch { 9 => "bb7", 11 => "7", _ => "b7" },
            _ => Tension[s],
        };
    }
}
