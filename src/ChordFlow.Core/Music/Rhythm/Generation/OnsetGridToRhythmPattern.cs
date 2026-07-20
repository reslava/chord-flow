namespace ChordFlow.Music.Rhythm.Generation;

/// <summary>
/// Projects an <see cref="OnsetGrid"/> to a <see cref="RhythmPattern"/> for the comping/lead path
/// (design §2a). The <b>ring-to-next-onset</b> (legato) policy is fixed for v1 (req IN5/EX7): each onset
/// becomes a <see cref="RhythmEvent"/> that lasts until the next onset in the bar, and the last onset of a
/// bar rings to the barline (no cross-bar tie — req EX4). An empty bar becomes an empty
/// <see cref="PatternBar"/> (the quantizer emits a whole-bar rest). Because durations are onset-to-onset (or
/// onset-to-barline) they stay inside the verified <c>:N</c> + rest vocabulary — no unverified tie/dotted
/// token ever reaches the renderer (req C4). Pure Music (both types are Music types).
/// </summary>
public static class OnsetGridToRhythmPattern
{
    /// <summary>Project <paramref name="grid"/> to a legato <see cref="RhythmPattern"/>.</summary>
    public static RhythmPattern Project(
        OnsetGrid grid, string id = "generated", string name = "Generated Rhythm")
    {
        ArgumentNullException.ThrowIfNull(grid);
        TimeSignature ts = grid.TimeSignature;
        int barTicks = ts.BarTicks;

        var bars = new PatternBar[grid.Bars.Count];
        for (int b = 0; b < grid.Bars.Count; b++)
        {
            int[] onsets = grid.Bars[b].OnsetTicks(ts).ToArray();
            var events = new RhythmEvent[onsets.Length];
            for (int i = 0; i < onsets.Length; i++)
            {
                int end = i + 1 < onsets.Length ? onsets[i + 1] : barTicks;
                events[i] = RhythmEvent.Hit(onsets[i], end - onsets[i]);
            }

            bars[b] = new PatternBar(events);
        }

        return new RhythmPattern(id, name, bars, ts);
    }
}
