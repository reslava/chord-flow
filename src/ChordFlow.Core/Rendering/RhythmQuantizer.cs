using ChordFlow.Music.Rhythm;

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

    // Grid cell ticks at PPQ 48 used to classify a beat (see ClassifyBeats).
    private const int SixteenthTicks = TickGrid.Ppq / 4;          // 12 — straight 16th
    private const int EighthTripletTicks = TickGrid.Ppq / 3;      // 16 — eighth-triplet cell
    private const int SixteenthTripletTicks = TickGrid.Ppq / 6;   // 8  — 16th-triplet cell

    // A triplet packs 3 notes into the time of 2: a slot's straight note value spans length·3/2 ticks,
    // so we decompose against the straight DurationTable scaled by 3/2 and tag the result Tuplet(3,2).
    private static readonly Tuplet TripletMarker = new(3, 2);

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
        bool[] tripletBeats = ClassifyBeats(events, barTicks, beatTicks);

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
                EmitSpan(slots, cursor, e.Position, isRest: true, beatTicks, boundaries, tripletBeats);
            }

            EmitSpan(slots, e.Position, end, isRest: false, beatTicks, boundaries, tripletBeats);
            cursor = end;
        }

        if (cursor < barTicks)
        {
            EmitSpan(slots, cursor, barTicks, isRest: true, beatTicks, boundaries, tripletBeats);
        }

        return slots;
    }

    // Classify each beat as straight or triplet from the events' edge ticks (the subdivision is gone by
    // now — events carry only absolute ticks). A beat is TRIPLET when every interior edge falls on the
    // triplet grid (a multiple of EighthTripletTicks=16 or SixteenthTripletTicks=8) and at least one
    // edge is off the straight 16th grid (not a multiple of SixteenthTicks=12). A beat with no interior
    // edge — a sustained note or rest filling it — is straight (a plain quarter). Mixed/finer grids
    // (e.g. a 32nd at 6t) stay "straight" and surface later as a LargestFit failure (out of v1 scope).
    private static bool[] ClassifyBeats(IReadOnlyList<RhythmEvent> events, int barTicks, int beatTicks)
    {
        int beatCount = (barTicks + beatTicks - 1) / beatTicks;
        var triplet = new bool[beatCount];
        var anyOffStraight = new bool[beatCount];
        var allOnTriplet = new bool[beatCount];
        Array.Fill(allOnTriplet, true);

        foreach (RhythmEvent e in events)
        {
            foreach (int edge in stackalloc[] { e.Position, e.Position + e.Length })
            {
                int beat = edge / beatTicks;
                int rel = edge - (beat * beatTicks);
                if (beat >= beatCount || rel == 0)
                {
                    continue; // a beat line, not an interior edge
                }

                if (rel % SixteenthTicks != 0)
                {
                    anyOffStraight[beat] = true;
                }

                if (rel % SixteenthTripletTicks != 0)
                {
                    allOnTriplet[beat] = false;
                }
            }
        }

        for (int b = 0; b < beatCount; b++)
        {
            triplet[b] = anyOffStraight[b] && allOnTriplet[b];
        }

        return triplet;
    }

    // Emit one note/rest span [start,end), breaking at beat lines and decomposing each chunk into
    // representable note values. A NOTE additionally breaks at interior chord boundaries and re-attacks
    // there (not tied); other note continuation slots are tied. Rests are never tied and ignore chord
    // boundaries (a rest has no attack to re-trigger — the chord changes silently).
    private static void EmitSpan(
        List<RhythmSlot> slots, int start, int end, bool isRest, int beatTicks,
        HashSet<int>? boundaries, bool[] tripletBeats)
    {
        int p = start;
        bool firstSlotOfSpan = true;

        while (p < end)
        {
            int beat = p / beatTicks;
            bool triplet = tripletBeats[beat];

            // A rest, or any content on a triplet beat, is chunked one beat at a time: triplet content is
            // sub-beat, and rests split per beat and never tie. A straight NOTE instead extends across
            // beat lines so a beat-aligned ring coalesces into a single note value (a whole note across
            // the bar, a half note on beat 1/3) rather than tied quarters — stopping only at the span
            // end, the next triplet beat, or a chord boundary (which re-attacks).
            int chunkEnd;
            if (isRest || triplet)
            {
                chunkEnd = Math.Min((beat + 1) * beatTicks, end);
            }
            else
            {
                chunkEnd = end;
                for (int bt = beat + 1; bt * beatTicks < end; bt++)
                {
                    if (tripletBeats[bt])
                    {
                        chunkEnd = bt * beatTicks;
                        break;
                    }
                }
            }

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
                (int dTicks, int noteValue) = triplet
                    ? LargestFitTuplet(remaining)
                    : isRest ? LargestFit(remaining) : LargestAlignedFit(q, remaining);

                // Tied only when a note continuation can't stand alone — never on the span's first slot
                // and never at a chord boundary (those re-attack). Coalescing now makes beat-aligned rings
                // a single slot, so a tie survives only for genuinely syncopated/dotted spans (still
                // unsupported downstream — C4).
                bool tied = !isRest
                    && !firstSlotOfSpan
                    && (boundaries is null || !boundaries.Contains(q));

                slots.Add(new RhythmSlot(noteValue, isRest, tied, q, triplet ? TripletMarker : null));
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

    // Largest representable note value for a straight NOTE at bar-relative tick <paramref name="startTick"/>:
    // the value must both fit the remaining ticks AND be metrically aligned (startTick % its ticks == 0), so
    // a whole note only forms at the bar start, a half note only on beat 1/3, etc. This coalesces a
    // beat-aligned ring into one note instead of tied quarters; a non-aligned (syncopated) remainder falls to
    // a smaller value and the continuation ties (still unsupported downstream — C4). Straight content always
    // sits on the 12-tick grid, so the 16th always satisfies alignment and the loop terminates.
    private static (int Ticks, int NoteValue) LargestAlignedFit(int startTick, int remaining)
    {
        foreach ((int ticks, int noteValue) in DurationTable)
        {
            if (ticks <= remaining && startTick % ticks == 0)
            {
                return (ticks, noteValue);
            }
        }

        throw new NotSupportedException(
            $"Cannot quantize a {remaining}-tick note at tick {startTick} to an aligned note value at PPQ {TickGrid.Ppq}.");
    }

    // Largest representable note value for a triplet-grid span: scale the remaining ticks by 3/2 into
    // straight-duration space, pick the largest straight value that fits, and report the actual tick
    // advance (its straight ticks scaled back by 2/3). A triplet cell of 16t → :8 (24t straight); 32t →
    // :4 (48t straight); 8t → :16 (12t straight). A remainder needing a dotted value (e.g. 24t → 36t
    // straight) decomposes into multiple slots and ties — which still throw (C4), unchanged.
    private static (int Ticks, int NoteValue) LargestFitTuplet(int remaining)
    {
        int scaled = remaining * TripletMarker.Numerator / TripletMarker.Denominator;
        foreach ((int ticks, int noteValue) in DurationTable)
        {
            if (ticks <= scaled)
            {
                return (ticks * TripletMarker.Denominator / TripletMarker.Numerator, noteValue);
            }
        }

        throw new NotSupportedException(
            $"Cannot quantize a {remaining}-tick triplet remainder to a representable note value at PPQ {TickGrid.Ppq}.");
    }
}
