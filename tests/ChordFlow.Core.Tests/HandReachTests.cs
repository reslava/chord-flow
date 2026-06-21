using ChordFlow.Instruments.Guitar;
using Xunit;

namespace ChordFlow.Core.Tests;

public class HandReachTests
{
    [Theory]
    [InlineData(Finger.Index, 1, 3)]
    [InlineData(Finger.Middle, 1, 1)]
    [InlineData(Finger.Ring, 1, 1)]
    [InlineData(Finger.Pinky, 4, 0)]
    public void Of_ReturnsTheGlobalReachTable(Finger finger, int behind, int ahead)
    {
        HandReach.Reach reach = HandReach.Of(finger);
        Assert.Equal(behind, reach.Behind);
        Assert.Equal(ahead, reach.Ahead);
    }

    [Fact]
    public void Envelope_ExtendsTheZone_ByTheAnchorFingerReach()
    {
        // E-major zone is [8,10]; index-anchored (root lowest) reaches 1 behind, 3 ahead -> [7,13].
        FretWindow window = HandReach.Envelope(Finger.Index, new OctaveZone(8, 10));
        Assert.Equal(new FretWindow(7, 13), window);
    }

    [Fact]
    public void Envelope_PinkyAnchor_ReachesFarBehind_NotAhead()
    {
        // Pinky (4/0) is what admits the stretchy C/G shapes: reach down, not up.
        FretWindow window = HandReach.Envelope(Finger.Pinky, new OctaveZone(5, 8));
        Assert.Equal(new FretWindow(1, 8), window);
    }

    [Fact]
    public void Envelope_ClampsLowEdgeToZero()
    {
        // C-major zone [1,3], pinky-anchored: 1 - 4 = -3 clamps to 0.
        FretWindow window = HandReach.Envelope(Finger.Pinky, new OctaveZone(1, 3));
        Assert.Equal(new FretWindow(0, 3), window);
    }
}
