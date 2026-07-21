namespace ChordFlow.Music.Rhythm.Generation;

/// <summary>
/// The free-fill generation strategy: seed-random onsets over <see cref="RandomParams.ContentBars"/> bars
/// drawn from a note-value palette, then <see cref="RandomParams.SilenceBars"/> empty bars (design §3b).
/// It is the Pattern strategy with the family opened up and the behaviour set to "random" — same
/// <see cref="OnsetGrid"/> output, same projections. v1 places onsets on a fixed sixteenth base grid.
/// </summary>
public static class RandomStrategy
{
    /// <summary>The v1 base subdivision (sixteenths) every random value is measured against.</summary>
    private const int BaseSubdivision = 4;

    /// <summary>Generate the onset grid for <paramref name="p"/>. Fails loud on out-of-range counts / palette.</summary>
    public static OnsetGrid Generate(RandomParams p)
    {
        ArgumentNullException.ThrowIfNull(p);
        if (p.ContentBars is < 1 or > 4)
        {
            throw new ArgumentOutOfRangeException(
                nameof(p), p.ContentBars, "ContentBars must be between 1 and 4 (req IN4).");
        }

        if (p.SilenceBars is < 0 or > 4)
        {
            throw new ArgumentOutOfRangeException(
                nameof(p), p.SilenceBars, "SilenceBars must be between 0 and 4 (req IN4).");
        }

        if (p.ValuePalette is null || p.ValuePalette.Count == 0)
        {
            throw new ArgumentException("The value palette must have at least one note value.", nameof(p));
        }

        if (p.RestProbability is < 0.0 or > 1.0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(p), p.RestProbability, "RestProbability must be between 0 and 1 (req IN12).");
        }

        int beatsPerBar = p.Ts.Numerator;
        int cellsPerBar = beatsPerBar * BaseSubdivision;
        int[] advances = p.ValuePalette.Select(v => ValueToBaseCells(v)).ToArray();

        var rng = new Random(p.Seed);
        var bars = new OnsetBar[p.ContentBars + p.SilenceBars];
        for (int i = 0; i < p.ContentBars; i++)
        {
            bars[i] = FillBar(rng, advances, beatsPerBar, cellsPerBar, p.RestProbability);
        }

        for (int i = 0; i < p.SilenceBars; i++)
        {
            bars[p.ContentBars + i] = OnsetBar.Rest(beatsPerBar);
        }

        return OnsetGrid.Of(bars, p.Ts);
    }

    // Walk the sixteenth grid: at each step the current cell is a rest (with restProbability) or an onset,
    // then advance by a random palette value, until the bar is full. Beat 1 is not forced — it may rest.
    // Group the landed onset cells by beat into blocks.
    private static OnsetBar FillBar(Random rng, int[] advances, int beatsPerBar, int cellsPerBar, double restProbability)
    {
        var onsetCells = new List<int>();
        int pos = 0;
        while (pos < cellsPerBar)
        {
            if (rng.NextDouble() >= restProbability)
            {
                onsetCells.Add(pos);
            }

            pos += advances[rng.Next(advances.Length)];
        }

        var beats = new Block[beatsPerBar];
        for (int b = 0; b < beatsPerBar; b++)
        {
            int beatStart = b * BaseSubdivision;
            int[] cells = onsetCells
                .Where(c => c >= beatStart && c < beatStart + BaseSubdivision)
                .Select(c => c - beatStart)
                .ToArray();
            beats[b] = cells.Length == 0 ? Block.Empty(BaseSubdivision) : Block.Of(BaseSubdivision, cells);
        }

        return new OnsetBar(beats);
    }

    // alphaTex note value (4/8/16 …) → whole base cells to advance (a quarter = 4 sixteenths). Must divide evenly.
    private static int ValueToBaseCells(int value)
    {
        int cellsPerBeat = BaseSubdivision;              // 4 sixteenths per quarter
        int cellsPerWhole = cellsPerBeat * 4;            // 16 sixteenths per whole note
        if (value < 1 || cellsPerWhole % value != 0)
        {
            throw new ArgumentException(
                $"Value {value} is not on the v1 sixteenth grid (must divide {cellsPerWhole}; triplets are EX3).");
        }

        return cellsPerWhole / value;
    }
}
