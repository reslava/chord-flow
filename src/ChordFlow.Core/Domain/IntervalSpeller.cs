namespace ChordFlow.Domain;

/// <summary>
/// The single authority for naming a semitone distance — the interval peer of
/// <see cref="NoteSpeller"/> (which spells pitch classes per key). It owns two label spaces:
/// <list type="bullet">
/// <item><see cref="Name"/> — the flats-only, role-free <b>substrate vocabulary</b>, computed
/// and <b>unfolded</b> so the second octave yields <c>9/10/11/13…</c> for free (used by scales /
/// arpeggios, which have real octaves).</item>
/// <item><see cref="Label"/> — the <b>chord-context</b> spelling: role-keyed chord tones
/// (<c>R/b3/3/b5/5/#5/b7/bb7/7</c>) falling back to the conventional compound tensions
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
            ChordToneFunction.Seventh => s switch { 9 => "bb7", 11 => "7", _ => "b7" },
            _ => Tension[s],
        };
    }
}
