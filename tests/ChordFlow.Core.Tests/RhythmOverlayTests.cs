using ChordFlow.Domain;
using Xunit;

namespace ChordFlow.Core.Tests;

public class RhythmOverlayTests
{
    // Two eighths per beat across a 4/4 bar: positions 0,24,48,72,96,120,144,168 (length 24 each).
    private static IReadOnlyList<RhythmEvent> EighthsBar() =>
        Enumerable.Range(0, 8).Select(i => RhythmEvent.Hit(i * 24, 24)).ToArray();

    [Fact]
    public void Feel_Straight_IsIdentity()
    {
        var events = EighthsBar();

        var warped = FeelTransform.Apply(events, Feel.Straight, TimeSignature.FourFour);

        Assert.Equal(events, warped);
    }

    [Fact]
    public void Feel_Swing_PushesOffBeatToTwoThirdsAndMakesLongShort()
    {
        var events = EighthsBar();

        var warped = FeelTransform.Apply(events, Feel.Swing, TimeSignature.FourFour);

        // Beat 1: on-beat eighth lengthens to 32 (long); off-beat moves to 32 and shortens to 16 (short).
        Assert.Equal(new RhythmEvent(0, 32, Stroke.Down, Accent.Normal), warped[0]);
        Assert.Equal(new RhythmEvent(32, 16, Stroke.Down, Accent.Normal), warped[1]);
        // Beat 2: same warp, offset by one beat (48).
        Assert.Equal(48, warped[2].Position);
        Assert.Equal(32, warped[2].Length);
        Assert.Equal(48 + 32, warped[3].Position);
        Assert.Equal(16, warped[3].Length);
    }

    [Fact]
    public void Feel_Shuffle_PushesOffBeatToThreeQuarters()
    {
        var events = EighthsBar();

        var warped = FeelTransform.Apply(events, Feel.Shuffle, TimeSignature.FourFour);

        Assert.Equal(36, warped[0].Length);      // on-beat lengthens to 3/4 of 48
        Assert.Equal(36, warped[1].Position);    // off-beat at 36
        Assert.Equal(12, warped[1].Length);      // ...shortened to a sixteenth
    }

    [Fact]
    public void Feel_DoesNotAffectQuarterGridPatterns()
    {
        // The MVP quarter patterns have no off-beat events, so swing is a no-op on them.
        var warped = FeelTransform.Apply(SeedData.Quarters.Bars[0].Events, Feel.Swing, TimeSignature.FourFour);

        Assert.Equal(SeedData.Quarters.Bars[0].Events, warped);
    }

    [Fact]
    public void AccentPattern_Backbeat_AccentsBeats2And4()
    {
        var accented = AccentPattern.Backbeat.Apply(SeedData.Quarters.Bars[0].Events, TimeSignature.FourFour);

        Assert.Equal(Accent.Normal, accented[0].Accent);   // beat 1
        Assert.Equal(Accent.Accented, accented[1].Accent); // beat 2
        Assert.Equal(Accent.Normal, accented[2].Accent);   // beat 3
        Assert.Equal(Accent.Accented, accented[3].Accent); // beat 4
    }

    [Fact]
    public void StrokeOverlay_AlternateDownUp_AssignsByOrder()
    {
        var stroked = StrokeOverlay.AlternateDownUp(SeedData.Quarters.Bars[0].Events);

        Assert.Equal(new[] { Stroke.Down, Stroke.Up, Stroke.Down, Stroke.Up }, stroked.Select(e => e.Stroke));
    }

    [Fact]
    public void Compose_AccentThenFeel_YieldsExpectedTimingAndAccentsWithoutMutatingBase()
    {
        var basePattern = SeedData.Beat1And3; // hits on beats 1 & 3 (quarters)
        var originalEvents = basePattern.Bars[0].Events.ToArray(); // snapshot for the no-mutation check

        // Compose overlays: accent the backbeat, then apply swing feel.
        var accented = AccentPattern.Backbeat.Apply(basePattern.Bars[0].Events, basePattern.TimeSignature);
        var composed = FeelTransform.Apply(accented, Feel.Swing, basePattern.TimeSignature);

        // Beat 1 hit (offset 0, quarter) and beat 3 hit (offset 0, quarter) — neither is an off-beat
        // eighth, so timing is unchanged; neither is on beat 2/4, so accents stay Normal.
        Assert.Equal(0, composed[0].Position);
        Assert.Equal(96, composed[1].Position);
        Assert.All(composed, e => Assert.Equal(Accent.Normal, e.Accent));

        // The base pattern's events were not mutated by either overlay.
        Assert.Equal(originalEvents, basePattern.Bars[0].Events);
        Assert.NotSame(basePattern.Bars[0].Events, composed);
    }

    [Fact]
    public void Compose_OnEighths_AccentSurvivesFeelWarp()
    {
        var events = EighthsBar();

        // Accent every downbeat (beats 1-4), then swing.
        var accentAll = new AccentPattern(new[] { 0, 1, 2, 3 }).Apply(events, TimeSignature.FourFour);
        var composed = FeelTransform.Apply(accentAll, Feel.Swing, TimeSignature.FourFour);

        // Both eighths share beat 0, so both carry the accent; the feel warp preserves the accents
        // while applying the long-short timing (on-beat lengthens, off-beat shifts).
        Assert.Equal(Accent.Accented, composed[0].Accent);
        Assert.Equal(32, composed[0].Length);
        Assert.Equal(Accent.Accented, composed[1].Accent);
        Assert.Equal(32, composed[1].Position);
    }
}
