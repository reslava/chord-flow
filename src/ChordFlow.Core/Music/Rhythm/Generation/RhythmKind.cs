namespace ChordFlow.Music.Rhythm.Generation;

/// <summary>
/// A <b>kind</b> of rhythm — an ordered set of candidate <b>bar patterns</b> (each an <see cref="OnsetBar"/>)
/// the Pattern strategy draws from (design §3a v2). A generated <b>family</b> enumerates every bar of a given
/// density/placement; a curated <b>figure</b> is a small set (a singleton for a one-bar figure, an ordered
/// sequence for a multi-bar clave played via <c>Cycle</c>). <see cref="Category"/> is <c>density</c> /
/// <c>placement</c> / <c>figure</c> for grouping the picker. The selection layer decides how bars are drawn
/// from <see cref="Patterns"/> across a phrase.
/// </summary>
public sealed record RhythmKind(string Id, string Name, string Category, IReadOnlyList<OnsetBar> Patterns)
{
    private const int BeatsPerBar = 4; // 4/4 only (req EX5)

    /// <summary>Every bar with exactly <paramref name="onsetCount"/> onsets among the subdivision's cells.</summary>
    public static RhythmKind Density(int subdivision, int onsetCount)
    {
        int totalCells = subdivision * BeatsPerBar;
        var patterns = Combinations(Enumerable.Range(0, totalCells).ToArray(), onsetCount)
            .Select(cells => OnsetBar.FromCells(subdivision, BeatsPerBar, cells))
            .ToArray();
        string sub = SubName(subdivision);
        return new RhythmKind($"density:{sub}:{onsetCount}", $"{Cap(sub)} · {onsetCount}-onset", "density", patterns);
    }

    /// <summary>Density restricted to a cell <paramref name="region"/> — <c>onbeat</c> (downbeat cells) /
    /// <c>offbeat</c> (the <c>&amp;</c>s) / <c>all</c> — the syncopation-axis families.</summary>
    public static RhythmKind Placement(int subdivision, string region, int onsetCount)
    {
        int totalCells = subdivision * BeatsPerBar;
        var regionCells = Enumerable.Range(0, totalCells).Where(c => InRegion(c, subdivision, region)).ToArray();
        var patterns = Combinations(regionCells, onsetCount)
            .Select(cells => OnsetBar.FromCells(subdivision, BeatsPerBar, cells))
            .ToArray();
        return new RhythmKind(
            $"placement:{SubName(subdivision)}:{region}:{onsetCount}",
            $"{Cap(SubName(subdivision))} {region} · {onsetCount}-onset", "placement", patterns);
    }

    private static bool InRegion(int cell, int subdivision, string region) => region switch
    {
        "onbeat" => cell % subdivision == 0,
        "offbeat" => cell % subdivision != 0,
        _ => true,
    };

    private static string SubName(int subdivision) =>
        subdivision == 1 ? "quarter" : subdivision == 2 ? "eighth" : $"sub{subdivision}";

    private static string Cap(string s) => char.ToUpperInvariant(s[0]) + s[1..];

    // All k-combinations of items (ascending), standard next-combination stepping. Empty for k out of [0, n].
    private static IEnumerable<int[]> Combinations(int[] items, int k)
    {
        if (k < 0 || k > items.Length)
        {
            yield break;
        }

        if (k == 0)
        {
            yield return Array.Empty<int>();
            yield break;
        }

        var idx = Enumerable.Range(0, k).ToArray();
        while (true)
        {
            yield return idx.Select(i => items[i]).ToArray();
            int p = k - 1;
            while (p >= 0 && idx[p] == items.Length - k + p)
            {
                p--;
            }

            if (p < 0)
            {
                yield break;
            }

            idx[p]++;
            for (int j = p + 1; j < k; j++)
            {
                idx[j] = idx[j - 1] + 1;
            }
        }
    }
}
