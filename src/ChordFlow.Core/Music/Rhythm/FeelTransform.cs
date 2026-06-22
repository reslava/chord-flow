namespace ChordFlow.Music.Rhythm;

/// <summary>
/// Applies a <see cref="TripletFeel"/> to a straight bar of <see cref="RhythmEvent"/>s as a playback-time
/// warp, returning a <b>new</b> event list (the base pattern is never mutated — ctx C4). <see cref="TripletFeel.None"/>
/// is the identity; the swung feels push the off-beat eighth (the "and" at the half-beat) later so the
/// on-beat eighth lengthens and the off-beat shortens — the long-short groove.
/// <para>
/// <b>Not used by the alphaTex render path</b> — swing is delegated to alphaTab's native <c>\tf</c>
/// directive there. This self-computed warp is retained for the <see cref="Rendering.IScoreRenderer"/>
/// export seam (a future MIDI / GuitarPro exporter has no alphaTab to swing playback and must bake the
/// groove into ticks itself).
/// </para>
/// </summary>
public static class FeelTransform
{
    /// <summary>The fraction of a beat at which the off-beat eighth lands for <paramref name="feel"/>.</summary>
    public static double OffBeatRatio(TripletFeel feel) => feel switch
    {
        TripletFeel.None => 1.0 / 2,
        TripletFeel.Triplet8th => 2.0 / 3,
        TripletFeel.Triplet16th => 2.0 / 3,
        TripletFeel.Dotted8th => 3.0 / 4,
        TripletFeel.Dotted16th => 3.0 / 4,
        TripletFeel.Scottish8th => 1.0 / 3,
        TripletFeel.Scottish16th => 1.0 / 3,
        _ => 1.0 / 2,
    };

    /// <summary>Warp <paramref name="events"/> by <paramref name="feel"/> within <paramref name="timeSignature"/>.</summary>
    public static IReadOnlyList<RhythmEvent> Apply(
        IReadOnlyList<RhythmEvent> events, TripletFeel feel, TimeSignature timeSignature)
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
