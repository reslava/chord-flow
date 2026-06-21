using ChordFlow.Exercises;
using ChordFlow.Music.Harmony;
using ChordFlow.Music.Progressions;
using ChordFlow.Music.Rhythm;
using ChordFlow.Music.Songs;
using System.Collections.Generic;
using System.Linq;
using ChordFlow.Rendering;
using Xunit;

namespace ChordFlow.Core.Tests;

/// <summary>
/// <c>AlphaTexRenderer.Render(RealizedSong, …)</c>: <see cref="Song.OfProgression"/> lift parity with a
/// manual single-section song, multi-section concatenation, inline <c>\ks</c> emitted only on key change,
/// and the stateful <c>:N</c> duration carried across section seams.
/// </summary>
public class SongRenderTests
{
    private static readonly Key CMajor = new(new PitchClass(0), false);
    private static readonly TimeSignature Ts = TimeSignature.FourFour;
    private static readonly AlphaTexRenderer Renderer = new();

    private static readonly Progression Blues =
        ProgressionParser.Parse("blues", "12-Bar Blues", "17 17 17 17 47 47 17 17 57 47 17 57", Ts);

    // The bar body is everything after the header terminator ".\n".
    private static string Body(string tex)
    {
        int marker = tex.IndexOf(".\n", System.StringComparison.Ordinal);
        return tex[(marker + 2)..];
    }

    private static Song OneBluesSong(params ArrangementItem[] items)
    {
        var parts = new Dictionary<string, Part> { ["blues"] = new InlineProgression("blues", Blues) };
        return Song.FromSections("s", "Song", CMajor, parts, items);
    }

    [Fact]
    public void Render_OfProgressionLift_BarBodyMatchesManualSingleSectionSong()
    {
        // Song.OfProgression lifts a bare progression into a one-section song; its rendered body must match
        // a manually-authored single-section song over the same progression (IN2 — one realization path,
        // no Progression-vs-Song branch).
        RealizedSong viaLift = SongExpander.Expand(Song.OfProgression(Blues, CMajor), new EmptyStore());
        RealizedSong viaManual = SongExpander.Expand(OneBluesSong(new PartPlay("blues", 1)), new EmptyStore());

        string liftTex = Renderer.Render(viaLift, SeedData.Beat1And3, 100, Difficulty.Beginner);
        string manualTex = Renderer.Render(viaManual, SeedData.Beat1And3, 100, Difficulty.Beginner);

        Assert.Equal(Body(liftTex), Body(manualTex));
    }

    [Fact]
    public void Render_TwoSameKeySections_Concatenates24Bars_NoKeyChange()
    {
        RealizedSong realized = SongExpander.Expand(OneBluesSong(new PartPlay("blues", 2)), new EmptyStore());
        string body = Body(Renderer.Render(realized, SeedData.Beat1And3, 100, Difficulty.Beginner));

        Assert.Equal(24, body.Split(" |").Length - 1);   // 24 bar terminators
        Assert.DoesNotContain("\\ks", body);             // same key throughout → no inline key change
    }

    [Fact]
    public void Render_KeyChange_EmitsExactlyOneInlineKs()
    {
        var items = new ArrangementItem[]
        {
            new PartPlay("blues", 1),
            new RelativeMod(new Modulation(7, null)),   // C → G
            new PartPlay("blues", 1),
        };
        RealizedSong realized = SongExpander.Expand(OneBluesSong(items), new EmptyStore());
        string body = Body(Renderer.Render(realized, SeedData.Beat1And3, 100, Difficulty.Beginner));

        // The header carries the first key; exactly one inline \ks appears at the G section.
        Assert.Equal(1, CountOccurrences(body, "\\ks"));
    }

    [Fact]
    public void Render_DurationStateCarriesAcrossSeam()
    {
        // Beat1And3 quantizes to all-quarter slots, so a single section emits ":4" exactly once and every
        // later slot is bare. If currentDuration carries across the section seam, two sections still emit it once.
        string oneBody = Body(Renderer.Render(
            SongExpander.Expand(OneBluesSong(new PartPlay("blues", 1)), new EmptyStore()),
            SeedData.Beat1And3, 100, Difficulty.Beginner));
        string twoBody = Body(Renderer.Render(
            SongExpander.Expand(OneBluesSong(new PartPlay("blues", 2)), new EmptyStore()),
            SeedData.Beat1And3, 100, Difficulty.Beginner));

        Assert.Equal(1, CountOccurrences(oneBody, ":"));
        Assert.Equal(1, CountOccurrences(twoBody, ":"));   // not 2 — the ":4" carried over the seam
    }

    [Fact]
    public void Render_EmptySong_Throws()
    {
        Assert.Throws<System.ArgumentException>(
            () => Renderer.Render(new RealizedSong(System.Array.Empty<RealizedSection>()),
                SeedData.Beat1And3, 100, Difficulty.Beginner));
    }

    private static int CountOccurrences(string haystack, string needle)
    {
        int count = 0;
        int i = 0;
        while ((i = haystack.IndexOf(needle, i, System.StringComparison.Ordinal)) >= 0)
        {
            count++;
            i += needle.Length;
        }

        return count;
    }

    private sealed class EmptyStore : IProgressionStore
    {
        public Progression? Find(string id) => null;
    }
}
