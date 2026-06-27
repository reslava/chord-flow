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
}
