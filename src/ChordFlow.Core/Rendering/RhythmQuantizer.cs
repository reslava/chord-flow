using ChordFlow.Music.Rhythm;

namespace ChordFlow.Rendering;

/// <summary>
/// Compiles a tick-grid bar into a sequence of sequential note/rest <see cref="RhythmSlot"/>s that
/// alphaTex can consume — the renderer responsibility the rhythm migration buys. Walks events in tick
/// order and fills gaps with rests. Under the accurate-notation grammar a <b>note event maps to exactly
/// one slot</b>: its duration must be a single notatable value — a base value (whole/half/quarter/8th/16th)
/// or a single-dotted value (1.5×), straight or on a triplet grid — otherwise it throws (the author must
/// tie it with <c>_</c>). Ties are <b>authored</b>, not inferred: an event's <see cref="RhythmEvent.TiedToNext"/>
/// becomes <see cref="RhythmSlot.TiedToPrevious"/> on the following note. A note split across a
/// <em>chord boundary</em> re-attacks (you cannot tie one chord into a different chord), and an authored
/// tie that lands on a chord boundary is rejected. Rests decompose into representable values and are
/// never tied. Lives in the <c>Rendering/</c> seam so the domain stays timing-only and
/// <see cref="AlphaTexRenderer"/> remains the only code that formats the final tokens.
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
        IReadOnlyList<RhythmEvent> events, TimeSignature timeSignature, IReadOnlyList<int> chordBoundaries,
        bool startTied = false) =>
        Quantize(events, timeSignature.BarTicks, timeSignature.BeatTicks, chordBoundaries, startTied);

    /// <summary>Quantize a pickup / leading measure (its own length, beat-line splitting at the quarter).</summary>
    public static IReadOnlyList<RhythmSlot> Quantize(PickupMeasure pickup)
    {
        ArgumentNullException.ThrowIfNull(pickup);
        return Quantize(pickup.Events, pickup.LengthTicks, TickGrid.Ppq, NoBoundaries);
    }

    /// <summary>
    /// Quantize <paramref name="events"/> spanning a measure of <paramref name="barTicks"/> ticks,
    /// re-attacking notes at each interior chord boundary in <paramref name="chordBoundaries"/>.
    /// <paramref name="startTied"/> marks the bar's first note as tied into the previous bar (a leading
    /// <c>_</c> cross-bar tie).
    /// </summary>
    public static IReadOnlyList<RhythmSlot> Quantize(
        IReadOnlyList<RhythmEvent> events, int barTicks, int beatTicks, IReadOnlyList<int> chordBoundaries,
        bool startTied = false)
    {
        ArgumentNullException.ThrowIfNull(events);
        ArgumentNullException.ThrowIfNull(chordBoundaries);
        if (barTicks <= 0) throw new ArgumentOutOfRangeException(nameof(barTicks));
        if (beatTicks <= 0) throw new ArgumentOutOfRangeException(nameof(beatTicks));

        var boundaries = chordBoundaries.Count == 0 ? null : new HashSet<int>(chordBoundaries);
        bool[] tripletBeats = ClassifyBeats(events, barTicks, beatTicks);

        var slots = new List<RhythmSlot>();
        int cursor = 0;
        bool prevTiedToNext = startTied; // the previous note (this bar's, or the previous bar's) ties into this one

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
                EmitRestSpan(slots, cursor, e.Position, beatTicks, tripletBeats);
            }

            EmitNoteSpan(slots, e.Position, end, prevTiedToNext, beatTicks, boundaries, tripletBeats);
            prevTiedToNext = e.TiedToNext;
            cursor = end;
        }

        if (cursor < barTicks)
        {
            EmitRestSpan(slots, cursor, barTicks, beatTicks, tripletBeats);
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

    // Emit a NOTE span [start,end). A TIED note (the '_' continuation) wins over harmony: it is one HELD
    // slot that ignores chord boundaries (the renderer re-states the previously-sounding strings, so the
    // chord change underneath is silently overridden). A non-tied note splits at interior chord boundaries,
    // RE-ATTACKING each piece under its chord. Every piece must be exactly one base or dotted value (straight
    // or triplet); otherwise SingleValue throws and the author must tie with '_'.
    private static void EmitNoteSpan(
        List<RhythmSlot> slots, int start, int end, bool tiedToPrevious, int beatTicks,
        HashSet<int>? boundaries, bool[] tripletBeats)
    {
        if (tiedToPrevious)
        {
            bool tripletHeld = tripletBeats[start / beatTicks];
            (int heldValue, bool heldDotted) = SingleValue(end - start, tripletHeld);
            slots.Add(new RhythmSlot(
                heldValue, IsRest: false, TiedToPrevious: true, start, tripletHeld ? TripletMarker : null, heldDotted));
            return;
        }

        int p = start;
        while (p < end)
        {
            // Stop at the nearest interior chord boundary so the next piece re-attacks under its chord.
            int pieceEnd = end;
            if (boundaries is not null)
            {
                foreach (int b in boundaries)
                {
                    if (b > p && b < pieceEnd)
                    {
                        pieceEnd = b;
                    }
                }
            }

            bool triplet = tripletBeats[p / beatTicks];
            (int noteValue, bool dotted) = SingleValue(pieceEnd - p, triplet);
            slots.Add(new RhythmSlot(
                noteValue, IsRest: false, TiedToPrevious: false, p, triplet ? TripletMarker : null, dotted));

            p = pieceEnd;
        }
    }

    // Emit a REST span [start,end). Straight rests coalesce into the largest metrically-ALIGNED value (so
    // a rest over beats 3-4 is one ":2 r", not two ":4 r"), the alignment rule keeping the bar's beat
    // structure visible. Triplet beats stay chunked per beat with the tuplet marker. Rests are never tied
    // and ignore chord boundaries (silence has no attack to re-trigger).
    private static void EmitRestSpan(
        List<RhythmSlot> slots, int start, int end, int beatTicks, bool[] tripletBeats)
    {
        int p = start;
        while (p < end)
        {
            int beat = p / beatTicks;

            if (tripletBeats[beat])
            {
                int chunkEnd = Math.Min((beat + 1) * beatTicks, end);
                int q = p;
                while (q < chunkEnd)
                {
                    (int dTicks, int noteValue) = LargestFitTuplet(chunkEnd - q);
                    slots.Add(new RhythmSlot(noteValue, IsRest: true, TiedToPrevious: false, q, TripletMarker));
                    q += dTicks;
                }

                p = chunkEnd;
            }
            else
            {
                // The largest value that fits AND is aligned to the onset tick — bounded so it never crosses
                // into a triplet beat. Straight content sits on the 12-tick grid, so the 16th always aligns.
                int limit = end;
                for (int bt = beat + 1; bt * beatTicks < end; bt++)
                {
                    if (tripletBeats[bt])
                    {
                        limit = bt * beatTicks;
                        break;
                    }
                }

                (int dTicks, int noteValue) = LargestAlignedFit(p, limit - p);
                slots.Add(new RhythmSlot(noteValue, IsRest: true, TiedToPrevious: false, p));
                p += dTicks;
            }
        }
    }

    // The single notatable value of a note span of <paramref name="length"/> ticks: a base value, or a
    // single-dotted value (1.5×). On a triplet beat the length is scaled 3/2 into straight space first
    // (and the slot carries the tuplet marker). Anything else is not one value — the author must tie it.
    private static (int NoteValue, bool Dotted) SingleValue(int length, bool triplet)
    {
        int straight = triplet ? length * TripletMarker.Numerator / TripletMarker.Denominator : length;

        foreach ((int ticks, int noteValue) in DurationTable)
        {
            if (straight == ticks)
            {
                return (noteValue, false);
            }
        }

        foreach ((int ticks, int noteValue) in DurationTable)
        {
            if (straight == ticks * 3 / 2) // a single augmentation dot = base + half
            {
                return (noteValue, true);
            }
        }

        throw new NotSupportedException(
            $"A {length}-tick note is not a single notatable value at PPQ {TickGrid.Ppq} — tie it with '_' " +
            "(or change the cells); dotted values up to a single dot are supported.");
    }

    // The largest representable value that fits the remaining ticks AND is aligned to the onset tick
    // (startTick % its ticks == 0), so a half rest only forms at an aligned position — this both coalesces
    // adjacent rests and keeps the bar's beat structure readable. The 16th always aligns on the 12-grid.
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
            $"Cannot align a {remaining}-tick rest at tick {startTick} at PPQ {TickGrid.Ppq}.");
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
