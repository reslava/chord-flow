namespace ChordFlow.Instruments.Guitar;

/// <summary>
/// A left-hand finger, ordered by its natural fret position (lower fret = lower value). The CAGED engine
/// derives which finger <b>anchors</b> a shape's root from the root's rank in the placed span (see
/// <c>AnchorFinger</c>), then reads this finger's reach to bound the box.
/// </summary>
public enum Finger
{
    Index = 1,
    Middle = 2,
    Ring = 3,
    Pinky = 4,
}

/// <summary>A fret window <c>[<see cref="MinFret"/>, <see cref="MaxFret"/>]</c> — the span a box may occupy.</summary>
public readonly record struct FretWindow(int MinFret, int MaxFret);

/// <summary>
/// The single global <b>anchor-relative reach table</b> — the one piece of non-theory, physical data the CAGED
/// engine adds on top of the locked substrates (req <c>IN3</c>/<c>C4</c>). It is <b>not</b> a flat fret cap and is
/// <b>never</b> per-shape: each finger has a (behind, ahead) reach in frets, and the box envelope is the shape's
/// octave zone extended by the <i>anchor</i> finger's reach. A finger that anchors low reaches up (index 1/3); one
/// that anchors high reaches down (pinky 4/0) — which is exactly what admits the stretchy pinky-anchored C/G shapes
/// instead of pruning them. The numbers are seeded from hand ergonomics and are the only thing the frets golden
/// oracle (<c>IN6</c>) calibrates — and it calibrates them with a <b>single global edit</b>, never a per-shape one.
/// Pure data — no I/O, no UI.
/// </summary>
public static class HandReach
{
    /// <summary>How far a finger stretches <see cref="Behind"/> (toward the nut / lower frets) and
    /// <see cref="Ahead"/> (toward the bridge / higher frets) from its anchored fret.</summary>
    public readonly record struct Reach(int Behind, int Ahead);

    // The one global, ergonomic, oracle-calibrated table (Rafa's values, chat-001 2026-06-20). Ring is a
    // placeholder until a shape actually anchors on it.
    private static readonly IReadOnlyDictionary<Finger, Reach> Table =
        new Dictionary<Finger, Reach>
        {
            [Finger.Index] = new Reach(Behind: 1, Ahead: 3),
            [Finger.Middle] = new Reach(Behind: 1, Ahead: 1),
            [Finger.Ring] = new Reach(Behind: 1, Ahead: 1),   // placeholder — no shape anchors on the ring yet
            [Finger.Pinky] = new Reach(Behind: 4, Ahead: 0),
        };

    /// <summary>The (behind, ahead) reach of <paramref name="finger"/>.</summary>
    public static Reach Of(Finger finger)
    {
        if (!Table.TryGetValue(finger, out Reach reach))
        {
            throw new ArgumentOutOfRangeException(nameof(finger), finger, "No reach is defined for this finger.");
        }

        return reach;
    }

    /// <summary>
    /// The <b>CAGED-zone envelope</b>: the shape's octave <paramref name="zone"/> extended by the
    /// <paramref name="anchor"/> finger's reach — <c>[zone.Min − behind, zone.Max + ahead]</c>, with the low edge
    /// clamped to fret 0. This is the outer bound on where a box's tones may land for a <i>known</i> anchor finger;
    /// the <i>used</i> zone (the actual span a quality occupies) is minimized inside it during candidate selection.
    /// </summary>
    public static FretWindow Envelope(Finger anchor, OctaveZone zone)
    {
        Reach reach = Of(anchor);
        int min = Math.Max(0, zone.MinFret - reach.Behind);
        int max = zone.MaxFret + reach.Ahead;
        return new FretWindow(min, max);
    }
}
