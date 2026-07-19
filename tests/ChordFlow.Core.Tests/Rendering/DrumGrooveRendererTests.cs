using ChordFlow.Instruments.Drums;
using ChordFlow.Music.Rhythm;
using ChordFlow.Rendering;
using Xunit;

namespace ChordFlow.Core.Tests;

public class DrumGrooveRendererTests
{
    private static string Render(string dsl, int tempo = 120)
    {
        DrumGroove groove = DrumGrooveParser.Parse("g", "Groove", dsl, TimeSignature.FourFour);
        return new DrumGrooveRenderer().Render(groove, tempo);
    }

    // The note body after the header terminator (the lone ".").
    private static string Body(string tex)
    {
        int idx = tex.IndexOf("\n.\n", StringComparison.Ordinal);
        return tex[(idx + 3)..];
    }

    [Fact]
    public void Render_EmitsPercussionHeader_AndNoKeySignature()
    {
        string tex = Render("BD :1 x...");

        Assert.Contains("\\instrument percussion", tex);
        Assert.Contains("\\articulation defaults", tex);
        Assert.Contains("\\ts 4 4", tex);
        Assert.Contains("\\tempo 120", tex);
        Assert.DoesNotContain("\\ks", tex); // percussion is keyless (req C6)
    }

    [Fact]
    public void Render_RockBeat_GroupsSimultaneousHitsInParens()
    {
        string body = Body(Render(
            "HH :2 xxxxxxxx\n" +
            "SD :2 ..x...x.\n" +
            "BD :2 x...x..."));

        // Straight 8ths → one :8 duration for the whole bar, no rests.
        Assert.StartsWith(":8 ", body);
        Assert.Contains("(kickhit hihatclosed)", body);
        Assert.Contains("(snarehit hihatclosed)", body);
        // The hi-hat sounds on all 8 eighths.
        Assert.Equal(8, Occurrences(body, "hihatclosed"));
        Assert.EndsWith(" |", body.TrimEnd());
    }

    [Fact]
    public void Render_SingleVoiceHit_HasNoParens()
    {
        string body = Body(Render("BD :2 x......."));
        Assert.Contains("kickhit", body);
        Assert.DoesNotContain("(kickhit)", body);
    }

    [Fact]
    public void Render_SparseGroove_FillsSilenceWithRests()
    {
        string body = Body(Render("BD :1 x..."));
        Assert.Contains("r", body); // kick on beat 1, then rests
    }

    [Fact]
    public void Render_Shuffle_UsesTripletTupletMarkers()
    {
        string body = Body(Render("RD :3 x.x x.x x.x x.x"));
        Assert.Contains("{tu 3}", body);
        Assert.Contains("ridemiddle", body);
    }

    [Fact]
    public void Render_MultiBar_EmitsTwoBars()
    {
        string body = Body(Render(
            "HH :2 xxxxxxxx | xxxxxxxx\n" +
            "BD :2 x...x... | x...x..."));

        Assert.Equal(2, Occurrences(body, "|"));
    }

    [Fact]
    public void Render_UsesTheRequestedTempo()
    {
        Assert.Contains("\\tempo 90", Render("BD :1 x...", tempo: 90));
    }

    private static int Occurrences(string haystack, string needle)
    {
        int count = 0;
        for (int i = haystack.IndexOf(needle, StringComparison.Ordinal);
             i >= 0;
             i = haystack.IndexOf(needle, i + needle.Length, StringComparison.Ordinal))
        {
            count++;
        }

        return count;
    }
}
