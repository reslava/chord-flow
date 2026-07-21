namespace ChordFlow.Music.Rhythm.Generation;

/// <summary>
/// Shifts every onset of a bar pattern <see cref="Cells"/> cells later, wrapping at the bar end — the
/// syncopation/push transform kept from v1 (design §3a). Assumes a uniform subdivision across the bar (the v1
/// kinds are uniform); <c>Cells = 0</c> is the identity. E.g. downbeats `x.x.` displaced by 1 → `.x.x` (backbeat).
/// </summary>
public sealed record DisplaceTransform(int Cells)
{
    /// <summary>Return <paramref name="bar"/> with every onset shifted <see cref="Cells"/> cells later (bar-wrapping).</summary>
    public OnsetBar Apply(OnsetBar bar)
    {
        if (Cells == 0 || bar.IsEmpty)
        {
            return bar;
        }

        int subdivision = bar.Beats[0].Subdivision;
        int beatsPerBar = bar.Beats.Count;
        int totalCells = subdivision * beatsPerBar;
        var shifted = Enumerable.Range(0, beatsPerBar)
            .SelectMany(b => bar.Beats[b].Onsets.Select(k => b * subdivision + k))
            .Select(c => (((c + Cells) % totalCells) + totalCells) % totalCells);
        return OnsetBar.FromCells(subdivision, beatsPerBar, shifted);
    }
}
