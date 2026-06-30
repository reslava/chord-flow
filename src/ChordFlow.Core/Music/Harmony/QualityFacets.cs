namespace ChordFlow.Music.Harmony;

/// <summary>
/// The "emotion" facet — a chord's 3rd. Read from the 3rd-degree interval: a major 3rd, a minor 3rd,
/// or no 3rd at all (suspended).
/// </summary>
public enum ThirdFacet
{
    /// <summary>Major 3rd (interval 4) — token <c>major</c>.</summary>
    Major,

    /// <summary>Minor 3rd (interval 3) — token <c>minor</c>.</summary>
    Minor,

    /// <summary>No 3rd present — token <c>suspended</c>.</summary>
    Suspended,
}

/// <summary>
/// The "stability" facet — a chord's 5th. Read from the 5th-degree interval: perfect, augmented (♯5),
/// or diminished (♭5).
/// </summary>
public enum FifthFacet
{
    /// <summary>Perfect 5th (interval 7) — token <c>perfect</c>.</summary>
    Perfect,

    /// <summary>Augmented 5th (interval 8) — token <c>augmented</c>.</summary>
    Augmented,

    /// <summary>Diminished 5th (interval 6) — token <c>diminished</c>.</summary>
    Diminished,
}

/// <summary>
/// The "color" facet — a chord's 6th/7th extension. Read from the 6th/7th degree: a plain triad,
/// an added 6th, a ♭7, a ♮7, or a ♭♭7 (dim7).
/// </summary>
public enum SeventhFacet
{
    /// <summary>No 6th or 7th present — token <c>triad</c>.</summary>
    Triad,

    /// <summary>Added 6th (Sixth function) — token <c>6</c>.</summary>
    Sixth,

    /// <summary>Minor 7th (interval 10) — token <c>7</c>.</summary>
    Seventh,

    /// <summary>Major 7th (interval 11) — token <c>maj7</c>.</summary>
    MajorSeventh,

    /// <summary>Diminished 7th / ♭♭7 (interval 9) — token <c>dim7</c>.</summary>
    DiminishedSeventh,
}

/// <summary>
/// The orthogonal decomposition of a <see cref="Quality"/> into three filterable facets — its
/// <b>3rd</b> (emotion), <b>5th</b> (stability) and <b>7th/color</b> — <b>derived from the quality's
/// chord-tone spelling</b> (via <see cref="ChordTones"/> over <see cref="QualityFormulas"/>), never
/// hand-authored, so the mapping stays auto-correct as qualities are added. The three axes are
/// collision-free across the engine's qualities, giving each a unique (3rd × 5th × 7th) cell.
/// Instrument-agnostic: reads only the harmony layer, references nothing under <c>Instruments/</c>
/// (architecture-test-safe).
/// </summary>
public readonly record struct QualityFacets(ThirdFacet Third, FifthFacet Fifth, SeventhFacet Seventh)
{
    /// <summary>Decompose <paramref name="quality"/> into its (3rd, 5th, 7th) facets, read from its spelling.</summary>
    public static QualityFacets Of(Quality quality)
    {
        // The root is irrelevant to facets — only interval + function matter — so realize the tones at an
        // arbitrary root. Reuse ChordTones (the single spelling-derived source) instead of re-parsing the
        // formula, so the facets can never drift from the chord-content table.
        IReadOnlyList<ChordTone> tones = ChordTones.Of(new Chord(new PitchClass(0), quality));

        return new QualityFacets(DeriveThird(tones), DeriveFifth(tones), DeriveSeventh(tones));
    }

    /// <summary>The wire/filter token for the 3rd facet (<c>major</c> / <c>minor</c> / <c>suspended</c>).</summary>
    public string ThirdToken => Third switch
    {
        ThirdFacet.Major => "major",
        ThirdFacet.Minor => "minor",
        ThirdFacet.Suspended => "suspended",
        _ => throw new ArgumentOutOfRangeException(nameof(Third), Third, "Unknown 3rd facet."),
    };

    /// <summary>The wire/filter token for the 5th facet (<c>perfect</c> / <c>augmented</c> / <c>diminished</c>).</summary>
    public string FifthToken => Fifth switch
    {
        FifthFacet.Perfect => "perfect",
        FifthFacet.Augmented => "augmented",
        FifthFacet.Diminished => "diminished",
        _ => throw new ArgumentOutOfRangeException(nameof(Fifth), Fifth, "Unknown 5th facet."),
    };

    /// <summary>The wire/filter token for the 7th/color facet (<c>triad</c> / <c>6</c> / <c>7</c> / <c>maj7</c> / <c>dim7</c>).</summary>
    public string SeventhToken => Seventh switch
    {
        SeventhFacet.Triad => "triad",
        SeventhFacet.Sixth => "6",
        SeventhFacet.Seventh => "7",
        SeventhFacet.MajorSeventh => "maj7",
        SeventhFacet.DiminishedSeventh => "dim7",
        _ => throw new ArgumentOutOfRangeException(nameof(Seventh), Seventh, "Unknown 7th facet."),
    };

    private static ThirdFacet DeriveThird(IReadOnlyList<ChordTone> tones)
    {
        ChordTone? third = Find(tones, ChordToneFunction.Third);
        if (third is null)
        {
            return ThirdFacet.Suspended;
        }

        return third.Value.Interval switch
        {
            4 => ThirdFacet.Major,
            3 => ThirdFacet.Minor,
            _ => throw new ArgumentOutOfRangeException(
                nameof(tones), third.Value.Interval, "3rd interval does not map to a 3rd facet."),
        };
    }

    private static FifthFacet DeriveFifth(IReadOnlyList<ChordTone> tones)
    {
        ChordTone? fifth = Find(tones, ChordToneFunction.Fifth);
        if (fifth is null)
        {
            throw new ArgumentException("Quality has no 5th; cannot derive a 5th facet.", nameof(tones));
        }

        return fifth.Value.Interval switch
        {
            7 => FifthFacet.Perfect,
            8 => FifthFacet.Augmented,
            6 => FifthFacet.Diminished,
            _ => throw new ArgumentOutOfRangeException(
                nameof(tones), fifth.Value.Interval, "5th interval does not map to a 5th facet."),
        };
    }

    private static SeventhFacet DeriveSeventh(IReadOnlyList<ChordTone> tones)
    {
        if (Find(tones, ChordToneFunction.Sixth) is not null)
        {
            return SeventhFacet.Sixth;
        }

        ChordTone? seventh = Find(tones, ChordToneFunction.Seventh);
        if (seventh is null)
        {
            return SeventhFacet.Triad;
        }

        return seventh.Value.Interval switch
        {
            10 => SeventhFacet.Seventh,
            11 => SeventhFacet.MajorSeventh,
            9 => SeventhFacet.DiminishedSeventh,
            _ => throw new ArgumentOutOfRangeException(
                nameof(tones), seventh.Value.Interval, "7th interval does not map to a 7th facet."),
        };
    }

    private static ChordTone? Find(IReadOnlyList<ChordTone> tones, ChordToneFunction function)
    {
        foreach (ChordTone tone in tones)
        {
            if (tone.Function == function)
            {
                return tone;
            }
        }

        return null;
    }
}
