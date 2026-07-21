namespace ChordFlow.Music.Rhythm.Generation;

/// <summary>
/// One bar of onset pattern — an ordered list of <see cref="Block"/>s, one per beat (4 in 4/4). Beats may
/// carry different subdivisions (per-beat runs — the Rhythm DSL already allows this; the v1 families keep a
/// bar uniform, but the model is future-proof by construction). Pure attack positions — no durations, no
/// instrument.
/// </summary>
public sealed record OnsetBar(IReadOnlyList<Block> Beats)
{
    /// <summary>True when every beat is empty — a silent bar.</summary>
    public bool IsEmpty => Beats.All(b => b.IsEmpty);

    /// <summary>A silent bar of <paramref name="beats"/> empty beats (used by RestBar / silence fills).</summary>
    public static OnsetBar Rest(int beats) =>
        new(Enumerable.Range(0, beats).Select(_ => Block.Empty(1)).ToArray());

    /// <summary>Total onsets in the bar (its density).</summary>
    public int OnsetCount => Beats.Sum(b => b.Onsets.Count);

    /// <summary>
    /// Build a bar from a flat set of <b>bar-relative cell indices</b> at <paramref name="subdivision"/> over
    /// <paramref name="beatsPerBar"/> beats — the authoring entry point for kinds/figures. Cell <c>c</c> lands on
    /// beat <c>c / subdivision</c> at intra-beat cell <c>c % subdivision</c>.
    /// </summary>
    public static OnsetBar FromCells(int subdivision, int beatsPerBar, IEnumerable<int> cells)
    {
        var byBeat = new List<int>[beatsPerBar];
        for (int b = 0; b < beatsPerBar; b++)
        {
            byBeat[b] = new List<int>();
        }

        foreach (int cell in cells)
        {
            byBeat[cell / subdivision].Add(cell % subdivision);
        }

        var blocks = new Block[beatsPerBar];
        for (int b = 0; b < beatsPerBar; b++)
        {
            blocks[b] = byBeat[b].Count == 0 ? Block.Empty(subdivision) : Block.Of(subdivision, byBeat[b].ToArray());
        }

        return new OnsetBar(blocks);
    }

    /// <summary>
    /// Build a bar from a cell <b>mask</b> — <c>x</c>/<c>X</c> = onset, any other char = rest — at
    /// <paramref name="subdivision"/> cells per beat. Mask length must be a whole number of beats; e.g.
    /// <c>FromMask(2, "x..x..x.")</c> is the tresillo (8 eighth-cells → 4 beats). The figure-catalog authoring form.
    /// </summary>
    public static OnsetBar FromMask(int subdivision, string mask)
    {
        int beatsPerBar = mask.Length / subdivision;
        var cells = mask.Select((c, i) => (c, i)).Where(t => t.c is 'x' or 'X').Select(t => t.i);
        return FromCells(subdivision, beatsPerBar, cells);
    }

    /// <summary>
    /// Every attack in the bar as a bar-relative tick (0 = the bar's downbeat), ascending. Beat <c>b</c>'s
    /// cell tick is offset by <c>b * ts.BeatTicks</c>. This is the single onset stream both projections read.
    /// </summary>
    public IEnumerable<int> OnsetTicks(TimeSignature ts)
    {
        for (int b = 0; b < Beats.Count; b++)
        {
            int beatOffset = b * ts.BeatTicks;
            foreach (int cellTick in Beats[b].OnsetTicks(ts.BeatTicks))
            {
                yield return beatOffset + cellTick;
            }
        }
    }
}
