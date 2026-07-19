using ChordFlow.Music.Rhythm;

namespace ChordFlow.Instruments.Drums;

/// <summary>
/// One voice's hits within a single bar of a <see cref="DrumGroove"/> — a percussion lane. Reuses the
/// pure <see cref="RhythmEvent"/> tick-grid primitive (req C2, "two DSLs, one model"): each hit is a
/// one-cell <see cref="RhythmEvent"/> whose <see cref="RhythmEvent.Position"/> is the bar-relative onset
/// tick and whose <see cref="RhythmEvent.Length"/> is the authoring cell width. Percussion hits are
/// instantaneous, so stroke/accent are unused here and length is only the occupied cell — the renderer
/// derives note durations from the merged onset grid, not from a lane's individual lengths.
/// </summary>
public sealed record DrumLane(DrumVoice Voice, IReadOnlyList<RhythmEvent> Events);

/// <summary>
/// One bar of a groove: the set of <see cref="DrumLane"/>s sounding in it (at most one per
/// <see cref="DrumVoice"/>). Bar-major so the renderer walks bar-by-bar and merges lanes into
/// simultaneous-hit groups at each onset; the DSL authors lane-major (rows) and the parser transposes.
/// </summary>
public sealed record DrumBar(IReadOnlyList<DrumLane> Lanes);

/// <summary>
/// A drum groove — ChordFlow's first percussion play-unit and the drums peer of <see cref="RhythmPattern"/>.
/// A groove is a <b>multi-lane rhythm</b> over the fixed 48-PPQ tick grid: one or more <see cref="DrumBar"/>s,
/// each a set of voice lanes. It carries <b>no harmony</b> — no key, no chords, no <c>Song</c>/<c>Exercise</c>
/// (req C6); it renders and plays standalone. Multi-bar from the start (durable shape): a one-bar groove is a
/// single <see cref="DrumBar"/> — use <see cref="SingleBar"/> — so multi-bar tiling under a progression stays
/// an additive, phase-2 feature (<c>drums/drums-under-a-song</c>). 4/4 only for v1 (req C8).
/// </summary>
public sealed record DrumGroove(
    string Id,
    string Name,
    IReadOnlyList<DrumBar> Bars,
    TimeSignature TimeSignature)
{
    /// <summary>Construct a single-bar groove from its lanes — the common case.</summary>
    public static DrumGroove SingleBar(
        string id,
        string name,
        IReadOnlyList<DrumLane> lanes,
        TimeSignature timeSignature) =>
        new(id, name, new[] { new DrumBar(lanes) }, timeSignature);

    /// <summary>
    /// The distinct voices used anywhere in the groove, in first-seen order (bar 0's lanes first). The row
    /// set DrumsR draws and the renderer/diagram enumerate; stable ordering keeps the drawn grid consistent.
    /// </summary>
    public IReadOnlyList<DrumVoice> DistinctVoices()
    {
        var seen = new List<DrumVoice>();
        foreach (DrumBar bar in Bars)
        {
            foreach (DrumLane lane in bar.Lanes)
            {
                if (!seen.Contains(lane.Voice))
                {
                    seen.Add(lane.Voice);
                }
            }
        }

        return seen;
    }
}
