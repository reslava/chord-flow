using ChordFlow.Music.Harmony;
using Xunit;

namespace ChordFlow.Core.Tests;

public class ChordTonesTests
{
    // Each v1 quality (C5) spelled from a C root (pitch class 0) so intervals == pitch classes.
    [Theory]
    [InlineData(Quality.Major, new[] { 0, 4, 7 })]
    [InlineData(Quality.Minor, new[] { 0, 3, 7 })]
    [InlineData(Quality.Dominant7, new[] { 0, 4, 7, 10 })]
    [InlineData(Quality.Major7, new[] { 0, 4, 7, 11 })]
    [InlineData(Quality.Minor7, new[] { 0, 3, 7, 10 })]
    [InlineData(Quality.HalfDiminished7, new[] { 0, 3, 6, 10 })]
    [InlineData(Quality.Diminished, new[] { 0, 3, 6 })]
    [InlineData(Quality.Diminished7, new[] { 0, 3, 6, 9 })]
    [InlineData(Quality.Augmented, new[] { 0, 4, 8 })]
    [InlineData(Quality.Major6, new[] { 0, 4, 7, 9 })]
    [InlineData(Quality.Minor6, new[] { 0, 3, 7, 9 })]
    public void Of_EachQuality_SpellsTheC5IntervalSet(Quality quality, int[] expected)
    {
        var chord = new Chord(new PitchClass(0), quality);

        var intervals = ChordTones.Of(chord).Select(t => t.Interval).ToArray();

        Assert.Equal(expected, intervals);
    }

    [Fact]
    public void PitchClassesOf_TransposesRelativeToRoot()
    {
        // G7 (root pc 7) → G B D F = 7, 11, 2, 5.
        var g7 = new Chord(new PitchClass(7), Quality.Dominant7);

        var pcs = ChordTones.PitchClassesOf(g7).Select(p => p.Value).ToArray();

        Assert.Equal(new[] { 7, 11, 2, 5 }, pcs);
    }

    [Fact]
    public void Of_TagsGuideTones_ThirdAndSeventh()
    {
        // Dom7 guide tones: the major 3rd (+4) and the minor 7th (+10).
        var g7 = new Chord(new PitchClass(7), Quality.Dominant7);

        var tones = ChordTones.Of(g7);

        Assert.Equal(ChordToneFunction.Root, tones[0].Function);
        Assert.Equal(ChordToneFunction.Third, tones[1].Function);
        Assert.Equal(ChordToneFunction.Fifth, tones[2].Function);
        Assert.Equal(ChordToneFunction.Seventh, tones[3].Function);

        ChordTone third = Assert.Single(tones, t => t.Function == ChordToneFunction.Third);
        ChordTone seventh = Assert.Single(tones, t => t.Function == ChordToneFunction.Seventh);
        Assert.Equal(11, third.PitchClassFor(g7.Root).Value);  // B
        Assert.Equal(5, seventh.PitchClassFor(g7.Root).Value); // F
    }

    [Theory]
    // Semitone 9 is enharmonically ambiguous — it resolves to a function by the quality's formula degree:
    // the 6 of a 6/m6 chord vs the bb7 of a dim7. (IN10/C6)
    [InlineData(Quality.Major6, ChordToneFunction.Sixth)]
    [InlineData(Quality.Minor6, ChordToneFunction.Sixth)]
    [InlineData(Quality.Diminished7, ChordToneFunction.Seventh)]
    public void Of_SemitoneNine_ResolvesToFunctionByQuality(Quality quality, ChordToneFunction expected)
    {
        var chord = new Chord(new PitchClass(0), quality);

        ChordTone tone = Assert.Single(ChordTones.Of(chord), t => t.Interval == 9);

        Assert.Equal(expected, tone.Function);
    }

    [Fact]
    public void Of_HalfDiminished_FifthIsDiminished()
    {
        // m7b5 fifth is the diminished 5th (+6), still classified as the Fifth.
        var chord = new Chord(new PitchClass(0), Quality.HalfDiminished7);

        ChordTone fifth = Assert.Single(ChordTones.Of(chord), t => t.Function == ChordToneFunction.Fifth);

        Assert.Equal(6, fifth.Interval);
    }
}
