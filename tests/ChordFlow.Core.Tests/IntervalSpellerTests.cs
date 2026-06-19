using ChordFlow.Domain;
using Xunit;

namespace ChordFlow.Core.Tests;

/// <summary>
/// The single interval-spelling authority (the peer of <see cref="NoteSpeller"/>). Pins both label
/// spaces: <see cref="IntervalSpeller.Name"/> — the computed, unfolded flats substrate vocabulary —
/// and <see cref="IntervalSpeller.Label"/> — the role-keyed chord-context spelling with conventional
/// compound tensions. <see cref="VoicingDiagramTests"/> is the companion byte-for-byte oracle that the
/// voicing diagram's labels are unchanged after it delegates here.
/// </summary>
public class IntervalSpellerTests
{
    [Theory]
    [InlineData(0, "1")]
    [InlineData(1, "b2")]
    [InlineData(2, "2")]
    [InlineData(3, "b3")]
    [InlineData(4, "3")]
    [InlineData(5, "4")]
    [InlineData(6, "b5")]
    [InlineData(7, "5")]
    [InlineData(8, "b6")]
    [InlineData(9, "6")]
    [InlineData(10, "b7")]
    [InlineData(11, "7")]
    public void Name_SpellsTheFirstOctaveAsFlatsDegrees(int semitone, string expected) =>
        Assert.Equal(expected, IntervalSpeller.Name(semitone));

    [Theory]
    [InlineData(12, "8")]   // the octave
    [InlineData(13, "b9")]
    [InlineData(14, "9")]
    [InlineData(15, "b10")]
    [InlineData(16, "10")]
    [InlineData(17, "11")]
    [InlineData(18, "b12")]
    [InlineData(19, "12")]
    [InlineData(20, "b13")]
    [InlineData(21, "13")]
    [InlineData(22, "b14")]
    [InlineData(23, "14")]
    [InlineData(24, "15")]  // the double octave
    public void Name_UnfoldsTheSecondOctaveByFormula(int semitone, string expected) =>
        Assert.Equal(expected, IntervalSpeller.Name(semitone));

    [Fact]
    public void Name_RejectsNegativeDistances() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => IntervalSpeller.Name(-1));

    [Theory]
    [InlineData(0, ChordToneFunction.Root, "R")]
    [InlineData(3, ChordToneFunction.Third, "b3")]
    [InlineData(4, ChordToneFunction.Third, "3")]
    [InlineData(6, ChordToneFunction.Fifth, "b5")]
    [InlineData(7, ChordToneFunction.Fifth, "5")]
    [InlineData(8, ChordToneFunction.Fifth, "#5")]   // aug
    [InlineData(9, ChordToneFunction.Seventh, "bb7")] // dim7
    [InlineData(10, ChordToneFunction.Seventh, "b7")]
    [InlineData(11, ChordToneFunction.Seventh, "7")]
    public void Label_SpellsChordTonesByRole(int semitone, ChordToneFunction role, string expected) =>
        Assert.Equal(expected, IntervalSpeller.Label(semitone, role));

    [Theory]
    [InlineData(0, "R")]
    [InlineData(1, "b9")]
    [InlineData(2, "9")]
    [InlineData(3, "#9")]
    [InlineData(4, "3")]
    [InlineData(5, "11")]
    [InlineData(6, "#11")]
    [InlineData(7, "5")]
    [InlineData(8, "b13")]
    [InlineData(9, "13")]
    [InlineData(10, "b7")]
    [InlineData(11, "7")]
    public void Label_FallsBackToConventionalTensionsWhenRoleIsNull(int semitone, string expected) =>
        Assert.Equal(expected, IntervalSpeller.Label(semitone, role: null));

    [Theory]
    [InlineData(12, ChordToneFunction.Root, "R")]      // 12 mod 12 = 0
    [InlineData(15, null, "#9")]                        // 15 mod 12 = 3, tension
    [InlineData(20, ChordToneFunction.Fifth, "#5")]    // 20 mod 12 = 8, an aug fifth up an octave
    public void Label_ReducesTheSemitoneModTwelve(int semitone, ChordToneFunction? role, string expected) =>
        Assert.Equal(expected, IntervalSpeller.Label(semitone, role));
}
