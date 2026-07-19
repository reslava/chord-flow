using ChordFlow.Exercises;
using ChordFlow.Instruments.Drums;
using ChordFlow.Music.Harmony;
using ChordFlow.Music.Progressions;
using ChordFlow.Music.Rhythm;
using ChordFlow.Music.Songs;
using Xunit;

namespace ChordFlow.Core.Tests;

/// <summary>
/// The play-unit is a typed <see cref="InstrumentPart"/> union (drums-under-a-song D1/IN1): the canonical
/// member is <see cref="Exercise.Parts"/>; <see cref="Exercise.Comping"/>/<see cref="Exercise.Lead"/>/
/// <see cref="Exercise.Drums"/> are intent accessors that also enforce the invariants (exactly one comping,
/// at most one lead/drum — C4). The convenience constructor keeps the pre-parts guitar-only call shape.
/// </summary>
public class ExerciseModelTests
{
    private static readonly Song Song = Song.OfProgression(
        ProgressionParser.Parse("p", "P", "1", TimeSignature.FourFour),
        new Key(new PitchClass(0), IsMinor: false));

    private static DrumGroove Groove() => DrumGroove.SingleBar(
        "g", "G",
        new[] { new DrumLane(DrumVoice.Kick, new[] { RhythmEvent.Hit(0, 48) }) },
        TimeSignature.FourFour);

    private static Exercise Ex(params InstrumentPart[] parts) =>
        new(Song, parts, KeyOverride: null, Tempo: 80, Difficulty.Beginner, TripletFeel.None);

    [Fact]
    public void ConvenienceCtor_BuildsCompingAndOptionalLeadParts()
    {
        var comping = SeedData.Beat1And3;
        var lead = SeedData.Quarters;

        var withLead = new Exercise(Song, comping, lead, null, 80, Difficulty.Beginner, TripletFeel.None);
        Assert.Same(comping, withLead.Comping);
        Assert.Same(lead, withLead.Lead);
        Assert.Null(withLead.Drums);

        var noLead = new Exercise(Song, comping, Lead: null, KeyOverride: null, 80, Difficulty.Beginner, TripletFeel.None);
        Assert.Same(comping, noLead.Comping);
        Assert.Null(noLead.Lead);
    }

    [Fact]
    public void Accessors_ProjectEachPartArm()
    {
        var comping = SeedData.Beat1And3;
        var groove = Groove();
        var ex = Ex(new CompingPart(comping), new DrumPart(groove) { Volume = 0.5 });

        Assert.Same(comping, ex.Comping);
        Assert.Null(ex.Lead);
        Assert.Same(groove, ex.Drums);
        // Per-part mix rides the part, not the Exercise.
        Assert.Equal(0.5, ((DrumPart)ex.Parts[1]).Volume);
    }

    [Fact]
    public void Comping_IsRequired_MissingThrows()
    {
        var ex = Ex(new DrumPart(Groove())); // no comping part
        Assert.ThrowsAny<System.InvalidOperationException>(() => ex.Comping);
    }

    [Fact]
    public void AtMostOne_DrumPart_AmbiguousThrows()
    {
        var ex = Ex(new CompingPart(SeedData.Beat1And3), new DrumPart(Groove()), new DrumPart(Groove()));
        Assert.ThrowsAny<System.InvalidOperationException>(() => ex.Drums);
    }
}
