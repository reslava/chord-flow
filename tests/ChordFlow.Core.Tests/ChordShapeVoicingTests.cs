using ChordFlow.Instruments.Guitar;
using ChordFlow.Music.Harmony;
using Xunit;

namespace ChordFlow.Core.Tests;

public class ChordShapeVoicingTests
{
    private static readonly PitchClass C = new(0);

    [Fact]
    public void ToVoicing_MutedStringBecomesMuted_RestBecomePositions()
    {
        // Dominant7 A-shape at C = "x 3 5 3 5 3" (low-E→high-E): string 6 muted, strings 5..1 sounded.
        ChordShape shape = CagedDerivation.Derive(Quality.Dominant7, CagedShape.A, C, 3, 19);

        Voicing voicing = ChordShapeVoicing.ToVoicing(shape);

        Assert.Equal(new[] { 6 }, voicing.MutedStrings);
        Assert.Equal(5, voicing.Positions.Count);
        Assert.DoesNotContain(voicing.Positions, p => p.String == 6);
        // Every sounded string maps to its derived fret.
        Assert.Equal(3, voicing.Positions.Single(p => p.String == 5).Fret);
        Assert.Equal(5, voicing.Positions.Single(p => p.String == 4).Fret);
    }

    [Fact]
    public void ToVoicing_FirstFret_IsLowestSoundingFret()
    {
        ChordShape shape = CagedDerivation.Derive(Quality.Dominant7, CagedShape.A, C, 3, 19);

        Voicing voicing = ChordShapeVoicing.ToVoicing(shape);

        Assert.Equal(3, voicing.FirstFret);
    }

    [Fact]
    public void ToVoicing_AllSoundedShape_HasNoMutedStrings()
    {
        // Dominant7 E-shape at C = "8 10 8 9 8 8" — every string sounds.
        ChordShape shape = CagedDerivation.Derive(Quality.Dominant7, CagedShape.E, C, 8, 24);

        Voicing voicing = ChordShapeVoicing.ToVoicing(shape);

        Assert.Null(voicing.MutedStrings);
        Assert.Equal(6, voicing.Positions.Count);
        Assert.Null(voicing.BarreFret);
    }
}
