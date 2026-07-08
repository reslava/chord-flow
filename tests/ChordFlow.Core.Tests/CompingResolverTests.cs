using ChordFlow.Features.Voicings;
using ChordFlow.Instruments.Guitar;
using ChordFlow.Music.Harmony;
using ChordFlow.Music.Progressions;
using ChordFlow.Music.Rhythm;
using ChordFlow.Music.Songs;
using ChordFlow.Persistence;
using ChordFlow.Rendering;
using Xunit;

namespace ChordFlow.Core.Tests;

public class CompingResolverTests
{
    private static readonly Key C = new(new PitchClass(0), false);

    private static RealizedSong Realize(string dsl, Key key)
    {
        Progression prog = ProgressionParser.Parse("t", "T", dsl, TimeSignature.FourFour);
        return new RealizedSong(new[] { new RealizedSection("t", key, Transposer.RealizeBars(prog, key)) });
    }

    private static IEnumerable<Chord> ChordsOf(RealizedSong song) =>
        song.Sections.SelectMany(s => s.Bars).SelectMany(b => b.Spans).Select(sp => sp.Chord).Distinct();

    [Fact]
    public void AutomaticDefault_ResolvesAGripForEveryChord()
    {
        RealizedSong song = Realize("17_47 57_27", C); // C7, F7, G7, D7

        CompingPlan plan = CompingResolver.Resolve(song, VoicingSource.Default, StoredVoicingSource.Empty);

        foreach (Chord chord in ChordsOf(song))
        {
            Assert.NotEmpty(plan.For(chord).Positions);
        }
    }

    [Fact]
    public void MainUserEmpty_FallsBackToAutomatic()
    {
        RealizedSong song = Realize("17", C);
        var mainUser = new VoicingSource(Kind: VoicingSource.User);

        CompingPlan plan = CompingResolver.Resolve(song, mainUser, StoredVoicingSource.Empty);

        // No user voicings exist, so the fallback chain lands on automatic — a grip is still produced.
        Assert.NotEmpty(plan.For(ChordsOf(song).Single()).Positions);
    }

    [Fact]
    public void Region_AnchorsGripsWithinTheWindow()
    {
        RealizedSong song = Realize("17", C);
        var region = new VoicingSource(MinFret: 5, MaxFret: 12);

        CompingPlan plan = CompingResolver.Resolve(song, region, StoredVoicingSource.Empty);

        Voicing grip = plan.For(ChordsOf(song).Single());
        Assert.All(grip.Positions, p => Assert.InRange(p.Fret, 5, 15));
    }

    [Fact]
    public void NoSourceCanComp_ThrowsLoud()
    {
        // A diminished triad is not in the automatic catalog (only dim7 is) and no stored source has it.
        RealizedSong song = Realize("7dim", C);

        Assert.Throws<InvalidOperationException>(
            () => CompingResolver.Resolve(song, VoicingSource.Default, StoredVoicingSource.Empty));
    }

    [Fact]
    public void UnknownRanking_ThrowsFormatException()
    {
        RealizedSong song = Realize("17", C);
        var bogus = new VoicingSource(Ranking: "nonsense");

        Assert.Throws<FormatException>(
            () => CompingResolver.Resolve(song, bogus, StoredVoicingSource.Empty));
    }

    [Fact]
    public void ShellFamily_CompsThreeNoteGuideToneGrips()
    {
        RealizedSong song = Realize("17", C); // C7
        var shell = new VoicingSource(Family: "shell");

        CompingPlan plan = CompingResolver.Resolve(song, shell, StoredVoicingSource.Empty);

        Assert.Equal(3, plan.For(ChordsOf(song).Single()).Positions.Count);
    }

    [Fact]
    public void DoubledShellFamily_DropsTheFifth_FewerNotesThanCaged()
    {
        RealizedSong song = Realize("17", C);

        int caged = CompingResolver.Resolve(song, VoicingSource.Default, StoredVoicingSource.Empty)
            .For(ChordsOf(song).Single()).Positions.Count;
        int doubledShell = CompingResolver.Resolve(song, new VoicingSource(Family: "dshell"), StoredVoicingSource.Empty)
            .For(ChordsOf(song).Single()).Positions.Count;

        Assert.True(doubledShell < caged, $"doubled-shell ({doubledShell}) should drop the fifth vs caged ({caged}).");
    }

    [Fact]
    public void TriadUnderShellFamily_FallsBackToCagedGrip()
    {
        RealizedSong song = Realize("1", C); // C major triad — no shell exists
        var shell = new VoicingSource(Family: "shell");

        CompingPlan plan = CompingResolver.Resolve(song, shell, StoredVoicingSource.Empty);

        // Fell back to the caged family: a full chord grip, more than three notes.
        Assert.True(plan.For(ChordsOf(song).Single()).Positions.Count > 3);
    }

    [Fact]
    public void UnknownFamily_ThrowsFormatException()
    {
        RealizedSong song = Realize("17", C);
        var bogus = new VoicingSource(Family: "nonsense");

        Assert.Throws<FormatException>(
            () => CompingResolver.Resolve(song, bogus, StoredVoicingSource.Empty));
    }

    [Fact]
    public void FallbackToUserStored_WhenAutomaticHasNoneForTheChord()
    {
        RealizedSong song = Realize("7dim", C); // automatic can't voice a diminished triad
        Voicing userGrip = new(new[] { new FretPosition(4, 1), new FretPosition(3, 2), new FretPosition(2, 1) });
        var stored = new FakeStored(ContentSource.User, song.Sections[0].Bars.SelectMany(b => b.Spans).First().Chord, userGrip);

        CompingPlan plan = CompingResolver.Resolve(song, VoicingSource.Default, stored);

        Assert.Same(userGrip, plan.For(ChordsOf(song).Single()));
    }

    private sealed class FakeStored : IStoredVoicingSource
    {
        private readonly ContentSource _source;
        private readonly Chord _chord;
        private readonly Voicing _grip;

        public FakeStored(ContentSource source, Chord chord, Voicing grip)
        {
            _source = source;
            _chord = chord;
            _grip = grip;
        }

        public IReadOnlyList<Voicing> Candidates(Chord chord, ContentSource source, string? packageId) =>
            source == _source && chord.Equals(_chord) ? new[] { _grip } : Array.Empty<Voicing>();
    }

    // ---- The explicit-voicing cascade: {…} annotations + `voice` defaults (IN1/IN5/IN6) ----

    private sealed class NullStore : IProgressionStore
    {
        public Progression? Find(string id) => null;
    }

    private static RealizedSong RealizeSong(string songDsl, Key key) =>
        SongExpander.Expand(SongParser.Parse("t", "T", songDsl, TimeSignature.FourFour), new NullStore(), key);

    private static RealizedSpan SpanAt(RealizedSong song, int barIndex, int spanIndex = 0) =>
        song.Sections[0].Bars[barIndex].Spans[spanIndex];

    private static int? Fret(Voicing v, int stringNumber) =>
        v.Positions.Where(p => p.String == stringNumber).Select(p => (int?)p.Fret).SingleOrDefault();

    private static CompingPlan ResolveSong(RealizedSong song) =>
        CompingResolver.Resolve(song, VoicingSource.Default, StoredVoicingSource.Empty);

    [Fact]
    public void PerChordAnnotation_OverridesOnlyThatOccurrence()
    {
        // First I7 pinned; the second I7 (same chord value) is left to the fill → they comp differently.
        RealizedSong song = RealizeSong("A = 17 {8 x 7 9 8 x} 17\nA", C);
        CompingPlan plan = ResolveSong(song);

        Voicing pinned = plan.For(SpanAt(song, 0));
        Voicing filled = plan.For(SpanAt(song, 1));

        Assert.Equal(8, Fret(pinned, 6));   // grip verbatim at C7 (bass low-E fret 8 = C)
        Assert.NotEqual(
            pinned.Positions.OrderBy(p => p.String),
            filled.Positions.OrderBy(p => p.String));
    }

    [Fact]
    public void DegreeScopedVoiceDefault_AppliesToEveryOccurrenceOfThatDegree()
    {
        RealizedSong song = RealizeSong("voice 17 = 8 x 7 9 8 x\nA = 17 47 17\nA", C);
        CompingPlan plan = ResolveSong(song);

        Assert.Equal(8, Fret(plan.For(SpanAt(song, 0)), 6));   // I7 → default
        Assert.Equal(8, Fret(plan.For(SpanAt(song, 2)), 6));   // the other I7 → same default
        // IV7 (F7) is a different degree → the fill, not the default.
        Assert.NotEqual(
            plan.For(SpanAt(song, 0)).Positions.OrderBy(p => p.String),
            plan.For(SpanAt(song, 1)).Positions.OrderBy(p => p.String));
    }

    [Fact]
    public void QualityScopedVoiceDefault_TransposesToEachChordRoot()
    {
        // `voice *7` = a movable dom7 grip (bass C): at C7 verbatim (fret6=8), at F7 it slides up to fret6=1.
        RealizedSong song = RealizeSong("voice *7 = 8 x 7 9 8 x\nA = 17 47\nA", C);
        CompingPlan plan = ResolveSong(song);

        Assert.Equal(8, Fret(plan.For(SpanAt(song, 0)), 6));   // C7
        Assert.Equal(1, Fret(plan.For(SpanAt(song, 1)), 6));   // F7 (shape shifted +5, octave-folded)
    }

    [Fact]
    public void DegreeScopedDefault_BeatsQualityScopedDefault()
    {
        RealizedSong song = RealizeSong("voice *7 = 8 x 7 9 8 x\nvoice 17 = x 3 2 3 1 x\nA = 17 47\nA", C);
        CompingPlan plan = ResolveSong(song);

        // C7 (degree 1) takes the degree default `x 3 2 3 1 x` (low-E muted), not the *7 grip.
        Assert.Null(Fret(plan.For(SpanAt(song, 0)), 6));
        Assert.Equal(3, Fret(plan.For(SpanAt(song, 0)), 5));
        // F7 (degree 4) has no degree default → the *7 quality default, transposed (fret6=1).
        Assert.Equal(1, Fret(plan.For(SpanAt(song, 1)), 6));
    }

    [Fact]
    public void PerChordAnnotation_BeatsVoiceDefault()
    {
        RealizedSong song = RealizeSong("voice 17 = 8 x 7 9 8 x\nA = 17 {x 3 2 3 1 x} 17\nA", C);
        CompingPlan plan = ResolveSong(song);

        Assert.Null(Fret(plan.For(SpanAt(song, 0)), 6));   // annotated occurrence → the {…} grip
        Assert.Equal(8, Fret(plan.For(SpanAt(song, 1)), 6));   // the other I7 → the degree default
    }

    [Fact]
    public void UnresolvableReferenceAnnotation_FailsLoud()
    {
        RealizedSong song = RealizeSong("A = 17 {u: nope}\nA", C);
        Assert.Throws<InvalidOperationException>(() => ResolveSong(song));
    }

    [Fact]
    public void MalformedGripAnnotation_FailsLoudAtResolution()
    {
        // Five frets, not six — the opaque spec is only validated when the Features layer parses it.
        RealizedSong song = RealizeSong("A = 17 {8 x 7 9 8}\nA", C);
        Assert.Throws<FormatException>(() => ResolveSong(song));
    }
}
