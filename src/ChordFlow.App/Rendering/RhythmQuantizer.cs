using ChordFlow.Domain;

namespace ChordFlow.Rendering;

/// <summary>
/// Compiles a tick-grid bar into a sequence of sequential note/rest <see cref="RhythmSlot"/>s that
/// alphaTex can consume (ctx IN12) — the new renderer responsibility that the rhythm migration buys.
/// Walks events in tick order, fills gaps with rests, splits spans at beat lines, and decomposes each
/// piece into representable note values. A note split across a beat line yields tied continuation
/// slots; rests are emitted as separate cells. Lives in the <c>Rendering/</c> seam so the domain stays
/// timing-only and <see cref="AlphaTexRenderer"/> remains the only code that formats the final tokens.
/// </summary>
public static class RhythmQuantizer
{
    // Representable note values, largest first: ticks -> alphaTex :N number.
    private static readonly (int Ticks, int NoteValue)[] DurationTable =
    {
        (TickGrid.Ppq * 4, 1),  // whole  = 192
        (TickGrid.Ppq * 2, 2),  // half   = 96
        (TickGrid.Ppq, 4),      // quarter= 48
        (TickGrid.Ppq / 2, 8),  // eighth = 24
        (TickGrid.Ppq / 4, 16), // 16th   = 12
    };

    /// <summary>Quantize one bar of <paramref name="events"/> in <paramref name="timeSignature"/>.</summary>
    public static IReadOnlyList<RhythmSlot> Quantize(
        IReadOnlyList<RhythmEvent> events, TimeSignature timeSignature) =>
        Quantize(events, timeSignature.BarTicks, timeSignature.BeatTicks);

    /// <summary>Quantize a pickup / leading measure (its own length, beat-line splitting at the quarter).</summary>
    public static IReadOnlyList<RhythmSlot> Quantize(PickupMeasure pickup)
    {
        ArgumentNullException.ThrowIfNull(pickup);
        return Quantize(pickup.Events, pickup.LengthTicks, TickGrid.Ppq);
    }

    /// <summary>
    /// Quantize <paramref name="events"/> spanning a measure of <paramref name="barTicks"/> ticks,
    /// splitting notes/rests at every <paramref name="beatTicks"/> grid line.
    /// </summary>
    public static IReadOnlyList<RhythmSlot> Quantize(
        IReadOnlyList<RhythmEvent> events, int barTicks, int beatTicks)
    {
        ArgumentNullException.ThrowIfNull(events);
        if (barTicks <= 0) throw new ArgumentOutOfRangeException(nameof(barTicks));
        if (beatTicks <= 0) throw new ArgumentOutOfRangeException(nameof(beatTicks));

        var slots = new List<RhythmSlot>();
        int cursor = 0;

        foreach (RhythmEvent e in events.OrderBy(e => e.Position))
        {
            if (e.Length <= 0)
            {
                throw new ArgumentException($"Rhythm event at {e.Position} has non-positive length {e.Length}.", nameof(events));
            }

            if (e.Position < cursor)
            {
                throw new ArgumentException($"Rhythm event at {e.Position} overlaps the previous event (ends at {cursor}).", nameof(events));
            }

            int end = e.Position + e.Length;
            if (end > barTicks)
            {
                throw new ArgumentException($"Rhythm event [{e.Position},{end}) exceeds the {barTicks}-tick bar.", nameof(events));
            }

            if (e.Position > cursor)
            {
                EmitSpan(slots, cursor, e.Position, isRest: true, beatTicks);
            }

            EmitSpan(slots, e.Position, end, isRest: false, beatTicks);
            cursor = end;
        }

        if (cursor < barTicks)
        {
            EmitSpan(slots, cursor, barTicks, isRest: true, beatTicks);
        }

        return slots;
    }

    // Emit one note/rest span [start,end), breaking at beat lines and decomposing each chunk into
    // representable note values. Continuation slots of a NOTE are tied; rests are never tied.
    private static void EmitSpan(List<RhythmSlot> slots, int start, int end, bool isRest, int beatTicks)
    {
        int p = start;
        bool firstSlotOfSpan = true;

        while (p < end)
        {
            int nextBeatLine = ((p / beatTicks) + 1) * beatTicks;
            int chunkEnd = Math.Min(nextBeatLine, end);

            int q = p;
            while (q < chunkEnd)
            {
                int remaining = chunkEnd - q;
                (int dTicks, int noteValue) = LargestFit(remaining);
                slots.Add(new RhythmSlot(noteValue, isRest, TiedToPrevious: !isRest && !firstSlotOfSpan));
                firstSlotOfSpan = false;
                q += dTicks;
            }

            p = chunkEnd;
        }
    }

    private static (int Ticks, int NoteValue) LargestFit(int remaining)
    {
        foreach ((int ticks, int noteValue) in DurationTable)
        {
            if (ticks <= remaining)
            {
                return (ticks, noteValue);
            }
        }

        throw new NotSupportedException(
            $"Cannot quantize a {remaining}-tick remainder to a representable note value at PPQ {TickGrid.Ppq} " +
            "(tuplets/32nds are out of v1 scope).");
    }
}
