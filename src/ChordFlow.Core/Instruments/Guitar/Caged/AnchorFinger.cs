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
    /// <para>When <paramref name="indexOnStretchBack"/> is set, the index is committed to the box's low edge (the
    /// E-shape behind-1 stretch-back fret), so the fingers count up one-per-fret from it — a root one fret above the
    /// stretch lands on the <b>middle</b> finger, not the ring (chat-001 review: E-shape 1-behind grips anchor m).</para>
    /// </summary>
    public static Finger Derive(int anchorFret, int boxMinFret, int boxMaxFret, bool indexOnStretchBack = false)
    {
        if (boxMaxFret < boxMinFret) throw new ArgumentException("Box max fret is below its min fret.");
        if (anchorFret > boxMaxFret)
            throw new ArgumentOutOfRangeException(nameof(anchorFret), "Anchor fret is above the realized box.");

        // Index pinned to the stretch-back fret at the low edge: every other finger steps up one fret from it.
        if (indexOnStretchBack)
        {
            return (anchorFret - boxMinFret) switch
            {
                <= 0 => Finger.Index,
                1 => Finger.Middle,
                2 => Finger.Ring,
                _ => Finger.Pinky,
            };
        }

        // Root at or below the box's low edge — including an OPEN root (fret 0) that the fretted box excludes
        // (e.g. open D7 "x x 0 2 1 2") — anchors the hand at the low end: index / open position. The fretted
        // box is built from fretted notes only, so an open root sits below boxMin; that is the open-position
        // case, not an error (caged-derive-anchor-edge).
        if (anchorFret <= boxMinFret) return Finger.Index;
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
