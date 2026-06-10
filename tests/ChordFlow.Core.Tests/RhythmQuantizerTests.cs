using ChordFlow.Domain;
using ChordFlow.Rendering;
using Xunit;

namespace ChordFlow.Core.Tests;

public class RhythmQuantizerTests
{
    private static (int NoteValue, bool IsRest, bool Tied) S(RhythmSlot s) => (s.NoteValue, s.IsRest, s.TiedToPrevious);

    [Fact]
    public void TimeSignature_FourFour_DerivesBarAndBeatTicks()
    {
        Assert.Equal(48, TimeSignature.FourFour.BeatTicks);
        Assert.Equal(192, TimeSignature.FourFour.BarTicks);
        Assert.Equal(48, TickGrid.Ppq);
        Assert.Equal(192, TickGrid.WholeNoteTicks);
    }

    [Fact]
    public void Quantize_Beat1_IsQuarterHitThenThreeQuarterRests()
    {
        var slots = RhythmQuantizer.Quantize(SeedData.Beat1.Events, TimeSignature.FourFour);

        Assert.Equal(
            new (int, bool, bool)[] { (4, false, false), (4, true, false), (4, true, false), (4, true, false) },
            slots.Select(S));
    }

    [Fact]
    public void Quantize_Beat1And3_IsHitRestHitRest()
    {
        var slots = RhythmQuantizer.Quantize(SeedData.Beat1And3.Events, TimeSignature.FourFour);

        Assert.Equal(
            new (int, bool, bool)[] { (4, false, false), (4, true, false), (4, false, false), (4, true, false) },
            slots.Select(S));
    }

    [Fact]
    public void Quantize_Quarters_IsFourQuarterHits()
    {
        var slots = RhythmQuantizer.Quantize(SeedData.Quarters.Events, TimeSignature.FourFour);

        Assert.Equal(
            new (int, bool, bool)[] { (4, false, false), (4, false, false), (4, false, false), (4, false, false) },
            slots.Select(S));
        Assert.All(slots, s => Assert.False(s.IsRest));
    }

    [Fact]
    public void Quantize_NoteSpanningTwoBeats_SplitsIntoTiedQuarters()
    {
        // A half note from beat 1: two quarters, the second tied to the first.
        var events = new[] { RhythmEvent.Hit(0, 96) };

        var slots = RhythmQuantizer.Quantize(events, TimeSignature.FourFour);

        Assert.Equal((4, false, false), S(slots[0]));
        Assert.Equal((4, false, true), S(slots[1])); // tied continuation
        Assert.Equal(2, slots.Count(s => !s.IsRest));
    }

    [Fact]
    public void Quantize_SixteenthHitOnDownbeat_EmitsSixteenthThenRests()
    {
        var events = new[] { RhythmEvent.Hit(0, 12) }; // one sixteenth

        var slots = RhythmQuantizer.Quantize(events, TimeSignature.FourFour);

        Assert.Equal((16, false, false), S(slots[0]));
        // Remainder of beat 1 is an eighth + sixteenth of rest, then three quarter rests.
        Assert.Equal((8, true, false), S(slots[1]));
        Assert.Equal((16, true, false), S(slots[2]));
        Assert.Equal((4, true, false), S(slots[3]));
    }

    [Fact]
    public void Quantize_SortsUnorderedEvents()
    {
        var events = new[] { RhythmEvent.Hit(96, 48), RhythmEvent.Hit(0, 48) };

        var slots = RhythmQuantizer.Quantize(events, TimeSignature.FourFour);

        Assert.False(slots[0].IsRest); // beat 1 hit comes first after sorting
        Assert.True(slots[1].IsRest);
        Assert.False(slots[2].IsRest); // beat 3 hit
    }

    [Fact]
    public void Quantize_OverlappingEvents_Throws()
    {
        var events = new[] { RhythmEvent.Hit(0, 96), RhythmEvent.Hit(48, 48) };

        Assert.Throws<ArgumentException>(() => RhythmQuantizer.Quantize(events, TimeSignature.FourFour));
    }

    [Fact]
    public void Quantize_EventBeyondBar_Throws()
    {
        var events = new[] { RhythmEvent.Hit(168, 48) }; // ends at 216 > 192

        Assert.Throws<ArgumentException>(() => RhythmQuantizer.Quantize(events, TimeSignature.FourFour));
    }

    [Fact]
    public void Quantize_Pickup_LeadingMeasureQuantizesToItsOwnLength()
    {
        // A one-beat pickup: a single quarter note filling a 48-tick leading measure.
        var pickup = new PickupMeasure(new[] { RhythmEvent.Hit(0, 48) }, LengthTicks: 48);

        var slots = RhythmQuantizer.Quantize(pickup);

        Assert.Single(slots);
        Assert.Equal((4, false, false), S(slots[0]));
    }

    [Fact]
    public void Quantize_PopulatesStartTick_AtEachSlotOnset()
    {
        var slots = RhythmQuantizer.Quantize(SeedData.Quarters.Events, TimeSignature.FourFour);

        Assert.Equal(new[] { 0, 48, 96, 144 }, slots.Select(s => s.StartTick));
    }

    [Fact]
    public void Quantize_NoteAcrossChordBoundary_ReAttacks_NotTied()
    {
        // A half note from beat 1 with a chord boundary at tick 48: two quarters, the second
        // RE-ATTACKED (not tied) — you cannot tie one chord into a different chord.
        var events = new[] { RhythmEvent.Hit(0, 96) };

        var slots = RhythmQuantizer.Quantize(events, TimeSignature.FourFour, new[] { 48 });

        // The two sounding quarters (the rest of the bar past tick 96 fills with rests).
        Assert.Equal((4, false, false), S(slots[0]));
        Assert.Equal((4, false, false), S(slots[1])); // re-attack, NOT tied
        Assert.Equal(0, slots[0].StartTick);
        Assert.Equal(48, slots[1].StartTick);
        Assert.Equal(2, slots.Count(s => !s.IsRest));
    }

    [Fact]
    public void Quantize_NoteAcrossBeatLineWithinSameChord_StillTies()
    {
        // Same half note but no chord boundary — the beat-line split still ties (existing behaviour).
        var events = new[] { RhythmEvent.Hit(0, 96) };

        var slots = RhythmQuantizer.Quantize(events, TimeSignature.FourFour, Array.Empty<int>());

        Assert.Equal((4, false, false), S(slots[0]));
        Assert.Equal((4, false, true), S(slots[1])); // tied continuation
    }

    [Fact]
    public void Quantize_WholeNoteAcrossFourChordBoundaries_ReAttacksEach()
    {
        // A sustained whole note over a 4-chord bar (boundaries 48/96/144): four quarter attacks,
        // none tied, each carrying its onset tick.
        var events = new[] { RhythmEvent.Hit(0, 192) };

        var slots = RhythmQuantizer.Quantize(events, TimeSignature.FourFour, new[] { 48, 96, 144 });

        Assert.Equal(4, slots.Count);
        Assert.All(slots, s => Assert.False(s.IsRest));
        Assert.All(slots, s => Assert.False(s.TiedToPrevious)); // every chord re-attacks
        Assert.Equal(new[] { 0, 48, 96, 144 }, slots.Select(s => s.StartTick));
    }

    [Fact]
    public void Quantize_RestAcrossChordBoundary_StaysRest_NoPhantomAttack()
    {
        // Beat-1 hit then a rest that spans a chord boundary at 96: the boundary must NOT turn the
        // rest into a strike — the chord changes silently and is first heard at the next attack.
        var slots = RhythmQuantizer.Quantize(SeedData.Beat1.Events, TimeSignature.FourFour, new[] { 96 });

        Assert.False(slots[0].IsRest);                 // the beat-1 hit
        Assert.Equal(1, slots.Count(s => !s.IsRest));  // exactly one attack in the bar
        Assert.All(slots.Skip(1), s => Assert.True(s.IsRest));
    }
}
