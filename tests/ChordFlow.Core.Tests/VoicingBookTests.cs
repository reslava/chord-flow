using ChordFlow.Exercises;
using ChordFlow.Music.Harmony;
using ChordFlow.Music.Progressions;
using Xunit;

using ChordFlow.Instruments.Guitar;

namespace ChordFlow.Core.Tests;

public class VoicingBookTests
{
    // Open-string pitch classes, indexed by alphaTab string number (1 = high E .. 6 = low E).
    // Index 0 is unused so the string number indexes directly.
    private static readonly int[] OpenStringPc = { 0, 4, 11, 7, 2, 9, 4 };

    private static int NotePc(FretPosition p) => (OpenStringPc[p.String] + p.Fret) % 12;

    // A book with no authored library — every chord resolves through the strategy fallback, exactly as
    // the pre-authoring VoicingBook did.
    private static VoicingBook StrategyOnly() => new(Array.Empty<VoicingShape>());

    // The movable shell covers every root — all 12 keys, not just the Bb blues' I/IV/V.
    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(2)] [InlineData(3)]
    [InlineData(4)] [InlineData(5)] [InlineData(6)] [InlineData(7)]
    [InlineData(8)] [InlineData(9)] [InlineData(10)] [InlineData(11)]
    public void Lookup_Dominant7Shell_SpellsRootMajorThirdMinorSeventh(int root)
    {
        var chord = new Chord(new PitchClass(root), Quality.Dominant7);

        Voicing voicing = StrategyOnly().Lookup(chord, Difficulty.Beginner);

        var actual = voicing.Positions.Select(NotePc).ToHashSet();
        var expected = new HashSet<int> { root % 12, (root + 4) % 12, (root + 10) % 12 };
        Assert.Equal(3, voicing.Positions.Count);
        Assert.Equal(expected, actual);
        // Shape stays contiguous and on the fretboard (no negative frets).
        Assert.All(voicing.Positions, p => Assert.InRange(p.Fret, 0, 12));
    }

    // The minor-7 shell shares the movable shape with a minor 3rd instead of the major 3rd.
    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(2)] [InlineData(3)]
    [InlineData(4)] [InlineData(5)] [InlineData(6)] [InlineData(7)]
    [InlineData(8)] [InlineData(9)] [InlineData(10)] [InlineData(11)]
    public void Lookup_Minor7Shell_SpellsRootMinorThirdMinorSeventh(int root)
    {
        var chord = new Chord(new PitchClass(root), Quality.Minor7);

        Voicing voicing = StrategyOnly().Lookup(chord, Difficulty.Beginner);

        var actual = voicing.Positions.Select(NotePc).ToHashSet();
        var expected = new HashSet<int> { root % 12, (root + 3) % 12, (root + 10) % 12 };
        Assert.Equal(3, voicing.Positions.Count);
        Assert.Equal(expected, actual);
        // Shape stays contiguous and never needs a negative fret.
        Assert.All(voicing.Positions, p => Assert.True(p.Fret >= 0));
    }

    // The three previously hand-authored rows must come out byte-identical so existing
    // Bb-blues rendering (and the renderer tests) are unchanged.
    [Theory]
    [InlineData(10, 1, 0, 1)] // Bb7
    [InlineData(3, 6, 5, 6)]  // Eb7
    [InlineData(5, 8, 7, 8)]  // F7
    public void Lookup_AuthoredBluesChords_MatchOriginalFrets(int root, int s5, int s4, int s3)
    {
        var chord = new Chord(new PitchClass(root), Quality.Dominant7);

        Voicing voicing = StrategyOnly().Lookup(chord, Difficulty.Beginner);

        Assert.Equal(new FretPosition(5, s5), voicing.Positions[0]);
        Assert.Equal(new FretPosition(4, s4), voicing.Positions[1]);
        Assert.Equal(new FretPosition(3, s3), voicing.Positions[2]);
    }

    [Fact]
    public void Lookup_EveryChordOfTheBbBlues_Resolves()
    {
        var bb = new Key(new PitchClass(10), false);
        VoicingBook book = StrategyOnly();

        foreach (Chord chord in Transposer.Realize(SeedData.TwelveBarBlues, bb))
        {
            Voicing voicing = book.Lookup(chord, Difficulty.Beginner);
            Assert.Equal(3, voicing.Positions.Count);
        }
    }

    [Fact]
    public void Lookup_EveryKeyOfTheBlues_Resolves()
    {
        VoicingBook book = StrategyOnly();

        // What used to throw (C blues needs C7) now resolves — the movable shape covers all keys.
        foreach (Key key in SeedData.AllMajorKeys)
        {
            foreach (Chord chord in Transposer.Realize(SeedData.TwelveBarBlues, key))
            {
                Voicing voicing = book.Lookup(chord, Difficulty.Beginner);
                Assert.Equal(3, voicing.Positions.Count);
            }
        }
    }

    [Fact]
    public void Lookup_NonDominant7Quality_WithNoStored_Throws()
    {
        var cMajor = new Chord(new PitchClass(0), Quality.Major); // shell covers dom7/min7; Major still throws

        Assert.Throws<NotSupportedException>(() => StrategyOnly().Lookup(cMajor, Difficulty.Beginner));
    }

    [Fact]
    public void Lookup_NonBeginnerDifficulty_Throws()
    {
        var bb7 = new Chord(new PitchClass(10), Quality.Dominant7);

        Assert.Throws<NotSupportedException>(() => StrategyOnly().Lookup(bb7, Difficulty.Intermediate));
    }

    [Fact]
    public void Lookup_BeginnerShell_CarriesDiagramMetadata()
    {
        // Bb7 = frets (1,0,1) on strings 5/4/3; diagram first fret is the lowest used (0),
        // strings 1/2/6 are muted, and the shell shape uses no barre.
        var bb7 = new Chord(new PitchClass(10), Quality.Dominant7);

        Voicing voicing = StrategyOnly().Lookup(bb7, Difficulty.Beginner);

        Assert.Null(voicing.BarreFret);
        Assert.Equal(0, voicing.FirstFret);
        Assert.Equal(new[] { 1, 2, 6 }, voicing.MutedStrings);
    }

    [Fact]
    public void BeginnerShellStrategy_ReportsBeginnerDifficulty()
    {
        Assert.Equal(Difficulty.Beginner, new BeginnerShellStrategy().Difficulty);
    }

    // --- Authored-voicing behaviour (slice 1) ---

    [Fact]
    public void Lookup_StoredMajor_ResolvesWhereTheStrategyWouldThrow()
    {
        // The strategy covers no Major; a stored Cmaj voicing makes the lookup succeed (shadowing the gap).
        VoicingShape cShape = VoicingDslParser.Parse("voicing Cmaj shape:C root:5 frets: x 3 2 0 1 0");
        var book = new VoicingBook(new[] { cShape });
        var chord = new Chord(new PitchClass(0), Quality.Major);

        Voicing got = book.Lookup(chord, Difficulty.Beginner);

        Assert.Equal(cShape.Realize(chord.Root)!.Positions.OrderBy(p => p.String), got.Positions.OrderBy(p => p.String));
    }

    [Fact]
    public void Lookup_StoredVoicing_ShadowsTheGeneratedShape()
    {
        // A stored Dominant7 voicing must shadow the generated shell for the same chord.
        VoicingShape stored = VoicingDslParser.Parse("voicing C7 shape:E root:6 frets: 8 10 8 9 8 8");
        var chord = new Chord(new PitchClass(0), Quality.Dominant7);

        Voicing fromStored = new VoicingBook(new[] { stored }).Lookup(chord, Difficulty.Beginner);
        Voicing fromStrategy = StrategyOnly().Lookup(chord, Difficulty.Beginner);

        Assert.Equal(stored.Realize(chord.Root)!.Positions.OrderBy(p => p.String), fromStored.Positions.OrderBy(p => p.String));
        Assert.NotEqual(fromStrategy.Positions.Count, fromStored.Positions.Count); // shell is 3 notes; the stored shape isn't
    }

    [Fact]
    public void Matching_IsExactQuality_Maj7DoesNotCoverMaj()
    {
        // A stored Cmaj7 must not answer a Cmaj lookup — exact quality only.
        VoicingShape maj7 = VoicingDslParser.Parse("voicing Cmaj7 shape:C root:5 frets: x 3 2 0 0 0");
        var book = new VoicingBook(new[] { maj7 });
        var cMajor = new Chord(new PitchClass(0), Quality.Major);

        Assert.Empty(book.Candidates(cMajor, Difficulty.Beginner));
        Assert.Throws<NotSupportedException>(() => book.Lookup(cMajor, Difficulty.Beginner));
    }

    [Fact]
    public void Candidates_NoStoredMatch_IsEmpty()
    {
        VoicingShape cMaj = VoicingDslParser.Parse("voicing Cmaj shape:C root:5 frets: x 3 2 0 1 0");
        var book = new VoicingBook(new[] { cMaj });

        Assert.Empty(book.Candidates(new Chord(new PitchClass(0), Quality.Dominant7), Difficulty.Beginner));
    }

    [Fact]
    public void Candidates_RealizeToChordRoot_SoundsTheTargetChord()
    {
        VoicingShape cMaj = VoicingDslParser.Parse("voicing Cmaj shape:C root:5 frets: x 3 2 0 1 0");
        var book = new VoicingBook(new[] { cMaj });

        // Realized at G (pc 7) the candidate must spell a G-major triad.
        IReadOnlyList<Voicing> candidates = book.Candidates(new Chord(new PitchClass(7), Quality.Major), Difficulty.Beginner);

        Assert.Single(candidates);
        var pcs = candidates[0].Positions.Select(NotePc).ToHashSet();
        Assert.Equal(new HashSet<int> { 7, 11, 2 }, pcs); // G B D
    }

    [Fact]
    public void Candidates_RankedByNeckPosition()
    {
        // Two Major shapes for the same chord: the C-shape (open, fret 0) ranks before the E-shape (fret 8).
        VoicingShape cShape = VoicingDslParser.Parse("voicing Cmaj shape:C root:5 frets: x 3 2 0 1 0");
        VoicingShape eShape = VoicingDslParser.Parse("voicing Cmaj shape:E root:6 frets: 8 10 10 9 8 8");
        var book = new VoicingBook(new[] { eShape, cShape }); // deliberately unsorted on input

        IReadOnlyList<Voicing> candidates = book.Candidates(new Chord(new PitchClass(0), Quality.Major), Difficulty.Beginner);

        Assert.Equal(2, candidates.Count);
        Assert.True(candidates[0].FirstFret < candidates[1].FirstFret);
        Assert.Equal(0, candidates[0].FirstFret);
    }

    [Fact]
    public void FamiliarityRank_OrdersTheBarreRootsFirst()
    {
        Assert.True(CagedShape.E.FamiliarityRank() < CagedShape.A.FamiliarityRank());
        Assert.True(CagedShape.A.FamiliarityRank() < CagedShape.G.FamiliarityRank());
        Assert.True(CagedShape.G.FamiliarityRank() < CagedShape.C.FamiliarityRank());
        Assert.True(CagedShape.C.FamiliarityRank() < CagedShape.D.FamiliarityRank());
    }
}
