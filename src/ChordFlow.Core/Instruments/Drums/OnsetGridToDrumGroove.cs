using ChordFlow.Music.Rhythm;
using ChordFlow.Music.Rhythm.Generation;

namespace ChordFlow.Instruments.Drums;

/// <summary>
/// Projects an <see cref="OnsetGrid"/> to a single-lane <see cref="DrumGroove"/> for the drums path
/// (design §2b). Each onset becomes a one-cell hit (<see cref="RhythmEvent.Hit"/>) on one
/// <see cref="DrumLane"/> of the chosen <see cref="DrumVoice"/> — onsets map 1:1, no sustain policy (a drum
/// hit is instantaneous, req IN5). One <see cref="DrumBar"/> per generated bar (an empty bar keeps the lane
/// with no hits, so DrumsR still draws the voice row). This lives under <c>Instruments/Drums</c> because it
/// targets a Drums type — the legal <c>Instruments → Music</c> direction; the reverse edge (req C2) is never
/// crossed.
/// </summary>
public static class OnsetGridToDrumGroove
{
    /// <summary>Project <paramref name="grid"/> to a single-voice groove (default closed hi-hat).</summary>
    public static DrumGroove Project(
        OnsetGrid grid,
        DrumVoice voice = DrumVoice.HiHatClosed,
        string id = "generated",
        string name = "Generated Rhythm")
    {
        ArgumentNullException.ThrowIfNull(grid);
        TimeSignature ts = grid.TimeSignature;

        var bars = new DrumBar[grid.Bars.Count];
        for (int b = 0; b < grid.Bars.Count; b++)
        {
            OnsetBar bar = grid.Bars[b];
            var hits = new List<RhythmEvent>();
            for (int beat = 0; beat < bar.Beats.Count; beat++)
            {
                Block block = bar.Beats[beat];
                int cellTicks = ts.BeatTicks / block.Subdivision;
                int beatOffset = beat * ts.BeatTicks;
                foreach (int k in block.Onsets)
                {
                    hits.Add(RhythmEvent.Hit(beatOffset + k * cellTicks, cellTicks));
                }
            }

            bars[b] = new DrumBar(new[] { new DrumLane(voice, hits) });
        }

        return new DrumGroove(id, name, bars, ts);
    }
}
