namespace ChordFlow.Music.Rhythm.Generation;

/// <summary>
/// A sequence behaviour: decides, per bar, how the base <see cref="BarOperator"/> evolves across the 1–4
/// bars — <c>(barIndex, baseOperator, family) → OnsetBar</c> (design §3a). A discriminated union of the five
/// v1 behaviours. (Random-in-family and Ramp are a later phase — req EX8.)
/// </summary>
public abstract record SequenceBehaviour
{
    /// <summary>The bar at <paramref name="barIndex"/> for this behaviour.</summary>
    public abstract OnsetBar BarAt(
        int barIndex, BarOperator baseOperator, RhythmFamily family, int beatsPerBar, Random rng);

    /// <summary>Identical every bar — the base operator's bar, repeated. Internalize before varying.</summary>
    public sealed record Repeat : SequenceBehaviour
    {
        public override OnsetBar BarAt(
            int barIndex, BarOperator baseOperator, RhythmFamily family, int beatsPerBar, Random rng) =>
            baseOperator.BuildBar(family, beatsPerBar, rng);
    }

    /// <summary>
    /// Bar <c>N</c> plays the family's <c>N</c>-th block (mod list length) on every beat — a guided tour of
    /// one family. Ignores the base operator (the family list is the tour).
    /// </summary>
    public sealed record Cycle : SequenceBehaviour
    {
        public override OnsetBar BarAt(
            int barIndex, BarOperator baseOperator, RhythmFamily family, int beatsPerBar, Random rng)
        {
            Block block = family.Blocks[barIndex % family.Blocks.Count];
            var beats = new Block[beatsPerBar];
            Array.Fill(beats, block);
            return new OnsetBar(beats);
        }
    }

    /// <summary>
    /// Bind the base operator's parameter to <c>barIndex</c> so the same shape is felt against every metric
    /// position — the signature drill. Sweeps <see cref="BarOperator.Isolate"/>'s beat (1→2→3→4) or
    /// <see cref="BarOperator.Displace"/>'s cells (through the beat's subdivisions); any other operator
    /// falls back to repeating it.
    /// </summary>
    public sealed record Sweep : SequenceBehaviour
    {
        public override OnsetBar BarAt(
            int barIndex, BarOperator baseOperator, RhythmFamily family, int beatsPerBar, Random rng)
        {
            BarOperator swept = baseOperator switch
            {
                BarOperator.Isolate => new BarOperator.Isolate(barIndex % beatsPerBar),
                BarOperator.Displace => new BarOperator.Displace(barIndex % family.Subdivision),
                _ => baseOperator,
            };
            return swept.BuildBar(family, beatsPerBar, rng);
        }
    }

    /// <summary>
    /// Insert silent bars between content bars: within each cycle of
    /// <see cref="ContentBars"/> + <see cref="RestBars"/> bars, the first are the base operator's content and
    /// the rest are empty — hold time through silence.
    /// </summary>
    public sealed record RestBar(int ContentBars = 1, int RestBars = 1) : SequenceBehaviour
    {
        public override OnsetBar BarAt(
            int barIndex, BarOperator baseOperator, RhythmFamily family, int beatsPerBar, Random rng)
        {
            int period = ContentBars + RestBars;
            bool isContent = barIndex % period < ContentBars;
            return isContent
                ? baseOperator.BuildBar(family, beatsPerBar, rng)
                : OnsetBar.Rest(beatsPerBar);
        }
    }

    /// <summary>A content bar (the call), then an empty "your turn" bar (the response), alternating.</summary>
    public sealed record CallResponse : SequenceBehaviour
    {
        public override OnsetBar BarAt(
            int barIndex, BarOperator baseOperator, RhythmFamily family, int beatsPerBar, Random rng) =>
            barIndex % 2 == 0
                ? baseOperator.BuildBar(family, beatsPerBar, rng)
                : OnsetBar.Rest(beatsPerBar);
    }
}
