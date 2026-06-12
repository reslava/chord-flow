using System.Collections.Generic;
using System.Linq;
using ChordFlow.Domain;
using Xunit;

namespace ChordFlow.Core.Tests;

/// <summary>
/// <see cref="SongExpander.Expand"/>: the modulation fold (accumulation + absolute reset), repeat expansion,
/// local-shadows-store resolution, and fail-loud on an unresolved stored reference.
/// </summary>
public class SongExpanderTests
{
    private static readonly Key CMajor = new(new PitchClass(0), false);
    private static readonly TimeSignature Ts = TimeSignature.FourFour;

    // A dictionary-backed IProgressionStore over parsed DSL.
    private sealed class FakeStore : IProgressionStore
    {
        private readonly Dictionary<string, Progression> _byId;

        public FakeStore(params (string Id, string Dsl)[] progs) =>
            _byId = progs.ToDictionary(p => p.Id, p => ProgressionParser.Parse(p.Id, p.Id, p.Dsl, Ts));

        public Progression? Find(string id) => _byId.TryGetValue(id, out Progression? p) ? p : null;
    }

    private static Part Inline(string name, string dsl) =>
        new InlineProgression(name, ProgressionParser.Parse(name, name, dsl, Ts));

    private static readonly IProgressionStore EmptyStore = new FakeStore();

    [Fact]
    public void Expand_RelativeModulations_Accumulate()
    {
        var parts = new Dictionary<string, Part> { ["A"] = Inline("A", "1 4 5 1") };
        var items = new ArrangementItem[]
        {
            new PartPlay("A", 1),                          // key C (0)
            new RelativeMod(new Modulation(7, null)),      // → G (7)
            new PartPlay("A", 1),
            new RelativeMod(new Modulation(7, null)),      // → D (2)
            new PartPlay("A", 1),
        };
        Song song = Song.FromSections("s", "S", CMajor, parts, items);

        RealizedSong realized = SongExpander.Expand(song, EmptyStore);

        Assert.Equal(new[] { 0, 7, 2 }, realized.Sections.Select(s => s.Key.Tonic.Value));
    }

    [Fact]
    public void Expand_AbsoluteKey_ResetsRunningKey()
    {
        var parts = new Dictionary<string, Part> { ["A"] = Inline("A", "1 4 5 1") };
        var items = new ArrangementItem[]
        {
            new PartPlay("A", 1),                          // C (0)
            new RelativeMod(new Modulation(7, null)),      // → G (7)
            new PartPlay("A", 1),
            new AbsoluteKey(CMajor),                       // reset home → C (0)
            new PartPlay("A", 1),
        };
        Song song = Song.FromSections("s", "S", CMajor, parts, items);

        RealizedSong realized = SongExpander.Expand(song, EmptyStore);

        Assert.Equal(new[] { 0, 7, 0 }, realized.Sections.Select(s => s.Key.Tonic.Value));
    }

    [Fact]
    public void Expand_Repeat_ExpandsToNSections_WithLabelAndKey()
    {
        var parts = new Dictionary<string, Part> { ["verse"] = Inline("verse", "1 4 5 1") };
        var items = new ArrangementItem[] { new PartPlay("verse", 3) };
        Song song = Song.FromSections("s", "S", CMajor, parts, items);

        RealizedSong realized = SongExpander.Expand(song, EmptyStore);

        Assert.Equal(3, realized.Sections.Count);
        Assert.All(realized.Sections, s =>
        {
            Assert.Equal("verse", s.Label);
            Assert.Equal(0, s.Key.Tonic.Value);
            Assert.Equal(4, s.Bars.Count);
        });
    }

    [Fact]
    public void Expand_LocalInline_ShadowsStoredProgressionOfSameName()
    {
        // Local "blues" is 4 bars; the stored "blues" is 12 — if the local shadows, we get 4 bars.
        var parts = new Dictionary<string, Part> { ["blues"] = Inline("blues", "1 4 5 1") };
        var items = new ArrangementItem[] { new PartPlay("blues", 1) };
        Song song = Song.FromSections("s", "S", CMajor, parts, items);
        var store = new FakeStore(("blues", "17 17 17 17 47 47 17 17 57 47 17 57"));

        RealizedSong realized = SongExpander.Expand(song, store);

        Assert.Equal(4, Assert.Single(realized.Sections).Bars.Count);
    }

    [Fact]
    public void Expand_StoredReference_ResolvesAgainstStore()
    {
        var parts = new Dictionary<string, Part> { ["verse"] = new ProgressionReference("verse", "12bar_blues") };
        var items = new ArrangementItem[] { new PartPlay("verse", 1) };
        Song song = Song.FromSections("s", "S", CMajor, parts, items);
        var store = new FakeStore(("12bar_blues", "17 17 17 17 47 47 17 17 57 47 17 57"));

        RealizedSong realized = SongExpander.Expand(song, store);

        Assert.Equal(12, Assert.Single(realized.Sections).Bars.Count);
    }

    [Fact]
    public void Expand_UnresolvedReference_FailsLoud()
    {
        var parts = new Dictionary<string, Part> { ["verse"] = new ProgressionReference("verse", "gone") };
        var items = new ArrangementItem[] { new PartPlay("verse", 1) };
        Song song = Song.FromSections("s", "S", CMajor, parts, items);

        var ex = Assert.Throws<System.InvalidOperationException>(() => SongExpander.Expand(song, EmptyStore));
        Assert.Contains("not found", ex.Message);
    }
}
