using ChordFlow.Instruments.Drums;
using ChordFlow.Music.Rhythm;
using Xunit;

namespace ChordFlow.Core.Tests;

public class DrumGrooveTests
{
    [Theory]
    [InlineData(DrumVoice.Kick, "kickhit")]
    [InlineData(DrumVoice.Snare, "snarehit")]
    [InlineData(DrumVoice.HiHatClosed, "hihatclosed")]
    [InlineData(DrumVoice.HiHatOpen, "hihatopen")]
    [InlineData(DrumVoice.HiHatPedal, "pedalhihathit")]
    [InlineData(DrumVoice.Ride, "ridemiddle")]
    [InlineData(DrumVoice.RideBell, "ridebell")]
    [InlineData(DrumVoice.Crash, "crashhighhit")]
    [InlineData(DrumVoice.HighTom, "hightomhit")]
    [InlineData(DrumVoice.MidTom, "midtomhit")]
    [InlineData(DrumVoice.FloorTom, "lowfloortomhit")]
    public void Articulation_MapsEachVoiceToItsAlphaTab183Token(DrumVoice voice, string articulation)
    {
        Assert.Equal(articulation, voice.Articulation());
    }

    [Theory]
    [InlineData(DrumVoice.Kick, "BD")]
    [InlineData(DrumVoice.Snare, "SD")]
    [InlineData(DrumVoice.HiHatClosed, "HH")]
    [InlineData(DrumVoice.HiHatOpen, "OH")]
    [InlineData(DrumVoice.HiHatPedal, "PH")]
    public void Token_IsTheCanonicalShortToken(DrumVoice voice, string token)
    {
        Assert.Equal(token, voice.Token());
    }

    [Theory]
    [InlineData("BD", DrumVoice.Kick)]
    [InlineData("Kick", DrumVoice.Kick)]
    [InlineData("KD", DrumVoice.Kick)]
    [InlineData("SD", DrumVoice.Snare)]
    [InlineData("Snare", DrumVoice.Snare)]
    [InlineData("HH", DrumVoice.HiHatClosed)]
    [InlineData("HiHat", DrumVoice.HiHatClosed)]
    [InlineData("OH", DrumVoice.HiHatOpen)]
    [InlineData("FootHat", DrumVoice.HiHatPedal)]
    [InlineData("Ride", DrumVoice.Ride)]
    [InlineData("FT", DrumVoice.FloorTom)]
    public void TryParse_ResolvesShortAndFullAliases(string token, DrumVoice expected)
    {
        Assert.True(DrumVoices.TryParse(token, out DrumVoice voice));
        Assert.Equal(expected, voice);
    }

    [Theory]
    [InlineData("bd")]
    [InlineData("hihat")]
    [InlineData("KICK")]
    public void TryParse_IsCaseInsensitive(string token)
    {
        Assert.True(DrumVoices.TryParse(token, out _));
    }

    [Theory]
    [InlineData("ZZ")]
    [InlineData("cowbell")]
    [InlineData("")]
    public void TryParse_RejectsUnknownVoice(string token)
    {
        Assert.False(DrumVoices.TryParse(token, out _));
    }

    [Fact]
    public void SingleBar_WrapsLanesInOneBar()
    {
        var groove = DrumGroove.SingleBar(
            "rock",
            "Rock",
            new[]
            {
                new DrumLane(DrumVoice.HiHatClosed, new[] { RhythmEvent.Hit(0, 24), RhythmEvent.Hit(24, 24) }),
                new DrumLane(DrumVoice.Kick, new[] { RhythmEvent.Hit(0, 48) }),
            },
            TimeSignature.FourFour);

        Assert.Single(groove.Bars);
        Assert.Equal(2, groove.Bars[0].Lanes.Count);
        Assert.Equal(TimeSignature.FourFour, groove.TimeSignature);
    }

    [Fact]
    public void DistinctVoices_ReturnsFirstSeenOrderAcrossBars()
    {
        var groove = new DrumGroove(
            "g",
            "G",
            new[]
            {
                new DrumBar(new[]
                {
                    new DrumLane(DrumVoice.HiHatClosed, Array.Empty<RhythmEvent>()),
                    new DrumLane(DrumVoice.Kick, Array.Empty<RhythmEvent>()),
                }),
                new DrumBar(new[]
                {
                    new DrumLane(DrumVoice.Kick, Array.Empty<RhythmEvent>()),
                    new DrumLane(DrumVoice.Snare, Array.Empty<RhythmEvent>()),
                }),
            },
            TimeSignature.FourFour);

        Assert.Equal(
            new[] { DrumVoice.HiHatClosed, DrumVoice.Kick, DrumVoice.Snare },
            groove.DistinctVoices());
    }
}
