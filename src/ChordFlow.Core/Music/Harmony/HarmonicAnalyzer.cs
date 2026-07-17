namespace ChordFlow.Music.Harmony;

/// <summary>
/// A pure harmonic-analysis pass — a Harmony <b>sink</b> (no I/O, references nothing under
/// <c>Instruments/</c> or <c>Rendering/</c>). Given a concrete <see cref="Chord"/> and a <see cref="Key"/> it
/// labels the chord's harmonic function as a structured <see cref="ChordAnalysis"/>. The reasoner north star
/// applied to <i>function</i>: the same way the Voicings Engine derives voicings from theory, this derives
/// <b>roles</b> from theory.
///
/// <para>Design invariants: <b>pitch-based</b> (works from the chord root + quality, never a DSL degree),
/// <b>context-free per chord</b> (labels from key + quality alone; resolution is not consulted — D1), and
/// <b>symmetric across major and minor</b> tonics (D4, including the harmonic-minor V/vii°). Classification
/// keys on a chord's <b>functional core</b> (3rd + 5th + 7th via <see cref="QualityFacets"/>), not exact
/// <see cref="Quality"/> equality, so extensions never change function — a future dom9/13/7♯9 analyzes as a
/// dominant with no change.</para>
/// </summary>
public static class HarmonicAnalyzer
{
    /// <summary>
    /// Analyze a sequence of (chord, key) pairs. The key may vary per position — modulation / multi-key are
    /// already real at the song level — so a modulating passage is analyzed region-by-region for free. v1 is
    /// order-independent (each chord is analyzed on its own key, D1/EX5); the sequence is accepted so
    /// resolution-based labeling can be added later without an API change.
    /// </summary>
    public static IReadOnlyList<ChordAnalysis> Analyze(IReadOnlyList<(Chord Chord, Key Key)> chords)
    {
        ArgumentNullException.ThrowIfNull(chords);

        var results = new ChordAnalysis[chords.Count];
        for (int i = 0; i < chords.Count; i++)
        {
            results[i] = Analyze(chords[i].Chord, chords[i].Key);
        }

        return results;
    }

    /// <summary>Analyze a single <paramref name="chord"/> in <paramref name="key"/>.</summary>
    public static ChordAnalysis Analyze(Chord chord, Key key)
    {
        ArgumentNullException.ThrowIfNull(chord);
        ArgumentNullException.ThrowIfNull(key);

        int rootOffset = Mod12(chord.Root.Value - key.Tonic.Value);
        RomanDegree function = HonestFunction(rootOffset, chord.Quality);

        // Precedence (D2): Diatonic > SecondaryDominant > SecondaryLeadingTone > TritoneSub > Borrowed > Chromatic.
        if (IsDiatonic(rootOffset, chord.Quality, key))
        {
            return new ChordAnalysis(HarmonicCategory.Diatonic, function);
        }

        // The tonic is never a secondary function (the blues I7 is not "V/IV") — the blues ruling.
        if (rootOffset != 0)
        {
            if (IsSecondaryDominant(rootOffset, chord.Quality, key, out ScaleDegree domTarget))
            {
                return new ChordAnalysis(HarmonicCategory.SecondaryDominant, function, domTarget);
            }

            if (IsSecondaryLeadingTone(rootOffset, chord.Quality, key, out ScaleDegree ltTarget))
            {
                return new ChordAnalysis(HarmonicCategory.SecondaryLeadingTone, function, ltTarget);
            }

            if (IsTritoneSub(rootOffset, chord.Quality, out ScaleDegree tsTarget))
            {
                return new ChordAnalysis(HarmonicCategory.TritoneSub, function, tsTarget);
            }
        }

        if (IsBorrowed(rootOffset, chord.Quality, key, out KeyMode source))
        {
            return new ChordAnalysis(HarmonicCategory.Borrowed, function, Target: null, SourceMode: source);
        }

        return new ChordAnalysis(HarmonicCategory.Chromatic, function);
    }

    // The honest key-relative degree of a root offset (0..11), in the conventional major-frame Roman spelling
    // (♭II ♭III ♯IV ♭VI ♭VII), so a minor key reads i ii° ♭III iv v ♭VI ♭VII as convention expects. The chord's
    // own quality carries straight through.
    private static RomanDegree HonestFunction(int rootOffset, Quality quality)
    {
        (int degree, Accidental accidental) = DegreeTable[rootOffset];
        return new RomanDegree(degree, quality, accidental);
    }

    private static readonly (int Degree, Accidental Accidental)[] DegreeTable =
    {
        (1, Accidental.Natural), // 0
        (2, Accidental.Flat),    // 1  ♭II
        (2, Accidental.Natural), // 2
        (3, Accidental.Flat),    // 3  ♭III
        (3, Accidental.Natural), // 4
        (4, Accidental.Natural), // 5
        (4, Accidental.Sharp),   // 6  ♯IV
        (5, Accidental.Natural), // 7
        (6, Accidental.Flat),    // 8  ♭VI
        (6, Accidental.Natural), // 9
        (7, Accidental.Flat),    // 10 ♭VII
        (7, Accidental.Natural), // 11
    };

    // Diatonic in the key's own scale (functional-core match, triad-vs-7th tolerant). For a minor key the
    // functional dominant (V/V7, a raised 3rd) and leading-tone diminished (raised 7th) — which natural minor
    // lacks — are treated as diatonic too.
    private static bool IsDiatonic(int rootOffset, Quality quality, Key key)
    {
        Scale scale = Scale.ForKey(key);
        int rootPc = Mod12(key.Tonic.Value + rootOffset);

        for (int d = 1; d <= scale.Count; d++)
        {
            if (scale.DegreePitchClass(d).Value != rootPc)
            {
                continue;
            }

            Quality diatonic = DiatonicChord.Build(scale, new ScaleDegree(d)).Quality;
            if (CoreMatches(quality, diatonic))
            {
                return true;
            }

            // Harmonic-minor functional dominant: a minor key's V is a major triad / dominant 7, not the
            // natural-minor v.
            return key.IsMinor && d == 5 && IsDominantOrMajorTriad(quality);
        }

        // Harmonic-minor leading-tone: the raised 7 (a semitone below the tonic) carrying a diminished chord —
        // the vii° that natural minor (offset 10) does not contain.
        return key.IsMinor && rootOffset == 11 && IsDiminishedTriadOrSeventh(quality);
    }

    // A dominant-family chord (major 3rd + ♭7) whose root is a perfect fifth above a non-tonic diatonic degree
    // tonicizes it (V/x).
    private static bool IsSecondaryDominant(int rootOffset, Quality quality, Key key, out ScaleDegree target)
    {
        target = default;
        if (!IsDominant(quality))
        {
            return false;
        }

        int targetOffset = Mod12(rootOffset + 5); // down a perfect fifth (= up a fourth) → the tonicized root
        return TryNonTonicDiatonicDegree(targetOffset, key, out target);
    }

    // A fully-diminished chord (triad or dim7) a semitone below a non-tonic diatonic degree tonicizes it (vii°/x).
    private static bool IsSecondaryLeadingTone(int rootOffset, Quality quality, Key key, out ScaleDegree target)
    {
        target = default;
        if (!IsDiminishedTriadOrSeventh(quality))
        {
            return false;
        }

        int targetOffset = Mod12(rootOffset + 1); // a semitone up → the tonicized root
        return TryNonTonicDiatonicDegree(targetOffset, key, out target);
    }

    // v1 tritone substitution: the ♭II7 substituting for the primary dominant, resolving down a semitone to I.
    // (Tritone subs of secondary dominants are a later, sequence-aware refinement.)
    private static bool IsTritoneSub(int rootOffset, Quality quality, out ScaleDegree target)
    {
        target = default;
        if (rootOffset == 1 && IsDominant(quality))
        {
            target = new ScaleDegree(1);
            return true;
        }

        return false;
    }

    // Modal mixture: the chord matches a diatonic chord of the PARALLEL mode (same tonic, opposite mode).
    private static bool IsBorrowed(int rootOffset, Quality quality, Key key, out KeyMode source)
    {
        source = default;
        var parallel = new Key(key.Tonic, !key.IsMinor);
        Scale scale = Scale.ForKey(parallel);
        int rootPc = Mod12(key.Tonic.Value + rootOffset);

        for (int d = 1; d <= scale.Count; d++)
        {
            if (scale.DegreePitchClass(d).Value != rootPc)
            {
                continue;
            }

            Quality diatonic = DiatonicChord.Build(scale, new ScaleDegree(d)).Quality;
            if (CoreMatches(quality, diatonic))
            {
                source = parallel.IsMinor ? KeyMode.Minor : KeyMode.Major;
                return true;
            }

            return false;
        }

        return false;
    }

    // The chord matches the diatonic quality by functional core: same 3rd + same 5th, and either the same
    // 7th/color OR a plain triad (so a bare Dm triad matches the diatonic Dm7 "ii", a G triad matches "V").
    private static bool CoreMatches(Quality chordQuality, Quality diatonicQuality)
    {
        QualityFacets c = QualityFacets.Of(chordQuality);
        QualityFacets d = QualityFacets.Of(diatonicQuality);
        return c.Third == d.Third
            && c.Fifth == d.Fifth
            && (c.Seventh == d.Seventh || c.Seventh == SeventhFacet.Triad);
    }

    // Dominant family = major 3rd + ♭7. Fifth is ignored so a future altered dominant still reads as dominant.
    private static bool IsDominant(Quality quality)
    {
        QualityFacets f = QualityFacets.Of(quality);
        return f.Third == ThirdFacet.Major && f.Seventh == SeventhFacet.Seventh;
    }

    // The harmonic-minor V: a major triad (V) or dominant 7 (V7) — not maj7/6.
    private static bool IsDominantOrMajorTriad(Quality quality)
    {
        QualityFacets f = QualityFacets.Of(quality);
        return f.Third == ThirdFacet.Major
            && f.Fifth == FifthFacet.Perfect
            && (f.Seventh == SeventhFacet.Triad || f.Seventh == SeventhFacet.Seventh);
    }

    // A fully-diminished chord: diminished triad or dim7 (NOT half-diminished, whose ♭7 makes it a predominant).
    private static bool IsDiminishedTriadOrSeventh(Quality quality)
    {
        QualityFacets f = QualityFacets.Of(quality);
        return f.Third == ThirdFacet.Minor
            && f.Fifth == FifthFacet.Diminished
            && (f.Seventh == SeventhFacet.Triad || f.Seventh == SeventhFacet.DiminishedSeventh);
    }

    // A diatonic degree in [2..7] (i.e. non-tonic) whose pitch class matches tonic + offset.
    private static bool TryNonTonicDiatonicDegree(int offset, Key key, out ScaleDegree degree)
    {
        degree = default;
        Scale scale = Scale.ForKey(key);
        int pc = Mod12(key.Tonic.Value + offset);

        for (int d = 2; d <= scale.Count; d++)
        {
            if (scale.DegreePitchClass(d).Value == pc)
            {
                degree = new ScaleDegree(d);
                return true;
            }
        }

        return false;
    }

    private static int Mod12(int value) => ((value % 12) + 12) % 12;
}
