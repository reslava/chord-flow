using ChordFlow.Music.Progressions;
using ChordFlow.Music.Rhythm;
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
    public void Quantize_Beat1_RingsAsAWholeNote()
    {
        // Sustain-literal Beat 1 rings the whole bar → a single whole note (coalesced, no rests/ties).
        var slots = RhythmQuantizer.Quantize(SeedData.Beat1.Bars[0].Events, TimeSignature.FourFour);

        Assert.Equal(new (int, bool, bool)[] { (1, false, false) }, slots.Select(S));
    }

    [Fact]
    public void Quantize_Beat1And3_IsTwoHalfNotes()
    {
        // Sustain-literal Beats 1 & 3 ring to the next strike → two half notes (coalesced, no rests/ties).
        var slots = RhythmQuantizer.Quantize(SeedData.Beat1And3.Bars[0].Events, TimeSignature.FourFour);

        Assert.Equal(
            new (int, bool, bool)[] { (2, false, false), (2, false, false) },
            slots.Select(S));
    }

    [Fact]
    public void Quantize_Quarters_IsFourQuarterHits()
    {
        var slots = RhythmQuantizer.Quantize(SeedData.Quarters.Bars[0].Events, TimeSignature.FourFour);

        Assert.Equal(
            new (int, bool, bool)[] { (4, false, false), (4, false, false), (4, false, false), (4, false, false) },
            slots.Select(S));
        Assert.All(slots, s => Assert.False(s.IsRest));
    }

    [Fact]
    public void Quantize_HalfNoteOnBeat1_CoalescesToASingleHalfNote()
    {
        // A beat-aligned half note from beat 1 is one ":2" slot — NOT two tied quarters (coalescing).
        var events = new[] { RhythmEvent.Hit(0, 96) };

        var slots = RhythmQuantizer.Quantize(events, TimeSignature.FourFour);

        Assert.Equal((2, false, false), S(slots[0]));        // a single half note, untied
        Assert.Equal(1, slots.Count(s => !s.IsRest));
        Assert.Equal((2, true, false), S(slots[1]));         // beats 3 & 4 coalesce to one half rest
        Assert.Equal(1, slots.Count(s => s.IsRest));
    }

    [Fact]
    public void Quantize_WholeNote_CoalescesToASingleWholeNote()
    {
        var slots = RhythmQuantizer.Quantize(new[] { RhythmEvent.Hit(0, 192) }, TimeSignature.FourFour);

        Assert.Single(slots);
        Assert.Equal((1, false, false), S(slots[0])); // one ":1", no ties
    }

    [Fact]
    public void Quantize_SyncopatedHalfNoteOnBeat2_IsOneHalfNote_NoTie()
    {
        // A half note starting on beat 2 is a single representable value (96t = :2), so it emits ONE
        // half note — the author chooses to split it into tied quarters with '_' if they want that.
        var events = new[] { RhythmEvent.Hit(48, 96) };

        var slots = RhythmQuantizer.Quantize(events, TimeSignature.FourFour);

        Assert.Equal((4, true, false), S(slots[0]));   // beat-1 quarter rest
        Assert.Equal((2, false, false), S(slots[1]));  // a single half note at beat 2, untied
        Assert.Equal(48, slots[1].StartTick);
        Assert.Equal(1, slots.Count(s => !s.IsRest));
        Assert.DoesNotContain(slots, s => s.TiedToPrevious);
    }

    [Fact]
    public void Quantize_SixteenthHitOnDownbeat_EmitsSixteenthThenRests()
    {
        var events = new[] { RhythmEvent.Hit(0, 12) }; // one sixteenth

        var slots = RhythmQuantizer.Quantize(events, TimeSignature.FourFour);

        Assert.Equal((16, false, false), S(slots[0]));
        // Aligned rest fill: a 16th rest (to the eighth grid), an eighth rest (rest of beat 1), a quarter
        // rest (beat 2), then a half rest (beats 3-4 coalesced).
        Assert.Equal((16, true, false), S(slots[1]));
        Assert.Equal((8, true, false), S(slots[2]));
        Assert.Equal((4, true, false), S(slots[3]));
        Assert.Equal((2, true, false), S(slots[4]));
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
        var slots = RhythmQuantizer.Quantize(SeedData.Quarters.Bars[0].Events, TimeSignature.FourFour);

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
    public void Quantize_HalfNoteWithinSameChord_CoalescesNoTie()
    {
        // Same beat-aligned half note, no chord boundary — coalesces to one ":2", no tie.
        var events = new[] { RhythmEvent.Hit(0, 96) };

        var slots = RhythmQuantizer.Quantize(events, TimeSignature.FourFour, Array.Empty<int>());

        Assert.Equal((2, false, false), S(slots[0]));
        Assert.DoesNotContain(slots, s => s.TiedToPrevious);
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
        // A staccato beat-1 quarter then a rest that spans a chord boundary at 96: the boundary must NOT
        // turn the rest into a strike — the chord changes silently and is first heard at the next attack.
        var events = new[] { RhythmEvent.Hit(0, 48) }; // explicit staccato hit (Beat 1 now rings)
        var slots = RhythmQuantizer.Quantize(events, TimeSignature.FourFour, new[] { 96 });

        Assert.False(slots[0].IsRest);                 // the beat-1 hit
        Assert.Equal(1, slots.Count(s => !s.IsRest));  // exactly one attack in the bar
        Assert.All(slots.Skip(1), s => Assert.True(s.IsRest));
    }

    // ---- Dotted notes + authored ties (accurate-notation grammar) -----------

    [Fact]
    public void Quantize_DottedQuarter_IsOneDottedSlot()
    {
        // 72 ticks = a dotted quarter: ONE slot (NoteValue 4 + Dotted), not quarter + tied eighth.
        var slots = RhythmQuantizer.Quantize(new[] { RhythmEvent.Hit(0, 72) }, TimeSignature.FourFour);

        Assert.Equal(4, slots[0].NoteValue);
        Assert.True(slots[0].Dotted);
        Assert.False(slots[0].TiedToPrevious);
        Assert.False(slots[0].IsRest);
    }

    [Fact]
    public void Quantize_AuthoredTie_SetsTiedToPreviousOnTheContinuation()
    {
        // A '_' tie: event A carries TiedToNext, so the next note's slot is TiedToPrevious (and not dotted).
        var events = new[] { RhythmEvent.Hit(0, 48) with { TiedToNext = true }, RhythmEvent.Hit(48, 48) };

        var slots = RhythmQuantizer.Quantize(events, TimeSignature.FourFour);

        Assert.False(slots[0].TiedToPrevious);
        Assert.True(slots[1].TiedToPrevious);
        Assert.False(slots[0].Dotted);
    }

    [Fact]
    public void Quantize_NonRepresentableNote_Throws()
    {
        // 120 ticks (2.5 beats) is not a single value — it must be tied; the quantizer refuses to guess.
        Assert.Throws<NotSupportedException>(
            () => RhythmQuantizer.Quantize(new[] { RhythmEvent.Hit(0, 120) }, TimeSignature.FourFour));
    }

    [Fact]
    public void Quantize_TieAcrossChordBoundary_HoldsTheTie_RhythmWins()
    {
        // Rhythm wins over harmony: a tie landing on a chord change is HELD (one tied slot, not re-attacked).
        // The renderer holds the previous voicing's strings; the chord change underneath is overridden.
        var events = new[] { RhythmEvent.Hit(0, 48) with { TiedToNext = true }, RhythmEvent.Hit(48, 48) };

        var slots = RhythmQuantizer.Quantize(events, TimeSignature.FourFour, new[] { 48 });

        Assert.Equal((4, false, false), S(slots[0]));
        Assert.Equal((4, false, true), S(slots[1]));   // held tie, not a re-attack
        Assert.Equal(2, slots.Count(s => !s.IsRest));
    }

    [Fact]
    public void Quantize_CrossBarTie_StartTied_MarksFirstNoteTied()
    {
        // The renderer passes startTied for a bar that opens with a leading '_' (a cross-bar anticipation).
        var events = new[] { RhythmEvent.Hit(0, 96) };

        var slots = RhythmQuantizer.Quantize(events, TimeSignature.FourFour, Array.Empty<int>(), startTied: true);

        Assert.True(slots[0].TiedToPrevious); // the bar's first note is tied into the previous bar
    }

    // ---- Triplet grid (IN7) -------------------------------------------------

    private static IReadOnlyList<RhythmEvent> Bar(string dsl) =>
        RhythmPatternParser.Parse("p", "P", dsl, TimeSignature.FourFour).Bars[0].Events;

    [Fact]
    public void Quantize_EighthTriplets_EmitsTwelveTupledEighths()
    {
        // ":3 XXX×4" = twelve eighth-triplets — every slot :8 tagged (3,2), none straight, none tied.
        var slots = RhythmQuantizer.Quantize(Bar(":3 XXX XXX XXX XXX"), TimeSignature.FourFour);

        Assert.Equal(12, slots.Count);
        Assert.All(slots, s =>
        {
            Assert.Equal(8, s.NoteValue);
            Assert.Equal(new Tuplet(3, 2), s.Tuplet);
            Assert.False(s.IsRest);
            Assert.False(s.TiedToPrevious);
        });
        Assert.Equal(Enumerable.Range(0, 12).Select(i => i * 16), slots.Select(s => s.StartTick));
    }

    [Fact]
    public void Quantize_SixteenthTriplets_EmitTupledSixteenths()
    {
        // ":6 XXXXXX×4" = twenty-four 16th-triplets — every slot :16 tagged (3,2).
        var slots = RhythmQuantizer.Quantize(Bar(":6 XXXXXX XXXXXX XXXXXX XXXXXX"), TimeSignature.FourFour);

        Assert.Equal(24, slots.Count);
        Assert.All(slots, s =>
        {
            Assert.Equal(16, s.NoteValue);
            Assert.Equal(new Tuplet(3, 2), s.Tuplet);
        });
    }

    [Fact]
    public void Quantize_StraightBeats_CarryNoTupletMarker()
    {
        var slots = RhythmQuantizer.Quantize(Bar("X...X...X...X..."), TimeSignature.FourFour);

        Assert.All(slots, s => Assert.Null(s.Tuplet));
    }

    [Fact]
    public void Quantize_SustainedTripletNote_RendersAsLargerTupletValue_NoTie()
    {
        // "X.X:3" on beat 1 = a 2-cell note (32t) then a 1-cell note (16t). The 32t note is a quarter
        // under (3,2) — a single slot, NOT two tied eighths — so the tie rule never fires.
        var slots = RhythmQuantizer.Quantize(Bar("X.X:3 X... X... X..."), TimeSignature.FourFour);

        Assert.Equal(4, slots[0].NoteValue);            // :4 = the 32-tick triplet note
        Assert.Equal(new Tuplet(3, 2), slots[0].Tuplet);
        Assert.False(slots[0].TiedToPrevious);
        Assert.Equal(8, slots[1].NoteValue);            // :8 = the final 16-tick triplet cell
        Assert.Equal(new Tuplet(3, 2), slots[1].Tuplet);
        Assert.Equal(0, slots[0].StartTick);
        Assert.Equal(32, slots[1].StartTick);
    }

    [Fact]
    public void Quantize_PerBeatMixedGrid_TagsOnlyTheTripletBeats()
    {
        // beat 1 triplet · beat 2 straight quarter · beat 3 triplet (sustain) · beat 4 straight quarter.
        var slots = RhythmQuantizer.Quantize(Bar("XXX:3 X... X.X:3 X..."), TimeSignature.FourFour);

        Assert.Equal(new int?[] { 3, 3, 3, null, 3, 3, null }, slots.Select(s => s.Tuplet?.Numerator));
        Assert.Equal(new[] { 8, 8, 8, 4, 4, 8, 4 }, slots.Select(s => s.NoteValue));
        Assert.Equal(new[] { 0, 16, 32, 48, 96, 128, 144 }, slots.Select(s => s.StartTick));
    }

    [Fact]
    public void Quantize_TripletBeatWithLeadingRest_TagsTheRestToo()
    {
        // "-XX:3" = a triplet rest cell then two attacks; the rest is tupled so it holds 1/3 of the beat.
        var slots = RhythmQuantizer.Quantize(Bar("-XX:3 X... X... X..."), TimeSignature.FourFour);

        Assert.True(slots[0].IsRest);
        Assert.Equal(8, slots[0].NoteValue);
        Assert.Equal(new Tuplet(3, 2), slots[0].Tuplet);
    }
}
