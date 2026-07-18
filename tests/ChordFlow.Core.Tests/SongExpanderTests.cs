using ChordFlow.Music.Harmony;
using ChordFlow.Music.Progressions;
using ChordFlow.Music.Rhythm;
using ChordFlow.Music.Songs;
using System.Collections.Generic;
using System.Linq;
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

    // A song whose sections sit in different keys AND modes is already expressible via the `key` stream — a minor
    // section (`key Am`), a major one (`key G`), a minor one (`key Bm`) — and each realizes in its own key + mode.
    // This confirms the multi-key/multi-mode arrangement threads the mode through (minor-mode-ui-threading IN6).
    [Fact]
    public void Expand_MultiKeyMultiMode_RealizesEachSectionInItsOwnKeyAndMode()
    {
        RealizedSong realized = ParseAndExpand(string.Join("\n",
            "a = 1 4 5", "b = 1 4 5", "c = 1 4 5",
            "key Am", "a",
            "key G", "b",
            "key Bm", "c"));

        Assert.Equal(3, realized.Sections.Count);
        Assert.Equal((9, true), (realized.Sections[0].Key.Tonic.Value, realized.Sections[0].Key.IsMinor));   // A minor
        Assert.Equal((7, false), (realized.Sections[1].Key.Tonic.Value, realized.Sections[1].Key.IsMinor));  // G major
        Assert.Equal((11, true), (realized.Sections[2].Key.Tonic.Value, realized.Sections[2].Key.IsMinor));  // B minor
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

    // ---- Annotations + voice map + degree carried through realization (IN1/IN4/IN5) ----

    private static RealizedSong ParseAndExpand(string dsl) =>
        SongExpander.Expand(SongParser.Parse("s", "S", dsl, Ts), EmptyStore);

    [Fact]
    public void Expand_CarriesSongVoiceMap_OntoRealizedSong()
    {
        RealizedSong realized = ParseAndExpand("voice *7 = 3 3 2 3 1 x\nA = 17 47 17 17\nA");

        Assert.Equal("3 3 2 3 1 x", realized.Voices[VoiceSelector.ForQuality(Quality.Dominant7)]);
    }

    [Fact]
    public void Expand_CarriesPerChordAnnotation_OntoRealizedSpan()
    {
        RealizedSong realized = ParseAndExpand("A = 17 {8 x 7 9 8 x} 47 17 17\nA");

        RealizedSpan first = realized.Sections[0].Bars[0].Spans[0];
        Assert.Equal("8 x 7 9 8 x", first.VoicingAnnotation);
        // A plain chord keeps a null annotation.
        Assert.Null(realized.Sections[0].Bars[1].Spans[0].VoicingAnnotation);
    }

    [Fact]
    public void Expand_RealizedSpan_ExposesOriginatingDegree_ThroughTransposition()
    {
        // In A major (key A), the I7 chord's root is A, but the span still reports degree 1 (dominant 7) so a
        // degree-scoped `voice 17` default can match after transposition.
        RealizedSong realized = ParseAndExpand("key A\nA = 17 47 17 17\nA");

        RealizedSpan first = realized.Sections[0].Bars[0].Spans[0];
        Assert.Equal(new RomanDegree(1, Quality.Dominant7), first.Degree);
        Assert.Equal(new PitchClass(9), first.Chord.Root);   // A = 9, confirms it really transposed
    }
}
