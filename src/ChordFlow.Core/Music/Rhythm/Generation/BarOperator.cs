namespace ChordFlow.Music.Rhythm.Generation;

/// <summary>
/// A bar-composition operator: decides, per beat, whether the beat sounds and which family block it gets —
/// <c>(family, beatIndex, beatsPerBar, rng) → Block</c> (design §3a). Operators build one <see cref="OnsetBar"/>
/// via <see cref="BuildBar"/>; a <see cref="SequenceBehaviour"/> chooses the operator (and its per-bar
/// parameters) across bars. A discriminated union of the six v1 operators.
/// </summary>
public abstract record BarOperator
{
    /// <summary>The block for beat <paramref name="beatIndex"/> of a <paramref name="beatsPerBar"/>-beat bar.</summary>
    public abstract Block Apply(RhythmFamily family, int beatIndex, int beatsPerBar, Random rng);

    /// <summary>Apply this operator across all beats to build one bar.</summary>
    public OnsetBar BuildBar(RhythmFamily family, int beatsPerBar, Random rng)
    {
        var beats = new Block[beatsPerBar];
        for (int b = 0; b < beatsPerBar; b++)
        {
            beats[b] = Apply(family, b, beatsPerBar, rng);
        }

        return new OnsetBar(beats);
    }

    /// <summary>The steady-pulse baseline — the family's primary block on every beat.</summary>
    public sealed record Uniform : BarOperator
    {
        public override Block Apply(RhythmFamily family, int beatIndex, int beatsPerBar, Random rng) =>
            family.Primary;
    }

    /// <summary>Exactly beat <see cref="Beat"/> sounds; every other beat is silent. The single-onset trainer.</summary>
    public sealed record Isolate(int Beat) : BarOperator
    {
        public override Block Apply(RhythmFamily family, int beatIndex, int beatsPerBar, Random rng) =>
            beatIndex == Beat ? family.Primary : family.Silence;
    }

    /// <summary>
    /// Beat 0 is fixed to the strong primary (the lighthouse); the remaining beats rotate through the
    /// family's block list. Keeps the "1" audible while beats 2–4 vary.
    /// </summary>
    public sealed record AnchorRotate : BarOperator
    {
        public override Block Apply(RhythmFamily family, int beatIndex, int beatsPerBar, Random rng) =>
            beatIndex == 0 ? family.Primary : family.Blocks[beatIndex % family.Blocks.Count];
    }

    /// <summary>Onsets only on the chosen <see cref="Beats"/> (e.g. <c>[1,3]</c> = the backbeat). Axis-A template.</summary>
    public sealed record Mask(IReadOnlyList<int> Beats) : BarOperator
    {
        public override Block Apply(RhythmFamily family, int beatIndex, int beatsPerBar, Random rng) =>
            Beats.Contains(beatIndex) ? family.Primary : family.Silence;
    }

    /// <summary>
    /// Every beat plays the primary displaced <see cref="Cells"/> cells later within the beat (wrapping at the
    /// subdivision). On the eighth family, <c>Displace(1)</c> turns the on-beat into the &amp; — the offbeat maker.
    /// </summary>
    public sealed record Displace(int Cells) : BarOperator
    {
        public override Block Apply(RhythmFamily family, int beatIndex, int beatsPerBar, Random rng)
        {
            int n = family.Subdivision;
            int[] shifted = family.Primary.Onsets
                .Select(k => ((k + Cells) % n + n) % n)
                .ToArray();
            return Block.Of(n, shifted);
        }
    }

    /// <summary>The first <see cref="Count"/> beats sound, the rest are silent — density as a dial (grow from beat 1).</summary>
    public sealed record Accumulate(int Count) : BarOperator
    {
        public override Block Apply(RhythmFamily family, int beatIndex, int beatsPerBar, Random rng) =>
            beatIndex < Count ? family.Primary : family.Silence;
    }

    /// <summary>Drop the last <see cref="Count"/> beats — the inverse of <see cref="Accumulate"/> (thin from the end).</summary>
    public sealed record Thin(int Count) : BarOperator
    {
        public override Block Apply(RhythmFamily family, int beatIndex, int beatsPerBar, Random rng) =>
            beatIndex < beatsPerBar - Count ? family.Primary : family.Silence;
    }
}
