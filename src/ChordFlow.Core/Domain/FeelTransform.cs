namespace ChordFlow.Domain;

/// <summary>
/// Applies a <see cref="Feel"/> to a straight bar of <see cref="RhythmEvent"/>s as a playback-time
/// warp, returning a <b>new</b> event list (the base pattern is never mutated — ctx C4). Straight is
/// the identity; swing/shuffle/triplet push the off-beat eighth (the "and" at the half-beat) later so
/// the on-beat eighth lengthens and the off-beat shortens — the long-short groove.
/// </summary>
public static class FeelTransform
{
    /// <summary>The fraction of a beat at which the off-beat eighth lands for <paramref name="feel"/>.</summary>
    public static double OffBeatRatio(Feel feel) => feel switch
    {
        Feel.Straight => 1.0 / 2,
        Feel.Swing => 2.0 / 3,
        Feel.Shuffle => 3.0 / 4,
        Feel.Triplet => 2.0 / 3,
        _ => 1.0 / 2,
    };

    /// <summary>Warp <paramref name="events"/> by <paramref name="feel"/> within <paramref name="timeSignature"/>.</summary>
    public static IReadOnlyList<RhythmEvent> Apply(
        IReadOnlyList<RhythmEvent> events, Feel feel, TimeSignature timeSignature)
    {
        ArgumentNullException.ThrowIfNull(events);

        int beat = timeSignature.BeatTicks;
        int halfBeat = beat / 2;
        int swingPoint = (int)Math.Round(OffBeatRatio(feel) * beat);

        var result = new RhythmEvent[events.Count];
        for (int i = 0; i < events.Count; i++)
        {
            RhythmEvent e = events[i];
            int offset = e.Position % beat;

            if (offset == halfBeat)
            {
                // The off-beat "and" moves later; it keeps ending where it did, so it shortens.
                int newPosition = (e.Position - offset) + swingPoint;
                int newLength = Math.Max(1, (e.Position + e.Length) - newPosition);
                result[i] = e with { Position = newPosition, Length = newLength };
            }
            else if (offset == 0 && e.Length == halfBeat)
            {
                // The on-beat eighth lengthens up to the (moved) off-beat — the "long" of long-short.
                result[i] = e with { Length = swingPoint };
            }
            else
            {
                result[i] = e;
            }
        }

        return result;
    }
}
