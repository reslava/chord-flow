using ChordFlow.Domain;
using Xunit;

namespace ChordFlow.Tests;

public class LeadTargetsTests
{
    private static int Pc(Chord chord, TargetZone z) => LeadTargets.PitchClassOf(chord, z).Value;

    [Fact]
    public void GuideTones_G7_AreThirdBAndFlatSeventhF()
    {
        var g7 = new Chord(new PitchClass(7), Quality.Dominant7); // G7

        var guides = LeadTargets.GuideTones(g7);

        Assert.Equal(2, guides.Count);
        Assert.All(guides, z => Assert.Equal(Importance.Primary, z.Importance));

        TargetZone third = Assert.Single(guides, z => z.Tone.Function == ChordToneFunction.Third);
        TargetZone seventh = Assert.Single(guides, z => z.Tone.Function == ChordToneFunction.Seventh);
        Assert.Equal(11, Pc(g7, third));  // B
        Assert.Equal(5, Pc(g7, seventh)); // F
    }

    // ii–V–I in C: Dm7, G7, Cmaj7. Guide-tone targets resolve to the expected pitch classes.
    [Theory]
    [InlineData(2, Quality.Minor7, 5, 0)]      // Dm7  -> 3 = F (5),  b7 = C (0)
    [InlineData(7, Quality.Dominant7, 11, 5)]  // G7   -> 3 = B (11), b7 = F (5)
    [InlineData(0, Quality.Major7, 4, 11)]     // Cmaj7-> 3 = E (4),  7  = B (11)
    public void GuideTones_IiVI_ResolveToExpectedPitchClasses(int root, Quality quality, int expectedThird, int expectedSeventh)
    {
        var chord = new Chord(new PitchClass(root), quality);

        var guides = LeadTargets.GuideTones(chord);

        int third = Pc(chord, guides.Single(z => z.Tone.Function == ChordToneFunction.Third));
        int seventh = Pc(chord, guides.Single(z => z.Tone.Function == ChordToneFunction.Seventh));
        Assert.Equal(expectedThird, third);
        Assert.Equal(expectedSeventh, seventh);
    }

    [Fact]
    public void Resolve_G7Third_ReturnsOnlyPositionsThatSoundB()
    {
        var g7 = new Chord(new PitchClass(7), Quality.Dominant7);
        TargetZone third = LeadTargets.GuideTones(g7).Single(z => z.Tone.Function == ChordToneFunction.Third);

        var positions = LeadTargets.Resolve(g7, third);

        Assert.NotEmpty(positions);
        // Every returned fret must sound a B (pitch class 11).
        var openPc = new[] { 0, 4, 11, 7, 2, 9, 4 };
        Assert.All(positions, p => Assert.Equal(11, (openPc[p.String] + p.Fret) % 12));
        // Open B string (string 2, fret 0) is a B.
        Assert.Contains(new FretPosition(2, 0), positions);
    }

    [Fact]
    public void GuideTones_Triad_HasOnlyTheThird()
    {
        var cMajor = new Chord(new PitchClass(0), Quality.Major); // triad, no 7th

        var guides = LeadTargets.GuideTones(cMajor);

        Assert.Single(guides);
        Assert.Equal(ChordToneFunction.Third, guides[0].Tone.Function);
    }
}
