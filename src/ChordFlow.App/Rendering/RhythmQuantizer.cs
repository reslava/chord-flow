using ChordFlow.Domain;

namespace ChordFlow.Rendering;

/// <summary>
/// Compiles a tick-grid bar into a sequence of sequential note/rest <see cref="RhythmSlot"/>s that
/// alphaTex can consume (ctx IN12) — the new renderer responsibility that the rhythm migration buys.
/// Walks events in tick order, fills gaps with rests, splits spans at beat lines (and, for the
/// harmonic-rhythm layer, at <c>ChordSpan</c> boundaries), and decomposes each piece into representable
/// note values. A note split across a beat line yields tied continuation slots; a note split across a
/// <em>chord boundary</em> re-attacks (you cannot tie one chord into a different chord); rests are
/// emitted as separate cells and never tied. Lives in the <c>Rendering/</c> seam so the domain stays
/// timing-only and <see cref="AlphaTexRenderer"/> remains the only code that formats the final tokens.
/// </summary>
public static class RhythmQuantizer
{
    private static readonly IReadOnlyList<int> NoBoundaries = Array.Empty<int>();

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
        Quantize(events, timeSignature.BarTicks, timeSignature.BeatTicks, NoBoundaries);

    /// <summary>
    /// Quantize one bar of <paramref name="events"/> in <paramref name="timeSignature"/>, re-attacking
    /// notes at each interior <c>ChordSpan</c> boundary in <paramref name="chordBoundaries"/> (bar-relative
    /// ticks, exclusive of 0 and the bar end).
    /// </summary>
    public static IReadOnlyList<RhythmSlot> Quantize(
        IReadOnlyList<RhythmEvent> events, TimeSignature timeSignature, IReadOnlyList<int> chordBoundaries) =>
        Quantize(events, timeSignature.BarTicks, timeSignature.BeatTicks, chordBoundaries);

    /// <summary>Quantize a pickup / leading measure (its own length, beat-line splitting at the quarter).</summary>
    public static IReadOnlyList<RhythmSlot> Quantize(PickupMeasure pickup)
    {
        ArgumentNullException.ThrowIfNull(pickup);
        return Quantize(pickup.Events, pickup.LengthTicks, TickGrid.Ppq, NoBoundaries);
    }

    /// <summary>
    /// Quantize <paramref name="events"/> spanning a measure of <paramref name="barTicks"/> ticks,
    /// splitting notes/rests at every <paramref name="beatTicks"/> grid line and re-attacking notes at
    /// each interior chord boundary in <paramref name="chordBoundaries"/>.
    /// </summary>
    public static IReadOnlyList<RhythmSlot> Quantize(
        IReadOnlyList<RhythmEvent> events, int barTicks, int beatTicks, IReadOnlyList<int> chordBoundaries)
    {
        ArgumentNullException.ThrowIfNull(events);
        ArgumentNullException.ThrowIfNull(chordBoundaries);
        if (barTicks <= 0) throw new ArgumentOutOfRangeException(nameof(barTicks));
        if (beatTicks <= 0) throw new ArgumentOutOfRangeException(nameof(beatTicks));

        var boundaries = chordBoundaries.Count == 0 ? null : new HashSet<int>(chordBoundaries);

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
                EmitSpan(slots, cursor, e.Position, isRest: true, beatTicks, boundaries);
            }

            EmitSpan(slots, e.Position, end, isRest: false, beatTicks, boundaries);
            cursor = end;
        }

        if (cursor < barTicks)
        {
            EmitSpan(slots, cursor, barTicks, isRest: true, beatTicks, boundaries);
        }

        return slots;
    }

    // Emit one note/rest span [start,end), breaking at beat lines and decomposing each chunk into
    // representable note values. A NOTE additionally breaks at interior chord boundaries and re-attacks
    // there (not tied); other note continuation slots are tied. Rests are never tied and ignore chord
    // boundaries (a rest has no attack to re-trigger — the chord changes silently).
    private static void EmitSpan(
        List<RhythmSlot> slots, int start, int end, bool isRest, int beatTicks, HashSet<int>? boundaries)
    {
        int p = start;
        bool firstSlotOfSpan = true;

        while (p < end)
        {
            int chunkEnd = Math.Min(((p / beatTicks) + 1) * beatTicks, end);

            // For a note, stop at the nearest interior chord boundary so the next slot re-attacks.
            if (!isRest && boundaries is not null)
            {
                foreach (int b in boundaries)
                {
                    if (b > p && b < chunkEnd)
                    {
                        chunkEnd = b;
                    }
                }
            }

            int q = p;
            while (q < chunkEnd)
            {
                int remaining = chunkEnd - q;
                (int dTicks, int noteValue) = LargestFit(remaining);

                // Tied only when this is a note continuation split by a beat line — never on the span's
                // first slot and never at a chord boundary (those re-attack).
                bool tied = !isRest
                    && !firstSlotOfSpan
                    && (boundaries is null || !boundaries.Contains(q));

                slots.Add(new RhythmSlot(noteValue, isRest, tied, q));
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
