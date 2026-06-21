namespace ChordFlow.Instruments.Guitar;

/// <summary>
/// Derives the <b>anchor finger</b> of a placed CAGED shape from the root's rank in the realized fret span —
/// the rule from the idea (req <c>IN2</c>): root is the lowest fret in the box → <see cref="Finger.Index"/> (hand
/// reaches up); root is the highest fret → <see cref="Finger.Pinky"/> (reaches down); root inside → a middle
/// finger. The C and G shapes carry the root on top of a cluster below, so they come out pinky-anchored; E, A, D
/// carry the root in the bass, so they come out index-anchored — and the minor/major third can shift the box's
/// extremes, which is what flips the margins. Pure geometry — no I/O, no UI.
/// </summary>
public static class AnchorFinger
{
    /// <summary>
    /// The finger anchoring the root at <paramref name="anchorFret"/> within the realized <b>fretted</b> box
    /// <c>[<paramref name="boxMinFret"/>, <paramref name="boxMaxFret"/>]</c> (open strings excluded — they need no
    /// finger). Lowest fret → index; interior → middle (low side) / ring (high side); highest fret → <b>ring</b> for a
    /// tight grip, <b>pinky</b> only when the grip spans the full 4-fret hand (chat-002): the C/G shapes carry the root
    /// on the pinky side, but a 3-fret box reaches it with the ring, a 4-fret box needs the pinky.
    /// </summary>
    public static Finger Derive(int anchorFret, int boxMinFret, int boxMaxFret)
    {
        if (boxMaxFret < boxMinFret) throw new ArgumentException("Box max fret is below its min fret.");
        if (anchorFret < boxMinFret || anchorFret > boxMaxFret)
            throw new ArgumentOutOfRangeException(nameof(anchorFret), "Anchor fret is outside the realized box.");

        if (anchorFret == boxMinFret) return Finger.Index;
        if (anchorFret == boxMaxFret)
        {
            int width = boxMaxFret - boxMinFret + 1; // fretted width (inclusive); width 4 = the full 4-finger hand
            return width >= 4 ? Finger.Pinky : Finger.Ring;
        }

        // Interior root: split the box at its midpoint — a root nearer the low edge takes the middle finger,
        // nearer the high edge the ring finger.
        double midpoint = (boxMinFret + boxMaxFret) / 2.0;
        return anchorFret <= midpoint ? Finger.Middle : Finger.Ring;
    }
}
