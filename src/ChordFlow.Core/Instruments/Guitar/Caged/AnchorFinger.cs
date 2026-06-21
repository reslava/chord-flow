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
    /// The finger anchoring the root at <paramref name="anchorFret"/> within the realized box
    /// <c>[<paramref name="boxMinFret"/>, <paramref name="boxMaxFret"/>]</c>. Lowest fret → index, highest → pinky,
    /// interior → middle (nearer the low side) or ring (nearer the high side).
    /// </summary>
    public static Finger Derive(int anchorFret, int boxMinFret, int boxMaxFret)
    {
        if (boxMaxFret < boxMinFret) throw new ArgumentException("Box max fret is below its min fret.");
        if (anchorFret < boxMinFret || anchorFret > boxMaxFret)
            throw new ArgumentOutOfRangeException(nameof(anchorFret), "Anchor fret is outside the realized box.");

        if (anchorFret == boxMinFret) return Finger.Index;
        if (anchorFret == boxMaxFret) return Finger.Pinky;

        // Interior root: split the box at its midpoint — a root nearer the low edge takes the middle finger,
        // nearer the high edge the ring finger.
        double midpoint = (boxMinFret + boxMaxFret) / 2.0;
        return anchorFret <= midpoint ? Finger.Middle : Finger.Ring;
    }
}
